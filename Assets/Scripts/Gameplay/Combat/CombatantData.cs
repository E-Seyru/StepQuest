// Purpose: Serializable data structure for combatant state in combat
// Filepath: Assets/Scripts/Gameplay/Combat/CombatantData.cs

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable data structure representing a combatant's state during combat.
/// Used for persistence across app sessions and for the Combatant runtime wrapper.
/// </summary>
[Serializable]
public class CombatantData
{
    // === IDENTITY ===

    /// <summary>Unique identifier (player ID or enemy definition ID)</summary>
    public string CombatantId;

    /// <summary>Display name shown in UI</summary>
    public string DisplayName;

    /// <summary>True if this is the player</summary>
    public bool IsPlayer;

    // === HEALTH STATE ===

    [SerializeField] private float _currentHealth;
    [SerializeField] private float _maxHealth;
    [SerializeField] private float _currentShield;

    /// <summary>Current health points (clamped to 0-MaxHealth)</summary>
    public float CurrentHealth
    {
        get => _currentHealth;
        set => _currentHealth = Mathf.Clamp(value, 0, _maxHealth);
    }

    /// <summary>Maximum health points</summary>
    public float MaxHealth
    {
        get => _maxHealth;
        set
        {
            _maxHealth = Mathf.Max(1, value);
            // Clamp current health if max changed
            _currentHealth = Mathf.Min(_currentHealth, _maxHealth);
        }
    }

    /// <summary>Current shield amount (non-negative)</summary>
    public float CurrentShield
    {
        get => _currentShield;
        set => _currentShield = Mathf.Max(0, value);
    }

    // === STATUS EFFECTS ===

    /// <summary>List of active status effects</summary>
    public List<ActiveStatusEffect> ActiveEffects = new List<ActiveStatusEffect>();

    // === BASE STATS ===

    /// <summary>Base combat stats before modifiers</summary>
    public CombatantStats BaseStats = CombatantStats.Default;

    // === ABILITY REFERENCES ===

    /// <summary>IDs of abilities this combatant has (resolved at runtime)</summary>
    public List<string> AbilityIds = new List<string>();

    // === CONSTRUCTORS ===

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    public CombatantData()
    {
        CombatantId = string.Empty;
        DisplayName = string.Empty;
        IsPlayer = false;
        _currentHealth = 0;
        _maxHealth = 100;
        _currentShield = 0;
        ActiveEffects = new List<ActiveStatusEffect>();
        BaseStats = CombatantStats.Default;
        AbilityIds = new List<string>();
    }

    /// <summary>
    /// Create combatant data for the player
    /// </summary>
    public static CombatantData CreatePlayer(float maxHealth, List<AbilityDefinition> abilities = null)
    {
        var data = new CombatantData
        {
            CombatantId = "player",
            DisplayName = "Player",
            IsPlayer = true,
            _maxHealth = maxHealth,
            _currentHealth = maxHealth,
            _currentShield = 0,
            BaseStats = CombatantStats.Default
        };

        if (abilities != null)
        {
            foreach (var ability in abilities)
            {
                if (ability != null && !string.IsNullOrEmpty(ability.AbilityID))
                {
                    data.AbilityIds.Add(ability.AbilityID);
                }
            }
        }

        return data;
    }

    /// <summary>
    /// Create combatant data from an enemy definition
    /// </summary>
    public static CombatantData CreateFromEnemy(EnemyDefinition enemy)
    {
        if (enemy == null)
        {
            return new CombatantData();
        }

        var data = new CombatantData
        {
            CombatantId = enemy.EnemyID,
            DisplayName = enemy.GetDisplayName(),
            IsPlayer = false,
            _maxHealth = enemy.MaxHealth,
            _currentHealth = enemy.MaxHealth,
            _currentShield = 0,
            BaseStats = CombatantStats.Default
        };

        if (enemy.Abilities != null)
        {
            foreach (var ability in enemy.Abilities)
            {
                if (ability != null && !string.IsNullOrEmpty(ability.AbilityID))
                {
                    data.AbilityIds.Add(ability.AbilityID);
                }
            }
        }

        return data;
    }

