// Purpose: Manages player activities (mining, gathering, etc.) with step-based progression
// Filepath: Assets/Scripts/Gameplay/Activities/ActivityManager.cs
using System;
using System.Collections;
using UnityEngine;

public class ActivityManager : MonoBehaviour
{
    public static ActivityManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float autoSaveInterval = 30f; // Auto-save every 30 seconds during activity
    [SerializeField] private bool enableDebugLogs = false;

    // References to other managers
    private DataManager dataManager;
    private StepManager stepManager;
    private InventoryManager inventoryManager;
    private MapManager mapManager;


    // Activity registries (to be assigned in inspector)
    [Header("Activity Data")]
    [SerializeField] private ActivityRegistry activityRegistry;

    // Auto-save tracking
    private float timeSinceLastSave = 0f;
    private bool hasUnsavedProgress = false;

    // Current activity cache (for performance)
    private ActivityData currentActivityCache;
    private ActivityVariant currentVariantCache;

    // --- AUTO-SAVE COROUTINE ---
    private Coroutine autoSaveCoroutine;

    private void OnEnable() => autoSaveCoroutine = StartCoroutine(AutoSaveLoop());

    // Events for UI
    public event Action<ActivityData, ActivityVariant> OnActivityStarted;
    public event Action<ActivityData, ActivityVariant> OnActivityStopped;
    public event Action<ActivityData, ActivityVariant, int> OnActivityTick; // activity, variant, ticks completed
    public event Action<ActivityData, ActivityVariant> OnActivityProgress; // for UI updates
    public ActivityRegistry ActivityRegistry => activityRegistry;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Logger.LogWarning("ActivityManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.General);
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Get references to other managers
        dataManager = DataManager.Instance;
        stepManager = StepManager.Instance;
        inventoryManager = InventoryManager.Instance;
        mapManager = MapManager.Instance;

        // Validate dependencies
        if (!ValidateDependencies())
        {
            Logger.LogError("ActivityManager: Critical dependencies missing! ActivityManager will not function properly.", Logger.LogCategory.General);
            return;
        }

        // Subscribe to step updates
        if (stepManager != null)
        {
            // Note: StepManager doesn't have OnStepsUpdated event in the provided code
            // We'll need to check for step changes in Update() instead
            Logger.LogInfo("ActivityManager: Will monitor steps via Update() loop", Logger.LogCategory.General);
        }

        // Process any offline activity progress
        StartCoroutine(ProcessOfflineProgressDelayed());

        Logger.LogInfo("ActivityManager: Initialized successfully", Logger.LogCategory.General);
    }

    void Update()
    {
        // Monitor for step changes and auto-save
        if (HasActiveActivity())
        {
            CheckForStepUpdates();
            HandleAutoSave();
        }
    }

    /// <summary>
    /// Process offline progress after a short delay to ensure all managers are ready
    /// </summary>
    private IEnumerator ProcessOfflineProgressDelayed()
    {
        yield return new WaitForSeconds(1f); // Wait for other managers to initialize

        if (HasActiveActivity())
        {
            ProcessOfflineProgress();
        }
    }

    /// <summary>
    /// Validate that all required dependencies are available
    /// </summary>
    private bool ValidateDependencies()
    {
        bool isValid = true;

        if (dataManager == null)
        {
            Logger.LogError("ActivityManager: DataManager not found!", Logger.LogCategory.General);
            isValid = false;
        }

        if (stepManager == null)
        {
            Logger.LogError("ActivityManager: StepManager not found!", Logger.LogCategory.General);
            isValid = false;
        }

        if (inventoryManager == null)
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

    // === PUBLIC ACTIVITY CONTROL METHODS ===

    /// <summary>
    /// Start a new activity
    /// </summary>
    public bool StartActivity(string activityId, string variantId)
    {
        if (!CanStartActivity())
        {
            Logger.LogWarning($"ActivityManager: Cannot start activity {activityId}/{variantId} - conditions not met", Logger.LogCategory.General);
            return false;
        }

        // Get activity definition and variant
        var activityDef = GetActivityDefinition(activityId);
        var variant = GetActivityVariant(activityId, variantId);

        if (activityDef == null || variant == null)
        {
            Logger.LogError($"ActivityManager: Activity {activityId}/{variantId} not found in registry!", Logger.LogCategory.General);
            return false;
        }

        if (!variant.IsValidVariant())
        {
            Logger.LogError($"ActivityManager: Activity variant {activityId}/{variantId} is not valid!", Logger.LogCategory.General);
            return false;
        }

        if (!activityDef.IsValidActivity())
        {
            Logger.LogError($"ActivityManager: Activity {activityId} is not valid!", Logger.LogCategory.General);
            return false;
        }

        // Get current location and steps
        string currentLocationId = dataManager.PlayerData.CurrentLocationId;
        long currentSteps = dataManager.PlayerData.TotalSteps;

        // Start the activity in PlayerData
        dataManager.PlayerData.StartActivity(activityId, variantId, currentSteps, currentLocationId);

        // Update cache
        RefreshActivityCache();

        // Save immediately
        _ = dataManager.SaveGameAsync();

        // Trigger events
        OnActivityStarted?.Invoke(currentActivityCache, currentVariantCache);

        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityManager: Started activity {variant.GetDisplayName()} at {currentLocationId}", Logger.LogCategory.General);
        }

        return true;
    }

