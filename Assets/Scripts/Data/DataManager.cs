// ===============================================
// 1. NOUVEAU DataManager (Facade Pattern)
// ===============================================
// Purpose: Main manager for all data operations - REFACTORED
// Filepath: Assets/Scripts/Data/DataManager.cs

using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    // === SAME PUBLIC API - ZERO BREAKING CHANGES ===
    public PlayerData PlayerData { get; private set; }
    public LocalDatabase LocalDatabase => databaseService?.LocalDatabase;

    // === THREAD SAFETY ===
    private static readonly object playerDataLock = new object();

    // === INTERNAL SERVICES (NOUVEAU) ===
    private DataManagerDatabaseService databaseService;
    private DataManagerPlayerDataService playerDataService;
    private DataManagerValidationService validationService;
    private DataManagerSaveLoadService saveLoadService;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            InitializeServices();
        }
        else
        {
            Logger.LogWarning("DataManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.General);
            Destroy(gameObject);
        }
    }

    private void InitializeServices()
    {
        // Initialiser les services dans l'ordre
        databaseService = new DataManagerDatabaseService();
        validationService = new DataManagerValidationService();
        playerDataService = new DataManagerPlayerDataService(databaseService, validationService);
        saveLoadService = new DataManagerSaveLoadService(databaseService, validationService);

        // Initialiser la base de donnees
        databaseService.Initialize();

        // Charger les donnees du joueur
        PlayerData = playerDataService.LoadPlayerData();

        Logger.LogInfo("DataManager initialized and game data loaded.", Logger.LogCategory.General);
        if (PlayerData != null)
        {
            Logger.LogInfo($"DataManager: Loaded PlayerData - TotalSteps: {PlayerData.TotalSteps}, " +
                          $"LastSync: {LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastSyncEpochMs)}, " +
                          $"LastPause: {LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastPauseEpochMs)}, " +
                          $"LastChange: {LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastStepsChangeEpochMs)}, " +
                          $"DailySteps: {PlayerData.DailySteps}", Logger.LogCategory.General);

            if (PlayerData.IsCurrentlyTraveling())
            {
                long travelProgress = PlayerData.GetTravelProgress(PlayerData.TotalSteps);
                Logger.LogInfo($"DataManager: Travel state loaded - Destination: {PlayerData.TravelDestinationId}, " +
                              $"Progress: {travelProgress}/{PlayerData.TravelRequiredSteps} steps", Logger.LogCategory.General);
            }
        }
    }

    // === PUBLIC API (DELEGATED TO SERVICES) ===

    public void CheckAndResetDailySteps()
    {
        Logger.LogInfo("DataManager: CheckAndResetDailySteps called but ignored - this functionality is now handled by StepManager", Logger.LogCategory.General);
    }

    public async Task SaveGameAsync()
    {
        await saveLoadService.SaveGameAsync(PlayerData);
    }

    public void SaveGame()
    {
        saveLoadService.SaveGame(PlayerData);
    }

    public void SaveTravelProgress()
    {
        saveLoadService.SaveTravelProgress(PlayerData);
    }

    public void ForceSave()
    {
        Logger.LogInfo("DataManager: Force save requested", Logger.LogCategory.General);
        SaveGame();
    }

    public void ForceSaveTravelProgress()
    {
        if (!PlayerData.IsCurrentlyTraveling())
        {
            Logger.LogWarning("DataManager: ForceSaveTravelProgress called but not currently traveling!", Logger.LogCategory.General);
            return;
        }

        Logger.LogInfo("DataManager: Force save travel progress requested", Logger.LogCategory.General);
        SaveTravelProgress();
    }

    public string GetTravelStateDebugInfo()
    {
        return playerDataService.GetTravelStateDebugInfo(PlayerData);
    }

    // === THREAD-SAFE PLAYERDATA MODIFICATION METHODS ===

    /// <summary>
    /// Thread-safe method to update step counts.
    /// Use this instead of directly modifying PlayerData.TotalSteps/DailySteps.
    /// </summary>
    public void UpdateSteps(long totalSteps, long dailySteps, long lastStepsDelta = 0)
    {
        lock (playerDataLock)
        {
            PlayerData.TotalSteps = totalSteps;
            PlayerData.DailySteps = dailySteps;
            if (lastStepsDelta != 0)
            {
                PlayerData.LastStepsDelta = lastStepsDelta;
                PlayerData.LastStepsChangeEpochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }
    }

    /// <summary>
    /// Thread-safe method to start a travel.
    /// Returns false if player is doing an activity (invalid state).
    /// </summary>
    public bool StartTravel(string destinationId, int requiredSteps, long currentSteps,
                            string finalDestinationId = null, string originLocationId = null)
    {
        lock (playerDataLock)
        {
            // Validation: Cannot travel while doing activity
            if (PlayerData.HasActiveActivity())
            {
                Logger.LogWarning("DataManager: Cannot start travel - player has active activity!", Logger.LogCategory.General);
                return false;
            }

            PlayerData.TravelDestinationId = destinationId;
            PlayerData.TravelStartSteps = currentSteps;
            PlayerData.TravelRequiredSteps = requiredSteps;
            PlayerData.TravelFinalDestinationId = finalDestinationId;
            PlayerData.TravelOriginLocationId = originLocationId;

            return true;
        }
    }

    /// <summary>
    /// Thread-safe method to complete current travel segment.
    /// </summary>
    public void CompleteTravel()
    {
        lock (playerDataLock)
        {
            PlayerData.CompleteTravel();
        }
    }

    /// <summary>
    /// Thread-safe method to cancel travel.
    /// </summary>
    public void CancelTravel()
    {
        lock (playerDataLock)
        {
            PlayerData.CancelTravel();
        }
    }

    /// <summary>
    /// Thread-safe method to start an activity.
    /// Returns false if player is traveling (invalid state).
    /// </summary>
    public bool StartActivity(ActivityData activityData)
    {
        lock (playerDataLock)
        {
            // Validation: Cannot do activity while traveling
            if (PlayerData.IsCurrentlyTraveling())
            {
                Logger.LogWarning("DataManager: Cannot start activity - player is traveling!", Logger.LogCategory.General);
                return false;
            }

            PlayerData.CurrentActivity = activityData;
            return true;
        }
    }

    /// <summary>
    /// Thread-safe method to update activity progress.
    /// </summary>
    public void UpdateActivity(ActivityData activityData)
    {
        lock (playerDataLock)
        {
            PlayerData.CurrentActivity = activityData;
        }
    }

    /// <summary>
    /// Thread-safe method to stop current activity.
    /// </summary>
    public void StopActivity()
    {
        lock (playerDataLock)
        {
            PlayerData.StopActivity();
        }
    }

    /// <summary>
    /// Thread-safe read of current state for checks.
    /// </summary>
    public (bool isTraveling, bool hasActivity, long totalSteps) GetCurrentState()
    {
        lock (playerDataLock)
        {
            return (PlayerData.IsCurrentlyTraveling(),
                    PlayerData.HasActiveActivity(),
                    PlayerData.TotalSteps);
        }
    }

    /// <summary>
    /// Thread-safe method to reset daily steps (called at midnight).
    /// </summary>
    public void ResetDailySteps(string newDateStr)
    {
        lock (playerDataLock)
        {
            PlayerData.DailySteps = 0;
            PlayerData.LastDailyResetDate = newDateStr;
        }
    }

    /// <summary>
    /// Thread-safe method to update location after travel completion.
    /// </summary>
    public void SetCurrentLocation(string locationId)
    {
        lock (playerDataLock)
        {
            PlayerData.CurrentLocationId = locationId;
        }
    }

    void OnApplicationQuit()
    {
        if (PlayerData != null)
        {
            Logger.LogInfo("DataManager: Application quitting, ensuring data is saved.", Logger.LogCategory.General);

            if (PlayerData.IsCurrentlyTraveling())
            {
                Logger.LogInfo("DataManager: Saving travel progress before quit.", Logger.LogCategory.General);
                ForceSaveTravelProgress();
            }

            SaveGame();
        }

        databaseService?.Cleanup();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && PlayerData != null)
        {
            Logger.LogInfo("DataManager: Application pausing, saving data.", Logger.LogCategory.General);

            if (PlayerData.IsCurrentlyTraveling())
            {
                Logger.LogInfo("DataManager: Saving travel progress before pause.", Logger.LogCategory.General);
                ForceSaveTravelProgress();
            }

            SaveGame();
        }
    }
}

