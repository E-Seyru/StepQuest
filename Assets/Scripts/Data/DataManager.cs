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
            DontDestroyOnLoad(gameObject); // Pour que le DataManager persiste entre les scènes
            InitializeManager();
        }
        else
        {
            Destroy(gameObject); // S'il y a déjà un DataManager, on détruit ce doublon
        }
    }

    private void InitializeManager()
    {

        UnityEngine.Debug.Log("DataManager: Calling SQLitePCL.Batteries_V2.Init()"); // Log pour vérifier
        SQLitePCL.Batteries_V2.Init();
        UnityEngine.Debug.Log("DataManager: SQLitePCL.Batteries_V2.Init() called.");

        _localDatabase = new LocalDatabase();
        _localDatabase.InitializeDatabase(); // Prépare la connexion et la table

        LoadGame(); // Charge les données du joueur
        Logger.LogInfo("DataManager initialized and game data loaded.");
    }

    private void LoadGame()
    {
        if (_localDatabase == null)
        {
            Logger.LogError("DataManager: Cannot load game, LocalDatabase is not initialized.");
            // En cas d'erreur grave, on pourrait initialiser avec des données par défaut
            CurrentPlayerData = new PlayerData(); // Assure que CurrentPlayerData n'est jamais null
            CurrentPlayerData.Id = 1; // Important pour la première sauvegarde si la DB a échoué
            return;
        }

        CurrentPlayerData = _localDatabase.LoadPlayerData();
        Logger.LogInfo("DataManager: PlayerData loaded.");

        // Vérification supplémentaire : si LoadPlayerData retourne un PlayerData avec Id 0
        // (ce qui pourrait arriver si on modifie LoadPlayerData pour retourner new PlayerData() sans fixer l'Id),
        // il faut s'assurer qu'il est prêt pour la sauvegarde.
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
    void OnApplicationQuit() // Automatiquement appelé lors de la fermeture de l'application
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