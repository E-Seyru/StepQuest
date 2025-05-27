// Filepath: Assets/Scripts/Services/StepTracking/StepManager.cs
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

    private bool isInitialized = false;

#if UNITY_EDITOR
    // EN ÉDITEUR: Fonctionnement simplifié
    private Coroutine editorUpdateCoroutine;
#else
    // SUR DEVICE: Variables pour la gestion complète des capteurs
    private bool isAppInForeground = true;
    private bool isReturningFromBackground = false;
    private bool isSensorStartCountValid = false;
    
    // Variables internes pour le capteur direct
    private long sensorStartCount = -1;
    private long sensorDeltaThisSession = 0;
    private long lastRecordedSensorValue = -1;
    
    // Paramètres de débounce et filtrage
    private const int SENSOR_SPIKE_THRESHOLD = 50;
    private const int SENSOR_DEBOUNCE_SECONDS = 3;
    private float lastLargeUpdateTime = 0f;
    private const long MAX_STEPS_PER_UPDATE = 100000;
    
    // Période de grâce après le retour au premier plan
    private const float SENSOR_GRACE_PERIOD = 5.0f;
    private float sensorGraceTimer = 0f;
    private bool inSensorGracePeriod = false;
    
    // Paramètre pour contrôler la fréquence des sauvegardes DB
    private const float DB_SAVE_INTERVAL = 3.0f;
    private float lastDBSaveTime = 0f;
    
    // Timestamp dédié au dernier catch-up API
    private long lastApiCatchUpEpochMs = 0;
    
    // Détection de crash
    private bool wasProbablyCrash = false;
    
    // Constantes pour éviter le chevauchement à minuit
    private const long MIDNIGHT_SAFETY_MS = 500;
    private const long SENSOR_API_PADDING_MS = 1500;
    
    // Variable pour vérifier et éviter les dédoublements
    private string lastMidnightSplitKey = "";
