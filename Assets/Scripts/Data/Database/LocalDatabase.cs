// Filepath: Assets/Scripts/Data/Database/LocalDatabase.cs
using SQLite;
using System;
using System.IO;
using UnityEngine;

public class LocalDatabase
{
    private SQLiteConnection _connection;
    private string _databasePath;

    private const string DatabaseFilename = "StepQuestRPG_Data.db";
    private const int DATABASE_VERSION = 2; // Incrémenté pour gérer les migrations

    public void InitializeDatabase()
    {
        // Définir le chemin de la base de données
        _databasePath = Path.Combine(Application.persistentDataPath, DatabaseFilename);
        Logger.LogInfo($"LocalDatabase: Database path is: {_databasePath}");

        try
        {
            // Force la suppression de la base de données existante pour repartir proprement
            // À désactiver en production après vérification du bon fonctionnement
            bool forceReset = false;

            if (forceReset && File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
                Logger.LogInfo("LocalDatabase: Reset - Existing database file deleted");
            }

            // Créer ou ouvrir la connexion SQLite
            _connection = new SQLiteConnection(_databasePath);
            Logger.LogInfo("LocalDatabase: Connection established");

            // Vérifier si la base de données nécessite une migration
            ManageDatabaseMigration();

            // Créer la table PlayerData si elle n'existe pas déjà
            _connection.CreateTable<PlayerData>();
            Logger.LogInfo("LocalDatabase: PlayerData table created/verified");

            // Vérifier la structure de la table
            DebugLogTableStructure();
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Initialization error: {ex.Message}");
        }
    }

    // Gestion des migrations de base de données
    private void ManageDatabaseMigration()
    {
        try
        {
            // Créer une table de version si elle n'existe pas
            _connection.Execute("CREATE TABLE IF NOT EXISTS DatabaseVersion (Version INTEGER)");

            // Récupérer la version actuelle
            int currentVersion = 0;
            var result = _connection.Query<DatabaseVersionInfo>("SELECT Version FROM DatabaseVersion LIMIT 1");
            if (result.Count > 0)
            {
                currentVersion = result[0].Version;
            }
            else
            {
                // Aucune version trouvée, insérer la version initiale
                _connection.Execute("INSERT INTO DatabaseVersion (Version) VALUES (1)");
                currentVersion = 1;
            }

            Logger.LogInfo($"LocalDatabase: Current database version: {currentVersion}, Target version: {DATABASE_VERSION}");

            // Appliquer les migrations nécessaires
            if (currentVersion < DATABASE_VERSION)
            {
                // Migration de la version 1 à 2
                if (currentVersion == 1 && DATABASE_VERSION >= 2)
                {
                    Logger.LogInfo("LocalDatabase: Migrating from version 1 to 2...");

                    try
                    {
                        // Ajouter les colonnes de suivi des anomalies
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN LastStepsDelta INTEGER DEFAULT 0");
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN LastStepsChangeEpochMs INTEGER DEFAULT 0");

                        // Mettre à jour la version
                        _connection.Execute("UPDATE DatabaseVersion SET Version = 2");
                        Logger.LogInfo("LocalDatabase: Migration to version 2 completed");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"LocalDatabase: Migration error: {ex.Message}");
                    }
                }

                // Ajouter d'autres migrations ici au besoin:
                // if (currentVersion == 2 && DATABASE_VERSION >= 3) {...}
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Migration management error: {ex.Message}");
        }
    }

    public PlayerData LoadPlayerData()
    {
        if (_connection == null)
        {
            Logger.LogError("LocalDatabase: Connection not initialized");
            return new PlayerData();
        }

        try
        {
            // Tenter de charger l'enregistrement avec Id=1
            PlayerData data = _connection.Table<PlayerData>().FirstOrDefault(p => p.Id == 1);

            if (data != null)
            {
                Logger.LogInfo($"LocalDatabase: Data loaded - TotalSteps: {data.TotalPlayerSteps}, LastSync: {data.LastSyncEpochMs}");
                return data;
            }
            else
            {
                // Créer un nouveau PlayerData si aucun n'existe
                PlayerData newData = new PlayerData();
                _connection.Insert(newData);
                Logger.LogInfo("LocalDatabase: New PlayerData created and saved");
                return newData;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Error loading data: {ex.Message}");
            return new PlayerData();
        }
    }

    public void SavePlayerData(PlayerData data)
    {
        if (_connection == null)
        {
            Logger.LogError("LocalDatabase: Connection not initialized");
            return;
        }

        if (data == null)
        {
            Logger.LogError("LocalDatabase: Data is null");
            return;
        }

        try
        {
            // Vérifier que l'identifiant est correct
            if (data.Id <= 0)
                data.Id = 1;

            // Vérifier que LastSyncEpochMs est correctement défini
            if (data.LastSyncEpochMs <= 0 && data.TotalPlayerSteps > 0)
            {
                Logger.LogWarning("LocalDatabase: LastSyncEpochMs needs initialization");
                data.LastSyncEpochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            // Enregistrer les données
            int result = _connection.InsertOrReplace(data);
            Logger.LogInfo($"LocalDatabase: Data saved - TotalSteps: {data.TotalPlayerSteps}, LastSync: {data.LastSyncEpochMs}, LastDelta: {data.LastStepsDelta}, Result: {result}");

            // Vérifier que la sauvegarde a réussi
            VerifySaveSuccess(data);
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Save error: {ex.Message}");
        }
    }

    public void CloseDatabase()
    {
        if (_connection != null)
        {
            _connection.Close();
            _connection = null;
            Logger.LogInfo("LocalDatabase: Connection closed");
        }
    }

    // Méthode privée pour vérifier la structure de la table
    private void DebugLogTableStructure()
    {
        try
        {
            var tableInfo = _connection.GetTableInfo("PlayerData");
            string columns = "";
            foreach (var col in tableInfo)
            {
                columns += col.Name + ", ";
            }
            Logger.LogInfo($"LocalDatabase: Table columns: {columns}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Error checking table structure: {ex.Message}");
        }
    }

    // Méthode privée pour vérifier que la sauvegarde a réussi
    private void VerifySaveSuccess(PlayerData originalData)
    {
        try
        {
            var savedData = _connection.Table<PlayerData>().FirstOrDefault(p => p.Id == 1);
            if (savedData != null)
            {
                Logger.LogInfo($"LocalDatabase: Verification - Saved data: TotalSteps: {savedData.TotalPlayerSteps}, LastSync: {savedData.LastSyncEpochMs}");

                // Vérifier que les valeurs correspondent
                if (savedData.TotalPlayerSteps != originalData.TotalPlayerSteps ||
                    savedData.LastSyncEpochMs != originalData.LastSyncEpochMs)
                {
                    Logger.LogError("LocalDatabase: Verification FAILED - Data mismatch after save");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Verification error: {ex.Message}");
        }
    }
}

// Classe pour stocker les informations de version de la base de données
class DatabaseVersionInfo
{
    public int Version { get; set; }
}