// ===============================================
// 2. SERVICE: Database Management
// ===============================================
public class DataManagerDatabaseService
{
    public LocalDatabase LocalDatabase { get; private set; }

    public void Initialize()
    {
        LocalDatabase = new LocalDatabase();
        LocalDatabase.InitializeDatabase();

#if UNITY_EDITOR
        Logger.LogInfo("DataManager: Running in Editor mode with LocalDatabase support", Logger.LogCategory.General);
#endif
    }

    public void Cleanup()
    {
        LocalDatabase?.CloseDatabase();
    }
}

// ===============================================
// 3. SERVICE: PlayerData Management
// ===============================================
public class DataManagerPlayerDataService
{
    private DataManagerDatabaseService databaseService;
    private DataManagerValidationService validationService;

    public DataManagerPlayerDataService(DataManagerDatabaseService databaseService, DataManagerValidationService validationService)
    {
        this.databaseService = databaseService;
        this.validationService = validationService;
    }

    public PlayerData LoadPlayerData()
    {
        if (databaseService.LocalDatabase == null)
        {
            Logger.LogError("DataManager: Cannot load game, LocalDatabase is not initialized.", Logger.LogCategory.General);
            return new PlayerData();
        }

        PlayerData playerData = databaseService.LocalDatabase.LoadPlayerData();

        Logger.LogInfo($"DataManager: LoadGame → loaded TotalSteps={playerData.TotalSteps}, " +
                      $"LastSync={LocalDatabase.GetReadableDateFromEpoch(playerData.LastSyncEpochMs)}, " +
                      $"LastPause={LocalDatabase.GetReadableDateFromEpoch(playerData.LastPauseEpochMs)}, " +
                      $"LastChange={LocalDatabase.GetReadableDateFromEpoch(playerData.LastStepsChangeEpochMs)}, " +
                      $"DailySteps={playerData.DailySteps}, LastResetDate={playerData.LastDailyResetDate}", Logger.LogCategory.General);

        // Fixes pour les nouvelles installations
        if (playerData.Id == 0)
        {
            playerData.Id = 1;
            databaseService.LocalDatabase.SavePlayerData(playerData);
            Logger.LogInfo("DataManager: Fixed PlayerData Id and saved.", Logger.LogCategory.General);
        }

        if (string.IsNullOrEmpty(playerData.LastDailyResetDate))
        {
            playerData.LastDailyResetDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            Logger.LogInfo($"DataManager: Initialized LastDailyResetDate to {playerData.LastDailyResetDate}", Logger.LogCategory.General);
            databaseService.LocalDatabase.SavePlayerData(playerData);
        }

        return playerData;
    }