#endif

    void Awake()
    {
        // Protection contre les instances multiples
        if (Instance != null && Instance != this)
        {
            Logger.LogWarning("StepManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.StepLog);
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
        Logger.LogInfo("StepManager: Start - Initializing...", Logger.LogCategory.StepLog);
        yield return StartCoroutine(WaitForServices());

        if (dataManager == null)
        {
            Logger.LogError("StepManager: DataManager not found. StepManager cannot function.", Logger.LogCategory.StepLog);
            isInitialized = false;
            yield break;
        }

#if UNITY_EDITOR
        yield return StartCoroutine(InitializeEditorMode());
#else
        if (apiCounter == null || uiManager == null)
        {
            Logger.LogError("StepManager: Critical services not found. StepManager cannot function.", Logger.LogCategory.StepLog);
            isInitialized = false;
            yield break;
        }
        
        apiCounter.InitializeService();
        // Attendre un peu pour s'assurer que le DataManager ait correctement chargé les données
        yield return new WaitForSeconds(1.0f);
        yield return StartCoroutine(HandleAppOpeningOrResuming());
#endif

        isInitialized = true;
        Logger.LogInfo("StepManager: Initialization complete.", Logger.LogCategory.StepLog);
    }

    IEnumerator WaitForServices()
    {
        while (DataManager.Instance == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
        dataManager = DataManager.Instance;

#if !UNITY_EDITOR
        while (RecordingAPIStepCounter.Instance == null || UIManager.Instance == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
        apiCounter = RecordingAPIStepCounter.Instance;
        uiManager = UIManager.Instance;
#endif

        Logger.LogInfo("StepManager: All dependent services found.", Logger.LogCategory.StepLog);
    }

#if UNITY_EDITOR
    // ===== MODE ÉDITEUR SIMPLIFIÉ =====
    IEnumerator InitializeEditorMode()
    {
        Logger.LogInfo("StepManager: [EDITOR] Initializing in Editor mode", Logger.LogCategory.StepLog);

        // Charger les pas depuis DataManager
        TotalSteps = dataManager.PlayerData.TotalSteps;
        DailySteps = dataManager.PlayerData.DailySteps;

        // Vérifier le changement de jour
        string currentDateStr = DateTime.Now.ToString("yyyy-MM-dd");
        string lastResetDateStr = dataManager.PlayerData.LastDailyResetDate;
        bool isDayChanged = string.IsNullOrEmpty(lastResetDateStr) || lastResetDateStr != currentDateStr;

        if (isDayChanged)
        {
            Logger.LogInfo($"StepManager: [EDITOR] Day change detected. Resetting daily steps from {DailySteps} to 0", Logger.LogCategory.StepLog);
            DailySteps = 0;
            dataManager.PlayerData.DailySteps = 0;
            dataManager.PlayerData.LastDailyResetDate = currentDateStr;
            dataManager.SaveGame();
        }

        Logger.LogInfo($"StepManager: [EDITOR] Loaded - TotalSteps: {TotalSteps}, DailySteps: {DailySteps}", Logger.LogCategory.StepLog);

        // Démarrer la surveillance des changements en éditeur
        editorUpdateCoroutine = StartCoroutine(EditorUpdateLoop());

        yield return null;
    }

    // Boucle simple pour l'éditeur qui surveille les changements du DataManager
    IEnumerator EditorUpdateLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f); // Vérifier toutes les 0.5 secondes

            if (dataManager?.PlayerData != null)
            {
                // Vérifier si les pas ont changé (par le simulateur par exemple)
                bool totalChanged = dataManager.PlayerData.TotalSteps != TotalSteps;
                bool dailyChanged = dataManager.PlayerData.DailySteps != DailySteps;

                if (totalChanged || dailyChanged)
                {
                    TotalSteps = dataManager.PlayerData.TotalSteps;
                    DailySteps = dataManager.PlayerData.DailySteps;

                    Logger.LogInfo($"StepManager: [EDITOR] Steps updated - Total: {TotalSteps}, Daily: {DailySteps}", Logger.LogCategory.StepLog);
                }
            }
        }
    }
#else
    // ===== MODE DEVICE COMPLET =====
    IEnumerator HandleAppOpeningOrResuming()
    {
        Logger.LogInfo("StepManager: HandleAppOpeningOrResuming started.", Logger.LogCategory.StepLog);
        isAppInForeground = true;

        // Charger les données actuelles
        TotalSteps = dataManager.PlayerData.TotalSteps;
        DailySteps = dataManager.PlayerData.DailySteps;
        long lastSyncEpochMs = dataManager.PlayerData.LastSyncEpochMs;
        long lastPauseEpochMs = dataManager.PlayerData.LastPauseEpochMs;
        long nowEpochMs = GetCurrentEpochMs();

        // Charger lastApiCatchUpEpochMs depuis PlayerData
        lastApiCatchUpEpochMs = dataManager.PlayerData.LastApiCatchUpEpochMs;
        Logger.LogInfo($"StepManager: lastApiCatchUpEpochMs loaded from database: {LocalDatabase.GetReadableDateFromEpoch(lastApiCatchUpEpochMs)}", Logger.LogCategory.StepLog);

        // Détection de crash
        if (lastPauseEpochMs < lastSyncEpochMs)
        {
            wasProbablyCrash = true;
            Logger.LogWarning($"StepManager: Probable crash detected! LastPause ({LocalDatabase.GetReadableDateFromEpoch(lastPauseEpochMs)}) < LastSync ({LocalDatabase.GetReadableDateFromEpoch(lastSyncEpochMs)})", Logger.LogCategory.StepLog);
        }

        // Vérifier explicitement si le jour a changé
        string currentDateStr = GetLocalDateString();
        string lastResetDateStr = dataManager.PlayerData.LastDailyResetDate;
        bool isDayChanged = string.IsNullOrEmpty(lastResetDateStr) || lastResetDateStr != currentDateStr;

        if (isDayChanged)
        {
            Logger.LogInfo($"StepManager: **** DAY CHANGE DETECTED **** Last reset date: {lastResetDateStr}, Current date: {currentDateStr}", Logger.LogCategory.StepLog);

            // Réinitialiser clairement les pas quotidiens
            DailySteps = 0;
            dataManager.PlayerData.DailySteps = 0;
            dataManager.PlayerData.LastDailyResetDate = currentDateStr;

            // Sauvegarder immédiatement
            dataManager.SaveGame();

            Logger.LogInfo($"StepManager: Daily steps reset to 0 for new day {currentDateStr}. Saved to database.", Logger.LogCategory.StepLog);
        }
        else
        {
            Logger.LogInfo($"StepManager: Same day detected. LastResetDate: {lastResetDateStr}, Today: {currentDateStr}", Logger.LogCategory.StepLog);
        }

        Logger.LogInfo($"StepManager: Loaded state - Initial TotalSteps: {TotalSteps}, DailySteps: {DailySteps}, " +
                      $"LastSync: {LocalDatabase.GetReadableDateFromEpoch(lastSyncEpochMs)}, " +
                      $"LastPause: {LocalDatabase.GetReadableDateFromEpoch(lastPauseEpochMs)}, " +
                      $"LastApiCatchUp: {LocalDatabase.GetReadableDateFromEpoch(lastApiCatchUpEpochMs)}, " +
                      $"Now: {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)}", Logger.LogCategory.StepLog);

        // Vérification des permissions requise AVANT TOUT !
        if (!apiCounter.HasPermission())
        {
            Logger.LogInfo("StepManager: Permission not granted. Requesting permission...", Logger.LogCategory.StepLog);
            apiCounter.RequestPermission();
            float permissionWaitTime = 0f;
            while (!apiCounter.HasPermission() && permissionWaitTime < 30f)
            {
                Logger.LogInfo("StepManager: Waiting for permission grant for API operations...", Logger.LogCategory.StepLog);
                yield return new WaitForSeconds(1f);
                permissionWaitTime += 1f;
            }
        }

        if (!apiCounter.HasPermission())
        {
            Logger.LogError("StepManager: Permission still not granted after request. Cannot proceed with API sync or sensor.", Logger.LogCategory.StepLog);
            yield break;
        }

        // S'abonner à l'API de comptage
        apiCounter.SubscribeToRecordingApiIfNeeded();

        // Vérifier si c'est le premier démarrage réel
        bool isFirstRun = TotalSteps == 0 && lastSyncEpochMs == 0;

        // Si c'est le premier démarrage, on initialise uniquement le timestamp
        if (isFirstRun)
        {
            Logger.LogInfo("StepManager: First app startup detected (TotalSteps=0 AND LastSync=0). Initializing LastSync timestamp without API catch-up.", Logger.LogCategory.StepLog);
            dataManager.PlayerData.LastSyncEpochMs = nowEpochMs;
            dataManager.PlayerData.LastPauseEpochMs = nowEpochMs;
            lastApiCatchUpEpochMs = nowEpochMs; // Initialiser aussi lastApiCatchUpEpochMs
            dataManager.SaveGame();
            Logger.LogInfo($"StepManager: API Catch-up skipped for first start. TotalSteps: {TotalSteps}. Updated LastSync to: {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)}", Logger.LogCategory.StepLog);
        }
        else
        {
            // Pour tous les autres cas (y compris retour d'arrière-plan), récupérer les pas via l'API

            // S'assurer que LastApiCatchUpEpochMs a une valeur valide
            if (lastApiCatchUpEpochMs <= 0)
            {
                // Si on a des pas mais pas de timestamp valide, on utilise une date récente (24h avant)
                Logger.LogInfo("StepManager: LastApiCatchUpEpochMs value invalid (0) but TotalSteps > 0. Setting to 24h ago.", Logger.LogCategory.StepLog);
                lastApiCatchUpEpochMs = nowEpochMs - (24 * 60 * 60 * 1000); // 24h en millisecondes
            }

            // 1. borne de départ "brute"
            long startTimeMs = wasProbablyCrash ? lastPauseEpochMs : lastApiCatchUpEpochMs;
            long endTimeMs = nowEpochMs;

            // 2. Si le capteur direct recouvre déjà la borne, pousse-la d'un padding
            if (!wasProbablyCrash && lastSyncEpochMs >= startTimeMs)
            {
                startTimeMs = lastSyncEpochMs + SENSOR_API_PADDING_MS;   // évite le chevauchement exact
                Logger.LogInfo($"StepManager: Small overlap détecté – API catch-up décalé à {LocalDatabase.GetReadableDateFromEpoch(startTimeMs)}", Logger.LogCategory.StepLog);
            }

            // 3. On ne déclenche l'API que si l'intervalle est toujours valide
            if (endTimeMs > startTimeMs)
            {
                // Récupérer les pas depuis la dernière synchronisation jusqu'à maintenant
                // avec gestion spéciale en cas de chevauchement de minuit
                if (isReturningFromBackground)
                {
                    Logger.LogInfo($"StepManager: RETURNING FROM BACKGROUND - Starting API Catch-up from {LocalDatabase.GetReadableDateFromEpoch(startTimeMs)} to {LocalDatabase.GetReadableDateFromEpoch(endTimeMs)}", Logger.LogCategory.StepLog);
                }
                else
                {
                    Logger.LogInfo($"StepManager: Starting API Catch-up from {LocalDatabase.GetReadableDateFromEpoch(startTimeMs)} to {LocalDatabase.GetReadableDateFromEpoch(endTimeMs)}", Logger.LogCategory.StepLog);
                }

                yield return StartCoroutine(HandleMidnightSplitStepCount(startTimeMs, endTimeMs));

                // Après un catch-up API réussi, mettre à jour le timestamp dédié
                lastApiCatchUpEpochMs = nowEpochMs;
                // Sauvegarder la valeur dans PlayerData
                dataManager.PlayerData.LastApiCatchUpEpochMs = lastApiCatchUpEpochMs;
                Logger.LogInfo($"StepManager: Updated lastApiCatchUpEpochMs to: {LocalDatabase.GetReadableDateFromEpoch(lastApiCatchUpEpochMs)}", Logger.LogCategory.StepLog);

                // Appel direct à la méthode ClearStoredRange de RecordingAPIStepCounter
                apiCounter.ClearStoredRange();

                // Sauvegarde immédiate pour éviter la perte de l'horodatage en cas de crash
                // juste après un catch-up
                dataManager.SaveGame();
                Logger.LogInfo("StepManager: Saved LastApiCatchUpEpochMs immediately after API catch-up to prevent loss on crash", Logger.LogCategory.StepLog);
            }
            else
            {
                Logger.LogInfo("StepManager: API catch-up skipped – pas d'intervalle valide après ajustement.", Logger.LogCategory.StepLog);
            }

            // Toujours mettre à jour LastSyncEpochMs et LastPauseEpochMs au temps actuel
            dataManager.PlayerData.LastSyncEpochMs = nowEpochMs;
            dataManager.PlayerData.LastPauseEpochMs = nowEpochMs;
            dataManager.SaveGame();
            Logger.LogInfo($"StepManager: Updated LastSync and LastPause to: {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)}", Logger.LogCategory.StepLog);
        }

        // Si on revient de l'arrière-plan, activer la période de grâce pour le sensor
        if (isReturningFromBackground)
        {
            Logger.LogInfo("StepManager: Activating sensor grace period after returning from background.", Logger.LogCategory.StepLog);
            inSensorGracePeriod = true;
            sensorGraceTimer = 0f;
            isSensorStartCountValid = false; // Réinitialiser le flag
            // Reset le flag
            isReturningFromBackground = false;
            wasProbablyCrash = false; // Réinitialiser également ce flag
        }

        // Attendre que sensorStartCount soit valide avant de démarrer le capteur direct
        yield return StartCoroutine(InitializeDirectSensor());

        // Démarrer la coroutine de mise à jour du capteur direct
        StartCoroutine(DirectSensorUpdateLoop());
    }

    // Initialiser le capteur direct et attendre une valeur valide
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
        inSensorGracePeriod = true;
        sensorGraceTimer = 0f;
        Logger.LogInfo("StepManager: Direct sensor initialization complete. Grace period started.", Logger.LogCategory.StepLog);
    }

    // Mettre à jour le timestamp du capteur direct
    private void UpdateLastDirectSensorTimestamp()
    {
        // Enregistrer le timestamp actuel comme dernier point de synchronisation
        long nowEpochMs = GetCurrentEpochMs();
        dataManager.PlayerData.LastSyncEpochMs = nowEpochMs;

        // Mettre à jour aussi LastPauseEpochMs pour se protéger contre les crashs
        dataManager.PlayerData.LastPauseEpochMs = nowEpochMs;

        Logger.LogInfo($"StepManager: Updated LastSyncEpochMs and LastPauseEpochMs to current time after direct sensor update: {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)}", Logger.LogCategory.StepLog);
    }

    // Méthode pour gérer le chevauchement de minuit
    private IEnumerator HandleMidnightSplitStepCount(long startTimeMs, long endTimeMs)
    {
        // Vérifier si l'intervalle chevauche minuit
        bool spansMidnight = DoesIntervalSpanMidnight(startTimeMs, endTimeMs);

        // Déterminer si l'absence couvre plusieurs jours
        bool isMultiDayAbsence = false;
        if (spansMidnight)
        {
            // Calculer nombre de jours entre les deux dates
            DateTime startDate = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs).ToLocalTime().DateTime;
            DateTime endDate = DateTimeOffset.FromUnixTimeMilliseconds(endTimeMs).ToLocalTime().DateTime;
            TimeSpan dayDifference = endDate.Date - startDate.Date;
            isMultiDayAbsence = dayDifference.Days > 1;

            if (isMultiDayAbsence)
            {
                Logger.LogInfo($"StepManager: Multi-day absence detected ({dayDifference.Days} days). Will only count today's steps for DailySteps.", Logger.LogCategory.StepLog);
            }
        }

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
                Logger.LogWarning("StepManager: GetDeltaSinceFromAPI returned error, defaulting delta to 0.", Logger.LogCategory.StepLog);
            }
            else if (deltaApiSinceLast > MAX_STEPS_PER_UPDATE)
            {
                Logger.LogWarning($"StepManager: API returned {deltaApiSinceLast} steps, which exceeds threshold. Limiting to {MAX_STEPS_PER_UPDATE}.", Logger.LogCategory.StepLog);
                deltaApiSinceLast = MAX_STEPS_PER_UPDATE;
            }

            // Mettre à jour les deux compteurs normalement
            TotalSteps += deltaApiSinceLast;
            DailySteps += deltaApiSinceLast;

            Logger.LogInfo($"StepManager: API Catch-up - Delta: {deltaApiSinceLast}. New TotalSteps: {TotalSteps}, DailySteps: {DailySteps}", Logger.LogCategory.StepLog);
        }
        else if (isMultiDayAbsence)
        {
            // Cas: absence de plusieurs jours

            // Force explicitement la réinitialisation du compteur quotidien
            string currentDateStr = GetLocalDateString();
            Logger.LogInfo($"StepManager: **CRITICAL** Multi-day absence detected, forcing DailySteps reset. Setting LastDailyResetDate to {currentDateStr}", Logger.LogCategory.StepLog);

            // Réinitialisation explicite
            DailySteps = 0;
            dataManager.PlayerData.DailySteps = 0;
            dataManager.PlayerData.LastDailyResetDate = currentDateStr;

            // 1. Calculer le début du jour courant (00:00 aujourd'hui)
            DateTime nowLocal = DateTime.Now;
            DateTime todayMidnight = new DateTime(nowLocal.Year, nowLocal.Month, nowLocal.Day, 0, 0, 0, DateTimeKind.Local);
            long todayMidnightMs = new DateTimeOffset(todayMidnight, TimeZoneInfo.Local.GetUtcOffset(todayMidnight)).ToUnixTimeMilliseconds();

            // Créer une clé unique pour cette division spéciale
            string splitKey = $"multiday_{startTimeMs}_{todayMidnightMs}_{endTimeMs}";

            // Vérifier si nous avons déjà effectué cette requête récemment
            if (splitKey == lastMidnightSplitKey)
            {
                Logger.LogWarning($"StepManager: DUPLICATE MULTIDAY SPLIT DETECTED! Skipping to avoid double counting. SplitKey: {splitKey}", Logger.LogCategory.StepLog);
                yield break;
            }

            // Ajouter décalage de sécurité pour éviter le chevauchement
            long todayMidnightMinus = todayMidnightMs - MIDNIGHT_SAFETY_MS;
            long todayMidnightPlus = todayMidnightMs + MIDNIGHT_SAFETY_MS;

            Logger.LogInfo($"StepManager: Splitting multi-day interval at today's midnight: {LocalDatabase.GetReadableDateFromEpoch(todayMidnightMs)}", Logger.LogCategory.StepLog);

            // 2. Récupérer tous les pas passés (avant aujourd'hui)
            long stepsBeforeToday = 0;
            yield return StartCoroutine(apiCounter.GetDeltaSinceFromAPI(startTimeMs, todayMidnightMinus, (result) =>
            {
                stepsBeforeToday = result;
            }));

            // Vérifier les limites
            if (stepsBeforeToday < 0)
            {
                stepsBeforeToday = 0;
                Logger.LogWarning("StepManager: GetDeltaSinceFromAPI (before today) returned error, defaulting to 0.", Logger.LogCategory.StepLog);
            }
            else if (stepsBeforeToday > MAX_STEPS_PER_UPDATE)
            {
                Logger.LogWarning($"StepManager: API returned {stepsBeforeToday} steps before today, which exceeds threshold. Limiting to {MAX_STEPS_PER_UPDATE}.", Logger.LogCategory.StepLog);
                stepsBeforeToday = MAX_STEPS_PER_UPDATE;
            }

            // 3. Récupérer les pas d'aujourd'hui uniquement
            long stepsToday = 0;
            yield return StartCoroutine(apiCounter.GetDeltaSinceFromAPI(todayMidnightPlus, endTimeMs, (result) =>
            {
                stepsToday = result;
            }));

            // Vérifier les limites
            if (stepsToday < 0)
            {
                stepsToday = 0;
                Logger.LogWarning("StepManager: GetDeltaSinceFromAPI (today only) returned error, defaulting to 0.", Logger.LogCategory.StepLog);
            }
            else if (stepsToday > MAX_STEPS_PER_UPDATE)
            {
                Logger.LogWarning($"StepManager: API returned {stepsToday} steps for today, which exceeds threshold. Limiting to {MAX_STEPS_PER_UPDATE}.", Logger.LogCategory.StepLog);
                stepsToday = MAX_STEPS_PER_UPDATE;
            }

            // 4. Mettre à jour les compteurs correctement
            TotalSteps += (stepsBeforeToday + stepsToday); // Ajouter tous les pas au total
            DailySteps += stepsToday;                      // N'ajouter que les pas d'aujourd'hui au compteur journalier

            // Enregistrer cette split pour ne pas la répéter
            lastMidnightSplitKey = splitKey;

            Logger.LogInfo($"StepManager: Multi-day API Catch-up - Steps before today: {stepsBeforeToday}, " +
                          $"Steps today only: {stepsToday}. " +
                          $"New TotalSteps: {TotalSteps}, DailySteps: {DailySteps}", Logger.LogCategory.StepLog);

            Logger.LogInfo($"StepManager: Multi-day split intervals - Total: {LocalDatabase.GetReadableDateFromEpoch(startTimeMs)} to {LocalDatabase.GetReadableDateFromEpoch(endTimeMs)}, " +
                          $"Before today: {LocalDatabase.GetReadableDateFromEpoch(startTimeMs)} to {LocalDatabase.GetReadableDateFromEpoch(todayMidnightMinus)}, " +
                          $"Today only: {LocalDatabase.GetReadableDateFromEpoch(todayMidnightPlus)} to {LocalDatabase.GetReadableDateFromEpoch(endTimeMs)}", Logger.LogCategory.StepLog);
        }
        else
        {
            // Cas avec chevauchement de minuit sur une seule nuit : diviser en deux requêtes

            // Force explicitement la réinitialisation du compteur quotidien
            string currentDateStr = GetLocalDateString();
            Logger.LogInfo($"StepManager: **CRITICAL** Midnight boundary detected, forcing DailySteps reset. Setting LastDailyResetDate to {currentDateStr}", Logger.LogCategory.StepLog);

            // Réinitialisation explicite
            DailySteps = 0;
            dataManager.PlayerData.DailySteps = 0;
            dataManager.PlayerData.LastDailyResetDate = currentDateStr;

            // 1. Trouver le timestamp de minuit entre les deux timestamps
            long midnightMs = FindMidnightTimestamp(startTimeMs, endTimeMs);
            Logger.LogInfo($"StepManager: Interval spans midnight at {LocalDatabase.GetReadableDateFromEpoch(midnightMs)}. Splitting step counts.", Logger.LogCategory.StepLog);

            // Créer une clé unique pour cette split de minuit pour détecter les doublons
            string splitKey = $"{startTimeMs}_{midnightMs}_{endTimeMs}";

            // Vérifier si nous avons déjà effectué cette requête récemment
            if (splitKey == lastMidnightSplitKey)
            {
                Logger.LogWarning($"StepManager: DUPLICATE MIDNIGHT SPLIT DETECTED! Skipping to avoid double counting. SplitKey: {splitKey}", Logger.LogCategory.StepLog);
                yield break;
            }

            // Ajouter un décalage de sécurité pour éviter le chevauchement à minuit
            long midnightMinus = midnightMs - MIDNIGHT_SAFETY_MS;
            long midnightPlus = midnightMs + MIDNIGHT_SAFETY_MS;

            // 2. Récupérer les pas de la période avant minuit (jour précédent)
            long stepsBeforeMidnight = 0;
            yield return StartCoroutine(apiCounter.GetDeltaSinceFromAPI(startTimeMs, midnightMinus, (result) =>
            {
                stepsBeforeMidnight = result;
            }));

            // Vérifier les limites
            if (stepsBeforeMidnight < 0)
            {
                stepsBeforeMidnight = 0;
                Logger.LogWarning("StepManager: GetDeltaSinceFromAPI (before midnight) returned error, defaulting to 0.", Logger.LogCategory.StepLog);
            }
            else if (stepsBeforeMidnight > MAX_STEPS_PER_UPDATE)
            {
                Logger.LogWarning($"StepManager: API returned {stepsBeforeMidnight} steps before midnight, which exceeds threshold. Limiting to {MAX_STEPS_PER_UPDATE}.", Logger.LogCategory.StepLog);
                stepsBeforeMidnight = MAX_STEPS_PER_UPDATE;
            }

            // 3. Récupérer les pas de la période après minuit (jour courant)
            long stepsAfterMidnight = 0;
            yield return StartCoroutine(apiCounter.GetDeltaSinceFromAPI(midnightPlus, endTimeMs, (result) =>
            {
                stepsAfterMidnight = result;
            }));

            // Vérifier les limites
            if (stepsAfterMidnight < 0)
            {
                stepsAfterMidnight = 0;
                Logger.LogWarning("StepManager: GetDeltaSinceFromAPI (after midnight) returned error, defaulting to 0.", Logger.LogCategory.StepLog);
            }
            else if (stepsAfterMidnight > MAX_STEPS_PER_UPDATE)
            {
                Logger.LogWarning($"StepManager: API returned {stepsAfterMidnight} steps after midnight, which exceeds threshold. Limiting to {MAX_STEPS_PER_UPDATE}.", Logger.LogCategory.StepLog);
                stepsAfterMidnight = MAX_STEPS_PER_UPDATE;
            }

            // 4. Mettre à jour les compteurs
            TotalSteps += (stepsBeforeMidnight + stepsAfterMidnight); // Ajouter tous les pas au total
            DailySteps += stepsAfterMidnight; // N'ajouter que les pas après minuit au compteur journalier

            // Enregistrer cette split pour ne pas la répéter
            lastMidnightSplitKey = splitKey;

            Logger.LogInfo($"StepManager: API Catch-up - Steps before midnight: {stepsBeforeMidnight}, " +
                          $"Steps after midnight: {stepsAfterMidnight}. " +
                          $"New TotalSteps: {TotalSteps}, DailySteps: {DailySteps}", Logger.LogCategory.StepLog);

            // Logguer explicitement les intervalles utilisés pour le debug
            Logger.LogInfo($"StepManager: Midnight split intervals - Before: {LocalDatabase.GetReadableDateFromEpoch(startTimeMs)} to {LocalDatabase.GetReadableDateFromEpoch(midnightMinus)}, " +
                          $"After: {LocalDatabase.GetReadableDateFromEpoch(midnightPlus)} to {LocalDatabase.GetReadableDateFromEpoch(endTimeMs)}, " +
                          $"Gap: {MIDNIGHT_SAFETY_MS * 2}ms", Logger.LogCategory.StepLog);
        }

        // Sauvegarder les données mises à jour
        dataManager.PlayerData.TotalSteps = TotalSteps;
        dataManager.PlayerData.DailySteps = DailySteps;
    }

    // Vérifier si l'intervalle chevauche minuit
    private bool DoesIntervalSpanMidnight(long startTimeMs, long endTimeMs)
    {
        DateTime startDate = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs)
            .ToLocalTime().DateTime.Date;
        DateTime endDate = DateTimeOffset.FromUnixTimeMilliseconds(endTimeMs)
            .ToLocalTime().DateTime.Date;
        return startDate != endDate;
    }

    // Trouver le timestamp du minuit entre les deux timestamps
    private long FindMidnightTimestamp(long startTimeMs, long endTimeMs)
    {
        DateTime startDateTime = DateTimeOffset.FromUnixTimeMilliseconds(startTimeMs).ToLocalTime().DateTime;
        DateTime nextMidnight = startDateTime.Date.AddDays(1); // Minuit le jour suivant
        return new DateTimeOffset(nextMidnight, TimeZoneInfo.Local.GetUtcOffset(nextMidnight)).ToUnixTimeMilliseconds();
    }

    IEnumerator DirectSensorUpdateLoop()
    {
        Logger.LogInfo("StepManager: Starting DirectSensorUpdateLoop.", Logger.LogCategory.StepLog);
        lastDBSaveTime = Time.time; // Initialiser le timer de sauvegarde

        while (isAppInForeground)
        {
            yield return new WaitForSeconds(1.0f);

            // Mise à jour de la période de grâce
            if (inSensorGracePeriod)
            {
                sensorGraceTimer += 1.0f;
                if (sensorGraceTimer >= SENSOR_GRACE_PERIOD)
                {
                    inSensorGracePeriod = false;
                    Logger.LogInfo("StepManager: Sensor grace period ended - normal step counting resumed.", Logger.LogCategory.StepLog);
                }
                else
                {
                    // Pendant la période de grâce, on continue à lire les valeurs du capteur mais on ignore les changements
                    long currentRawValue = apiCounter.GetCurrentRawSensorSteps();
                    if (currentRawValue > 0 && currentRawValue != lastRecordedSensorValue)
                    {
                        Logger.LogInfo($"StepManager: [GRACE PERIOD] Ignoring sensor update from {lastRecordedSensorValue} to {currentRawValue}", Logger.LogCategory.StepLog);

                        // Mettre à jour la valeur de référence pour éviter les écarts importants après la période de grâce
                        lastRecordedSensorValue = currentRawValue;
                        sensorStartCount = currentRawValue;
                        sensorDeltaThisSession = 0;
                    }

                    // Ignorer le reste de la boucle pendant la période de grâce
                    continue;
                }
            }

            if (!apiCounter.HasPermission())
            {
                Logger.LogWarning("StepManager: Permission lost during sensor update loop. Stopping sensor.", Logger.LogCategory.StepLog);
                apiCounter.StopDirectSensorListener();
                isAppInForeground = false;
                break;
            }

            // Vérifier si sensorStartCount est valide
            if (!isSensorStartCountValid)
            {
                Logger.LogWarning("StepManager: sensorStartCount is not valid. Skipping sensor update.", Logger.LogCategory.StepLog);
                continue;
            }

            long rawSensorValue = apiCounter.GetCurrentRawSensorSteps();

            if (rawSensorValue < 0)
            {
                if (rawSensorValue == -2)
                {
                    Logger.LogError("StepManager: Direct sensor became unavailable! Stopping loop.", Logger.LogCategory.StepLog);
                    apiCounter.StopDirectSensorListener();
                    isAppInForeground = false;
                    break;
                }
                continue;
            }

            // Stocker la valeur actuelle pour référence future
            lastRecordedSensorValue = rawSensorValue;

            if (rawSensorValue < sensorStartCount)
            {
                // Amélioration de la détection des réinitialisations capteur
                // Distinguer entre réinitialisation réelle et erreur temporaire
                if (sensorStartCount - rawSensorValue > 1000)
                {
                    // Si la valeur est significativement plus petite, c'est probablement une réinitialisation
                    Logger.LogInfo($"StepManager: Sensor full reset detected. Old={sensorStartCount}, New={rawSensorValue}", Logger.LogCategory.StepLog);

                    // Sauvegarder les pas actuels avant de réinitialiser le compteur
                    long currentTime = GetCurrentEpochMs();
                    long previousApiCatchUp = dataManager.PlayerData.LastApiCatchUpEpochMs;

                    // Déclencher immédiatement un mini catch-up API pour ne pas perdre la continuité
                    Logger.LogInfo($"StepManager: Initiating mini API catch-up after sensor reset from {LocalDatabase.GetReadableDateFromEpoch(previousApiCatchUp)} to {LocalDatabase.GetReadableDateFromEpoch(currentTime)}", Logger.LogCategory.StepLog);

                    // Mise à jour des timestamps
                    sensorStartCount = rawSensorValue;
                    sensorDeltaThisSession = 0;

                    // Sauvegarder l'état actuel avant de réinitialiser
                    UpdateLastDirectSensorTimestamp();
                    dataManager.SaveGame();
                }
                else
                {
                    // Petite diminution, potentiellement une erreur de lecture
                    Logger.LogWarning($"StepManager: Unexpected sensor value decrease from {sensorStartCount} to {rawSensorValue}. Ignoring.", Logger.LogCategory.StepLog);
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
                            Logger.LogWarning($"StepManager: Anomaly detected! {newIndividualSensorSteps} steps in quick succession filtered out.", Logger.LogCategory.StepLog);
                            newIndividualSensorSteps = 0; // Ignorer cette mise à jour
                        }
                        else
                        {
                            lastLargeUpdateTime = currentTime;
                            Logger.LogInfo($"StepManager: Large step update of {newIndividualSensorSteps} accepted after debounce check.", Logger.LogCategory.StepLog);
                        }
                    }

                    // Mettre à jour les pas seulement si la valeur filtrée est valide
                    if (newIndividualSensorSteps > 0)
                    {
                        // Vérifier si l'incrément reste dans des limites raisonnables
                        if (newIndividualSensorSteps > MAX_STEPS_PER_UPDATE)
                        {
                            Logger.LogWarning($"StepManager: Unusually large step increment detected: {newIndividualSensorSteps}. Limiting to {MAX_STEPS_PER_UPDATE}.", Logger.LogCategory.StepLog);
                            newIndividualSensorSteps = MAX_STEPS_PER_UPDATE;
                        }

                        // Vérifier si nous avons changé de jour au cours de la session
                        string currentDateStr = GetLocalDateString();
                        if (dataManager.PlayerData.LastDailyResetDate != currentDateStr)
                        {
                            Logger.LogInfo($"StepManager: Day change detected during step update. Resetting daily steps to 0 and updating LastDailyResetDate.", Logger.LogCategory.StepLog);
                            DailySteps = 0;
                            dataManager.PlayerData.DailySteps = 0;
                            dataManager.PlayerData.LastDailyResetDate = currentDateStr;
                        }

                        sensorDeltaThisSession += newIndividualSensorSteps;
                        TotalSteps += newIndividualSensorSteps;
                        DailySteps += newIndividualSensorSteps;
                        Logger.LogInfo($"StepManager: New steps: {newIndividualSensorSteps}, TotalSteps: {TotalSteps}, DailySteps: {DailySteps}", Logger.LogCategory.StepLog);

                        // Sauvegarde périodique au lieu de sauvegarde à chaque mise à jour
                        dataManager.PlayerData.TotalSteps = TotalSteps;
                        dataManager.PlayerData.DailySteps = DailySteps;

                        // Mettre à jour les timestamps de dernière synchronisation et pause
                        UpdateLastDirectSensorTimestamp();

                        // Vérifier si c'est le moment de sauvegarder
                        float currentTime = Time.time;
                        if (currentTime - lastDBSaveTime >= DB_SAVE_INTERVAL)
                        {
                            dataManager.SaveGame();
                            lastDBSaveTime = currentTime;
                            Logger.LogInfo($"StepManager: Periodic DB save after {DB_SAVE_INTERVAL} seconds.", Logger.LogCategory.StepLog);
                        }
                    }
                }
            }
        }

        // Toujours sauvegarder avant de sortir de la boucle
        if (dataManager != null && dataManager.PlayerData != null)
        {
            dataManager.SaveGame();
        }

        Logger.LogInfo("StepManager: Exiting DirectSensorUpdateLoop.", Logger.LogCategory.StepLog);
    }
