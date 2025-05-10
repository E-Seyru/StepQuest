// Filepath: Assets/Scripts/Data/DataManager.cs
using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public PlayerData CurrentPlayerData { get; private set; }
    private LocalDatabase _localDatabase;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Pour que le DataManager persiste entre les sc�nes
            InitializeManager();
        }
        else
        {
            Destroy(gameObject); // S'il y a d�j� un DataManager, on d�truit ce doublon
        }
    }

    private void InitializeManager()
    {

        UnityEngine.Debug.Log("DataManager: Calling SQLitePCL.Batteries_V2.Init()"); // Log pour v�rifier
        SQLitePCL.Batteries_V2.Init();
        UnityEngine.Debug.Log("DataManager: SQLitePCL.Batteries_V2.Init() called.");

        _localDatabase = new LocalDatabase();
        _localDatabase.InitializeDatabase(); // Pr�pare la connexion et la table

        LoadGame(); // Charge les donn�es du joueur
        Logger.LogInfo("DataManager initialized and game data loaded.");
    }

    private void LoadGame()
    {
        if (_localDatabase == null)
        {
            Logger.LogError("DataManager: Cannot load game, LocalDatabase is not initialized.");
            // En cas d'erreur grave, on pourrait initialiser avec des donn�es par d�faut
            CurrentPlayerData = new PlayerData(); // Assure que CurrentPlayerData n'est jamais null
            CurrentPlayerData.Id = 1; // Important pour la premi�re sauvegarde si la DB a �chou�
            return;
        }

        CurrentPlayerData = _localDatabase.LoadPlayerData();
        Logger.LogInfo("DataManager: PlayerData loaded.");

        // V�rification suppl�mentaire : si LoadPlayerData retourne un PlayerData avec Id 0
        // (ce qui pourrait arriver si on modifie LoadPlayerData pour retourner new PlayerData() sans fixer l'Id),
        // il faut s'assurer qu'il est pr�t pour la sauvegarde.
        if (CurrentPlayerData.Id == 0)
        {
            CurrentPlayerData.Id = 1;
            Logger.LogWarning("DataManager: Loaded PlayerData had Id 0. Set to 1 for consistency.");
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

        // Assurons-nous que l'Id est correct avant de sauvegarder, surtout si c'est une nouvelle partie.
        if (CurrentPlayerData.Id == 0)
        {
            CurrentPlayerData.Id = 1;
            Logger.LogWarning("DataManager: CurrentPlayerData had Id 0 before saving. Set to 1.");
        }

        _localDatabase.SavePlayerData(CurrentPlayerData);
        Logger.LogInfo("DataManager: PlayerData saved.");
    }
    void OnApplicationQuit() // Automatiquement appel� lors de la fermeture de l'application
    {


        if (CurrentPlayerData != null)
        {
            SaveGame();
        }

        if (_localDatabase != null)
        {
            _localDatabase.CloseDatabase();
            Logger.LogInfo("DataManager: Database connection closed on application quit.");
        }
    }

}