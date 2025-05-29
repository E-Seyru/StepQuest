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

    [Header("Travel Save Settings")]
    [SerializeField] private float travelSaveInterval = 10f; // Sauvegarde toutes les 10 secondes pendant le voyage
    [SerializeField] private int minStepsProgressToSave = 5; // Sauvegarder seulement si au moins 5 pas de progrès

    // References
    private DataManager dataManager;
    private StepManager stepManager;

    // Current state
    public MapLocationDefinition CurrentLocation { get; private set; }

    // Travel save tracking
    private long lastSavedTotalSteps = -1; // Track last saved total steps instead of progress

    // NOUVELLE OPTIMISATION : Variables pour réduire la fréquence des sauvegardes
    private float timeSinceLastTravelSave = 0f;
    private const float TRAVEL_SAVE_INTERVAL_DURING_TRAVEL = 20f; // Sauvegarde toutes les 20 secondes pendant un voyage

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

            // NOUVEAU: Si on est en voyage, restaurer l'état au démarrage
            if (dataManager.PlayerData.IsCurrentlyTraveling())
            {
                long currentTotalSteps = dataManager.PlayerData.TotalSteps;
                long progressSteps = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);
                int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;
                string destinationId = dataManager.PlayerData.TravelDestinationId;

                Logger.LogInfo($"MapManager: Restored travel progress: {progressSteps}/{requiredSteps} steps to {destinationId}", Logger.LogCategory.MapLog);
                lastSavedTotalSteps = currentTotalSteps; // Initialiser le tracking avec les pas actuels
            }
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

            Logger.LogInfo($"MapManager: Found ongoing travel to {destination} (Progress: {travelProgress}/{requiredSteps}).", Logger.LogCategory.MapLog);

            // Vérifier que l'état de voyage est cohérent
            if (string.IsNullOrEmpty(destination) || !_locationRegistry.HasLocation(destination))
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
                Logger.LogInfo($"MapManager: Travel to {destination} was already completed ({travelProgress}/{requiredSteps}). Completing now.", Logger.LogCategory.MapLog);
                CompleteTravel();
                return;
            }

            // État de voyage valide, continuer normalement
            Logger.LogInfo($"MapManager: Valid ongoing travel state restored.", Logger.LogCategory.MapLog);
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

        // Reset tracking
        lastSavedTotalSteps = -1;

        dataManager.SaveGame();
    }

    public bool CanTravelTo(string destinationLocationId)
    {

        if (ActivityManager.Instance.ShouldBlockTravel())
            return false;

        // Check if we have a current location
        if (CurrentLocation == null)
        {
            return false;
        }

        // Check if already traveling
        if (dataManager.PlayerData.IsCurrentlyTraveling())
        {
            return false;
        }

        // Check if destination exists
        var destination = _locationRegistry.GetLocationById(destinationLocationId);
        if (destination == null)
        {
            return false;
        }

        // Check if we're already at that location
        if (CurrentLocation.LocationID == destinationLocationId)
        {
            return false;
        }

        // Check if locations are connected
        if (!_locationRegistry.CanTravelBetween(CurrentLocation.LocationID, destinationLocationId))
        {
            return false;
        }

        return true;
    }

    public void StartTravel(string destinationLocationId)
    {
        if (!CanTravelTo(destinationLocationId))
        {
            Logger.LogWarning($"MapManager: StartTravel called for '{destinationLocationId}', but CanTravelTo returned false. Current loc: '{CurrentLocation?.LocationID}', Traveling: {dataManager.PlayerData.IsCurrentlyTraveling()}", Logger.LogCategory.MapLog);
            return;
        }

        var destination = _locationRegistry.GetLocationById(destinationLocationId);
        int stepCost = _locationRegistry.GetTravelCost(CurrentLocation.LocationID, destinationLocationId);

        // Start the travel
        dataManager.PlayerData.TravelDestinationId = destinationLocationId;
        dataManager.PlayerData.TravelStartSteps = dataManager.PlayerData.TotalSteps; // Current step count
        dataManager.PlayerData.TravelRequiredSteps = stepCost;

        // NOUVEAU: Initialiser le tracking de sauvegarde
        lastSavedTotalSteps = dataManager.PlayerData.TotalSteps; // Sauvegarder les pas actuels comme référence
        timeSinceLastTravelSave = 0f; // Réinitialiser le timer

        // MODIFIÉ: Sauvegarde immédiate et obligatoire au démarrage du voyage
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

        // NOUVELLE OPTIMISATION: Logique de sauvegarde temporisée pendant le voyage
        timeSinceLastTravelSave += Time.deltaTime;

        if (lastSavedTotalSteps != currentTotalSteps) // Condition initiale : sauvegarder si les pas ont changé
        {
            if (timeSinceLastTravelSave >= TRAVEL_SAVE_INTERVAL_DURING_TRAVEL)
            {
                dataManager.SaveTravelProgress();
                lastSavedTotalSteps = currentTotalSteps; // Mettre à jour après la sauvegarde réussie
                timeSinceLastTravelSave = 0f; // Réinitialiser le timer

                Logger.LogInfo($"MapManager: Auto-saved travel progress (timed): {progressSteps}/{requiredSteps} steps to {destinationId}", Logger.LogCategory.MapLog);
            }
        }

        // Emit progress event
        OnTravelProgress?.Invoke(destinationId, (int)progressSteps, requiredSteps);

        // Log progress less often and with more infos
        if (progressSteps > 0 && (progressSteps % 25 == 0 || progressSteps == 1)) // Log at 1 step and then every 25
        {
            var destinationLocation = _locationRegistry.GetLocationById(destinationId);
            Logger.LogInfo($"MapManager: Travel progress: {progressSteps}/{requiredSteps} steps to {destinationLocation?.DisplayName ?? destinationId} ({destinationId})", Logger.LogCategory.MapLog);
        }

        // Check if travel is complete
        if (dataManager.PlayerData.IsTravelComplete(currentTotalSteps))
        {
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
        dataManager.PlayerData.TravelDestinationId = null;
        dataManager.PlayerData.TravelStartSteps = 0;
        dataManager.PlayerData.TravelRequiredSteps = 0;

        // NOUVEAU: Reset tracking
        lastSavedTotalSteps = -1;
        timeSinceLastTravelSave = 0f;

        // Update current location property in MapManager
        CurrentLocation = destinationLocation;

        // MODIFIÉ: Sauvegarde immédiate et obligatoire à la fin du voyage
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

    // NOUVEAU: Méthode publique pour forcer une sauvegarde du voyage (utile pour debug)
    public void ForceSaveTravelProgress()
    {
        if (dataManager.PlayerData.IsCurrentlyTraveling())
        {
            dataManager.SaveTravelProgress();
            lastSavedTotalSteps = dataManager.PlayerData.TotalSteps;

            long currentProgress = dataManager.PlayerData.GetTravelProgress(dataManager.PlayerData.TotalSteps);

            Logger.LogInfo($"MapManager: Force saved travel progress: {currentProgress}/{dataManager.PlayerData.TravelRequiredSteps}", Logger.LogCategory.MapLog);
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