#endif

    // ===== MÉTHODES COMMUNES =====
    void HandleAppPausingOrClosing()
    {
        if (!isInitialized) return;

        Logger.LogInfo("StepManager: HandleAppPausingOrClosing started.", Logger.LogCategory.StepLog);

#if UNITY_EDITOR
        // En éditeur, pas besoin de gérer les capteurs
        Logger.LogInfo("StepManager: [EDITOR] App pausing/closing - simple save", Logger.LogCategory.StepLog);
#else
        isAppInForeground = false;

        // Arrêter le capteur direct quand l'application n'est plus au premier plan
        apiCounter.StopDirectSensorListener();
        Logger.LogInfo("StepManager: Direct sensor listener stopped.", Logger.LogCategory.StepLog);
#endif

        // Enregistrer le timestamp de pause pour résoudre le problème de double comptage
        long nowEpochMs = GetCurrentEpochMs();
        dataManager.PlayerData.LastSyncEpochMs = nowEpochMs;
        dataManager.PlayerData.LastPauseEpochMs = nowEpochMs;

#if !UNITY_EDITOR
        // IMPORTANT: Mettre également à jour LastApiCatchUpEpochMs pour éviter le double comptage
        // lors du prochain retour de l'arrière-plan
        dataManager.PlayerData.LastApiCatchUpEpochMs = nowEpochMs;
        Logger.LogInfo($"StepManager: Updated LastApiCatchUpEpochMs to {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)} when going to background", Logger.LogCategory.StepLog);
#endif

        // Force une sauvegarde à chaque pause/fermeture pour s'assurer que les données sont persistées
        dataManager.PlayerData.TotalSteps = TotalSteps;
        dataManager.PlayerData.DailySteps = DailySteps;
        Logger.LogInfo($"StepManager: Saving steps. Final TotalSteps: {TotalSteps}, DailySteps: {DailySteps}, " +
                       $"LastPauseEpochMs: {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)}", Logger.LogCategory.StepLog);
        dataManager.SaveGame();
        Logger.LogInfo("StepManager: Data saved on pause/close.", Logger.LogCategory.StepLog);
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!isInitialized) return;

        if (pauseStatus)
        {
            // L'application va en arrière-plan
            Logger.LogInfo("StepManager: OnApplicationPause → app goes to background", Logger.LogCategory.StepLog);
            HandleAppPausingOrClosing();
        }
        else
        {
            // L'application revient au premier plan
            Logger.LogInfo("StepManager: OnApplicationPause → app returns to foreground", Logger.LogCategory.StepLog);

#if !UNITY_EDITOR
            // Marquer que l'application retourne au premier plan après avoir été en arrière-plan
            isReturningFromBackground = true;

            if (isInitialized)
            {
                StartCoroutine(HandleAppOpeningOrResuming());
            }
#endif
        }
    }

    void OnApplicationQuit()
    {
#if UNITY_EDITOR
        // En éditeur, toujours sauvegarder
        HandleAppPausingOrClosing();
#else
        if (isAppInForeground)
        {
            HandleAppPausingOrClosing();
        }
#endif
        Logger.LogInfo("StepManager: Application Quitting.", Logger.LogCategory.StepLog);
    }

    // Obtenir la date locale actuelle au format "yyyy-MM-dd"
    private string GetLocalDateString()
    {
        return DateTime.Now.ToString("yyyy-MM-dd");
    }

    // Obtenir l'horodatage Unix actuel en millisecondes
    private long GetCurrentEpochMs()
    {
        return new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
    }
}