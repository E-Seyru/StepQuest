// Filepath: Assets/Scripts/Services/StepTracking/RecordingAPIStepCounter.cs
using System;
using System.Collections;
using UnityEngine;

public class RecordingAPIStepCounter : MonoBehaviour
{
    private AndroidJavaClass stepPluginClass;
    private const string fullPluginClassName = "com.StepQuest.steps.StepPlugin";

    private bool isPluginClassInitialized = false;
    private bool isSubscribedToApi = false;
    private bool lastPermissionResult = false;
    private int permissionCheckCounter = 0;
    private const int LOG_FREQUENCY = GameConstants.ApiLogFrequency;

    private const int MAX_API_READ_ATTEMPTS = GameConstants.MaxApiReadAttempts;
    private const float BASE_API_WAIT_TIME = GameConstants.BaseApiWaitTimeSeconds;

    public static RecordingAPIStepCounter Instance { get; private set; }

    // SIMPLIFIe: Variables pour l'editeur - plus simples
#if UNITY_EDITOR
    private bool editorSensorActive = false;
#endif

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

        }
        else
        {
            Logger.LogWarning("RecordingAPIStepCounter: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.StepLog);
            Destroy(gameObject);
            return;
        }
    }

    public void InitializeService()
    {
        if (isPluginClassInitialized) return;

#if UNITY_EDITOR
        // Mode editeur - simple
        Logger.LogInfo("RecordingAPIStepCounter: Running in Editor mode", Logger.LogCategory.StepLog);
        isPluginClassInitialized = true;
        return;
#else
        Logger.LogInfo("RecordingAPIStepCounter: InitializeService called.", Logger.LogCategory.StepLog);
        try
        {
            stepPluginClass = new AndroidJavaClass(fullPluginClassName);
            isPluginClassInitialized = true;
            Logger.LogInfo($"RecordingAPIStepCounter: {fullPluginClassName} class found successfully.", Logger.LogCategory.StepLog);
        }
        catch (AndroidJavaException e)
        {
            Logger.LogError($"RecordingAPIStepCounter: Failed to initialize AndroidJavaClass for {fullPluginClassName}. Exception: {e}", Logger.LogCategory.StepLog);
            stepPluginClass = null;
            isPluginClassInitialized = false;
        }
#endif
    }

    public void RequestPermission()
    {
#if UNITY_EDITOR
        Logger.LogInfo("RecordingAPIStepCounter: Editor mode - permission automatically granted", Logger.LogCategory.StepLog);
        return;
#else
        if (!isPluginClassInitialized || stepPluginClass == null)
        {
            Logger.LogWarning("RecordingAPIStepCounter: RequestPermission called but plugin class not initialized.", Logger.LogCategory.StepLog);
            return;
        }
        Logger.LogInfo("RecordingAPIStepCounter: Requesting activity recognition permission via plugin.", Logger.LogCategory.StepLog);
        stepPluginClass.CallStatic("requestActivityRecognitionPermission");
#endif
    }

    public bool HasPermission()
    {
#if UNITY_EDITOR
        return true; // Toujours vrai dans l'editeur
#else
        if (!isPluginClassInitialized || stepPluginClass == null)
        {
            Logger.LogWarning("RecordingAPIStepCounter: HasPermission called but plugin class not initialized.", Logger.LogCategory.StepLog);
            return false;
        }

        bool hasPermission = stepPluginClass.CallStatic<bool>("hasActivityRecognitionPermission");

        permissionCheckCounter++;
        if (hasPermission != lastPermissionResult || permissionCheckCounter >= LOG_FREQUENCY)
        {
          
            lastPermissionResult = hasPermission;
        }

        return hasPermission;
#endif
    }

    public void SubscribeToRecordingApiIfNeeded()
    {
#if UNITY_EDITOR
        Logger.LogInfo("RecordingAPIStepCounter: Editor mode - API subscription simulated", Logger.LogCategory.StepLog);
        isSubscribedToApi = true;
        return;
#else
        if (!isPluginClassInitialized || stepPluginClass == null)
        {
            Logger.LogWarning("RecordingAPIStepCounter: SubscribeToRecordingApiIfNeeded called but plugin class not initialized.", Logger.LogCategory.StepLog);
            return;
        }
        if (!HasPermission())
        {
            Logger.LogWarning("RecordingAPIStepCounter: Cannot subscribe, permission not granted.", Logger.LogCategory.StepLog);
            return;
        }
        if (isSubscribedToApi)
        {
            return;
        }

      
        stepPluginClass.CallStatic("subscribeToRecordingAPI");
        isSubscribedToApi = true;
#endif
    }

    public IEnumerator GetDeltaSinceFromAPI(long fromEpochMs, long toEpochMs, System.Action<long> onResultCallback)
    {
#if UNITY_EDITOR
        // Simulation SIMPLE pour l'editeur - retourne toujours 0 pour eviter les conflits
        yield return new WaitForSeconds(0.1f);
        Logger.LogInfo($"RecordingAPIStepCounter: [EDITOR] API call simulated, returning 0 steps", Logger.LogCategory.StepLog);
        onResultCallback?.Invoke(0);
        yield break;
#else
        if (!isPluginClassInitialized || stepPluginClass == null || !HasPermission())
        {
            Logger.LogError("RecordingAPIStepCounter: GetDeltaSinceFromAPI cannot execute - plugin not ready or no permission.", Logger.LogCategory.StepLog);
            onResultCallback?.Invoke(-1);
            yield break;
        }

        if (ShouldSkipRange(fromEpochMs, toEpochMs))
        {
            Logger.LogWarning($"RecordingAPIStepCounter: Skipping already read range from {LocalDatabase.GetReadableDateFromEpoch(fromEpochMs)} to {LocalDatabase.GetReadableDateFromEpoch(toEpochMs)}", Logger.LogCategory.StepLog);
            onResultCallback?.Invoke(0);
            yield break;
        }

        if (fromEpochMs <= 1)
        {
            Logger.LogInfo($"RecordingAPIStepCounter: GetDeltaSinceFromAPI called with very old/initial fromEpochMs={fromEpochMs}. Getting all available history.", Logger.LogCategory.StepLog);
        }
        else if (fromEpochMs >= toEpochMs)
        {
            Logger.LogWarning($"RecordingAPIStepCounter: GetDeltaSinceFromAPI called with fromEpochMs ({LocalDatabase.GetReadableDateFromEpoch(fromEpochMs)}) >= toEpochMs ({LocalDatabase.GetReadableDateFromEpoch(toEpochMs)}). Returning 0 steps.", Logger.LogCategory.StepLog);
            onResultCallback?.Invoke(0);
            yield break;
        }

        Logger.LogInfo($"RecordingAPIStepCounter: Requesting readStepsForTimeRange from plugin for GetDeltaSinceFromAPI (from: {LocalDatabase.GetReadableDateFromEpoch(fromEpochMs)}, to: {LocalDatabase.GetReadableDateFromEpoch(toEpochMs)}).", Logger.LogCategory.StepLog);
        stepPluginClass.CallStatic("readStepsForTimeRange", fromEpochMs, toEpochMs);

        int attempts = 0;
        long lastResult = -1;
        long currentResult = -1;

        while (attempts < MAX_API_READ_ATTEMPTS)
        {
            yield return new WaitForSeconds(BASE_API_WAIT_TIME * (attempts + 1));

            currentResult = stepPluginClass.CallStatic<long>("getStoredStepsForCustomRange");
            Logger.LogInfo($"RecordingAPIStepCounter: API read attempt {attempts + 1}/{MAX_API_READ_ATTEMPTS}, value: {currentResult}", Logger.LogCategory.StepLog);

            if (currentResult >= 0 && (currentResult == lastResult || attempts >= MAX_API_READ_ATTEMPTS - 1))
            {
                Logger.LogInfo($"RecordingAPIStepCounter: GetDeltaSince stable result after {attempts + 1} attempts: {currentResult}", Logger.LogCategory.StepLog);
                break;
            }

            lastResult = currentResult;
            attempts++;
        }

        Logger.LogInfo($"RecordingAPIStepCounter: GetDeltaSinceFromAPI received {currentResult} steps from plugin for time range {LocalDatabase.GetReadableDateFromEpoch(fromEpochMs)} to {LocalDatabase.GetReadableDateFromEpoch(toEpochMs)}.", Logger.LogCategory.StepLog);
        onResultCallback?.Invoke(currentResult >= 0 ? currentResult : 0);

        ClearStoredRange();
#endif
    }

    private bool ShouldSkipRange(long fromEpochMs, long toEpochMs)
    {
#if UNITY_EDITOR
        return false; // Pas de skip dans l'editeur
#else
        try
        {
            string lastRange = PlayerPrefs.GetString("LastReadRange", "");
            if (!string.IsNullOrEmpty(lastRange))
            {
                string[] parts = lastRange.Split(':');
                if (parts.Length == 2)
                {
                    long lastStart = long.Parse(parts[0]);
                    long lastEnd = long.Parse(parts[1]);

                    bool overlaps = (fromEpochMs <= lastEnd && toEpochMs >= lastStart);

                    if (overlaps)
                    {
                        Logger.LogWarning($"RecordingAPIStepCounter: Current range ({LocalDatabase.GetReadableDateFromEpoch(fromEpochMs)} to {LocalDatabase.GetReadableDateFromEpoch(toEpochMs)}) " +
                                         $"overlaps with last recorded range ({LocalDatabase.GetReadableDateFromEpoch(lastStart)} to {LocalDatabase.GetReadableDateFromEpoch(lastEnd)})", Logger.LogCategory.StepLog);
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"RecordingAPIStepCounter: Error checking last range: {ex.Message}", Logger.LogCategory.StepLog);
        }

        return false;
#endif
    }

    public void ClearStoredRange()
    {
#if UNITY_EDITOR
        Logger.LogInfo("RecordingAPIStepCounter: [EDITOR] ClearStoredRange called", Logger.LogCategory.StepLog);
        return;
#else
        if (!isPluginClassInitialized || stepPluginClass == null)
        {
            Logger.LogWarning("RecordingAPIStepCounter: ClearStoredRange called but plugin class not initialized.", Logger.LogCategory.StepLog);
            return;
        }

        bool clearSuccess = false;

        try
        {
            clearSuccess = stepPluginClass.CallStatic<bool>("clearStoredStepsForCustomRange");

            if (!clearSuccess)
            {
          
                HandleClearFailure();
                Logger.LogWarning("RecordingAPIStepCounter: Plugin reported failure to clear stored range. Using fallback mechanism.", Logger.LogCategory.StepLog);
            }
        }
        catch (AndroidJavaException ex)
        {
            Logger.LogError($"RecordingAPIStepCounter: Failed to clear stored range in plugin. Exception: {ex.Message}", Logger.LogCategory.StepLog);
            HandleClearFailure();
            Logger.LogWarning("RecordingAPIStepCounter: clearStoredStepsForCustomRange function may not be available. Using fallback mechanism.", Logger.LogCategory.StepLog);
        }
#endif
    }

    private void HandleClearFailure()
    {
        try
        {
            if (lastReadStartMs > 0 && lastReadEndMs > 0)
            {
                PlayerPrefs.SetString("LastReadRange", $"{lastReadStartMs}:{lastReadEndMs}");
                PlayerPrefs.Save();
                Logger.LogInfo($"RecordingAPIStepCounter: Saved last read range {LocalDatabase.GetReadableDateFromEpoch(lastReadStartMs)} to {LocalDatabase.GetReadableDateFromEpoch(lastReadEndMs)} to PlayerPrefs as fallback.", Logger.LogCategory.StepLog);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"RecordingAPIStepCounter: Failed to save fallback data: {ex.Message}", Logger.LogCategory.StepLog);
        }
    }

    private long lastReadStartMs = 0;
    private long lastReadEndMs = 0;

    public IEnumerator GetDeltaSinceFromAPI(long fromEpochMs, System.Action<long> onResultCallback)
    {
        long nowEpochMs = GetLocalEpochMs();
        return GetDeltaSinceFromAPI(fromEpochMs, nowEpochMs, onResultCallback);
    }

    private long GetLocalEpochMs()
    {
        return new System.DateTimeOffset(System.DateTime.Now).ToUnixTimeMilliseconds();
    }

    public void StartDirectSensorListener()
    {
#if UNITY_EDITOR
        Logger.LogInfo("RecordingAPIStepCounter: [EDITOR] Direct sensor started", Logger.LogCategory.StepLog);
        editorSensorActive = true;
        return;
#else
        if (!isPluginClassInitialized || stepPluginClass == null || !HasPermission())
        {
            Logger.LogWarning("RecordingAPIStepCounter: StartDirectSensorListener - plugin not ready or no permission.", Logger.LogCategory.StepLog);
            return;
        }
        
        stepPluginClass.CallStatic("startDirectStepCounterListener");
#endif
    }

    public void StopDirectSensorListener()
    {
#if UNITY_EDITOR
        Logger.LogInfo("RecordingAPIStepCounter: [EDITOR] Direct sensor stopped", Logger.LogCategory.StepLog);
        editorSensorActive = false;
        return;
#else
        if (!isPluginClassInitialized || stepPluginClass == null)
        {
            Logger.LogWarning("RecordingAPIStepCounter: StopDirectSensorListener - plugin class not initialized.", Logger.LogCategory.StepLog);
            return;
        }
      
        stepPluginClass.CallStatic("stopDirectStepCounterListener");
#endif
    }

    public long GetCurrentRawSensorSteps()
    {
#if UNITY_EDITOR
        // Dans l'editeur, retourne -1 pour desactiver le systeme de capteur direct
        // Les pas seront geres uniquement par EditorStepSimulator
        return -1;
#else
        if (!isPluginClassInitialized || stepPluginClass == null || !HasPermission())
        {
            return -1;
        }
        return stepPluginClass.CallStatic<long>("getCurrentRawSensorSteps");
#endif
    }
}