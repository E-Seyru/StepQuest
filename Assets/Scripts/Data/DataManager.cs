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

    // NOUVEAU: Optimisation des sauvegardes de voyage - SUPPRIMÉ car maintenant on sauvegarde à chaque pas
    // Plus de limitation de temps pour les sauvegardes de voyage

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
        _localDatabase = new LocalDatabase();
        _localDatabase.InitializeDatabase();

        LoadGame();
        Logger.LogInfo("DataManager initialized and game data loaded.", Logger.LogCategory.General);
        if (PlayerData != null)
        {
            Logger.LogInfo($"DataManager: Loaded PlayerData - TotalSteps: {PlayerData.TotalSteps}, " +
                          $"LastSync: {LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastSyncEpochMs)}, " +
                          $"LastPause: {LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastPauseEpochMs)}, " +
                          $"LastChange: {LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastStepsChangeEpochMs)}, " +
                          $"DailySteps: {PlayerData.DailySteps}", Logger.LogCategory.General);

            // NOUVEAU: Log de l'état de voyage s'il existe
            if (PlayerData.IsCurrentlyTraveling())
            {
                long travelProgress = PlayerData.GetTravelProgress(PlayerData.TotalSteps);
                Logger.LogInfo($"DataManager: Travel state loaded - Destination: {PlayerData.TravelDestinationId}, " +
                              $"Progress: {travelProgress}/{PlayerData.TravelRequiredSteps} steps", Logger.LogCategory.General);
            }
        }

        // Ne plus vérifier le changement de jour ici - désormais géré par StepManager
        // CheckAndResetDailySteps();
    }

    private void LoadGame()
    {
        if (_localDatabase == null)
        {
            Logger.LogError("DataManager: Cannot load game, LocalDatabase is not initialized.", Logger.LogCategory.General);
            PlayerData = new PlayerData(); // Crée un PlayerData avec les valeurs par défaut (LastSyncEpochMs = 0)
            return;
        }

        PlayerData = _localDatabase.LoadPlayerData();

        Logger.LogInfo($"DataManager: LoadGame → loaded TotalSteps={PlayerData.TotalSteps}, " +
                      $"LastSync={LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastSyncEpochMs)}, " +
                      $"LastPause={LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastPauseEpochMs)}, " +
                      $"LastChange={LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastStepsChangeEpochMs)}, " +
                      $"DailySteps={PlayerData.DailySteps}, LastResetDate={PlayerData.LastDailyResetDate}", Logger.LogCategory.General);

        // Vérification supplémentaire - si pas de données, sauvegarder immédiatement
        if (PlayerData.Id == 0)
        {
            PlayerData.Id = 1;
            SaveGame();
            Logger.LogInfo("DataManager: Fixed PlayerData Id and saved.", Logger.LogCategory.General);
        }

        // Si LastDailyResetDate est null ou vide, l'initialiser
        if (string.IsNullOrEmpty(PlayerData.LastDailyResetDate))
        {
            PlayerData.LastDailyResetDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            Logger.LogInfo($"DataManager: Initialized LastDailyResetDate to {PlayerData.LastDailyResetDate}", Logger.LogCategory.General);
            SaveGame();
        }
    }

    // Cette méthode est désactivée pour laisser le StepManager gérer la réinitialisation quotidienne
    public void CheckAndResetDailySteps()
    {
        // Déactivé - gestion déplacée vers StepManager
        // Le StepManager s'occupe de la réinitialisation des pas quotidiens
        Logger.LogInfo("DataManager: CheckAndResetDailySteps called but ignored - this functionality is now handled by StepManager", Logger.LogCategory.General);
    }

    public void SaveGame()
    {
        if (_localDatabase == null)
        {
            Logger.LogError("DataManager: Cannot save game, LocalDatabase is not initialized.", Logger.LogCategory.General);
            return;
        }
        if (PlayerData == null)
        {
            Logger.LogError("DataManager: Cannot save game, PlayerData is null.", Logger.LogCategory.General);
            return;
        }

        // Assurez-vous que l'ID est toujours valide
        if (PlayerData.Id <= 0)
        {
            PlayerData.Id = 1;
        }

        try
        {
            // NE PLUS vérifier si c'est un nouveau jour - géré par StepManager
            // CheckAndResetDailySteps();

            // Vérifier l'intégrité des données avant de sauvegarder
            ValidatePlayerData();

            Logger.LogInfo($"DataManager: SaveGame → saving TotalSteps={PlayerData.TotalSteps}, " +
                          $"LastSync={LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastSyncEpochMs)}, " +
                          $"LastPause={LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastPauseEpochMs)}, " +
                          $"LastChange={LocalDatabase.GetReadableDateFromEpoch(PlayerData.LastStepsChangeEpochMs)}, " +
                          $"LastDelta={PlayerData.LastStepsDelta}, " +
                          $"DailySteps={PlayerData.DailySteps}, " +
                          $"LastDailyResetDate={PlayerData.LastDailyResetDate}", Logger.LogCategory.General);

            // NOUVEAU: Log de l'état de voyage lors de la sauvegarde
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
    }

    // NOUVEAU: Méthode pour sauvegarder spécifiquement le progrès de voyage (maintenant sans limitation de temps)
    public void SaveTravelProgress()
    {
        // Vérifier qu'on est bien en voyage
        if (!PlayerData.IsCurrentlyTraveling())
        {
            Logger.LogWarning("DataManager: SaveTravelProgress called but not currently traveling!", Logger.LogCategory.General);
            return;
        }

        // Vérifier l'intégrité de l'état de voyage avant de sauvegarder
        if (!ValidateTravelState())
        {
            Logger.LogWarning("DataManager: SaveTravelProgress aborted - travel state is invalid!", Logger.LogCategory.General);
            return;
        }

        // Procéder à la sauvegarde immédiate
        try
        {
            long travelProgress = PlayerData.GetTravelProgress(PlayerData.TotalSteps);

            Logger.LogInfo($"DataManager: SaveTravelProgress → Destination={PlayerData.TravelDestinationId}, " +
                          $"Progress={travelProgress}/{PlayerData.TravelRequiredSteps} steps, " +
                          $"StartSteps={PlayerData.TravelStartSteps}, TotalSteps={PlayerData.TotalSteps}", Logger.LogCategory.General);

            _localDatabase.SavePlayerData(PlayerData);

            Logger.LogInfo("DataManager: Travel progress saved successfully", Logger.LogCategory.General);
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"DataManager: Exception during SaveTravelProgress: {ex.Message}", Logger.LogCategory.General);
        }
    }

    // NOUVEAU: Valider l'état de voyage avant sauvegarde
    private bool ValidateTravelState()
    {
        if (PlayerData == null) return false;

        // Vérifier que les données de voyage sont cohérentes
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

        if (PlayerData.TotalSteps < PlayerData.TravelStartSteps)
        {
            Logger.LogError($"DataManager: Current total steps ({PlayerData.TotalSteps}) less than travel start steps ({PlayerData.TravelStartSteps})", Logger.LogCategory.General);
            return false;
        }

        long travelProgress = PlayerData.GetTravelProgress(PlayerData.TotalSteps);
        if (travelProgress < 0)
        {
            Logger.LogError($"DataManager: Negative travel progress: {travelProgress}", Logger.LogCategory.General);
            return false;
        }

        // Avertissement si le progrès dépasse largement le requis (mais ne pas bloquer)
        if (travelProgress > PlayerData.TravelRequiredSteps * 2)
        {
            Logger.LogWarning($"DataManager: Travel progress ({travelProgress}) is much higher than required ({PlayerData.TravelRequiredSteps}). This might indicate a problem.", Logger.LogCategory.General);
        }

        return true;
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
            Logger.LogWarning($"DataManager: Suspicious step delta detected: {PlayerData.LastStepsDelta} > {MAX_ACCEPTABLE_STEPS_DELTA}", Logger.LogCategory.General);

            // Restaurer une valeur plus raisonnable
            long newSteps = PlayerData.TotalSteps - PlayerData.LastStepsDelta + MAX_ACCEPTABLE_STEPS_DELTA;
            Logger.LogWarning($"DataManager: Capping steps from {PlayerData.TotalSteps} to {newSteps}", Logger.LogCategory.General);

            // Réinitialiser le nombre de pas et le delta
            PlayerData.TotalPlayerSteps = newSteps;
            PlayerData.LastStepsDelta = MAX_ACCEPTABLE_STEPS_DELTA;
        }

        // Vérifier si les pas quotidiens sont suspicieusement élevés
        if (PlayerData.DailySteps > MAX_ACCEPTABLE_DAILY_STEPS)
        {
            Logger.LogWarning($"DataManager: Suspicious daily steps detected: {PlayerData.DailySteps} > {MAX_ACCEPTABLE_DAILY_STEPS}", Logger.LogCategory.General);
            PlayerData.DailySteps = MAX_ACCEPTABLE_DAILY_STEPS;
        }

        // Vérifier si les valeurs de timestamp sont cohérentes
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

        // NOUVEAU: Valider aussi l'état de voyage si en cours
        if (PlayerData.IsCurrentlyTraveling())
        {
            ValidateTravelState();
        }
    }

    // NOUVEAU: Méthode utilitaire pour obtenir des informations de debug sur l'état de voyage
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

    // NOUVEAU: Méthode utilitaire pour forcer une sauvegarde immédiate (ignorer les limitations de temps)
    public void ForceSave()
    {
        Logger.LogInfo("DataManager: Force save requested", Logger.LogCategory.General);
        SaveGame();
    }

    // NOUVEAU: Méthode utilitaire pour forcer une sauvegarde de voyage (plus de limitation de temps)
    public void ForceSaveTravelProgress()
    {
        if (!PlayerData.IsCurrentlyTraveling())
        {
            Logger.LogWarning("DataManager: ForceSaveTravelProgress called but not currently traveling!", Logger.LogCategory.General);
            return;
        }

        Logger.LogInfo("DataManager: Force save travel progress requested", Logger.LogCategory.General);
        SaveTravelProgress(); // Maintenant SaveTravelProgress n'a plus de limitation de temps
    }

    void OnApplicationQuit()
    {
        if (PlayerData != null)
        {
            Logger.LogInfo("DataManager: Application quitting, ensuring data is saved.", Logger.LogCategory.General);

            // NOUVEAU: Si en voyage, sauvegarder le progrès une dernière fois
            if (PlayerData.IsCurrentlyTraveling())
            {
                Logger.LogInfo("DataManager: Saving travel progress before quit.", Logger.LogCategory.General);
                ForceSaveTravelProgress();
            }

            SaveGame();
        }

        if (_localDatabase != null)
        {
            _localDatabase.CloseDatabase();
        }
    }

    // NOUVEAU: Méthode appelée quand l'application se met en pause (Android)
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && PlayerData != null)
        {
            Logger.LogInfo("DataManager: Application pausing, saving data.", Logger.LogCategory.General);

            // Si en voyage, sauvegarder le progrès
            if (PlayerData.IsCurrentlyTraveling())
            {
                Logger.LogInfo("DataManager: Saving travel progress before pause.", Logger.LogCategory.General);
                ForceSaveTravelProgress();
            }

            SaveGame();
        }
    }
}