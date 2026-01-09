// Purpose: Utility methods for map graph operations (auto-layout, validation, etc.)
// Filepath: Assets/Scripts/Editor/MapGraph/MapGraphUtility.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Static utility class providing helper methods for the Map Editor.
/// Includes force-directed auto-layout, validation, and save operations.
/// </summary>
public static class MapGraphUtility
{
    // Layout constants
    private const float REPULSION_STRENGTH = 5000f;
    private const float ATTRACTION_STRENGTH = 0.1f;
    private const float GRAVITY_STRENGTH = 0.05f;
    private const float DAMPING = 0.9f;
    private const int MAX_ITERATIONS = 150;
    private const float MIN_DISTANCE = 50f;
    private const float CONVERGENCE_THRESHOLD = 0.5f;

    /// <summary>
    /// Calculate auto-layout positions using force-directed algorithm
    /// </summary>
    public static Dictionary<string, Vector2> CalculateAutoLayout(LocationRegistry registry, float nodeWidth, float nodeHeight)
    {
        var positions = new Dictionary<string, Vector2>();
        var velocities = new Dictionary<string, Vector2>();

        if (registry == null || registry.AllLocations == null)
            return positions;

        var locations = registry.AllLocations.Where(l => l != null).ToList();
        if (locations.Count == 0)
            return positions;

        // Initialize positions in a circle or use existing
        float radius = Mathf.Max(200f, locations.Count * 50f);
        Vector2 center = new Vector2(400, 300);

        for (int i = 0; i < locations.Count; i++)
        {
            var location = locations[i];
            Vector2 initialPos;

            if (location.EditorPosition != Vector2.zero)
            {
                initialPos = location.EditorPosition;
            }
            else
            {
                // Arrange in circle
                float angle = (2 * Mathf.PI * i) / locations.Count;
                initialPos = center + new Vector2(
                    Mathf.Cos(angle) * radius,
                    Mathf.Sin(angle) * radius
                );
            }

            positions[location.LocationID] = initialPos;
            velocities[location.LocationID] = Vector2.zero;
        }

        // Build connection lookup
        var connections = new Dictionary<string, HashSet<string>>();
        foreach (var location in locations)
        {
            connections[location.LocationID] = new HashSet<string>();
            if (location.Connections != null)
            {
                foreach (var conn in location.Connections.Where(c => c != null))
                {
                    if (!string.IsNullOrEmpty(conn.DestinationLocationID))
                    {
                        connections[location.LocationID].Add(conn.DestinationLocationID);
                    }
                }
            }
        }

        // Run force-directed algorithm
        for (int iteration = 0; iteration < MAX_ITERATIONS; iteration++)
        {
            var forces = new Dictionary<string, Vector2>();
            foreach (var location in locations)
            {
                forces[location.LocationID] = Vector2.zero;
            }

            // Calculate repulsive forces between all nodes
            for (int i = 0; i < locations.Count; i++)
            {
                for (int j = i + 1; j < locations.Count; j++)
                {
                    var loc1 = locations[i];
                    var loc2 = locations[j];

                    Vector2 pos1 = positions[loc1.LocationID];
                    Vector2 pos2 = positions[loc2.LocationID];

                    Vector2 delta = pos1 - pos2;
                    float distance = Mathf.Max(delta.magnitude, MIN_DISTANCE);
                    Vector2 direction = delta.normalized;

                    // Coulomb's law: F = k / d^2
                    float forceMagnitude = REPULSION_STRENGTH / (distance * distance);
                    Vector2 force = direction * forceMagnitude;

                    forces[loc1.LocationID] += force;
                    forces[loc2.LocationID] -= force;
                }
            }

            // Calculate attractive forces along connections
            foreach (var location in locations)
            {
                Vector2 pos1 = positions[location.LocationID];

                foreach (var destId in connections[location.LocationID])
                {
                    if (!positions.ContainsKey(destId))
                        continue;

                    Vector2 pos2 = positions[destId];
                    Vector2 delta = pos2 - pos1;
                    float distance = delta.magnitude;

                    if (distance < MIN_DISTANCE)
                        continue;

                    Vector2 direction = delta.normalized;

                    // Hooke's law: F = k * d
                    float forceMagnitude = ATTRACTION_STRENGTH * distance;
                    Vector2 force = direction * forceMagnitude;

                    forces[location.LocationID] += force;
                }
            }

            // Apply gravity toward center
            foreach (var location in locations)
            {
                Vector2 pos = positions[location.LocationID];
                Vector2 toCenter = center - pos;
                forces[location.LocationID] += toCenter * GRAVITY_STRENGTH;
            }

            // Apply forces and check convergence
            float maxMovement = 0f;

            foreach (var location in locations)
            {
                string id = location.LocationID;

                // Update velocity with damping
                velocities[id] = (velocities[id] + forces[id]) * DAMPING;

                // Update position
                positions[id] += velocities[id];

                // Track max movement for convergence check
                maxMovement = Mathf.Max(maxMovement, velocities[id].magnitude);
            }

            // Check for convergence
            if (maxMovement < CONVERGENCE_THRESHOLD)
            {
                Logger.LogInfo($"Auto-layout converged after {iteration + 1} iterations", Logger.LogCategory.EditorLog);
                break;
            }
        }

        // Normalize positions to start from positive coordinates
        float minX = float.MaxValue, minY = float.MaxValue;
        foreach (var pos in positions.Values)
        {
            minX = Mathf.Min(minX, pos.x);
            minY = Mathf.Min(minY, pos.y);
        }

        // Offset all positions to ensure they're in positive space with padding
        var finalPositions = new Dictionary<string, Vector2>();
        float padding = 50f;
        foreach (var kvp in positions)
        {
            finalPositions[kvp.Key] = new Vector2(
                kvp.Value.x - minX + padding,
                kvp.Value.y - minY + padding
            );
        }

        return finalPositions;
    }

