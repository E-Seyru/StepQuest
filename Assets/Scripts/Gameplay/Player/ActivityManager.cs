// ===============================================
// NOUVEAU ActivityManager (Facade Pattern) - REFACTORED
// ===============================================
// Purpose: Manages player activities (mining, gathering, etc.) with step-based progression
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

    // === INTERNAL SERVICES (NOUVEAU) ===
    private ActivityExecutionService executionService;
    private ActivityProgressService progressService;
    private ActivityValidationService validationService;
    private ActivityCacheService cacheService;
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
        progressService = new ActivityProgressService(cacheService, enableDebugLogs);
        executionService = new ActivityExecutionService(cacheService, progressService, validationService, enableDebugLogs);
        PersistenceService = new ActivityPersistenceService(autoSaveInterval);

        // Wire events from services to public events
        executionService.OnActivityStarted += (activity, variant) => OnActivityStarted?.Invoke(activity, variant);
        executionService.OnActivityStopped += (activity, variant) => OnActivityStopped?.Invoke(activity, variant);
        progressService.OnActivityTick += (activity, variant, ticks) => OnActivityTick?.Invoke(activity, variant, ticks);
        progressService.OnActivityProgress += (activity, variant) => OnActivityProgress?.Invoke(activity, variant);
    }

    void Start()
    {
        // Initialize all services
        validationService.Initialize();
        cacheService.Initialize();
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

        Logger.LogInfo("ActivityManager: Initialized successfully", Logger.LogCategory.General);
    }

    private void ProcessOfflineProgressInvoke()
    {
        progressService.ProcessOfflineProgress();
    }

    void Update()
    {
        // Monitor for step changes only
        if (HasActiveActivity())
        {
            progressService.CheckForStepUpdates();
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

    void OnDestroy()
    {
        PersistenceService?.Cleanup();
    }
}

// ===============================================
// SERVICE: ActivityExecutionService
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

        // Get current location and steps
        var dataManager = DataManager.Instance;
        string currentLocationId = dataManager.PlayerData.CurrentLocationId;
        long currentSteps = dataManager.PlayerData.TotalSteps;

        // Start the activity in PlayerData
        dataManager.PlayerData.StartActivity(activityId, variantId, currentSteps, currentLocationId);

        // Update cache
        cacheService.RefreshActivityCache();

        // Save immediately
        _ = dataManager.SaveGameAsync();

        // Mark as dirty in case save fails
        ActivityManager.Instance.PersistenceService.MarkDirty();

        // Trigger events
        var (activity, variantCache) = cacheService.GetCurrentActivityInfo();
        OnActivityStarted?.Invoke(activity, variantCache);

        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityManager: Started activity {variant.GetDisplayName()} at {currentLocationId}", Logger.LogCategory.General);
        }

        return true;
    }

    public bool StopActivity()
    {
        if (!HasActiveActivity())
        {
            Logger.LogWarning("ActivityManager: No active activity to stop", Logger.LogCategory.General);
            return false;
        }

        var (activityToStop, variantToStop) = cacheService.GetCurrentActivityInfo();

        // Stop the activity in PlayerData
        DataManager.Instance.PlayerData.StopActivity();

        // Clear cache
        cacheService.ClearActivityCache();

        // Save immediately
        _ = DataManager.Instance.SaveGameAsync();

        // Mark as dirty in case save fails
        ActivityManager.Instance.PersistenceService.MarkDirty();

        // Trigger events
        OnActivityStopped?.Invoke(activityToStop, variantToStop);

        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityManager: Stopped activity {variantToStop?.GetDisplayName()}", Logger.LogCategory.General);
        }

        return true;
    }

    public bool CanStartActivity()
    {
        if (HasActiveActivity())
        {
            if (enableDebugLogs)
                Logger.LogInfo("ActivityManager: Cannot start - already has active activity", Logger.LogCategory.General);
            return false;
        }

        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData?.IsCurrentlyTraveling() == true)
        {
            if (enableDebugLogs)
                Logger.LogInfo("ActivityManager: Cannot start - currently traveling", Logger.LogCategory.General);
            return false;
        }

        return true;
    }

    public bool HasActiveActivity()
    {
        return DataManager.Instance?.PlayerData?.HasActiveActivity() == true;
    }

    public (ActivityData activity, ActivityVariant variant) GetCurrentActivityInfo()
    {
        if (!HasActiveActivity())
            return (null, null);

        return cacheService.GetCurrentActivityInfo();
    }

    public string GetDebugInfo()
    {
        if (!HasActiveActivity())
            return "No active activity";

        var (currentActivityCache, currentVariantCache) = cacheService.GetCurrentActivityInfo();
        if (currentActivityCache == null || currentVariantCache == null)
            return "Activity cache error";

        float progress = currentActivityCache.GetProgressToNextTick(currentVariantCache);
        return $"Activity: {currentVariantCache.GetDisplayName()}\n" +
               $"Progress: {currentActivityCache.AccumulatedSteps}/{currentVariantCache.ActionCost} steps ({progress:P})\n" +
               $"Location: {currentActivityCache.LocationId}\n" +
               $"Elapsed: {TimeSpan.FromMilliseconds(currentActivityCache.GetElapsedTimeMs()).TotalMinutes:F1} min";
    }
}