    public string GetTravelStateDebugInfo(PlayerData playerData)
    {
        if (!playerData.IsCurrentlyTraveling())
        {
            return "No active travel";
        }

        long travelProgress = playerData.GetTravelProgress(playerData.TotalSteps);
        float progressPercent = (float)travelProgress / playerData.TravelRequiredSteps * 100f;

        return $"Travel to {playerData.TravelDestinationId}: {travelProgress}/{playerData.TravelRequiredSteps} steps ({progressPercent:F1}%)";
    }
}

// ===============================================
// 4. SERVICE: Validation
// ===============================================
public class DataManagerValidationService
{
    // Constantes pour la detection d'anomalies (DeSACTIVeES EN eDITEUR)
    private const long MAX_ACCEPTABLE_STEPS_DELTA = GameConstants.MaxAcceptableStepsDelta;
    private const long MAX_ACCEPTABLE_DAILY_STEPS = GameConstants.MaxAcceptableDailySteps;

    public void ValidatePlayerData(PlayerData playerData)
    {
#if !UNITY_EDITOR
        long nowEpochMs = GetCurrentEpochMs();

        // Validation des timestamps
        if (playerData.LastSyncEpochMs > nowEpochMs + 3600000 || // Plus d'1h dans le futur
            playerData.LastPauseEpochMs > nowEpochMs + 3600000 ||
            playerData.LastStepsChangeEpochMs > nowEpochMs + 3600000)
        {
            Logger.LogWarning($"DataManager: Future timestamps detected! " +
                             $"LastSync: {LocalDatabase.GetReadableDateFromEpoch(playerData.LastSyncEpochMs)}, " +
                             $"LastPause: {LocalDatabase.GetReadableDateFromEpoch(playerData.LastPauseEpochMs)}, " +
                             $"LastChange: {LocalDatabase.GetReadableDateFromEpoch(playerData.LastStepsChangeEpochMs)}, " +
                             $"Now: {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)}. Resetting to now.", Logger.LogCategory.General);

            playerData.LastSyncEpochMs = nowEpochMs;
            playerData.LastPauseEpochMs = nowEpochMs;
            playerData.LastStepsChangeEpochMs = nowEpochMs;
        }

        if (playerData.IsCurrentlyTraveling())
        {
            ValidateTravelState(playerData);
        }
#endif
    }

