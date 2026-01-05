// ===============================================
// ActivityManager (Facade Pattern) - MIGRe VERS EVENTBUS
// ===============================================
// Purpose: Manages player activities (mining, gathering, crafting, etc.) with step-based AND time-based progression
// Filepath: Assets/Scripts/Gameplay/Player/ActivityManager.cs

using ActivityEvents; // NOUVEAU: Import pour EventBus
using System;
using System.Collections;
using UnityEngine;

public class ActivityManager : MonoBehaviour
{
    public static ActivityManager Instance { get; private set; }

    // === SAME PUBLIC API - ZERO BREAKING CHANGES ===
    [Header("Settings")]
    [SerializeField] private float autoSaveInterval = 30f;
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Activity Data")]
    [SerializeField] private ActivityRegistry activityRegistry;

    // === PUBLIC ACCESSORS - SAME AS BEFORE ===
    public ActivityRegistry ActivityRegistry => activityRegistry;

    // === INTERNAL SERVICES (EXISTANT + NOUVEAU) ===
    private ActivityExecutionService executionService;
    private ActivityProgressService progressService;
    private ActivityValidationService validationService;
    private ActivityCacheService cacheService;
    private ActivityTimeService timeService; // NOUVEAU SERVICE
    public ActivityPersistenceService PersistenceService { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            InitializeServices();
        }
        else
        {
            Logger.LogWarning("ActivityManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.General);
            Destroy(gameObject);
        }
    }

    private void InitializeServices()
    {
        // Initialize services with dependency injection
        validationService = new ActivityValidationService();
        cacheService = new ActivityCacheService(activityRegistry);
        timeService = new ActivityTimeService(cacheService, enableDebugLogs); // NOUVEAU
        progressService = new ActivityProgressService(cacheService, timeService, enableDebugLogs); // MODIFIE
        executionService = new ActivityExecutionService(cacheService, progressService, validationService, enableDebugLogs);
        PersistenceService = new ActivityPersistenceService(autoSaveInterval);

        // Les services publient maintenant directement via EventBus - plus de wire events !
        Logger.LogInfo("ActivityManager: Services initialized with EventBus", Logger.LogCategory.General);
    }

    void Start()
    {
        // Initialize all services
        validationService.Initialize();
        cacheService.Initialize();
        timeService.Initialize(); // NOUVEAU
        progressService.Initialize();
        executionService.Initialize();
        PersistenceService.Initialize();

        // Validate dependencies
        if (!validationService.ValidateAllDependencies(activityRegistry))
        {
            Logger.LogError("ActivityManager: Critical dependencies missing! ActivityManager will not function properly.", Logger.LogCategory.General);
            return;
        }

        // Process any offline activity progress
        Invoke(nameof(ProcessOfflineProgressInvoke), 1f);
    }

    private void ProcessOfflineProgressInvoke()
    {
        progressService.ProcessOfflineProgress();
    }

    // MODIFIE: Update method pour gerer les deux types d'activites
    void Update()
    {
        if (HasActiveActivity())
        {
            var (currentActivity, _) = GetCurrentActivityInfo();
            if (currentActivity != null)
            {
                if (currentActivity.IsTimeBased)
                {
                    // Verifier les mises a jour de temps
                    timeService.CheckForTimeUpdates();
                }
                else
                {
                    // Verifier les mises a jour de pas
                    progressService.CheckForStepUpdates();
                }
            }
        }
    }

