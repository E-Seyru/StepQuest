// Purpose: Runtime wrapper implementing ICombatant interface around CombatantData
// Filepath: Assets/Scripts/Gameplay/Combat/Combatant.cs

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime wrapper that implements ICombatant around CombatantData.
/// Handles ability resolution and provides the full ICombatant interface.
/// </summary>
public class Combatant : ICombatant
{
    // === BACKING DATA ===

    private readonly CombatantData _data;
    private List<AbilityDefinition> _resolvedAbilities;
    private EnemyDefinition _enemyDefinition; // Cached for enemies

    // === CONSTRUCTORS ===

    /// <summary>
    /// Create a combatant from existing data
    /// </summary>
    public Combatant(CombatantData data)
    {
        _data = data ?? new CombatantData();
        _resolvedAbilities = null;
    }

    /// <summary>
    /// Create a player combatant
    /// </summary>
    public static Combatant CreatePlayer(float maxHealth, List<AbilityDefinition> abilities)
    {
        var data = CombatantData.CreatePlayer(maxHealth, abilities);
        var combatant = new Combatant(data);
        combatant._resolvedAbilities = abilities != null ? new List<AbilityDefinition>(abilities) : new List<AbilityDefinition>();
        return combatant;
    }

    /// <summary>
    /// Create an enemy combatant from definition
    /// </summary>
    public static Combatant CreateFromEnemy(EnemyDefinition enemy)
    {
        var data = CombatantData.CreateFromEnemy(enemy);
        var combatant = new Combatant(data);
        combatant._enemyDefinition = enemy;
        combatant._resolvedAbilities = enemy?.Abilities != null
            ? new List<AbilityDefinition>(enemy.Abilities)
            : new List<AbilityDefinition>();
        return combatant;
    }

    // === ICombatant IDENTITY ===

    public string CombatantId => _data.CombatantId;
    public string DisplayName => _data.DisplayName;
    public bool IsPlayer => _data.IsPlayer;

    // === ICombatant HEALTH ===

    public float CurrentHealth => _data.CurrentHealth;
    public float MaxHealth => _data.MaxHealth;
    public float HealthPercentage => _data.HealthPercentage;
    public bool IsAlive => _data.IsAlive;

    // === ICombatant COMBAT ACTIONS ===

    public float TakeDamage(float amount, bool ignoreShield = false)
    {
        var modifiedStats = GetModifiedStats();
        return _data.ApplyDamage(amount, ignoreShield, modifiedStats.DefenseMultiplier);
    }

    public float Heal(float amount)
    {
        return _data.ApplyHeal(amount);
    }

    // === ICombatant SHIELD ===

    public float CurrentShield => _data.CurrentShield;

    public void AddShield(float amount)
    {
        _data.AddShield(amount);
    }

    public float AbsorbDamageWithShield(float damage)
    {
        if (damage <= 0) return 0;

        float currentShield = _data.CurrentShield;
        if (currentShield <= 0) return damage;

        if (currentShield >= damage)
        {
            _data.CurrentShield -= damage;
            return 0;
        }
        else
        {
            float remaining = damage - currentShield;
            _data.CurrentShield = 0;
            return remaining;
        }
    }

    // === ICombatant STATUS EFFECTS ===

    public List<ActiveStatusEffect> ActiveEffects => _data.ActiveEffects;

    public void ApplyStatusEffect(StatusEffectDefinition effect, int stacks = 1)
    {
        _data.ApplyStatusEffect(effect, stacks);
    }

    public void RemoveStatusEffect(string effectId)
    {
        _data.RemoveStatusEffect(effectId);
    }

    public void ClearAllStatusEffects()
    {
        _data.ClearAllStatusEffects();
    }

    public bool HasStatusEffect(string effectId)
    {
        return _data.HasStatusEffect(effectId);
    }

    public bool HasStatusEffect(StatusEffectType type)
    {
        return _data.HasStatusEffect(type);
    }

