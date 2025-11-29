// Purpose: ScriptableObject defining a status effect that can be applied in combat
// Filepath: Assets/Scripts/Data/ScriptableObjects/StatusEffectDefinition.cs

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Types of status effects that can be applied in combat
/// </summary>
public enum StatusEffectType
{
    // Damage over time
    Poison,
    Burn,
    Bleed,

    // Control
    Stun,

    // Healing
    Regeneration,

    // Defensive
    Shield,

    // Stat modifiers - Buffs
    AttackBuff,
    DefenseBuff,
    SpeedBuff,

    // Stat modifiers - Debuffs
    AttackDebuff,
    DefenseDebuff,
    SpeedDebuff
}

/// <summary>
/// How status effect stacks behave when reapplied
/// </summary>
public enum StackingBehavior
{
    /// <summary>Stacks add up (e.g., 3 + 2 = 5 stacks)</summary>
    Additive,

    /// <summary>Only refreshes duration, no stack increase</summary>
    RefreshDuration,

    /// <summary>Stacks add up to a maximum, then refreshes duration</summary>
    MaxStacks,

    /// <summary>New application replaces old one entirely</summary>
    Replace
}

/// <summary>
/// What happens when the effect ticks
/// </summary>
public enum EffectBehavior
{
    /// <summary>Deals damage each tick (Poison, Burn, Bleed)</summary>
    DamageOverTime,

    /// <summary>Heals each tick (Regeneration)</summary>
    HealOverTime,

    /// <summary>Modifies a stat for duration (Buffs/Debuffs)</summary>
    StatModifier,

    /// <summary>Prevents actions - pauses ability cooldowns (Stun)</summary>
    ControlEffect
}

