// Filepath: Assets/Scripts/Data/DataManager.cs
using System;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public PlayerData PlayerData { get; private set; }
    private LocalDatabase _localDatabase;

    // Constantes pour la détection d'anomalies (DÉSACTIVÉES EN ÉDITEUR)
    private const long MAX_ACCEPTABLE_STEPS_DELTA = 10000;
    private const long MAX_ACCEPTABLE_DAILY_STEPS = 50000;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeManager();
        }
        else
        {
            Logger.LogWarning("DataManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.General);
            Destroy(gameObject);
        }
    }

    private void InitializeManager()
    {
#if UNITY_EDITOR
        // En mode éditeur, utiliser une base de données en mémoire simple
        Logger.LogInfo("DataManager: Running in Editor mode - using simple in-memory database", Logger.LogCategory.General);
        InitializeEditorMode();
#else
        _localDatabase = new LocalDatabase();
        _localDatabase.InitializeDatabase();
        LoadGame();
#endif

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

#if UNITY_EDITOR
    // Mode éditeur simplifié - pas de base de données SQLite
    private void InitializeEditorMode()
    {
        // Charger depuis PlayerPrefs ou créer nouveau
        if (PlayerPrefs.HasKey("EditorPlayerData_TotalSteps"))
        {
            PlayerData = new PlayerData();
            PlayerData.TotalPlayerSteps = long.Parse(PlayerPrefs.GetString("EditorPlayerData_TotalSteps", "0"));
            PlayerData.DailySteps = long.Parse(PlayerPrefs.GetString("EditorPlayerData_DailySteps", "0"));
            PlayerData.LastSyncEpochMs = long.Parse(PlayerPrefs.GetString("EditorPlayerData_LastSync", "0"));
            PlayerData.LastPauseEpochMs = long.Parse(PlayerPrefs.GetString("EditorPlayerData_LastPause", "0"));
            PlayerData.LastDailyResetDate = PlayerPrefs.GetString("EditorPlayerData_LastReset", DateTime.Now.ToString("yyyy-MM-dd"));
            PlayerData.CurrentLocationId = PlayerPrefs.GetString("EditorPlayerData_Location", "Foret_01");

            Logger.LogInfo($"DataManager: [EDITOR] Loaded from PlayerPrefs - TotalSteps: {PlayerData.TotalSteps}, DailySteps: {PlayerData.DailySteps}", Logger.LogCategory.General);
        }
        else
        {
            PlayerData = new PlayerData();
            Logger.LogInfo("DataManager: [EDITOR] Created new PlayerData", Logger.LogCategory.General);
        }
    }
#endif

    private void LoadGame()
    {
        if (_localDatabase == null)
        {
            Logger.LogError("DataManager: Cannot load game, LocalDatabase is not initialized.", Logger.LogCategory.General);
            PlayerData = new PlayerData();
            return;
        }

        PlayerData = _localDatabase.LoadPlayerData();

        Logger.LogInfo($"DataManager: LoadGame → loaded TotalSteps={PlayerData.TotalSteps}, " +
                      $"LastSync={LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastSyncEpochMs)}, " +
                      $"LastPause={LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastPauseEpochMs)}, " +
                      $"LastChange={LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastStepsChangeEpochMs)}, " +
                      $"DailySteps={PlayerData.DailySteps}, LastResetDate={PlayerData.LastDailyResetDate}", Logger.LogCategory.General);

        if (PlayerData.Id == 0)
        {
            PlayerData.Id = 1;
            SaveGame();
            Logger.LogInfo("DataManager: Fixed PlayerData Id and saved.", Logger.LogCategory.General);
        }

        if (string.IsNullOrEmpty(PlayerData.LastDailyResetDate))
        {
            PlayerData.LastDailyResetDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            Logger.LogInfo($"DataManager: Initialized LastDailyResetDate to {PlayerData.LastDailyResetDate}", Logger.LogCategory.General);
            SaveGame();
        }
    }

    public void CheckAndResetDailySteps()
    {
        Logger.LogInfo("DataManager: CheckAndResetDailySteps called but ignored - this functionality is now handled by StepManager", Logger.LogCategory.General);
    }

    public void SaveGame()
    {
        if (PlayerData == null)
        {
            Logger.LogError("DataManager: Cannot save game, PlayerData is null.", Logger.LogCategory.General);
            return;
        }

#if UNITY_EDITOR
        // En mode éditeur, sauver dans PlayerPrefs
        SaveToPlayerPrefs();
        return;
#else
        if (_localDatabase == null)
        {
            Logger.LogError("DataManager: Cannot save game, LocalDatabase is not initialized.", Logger.LogCategory.General);
            return;
        }

        if (PlayerData.Id <= 0)
        {
            PlayerData.Id = 1;
        }

        try
        {
            ValidatePlayerData();

            Logger.LogInfo($"DataManager: SaveGame → saving TotalSteps={PlayerData.TotalSteps}, " +
                          $"LastSync={LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastSyncEpochMs)}, " +
                          $"LastPause={LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastPauseEpochMs)}, " +
                          $"LastChange={LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastStepsChangeEpochMs)}, " +
                          $"LastDelta={PlayerData.LastStepsDelta}, " +
                          $"DailySteps={PlayerData.DailySteps}, " +
                          $"LastDailyResetDate={PlayerData.LastDailyResetDate}", Logger.LogCategory.General);

            if (PlayerData.IsCurrentlyTraveling())
            {
                long travelProgress = PlayerData.GetTravelProgress(PlayerData.TotalSteps);
                Logger.LogInfo($"DataManager: SaveGame → Travel state: Destination={PlayerData.TravelDestinationId}, " +
                              $"Progress={travelProgress}/{PlayerData.TravelRequiredSteps} steps, " +
                              $"StartSteps={PlayerData.TravelStartSteps}", Logger.LogCategory.General);
            }

            _localDatabase.SavePlayerData(PlayerData);
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"DataManager: Exception during SaveGame: {ex.Message}", Logger.LogCategory.General);
        }
#endif
    }

