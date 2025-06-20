// ===============================================
// MapManager avec Pathfinding Intelligent - ENHANCED VERSION
// ===============================================
// Purpose: Handles player movement between locations with intelligent pathfinding
// Filepath: Assets/Scripts/Gameplay/World/MapManager.cs

using MapEvents;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    // === PUBLIC API ===
    [Header("Registry")]
    [SerializeField] private LocationRegistry _locationRegistry;
    public LocationRegistry LocationRegistry => _locationRegistry;

    [Header("Travel Save Settings")]
    [SerializeField] private float travelSaveInterval = 10f;
    [SerializeField] private int minStepsProgressToSave = 5;

    [Header("Pathfinding Settings")]
    [SerializeField] private bool enablePathfinding = true;
    [SerializeField] private bool enablePathfindingDebug = true;

    // === PUBLIC PROPERTIES ===
    public MapLocationDefinition CurrentLocation { get; private set; }

    // === INTERNAL SERVICES ===
    private MapTravelService travelService;
    private MapLocationService locationService;
    private MapSaveService saveService;
    private MapValidationService validationService;
    private MapEventService eventService;
    private MapPathfindingService pathfindingService; // NOUVEAU !

    // === INTERNAL ACCESSORS FOR SERVICES ===
    internal MapEventService EventService => eventService;
    internal MapSaveService SaveService => saveService;
    internal MapValidationService ValidationService => validationService;
    internal MapPathfindingService PathfindingService => pathfindingService; // NOUVEAU !

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeServices();
        }
        else
        {
            Logger.LogWarning("MapManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.MapLog);
            Destroy(gameObject);
        }
    }

    void Start()
    {
        locationService?.Initialize();
        travelService?.Initialize();

        // NOUVEAU : Initialiser le pathfinding
        if (enablePathfinding && pathfindingService != null)
        {
            pathfindingService.RebuildCache();
        }
    }

    void Update()
    {
        travelService?.Update();
        saveService?.Update();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void InitializeServices()
    {
        eventService = new MapEventService();
        validationService = new MapValidationService(this);
        saveService = new MapSaveService(this, travelSaveInterval, minStepsProgressToSave);
        locationService = new MapLocationService(this, eventService);

        // NOUVEAU : Initialiser le service de pathfinding
        if (enablePathfinding && _locationRegistry != null)
        {
            pathfindingService = new MapPathfindingService(_locationRegistry);
            Logger.LogInfo("MapManager: Pathfinding service initialized", Logger.LogCategory.MapLog);
        }

        travelService = new MapTravelService(this, locationService, validationService, eventService, saveService, pathfindingService);
    }

    // === PUBLIC API - ENHANCED WITH PATHFINDING ===

    /// <summary>
    /// ENHANCED: Peut maintenant voyager vers des destinations non directement connectées
    /// </summary>
    public bool CanTravelTo(string destinationLocationId)
    {
        return validationService?.CanTravelTo(destinationLocationId) ?? false;
    }

    /// <summary>
    /// ENHANCED: Démarre un voyage intelligent (direct ou avec pathfinding)
    /// </summary>
    public void StartTravel(string destinationLocationId)
    {
        travelService?.StartTravel(destinationLocationId);
    }

    /// <summary>
    /// ENHANCED: Retourne des informations détaillées sur le voyage (incluant pathfinding)
    /// </summary>
    public TravelInfo GetTravelInfo(string destinationLocationId)
    {
        return travelService?.GetTravelInfo(destinationLocationId);
    }

    /// <summary>
    /// NOUVEAU: Retourne les détails du chemin calculé par pathfinding
    /// </summary>
    public MapPathfindingService.PathResult GetPathDetails(string destinationLocationId)
    {
        if (CurrentLocation == null || !enablePathfinding || pathfindingService == null)
            return null;

        return pathfindingService.FindPath(CurrentLocation.LocationID, destinationLocationId);
    }

    /// <summary>
    /// NOUVEAU: Debug d'un chemin spécifique
    /// </summary>
    public void DebugPath(string destinationLocationId)
    {
        if (CurrentLocation == null || !enablePathfindingDebug || pathfindingService == null)
            return;

        pathfindingService.DebugPath(CurrentLocation.LocationID, destinationLocationId);
    }

    public void OnPOIClicked(string locationId)
    {
        travelService?.OnPOIClicked(locationId);
    }

    public void ClearTravelState()
    {
        travelService?.ClearTravelState();
    }

    public void ForceSaveTravelProgress()
    {
        saveService?.ForceSaveTravelProgress();
    }

    // === INTERNAL METHODS FOR SERVICES ===
    internal void SetCurrentLocation(MapLocationDefinition location)
    {
        CurrentLocation = location;
    }

    // Helper class for travel information - ENHANCED
    [System.Serializable]
    public class TravelInfo
    {
        public MapLocationDefinition From;
        public MapLocationDefinition To;
        public int StepCost;
        public bool CanTravel;

        // NOUVEAU : Informations sur le pathfinding
        public bool RequiresPathfinding;
        public MapPathfindingService.PathResult PathDetails;
        public int SegmentCount => PathDetails?.Segments?.Count ?? 0;
    }
}