    public bool ValidateTravelState(PlayerData playerData)
    {
        if (!playerData.IsCurrentlyTraveling())
        {
            Logger.LogWarning("DataManager: ValidateTravelState called but not currently traveling!", Logger.LogCategory.General);
            return false;
        }

        // Validation basique de l'etat de voyage
        if (string.IsNullOrEmpty(playerData.TravelDestinationId))
        {
            Logger.LogError("DataManager: Travel state corrupted - no destination!", Logger.LogCategory.General);
            return false;
        }

        if (playerData.TravelRequiredSteps <= 0)
        {
            Logger.LogError("DataManager: Travel state corrupted - invalid required steps!", Logger.LogCategory.General);
            return false;
        }

        long travelProgress = playerData.GetTravelProgress(playerData.TotalSteps);
        if (travelProgress < 0)
        {
            Logger.LogWarning("DataManager: Travel progress negative, capping to 0", Logger.LogCategory.General);
            return false;
        }

        return true;
    }

    private static long GetCurrentEpochMs()
    {
        return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
    }
}

// ===============================================
// 5. SERVICE: Save/Load Operations
// ===============================================
public class DataManagerSaveLoadService
{
    private DataManagerDatabaseService databaseService;
    private DataManagerValidationService validationService;

    public DataManagerSaveLoadService(DataManagerDatabaseService databaseService, DataManagerValidationService validationService)
    {
        this.databaseService = databaseService;
        this.validationService = validationService;
    }

    public async Task SaveGameAsync(PlayerData playerData)
    {
        // Bail out fast if something is obviously wrong
        if (playerData == null || databaseService.LocalDatabase == null) return;

        // ---- TAKE A SNAPSHOT ON THE MAIN THREAD ----
        // Serialise & deserialise is the quickest "poor-man's deep clone"
        PlayerData snapshot = JsonConvert.DeserializeObject<PlayerData>(
            JsonConvert.SerializeObject(playerData));

        // ---- RUN THE SQLITE WRITE ON A WORKER THREAD ----
        await Task.Run(() => databaseService.LocalDatabase.SavePlayerData(snapshot));
    }

    public void SaveGame(PlayerData playerData)
    {
        if (playerData == null)
        {
            Logger.LogError("DataManager: Cannot save game, PlayerData is null.", Logger.LogCategory.General);
            return;
        }

        if (databaseService.LocalDatabase == null)
        {
            Logger.LogError("DataManager: Cannot save game, LocalDatabase is not initialized.", Logger.LogCategory.General);
            return;
        }

        if (playerData.Id <= 0)
        {
            playerData.Id = 1;
        }

        try
        {
            validationService.ValidatePlayerData(playerData);

            Logger.LogInfo($"DataManager: SaveGame → saving TotalSteps={playerData.TotalSteps}, " +
                          $"LastSync={LocalDatabase.GetReadableDateFromEpoch(playerData.LastSyncEpochMs)}, " +
                          $"LastPause={LocalDatabase.GetReadableDateFromEpoch(playerData.LastPauseEpochMs)}, " +
                          $"LastChange={LocalDatabase.GetReadableDateFromEpoch(playerData.LastStepsChangeEpochMs)}, " +
                          $"LastDelta={playerData.LastStepsDelta}, " +
                          $"DailySteps={playerData.DailySteps}, " +
                          $"LastDailyResetDate={playerData.LastDailyResetDate}", Logger.LogCategory.General);

            if (playerData.IsCurrentlyTraveling())
            {
                long travelProgress = playerData.GetTravelProgress(playerData.TotalSteps);
                Logger.LogInfo($"DataManager: SaveGame → Travel state: Destination={playerData.TravelDestinationId}, " +
                              $"Progress={travelProgress}/{playerData.TravelRequiredSteps} steps, " +
                              $"StartSteps={playerData.TravelStartSteps}", Logger.LogCategory.General);
            }

            databaseService.LocalDatabase.SavePlayerData(playerData);
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"DataManager: Exception during SaveGame: {ex.Message}", Logger.LogCategory.General);
        }
    }

    public void SaveTravelProgress(PlayerData playerData)
    {
        if (!playerData.IsCurrentlyTraveling())
        {
            Logger.LogWarning("DataManager: SaveTravelProgress called but not currently traveling!", Logger.LogCategory.General);
            return;
        }

        if (!validationService.ValidateTravelState(playerData))
        {
            Logger.LogWarning("DataManager: SaveTravelProgress aborted - travel state is invalid!", Logger.LogCategory.General);
            return;
        }

        try
        {
            long travelProgress = playerData.GetTravelProgress(playerData.TotalSteps);
            Logger.LogInfo($"DataManager: SaveTravelProgress → Destination={playerData.TravelDestinationId}, " +
                          $"Progress={travelProgress}/{playerData.TravelRequiredSteps} steps", Logger.LogCategory.General);

            databaseService.LocalDatabase.SavePlayerData(playerData);
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"DataManager: Exception during SaveTravelProgress: {ex.Message}", Logger.LogCategory.General);
        }
    }
}