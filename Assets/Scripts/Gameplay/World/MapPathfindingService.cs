// Purpose: Service de pathfinding intelligent pour trouver des chemins entre POI non directement connectes
// Filepath: Assets/Scripts/Gameplay/World/MapPathfindingService.cs
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Service qui calcule les chemins optimaux entre locations en utilisant l'algorithme de Dijkstra
/// </summary>
public class MapPathfindingService
{
    private readonly LocationRegistry locationRegistry;
    private Dictionary<string, Dictionary<string, PathResult>> pathCache;
    private bool isCacheValid;

    public MapPathfindingService(LocationRegistry registry)
    {
        locationRegistry = registry;
        pathCache = new Dictionary<string, Dictionary<string, PathResult>>();
        isCacheValid = false;
    }

    /// <summary>
    /// Resultat d'un calcul de chemin
    /// </summary>
    public class PathResult
    {
        public bool IsReachable { get; set; }
        public int TotalCost { get; set; }
        public List<string> Path { get; set; } // Liste des LocationID du chemin
        public List<PathSegment> Segments { get; set; } // Details de chaque segment

        public PathResult()
        {
            Path = new List<string>();
            Segments = new List<PathSegment>();
        }
    }

    /// <summary>
    /// Un segment individuel du chemin (A -> B avec coût)
    /// </summary>
    public class PathSegment
    {
        public string FromLocationId { get; set; }
        public string ToLocationId { get; set; }
        public int StepCost { get; set; }
        public MapLocationDefinition FromLocation { get; set; }
        public MapLocationDefinition ToLocation { get; set; }
    }

    /// <summary>
    /// Trouve le chemin optimal entre deux locations
    /// </summary>
    public PathResult FindPath(string fromLocationId, string toLocationId)
    {
        if (string.IsNullOrEmpty(fromLocationId) || string.IsNullOrEmpty(toLocationId))
        {
            Logger.LogError("MapPathfindingService: Invalid location IDs provided");
            return new PathResult { IsReachable = false };
        }

        if (fromLocationId == toLocationId)
        {
            Logger.LogInfo("MapPathfindingService: Source and destination are the same");
            return CreateSameLocationResult(fromLocationId);
        }

        // Verifier le cache d'abord
        if (isCacheValid && pathCache.ContainsKey(fromLocationId) && pathCache[fromLocationId].ContainsKey(toLocationId))
        {
            Logger.LogInfo($"MapPathfindingService: Using cached path from {fromLocationId} to {toLocationId}");
            return pathCache[fromLocationId][toLocationId];
        }

        // Calculer le chemin avec Dijkstra
        var result = CalculateShortestPath(fromLocationId, toLocationId);

        // Mettre en cache si valide
        if (result.IsReachable)
        {
            CachePathResult(fromLocationId, toLocationId, result);
        }

        return result;
    }

    /// <summary>
    /// Verifie si deux locations peuvent etre connectees (directement ou indirectement)
    /// </summary>
    public bool CanReach(string fromLocationId, string toLocationId)
    {
        var pathResult = FindPath(fromLocationId, toLocationId);
        return pathResult.IsReachable;
    }

    /// <summary>
    /// Obtient le coût total pour voyager entre deux locations
    /// </summary>
    public int GetTotalTravelCost(string fromLocationId, string toLocationId)
    {
        var pathResult = FindPath(fromLocationId, toLocationId);
        return pathResult.IsReachable ? pathResult.TotalCost : -1;
    }

    /// <summary>
    /// Invalide le cache (a appeler quand les connexions changent)
    /// </summary>
    public void InvalidateCache()
    {
        pathCache.Clear();
        isCacheValid = false;
        Logger.LogInfo("MapPathfindingService: Path cache invalidated");
    }

    /// <summary>
    /// Reconstruit le cache complet (utile au demarrage)
    /// </summary>
    public void RebuildCache()
    {
        InvalidateCache();

        if (locationRegistry?.AllLocations == null) return;

        var validLocations = locationRegistry.AllLocations.Where(loc => loc != null && !string.IsNullOrEmpty(loc.LocationID)).ToList();

        Logger.LogInfo($"MapPathfindingService: Rebuilding path cache for {validLocations.Count} locations...");

        // Calculer tous les chemins possibles
        foreach (var fromLocation in validLocations)
        {
            foreach (var toLocation in validLocations)
            {
                if (fromLocation.LocationID != toLocation.LocationID)
                {
                    var pathResult = CalculateShortestPath(fromLocation.LocationID, toLocation.LocationID);
                    if (pathResult.IsReachable)
                    {
                        CachePathResult(fromLocation.LocationID, toLocation.LocationID, pathResult);
                    }
                }
            }
        }

        isCacheValid = true;
        Logger.LogInfo($"MapPathfindingService: Cache rebuilt successfully");
    }

    #region Algorithme de Dijkstra

