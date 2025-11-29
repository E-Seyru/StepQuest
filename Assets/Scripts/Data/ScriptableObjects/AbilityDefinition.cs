// Purpose: ScriptableObject defining a combat ability
// Filepath: Assets/Scripts/Data/ScriptableObjects/AbilityDefinition.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Types of effects an ability can have (can be combined)
/// </summary>
public enum AbilityEffectType
{
    Damage,     // Deal damage to target
    Heal,       // Restore health to self
    Poison,     // Apply poison damage over time
    Shield      // Add shield to self
}

/// <summary>
/// ScriptableObject defining a combat ability with cooldown-based auto-trigger
/// </summary>
[CreateAssetMenu(fileName = "NewAbility", menuName = "WalkAndRPG/Combat/Ability Definition")]
public class AbilityDefinition : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Unique identifier for this ability")]
    public string AbilityID;

    [Tooltip("Display name shown in UI")]
    public string AbilityName;

    [TextArea(2, 4)]
    [Tooltip("Description of what this ability does")]
    public string Description;

    [Header("Visual")]
    [Tooltip("Icon for this ability")]
    public Sprite AbilityIcon;

    [Tooltip("Color associated with this ability")]
    public Color AbilityColor = Color.white;

    [Header("Ability Type")]
    [Tooltip("Effect types this ability has (can have multiple)")]
    public List<AbilityEffectType> EffectTypes = new List<AbilityEffectType>();

    [Header("Combat Stats")]
    [Tooltip("Time in seconds between auto-triggers")]
    [Min(0.1f)]
    public float Cooldown = 2f;

    [Tooltip("Weight for equipment system (higher = takes more slots)")]
    [Min(1)]
    public int Weight = 1;

    [Header("Effect Values")]
    [Tooltip("Damage dealt (if Damage type)")]
    [Min(0)]
    public float DamageAmount = 0f;

    [Tooltip("Healing amount (if Heal type)")]
    [Min(0)]
    public float HealAmount = 0f;

    [Tooltip("Poison damage per tick (if Poison type)")]
    [Min(0)]
    public float PoisonAmount = 0f;

    [Tooltip("Shield amount added (if Shield type)")]
    [Min(0)]
    public float ShieldAmount = 0f;

    [Header("Debug")]
    [TextArea(1, 2)]
    public string DeveloperNotes;

    /// <summary>
    /// Get display name for UI
    /// </summary>
    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(AbilityName) ? AbilityID : AbilityName;
    }

    /// <summary>
    /// Check if ability has a specific effect type
    /// </summary>
    public bool HasEffect(AbilityEffectType effectType)
    {
        return EffectTypes != null && EffectTypes.Contains(effectType);
    }

    /// <summary>
    /// Validate this ability definition
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(AbilityID)) return false;
        if (string.IsNullOrEmpty(AbilityName)) return false;
        if (Cooldown <= 0) return false;
        if (EffectTypes == null || EffectTypes.Count == 0) return false;
        return true;
    }

    /// <summary>
    /// Get a summary of this ability's effects for UI
    /// </summary>
    public string GetEffectsSummary()
    {
        var parts = new List<string>();

        if (HasEffect(AbilityEffectType.Damage) && DamageAmount > 0)
            parts.Add($"{DamageAmount} damage");
        if (HasEffect(AbilityEffectType.Heal) && HealAmount > 0)
            parts.Add($"{HealAmount} heal");
        if (HasEffect(AbilityEffectType.Poison) && PoisonAmount > 0)
            parts.Add($"{PoisonAmount} poison/tick");
        if (HasEffect(AbilityEffectType.Shield) && ShieldAmount > 0)
            parts.Add($"{ShieldAmount} shield");

        return parts.Count > 0 ? string.Join(", ", parts) : "No effects";
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-generate AbilityID from name if empty
        if (string.IsNullOrEmpty(AbilityID) && !string.IsNullOrEmpty(AbilityName))
        {
            AbilityID = AbilityName.ToLower().Replace(" ", "_").Replace("'", "");
        }
    }
#endif
}