    void OnEnable() => PersistenceService?.StartAutoSave();
    void OnDisable() => PersistenceService?.StopAutoSave();

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) PersistenceService?.ForceSave();
    }

    void OnApplicationQuit() => PersistenceService?.ForceSave();

    // === SAME PUBLIC API - EXACTLY THE SAME ===
    public bool StartActivity(string activityId, string variantId)
    {
        return executionService.StartActivity(activityId, variantId);
    }

    public bool StopActivity()
    {
        return executionService.StopActivity();
    }

    public bool CanStartActivity()
    {
        return executionService.CanStartActivity();
    }

    public bool HasActiveActivity()
    {
        return executionService.HasActiveActivity();
    }

    public (ActivityData activity, ActivityVariant variant) GetCurrentActivityInfo()
    {
        return executionService.GetCurrentActivityInfo();
    }

    public bool ShouldBlockTravel()
    {
        return HasActiveActivity();
    }

    public string GetDebugInfo()
    {
        return executionService.GetDebugInfo();
    }

    // === NOUVEAU: API POUR ACTIVITES TEMPORELLES ===

    /// <summary>
    /// NOUVEAU: Demarre une activite temporelle (crafting)
    /// </summary>
    public bool StartTimedActivity(string activityId, string variantId, string locationId = null)
    {
        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityManager: Attempting to start timed activity {activityId}/{variantId}", Logger.LogCategory.General);
        }

        // Validation
        if (!validationService.ValidateActivityStart(activityId, variantId))
        {
            Logger.LogWarning($"ActivityManager: Cannot start timed activity - validation failed", Logger.LogCategory.General);
            return false;
        }

        // Verifier qu'on a les materiaux pour crafter
        var variant = ActivityRegistry.GetActivityVariant(activityId, variantId);
        if (variant == null || !variant.IsTimeBased)
        {
            Logger.LogError($"ActivityManager: Variant {variantId} is not a time-based activity", Logger.LogCategory.General);
            return false;
        }

        var inventoryManager = InventoryManager.Instance;
        if (!variant.CanCraft(inventoryManager))
        {
            Logger.LogWarning($"ActivityManager: Cannot start crafting - missing materials for {variant.GetDisplayName()}", Logger.LogCategory.General);
            return false;
        }

        // Consommer les materiaux
        if (!variant.ConsumeCraftingMaterials(inventoryManager))
        {
            Logger.LogError($"ActivityManager: Failed to consume materials for {variant.GetDisplayName()}", Logger.LogCategory.General);
            return false;
        }

        // Arreter l'activite actuelle s'il y en a une
        if (DataManager.Instance.PlayerData.HasActiveActivity())
        {
            StopActivity();
        }

        // Utiliser la localisation actuelle si pas specifiee
        if (string.IsNullOrEmpty(locationId))
        {
            locationId = DataManager.Instance.PlayerData.CurrentLocationId ?? "unknown";
        }

        // Creer la nouvelle activite temporelle
        var activityData = new ActivityData(activityId, variantId, variant.CraftingTimeMs, locationId, true);

        // Demarrer l'execution
        bool success = executionService.StartActivity(activityData, variant);

        if (success && enableDebugLogs)
        {
            Logger.LogInfo($"ActivityManager: Started timed activity {variant.GetDisplayName()} (Duration: {variant.GetCraftingTimeText()})", Logger.LogCategory.General);
        }

        return success;
    }

    /// <summary>
    /// NOUVEAU: Verifie si on peut demarrer une activite temporelle
    /// </summary>
    public bool CanStartTimedActivity(string activityId, string variantId)
    {
        if (!validationService.ValidateActivityStart(activityId, variantId))
            return false;

        var variant = ActivityRegistry.GetActivityVariant(activityId, variantId);
        if (variant == null || !variant.IsTimeBased)
            return false;

        var inventoryManager = InventoryManager.Instance;
        return variant.CanCraft(inventoryManager);
    }

    void OnDestroy()
    {
        // Cancel any pending Invoke calls to prevent null reference exceptions
        CancelInvoke(nameof(ProcessOfflineProgressInvoke));

        PersistenceService?.Cleanup();

        // Clear singleton reference if this is the active instance
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

// ===============================================
// NOUVEAU SERVICE: ActivityTimeService - MIGRe VERS EVENTBUS
// ===============================================
public class ActivityTimeService
{
    private readonly ActivityCacheService cacheService;
    private readonly bool enableDebugLogs;

    public ActivityTimeService(ActivityCacheService cacheService, bool enableDebugLogs)
    {
        this.cacheService = cacheService;
        this.enableDebugLogs = enableDebugLogs;
    }

    public void Initialize()
    {
        // Service initialization if needed
        Logger.LogInfo("ActivityTimeService: Initialized", Logger.LogCategory.General);
    }

    /// <summary>
    /// Verifie et traite les mises a jour temporelles pour les activites time-based
    /// </summary>
    public void CheckForTimeUpdates()
    {
        var dataManager = DataManager.Instance;

        if (!dataManager?.PlayerData?.HasActiveActivity() == true) return;

        var (currentActivityCache, _) = cacheService.GetCurrentActivityInfo();

        if (currentActivityCache == null || !currentActivityCache.IsTimeBased) return;

        // Calculer le temps non-traite
        long unprocessedTimeMs = currentActivityCache.GetUnprocessedTimeMs();

        if (unprocessedTimeMs > 0)
        {
            ProcessNewTime(unprocessedTimeMs);
        }
    }

    /// <summary>
    /// Traite le nouveau temps ecoule pour les activites temporelles
    /// </summary>
    public void ProcessNewTime(long newTimeMs)
    {
        var dataManager = DataManager.Instance;
        if (!dataManager?.PlayerData?.HasActiveActivity() == true || newTimeMs <= 0) return;

        var (currentActivityCache, currentVariantCache) = cacheService.GetCurrentActivityInfo();
        if (currentActivityCache == null || currentVariantCache == null || !currentActivityCache.IsTimeBased) return;

        // Ajouter le temps a l'activite
        currentActivityCache.AddTime(newTimeMs);

        // Verifier si l'activite est maintenant terminee
        bool isComplete = currentActivityCache.IsComplete();

        if (isComplete)
        {
            ProcessTimedActivityCompletion(currentActivityCache, currentVariantCache);
        }
        else
        {
            // Mettre a jour les donnees (thread-safe)
            dataManager.UpdateActivity(currentActivityCache);

            // Marquer comme dirty pour persistence
            ActivityManager.Instance.PersistenceService.MarkDirty();

            // =====================================
            // EVENTBUS - Publier l'evenement de progression
            // =====================================
            EventBus.Publish(new ActivityProgressEvent(currentActivityCache, currentVariantCache,
                currentActivityCache.GetProgressToNextTick(currentVariantCache) * 100f));

            if (enableDebugLogs)
            {
                float progress = currentActivityCache.GetProgressToNextTick(currentVariantCache);
                Logger.LogInfo($"ActivityTimeService: Added {newTimeMs}ms to {currentVariantCache.GetDisplayName()}, Progress: {progress:F2}", Logger.LogCategory.General);
            }
        }
    }

    /// <summary>
    /// Traite la completion d'une activite temporelle (ex: crafting termine)
    /// </summary>
    private void ProcessTimedActivityCompletion(ActivityData activityCache, ActivityVariant variantCache)
    {
        if (variantCache == null) return;

        // Produire l'item crafte
        string resourceId = variantCache.PrimaryResource?.ItemID;

        if (!string.IsNullOrEmpty(resourceId))
        {
            var inventoryManager = InventoryManager.Instance;
            bool rewardSuccess = inventoryManager.AddItem("player", resourceId, 1);

            if (!rewardSuccess)
            {
                Logger.LogWarning($"ActivityTimeService: Failed to add crafted item {resourceId} to inventory - inventory full?", Logger.LogCategory.General);
            }
        }

        // Marquer l'activite comme terminee
        activityCache.ProcessTicks(variantCache, 1);

        // =====================================
        // EVENTBUS - Publier l'evenement de tick
        // =====================================
        EventBus.Publish(new ActivityTickEvent(activityCache, variantCache, 1, resourceId));

        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityTimeService: Completed crafting {variantCache.GetDisplayName()}", Logger.LogCategory.General);
        }

        // Verifier si on peut recommencer automatiquement (boucle)
        bool shouldContinue = false;
        if (variantCache.CanCraft(InventoryManager.Instance))
        {
            // Consommer les materiaux et redemarrer
            if (variantCache.ConsumeCraftingMaterials(InventoryManager.Instance))
            {
                // Reset l'activite pour une nouvelle boucle
                activityCache.AccumulatedTimeMs = 0;
                activityCache.LastProcessedTimeMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                shouldContinue = true;
            }
            else
            {
                // Impossible de consommer, arreter l'activite
                StopTimedActivity(activityCache, variantCache);
            }
        }
        else
        {
            // Plus de materiaux, arreter l'activite
            StopTimedActivity(activityCache, variantCache);
        }

        // Sauvegarder l'etat SEULEMENT si on continue (thread-safe)
        if (shouldContinue)
        {
            DataManager.Instance.UpdateActivity(activityCache);
            ActivityManager.Instance.PersistenceService.MarkDirty();
        }
    }

    /// <summary>
    /// Arrete une activite temporelle
    /// </summary>
    private void StopTimedActivity(ActivityData activityCache, ActivityVariant variantCache)
    {
        activityCache.Clear();
        DataManager.Instance.StopActivity();
        ActivityManager.Instance.PersistenceService.MarkDirty();

        // =====================================
        // EVENTBUS - Publier l'evenement d'arret
        // =====================================
        EventBus.Publish(new ActivityStoppedEvent(activityCache, variantCache, true, "Materials depleted"));
    }

    /// <summary>
    /// Traite la progression temporelle offline
    /// </summary>
    public void ProcessOfflineTimeProgress()
    {
        var dataManager = DataManager.Instance;
        if (!dataManager?.PlayerData?.HasActiveActivity() == true) return;

        var (currentActivityCache, currentVariantCache) = cacheService.GetCurrentActivityInfo();
        if (currentActivityCache == null || currentVariantCache == null || !currentActivityCache.IsTimeBased) return;

        // Calculer le temps offline total
        long offlineTimeMs = currentActivityCache.GetElapsedTimeMs();
        if (offlineTimeMs <= 0) return;

        // Simuler le temps offline par chunks pour gerer les boucles
        long remainingTime = offlineTimeMs;
        int completedCrafts = 0;
        bool activityWasStopped = false;

        while (remainingTime > 0 && currentActivityCache.IsTimeBased)
        {
            long timeNeeded = currentActivityCache.RequiredTimeMs - currentActivityCache.AccumulatedTimeMs;

            if (remainingTime >= timeNeeded)
            {
                // Completer ce craft
                remainingTime -= timeNeeded;
                currentActivityCache.AccumulatedTimeMs = currentActivityCache.RequiredTimeMs;
                completedCrafts++;

                // Donner les recompenses
                string resourceId = currentVariantCache.PrimaryResource?.ItemID;
                if (!string.IsNullOrEmpty(resourceId))
                {
                    InventoryManager.Instance.AddItem("player", resourceId, 1);
                }

                // Verifier si on peut continuer
                if (currentVariantCache.CanCraft(InventoryManager.Instance))
                {
                    currentVariantCache.ConsumeCraftingMaterials(InventoryManager.Instance);
                    currentActivityCache.AccumulatedTimeMs = 0; // Reset pour le prochain craft
                }
                else
                {
                    // Plus de materiaux, arreter (thread-safe)
                    currentActivityCache.Clear();
                    DataManager.Instance.StopActivity();
                    activityWasStopped = true;
                    break;
                }
            }
            else
            {
                // Temps partiel, ajouter ce qui reste
                currentActivityCache.AccumulatedTimeMs += remainingTime;
                remainingTime = 0;
            }
        }

        // Mettre a jour le timestamp SEULEMENT si l'activite n'a pas ete arretee (thread-safe)
        if (!activityWasStopped)
        {
            currentActivityCache.LastProcessedTimeMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DataManager.Instance.UpdateActivity(currentActivityCache);
            ActivityManager.Instance.PersistenceService.MarkDirty();
        }

        if (completedCrafts > 0)
        {
            Logger.LogInfo($"ActivityTimeService: Completed {completedCrafts} crafts offline", Logger.LogCategory.General);
        }
    }
}

