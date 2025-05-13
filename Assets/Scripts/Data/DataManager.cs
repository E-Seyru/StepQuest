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
        // Log pour v�rifier la nouvelle valeur
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
            CurrentPlayerData = new PlayerData(); // Cr�e un PlayerData avec les valeurs par d�faut (LastSyncEpochMs = 0)
            // CurrentPlayerData.Id = 1; // Assur� par le constructeur de PlayerData ou par LocalDatabase
            return;
        }

        CurrentPlayerData = _localDatabase.LoadPlayerData();
        Logger.LogInfo("DataManager: PlayerData loading process complete.");



        // Si c'est la premi�re fois et que LoadPlayerData retourne un nouvel objet,
        // LastSyncEpochMs sera 0 par d�faut, ce qui est correct.
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

        // L'Id devrait �tre g�r� correctement par PlayerData ou LocalDatabase
        // Logger.LogInfo($"DataManager: Saving PlayerData - TotalSteps: {CurrentPlayerData.TotalPlayerSteps}, LastSyncEpochMs: {CurrentPlayerData.LastSyncEpochMs}");
        _localDatabase.SavePlayerData(CurrentPlayerData);
        Logger.LogInfo($"DataManager [Test]: Saved PlayerData - TotalSteps: {CurrentPlayerData.TotalPlayerSteps}, LastSyncEpochMs: {CurrentPlayerData.LastSyncEpochMs}");
        // Logger.LogInfo("DataManager: PlayerData save request sent to LocalDatabase."); // SavePlayerData dans LocalDatabase a d�j� un log de succ�s
    }

    void OnApplicationQuit()
    {
        if (CurrentPlayerData != null) // Sauvegarder une derni�re fois
        {
            Logger.LogInfo("DataManager: Application quitting, ensuring data is saved.");
            SaveGame();
        }

        if (_localDatabase != null)
        {
            _localDatabase.CloseDatabase();
            // Logger.LogInfo("DataManager: Database connection closed on application quit."); // Log d�j� dans LocalDatabase
        }
    }
}