// Filepath: Assets/Scripts/Gameplay/StepManager.cs
using System;
using System.Collections;
using UnityEngine;

public class StepManager : MonoBehaviour
{
    public static StepManager Instance { get; private set; }

    // Variables pour compter les pas
    public long TotalSteps { get; private set; }
    public long DailySteps { get; private set; }

    // Références aux autres services
    private RecordingAPIStepCounter apiCounter;
    private DataManager dataManager;
    private UIManager uiManager;

    // Variables internes
    private long sensorStartCount = -1;
    private long sensorDeltaThisSession = 0;

    private bool isInitialized = false;
    private bool isAppInForeground = true;

    // Paramètres de débounce et filtrage
    private const int SENSOR_SPIKE_THRESHOLD = 50; // Nombre de pas considéré comme anormal en une seule mise à jour
    private const int SENSOR_DEBOUNCE_SECONDS = 3; // Temps minimum entre deux grandes variations
    private float lastLargeUpdateTime = 0f;
    private const long MAX_STEPS_PER_UPDATE = 1000; // Limite raisonnable pour une sauvegarde unique

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Logger.LogWarning("StepManager: Multiple instances detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    IEnumerator Start()
    {
        Logger.LogInfo("StepManager: Start - Initializing...");
        yield return StartCoroutine(WaitForServices());
        if (apiCounter == null || dataManager == null || uiManager == null)
        {
            Logger.LogError("StepManager: Critical services not found. StepManager cannot function.");
            isInitialized = false;
            yield break;
        }
        apiCounter.InitializeService();

        // Attendre un peu pour s'assurer que le DataManager ait correctement chargé les données
        yield return new WaitForSeconds(1.0f);

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
        uiManager = UIManager.Instance;
        Logger.LogInfo("StepManager: All dependent services found.");
    }

    IEnumerator HandleAppOpeningOrResuming()
    {
        Logger.LogInfo("StepManager: HandleAppOpeningOrResuming started.");
        isAppInForeground = true;

        // IMPORTANT: NE PAS appeler dataManager.CheckAndResetDailySteps() car nous allons le faire nous-mêmes

        // Charger les données actuelles
        TotalSteps = dataManager.PlayerData.TotalSteps;
        DailySteps = dataManager.PlayerData.DailySteps;
        long lastSyncEpochMs = dataManager.PlayerData.LastSyncEpochMs;
        long lastPauseEpochMs = dataManager.PlayerData.LastPauseEpochMs;
        long nowEpochMs = new System.DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeMilliseconds();

        // Vérifier explicitement si le jour a changé
        string currentDateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string lastResetDateStr = dataManager.PlayerData.LastDailyResetDate;
        bool isDayChanged = string.IsNullOrEmpty(lastResetDateStr) || lastResetDateStr != currentDateStr;

        if (isDayChanged)
        {
            // Log explicite pour confirmer que cette branche est exécutée
            Logger.LogInfo($"StepManager: **** DAY CHANGE DETECTED **** Last reset date: {lastResetDateStr}, Current date: {currentDateStr}");

            // Réinitialiser clairement les pas quotidiens
            DailySteps = 0;
            dataManager.PlayerData.DailySteps = 0;
            dataManager.PlayerData.LastDailyResetDate = currentDateStr;

            // Sauvegarder immédiatement pour s'assurer que le changement est persisté
            dataManager.SaveGame();

            // Log de confirmation
            Logger.LogInfo($"StepManager: Daily steps reset to 0 for new day {currentDateStr}. Saved to database.");
        }
        else
        {
            Logger.LogInfo($"StepManager: Same day detected. LastResetDate: {lastResetDateStr}, Today: {currentDateStr}");
        }

        // Convertir les timestamps en dates lisibles
        string lastSyncDate = LocalDatabase.GetReadableDateFromEpoch(lastSyncEpochMs);
        string lastPauseDate = LocalDatabase.GetReadableDateFromEpoch(lastPauseEpochMs);
        string nowDate = LocalDatabase.GetReadableDateFromEpoch(nowEpochMs);

        Logger.LogInfo($"StepManager: Loaded state - Initial TotalSteps: {TotalSteps}, DailySteps: {DailySteps}, " +
                      $"LastSync: {lastSyncEpochMs} ({lastSyncDate}), " +
                      $"LastPause: {lastPauseEpochMs} ({lastPauseDate}), " +
                      $"Now: {nowEpochMs} ({nowDate})");

        // Vérification des permissions requise AVANT TOUT !
        if (!apiCounter.HasPermission())
        {
            Logger.LogInfo("StepManager: Permission not granted. Requesting permission...");
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
            Logger.LogError("StepManager: Permission still not granted after request. Cannot proceed with API sync or sensor.");
            yield break;
        }

        // S'abonner à l'API de comptage
        apiCounter.SubscribeToRecordingApiIfNeeded();

        // Vérifier si c'est le premier démarrage réel (pas enregistrés = 0 ET LastSync = 0)
        bool isFirstRun = TotalSteps == 0 && lastSyncEpochMs == 0;

        // Si c'est le premier démarrage, on initialise uniquement le timestamp
        if (isFirstRun)
        {
            Logger.LogInfo("StepManager: First app startup detected (TotalSteps=0 AND LastSync=0). Initializing LastSync timestamp without API catch-up.");
            dataManager.PlayerData.LastSyncEpochMs = nowEpochMs;
            dataManager.PlayerData.LastPauseEpochMs = nowEpochMs;
            dataManager.SaveGame();
            Logger.LogInfo($"StepManager: API Catch-up skipped for first start. TotalSteps: {TotalSteps}. Updated LastSync to: {nowEpochMs} ({LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)})");
        }
        else
        {
            // S'assurer que LastSync a une valeur valide
            if (lastSyncEpochMs <= 0)
            {
                // Si on a des pas mais pas de timestamp valide, on utilise une date récente (24h avant)
                Logger.LogInfo("StepManager: LastSync value invalid (0) but TotalSteps > 0. Setting LastSync to 24h ago.");
                lastSyncEpochMs = nowEpochMs - (24 * 60 * 60 * 1000); // 24h en millisecondes
            }

            // CORRECTION: Utiliser LastPauseEpochMs comme fin de période si disponible
            long startTimeMs = lastSyncEpochMs;
            // Utiliser LastPauseEpochMs au lieu de nowEpochMs pour éviter le double comptage
            long endTimeMs = (lastPauseEpochMs > startTimeMs) ? lastPauseEpochMs : nowEpochMs;

            // Convertir en dates lisibles
            string startDate = LocalDatabase.GetReadableDateFromEpoch(startTimeMs);
            string endDate = LocalDatabase.GetReadableDateFromEpoch(endTimeMs);

            // Ne récupérer les pas que s'il y a un intervalle de temps valide
            if (endTimeMs > startTimeMs)
            {
                // Récupérer les pas depuis la dernière synchronisation jusqu'à la dernière pause
                // avec gestion spéciale en cas de chevauchement de minuit
                Logger.LogInfo($"StepManager: Starting API Catch-up from {startTimeMs} ({startDate}) to {endTimeMs} ({endDate})");

                yield return StartCoroutine(HandleMidnightSplitStepCount(startTimeMs, endTimeMs));
            }
            else
            {
                Logger.LogInfo("StepManager: API Catch-up skipped - no valid time interval available or app was just paused very recently.");
            }

            // Toujours mettre à jour LastSyncEpochMs au temps actuel
            dataManager.PlayerData.LastSyncEpochMs = nowEpochMs;
            dataManager.SaveGame();
            Logger.LogInfo($"StepManager: Updated LastSync to: {nowEpochMs} ({LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)})");
        }

        // Initialiser correctement sensorStartCount avec la valeur actuelle du capteur pour éviter de recompter les pas déjà récupérés par l'API
        long currentSensorValue = apiCounter.GetCurrentRawSensorSteps();
        if (currentSensorValue > 0)
        {
            sensorStartCount = currentSensorValue;
            sensorDeltaThisSession = 0;
            Logger.LogInfo($"StepManager: sensorStartCount initialized to {sensorStartCount} after API sync");
        }
        else
        {
            // Réinitialiser le compteur de capteur pour la nouvelle session
            sensorStartCount = -1;
            sensorDeltaThisSession = 0;
            Logger.LogInfo("StepManager: Sensor values reset for new session.");
        }

        // Démarrer l'écoute directe du capteur pour les pas en temps réel
        apiCounter.StartDirectSensorListener();
        Logger.LogInfo("StepManager: Direct sensor listener started.");

        StartCoroutine(DirectSensorUpdateLoop());
    }

    // Nouvelle méthode pour mettre à jour le timestamp du capteur direct
    private void UpdateLastDirectSensorTimestamp()
    {
        // Enregistrer le timestamp actuel comme dernier point de synchronisation
        long nowEpochMs = new System.DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeMilliseconds();
        dataManager.PlayerData.LastSyncEpochMs = nowEpochMs;
        Logger.LogInfo($"StepManager: Updated LastSyncEpochMs to current time after direct sensor update: {nowEpochMs} ({LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)})");
    }

    // Méthode pour gérer le chevauchement de minuit
    private IEnumerator HandleMidnightSplitStepCount(long startTimeMs, long endTimeMs)
    {
        // Vérifier si l'intervalle chevauche minuit
        bool spansMidnight = DoesIntervalSpanMidnight(startTimeMs, endTimeMs);

        if (!spansMidnight)
        {
            // Cas simple : pas de chevauchement de minuit
            long deltaApiSinceLast = 0;
            yield return StartCoroutine(apiCounter.GetDeltaSinceFromAPI(startTimeMs, endTimeMs, (result) =>
            {
                deltaApiSinceLast = result;
            }));

            // Vérifier les limites
            if (deltaApiSinceLast < 0)
            {
                deltaApiSinceLast = 0;
                Logger.LogWarning("StepManager: GetDeltaSinceFromAPI returned error, defaulting delta to 0.");
            }
            else if (deltaApiSinceLast > MAX_STEPS_PER_UPDATE)
            {
                Logger.LogWarning($"StepManager: API returned {deltaApiSinceLast} steps, which exceeds threshold. Limiting to {MAX_STEPS_PER_UPDATE}.");
                deltaApiSinceLast = MAX_STEPS_PER_UPDATE;
            }

            // Mettre à jour les deux compteurs normalement
            TotalSteps += deltaApiSinceLast;
            DailySteps += deltaApiSinceLast;

            Logger.LogInfo($"StepManager: API Catch-up - Delta: {deltaApiSinceLast}. New TotalSteps: {TotalSteps}, DailySteps: {DailySteps}");
        }
        else
        {
            // Cas avec chevauchement de minuit : diviser en deux requêtes

            // Force explicitement la réinitialisation du compteur quotidien
            string currentDateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
            Logger.LogInfo($"StepManager: **CRITICAL** Midnight boundary detected, forcing DailySteps reset. Setting LastDailyResetDate to {currentDateStr}");

            // Réinitialisation explicite
            DailySteps = 0;
            dataManager.PlayerData.DailySteps = 0;
            dataManager.PlayerData.LastDailyResetDate = currentDateStr;

            // 1. Trouver le timestamp de minuit entre les deux timestamps
            long midnightMs = FindMidnightTimestamp(startTimeMs, endTimeMs);
            string midnightDate = LocalDatabase.GetReadableDateFromEpoch(midnightMs);
            Logger.LogInfo($"StepManager: Interval spans midnight at {midnightMs} ({midnightDate}). Splitting step counts.");

            // 2. Récupérer les pas de la période avant minuit (jour précédent)
            long stepsBeforeMidnight = 0;
            yield return StartCoroutine(apiCounter.GetDeltaSinceFromAPI(startTimeMs, midnightMs, (result) =>
            {
                stepsBeforeMidnight = result;
            }));

            // Vérifier les limites
            if (stepsBeforeMidnight < 0)
            {
                stepsBeforeMidnight = 0;
                Logger.LogWarning("StepManager: GetDeltaSinceFromAPI (before midnight) returned error, defaulting to 0.");
            }
            else if (stepsBeforeMidnight > MAX_STEPS_PER_UPDATE)
            {
                Logger.LogWarning($"StepManager: API returned {stepsBeforeMidnight} steps before midnight, which exceeds threshold. Limiting to {MAX_STEPS_PER_UPDATE}.");
                stepsBeforeMidnight = MAX_STEPS_PER_UPDATE;
            }

            // 3. Récupérer les pas de la période après minuit (jour courant)
            long stepsAfterMidnight = 0;
            yield return StartCoroutine(apiCounter.GetDeltaSinceFromAPI(midnightMs, endTimeMs, (result) =>
            {
                stepsAfterMidnight = result;
            }));

            // Vérifier les limites
            if (stepsAfterMidnight < 0)
            {
                stepsAfterMidnight = 0;
                Logger.LogWarning("StepManager: GetDeltaSinceFromAPI (after midnight) returned error, defaulting to 0.");
            }
            else if (stepsAfterMidnight > MAX_STEPS_PER_UPDATE)
            {
                Logger.LogWarning($"StepManager: API returned {stepsAfterMidnight} steps after midnight, which exceeds threshold. Limiting to {MAX_STEPS_PER_UPDATE}.");
                stepsAfterMidnight = MAX_STEPS_PER_UPDATE;
            }

            // 4. Mettre à jour les compteurs
            TotalSteps += (stepsBeforeMidnight + stepsAfterMidnight); // Ajouter tous les pas au total
            DailySteps += stepsAfterMidnight; // N'ajouter que les pas après minuit au compteur journalier

            Logger.LogInfo($"StepManager: API Catch-up - Steps before midnight: {stepsBeforeMidnight}, " +
                          $"Steps after midnight: {stepsAfterMidnight}. " +
                          $"New TotalSteps: {TotalSteps}, DailySteps: {DailySteps}");
        }

        // Sauvegarder les données mises à jour
        dataManager.PlayerData.TotalSteps = TotalSteps;
        dataManager.PlayerData.DailySteps = DailySteps;
    }

    // Vérifier si l'intervalle chevauche minuit
    private bool DoesIntervalSpanMidnight(long startTimeMs, long endTimeMs)
    {
        DateTime startDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(startTimeMs).ToLocalTime().Date;
        DateTime endDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(endTimeMs).ToLocalTime().Date;
        return startDate != endDate;
    }

    // Trouver le timestamp du minuit entre les deux timestamps
    private long FindMidnightTimestamp(long startTimeMs, long endTimeMs)
    {
        DateTime startDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(startTimeMs).ToLocalTime();
        DateTime nextMidnight = startDateTime.Date.AddDays(1); // Minuit le jour suivant
        return new DateTimeOffset(nextMidnight.ToUniversalTime()).ToUnixTimeMilliseconds();
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
                // Amélioration de la détection des réinitialisations capteur
                // Distinguer entre réinitialisation réelle et erreur temporaire
                if (sensorStartCount - rawSensorValue > 1000)
                {
                    // Si la valeur est significativement plus petite, c'est probablement une réinitialisation
                    Logger.LogInfo($"StepManager: Sensor full reset detected. Old={sensorStartCount}, New={rawSensorValue}");
                    sensorStartCount = rawSensorValue;
                    sensorDeltaThisSession = 0;
                }
                else
                {
                    // Petite diminution, potentiellement une erreur de lecture
                    Logger.LogWarning($"StepManager: Unexpected sensor value decrease from {sensorStartCount} to {rawSensorValue}. Ignoring.");
                    // Ne pas modifier sensorStartCount, juste ignorer cette valeur
                }
                continue;
            }

            if (sensorStartCount != -1)
            {
                long currentTotalSensorStepsThisSession = (rawSensorValue >= sensorStartCount) ? (rawSensorValue - sensorStartCount) : 0;
                long newIndividualSensorSteps = currentTotalSensorStepsThisSession - sensorDeltaThisSession;

                if (newIndividualSensorSteps > 0)
                {
                    // Filtrer les pics anormaux avec débounce
                    if (newIndividualSensorSteps > SENSOR_SPIKE_THRESHOLD)
                    {
                        float currentTime = Time.time;
                        if (currentTime - lastLargeUpdateTime < SENSOR_DEBOUNCE_SECONDS)
                        {
                            Logger.LogWarning($"StepManager: Anomaly detected! {newIndividualSensorSteps} steps in quick succession filtered out.");
                            newIndividualSensorSteps = 0; // Ignorer cette mise à jour
                        }
                        else
                        {
                            lastLargeUpdateTime = currentTime;
                            Logger.LogInfo($"StepManager: Large step update of {newIndividualSensorSteps} accepted after debounce check.");
                        }
                    }

                    // Mettre à jour les pas seulement si la valeur filtrée est valide
                    if (newIndividualSensorSteps > 0)
                    {
                        // Vérifier si l'incrément reste dans des limites raisonnables
                        if (newIndividualSensorSteps > MAX_STEPS_PER_UPDATE)
                        {
                            Logger.LogWarning($"StepManager: Unusually large step increment detected: {newIndividualSensorSteps}. Limiting to {MAX_STEPS_PER_UPDATE}.");
                            newIndividualSensorSteps = MAX_STEPS_PER_UPDATE;
                        }

                        // Vérifier si nous avons changé de jour au cours de la session
                        string currentDateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
                        if (dataManager.PlayerData.LastDailyResetDate != currentDateStr)
                        {
                            Logger.LogInfo($"StepManager: Day change detected during step update. Resetting daily steps to 0 and updating LastDailyResetDate.");
                            DailySteps = 0;
                            dataManager.PlayerData.DailySteps = 0;
                            dataManager.PlayerData.LastDailyResetDate = currentDateStr;
                        }

                        sensorDeltaThisSession += newIndividualSensorSteps;
                        TotalSteps += newIndividualSensorSteps;
                        DailySteps += newIndividualSensorSteps;
                        Logger.LogInfo($"StepManager: New steps: {newIndividualSensorSteps}, TotalSteps: {TotalSteps}, DailySteps: {DailySteps}");

                        // Sauvegarde périodique pour assurer que les pas sont enregistrés même en cas de crash
                        dataManager.PlayerData.TotalSteps = TotalSteps;
                        dataManager.PlayerData.DailySteps = DailySteps;

                        // NOUVELLE LIGNE: Mettre à jour le timestamp de dernière synchronisation
                        UpdateLastDirectSensorTimestamp();

                        dataManager.SaveGame();
                    }
                }
            }
        }
        Logger.LogInfo("StepManager: Exiting DirectSensorUpdateLoop.");
    }

    void HandleAppPausingOrClosing()
    {
        if (!isInitialized) return;

        Logger.LogInfo("StepManager: HandleAppPausingOrClosing started.");
        isAppInForeground = false;

        apiCounter.StopDirectSensorListener();
        Logger.LogInfo("StepManager: Direct sensor listener stopped.");

        // Enregistrer le timestamp de pause pour résoudre le problème de double comptage
        long nowEpochMs = new System.DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeMilliseconds();
        string nowDate = LocalDatabase.GetReadableDateFromEpoch(nowEpochMs);
        dataManager.PlayerData.LastSyncEpochMs = nowEpochMs;
        dataManager.PlayerData.LastPauseEpochMs = nowEpochMs;

        // Force une sauvegarde à chaque pause/fermeture pour s'assurer que les données sont persistées
        dataManager.PlayerData.TotalSteps = TotalSteps;
        dataManager.PlayerData.DailySteps = DailySteps;
        Logger.LogInfo($"StepManager: Saving steps. Final TotalSteps: {TotalSteps}, DailySteps: {DailySteps}, " +
                       $"LastPauseEpochMs: {nowEpochMs} ({nowDate})");
        dataManager.SaveGame();
        Logger.LogInfo("StepManager: Data saved on pause/close.");
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!isInitialized && !pauseStatus) return;
        if (pauseStatus)
        {
            Logger.LogInfo("StepManager: OnApplicationPause → app goes to background");
            HandleAppPausingOrClosing();
        }
        else
        {
            Logger.LogInfo("StepManager: OnApplicationPause → app returns to foreground");
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