// ===============================================
// SERVICE: Validation - ENHANCED WITH PATHFINDING
// ===============================================
public class MapValidationService
{
    private readonly MapManager manager;

    public MapValidationService(MapManager manager)
    {
        this.manager = manager;
    }

    /// <summary>
    /// ENHANCED: Vérifie maintenant les connexions directes ET le pathfinding
    /// </summary>
    public bool CanTravelTo(string destinationLocationId)
    {
        if (manager.CurrentLocation == null)
        {
            Logger.LogError("MapManager: Cannot travel - no current location set", Logger.LogCategory.MapLog);
            return false;
        }

        if (string.IsNullOrEmpty(destinationLocationId))
        {
            Logger.LogWarning("MapManager: Cannot travel - destination ID is null or empty", Logger.LogCategory.MapLog);
            return false;
        }

        if (!manager.LocationRegistry.HasLocation(destinationLocationId))
        {
            Logger.LogWarning($"MapManager: Cannot travel - destination '{destinationLocationId}' not found in registry", Logger.LogCategory.MapLog);
            return false;
        }

        if (manager.CurrentLocation.LocationID == destinationLocationId)
        {
            Logger.LogInfo($"MapManager: Cannot travel - already at destination '{destinationLocationId}'", Logger.LogCategory.MapLog);
            return false;
        }

        if (ActivityManager.Instance.ShouldBlockTravel())
        {
            Logger.LogInfo($"MapManager: Cannot travel - activity in progress blocks travel", Logger.LogCategory.MapLog);
            return false;
        }

        // NOUVEAU : Vérifier d'abord les connexions directes
        if (manager.LocationRegistry.CanTravelBetween(manager.CurrentLocation.LocationID, destinationLocationId))
        {
            Logger.LogInfo($"MapManager: Direct connection available to '{destinationLocationId}'", Logger.LogCategory.MapLog);
            return true;
        }

        // NOUVEAU : Vérifier le pathfinding si activé
        if (manager.PathfindingService != null)
        {
            bool canReach = manager.PathfindingService.CanReach(manager.CurrentLocation.LocationID, destinationLocationId);
            if (canReach)
            {
                Logger.LogInfo($"MapManager: Pathfinding route available to '{destinationLocationId}'", Logger.LogCategory.MapLog);
                return true;
            }
        }

        Logger.LogInfo($"MapManager: Cannot travel - no path found to '{destinationLocationId}'", Logger.LogCategory.MapLog);
        return false;
    }

    public string GetTravelBlockReason(string destinationLocationId)
    {
        if (manager.CurrentLocation == null)
            return "no current location set.";

        if (string.IsNullOrEmpty(destinationLocationId))
            return "destination ID is invalid.";

        var destinationLocation = manager.LocationRegistry.GetLocationById(destinationLocationId);
        if (destinationLocation == null)
            return $"destination '{destinationLocationId}' not found.";

        if (manager.CurrentLocation.LocationID == destinationLocationId)
            return $"already at '{destinationLocation.DisplayName}'.";

        if (ActivityManager.Instance.ShouldBlockTravel())
            return "activity in progress blocks travel.";

        // ENHANCED : Message plus détaillé
        bool hasDirectConnection = manager.LocationRegistry.CanTravelBetween(manager.CurrentLocation.LocationID, destinationLocationId);
        bool hasPathfindingRoute = manager.PathfindingService?.CanReach(manager.CurrentLocation.LocationID, destinationLocationId) ?? false;

        if (!hasDirectConnection && !hasPathfindingRoute)
            return $"no route available to '{destinationLocation.DisplayName}'.";

        return "unknown reason";
    }
}