    /// <summary>
    /// Implementation de l'algorithme de Dijkstra pour trouver le chemin le plus court
    /// </summary>
    private PathResult CalculateShortestPath(string startLocationId, string targetLocationId)
    {
        // Verifier que les locations existent
        if (!locationRegistry.HasLocation(startLocationId) || !locationRegistry.HasLocation(targetLocationId))
        {
            Logger.LogWarning($"MapPathfindingService: One or both locations not found: {startLocationId}, {targetLocationId}");
            return new PathResult { IsReachable = false };
        }

        // Structures pour Dijkstra
        var distances = new Dictionary<string, int>();
        var previous = new Dictionary<string, string>();
        var unvisited = new HashSet<string>();

        // Initialiser toutes les locations
        foreach (var location in locationRegistry.AllLocations)
        {
            if (location != null && !string.IsNullOrEmpty(location.LocationID))
            {
                distances[location.LocationID] = location.LocationID == startLocationId ? 0 : int.MaxValue;
                unvisited.Add(location.LocationID);
            }
        }

        while (unvisited.Count > 0)
        {
            // Trouver la location non visitee avec la distance minimale
            // Using Aggregate instead of OrderBy().First() for O(n) instead of O(n log n)
            string currentLocationId = unvisited.Aggregate((min, id) =>
                distances[id] < distances[min] ? id : min);

            if (distances[currentLocationId] == int.MaxValue)
            {
                // Plus de locations accessibles
                break;
            }

            unvisited.Remove(currentLocationId);

            // Si on a atteint la destination
            if (currentLocationId == targetLocationId)
            {
                return BuildPathResult(startLocationId, targetLocationId, distances, previous);
            }

            // Examiner tous les voisins
            var currentLocation = locationRegistry.GetLocationById(currentLocationId);
            if (currentLocation?.Connections != null)
            {
                foreach (var connection in currentLocation.Connections)
                {
                    if (!connection.IsAvailable || connection.StepCost <= 0) continue;

                    string neighborId = connection.DestinationLocationID;
                    if (!unvisited.Contains(neighborId)) continue;

                    int newDistance = distances[currentLocationId] + connection.StepCost;
                    if (newDistance < distances[neighborId])
                    {
                        distances[neighborId] = newDistance;
                        previous[neighborId] = currentLocationId;
                    }
                }
            }
        }

        // Aucun chemin trouve
        Logger.LogInfo($"MapPathfindingService: No path found from {startLocationId} to {targetLocationId}");
        return new PathResult { IsReachable = false };
    }

    /// <summary>
    /// Construit le resultat du chemin a partir des donnees de Dijkstra
    /// </summary>
    private PathResult BuildPathResult(string startLocationId, string targetLocationId,
                                     Dictionary<string, int> distances, Dictionary<string, string> previous)
    {
        var result = new PathResult
        {
            IsReachable = true,
            TotalCost = distances[targetLocationId]
        };

        // Reconstruire le chemin en remontant depuis la destination
        var path = new List<string>();
        string currentId = targetLocationId;

        while (currentId != null)
        {
            path.Add(currentId);
            previous.TryGetValue(currentId, out currentId);
        }

        path.Reverse();
        result.Path = path;

        // Creer les segments detailles
        for (int i = 0; i < path.Count - 1; i++)
        {
            string fromId = path[i];
            string toId = path[i + 1];

            var fromLocation = locationRegistry.GetLocationById(fromId);
            var toLocation = locationRegistry.GetLocationById(toId);
            int stepCost = locationRegistry.GetTravelCost(fromId, toId);

            result.Segments.Add(new PathSegment
            {
                FromLocationId = fromId,
                ToLocationId = toId,
                StepCost = stepCost,
                FromLocation = fromLocation,
                ToLocation = toLocation
            });
        }

        Logger.LogInfo($"MapPathfindingService: Path found from {startLocationId} to {targetLocationId} " +
                      $"({result.TotalCost} steps, {result.Segments.Count} segments)");

        return result;
    }

    #endregion

    #region Gestion du Cache

    /// <summary>
    /// Met en cache un resultat de chemin
    /// </summary>
    private void CachePathResult(string fromLocationId, string toLocationId, PathResult result)
    {
        if (!pathCache.ContainsKey(fromLocationId))
        {
            pathCache[fromLocationId] = new Dictionary<string, PathResult>();
        }
        pathCache[fromLocationId][toLocationId] = result;
    }

    /// <summary>
    /// Cree un resultat pour le cas où source = destination
    /// </summary>
    private PathResult CreateSameLocationResult(string locationId)
    {
        var location = locationRegistry.GetLocationById(locationId);
        return new PathResult
        {
            IsReachable = true,
            TotalCost = 0,
            Path = new List<string> { locationId },
            Segments = new List<PathSegment>()
        };
    }

    #endregion

    #region Debug et Diagnostics

    /// <summary>
    /// Affiche des informations de debug sur un chemin
    /// </summary>
    public void DebugPath(string fromLocationId, string toLocationId)
    {
        var pathResult = FindPath(fromLocationId, toLocationId);

        if (!pathResult.IsReachable)
        {
            Logger.LogInfo($"DEBUG PATH: No path exists from {fromLocationId} to {toLocationId}");
            return;
        }

        Logger.LogInfo($"DEBUG PATH: {fromLocationId} → {toLocationId} (Total: {pathResult.TotalCost} steps)");
        foreach (var segment in pathResult.Segments)
        {
            var fromName = segment.FromLocation?.DisplayName ?? segment.FromLocationId;
            var toName = segment.ToLocation?.DisplayName ?? segment.ToLocationId;
            Logger.LogInfo($"  • {fromName} → {toName} ({segment.StepCost} steps)");
        }
    }

    /// <summary>
    /// Retourne des statistiques sur le cache
    /// </summary>
    public void LogCacheStats()
    {
        int totalPaths = pathCache.Values.Sum(dict => dict.Count);
        Logger.LogInfo($"MapPathfindingService: Cache contains {totalPaths} precomputed paths across {pathCache.Count} source locations");
    }

    #endregion
}