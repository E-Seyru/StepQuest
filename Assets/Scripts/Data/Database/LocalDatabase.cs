// Filepath: Assets/Scripts/Data/Database/LocalDatabase.cs
using SQLite; // Pour interagir avec la base de données SQLite
using System.IO; // Pour gérer les chemins de fichiers
using UnityEngine; // Pour les fonctionnalités Unity

public class LocalDatabase
{
    private SQLiteConnection _connection;
    private string _databasePath;

    private const string DatabaseFilename = "StepQuestRPG_Data.db";

    public void InitializeDatabase()
    {
        _databasePath = Path.Combine(Application.persistentDataPath, DatabaseFilename);
        Logger.LogInfo($"LocalDatabase: Database path is: {_databasePath}");

        _connection = new SQLiteConnection(_databasePath);
        Logger.LogInfo("LocalDatabase: Database connection established.");

        _connection.CreateTable<PlayerData>();
        Logger.LogInfo("LocalDatabase: PlayerData table ensured");
    }

    public void SavePlayerData(PlayerData data)
    {
        if (_connection == null)
        {
            Logger.LogError("LocalDatabase: Database connection is not initialized.");
            return;
        }
        if (data == null)
        {
            Logger.LogError("LocalDatabase: PlayerData is null.");
            return;
        }

        if (data.Id == 0)
        {
            data.Id = 1;
        }

        int rowsAffected = _connection.InsertOrReplace(data);
        if (rowsAffected > 0)
        {
            Logger.LogInfo($"LocalDatabase: PlayerData saved successfully (Id: {data.Id}). TotalSteps: {data.TotalPlayerSteps}, LastSyncEpoch: {data.LastSyncEpochMs}");
        }
        else
        {
            Logger.LogWarning("LocalDatabase: PlayerData save operation did not affect any rows. This might be an issue if an update was expected.");
        }
    }

    public PlayerData LoadPlayerData()

    {
        if (_connection == null)
        {
            Logger.LogError("LocalDatabase: Database connection is not initialized.");
            return new PlayerData();


        }

        PlayerData loadedData = _connection.Table<PlayerData>().FirstOrDefault(p => p.Id == 1);

        if (loadedData != null)
        {
            Logger.LogInfo($"LocalDatabase: PlayerData loaded successfully. TotalSteps: {loadedData.TotalPlayerSteps}, LastSyncEpoch: {loadedData.LastSyncEpochMs}");
            return loadedData;

        }
        else
        {
            // Aucune donnée trouvée (par exemple, premier lancement du jeu)
            Logger.LogInfo("LocalDatabase: No PlayerData found in database. Returning new PlayerData instance.");
            // On retourne un nouvel objet PlayerData avec les valeurs par défaut (0 pas, etc.)
            // On pourrait aussi sauvegarder ce nouvel objet ici si on veut qu'il ait un ID 1 tout de suite.
            PlayerData defaultData = new PlayerData();
            defaultData.Id = 1; // On s'assure qu'il a l'ID 1 pour la prochaine sauvegarde.
                                // Optionnel: sauvegarder immédiatement ces données par défaut.
                                // SavePlayerData(defaultData); // Décommentez si vous voulez que le fichier DB soit créé/mis à jour au premier chargement.
            return defaultData;
        }
    }
    public void CloseDatabase()
    {
        if (_connection != null)
        {
            _connection.Close();
            _connection = null; // Libérer la référence
            Logger.LogInfo("LocalDatabase: Database connection closed.");
        }
    }

}