// ===============================================
// SERVICE: ActivityExecutionService - MIGRe VERS EVENTBUS
// ===============================================
public class ActivityExecutionService
{
    private readonly ActivityCacheService cacheService;
    private readonly ActivityProgressService progressService;
    private readonly ActivityValidationService validationService;
    private readonly bool enableDebugLogs;

    public ActivityExecutionService(ActivityCacheService cacheService, ActivityProgressService progressService,
                                  ActivityValidationService validationService, bool enableDebugLogs)
    {
        this.cacheService = cacheService;
        this.progressService = progressService;
        this.validationService = validationService;
        this.enableDebugLogs = enableDebugLogs;
    }

    public void Initialize()
    {
        // Service initialization if needed
    }

    public bool StartActivity(string activityId, string variantId)
    {
        if (!CanStartActivity())
        {
            Logger.LogWarning($"ActivityManager: Cannot start activity {activityId}/{variantId} - conditions not met", Logger.LogCategory.General);
            return false;
        }

        // Get activity definition and variant
        var activityDef = cacheService.GetActivityDefinition(activityId);
        var variant = cacheService.GetActivityVariant(activityId, variantId);

        if (activityDef == null || variant == null)
        {
            Logger.LogError($"ActivityManager: Activity {activityId}/{variantId} not found in registry!", Logger.LogCategory.General);
            return false;
        }

        if (!validationService.ValidateVariant(variant) || !validationService.ValidateActivity(activityDef))
        {
            Logger.LogError($"ActivityManager: Activity {activityId}/{variantId} validation failed!", Logger.LogCategory.General);
            return false;
        }

        // Stop current activity if any
        if (HasActiveActivity())
        {
            StopActivity();
        }

        // Create new activity based on type
        ActivityData newActivity;
        string currentLocationId = DataManager.Instance.PlayerData.CurrentLocationId ?? "unknown";

        if (variant.IsTimeBased)
        {
            // This should not happen - use StartTimedActivity instead
            Logger.LogError($"ActivityManager: Use StartTimedActivity() for time-based activities!", Logger.LogCategory.General);
            return false;
        }
        else
        {
            // Step-based activity
            long currentSteps = DataManager.Instance.PlayerData.TotalSteps;
            newActivity = new ActivityData(activityId, variantId, currentSteps, currentLocationId);
        }

        return StartActivity(newActivity, variant);
    }

