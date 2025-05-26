// Purpose: Renders dashed line paths between connected POIs automatically
// Filepath: Assets/Scripts/Gameplay/World/POIPathRenderer.cs
using UnityEngine;

public class POIPathRenderer : MonoBehaviour
{
    [Header("Path Settings")]
    [SerializeField] private Material dashLineMaterial; // Matériel pour les lignes pointillées
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private Color lineColor = Color.white;
    [SerializeField] private float dashLength = 0.5f;
    [SerializeField] private float gapLength = 0.3f;
    [SerializeField] private int dashResolution = 20; // Nombre de segments pour créer l'effet dash
    [SerializeField] private float poiBuffer = 0.5f; // Distance pour éviter les POIs

    [Header("Performance")]
    [SerializeField] private bool updateOnStart = true;
    [SerializeField] private bool showDebugInfo = false;

    // Références
    private MapManager mapManager;
    private LocationRegistry locationRegistry;
    private LineRenderer[] pathLines;

    void Start()
    {
        if (updateOnStart)
        {
            // Petit délai pour s'assurer que tout est initialisé
            Invoke(nameof(GenerateAllPaths), 0.5f);
        }
    }

    /// <summary>
    /// Génère automatiquement toutes les lignes entre POIs connectés
    /// </summary>
    public void GenerateAllPaths()
    {
        // Obtenir les références
        mapManager = MapManager.Instance;
        if (mapManager == null)
        {
            Logger.LogError("POIPathRenderer: MapManager not found!", Logger.LogCategory.MapLog);
            return;
        }

        locationRegistry = mapManager.LocationRegistry;
        if (locationRegistry == null)
        {
            Logger.LogError("POIPathRenderer: LocationRegistry not found!", Logger.LogCategory.MapLog);
            return;
        }

        // Nettoyer les anciennes lignes
        ClearExistingPaths();

        // Trouver tous les POIs
        POI[] allPOIs = FindObjectsOfType<POI>();
        if (showDebugInfo)
        {
            Logger.LogInfo($"POIPathRenderer: Found {allPOIs.Length} POIs", Logger.LogCategory.MapLog);
        }

        // Créer les lignes entre POIs connectés
        CreatePathsBetweenPOIs(allPOIs);

        Logger.LogInfo($"POIPathRenderer: Generated paths for {allPOIs.Length} POIs", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// Crée les chemins entre tous les POIs connectés
    /// </summary>
    private void CreatePathsBetweenPOIs(POI[] pois)
    {
        int pathCount = 0;

        for (int i = 0; i < pois.Length; i++)
        {
            for (int j = i + 1; j < pois.Length; j++)
            {
                POI poiA = pois[i];
                POI poiB = pois[j];

                // Vérifier si les locations sont connectées
                if (locationRegistry.CanTravelBetween(poiA.LocationID, poiB.LocationID))
                {
                    CreatePathBetweenPOIs(poiA, poiB, pathCount);
                    pathCount++;

                    if (showDebugInfo)
                    {
                        Logger.LogInfo($"POIPathRenderer: Created path between {poiA.LocationID} and {poiB.LocationID}", Logger.LogCategory.MapLog);
                    }
                }
            }
        }

        Logger.LogInfo($"POIPathRenderer: Created {pathCount} paths total", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// Crée une ligne pointillée entre deux POIs
    /// </summary>
    private void CreatePathBetweenPOIs(POI poiA, POI poiB, int pathIndex)
    {
        // Positions : utiliser les points de départ de voyage des POIs
        Vector3 startPos = poiA.GetTravelPathStartPosition();
        Vector3 endPos = poiB.GetTravelPathStartPosition();

        // Ajuster Z pour que les lignes soient au-dessus de la carte
        startPos.z = -1f;
        endPos.z = -1f;

        // Ajuster les positions pour éviter les POIs
        Vector3 direction = (endPos - startPos).normalized;
        Vector3 adjustedStart = startPos + direction * poiBuffer;
        Vector3 adjustedEnd = endPos - direction * poiBuffer;

        // Vérifier que la ligne ajustée a encore une longueur suffisante
        float adjustedDistance = Vector3.Distance(adjustedStart, adjustedEnd);
        if (adjustedDistance > dashLength) // Seulement si assez long pour au moins un tiret
        {
            CreateDashSegments(adjustedStart, adjustedEnd, pathIndex);
        }
    }

    /// <summary>
    /// Crée des segments séparés pour simuler une ligne pointillée
    /// </summary>
    private void CreateDashSegments(Vector3 startPos, Vector3 endPos, int pathIndex)
    {
        float totalDistance = Vector3.Distance(startPos, endPos);
        float segmentLength = dashLength + gapLength;
        int segmentCount = Mathf.CeilToInt(totalDistance / segmentLength);

        // Trouver tous les POIs pour vérifier les collisions
        POI[] allPOIs = FindObjectsOfType<POI>();

        for (int i = 0; i < segmentCount; i++)
        {
            float segmentStart = i * segmentLength;
            float segmentEnd = segmentStart + dashLength;

            // Arrêter si on dépasse la distance totale
            if (segmentStart >= totalDistance) break;

            // Calculer les positions du tiret
            float t1 = segmentStart / totalDistance;
            float t2 = Mathf.Min(segmentEnd, totalDistance) / totalDistance;

            Vector3 dashStart = Vector3.Lerp(startPos, endPos, t1);
            Vector3 dashEnd = Vector3.Lerp(startPos, endPos, t2);

            // Vérifier si ce segment entre en collision avec un POI
            if (!IsSegmentCollidingWithPOIs(dashStart, dashEnd, allPOIs))
            {
                // Créer un LineRenderer pour ce segment seulement s'il ne collisionne pas
                CreateSingleDashSegment(dashStart, dashEnd, i, pathIndex);
            }
        }
    }

    /// <summary>
    /// Vérifie si un segment entre en collision avec des POIs
    /// </summary>
    private bool IsSegmentCollidingWithPOIs(Vector3 segmentStart, Vector3 segmentEnd, POI[] allPOIs)
    {
        foreach (POI poi in allPOIs)
        {
            Vector3 poiPosition = poi.transform.position;
            poiPosition.z = segmentStart.z; // Même Z pour la comparaison

            // Calculer la distance du POI à la ligne
            float distanceToLine = DistancePointToLineSegment(poiPosition, segmentStart, segmentEnd);

            // Si la distance est inférieure au buffer, il y a collision
            if (distanceToLine < poiBuffer)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Calcule la distance d'un point à un segment de ligne
    /// </summary>
    private float DistancePointToLineSegment(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 lineDirection = lineEnd - lineStart;
        float lineLength = lineDirection.magnitude;

        if (lineLength == 0) return Vector3.Distance(point, lineStart);

        Vector3 lineNormalized = lineDirection / lineLength;
        Vector3 pointToLineStart = point - lineStart;

        // Projection du point sur la ligne
        float projectionLength = Vector3.Dot(pointToLineStart, lineNormalized);
        projectionLength = Mathf.Clamp(projectionLength, 0, lineLength);

        Vector3 closestPointOnLine = lineStart + lineNormalized * projectionLength;
        return Vector3.Distance(point, closestPointOnLine);
    }

    /// <summary>
    /// Crée un seul segment de tiret
    /// </summary>
    private void CreateSingleDashSegment(Vector3 start, Vector3 end, int segmentIndex, int pathIndex)
    {
        // Créer un GameObject pour ce segment
        GameObject segmentObject = new GameObject($"Dash_Segment_{pathIndex}_{segmentIndex}");
        segmentObject.transform.SetParent(transform);

        // Ajouter et configurer le LineRenderer
        LineRenderer lineRenderer = segmentObject.AddComponent<LineRenderer>();

        // Configuration de base
        lineRenderer.material = dashLineMaterial;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;

        // Définir la couleur via le gradient
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(lineColor, 0.0f), new GradientColorKey(lineColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(lineColor.a, 0.0f), new GradientAlphaKey(lineColor.a, 1.0f) }
        );
        lineRenderer.colorGradient = gradient;

        // Définir les positions
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        // Désactiver les ombres pour les performances
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        // Configuration du tri pour apparaître au-dessus de la carte
        lineRenderer.sortingLayerName = "Default";
        lineRenderer.sortingOrder = 1;
    }

    /// <summary>
    /// Nettoie toutes les lignes existantes
    /// </summary>
    private void ClearExistingPaths()
    {
        // Détruire tous les enfants (les lignes précédentes)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// Méthode publique pour regénérer les chemins (utile si les POIs changent)
    /// </summary>
    public void RegeneratePaths()
    {
        GenerateAllPaths();
    }

    /// <summary>
    /// Crée un matériel de base pour les lignes pointillées si aucun n'est assigné
    /// </summary>
    void OnValidate()
    {
        if (dashLineMaterial == null)
        {
            // En mode éditeur seulement, créer un matériel temporaire
#if UNITY_EDITOR
            dashLineMaterial = new Material(Shader.Find("Sprites/Default"));
#endif
        }
    }
}