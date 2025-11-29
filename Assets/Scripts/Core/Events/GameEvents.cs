// Purpose: evenements concrets du jeu pour remplacer les evenements existants
// Filepath: Assets/Scripts/Core/Events/GameEvents.cs

/// <summary>
/// evenements lies au GameManager
/// </summary>
namespace GameEvents
{
    /// <summary>
    /// Publie quand l'etat du jeu change (ex: Menu -> Playing -> Paused)
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
    /// Publie AVANT que l'etat du jeu change (annulable)
    /// Nouveau: permet d'empecher un changement d'etat si necessaire
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
/// evenements lies au MapManager et aux deplacements
/// </summary>
namespace MapEvents
{
    /// <summary>
    /// Publie quand le joueur arrive a une nouvelle location
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
    /// Publie pendant le voyage entre deux locations
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
    /// Publie quand un voyage commence
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
    /// Publie quand un voyage se termine avec succes
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
/// evenements lies aux activites (mining, crafting, combat, etc.)
/// </summary>
namespace ActivityEvents
{
    /// <summary>
    /// Publie pendant qu'une activite progresse
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
    /// Publie quand une activite s'arrete (volontairement ou automatiquement)
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
    /// Publie a chaque "tick" d'activite (quand le joueur gagne quelque chose)
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
    /// Publie quand une nouvelle activite commence
    /// Nouveau: permet de notifier le debut d'une activite
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

/// <summary>
/// Evenements lies au combat
/// </summary>
namespace CombatEvents
{
    /// <summary>
    /// Publie quand un combat commence
    /// </summary>
    public class CombatStartedEvent : EventBusEvent
    {
        public CombatData Combat { get; }
        public EnemyDefinition Enemy { get; }

        public CombatStartedEvent(CombatData combat, EnemyDefinition enemy)
        {
            Combat = combat;
            Enemy = enemy;
        }

        public override string ToString()
        {
            return $"{base.ToString()} - Combat started vs {Enemy?.GetDisplayName() ?? "Unknown"}";
        }
    }

    /// <summary>
    /// Publie quand la sante change (joueur ou ennemi)
    /// </summary>
    public class CombatHealthChangedEvent : EventBusEvent
    {
        public bool IsPlayer { get; }
        public float CurrentHealth { get; }
        public float MaxHealth { get; }
        public float CurrentShield { get; }
        public float HealthPercentage => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;

        public CombatHealthChangedEvent(bool isPlayer, float currentHealth, float maxHealth, float currentShield)
        {
            IsPlayer = isPlayer;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            CurrentShield = currentShield;
        }

        public override string ToString()
        {
            var target = IsPlayer ? "Player" : "Enemy";
            return $"{base.ToString()} - {target} health: {CurrentHealth}/{MaxHealth} (Shield: {CurrentShield})";
        }
    }

    /// <summary>
    /// Publie quand une capacite est utilisee
    /// </summary>
    public class CombatAbilityUsedEvent : EventBusEvent
    {
        public bool IsPlayerAbility { get; }
        public AbilityDefinition Ability { get; }
        public int InstanceIndex { get; }
        public float DamageDealt { get; }
        public float HealingDone { get; }
        public float ShieldAdded { get; }
        public float PoisonApplied { get; }

        public CombatAbilityUsedEvent(bool isPlayerAbility, AbilityDefinition ability, int instanceIndex,
            float damageDealt = 0, float healingDone = 0, float shieldAdded = 0, float poisonApplied = 0)
        {
            IsPlayerAbility = isPlayerAbility;
            Ability = ability;
            InstanceIndex = instanceIndex;
            DamageDealt = damageDealt;
            HealingDone = healingDone;
            ShieldAdded = shieldAdded;
            PoisonApplied = poisonApplied;
        }

        public override string ToString()
        {
            var source = IsPlayerAbility ? "Player" : "Enemy";
            return $"{base.ToString()} - {source} used {Ability?.GetDisplayName() ?? "Unknown"}";
        }
    }

    /// <summary>
    /// Publie quand le poison fait des degats (tick)
    /// </summary>
    public class CombatPoisonTickEvent : EventBusEvent
    {
        public bool IsPlayer { get; }
        public float PoisonDamage { get; }
        public float RemainingStacks { get; }

        public CombatPoisonTickEvent(bool isPlayer, float poisonDamage, float remainingStacks)
        {
            IsPlayer = isPlayer;
            PoisonDamage = poisonDamage;
            RemainingStacks = remainingStacks;
        }

        public override string ToString()
        {
            var target = IsPlayer ? "Player" : "Enemy";
            return $"{base.ToString()} - {target} took {PoisonDamage} poison damage (stacks: {RemainingStacks})";
        }
    }

    /// <summary>
    /// Publie quand un combat se termine
    /// </summary>
    public class CombatEndedEvent : EventBusEvent
    {
        public bool PlayerWon { get; }
        public EnemyDefinition Enemy { get; }
        public int ExperienceGained { get; }
        public System.Collections.Generic.Dictionary<ItemDefinition, int> LootDropped { get; }
        public string EndReason { get; } // "victory", "defeat", "fled"

        public CombatEndedEvent(bool playerWon, EnemyDefinition enemy, int experienceGained,
            System.Collections.Generic.Dictionary<ItemDefinition, int> lootDropped, string endReason = "")
        {
            PlayerWon = playerWon;
            Enemy = enemy;
            ExperienceGained = experienceGained;
            LootDropped = lootDropped ?? new System.Collections.Generic.Dictionary<ItemDefinition, int>();
            EndReason = string.IsNullOrEmpty(endReason) ? (playerWon ? "victory" : "defeat") : endReason;
        }

        public override string ToString()
        {
            var result = PlayerWon ? "Victory" : "Defeat";
            return $"{base.ToString()} - {result} vs {Enemy?.GetDisplayName() ?? "Unknown"} (+{ExperienceGained} XP)";
        }
    }

    /// <summary>
    /// Publie quand le joueur quitte le combat volontairement
    /// </summary>
    public class CombatFledEvent : EventBusEvent
    {
        public EnemyDefinition Enemy { get; }

        public CombatFledEvent(EnemyDefinition enemy)
        {
            Enemy = enemy;
        }

        public override string ToString()
        {
            return $"{base.ToString()} - Fled from {Enemy?.GetDisplayName() ?? "Unknown"}";
        }
    }

    /// <summary>
    /// Publie quand le cooldown d'une capacite commence
    /// </summary>
    public class CombatAbilityCooldownStartedEvent : EventBusEvent
    {
        public bool IsPlayerAbility { get; }
        public AbilityDefinition Ability { get; }
        public int InstanceIndex { get; }
        public float CooldownDuration { get; }

        public CombatAbilityCooldownStartedEvent(bool isPlayerAbility, AbilityDefinition ability, int instanceIndex, float cooldownDuration)
        {
            IsPlayerAbility = isPlayerAbility;
            Ability = ability;
            InstanceIndex = instanceIndex;
            CooldownDuration = cooldownDuration;
        }

        public override string ToString()
        {
            var source = IsPlayerAbility ? "Player" : "Enemy";
            return $"{base.ToString()} - {source}'s {Ability?.GetDisplayName() ?? "Unknown"} cooldown started ({CooldownDuration}s)";
        }
    }
}