// Purpose: ScriptableObject defining a combat ability
// Filepath: Assets/Scripts/Data/ScriptableObjects/AbilityDefinition.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Types of effects an ability can have
/// </summary>
public enum AbilityEffectType
{
    Damage,         // Deal damage to target
    Heal,           // Restore health
    Poison,         // Apply poison (legacy - use StatusEffect instead)
    Shield,         // Add shield
    StatusEffect    // Apply a generic status effect (new system)
}

/// <summary>
/// A single effect that an ability applies when used.
/// Part of the new generic effects system.
/// </summary>
[Serializable]
public class AbilityEffect
{
    [Tooltip("Type of effect")]
    public AbilityEffectType Type;

    [Tooltip("Value for this effect (damage, heal, or shield amount)")]
    [Min(0)]
    public float Value;

    [Tooltip("Status effect to apply (only for StatusEffect type)")]
    public StatusEffectDefinition StatusEffect;

    [Tooltip("Number of stacks to apply (only for StatusEffect type)")]
    [Min(1)]
    public int StatusEffectStacks = 1;

    [Tooltip("If true, effect targets self instead of enemy (for heals/shields/buffs)")]
    public bool TargetsSelf;

    /// <summary>
    /// Get a summary of this effect
    /// </summary>
    public string GetSummary()
    {
        switch (Type)
        {
            case AbilityEffectType.Damage:
                return $"{Value} damage";
            case AbilityEffectType.Heal:
                return $"{Value} heal" + (TargetsSelf ? " (self)" : "");
            case AbilityEffectType.Shield:
                return $"{Value} shield" + (TargetsSelf ? " (self)" : "");
            case AbilityEffectType.StatusEffect:
                if (StatusEffect != null)
                {
                    string target = TargetsSelf ? " (self)" : "";
                    return $"{StatusEffect.GetDisplayName()} x{StatusEffectStacks}{target}";
                }
                return "No effect";
            case AbilityEffectType.Poison:
                return $"{Value} poison (legacy)";
            default:
                return "Unknown effect";
        }
    }
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

    [Header("Effects (New System)")]
    [Tooltip("List of effects this ability applies. Use this for new abilities.")]
    public List<AbilityEffect> Effects = new List<AbilityEffect>();

    [Header("Combat Stats")]
    [Tooltip("Time in seconds between auto-triggers")]
    [Min(0.1f)]
    public float Cooldown = 2f;

    [Tooltip("Weight for equipment system (higher = takes more slots)")]
    [Min(1)]
    public int Weight = 1;

    // === LEGACY FIELDS (for backwards compatibility) ===
    // These will be used if Effects list is empty

    [Header("Legacy Effect Types (use Effects list instead)")]
    [Tooltip("Effect types this ability has (LEGACY - use Effects list instead)")]
    public List<AbilityEffectType> EffectTypes = new List<AbilityEffectType>();

    [Header("Legacy Effect Values (use Effects list instead)")]
    [Tooltip("Damage dealt (LEGACY)")]
    [Min(0)]
    public float DamageAmount = 0f;

    [Tooltip("Healing amount (LEGACY)")]
    [Min(0)]
    public float HealAmount = 0f;

    [Tooltip("Poison stacks to apply (LEGACY)")]
    [Min(0)]
    public float PoisonAmount = 0f;

    [Tooltip("Shield amount added (LEGACY)")]
    [Min(0)]
    public float ShieldAmount = 0f;

    [Header("Debug")]
    [TextArea(1, 2)]
    public string DeveloperNotes;

    // === PUBLIC PROPERTIES ===

    /// <summary>
    /// Check if this ability uses the new Effects system
    /// </summary>
    public bool UsesNewEffectSystem => Effects != null && Effects.Count > 0;

    // === PUBLIC METHODS ===

