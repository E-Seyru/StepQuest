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

    private bool isInitialized = false;
    private bool isAppInForeground = true;
    private bool isReturningFromBackground = false;
    private bool isSensorStartCountValid = false; // Nouveau flag pour s'assurer que sensorStartCount est valide

    // Paramètres de débounce et filtrage
    private const int SENSOR_SPIKE_THRESHOLD = 50; // Nombre de pas considéré comme anormal en une seule mise à jour
    private const int SENSOR_DEBOUNCE_SECONDS = 3; // Temps minimum entre deux grandes variations
    private float lastLargeUpdateTime = 0f;
    private const long MAX_STEPS_PER_UPDATE = 1000; // Limite raisonnable pour une sauvegarde unique

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

    void Awake()
    {
        // Protection contre les instances multiples (Faille E)
        if (Instance != null && Instance != this)
        {
            Logger.LogWarning("StepManager: Multiple instances detected! Destroying duplicate.");
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

        // Charger les données actuelles
        TotalSteps = dataManager.PlayerData.TotalSteps;
        DailySteps = dataManager.PlayerData.DailySteps;
        long lastSyncEpochMs = dataManager.PlayerData.LastSyncEpochMs;
        long lastPauseEpochMs = dataManager.PlayerData.LastPauseEpochMs;
        long nowEpochMs = GetCurrentEpochMs();

        // MODIFIÉ: Charger lastApiCatchUpEpochMs depuis PlayerData
        lastApiCatchUpEpochMs = dataManager.PlayerData.LastApiCatchUpEpochMs;
        Logger.LogInfo($"StepManager: lastApiCatchUpEpochMs loaded from database: {LocalDatabase.GetReadableDateFromEpoch(lastApiCatchUpEpochMs)}");

        // NOUVEAU: Détection de crash (Faille A)
        // Si LastPauseEpochMs est antérieur à LastSyncEpochMs, l'application a probablement crashé
        if (lastPauseEpochMs < lastSyncEpochMs)
        {
            wasProbablyCrash = true;
            Logger.LogWarning($"StepManager: Probable crash detected! LastPause ({LocalDatabase.GetReadableDateFromEpoch(lastPauseEpochMs)}) < LastSync ({LocalDatabase.GetReadableDateFromEpoch(lastSyncEpochMs)})");
        }

        // Vérifier explicitement si le jour a changé en utilisant TimeZoneInfo.Local (Faille B)
        string currentDateStr = GetLocalDateString();
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

        Logger.LogInfo($"StepManager: Loaded state - Initial TotalSteps: {TotalSteps}, DailySteps: {DailySteps}, " +
                      $"LastSync: {LocalDatabase.GetReadableDateFromEpoch(lastSyncEpochMs)}, " +
                      $"LastPause: {LocalDatabase.GetReadableDateFromEpoch(lastPauseEpochMs)}, " +
                      $"LastApiCatchUp: {LocalDatabase.GetReadableDateFromEpoch(lastApiCatchUpEpochMs)}, " +
                      $"Now: {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)}");

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
            lastApiCatchUpEpochMs = nowEpochMs; // Initialiser aussi lastApiCatchUpEpochMs
            dataManager.SaveGame();
            Logger.LogInfo($"StepManager: API Catch-up skipped for first start. TotalSteps: {TotalSteps}. Updated LastSync to: {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)}");
        }
        else
        {
            // Pour tous les autres cas (y compris retour d'arrière-plan), récupérer les pas via l'API

            // S'assurer que LastSync a une valeur valide
            if (lastApiCatchUpEpochMs <= 0)
            {
                // Si on a des pas mais pas de timestamp valide, on utilise une date récente (24h avant)
                Logger.LogInfo("StepManager: LastApiCatchUpEpochMs value invalid (0) but TotalSteps > 0. Setting to 24h ago.");
                lastApiCatchUpEpochMs = nowEpochMs - (24 * 60 * 60 * 1000); // 24h en millisecondes
            }

            // IMPORTANT: Pour éviter le double comptage après un crash (Faille A)
            // Si un crash est détecté, utiliser LastPauseEpochMs comme borne de départ
            long startTimeMs = wasProbablyCrash ? lastPauseEpochMs : lastApiCatchUpEpochMs;
            long endTimeMs = nowEpochMs;

            // NOUVEAU: Ne faire un catch-up API que si nécessaire (éviter le double comptage)
            // Si LastSyncEpochMs est plus récent que LastApiCatchUpEpochMs, cela signifie que le capteur direct
            // a enregistré des pas pendant cette période, donc pas besoin de catch-up API
            if (lastSyncEpochMs > lastApiCatchUpEpochMs && !wasProbablyCrash)
            {
                Logger.LogInfo($"StepManager: Skipping API catch-up because direct sensor was active from {LocalDatabase.GetReadableDateFromEpoch(lastApiCatchUpEpochMs)} to {LocalDatabase.GetReadableDateFromEpoch(lastSyncEpochMs)}");

                // Mettre à jour LastApiCatchUpEpochMs pour la cohérence
                lastApiCatchUpEpochMs = nowEpochMs;
                dataManager.PlayerData.LastApiCatchUpEpochMs = lastApiCatchUpEpochMs;
                dataManager.SaveGame();
                Logger.LogInfo($"StepManager: Updated LastApiCatchUpEpochMs to now: {LocalDatabase.GetReadableDateFromEpoch(lastApiCatchUpEpochMs)} without API catch-up");
            }
            // Ne récupérer les pas que s'il y a un intervalle de temps valide
            else if (endTimeMs > startTimeMs)
            {
                // Récupérer les pas depuis la dernière synchronisation jusqu'à maintenant
                // avec gestion spéciale en cas de chevauchement de minuit
                if (isReturningFromBackground)
                {
                    Logger.LogInfo($"StepManager: RETURNING FROM BACKGROUND - Starting API Catch-up from {LocalDatabase.GetReadableDateFromEpoch(startTimeMs)} to {LocalDatabase.GetReadableDateFromEpoch(endTimeMs)}");
                }
                else
                {
                    Logger.LogInfo($"StepManager: Starting API Catch-up from {LocalDatabase.GetReadableDateFromEpoch(startTimeMs)} to {LocalDatabase.GetReadableDateFromEpoch(endTimeMs)}");
                }

                yield return StartCoroutine(HandleMidnightSplitStepCount(startTimeMs, endTimeMs));

                // NOUVEAU: Après un catch-up API réussi, mettre à jour le timestamp dédié (Faille A)
                lastApiCatchUpEpochMs = nowEpochMs;
                // Sauvegarder la valeur dans PlayerData
                dataManager.PlayerData.LastApiCatchUpEpochMs = lastApiCatchUpEpochMs;
                Logger.LogInfo($"StepManager: Updated lastApiCatchUpEpochMs to: {LocalDatabase.GetReadableDateFromEpoch(lastApiCatchUpEpochMs)}");

                // Appel direct à la méthode ClearStoredRange de RecordingAPIStepCounter
                apiCounter.ClearStoredRange();

                // Sauvegarde immédiate pour éviter la perte de l'horodatage en cas de crash
                // juste après un catch-up (Faille #2 du document)
                dataManager.SaveGame();
                Logger.LogInfo("StepManager: Saved LastApiCatchUpEpochMs immediately after API catch-up to prevent loss on crash");
            }
            else
            {
                Logger.LogInfo("StepManager: API Catch-up skipped - no valid time interval available or app was just paused very recently.");
            }

            // Toujours mettre à jour LastSyncEpochMs et LastPauseEpochMs au temps actuel
            dataManager.PlayerData.LastSyncEpochMs = nowEpochMs;
            dataManager.PlayerData.LastPauseEpochMs = nowEpochMs;
            dataManager.SaveGame();
            Logger.LogInfo($"StepManager: Updated LastSync and LastPause to: {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)}");
        }

        // Si on revient de l'arrière-plan, activer la période de grâce pour le sensor
        if (isReturningFromBackground)
        {
            Logger.LogInfo("StepManager: Activating sensor grace period after returning from background.");
            inSensorGracePeriod = true;
            sensorGraceTimer = 0f;
            isSensorStartCountValid = false; // Réinitialiser le flag
            // Reset le flag
            isReturningFromBackground = false;
            wasProbablyCrash = false; // Réinitialiser également ce flag
        }

        // AMÉLIORÉ: Attendre que sensorStartCount soit valide avant de démarrer le capteur direct (Faille D)
        yield return StartCoroutine(InitializeDirectSensor());

        // Démarrer la coroutine de mise à jour du capteur direct
        StartCoroutine(DirectSensorUpdateLoop());
    }

    // NOUVELLE méthode: Initialiser le capteur direct et attendre une valeur valide (Faille D)
    private IEnumerator InitializeDirectSensor()
    {
        Logger.LogInfo("StepManager: Initializing direct sensor...");

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

                        Logger.LogInfo($"StepManager: sensorStartCount successfully initialized to {sensorStartCount} after {stableReadingCount} stable readings");
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
            Logger.LogInfo($"StepManager: Couldn't get stable readings. Using last sensor value: {sensorStartCount}");
        }
        else if (!isSensorStartCountValid)
        {
            Logger.LogWarning("StepManager: Couldn't initialize sensorStartCount with a valid value.");
            sensorStartCount = -1;
            lastRecordedSensorValue = -1;
            sensorDeltaThisSession = 0;
        }

        // La période de grâce commence après l'initialisation du capteur
        inSensorGracePeriod = true;
        sensorGraceTimer = 0f;
        Logger.LogInfo("StepManager: Direct sensor initialization complete. Grace period started.");
    }

    // Nouvelle méthode pour mettre à jour le timestamp du capteur direct
    private void UpdateLastDirectSensorTimestamp()
    {
        // Enregistrer le timestamp actuel comme dernier point de synchronisation
        long nowEpochMs = GetCurrentEpochMs();
        dataManager.PlayerData.LastSyncEpochMs = nowEpochMs;

        // NOUVEAU: Mettre à jour aussi LastPauseEpochMs pour se protéger contre les crashs (Faille A)
        dataManager.PlayerData.LastPauseEpochMs = nowEpochMs;

        Logger.LogInfo($"StepManager: Updated LastSyncEpochMs and LastPauseEpochMs to current time after direct sensor update: {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)}");
    }

    // NOUVELLE méthode: Effacer la valeur stockée dans le plugin pour éviter le double comptage (Faille C)
    private void ClearStoredStepsRangeInPlugin()
    {
        if (!isPluginFunctionAvailable("clearStoredRange"))
        {
            Logger.LogWarning("StepManager: clearStoredRange function not available in plugin. Double counting may occur!");
            return;
        }

        try
        {
            apiCounter.GetType().GetMethod("ClearStoredRange").Invoke(apiCounter, null);
            Logger.LogInfo("StepManager: Successfully cleared stored range in plugin.");
        }
        catch (Exception ex)
        {
            Logger.LogError($"StepManager: Failed to clear stored range in plugin. Exception: {ex.Message}");
        }
    }

    // Helper pour vérifier si une fonction du plugin est disponible
    private bool isPluginFunctionAvailable(string functionName)
    {
        // Simple mock pour simuler la disponibilité de la fonction
        // Dans l'implémentation réelle, cette méthode vérifierait si la fonction existe dans le plugin
        return false; // Supposons que la fonction n'est pas disponible pour l'instant
    }

    // Méthode pour gérer le chevauchement de minuit
    private IEnumerator HandleMidnightSplitStepCount(long startTimeMs, long endTimeMs)
    {
        // Vérifier si l'intervalle chevauche minuit en utilisant TimeZoneInfo.Local (Faille B)
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
            string currentDateStr = GetLocalDateString();
            Logger.LogInfo($"StepManager: **CRITICAL** Midnight boundary detected, forcing DailySteps reset. Setting LastDailyResetDate to {currentDateStr}");

            // Réinitialisation explicite
            DailySteps = 0;
            dataManager.PlayerData.DailySteps = 0;
            dataManager.PlayerData.LastDailyResetDate = currentDateStr;

            // 1. Trouver le timestamp de minuit entre les deux timestamps
            long midnightMs = FindMidnightTimestamp(startTimeMs, endTimeMs);
            Logger.LogInfo($"StepManager: Interval spans midnight at {LocalDatabase.GetReadableDateFromEpoch(midnightMs)}. Splitting step counts.");

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

            // NOUVEAU: Effacer la valeur stockée dans le plugin (Faille C)
            ClearStoredStepsRangeInPlugin();

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
        Logger.LogInfo("StepManager: Starting DirectSensorUpdateLoop.");
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
                    Logger.LogInfo("StepManager: Sensor grace period ended - normal step counting resumed.");
                }
                else
                {
                    // Pendant la période de grâce, on continue à lire les valeurs du capteur mais on ignore les changements
                    long currentRawValue = apiCounter.GetCurrentRawSensorSteps();
                    if (currentRawValue > 0 && currentRawValue != lastRecordedSensorValue)
                    {
                        Logger.LogInfo($"StepManager: [GRACE PERIOD] Ignoring sensor update from {lastRecordedSensorValue} to {currentRawValue}");

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
                Logger.LogWarning("StepManager: Permission lost during sensor update loop. Stopping sensor.");
                apiCounter.StopDirectSensorListener();
                isAppInForeground = false;
                break;
            }

            // Vérifier si sensorStartCount est valide
            if (!isSensorStartCountValid)
            {
                Logger.LogWarning("StepManager: sensorStartCount is not valid. Skipping sensor update.");
                continue;
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

            // Stocker la valeur actuelle pour référence future
            lastRecordedSensorValue = rawSensorValue;

            if (rawSensorValue < sensorStartCount)
            {
                // Amélioration de la détection des réinitialisations capteur
                // Distinguer entre réinitialisation réelle et erreur temporaire
                if (sensorStartCount - rawSensorValue > 1000)
                {
                    // Si la valeur est significativement plus petite, c'est probablement une réinitialisation
                    Logger.LogInfo($"StepManager: Sensor full reset detected. Old={sensorStartCount}, New={rawSensorValue}");

                    // Sauvegarder les pas actuels avant de réinitialiser le compteur
                    long currentTime = GetCurrentEpochMs();
                    long previousApiCatchUp = dataManager.PlayerData.LastApiCatchUpEpochMs;

                    // Déclencher immédiatement un mini catch-up API pour ne pas perdre la continuité
                    Logger.LogInfo($"StepManager: Initiating mini API catch-up after sensor reset from {LocalDatabase.GetReadableDateFromEpoch(previousApiCatchUp)} to {LocalDatabase.GetReadableDateFromEpoch(currentTime)}");

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
                        string currentDateStr = GetLocalDateString();
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

                        // AMÉLIORÉ: Sauvegarde périodique au lieu de sauvegarde à chaque mise à jour
                        dataManager.PlayerData.TotalSteps = TotalSteps;
                        dataManager.PlayerData.DailySteps = DailySteps;

                        // NOUVELLE LIGNE: Mettre à jour les timestamps de dernière synchronisation et pause
                        UpdateLastDirectSensorTimestamp();

                        // Vérifier si c'est le moment de sauvegarder
                        float currentTime = Time.time;
                        if (currentTime - lastDBSaveTime >= DB_SAVE_INTERVAL)
                        {
                            dataManager.SaveGame();
                            lastDBSaveTime = currentTime;
                            Logger.LogInfo($"StepManager: Periodic DB save after {DB_SAVE_INTERVAL} seconds.");
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

        Logger.LogInfo("StepManager: Exiting DirectSensorUpdateLoop.");
    }

    void HandleAppPausingOrClosing()
    {
        if (!isInitialized) return;

        Logger.LogInfo("StepManager: HandleAppPausingOrClosing started.");
        isAppInForeground = false;

        // Arrêter le capteur direct quand l'application n'est plus au premier plan
        apiCounter.StopDirectSensorListener();
        Logger.LogInfo("StepManager: Direct sensor listener stopped.");

        // Enregistrer le timestamp de pause pour résoudre le problème de double comptage
        long nowEpochMs = GetCurrentEpochMs();
        dataManager.PlayerData.LastSyncEpochMs = nowEpochMs;
        dataManager.PlayerData.LastPauseEpochMs = nowEpochMs;

        // IMPORTANT: Mettre également à jour LastApiCatchUpEpochMs pour éviter le double comptage
        // lors du prochain retour de l'arrière-plan (Faille #1 du document)
        dataManager.PlayerData.LastApiCatchUpEpochMs = nowEpochMs;
        Logger.LogInfo($"StepManager: Updated LastApiCatchUpEpochMs to {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)} when going to background");

        // Force une sauvegarde à chaque pause/fermeture pour s'assurer que les données sont persistées
        dataManager.PlayerData.TotalSteps = TotalSteps;
        dataManager.PlayerData.DailySteps = DailySteps;
        Logger.LogInfo($"StepManager: Saving steps. Final TotalSteps: {TotalSteps}, DailySteps: {DailySteps}, " +
                       $"LastPauseEpochMs: {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)}");
        dataManager.SaveGame();
        Logger.LogInfo("StepManager: Data saved on pause/close.");
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (!isInitialized) return;

        if (pauseStatus)
        {
            // L'application va en arrière-plan
            Logger.LogInfo("StepManager: OnApplicationPause → app goes to background");
            HandleAppPausingOrClosing();
        }
        else
        {
            // L'application revient au premier plan
            Logger.LogInfo("StepManager: OnApplicationPause → app returns to foreground");

            // Marquer que l'application retourne au premier plan après avoir été en arrière-plan
            isReturningFromBackground = true;

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

    // MODIFIÉ: Harmoniser la gestion des fuseaux horaires (Faille B)

    // Obtenir la date locale actuelle au format "yyyy-MM-dd"
    private string GetLocalDateString()
    {
        return DateTime.Now.ToString("yyyy-MM-dd");
    }

    // Obtenir l'horodatage Unix actuel en millisecondes
    // MODIFIÉ: Utiliser DateTime.Now pour cohérence avec le reste du code
    private long GetCurrentEpochMs()
    {
        return new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds();
    }
}