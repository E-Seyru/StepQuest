// Filepath: Assets/Scripts/Services/StepTracking/RecordingAPIStepCounter.cs
using System.Collections;
using UnityEngine;

public class RecordingAPIStepCounter : MonoBehaviour
{
    private AndroidJavaClass stepPluginClass;
    private const string fullPluginClassName = "com.StepQuest.steps.StepPlugin"; // Assurez-vous que c'est le bon package

    private bool isPluginClassInitialized = false;
    private bool isSubscribedToApi = false; // Pour suivre l'état de l'abonnement

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
            Destroy(gameObject);
            return;
        }
    }

    // Cette méthode sera appelée par StepManager au démarrage
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

    // --- Gestion des Permissions ---
    public bool HasPermission()
    {
        if (!isPluginClassInitialized || stepPluginClass == null)
        {
            Logger.LogWarning("RecordingAPIStepCounter: HasPermission called but plugin class not initialized.");
            return false;
        }
        return stepPluginClass.CallStatic<bool>("hasActivityRecognitionPermission");
    }

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

    // --- Gestion de l'Abonnement API Recording ---
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
            // Logger.LogInfo("RecordingAPIStepCounter: Already subscribed to API Recording."); // Peut être verbeux
            return;
        }

        Logger.LogInfo("RecordingAPIStepCounter: Attempting to subscribe to API Recording.");
        stepPluginClass.CallStatic("subscribeToRecordingAPI");
        isSubscribedToApi = true; // On suppose que l'appel initie l'abonnement.
                                  // La réussite réelle est loggée par le plugin Java.
    }


    // --- Lecture des Données API Recording ---

    // Coroutine pour GetDeltaToday()
    // Le StepManager démarrera cette coroutine et attendra le callback.
    public IEnumerator GetDeltaTodayFromAPI(System.Action<long> onResultCallback)
    {
        if (!isPluginClassInitialized || stepPluginClass == null || !HasPermission())
        {
            Logger.LogError("RecordingAPIStepCounter: GetDeltaTodayFromAPI cannot execute - plugin not ready or no permission.");
            onResultCallback?.Invoke(-1); // Indiquer une erreur ou pas de données
            yield break;
        }

        Logger.LogInfo("RecordingAPIStepCounter: Requesting readTodaysStepData from plugin for GetDeltaTodayFromAPI.");
        stepPluginClass.CallStatic("readTodaysStepData");

        // Attendre que le plugin Java ait le temps de lire et de stocker la valeur
        // Ce délai doit être suffisant pour que l'appel asynchrone en Java se termine.
        yield return new WaitForSeconds(1.5f); // Ajustez ce délai si nécessaire

        long steps = stepPluginClass.CallStatic<long>("getStoredStepsForToday");
        Logger.LogInfo($"RecordingAPIStepCounter: GetDeltaTodayFromAPI received {steps} steps from plugin.");
        onResultCallback?.Invoke(steps);
    }

    // Coroutine pour GetDeltaSince(fromEpochMs)
    // Le StepManager démarrera cette coroutine et attendra le callback.
    public IEnumerator GetDeltaSinceFromAPI(long fromEpochMs, System.Action<long> onResultCallback)
    {
        if (!isPluginClassInitialized || stepPluginClass == null || !HasPermission())
        {
            Logger.LogError("RecordingAPIStepCounter: GetDeltaSinceFromAPI cannot execute - plugin not ready or no permission.");
            onResultCallback?.Invoke(-1); // Indiquer une erreur ou pas de données
            yield break;
        }

        long nowEpochMs = new System.DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeMilliseconds();
        if (fromEpochMs == 0)
        {
            Logger.LogInfo($"RecordingAPIStepCounter: GetDeltaSinceFromAPI called with fromEpochMs=0. Will use today's delta via plugin.");
            // Le plugin Java gère fromEpochMs=0 en lisant les pas du jour et en les stockant dans la variable customRange.
        }
        else if (fromEpochMs >= nowEpochMs)
        {
            Logger.LogWarning($"RecordingAPIStepCounter: GetDeltaSinceFromAPI called with fromEpochMs ({fromEpochMs}) >= nowEpochMs ({nowEpochMs}). Returning 0 steps.");
            onResultCallback?.Invoke(0);
            yield break;
        }


        Logger.LogInfo($"RecordingAPIStepCounter: Requesting readStepsForTimeRange from plugin for GetDeltaSinceFromAPI (from: {fromEpochMs}, to: {nowEpochMs}).");
        stepPluginClass.CallStatic("readStepsForTimeRange", fromEpochMs, nowEpochMs);

        yield return new WaitForSeconds(1.5f); // Ajustez ce délai si nécessaire

        long steps = stepPluginClass.CallStatic<long>("getStoredStepsForCustomRange");
        Logger.LogInfo($"RecordingAPIStepCounter: GetDeltaSinceFromAPI received {steps} steps from plugin for custom range.");
        onResultCallback?.Invoke(steps);
    }


    // --- Méthodes pour le Capteur Direct (seront appelées par StepManager) ---
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
            // Pas besoin de vérifier la permission pour arrêter, mais le plugin doit être initialisé
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
            // Logger.LogWarning("RecordingAPIStepCounter: GetCurrentRawSensorSteps - plugin not ready or no permission.");
            return -1; // Ou une autre valeur d'erreur que StepManager peut interpréter
        }
        return stepPluginClass.CallStatic<long>("getCurrentRawSensorSteps");
    }
}