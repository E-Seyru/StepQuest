// Purpose: Data structures for item stats and rarity-based stat variations
// Filepath: Assets/Scripts/Data/Models/ItemStats.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Categories of item stats for organization and filtering
/// </summary>
public enum ItemStatCategory
{
    Combat,      // Stats affecting combat (Force, Agility, Defense, etc.)
    Activity,    // Stats affecting activities (StepReduction, TimeReduction, YieldBonus)
    Travel,      // Stats affecting travel (TravelSpeed, StaminaReduction)
    Social,      // Stats affecting NPC interactions (Charisma, RelationshipBonus)
    Consumable   // Stats for consumable effects (HealAmount, BuffDuration)
}

/// <summary>
/// Types of stats that items can have
/// </summary>
public enum ItemStatType
{
    // === Combat Stats ===
    Force,              // Physical attack power
    Agility,            // Speed and evasion
    Defense,            // Damage reduction
    MaxHealth,          // Bonus HP
    CriticalChance,     // % chance to crit
    CriticalDamage,     // % bonus damage on crit

    // === Activity Stats ===
    MiningStepReduction,      // Reduces steps needed for mining
    WoodcuttingStepReduction, // Reduces steps needed for woodcutting
    FishingStepReduction,     // Reduces steps needed for fishing
    GatheringStepReduction,   // Reduces steps needed for general gathering
    CraftingTimeReduction,    // Reduces time for crafting (%)
    YieldBonus,               // % bonus resources from activities

    // === Travel Stats ===
    TravelSpeedBonus,   // % faster travel
    StaminaReduction,   // Reduces step cost for travel

    // === Social Stats ===
    Charisma,           // Affects NPC interactions
    RelationshipBonus,  // % bonus relationship points

    // === Consumable Stats ===
    HealAmount,         // HP restored
    BuffDuration,       // Duration of buffs in seconds
    BuffStrength,       // Strength multiplier for buffs

    // === Special ===
    ExperienceBonus,    // % bonus XP from all sources
    LuckBonus           // Affects rare drops and crafting rarity rolls
}

/// <summary>
/// A single stat with its value
/// </summary>
[Serializable]
public class ItemStat
{
    [Tooltip("The type of stat")]
    public ItemStatType StatType;

    [Tooltip("The value of this stat")]
    public float Value;

    [Tooltip("Is this stat displayed as a percentage?")]
    public bool IsPercentage;

    public ItemStat()
    {
        StatType = ItemStatType.Force;
        Value = 0f;
        IsPercentage = false;
    }

    public ItemStat(ItemStatType type, float value, bool isPercentage = false)
    {
        StatType = type;
        Value = value;
        IsPercentage = isPercentage;
    }

    /// <summary>
    /// Get the display string for this stat (e.g., "Force: 10" or "Crit: 15%")
    /// </summary>
    public string GetDisplayString()
    {
        string statName = GetStatDisplayName(StatType);
        string valueStr = IsPercentage ? $"{Value:F0}%" : $"{Value:F0}";
        return $"{statName}: {valueStr}";
    }

    /// <summary>
    /// Get a human-readable name for a stat type
    /// </summary>
    public static string GetStatDisplayName(ItemStatType statType)
    {
        return statType switch
        {
            // Combat
            ItemStatType.Force => "Force",
            ItemStatType.Agility => "Agilite",
            ItemStatType.Defense => "Defense",
            ItemStatType.MaxHealth => "Vie Max",
            ItemStatType.CriticalChance => "Chance Critique",
            ItemStatType.CriticalDamage => "Degats Critiques",

            // Activity
            ItemStatType.MiningStepReduction => "Pas (Minage)",
            ItemStatType.WoodcuttingStepReduction => "Pas (Bucheron)",
            ItemStatType.FishingStepReduction => "Pas (Peche)",
            ItemStatType.GatheringStepReduction => "Pas (Cueillette)",
            ItemStatType.CraftingTimeReduction => "Temps Fabrication",
            ItemStatType.YieldBonus => "Bonus Rendement",

            // Travel
            ItemStatType.TravelSpeedBonus => "Vitesse Voyage",
            ItemStatType.StaminaReduction => "Endurance",

            // Social
            ItemStatType.Charisma => "Charisme",
            ItemStatType.RelationshipBonus => "Bonus Relation",

            // Consumable
            ItemStatType.HealAmount => "Soin",
            ItemStatType.BuffDuration => "Duree Effet",
            ItemStatType.BuffStrength => "Puissance Effet",

            // Special
            ItemStatType.ExperienceBonus => "Bonus XP",
            ItemStatType.LuckBonus => "Chance",

            _ => statType.ToString()
        };
    }

