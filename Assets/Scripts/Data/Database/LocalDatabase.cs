// Filepath: Assets/Scripts/Data/Database/LocalDatabase.cs
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class LocalDatabase
{
    private SQLiteConnection _connection;
    private string _databasePath;

    private const string DatabaseFilename = "StepQuestRPG_Data.db";
    private const int DATABASE_VERSION = 8; // MODIFIE: Incremente pour ActivityData (5 -> 6)

    public void InitializeDatabase()
    {
        // Definir le chemin de la base de donnees
        _databasePath = Path.Combine(Application.persistentDataPath, DatabaseFilename);


        try
        {
            // Force la suppression de la base de donnees existante uniquement en mode editeur
#if UNITY_EDITOR
            bool forceReset = false; // À activer seulement pour les tests en editeur
            if (forceReset && File.Exists(_databasePath))
            {
                File.Delete(_databasePath);

            }
#endif

            // Creer ou ouvrir la connexion SQLite
            _connection = new SQLiteConnection(_databasePath);


            // Verifier si la base de donnees necessite une migration
            ManageDatabaseMigration();

            // Creer les tables principales
            CreateTables();

            // Verifier la structure des tables
            DebugLogTableStructures();
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Initialization error: {ex.Message}");
        }
    }

    /// <summary>
    /// NOUVEAU: Creer toutes les tables necessaires
    /// </summary>
    private void CreateTables()
    {
        // Table PlayerData (existante)
        _connection.CreateTable<PlayerData>();


        // Table InventoryContainers (existante)
        _connection.CreateTable<InventoryContainerData>();

    }

    // Methode utilitaire pour convertir un epoch timestamp en date lisible
    public static string GetReadableDateFromEpoch(long epochMs)
    {
        if (epochMs <= 0) return "Jamais";
        try
        {
            // MODIFIE: Utiliser DateTime.Now et le fuseau horaire local (Faille B)
            DateTime date = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(epochMs);
            return date.ToLocalTime().ToString("HH:mm:ss dd/MM/yyyy");
        }
        catch
        {
            return "Epoch invalide";
        }
    }

    // Gestion des migrations de base de donnees
    private void ManageDatabaseMigration()
    {
        try
        {
            // Creer une table de version si elle n'existe pas
            _connection.Execute("CREATE TABLE IF NOT EXISTS DatabaseVersion (Version INTEGER)");

            // Recuperer la version actuelle
            int currentVersion = 0;
            var result = _connection.Query<DatabaseVersionInfo>("SELECT Version FROM DatabaseVersion LIMIT 1");
            if (result.Count > 0)
            {
                currentVersion = result[0].Version;
            }
            else
            {
                // Aucune version trouvee, inserer la version initiale
                _connection.Execute("INSERT INTO DatabaseVersion (Version) VALUES (1)");
                currentVersion = 1;
            }

            Logger.LogInfo($"LocalDatabase: Current database version: {currentVersion}, Target version: {DATABASE_VERSION}");

            // Appliquer les migrations necessaires
            if (currentVersion < DATABASE_VERSION)
            {
                // Migrations existantes (2, 3, 4, 5)
                ApplyMigrations(currentVersion);

                // MIGRATION: Version 5 -> 6 pour ActivityData (existante)
                if (currentVersion < 6 && DATABASE_VERSION >= 6)
                {
                    Logger.LogInfo("LocalDatabase: Migrating to version 6 - Adding ActivityData support...");

                    try
                    {
                        // Ajouter la colonne CurrentActivityJson a la table PlayerData
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN CurrentActivityJson TEXT DEFAULT NULL");

                        Logger.LogInfo("LocalDatabase: Added CurrentActivityJson column to PlayerData table");

                        // Mettre a jour la version
                        _connection.Execute("UPDATE DatabaseVersion SET Version = 6");
                        Logger.LogInfo("LocalDatabase: Migration to version 6 completed");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"LocalDatabase: Migration to version 6 error: {ex.Message}");
                    }
                }

                // ⭐ NOUVELLE MIGRATION: Version 6 -> 7 pour voyage multi-segment
                if (currentVersion < 7 && DATABASE_VERSION >= 7)
                {
                    Logger.LogInfo("LocalDatabase: Migrating to version 7 - Adding multi-segment travel support...");

                    try
                    {
                        // Ajouter les colonnes pour les voyages multi-segments
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN TravelFinalDestinationId TEXT DEFAULT NULL");
                        _connection.Execute("ALTER TABLE PlayerData ADD COLUMN TravelOriginLocationId TEXT DEFAULT NULL");

                        Logger.LogInfo("LocalDatabase: Added TravelFinalDestinationId and TravelOriginLocationId columns");

                        // Mettre a jour la version
                        _connection.Execute("UPDATE DatabaseVersion SET Version = 7");
                        Logger.LogInfo("LocalDatabase: Migration to version 7 completed");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"LocalDatabase: Migration to version 7 error: {ex.Message}");
                    }
                }
                // Version 7 -> 8 pour le système d'XP
                if (currentVersion < 8 && DATABASE_VERSION >= 8)
                {
                    Logger.LogInfo("LocalDatabase: Migrating to version 8 - Adding XP system support...");

                    try
                    {
                        // Vérifier et ajouter les colonnes XP seulement si elles n'existent pas
                        var tableInfo = _connection.GetTableInfo("PlayerData");
                        bool hasSkillsJson = tableInfo.Any(col => col.Name == "SkillsJson");
                        bool hasSubSkillsJson = tableInfo.Any(col => col.Name == "SubSkillsJson");

                        if (!hasSkillsJson)
                        {
                            _connection.Execute("ALTER TABLE PlayerData ADD COLUMN SkillsJson TEXT DEFAULT NULL");
                            Logger.LogInfo("LocalDatabase: Added SkillsJson column");
                        }
                        else
                        {
                            Logger.LogInfo("LocalDatabase: SkillsJson column already exists");
                        }

                        if (!hasSubSkillsJson)
                        {
                            _connection.Execute("ALTER TABLE PlayerData ADD COLUMN SubSkillsJson TEXT DEFAULT NULL");
                            Logger.LogInfo("LocalDatabase: Added SubSkillsJson column");
                        }
                        else
                        {
                            Logger.LogInfo("LocalDatabase: SubSkillsJson column already exists");
                        }

                        // Mettre a jour la version
                        _connection.Execute("UPDATE DatabaseVersion SET Version = 8");
                        Logger.LogInfo("LocalDatabase: Migration to version 8 completed");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"LocalDatabase: Migration to version 8 error: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Migration management error: {ex.Message}");
        }
    }

    /// <summary>
    /// Appliquer les migrations existantes (versions 2, 3, 4, 5)
    /// </summary>
    private void ApplyMigrations(int currentVersion)
    {
        // Migration de la version 1 a 2
        if (currentVersion == 1 && DATABASE_VERSION >= 2)
        {

            try
            {
                _connection.Execute("ALTER TABLE PlayerData ADD COLUMN LastStepsDelta INTEGER DEFAULT 0");
                _connection.Execute("ALTER TABLE PlayerData ADD COLUMN LastStepsChangeEpochMs INTEGER DEFAULT 0");
                _connection.Execute("UPDATE DatabaseVersion SET Version = 2");

                currentVersion = 2;
            }
            catch (Exception ex)
            {
                Logger.LogError($"LocalDatabase: Migration to version 2 error: {ex.Message}");
            }
        }

        // Migration de la version 2 a 3
        if (currentVersion == 2 && DATABASE_VERSION >= 3)
        {

            try
            {
                _connection.Execute("ALTER TABLE PlayerData ADD COLUMN DailySteps INTEGER DEFAULT 0");
                _connection.Execute("ALTER TABLE PlayerData ADD COLUMN LastDailyResetDate TEXT DEFAULT ''");
                string todayDate = DateTime.Now.ToString("yyyy-MM-dd");
                _connection.Execute($"UPDATE PlayerData SET LastDailyResetDate = '{todayDate}'");
                _connection.Execute("UPDATE DatabaseVersion SET Version = 3");

                currentVersion = 3;
            }
            catch (Exception ex)
            {
                Logger.LogError($"LocalDatabase: Migration to version 3 error: {ex.Message}");
            }
        }

        // Migration de la version 3 a 4
        if (currentVersion == 3 && DATABASE_VERSION >= 4)
        {

            try
            {
                _connection.Execute("ALTER TABLE PlayerData ADD COLUMN LastApiCatchUpEpochMs INTEGER DEFAULT 0");
                _connection.Execute("UPDATE PlayerData SET LastApiCatchUpEpochMs = LastSyncEpochMs");
                _connection.Execute("UPDATE DatabaseVersion SET Version = 4");

                currentVersion = 4;
            }
            catch (Exception ex)
            {
                Logger.LogError($"LocalDatabase: Migration to version 4 error: {ex.Message}");
            }
        }

        // Migration de la version 4 a 5 (InventoryContainers)
        if (currentVersion == 4 && DATABASE_VERSION >= 5)
        {


            try
            {
                // La table sera creee automatiquement par CreateTable<InventoryContainerData>()
                // Pas besoin de CREATE TABLE manuel

                // Initialiser les conteneurs par defaut
                InitializeDefaultContainers();

                // Mettre a jour la version
                _connection.Execute("UPDATE DatabaseVersion SET Version = 5");

                currentVersion = 5;
            }
            catch (Exception ex)
            {
                Logger.LogError($"LocalDatabase: Migration to version 5 error: {ex.Message}");
            }
        }

        // La migration vers la version 6 sera geree dans ManageDatabaseMigration()
    }

    /// <summary>
    /// Initialiser les conteneurs par defaut lors de la première migration (version 5)
    /// </summary>
    private void InitializeDefaultContainers()
    {
        // Verifier si les conteneurs existent deja
        var existingContainers = _connection.Query<InventoryContainerData>("SELECT ContainerID FROM InventoryContainers");

        if (existingContainers.Count == 0)
        {


            // Creer conteneur joueur par defaut
            var playerContainer = new InventoryContainerData
            {
                ContainerID = "player",
                ContainerType = "Player",
                MaxSlots = 20,
                SlotsData = CreateEmptySlots(20),
                LastResetStepCount = 0
            };

            // Creer conteneur banque par defaut
            var bankContainer = new InventoryContainerData
            {
                ContainerID = "bank",
                ContainerType = "Bank",
                MaxSlots = 50,
                SlotsData = CreateEmptySlots(50),
                LastResetStepCount = 0
            };

            // Inserer dans la base de donnees
            _connection.Insert(playerContainer);
            _connection.Insert(bankContainer);


        }
        else
        {
            Logger.LogInfo($"LocalDatabase: Found {existingContainers.Count} existing containers, skipping default initialization");
        }
    }

    /// <summary>
    /// Creer des slots vides en format JSON
    /// </summary>
    private string CreateEmptySlots(int slotCount)
    {
        var emptySlots = new List<InventorySlot>();
        for (int i = 0; i < slotCount; i++)
        {
            emptySlots.Add(new InventorySlot());
        }
        return JsonConvert.SerializeObject(emptySlots);
    }

    // === METHODES PLAYERDATA ===

    public PlayerData LoadPlayerData()
    {
        if (_connection == null)
        {
            Logger.LogError("LocalDatabase: Connection not initialized");
            return new PlayerData();
        }

        try
        {
            PlayerData data = _connection.Table<PlayerData>().FirstOrDefault(p => p.Id == 1);

            if (data != null)
            {

                return data;
            }
            else
            {
                PlayerData newData = new PlayerData();
                _connection.Insert(newData);

                return newData;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Error loading PlayerData: {ex.Message}");
            return new PlayerData();
        }
    }

    public void SavePlayerData(PlayerData data)
    {
        // AJOUT: Protection contre les appels après fermeture
        if (_connection == null)
        {
            Logger.LogWarning("LocalDatabase: SavePlayerData called but connection is null (probably shutting down)");
            return;
        }

        if (data == null)
        {
            Logger.LogError("LocalDatabase: PlayerData is null");
            return;
        }

        try
        {
            if (data.Id <= 0)
                data.Id = 1;

            if (data.LastSyncEpochMs <= 0 && data.TotalPlayerSteps > 0)
            {
                Logger.LogWarning("LocalDatabase: LastSyncEpochMs needs initialization");
                data.LastSyncEpochMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }

            if (string.IsNullOrEmpty(data.LastDailyResetDate))
            {
                data.LastDailyResetDate = DateTime.Now.ToString("yyyy-MM-dd");
                Logger.LogWarning($"LocalDatabase: LastDailyResetDate was empty, initialized to {data.LastDailyResetDate}");
            }

            if (data.LastApiCatchUpEpochMs <= 0 && data.LastSyncEpochMs > 0)
            {
                data.LastApiCatchUpEpochMs = data.LastSyncEpochMs;
                Logger.LogWarning($"LocalDatabase: LastApiCatchUpEpochMs was 0, initialized to LastSyncEpochMs: {GetReadableDateFromEpoch(data.LastApiCatchUpEpochMs)}");
            }

            int result = _connection.InsertOrReplace(data);
            Logger.LogInfo($"LocalDatabase: PlayerData saved - TotalSteps: {data.TotalPlayerSteps}, " +
                           $"LastSync: {GetReadableDateFromEpoch(data.LastSyncEpochMs)}, " +
                           $"LastPause: {GetReadableDateFromEpoch(data.LastPauseEpochMs)}, " +
                           $"LastDelta: {data.LastStepsDelta}, " +
                           $"LastChange: {GetReadableDateFromEpoch(data.LastStepsChangeEpochMs)}, " +
                           $"DailySteps: {data.DailySteps}, " +
                           $"LastReset: {data.LastDailyResetDate}, " +
                           $"LastApiCatchUp: {GetReadableDateFromEpoch(data.LastApiCatchUpEpochMs)}, " +
                           $"Activity: {(data.HasActiveActivity() ? data.CurrentActivity.ActivityId : "None")}, " +
                           $"Result: {result}");

#if UNITY_EDITOR || DEVELOPMENT_BUILD 
            VerifySaveSuccess(data);
#endif
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Save PlayerData error: {ex.Message}");
        }
    }

    // === METHODES INVENTORY (existantes, pas changees) ===

    /// <summary>
    /// Charger un conteneur d'inventaire par ID
    /// </summary>
    public InventoryContainerData LoadInventoryContainer(string containerId)
    {
        if (_connection == null)
        {
            Logger.LogError("LocalDatabase: Connection not initialized");
            return null;
        }

        try
        {
            var containerData = _connection.Table<InventoryContainerData>()
                .FirstOrDefault(c => c.ContainerID == containerId);

            if (containerData == null)
            {


                Logger.LogWarning($"LocalDatabase: Container '{containerId}' not found in database");
            }

            return containerData;
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Error loading container '{containerId}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Sauvegarder un conteneur d'inventaire
    /// </summary>
    public void SaveInventoryContainer(InventoryContainerData containerData)
    {
        // AJOUT: Protection contre les appels après fermeture
        if (_connection == null)
        {
            Logger.LogWarning($"LocalDatabase: SaveInventoryContainer called but connection is null (probably shutting down)");
            return;
        }

        if (containerData == null)
        {
            Logger.LogError("LocalDatabase: InventoryContainerData is null");
            return;
        }

        try
        {
            int result = _connection.InsertOrReplace(containerData);
            Logger.LogInfo($"LocalDatabase: Container '{containerData.ContainerID}' saved - Type: {containerData.ContainerType}, MaxSlots: {containerData.MaxSlots}, Result: {result}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Error saving container '{containerData.ContainerID}': {ex.Message}");
        }
    }

    /// <summary>
    /// Charger tous les conteneurs d'inventaire
    /// </summary>
    public List<InventoryContainerData> LoadAllInventoryContainers()
    {
        if (_connection == null)
        {
            Logger.LogError("LocalDatabase: Connection not initialized");
            return new List<InventoryContainerData>();
        }

        try
        {
            var containers = _connection.Table<InventoryContainerData>().ToList();

            return containers;
        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Error loading all containers: {ex.Message}");
            return new List<InventoryContainerData>();
        }
    }

    public void CloseDatabase()
    {
        if (_connection != null)
        {
            _connection.Close();
            _connection = null;

        }
    }

    // === METHODES PRIVEES ===

    private void DebugLogTableStructures()
    {
        try
        {
            // Structure PlayerData
            var playerTableInfo = _connection.GetTableInfo("PlayerData");
            string playerColumns = string.Join(", ", playerTableInfo.Select(col => col.Name));


            // Structure InventoryContainers
            var inventoryTableInfo = _connection.GetTableInfo("InventoryContainers");
            string inventoryColumns = string.Join(", ", inventoryTableInfo.Select(col => col.Name));

        }
        catch (Exception ex)
        {
            Logger.LogError($"LocalDatabase: Error checking table structures: {ex.Message}");
        }
    }

    private void VerifySaveSuccess(PlayerData originalData)
    {
        try
        {
            var savedData = _connection.Table<PlayerData>().FirstOrDefault(p => p.Id == 1);
            if (savedData != null)
            {

                if (savedData.TotalPlayerSteps != originalData.TotalPlayerSteps ||
                    savedData.LastSyncEpochMs != originalData.LastSyncEpochMs ||
                    savedData.DailySteps != originalData.DailySteps ||
                    savedData.LastDailyResetDate != originalData.LastDailyResetDate ||
                    savedData.LastApiCatchUpEpochMs != originalData.LastApiCatchUpEpochMs ||
                    savedData.CurrentActivityJson != originalData.CurrentActivityJson)
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

// Classe pour stocker les informations de version de la base de donnees
class DatabaseVersionInfo
{
    public int Version { get; set; }
}

// Structure de donnees pour les conteneurs d'inventaire en base (existante, pas changee)
[Table("InventoryContainers")]
public class InventoryContainerData
{
    [PrimaryKey]
    public string ContainerID { get; set; }

    public string ContainerType { get; set; }

    public int MaxSlots { get; set; }

    [Column("SlotsData")]
    public string SlotsData { get; set; } // JSON des InventorySlot

    public long LastResetStepCount { get; set; }

    /// <summary>
    /// Convertir vers InventoryContainer (pour utilisation dans le jeu)
    /// </summary>
    public InventoryContainer ToInventoryContainer()
    {
        // Parse container type
        InventoryContainerType containerType = InventoryContainerType.Player;
        if (Enum.TryParse<InventoryContainerType>(ContainerType, out var parsedType))
        {
            containerType = parsedType;
        }

        // Creer le conteneur
        var container = new InventoryContainer(containerType, ContainerID, MaxSlots);

        // Deserialiser les slots
        if (!string.IsNullOrEmpty(SlotsData))
        {
            try
            {
                var slots = JsonConvert.DeserializeObject<List<InventorySlot>>(SlotsData);
                if (slots != null)
                {
                    // Remplacer les slots
                    container.Slots.Clear();
                    container.Slots.AddRange(slots);

                    // S'assurer qu'on a le bon nombre de slots
                    while (container.Slots.Count < MaxSlots)
                    {
                        container.Slots.Add(new InventorySlot());
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"InventoryContainerData: Error deserializing slots for '{ContainerID}': {ex.Message}");
            }
        }

        // Metadonnees
        if (LastResetStepCount > 0)
        {
            container.SetMetadata("LastResetStepCount", LastResetStepCount.ToString());
        }

        return container;
    }

    /// <summary>
    /// Creer depuis InventoryContainer (pour sauvegarde en base)
    /// </summary>
    public static InventoryContainerData FromInventoryContainer(InventoryContainer container)
    {
        var data = new InventoryContainerData
        {
            ContainerID = container.ContainerID,
            ContainerType = container.ContainerType.ToString(),
            MaxSlots = container.MaxSlots,
            SlotsData = JsonConvert.SerializeObject(container.Slots),
            LastResetStepCount = 0
        };

        // Recuperer LastResetStepCount des metadonnees si present
        if (long.TryParse(container.GetMetadata("LastResetStepCount", "0"), out long lastReset))
        {
            data.LastResetStepCount = lastReset;
        }

        return data;
    }
}