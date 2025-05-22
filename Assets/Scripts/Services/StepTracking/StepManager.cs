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
    private long lastRecordedSensorValue = -1;

    // --- Boolean Flags for State Management ---
    // isInitialized: True after Start() completes initialization (services ready, initial data load/sync).
    // Prevents OnApplicationPause actions before setup.
    private bool isInitialized = false;

    // isAppInForeground: True if the app has focus. Controls DirectSensorUpdateLoop.
    // Set true on resume, false on pause or if sensor permission is lost.
    private bool isAppInForeground = true;

    // isReturningFromBackground: Transient flag. True when app resumes via OnApplicationPause(false).
    // Used by OrchestrateAppOpeningOrResuming to trigger specific resume logic (e.g., grace period), then reset.
    private bool isReturningFromBackground = false;

    // isSensorStartCountValid: True if sensorStartCount has a valid initial reading from the direct sensor.
    // Checked in DirectSensorUpdateLoop. Set by InitializeDirectSensor. Reset on resume.
    private bool isSensorStartCountValid = false;

    // inSensorGracePeriod: True during SENSOR_GRACE_PERIOD after sensor init or app resume.
    // Sensor readings update sensorStartCount but don't count as steps, preventing step jumps.
    // Managed by HandleSensorGracePeriod and InitializeDirectSensor.
    private bool inSensorGracePeriod = false;

    // wasProbablyCrash: True if lastPauseEpochMs < lastSyncEpochMs during data load, suggesting an unclean shutdown.
    // Used in PerformApiCatchUp to adjust API fetch start time to potentially recover missed steps. Reset on resume.
    private bool wasProbablyCrash = false;

    // Paramètres de débounce et filtrage
    private const int SENSOR_SPIKE_THRESHOLD = 50; // Nombre de pas considéré comme anormal en une seule mise à jour
    private const int SENSOR_DEBOUNCE_SECONDS = 3; // Temps minimum entre deux grandes variations
    private float lastLargeUpdateTime = 0f;
    private const long MAX_STEPS_PER_UPDATE = 100000; // Limite raisonnable pour une sauvegarde unique

    // Période de grâce après le retour au premier plan (en secondes)
    private const float SENSOR_GRACE_PERIOD = 5.0f; // Augmenté de 2s à 5s (Faille D)
    private float sensorGraceTimer = 0f;
    private bool inSensorGracePeriod = false;

    // Nouveau: paramètre pour contrôler la fréquence des sauvegardes DB
    private const float DB_SAVE_INTERVAL = 3.0f; // Réduit à 3 secondes pour limiter la perte de données en cas de crash
    private float lastDBSaveTime = 0f;

    // Nouveau: timestamp dédié au dernier catch-up API (Faille A)
    private long lastApiCatchUpEpochMs = 0;

    // Nouveau: détection de crash
    private bool wasProbablyCrash = false;

    // NOUVELLE CONSTANTE: décalage en ms pour éviter le chevauchement à minuit
    private const long MIDNIGHT_SAFETY_MS = 500;

    // NOUVELLE CONSTANTE: padding pour éviter le chevauchement exact entre capteur direct et API
    private const long SENSOR_API_PADDING_MS = 1500;   // 1 seconde

    // NOUVELLE VARIABLE: pour vérifier et éviter les dédoublements
    private string lastMidnightSplitKey = "";

    void Awake()
    {
        // Protection contre les instances multiples (Faille E)
        if (Instance != null && Instance != this)
        {
            Logger.LogWarning("StepManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.General);
            Destroy(gameObject);
            return;
        }

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    IEnumerator Start()
    {
        Logger.LogInfo("StepManager: Start - Initializing...", Logger.LogCategory.General);
        yield return StartCoroutine(WaitForServices());
        if (apiCounter == null || dataManager == null || uiManager == null)
        {
            Logger.LogError("StepManager: Critical services not found. StepManager cannot function.", Logger.LogCategory.General);
            isInitialized = false;
            yield break;
        }
        apiCounter.InitializeService();

        // Attendre un peu pour s'assurer que le DataManager ait correctement chargé les données
        yield return new WaitForSeconds(1.0f);

        // Refactored: Call the new main orchestrator for app opening/resuming
        yield return StartCoroutine(OrchestrateAppOpeningOrResuming());
        isInitialized = true;
        Logger.LogInfo("StepManager: Initialization complete.", Logger.LogCategory.General);
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
        Logger.LogInfo("StepManager: All dependent services found.", Logger.LogCategory.General);
    }

    // Renamed from HandleAppOpeningOrResuming to better reflect its new role as an orchestrator
    private IEnumerator OrchestrateAppOpeningOrResuming()
    {
        Logger.LogInfo("StepManager: OrchestrateAppOpeningOrResuming started.", Logger.LogCategory.StepLog);
        isAppInForeground = true; // Set app state

        long nowEpochMs = GetCurrentEpochMs();
        long lastSyncEpochMs, lastPauseEpochMs;

        // Stage 1: Load initial data and handle day changes
        LoadInitialDataAndHandleDayChange(out lastSyncEpochMs, out lastPauseEpochMs);
        Logger.LogInfo($"StepManager: Loaded state - Initial TotalSteps: {TotalSteps}, DailySteps: {DailySteps}, " +
                      $"LastSync: {LocalDatabase.GetReadableDateFromEpoch(lastSyncEpochMs)}, " +
                      $"LastPause: {LocalDatabase.GetReadableDateFromEpoch(lastPauseEpochMs)}, " +
                      $"LastApiCatchUp: {LocalDatabase.GetReadableDateFromEpoch(lastApiCatchUpEpochMs)}, " +
                      $"Now: {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)}", Logger.LogCategory.StepLog);

        // Stage 2: Handle API permissions
        bool permissionGranted = yield return StartCoroutine(RequestApiPermission());
        if (!permissionGranted)
        {
            Logger.LogError("StepManager: API permission not granted. Cannot proceed.", Logger.LogCategory.General);
            yield break; // Exit if permission is not granted
        }
        apiCounter.SubscribeToRecordingApiIfNeeded();

        // Stage 3: Perform API catch-up if not first run
        bool isFirstRun = TotalSteps == 0 && lastSyncEpochMs == 0;
        if (isFirstRun)
        {
            HandleFirstRunInitialization(nowEpochMs);
        }
        else
        {
            yield return StartCoroutine(PerformApiCatchUp(nowEpochMs, lastSyncEpochMs, lastPauseEpochMs));
        }

        // Stage 4: Handle returning from background state and initialize direct sensor
        HandleReturningFromBackgroundState();
        yield return StartCoroutine(InitializeDirectSensorAndStartLoop());
    }

    /// <summary>
    /// Loads initial step data from DataManager, checks for day changes,
    /// and detects potential crashes.
    /// </summary>
    private void LoadInitialDataAndHandleDayChange(out long lastSyncEpochMs, out long lastPauseEpochMs)
    {
        TotalSteps = dataManager.PlayerData.TotalSteps;
        DailySteps = dataManager.PlayerData.DailySteps;
        lastSyncEpochMs = dataManager.PlayerData.LastSyncEpochMs;
        lastPauseEpochMs = dataManager.PlayerData.LastPauseEpochMs;

        // Load lastApiCatchUpEpochMs from PlayerData
        lastApiCatchUpEpochMs = dataManager.PlayerData.LastApiCatchUpEpochMs;
        Logger.LogInfo($"StepManager: lastApiCatchUpEpochMs loaded from database: {LocalDatabase.GetReadableDateFromEpoch(lastApiCatchUpEpochMs)}", Logger.LogCategory.StepLog);

        // Crash detection
        if (lastPauseEpochMs < lastSyncEpochMs)
        {
            wasProbablyCrash = true;
            Logger.LogWarning($"StepManager: Probable crash detected! LastPause ({LocalDatabase.GetReadableDateFromEpoch(lastPauseEpochMs)}) < LastSync ({LocalDatabase.GetReadableDateFromEpoch(lastSyncEpochMs)})", Logger.LogCategory.StepLog);
        }

        // Check for day change
        string currentDateStr = GetLocalDateString();
        string lastResetDateStr = dataManager.PlayerData.LastDailyResetDate;
        bool isDayChanged = string.IsNullOrEmpty(lastResetDateStr) || lastResetDateStr != currentDateStr;

        if (isDayChanged)
        {
            Logger.LogInfo($"StepManager: **** DAY CHANGE DETECTED **** Last reset date: {lastResetDateStr}, Current date: {currentDateStr}", Logger.LogCategory.StepLog);
            DailySteps = 0;
            dataManager.PlayerData.DailySteps = 0;
            dataManager.PlayerData.LastDailyResetDate = currentDateStr;
            dataManager.SaveGame();
            Logger.LogInfo($"StepManager: Daily steps reset to 0 for new day {currentDateStr}. Saved to database.", Logger.LogCategory.StepLog);
        }
        else
        {
            Logger.LogInfo($"StepManager: Same day detected. LastResetDate: {lastResetDateStr}, Today: {currentDateStr}", Logger.LogCategory.StepLog);
        }
    }

    /// <summary>
    /// Handles the process of requesting step counting API permission.
    /// Returns true if permission is granted, false otherwise.
    /// </summary>
    private IEnumerator RequestApiPermission()
    {
        if (apiCounter.HasPermission())
        {
            yield return true; // Permission already granted
            yield break;
        }

        Logger.LogInfo("StepManager: Permission not granted. Requesting permission...", Logger.LogCategory.General);
        apiCounter.RequestPermission();
        float permissionWaitTime = 0f;
        while (!apiCounter.HasPermission() && permissionWaitTime < 30f)
        {
            Logger.LogInfo("StepManager: Waiting for permission grant for API operations...", Logger.LogCategory.General);
            yield return new WaitForSeconds(1f);
            permissionWaitTime += 1f;
        }

        if (!apiCounter.HasPermission())
        {
            Logger.LogError("StepManager: Permission still not granted after request.", Logger.LogCategory.General);
            yield return false;
        }
        else
        {
            Logger.LogInfo("StepManager: Permission granted.", Logger.LogCategory.General);
            yield return true;
        }
    }

    /// <summary>
    /// Initializes timestamps for the very first application run.
    /// </summary>
    private void HandleFirstRunInitialization(long nowEpochMs)
    {
        Logger.LogInfo("StepManager: First app startup detected (TotalSteps=0 AND LastSync=0). Initializing LastSync timestamp without API catch-up.", Logger.LogCategory.StepLog);
        dataManager.PlayerData.LastSyncEpochMs = nowEpochMs;
        dataManager.PlayerData.LastPauseEpochMs = nowEpochMs;
        lastApiCatchUpEpochMs = nowEpochMs; // Initialize also lastApiCatchUpEpochMs
        dataManager.SaveGame();
        Logger.LogInfo($"StepManager: API Catch-up skipped for first start. TotalSteps: {TotalSteps}. Updated LastSync to: {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)}", Logger.LogCategory.StepLog);
    }

    /// <summary>
    /// Performs the API catch-up logic to synchronize steps.
    /// </summary>
    private IEnumerator PerformApiCatchUp(long nowEpochMs, long lastSyncEpochMs, long lastPauseEpochMs)
    {
        // Ensure LastApiCatchUpEpochMs has a valid value
        if (lastApiCatchUpEpochMs <= 0 && TotalSteps > 0) // Check TotalSteps > 0 to avoid setting for a truly new user post-reset/reinstall with no steps
        {
            Logger.LogInfo("StepManager: LastApiCatchUpEpochMs value invalid (0 or negative) but TotalSteps > 0. Setting to 24h ago.", Logger.LogCategory.StepLog);
            lastApiCatchUpEpochMs = nowEpochMs - (24 * 60 * 60 * 1000); // 24h in milliseconds
        }
        else if (lastApiCatchUpEpochMs <= 0)
        {
             Logger.LogInfo("StepManager: LastApiCatchUpEpochMs value invalid (0 or negative) and TotalSteps is 0. Setting to nowEpochMs.", Logger.LogCategory.StepLog);
            lastApiCatchUpEpochMs = nowEpochMs; // If no steps and invalid, just align it to now.
        }


        long startTimeMs = wasProbablyCrash ? lastPauseEpochMs : lastApiCatchUpEpochMs;
        long endTimeMs = nowEpochMs;

        // Adjust startTimeMs if direct sensor data might overlap
        if (!wasProbablyCrash && lastSyncEpochMs >= startTimeMs)
        {
            startTimeMs = lastSyncEpochMs + SENSOR_API_PADDING_MS;
            Logger.LogInfo($"StepManager: Small overlap detected – API catch-up start time adjusted to {LocalDatabase.GetReadableDateFromEpoch(startTimeMs)}", Logger.LogCategory.StepLog);
        }

        if (endTimeMs > startTimeMs)
        {
            if (isReturningFromBackground)
            {
                Logger.LogInfo($"StepManager: RETURNING FROM BACKGROUND - Starting API Catch-up from {LocalDatabase.GetReadableDateFromEpoch(startTimeMs)} to {LocalDatabase.GetReadableDateFromEpoch(endTimeMs)}", Logger.LogCategory.StepLog);
            }
            else
            {
                Logger.LogInfo($"StepManager: Starting API Catch-up from {LocalDatabase.GetReadableDateFromEpoch(startTimeMs)} to {LocalDatabase.GetReadableDateFromEpoch(endTimeMs)}", Logger.LogCategory.StepLog);
            }

            yield return StartCoroutine(HandleMidnightSplitStepCount(startTimeMs, endTimeMs));

            lastApiCatchUpEpochMs = nowEpochMs;
            dataManager.PlayerData.LastApiCatchUpEpochMs = lastApiCatchUpEpochMs;
            Logger.LogInfo($"StepManager: Updated lastApiCatchUpEpochMs to: {LocalDatabase.GetReadableDateFromEpoch(lastApiCatchUpEpochMs)}", Logger.LogCategory.StepLog);

            apiCounter.ClearStoredRange();
            dataManager.SaveGame(); // Save immediately after successful catch-up and timestamp update
            Logger.LogInfo("StepManager: Saved LastApiCatchUpEpochMs immediately after API catch-up.", Logger.LogCategory.StepLog);
        }
        else
        {
            Logger.LogInfo($"StepManager: API catch-up skipped – no valid interval. Start: {LocalDatabase.GetReadableDateFromEpoch(startTimeMs)}, End: {LocalDatabase.GetReadableDateFromEpoch(endTimeMs)}", Logger.LogCategory.StepLog);
        }

        // Always update LastSyncEpochMs and LastPauseEpochMs to the current time after attempt
        dataManager.PlayerData.LastSyncEpochMs = nowEpochMs;
        dataManager.PlayerData.LastPauseEpochMs = nowEpochMs;
        dataManager.SaveGame();
        Logger.LogInfo($"StepManager: Updated LastSync and LastPause to: {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)} after API catch-up attempt.", Logger.LogCategory.StepLog);
    }

    /// <summary>
    /// Handles state adjustments when returning from background.
    /// </summary>
    private void HandleReturningFromBackgroundState()
    {
        if (isReturningFromBackground)
        {
            Logger.LogInfo("StepManager: Activating sensor grace period after returning from background.", Logger.LogCategory.StepLog);
            inSensorGracePeriod = true;
            sensorGraceTimer = 0f;
            isSensorStartCountValid = false; // Invalidate sensor start count
            isReturningFromBackground = false; // Reset flag
            wasProbablyCrash = false; // Reset crash flag
        }
    }

    /// <summary>
    /// Initializes the direct sensor and starts its update loop.
    /// </summary>
    private IEnumerator InitializeDirectSensorAndStartLoop()
    {
        // This replaces the direct call to InitializeDirectSensor and starting DirectSensorUpdateLoop
        yield return StartCoroutine(InitializeDirectSensor()); // InitializeDirectSensor is already a coroutine
        StartCoroutine(DirectSensorUpdateLoop());
    }

    // Existing InitializeDirectSensor method (ensure it's a coroutine as expected)
    private IEnumerator InitializeDirectSensor()
    {
        Logger.LogInfo("StepManager: Initializing direct sensor...", Logger.LogCategory.StepLog);

        // Démarrer l'écoute directe du capteur
        apiCounter.StartDirectSensorListener();

        // Attendre d'obtenir une valeur valide pour sensorStartCount
        float waitTime = 0f;
        long currentSensorValue = -1;
        int stableReadingCount = 0;
        long previousReading = -1;

        while (waitTime < 10.0f) // Maximum 10 secondes d'attente
        {
            currentSensorValue = apiCounter.GetCurrentRawSensorSteps();

            if (currentSensorValue > 0)
            {
                // Vérifier si la lecture est stable (même valeur plusieurs fois)
                if (currentSensorValue == previousReading)
                {
                    stableReadingCount++;

                    // Si on a 3 lectures stables consécutives, on considère que la valeur est valide
                    if (stableReadingCount >= 3)
                    {
                        sensorStartCount = currentSensorValue;
                        lastRecordedSensorValue = currentSensorValue;
                        sensorDeltaThisSession = 0;
                        isSensorStartCountValid = true;

                        Logger.LogInfo($"StepManager: sensorStartCount successfully initialized to {sensorStartCount} after {stableReadingCount} stable readings", Logger.LogCategory.StepLog);
                        break;
                    }
                }
                else
                {
                    // Réinitialiser le compteur de lectures stables
                    stableReadingCount = 1;
                    previousReading = currentSensorValue;
                }
            }

            yield return new WaitForSeconds(0.5f);
            waitTime += 0.5f;
        }

        // Si on n'a pas réussi à obtenir une valeur stable, on utilise la dernière valeur obtenue
        if (!isSensorStartCountValid && currentSensorValue > 0)
        {
            sensorStartCount = currentSensorValue;
            lastRecordedSensorValue = currentSensorValue;
            sensorDeltaThisSession = 0;
            isSensorStartCountValid = true;
            Logger.LogInfo($"StepManager: Couldn't get stable readings. Using last sensor value: {sensorStartCount}", Logger.LogCategory.StepLog);
        }
        else if (!isSensorStartCountValid)
        {
            Logger.LogWarning("StepManager: Couldn't initialize sensorStartCount with a valid value.", Logger.LogCategory.StepLog);
            sensorStartCount = -1;
            lastRecordedSensorValue = -1;
            sensorDeltaThisSession = 0;
        }

        // La période de grâce commence après l'initialisation du capteur
        // Condition: only set if not already set by HandleReturningFromBackgroundState
        if (!inSensorGracePeriod) 
        {
            inSensorGracePeriod = true;
            sensorGraceTimer = 0f;
        }
        Logger.LogInfo("StepManager: Direct sensor initialization complete. Grace period active or re-activated.", Logger.LogCategory.StepLog);
    }

    // Nouvelle méthode pour mettre à jour le timestamp du capteur direct
    private void UpdateLastDirectSensorTimestamp()
    {
        long nowEpochMs = GetCurrentEpochMs();
        dataManager.PlayerData.LastSyncEpochMs = nowEpochMs;
        dataManager.PlayerData.LastPauseEpochMs = nowEpochMs;
        // This log can be very frequent. Consider changing to LogDebug or removing if too noisy.
        // For now, retained as per instruction not to change Logger.cs levels.
        Logger.LogInfo($"StepManager: Updated LastSync/PauseEpochMs to {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)} after direct sensor update.", Logger.LogCategory.StepLog);
    }

    // Méthode pour gérer le chevauchement de minuit
    private IEnumerator HandleMidnightSplitStepCount(long startTimeMs, long endTimeMs)
    {
        bool spansMidnight = DoesIntervalSpanMidnight(startTimeMs, endTimeMs);
        bool isMultiDayAbsence = false;

        if (spansMidnight)
        {
            DateTime startDate = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs).ToLocalTime().DateTime;
            DateTime endDate = DateTimeOffset.FromUnixTimeMilliseconds(endTimeMs).ToLocalTime().DateTime;
            isMultiDayAbsence = (endDate.Date - startDate.Date).Days > 1;

            if (isMultiDayAbsence)
            {
                Logger.LogInfo($"StepManager: Multi-day absence detected ({(endDate.Date - startDate.Date).Days} days).", Logger.LogCategory.StepLog);
            }
        }

        if (!spansMidnight)
        {
            yield return StartCoroutine(ProcessApiStepsNoSplit(startTimeMs, endTimeMs));
        }
        else if (isMultiDayAbsence)
        {
            yield return StartCoroutine(ProcessApiStepsMultiDaySplit(startTimeMs, endTimeMs));
        }
        else // Single midnight span
        {
            yield return StartCoroutine(ProcessApiStepsSingleMidnightSplit(startTimeMs, endTimeMs));
        }

        // Sauvegarder les données mises à jour après toute opération de comptage
        dataManager.PlayerData.TotalSteps = TotalSteps;
        dataManager.PlayerData.DailySteps = DailySteps;
        // Note: dataManager.SaveGame() is called by the PerformApiCatchUp after this coroutine finishes.
    }

    /// <summary>
    /// Processes API steps when the interval does not span midnight.
    /// </summary>
    private IEnumerator ProcessApiStepsNoSplit(long startTimeMs, long endTimeMs)
    {
        Logger.LogInfo($"StepManager: Processing API steps with no midnight split from {LocalDatabase.GetReadableDateFromEpoch(startTimeMs)} to {LocalDatabase.GetReadableDateFromEpoch(endTimeMs)}.", Logger.LogCategory.StepLog);
        long deltaApiSinceLast = 0;
        yield return StartCoroutine(apiCounter.GetDeltaSinceFromAPI(startTimeMs, endTimeMs, result => deltaApiSinceLast = result));

        deltaApiSinceLast = SanitizeStepDelta(deltaApiSinceLast, "ProcessApiStepsNoSplit");

        TotalSteps += deltaApiSinceLast;
        DailySteps += deltaApiSinceLast;
        Logger.LogInfo($"StepManager: No-split API Catch-up - Delta: {deltaApiSinceLast}. New TotalSteps: {TotalSteps}, DailySteps: {DailySteps}", Logger.LogCategory.StepLog);
    }

    /// <summary>
    /// Processes API steps for a multi-day absence, splitting counts for "before today" and "today".
    /// </summary>
    private IEnumerator ProcessApiStepsMultiDaySplit(long startTimeMs, long endTimeMs)
    {
        Logger.LogInfo($"StepManager: Processing API steps for multi-day split from {LocalDatabase.GetReadableDateFromEpoch(startTimeMs)} to {LocalDatabase.GetReadableDateFromEpoch(endTimeMs)}.", Logger.LogCategory.StepLog);
        string currentDateStr = GetLocalDateString();
        Logger.LogInfo($"StepManager: Multi-day absence detected, forcing DailySteps reset for {currentDateStr}.", Logger.LogCategory.StepLog);
        DailySteps = 0;
        dataManager.PlayerData.DailySteps = 0;
        dataManager.PlayerData.LastDailyResetDate = currentDateStr;

        // Use UtcNow and then convert to local for midnight calculation to ensure consistency
        DateTime nowUtc = DateTime.UtcNow;
        DateTime nowLocal = nowUtc.ToLocalTime(); // Convert UTC to local time
        DateTime todayMidnight = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, 0, 0, 0, DateTimeKind.Local);
        // todayMidnightMs is used for splitKey, which is fine as it's a relative point.
        // The actual API calls use carefully constructed UTC timestamps (todayMidnightMinusSafe, todayMidnightPlusSafe).
        long todayMidnightMs = new DateTimeOffset(todayMidnight).ToUnixTimeMilliseconds();

        string splitKey = $"multiday_{startTimeMs}_{todayMidnightMs}_{endTimeMs}";
        if (splitKey == lastMidnightSplitKey)
        {
            Logger.LogWarning($"StepManager: DUPLICATE MULTIDAY SPLIT DETECTED! Skipping. Key: {splitKey}", Logger.LogCategory.StepLog);
            yield break;
        }

        long todayMidnightMinusSafe = todayMidnightMs - MIDNIGHT_SAFETY_MS;
        long todayMidnightPlusSafe = todayMidnightMs + MIDNIGHT_SAFETY_MS;

        Logger.LogInfo($"StepManager: Splitting multi-day interval at today's midnight: {LocalDatabase.GetReadableDateFromEpoch(todayMidnightMs)}. Safe split: {LocalDatabase.GetReadableDateFromEpoch(todayMidnightMinusSafe)} | {LocalDatabase.GetReadableDateFromEpoch(todayMidnightPlusSafe)}", Logger.LogCategory.StepLog);

        long stepsBeforeToday = 0;
        if (startTimeMs < todayMidnightMinusSafe) // Only fetch if there's a valid interval
        {
            yield return StartCoroutine(apiCounter.GetDeltaSinceFromAPI(startTimeMs, todayMidnightMinusSafe, result => stepsBeforeToday = result));
            stepsBeforeToday = SanitizeStepDelta(stepsBeforeToday, "ProcessApiStepsMultiDaySplit - BeforeToday");
        } else {
            Logger.LogInfo($"StepManager: No interval to query for steps before today. Start: {LocalDatabase.GetReadableDateFromEpoch(startTimeMs)}, MidnightSafe: {LocalDatabase.GetReadableDateFromEpoch(todayMidnightMinusSafe)}", Logger.LogCategory.StepLog);
        }


        long stepsToday = 0;
        if (endTimeMs > todayMidnightPlusSafe) // Only fetch if there's a valid interval
        {
            yield return StartCoroutine(apiCounter.GetDeltaSinceFromAPI(todayMidnightPlusSafe, endTimeMs, result => stepsToday = result));
            stepsToday = SanitizeStepDelta(stepsToday, "ProcessApiStepsMultiDaySplit - Today");
        } else {
            Logger.LogInfo($"StepManager: No interval to query for steps today. End: {LocalDatabase.GetReadableDateFromEpoch(endTimeMs)}, MidnightSafe: {LocalDatabase.GetReadableDateFromEpoch(todayMidnightPlusSafe)}", Logger.LogCategory.StepLog);
        }


        TotalSteps += (stepsBeforeToday + stepsToday);
        DailySteps += stepsToday; // Only today's steps for daily
        lastMidnightSplitKey = splitKey;

        Logger.LogInfo($"StepManager: Multi-day API Catch-up - Steps before today: {stepsBeforeToday}, Steps today: {stepsToday}. New TotalSteps: {TotalSteps}, DailySteps: {DailySteps}", Logger.LogCategory.StepLog);
    }

    /// <summary>
    /// Processes API steps when the interval spans a single midnight.
    /// </summary>
    private IEnumerator ProcessApiStepsSingleMidnightSplit(long startTimeMs, long endTimeMs)
    {
        Logger.LogInfo($"StepManager: Processing API steps for single midnight split from {LocalDatabase.GetReadableDateFromEpoch(startTimeMs)} to {LocalDatabase.GetReadableDateFromEpoch(endTimeMs)}.", Logger.LogCategory.StepLog);
        string currentDateStr = GetLocalDateString();
        Logger.LogInfo($"StepManager: Single midnight boundary detected, forcing DailySteps reset for {currentDateStr}.", Logger.LogCategory.StepLog);
        DailySteps = 0;
        dataManager.PlayerData.DailySteps = 0;
        dataManager.PlayerData.LastDailyResetDate = currentDateStr;

        long midnightMs = FindMidnightTimestamp(startTimeMs, endTimeMs);
        Logger.LogInfo($"StepManager: Interval spans midnight at {LocalDatabase.GetReadableDateFromEpoch(midnightMs)}. Splitting step counts.", Logger.LogCategory.StepLog);

        string splitKey = $"single_{startTimeMs}_{midnightMs}_{endTimeMs}";
        if (splitKey == lastMidnightSplitKey)
        {
            Logger.LogWarning($"StepManager: DUPLICATE SINGLE MIDNIGHT SPLIT DETECTED! Skipping. Key: {splitKey}", Logger.LogCategory.StepLog);
            yield break;
        }

        long midnightMinusSafe = midnightMs - MIDNIGHT_SAFETY_MS;
        long midnightPlusSafe = midnightMs + MIDNIGHT_SAFETY_MS;
        Logger.LogInfo($"StepManager: Safe split around midnight: {LocalDatabase.GetReadableDateFromEpoch(midnightMinusSafe)} | {LocalDatabase.GetReadableDateFromEpoch(midnightPlusSafe)}", Logger.LogCategory.StepLog);


        long stepsBeforeMidnight = 0;
        if (startTimeMs < midnightMinusSafe) // Ensure valid interval
        {
            yield return StartCoroutine(apiCounter.GetDeltaSinceFromAPI(startTimeMs, midnightMinusSafe, result => stepsBeforeMidnight = result));
            stepsBeforeMidnight = SanitizeStepDelta(stepsBeforeMidnight, "ProcessApiStepsSingleMidnightSplit - BeforeMidnight");
        } else {
             Logger.LogInfo($"StepManager: No interval to query for steps before midnight. Start: {LocalDatabase.GetReadableDateFromEpoch(startTimeMs)}, MidnightSafe: {LocalDatabase.GetReadableDateFromEpoch(midnightMinusSafe)}", Logger.LogCategory.StepLog);
        }

        long stepsAfterMidnight = 0;
        if (endTimeMs > midnightPlusSafe) // Ensure valid interval
        {
            yield return StartCoroutine(apiCounter.GetDeltaSinceFromAPI(midnightPlusSafe, endTimeMs, result => stepsAfterMidnight = result));
            stepsAfterMidnight = SanitizeStepDelta(stepsAfterMidnight, "ProcessApiStepsSingleMidnightSplit - AfterMidnight");
        } else {
            Logger.LogInfo($"StepManager: No interval to query for steps after midnight. End: {LocalDatabase.GetReadableDateFromEpoch(endTimeMs)}, MidnightSafe: {LocalDatabase.GetReadableDateFromEpoch(midnightPlusSafe)}", Logger.LogCategory.StepLog);
        }

        TotalSteps += (stepsBeforeMidnight + stepsAfterMidnight);
        DailySteps += stepsAfterMidnight; // Only after midnight steps for current daily
        lastMidnightSplitKey = splitKey;

        Logger.LogInfo($"StepManager: Single-midnight API Catch-up - Steps before midnight: {stepsBeforeMidnight}, Steps after midnight: {stepsAfterMidnight}. New TotalSteps: {TotalSteps}, DailySteps: {DailySteps}", Logger.LogCategory.StepLog);
    }
    
    /// <summary>
    /// Sanitizes the step delta received from the API, ensuring it's not negative and within reasonable limits.
    /// </summary>
    private long SanitizeStepDelta(long delta, string context)
    {
        if (delta < 0)
        {
            Logger.LogWarning($"StepManager: {context} - API returned negative delta ({delta}). Defaulting to 0.", Logger.LogCategory.StepLog);
            return 0;
        }
        if (delta > MAX_STEPS_PER_UPDATE) // Assuming MAX_STEPS_PER_UPDATE is a class constant
        {
            Logger.LogWarning($"StepManager: {context} - API returned {delta} steps, exceeds threshold {MAX_STEPS_PER_UPDATE}. Limiting.", Logger.LogCategory.StepLog);
            return MAX_STEPS_PER_UPDATE;
        }
        return delta;
    }

    // Vérifier si l'intervalle chevauche minuit en utilisant TimeZoneInfo.Local (Faille B)
    private bool DoesIntervalSpanMidnight(long startTimeMs, long endTimeMs)
    {
        DateTime startDate = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs)
            .ToLocalTime().DateTime.Date;
        DateTime endDate = DateTimeOffset.FromUnixTimeMilliseconds(endTimeMs)
            .ToLocalTime().DateTime.Date;
        return startDate != endDate;
    }

    // Trouver le timestamp du minuit entre les deux timestamps en utilisant TimeZoneInfo.Local (Faille B)
    private long FindMidnightTimestamp(long startTimeMs, long endTimeMs)
    {
        DateTime startDateTime = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs).ToLocalTime().DateTime;
        DateTime nextMidnight = startDateTime.Date.AddDays(1); // Minuit le jour suivant
        return new DateTimeOffset(nextMidnight, TimeZoneInfo.Local.GetUtcOffset(nextMidnight)).ToUnixTimeMilliseconds();
    }

    IEnumerator DirectSensorUpdateLoop()
    {
        Logger.LogInfo("StepManager: Starting DirectSensorUpdateLoop.", Logger.LogCategory.StepLog);
        lastDBSaveTime = Time.time; // Initialize save timer

        while (isAppInForeground)
        {
            yield return new WaitForSeconds(1.0f); // Main loop tick

            if (HandleSensorGracePeriod())
            {
                continue; // Skip normal processing if in grace period and still active
            }

            if (!CheckSensorPrerequisites()) // Checks permission and sensorStartCount validity
            {
                if (!isAppInForeground) break; // Exit loop if app went to background due to lost permission
                continue; // Skip if prerequisites not met
            }

            long rawSensorValue;
            SensorReadStatus sensorStatus = ReadAndValidateSensorValue(out rawSensorValue);

            if (sensorStatus == SensorReadStatus.Error) // Critical error, sensor unavailable
            {
                isAppInForeground = false; // Effectively stops the loop
                break;
            }
            if (sensorStatus == SensorReadStatus.Skip) // Non-critical error or value to ignore
            {
                continue;
            }
            // SensorReadStatus.Valid means rawSensorValue is good

            lastRecordedSensorValue = rawSensorValue; // Update for future reference

            if (HandleSensorResetIfNeeded(rawSensorValue))
            {
                continue; // Skip further processing if sensor was reset
            }
            
            if (sensorStartCount != -1) // Ensure sensorStartCount is valid before calculating delta
            {
                long newSteps = ProcessNewSteps(rawSensorValue, ref sensorDeltaThisSession, ref lastLargeUpdateTime);
                if (newSteps > 0)
                {
                    UpdateStepsAndSaveData(newSteps, ref lastDBSaveTime);
                }
            }
        }

        // Always save before exiting the loop if DataManager is available
        if (dataManager != null && dataManager.PlayerData != null)
        {
            dataManager.SaveGame();
            Logger.LogInfo("StepManager: Saved data on exiting DirectSensorUpdateLoop.");
        }
        Logger.LogInfo("StepManager: Exiting DirectSensorUpdateLoop.");
    }

    /// <summary>
    /// Handles the sensor grace period logic.
    /// Returns true if the grace period is active and further processing should be skipped.
    /// </summary>
    private bool HandleSensorGracePeriod()
    {
        if (!inSensorGracePeriod) return false;

        sensorGraceTimer += 1.0f;
        if (sensorGraceTimer >= SENSOR_GRACE_PERIOD)
        {
            inSensorGracePeriod = false;
            Logger.LogInfo("StepManager: Sensor grace period ended - normal step counting resumed.", Logger.LogCategory.StepLog);
            return false; // Grace period just ended, proceed to normal counting
        }
        else
        {
            // During grace period, read sensor but ignore changes for step counting
            // However, update sensorStartCount to the latest raw value to prevent large jumps when grace period ends.
            long currentRawValue = apiCounter.GetCurrentRawSensorSteps();
            if (currentRawValue > 0 && currentRawValue != lastRecordedSensorValue) // Check if value is valid and changed
            {
                Logger.LogInfo($"StepManager: [GRACE PERIOD] Sensor value changed from {lastRecordedSensorValue} to {currentRawValue}. Updating baseline.", Logger.LogCategory.StepLog);
                lastRecordedSensorValue = currentRawValue;
                sensorStartCount = currentRawValue; // Update start count to this new value
                sensorDeltaThisSession = 0; // Reset delta for this session
            }
            // Reduced frequency of this log. Log once when grace period becomes active.
            // The end of grace period is already logged.
            // Logger.LogInfo($"StepManager: [GRACE PERIOD] Active, {SENSOR_GRACE_PERIOD - sensorGraceTimer:F1}s remaining. Skipping normal step processing.");
            return true; // Grace period active, skip rest of the loop
        }
    }

    /// <summary>
    /// Checks for essential prerequisites like API permission and valid sensorStartCount.
    /// Returns true if all prerequisites are met, false otherwise.
    /// May set isAppInForeground to false if permission is lost.
    /// </summary>
    private bool CheckSensorPrerequisites()
    {
        if (!apiCounter.HasPermission())
        {
            Logger.LogWarning("StepManager: Permission lost during sensor update loop. Stopping sensor.", Logger.LogCategory.StepLog);
            apiCounter.StopDirectSensorListener();
            isAppInForeground = false; // This will cause the main loop to exit
            return false;
        }

        if (!isSensorStartCountValid)
        {
            Logger.LogWarning("StepManager: sensorStartCount is not valid. Skipping sensor update.", Logger.LogCategory.StepLog);
            // Attempt to re-initialize the sensor if start count is invalid.
            // This can happen if the initial InitializeDirectSensor failed or was interrupted.
            StartCoroutine(InitializeDirectSensor());
            return false;
        }
        return true;
    }

    private enum SensorReadStatus { Valid, Skip, Error }

    /// <summary>
    /// Reads the raw sensor value and validates it.
    /// Outputs the raw sensor value.
    /// Returns Enum indicating status: Valid, Skip (ignore this reading), Error (stop loop).
    /// </summary>
    private SensorReadStatus ReadAndValidateSensorValue(out long rawSensorValue)
    {
        rawSensorValue = apiCounter.GetCurrentRawSensorSteps();

        if (rawSensorValue < 0)
        {
            if (rawSensorValue == -2) // Specific error code for sensor unavailable
            {
                Logger.LogError("StepManager: Direct sensor became unavailable! Stopping loop.", Logger.LogCategory.StepLog);
                apiCounter.StopDirectSensorListener();
                return SensorReadStatus.Error; // Critical error
            }
            Logger.LogWarning($"StepManager: Invalid raw sensor value ({rawSensorValue}). Skipping this update.", Logger.LogCategory.StepLog);
            return SensorReadStatus.Skip; // Invalid reading, but not critical
        }
        return SensorReadStatus.Valid; // Value is valid
    }
    
    /// <summary>
    /// Handles potential sensor resets (e.g., due to OS or hardware).
    /// Returns true if a reset was handled and current iteration should skip further step processing.
    /// </summary>
    private bool HandleSensorResetIfNeeded(long rawSensorValue)
    {
        if (rawSensorValue < sensorStartCount)
        {
            // Distinguish between a significant reset and minor fluctuation
            if (sensorStartCount - rawSensorValue > 1000) // Threshold for definite reset
            {
                Logger.LogInfo($"StepManager: Sensor full reset detected. Old start count: {sensorStartCount}, New raw value: {rawSensorValue}", Logger.LogCategory.StepLog);

                // Option: Trigger a mini API catch-up here if necessary, though current logic defers this.
                // For now, just resetting internal counters and saving state.
                // long currentTime = GetCurrentEpochMs();
                // Logger.LogInfo($"StepManager: Consider mini API catch-up due to sensor reset from {LocalDatabase.GetReadableDateFromEpoch(dataManager.PlayerData.LastApiCatchUpEpochMs)} to {LocalDatabase.GetReadableDateFromEpoch(currentTime)}");

                sensorStartCount = rawSensorValue; // Reset sensorStartCount to the new baseline
                sensorDeltaThisSession = 0;        // Reset delta for the current session

                UpdateLastDirectSensorTimestamp(); // Update sync timestamps
                dataManager.SaveGame();            // Persist this new state immediately
                Logger.LogInfo($"StepManager: Sensor state reset. New sensorStartCount: {sensorStartCount}. Data saved.", Logger.LogCategory.StepLog);
                return true; // Reset handled, skip further processing for this iteration
            }
            else
            {
                // Minor decrease, potentially a temporary error or fluctuation.
                Logger.LogWarning($"StepManager: Unexpected sensor value decrease from {sensorStartCount} to {rawSensorValue}. Ignoring this specific reading.", Logger.LogCategory.StepLog);
                // Do not modify sensorStartCount here, just ignore this anomalous value.
                return true; // Ignore this reading, skip further processing
            }
        }
        return false; // No reset detected or handled in a way that requires skipping
    }


    /// <summary>
    /// Processes new steps from the raw sensor value, including filtering and debouncing.
    /// Returns the number of new steps detected after filtering.
    /// </summary>
    private long ProcessNewSteps(long rawSensorValue, ref long currentSensorDeltaThisSession, ref float lastSensorLargeUpdateTime)
    {
        // Calculate total steps recorded by sensor in this app session, relative to its initial start count
        long currentTotalSensorStepsThisSession = (rawSensorValue >= sensorStartCount) ? (rawSensorValue - sensorStartCount) : 0;
        // Calculate new steps since the last update in this session
        long newIndividualSensorSteps = currentTotalSensorStepsThisSession - currentSensorDeltaThisSession;

        if (newIndividualSensorSteps <= 0)
        {
            return 0; // No new steps or a decrease already handled by reset logic
        }

        // Filter abnormal spikes with debounce
        if (newIndividualSensorSteps > SENSOR_SPIKE_THRESHOLD)
        {
            float currentTime = Time.time; // Unity's Time.time for debounce timing
            if (currentTime - lastSensorLargeUpdateTime < SENSOR_DEBOUNCE_SECONDS)
            {
                Logger.LogWarning($"StepManager: Anomaly detected! {newIndividualSensorSteps} steps in quick succession filtered out (debounce).", Logger.LogCategory.StepLog);
                return 0; // Ignore this update due to debouncing
            }
            else
            {
                lastSensorLargeUpdateTime = currentTime; // Update time of last large step update
                Logger.LogInfo($"StepManager: Large step update of {newIndividualSensorSteps} accepted after debounce check.", Logger.LogCategory.StepLog);
            }
        }

        // Limit increment to a reasonable maximum
        if (newIndividualSensorSteps > MAX_STEPS_PER_UPDATE)
        {
            Logger.LogWarning($"StepManager: Unusually large step increment: {newIndividualSensorSteps}. Limiting to {MAX_STEPS_PER_UPDATE}.", Logger.LogCategory.StepLog);
            newIndividualSensorSteps = MAX_STEPS_PER_UPDATE;
        }
        
        return newIndividualSensorSteps;
    }

    /// <summary>
    /// Updates total and daily steps, handles day changes during session, and manages periodic data saving.
    /// </summary>
    private void UpdateStepsAndSaveData(long newSteps, ref float lastDbSaveTimestamp)
    {
        // Check for day change during the session (e.g., app left open overnight)
        string currentDateStr = GetLocalDateString();
        if (dataManager.PlayerData.LastDailyResetDate != currentDateStr)
        {
            Logger.LogInfo($"StepManager: Day change detected during active step update. Current Date: {currentDateStr}, Last Reset: {dataManager.PlayerData.LastDailyResetDate}. Resetting daily steps.", Logger.LogCategory.StepLog);
            DailySteps = 0; // Reset daily steps counter
            dataManager.PlayerData.DailySteps = 0; // Persist reset
            dataManager.PlayerData.LastDailyResetDate = currentDateStr; // Update reset date
        }

        // Update step counters
        sensorDeltaThisSession += newSteps; // Update delta for current app session using direct sensor
        TotalSteps += newSteps;
        DailySteps += newSteps;
        // Concise log for step updates
        Logger.LogInfo($"StepManager: Sensor steps: +{newSteps}. SessionΔ: {sensorDeltaThisSession}, Daily: {DailySteps}, Total: {TotalSteps}", Logger.LogCategory.StepLog);

        // Persist updated step counts
        dataManager.PlayerData.TotalSteps = TotalSteps;
        dataManager.PlayerData.DailySteps = DailySteps;

        // Update sync timestamps to reflect this sensor activity
        UpdateLastDirectSensorTimestamp();

        // Periodic DB save
        float currentTime = Time.time; // Unity's Time.time for save interval timing
        if (currentTime - lastDbSaveTimestamp >= DB_SAVE_INTERVAL)
        {
            dataManager.SaveGame();
            lastDbSaveTimestamp = currentTime; // Update timestamp of the last save
            Logger.LogInfo($"StepManager: Periodic DB save triggered. Interval: {DB_SAVE_INTERVAL}s.", Logger.LogCategory.StepLog);
        }
    }

    void HandleAppPausingOrClosing()
    {
        if (!isInitialized) return;

        Logger.LogInfo("StepManager: HandleAppPausingOrClosing started.", Logger.LogCategory.General);
        isAppInForeground = false;

        // Arrêter le capteur direct quand l'application n'est plus au premier plan
        apiCounter.StopDirectSensorListener();
        Logger.LogInfo("StepManager: Direct sensor listener stopped.", Logger.LogCategory.StepLog);

        // Enregistrer le timestamp de pause pour résoudre le problème de double comptage
        long nowEpochMs = GetCurrentEpochMs();
        dataManager.PlayerData.LastSyncEpochMs = nowEpochMs;
        dataManager.PlayerData.LastPauseEpochMs = nowEpochMs;

        // IMPORTANT: Mettre également à jour LastApiCatchUpEpochMs pour éviter le double comptage
        // lors du prochain retour de l'arrière-plan (Faille #1 du document)
        dataManager.PlayerData.LastApiCatchUpEpochMs = nowEpochMs;
        
        // Consolidate logging for pause/close
        Logger.LogInfo($"StepManager: App pausing/closing. Updated LastSync/Pause/ApiCatchUp EpochMs to {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)}. Saving data. Final TotalSteps: {TotalSteps}, DailySteps: {DailySteps}", Logger.LogCategory.General);
        
        // Force une sauvegarde à chaque pause/fermeture pour s'assurer que les données sont persistées
        dataManager.PlayerData.TotalSteps = TotalSteps;
        dataManager.PlayerData.DailySteps = DailySteps;
        dataManager.SaveGame(); // This will log through LocalDatabase.SavePlayerData
        // Logger.LogInfo("StepManager: Data saved on pause/close."); // Redundant if SaveGame logs
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!isInitialized) return;

        if (pauseStatus)
        {
            // L'application va en arrière-plan
            Logger.LogInfo("StepManager: OnApplicationPause → app goes to background", Logger.LogCategory.General);
            HandleAppPausingOrClosing();
        }
        else
        {
            // L'application revient au premier plan
            Logger.LogInfo("StepManager: OnApplicationPause → app returns to foreground", Logger.LogCategory.General);

            // Marquer que l'application retourne au premier plan après avoir été en arrière-plan
            isReturningFromBackground = true;

            if (isInitialized)
            {
                // Refactored: Call the new main orchestrator
                StartCoroutine(OrchestrateAppOpeningOrResuming());
            }
        }
    }

    void OnApplicationQuit()
    {
        if (isAppInForeground)
        {
            HandleAppPausingOrClosing();
        }
        Logger.LogInfo("StepManager: Application Quitting.", Logger.LogCategory.General);
    }

    // MODIFIÉ: Harmoniser la gestion des fuseaux horaires (Faille B)

    // Obtenir la date locale actuelle au format "yyyy-MM-dd"
    // This method is critical for daily resets and MUST reflect the user's local date.
    private string GetLocalDateString()
    {
        // Convert UtcNow to local time first, then get the date string.
        return DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd");
    }

    // Obtenir l'horodatage Unix actuel en millisecondes, standardized to UTC.
    private long GetCurrentEpochMs()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}