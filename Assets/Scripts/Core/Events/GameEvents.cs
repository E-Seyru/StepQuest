// Purpose: Événements concrets du jeu pour remplacer les événements existants
// Filepath: Assets/Scripts/Core/Events/GameEvents.cs

/// <summary>
/// Événements liés au GameManager
/// </summary>
namespace GameEvents
{
    /// <summary>
    /// Publié quand l'état du jeu change (ex: Menu -> Playing -> Paused)
    /// Remplace: GameManager.OnGameStateChanged
    /// </summary>
    public class GameStateChangedEvent : EventBusEvent
    {
        public GameState PreviousState { get; }
        public GameState NewState { get; }

        public GameStateChangedEvent(GameState previousState, GameState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }

        public override string ToString()
        {
            return $"{base.ToString()} - {PreviousState} → {NewState}";
        }
    }

    /// <summary>
    /// Publié AVANT que l'état du jeu change (annulable)
    /// Nouveau: permet d'empêcher un changement d'état si nécessaire
    /// </summary>
    public class BeforeGameStateChangeEvent : CancellableEventBusEvent
    {
        public GameState CurrentState { get; }
        public GameState RequestedState { get; }

        public BeforeGameStateChangeEvent(GameState currentState, GameState requestedState)
        {
            CurrentState = currentState;
            RequestedState = requestedState;
        }

        public override string ToString()
        {
            return $"{base.ToString()} - Requesting {CurrentState} → {RequestedState}";
        }
    }
}

/// <summary>
/// Événements liés au MapManager et aux déplacements
/// </summary>
namespace MapEvents
{
    /// <summary>
    /// Publié quand le joueur arrive à une nouvelle location
    /// Remplace: MapManager.OnLocationChanged
    /// </summary>
    public class LocationChangedEvent : EventBusEvent
    {
        public MapLocationDefinition PreviousLocation { get; }
        public MapLocationDefinition NewLocation { get; }

        public LocationChangedEvent(MapLocationDefinition previousLocation, MapLocationDefinition newLocation)
        {
            PreviousLocation = previousLocation;
            NewLocation = newLocation;
        }

        public override string ToString()
        {
            var prevName = PreviousLocation?.DisplayName ?? "None";
            var newName = NewLocation?.DisplayName ?? "None";
            return $"{base.ToString()} - {prevName} → {newName}";
        }
    }

    /// <summary>
    /// Publié pendant le voyage entre deux locations
    /// Remplace: MapManager.OnTravelProgress
    /// </summary>
    public class TravelProgressEvent : EventBusEvent
    {
        public string DestinationLocationId { get; }
        public int CurrentSteps { get; }
        public int RequiredSteps { get; }
        public float ProgressPercentage => RequiredSteps > 0 ? (float)CurrentSteps / RequiredSteps * 100f : 0f;

        public TravelProgressEvent(string destinationLocationId, int currentSteps, int requiredSteps)
        {
            DestinationLocationId = destinationLocationId;
            CurrentSteps = currentSteps;
            RequiredSteps = requiredSteps;
        }

        public override string ToString()
        {
            return $"{base.ToString()} - To {DestinationLocationId}: {CurrentSteps}/{RequiredSteps} ({ProgressPercentage:F1}%)";
        }
    }

    /// <summary>
    /// Publié quand un voyage commence
    /// Remplace: MapManager.OnTravelStarted
    /// </summary>
    public class TravelStartedEvent : EventBusEvent
    {
        public string DestinationLocationId { get; }
        public MapLocationDefinition CurrentLocation { get; }
        public int RequiredSteps { get; }

        public TravelStartedEvent(string destinationLocationId, MapLocationDefinition currentLocation, int requiredSteps)
        {
            DestinationLocationId = destinationLocationId;
            CurrentLocation = currentLocation;
            RequiredSteps = requiredSteps;
        }

        public override string ToString()
        {
            return $"{base.ToString()} - From {CurrentLocation?.DisplayName} to {DestinationLocationId} ({RequiredSteps} steps)";
        }
    }

    /// <summary>
    /// Publié quand un voyage se termine avec succès
    /// Remplace: MapManager.OnTravelCompleted
    /// </summary>
    public class TravelCompletedEvent : EventBusEvent
    {
        public string DestinationLocationId { get; }
        public MapLocationDefinition NewLocation { get; }
        public int StepsTaken { get; }