// ===============================================
// SERVICE: Travel Management - ENHANCED WITH PATHFINDING
// ===============================================
public class MapTravelService
{
    private readonly MapManager manager;
    private readonly MapLocationService locationService;
    private readonly MapValidationService validationService;
    private readonly MapEventService eventService;
    private readonly MapSaveService saveService;
    private readonly MapPathfindingService pathfindingService; // NOUVEAU !
    private int lastProgressSteps = -1;

    // NOUVEAU : État pour voyage multi-segments
    private MapPathfindingService.PathResult currentPathDetails;
    private int currentSegmentIndex;
    private bool isMultiSegmentTravel;

    public MapTravelService(MapManager manager, MapLocationService locationService,
                           MapValidationService validationService, MapEventService eventService,
                           MapSaveService saveService, MapPathfindingService pathfindingService = null)
    {
        this.manager = manager;
        this.locationService = locationService;
        this.validationService = validationService;
        this.eventService = eventService;
        this.saveService = saveService;
        this.pathfindingService = pathfindingService; // NOUVEAU !
    }

    public void Initialize()
    {
        ValidateTravelState();
    }

    public void Update()
    {
        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            CheckTravelProgress();
        }
    }

    /// <summary>
    /// ENHANCED: Démarre un voyage intelligent
    /// </summary>
    public void StartTravel(string destinationLocationId)
    {
        if (!validationService.CanTravelTo(destinationLocationId))
        {
            Logger.LogWarning($"MapManager: StartTravel called for '{destinationLocationId}', but CanTravelTo returned false.", Logger.LogCategory.MapLog);
            return;
        }

        var dataManager = DataManager.Instance;
        var destination = manager.LocationRegistry.GetLocationById(destinationLocationId);

        if (destination == null)
        {
            Logger.LogError($"MapManager: StartTravel - Destination '{destinationLocationId}' not found in registry.", Logger.LogCategory.MapLog);
            return;
        }

        // NOUVEAU : Déterminer le type de voyage (direct ou pathfinding)
        bool isDirectConnection = manager.LocationRegistry.CanTravelBetween(manager.CurrentLocation.LocationID, destinationLocationId);

        if (isDirectConnection)
        {
            // Voyage direct (comportement original)
            StartDirectTravel(destinationLocationId, destination, dataManager);
        }
        else if (pathfindingService != null)
        {
            // Voyage avec pathfinding
            StartPathfindingTravel(destinationLocationId, destination, dataManager);
        }
        else
        {
            Logger.LogError($"MapManager: Cannot start travel - no direct connection and pathfinding disabled", Logger.LogCategory.MapLog);
        }
    }

    /// <summary>
    /// NOUVEAU : Démarre un voyage direct (comportement original)
    /// </summary>
    private void StartDirectTravel(string destinationLocationId, MapLocationDefinition destination, DataManager dataManager)
    {
        int stepCost = manager.LocationRegistry.GetTravelCost(manager.CurrentLocation.LocationID, destinationLocationId);
        long currentSteps = dataManager.PlayerData.TotalSteps;

        // Configurer les données de voyage
        dataManager.PlayerData.TravelDestinationId = destinationLocationId;
        dataManager.PlayerData.TravelStartSteps = currentSteps;
        dataManager.PlayerData.TravelRequiredSteps = stepCost;

        // Réinitialiser l'état multi-segment
        ResetMultiSegmentState();

        // Sauvegarder immédiatement le début du voyage
        dataManager.SaveGame();
        saveService.ResetSaveTracking(currentSteps);

        Logger.LogInfo($"MapManager: Started DIRECT travel from {manager.CurrentLocation.LocationID} to {destinationLocationId} ({stepCost} steps)", Logger.LogCategory.MapLog);

        // Déclencher l'événement
        eventService.TriggerTravelStarted(destinationLocationId, manager.CurrentLocation, stepCost);
    }

    /// <summary>
    /// NOUVEAU : Démarre un voyage avec pathfinding
    /// </summary>
    private void StartPathfindingTravel(string destinationLocationId, MapLocationDefinition destination, DataManager dataManager)
    {
        // Calculer le chemin
        currentPathDetails = pathfindingService.FindPath(manager.CurrentLocation.LocationID, destinationLocationId);

        if (!currentPathDetails.IsReachable)
        {
            Logger.LogError($"MapManager: Pathfinding failed to find route to {destinationLocationId}", Logger.LogCategory.MapLog);
            return;
        }

        // Démarrer le voyage multi-segment
        isMultiSegmentTravel = true;
        currentSegmentIndex = 0;

        // Démarrer le premier segment
        var firstSegment = currentPathDetails.Segments[0];
        StartSegmentTravel(firstSegment, dataManager);

        Logger.LogInfo($"MapManager: Started PATHFINDING travel from {manager.CurrentLocation.LocationID} to {destinationLocationId} " +
                      $"({currentPathDetails.TotalCost} total steps, {currentPathDetails.Segments.Count} segments)", Logger.LogCategory.MapLog);

        // Déclencher l'événement avec le coût total
        eventService.TriggerTravelStarted(destinationLocationId, manager.CurrentLocation, currentPathDetails.TotalCost);
    }

    /// <summary>
    /// NOUVEAU : Démarre un segment individuel du voyage
    /// </summary>
    private void StartSegmentTravel(MapPathfindingService.PathSegment segment, DataManager dataManager)
    {
        long currentSteps = dataManager.PlayerData.TotalSteps;

        // Configurer les données de voyage pour ce segment
        dataManager.PlayerData.TravelDestinationId = segment.ToLocationId;
        dataManager.PlayerData.TravelStartSteps = currentSteps;
        dataManager.PlayerData.TravelRequiredSteps = segment.StepCost;

        // Sauvegarder
        dataManager.SaveGame();
        saveService.ResetSaveTracking(currentSteps);

        Logger.LogInfo($"MapManager: Starting segment {currentSegmentIndex + 1}/{currentPathDetails.Segments.Count} " +
                      $"from {segment.FromLocationId} to {segment.ToLocationId} ({segment.StepCost} steps)", Logger.LogCategory.MapLog);
    }

    public void CheckTravelProgress()
    {
        var dataManager = DataManager.Instance;
        if (!dataManager.PlayerData.IsCurrentlyTraveling()) return;

        long currentTotalSteps = dataManager.PlayerData.TotalSteps;
        string destinationId = dataManager.PlayerData.TravelDestinationId;
        int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;
        long progressSteps = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);

        // Mettre à jour la sauvegarde si nécessaire
        saveService.CheckAndSaveProgress(currentTotalSteps, progressSteps);

        // Vérifier si le segment actuel est terminé
        if (dataManager.PlayerData.IsTravelComplete(currentTotalSteps))
        {
            if (isMultiSegmentTravel && currentSegmentIndex < currentPathDetails.Segments.Count - 1)
            {
                // Passer au segment suivant
                CompleteCurrentSegment(dataManager);
            }
            else
            {
                // Voyage terminé
                CompleteTravel();
            }
        }
        else
        {
            // Déclencher l'événement de progrès seulement si ça a changé
            if ((int)progressSteps != lastProgressSteps)
            {
                eventService.TriggerTravelProgress(destinationId, (int)progressSteps, requiredSteps);
                lastProgressSteps = (int)progressSteps;
            }
        }
    }

    /// <summary>
    /// NOUVEAU : Termine le segment actuel et démarre le suivant
    /// </summary>
    private void CompleteCurrentSegment(DataManager dataManager)
    {
        var currentSegment = currentPathDetails.Segments[currentSegmentIndex];
        var segmentDestination = manager.LocationRegistry.GetLocationById(currentSegment.ToLocationId);

        Logger.LogInfo($"MapManager: Completed segment {currentSegmentIndex + 1}/{currentPathDetails.Segments.Count} " +
                      $"- arrived at {segmentDestination?.DisplayName}", Logger.LogCategory.MapLog);

        // Mettre à jour la location actuelle
        manager.SetCurrentLocation(segmentDestination);
        dataManager.PlayerData.CurrentLocationId = currentSegment.ToLocationId;

        // Nettoyer l'état du segment actuel
        dataManager.PlayerData.TravelDestinationId = null;
        dataManager.PlayerData.TravelStartSteps = 0;
        dataManager.PlayerData.TravelRequiredSteps = 0;

        // Passer au segment suivant
        currentSegmentIndex++;
        if (currentSegmentIndex < currentPathDetails.Segments.Count)
        {
            var nextSegment = currentPathDetails.Segments[currentSegmentIndex];
            StartSegmentTravel(nextSegment, dataManager);
        }

        // Sauvegarder
        dataManager.SaveGame();
    }

    public void CompleteTravel()
    {
        var dataManager = DataManager.Instance;
        string destinationId = dataManager.PlayerData.TravelDestinationId;
        var destinationLocation = manager.LocationRegistry.GetLocationById(destinationId);

        if (destinationLocation == null)
        {
            Logger.LogError($"MapManager: CompleteTravel - Destination '{destinationId}' not found. Cancelling travel.", Logger.LogCategory.MapLog);
            ClearTravelState();
            return;
        }

        // Calculer les pas pris pour le voyage
        long startSteps = dataManager.PlayerData.TravelStartSteps;
        long currentSteps = dataManager.PlayerData.TotalSteps;
        int stepsTaken = (int)(currentSteps - startSteps);

        // NOUVEAU : Si c'est un voyage multi-segment, calculer le total
        int totalStepsTaken = stepsTaken;
        if (isMultiSegmentTravel && currentPathDetails != null)
        {
            totalStepsTaken = currentPathDetails.TotalCost;
        }

        Logger.LogInfo($"MapManager: Travel completed! Arrived at {destinationLocation.DisplayName} after {totalStepsTaken} steps", Logger.LogCategory.MapLog);

        // Garder référence de l'ancienne location pour l'événement
        var previousLocation = manager.CurrentLocation;

        // Mettre à jour la location du joueur
        dataManager.PlayerData.CurrentLocationId = destinationId;

        // Nettoyer les données de voyage
        dataManager.PlayerData.TravelDestinationId = null;
        dataManager.PlayerData.TravelStartSteps = 0;
        dataManager.PlayerData.TravelRequiredSteps = 0;

        // Mettre à jour la location actuelle dans le manager
        manager.SetCurrentLocation(destinationLocation);

        // NOUVEAU : Nettoyer l'état multi-segment
        ResetMultiSegmentState();

        // Reset du tracking de sauvegarde
        saveService.ResetSaveTracking(-1);

        // Sauvegarde immédiate et obligatoire à la fin du voyage
        dataManager.SaveGame();
        Logger.LogInfo($"MapManager: Travel state cleared and game saved", Logger.LogCategory.MapLog);

        // Déclencher les événements APRÈS que tout l'état soit mis à jour et sauvé
        eventService.TriggerTravelCompleted(destinationId, destinationLocation, totalStepsTaken);
        eventService.TriggerLocationChanged(previousLocation, destinationLocation);
    }

    /// <summary>
    /// ENHANCED: Retourne des informations détaillées sur le voyage
    /// </summary>
    public MapManager.TravelInfo GetTravelInfo(string destinationLocationId)
    {
        if (manager.CurrentLocation == null || !manager.LocationRegistry.HasLocation(destinationLocationId))
            return null;

        var destination = manager.LocationRegistry.GetLocationById(destinationLocationId);
        if (destination == null) return null;

        bool canTravel = validationService.CanTravelTo(destinationLocationId);
        bool isDirectConnection = manager.LocationRegistry.CanTravelBetween(manager.CurrentLocation.LocationID, destinationLocationId);

        int stepCost;
        MapPathfindingService.PathResult pathDetails = null;

        if (isDirectConnection)
        {
            stepCost = manager.LocationRegistry.GetTravelCost(manager.CurrentLocation.LocationID, destinationLocationId);
        }
        else if (pathfindingService != null)
        {
            pathDetails = pathfindingService.FindPath(manager.CurrentLocation.LocationID, destinationLocationId);
            stepCost = pathDetails.IsReachable ? pathDetails.TotalCost : -1;
        }
        else
        {
            stepCost = -1;
        }

        return new MapManager.TravelInfo
        {
            From = manager.CurrentLocation,
            To = destination,
            StepCost = stepCost,
            CanTravel = canTravel,
            RequiresPathfinding = !isDirectConnection,
            PathDetails = pathDetails
        };
    }

    public void OnPOIClicked(string locationId)
    {
        if (validationService.CanTravelTo(locationId))
        {
            var travelInfo = GetTravelInfo(locationId);
            if (travelInfo != null && travelInfo.To != null)
            {
                string travelType = travelInfo.RequiresPathfinding ? "PATHFINDING" : "DIRECT";
                Logger.LogInfo($"MapManager: POI clicked - Starting {travelType} travel to {travelInfo.To.DisplayName}", Logger.LogCategory.MapLog);
                StartTravel(locationId);
            }
            else
            {
                Logger.LogWarning($"MapManager: CanTravelTo '{locationId}' was true, but GetTravelInfo failed.", Logger.LogCategory.MapLog);
            }
        }
        else
        {
            Logger.LogInfo($"MapManager: POI clicked but cannot travel to '{locationId}'", Logger.LogCategory.MapLog);
        }
    }

    /// <summary>
    /// NOUVEAU : Réinitialise l'état multi-segment
    /// </summary>
    private void ResetMultiSegmentState()
    {
        isMultiSegmentTravel = false;
        currentSegmentIndex = 0;
        currentPathDetails = null;
    }

    public void ClearTravelState()
    {
        var dataManager = DataManager.Instance;

        // Nettoyer les données de voyage
        dataManager.PlayerData.TravelDestinationId = null;
        dataManager.PlayerData.TravelStartSteps = 0;
        dataManager.PlayerData.TravelRequiredSteps = 0;

        // NOUVEAU : Nettoyer l'état multi-segment
        ResetMultiSegmentState();

        // Reset du tracking de sauvegarde
        saveService.ResetSaveTracking(-1);

        dataManager.SaveGame();
        Logger.LogInfo($"MapManager: Travel state cleared", Logger.LogCategory.MapLog);
    }

    private void ValidateTravelState()
    {
        var dataManager = DataManager.Instance;
        if (!dataManager.PlayerData.IsCurrentlyTraveling())
        {
            return; // Pas en voyage, rien à valider
        }

        string destination = dataManager.PlayerData.TravelDestinationId;
        int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;
        long currentTotalSteps = dataManager.PlayerData.TotalSteps;
        long travelProgress = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);

        if (string.IsNullOrEmpty(destination))
        {
            Logger.LogWarning($"MapManager: Invalid travel destination. Clearing travel state.", Logger.LogCategory.MapLog);
            ClearTravelState();
            return;
        }

        if (requiredSteps <= 0)
        {
            Logger.LogWarning($"MapManager: Invalid travel required steps: {requiredSteps}. Clearing travel state.", Logger.LogCategory.MapLog);
            ClearTravelState();
            return;
        }

        if (travelProgress < 0)
        {
            Logger.LogWarning($"MapManager: Invalid travel progress: {travelProgress}. Clearing travel state.", Logger.LogCategory.MapLog);
            ClearTravelState();
            return;
        }

        // Si le voyage est déjà terminé selon les pas, le compléter immédiatement
        if (dataManager.PlayerData.IsTravelComplete(currentTotalSteps))
        {
            Logger.LogInfo($"MapManager: Travel already complete on startup - completing now", Logger.LogCategory.MapLog);
            CompleteTravel();
            return;
        }

        // État de voyage valide, continuer normalement
        Logger.LogInfo($"MapManager: Travel state valid - continuing travel to {destination}", Logger.LogCategory.MapLog);
    }
}

