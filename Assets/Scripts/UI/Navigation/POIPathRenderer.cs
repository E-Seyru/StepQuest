// Purpose: Renders dashed line paths between connected POIs automatically
// Filepath: Assets/Scripts/Gameplay/World/POIPathRenderer.cs
using UnityEngine;

public class POIPathRenderer : MonoBehaviour
{
    [Header("Path Settings")]
    [SerializeField] private Material dashLineMaterial; // Materiel pour les lignes pointillees
    [SerializeField] private float lineWidth = 0.05f;
    [SerializeField] private Color lineColor = Color.white;
    [SerializeField] private float dashLength = 0.5f;
    [SerializeField] private float gapLength = 0.3f;
    [SerializeField] private int dashResolution = 20; // Nombre de segments pour creer l'effet dash
    [SerializeField] private float poiBuffer = 0.5f; // Distance pour eviter les POIs

    [Header("Performance")]
    [SerializeField] private bool updateOnStart = true;
    [SerializeField] private bool showDebugInfo = false;

    [Header("References")]
    [Tooltip("Parent GameObject containing all POIs. If not assigned, will use FindObjectsOfType (slower).")]
    [SerializeField] private Transform poisParent;

    // References
    private MapManager mapManager;
    private LocationRegistry locationRegistry;
    private LineRenderer[] pathLines;
    private POI[] cachedPOIs;

    void Start()
    {
        if (updateOnStart)
        {
            // Petit delai pour s'assurer que tout est initialise
            Invoke(nameof(GenerateAllPaths), 0.5f);
        }
    }

    void OnDestroy()
    {
        // Cancel any pending Invoke calls to prevent null reference exceptions
        CancelInvoke(nameof(GenerateAllPaths));
    }

    /// <summary>
    /// Genere automatiquement toutes les lignes entre POIs connectes
    /// </summary>
    public void GenerateAllPaths()
    {
        // Obtenir les references
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

        // Trouver tous les POIs - use cached reference if available
        if (cachedPOIs == null || cachedPOIs.Length == 0)
        {
            if (poisParent != null)
            {
                cachedPOIs = poisParent.GetComponentsInChildren<POI>(true);
            }
            else
            {
                cachedPOIs = FindObjectsOfType<POI>();
            }
        }

        if (showDebugInfo)
        {
            Logger.LogInfo($"POIPathRenderer: Found {cachedPOIs.Length} POIs", Logger.LogCategory.MapLog);
        }

        // Creer les lignes entre POIs connectes
        CreatePathsBetweenPOIs(cachedPOIs);

        Logger.LogInfo($"POIPathRenderer: Generated paths for {cachedPOIs.Length} POIs", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// Cree les chemins entre tous les POIs connectes
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

                // Verifier si les locations sont connectees
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
    /// Cree une ligne pointillee entre deux POIs
    /// </summary>
    private void CreatePathBetweenPOIs(POI poiA, POI poiB, int pathIndex)
    {
        // Positions : utiliser les points de depart de voyage des POIs
        Vector3 startPos = poiA.GetTravelPathStartPosition();
        Vector3 endPos = poiB.GetTravelPathStartPosition();

        // Ajuster Z pour que les lignes soient au-dessus de la carte
        startPos.z = -1f;
        endPos.z = -1f;

        // Ajuster les positions pour eviter les POIs
        Vector3 direction = (endPos - startPos).normalized;
        Vector3 adjustedStart = startPos + direction * poiBuffer;
        Vector3 adjustedEnd = endPos - direction * poiBuffer;

        // Verifier que la ligne ajustee a encore une longueur suffisante
        float adjustedDistance = Vector3.Distance(adjustedStart, adjustedEnd);
        if (adjustedDistance > dashLength) // Seulement si assez long pour au moins un tiret
        {
            CreateDashSegments(adjustedStart, adjustedEnd, pathIndex);
        }
    }

    /// <summary>
    /// Cree des segments separes pour simuler une ligne pointillee
    /// </summary>
    private void CreateDashSegments(Vector3 startPos, Vector3 endPos, int pathIndex)
    {
        float totalDistance = Vector3.Distance(startPos, endPos);
        float segmentLength = dashLength + gapLength;
        int segmentCount = Mathf.CeilToInt(totalDistance / segmentLength);

        // Use cached POIs for collision checks
        POI[] allPOIs = cachedPOIs ?? System.Array.Empty<POI>();

        for (int i = 0; i < segmentCount; i++)
        {
            float segmentStart = i * segmentLength;
            float segmentEnd = segmentStart + dashLength;

            // Arreter si on depasse la distance totale
            if (segmentStart >= totalDistance) break;

            // Calculer les positions du tiret
            float t1 = segmentStart / totalDistance;
            float t2 = Mathf.Min(segmentEnd, totalDistance) / totalDistance;

            Vector3 dashStart = Vector3.Lerp(startPos, endPos, t1);
            Vector3 dashEnd = Vector3.Lerp(startPos, endPos, t2);

            // Verifier si ce segment entre en collision avec un POI
            if (!IsSegmentCollidingWithPOIs(dashStart, dashEnd, allPOIs))
            {
                // Creer un LineRenderer pour ce segment seulement s'il ne collisionne pas
                CreateSingleDashSegment(dashStart, dashEnd, i, pathIndex);
            }
        }
    }

    /// <summary>
    /// Verifie si un segment entre en collision avec des POIs
    /// </summary>
    private bool IsSegmentCollidingWithPOIs(Vector3 segmentStart, Vector3 segmentEnd, POI[] allPOIs)
    {
        foreach (POI poi in allPOIs)
        {
            Vector3 poiPosition = poi.transform.position;
            poiPosition.z = segmentStart.z; // Meme Z pour la comparaison

            // Calculer la distance du POI a la ligne
            float distanceToLine = DistancePointToLineSegment(poiPosition, segmentStart, segmentEnd);

            // Si la distance est inferieure au buffer, il y a collision
            if (distanceToLine < poiBuffer)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Calcule la distance d'un point a un segment de ligne
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
    /// Cree un seul segment de tiret
    /// </summary>
    private void CreateSingleDashSegment(Vector3 start, Vector3 end, int segmentIndex, int pathIndex)
    {
        // Creer un GameObject pour ce segment
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

        // Definir la couleur via le gradient
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(lineColor, 0.0f), new GradientColorKey(lineColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(lineColor.a, 0.0f), new GradientAlphaKey(lineColor.a, 1.0f) }
        );
        lineRenderer.colorGradient = gradient;

        // Definir les positions
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        // Desactiver les ombres pour les performances
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        // Configuration du tri pour appara�tre au-dessus de la carte
        lineRenderer.sortingLayerName = "Default";
        lineRenderer.sortingOrder = 1;
    }

    /// <summary>
    /// Nettoie toutes les lignes existantes
    /// </summary>
    private void ClearExistingPaths()
    {
        // Detruire tous les enfants (les lignes precedentes)
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// Methode publique pour regenerer les chemins (utile si les POIs changent)
    /// </summary>
    public void RegeneratePaths()
    {
        GenerateAllPaths();
    }

    /// <summary>
    /// Cree un materiel de base pour les lignes pointillees si aucun n'est assigne
    /// </summary>
    void OnValidate()
    {
        if (dashLineMaterial == null)
        {
            // En mode editeur seulement, creer un materiel temporaire
#if UNITY_EDITOR
            dashLineMaterial = new Material(Shader.Find("Sprites/Default"));
#endif
        }
    }
}