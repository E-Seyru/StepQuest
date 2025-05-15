// Filepath: Assets/Scripts/Services/StepTracking/RecordingAPIStepCounter.cs
using System.Collections;
using UnityEngine;

public class RecordingAPIStepCounter : MonoBehaviour
{
    private AndroidJavaClass stepPluginClass;
    private const string fullPluginClassName = "com.StepQuest.steps.StepPlugin";

    private bool isPluginClassInitialized = false;
    private bool isSubscribedToApi = false;

    // Constantes pour le mécanisme d'attente amélioré
    private const int MAX_API_READ_ATTEMPTS = 5;
    private const float BASE_API_WAIT_TIME = 0.5f;

    public static RecordingAPIStepCounter Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Logger.LogWarning("RecordingAPIStepCounter: Multiple instances detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    public void InitializeService()
    {
        if (isPluginClassInitialized) return;

        Logger.LogInfo("RecordingAPIStepCounter: InitializeService called.");
        try
        {
            stepPluginClass = new AndroidJavaClass(fullPluginClassName);
            isPluginClassInitialized = true;
            Logger.LogInfo($"RecordingAPIStepCounter: {fullPluginClassName} class found successfully.");
        }
        catch (AndroidJavaException e)
        {
            Logger.LogError($"RecordingAPIStepCounter: Failed to initialize AndroidJavaClass for {fullPluginClassName}. Exception: {e}");
            stepPluginClass = null;
            isPluginClassInitialized = false;
        }
    }

    // Gestion des Permissions
    public void RequestPermission()
    {
        if (!isPluginClassInitialized || stepPluginClass == null)
        {
            Logger.LogWarning("RecordingAPIStepCounter: RequestPermission called but plugin class not initialized.");
            return;
        }
        Logger.LogInfo("RecordingAPIStepCounter: Requesting activity recognition permission via plugin.");
        stepPluginClass.CallStatic("requestActivityRecognitionPermission");
    }

    public bool HasPermission()
    {
        if (!isPluginClassInitialized || stepPluginClass == null)
        {
            Logger.LogWarning("RecordingAPIStepCounter: HasPermission called but plugin class not initialized.");
            return false;
        }

        bool hasPermission = stepPluginClass.CallStatic<bool>("hasActivityRecognitionPermission");
        Logger.LogInfo($"RecordingAPIStepCounter: Permission check result: {hasPermission}");
        return hasPermission;
    }

    // Gestion de l'Abonnement API Recording
    public void SubscribeToRecordingApiIfNeeded()
    {
        if (!isPluginClassInitialized || stepPluginClass == null)
        {
            Logger.LogWarning("RecordingAPIStepCounter: SubscribeToRecordingApiIfNeeded called but plugin class not initialized.");
            return;
        }
        if (!HasPermission())
        {
            Logger.LogWarning("RecordingAPIStepCounter: Cannot subscribe, permission not granted.");
            return;
        }
        if (isSubscribedToApi)
        {
            return;
        }

        Logger.LogInfo("RecordingAPIStepCounter: Attempting to subscribe to API Recording.");
        stepPluginClass.CallStatic("subscribeToRecordingAPI");
        isSubscribedToApi = true;
    }

    // Lecture des données entre deux timestamps spécifiques avec mécanisme d'attente amélioré
    public IEnumerator GetDeltaSinceFromAPI(long fromEpochMs, long toEpochMs, System.Action<long> onResultCallback)
    {
        if (!isPluginClassInitialized || stepPluginClass == null || !HasPermission())
        {
            Logger.LogError("RecordingAPIStepCounter: GetDeltaSinceFromAPI cannot execute - plugin not ready or no permission.");
            onResultCallback?.Invoke(-1);
            yield break;
        }

        // Vérifier et gérer le cas spécial où fromEpochMs est 0 ou très ancien
        if (fromEpochMs <= 1)
        {
            Logger.LogInfo($"RecordingAPIStepCounter: GetDeltaSinceFromAPI called with very old/initial fromEpochMs={fromEpochMs}. Getting all available history.");
            // Le plugin StepPlugin va gérer ce cas spécial
        }
        else if (fromEpochMs >= toEpochMs)
        {
            Logger.LogWarning($"RecordingAPIStepCounter: GetDeltaSinceFromAPI called with fromEpochMs ({fromEpochMs}) >= toEpochMs ({toEpochMs}). Returning 0 steps.");
            onResultCallback?.Invoke(0);
            yield break;
        }

        Logger.LogInfo($"RecordingAPIStepCounter: Requesting readStepsForTimeRange from plugin for GetDeltaSinceFromAPI (from: {fromEpochMs}, to: {toEpochMs}).");
        stepPluginClass.CallStatic("readStepsForTimeRange", fromEpochMs, toEpochMs);

        // Mécanisme d'attente amélioré avec vérification
        int attempts = 0;
        long lastResult = -1;
        long currentResult = -1;

        while (attempts < MAX_API_READ_ATTEMPTS)
        {
            // Attente progressive qui augmente avec les tentatives
            yield return new WaitForSeconds(BASE_API_WAIT_TIME * (attempts + 1));

            currentResult = stepPluginClass.CallStatic<long>("getStoredStepsForCustomRange");
            Logger.LogInfo($"RecordingAPIStepCounter: API read attempt {attempts + 1}/{MAX_API_READ_ATTEMPTS}, value: {currentResult}");

            // Si on a une valeur valide et qu'elle est stable (deux lectures identiques), on peut sortir
            if (currentResult >= 0 && (currentResult == lastResult || attempts >= MAX_API_READ_ATTEMPTS - 1))
            {
                Logger.LogInfo($"RecordingAPIStepCounter: GetDeltaSince stable result after {attempts + 1} attempts: {currentResult}");
                break;
            }

            lastResult = currentResult;
            attempts++;
        }

        Logger.LogInfo($"RecordingAPIStepCounter: GetDeltaSinceFromAPI received {currentResult} steps from plugin for time range {fromEpochMs} to {toEpochMs}.");
        onResultCallback?.Invoke(currentResult >= 0 ? currentResult : 0);
    }

    // Maintient l'ancienne méthode pour compatibilité, mais utilise la nouvelle en interne
    public IEnumerator GetDeltaSinceFromAPI(long fromEpochMs, System.Action<long> onResultCallback)
    {
        long nowEpochMs = new System.DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeMilliseconds();
        return GetDeltaSinceFromAPI(fromEpochMs, nowEpochMs, onResultCallback);
    }

    // Méthodes pour le capteur direct
    public void StartDirectSensorListener()
    {
        if (!isPluginClassInitialized || stepPluginClass == null || !HasPermission())
        {
            Logger.LogWarning("RecordingAPIStepCounter: StartDirectSensorListener - plugin not ready or no permission.");
            return;
        }
        Logger.LogInfo("RecordingAPIStepCounter: Requesting plugin to start direct step counter listener.");
        stepPluginClass.CallStatic("startDirectStepCounterListener");
    }

    public void StopDirectSensorListener()
    {
        if (!isPluginClassInitialized || stepPluginClass == null)
        {
            Logger.LogWarning("RecordingAPIStepCounter: StopDirectSensorListener - plugin class not initialized.");
            return;
        }
        Logger.LogInfo("RecordingAPIStepCounter: Requesting plugin to stop direct step counter listener.");
        stepPluginClass.CallStatic("stopDirectStepCounterListener");
    }

    public long GetCurrentRawSensorSteps()
    {
        if (!isPluginClassInitialized || stepPluginClass == null || !HasPermission())
        {
            return -1;
        }
        return stepPluginClass.CallStatic<long>("getCurrentRawSensorSteps");
    }
}