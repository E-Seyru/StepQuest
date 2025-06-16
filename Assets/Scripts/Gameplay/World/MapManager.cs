// ===============================================
// NOUVEAU MapManager (Facade Pattern) - REFACTORED
// ===============================================
// Purpose: Handles player movement between locations on the world map based on steps
// Filepath: Assets/Scripts/Gameplay/World/MapManager.cs

using System;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    // === SAME PUBLIC API - ZERO BREAKING CHANGES ===
    [Header("Registry")]
    [SerializeField] private LocationRegistry _locationRegistry;
    public LocationRegistry LocationRegistry => _locationRegistry;

    [Header("Travel Save Settings")]
    [SerializeField] private float travelSaveInterval = 10f;
    [SerializeField] private int minStepsProgressToSave = 5;

    // === PUBLIC PROPERTIES - SAME AS BEFORE ===
    public MapLocationDefinition CurrentLocation { get; private set; }

    // === PUBLIC EVENTS - SAME AS BEFORE ===
    public event Action<MapLocationDefinition> OnLocationChanged;
    public event Action<string, int, int> OnTravelProgress;
    public event Action<string> OnTravelStarted;
    public event Action<string> OnTravelCompleted;

    // === INTERNAL SERVICES (NOUVEAU) ===
    private MapTravelService travelService;
    private MapLocationService locationService;
    private MapSaveService saveService;
    private MapValidationService validationService;
    private MapEventService eventService;

    // === INTERNAL ACCESSORS FOR SERVICES ===
    internal MapEventService EventService => eventService;
    internal MapSaveService SaveService => saveService;
    internal MapValidationService ValidationService => validationService;

    internal void RaiseLocationChanged(MapLocationDefinition loc)
    => OnLocationChanged?.Invoke(loc);

    internal void RaiseTravelProgress(string destId, int cur, int req)
        => OnTravelProgress?.Invoke(destId, cur, req);

    internal void RaiseTravelStarted(string destId)
        => OnTravelStarted?.Invoke(destId);

    internal void RaiseTravelCompleted(string destId)
        => OnTravelCompleted?.Invoke(destId);

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

    private void InitializeServices()
    {
        // Initialiser les services dans l'ordre de dépendance
        eventService = new MapEventService(this);
        validationService = new MapValidationService(this);
        locationService = new MapLocationService(this, eventService);
        saveService = new MapSaveService(this, travelSaveInterval, minStepsProgressToSave);
        travelService = new MapTravelService(this, locationService, validationService, eventService, saveService);

        Logger.LogInfo("MapManager: Services initialized successfully", Logger.LogCategory.MapLog);
    }

    void Start()
    {
        if (_locationRegistry == null)
        {
            Logger.LogError("MapManager: LocationRegistry not assigned! Please assign it in the inspector.", Logger.LogCategory.MapLog);
            return;
        }

        if (DataManager.Instance == null)
        {
            Logger.LogError("MapManager: DataManager not found!", Logger.LogCategory.MapLog);
            return;
        }

        // Initialiser les services
        locationService?.Initialize();
        saveService?.Initialize();
        travelService?.Initialize();

        Logger.LogInfo("MapManager: Initialized successfully", Logger.LogCategory.MapLog);
    }

    void Update()
    {
        // Déléguer le travail au service de voyage
        travelService?.Update();
        saveService?.Update();
    }

    // === SAME PUBLIC API METHODS - ZERO BREAKING CHANGES ===

    public bool CanTravelTo(string destinationLocationId)
    {
        return validationService?.CanTravelTo(destinationLocationId) ?? false;
    }

    public void StartTravel(string destinationLocationId)
    {
        travelService?.StartTravel(destinationLocationId);
    }

    public TravelInfo GetTravelInfo(string destinationLocationId)
    {
        return travelService?.GetTravelInfo(destinationLocationId);
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

    // Helper class for travel information
    [System.Serializable]
    public class TravelInfo
    {
        public MapLocationDefinition From;
        public MapLocationDefinition To;
        public int StepCost;
        public bool CanTravel;
    }
}

