// ===============================================
// ActivityManager (Facade Pattern) - EXTENDED WITH TIME-BASED ACTIVITIES
// ===============================================
// Purpose: Manages player activities (mining, gathering, crafting, etc.) with step-based AND time-based progression
// Filepath: Assets/Scripts/Gameplay/Player/ActivityManager.cs

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

    // === PUBLIC EVENTS - SAME AS BEFORE ===
    public event Action<ActivityData, ActivityVariant> OnActivityStarted;
    public event Action<ActivityData, ActivityVariant> OnActivityStopped;
    public event Action<ActivityData, ActivityVariant, int> OnActivityTick;
    public event Action<ActivityData, ActivityVariant> OnActivityProgress;

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
            DontDestroyOnLoad(gameObject);
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

        // Wire events from services to public events
        executionService.OnActivityStarted += (activity, variant) => OnActivityStarted?.Invoke(activity, variant);
        executionService.OnActivityStopped += (activity, variant) => OnActivityStopped?.Invoke(activity, variant);
        progressService.OnActivityTick += (activity, variant, ticks) => OnActivityTick?.Invoke(activity, variant, ticks);
        progressService.OnActivityProgress += (activity, variant) => OnActivityProgress?.Invoke(activity, variant);

        // NOUVEAU : Événements temporels
        timeService.OnTimedActivityCompleted += (activity, variant) => OnActivityTick?.Invoke(activity, variant, 1);
        timeService.OnTimedActivityProgress += (activity, variant) => OnActivityProgress?.Invoke(activity, variant); // MANQUAIT !
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

    // MODIFIE: Update method pour gérer les deux types d'activités
    void Update()
    {
        if (HasActiveActivity())
        {
            var (currentActivity, _) = GetCurrentActivityInfo();
            if (currentActivity != null)
            {
                if (currentActivity.IsTimeBased)
                {
                    // Vérifier les mises à jour de temps
                    timeService.CheckForTimeUpdates();
                }
                else
                {
                    // Vérifier les mises à jour de pas
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
    /// NOUVEAU: Démarre une activité temporelle (crafting)
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

        // Vérifier qu'on a les matériaux pour crafter
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

        // Consommer les matériaux
        if (!variant.ConsumeCraftingMaterials(inventoryManager))
        {
            Logger.LogError($"ActivityManager: Failed to consume materials for {variant.GetDisplayName()}", Logger.LogCategory.General);
            return false;
        }

        // Arrêter l'activité actuelle s'il y en a une
        if (DataManager.Instance.PlayerData.HasActiveActivity())
        {
            StopActivity();
        }

        // Utiliser la localisation actuelle si pas spécifiée
        if (string.IsNullOrEmpty(locationId))
        {
            locationId = DataManager.Instance.PlayerData.CurrentLocationId ?? "unknown";
        }

        // Créer la nouvelle activité temporelle
        var activityData = new ActivityData(activityId, variantId, variant.CraftingTimeMs, locationId, true);

        // Démarrer l'exécution
        bool success = executionService.StartActivity(activityData, variant);

        if (success)
        {
            OnActivityStarted?.Invoke(activityData, variant);

            if (enableDebugLogs)
            {
                Logger.LogInfo($"ActivityManager: Started timed activity {variant.GetDisplayName()} (Duration: {variant.GetCraftingTimeText()})", Logger.LogCategory.General);
            }
        }

        return success;
    }

    /// <summary>
    /// NOUVEAU: Vérifie si on peut démarrer une activité temporelle
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
        PersistenceService?.Cleanup();
    }
}

// ===============================================
// NOUVEAU SERVICE: ActivityTimeService
// ===============================================
public class ActivityTimeService
{
    private readonly ActivityCacheService cacheService;
    private readonly bool enableDebugLogs;

    public event Action<ActivityData, ActivityVariant> OnTimedActivityCompleted;
    public event Action<ActivityData, ActivityVariant> OnTimedActivityProgress;

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
    /// Vérifie et traite les mises à jour temporelles pour les activités time-based
    /// </summary>
    public void CheckForTimeUpdates()
    {
        var dataManager = DataManager.Instance;

        if (!dataManager?.PlayerData?.HasActiveActivity() == true) return;

        var (currentActivityCache, _) = cacheService.GetCurrentActivityInfo();

        if (currentActivityCache == null || !currentActivityCache.IsTimeBased) return;

        // Calculer le temps non-traité
        long unprocessedTimeMs = currentActivityCache.GetUnprocessedTimeMs();

        if (unprocessedTimeMs > 0)
        {
            ProcessNewTime(unprocessedTimeMs);
        }
    }

    /// <summary>
    /// Traite le nouveau temps écoulé pour les activités temporelles
    /// </summary>
    public void ProcessNewTime(long newTimeMs)
    {
        var dataManager = DataManager.Instance;
        if (!dataManager?.PlayerData?.HasActiveActivity() == true || newTimeMs <= 0) return;

        var (currentActivityCache, currentVariantCache) = cacheService.GetCurrentActivityInfo();
        if (currentActivityCache == null || currentVariantCache == null || !currentActivityCache.IsTimeBased) return;

        // Ajouter le temps à l'activité
        currentActivityCache.AddTime(newTimeMs);

        // Vérifier si l'activité est maintenant terminée
        bool isComplete = currentActivityCache.IsComplete();

        if (isComplete)
        {
            ProcessTimedActivityCompletion(currentActivityCache, currentVariantCache);
        }
        else
        {
            // Mettre à jour les données
            dataManager.PlayerData.CurrentActivity = currentActivityCache;

            // Marquer comme dirty pour persistence
            ActivityManager.Instance.PersistenceService.MarkDirty();

            // Déclencher l'événement de progression pour l'UI
            OnTimedActivityProgress?.Invoke(currentActivityCache, currentVariantCache);

            if (enableDebugLogs)
            {
                float progress = currentActivityCache.GetProgressToNextTick(currentVariantCache);
                Logger.LogInfo($"ActivityTimeService: Added {newTimeMs}ms to {currentVariantCache.GetDisplayName()}, Progress: {progress:F2}", Logger.LogCategory.General);
            }
        }
    }

    /// <summary>
    /// Traite la completion d'une activité temporelle (ex: crafting terminé)
    /// </summary>
    private void ProcessTimedActivityCompletion(ActivityData activityCache, ActivityVariant variantCache)
    {
        if (variantCache == null) return;

        // Produire l'item crafté
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

        // Marquer l'activité comme terminée
        activityCache.ProcessTicks(variantCache, 1);

        // Déclencher l'événement de completion
        OnTimedActivityCompleted?.Invoke(activityCache, variantCache);

        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityTimeService: Completed crafting {variantCache.GetDisplayName()}", Logger.LogCategory.General);
        }

        // Vérifier si on peut recommencer automatiquement (boucle)
        if (variantCache.CanCraft(InventoryManager.Instance))
        {
            // Consommer les matériaux et redémarrer
            if (variantCache.ConsumeCraftingMaterials(InventoryManager.Instance))
            {
                // Reset l'activité pour une nouvelle boucle
                activityCache.AccumulatedTimeMs = 0;
                activityCache.LastProcessedTimeMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();


            }
            else
            {
                // Impossible de consommer, arrêter l'activité
                StopTimedActivity(activityCache, variantCache);
            }
        }
        else
        {
            // Plus de matériaux, arrêter l'activité
            StopTimedActivity(activityCache, variantCache);
        }

        // Sauvegarder l'état
        DataManager.Instance.PlayerData.CurrentActivity = activityCache;
        ActivityManager.Instance.PersistenceService.MarkDirty();
    }

    /// <summary>
    /// Arrête une activité temporelle
    /// </summary>
    private void StopTimedActivity(ActivityData activityCache, ActivityVariant variantCache)
    {
        activityCache.Clear();
        DataManager.Instance.PlayerData.CurrentActivity = null;
        ActivityManager.Instance.PersistenceService.MarkDirty();


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



        // Simuler le temps offline par chunks pour gérer les boucles
        long remainingTime = offlineTimeMs;
        int completedCrafts = 0;

        while (remainingTime > 0 && currentActivityCache.IsTimeBased)
        {
            long timeNeeded = currentActivityCache.RequiredTimeMs - currentActivityCache.AccumulatedTimeMs;

            if (remainingTime >= timeNeeded)
            {
                // Compléter ce craft
                remainingTime -= timeNeeded;
                currentActivityCache.AccumulatedTimeMs = currentActivityCache.RequiredTimeMs;
                completedCrafts++;

                // Donner les récompenses
                string resourceId = currentVariantCache.PrimaryResource?.ItemID;
                if (!string.IsNullOrEmpty(resourceId))
                {
                    InventoryManager.Instance.AddItem("player", resourceId, 1);
                }

                // Vérifier si on peut continuer
                if (currentVariantCache.CanCraft(InventoryManager.Instance))
                {
                    currentVariantCache.ConsumeCraftingMaterials(InventoryManager.Instance);
                    currentActivityCache.AccumulatedTimeMs = 0; // Reset pour le prochain craft
                }
                else
                {
                    // Plus de matériaux, arrêter
                    currentActivityCache.Clear();
                    DataManager.Instance.PlayerData.CurrentActivity = null;
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

        // Mettre à jour le timestamp
        currentActivityCache.LastProcessedTimeMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        DataManager.Instance.PlayerData.CurrentActivity = currentActivityCache;
        ActivityManager.Instance.PersistenceService.MarkDirty();

        if (completedCrafts > 0)
        {
            Logger.LogInfo($"ActivityTimeService: Completed {completedCrafts} crafts offline", Logger.LogCategory.General);
        }
    }
}

// ===============================================
// SERVICE: ActivityExecutionService (MODIFIE)
// ===============================================
public class ActivityExecutionService
{
    private readonly ActivityCacheService cacheService;
    private readonly ActivityProgressService progressService;
    private readonly ActivityValidationService validationService;
    private readonly bool enableDebugLogs;

    public event Action<ActivityData, ActivityVariant> OnActivityStarted;
    public event Action<ActivityData, ActivityVariant> OnActivityStopped;

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

    // NOUVEAU: Méthode interne pour démarrer avec ActivityData directe
    internal bool StartActivity(ActivityData activityData, ActivityVariant variant)
    {
        // Save to player data
        DataManager.Instance.PlayerData.CurrentActivity = activityData;

        // Mark for saving
        ActivityManager.Instance.PersistenceService.MarkDirty();

        // Trigger event
        OnActivityStarted?.Invoke(activityData, variant);

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

        // Clear the activity
        DataManager.Instance.PlayerData.CurrentActivity = null;

        // Mark for saving
        ActivityManager.Instance.PersistenceService.MarkDirty();

        // Trigger event
        OnActivityStopped?.Invoke(currentActivity, currentVariant);

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
// SERVICE: ActivityProgressService (MODIFIE)
// ===============================================
public class ActivityProgressService
{
    private readonly ActivityCacheService cacheService;
    private readonly ActivityTimeService timeService;
    private readonly bool enableDebugLogs;

    public event Action<ActivityData, ActivityVariant, int> OnActivityTick;
    public event Action<ActivityData, ActivityVariant> OnActivityProgress;

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

        // Check for completed ticks
        int completedTicks = currentActivityCache.CalculateCompleteTicks(currentVariantCache, 0);

        if (completedTicks > 0)
        {
            ProcessActivityTicks(completedTicks, currentActivityCache, currentVariantCache);
        }

        // Update progress
        dataManager.PlayerData.CurrentActivity = currentActivityCache;

        // Mark as dirty for persistence
        ActivityManager.Instance.PersistenceService.MarkDirty();

        // Trigger progress event for UI
        OnActivityProgress?.Invoke(currentActivityCache, currentVariantCache);

        if (enableDebugLogs && newSteps > 0)
        {
            float progress = currentActivityCache.GetProgressToNextTick(currentVariantCache);
            Logger.LogInfo($"ActivityManager: Added {newSteps} steps to {currentVariantCache.GetDisplayName()}, Progress: {progress:F2}", Logger.LogCategory.General);
        }
    }

    private void ProcessActivityTicks(int ticksCompleted, ActivityData currentActivityCache, ActivityVariant currentVariantCache)
    {
        if (ticksCompleted <= 0 || currentVariantCache == null) return;

        // Calculate rewards
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

        // Update activity progress
        currentActivityCache.ProcessTicks(currentVariantCache, ticksCompleted);
        DataManager.Instance.PlayerData.CurrentActivity = currentActivityCache;

        // Mark as dirty for persistence
        ActivityManager.Instance.PersistenceService.MarkDirty();

        // Trigger tick event
        OnActivityTick?.Invoke(currentActivityCache, currentVariantCache, ticksCompleted);

        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityManager: Completed {ticksCompleted} ticks, gained {totalRewards} {resourceId}", Logger.LogCategory.General);
        }
    }

    // MODIFIE: ProcessOfflineProgress pour gérer les deux types
    public void ProcessOfflineProgress()
    {
        var dataManager = DataManager.Instance;
        if (!dataManager?.PlayerData?.HasActiveActivity() == true) return;

        var (currentActivityCache, currentVariantCache) = cacheService.GetCurrentActivityInfo();
        if (currentActivityCache == null || currentVariantCache == null) return;

        if (currentActivityCache.IsTimeBased)
        {
            // Déléguer au service temporel
            timeService.ProcessOfflineTimeProgress();
        }
        else
        {
            // Code existant pour les activités basées sur les pas
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