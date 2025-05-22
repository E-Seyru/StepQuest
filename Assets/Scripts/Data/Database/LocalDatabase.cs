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
    private const int DATABASE_VERSION = 4; // Incrementé pour gérer les migrations (3 -> 4 pour LastApiCatchUpEpochMs)

    public void InitializeDatabase()
    {
        // Définir le chemin de la base de données
        _databasePath = Path.Combine(Application.persistentDataPath, DatabaseFilename);
        Logger.LogInfo($"LocalDatabase: Database path is: {_databasePath}");

        try
        {
            // Force la suppression de la base de données existante uniquement en mode éditeur
#if UNITY_EDITOR
            bool forceReset = false; // À activer seulement pour les tests en éditeur
            if (forceReset && File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
                Logger.LogInfo("LocalDatabase: Reset - Existing database file deleted");
            }
#endif

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

    // Méthode utilitaire pour convertir un epoch timestamp en date lisible
    public static string GetReadableDateFromEpoch(long epochMs)
    {
        if (epochMs <= 0) return "Jamais";
        try
        {
            // MODIFIÉ: Utiliser DateTime.Now et le fuseau horaire local (Faille B)
            DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(epochMs);
            return date.ToLocalTime().ToString("HH:mm:ss dd/MM/yyyy");
        }
        catch
        {
            return "Epoch invalide";
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

                // Migration de la version 2 à 3 pour ajouter DailySteps et LastDailyResetDate
                if (currentVersion == 2 && DATABASE_VERSION >= 3)
                {
                    Logger.LogInfo("LocalDatabase: Migrating from version 2 to 3...");

                    try
                    {
                        // Ajouter les colonnes pour le comptage quotidien
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN DailySteps INTEGER DEFAULT 0");
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN LastDailyResetDate TEXT DEFAULT ''");

                        // Initialiser LastDailyResetDate à la date du jour
                        string todayDate = DateTime.Now.ToString("yyyy-MM-dd"); // MODIFIÉ: Utiliser DateTime.Now (Faille B)
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

                // NOUVELLE MIGRATION: Migration de la version 3 à 4 pour ajouter LastApiCatchUpEpochMs (Faille A)
                if (currentVersion == 3 && DATABASE_VERSION >= 4)
                {
                    Logger.LogInfo("LocalDatabase: Migrating from version 3 to 4...");

                    try
                    {
                        // Ajouter la colonne pour le timestamp dédié à l'API catch-up
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN LastApiCatchUpEpochMs INTEGER DEFAULT 0");

                        // Initialiser LastApiCatchUpEpochMs à la même valeur que LastSyncEpochMs pour les données existantes
                        _connection.Execute("UPDATE PlayerData SET LastApiCatchUpEpochMs = LastSyncEpochMs");

                        // Mettre à jour la version
                        _connection.Execute("UPDATE DatabaseVersion SET Version = 4");
                        Logger.LogInfo("LocalDatabase: Migration to version 4 completed");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"LocalDatabase: Migration error: {ex.Message}");
                    }
                }

                if (currentVersion == 4 && DATABASE_VERSION >= 5)
                {
                    Logger.LogInfo("LocalDatabase: Migrating from version 4 to 5...");
                    try
                    {
                        // Ajouter les colonnes pour le système de voyage
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN CurrentLocationId TEXT DEFAULT 'Foret_01'");
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN TravelDestinationId TEXT DEFAULT NULL");
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN TravelStartSteps INTEGER DEFAULT 0");
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN TravelRequiredSteps INTEGER DEFAULT 0");

                        _connection.Execute("UPDATE DatabaseVersion SET Version = 5");
                        Logger.LogInfo("LocalDatabase: Migration to version 5 completed");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"LocalDatabase: Migration error: {ex.Message}");
                    }
                }

                // Ajouter d'autres migrations ici au besoin:
                // if (currentVersion == 4 && DATABASE_VERSION >= 5) {...}
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
                               $"LastSync: {GetReadableDateFromEpoch(data.LastSyncEpochMs)}, " +
                               $"LastPause: {GetReadableDateFromEpoch(data.LastPauseEpochMs)}, " +
                               $"LastChange: {GetReadableDateFromEpoch(data.LastStepsChangeEpochMs)}, " +
                               $"DailySteps: {data.DailySteps}, LastReset: {data.LastDailyResetDate}, " +
                               $"LastApiCatchUp: {GetReadableDateFromEpoch(data.LastApiCatchUpEpochMs)}");
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
                data.LastSyncEpochMs = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // MODIFIÉ: Utiliser DateTime.Now (Faille B)
            }

            // S'assurer que LastDailyResetDate n'est pas vide
            if (string.IsNullOrEmpty(data.LastDailyResetDate))
            {
                data.LastDailyResetDate = DateTime.Now.ToString("yyyy-MM-dd"); // MODIFIÉ: Utiliser DateTime.Now (Faille B)
                Logger.LogWarning($"LocalDatabase: LastDailyResetDate was empty, initialized to {data.LastDailyResetDate}");
            }

            // NOUVEAU: S'assurer que LastApiCatchUpEpochMs a une valeur par défaut
            if (data.LastApiCatchUpEpochMs <= 0 && data.LastSyncEpochMs > 0)
            {
                data.LastApiCatchUpEpochMs = data.LastSyncEpochMs;
                Logger.LogWarning($"LocalDatabase: LastApiCatchUpEpochMs was 0, initialized to LastSyncEpochMs: {GetReadableDateFromEpoch(data.LastApiCatchUpEpochMs)}");
            }

            // Enregistrer les données
            int result = _connection.InsertOrReplace(data);
            Logger.LogInfo($"LocalDatabase: Data saved - TotalSteps: {data.TotalPlayerSteps}, " +
                           $"LastSync: {GetReadableDateFromEpoch(data.LastSyncEpochMs)}, " +
                           $"LastPause: {GetReadableDateFromEpoch(data.LastPauseEpochMs)}, " +
                           $"LastDelta: {data.LastStepsDelta}, " +
                           $"LastChange: {GetReadableDateFromEpoch(data.LastStepsChangeEpochMs)}, " +
                           $"DailySteps: {data.DailySteps}, " +
                           $"LastReset: {data.LastDailyResetDate}, " +
                           $"LastApiCatchUp: {GetReadableDateFromEpoch(data.LastApiCatchUpEpochMs)}, " +
                           $"Result: {result}");

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
                Logger.LogInfo($"LocalDatabase: Verification - Saved data: TotalSteps: {savedData.TotalPlayerSteps}, " +
                              $"LastSync: {GetReadableDateFromEpoch(savedData.LastSyncEpochMs)}, " +
                              $"LastPause: {GetReadableDateFromEpoch(savedData.LastPauseEpochMs)}, " +
                              $"DailySteps: {savedData.DailySteps}, " +
                              $"LastApiCatchUp: {GetReadableDateFromEpoch(savedData.LastApiCatchUpEpochMs)}");

                // Vérifier que les valeurs correspondent
                if (savedData.TotalPlayerSteps != originalData.TotalPlayerSteps ||
                    savedData.LastSyncEpochMs != originalData.LastSyncEpochMs ||
                    savedData.DailySteps != originalData.DailySteps ||
                    savedData.LastDailyResetDate != originalData.LastDailyResetDate ||
                    savedData.LastApiCatchUpEpochMs != originalData.LastApiCatchUpEpochMs)
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