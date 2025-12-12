// Purpose: Service responsible for executing abilities and managing cooldowns during combat
// Filepath: Assets/Scripts/Gameplay/Combat/Services/CombatAbilityService.cs

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Result of executing an ability
/// </summary>
public struct AbilityExecutionResult
{
    public float DamageDealt;
    public float HealingDone;
    public float ShieldAdded;
    public Dictionary<StatusEffectDefinition, int> StatusEffectsApplied;

    public static AbilityExecutionResult Empty => new AbilityExecutionResult
    {
        DamageDealt = 0,
        HealingDone = 0,
        ShieldAdded = 0,
        StatusEffectsApplied = new Dictionary<StatusEffectDefinition, int>()
    };
}

/// <summary>
/// Service responsible for executing abilities and applying their effects.
/// </summary>
public class CombatAbilityService
{
    private readonly CombatEventService _eventService;
    private readonly CombatStatusEffectService _statusEffectService;
    private readonly bool _enableDebugLogs;

    public CombatAbilityService(CombatEventService eventService, CombatStatusEffectService statusEffectService, bool enableDebugLogs = false)
    {
        _eventService = eventService;
        _statusEffectService = statusEffectService;
        _enableDebugLogs = enableDebugLogs;
    }

    /// <summary>
    /// Execute an ability from source to target.
    /// </summary>
    /// <param name="ability">The ability to execute</param>
    /// <param name="source">The combatant using the ability</param>
    /// <param name="target">The enemy combatant (target for damage, opposite for heals/shields)</param>
    /// <param name="instanceIndex">Index for duplicate ability tracking</param>
    /// <returns>Result of the ability execution</returns>
    public AbilityExecutionResult ExecuteAbility(AbilityDefinition ability, ICombatant source, ICombatant target, int instanceIndex)
    {
        if (ability == null || source == null || target == null)
        {
            return AbilityExecutionResult.Empty;
        }

        // Get stat modifiers from source
        var sourceStats = source.GetModifiedStats();

        // Process effects
        var result = ProcessEffects(ability, source, target, sourceStats);

        // Publish ability used event
        _eventService.PublishAbilityUsed(
            source.IsPlayer,
            ability,
            instanceIndex,
            result.DamageDealt,
            result.HealingDone,
            result.ShieldAdded
        );

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatAbilityService: {source.DisplayName} used {ability.GetDisplayName()} - " +
                $"Damage: {result.DamageDealt}, Heal: {result.HealingDone}, Shield: {result.ShieldAdded}",
                Logger.LogCategory.General);
        }

        return result;
    }

    /// <summary>
    /// Process ability effects
    /// </summary>
    private AbilityExecutionResult ProcessEffects(AbilityDefinition ability, ICombatant source, ICombatant target, CombatantStats sourceStats)
    {
        var result = new AbilityExecutionResult
        {
            StatusEffectsApplied = new Dictionary<StatusEffectDefinition, int>()
        };

        if (ability.Effects == null) return result;

        foreach (var effect in ability.Effects)
        {
            if (effect == null) continue;

            // Determine the actual target (self vs enemy)
            ICombatant effectTarget = effect.TargetsSelf ? source : target;

            switch (effect.Type)
            {
                case AbilityEffectType.Damage:
                    if (effect.Value > 0)
                    {
                        float modifiedDamage = effect.Value * sourceStats.AttackMultiplier;
                        float actualDamage = target.TakeDamage(modifiedDamage);
                        result.DamageDealt += actualDamage;
                        _eventService.PublishHealthChanged(target);
                    }
                    break;

                case AbilityEffectType.Heal:
                    if (effect.Value > 0)
                    {
                        float actualHeal = effectTarget.Heal(effect.Value);
                        result.HealingDone += actualHeal;
                        _eventService.PublishHealthChanged(effectTarget);
                    }
                    break;

                case AbilityEffectType.Shield:
                    if (effect.Value > 0)
                    {
                        effectTarget.AddShield(effect.Value);
                        result.ShieldAdded += effect.Value;
                        _eventService.PublishHealthChanged(effectTarget);
                    }
                    break;

                case AbilityEffectType.StatusEffect:
                    if (effect.StatusEffect != null && effect.StatusEffectStacks > 0)
                    {
                        _statusEffectService.ApplyEffect(effectTarget, effect.StatusEffect, effect.StatusEffectStacks, source.IsPlayer);

                        if (!result.StatusEffectsApplied.ContainsKey(effect.StatusEffect))
                        {
                            result.StatusEffectsApplied[effect.StatusEffect] = 0;
                        }
                        result.StatusEffectsApplied[effect.StatusEffect] += effect.StatusEffectStacks;
                    }
                    break;
            }
        }

        return result;
    }

    /// <summary>
    /// Calculate adjusted cooldown based on speed modifier
    /// </summary>
    public float GetAdjustedCooldown(float baseCooldown, CombatantStats stats)
    {
        if (stats.SpeedMultiplier <= 0) return baseCooldown;
        return baseCooldown / stats.SpeedMultiplier;
    }

    /// <summary>
    /// Check if a combatant can use abilities (not stunned)
    /// </summary>
    public bool CanUseAbilities(ICombatant combatant)
    {
        return combatant != null && !combatant.IsStunned;
    }
}