    // NOUVEAU: Methode interne pour demarrer avec ActivityData directe
    internal bool StartActivity(ActivityData activityData, ActivityVariant variant)
    {
        // Save to player data (thread-safe with validation)
        if (!DataManager.Instance.StartActivity(activityData))
        {
            Logger.LogWarning("ActivityManager: Cannot start activity - player is traveling!", Logger.LogCategory.General);
            return false;
        }

        // Mark for saving
        ActivityManager.Instance.PersistenceService.MarkDirty();

        // =====================================
        // EVENTBUS - Publier l'evenement de debut
        // =====================================
        EventBus.Publish(new ActivityStartedEvent(activityData, variant));

        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityManager: Started activity {variant.GetDisplayName()}", Logger.LogCategory.General);
        }

        return true;
    }

    public bool StopActivity()
    {
        var (currentActivity, currentVariant) = GetCurrentActivityInfo();
        if (currentActivity == null || currentVariant == null)
        {
            Logger.LogWarning("ActivityManager: No active activity to stop", Logger.LogCategory.General);
            return false;
        }

        // Clear the activity (thread-safe)
        DataManager.Instance.StopActivity();

        // Mark for saving
        ActivityManager.Instance.PersistenceService.MarkDirty();

        // =====================================
        // EVENTBUS - Publier l'evenement d'arret
        // =====================================
        EventBus.Publish(new ActivityStoppedEvent(currentActivity, currentVariant, false, "Manually stopped"));

        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityManager: Stopped activity {currentVariant.GetDisplayName()}", Logger.LogCategory.General);
        }

        return true;
    }

    public bool CanStartActivity()
    {
        return validationService.CanStartActivity();
    }

    public bool HasActiveActivity()
    {
        return DataManager.Instance?.PlayerData?.HasActiveActivity() == true;
    }

    public (ActivityData activity, ActivityVariant variant) GetCurrentActivityInfo()
    {
        return cacheService.GetCurrentActivityInfo();
    }

    public string GetDebugInfo()
    {
        var (activity, variant) = GetCurrentActivityInfo();
        if (activity == null || variant == null)
            return "No active activity";

        return activity.ToString(variant) + $"\nElapsed Time: {TimeSpan.FromMilliseconds(activity.GetElapsedTimeMs()).TotalMinutes:F1} min";
    }
}