// ===============================================
// SERVICE: Travel Management
// ===============================================
public class MapTravelService
{
    private readonly MapManager manager;
    private readonly MapLocationService locationService;
    private readonly MapValidationService validationService;
    private readonly MapEventService eventService;
    private readonly MapSaveService saveService;

    public MapTravelService(MapManager manager, MapLocationService locationService,
                           MapValidationService validationService, MapEventService eventService,
                           MapSaveService saveService)
    {
        this.manager = manager;
        this.locationService = locationService;
        this.validationService = validationService;
        this.eventService = eventService;
        this.saveService = saveService;
    }

    public void Initialize()
    {
        // Vérifier et nettoyer l'état de voyage au démarrage
        ValidateTravelState();
    }

    public void Update()
    {
        // Vérifier les progrès de voyage si actuellement en voyage
        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            CheckTravelProgress();
        }
    }

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

        int stepCost = manager.LocationRegistry.GetTravelCost(manager.CurrentLocation.LocationID, destinationLocationId);
        long currentSteps = dataManager.PlayerData.TotalSteps;

        // Configurer les données de voyage
        dataManager.PlayerData.TravelDestinationId = destinationLocationId;
        dataManager.PlayerData.TravelStartSteps = currentSteps;
        dataManager.PlayerData.TravelRequiredSteps = stepCost;

        // Sauvegarder immédiatement le début du voyage
        dataManager.SaveGame();
        saveService.ResetSaveTracking(currentSteps);

        Logger.LogInfo($"MapManager: Started travel from {manager.CurrentLocation.LocationID} to {destinationLocationId} ({stepCost} steps)", Logger.LogCategory.MapLog);

        // Déclencher les événements
        eventService.TriggerTravelStarted(destinationLocationId);
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

        // Log progress moins souvent
        if (progressSteps > 0 && (progressSteps % 25 == 0 || progressSteps == 1))
        {
            var destinationLocation = manager.LocationRegistry.GetLocationById(destinationId);
            Logger.LogInfo($"MapManager: Travel progress {progressSteps}/{requiredSteps} steps to {destinationId}", Logger.LogCategory.MapLog);
        }

        // Déclencher l'événement de progrès
        eventService.TriggerTravelProgress(destinationId, (int)progressSteps, requiredSteps);

        // Vérifier si le voyage est terminé
        if (dataManager.PlayerData.IsTravelComplete(currentTotalSteps))
        {
            CompleteTravel();
        }
    }

    public void CompleteTravel()
    {
        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData == null) return;

        string destinationId = dataManager.PlayerData.TravelDestinationId;
        var destinationLocation = manager.LocationRegistry.GetLocationById(destinationId);

        if (destinationLocation == null)
        {
            Logger.LogError($"MapManager: CompleteTravel - Destination ID '{destinationId}' not found in registry. Cancelling travel.", Logger.LogCategory.MapLog);
            ClearTravelState();
            return;
        }

        long finalProgressSteps = dataManager.PlayerData.GetTravelProgress(dataManager.PlayerData.TotalSteps);
        Logger.LogInfo($"MapManager: Travel completed! Arrived at {destinationLocation.DisplayName} after {finalProgressSteps} steps", Logger.LogCategory.MapLog);

        // Mettre à jour la location du joueur
        dataManager.PlayerData.CurrentLocationId = destinationId;

        // Nettoyer les données de voyage
        dataManager.PlayerData.TravelDestinationId = null;
        dataManager.PlayerData.TravelStartSteps = 0;
        dataManager.PlayerData.TravelRequiredSteps = 0;

        // Mettre à jour la location actuelle dans le manager
        manager.SetCurrentLocation(destinationLocation);

        // Reset du tracking de sauvegarde
        saveService.ResetSaveTracking(-1);

        // Sauvegarde immédiate et obligatoire à la fin du voyage
        dataManager.SaveGame();
        Logger.LogInfo($"MapManager: Travel state cleared and game saved", Logger.LogCategory.MapLog);

        // Déclencher les événements APRÈS que tout l'état soit mis à jour et sauvé
        eventService.TriggerTravelCompleted(destinationId);
        eventService.TriggerLocationChanged(destinationLocation);
    }

    public MapManager.TravelInfo GetTravelInfo(string destinationLocationId)
    {
        if (manager.CurrentLocation == null || !manager.LocationRegistry.HasLocation(destinationLocationId))
            return null;

        var destination = manager.LocationRegistry.GetLocationById(destinationLocationId);
        if (destination == null) return null;

        int stepCost = manager.LocationRegistry.GetTravelCost(manager.CurrentLocation.LocationID, destinationLocationId);
        bool canTravel = validationService.CanTravelTo(destinationLocationId);

        return new MapManager.TravelInfo
        {
            From = manager.CurrentLocation,
            To = destination,
            StepCost = stepCost,
            CanTravel = canTravel
        };
    }

    public void OnPOIClicked(string locationId)
    {
        if (validationService.CanTravelTo(locationId))
        {
            var travelInfo = GetTravelInfo(locationId);
            if (travelInfo != null && travelInfo.To != null)
            {
                Logger.LogInfo($"MapManager: POI clicked - Starting travel to {travelInfo.To.DisplayName}", Logger.LogCategory.MapLog);
                StartTravel(locationId);
            }
            else
            {
                Logger.LogWarning($"MapManager: CanTravelTo '{locationId}' was true, but GetTravelInfo failed. This should not happen.", Logger.LogCategory.MapLog);
            }
        }
        else
        {
            var destinationLocation = manager.LocationRegistry.GetLocationById(locationId);
            string reason = validationService.GetTravelBlockReason(locationId);
            Logger.LogInfo($"MapManager: Cannot travel to '{destinationLocation?.DisplayName ?? locationId}' ({locationId}). Reason: {reason}", Logger.LogCategory.MapLog);
        }
    }

    public void ClearTravelState()
    {
        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData == null) return;

        string oldDest = dataManager.PlayerData.TravelDestinationId;
        long oldStartSteps = dataManager.PlayerData.TravelStartSteps;
        int oldReqSteps = dataManager.PlayerData.TravelRequiredSteps;

        Logger.LogInfo($"MapManager: Clearing travel state - was traveling to '{oldDest}' ({oldStartSteps} -> {oldReqSteps} steps)", Logger.LogCategory.MapLog);

        dataManager.PlayerData.TravelDestinationId = null;
        dataManager.PlayerData.TravelStartSteps = 0;
        dataManager.PlayerData.TravelRequiredSteps = 0;

        // Reset tracking
        saveService.ResetSaveTracking(-1);

        dataManager.SaveGame();
    }

    private void ValidateTravelState()
    {
        var dataManager = DataManager.Instance;
        if (!dataManager.PlayerData.IsCurrentlyTraveling())
        {
            Logger.LogInfo("MapManager: No ongoing travel found at startup.", Logger.LogCategory.MapLog);
            return;
        }

        string destination = dataManager.PlayerData.TravelDestinationId;
        long currentTotalSteps = dataManager.PlayerData.TotalSteps;
        long travelProgress = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);
        int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;

        Logger.LogInfo($"MapManager: Validating travel state - destination: '{destination}', progress: {travelProgress}/{requiredSteps}", Logger.LogCategory.MapLog);

        // Vérifier que l'état de voyage est cohérent
        if (string.IsNullOrEmpty(destination) || !manager.LocationRegistry.HasLocation(destination))
        {
            Logger.LogWarning($"MapManager: Invalid travel destination '{destination}'. Clearing travel state.", Logger.LogCategory.MapLog);
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

    public void Initialize()
    {
        // Service initialization if needed
    }

    public void Update()
    {
        if (DataManager.Instance?.PlayerData?.IsCurrentlyTraveling() == true)
        {
            timeSinceLastTravelSave += Time.deltaTime;
        }
    }

    public void CheckAndSaveProgress(long currentTotalSteps, long progressSteps)
    {
        bool shouldSave = ShouldSaveProgress(currentTotalSteps);

        if (shouldSave)
        {
            DataManager.Instance.SaveTravelProgress();
            lastSavedTotalSteps = currentTotalSteps;
            timeSinceLastTravelSave = 0f;

            Logger.LogInfo($"MapManager: Auto-saved travel progress: {progressSteps}/{DataManager.Instance.PlayerData.TravelRequiredSteps}", Logger.LogCategory.MapLog);
        }
    }

    public void ForceSaveTravelProgress()
    {
        var dataManager = DataManager.Instance;
        if (dataManager.PlayerData.IsCurrentlyTraveling())
        {
            dataManager.SaveTravelProgress();
            lastSavedTotalSteps = dataManager.PlayerData.TotalSteps;

            long currentProgress = dataManager.PlayerData.GetTravelProgress(dataManager.PlayerData.TotalSteps);
            Logger.LogInfo($"MapManager: Force saved travel progress: {currentProgress}/{dataManager.PlayerData.TravelRequiredSteps}", Logger.LogCategory.MapLog);
        }
    }

    public void ResetSaveTracking(long totalSteps)
    {
        lastSavedTotalSteps = totalSteps;
        timeSinceLastTravelSave = 0f;
    }

    private bool ShouldSaveProgress(long currentTotalSteps)
    {
        // Sauvegarder si c'est la première fois ou après l'intervalle de temps
        if (lastSavedTotalSteps == -1 || timeSinceLastTravelSave >= TRAVEL_SAVE_INTERVAL_DURING_TRAVEL)
        {
            return true;
        }

        // Sauvegarder si assez de progrès a été fait
        long stepsSinceLastSave = currentTotalSteps - lastSavedTotalSteps;
        return stepsSinceLastSave >= minStepsProgressToSave;
    }
}

