// Filepath: Assets/Scripts/Gameplay/StepManager.cs
using System.Collections;
using UnityEngine;

public class StepManager : MonoBehaviour
{
    public static StepManager Instance { get; private set; }

    // Variables exposées à l'UIManager (selon le plan)
    public long CurrentDisplayStepsToday { get; private set; }
    public long CurrentDisplayTotalSteps { get; private set; }

    // Références aux autres services
    private RecordingAPIStepCounter apiCounter;
    private DataManager dataManager;
    private UIManager uiManager; // On garde la référence pour s'assurer qu'il est là

    // Variables internes pour la logique du plan
    private long baseTodayStepsFromAPI;
    private long baseTotalStepsAtOpeningAfterApiSync; // Renommé pour clarté

    private long sensorStartCount = -1;
    private long sensorDeltaThisSession = 0;

    private bool isInitialized = false;
    private bool isAppInForeground = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    IEnumerator Start()
    {
        Logger.LogInfo("StepManager: Start - Initializing...");
        yield return StartCoroutine(WaitForServices());
        if (apiCounter == null || dataManager == null || uiManager == null) // uiManager est vérifié ici
        {
            Logger.LogError("StepManager: Critical services not found. StepManager cannot function.");
            isInitialized = false;
            yield break;
        }
        apiCounter.InitializeService();
        yield return StartCoroutine(HandleAppOpeningOrResuming());
        isInitialized = true;
        Logger.LogInfo("StepManager: Initialization complete.");
    }