    // === COMPUTED PROPERTIES ===

    /// <summary>Current health as percentage (0-1)</summary>
    public float HealthPercentage => _maxHealth > 0 ? _currentHealth / _maxHealth : 0;

    /// <summary>True if current health > 0</summary>
    public bool IsAlive => _currentHealth > 0;

    /// <summary>True if any active effect prevents actions (stun)</summary>
    public bool IsStunned
    {
        get
        {
            foreach (var effect in ActiveEffects)
            {
                if (effect != null && effect.IsActive && effect.PreventsActions)
                {
                    return true;
                }
            }
            return false;
        }
    }

    // === COMBAT METHODS ===

    /// <summary>
    /// Apply damage to this combatant.
    /// Shield absorbs damage first, then applies to health.
    /// Also processes on-hit decay for status effects.
    /// </summary>
    /// <param name="rawDamage">Damage before defense modifier</param>
    /// <param name="ignoreShield">If true, bypasses shield</param>
    /// <param name="defenseMultiplier">Defense stat multiplier</param>
    /// <returns>Actual damage dealt to health</returns>
    public float ApplyDamage(float rawDamage, bool ignoreShield = false, float defenseMultiplier = 1f)
    {
        if (rawDamage <= 0) return 0;

        // Apply defense modifier (lower = less damage taken)
        float damage = rawDamage * defenseMultiplier;

        // Shield absorption (unless ignored)
        if (!ignoreShield && _currentShield > 0)
        {
            if (_currentShield >= damage)
            {
                _currentShield -= damage;
                return 0; // All absorbed by shield
            }
            else
            {
                damage -= _currentShield;
                _currentShield = 0;
            }
        }

        // Apply to health
        float previousHealth = _currentHealth;
        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        float actualDamage = previousHealth - _currentHealth;

        // Process on-hit decay for all status effects
        if (actualDamage > 0)
        {
            ProcessOnHitDecay();
        }

        return actualDamage;
    }

    /// <summary>
    /// Process on-hit decay for all active status effects.
    /// Called when this combatant takes damage.
    /// </summary>
    private void ProcessOnHitDecay()
    {
        var effectsToRemove = new List<string>();

        foreach (var effect in ActiveEffects)
        {
            if (effect == null || !effect.IsActive) continue;

            int stacksBefore = effect.CurrentStacks;
            effect.ProcessOnHitDecay();
            int stacksAfter = effect.CurrentStacks;

            if (stacksAfter <= 0)
            {
                effectsToRemove.Add(effect.EffectId);
            }
        }

        // Remove depleted effects
        foreach (var effectId in effectsToRemove)
        {
            RemoveStatusEffect(effectId);
        }
    }

    /// <summary>
    /// Heal this combatant (cannot exceed max health)
    /// </summary>
    /// <returns>Actual amount healed</returns>
    public float ApplyHeal(float amount)
    {
        if (amount <= 0) return 0;

        float previousHealth = _currentHealth;
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        return _currentHealth - previousHealth;
    }

    /// <summary>
    /// Add shield to this combatant
    /// </summary>
    public void AddShield(float amount)
    {
        if (amount > 0)
        {
            _currentShield += amount;
        }
    }

    // === STATUS EFFECT METHODS ===

    /// <summary>
    /// Apply a status effect, handling stacking behavior
    /// </summary>
    public void ApplyStatusEffect(StatusEffectDefinition definition, int stacks = 1)
    {
        if (definition == null || stacks <= 0) return;

        // Check if we already have this effect
        var existing = GetStatusEffect(definition.EffectID);
        if (existing != null)
        {
            existing.AddStacks(stacks, definition);
        }
        else
        {
            var newEffect = new ActiveStatusEffect(definition, stacks);
            ActiveEffects.Add(newEffect);
        }
    }

    /// <summary>
    /// Remove a status effect by ID
    /// </summary>
    public void RemoveStatusEffect(string effectId)
    {
        ActiveEffects.RemoveAll(e => e.EffectId == effectId);
    }

    /// <summary>
    /// Remove all status effects
    /// </summary>
    public void ClearAllStatusEffects()
    {
        ActiveEffects.Clear();
    }

