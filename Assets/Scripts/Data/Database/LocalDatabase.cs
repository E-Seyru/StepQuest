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
            _connection.Execute("CREATE TABLE IF NOT EXISTS DatabaseVersion (Version INTEGER)");

            int currentVersion = GetCurrentDatabaseVersion();
            Logger.LogInfo($"LocalDatabase: Current database version: {currentVersion}, Target version: {DATABASE_VERSION}");

            while (currentVersion < DATABASE_VERSION)
            {
                Logger.LogInfo($"LocalDatabase: Migrating from version {currentVersion} to {currentVersion + 1}...");
                bool migrationSuccess = false;
                switch (currentVersion)
                {
                    case 1:
                        migrationSuccess = MigrateFrom1To2();
                        break;
                    case 2:
                        migrationSuccess = MigrateFrom2To3();
                        break;
                    case 3:
                        migrationSuccess = MigrateFrom3To4();
                        break;
                    case 4:
                        migrationSuccess = MigrateFrom4To5();
                        break;
                    // Ajouter d'autres cas de migration ici
                    default:
                        Logger.LogError($"LocalDatabase: No migration path defined for version {currentVersion}. Halting migration.");
                        return; // Arrêter si aucune migration n'est définie
                }

                if (migrationSuccess)
                {
                    currentVersion++; // Incrémenter uniquement si la migration a réussi
                    _connection.Execute("UPDATE DatabaseVersion SET Version = ?", currentVersion);
                    Logger.LogInfo($"LocalDatabase: Migration to version {currentVersion} completed successfully.");
                }
                else
                {
                    Logger.LogError($"LocalDatabase: Migration from version {currentVersion} failed. Halting further migrations.");
                    return; // Arrêter les migrations si une étape échoue
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Migration management error: {ex.Message}");
        }
    }

    private int GetCurrentDatabaseVersion()
    {
        var result = _connection.Query<DatabaseVersionInfo>("SELECT Version FROM DatabaseVersion LIMIT 1");
        if (result.Count > 0)
        {
            return result[0].Version;
        }
        else
        {
            _connection.Execute("INSERT INTO DatabaseVersion (Version) VALUES (1)");
            return 1;
        }
    }

    private bool MigrateFrom1To2()
    {
        try
        {
            _connection.Execute("ALTER TABLE PlayerData ADD COLUMN LastStepsDelta INTEGER DEFAULT 0");
            _connection.Execute("ALTER TABLE PlayerData ADD COLUMN LastStepsChangeEpochMs INTEGER DEFAULT 0");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Migration error (1 to 2): {ex.Message}");
            return false;
        }
    }

    private bool MigrateFrom2To3()
    {
        try
        {
            _connection.Execute("ALTER TABLE PlayerData ADD COLUMN DailySteps INTEGER DEFAULT 0");
            _connection.Execute("ALTER TABLE PlayerData ADD COLUMN LastDailyResetDate TEXT DEFAULT ''");
            string todayDate = DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd");
            _connection.Execute($"UPDATE PlayerData SET LastDailyResetDate = '{todayDate}'");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Migration error (2 to 3): {ex.Message}");
            return false;
        }
    }

    private bool MigrateFrom3To4()
    {
        try
        {
            _connection.Execute("ALTER TABLE PlayerData ADD COLUMN LastApiCatchUpEpochMs INTEGER DEFAULT 0");
            _connection.Execute("UPDATE PlayerData SET LastApiCatchUpEpochMs = LastSyncEpochMs");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Migration error (3 to 4): {ex.Message}");
            return false;
        }
    }

    private bool MigrateFrom4To5()
    {
        try
        {
            _connection.Execute("ALTER TABLE PlayerData ADD COLUMN CurrentLocationId TEXT DEFAULT 'Foret_01'");
            _connection.Execute("ALTER TABLE PlayerData ADD COLUMN TravelDestinationId TEXT DEFAULT NULL");
            _connection.Execute("ALTER TABLE PlayerData ADD COLUMN TravelStartSteps INTEGER DEFAULT 0");
            _connection.Execute("ALTER TABLE PlayerData ADD COLUMN TravelRequiredSteps INTEGER DEFAULT 0");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Migration error (4 to 5): {ex.Message}");
            return false;
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
                // Reduced verbosity: Log key fields for confirmation.
                Logger.LogInfo($"LocalDatabase: PlayerData loaded (Id: {data.Id}). TotalSteps: {data.TotalPlayerSteps}, DailySteps: {data.DailySteps}, LastSync: {GetReadableDateFromEpoch(data.LastSyncEpochMs)}.");
                return data;
            }
            else
            {
                // Créer un nouveau PlayerData si aucun n'existe
                PlayerData newData = new PlayerData(); // Default Id is 1
                _connection.Insert(newData);
                // Log creation of new data specifically.
                Logger.LogInfo($"LocalDatabase: No existing PlayerData found. New PlayerData (Id: {newData.Id}) created and saved.");
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
                // Standardize to UTC for internal storage
                data.LastSyncEpochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            // S'assurer que LastDailyResetDate n'est pas vide
            if (string.IsNullOrEmpty(data.LastDailyResetDate))
            {
                // LastDailyResetDate should reflect the user's local date
                data.LastDailyResetDate = DateTime.UtcNow.ToLocalTime().ToString("yyyy-MM-dd");
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
            // Reduced verbosity: Log key fields and operation result.
            Logger.LogInfo($"LocalDatabase: PlayerData saved (Id: {data.Id}, SQLite result: {result}). TotalSteps: {data.TotalPlayerSteps}, DailySteps: {data.DailySteps}, LastSync: {GetReadableDateFromEpoch(data.LastSyncEpochMs)}.");

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
            var savedData = _connection.Table<PlayerData>().FirstOrDefault(p => p.Id == 1); // Assuming Id is always 1 for PlayerData
            if (savedData != null)
            {
                // Reduced verbosity for the info log during verification.
                Logger.LogInfo($"LocalDatabase: Verifying saved PlayerData (Id: {savedData.Id}). Verified TotalSteps: {savedData.TotalPlayerSteps}, LastSync: {GetReadableDateFromEpoch(savedData.LastSyncEpochMs)}.");

                // Vérifier que les valeurs correspondent
                // Keep essential fields for mismatch check.
                if (savedData.TotalPlayerSteps != originalData.TotalPlayerSteps ||
                    savedData.LastSyncEpochMs != originalData.LastSyncEpochMs || // Key sync timestamp
                    savedData.DailySteps != originalData.DailySteps || // Important for daily mechanics
                    savedData.LastDailyResetDate != originalData.LastDailyResetDate || // Critical for daily resets
                    savedData.LastApiCatchUpEpochMs != originalData.LastApiCatchUpEpochMs) // Important for API sync logic
                {
                    // Log all fields in case of a mismatch for detailed debugging.
                    Logger.LogError("LocalDatabase: Verification FAILED - Data mismatch after save. " +
                                   $"Expected Total: {originalData.TotalPlayerSteps}, Got: {savedData.TotalPlayerSteps}. " +
                                   $"Expected Daily: {originalData.DailySteps}, Got: {savedData.DailySteps}. " +
                                   $"Expected LastSync: {GetReadableDateFromEpoch(originalData.LastSyncEpochMs)}, Got: {GetReadableDateFromEpoch(savedData.LastSyncEpochMs)}. " +
                                   $"Expected LastReset: {originalData.LastDailyResetDate}, Got: {savedData.LastDailyResetDate}. " +
                                   $"Expected LastApiCatchUp: {GetReadableDateFromEpoch(originalData.LastApiCatchUpEpochMs)}, Got: {GetReadableDateFromEpoch(savedData.LastApiCatchUpEpochMs)}."
                                   );
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Verification error: {ex.Message}");
        }
    }
    
    // Classe privée pour stocker les informations de version de la base de données
    private class DatabaseVersionInfo
    {
        public int Version { get; set; }
    }
}