    IEnumerator WaitForServices()
    {
        while (RecordingAPIStepCounter.Instance == null || DataManager.Instance == null || UIManager.Instance == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
        apiCounter = RecordingAPIStepCounter.Instance;
        dataManager = DataManager.Instance;
        uiManager = UIManager.Instance; // Récupérer l'instance de UIManager
        Logger.LogInfo("StepManager: All dependent services found.");
    }

    IEnumerator HandleAppOpeningOrResuming()
    {
        Logger.LogInfo("StepManager: HandleAppOpeningOrResuming started.");
        isAppInForeground = true;

        // CurrentDisplayTotalSteps est initialisé avec la valeur de DataManager au début.
        baseTotalStepsAtOpeningAfterApiSync = dataManager.CurrentPlayerData.TotalPlayerSteps; // Stocker la valeur avant le rattrapage API
        long lastSyncEpochMs = dataManager.CurrentPlayerData.LastSyncEpochMs;
        long nowEpochMs = new System.DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeMilliseconds();

        Logger.LogInfo($"StepManager: Loaded state - Initial TotalSteps (from DM): {baseTotalStepsAtOpeningAfterApiSync}, LastSync: {lastSyncEpochMs}");

        if (!apiCounter.HasPermission())
        {
            apiCounter.RequestPermission();
            float permissionWaitTime = 0f;
            while (!apiCounter.HasPermission() && permissionWaitTime < 30f)
            {
                Logger.LogInfo("StepManager: Waiting for permission grant for API operations...");
                yield return new WaitForSeconds(1f);
                permissionWaitTime += 1f;
            }
        }

        if (!apiCounter.HasPermission())
        {
            Logger.LogError("StepManager: Permission still not granted after request. Cannot proceed with API sync.");
            CurrentDisplayStepsToday = 0; // Ou une valeur d'erreur
            CurrentDisplayTotalSteps = baseTotalStepsAtOpeningAfterApiSync; // Afficher le total stocké
            // UIManager mettra à jour l'UI depuis ces propriétés dans son Update()
            yield break;
        }

        apiCounter.SubscribeToRecordingApiIfNeeded();

        Logger.LogInfo("StepManager: Starting API Catch-up (GetDeltaSince).");
        long deltaApiSinceLast = 0;
        yield return StartCoroutine(apiCounter.GetDeltaSinceFromAPI(lastSyncEpochMs, (result) =>
        {
            deltaApiSinceLast = result;
        }));

        if (deltaApiSinceLast < 0) { deltaApiSinceLast = 0; Logger.LogWarning("StepManager: GetDeltaSinceFromAPI returned error, defaulting delta to 0."); }

        // Mettre à jour baseTotalStepsAtOpeningAfterApiSync pour refléter le total après rattrapage
        baseTotalStepsAtOpeningAfterApiSync += deltaApiSinceLast;
        dataManager.CurrentPlayerData.TotalPlayerSteps = baseTotalStepsAtOpeningAfterApiSync;
        dataManager.CurrentPlayerData.LastSyncEpochMs = nowEpochMs;
        dataManager.SaveGame();
        Logger.LogInfo($"StepManager: API Catch-up - Delta: {deltaApiSinceLast}. New TotalSteps in DM: {baseTotalStepsAtOpeningAfterApiSync}. Updated LastSync to: {nowEpochMs}");

        Logger.LogInfo("StepManager: Reading API for BaseTodaySteps (GetDeltaToday).");
        yield return StartCoroutine(apiCounter.GetDeltaTodayFromAPI((result) =>
        {
            baseTodayStepsFromAPI = result;
        }));
        if (baseTodayStepsFromAPI < 0) { baseTodayStepsFromAPI = 0; Logger.LogWarning("StepManager: GetDeltaTodayFromAPI returned error, defaulting baseToday to 0."); }
        Logger.LogInfo($"StepManager: BaseTodaySteps from API: {baseTodayStepsFromAPI}");

        sensorStartCount = -1;
        sensorDeltaThisSession = 0;
        apiCounter.StartDirectSensorListener();
        Logger.LogInfo("StepManager: Direct sensor listener started. sensorStartCount and sensorDeltaThisSession reset.");

        CurrentDisplayStepsToday = baseTodayStepsFromAPI;
        CurrentDisplayTotalSteps = baseTotalStepsAtOpeningAfterApiSync; // Total après rattrapage API, avant delta capteur session
        // UIManager mettra à jour l'UI dans son propre Update()
        Logger.LogInfo($"StepManager: Initial values for UI - StepsToday: {CurrentDisplayStepsToday}, TotalSteps: {CurrentDisplayTotalSteps}");

        StartCoroutine(DirectSensorUpdateLoop());
    }

    IEnumerator DirectSensorUpdateLoop()
    {
        Logger.LogInfo("StepManager: Starting DirectSensorUpdateLoop.");
        while (isAppInForeground)
        {
            yield return new WaitForSeconds(1.0f);

            if (!apiCounter.HasPermission())
            {
                Logger.LogWarning("StepManager: Permission lost during sensor update loop. Stopping sensor.");
                apiCounter.StopDirectSensorListener();
                isAppInForeground = false;
                break;
            }

            long rawSensorValue = apiCounter.GetCurrentRawSensorSteps();

            if (rawSensorValue < 0)
            {
                if (rawSensorValue == -2)
                {
                    Logger.LogError("StepManager: Direct sensor became unavailable! Stopping loop.");
                    apiCounter.StopDirectSensorListener();
                    isAppInForeground = false;
                    break;
                }
                continue;
            }

            if (sensorStartCount == -1)
            {
                sensorStartCount = rawSensorValue;
                sensorDeltaThisSession = 0;
                Logger.LogInfo($"StepManager: First sensor event. sensorStartCount set to: {sensorStartCount}");
            }
            else if (rawSensorValue < sensorStartCount)
            {
                Logger.LogWarning($"StepManager: Sensor reset detected (new: {rawSensorValue} < start: {sensorStartCount}). Adjusting start count.");
                sensorStartCount = rawSensorValue;
                sensorDeltaThisSession = 0;
            }

            if (sensorStartCount != -1)
            {
                long currentTotalSensorStepsThisSession = (rawSensorValue >= sensorStartCount) ? (rawSensorValue - sensorStartCount) : 0;
                long newIndividualSensorSteps = currentTotalSensorStepsThisSession - sensorDeltaThisSession;

                if (newIndividualSensorSteps > 0)
                {
                    sensorDeltaThisSession += newIndividualSensorSteps;
                }
            }

            CurrentDisplayStepsToday = baseTodayStepsFromAPI + sensorDeltaThisSession;
            CurrentDisplayTotalSteps = baseTotalStepsAtOpeningAfterApiSync + sensorDeltaThisSession;
            // UIManager mettra à jour l'UI dans son propre Update()
        }
        Logger.LogInfo("StepManager: Exiting DirectSensorUpdateLoop.");
    }

    void HandleAppPausingOrClosing()
    {
        if (!isInitialized) return;

        Logger.LogInfo("StepManager: HandleAppPausingOrClosing started.");
        isAppInForeground = false;
        // StopCoroutine(DirectSensorUpdateLoop()); // La boucle s'arrêtera à cause de isAppInForeground = false

        apiCounter.StopDirectSensorListener();
        Logger.LogInfo("StepManager: Direct sensor listener stopped.");

        // CurrentDisplayTotalSteps contient déjà baseTotalStepsAtOpeningAfterApiSync + sensorDeltaThisSession
        // C'est la valeur finale à sauvegarder dans DataManager pour TotalPlayerSteps.
        if (sensorDeltaThisSession > 0 || dataManager.CurrentPlayerData.TotalPlayerSteps != CurrentDisplayTotalSteps) // Sauvegarder si le delta capteur a changé ou si le total a changé pour une autre raison
        {
            dataManager.CurrentPlayerData.TotalPlayerSteps = CurrentDisplayTotalSteps;
            Logger.LogInfo($"StepManager: Validating sensorDelta ({sensorDeltaThisSession}). Final TotalSteps to save: {dataManager.CurrentPlayerData.TotalPlayerSteps}");
        }

        dataManager.SaveGame();
        Logger.LogInfo("StepManager: Data saved on pause/close.");
    }

    // Supprimé : void UpdateUIManager()
    // {
    //     // Cette méthode n'est plus nécessaire, UIManager lit les propriétés directement.
    // }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!isInitialized && !pauseStatus) return;
        if (pauseStatus)
        {
            HandleAppPausingOrClosing();
        }
        else
        {
            if (isInitialized)
            {
                StartCoroutine(HandleAppOpeningOrResuming());
            }
        }
    }

    void OnApplicationQuit()
    {
        if (isAppInForeground)
        {
            HandleAppPausingOrClosing();
        }
        Logger.LogInfo("StepManager: Application Quitting.");
    }
}