// ===============================================
// SERVICE: ActivityProgressService - MIGRe VERS EVENTBUS
// ===============================================
public class ActivityProgressService
{
    private readonly ActivityCacheService cacheService;
    private readonly ActivityTimeService timeService;
    private readonly bool enableDebugLogs;

    public ActivityProgressService(ActivityCacheService cacheService, ActivityTimeService timeService, bool enableDebugLogs)
    {
        this.cacheService = cacheService;
        this.timeService = timeService;
        this.enableDebugLogs = enableDebugLogs;
    }

    public void Initialize()
    {
        // Service initialization if needed
    }

    public void CheckForStepUpdates()
    {
        var dataManager = DataManager.Instance;
        var stepManager = StepManager.Instance;

        if (!dataManager?.PlayerData?.HasActiveActivity() == true || stepManager == null) return;

        long currentSteps = stepManager.TotalSteps;
        var (currentActivityCache, _) = cacheService.GetCurrentActivityInfo();

        if (currentActivityCache == null || currentActivityCache.IsTimeBased) return; // Skip time-based activities

        long lastProcessed = currentActivityCache.LastProcessedTotalSteps;

        if (currentSteps > lastProcessed)
        {
            int newSteps = (int)(currentSteps - lastProcessed);
            ProcessNewSteps(newSteps);
        }
    }