    /// <summary>
    /// Get active effect by ID
    /// </summary>
    public ActiveStatusEffect GetStatusEffect(string effectId)
    {
        return ActiveEffects.Find(e => e.EffectId == effectId && e.IsActive);
    }

    /// <summary>
    /// Get effect by ID regardless of active status (needed for removal of expired effects)
    /// </summary>
    public ActiveStatusEffect GetStatusEffectIncludingExpired(string effectId)
    {
        return ActiveEffects.Find(e => e.EffectId == effectId);
    }

    /// <summary>
    /// Check if has effect by ID
    /// </summary>
    public bool HasStatusEffect(string effectId)
    {
        return GetStatusEffect(effectId) != null;
    }

    /// <summary>
    /// Check if has any effect of a specific type
    /// </summary>
    public bool HasStatusEffect(StatusEffectType type)
    {
        foreach (var effect in ActiveEffects)
        {
            if (effect != null && effect.IsActive && effect.EffectType == type)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Remove expired status effects
    /// </summary>
    public int CleanupExpiredEffects()
    {
        return ActiveEffects.RemoveAll(e => e == null || e.IsExpired || e.CurrentStacks <= 0);
    }

    // === STAT CALCULATION ===

    /// <summary>
    /// Calculate modified stats from base stats + active buff/debuff effects
    /// </summary>
    public CombatantStats CalculateModifiedStats()
    {
        var result = BaseStats;

        foreach (var effect in ActiveEffects)
        {
            if (effect == null || !effect.IsActive) continue;

            var def = effect.GetDefinition();
            if (def == null || !def.IsStatModifier) continue;

            float modifier = def.ScalesWithStacks
                ? 1f + (def.BaseValue * effect.CurrentStacks)
                : 1f + def.BaseValue;

            switch (def.EffectType)
            {
                case StatusEffectType.AttackBuff:
                    result.AttackMultiplier *= modifier;
                    break;
                case StatusEffectType.AttackDebuff:
                    result.AttackMultiplier *= modifier; // BaseValue should be negative
                    break;
                case StatusEffectType.DefenseBuff:
                    result.DefenseMultiplier *= modifier; // Lower = less damage taken
                    break;
                case StatusEffectType.DefenseDebuff:
                    result.DefenseMultiplier *= modifier;
                    break;
                case StatusEffectType.SpeedBuff:
                    result.SpeedMultiplier *= modifier;
                    break;
                case StatusEffectType.SpeedDebuff:
                    result.SpeedMultiplier *= modifier;
                    break;
            }
        }

        return result;
    }

    // === UTILITY ===

    /// <summary>
    /// Clear all combat state
    /// </summary>
    public void Clear()
    {
        CombatantId = string.Empty;
        DisplayName = string.Empty;
        IsPlayer = false;
        _currentHealth = 0;
        _maxHealth = 100;
        _currentShield = 0;
        ActiveEffects.Clear();
        BaseStats = CombatantStats.Default;
        AbilityIds.Clear();
    }

    /// <summary>
    /// Check if data is valid
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(CombatantId)) return false;
        if (_maxHealth <= 0) return false;
        if (_currentHealth < 0) return false;
        return true;
    }

    /// <summary>
    /// Create a deep copy
    /// </summary>
    public CombatantData Clone()
    {
        var clone = new CombatantData
        {
            CombatantId = this.CombatantId,
            DisplayName = this.DisplayName,
            IsPlayer = this.IsPlayer,
            _currentHealth = this._currentHealth,
            _maxHealth = this._maxHealth,
            _currentShield = this._currentShield,
            BaseStats = this.BaseStats,
            AbilityIds = new List<string>(this.AbilityIds)
        };

        foreach (var effect in ActiveEffects)
        {
            if (effect != null)
            {
                clone.ActiveEffects.Add(effect.Clone());
            }
        }

        return clone;
    }

    public override string ToString()
    {
        return $"[{DisplayName}: {_currentHealth:F0}/{_maxHealth:F0} HP, {_currentShield:F0} Shield, {ActiveEffects.Count} effects]";
    }
}