// ===============================================
// SERVICE: Validation Management
// ===============================================
public class MapValidationService
{
    private readonly MapManager manager;

    public MapValidationService(MapManager manager)
    {
        this.manager = manager;
    }

    public bool CanTravelTo(string destinationLocationId)
    {
        if (ActivityManager.Instance.ShouldBlockTravel())
            return false;

        if (manager.CurrentLocation == null)
            return false;

        var dataManager = DataManager.Instance;
        if (dataManager.PlayerData.IsCurrentlyTraveling())
            return false;

        var destination = manager.LocationRegistry.GetLocationById(destinationLocationId);
        if (destination == null)
            return false;

        if (manager.CurrentLocation.LocationID == destinationLocationId)
            return false;

        if (!manager.LocationRegistry.CanTravelBetween(manager.CurrentLocation.LocationID, destinationLocationId))
            return false;

        return true;
    }

    public string GetTravelBlockReason(string destinationLocationId)
    {
        if (manager.CurrentLocation == null)
            return "no current player location.";

        var dataManager = DataManager.Instance;
        if (dataManager.PlayerData.IsCurrentlyTraveling())
            return $"already traveling to {dataManager.PlayerData.TravelDestinationId}.";

        var destinationLocation = manager.LocationRegistry.GetLocationById(destinationLocationId);
        if (destinationLocation == null)
            return $"destination '{destinationLocationId}' not found.";

        if (manager.CurrentLocation.LocationID == destinationLocationId)
            return $"already at '{destinationLocation.DisplayName}'.";

        if (!manager.LocationRegistry.CanTravelBetween(manager.CurrentLocation.LocationID, destinationLocationId))
            return $"not connected to '{destinationLocation.DisplayName}'.";

        if (ActivityManager.Instance.ShouldBlockTravel())
            return "activity in progress blocks travel.";

        return "unknown reason";
    }
}

// ===============================================
// SERVICE: Event Management
// ===============================================
public class MapEventService
{
    private readonly MapManager manager;

    public MapEventService(MapManager manager)
    {
        this.manager = manager;
    }

    public void TriggerLocationChanged(MapLocationDefinition location)
    {
        manager.RaiseLocationChanged(location);
    }

    public void TriggerTravelProgress(string destinationId, int currentSteps, int requiredSteps)
    {
        manager.RaiseTravelProgress(destinationId, currentSteps, requiredSteps);
    }

    public void TriggerTravelStarted(string destinationId)
    {
        manager.RaiseTravelStarted(destinationId);
    }

    public void TriggerTravelCompleted(string destinationId)
    {
        manager.RaiseTravelCompleted(destinationId);
    }
}