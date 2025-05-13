// Filepath: Assets/Scripts/Data/DataManager.cs
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public PlayerData CurrentPlayerData { get; private set; }
    private LocalDatabase _localDatabase; // En supposant que LocalDatabase.cs existe et fonctionne

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
            Destroy(gameObject);
        }
    }

    private void InitializeManager()
    {
        _localDatabase = new LocalDatabase();
        _localDatabase.InitializeDatabase();

        LoadGame();
        Logger.LogInfo("DataManager initialized and game data loaded.");
        // Log pour vérifier la nouvelle valeur
        if (CurrentPlayerData != null)
        {
            Logger.LogInfo($"DataManager: Loaded PlayerData - TotalSteps: {CurrentPlayerData.TotalPlayerSteps}, LastSyncEpochMs: {CurrentPlayerData.LastSyncEpochMs}");

            // --- FIN DU CODE DE TEST TEMPORAIRE ---
        }
    }

    private void LoadGame()
    {
        if (_localDatabase == null)
        {
            Logger.LogError("DataManager: Cannot load game, LocalDatabase is not initialized.");
            CurrentPlayerData = new PlayerData(); // Crée un PlayerData avec les valeurs par défaut (LastSyncEpochMs = 0)
            // CurrentPlayerData.Id = 1; // Assuré par le constructeur de PlayerData ou par LocalDatabase
            return;
        }

        CurrentPlayerData = _localDatabase.LoadPlayerData();
        Logger.LogInfo("DataManager: PlayerData loading process complete.");



        // Si c'est la première fois et que LoadPlayerData retourne un nouvel objet,
        // LastSyncEpochMs sera 0 par défaut, ce qui est correct.
        if (CurrentPlayerData.Id == 0 && CurrentPlayerData.TotalPlayerSteps == 0 && CurrentPlayerData.LastSyncEpochMs == 0)
        {
            Logger.LogInfo("DataManager: Looks like a fresh PlayerData load (or first time).");
        }
    }

    public void SaveGame()
    {
        if (_localDatabase == null)
        {
            Logger.LogError("DataManager: Cannot save game, LocalDatabase is not initialized.");
            return;
        }
        if (CurrentPlayerData == null)
        {
            Logger.LogError("DataManager: Cannot save game, CurrentPlayerData is null.");
            return;
        }

        // L'Id devrait être géré correctement par PlayerData ou LocalDatabase
        // Logger.LogInfo($"DataManager: Saving PlayerData - TotalSteps: {CurrentPlayerData.TotalPlayerSteps}, LastSyncEpochMs: {CurrentPlayerData.LastSyncEpochMs}");
        _localDatabase.SavePlayerData(CurrentPlayerData);
        Logger.LogInfo($"DataManager [Test]: Saved PlayerData - TotalSteps: {CurrentPlayerData.TotalPlayerSteps}, LastSyncEpochMs: {CurrentPlayerData.LastSyncEpochMs}");
        // Logger.LogInfo("DataManager: PlayerData save request sent to LocalDatabase."); // SavePlayerData dans LocalDatabase a déjà un log de succès
    }

    void OnApplicationQuit()
    {
        if (CurrentPlayerData != null) // Sauvegarder une dernière fois
        {
            Logger.LogInfo("DataManager: Application quitting, ensuring data is saved.");
            SaveGame();
        }

        if (_localDatabase != null)
        {
            _localDatabase.CloseDatabase();
            // Logger.LogInfo("DataManager: Database connection closed on application quit."); // Log déjà dans LocalDatabase
        }
    }
}