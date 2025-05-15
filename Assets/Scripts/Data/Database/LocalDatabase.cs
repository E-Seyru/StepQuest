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
    private const int DATABASE_VERSION = 3; // Incrйmentй pour gйrer les migrations (2 -> 3 pour DailySteps)

    public void InitializeDatabase()
    {
        // Dйfinir le chemin de la base de donnйes
        _databasePath = Path.Combine(Application.persistentDataPath, DatabaseFilename);
        Logger.LogInfo($"LocalDatabase: Database path is: {_databasePath}");

        try
        {
            // Force la suppression de la base de donnйes existante pour repartir proprement
            // А dйsactiver en production aprиs vйrification du bon fonctionnement
            bool forceReset = false;

            if (forceReset && File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
                Logger.LogInfo("LocalDatabase: Reset - Existing database file deleted");
            }

            // Crйer ou ouvrir la connexion SQLite
            _connection = new SQLiteConnection(_databasePath);
            Logger.LogInfo("LocalDatabase: Connection established");

            // Vйrifier si la base de donnйes nйcessite une migration
            ManageDatabaseMigration();

            // Crйer la table PlayerData si elle n'existe pas dйjа
            _connection.CreateTable<PlayerData>();
            Logger.LogInfo("LocalDatabase: PlayerData table created/verified");

            // Vйrifier la structure de la table
            DebugLogTableStructure();
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Initialization error: {ex.Message}");
        }
    }

    // Gestion des migrations de base de donnйes
    private void ManageDatabaseMigration()
    {
        try
        {
            // Crйer une table de version si elle n'existe pas
            _connection.Execute("CREATE TABLE IF NOT EXISTS DatabaseVersion (Version INTEGER)");

            // Rйcupйrer la version actuelle
            int currentVersion = 0;
            var result = _connection.Query<DatabaseVersionInfo>("SELECT Version FROM DatabaseVersion LIMIT 1");
            if (result.Count > 0)
            {
                currentVersion = result[0].Version;
            }
            else
            {
                // Aucune version trouvйe, insйrer la version initiale
                _connection.Execute("INSERT INTO DatabaseVersion (Version) VALUES (1)");
                currentVersion = 1;
            }

            Logger.LogInfo($"LocalDatabase: Current database version: {currentVersion}, Target version: {DATABASE_VERSION}");

            // Appliquer les migrations nйcessaires
            if (currentVersion < DATABASE_VERSION)
            {
                // Migration de la version 1 а 2
                if (currentVersion == 1 && DATABASE_VERSION >= 2)
                {
                    Logger.LogInfo("LocalDatabase: Migrating from version 1 to 2...");

                    try
                    {
                        // Ajouter les colonnes de suivi des anomalies
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN LastStepsDelta INTEGER DEFAULT 0");
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN LastStepsChangeEpochMs INTEGER DEFAULT 0");

                        // Mettre а jour la version
                        _connection.Execute("UPDATE DatabaseVersion SET Version = 2");
                        Logger.LogInfo("LocalDatabase: Migration to version 2 completed");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"LocalDatabase: Migration error: {ex.Message}");
                    }
                }

                // NOUVELLE MIGRATION: Migration de la version 2 à 3 pour ajouter DailySteps et LastDailyResetDate
                if (currentVersion == 2 && DATABASE_VERSION >= 3)
                {
                    Logger.LogInfo("LocalDatabase: Migrating from version 2 to 3...");

                    try
                    {
                        // Ajouter les colonnes pour le comptage quotidien
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN DailySteps INTEGER DEFAULT 0");
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN LastDailyResetDate TEXT DEFAULT ''");

                        // Initialiser LastDailyResetDate à la date du jour
                        string todayDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                        _connection.Execute($"UPDATE PlayerData SET LastDailyResetDate = '{todayDate}'");

                        // Mettre à jour la version
                        _connection.Execute("UPDATE DatabaseVersion SET Version = 3");
                        Logger.LogInfo("LocalDatabase: Migration to version 3 completed");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"LocalDatabase: Migration error: {ex.Message}");
                    }
                }

                // Ajouter d'autres migrations ici au besoin:
                // if (currentVersion == 3 && DATABASE_VERSION >= 4) {...}
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
                Logger.LogInfo($"LocalDatabase: Data loaded - TotalSteps: {data.TotalPlayerSteps}, " +
                               $"LastSync: {data.LastSyncEpochMs} ({GetReadableDateFromEpoch(data.LastSyncEpochMs)}), " +
                               $"LastPause: {data.LastPauseEpochMs} ({GetReadableDateFromEpoch(data.LastPauseEpochMs)}), " +
                               $"LastChange: {data.LastStepsChangeEpochMs} ({GetReadableDateFromEpoch(data.LastStepsChangeEpochMs)}), " +
                               $"DailySteps: {data.DailySteps}, LastReset: {data.LastDailyResetDate}");
                return data;
            }
            else
            {
                // Crйer un nouveau PlayerData si aucun n'existe
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

    // Méthode utilitaire pour convertir un epoch timestamp en date lisible
    public static string GetReadableDateFromEpoch(long epochMs)
    {
        if (epochMs <= 0) return "Jamais";
        try
        {
            DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(epochMs);
            return date.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
        }
        catch
        {
            return "Epoch invalide";
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
            // Vйrifier que l'identifiant est correct
            if (data.Id <= 0)
                data.Id = 1;

            // Vйrifier que LastSyncEpochMs est correctement dйfini
            if (data.LastSyncEpochMs <= 0 && data.TotalPlayerSteps > 0)
            {
                Logger.LogWarning("LocalDatabase: LastSyncEpochMs needs initialization");
                data.LastSyncEpochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            // S'assurer que LastDailyResetDate n'est pas vide
            if (string.IsNullOrEmpty(data.LastDailyResetDate))
            {
                data.LastDailyResetDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
                Logger.LogWarning($"LocalDatabase: LastDailyResetDate was empty, initialized to {data.LastDailyResetDate}");
            }

            // Enregistrer les donnйes
            int result = _connection.InsertOrReplace(data);
            Logger.LogInfo($"LocalDatabase: Data saved - TotalSteps: {data.TotalPlayerSteps}, " +
                           $"LastSync: {data.LastSyncEpochMs} ({GetReadableDateFromEpoch(data.LastSyncEpochMs)}), " +
                           $"LastPause: {data.LastPauseEpochMs} ({GetReadableDateFromEpoch(data.LastPauseEpochMs)}), " +
                           $"LastDelta: {data.LastStepsDelta}, " +
                           $"LastChange: {data.LastStepsChangeEpochMs} ({GetReadableDateFromEpoch(data.LastStepsChangeEpochMs)}), " +
                           $"DailySteps: {data.DailySteps}, " +
                           $"LastReset: {data.LastDailyResetDate}, " +
                           $"Result: {result}");

            // Vйrifier que la sauvegarde a rйussi
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

    // Mйthode privйe pour vйrifier la structure de la table
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

    // Mйthode privйe pour vйrifier que la sauvegarde a rйussi
    private void VerifySaveSuccess(PlayerData originalData)
    {
        try
        {
            var savedData = _connection.Table<PlayerData>().FirstOrDefault(p => p.Id == 1);
            if (savedData != null)
            {
                Logger.LogInfo($"LocalDatabase: Verification - Saved data: TotalSteps: {savedData.TotalPlayerSteps}, " +
                              $"LastSync: {savedData.LastSyncEpochMs} ({GetReadableDateFromEpoch(savedData.LastSyncEpochMs)}), " +
                              $"LastPause: {savedData.LastPauseEpochMs} ({GetReadableDateFromEpoch(savedData.LastPauseEpochMs)}), " +
                              $"DailySteps: {savedData.DailySteps}");

                // Vйrifier que les valeurs correspondent
                if (savedData.TotalPlayerSteps != originalData.TotalPlayerSteps ||
                    savedData.LastSyncEpochMs != originalData.LastSyncEpochMs ||
                    savedData.DailySteps != originalData.DailySteps ||
                    savedData.LastDailyResetDate != originalData.LastDailyResetDate)
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

// Classe pour stocker les informations de version de la base de donnйes
class DatabaseVersionInfo
{
    public int Version { get; set; }
}