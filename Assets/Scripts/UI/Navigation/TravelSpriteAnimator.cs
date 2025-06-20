// Purpose: Animates a sprite that moves along the travel path during journey
// Filepath: Assets/Scripts/UI/Components/TravelSpriteAnimator.cs
using MapEvents; // NOUVEAU: Import pour EventBus
using System.Collections.Generic; // Pour Dictionary
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

    // NOUVEAU : Cache des positions POI pour eviter les recherches repetees
    private Dictionary<string, Vector3> poiPositionsCache = new Dictionary<string, Vector3>();
    private bool isCacheInitialized = false;

    // References
    private MapManager mapManager;
    private DataManager dataManager;
    private LocationRegistry locationRegistry;

    // Etat du voyage
    private Vector3 startPosition;
    private Vector3 endPosition;
    private bool isAnimating = false;
    private int currentBounceId = -1;

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
            travelSprite.SetActive(true);
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
        // EVENTBUS - S'abonner aux événements de voyage
        // =====================================
        EventBus.Subscribe<TravelStartedEvent>(OnTravelStarted);
        EventBus.Subscribe<TravelCompletedEvent>(OnTravelCompleted);
        EventBus.Subscribe<TravelProgressEvent>(OnTravelProgress);
        EventBus.Subscribe<LocationChangedEvent>(OnLocationChanged);

        // Validation
        if (travelSprite == null)
        {
            Logger.LogError("TravelSpriteAnimator: travelSprite not assigned!", Logger.LogCategory.MapLog);
        }

        // NOUVEAU : Initialiser le cache des positions POI
        InitializePOICache();

        // Positionner le personnage à sa location actuelle au demarrage
        StartCoroutine(PositionPlayerAfterDelay());
    }

    /// <summary>
    /// NOUVEAU : Initialise le cache des positions POI une seule fois au demarrage
    /// </summary>
    private void InitializePOICache()
    {
        Logger.LogInfo("TravelSpriteAnimator: Initializing POI positions cache...", Logger.LogCategory.MapLog);

        // Chercher le GameObject "WorldMap" dans la scène
        GameObject worldMapObject = GameObject.Find("WorldMap");
        if (worldMapObject == null)
        {
            Logger.LogError("TravelSpriteAnimator: WorldMap GameObject not found! Cannot cache POI positions.", Logger.LogCategory.MapLog);
            return;
        }

        // Sauvegarder l'etat actuel de la carte
        bool wasMapActive = worldMapObject.activeInHierarchy;

        // Activer temporairement la WorldMap si elle etait desactivee
        if (!wasMapActive)
        {
            worldMapObject.SetActive(true);
        }

        // OPTIMISATION : Chercher les POI seulement dans WorldMap au lieu de toute la scène
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
    }

    /// <summary>
    /// MODIFIE : Utilise maintenant le cache au lieu de chercher dans la scène
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
    /// (utile si vous ajoutez des POI dynamiquement)
    /// </summary>
    public void RefreshPOICache()
    {
        poiPositionsCache.Clear();
        isCacheInitialized = false;
        InitializePOICache();
    }

    // === LE RESTE DU CODE RESTE IDENTIQUE ===

    private System.Collections.IEnumerator PositionPlayerAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            string destinationId = dataManager.PlayerData.TravelDestinationId;
            SetupTravelPath(destinationId);

            long currentTotalSteps = dataManager.PlayerData.TotalSteps;
            long progressSteps = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);
            int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;

            float progress = requiredSteps > 0 ? (float)progressSteps / requiredSteps : 0f;
            progress = Mathf.Clamp01(progress);

            if (travelSprite != null)
            {
                Vector3 currentPos = Vector3.Lerp(startPosition, endPosition, progress);
                travelSprite.transform.position = currentPos;
            }

            if (bounceAnimation)
            {
                StartBounceAnimation();
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
        // EVENTBUS - Se désabonner des événements
        // =====================================
        EventBus.Unsubscribe<TravelStartedEvent>(OnTravelStarted);
        EventBus.Unsubscribe<TravelCompletedEvent>(OnTravelCompleted);
        EventBus.Unsubscribe<TravelProgressEvent>(OnTravelProgress);
        EventBus.Unsubscribe<LocationChangedEvent>(OnLocationChanged);

        StopAllAnimations();
    }

    // === GESTIONNAIRES D'ÉVÉNEMENTS - ADAPTÉS POUR EVENTBUS ===

    private void OnLocationChanged(LocationChangedEvent eventData)
    {
        if (!dataManager.PlayerData.IsCurrentlyTraveling())
        {
            PositionPlayerAtCurrentLocation();
        }
    }

    private void OnTravelStarted(TravelStartedEvent eventData)
    {
        StopAllAnimations();
        SetupTravelPath(eventData.DestinationLocationId);
        PositionPlayerAtStart();
        if (bounceAnimation) StartBounceAnimation();
    }

    private void OnTravelProgress(TravelProgressEvent eventData)
    {
        if (travelSprite != null && travelSprite.activeSelf)
        {
            float progress = eventData.RequiredSteps > 0 ? (float)eventData.CurrentSteps / eventData.RequiredSteps : 0f;
            progress = Mathf.Clamp01(progress);

            UpdateSpritePosition(progress);
        }
    }

    private void OnTravelCompleted(TravelCompletedEvent eventData)
    {
        AnimateToDestination();
        StopBounceAnimation();
    }

    private void SetupTravelPath(string destinationId)
    {
        if (mapManager?.CurrentLocation == null || locationRegistry == null)
        {
            Logger.LogError("TravelSpriteAnimator: Cannot setup travel path - missing references", Logger.LogCategory.MapLog);
            return;
        }

        // MODIFIE : Utilise le cache au lieu de chercher dans la scène
        startPosition = FindPOITravelStartPosition(mapManager.CurrentLocation.LocationID);
        endPosition = FindPOITravelStartPosition(destinationId);

        startPosition += spriteOffset;
        endPosition += spriteOffset;

        Logger.LogInfo($"TravelSpriteAnimator: Path setup from {startPosition} to {endPosition}", Logger.LogCategory.MapLog);
    }

    private void PositionPlayerAtStart()
    {
        if (travelSprite == null) return;

        travelSprite.SetActive(true);
        travelSprite.transform.position = startPosition;
        travelSprite.transform.localScale = Vector3.one * spriteScale;

        if (bounceAnimation)
        {
            StartBounceAnimation();
        }
    }

    public void PositionPlayerAtCurrentLocation()
    {
        if (mapManager?.CurrentLocation == null || travelSprite == null)
        {
            Logger.LogWarning("TravelSpriteAnimator: Cannot position player - mapManager.CurrentLocation or travelSprite is null", Logger.LogCategory.MapLog);
            return;
        }

        // MODIFIE : Utilise le cache
        Vector3 currentPos = FindPOITravelStartPosition(mapManager.CurrentLocation.LocationID);

        if (currentPos == Vector3.zero)
        {
            Logger.LogError($"TravelSpriteAnimator: Could not find cached position for location '{mapManager.CurrentLocation.LocationID}'", Logger.LogCategory.MapLog);
            return;
        }

        currentPos += spriteOffset;

        travelSprite.transform.position = currentPos;
        travelSprite.SetActive(true);
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
                Logger.LogInfo("TravelSpriteAnimator: Player positioned at destination", Logger.LogCategory.MapLog);
            });
    }

    private void StartBounceAnimation()
    {
        if (spriteVisual == null) return;

        LeanTween.cancel(spriteVisual.gameObject);
        spriteVisual.localPosition = Vector3.zero;

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
            LeanTween.cancel(currentBounceId);

        currentBounceId = -1;
        if (spriteVisual != null)
            spriteVisual.localPosition = Vector3.zero;
    }

    private void StopAllAnimations()
    {
        if (moveTweenId >= 0)
            LeanTween.cancel(moveTweenId);

        if (spriteVisual != null)
            LeanTween.cancel(spriteVisual.gameObject);

        moveTweenId = -1;
        currentBounceId = -1;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceUpdatePosition()
    {
        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            long currentTotalSteps = dataManager.PlayerData.TotalSteps;
            long progressSteps = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);
            int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;

            float progress = requiredSteps > 0 ? (float)progressSteps / requiredSteps : 0f;
            progress = Mathf.Clamp01(progress);

            if (travelSprite != null)
            {
                Vector3 currentPosition = Vector3.Lerp(startPosition, endPosition, progress);
                travelSprite.transform.position = currentPosition;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!showDebugPath) return;

        Gizmos.color = debugPathColor;

        if (startPosition != Vector3.zero && endPosition != Vector3.zero)
        {
            Gizmos.DrawLine(startPosition, endPosition);
            Gizmos.DrawWireSphere(startPosition, 0.5f);
            Gizmos.DrawWireSphere(endPosition, 0.5f);
        }
    }
}