/// <summary>
/// ScriptableObject defining a status effect that can be applied in combat.
/// Each effect defines its own stacking behavior, duration, and tick mechanics.
/// </summary>
[CreateAssetMenu(fileName = "NewStatusEffect", menuName = "WalkAndRPG/Combat/Status Effect Definition")]
public class StatusEffectDefinition : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Unique identifier for this status effect")]
    public string EffectID;

    [Tooltip("Display name shown in UI")]
    public string EffectName;

    [TextArea(2, 4)]
    [Tooltip("Description of what this effect does")]
    public string Description;

    [Header("Visual")]
    [Tooltip("Icon for this status effect")]
    public Sprite EffectIcon;

    [Tooltip("Color associated with this effect (for UI and popups)")]
    public Color EffectColor = Color.white;

    [Header("Effect Configuration")]
    [Tooltip("Category of this status effect")]
    public StatusEffectType EffectType;

    [Tooltip("What happens when this effect is active")]
    public EffectBehavior Behavior;

    [Tooltip("How this effect stacks when reapplied")]
    public StackingBehavior Stacking = StackingBehavior.Additive;

    [Header("Duration & Ticking")]
    [Tooltip("Duration in seconds. 0 = permanent until removed or combat ends")]
    [Min(0)]
    public float Duration = 5f;

    [Tooltip("How often the effect ticks (for DoT/HoT). 0 = no ticking (instant or stat modifier)")]
    [Min(0)]
    public float TickInterval = 1f;

    [Header("Effect Values")]
    [Tooltip("Base value per tick (damage/heal) or modifier percentage (e.g., 0.25 = 25% buff)")]
    public float BaseValue = 1f;

    [Tooltip("If true, tick value = BaseValue * CurrentStacks. If false, value is flat BaseValue.")]
    public bool ScalesWithStacks = true;

    [Header("Stacking Rules")]
    [Tooltip("Maximum number of stacks this effect can have")]
    [Min(1)]
    public int MaxStacks = 99;

    [Header("Control Flags")]
    [Tooltip("If true, this effect pauses ability cooldowns (for Stun)")]
    public bool PreventsActions = false;

    [Tooltip("If true, this effect is removed when the target takes damage")]
    public bool RemovedOnDamage = false;

    [Header("Debug")]
    [TextArea(1, 2)]
    public string DeveloperNotes;

    // === PUBLIC METHODS ===

    /// <summary>
    /// Get display name for UI
    /// </summary>
    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(EffectName) ? EffectID : EffectName;
    }

    /// <summary>
    /// Calculate the tick value based on current stacks
    /// </summary>
    public float CalculateTickValue(int stacks)
    {
        if (stacks <= 0) return 0;
        return ScalesWithStacks ? BaseValue * stacks : BaseValue;
    }

    /// <summary>
    /// Check if this effect deals damage over time
    /// </summary>
    public bool IsDamageOverTime => Behavior == EffectBehavior.DamageOverTime;

    /// <summary>
    /// Check if this effect heals over time
    /// </summary>
    public bool IsHealOverTime => Behavior == EffectBehavior.HealOverTime;

    /// <summary>
    /// Check if this effect modifies stats
    /// </summary>
    public bool IsStatModifier => Behavior == EffectBehavior.StatModifier;

    /// <summary>
    /// Check if this effect is a control effect (stun, etc.)
    /// </summary>
    public bool IsControlEffect => Behavior == EffectBehavior.ControlEffect;

    /// <summary>
    /// Check if this is a buff (positive stat modifier)
    /// </summary>
    public bool IsBuff => EffectType == StatusEffectType.AttackBuff ||
                          EffectType == StatusEffectType.DefenseBuff ||
                          EffectType == StatusEffectType.SpeedBuff ||
                          EffectType == StatusEffectType.Regeneration;

    /// <summary>
    /// Check if this is a debuff (negative effect)
    /// </summary>
    public bool IsDebuff => EffectType == StatusEffectType.AttackDebuff ||
                            EffectType == StatusEffectType.DefenseDebuff ||
                            EffectType == StatusEffectType.SpeedDebuff ||
                            EffectType == StatusEffectType.Poison ||
                            EffectType == StatusEffectType.Burn ||
                            EffectType == StatusEffectType.Bleed ||
                            EffectType == StatusEffectType.Stun;

    /// <summary>
    /// Validate this status effect definition
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(EffectID)) return false;
        if (string.IsNullOrEmpty(EffectName)) return false;
        if (MaxStacks < 1) return false;

        // DoT/HoT should have tick interval
        if ((Behavior == EffectBehavior.DamageOverTime || Behavior == EffectBehavior.HealOverTime)
            && TickInterval <= 0) return false;

        return true;
    }

    /// <summary>
    /// Get a summary of this effect for UI
    /// </summary>
    public string GetEffectSummary()
    {
        string summary = "";

        switch (Behavior)
        {
            case EffectBehavior.DamageOverTime:
                summary = ScalesWithStacks
                    ? $"{BaseValue}/stack damage every {TickInterval}s"
                    : $"{BaseValue} damage every {TickInterval}s";
                break;

            case EffectBehavior.HealOverTime:
                summary = ScalesWithStacks
                    ? $"{BaseValue}/stack healing every {TickInterval}s"
                    : $"{BaseValue} healing every {TickInterval}s";
                break;

            case EffectBehavior.StatModifier:
                string percent = (BaseValue * 100).ToString("F0");
                summary = BaseValue > 0 ? $"+{percent}%" : $"{percent}%";
                break;

            case EffectBehavior.ControlEffect:
                summary = PreventsActions ? "Pauses abilities" : "Control effect";
                break;
        }

        if (Duration > 0)
            summary += $" for {Duration}s";

        return summary;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-generate EffectID from name if empty
        if (string.IsNullOrEmpty(EffectID) && !string.IsNullOrEmpty(EffectName))
        {
            EffectID = EffectName.ToLower().Replace(" ", "_").Replace("'", "");
        }

        // Ensure control effects have PreventsActions set
        if (EffectType == StatusEffectType.Stun && !PreventsActions)
        {
            PreventsActions = true;
        }
    }
#endif
}