    /// <summary>
    /// Stop the current activity
    /// </summary>
    public bool StopActivity()
    {
        if (!HasActiveActivity())
        {
            Logger.LogWarning("ActivityManager: No active activity to stop", Logger.LogCategory.General);
            return false;
        }

        var activityToStop = currentActivityCache;
        var variantToStop = currentVariantCache;

        // Stop the activity in PlayerData
        dataManager.PlayerData.StopActivity();

        // Clear cache
        ClearActivityCache();

        // Save immediately
        _ = dataManager.SaveGameAsync();

        // Trigger events
        OnActivityStopped?.Invoke(activityToStop, variantToStop);

        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityManager: Stopped activity {variantToStop?.GetDisplayName()}", Logger.LogCategory.General);
        }

        return true;
    }

    /// <summary>
    /// Check if player can start an activity
    /// </summary>
    public bool CanStartActivity()
    {
        if (HasActiveActivity())
        {
            Logger.LogInfo("ActivityManager: Cannot start - already has active activity", Logger.LogCategory.General);
            return false;
        }

        if (dataManager.PlayerData.IsCurrentlyTraveling())
        {
            Logger.LogInfo("ActivityManager: Cannot start - currently traveling", Logger.LogCategory.General);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Check if player has an active activity
    /// </summary>
    public bool HasActiveActivity()
    {
        return dataManager?.PlayerData?.HasActiveActivity() == true;
    }

    /// <summary>
    /// Get current activity info for UI
    /// </summary>
    public (ActivityData activity, ActivityVariant variant) GetCurrentActivityInfo()
    {
        if (!HasActiveActivity())
            return (null, null);

        RefreshActivityCache();
        return (currentActivityCache, currentVariantCache);
    }

    // === STEP PROCESSING ===

    /// <summary>
    /// CORRIGE : Check for step updates and process them
    /// Utilise LastProcessedTotalSteps de l'activite au lieu d'une variable non-sauvegardee
    /// </summary>
    private void CheckForStepUpdates()
    {
        if (!HasActiveActivity() || stepManager == null) return;

        long currentSteps = stepManager.TotalSteps;

        RefreshActivityCache();
        if (currentActivityCache == null) return;

        // Utiliser LastProcessedTotalSteps de l'activite au lieu de lastProcessedSteps local
        long lastProcessed = currentActivityCache.LastProcessedTotalSteps;

        // Check if steps have increased
        if (currentSteps > lastProcessed)
        {
            int newSteps = (int)(currentSteps - lastProcessed);
            ProcessNewSteps(newSteps);
        }
    }

    /// <summary>
    /// Process new steps for the current activity
    /// </summary>
    private void ProcessNewSteps(int newSteps)
    {
        if (!HasActiveActivity() || newSteps <= 0) return;

        RefreshActivityCache();
        if (currentActivityCache == null || currentVariantCache == null) return;

        // Add steps to activity
        currentActivityCache.AddSteps(newSteps);

        // Check for completed ticks
        int completedTicks = currentActivityCache.CalculateCompleteTicks(currentVariantCache, 0);

        if (completedTicks > 0)
        {
            ProcessActivityTicks(completedTicks);
        }

        // Update progress
        dataManager.PlayerData.CurrentActivity = currentActivityCache;
        hasUnsavedProgress = true;

        // Trigger progress event for UI
        OnActivityProgress?.Invoke(currentActivityCache, currentVariantCache);

        if (enableDebugLogs && newSteps > 0)
        {
            float progress = currentActivityCache.GetProgressToNextTick(currentVariantCache);
            Logger.LogInfo($"ActivityManager: Added {newSteps} steps to {currentVariantCache.GetDisplayName()}, Progress: {progress:F2}", Logger.LogCategory.General);
        }
    }

    /// <summary>
    /// Process completed ticks and give rewards
    /// </summary>
    private void ProcessActivityTicks(int ticksCompleted)
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
        bool rewardSuccess = inventoryManager.AddItem("player", resourceId, totalRewards);

        if (!rewardSuccess)
        {
            Logger.LogWarning($"ActivityManager: Failed to add {totalRewards} {resourceId} to inventory - inventory full?", Logger.LogCategory.General);
            // TODO: Handle full inventory (pause activity? drop items?)
        }

        // Update activity progress
        currentActivityCache.ProcessTicks(currentVariantCache, ticksCompleted);
        dataManager.PlayerData.CurrentActivity = currentActivityCache;

        // Trigger tick event
        OnActivityTick?.Invoke(currentActivityCache, currentVariantCache, ticksCompleted);

        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityManager: Completed {ticksCompleted} ticks, gained {totalRewards} {resourceId}", Logger.LogCategory.General);
        }

        hasUnsavedProgress = true;
    }

    /// <summary>
    /// CORRIGE : Process offline activity progress
    /// Utilise LastProcessedTotalSteps pour calculer seulement les pas offline
    /// </summary>
    private void ProcessOfflineProgress()
    {
        if (!HasActiveActivity()) return;

        RefreshActivityCache();
        if (currentActivityCache == null || currentVariantCache == null) return;

        // Calculate offline time
        long offlineTimeMs = currentActivityCache.GetElapsedTimeMs();
        if (offlineTimeMs <= 0) return;

        // CORRIGE : Utiliser LastProcessedTotalSteps au lieu de recalculer depuis StartSteps
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

    // === CACHE MANAGEMENT ===

    /// <summary>
    /// Refresh the activity cache from PlayerData
    /// </summary>
    private void RefreshActivityCache()
    {
        if (!HasActiveActivity())
        {
            ClearActivityCache();
            return;
        }

        currentActivityCache = dataManager.PlayerData.CurrentActivity;
        currentVariantCache = GetActivityVariant(currentActivityCache.ActivityId, currentActivityCache.VariantId);
    }

    /// <summary>
    /// Clear the activity cache
    /// </summary>
    private void ClearActivityCache()
    {
        currentActivityCache = null;
        currentVariantCache = null;
    }

    // === ACTIVITY REGISTRY ACCESS ===

    /// <summary>
    /// Get activity definition from registry
    /// </summary>
    private LocationActivity GetActivityDefinition(string activityId)
    {
        if (activityRegistry == null) return null;
        return activityRegistry.GetActivity(activityId);
    }

    /// <summary>
    /// Get activity variant from registry
    /// </summary>
    private ActivityVariant GetActivityVariant(string activityId, string variantId)
    {
        if (activityRegistry == null) return null;
        return activityRegistry.GetActivityVariant(activityId, variantId);
    }

    // === AUTO-SAVE ===

    /// <summary>
    /// Handle auto-save during activity
    /// </summary>
    private void HandleAutoSave()
    {
        if (!hasUnsavedProgress) return;

        timeSinceLastSave += Time.deltaTime;

        if (timeSinceLastSave >= autoSaveInterval)
        {
            dataManager.SaveGame();
            hasUnsavedProgress = false;
            timeSinceLastSave = 0f;

            if (enableDebugLogs)
            {
                Logger.LogInfo("ActivityManager: Auto-saved activity progress", Logger.LogCategory.General);
            }
        }
    }

    // === INTEGRATION WITH OTHER SYSTEMS ===

    /// <summary>
    /// Check if travel should be blocked due to active activity
    /// </summary>
    public bool ShouldBlockTravel()
    {
        return HasActiveActivity();
    }

    /// <summary>
    /// Get debug info about current activity
    /// </summary>
    public string GetDebugInfo()
    {
        if (!HasActiveActivity())
            return "No active activity";

        RefreshActivityCache();
        if (currentActivityCache == null || currentVariantCache == null)
            return "Activity cache error";

        float progress = currentActivityCache.GetProgressToNextTick(currentVariantCache);
        return $"Activity: {currentVariantCache.GetDisplayName()}\n" +
               $"Progress: {currentActivityCache.AccumulatedSteps}/{currentVariantCache.ActionCost} steps ({progress:P})\n" +
               $"Location: {currentActivityCache.LocationId}\n" +
               $"Elapsed: {TimeSpan.FromMilliseconds(currentActivityCache.GetElapsedTimeMs()).TotalMinutes:F1} min";
    }

    // === CLEANUP ===

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && hasUnsavedProgress)
        {
            dataManager.SaveGame();
            hasUnsavedProgress = false;
        }
    }

    void OnApplicationQuit()
    {
        if (hasUnsavedProgress)
        {
            dataManager.SaveGame();
        }
    }

    void OnDestroy()
    {
        // Clean up any subscriptions if needed
    }

    private void OnDisable()
    {
        if (autoSaveCoroutine != null) StopCoroutine(autoSaveCoroutine);
    }

    private IEnumerator AutoSaveLoop()
    {
        var wait = new WaitForSeconds(autoSaveInterval);   // one GC-free object
        while (true)
        {
            yield return wait;

            if (hasUnsavedProgress)
            {
                _ = dataManager.SaveGameAsync();           // fire-and-forget
                hasUnsavedProgress = false;
            }
        }
    }
}