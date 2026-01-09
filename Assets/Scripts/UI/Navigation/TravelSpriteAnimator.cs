// Purpose: Animates a sprite that moves along the travel path during journey - FIXED FOR PATHFINDING
// Filepath: Assets/Scripts/UI/Components/TravelSpriteAnimator.cs
using MapEvents;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TravelSpriteAnimator : MonoBehaviour
{
    [Header("Sprite Settings")]
    [SerializeField] private GameObject travelSprite;
    [SerializeField] private float spriteScale = 1f;
    [SerializeField] private Vector3 spriteOffset = Vector3.zero;
    [SerializeField] private Transform spriteVisual;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private LeanTweenType easeType = LeanTweenType.easeInOutQuad;
    [SerializeField] private bool bounceAnimation = true;
    [SerializeField] private float bounceHeight = 0.2f;
    [SerializeField] private float bounceSpeed = 2f;
    [SerializeField] private float moveSpeed = 0.75f;
    private int moveTweenId = -1;

    [Header("Debug")]
    [SerializeField] private bool showDebugPath = false;
    [SerializeField] private Color debugPathColor = Color.yellow;
    [SerializeField] private bool enablePathfindingDebug = true;

    [Header("References")]
    [Tooltip("Reference to the WorldMap GameObject. If not assigned, will try to find it by name.")]
    [SerializeField] private GameObject worldMapObject;

    // NOUVEAU : Cache des positions POI pour eviter les recherches repetees
    private Dictionary<string, Vector3> poiPositionsCache = new Dictionary<string, Vector3>();
    private bool isCacheInitialized = false;

    // References
    private MapManager mapManager;
    private DataManager dataManager;
    private LocationRegistry locationRegistry;

    // etat du voyage - MODIFIe POUR PATHFINDING
    private Vector3 startPosition;
    private Vector3 endPosition;
    private bool isAnimating = false;
    private int currentBounceId = -1;

    // NOUVEAU : etat pour gerer les voyages multi-segments et CurrentLocation null
    private bool isMultiSegmentTravel = false;
    private string currentSegmentDestination = "";
    private string finalDestination = "";
    private string lastKnownLocationId = null; // ⭐ NOUVEAU : Pour gerer CurrentLocation null
    private bool isTravelReversing = false; // Flag to prevent repositioning when reversing travel
    private Vector3 savedSpritePosition; // Saved position when reversing travel

    public static TravelSpriteAnimator Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Logger.LogWarning("TravelSpriteAnimator: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.MapLog);
            Destroy(gameObject);
            return;
        }

        if (travelSprite != null)
        {
            // Hide sprite initially - it will be shown after position is calculated
            travelSprite.SetActive(false);
        }
    }

    void Start()
    {
        // Obtenir les references
        mapManager = MapManager.Instance;
        dataManager = DataManager.Instance;

        if (mapManager != null)
        {
            locationRegistry = mapManager.LocationRegistry;
        }

        // =====================================
        // EVENTBUS - S'abonner aux evenements de voyage
        // =====================================
        EventBus.Subscribe<TravelStartedEvent>(OnTravelStarted);
        EventBus.Subscribe<TravelCompletedEvent>(OnTravelCompleted);
        EventBus.Subscribe<TravelProgressEvent>(OnTravelProgress);
        EventBus.Subscribe<LocationChangedEvent>(OnLocationChanged);
        EventBus.Subscribe<TravelCancelledEvent>(OnTravelCancelled);

        // Validation
        if (travelSprite == null)
        {
            Logger.LogError("TravelSpriteAnimator: travelSprite not assigned!", Logger.LogCategory.MapLog);
        }

        // NOUVEAU : Initialiser le cache des positions POI
        InitializePOICache();

        // Initialize lastKnownLocationId based on current state
        // Priority: TravelOriginLocationId (if traveling) > CurrentLocationId > CurrentLocation
        if (dataManager?.PlayerData != null)
        {
            if (dataManager.PlayerData.IsCurrentlyTraveling() &&
                !string.IsNullOrEmpty(dataManager.PlayerData.TravelOriginLocationId))
            {
                // Mid-travel: use the origin of current travel segment
                lastKnownLocationId = dataManager.PlayerData.TravelOriginLocationId;
                if (enablePathfindingDebug)
                {
                    Logger.LogInfo($"TravelSpriteAnimator: Initialized lastKnownLocationId from TravelOriginLocationId: {lastKnownLocationId}", Logger.LogCategory.MapLog);
                }
            }
            else if (!string.IsNullOrEmpty(dataManager.PlayerData.CurrentLocationId))
            {
                lastKnownLocationId = dataManager.PlayerData.CurrentLocationId;
                if (enablePathfindingDebug)
                {
                    Logger.LogInfo($"TravelSpriteAnimator: Initialized lastKnownLocationId from CurrentLocationId: {lastKnownLocationId}", Logger.LogCategory.MapLog);
                }
            }
        }
        else if (mapManager?.CurrentLocation != null)
        {
            lastKnownLocationId = mapManager.CurrentLocation.LocationID;
            if (enablePathfindingDebug)
            {
                Logger.LogInfo($"TravelSpriteAnimator: Initialized lastKnownLocationId from CurrentLocation: {lastKnownLocationId}", Logger.LogCategory.MapLog);
            }
        }

        // Positionner le personnage a sa location actuelle au demarrage
        StartCoroutine(PositionPlayerAfterDelay());
    }

    /// <summary>
    /// NOUVEAU : Initialise le cache des positions POI une seule fois au demarrage
    /// </summary>
    private void InitializePOICache()
    {
        Logger.LogInfo("TravelSpriteAnimator: Initializing POI positions cache...", Logger.LogCategory.MapLog);

        // Use serialized reference, fallback to Find if not assigned
        if (worldMapObject == null)
        {
            worldMapObject = GameObject.Find("WorldMap");
        }
        if (worldMapObject == null)
        {
            Logger.LogError("TravelSpriteAnimator: WorldMap GameObject not found! Assign it in the Inspector or ensure it exists in scene.", Logger.LogCategory.MapLog);
            return;
        }

        // Sauvegarder l'etat actuel de la carte
        bool wasMapActive = worldMapObject.activeInHierarchy;

        // Activer temporairement la WorldMap si elle etait desactivee
        if (!wasMapActive)
        {
            worldMapObject.SetActive(true);
        }

        // OPTIMISATION : Chercher les POI seulement dans WorldMap au lieu de toute la scene
        POI[] allPOIs = worldMapObject.GetComponentsInChildren<POI>(true); // 'true' pour inclure les POI desactives

        // Mettre en cache toutes les positions
        foreach (POI poi in allPOIs)
        {
            if (!string.IsNullOrEmpty(poi.LocationID))
            {
                Vector3 poiPosition = poi.GetTravelPathStartPosition();
                poiPositionsCache[poi.LocationID] = poiPosition;
            }
            else
            {
                Logger.LogWarning($"TravelSpriteAnimator: POI found without LocationID on GameObject '{poi.gameObject.name}'", Logger.LogCategory.MapLog);
            }
        }

        // Remettre la WorldMap dans son etat d'origine
        if (!wasMapActive)
        {
            worldMapObject.SetActive(false);
        }

        isCacheInitialized = true;
        Logger.LogInfo($"TravelSpriteAnimator: POI cache initialized with {poiPositionsCache.Count} positions", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// MODIFIe : Utilise maintenant le cache au lieu de chercher dans la scene
    /// </summary>
    private Vector3 FindPOITravelStartPosition(string locationId)
    {
        // Verifier que le cache est initialise
        if (!isCacheInitialized)
        {
            Logger.LogWarning("TravelSpriteAnimator: POI cache not initialized! Trying to initialize now...", Logger.LogCategory.MapLog);
            InitializePOICache();
        }

        // Chercher dans le cache
        if (poiPositionsCache.TryGetValue(locationId, out Vector3 cachedPosition))
        {
            return cachedPosition;
        }

        // Si pas trouve dans le cache, logger une erreur detaillee
        string availableLocations = string.Join(", ", poiPositionsCache.Keys);
        Logger.LogError($"TravelSpriteAnimator: POI position not found in cache for location '{locationId}'. " +
                       $"Available cached locations: [{availableLocations}]", Logger.LogCategory.MapLog);

        return Vector3.zero;
    }

    /// <summary>
    /// NOUVEAU : Methode utilitaire pour rafraîchir le cache si necessaire
    /// </summary>
    public void RefreshPOICache()
    {
        poiPositionsCache.Clear();
        isCacheInitialized = false;
        InitializePOICache();
        Logger.LogInfo("TravelSpriteAnimator: POI cache refreshed", Logger.LogCategory.MapLog);
    }

    private IEnumerator PositionPlayerAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

        // Wait for POI cache to be initialized
        if (!isCacheInitialized)
        {
            Logger.LogWarning("TravelSpriteAnimator: POI cache not ready yet, waiting...", Logger.LogCategory.MapLog);
            yield return new WaitForSeconds(0.5f);
        }

        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            // Resume travel animation: use TravelOriginLocationId as the start point
            string currentDestination = dataManager.PlayerData.TravelDestinationId;

            // Use TravelOriginLocationId if available, fallback to lastKnownLocationId
            string startLocationId = dataManager.PlayerData.TravelOriginLocationId;
            if (string.IsNullOrEmpty(startLocationId))
            {
                startLocationId = lastKnownLocationId;
            }

            if (string.IsNullOrEmpty(startLocationId))
            {
                Logger.LogError("TravelSpriteAnimator: Cannot restore travel - no start location available", Logger.LogCategory.MapLog);
                yield break;
            }

            // Validate that both locations exist in the cache before proceeding
            if (!poiPositionsCache.ContainsKey(startLocationId))
            {
                Logger.LogError($"TravelSpriteAnimator: Start location '{startLocationId}' not found in POI cache", Logger.LogCategory.MapLog);
                yield break;
            }
            if (!poiPositionsCache.ContainsKey(currentDestination))
            {
                Logger.LogError($"TravelSpriteAnimator: Destination '{currentDestination}' not found in POI cache", Logger.LogCategory.MapLog);
                yield break;
            }

            // Update lastKnownLocationId for consistency
            lastKnownLocationId = startLocationId;

            DetermineIfMultiSegmentTravel(currentDestination, startLocationId);
            SetupTravelPath(currentDestination, startLocationId);

            // Verify path was set up correctly (not at origin)
            if (startPosition == Vector3.zero || endPosition == Vector3.zero)
            {
                Logger.LogError($"TravelSpriteAnimator: Invalid path positions - start: {startPosition}, end: {endPosition}", Logger.LogCategory.MapLog);
                yield break;
            }

            long currentTotalSteps = dataManager.PlayerData.TotalSteps;
            long progressSteps = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);
            int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;

            float progress = requiredSteps > 0 ?
                (float)progressSteps / requiredSteps : 0f;
            progress = Mathf.Clamp01(progress);

            // Ensure sprite is visible when resuming travel
            if (travelSprite != null)
            {
                // IMPORTANT: Set position BEFORE activating to prevent showing at wrong location
                Vector3 targetPos = Vector3.Lerp(startPosition, endPosition, progress);
                travelSprite.transform.position = targetPos;
                travelSprite.SetActive(true);

                Logger.LogInfo($"TravelSpriteAnimator: Sprite activated at progress {progress:F2}, position set to {targetPos}", Logger.LogCategory.MapLog);
            }

            if (bounceAnimation)
            {
                StartBounceAnimation();
            }

            if (enablePathfindingDebug)
            {
                Logger.LogInfo($"TravelSpriteAnimator: Resumed travel animation from {startLocationId} to {currentDestination} - {progressSteps}/{requiredSteps} steps", Logger.LogCategory.MapLog);
            }
        }
        else
        {
            PositionPlayerAtCurrentLocation();
        }
    }

    void OnDestroy()
    {
        // =====================================
        // EVENTBUS - Se desabonner des evenements
        // =====================================
        EventBus.Unsubscribe<TravelStartedEvent>(OnTravelStarted);
        EventBus.Unsubscribe<TravelCompletedEvent>(OnTravelCompleted);
        EventBus.Unsubscribe<TravelProgressEvent>(OnTravelProgress);
        EventBus.Unsubscribe<LocationChangedEvent>(OnLocationChanged);
        EventBus.Unsubscribe<TravelCancelledEvent>(OnTravelCancelled);

        StopAllAnimations();
    }

    // === GESTIONNAIRES D'eVeNEMENTS - MODIFIeS POUR PATHFINDING ===

    /// <summary>
    /// MODIFIe : Gere maintenant les changements de location pendant les voyages multi-segments
    /// </summary>
    private void OnLocationChanged(LocationChangedEvent eventData)
    {
        if (enablePathfindingDebug)
        {
            Logger.LogInfo($"TravelSpriteAnimator: Location changed from {eventData.PreviousLocation?.DisplayName} to {eventData.NewLocation?.DisplayName}", Logger.LogCategory.MapLog);
        }

        // ⭐ NOUVEAU : Sauvegarder la nouvelle location
        if (eventData.NewLocation != null)
        {
            lastKnownLocationId = eventData.NewLocation.LocationID;
        }

        if (dataManager.PlayerData.IsCurrentlyTraveling())
        {
            // NOUVEAU : Pendant un voyage multi-segment, mettre a jour le chemin pour le segment suivant
            string nextDestination = dataManager.PlayerData.TravelDestinationId;

            if (nextDestination != currentSegmentDestination)
            {
                if (enablePathfindingDebug)
                {
                    Logger.LogInfo($"TravelSpriteAnimator: Segment changed - now traveling to {nextDestination}", Logger.LogCategory.MapLog);
                }

                // Configurer le nouveau segment
                SetupTravelPath(nextDestination, lastKnownLocationId);
                PositionPlayerAtStart();

                if (bounceAnimation)
                {
                    StartBounceAnimation();
                }
            }
        }
        else
        {
            // Voyage termine - positionner a la location actuelle
            PositionPlayerAtCurrentLocation();
            ResetMultiSegmentState();
        }
    }

    /// <summary>
    /// Handles travel cancellation - saves sprite position and sets flag for reverse travel
    /// </summary>
    private void OnTravelCancelled(TravelCancelledEvent eventData)
    {
        if (enablePathfindingDebug)
        {
            Logger.LogInfo($"TravelSpriteAnimator: Travel cancelled - reversing from {eventData.OriginalDestinationId} to {eventData.NewDestinationId} ({eventData.StepsToReturn} steps)", Logger.LogCategory.MapLog);
        }

        // Save the sprite's current world position BEFORE any changes
        if (travelSprite != null)
        {
            savedSpritePosition = travelSprite.transform.position;
            if (enablePathfindingDebug)
            {
                Logger.LogInfo($"TravelSpriteAnimator: Saved sprite position at {savedSpritePosition}", Logger.LogCategory.MapLog);
            }
        }

        // Set flag to prevent repositioning when TravelStartedEvent fires
        isTravelReversing = true;

        // Reset multi-segment state since we're now doing a simple return
        ResetMultiSegmentState();
    }

    /// <summary>
    /// MODIFIe : Gere le debut d'un voyage avec gestion de CurrentLocation null
    /// </summary>
    private void OnTravelStarted(TravelStartedEvent eventData)
    {
        if (enablePathfindingDebug)
        {
            Logger.LogInfo($"TravelSpriteAnimator: Travel started to {eventData.DestinationLocationId}", Logger.LogCategory.MapLog);
        }

        StopAllAnimations();

        // Check if this is a reverse travel (from cancellation)
        bool isReverseTravel = isTravelReversing;
        isTravelReversing = false; // Reset the flag

        if (isReverseTravel)
        {
            // For reverse travel: sprite stays where it is, path goes from current position to return destination
            // endPosition = the destination we're returning to (eventData.DestinationLocationId)
            endPosition = FindPOITravelStartPosition(eventData.DestinationLocationId) + spriteOffset;

            // startPosition = where the sprite currently is (saved position)
            startPosition = savedSpritePosition;

            // Update tracking state to prevent path recalculation in OnTravelProgress
            currentSegmentDestination = eventData.DestinationLocationId;

            // Update lastKnownLocationId to the conceptual current position (from PlayerData.CurrentLocationId)
            // Since CurrentLocation is null during reverse travel, we use PlayerData directly
            if (dataManager?.PlayerData != null && !string.IsNullOrEmpty(dataManager.PlayerData.CurrentLocationId))
            {
                lastKnownLocationId = dataManager.PlayerData.CurrentLocationId;
            }

            if (enablePathfindingDebug)
            {
                Logger.LogInfo($"TravelSpriteAnimator: Reverse travel setup - start at saved position {startPosition}, end at {eventData.DestinationLocationId}, lastKnownLocationId={lastKnownLocationId}", Logger.LogCategory.MapLog);
            }

            // Don't reposition - sprite is already at the correct position
            if (bounceAnimation)
            {
                StartBounceAnimation();
            }
            return;
        }

        // Normal travel (not a reverse)
        // ⭐ NOUVEAU : Utiliser la location de depart de l'evenement
        string fromLocationId = eventData.CurrentLocation?.LocationID;

        // Si pas de location dans l'evenement, utiliser la derniere connue
        if (string.IsNullOrEmpty(fromLocationId))
        {
            fromLocationId = lastKnownLocationId;
            if (enablePathfindingDebug)
            {
                Logger.LogInfo($"TravelSpriteAnimator: Using last known location {fromLocationId} as start", Logger.LogCategory.MapLog);
            }
        }
        else
        {
            // Sauvegarder pour usage futur
            lastKnownLocationId = fromLocationId;
        }

        // NOUVEAU : Determiner si c'est un voyage multi-segment
        DetermineIfMultiSegmentTravel(eventData.DestinationLocationId, fromLocationId);

        SetupTravelPath(eventData.DestinationLocationId, fromLocationId);
        PositionPlayerAtStart();

        if (bounceAnimation)
        {
            StartBounceAnimation();
        }
    }

    /// <summary>
    /// NOUVEAU : Determine si le voyage actuel fait partie d'un pathfinding multi-segment
    /// </summary>
    private void DetermineIfMultiSegmentTravel(string destinationId, string fromLocationId)
    {
        currentSegmentDestination = destinationId;

        // Verifier si c'est un voyage pathfinding en comparant avec une connexion directe
        if (!string.IsNullOrEmpty(fromLocationId) && locationRegistry != null)
        {
            bool hasDirectConnection = locationRegistry.CanTravelBetween(fromLocationId, destinationId);

            if (!hasDirectConnection && mapManager.PathfindingService != null)
            {
                var pathResult = mapManager.PathfindingService.FindPath(fromLocationId, destinationId);
                if (pathResult.IsReachable && pathResult.Segments.Count > 1)
                {
                    isMultiSegmentTravel = true;
                    finalDestination = pathResult.Path[pathResult.Path.Count - 1]; // Derniere destination

                    if (enablePathfindingDebug)
                    {
                        Logger.LogInfo($"TravelSpriteAnimator: Multi-segment travel detected - {pathResult.Segments.Count} segments to reach {finalDestination}", Logger.LogCategory.MapLog);
                    }
                    return;
                }
            }
        }

        // Voyage direct
        isMultiSegmentTravel = false;
        finalDestination = destinationId;
    }

    /// <summary>
    /// MODIFIe : Gere les mises a jour de progres avec reconnaissance des segments
    /// </summary>
    private void OnTravelProgress(TravelProgressEvent eventData)
    {
        if (travelSprite != null && travelSprite.activeSelf)
        {
            // Verifier si on a change de segment
            if (eventData.DestinationLocationId != currentSegmentDestination)
            {
                if (enablePathfindingDebug)
                {
                    Logger.LogInfo($"TravelSpriteAnimator: Progress event for different segment ({eventData.DestinationLocationId} vs {currentSegmentDestination}) - updating path", Logger.LogCategory.MapLog);
                }

                currentSegmentDestination = eventData.DestinationLocationId;
                SetupTravelPath(eventData.DestinationLocationId, lastKnownLocationId);
            }

            float progress = eventData.RequiredSteps > 0 ?
                (float)eventData.CurrentSteps / eventData.RequiredSteps : 0f;
            progress = Mathf.Clamp01(progress);

            UpdateSpritePosition(progress);
        }
    }

    /// <summary>
    /// MODIFIe : Gere la fin d'un voyage ou d'un segment
    /// </summary>
    private void OnTravelCompleted(TravelCompletedEvent eventData)
    {
        if (enablePathfindingDebug)
        {
            Logger.LogInfo($"TravelSpriteAnimator: Travel completed to {eventData.NewLocation?.DisplayName}", Logger.LogCategory.MapLog);
        }

        // ⭐ NOUVEAU : Sauvegarder la nouvelle location
        if (eventData.NewLocation != null)
        {
            lastKnownLocationId = eventData.NewLocation.LocationID;
        }

        AnimateToDestination();
        StopBounceAnimation();
        ResetMultiSegmentState();
    }

    /// <summary>
    /// NOUVEAU : Remet a zero l'etat multi-segment
    /// </summary>
    private void ResetMultiSegmentState()
    {
        isMultiSegmentTravel = false;
        currentSegmentDestination = "";
        finalDestination = "";
    }

    /// <summary>
    /// MODIFIe : Configure le chemin pour le segment actuel avec gestion de CurrentLocation null
    /// </summary>
    private void SetupTravelPath(string destinationId, string fromLocationId = null)
    {
        if (locationRegistry == null)
        {
            Logger.LogError("TravelSpriteAnimator: Cannot setup travel path - locationRegistry missing", Logger.LogCategory.MapLog);
            return;
        }

        // ⭐ NOUVEAU : Utiliser fromLocationId si fourni, sinon fallback sur CurrentLocation
        string startLocationId = fromLocationId;
        if (string.IsNullOrEmpty(startLocationId))
        {
            if (mapManager?.CurrentLocation == null)
            {
                Logger.LogError("TravelSpriteAnimator: Cannot setup travel path - no start location available", Logger.LogCategory.MapLog);
                return;
            }
            startLocationId = mapManager.CurrentLocation.LocationID;
        }

        // Sauvegarder pour usage futur
        lastKnownLocationId = startLocationId;

        // MODIFIe : Utilise le cache et configure pour le segment actuel
        startPosition = FindPOITravelStartPosition(startLocationId);
        endPosition = FindPOITravelStartPosition(destinationId);

        startPosition += spriteOffset;
        endPosition += spriteOffset;

        if (enablePathfindingDebug)
        {
            string segmentInfo = isMultiSegmentTravel ?
                $" (segment vers {destinationId}, final: {finalDestination})" : " (voyage direct)";
            Logger.LogInfo($"TravelSpriteAnimator: Path setup from {startLocationId} to {destinationId}{segmentInfo}", Logger.LogCategory.MapLog);
        }
    }

    private void PositionPlayerAtStart()
    {
        if (travelSprite == null) return;

        // Set position BEFORE activating to avoid flicker at wrong position
        travelSprite.transform.position = startPosition;
        travelSprite.transform.localScale = Vector3.one * spriteScale;
        travelSprite.SetActive(true);

        if (bounceAnimation)
        {
            StartBounceAnimation();
        }
    }

    public void PositionPlayerAtCurrentLocation()
    {
        // ⭐ MODIFIe : Utiliser lastKnownLocationId au lieu de CurrentLocation
        string locationId = null;

        if (mapManager?.CurrentLocation != null)
        {
            locationId = mapManager.CurrentLocation.LocationID;
            lastKnownLocationId = locationId;
        }
        else if (!string.IsNullOrEmpty(lastKnownLocationId))
        {
            locationId = lastKnownLocationId;
        }

        if (string.IsNullOrEmpty(locationId) || travelSprite == null)
        {
            Logger.LogWarning("TravelSpriteAnimator: Cannot position player - no location available or travelSprite is null", Logger.LogCategory.MapLog);
            return;
        }

        // MODIFIe : Utilise le cache
        Vector3 currentPos = FindPOITravelStartPosition(locationId);

        if (currentPos == Vector3.zero)
        {
            Logger.LogError($"TravelSpriteAnimator: Could not find cached position for location '{locationId}'", Logger.LogCategory.MapLog);
            return;
        }

        currentPos += spriteOffset;

        travelSprite.transform.position = currentPos;
        travelSprite.SetActive(true);

        if (enablePathfindingDebug)
        {
            Logger.LogInfo($"TravelSpriteAnimator: Player positioned at location {locationId}", Logger.LogCategory.MapLog);
        }
    }

    private void UpdateSpritePosition(float progress)
    {
        if (travelSprite == null) return;

        Vector3 target = Vector3.Lerp(startPosition, endPosition, progress);

        if (moveTweenId >= 0) LeanTween.cancel(moveTweenId);

        float distance = Vector3.Distance(travelSprite.transform.position, target);
        float duration = Mathf.Max(distance / moveSpeed, 0.05f);

        moveTweenId = LeanTween.move(travelSprite, target, duration)
                               .setEase(LeanTweenType.linear)
                               .id;
    }

    private void AnimateToDestination()
    {
        if (travelSprite == null) return;

        LeanTween.move(travelSprite, endPosition, animationDuration)
            .setEase(easeType)
            .setOnComplete(() =>
            {
                if (enablePathfindingDebug)
                {
                    Logger.LogInfo("TravelSpriteAnimator: Player positioned at destination", Logger.LogCategory.MapLog);
                }
            });
    }

    private void StartBounceAnimation()
    {
        if (spriteVisual == null) return;

        StopBounceAnimation();

        currentBounceId = LeanTween
            .moveLocalY(spriteVisual.gameObject,
                        bounceHeight,
                        0.5f / bounceSpeed)
            .setEase(LeanTweenType.easeInOutSine)
            .setLoopPingPong()
            .id;
    }

    private void StopBounceAnimation()
    {
        if (currentBounceId >= 0)
        {
            LeanTween.cancel(currentBounceId);
            currentBounceId = -1;
        }

        if (spriteVisual != null)
        {
            spriteVisual.localPosition = Vector3.zero;
        }
    }

    private void StopAllAnimations()
    {
        if (travelSprite != null)
        {
            LeanTween.cancel(travelSprite);
        }

        if (moveTweenId >= 0)
        {
            LeanTween.cancel(moveTweenId);
            moveTweenId = -1;
        }

        StopBounceAnimation();
        isAnimating = false;
    }

    void OnDrawGizmos()
    {
        if (!showDebugPath || !Application.isPlaying) return;

        Gizmos.color = debugPathColor;

        if (startPosition != Vector3.zero && endPosition != Vector3.zero)
        {
            Gizmos.DrawLine(startPosition, endPosition);
            Gizmos.DrawWireSphere(startPosition, 0.1f);
            Gizmos.DrawWireSphere(endPosition, 0.1f);
        }

        if (travelSprite != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(travelSprite.transform.position, 0.05f);
        }
    }
}