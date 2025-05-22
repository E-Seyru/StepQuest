// Purpose: Handles player movement between locations on the world map based on steps.
// Filepath: Assets/Scripts/Gameplay/World/MapManager.cs
using System;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [Header("Registry")]
    [SerializeField] private LocationRegistry locationRegistry;

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
            Logger.LogWarning("MapManager: Multiple instances detected! Destroying duplicate.", LogCategory.MapLog);
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Get references
        dataManager = DataManager.Instance;
        stepManager = StepManager.Instance;

        if (locationRegistry == null)
        {
            Logger.LogError("MapManager: LocationRegistry not assigned! Please assign it in the inspector.", LogCategory.MapLog);
            return;
        }

        if (dataManager == null)
        {
            Logger.LogError("MapManager: DataManager not found!", LogCategory.MapLog);
            return;
        }

        // Load current location
        LoadCurrentLocation();

        // NOUVEAU: Vérifier et nettoyer l'état de voyage au démarrage
        ValidateTravelState();

        Logger.LogInfo($"MapManager: Initialized. Current location: {CurrentLocation?.DisplayName ?? "Unknown"}", LogCategory.MapLog);
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
            Logger.LogWarning("MapManager: No current location set in PlayerData. Defaulting to first location.", LogCategory.MapLog);
            // Set to first available location or default
            currentLocationId = "Foret_01"; // Your default starting location
            dataManager.PlayerData.CurrentLocationId = currentLocationId;
            dataManager.SaveGame();
        }

        CurrentLocation = locationRegistry.GetLocationById(currentLocationId);

        if (CurrentLocation == null)
        {
            Logger.LogError($"MapManager: Current location '{currentLocationId}' not found in registry!", LogCategory.MapLog);
        }
        else
        {
            Logger.LogInfo($"MapManager: Loaded current location: {CurrentLocation.DisplayName} ({CurrentLocation.LocationID})", LogCategory.MapLog);
        }
    }

    // NOUVEAU: Valider et nettoyer l'état de voyage
    private void ValidateTravelState()
    {
        if (dataManager.PlayerData.IsCurrentlyTraveling())
        {
            string destination = dataManager.PlayerData.TravelDestinationId;
            long currentSteps = dataManager.PlayerData.TotalSteps;

            Logger.LogWarning($"MapManager: Found ongoing travel to {destination}. This might be leftover data!", LogCategory.MapLog);
            Logger.LogInfo($"MapManager: Travel progress: {dataManager.PlayerData.GetTravelProgress(currentSteps)}/{dataManager.PlayerData.TravelRequiredSteps}", LogCategory.MapLog);

            // CORRECTION: Auto-clear any leftover travel state for clean start
            Logger.LogWarning("MapManager: Clearing leftover travel state for clean game start...", LogCategory.MapLog);
            ClearTravelState();
        }
        else
        {
            Logger.LogInfo("MapManager: No ongoing travel found.", LogCategory.MapLog);
        }
    }

    // NOUVEAU: Clear travel state (méthode utilitaire)
    public void ClearTravelState()
    {
        Logger.LogInfo("MapManager: Clearing travel state...", LogCategory.MapLog);

        dataManager.PlayerData.TravelDestinationId = null;
        dataManager.PlayerData.TravelStartSteps = 0;
        dataManager.PlayerData.TravelRequiredSteps = 0;

        dataManager.SaveGame();

        Logger.LogInfo("MapManager: Travel state cleared.", LogCategory.MapLog);
    }

    public bool CanTravelTo(string destinationLocationId)
    {
        // Check if we have a current location
        if (CurrentLocation == null)
        {
            Logger.LogWarning("MapManager: Cannot travel - no current location set.", LogCategory.MapLog);
            return false;
        }

        // Check if already traveling
        if (dataManager.PlayerData.IsCurrentlyTraveling())
        {
            Logger.LogInfo($"MapManager: Cannot start new travel - already traveling to {dataManager.PlayerData.TravelDestinationId}.", LogCategory.MapLog);
            return false;
        }

        // Check if destination exists
        var destination = locationRegistry.GetLocationById(destinationLocationId);
        if (destination == null)
        {
            Logger.LogWarning($"MapManager: Cannot travel to '{destinationLocationId}' - location not found.", LogCategory.MapLog);
            return false;
        }

        // Check if we're already at that location
        if (CurrentLocation.LocationID == destinationLocationId)
        {
            Logger.LogInfo($"MapManager: Already at {destination.DisplayName}.", LogCategory.MapLog);
            return false;
        }

        // Check if locations are connected
        if (!locationRegistry.CanTravelBetween(CurrentLocation.LocationID, destinationLocationId))
        {
            Logger.LogInfo($"MapManager: Cannot travel from {CurrentLocation.DisplayName} to {destination.DisplayName} - not connected.", LogCategory.MapLog);
            return false;
        }

        return true;
    }

    public void StartTravel(string destinationLocationId)
    {
        if (!CanTravelTo(destinationLocationId))
        {
            return;
        }

        var destination = locationRegistry.GetLocationById(destinationLocationId);
        int stepCost = locationRegistry.GetTravelCost(CurrentLocation.LocationID, destinationLocationId);

        // CORRECTION: Debug le coût pour voir d'où vient 500
        Logger.LogInfo($"MapManager: DEBUG - Travel cost from {CurrentLocation.LocationID} to {destinationLocationId}: {stepCost}", LogCategory.MapLog);

        // Start the travel
        dataManager.PlayerData.TravelDestinationId = destinationLocationId;
        dataManager.PlayerData.TravelStartSteps = dataManager.PlayerData.TotalSteps; // Current step count
        dataManager.PlayerData.TravelRequiredSteps = stepCost;

        // Save immediately
        dataManager.SaveGame();

        Logger.LogInfo($"MapManager: Travel started from {CurrentLocation.DisplayName} to {destination.DisplayName}", LogCategory.MapLog);
        Logger.LogInfo($"MapManager: Required steps: {stepCost}, Starting from step: {dataManager.PlayerData.TotalSteps}", LogCategory.MapLog);

        OnTravelStarted?.Invoke(destinationLocationId);
    }

    private void CheckTravelProgress()
    {
        if (!dataManager.PlayerData.IsCurrentlyTraveling())
            return;

        long currentSteps = dataManager.PlayerData.TotalSteps;
        long progressSteps = dataManager.PlayerData.GetTravelProgress(currentSteps);
        int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;
        string destination = dataManager.PlayerData.TravelDestinationId;

        // Emit progress event
        OnTravelProgress?.Invoke(destination, (int)progressSteps, requiredSteps);

        // CORRECTION: Log progress moins souvent et avec plus d'infos
        if (progressSteps % 25 == 0 && progressSteps > 0) // Tous les 25 steps au lieu de 10
        {
            var destinationLocation = locationRegistry.GetLocationById(destination);
            Logger.LogInfo($"MapManager: Travel progress: {progressSteps}/{requiredSteps} steps to {destinationLocation?.DisplayName} ({destination})", LogCategory.MapLog);
        }

        // Check if travel is complete
        if (dataManager.PlayerData.IsTravelComplete(currentSteps))
        {
            Logger.LogInfo($"MapManager: Travel completed! Final progress: {progressSteps}/{requiredSteps}", LogCategory.MapLog);
            CompleteTravel();
        }
    }

    private void CompleteTravel()
    {
        string destinationId = dataManager.PlayerData.TravelDestinationId;
        var destination = locationRegistry.GetLocationById(destinationId);

        // Update player location
        dataManager.PlayerData.CurrentLocationId = destinationId;

        // Clear travel data - AMÉLIORATION: S'assurer que tout est bien null
        dataManager.PlayerData.TravelDestinationId = null;
        dataManager.PlayerData.TravelStartSteps = 0;
        dataManager.PlayerData.TravelRequiredSteps = 0;

        // Update current location
        CurrentLocation = destination;

        // Save game - IMPORTANT: Force save pour s'assurer que l'état est persisté
        dataManager.SaveGame();

        Logger.LogInfo($"MapManager: Travel completed! Arrived at {destination?.DisplayName} ({destinationId})", LogCategory.MapLog);
        Logger.LogInfo($"MapManager: Travel state cleared. IsCurrentlyTraveling: {dataManager.PlayerData.IsCurrentlyTraveling()}", LogCategory.MapLog);

        // Trigger events
        OnTravelCompleted?.Invoke(destinationId);
        OnLocationChanged?.Invoke(CurrentLocation);
    }

    // Public method to get travel info for UI
    public TravelInfo GetTravelInfo(string destinationLocationId)
    {
        if (CurrentLocation == null || !locationRegistry.HasLocation(destinationLocationId))
            return null;

        var destination = locationRegistry.GetLocationById(destinationLocationId);
        int stepCost = locationRegistry.GetTravelCost(CurrentLocation.LocationID, destinationLocationId);
        bool canTravel = CanTravelTo(destinationLocationId);

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
        Logger.LogInfo($"MapManager: POI clicked: {locationId}", LogCategory.MapLog);

        // NOUVEAU: Debug info pour diagnostiquer
        Logger.LogInfo($"MapManager: Current travel state - IsCurrentlyTraveling: {dataManager.PlayerData.IsCurrentlyTraveling()}, TravelDestinationId: '{dataManager.PlayerData.TravelDestinationId}'", LogCategory.MapLog);

        // For now, just try to start travel (later we'll show a panel)
        if (CanTravelTo(locationId))
        {
            var travelInfo = GetTravelInfo(locationId);
            Logger.LogInfo($"MapManager: Travel available to {travelInfo.To.DisplayName} for {travelInfo.StepCost} steps", LogCategory.MapLog);

            // For testing, auto-start travel
            StartTravel(locationId);
        }
        else
        {
            Logger.LogInfo($"MapManager: Cannot travel to {locationId}", LogCategory.MapLog);
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