

// ===============================================
// SERVICE: Event Management
// ===============================================
using ActivityEvents;
using GameEvents;
using MapEvents;

public class AboveCanvasEventService
{
    private readonly AboveCanvasManager manager;
    private readonly AboveCanvasDisplayService displayService;
    private readonly AboveCanvasAnimationService animationService;

    public AboveCanvasEventService(AboveCanvasManager manager, AboveCanvasDisplayService displayService, AboveCanvasAnimationService animationService)
    {
        this.manager = manager;
        this.displayService = displayService;
        this.animationService = animationService;
    }

    public void SubscribeToEvents()
    {
        // =====================================
        // EVENTBUS - Fini les managers !
        // =====================================

        // GameManager events → GameEvents
        EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);

        // MapManager events → MapEvents  
        EventBus.Subscribe<LocationChangedEvent>(OnLocationChanged);
        EventBus.Subscribe<TravelProgressEvent>(OnTravelProgress);
        EventBus.Subscribe<TravelStartedEvent>(OnTravelStarted);

        // ActivityManager events → ActivityEvents
        EventBus.Subscribe<ActivityProgressEvent>(OnActivityProgress);
        EventBus.Subscribe<ActivityStoppedEvent>(OnActivityStopped);
        EventBus.Subscribe<ActivityTickEvent>(OnActivityTick);

        Logger.LogInfo("AboveCanvasEventService: Subscribed to EventBus events", Logger.LogCategory.General);
    }

    public void UnsubscribeFromEvents()
    {
        // =====================================
        // EVENTBUS - Desabonnement simple
        // =====================================

        EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Unsubscribe<LocationChangedEvent>(OnLocationChanged);
        EventBus.Unsubscribe<TravelProgressEvent>(OnTravelProgress);
        EventBus.Unsubscribe<ActivityProgressEvent>(OnActivityProgress);
        EventBus.Unsubscribe<ActivityStoppedEvent>(OnActivityStopped);
        EventBus.Unsubscribe<ActivityTickEvent>(OnActivityTick);
        EventBus.Unsubscribe<TravelStartedEvent>(OnTravelStarted);

        Logger.LogInfo("AboveCanvasEventService: Unsubscribed from EventBus events", Logger.LogCategory.General);
    }

    public void Cleanup()
    {
        // Desabonner de tous les evenements
        UnsubscribeFromEvents();

        // Nettoyer les animations
        animationService?.Cleanup();
    }

    // =====================================
    // EVENT HANDLERS - Adaptes pour EventBus
    // =====================================

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        Logger.LogInfo($"AboveCanvasManager: Game state changed from {eventData.PreviousState} to {eventData.NewState}", Logger.LogCategory.General);
        displayService.RefreshDisplay();
    }

    private void OnLocationChanged(LocationChangedEvent eventData)
    {
        Logger.LogInfo($"AboveCanvasManager: Location changed from {eventData.PreviousLocation?.DisplayName ?? "None"} to {eventData.NewLocation?.DisplayName ?? "None"}", Logger.LogCategory.General);
        displayService.UpdateLocationDisplay();
    }

    private void OnTravelProgress(TravelProgressEvent eventData)
    {
        Logger.LogInfo($"AboveCanvasManager: Travel progress {eventData.CurrentSteps}/{eventData.RequiredSteps} to {eventData.DestinationLocationId}", Logger.LogCategory.General);
        displayService.UpdateTravelProgress(eventData.CurrentSteps, eventData.RequiredSteps);
    }


    private void OnTravelStarted(TravelStartedEvent eventData)
    {
        Logger.LogInfo($"AboveCanvasManager: Travel started to {eventData.DestinationLocationId} from {eventData.CurrentLocation?.DisplayName}", Logger.LogCategory.General);

        // Forcer une mise à jour complète de l'affichage pour récupérer les nouvelles icônes
        displayService.RefreshDisplay();
    }
    private void OnActivityProgress(ActivityProgressEvent eventData)
    {
        Logger.LogInfo($"AboveCanvasManager: Activity progress {eventData.Activity?.ActivityId}/{eventData.Variant?.VariantName} ({eventData.ProgressPercentage:F1}%)", Logger.LogCategory.General);
        displayService.UpdateActivityProgress(eventData.Activity, eventData.Variant);
    }

    private void OnActivityStopped(ActivityStoppedEvent eventData)
    {
        Logger.LogInfo($"AboveCanvasManager: Activity stopped {eventData.Activity?.ActivityId}/{eventData.Variant?.VariantName} (Completed: {eventData.WasCompleted})", Logger.LogCategory.General);
        displayService.RefreshDisplay();
    }

    private void OnActivityTick(ActivityTickEvent eventData)
    {
        if (eventData.TicksCompleted > 0)
        {
            animationService?.ShakeRightIcon(); // Animation de satisfaction !
            Logger.LogInfo($"AboveCanvasManager: Activity tick completed - {eventData.TicksCompleted} ticks, {eventData.Rewards.Length} rewards", Logger.LogCategory.General);
        }
    }
}