    public ActiveStatusEffect GetStatusEffect(string effectId)
    {
        return _data.GetStatusEffect(effectId);
    }

    public bool IsStunned => _data.IsStunned;

    // === ICombatant ABILITIES ===

    public List<AbilityDefinition> Abilities
    {
        get
        {
            if (_resolvedAbilities == null)
            {
                ResolveAbilities();
            }
            return _resolvedAbilities;
        }
    }

    // === ICombatant STATS ===

    public CombatantStats GetBaseStats()
    {
        return _data.BaseStats;
    }

    public CombatantStats GetModifiedStats()
    {
        return _data.CalculateModifiedStats();
    }

    // === ADDITIONAL METHODS ===

    /// <summary>
    /// Get the underlying data (for serialization/persistence)
    /// </summary>
    public CombatantData GetData()
    {
        return _data;
    }

    /// <summary>
    /// Get the enemy definition (if this is an enemy)
    /// </summary>
    public EnemyDefinition GetEnemyDefinition()
    {
        return _enemyDefinition;
    }

    /// <summary>
    /// Update status effect timers and clean up expired effects.
    /// Returns list of effects that ticked this update.
    /// </summary>
    public List<(ActiveStatusEffect effect, float value)> UpdateStatusEffects(float deltaTime)
    {
        var tickedEffects = new List<(ActiveStatusEffect, float)>();

        foreach (var effect in _data.ActiveEffects)
        {
            if (effect == null || !effect.IsActive) continue;

            bool shouldTick = effect.UpdateTimers(deltaTime);
            if (shouldTick)
            {
                float tickValue = effect.GetTickValue();
                tickedEffects.Add((effect, tickValue));
            }
        }

        // Clean up expired effects
        _data.CleanupExpiredEffects();

        return tickedEffects;
    }

    /// <summary>
    /// Set current health directly (for initialization or testing)
    /// </summary>
    public void SetHealth(float health)
    {
        _data.CurrentHealth = health;
    }

    /// <summary>
    /// Set max health and optionally heal to full
    /// </summary>
    public void SetMaxHealth(float maxHealth, bool healToFull = false)
    {
        _data.MaxHealth = maxHealth;
        if (healToFull)
        {
            _data.CurrentHealth = maxHealth;
        }
    }

    /// <summary>
    /// Set base stats
    /// </summary>
    public void SetBaseStats(CombatantStats stats)
    {
        _data.BaseStats = stats;
    }

    /// <summary>
    /// Override abilities list (for equipment changes, etc.)
    /// </summary>
    public void SetAbilities(List<AbilityDefinition> abilities)
    {
        _resolvedAbilities = abilities != null ? new List<AbilityDefinition>(abilities) : new List<AbilityDefinition>();

        // Update ability IDs in data
        _data.AbilityIds.Clear();
        foreach (var ability in _resolvedAbilities)
        {
            if (ability != null && !string.IsNullOrEmpty(ability.AbilityID))
            {
                _data.AbilityIds.Add(ability.AbilityID);
            }
        }
    }

    // === PRIVATE HELPERS ===

    /// <summary>
    /// Resolve ability definitions from stored IDs
    /// </summary>
    private void ResolveAbilities()
    {
        _resolvedAbilities = new List<AbilityDefinition>();

        // If we have an enemy definition, use its abilities directly
        if (_enemyDefinition != null && _enemyDefinition.Abilities != null)
        {
            _resolvedAbilities.AddRange(_enemyDefinition.Abilities);
            return;
        }

        // Otherwise, we'd need to resolve from an ability registry
        // For now, log a warning if we have ability IDs but no registry
        if (_data.AbilityIds.Count > 0)
        {
            Logger.LogWarning($"Combatant '{CombatantId}' has ability IDs but no ability registry to resolve them", Logger.LogCategory.General);
        }
    }

    public override string ToString()
    {
        return _data.ToString();
    }
}
