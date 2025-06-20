// ===============================================
// MapManager avec EventBus - REFACTORED
// ===============================================
// Purpose: Handles player movement between locations on the world map based on steps
// Filepath: Assets/Scripts/Gameplay/World/MapManager.cs

using MapEvents;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    // === PUBLIC API - SAME AS BEFORE ===
    [Header("Registry")]
    [SerializeField] private LocationRegistry _locationRegistry;
    public LocationRegistry LocationRegistry => _locationRegistry;

    [Header("Travel Save Settings")]
    [SerializeField] private float travelSaveInterval = 10f;
    [SerializeField] private int minStepsProgressToSave = 5;

    // === PUBLIC PROPERTIES - SAME AS BEFORE ===
    public MapLocationDefinition CurrentLocation { get; private set; }

    // === INTERNAL SERVICES ===
    private MapTravelService travelService;
    private MapLocationService locationService;
    private MapSaveService saveService;
    private MapValidationService validationService;
    private MapEventService eventService;

    // === INTERNAL ACCESSORS FOR SERVICES ===
    internal MapEventService EventService => eventService;
    internal MapSaveService SaveService => saveService;
    internal MapValidationService ValidationService => validationService;

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
        travelService = new MapTravelService(this, locationService, validationService, eventService, saveService);
    }

    // === PUBLIC API - SAME AS BEFORE ===
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

            // Si on est en voyage, restaurer l'etat au demarrage
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
// SERVICE: Travel Management
// ===============================================
public class MapTravelService
{
    private readonly MapManager manager;
    private readonly MapLocationService locationService;
    private readonly MapValidationService validationService;
    private readonly MapEventService eventService;
    private readonly MapSaveService saveService;
    private int lastProgressSteps = -1;

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

        // Configurer les donnees de voyage
        dataManager.PlayerData.TravelDestinationId = destinationLocationId;
        dataManager.PlayerData.TravelStartSteps = currentSteps;
        dataManager.PlayerData.TravelRequiredSteps = stepCost;

        // Sauvegarder immediatement le debut du voyage
        dataManager.SaveGame();
        saveService.ResetSaveTracking(currentSteps);

        Logger.LogInfo($"MapManager: Started travel from {manager.CurrentLocation.LocationID} to {destinationLocationId} ({stepCost} steps)", Logger.LogCategory.MapLog);

        // Declencher l'evenement
        eventService.TriggerTravelStarted(destinationLocationId, manager.CurrentLocation, stepCost);
    }

    public void CheckTravelProgress()
    {
        var dataManager = DataManager.Instance;
        if (!dataManager.PlayerData.IsCurrentlyTraveling()) return;

        long currentTotalSteps = dataManager.PlayerData.TotalSteps;
        string destinationId = dataManager.PlayerData.TravelDestinationId;
        int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;
        long progressSteps = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);

        // Mettre a jour la sauvegarde si necessaire
        saveService.CheckAndSaveProgress(currentTotalSteps, progressSteps);

        // Log progress moins souvent
        if (progressSteps > 0 && (progressSteps % 25 == 0 || progressSteps == 1))
        {
            var destinationLocation = manager.LocationRegistry.GetLocationById(destinationId);
            Logger.LogInfo($"MapManager: Travel progress {progressSteps}/{requiredSteps} steps to {destinationId}", Logger.LogCategory.MapLog);
        }

        // Declencher l'evenement de progrès
        if ((int)progressSteps != lastProgressSteps) // Seulement si ça a change !
        {
            eventService.TriggerTravelProgress(destinationId, (int)progressSteps, requiredSteps);
            lastProgressSteps = (int)progressSteps;
        }

        // Verifier si le voyage est termine
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

        // Calculer les pas pris pour le voyage
        long startSteps = dataManager.PlayerData.TravelStartSteps;
        long currentSteps = dataManager.PlayerData.TotalSteps;
        int stepsTaken = (int)(currentSteps - startSteps);

        Logger.LogInfo($"MapManager: Travel completed! Arrived at {destinationLocation.DisplayName} after {stepsTaken} steps", Logger.LogCategory.MapLog);

        // Garder reference de l'ancienne location pour l'evenement
        var previousLocation = manager.CurrentLocation;

        // Mettre a jour la location du joueur
        dataManager.PlayerData.CurrentLocationId = destinationId;

        // Nettoyer les donnees de voyage
        dataManager.PlayerData.TravelDestinationId = null;
        dataManager.PlayerData.TravelStartSteps = 0;
        dataManager.PlayerData.TravelRequiredSteps = 0;

        // Mettre a jour la location actuelle dans le manager
        manager.SetCurrentLocation(destinationLocation);

        // Reset du tracking de sauvegarde
        saveService.ResetSaveTracking(-1);

        // Sauvegarde immediate et obligatoire a la fin du voyage
        dataManager.SaveGame();
        Logger.LogInfo($"MapManager: Travel state cleared and game saved", Logger.LogCategory.MapLog);

        // Declencher les evenements APRÈS que tout l'etat soit mis a jour et sauve
        eventService.TriggerTravelCompleted(destinationId, destinationLocation, stepsTaken);
        eventService.TriggerLocationChanged(previousLocation, destinationLocation);
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
            string reason = validationService.GetTravelBlockReason(locationId);
            Logger.LogInfo($"MapManager: Cannot travel to '{locationId}': {reason}", Logger.LogCategory.MapLog);
        }
    }

    public void ClearTravelState()
    {
        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData == null) return;

        Logger.LogInfo("MapManager: Clearing travel state", Logger.LogCategory.MapLog);

        dataManager.PlayerData.TravelDestinationId = null;
        dataManager.PlayerData.TravelStartSteps = 0;
        dataManager.PlayerData.TravelRequiredSteps = 0;

        saveService.ResetSaveTracking(-1);
        dataManager.SaveGame();
    }

    private void ValidateTravelState()
    {
        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData == null || !dataManager.PlayerData.IsCurrentlyTraveling())
            return;

        string destination = dataManager.PlayerData.TravelDestinationId;
        int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;
        long currentTotalSteps = dataManager.PlayerData.TotalSteps;
        long travelProgress = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);

        // Verifications de coherence
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

        // Si le voyage est deja termine selon les pas, le completer immediatement
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
// SERVICE: Validation
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

        if (!manager.LocationRegistry.CanTravelBetween(manager.CurrentLocation.LocationID, destinationLocationId))
        {
            Logger.LogInfo($"MapManager: Cannot travel - no connection between '{manager.CurrentLocation.LocationID}' and '{destinationLocationId}'", Logger.LogCategory.MapLog);
            return false;
        }

        if (ActivityManager.Instance.ShouldBlockTravel())
        {
            Logger.LogInfo($"MapManager: Cannot travel - activity in progress blocks travel", Logger.LogCategory.MapLog);
            return false;
        }

        return true;
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

        if (!manager.LocationRegistry.CanTravelBetween(manager.CurrentLocation.LocationID, destinationLocationId))
            return $"not connected to '{destinationLocation.DisplayName}'.";

        if (ActivityManager.Instance.ShouldBlockTravel())
            return "activity in progress blocks travel.";

        return "unknown reason";
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