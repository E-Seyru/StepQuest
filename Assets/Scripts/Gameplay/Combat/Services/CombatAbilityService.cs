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
/// Works with both legacy ability format and new AbilityEffect list format.
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
    /// Handles both legacy fields and new AbilityEffect list.
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

        var result = new AbilityExecutionResult
        {
            StatusEffectsApplied = new Dictionary<StatusEffectDefinition, int>()
        };

        // Get stat modifiers from source
        var sourceStats = source.GetModifiedStats();

        // Check if ability has new Effects list
        if (ability.Effects != null && ability.Effects.Count > 0)
        {
            // Use new system
            result = ProcessNewEffects(ability, source, target, sourceStats);
        }
        else
        {
            // Use legacy fields
            result = ProcessLegacyEffects(ability, source, target, sourceStats);
        }

        // Calculate legacy poison for event (for backwards compatibility)
        float poisonApplied = 0;
        foreach (var kvp in result.StatusEffectsApplied)
        {
            if (kvp.Key != null && kvp.Key.EffectType == StatusEffectType.Poison)
            {
                poisonApplied += kvp.Value;
            }
        }

        // Publish ability used event
        _eventService.PublishAbilityUsed(
            source.IsPlayer,
            ability,
            instanceIndex,
            result.DamageDealt,
            result.HealingDone,
            result.ShieldAdded,
            poisonApplied
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
    /// Process ability effects using the new AbilityEffect list system
    /// </summary>
    private AbilityExecutionResult ProcessNewEffects(AbilityDefinition ability, ICombatant source, ICombatant target, CombatantStats sourceStats)
    {
        var result = new AbilityExecutionResult
        {
            StatusEffectsApplied = new Dictionary<StatusEffectDefinition, int>()
        };

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
    /// Process ability effects using legacy fields (backwards compatibility)
    /// </summary>
    private AbilityExecutionResult ProcessLegacyEffects(AbilityDefinition ability, ICombatant source, ICombatant target, CombatantStats sourceStats)
    {
        var result = new AbilityExecutionResult
        {
            StatusEffectsApplied = new Dictionary<StatusEffectDefinition, int>()
        };

        // Damage
        if (ability.HasEffect(AbilityEffectType.Damage) && ability.DamageAmount > 0)
        {
            float modifiedDamage = ability.DamageAmount * sourceStats.AttackMultiplier;
            float actualDamage = target.TakeDamage(modifiedDamage);
            result.DamageDealt = actualDamage;
            _eventService.PublishHealthChanged(target);
        }

        // Heal (always heals self)
        if (ability.HasEffect(AbilityEffectType.Heal) && ability.HealAmount > 0)
        {
            float actualHeal = source.Heal(ability.HealAmount);
            result.HealingDone = actualHeal;
            _eventService.PublishHealthChanged(source);
        }

        // Shield (always shields self)
        if (ability.HasEffect(AbilityEffectType.Shield) && ability.ShieldAmount > 0)
        {
            source.AddShield(ability.ShieldAmount);
            result.ShieldAdded = ability.ShieldAmount;
            _eventService.PublishHealthChanged(source);
        }

        // Poison (legacy - applies to target, need to find or create poison effect)
        if (ability.HasEffect(AbilityEffectType.Poison) && ability.PoisonAmount > 0)
        {
            // Try to find poison effect in registry
            var poisonEffect = StatusEffectRegistry.Instance?.GetEffect("poison");

            if (poisonEffect != null)
            {
                int stacks = Mathf.RoundToInt(ability.PoisonAmount);
                _statusEffectService.ApplyEffect(target, poisonEffect, stacks, source.IsPlayer);
                result.StatusEffectsApplied[poisonEffect] = stacks;
            }
            else
            {
                // Legacy fallback: directly add poison stacks to combatant data
                // This will be handled by the old system if StatusEffectRegistry isn't set up
                if (_enableDebugLogs)
                {
                    Logger.LogWarning("CombatAbilityService: No poison effect in registry, using legacy system", Logger.LogCategory.General);
                }
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