// ===============================================
// SERVICE: Event Management - MAINTENANT AVEC EVENTBUS
// ===============================================
public class MapEventService
{
    public void TriggerLocationChanged(MapLocationDefinition previousLocation, MapLocationDefinition newLocation)
    {
        EventBus.Publish(new LocationChangedEvent(previousLocation, newLocation));
    }

    public void TriggerTravelProgress(string destinationId, int currentSteps, int requiredSteps)
    {
        EventBus.Publish(new TravelProgressEvent(destinationId, currentSteps, requiredSteps));
    }

    public void TriggerTravelStarted(string destinationId, MapLocationDefinition currentLocation, int requiredSteps)
    {
        EventBus.Publish(new TravelStartedEvent(destinationId, currentLocation, requiredSteps));
    }

    public void TriggerTravelCompleted(string destinationId, MapLocationDefinition newLocation, int stepsTaken)
    {
        EventBus.Publish(new TravelCompletedEvent(destinationId, newLocation, stepsTaken));
    }
}

// ===============================================
// SERVICE: Location Management
// ===============================================
public class MapLocationService
{
    private readonly MapManager manager;
    private readonly MapEventService eventService;

    public MapLocationService(MapManager manager, MapEventService eventService)
    {
        this.manager = manager;
        this.eventService = eventService;
    }