#if UNITY_EDITOR
    private void SaveToPlayerPrefs()
    {
        if (PlayerData == null) return;

        PlayerPrefs.SetString("EditorPlayerData_TotalSteps", PlayerData.TotalSteps.ToString());
        PlayerPrefs.SetString("EditorPlayerData_DailySteps", PlayerData.DailySteps.ToString());
        PlayerPrefs.SetString("EditorPlayerData_LastSync", PlayerData.LastSyncEpochMs.ToString());
        PlayerPrefs.SetString("EditorPlayerData_LastPause", PlayerData.LastPauseEpochMs.ToString());
        PlayerPrefs.SetString("EditorPlayerData_LastReset", PlayerData.LastDailyResetDate);
        PlayerPrefs.SetString("EditorPlayerData_Location", PlayerData.CurrentLocationId ?? "Foret_01");
        PlayerPrefs.Save();

        Logger.LogInfo($"DataManager: [EDITOR] Saved to PlayerPrefs - TotalSteps: {PlayerData.TotalSteps}, DailySteps: {PlayerData.DailySteps}", Logger.LogCategory.General);
    }
#endif

    public void SaveTravelProgress()
    {
        if (!PlayerData.IsCurrentlyTraveling())
        {
            Logger.LogWarning("DataManager: SaveTravelProgress called but not currently traveling!", Logger.LogCategory.General);
            return;
        }

        if (!ValidateTravelState())
        {
            Logger.LogWarning("DataManager: SaveTravelProgress aborted - travel state is invalid!", Logger.LogCategory.General);
            return;
        }

        try
        {
            long travelProgress = PlayerData.GetTravelProgress(PlayerData.TotalSteps);

            Logger.LogInfo($"DataManager: SaveTravelProgress → Destination={PlayerData.TravelDestinationId}, " +
                          $"Progress={travelProgress}/{PlayerData.TravelRequiredSteps} steps, " +
                          $"StartSteps={PlayerData.TravelStartSteps}, TotalSteps={PlayerData.TotalSteps}", Logger.LogCategory.General);

#if UNITY_EDITOR
            SaveToPlayerPrefs();
#else
            if (_localDatabase != null)
            {
                _localDatabase.SavePlayerData(PlayerData);
            }
#endif

            Logger.LogInfo("DataManager: Travel progress saved successfully", Logger.LogCategory.General);
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"DataManager: Exception during SaveTravelProgress: {ex.Message}", Logger.LogCategory.General);
        }
    }

    private bool ValidateTravelState()
    {
        if (PlayerData == null) return false;

        if (string.IsNullOrEmpty(PlayerData.TravelDestinationId))
        {
            Logger.LogError("DataManager: Travel destination is null or empty!", Logger.LogCategory.General);
            return false;
        }

        if (PlayerData.TravelRequiredSteps <= 0)
        {
            Logger.LogError($"DataManager: Invalid travel required steps: {PlayerData.TravelRequiredSteps}", Logger.LogCategory.General);
            return false;
        }

        if (PlayerData.TravelStartSteps < 0)
        {
            Logger.LogError($"DataManager: Invalid travel start steps: {PlayerData.TravelStartSteps}", Logger.LogCategory.General);
            return false;
        }

        // VALIDATION ASSOUPLIE EN ÉDITEUR
#if UNITY_EDITOR
        // En éditeur, permettre plus de souplesse
        if (PlayerData.TotalSteps < PlayerData.TravelStartSteps)
        {
            Logger.LogWarning($"DataManager: [EDITOR] Current total steps ({PlayerData.TotalSteps}) less than travel start steps ({PlayerData.TravelStartSteps}) - Fixing automatically", Logger.LogCategory.General);
            PlayerData.TravelStartSteps = PlayerData.TotalSteps;
        }
#else
        if (PlayerData.TotalSteps < PlayerData.TravelStartSteps)
        {
            Logger.LogError($"DataManager: Current total steps ({PlayerData.TotalSteps}) less than travel start steps ({PlayerData.TravelStartSteps})", Logger.LogCategory.General);
            return false;
        }
#endif

        long travelProgress = PlayerData.GetTravelProgress(PlayerData.TotalSteps);
        if (travelProgress < 0)
        {
            Logger.LogError($"DataManager: Negative travel progress: {travelProgress}", Logger.LogCategory.General);
            return false;
        }

        if (travelProgress > PlayerData.TravelRequiredSteps * 2)
        {
            Logger.LogWarning($"DataManager: Travel progress ({travelProgress}) is much higher than required ({PlayerData.TravelRequiredSteps}). This might indicate a problem.", Logger.LogCategory.General);
        }

        return true;
    }

    // VALIDATIONS DÉSACTIVÉES EN ÉDITEUR
    private void ValidatePlayerData()
    {
#if UNITY_EDITOR
        Logger.LogInfo("DataManager: [EDITOR] Data validation skipped", Logger.LogCategory.General);
        return;
#else
        if (PlayerData.TotalSteps <= 0 || PlayerData.LastStepsChangeEpochMs <= 0)
        {
            return;
        }

        if (PlayerData.LastStepsDelta > MAX_ACCEPTABLE_STEPS_DELTA)
        {
            Logger.LogWarning($"DataManager: Suspicious step delta detected: {PlayerData.LastStepsDelta} > {MAX_ACCEPTABLE_STEPS_DELTA}", Logger.LogCategory.General);

            long newSteps = PlayerData.TotalSteps - PlayerData.LastStepsDelta + MAX_ACCEPTABLE_STEPS_DELTA;
            Logger.LogWarning($"DataManager: Capping steps from {PlayerData.TotalSteps} to {newSteps}", Logger.LogCategory.General);

            PlayerData.TotalPlayerSteps = newSteps;
            PlayerData.LastStepsDelta = MAX_ACCEPTABLE_STEPS_DELTA;
        }

        if (PlayerData.DailySteps > MAX_ACCEPTABLE_DAILY_STEPS)
        {
            Logger.LogWarning($"DataManager: Suspicious daily steps detected: {PlayerData.DailySteps} > {MAX_ACCEPTABLE_DAILY_STEPS}", Logger.LogCategory.General);
            PlayerData.DailySteps = MAX_ACCEPTABLE_DAILY_STEPS;
        }

        long nowEpochMs = new System.DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeMilliseconds();
        if (PlayerData.LastSyncEpochMs > nowEpochMs || PlayerData.LastPauseEpochMs > nowEpochMs ||
            PlayerData.LastStepsChangeEpochMs > nowEpochMs)
        {
            Logger.LogWarning($"DataManager: Invalid timestamp detected (in the future). " +
                             $"LastSync: {LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastSyncEpochMs)}, " +
                             $"LastPause: {LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastPauseEpochMs)}, " +
                             $"LastChange: {LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastStepsChangeEpochMs)}, " +
                             $"Now: {LocalDatabase.GetReadableDateFromEpoch(nowEpochMs)}. Resetting to now.", Logger.LogCategory.General);

            PlayerData.LastSyncEpochMs = nowEpochMs;
            PlayerData.LastPauseEpochMs = nowEpochMs;
            PlayerData.LastStepsChangeEpochMs = nowEpochMs;
        }

        if (PlayerData.IsCurrentlyTraveling())
        {
            ValidateTravelState();
        }
#endif
    }

    public string GetTravelStateDebugInfo()
    {
        if (!PlayerData.IsCurrentlyTraveling())
        {
            return "No active travel";
        }

        long travelProgress = PlayerData.GetTravelProgress(PlayerData.TotalSteps);
        float progressPercent = (float)travelProgress / PlayerData.TravelRequiredSteps * 100f;

        return $"Travel to {PlayerData.TravelDestinationId}: {travelProgress}/{PlayerData.TravelRequiredSteps} steps ({progressPercent:F1}%)";
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

#if !UNITY_EDITOR
        if (_localDatabase != null)
        {
            _localDatabase.CloseDatabase();
        }
#endif
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