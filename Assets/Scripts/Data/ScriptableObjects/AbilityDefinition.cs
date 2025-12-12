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
    Shield,         // Add shield
    StatusEffect    // Apply a status effect
}

/// <summary>
/// A single effect that an ability applies when used.
///
/// HOW EFFECTS WORK:
/// - Damage: Deals instant damage to the enemy. Value = damage amount.
/// - Heal: Restores health instantly. Value = heal amount. Use TargetsSelf for self-heal.
/// - Shield: Adds temporary shield that absorbs damage. Value = shield amount. Use TargetsSelf for self-shield.
/// - StatusEffect: Applies a status effect (DoT, buff, debuff, stun, etc).
///   The StatusEffect field determines WHAT happens, StatusEffectStacks determines HOW MUCH.
/// </summary>
[Serializable]
public class AbilityEffect
{
    [Tooltip("Type of effect to apply:\n\n" +
             "- Damage: Deal instant damage (Value = damage amount)\n" +
             "- Heal: Restore health instantly (Value = heal amount)\n" +
             "- Shield: Add damage-absorbing shield (Value = shield amount)\n" +
             "- StatusEffect: Apply a status effect like poison, burn, stun, buff, etc.")]
    public AbilityEffectType Type;

    [Tooltip("DEPENDS ON TYPE:\n\n" +
             "- Damage: Amount of damage dealt instantly\n" +
             "- Heal: Amount of health restored instantly\n" +
             "- Shield: Amount of shield added\n" +
             "- StatusEffect: NOT USED (damage/healing is defined in the StatusEffect itself)")]
    [Min(0)]
    public float Value;

    [Tooltip("ONLY FOR StatusEffect TYPE:\n\n" +
             "The status effect to apply (e.g., Poison, Burn, Stun, Regen).\n\n" +
             "The StatusEffect defines:\n" +
             "- What type of effect (DoT, HoT, buff, debuff, stun)\n" +
             "- How much damage/healing per tick\n" +
             "- How long it lasts\n" +
             "- How it stacks")]
    public StatusEffectDefinition StatusEffect;

    [Tooltip("ONLY FOR StatusEffect TYPE:\n\n" +
             "Number of stacks to apply.\n\n" +
             "Example with Poison (5 dmg/tick per stack):\n" +
             "- 1 stack = 5 damage per tick\n" +
             "- 3 stacks = 15 damage per tick\n" +
             "- 5 stacks = 25 damage per tick\n\n" +
             "Stacks can be limited by the StatusEffect's MaxStacks setting.")]
    [Min(1)]
    public int StatusEffectStacks = 1;

    [Tooltip("WHO RECEIVES THIS EFFECT:\n\n" +
             "- FALSE (default): Effect targets the ENEMY\n" +
             "  Use for: Damage, offensive debuffs, DoTs\n\n" +
             "- TRUE: Effect targets YOURSELF\n" +
             "  Use for: Heals, shields, buffs, HoTs\n\n" +
             "Example: A 'Vampiric Strike' might have:\n" +
             "- Damage effect with TargetsSelf=false (hurts enemy)\n" +
             "- Heal effect with TargetsSelf=true (heals you)")]
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

    [Header("Effects")]
    [Tooltip("List of effects this ability applies when used.\n\n" +
             "An ability can have MULTIPLE effects!\n\n" +
             "EXAMPLES:\n" +
             "- Basic Attack: 1 Damage effect\n" +
             "- Poison Strike: 1 Damage + 1 StatusEffect (Poison)\n" +
             "- Shield Bash: 1 Shield (self) + 1 Damage\n" +
             "- Vampiric Strike: 1 Damage + 1 Heal (self)\n\n" +
             "Hover over each field in an effect for detailed help.")]
    public List<AbilityEffect> Effects = new List<AbilityEffect>();

    [Header("Combat Stats")]
    [Tooltip("Time in seconds between auto-triggers")]
    [Min(0.1f)]
    public float Cooldown = 2f;

    [Tooltip("Weight for equipment system (higher = takes more slots)")]
    [Min(1)]
    public int Weight = 1;

    [Header("Debug")]
    [TextArea(1, 2)]
    public string DeveloperNotes;

    // === PUBLIC METHODS ===

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
        if (Effects == null) return false;

        foreach (var effect in Effects)
        {
            if (effect != null && effect.Type == effectType)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Validate this ability definition
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(AbilityID)) return false;
        if (string.IsNullOrEmpty(AbilityName)) return false;
        if (Cooldown <= 0) return false;
        if (Effects == null || Effects.Count == 0) return false;

        return true;
    }

    /// <summary>
    /// Get a summary of this ability's effects for UI
    /// </summary>
    public string GetEffectsSummary()
    {
        var parts = new List<string>();

        if (Effects != null)
        {
            foreach (var effect in Effects)
            {
                if (effect != null)
                {
                    parts.Add(effect.GetSummary());
                }
            }
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
#endif
}