// ===============================================
// SERVICE: ActivityProgressService
// ===============================================
public class ActivityProgressService
{
    private readonly ActivityCacheService cacheService;
    private readonly bool enableDebugLogs;

    public event Action<ActivityData, ActivityVariant, int> OnActivityTick;
    public event Action<ActivityData, ActivityVariant> OnActivityProgress;

    public ActivityProgressService(ActivityCacheService cacheService, bool enableDebugLogs)
    {
        this.cacheService = cacheService;
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

        if (currentActivityCache == null) return;

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
        if (currentActivityCache == null || currentVariantCache == null) return;

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

    public void ProcessOfflineProgress()
    {
        var dataManager = DataManager.Instance;
        if (!dataManager?.PlayerData?.HasActiveActivity() == true) return;

        var (currentActivityCache, currentVariantCache) = cacheService.GetCurrentActivityInfo();
        if (currentActivityCache == null || currentVariantCache == null) return;

        // Calculate offline time
        long offlineTimeMs = currentActivityCache.GetElapsedTimeMs();
        if (offlineTimeMs <= 0) return;

        // Calculate steps to process
        long currentTotalSteps = dataManager.PlayerData.TotalSteps;
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
// SERVICE: ActivityPersistenceService
// ===============================================
public class ActivityPersistenceService
{
    private readonly float autoSaveInterval;
    private bool hasUnsavedProgress = false;
    private Coroutine autoSaveCoroutine;

    public ActivityPersistenceService(float autoSaveInterval)
    {
        this.autoSaveInterval = autoSaveInterval;
    }

    public void Initialize()
    {
        // Service initialization if needed
    }

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

    public void MarkDirty()
    {
        hasUnsavedProgress = true;
    }

    public void ForceSave()
    {
        if (hasUnsavedProgress)
        {
            DataManager.Instance.SaveGame();
            hasUnsavedProgress = false;
        }
    }

    public void Cleanup()
    {
        StopAutoSave();
    }

    private IEnumerator AutoSaveLoop()
    {
        var wait = new WaitForSeconds(autoSaveInterval);
        while (true)
        {
            yield return wait;

            if (hasUnsavedProgress)
            {
                _ = DataManager.Instance.SaveGameAsync();
                hasUnsavedProgress = false;
            }
        }
    }
}

// ===============================================
// SERVICE: ActivityValidationService
// ===============================================
public class ActivityValidationService
{
    public void Initialize()
    {
        // Service initialization if needed
    }

    public bool ValidateAllDependencies(ActivityRegistry activityRegistry)
    {
        bool isValid = true;

        if (DataManager.Instance == null)
        {
            Logger.LogError("ActivityManager: DataManager not found!", Logger.LogCategory.General);
            isValid = false;
        }

        if (StepManager.Instance == null)
        {
            Logger.LogError("ActivityManager: StepManager not found!", Logger.LogCategory.General);
            isValid = false;
        }

        if (InventoryManager.Instance == null)
        {
            Logger.LogError("ActivityManager: InventoryManager not found!", Logger.LogCategory.General);
            isValid = false;
        }

        if (activityRegistry == null)
        {
            Logger.LogError("ActivityManager: ActivityRegistry not assigned in inspector!", Logger.LogCategory.General);
            isValid = false;
        }

        return isValid;
    }

    public bool ValidateVariant(ActivityVariant variant)
    {
        return variant?.IsValidVariant() == true;
    }

    public bool ValidateActivity(LocationActivity activity)
    {
        return activity?.ActivityReference?.IsValidActivity() == true;
    }
}

// ===============================================
// SERVICE: ActivityCacheService
// ===============================================
public class ActivityCacheService
{
    private readonly ActivityRegistry activityRegistry;
    private ActivityData currentActivityCache;
    private ActivityVariant currentVariantCache;

    public ActivityCacheService(ActivityRegistry activityRegistry)
    {
        this.activityRegistry = activityRegistry;
    }

    public void Initialize()
    {
        RefreshActivityCache();
    }

    public void RefreshActivityCache()
    {
        var dataManager = DataManager.Instance;
        if (!dataManager?.PlayerData?.HasActiveActivity() == true)
        {
            ClearActivityCache();
            return;
        }

        currentActivityCache = dataManager.PlayerData.CurrentActivity;
        currentVariantCache = GetActivityVariant(currentActivityCache.ActivityId, currentActivityCache.VariantId);
    }

    public void ClearActivityCache()
    {
        currentActivityCache = null;
        currentVariantCache = null;
    }

    public (ActivityData activity, ActivityVariant variant) GetCurrentActivityInfo()
    {
        RefreshActivityCache();
        return (currentActivityCache, currentVariantCache);
    }

    public LocationActivity GetActivityDefinition(string activityId)
    {
        if (activityRegistry == null) return null;
        return activityRegistry.GetActivity(activityId);
    }

    public ActivityVariant GetActivityVariant(string activityId, string variantId)
    {
        if (activityRegistry == null) return null;
        return activityRegistry.GetActivityVariant(activityId, variantId);
    }
}