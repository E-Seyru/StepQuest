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
    private const int DATABASE_VERSION = 2; // Incr�ment� pour g�rer les migrations

    public void InitializeDatabase()
    {
        // D�finir le chemin de la base de donn�es
        _databasePath = Path.Combine(Application.persistentDataPath, DatabaseFilename);
        Logger.LogInfo($"LocalDatabase: Database path is: {_databasePath}");

        try
        {
            // Force la suppression de la base de donn�es existante pour repartir proprement
            // � d�sactiver en production apr�s v�rification du bon fonctionnement
            bool forceReset = false;

            if (forceReset && File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
                Logger.LogInfo("LocalDatabase: Reset - Existing database file deleted");
            }

            // Cr�er ou ouvrir la connexion SQLite
            _connection = new SQLiteConnection(_databasePath);
            Logger.LogInfo("LocalDatabase: Connection established");

            // V�rifier si la base de donn�es n�cessite une migration
            ManageDatabaseMigration();

            // Cr�er la table PlayerData si elle n'existe pas d�j�
            _connection.CreateTable<PlayerData>();
            Logger.LogInfo("LocalDatabase: PlayerData table created/verified");

            // V�rifier la structure de la table
            DebugLogTableStructure();
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Initialization error: {ex.Message}");
        }
    }

    // Gestion des migrations de base de donn�es
    private void ManageDatabaseMigration()
    {
        try
        {
            // Cr�er une table de version si elle n'existe pas
            _connection.Execute("CREATE TABLE IF NOT EXISTS DatabaseVersion (Version INTEGER)");

            // R�cup�rer la version actuelle
            int currentVersion = 0;
            var result = _connection.Query<DatabaseVersionInfo>("SELECT Version FROM DatabaseVersion LIMIT 1");
            if (result.Count > 0)
            {
                currentVersion = result[0].Version;
            }
            else
            {
                // Aucune version trouv�e, ins�rer la version initiale
                _connection.Execute("INSERT INTO DatabaseVersion (Version) VALUES (1)");
                currentVersion = 1;
            }

            Logger.LogInfo($"LocalDatabase: Current database version: {currentVersion}, Target version: {DATABASE_VERSION}");

            // Appliquer les migrations n�cessaires
            if (currentVersion < DATABASE_VERSION)
            {
                // Migration de la version 1 � 2
                if (currentVersion == 1 && DATABASE_VERSION >= 2)
                {
                    Logger.LogInfo("LocalDatabase: Migrating from version 1 to 2...");

                    try
                    {
                        // Ajouter les colonnes de suivi des anomalies
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN LastStepsDelta INTEGER DEFAULT 0");
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN LastStepsChangeEpochMs INTEGER DEFAULT 0");

                        // Mettre � jour la version
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
                // Cr�er un nouveau PlayerData si aucun n'existe
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
            // V�rifier que l'identifiant est correct
            if (data.Id <= 0)
                data.Id = 1;

            // V�rifier que LastSyncEpochMs est correctement d�fini
            if (data.LastSyncEpochMs <= 0 && data.TotalPlayerSteps > 0)
            {
                Logger.LogWarning("LocalDatabase: LastSyncEpochMs needs initialization");
                data.LastSyncEpochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            // Enregistrer les donn�es
            int result = _connection.InsertOrReplace(data);
            Logger.LogInfo($"LocalDatabase: Data saved - TotalSteps: {data.TotalPlayerSteps}, LastSync: {data.LastSyncEpochMs}, LastDelta: {data.LastStepsDelta}, Result: {result}");

            // V�rifier que la sauvegarde a r�ussi
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

    // M�thode priv�e pour v�rifier la structure de la table
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

    // M�thode priv�e pour v�rifier que la sauvegarde a r�ussi
    private void VerifySaveSuccess(PlayerData originalData)
    {
        try
        {
            var savedData = _connection.Table<PlayerData>().FirstOrDefault(p => p.Id == 1);
            if (savedData != null)
            {
                Logger.LogInfo($"LocalDatabase: Verification - Saved data: TotalSteps: {savedData.TotalPlayerSteps}, LastSync: {savedData.LastSyncEpochMs}");

                // V�rifier que les valeurs correspondent
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

// Classe pour stocker les informations de version de la base de donn�es
class DatabaseVersionInfo
{
    public int Version { get; set; }
}