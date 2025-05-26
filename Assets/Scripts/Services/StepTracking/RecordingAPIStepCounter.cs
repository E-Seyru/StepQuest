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
    private const int LOG_FREQUENCY = 60;

    private const int MAX_API_READ_ATTEMPTS = 5;
    private const float BASE_API_WAIT_TIME = 0.5f;

    public static RecordingAPIStepCounter Instance { get; private set; }

    // NOUVEAU: Variables pour la simulation dans l'éditeur
#if UNITY_EDITOR
    private long editorSimulatedSteps = 0;
    private long editorLastSensorValue = 0;
    private bool editorSensorActive = false;
    private System.Random editorRandom = new System.Random();
#endif

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        // Mode éditeur - simulation
        Logger.LogInfo("RecordingAPIStepCounter: Running in Editor mode - using simulation", Logger.LogCategory.StepLog);
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
        return true; // Toujours vrai dans l'éditeur
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
            if (hasPermission != lastPermissionResult)
            {
                Logger.LogInfo($"RecordingAPIStepCounter: Permission status changed to: {hasPermission}", Logger.LogCategory.StepLog);
            }
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

        Logger.LogInfo("RecordingAPIStepCounter: Attempting to subscribe to API Recording.", Logger.LogCategory.StepLog);
        stepPluginClass.CallStatic("subscribeToRecordingAPI");
        isSubscribedToApi = true;
#endif
    }

    public IEnumerator GetDeltaSinceFromAPI(long fromEpochMs, long toEpochMs, System.Action<long> onResultCallback)
    {
#if UNITY_EDITOR
        // Simulation pour l'éditeur
        yield return StartCoroutine(SimulateAPIRead(fromEpochMs, toEpochMs, onResultCallback));
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

#if UNITY_EDITOR
    // NOUVEAU: Simulation de lecture API pour l'éditeur
    private IEnumerator SimulateAPIRead(long fromEpochMs, long toEpochMs, System.Action<long> onResultCallback)
    {
        // Attendre un peu pour simuler le délai réseau
        yield return new WaitForSeconds(0.2f + editorRandom.Next(0, 3) * 0.1f);

        // Calculer une simulation basée sur la durée
        long durationMs = toEpochMs - fromEpochMs;
        long durationHours = durationMs / (1000 * 60 * 60);

        // Simuler des pas basés sur la durée (plus c'est long, plus il y a de pas)
        long simulatedSteps = 0;

        if (durationHours <= 0)
        {
            simulatedSteps = editorRandom.Next(0, 5); // Très courte période
        }
        else if (durationHours <= 1)
        {
            simulatedSteps = editorRandom.Next(50, 200); // Une heure
        }
        else if (durationHours <= 8)
        {
            simulatedSteps = editorRandom.Next(200, 1000); // Journée de travail
        }
        else
        {
            simulatedSteps = editorRandom.Next(1000, 3000); // Journée complète
        }

        // Ajouter un peu de variabilité
        simulatedSteps += editorRandom.Next(-50, 50);
        simulatedSteps = Math.Max(0, simulatedSteps);

        Logger.LogInfo($"RecordingAPIStepCounter: [EDITOR SIMULATION] Simulated {simulatedSteps} steps for {durationHours}h period", Logger.LogCategory.StepLog);

        onResultCallback?.Invoke(simulatedSteps);
    }
#endif

    private bool ShouldSkipRange(long fromEpochMs, long toEpochMs)
    {
#if UNITY_EDITOR
        return false; // Pas de skip dans l'éditeur pour simplifier
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
        Logger.LogInfo("RecordingAPIStepCounter: [EDITOR] ClearStoredRange called - simulated success", Logger.LogCategory.StepLog);
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

            if (clearSuccess)
            {
                Logger.LogInfo("RecordingAPIStepCounter: Successfully cleared stored range in plugin.", Logger.LogCategory.StepLog);
            }
            else
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
        Logger.LogInfo("RecordingAPIStepCounter: [EDITOR] Starting direct sensor simulation", Logger.LogCategory.StepLog);
        editorSensorActive = true;
        editorLastSensorValue = editorRandom.Next(10000, 50000); // Valeur de départ aléatoire
        return;
#else
        if (!isPluginClassInitialized || stepPluginClass == null || !HasPermission())
        {
            Logger.LogWarning("RecordingAPIStepCounter: StartDirectSensorListener - plugin not ready or no permission.", Logger.LogCategory.StepLog);
            return;
        }
        Logger.LogInfo("RecordingAPIStepCounter: Requesting plugin to start direct step counter listener.", Logger.LogCategory.StepLog);
        stepPluginClass.CallStatic("startDirectStepCounterListener");
#endif
    }

    public void StopDirectSensorListener()
    {
#if UNITY_EDITOR
        Logger.LogInfo("RecordingAPIStepCounter: [EDITOR] Stopping direct sensor simulation", Logger.LogCategory.StepLog);
        editorSensorActive = false;
        return;
#else
        if (!isPluginClassInitialized || stepPluginClass == null)
        {
            Logger.LogWarning("RecordingAPIStepCounter: StopDirectSensorListener - plugin class not initialized.", Logger.LogCategory.StepLog);
            return;
        }
        Logger.LogInfo("RecordingAPIStepCounter: Requesting plugin to stop direct step counter listener.", Logger.LogCategory.StepLog);
        stepPluginClass.CallStatic("stopDirectStepCounterListener");
#endif
    }

    public long GetCurrentRawSensorSteps()
    {
#if UNITY_EDITOR
        if (!editorSensorActive) return -1;

        // Simuler une augmentation graduelle des pas
        if (UnityEngine.Random.Range(0f, 1f) < 0.1f) // 10% de chance d'augmenter
        {
            editorLastSensorValue += UnityEngine.Random.Range(1, 5);
        }

        return editorLastSensorValue;
#else
        if (!isPluginClassInitialized || stepPluginClass == null || !HasPermission())
        {
            return -1;
        }
        return stepPluginClass.CallStatic<long>("getCurrentRawSensorSteps");
#endif
    }
}