    public void ProcessNewSteps(int newSteps)
    {
        var dataManager = DataManager.Instance;
        if (!dataManager?.PlayerData?.HasActiveActivity() == true || newSteps <= 0) return;

        var (currentActivityCache, currentVariantCache) = cacheService.GetCurrentActivityInfo();
        if (currentActivityCache == null || currentVariantCache == null || currentActivityCache.IsTimeBased) return;

        // Add steps to activity
        currentActivityCache.AddSteps(newSteps);

        // Check if this is an exploration activity and get speed modifier
        var activityDef = cacheService.GetActivityDefinition(currentActivityCache.ActivityId);
        bool isExploration = activityDef?.ActivityReference?.IsExploration() ?? false;
        float speedModifier = 1.0f;
        if (isExploration && ExplorationManager.Instance != null)
        {
            speedModifier = ExplorationManager.Instance.GetExplorationSpeedModifier();
        }

        // =====================================
        // EVENTBUS - Publier l'evenement de progression AVANT le traitement des ticks
        // =====================================
        EventBus.Publish(new ActivityProgressEvent(currentActivityCache, currentVariantCache,
            currentActivityCache.GetProgressToNextTick(currentVariantCache, speedModifier) * 100f));

        // Check for completed ticks (with speed modifier for exploration)
        int completedTicks = currentActivityCache.CalculateCompleteTicks(currentVariantCache, 0, speedModifier);

        if (completedTicks > 0)
        {
            ProcessActivityTicks(completedTicks, currentActivityCache, currentVariantCache);
        }

        // Update progress (thread-safe)
        dataManager.UpdateActivity(currentActivityCache);

        // Mark as dirty for persistence
        ActivityManager.Instance.PersistenceService.MarkDirty();

        if (enableDebugLogs && newSteps > 0)
        {
            float progress = currentActivityCache.GetProgressToNextTick(currentVariantCache, speedModifier);
            Logger.LogInfo($"ActivityManager: Added {newSteps} steps to {currentVariantCache.GetDisplayName()}, Progress: {progress:F2}", Logger.LogCategory.General);
        }
    }

