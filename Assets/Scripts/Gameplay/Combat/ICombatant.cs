// Purpose: Interface abstraction for any entity that can participate in combat
// Filepath: Assets/Scripts/Gameplay/Combat/ICombatant.cs

using System;
using System.Collections.Generic;

/// <summary>
/// Combat stats that can be modified by buffs/debuffs.
/// All values are multipliers (1.0 = 100% = normal).
/// </summary>
[Serializable]
public struct CombatantStats
{
    /// <summary>Damage dealt multiplier (1.0 = normal, 1.25 = +25%)</summary>
    public float AttackMultiplier;

    /// <summary>Damage taken multiplier (1.0 = normal, 0.75 = -25% damage taken)</summary>
    public float DefenseMultiplier;

    /// <summary>Cooldown speed multiplier (1.0 = normal, 1.5 = 50% faster cooldowns)</summary>
    public float SpeedMultiplier;

    /// <summary>
    /// Default stats (all at 1.0 = 100%)
    /// </summary>
    public static CombatantStats Default => new CombatantStats
    {
        AttackMultiplier = 1f,
        DefenseMultiplier = 1f,
        SpeedMultiplier = 1f
    };

    /// <summary>
    /// Apply another stat modifier on top of this one
    /// </summary>
    public CombatantStats Apply(CombatantStats modifier)
    {
        return new CombatantStats
        {
            AttackMultiplier = this.AttackMultiplier * modifier.AttackMultiplier,
            DefenseMultiplier = this.DefenseMultiplier * modifier.DefenseMultiplier,
            SpeedMultiplier = this.SpeedMultiplier * modifier.SpeedMultiplier
        };
    }

    public override string ToString()
    {
        return $"[ATK:{AttackMultiplier:P0} DEF:{DefenseMultiplier:P0} SPD:{SpeedMultiplier:P0}]";
    }
}

/// <summary>
/// Interface for any entity that can participate in combat.
/// Enables player, enemies, and future summons/allies to share combat logic.
/// Designed to support future multi-enemy combat scenarios.
/// </summary>
public interface ICombatant
{
    // === IDENTITY ===

    /// <summary>Unique identifier for this combatant instance</summary>
    string CombatantId { get; }

    /// <summary>Display name shown in UI</summary>
    string DisplayName { get; }

    /// <summary>True if this is the player, false for enemies</summary>
    bool IsPlayer { get; }

    // === HEALTH ===

    /// <summary>Current health points</summary>
    float CurrentHealth { get; }

    /// <summary>Maximum health points</summary>
    float MaxHealth { get; }

    /// <summary>Current health as percentage (0-1)</summary>
    float HealthPercentage { get; }

    /// <summary>True if health > 0</summary>
    bool IsAlive { get; }

    // === COMBAT ACTIONS ===

    /// <summary>
    /// Apply damage to this combatant.
    /// Respects shield and defense modifiers.
    /// </summary>
    /// <param name="amount">Raw damage amount before modifiers</param>
    /// <param name="ignoreShield">If true, bypasses shield</param>
    /// <returns>Actual damage dealt after modifiers and shield</returns>
    float TakeDamage(float amount, bool ignoreShield = false);

    /// <summary>
    /// Heal this combatant.
    /// Cannot exceed max health.
    /// </summary>
    /// <param name="amount">Amount to heal</param>
    /// <returns>Actual amount healed</returns>
    float Heal(float amount);

    // === SHIELD ===

    /// <summary>Current shield amount</summary>
    float CurrentShield { get; }

    /// <summary>Add shield to this combatant</summary>
    void AddShield(float amount);

    /// <summary>
    /// Absorb damage with shield.
    /// </summary>
    /// <param name="damage">Incoming damage</param>
    /// <returns>Damage remaining after shield absorption</returns>
    float AbsorbDamageWithShield(float damage);

    // === STATUS EFFECTS ===

    /// <summary>List of active status effects on this combatant</summary>
    List<ActiveStatusEffect> ActiveEffects { get; }

    /// <summary>
    /// Apply a status effect to this combatant.
    /// Handles stacking based on effect's stacking behavior.
    /// </summary>
    void ApplyStatusEffect(StatusEffectDefinition effect, int stacks = 1);

    /// <summary>Remove a specific status effect by ID</summary>
    void RemoveStatusEffect(string effectId);

    /// <summary>Remove all status effects</summary>
    void ClearAllStatusEffects();

    /// <summary>Check if combatant has a specific effect by ID</summary>
    bool HasStatusEffect(string effectId);

    /// <summary>Check if combatant has any effect of a specific type</summary>
    bool HasStatusEffect(StatusEffectType type);

    /// <summary>Get active effect by ID (null if not present)</summary>
    ActiveStatusEffect GetStatusEffect(string effectId);

    /// <summary>True if any active effect has PreventsActions (is stunned)</summary>
    bool IsStunned { get; }

    // === ABILITIES ===

    /// <summary>List of abilities this combatant can use</summary>
    List<AbilityDefinition> Abilities { get; }

    // === STATS ===

    /// <summary>Get base combat stats (before buffs/debuffs)</summary>
    CombatantStats GetBaseStats();

    /// <summary>Get modified stats (after applying all active buff/debuff effects)</summary>
    CombatantStats GetModifiedStats();
}