    /// <summary>
    /// Validate the map structure and return issues
    /// </summary>
    public static List<string> ValidateMap(LocationRegistry registry)
    {
        var issues = new List<string>();

        if (registry == null || registry.AllLocations == null)
        {
            issues.Add("Registry is null or empty");
            return issues;
        }

        var locationIds = new HashSet<string>();

        foreach (var location in registry.AllLocations.Where(l => l != null))
        {
            // Check for duplicate IDs
            if (locationIds.Contains(location.LocationID))
            {
                issues.Add($"Duplicate LocationID: {location.LocationID}");
            }
            else
            {
                locationIds.Add(location.LocationID);
            }

            // Check for empty ID
            if (string.IsNullOrEmpty(location.LocationID))
            {
                issues.Add($"Location '{location.name}' has empty LocationID");
            }

            // Check connections
            if (location.Connections != null)
            {
                foreach (var conn in location.Connections.Where(c => c != null))
                {
                    // Check for empty destination
                    if (string.IsNullOrEmpty(conn.DestinationLocationID))
                    {
                        issues.Add($"Location '{location.LocationID}' has connection with empty destination");
                    }
                    // Check for invalid step cost
                    else if (conn.StepCost <= 0)
                    {
                        issues.Add($"Location '{location.LocationID}' has connection to '{conn.DestinationLocationID}' with invalid step cost: {conn.StepCost}");
                    }
                }
            }
        }

        // Check for broken references
        foreach (var location in registry.AllLocations.Where(l => l != null))
        {
            if (location.Connections == null) continue;

            foreach (var conn in location.Connections.Where(c => c != null))
            {
                if (!string.IsNullOrEmpty(conn.DestinationLocationID) &&
                    !locationIds.Contains(conn.DestinationLocationID))
                {
                    issues.Add($"Location '{location.LocationID}' has connection to non-existent location: '{conn.DestinationLocationID}'");
                }
            }
        }

        return issues;
    }

    /// <summary>
    /// Find isolated locations (no connections to or from)
    /// </summary>
    public static List<MapLocationDefinition> FindIsolatedLocations(LocationRegistry registry)
    {
        var isolated = new List<MapLocationDefinition>();

        if (registry == null || registry.AllLocations == null)
            return isolated;

        // Build set of all connected locations
        var connectedLocations = new HashSet<string>();

        foreach (var location in registry.AllLocations.Where(l => l != null))
        {
            if (location.Connections != null && location.Connections.Any(c => c != null))
            {
                connectedLocations.Add(location.LocationID);

                foreach (var conn in location.Connections.Where(c => c != null))
                {
                    if (!string.IsNullOrEmpty(conn.DestinationLocationID))
                    {
                        connectedLocations.Add(conn.DestinationLocationID);
                    }
                }
            }
        }

        // Find locations not in the connected set
        foreach (var location in registry.AllLocations.Where(l => l != null))
        {
            if (!connectedLocations.Contains(location.LocationID))
            {
                isolated.Add(location);
            }
        }

        return isolated;
    }