    /// <summary>
    /// Get the category for a stat type
    /// </summary>
    public static ItemStatCategory GetStatCategory(ItemStatType statType)
    {
        return statType switch
        {
            ItemStatType.Force or
            ItemStatType.Agility or
            ItemStatType.Defense or
            ItemStatType.MaxHealth or
            ItemStatType.CriticalChance or
            ItemStatType.CriticalDamage => ItemStatCategory.Combat,

            ItemStatType.MiningStepReduction or
            ItemStatType.WoodcuttingStepReduction or
            ItemStatType.FishingStepReduction or
            ItemStatType.GatheringStepReduction or
            ItemStatType.CraftingTimeReduction or
            ItemStatType.YieldBonus => ItemStatCategory.Activity,

            ItemStatType.TravelSpeedBonus or
            ItemStatType.StaminaReduction => ItemStatCategory.Travel,

            ItemStatType.Charisma or
            ItemStatType.RelationshipBonus => ItemStatCategory.Social,

            ItemStatType.HealAmount or
            ItemStatType.BuffDuration or
            ItemStatType.BuffStrength => ItemStatCategory.Consumable,

            _ => ItemStatCategory.Combat
        };
    }
}

/// <summary>
/// Stats for a specific rarity tier of an item
/// </summary>
[Serializable]
public class ItemRarityStats
{
    [Tooltip("The rarity tier (1=Common, 2=Uncommon, 3=Rare, 4=Epic, 5=Legendary)")]
    [Range(1, 5)]
    public int RarityTier = 1;

    [Tooltip("Stats at this rarity level")]
    public List<ItemStat> Stats = new List<ItemStat>();

    [Tooltip("Ability unlocked at this rarity (optional)")]
    public AbilityDefinition UnlockedAbility;

    public ItemRarityStats()
    {
        RarityTier = 1;
        Stats = new List<ItemStat>();
        UnlockedAbility = null;
    }

    public ItemRarityStats(int rarityTier)
    {
        RarityTier = rarityTier;
        Stats = new List<ItemStat>();
        UnlockedAbility = null;
    }

    /// <summary>
    /// Get the display name for this rarity tier
    /// </summary>
    public string GetRarityDisplayName()
    {
        return RarityTier switch
        {
            1 => "Commun",
            2 => "Peu commun",
            3 => "Rare",
            4 => "Epique",
            5 => "Legendaire",
            _ => "Inconnu"
        };
    }

    /// <summary>
    /// Get the color for this rarity tier
    /// </summary>
    public Color GetRarityColor()
    {
        return RarityTier switch
        {
            1 => Color.gray,
            2 => Color.green,
            3 => Color.blue,
            4 => new Color(0.6f, 0.0f, 1.0f), // Purple
            5 => new Color(1.0f, 0.6f, 0.0f), // Orange
            _ => Color.white
        };
    }

    /// <summary>
    /// Get a specific stat value, or default if not found
    /// </summary>
    public float GetStatValue(ItemStatType statType, float defaultValue = 0f)
    {
        foreach (var stat in Stats)
        {
            if (stat.StatType == statType)
                return stat.Value;
        }
        return defaultValue;
    }

    /// <summary>
    /// Check if this rarity has a specific stat
    /// </summary>
    public bool HasStat(ItemStatType statType)
    {
        foreach (var stat in Stats)
        {
            if (stat.StatType == statType)
                return true;
        }
        return false;
    }
}