    private void ProcessActivityTicks(int ticksCompleted, ActivityData currentActivityCache, ActivityVariant currentVariantCache)
    {
        if (ticksCompleted <= 0 || currentVariantCache == null) return;

        // Check if this is an exploration activity
        var activityDef = cacheService.GetActivityDefinition(currentActivityCache.ActivityId);
        bool isExploration = activityDef?.ActivityReference?.IsExploration() ?? false;

        if (isExploration)
        {
            // Exploration activities use ExplorationManager for discovery logic instead of item rewards
            ProcessExplorationTicks(ticksCompleted, currentActivityCache, currentVariantCache);
            return;
        }

        // Standard harvesting activity - give item rewards
        int totalRewards = ticksCompleted;
        string resourceId = currentVariantCache.PrimaryResource?.ItemID;

        if (string.IsNullOrEmpty(resourceId))
        {
            Logger.LogError($"ActivityManager: No resource ID found for variant {currentVariantCache.GetDisplayName()}", Logger.LogCategory.General);
            return;
        }

        // Give rewards to inventory
        var inventoryManager = InventoryManager.Instance;
        bool rewardSuccess = inventoryManager.AddItem("player", resourceId, totalRewards);

        if (!rewardSuccess)
        {
            Logger.LogWarning($"ActivityManager: Failed to add {totalRewards} {resourceId} to inventory - inventory full?", Logger.LogCategory.General);
        }

        // Update activity progress (thread-safe)
        currentActivityCache.ProcessTicks(currentVariantCache, ticksCompleted);
        DataManager.Instance.UpdateActivity(currentActivityCache);

        // Mark as dirty for persistence
        ActivityManager.Instance.PersistenceService.MarkDirty();

        // =====================================
        // EVENTBUS - Publier l'evenement de tick
        // =====================================
        EventBus.Publish(new ActivityTickEvent(currentActivityCache, currentVariantCache, ticksCompleted, resourceId));

        // =====================================
        // EVENTBUS - Publier l'evenement de progression APRES le traitement des ticks (pour montrer le reset)
        // Delai leger pour permettre aux animations de completion de s'afficher
        // =====================================
        ActivityManager.Instance.StartCoroutine(PublishProgressEventDelayed(currentActivityCache, currentVariantCache));

        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityManager: Completed {ticksCompleted} ticks, gained {totalRewards} {resourceId}", Logger.LogCategory.General);
        }
    }

    /// <summary>
    /// Process exploration ticks - delegates to ExplorationManager for discovery logic
    /// </summary>
    private void ProcessExplorationTicks(int ticksCompleted, ActivityData currentActivityCache, ActivityVariant currentVariantCache)
    {
        if (ticksCompleted <= 0) return;

        // Process each tick through ExplorationManager
        var explorationManager = ExplorationManager.Instance;
        if (explorationManager == null)
        {
            Logger.LogError("ActivityManager: ExplorationManager not found for exploration activity!", Logger.LogCategory.ActivityLog);
            return;
        }

        // Get the speed modifier from ExplorationManager (accounts for stats, buffs, etc.)
        float speedModifier = explorationManager.GetExplorationSpeedModifier();

        // Process each tick
        for (int i = 0; i < ticksCompleted; i++)
        {
            explorationManager.ProcessExplorationTick();
        }

        // Update activity progress with speed modifier (thread-safe)
        currentActivityCache.ProcessTicks(currentVariantCache, ticksCompleted, speedModifier);
        DataManager.Instance.UpdateActivity(currentActivityCache);

        // Mark as dirty for persistence
        ActivityManager.Instance.PersistenceService.MarkDirty();

        // Publish tick event (with null resource since exploration doesn't give items directly)
        EventBus.Publish(new ActivityTickEvent(currentActivityCache, currentVariantCache, ticksCompleted, null));

        // Publish progress event with delay
        ActivityManager.Instance.StartCoroutine(PublishProgressEventDelayed(currentActivityCache, currentVariantCache));

        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityManager: Completed {ticksCompleted} exploration ticks (speed modifier: {speedModifier:F2})", Logger.LogCategory.ActivityLog);
        }
    }

    /// <summary>
    /// Coroutine pour publier l'evenement de progression avec un leger delai
    /// </summary>
    private static System.Collections.IEnumerator PublishProgressEventDelayed(ActivityData activity, ActivityVariant variant)
    {
        // Attendre un court instant pour que l'animation de completion soit visible
        yield return new WaitForSeconds(0.3f);

        // Publier l'evenement de progression pour montrer le reset
        EventBus.Publish(new ActivityProgressEvent(activity, variant,
            activity.GetProgressToNextTick(variant) * 100f));
    }

    // MODIFIE: ProcessOfflineProgress pour gerer les deux types
    public void ProcessOfflineProgress()
    {
        var dataManager = DataManager.Instance;
        if (!dataManager?.PlayerData?.HasActiveActivity() == true) return;

        var (currentActivityCache, currentVariantCache) = cacheService.GetCurrentActivityInfo();
        if (currentActivityCache == null || currentVariantCache == null) return;

        if (currentActivityCache.IsTimeBased)
        {
            // Deleguer au service temporel
            timeService.ProcessOfflineTimeProgress();
        }
        else
        {
            // Code existant pour les activites basees sur les pas
            ProcessOfflineStepProgress(currentActivityCache, currentVariantCache);
        }
    }

    private void ProcessOfflineStepProgress(ActivityData currentActivityCache, ActivityVariant currentVariantCache)
    {
        // Calculate offline time
        long offlineTimeMs = currentActivityCache.GetElapsedTimeMs();
        if (offlineTimeMs <= 0) return;

        // Calculate steps to process
        long currentTotalSteps = DataManager.Instance.PlayerData.TotalSteps;
        long stepsToProcess = currentTotalSteps - currentActivityCache.LastProcessedTotalSteps;

        if (stepsToProcess > 0)
        {
            Logger.LogInfo($"ActivityManager: Processing {stepsToProcess} offline steps for {currentVariantCache.GetDisplayName()}", Logger.LogCategory.General);
            ProcessNewSteps((int)stepsToProcess);
        }

        if (enableDebugLogs)
        {
            TimeSpan offlineTime = TimeSpan.FromMilliseconds(offlineTimeMs);
            Logger.LogInfo($"ActivityManager: Processed offline progress - Time: {offlineTime.TotalMinutes:F1} minutes", Logger.LogCategory.General);
        }
    }
}

