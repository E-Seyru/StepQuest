// Purpose: Manages world map, locations, and travel logic with enhanced pathfinding
// Filepath: Assets/Scripts/Gameplay/World/MapManager.cs
using MapEvents;
using UnityEngine;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [Header("Location Management")]
    [SerializeField] private LocationRegistry locationRegistry;

    [Header("Pathfinding Settings")]
    [SerializeField] private bool enablePathfinding = true;
    [SerializeField] private bool enablePathfindingDebug = false;

    [Header("Travel Progress")]
    [SerializeField] private float travelSaveInterval = 30f;
    [SerializeField] private int minStepsProgressToSave = 50;

    // Services
    private MapLocationService locationService;
    private MapValidationService validationService;
    private MapTravelService travelService;
    private MapEventService eventService;
    private MapSaveService saveService;
    private MapPathfindingService pathfindingService;

    // Current state
    public MapLocationDefinition CurrentLocation { get; private set; }
    public LocationRegistry LocationRegistry => locationRegistry;
    public MapPathfindingService PathfindingService => pathfindingService;
    public MapSaveService SaveService => saveService;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeServices();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        locationService.Initialize();
        travelService.Initialize();
    }

    void Update()
    {
        saveService.Update();
        travelService.Update();
    }

    private void InitializeServices()
    {
        eventService = new MapEventService();
        saveService = new MapSaveService(this, travelSaveInterval, minStepsProgressToSave);
        validationService = new MapValidationService(this);
        locationService = new MapLocationService(this, eventService);

        if (enablePathfinding)
        {
            pathfindingService = new MapPathfindingService(locationRegistry);
        }


        travelService = new MapTravelService(this, locationService, validationService, eventService, saveService, pathfindingService);
    }

    // === PUBLIC API ===
    public bool CanTravelTo(string destinationLocationId)
    {
        return validationService?.CanTravelTo(destinationLocationId) ?? false;
    }

    /// <summary>
    /// ENHANCED: Demarre un voyage intelligent (direct ou avec pathfinding)
    /// </summary>
    public void StartTravel(string destinationLocationId)
    {
        travelService?.StartTravel(destinationLocationId);
    }

    /// <summary>
    /// ENHANCED: Retourne des informations detaillees sur le voyage (incluant pathfinding)
    /// </summary>
    public TravelInfo GetTravelInfo(string destinationLocationId)
    {
        return travelService?.GetTravelInfo(destinationLocationId);
    }

    /// <summary>
    /// NOUVEAU: Retourne les details du chemin calcule par pathfinding
    /// </summary>
    public MapPathfindingService.PathResult GetPathDetails(string destinationLocationId)
    {
        if (CurrentLocation == null || !enablePathfinding || pathfindingService == null)
            return null;

        return pathfindingService.FindPath(CurrentLocation.LocationID, destinationLocationId);
    }

    /// <summary>
    /// NOUVEAU: Debug d'un chemin specifique
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
    /// ENHANCED: Verifie maintenant les connexions directes ET le pathfinding
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

        // ⭐ NOUVEAU : Verifier si le joueur est deja en train de voyager
        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            Logger.LogInfo($"MapManager: Cannot travel - already traveling to {dataManager.PlayerData.TravelDestinationId}", Logger.LogCategory.MapLog);
            return false;
        }

        if (ActivityManager.Instance.ShouldBlockTravel())
        {
            Logger.LogInfo($"MapManager: Cannot travel - activity in progress blocks travel", Logger.LogCategory.MapLog);
            return false;
        }

        // NOUVEAU : Verifier d'abord les connexions directes
        if (manager.LocationRegistry.CanTravelBetween(manager.CurrentLocation.LocationID, destinationLocationId))
        {
            Logger.LogInfo($"MapManager: Direct connection available to '{destinationLocationId}'", Logger.LogCategory.MapLog);
            return true;
        }

        // NOUVEAU : Verifier le pathfinding si active
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

        // ENHANCED : Message plus detaille
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

    // NOUVEAU : etat pour voyage multi-segments
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
    /// ENHANCED: Demarre un voyage intelligent
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

        // NOUVEAU : Determiner le type de voyage (direct ou pathfinding)
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
    /// MODIFIe : Demarre un voyage direct avec sauvegarde de l'origine
    /// </summary>
    private void StartDirectTravel(string destinationLocationId, MapLocationDefinition destination, DataManager dataManager)
    {
        int stepCost = manager.LocationRegistry.GetTravelCost(manager.CurrentLocation.LocationID, destinationLocationId);
        long currentSteps = dataManager.PlayerData.TotalSteps;
        string originLocationId = manager.CurrentLocation.LocationID;

        // Configurer les donnees de voyage (thread-safe avec validation)
        if (!dataManager.StartTravel(destinationLocationId, stepCost, currentSteps, null, originLocationId))
        {
            Logger.LogWarning($"MapManager: StartDirectTravel blocked - player has active activity!", Logger.LogCategory.MapLog);
            return;
        }

        // Reinitialiser l'etat multi-segment
        ResetMultiSegmentState();

        // ⭐ NOUVEAU : Clear la location actuelle - on n'est plus nulle part pendant le voyage !
        manager.SetCurrentLocation(null);

        // Sauvegarder immediatement le debut du voyage
        dataManager.SaveGame();
        saveService.ResetSaveTracking(currentSteps);

        Logger.LogInfo($"MapManager: Started DIRECT travel from {originLocationId} to {destinationLocationId} ({stepCost} steps). CurrentLocation cleared.", Logger.LogCategory.MapLog);

        // Declencher l'evenement
        var startLocation = manager.LocationRegistry.GetLocationById(originLocationId);
        eventService.TriggerTravelStarted(destinationLocationId, startLocation, stepCost);
    }

    /// <summary>
    /// NOUVEAU : Demarre un voyage avec pathfinding
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

        // Validation: Cannot travel while doing activity
        if (dataManager.PlayerData.HasActiveActivity())
        {
            Logger.LogWarning($"MapManager: StartPathfindingTravel blocked - player has active activity!", Logger.LogCategory.MapLog);
            return;
        }

        // Demarrer le voyage multi-segment
        isMultiSegmentTravel = true;
        currentSegmentIndex = 0;

        // ⭐ NOUVEAU : Persister le flag multi-segment pour restauration apres crash
        dataManager.PlayerData.IsMultiSegmentTravel = true;

        // ⭐ NOUVEAU : Sauvegarder la location de depart avant de la clear
        var startLocation = manager.CurrentLocation;

        // Demarrer le premier segment (qui configure les donnees de voyage)
        var firstSegment = currentPathDetails.Segments[0];
        StartSegmentTravel(firstSegment, dataManager, destinationLocationId, startLocation.LocationID);

        Logger.LogInfo($"MapManager: Started PATHFINDING travel from {startLocation.LocationID} to {destinationLocationId} " +
                      $"({currentPathDetails.TotalCost} total steps, {currentPathDetails.Segments.Count} segments). Multi-segment data saved.", Logger.LogCategory.MapLog);

        // Declencher l'evenement avec le coût total
        eventService.TriggerTravelStarted(destinationLocationId, startLocation, currentPathDetails.TotalCost);
    }

    /// <summary>
    /// Demarre un segment individuel du voyage (multi-segment)
    /// </summary>
    private void StartSegmentTravel(MapPathfindingService.PathSegment segment,
                                    DataManager dataManager,
                                    string finalDestinationId = null,
                                    string originalOriginId = null)
    {
        long currentSteps = dataManager.PlayerData.TotalSteps;

        // Configurer les donnees de voyage pour ce segment (thread-safe)
        dataManager.StartTravel(
            segment.ToLocationId,
            segment.StepCost,
            currentSteps,
            finalDestinationId,  // null pour segments suivants, garde la valeur existante
            segment.FromLocationId
        );

        var originLocationObj = manager.LocationRegistry.GetLocationById(segment.FromLocationId);
        eventService.TriggerTravelStarted(segment.ToLocationId, originLocationObj, segment.StepCost);

        // On n'est « nulle part » pendant le deplacement
        if (manager.CurrentLocation != null)
            manager.SetCurrentLocation(null);

        // Sauvegarder et reinitialiser le tracking
        dataManager.SaveGame();
        saveService.ResetSaveTracking(currentSteps);

        Logger.LogInfo(
            $"MapManager: Started segment {currentSegmentIndex + 1}/{currentPathDetails.Segments.Count} " +
            $"from {segment.FromLocationId} to {segment.ToLocationId} ({segment.StepCost} steps).",
            Logger.LogCategory.MapLog);

        // Notifier l’UI
        var destinationLocation = manager.LocationRegistry.GetLocationById(segment.ToLocationId);
        if (destinationLocation != null)
        {
            eventService.TriggerTravelStarted(segment.ToLocationId, null, segment.StepCost);
            Logger.LogInfo($"MapManager: TravelStartedEvent triggered for segment to {destinationLocation.DisplayName}",
                           Logger.LogCategory.MapLog);
        }
        else
        {
            Logger.LogWarning($"MapManager: Could not find destination location {segment.ToLocationId} for event trigger",
                              Logger.LogCategory.MapLog);
        }
    }

    private void CheckTravelProgress()
    {
        var dataManager = DataManager.Instance;
        if (!dataManager.PlayerData.IsCurrentlyTraveling()) return;

        long currentTotalSteps = dataManager.PlayerData.TotalSteps;
        long progressSteps = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);
        int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;

        // Verifier si le voyage est termine
        if (progressSteps >= requiredSteps)
        {
            // NOUVEAU : Si c'est un voyage multi-segment, completer le segment
            if (isMultiSegmentTravel && currentSegmentIndex < currentPathDetails.Segments.Count - 1)
            {
                CompleteCurrentSegment(dataManager);
            }
            else
            {
                // Voyage termine
                CompleteTravel();
            }
        }
        else
        {
            // Mise a jour de la progression
            if ((int)progressSteps != lastProgressSteps)
            {
                eventService.TriggerTravelProgress(dataManager.PlayerData.TravelDestinationId, (int)progressSteps, requiredSteps);
                lastProgressSteps = (int)progressSteps;
            }
        }
    }

    /// <summary>
    /// Termine le segment courant, reporte les pas excedentaires sur le suivant
    /// et enchaîne jusqu’a ce qu’il n’y ait plus de segments ou de pas en trop.
    /// </summary>
    private void CompleteCurrentSegment(DataManager dataManager)
    {
        // ----- Segment courant ----------------------------------------------------
        var currentSegment = currentPathDetails.Segments[currentSegmentIndex];
        var segmentDest = manager.LocationRegistry.GetLocationById(currentSegment.ToLocationId);

        Logger.LogInfo(
            $"MapManager: Completed segment {currentSegmentIndex + 1}/{currentPathDetails.Segments.Count} " +
            $"- arrived at {segmentDest?.DisplayName}",
            Logger.LogCategory.MapLog);

        // 1) Calcul du surplus de pas pour CE segment
        long currentSteps = dataManager.PlayerData.TotalSteps;
        long segmentStart = dataManager.PlayerData.TravelStartSteps;
        int segmentCost = currentSegment.StepCost;
        long leftoverSteps = currentSteps - segmentStart - segmentCost;
        if (leftoverSteps < 0) leftoverSteps = 0;

        // 2) Mise a jour de la position reelle du joueur
        manager.SetCurrentLocation(segmentDest);
        dataManager.PlayerData.CurrentLocationId = currentSegment.ToLocationId;

        // 3) Nettoyage de l’etat du segment termine
        dataManager.PlayerData.TravelDestinationId = null;
        dataManager.PlayerData.TravelStartSteps = 0;
        dataManager.PlayerData.TravelRequiredSteps = 0;

        // ----- Segment suivant ----------------------------------------------------
        currentSegmentIndex++;

        if (currentSegmentIndex < currentPathDetails.Segments.Count)
        {
            var nextSegment = currentPathDetails.Segments[currentSegmentIndex];

            // Demarre le nouveau segment (TravelStartSteps = currentSteps)
            StartSegmentTravel(nextSegment, dataManager);

            // 3bis) Report immediat du surplus de pas
            if (leftoverSteps > 0)
            {
                // On “recule” le point de depart du segment suivant
                dataManager.PlayerData.TravelStartSteps -= leftoverSteps;

                Logger.LogInfo(
                    $"MapManager: Carried over {leftoverSteps} surplus steps to next segment.",
                    Logger.LogCategory.MapLog);

                // Si ce surplus suffit pour finir encore un segment, on enchaîne
                CheckTravelProgress();
            }
        }
        else
        {
            // Aucun segment restant : voyage termine
            CompleteTravel();
        }

        // ----- Sauvegarde ---------------------------------------------------------
        dataManager.SaveGame();
    }
    private void ApplyLeftoverSteps(long leftoverSteps, DataManager dataManager)
    {
        if (leftoverSteps <= 0) return;

        // On « remonte » TravelStartSteps pour que le surplus soit compte
        dataManager.PlayerData.TravelStartSteps -= leftoverSteps;

        Logger.LogInfo($"MapManager: Carried over {leftoverSteps} surplus steps to next segment.",
                       Logger.LogCategory.MapLog);
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

        // Garder reference de l'ancienne location pour l'evenement
        var previousLocation = manager.CurrentLocation;

        // Mettre a jour la location du joueur
        dataManager.PlayerData.CurrentLocationId = destinationId;

        // ⭐ NOUVEAU : Nettoyer TOUTES les donnees de voyage
        dataManager.PlayerData.TravelDestinationId = null;
        dataManager.PlayerData.TravelStartSteps = 0;
        dataManager.PlayerData.TravelRequiredSteps = 0;
        dataManager.PlayerData.TravelFinalDestinationId = null; // ⭐ NOUVEAU
        dataManager.PlayerData.TravelOriginLocationId = null; // ⭐ NOUVEAU

        // Mettre a jour la location actuelle dans le manager
        manager.SetCurrentLocation(destinationLocation);

        // NOUVEAU : Nettoyer l'etat multi-segment
        ResetMultiSegmentState();

        // Reset du tracking de sauvegarde
        saveService.ResetSaveTracking(-1);

        // Sauvegarde immediate et obligatoire a la fin du voyage
        dataManager.SaveGame();
        Logger.LogInfo($"MapManager: Travel state cleared and game saved", Logger.LogCategory.MapLog);

        // Declencher les evenements APReS que tout l'etat soit mis a jour et sauve
        eventService.TriggerTravelCompleted(destinationId, destinationLocation, totalStepsTaken);
        eventService.TriggerLocationChanged(previousLocation, destinationLocation);
    }

    /// <summary>
    /// ENHANCED: Retourne des informations detaillees sur le voyage
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
    /// NOUVEAU : Reinitialise l'etat multi-segment (local ET persiste)
    /// </summary>
    private void ResetMultiSegmentState()
    {
        isMultiSegmentTravel = false;
        currentSegmentIndex = 0;
        currentPathDetails = null;

        // ⭐ NOUVEAU : Aussi nettoyer le flag persiste
        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData != null)
        {
            dataManager.PlayerData.IsMultiSegmentTravel = false;
        }
    }

    public void ClearTravelState()
    {
        var dataManager = DataManager.Instance;

        // ⭐ NOUVEAU : Nettoyer TOUTES les donnees de voyage
        dataManager.PlayerData.TravelDestinationId = null;
        dataManager.PlayerData.TravelStartSteps = 0;
        dataManager.PlayerData.TravelRequiredSteps = 0;
        dataManager.PlayerData.TravelFinalDestinationId = null; // ⭐ NOUVEAU
        dataManager.PlayerData.TravelOriginLocationId = null; // ⭐ NOUVEAU

        // NOUVEAU : Nettoyer l'etat multi-segment
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
            return; // Pas en voyage, rien a valider
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

        // Si le voyage est deja termine selon les pas, le completer immediatement
        if (dataManager.PlayerData.IsTravelComplete(currentTotalSteps))
        {
            Logger.LogInfo($"MapManager: Travel already complete on startup - completing now", Logger.LogCategory.MapLog);
            CompleteTravel();
            return;
        }

        // ⭐ NOUVEAU : Restaurer l'etat multi-segment si necessaire
        RestoreMultiSegmentState();

        // etat de voyage valide, continuer normalement
        Logger.LogInfo($"MapManager: Travel state valid - continuing travel to {destination}", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// ⭐ NOUVEAU : Version simplifiee qui utilise les nouveaux champs
    /// </summary>
    private void RestoreMultiSegmentState()
    {
        var dataManager = DataManager.Instance;
        if (!dataManager.PlayerData.IsCurrentlyTraveling() || pathfindingService == null)
        {
            return;
        }

        // ⭐ NOUVEAU : Verifier d'abord le flag persiste
        if (!dataManager.PlayerData.IsMultiSegmentTravel)
        {
            Logger.LogInfo("MapManager: Restoring direct travel (IsMultiSegmentTravel=false)", Logger.LogCategory.MapLog);
            return;
        }

        // ⭐ NOUVEAU : Utiliser les champs sauvegardes
        string finalDestination = dataManager.PlayerData.TravelFinalDestinationId;
        string originLocation = dataManager.PlayerData.TravelOriginLocationId;

        if (string.IsNullOrEmpty(finalDestination))
        {
            // Donnees incoherentes - flag true mais pas de destination finale
            Logger.LogWarning("MapManager: IsMultiSegmentTravel=true but no FinalDestinationId - clearing flag", Logger.LogCategory.MapLog);
            dataManager.PlayerData.IsMultiSegmentTravel = false;
            return;
        }

        if (string.IsNullOrEmpty(originLocation))
        {
            Logger.LogWarning("MapManager: Cannot restore multi-segment - missing origin location", Logger.LogCategory.MapLog);
            // Nettoyer l'etat corrompu
            dataManager.PlayerData.TravelFinalDestinationId = null;
            dataManager.PlayerData.IsMultiSegmentTravel = false;
            return;
        }

        string currentDestination = dataManager.PlayerData.TravelDestinationId;

        // ⭐ NOUVEAU : Recalculer le chemin COMPLET de l'origine vers la destination finale
        var originalPath = pathfindingService.FindPath(originLocation, finalDestination);
        if (!originalPath.IsReachable || originalPath.Segments.Count <= 1)
        {
            Logger.LogWarning("MapManager: Cannot restore multi-segment travel - invalid path", Logger.LogCategory.MapLog);
            // Nettoyer l'etat corrompu
            dataManager.PlayerData.TravelFinalDestinationId = null;
            dataManager.PlayerData.TravelOriginLocationId = null;
            dataManager.PlayerData.IsMultiSegmentTravel = false;
            return;
        }

        // Trouver le segment actuel
        int foundSegmentIndex = -1;
        for (int i = 0; i < originalPath.Segments.Count; i++)
        {
            if (originalPath.Segments[i].ToLocationId == currentDestination)
            {
                foundSegmentIndex = i;
                break;
            }
        }

        if (foundSegmentIndex == -1)
        {
            Logger.LogWarning($"MapManager: Cannot find current segment for destination {currentDestination}", Logger.LogCategory.MapLog);
            dataManager.PlayerData.TravelFinalDestinationId = null;
            dataManager.PlayerData.TravelOriginLocationId = null;
            dataManager.PlayerData.IsMultiSegmentTravel = false;
            return;
        }

        // ⭐ RESTAURER L'eTAT MULTI-SEGMENT
        isMultiSegmentTravel = true;
        currentPathDetails = originalPath;
        currentSegmentIndex = foundSegmentIndex;

        Logger.LogInfo($"MapManager: Restored multi-segment travel - segment {foundSegmentIndex + 1}/{originalPath.Segments.Count} " +
                      $"({originLocation} -> {currentDestination} -> {finalDestination})", Logger.LogCategory.MapLog);
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
            currentLocationId = GameConstants.DefaultStartingLocationId; // Default starting location
            dataManager.PlayerData.CurrentLocationId = currentLocationId;
            dataManager.SaveGame();
        }

        // ⭐ NOUVEAU : Si on est en voyage, NE PAS definir CurrentLocation - on est "nulle part"
        if (dataManager.PlayerData.IsCurrentlyTraveling())
        {
            manager.SetCurrentLocation(null);

            long currentTotalSteps = dataManager.PlayerData.TotalSteps;
            long progressSteps = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);
            int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;
            string destinationId = dataManager.PlayerData.TravelDestinationId;

            Logger.LogInfo($"MapManager: Player is traveling to {destinationId} - CurrentLocation set to null. Progress: {progressSteps}/{requiredSteps} steps", Logger.LogCategory.MapLog);
            manager.SaveService.ResetSaveTracking(currentTotalSteps);
            return;
        }

        // Seulement si on N'EST PAS en voyage, charger la location actuelle
        var currentLocation = manager.LocationRegistry.GetLocationById(currentLocationId);

        if (currentLocation == null)
        {
            Logger.LogError($"MapManager: Current location '{currentLocationId}' not found in registry!", Logger.LogCategory.MapLog);
        }
        else
        {
            manager.SetCurrentLocation(currentLocation);
            Logger.LogInfo($"MapManager: Current location loaded: {currentLocation.DisplayName} ({currentLocationId})", Logger.LogCategory.MapLog);
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
    private const float TRAVEL_SAVE_INTERVAL_DURING_TRAVEL = GameConstants.TravelSaveIntervalSeconds;

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
            DataManager.Instance.SaveTravelProgress();
            lastSavedTotalSteps = currentTotalSteps;
            timeSinceLastTravelSave = 0f;
            Logger.LogInfo($"MapManager: Saved travel progress at {progressSteps} steps", Logger.LogCategory.MapLog);
        }
    }

    public void ResetSaveTracking(long totalSteps)
    {
        lastSavedTotalSteps = totalSteps;
        timeSinceLastTravelSave = 0f;
    }

    public void ForceSaveTravelProgress()
    {
        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            long currentTotalSteps = dataManager.PlayerData.TotalSteps;
            long progressSteps = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);

            dataManager.SaveTravelProgress();
            lastSavedTotalSteps = currentTotalSteps;
            timeSinceLastTravelSave = 0f;

            Logger.LogInfo($"MapManager: Force saved travel progress at {progressSteps} steps", Logger.LogCategory.MapLog);
        }
    }
}