    /// <summary>
    /// Check if a connection exists between two locations
    /// </summary>
    public static bool HasConnection(MapLocationDefinition from, string toLocationId)
    {
        if (from?.Connections == null)
            return false;

        return from.Connections.Any(c => c?.DestinationLocationID == toLocationId);
    }

    /// <summary>
    /// Check if connections are bidirectional between two locations
    /// </summary>
    public static bool IsBidirectional(LocationRegistry registry, string locationId1, string locationId2)
    {
        var loc1 = registry?.GetLocationById(locationId1);
        var loc2 = registry?.GetLocationById(locationId2);

        if (loc1 == null || loc2 == null)
            return false;

        bool has1to2 = HasConnection(loc1, locationId2);
        bool has2to1 = HasConnection(loc2, locationId1);

        return has1to2 && has2to1;
    }

    /// <summary>
    /// Add a connection between two locations
    /// </summary>
    public static void AddConnection(MapLocationDefinition from, string toLocationId, int stepCost, bool bidirectional, LocationRegistry registry)
    {
        if (from == null || string.IsNullOrEmpty(toLocationId))
            return;

        // Add connection from -> to
        if (!HasConnection(from, toLocationId))
        {
            if (from.Connections == null)
                from.Connections = new List<LocationConnection>();

            from.Connections.Add(new LocationConnection
            {
                DestinationLocationID = toLocationId,
                StepCost = stepCost,
                IsAvailable = true
            });

            EditorUtility.SetDirty(from);
        }

        // Add reverse connection if bidirectional
        if (bidirectional && registry != null)
        {
            var toLocation = registry.GetLocationById(toLocationId);
            if (toLocation != null && !HasConnection(toLocation, from.LocationID))
            {
                if (toLocation.Connections == null)
                    toLocation.Connections = new List<LocationConnection>();

                toLocation.Connections.Add(new LocationConnection
                {
                    DestinationLocationID = from.LocationID,
                    StepCost = stepCost,
                    IsAvailable = true
                });

                EditorUtility.SetDirty(toLocation);
            }
        }

        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Remove a connection between two locations
    /// </summary>
    public static void RemoveConnection(MapLocationDefinition from, string toLocationId, bool removeBidirectional, LocationRegistry registry)
    {
        if (from?.Connections == null)
            return;

        // Remove from -> to
        int removed = from.Connections.RemoveAll(c => c?.DestinationLocationID == toLocationId);
        if (removed > 0)
        {
            EditorUtility.SetDirty(from);
        }

        // Remove reverse if requested
        if (removeBidirectional && registry != null)
        {
            var toLocation = registry.GetLocationById(toLocationId);
            if (toLocation?.Connections != null)
            {
                int removedReverse = toLocation.Connections.RemoveAll(c => c?.DestinationLocationID == from.LocationID);
                if (removedReverse > 0)
                {
                    EditorUtility.SetDirty(toLocation);
                }
            }
        }

        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Load the main LocationRegistry asset
    /// </summary>
    public static LocationRegistry LoadLocationRegistry()
    {
        string[] guids = AssetDatabase.FindAssets("t:LocationRegistry");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<LocationRegistry>(path);
        }
        return null;
    }

    /// <summary>
    /// Delete a POI and all its connections.
    /// This removes:
    /// - All connections FROM this location
    /// - All connections TO this location (from other locations)
    /// - The POI GameObject from the scene
    /// - Optionally the MapLocationDefinition from the registry
    /// </summary>
    /// <param name="locationId">The location ID to delete</param>
    /// <param name="registry">The location registry</param>
    /// <param name="deleteLocationAsset">If true, also deletes the MapLocationDefinition ScriptableObject</param>
    /// <returns>True if deletion was successful</returns>
    public static bool DeletePOI(string locationId, LocationRegistry registry, bool deleteLocationAsset = false)
    {
        if (string.IsNullOrEmpty(locationId) || registry == null)
        {
            Logger.LogWarning("DeletePOI: Invalid locationId or registry", Logger.LogCategory.EditorLog);
            return false;
        }

        var location = registry.GetLocationById(locationId);
        if (location == null)
        {
            Logger.LogWarning($"DeletePOI: Location '{locationId}' not found in registry", Logger.LogCategory.EditorLog);
            return false;
        }

        // Track what we're doing for undo
        Undo.SetCurrentGroupName($"Delete POI {locationId}");
        int undoGroup = Undo.GetCurrentGroup();

        try
        {
            // 1. Remove all connections TO this location from other locations
            int removedIncomingConnections = 0;
            foreach (var otherLocation in registry.AllLocations.Where(l => l != null && l.LocationID != locationId))
            {
                if (otherLocation.Connections != null)
                {
                    int removed = otherLocation.Connections.RemoveAll(c => c?.DestinationLocationID == locationId);
                    if (removed > 0)
                    {
                        removedIncomingConnections += removed;
                        EditorUtility.SetDirty(otherLocation);
                    }
                }
            }

            // 2. Clear connections FROM this location (already handled by deleting, but be explicit)
            int removedOutgoingConnections = location.Connections?.Count ?? 0;
            if (location.Connections != null)
            {
                location.Connections.Clear();
                EditorUtility.SetDirty(location);
            }

            // 3. Find and delete the POI GameObject in the scene
            bool deletedGameObject = DeletePOIGameObject(locationId);

            // 4. Remove from registry if requested
            if (deleteLocationAsset)
            {
                registry.AllLocations.Remove(location);
                EditorUtility.SetDirty(registry);

                // Delete the asset file
                string assetPath = AssetDatabase.GetAssetPath(location);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    AssetDatabase.DeleteAsset(assetPath);
                }
            }

            AssetDatabase.SaveAssets();

            Logger.LogInfo($"DeletePOI: Deleted '{locationId}' - Removed {removedIncomingConnections} incoming connections, {removedOutgoingConnections} outgoing connections, GameObject deleted: {deletedGameObject}", Logger.LogCategory.EditorLog);

            Undo.CollapseUndoOperations(undoGroup);
            return true;
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"DeletePOI: Error deleting '{locationId}': {ex.Message}", Logger.LogCategory.EditorLog);
            return false;
        }
    }

    /// <summary>
    /// Find and delete a POI GameObject from the scene by its location ID
    /// </summary>
    public static bool DeletePOIGameObject(string locationId)
    {
        // Find all POI components in the scene
        var allPOIs = Object.FindObjectsOfType<POI>();

        foreach (var poi in allPOIs)
        {
            if (poi.LocationID == locationId)
            {
                Undo.DestroyObjectImmediate(poi.gameObject);
                Logger.LogInfo($"Deleted POI GameObject for location '{locationId}'", Logger.LogCategory.EditorLog);
                return true;
            }
        }

        Logger.LogWarning($"No POI GameObject found for location '{locationId}'", Logger.LogCategory.EditorLog);
        return false;
    }

    /// <summary>
    /// Remove all connections to and from a location (but keep the location itself)
    /// </summary>
    public static void RemoveAllConnections(string locationId, LocationRegistry registry)
    {
        if (string.IsNullOrEmpty(locationId) || registry == null)
            return;

        var location = registry.GetLocationById(locationId);

        // Remove outgoing connections
        if (location?.Connections != null)
        {
            location.Connections.Clear();
            EditorUtility.SetDirty(location);
        }

        // Remove incoming connections from all other locations
        foreach (var otherLocation in registry.AllLocations.Where(l => l != null && l.LocationID != locationId))
        {
            if (otherLocation.Connections != null)
            {
                int removed = otherLocation.Connections.RemoveAll(c => c?.DestinationLocationID == locationId);
                if (removed > 0)
                {
                    EditorUtility.SetDirty(otherLocation);
                }
            }
        }

        AssetDatabase.SaveAssets();
    }
}
#endif