    public void Initialize()
    {
        LoadCurrentLocation();
    }

    private void LoadCurrentLocation()
    {
        var dataManager = DataManager.Instance;
        string currentLocationId = dataManager.PlayerData.CurrentLocationId;

        if (string.IsNullOrEmpty(currentLocationId))
        {
            Logger.LogWarning("MapManager: No current location set in PlayerData. Defaulting to first location.", Logger.LogCategory.MapLog);
            currentLocationId = "Foret_01"; // Default starting location
            dataManager.PlayerData.CurrentLocationId = currentLocationId;
            dataManager.SaveGame();
        }

        var currentLocation = manager.LocationRegistry.GetLocationById(currentLocationId);

        if (currentLocation == null)
        {
            Logger.LogError($"MapManager: Current location '{currentLocationId}' not found in registry!", Logger.LogCategory.MapLog);
        }
        else
        {
            manager.SetCurrentLocation(currentLocation);
            Logger.LogInfo($"MapManager: Current location loaded: {currentLocation.DisplayName} ({currentLocationId})", Logger.LogCategory.MapLog);

            // Si on est en voyage, restaurer l'état au démarrage
            if (dataManager.PlayerData.IsCurrentlyTraveling())
            {
                long currentTotalSteps = dataManager.PlayerData.TotalSteps;
                long progressSteps = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);
                int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;
                string destinationId = dataManager.PlayerData.TravelDestinationId;

                Logger.LogInfo($"MapManager: Restored travel state - {progressSteps}/{requiredSteps} steps to {destinationId}", Logger.LogCategory.MapLog);
                manager.SaveService.ResetSaveTracking(currentTotalSteps);
            }
        }
    }
}