// ===============================================
// Les autres services restent identiques...
// ===============================================

// SERVICE: ActivityPersistenceService (INCHANGE)
public class ActivityPersistenceService
{
    private readonly float autoSaveInterval;
    private bool hasUnsavedProgress = false;
    private Coroutine autoSaveCoroutine;

    public ActivityPersistenceService(float autoSaveInterval)
    {
        this.autoSaveInterval = autoSaveInterval;
    }

    public void Initialize() { }

    public void StartAutoSave()
    {
        if (autoSaveCoroutine == null)
        {
            autoSaveCoroutine = ActivityManager.Instance.StartCoroutine(AutoSaveLoop());
        }
    }

    public void StopAutoSave()
    {
        if (autoSaveCoroutine != null)
        {
            ActivityManager.Instance.StopCoroutine(autoSaveCoroutine);
            autoSaveCoroutine = null;
        }
    }

    private IEnumerator AutoSaveLoop()
    {
        var wait = new WaitForSeconds(autoSaveInterval);
        while (true)
        {
            yield return wait;
            if (hasUnsavedProgress)
            {
                SaveProgress();
            }
        }
    }

    public void MarkDirty()
    {
        hasUnsavedProgress = true;
    }

    public void SaveProgress()
    {
        if (DataManager.Instance != null)
        {
            DataManager.Instance.SaveGame();
            hasUnsavedProgress = false;
        }
    }

    public void ForceSave()
    {
        SaveProgress();
    }

    public void Cleanup()
    {
        StopAutoSave();
        ForceSave();
    }
}

// SERVICE: ActivityCacheService (INCHANGE)
public class ActivityCacheService
{
    private readonly ActivityRegistry activityRegistry;

    public ActivityCacheService(ActivityRegistry activityRegistry)
    {
        this.activityRegistry = activityRegistry;
    }

    public void Initialize() { }

    public LocationActivity GetActivityDefinition(string activityId)
    {
        return activityRegistry?.GetActivity(activityId);
    }

    public ActivityVariant GetActivityVariant(string activityId, string variantId)
    {
        return activityRegistry?.GetActivityVariant(activityId, variantId);
    }

    public (ActivityData, ActivityVariant) GetCurrentActivityInfo()
    {
        var currentActivity = DataManager.Instance?.PlayerData?.CurrentActivity;
        if (currentActivity == null) return (null, null);

        var variant = GetActivityVariant(currentActivity.ActivityId, currentActivity.VariantId);
        return (currentActivity, variant);
    }
}

// SERVICE: ActivityValidationService (INCHANGE)
public class ActivityValidationService
{
    public void Initialize() { }

    public bool ValidateAllDependencies(ActivityRegistry registry)
    {
        return registry != null;
    }

    public bool ValidateActivityStart(string activityId, string variantId)
    {
        return !string.IsNullOrEmpty(activityId) && !string.IsNullOrEmpty(variantId);
    }

    public bool ValidateActivity(LocationActivity activity)
    {
        return activity != null && activity.ActivityReference != null;
    }

    public bool ValidateVariant(ActivityVariant variant)
    {
        return variant != null && variant.IsValidVariant();
    }

    public bool CanStartActivity()
    {
        return DataManager.Instance?.PlayerData != null;
    }
}