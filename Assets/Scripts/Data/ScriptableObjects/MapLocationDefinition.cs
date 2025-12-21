// Purpose: Extended MapLocationDefinition with detailed info and activities  
// Filepath: Assets/Scripts/Data/ScriptableObjects/MapLocationDefinition.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "New Location", menuName = "WalkAndRPG/Map Location")]
public class MapLocationDefinition : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Unique identifier for this location")]
    public string LocationID;

    [Tooltip("Display name shown in UI")]
    public string DisplayName;

    [Tooltip("Short description for POI tooltips")]
    [TextArea(1, 2)]
    public string Description;

    [Header("Detailed Info")]
    [Tooltip("Long, detailed description for the location details panel")]
    [TextArea(3, 8)]
    public string LongDescription;

    [Tooltip("Main image representing this location (for details panel)")]
    public Sprite LocationImage;

    [Tooltip("Background color or theme for this location")]
    public Color LocationThemeColor = Color.white;

    [Header("Travel Connections")]
    [Tooltip("Other locations this one connects to for travel")]
    public List<LocationConnection> Connections = new List<LocationConnection>();

    [Header("Activities")]
    [Tooltip("Activities available at this location")]
    public List<LocationActivity> AvailableActivities = new List<LocationActivity>();

    [Header("Combat")]
    [Tooltip("Enemies that can be fought at this location")]
    public List<LocationEnemy> AvailableEnemies = new List<LocationEnemy>();

    [Header("Social")]
    [Tooltip("NPCs that can be interacted with at this location")]
    public List<NPCDefinition> AvailableNPCs = new List<NPCDefinition>();

    [Header("Visual")]
    [Tooltip("Icon for POI representation on map")]
    public Sprite LocationIcon;

    [Tooltip("Color for POI on map")]
    public Color POIColor = Color.white;

    [Header("Game Design")]
    [Tooltip("Is this location currently accessible?")]
    public bool IsAccessible = true;

    [Tooltip("Steps or level required to unlock this location")]
    public int UnlockRequirement = 0;

    [Header("Debug")]
    [Tooltip("Development notes")]
    [TextArea(1, 3)]
    public string DeveloperNotes;

    /// <summary>
    /// Get all valid activities available at this location
    /// </summary>
    public List<LocationActivity> GetAvailableActivities()
    {
        if (AvailableActivities == null)
            return new List<LocationActivity>();

        return AvailableActivities.Where(activity => activity != null && activity.IsValidActivity()).ToList();
    }

    /// <summary>
    /// Get activity by ID
    /// </summary>
    public LocationActivity GetActivityById(string activityId)
    {
        if (AvailableActivities == null || string.IsNullOrEmpty(activityId))
            return null;

        return AvailableActivities.FirstOrDefault(activity =>
            activity != null &&
            activity.ActivityReference != null &&
            activity.ActivityReference.ActivityID == activityId);
    }

    /// <summary>
    /// Check if a specific activity is available at this location
    /// </summary>
    public bool HasActivity(string activityId)
    {
        return GetActivityById(activityId) != null;
    }

    /// <summary>
    /// Get count of available activities
    /// </summary>
    public int GetActivityCount()
    {
        return GetAvailableActivities().Count;
    }

    /// <summary>
    /// Get display text for activities (for UI summary)
    /// </summary>
    public string GetActivitiesSummary()
    {
        var validActivities = GetAvailableActivities();

        if (validActivities.Count == 0)
            return "Aucune activite disponible";

        if (validActivities.Count == 1)
            return $"1 activite: {validActivities[0].GetDisplayName()}";

        return $"{validActivities.Count} activites disponibles";
    }

    // === COMBAT METHODS ===

    /// <summary>
    /// Get all valid enemies available at this location
    /// </summary>
    public List<LocationEnemy> GetAvailableEnemies()
    {
        if (AvailableEnemies == null)
            return new List<LocationEnemy>();

        return AvailableEnemies.Where(enemy => enemy != null && enemy.IsValid()).ToList();
    }

    /// <summary>
    /// Get enemy by ID
    /// </summary>
    public LocationEnemy GetEnemyById(string enemyId)
    {
        if (AvailableEnemies == null || string.IsNullOrEmpty(enemyId))
            return null;

        return AvailableEnemies.FirstOrDefault(enemy =>
            enemy != null &&
            enemy.EnemyReference != null &&
            enemy.EnemyReference.EnemyID == enemyId);
    }

    /// <summary>
    /// Check if combat is available at this location
    /// </summary>
    public bool HasCombat()
    {
        return GetAvailableEnemies().Count > 0;
    }

    /// <summary>
    /// Get count of available enemies
    /// </summary>
    public int GetEnemyCount()
    {
        return GetAvailableEnemies().Count;
    }

    /// <summary>
    /// Get display text for combat (for UI summary)
    /// </summary>
    public string GetCombatSummary()
    {
        var validEnemies = GetAvailableEnemies();

        if (validEnemies.Count == 0)
            return "Pas de combat disponible";

        if (validEnemies.Count == 1)
            return $"1 ennemi: {validEnemies[0].GetDisplayName()}";

        return $"{validEnemies.Count} ennemis disponibles";
    }

    // === NPC METHODS ===

    /// <summary>
    /// Get all valid NPCs available at this location
    /// </summary>
    public List<NPCDefinition> GetAvailableNPCs()
    {
        if (AvailableNPCs == null)
            return new List<NPCDefinition>();

        return AvailableNPCs.Where(npc => npc != null && npc.IsValid() && npc.IsActive).ToList();
    }

    /// <summary>
    /// Get NPC by ID
    /// </summary>
    public NPCDefinition GetNPCById(string npcId)
    {
        if (AvailableNPCs == null || string.IsNullOrEmpty(npcId))
            return null;

        return AvailableNPCs.FirstOrDefault(npc =>
            npc != null &&
            npc.NPCID == npcId);
    }

    /// <summary>
    /// Check if NPCs are available at this location
    /// </summary>
    public bool HasNPCs()
    {
        return GetAvailableNPCs().Count > 0;
    }

    /// <summary>
    /// Get count of available NPCs
    /// </summary>
    public int GetNPCCount()
    {
        return GetAvailableNPCs().Count;
    }

    /// <summary>
    /// Get display text for NPCs (for UI summary)
    /// </summary>
    public string GetNPCSummary()
    {
        var validNPCs = GetAvailableNPCs();

        if (validNPCs.Count == 0)
            return "Aucun habitant";

        if (validNPCs.Count == 1)
            return $"1 habitant: {validNPCs[0].GetDisplayName()}";

        return $"{validNPCs.Count} habitants";
    }

    /// <summary>
    /// Get the best description to show (long if available, short otherwise)
    /// </summary>
    public string GetBestDescription()
    {
        if (!string.IsNullOrEmpty(LongDescription))
            return LongDescription;

        if (!string.IsNullOrEmpty(Description))
            return Description;

        return "Aucune description disponible.";
    }

    /// <summary>
    /// Validate this location definition
    /// </summary>
    public bool IsValid()
    {
        List<string> errors = new List<string>();

        if (string.IsNullOrEmpty(LocationID))
            errors.Add("LocationID is empty");

        if (string.IsNullOrEmpty(DisplayName))
            errors.Add("DisplayName is empty");

        // Validate connections
        if (Connections != null)
        {
            for (int i = 0; i < Connections.Count; i++)
            {
                var connection = Connections[i];
                if (connection == null)
                {
                    errors.Add($"Connection {i} is null");
                    continue;
                }

                if (string.IsNullOrEmpty(connection.DestinationLocationID))
                    errors.Add($"Connection {i} has empty DestinationLocationID");

                if (connection.StepCost <= 0)
                    errors.Add($"Connection {i} has invalid StepCost: {connection.StepCost}");
            }
        }

        // Validate activities
        if (AvailableActivities != null)
        {
            for (int i = 0; i < AvailableActivities.Count; i++)
            {
                var activity = AvailableActivities[i];
                if (activity == null)
                {
                    errors.Add($"Activity {i} is null");
                    continue;
                }

                if (activity.ActivityReference == null)
                    errors.Add($"Activity {i} has no ActivityReference");
            }
        }

        if (errors.Count > 0)
        {
            Logger.LogError($"MapLocationDefinition '{name}' validation failed:\n{string.Join("\n", errors)}", Logger.LogCategory.General);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Get the location icon with fallback logic
    /// </summary>
    public Sprite GetIcon()
    {
        // Retourne l'ic�ne specifique de la location (verification Unity-safe)
        if (LocationIcon != null) return LocationIcon;

        // Fallback possible : ic�ne par defaut selon le type de location
        // if (Type == LocationType.Village && defaultVillageIcon != null) return defaultVillageIcon;
        // if (Type == LocationType.Forest && defaultForestIcon != null) return defaultForestIcon;

        // Fallback final : aucune ic�ne
        return null;
    }

    /// <summary>
    /// Version alternative avec ic�ne par defaut
    /// </summary>
    public Sprite GetIconWithFallback(Sprite defaultIcon = null)
    {
        if (LocationIcon != null) return LocationIcon;
        return defaultIcon;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-generate LocationID from name if empty
        if (string.IsNullOrEmpty(LocationID) && !string.IsNullOrEmpty(name))
        {
            LocationID = name.Replace(" ", "_");
        }

        // Auto-generate DisplayName from LocationID if empty
        if (string.IsNullOrEmpty(DisplayName) && !string.IsNullOrEmpty(LocationID))
        {
            DisplayName = LocationID.Replace("_", " ");
        }

        // Remove null entries from lists
        if (Connections != null)
        {
            Connections.RemoveAll(c => c == null);
        }

        if (AvailableActivities != null)
        {
            AvailableActivities.RemoveAll(a => a == null);
        }
    }
#endif
}

// Keep the existing LocationConnection class
[System.Serializable]
public class LocationConnection
{
    [Tooltip("ID of the destination location")]
    public string DestinationLocationID;

    [Tooltip("Number of steps required to travel to this destination")]
    public int StepCost = 100;

    [Tooltip("Is this connection currently available?")]
    public bool IsAvailable = true;

    [Tooltip("Special requirements to use this connection")]
    public string Requirements;
}

/// <summary>
/// Represents an enemy available at a location for combat
/// </summary>
[System.Serializable]
public class LocationEnemy
{
    [Tooltip("Reference to the enemy definition")]
    public EnemyDefinition EnemyReference;

    [Tooltip("Is this enemy currently available to fight?")]
    public bool IsAvailable = true;

    [Tooltip("Minimum player level/requirement to see this enemy")]
    public int MinimumRequirement = 0;

    [Tooltip("Special requirements or description")]
    [TextArea(1, 2)]
    public string Requirements;

    /// <summary>
    /// Get display name for this enemy
    /// </summary>
    public string GetDisplayName()
    {
        return EnemyReference?.GetDisplayName() ?? "Unknown Enemy";
    }

    /// <summary>
    /// Check if this enemy entry is valid
    /// </summary>
    public bool IsValid()
    {
        return EnemyReference != null && IsAvailable;
    }

    /// <summary>
    /// Check if player can fight this enemy (based on requirements)
    /// </summary>
    public bool CanFight()
    {
        if (!IsValid()) return false;
        // TODO: Check player level/requirements when progression system is added
        return true;
    }
}