// ===============================================
// SERVICE: Save Management
// ===============================================
public class MapSaveService
{
    private readonly MapManager manager;
    private readonly float travelSaveInterval;
    private readonly int minStepsProgressToSave;

    private long lastSavedTotalSteps = -1;
    private float timeSinceLastTravelSave = 0f;
    private const float TRAVEL_SAVE_INTERVAL_DURING_TRAVEL = 20f;

    public MapSaveService(MapManager manager, float travelSaveInterval, int minStepsProgressToSave)
    {
        this.manager = manager;
        this.travelSaveInterval = travelSaveInterval;
        this.minStepsProgressToSave = minStepsProgressToSave;
    }

    public void Update()
    {
        timeSinceLastTravelSave += Time.deltaTime;
    }

    public void CheckAndSaveProgress(long currentTotalSteps, long progressSteps)
    {
        bool shouldSave = false;

        if (lastSavedTotalSteps == -1)
        {
            shouldSave = true;
        }
        else if (currentTotalSteps - lastSavedTotalSteps >= minStepsProgressToSave)
        {
            shouldSave = true;
        }
        else if (timeSinceLastTravelSave >= TRAVEL_SAVE_INTERVAL_DURING_TRAVEL)
        {
            shouldSave = true;
        }

        if (shouldSave)
        {
            DataManager.Instance.SaveGame();
            lastSavedTotalSteps = currentTotalSteps;
            timeSinceLastTravelSave = 0f;

            Logger.LogInfo($"MapManager: Travel progress saved at {progressSteps} steps", Logger.LogCategory.MapLog);
        }
    }

    public void ForceSaveTravelProgress()
    {
        var dataManager = DataManager.Instance;
        dataManager.SaveGame();
        lastSavedTotalSteps = dataManager.PlayerData.TotalSteps;
        timeSinceLastTravelSave = 0f;
        Logger.LogInfo("MapManager: Travel progress force saved", Logger.LogCategory.MapLog);
    }

    public void ResetSaveTracking(long totalSteps)
    {
        lastSavedTotalSteps = totalSteps;
        timeSinceLastTravelSave = 0f;
    }
}