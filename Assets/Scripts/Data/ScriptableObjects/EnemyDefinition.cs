// Purpose: ScriptableObject defining an enemy for combat
// Filepath: Assets/Scripts/Data/ScriptableObjects/EnemyDefinition.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loot drop entry for enemy rewards
/// </summary>
[System.Serializable]
public class LootDropEntry
{
    [Tooltip("Item to drop")]
    public ItemDefinition Item;

    [Tooltip("Minimum quantity to drop")]
    [Min(1)]
    public int MinQuantity = 1;

    [Tooltip("Maximum quantity to drop")]
    [Min(1)]
    public int MaxQuantity = 1;

    [Tooltip("Chance to drop (0-1)")]
    [Range(0f, 1f)]
    public float DropChance = 1f;

    /// <summary>
    /// Roll for loot and return quantity (0 if not dropped)
    /// </summary>
    public int RollDrop()
    {
        if (Random.value > DropChance) return 0;
        return Random.Range(MinQuantity, MaxQuantity + 1);
    }
}

/// <summary>
/// ScriptableObject defining an enemy that can be fought in combat
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "WalkAndRPG/Combat/Enemy Definition")]
public class EnemyDefinition : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Unique identifier for this enemy")]
    public string EnemyID;

    [Tooltip("Display name shown in UI")]
    public string EnemyName;

    [TextArea(2, 4)]
    [Tooltip("Description of this enemy")]
    public string Description;

    [Header("Visual")]
    [Tooltip("Sprite representing this enemy in combat")]
    public Sprite EnemySprite;

    [Tooltip("Color theme for this enemy")]
    public Color EnemyColor = Color.white;

    [Header("Combat Stats")]
    [Tooltip("Enemy level - affects difficulty display")]
    [Min(1)]
    public int Level = 1;

    [Tooltip("Maximum health points")]
    [Min(1)]
    public float MaxHealth = 100f;

    [Header("Abilities")]
    [Tooltip("Abilities this enemy uses in combat")]
    public List<AbilityDefinition> Abilities = new List<AbilityDefinition>();

    [Header("Rewards")]
    [Tooltip("Experience points awarded on defeat")]
    [Min(0)]
    public int ExperienceReward = 10;

    [Tooltip("Items that can drop on defeat")]
    public List<LootDropEntry> LootTable = new List<LootDropEntry>();

    [Header("Victory Messages")]
    [Tooltip("Title shown on victory screen")]
    public string VictoryTitle = "Victory!";

    [TextArea(1, 2)]
    [Tooltip("Description shown on victory screen")]
    public string VictoryDescription = "You defeated the enemy!";

    [Header("Debug")]
    [TextArea(1, 2)]
    public string DeveloperNotes;

    /// <summary>
    /// Get display name for UI
    /// </summary>
    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(EnemyName) ? EnemyID : EnemyName;
    }

    /// <summary>
    /// Generate loot drops based on loot table
    /// </summary>
    public Dictionary<ItemDefinition, int> GenerateLoot()
    {
        var loot = new Dictionary<ItemDefinition, int>();

        if (LootTable == null) return loot;

        foreach (var entry in LootTable)
        {
            if (entry == null || entry.Item == null) continue;

            int quantity = entry.RollDrop();
            if (quantity > 0)
            {
                if (loot.ContainsKey(entry.Item))
                    loot[entry.Item] += quantity;
                else
                    loot[entry.Item] = quantity;
            }
        }

        return loot;
    }

    /// <summary>
    /// Validate this enemy definition
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(EnemyID)) return false;
        if (string.IsNullOrEmpty(EnemyName)) return false;
        if (MaxHealth <= 0) return false;
        return true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-generate EnemyID from name if empty
        if (string.IsNullOrEmpty(EnemyID) && !string.IsNullOrEmpty(EnemyName))
        {
            EnemyID = EnemyName.ToLower().Replace(" ", "_").Replace("'", "");
        }
    }
#endif
}
