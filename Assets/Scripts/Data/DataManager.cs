// Filepath: Assets/Scripts/Data/DataManager.cs
using System;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public PlayerData PlayerData { get; private set; }
    private LocalDatabase _localDatabase;

    // Constantes pour la détection d'anomalies
    private const long MAX_ACCEPTABLE_STEPS_DELTA = 10000; // Nombre maximum de pas acceptable entre deux sauvegardes
    private const long MAX_ACCEPTABLE_DAILY_STEPS = 50000; // Nombre maximum de pas quotidiens raisonnable

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
            Logger.LogWarning("DataManager: Multiple instances detected! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void InitializeManager()
    {
        _localDatabase = new LocalDatabase();
        _localDatabase.InitializeDatabase();

        LoadGame();
        Logger.LogInfo("DataManager initialized and game data loaded.");
        if (PlayerData != null)
        {
            Logger.LogInfo($"DataManager: Loaded PlayerData - TotalSteps: {PlayerData.TotalSteps}, LastSyncEpochMs: {PlayerData.LastSyncEpochMs}, DailySteps: {PlayerData.DailySteps}");
        }

        // Vérifier si c'est un nouveau jour après le chargement
        CheckAndResetDailySteps();
    }

    private void LoadGame()
    {
        if (_localDatabase == null)
        {
            Logger.LogError("DataManager: Cannot load game, LocalDatabase is not initialized.");
            PlayerData = new PlayerData(); // Crée un PlayerData avec les valeurs par défaut (LastSyncEpochMs = 0)
            return;
        }

        PlayerData = _localDatabase.LoadPlayerData();
        Logger.LogInfo($"DataManager: LoadGame → loaded TotalSteps={PlayerData.TotalSteps}, LastSync={PlayerData.LastSyncEpochMs}, DailySteps={PlayerData.DailySteps}");

        // Vérification supplémentaire - si pas de données, sauvegarder immédiatement
        if (PlayerData.Id == 0)
        {
            PlayerData.Id = 1;
            SaveGame();
            Logger.LogInfo("DataManager: Fixed PlayerData Id and saved.");
        }

        // Si LastDailyResetDate est null ou vide, l'initialiser
        if (string.IsNullOrEmpty(PlayerData.LastDailyResetDate))
        {
            PlayerData.LastDailyResetDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            Logger.LogInfo($"DataManager: Initialized LastDailyResetDate to {PlayerData.LastDailyResetDate}");
            SaveGame();
        }
    }

    // Nouvelle méthode pour vérifier et réinitialiser les pas quotidiens au besoin
    public void CheckAndResetDailySteps()
    {
        if (PlayerData == null) return;

        string todayDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        if (PlayerData.LastDailyResetDate != todayDate)
        {
            Logger.LogInfo($"DataManager: New day detected. Resetting daily steps. Last reset: {PlayerData.LastDailyResetDate}, Today: {todayDate}");

            // Réinitialiser les pas quotidiens et mettre à jour la date
            PlayerData.DailySteps = 0;
            PlayerData.LastDailyResetDate = todayDate;

            // Sauvegarder les changements
            SaveGame();

            Logger.LogInfo("DataManager: Daily steps reset completed.");
        }
    }

    public void SaveGame()
    {
        if (_localDatabase == null)
        {
            Logger.LogError("DataManager: Cannot save game, LocalDatabase is not initialized.");
            return;
        }
        if (PlayerData == null)
        {
            Logger.LogError("DataManager: Cannot save game, PlayerData is null.");
            return;
        }

        // Assurez-vous que l'ID est toujours valide
        if (PlayerData.Id <= 0)
        {
            PlayerData.Id = 1;
        }

        try
        {
            // Vérifier si c'est un nouveau jour
            CheckAndResetDailySteps();

            // Vérifier l'intégrité des données avant de sauvegarder
            ValidatePlayerData();

            Logger.LogInfo($"DataManager: SaveGame → saving TotalSteps={PlayerData.TotalSteps}, LastSync={PlayerData.LastSyncEpochMs}, LastDelta={PlayerData.LastStepsDelta}, DailySteps={PlayerData.DailySteps}");
            _localDatabase.SavePlayerData(PlayerData);
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"DataManager: Exception during SaveGame: {ex.Message}");
        }
    }

    // Méthode pour vérifier l'intégrité des données avant sauvegarde
    private void ValidatePlayerData()
    {
        // Si c'est la première fois ou s'il n'y a pas encore de pas, rien à valider
        if (PlayerData.TotalSteps <= 0 || PlayerData.LastStepsChangeEpochMs <= 0)
        {
            return;
        }

        // Vérifier si le changement de pas est suspicieusement élevé
        if (PlayerData.LastStepsDelta > MAX_ACCEPTABLE_STEPS_DELTA)
        {
            Logger.LogWarning($"DataManager: Suspicious step delta detected: {PlayerData.LastStepsDelta} > {MAX_ACCEPTABLE_STEPS_DELTA}");

            // Restaurer une valeur plus raisonnable
            long newSteps = PlayerData.TotalSteps - PlayerData.LastStepsDelta + MAX_ACCEPTABLE_STEPS_DELTA;
            Logger.LogWarning($"DataManager: Capping steps from {PlayerData.TotalSteps} to {newSteps}");

            // Réinitialiser le nombre de pas et le delta
            PlayerData.TotalPlayerSteps = newSteps;
            PlayerData.LastStepsDelta = MAX_ACCEPTABLE_STEPS_DELTA;
        }

        // Vérifier si les pas quotidiens sont suspicieusement élevés
        if (PlayerData.DailySteps > MAX_ACCEPTABLE_DAILY_STEPS)
        {
            Logger.LogWarning($"DataManager: Suspicious daily steps detected: {PlayerData.DailySteps} > {MAX_ACCEPTABLE_DAILY_STEPS}");
            PlayerData.DailySteps = MAX_ACCEPTABLE_DAILY_STEPS;
        }

        // Vérifier si les valeurs de timestamp sont cohérentes
        long nowEpochMs = new System.DateTimeOffset(System.DateTime.UtcNow).ToUnixTimeMilliseconds();
        if (PlayerData.LastSyncEpochMs > nowEpochMs || PlayerData.LastPauseEpochMs > nowEpochMs ||
            PlayerData.LastStepsChangeEpochMs > nowEpochMs)
        {
            Logger.LogWarning("DataManager: Invalid timestamp detected (in the future). Resetting to now.");
            PlayerData.LastSyncEpochMs = nowEpochMs;
            PlayerData.LastPauseEpochMs = nowEpochMs;
            PlayerData.LastStepsChangeEpochMs = nowEpochMs;
        }
    }

    void OnApplicationQuit()
    {
        if (PlayerData != null)
        {
            Logger.LogInfo("DataManager: Application quitting, ensuring data is saved.");
            SaveGame();
        }

        if (_localDatabase != null)
        {
            _localDatabase.CloseDatabase();
        }
    }
}