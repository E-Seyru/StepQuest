// Purpose: Handles player movement between locations on the world map based on steps.
// Filepath: Assets/Scripts/Gameplay/World/MapManager.cs
using System;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }


    [Header("Registry")]
    [SerializeField] private LocationRegistry _locationRegistry; // Renamed to underscore to differentiate from public property
    public LocationRegistry LocationRegistry => _locationRegistry; // Public getter

    // References
    private DataManager dataManager;
    private StepManager stepManager;

    // Current state
    public MapLocationDefinition CurrentLocation { get; private set; }

    // Events
    public event Action<MapLocationDefinition> OnLocationChanged;
    public event Action<string, int, int> OnTravelProgress; // destination, current steps, required steps
    public event Action<string> OnTravelStarted;
    public event Action<string> OnTravelCompleted;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Logger.LogWarning("MapManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.MapLog);
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Get references
        dataManager = DataManager.Instance;
        stepManager = StepManager.Instance;

        if (_locationRegistry == null)
        {
            Logger.LogError("MapManager: LocationRegistry not assigned! Please assign it in the inspector.", Logger.LogCategory.MapLog);
            return;
        }

        if (dataManager == null)
        {
            Logger.LogError("MapManager: DataManager not found!", Logger.LogCategory.MapLog);
            return;
        }

        // Load current location
        LoadCurrentLocation();

        // NOUVEAU: Vérifier et nettoyer l'état de voyage au démarrage
        ValidateTravelState();

        Logger.LogInfo($"MapManager: Initialized. Current location: {CurrentLocation?.DisplayName ?? "Unknown"}", Logger.LogCategory.MapLog);
    }

    void Update()
    {
        // Check travel progress if currently traveling
        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            CheckTravelProgress();
        }
    }

    private void LoadCurrentLocation()
    {
        string currentLocationId = dataManager.PlayerData.CurrentLocationId;

        if (string.IsNullOrEmpty(currentLocationId))
        {
            Logger.LogWarning("MapManager: No current location set in PlayerData. Defaulting to first location.", Logger.LogCategory.MapLog);
            // Set to first available location or default
            currentLocationId = "Foret_01"; // Your default starting location
            dataManager.PlayerData.CurrentLocationId = currentLocationId;
            dataManager.SaveGame();
        }

        CurrentLocation = _locationRegistry.GetLocationById(currentLocationId);

        if (CurrentLocation == null)
        {
            Logger.LogError($"MapManager: Current location '{currentLocationId}' not found in registry!", Logger.LogCategory.MapLog);
        }
        else
        {
            Logger.LogInfo($"MapManager: Loaded current location: {CurrentLocation.DisplayName} ({CurrentLocation.LocationID})", Logger.LogCategory.MapLog);
            // Note: OnLocationChanged is typically invoked when location *changes*, not on initial load.
            // If needed for initial setup by other systems, invoke it here or have them query CurrentLocation.
        }
    }

    // NOUVEAU: Valider et nettoyer l'état de voyage
    private void ValidateTravelState()
    {
        if (dataManager.PlayerData.IsCurrentlyTraveling())
        {
            string destination = dataManager.PlayerData.TravelDestinationId;
            long currentTotalSteps = dataManager.PlayerData.TotalSteps;
            long travelProgress = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);
            int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;

            Logger.LogWarning($"MapManager: Found ongoing travel to {destination} (Progress: {travelProgress}/{requiredSteps}). This might be leftover data from a previous session.", Logger.LogCategory.MapLog);

            // CORRECTION: Auto-clear any leftover travel state for clean start
            Logger.LogInfo("MapManager: Attempting to clear leftover travel state...", Logger.LogCategory.MapLog);
            ClearTravelState(); // This will log the details of what was cleared
        }
        else
        {
            Logger.LogInfo("MapManager: No ongoing travel found at startup.", Logger.LogCategory.MapLog);
        }
    }

    // NOUVEAU: Clear travel state (méthode utilitaire)
    public void ClearTravelState()
    {
        if (dataManager?.PlayerData == null) return;

        string oldDest = dataManager.PlayerData.TravelDestinationId;
        long oldStartSteps = dataManager.PlayerData.TravelStartSteps;
        int oldReqSteps = dataManager.PlayerData.TravelRequiredSteps;

        if (!string.IsNullOrEmpty(oldDest) || oldReqSteps > 0) // Only log if there was something to clear
        {
            Logger.LogInfo($"MapManager: Clearing travel state. Was: Dest='{oldDest ?? "N/A"}', StartSteps='{oldStartSteps}', ReqSteps='{oldReqSteps}'.", Logger.LogCategory.MapLog);
        }

        dataManager.PlayerData.TravelDestinationId = null;
        dataManager.PlayerData.TravelStartSteps = 0;
        dataManager.PlayerData.TravelRequiredSteps = 0;

        dataManager.SaveGame();

        // Logger.LogInfo("MapManager: Travel state cleared from PlayerData.", Logger.LogCategory.MapLog); // Consolidated above
    }

    public bool CanTravelTo(string destinationLocationId)
    {
        // Check if we have a current location
        if (CurrentLocation == null)
        {
            // This log is fine, it's a specific failure case
            // Logger.LogWarning("MapManager: Cannot travel - no current location set.", Logger.LogCategory.MapLog);
            return false;
        }

        // Check if already traveling
        if (dataManager.PlayerData.IsCurrentlyTraveling())
        {
            // This log is fine
            // Logger.LogInfo($"MapManager: Cannot start new travel - already traveling to {dataManager.PlayerData.TravelDestinationId}.", Logger.LogCategory.MapLog);
            return false;
        }

        // Check if destination exists
        var destination = _locationRegistry.GetLocationById(destinationLocationId);
        if (destination == null)
        {
            // This log is fine
            // Logger.LogWarning($"MapManager: Cannot travel to '{destinationLocationId}' - location not found.", Logger.LogCategory.MapLog);
            return false;
        }

        // Check if we're already at that location
        if (CurrentLocation.LocationID == destinationLocationId)
        {
            // This log is fine
            // Logger.LogInfo($"MapManager: Already at {destination.DisplayName}.", Logger.LogCategory.MapLog);
            return false;
        }

        // Check if locations are connected
        if (!_locationRegistry.CanTravelBetween(CurrentLocation.LocationID, destinationLocationId))
        {
            // This log is fine
            // Logger.LogInfo($"MapManager: Cannot travel from {CurrentLocation.DisplayName} to {destination.DisplayName} - not connected.", Logger.LogCategory.MapLog);
            return false;
        }

        return true;
    }

    public void StartTravel(string destinationLocationId)
    {
        if (!CanTravelTo(destinationLocationId))
        {
            // Log why it failed inside CanTravelTo or OnPOIClicked
            Logger.LogWarning($"MapManager: StartTravel called for '{destinationLocationId}', but CanTravelTo returned false. Current loc: '{CurrentLocation?.LocationID}', Traveling: {dataManager.PlayerData.IsCurrentlyTraveling()}", Logger.LogCategory.MapLog);
            return;
        }

        var destination = _locationRegistry.GetLocationById(destinationLocationId);
        int stepCost = _locationRegistry.GetTravelCost(CurrentLocation.LocationID, destinationLocationId);

        // CORRECTION: Debug le coût pour voir d'où vient 500
        // This log is useful for debugging specific travel initiations.
        // Logger.LogInfo($"MapManager: DEBUG - Travel cost from {CurrentLocation.LocationID} to {destinationLocationId}: {stepCost}", Logger.LogCategory.MapLog);

        // Start the travel
        dataManager.PlayerData.TravelDestinationId = destinationLocationId;
        dataManager.PlayerData.TravelStartSteps = dataManager.PlayerData.TotalSteps; // Current step count
        dataManager.PlayerData.TravelRequiredSteps = stepCost;

        // Save immediately
        dataManager.SaveGame();

        Logger.LogInfo($"MapManager: Travel started from {CurrentLocation.DisplayName} to {destination.DisplayName}. Required steps: {stepCost}, Starting from step: {dataManager.PlayerData.TravelStartSteps}", Logger.LogCategory.MapLog);

        OnTravelStarted?.Invoke(destinationLocationId);
    }

    private void CheckTravelProgress()
    {
        if (dataManager?.PlayerData == null || !dataManager.PlayerData.IsCurrentlyTraveling())
            return;

        long currentTotalSteps = dataManager.PlayerData.TotalSteps;
        long progressSteps = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);
        int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;
        string destinationId = dataManager.PlayerData.TravelDestinationId;

        // Emit progress event
        OnTravelProgress?.Invoke(destinationId, (int)progressSteps, requiredSteps);

        // CORRECTION: Log progress less often and with more infos
        // This is a deliberate periodic log for player feedback/debugging, not necessarily "spam" unless undesired.
        // Keep it for now.
        if (progressSteps > 0 && (progressSteps % 25 == 0 || progressSteps == 1)) // Log at 1 step and then every 25
        {
            var destinationLocation = _locationRegistry.GetLocationById(destinationId);
            Logger.LogInfo($"MapManager: Travel progress: {progressSteps}/{requiredSteps} steps to {destinationLocation?.DisplayName ?? destinationId} ({destinationId})", Logger.LogCategory.MapLog);
        }

        // Check if travel is complete
        if (dataManager.PlayerData.IsTravelComplete(currentTotalSteps))
        {
            // This log is fine.
            // Logger.LogInfo($"MapManager: Travel completed! Final progress: {progressSteps}/{requiredSteps}", Logger.LogCategory.MapLog);
            CompleteTravel();
        }
    }

    private void CompleteTravel()
    {
        if (dataManager?.PlayerData == null) return;

        string destinationId = dataManager.PlayerData.TravelDestinationId;
        var destinationLocation = _locationRegistry.GetLocationById(destinationId);

        if (destinationLocation == null)
        {
            Logger.LogError($"MapManager: CompleteTravel - Destination ID '{destinationId}' not found in registry. Cancelling travel.", Logger.LogCategory.MapLog);
            ClearTravelState(); // Clear bad travel state
            return;
        }

        long finalProgressSteps = dataManager.PlayerData.GetTravelProgress(dataManager.PlayerData.TotalSteps);

        Logger.LogInfo($"MapManager: Completing travel to {destinationLocation.DisplayName} ({destinationId}). Final progress: {finalProgressSteps}/{dataManager.PlayerData.TravelRequiredSteps}", Logger.LogCategory.MapLog);

        // Update player location
        dataManager.PlayerData.CurrentLocationId = destinationId;

        // Clear travel data - AMÉLIORATION: S'assurer que tout est bien null
        // This is done before updating CurrentLocation property and invoking events
        dataManager.PlayerData.TravelDestinationId = null;
        dataManager.PlayerData.TravelStartSteps = 0;
        dataManager.PlayerData.TravelRequiredSteps = 0;

        // Update current location property in MapManager
        CurrentLocation = destinationLocation;

        // Save game - IMPORTANT: Force save to ensure the new location and cleared travel state are persisted
        dataManager.SaveGame();

        Logger.LogInfo($"MapManager: Arrived at {CurrentLocation.DisplayName}. Travel state cleared. IsCurrentlyTraveling: {dataManager.PlayerData.IsCurrentlyTraveling()}", Logger.LogCategory.MapLog);

        // Trigger events AFTER all state is updated and saved
        OnTravelCompleted?.Invoke(destinationId);
        OnLocationChanged?.Invoke(CurrentLocation);
    }

    // Public method to get travel info for UI
    public TravelInfo GetTravelInfo(string destinationLocationId)
    {
        if (CurrentLocation == null || !_locationRegistry.HasLocation(destinationLocationId))
            return null;

        var destination = _locationRegistry.GetLocationById(destinationLocationId);
        if (destination == null) return null;

        int stepCost = _locationRegistry.GetTravelCost(CurrentLocation.LocationID, destinationLocationId);
        bool canTravel = CanTravelTo(destinationLocationId); // This already checks if currently traveling, etc.

        return new TravelInfo
        {
            From = CurrentLocation,
            To = destination,
            StepCost = stepCost,
            CanTravel = canTravel
        };
    }

    // Method called by POI GameObjects when clicked
    public void OnPOIClicked(string locationId)
    {
        // Logger.LogInfo($"MapManager: POI clicked: {locationId}", Logger.LogCategory.MapLog); // POI already logs this.

        // NOUVEAU: Debug info pour diagnostiquer
        // This log is useful for diagnosing click issues.
        // Logger.LogInfo($"MapManager: Current travel state on POI click - IsCurrentlyTraveling: {dataManager.PlayerData.IsCurrentlyTraveling()}, TravelDestinationId: '{dataManager.PlayerData.TravelDestinationId}'", Logger.LogCategory.MapLog);

        if (CanTravelTo(locationId))
        {
            var travelInfo = GetTravelInfo(locationId); // Recalculate, as CanTravelTo could be true now
            if (travelInfo != null && travelInfo.To != null)
            {
                Logger.LogInfo($"MapManager: Travel available to {travelInfo.To.DisplayName} ({locationId}) for {travelInfo.StepCost} steps. Initiating travel.", Logger.LogCategory.MapLog);
                StartTravel(locationId);
            }
            else
            {
                Logger.LogWarning($"MapManager: CanTravelTo '{locationId}' was true, but GetTravelInfo failed. This should not happen.", Logger.LogCategory.MapLog);
            }
        }
        else
        {
            var destinationLocation = _locationRegistry.GetLocationById(locationId);
            string reason = "unknown reason";
            if (CurrentLocation == null) reason = "no current player location.";
            else if (dataManager.PlayerData.IsCurrentlyTraveling()) reason = $"already traveling to {dataManager.PlayerData.TravelDestinationId}.";
            else if (destinationLocation == null) reason = $"destination '{locationId}' not found.";
            else if (CurrentLocation.LocationID == locationId) reason = $"already at '{destinationLocation.DisplayName}'.";
            else if (!_locationRegistry.CanTravelBetween(CurrentLocation.LocationID, locationId)) reason = $"not connected to '{destinationLocation.DisplayName}'.";

            Logger.LogInfo($"MapManager: Cannot travel to '{destinationLocation?.DisplayName ?? locationId}' ({locationId}). Reason: {reason}", Logger.LogCategory.MapLog);
        }
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