    /// <summary>
    /// Get display name for UI
    /// </summary>
    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(AbilityName) ? AbilityID : AbilityName;
    }

    /// <summary>
    /// Check if ability has a specific effect type.
    /// Works with both new Effects list and legacy EffectTypes.
    /// </summary>
    public bool HasEffect(AbilityEffectType effectType)
    {
        // Check new system first
        if (Effects != null && Effects.Count > 0)
        {
            foreach (var effect in Effects)
            {
                if (effect != null && effect.Type == effectType)
                    return true;
            }
            return false;
        }

        // Fall back to legacy system
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

        // Valid if has new effects OR legacy effects
        bool hasNewEffects = Effects != null && Effects.Count > 0;
        bool hasLegacyEffects = EffectTypes != null && EffectTypes.Count > 0;

        return hasNewEffects || hasLegacyEffects;
    }

    /// <summary>
    /// Get a summary of this ability's effects for UI.
    /// Works with both new Effects list and legacy system.
    /// </summary>
    public string GetEffectsSummary()
    {
        var parts = new List<string>();

        // Use new system if available
        if (Effects != null && Effects.Count > 0)
        {
            foreach (var effect in Effects)
            {
                if (effect != null)
                {
                    parts.Add(effect.GetSummary());
                }
            }
        }
        else
        {
            // Fall back to legacy system
            if (HasEffect(AbilityEffectType.Damage) && DamageAmount > 0)
                parts.Add($"{DamageAmount} damage");
            if (HasEffect(AbilityEffectType.Heal) && HealAmount > 0)
                parts.Add($"{HealAmount} heal");
            if (HasEffect(AbilityEffectType.Poison) && PoisonAmount > 0)
                parts.Add($"{PoisonAmount} poison");
            if (HasEffect(AbilityEffectType.Shield) && ShieldAmount > 0)
                parts.Add($"{ShieldAmount} shield");
        }

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

    /// <summary>
    /// Migrate legacy effect data to new Effects list.
    /// Call this from inspector to convert old abilities.
    /// </summary>
    [ContextMenu("Migrate Legacy Data to Effects")]
    public void MigrateLegacyData()
    {
        if (Effects == null)
            Effects = new List<AbilityEffect>();

        // Only migrate if Effects is empty and we have legacy data
        if (Effects.Count > 0)
        {
            Debug.LogWarning($"AbilityDefinition '{AbilityName}': Effects list is not empty. Clear it first if you want to migrate.");
            return;
        }

        if (EffectTypes == null || EffectTypes.Count == 0)
        {
            Debug.LogWarning($"AbilityDefinition '{AbilityName}': No legacy data to migrate.");
            return;
        }

        // Migrate each legacy effect type
        if (EffectTypes.Contains(AbilityEffectType.Damage) && DamageAmount > 0)
        {
            Effects.Add(new AbilityEffect
            {
                Type = AbilityEffectType.Damage,
                Value = DamageAmount,
                TargetsSelf = false
            });
        }

        if (EffectTypes.Contains(AbilityEffectType.Heal) && HealAmount > 0)
        {
            Effects.Add(new AbilityEffect
            {
                Type = AbilityEffectType.Heal,
                Value = HealAmount,
                TargetsSelf = true // Heals always target self in legacy
            });
        }

        if (EffectTypes.Contains(AbilityEffectType.Shield) && ShieldAmount > 0)
        {
            Effects.Add(new AbilityEffect
            {
                Type = AbilityEffectType.Shield,
                Value = ShieldAmount,
                TargetsSelf = true // Shields always target self in legacy
            });
        }

        if (EffectTypes.Contains(AbilityEffectType.Poison) && PoisonAmount > 0)
        {
            // Note: For poison, you'll need to manually assign a StatusEffectDefinition
            // after running this migration
            Effects.Add(new AbilityEffect
            {
                Type = AbilityEffectType.StatusEffect,
                Value = 0, // Not used for status effects
                StatusEffectStacks = Mathf.RoundToInt(PoisonAmount),
                TargetsSelf = false
            });
            Debug.LogWarning($"AbilityDefinition '{AbilityName}': Poison migrated - please assign StatusEffect reference manually.");
        }

        Debug.Log($"AbilityDefinition '{AbilityName}': Migrated {Effects.Count} effects from legacy data.");

        // Mark dirty for saving
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