        public TravelCompletedEvent(string destinationLocationId, MapLocationDefinition newLocation, int stepsTaken)
        {
            DestinationLocationId = destinationLocationId;
            NewLocation = newLocation;
            StepsTaken = stepsTaken;
        }

        public override string ToString()
        {
            return $"{base.ToString()} - Arrived at {NewLocation?.DisplayName} ({StepsTaken} steps taken)";
        }
    }
}

/// <summary>
/// Événements liés aux activités (mining, crafting, combat, etc.)
/// </summary>
namespace ActivityEvents
{
    /// <summary>
    /// Publié pendant qu'une activité progresse
    /// Remplace: ActivityManager.OnActivityProgress
    /// </summary>
    public class ActivityProgressEvent : EventBusEvent
    {
        public ActivityData Activity { get; }
        public ActivityVariant Variant { get; }
        public float ProgressPercentage { get; }

        public ActivityProgressEvent(ActivityData activity, ActivityVariant variant, float progressPercentage = 0f)
        {
            Activity = activity;
            Variant = variant;
            ProgressPercentage = progressPercentage;
        }

        public override string ToString()
        {
            var activityId = Activity?.ActivityId ?? "Unknown";
            var variantId = Activity?.VariantId ?? "Unknown";
            return $"{base.ToString()} - {activityId}/{variantId} ({ProgressPercentage:F1}%)";
        }
    }

    /// <summary>
    /// Publié quand une activité s'arrête (volontairement ou automatiquement)
    /// Remplace: ActivityManager.OnActivityStopped
    /// </summary>
    public class ActivityStoppedEvent : EventBusEvent
    {
        public ActivityData Activity { get; }
        public ActivityVariant Variant { get; }
        public bool WasCompleted { get; }
        public string StopReason { get; }

        public ActivityStoppedEvent(ActivityData activity, ActivityVariant variant, bool wasCompleted, string stopReason = "")
        {
            Activity = activity;
            Variant = variant;
            WasCompleted = wasCompleted;
            StopReason = stopReason;
        }

        public override string ToString()
        {
            var activityId = Activity?.ActivityId ?? "Unknown";
            var variantId = Activity?.VariantId ?? "Unknown";
            var status = WasCompleted ? "Completed" : "Stopped";
            var reason = !string.IsNullOrEmpty(StopReason) ? $" ({StopReason})" : "";
            return $"{base.ToString()} - {activityId}/{variantId} {status}{reason}";
        }
    }

    /// <summary>
    /// Publié à chaque "tick" d'activité (quand le joueur gagne quelque chose)
    /// Remplace: ActivityManager.OnActivityTick
    /// </summary>
    public class ActivityTickEvent : EventBusEvent
    {
        public ActivityData Activity { get; }
        public ActivityVariant Variant { get; }
        public int TicksCompleted { get; }
        public object[] Rewards { get; } // Items, XP, etc.

        public ActivityTickEvent(ActivityData activity, ActivityVariant variant, int ticksCompleted, params object[] rewards)
        {
            Activity = activity;
            Variant = variant;
            TicksCompleted = ticksCompleted;
            Rewards = rewards ?? new object[0];
        }

        public override string ToString()
        {
            var activityId = Activity?.ActivityId ?? "Unknown";
            var variantId = Activity?.VariantId ?? "Unknown";
            return $"{base.ToString()} - {activityId}/{variantId} +{TicksCompleted} ticks ({Rewards.Length} rewards)";
        }
    }

    /// <summary>
    /// Publié quand une nouvelle activité commence
    /// Nouveau: permet de notifier le début d'une activité
    /// </summary>
    public class ActivityStartedEvent : EventBusEvent
    {
        public ActivityData Activity { get; }
        public ActivityVariant Variant { get; }

        public ActivityStartedEvent(ActivityData activity, ActivityVariant variant)
        {
            Activity = activity;
            Variant = variant;
        }

        public override string ToString()
        {
            var activityId = Activity?.ActivityId ?? "Unknown";
            var variantId = Activity?.VariantId ?? "Unknown";
            return $"{base.ToString()} - Started {activityId}/{variantId}";
        }
    }
}