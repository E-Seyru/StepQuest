// Purpose: Service responsible for managing status effects during combat
// Filepath: Assets/Scripts/Gameplay/Combat/Services/CombatStatusEffectService.cs

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Service responsible for applying, updating, and removing status effects during combat.
/// Handles tick processing for DoT/HoT effects and stun management.
/// </summary>
public class CombatStatusEffectService
{
    private readonly CombatEventService _eventService;
    private readonly bool _enableDebugLogs;

    // Track stun state for each combatant
    private bool _playerWasStunned;
    private bool _enemyWasStunned;

    // Reusable list to avoid allocations each frame
    private readonly List<string> _effectsToRemoveCache = new List<string>(8);

    public CombatStatusEffectService(CombatEventService eventService, bool enableDebugLogs = false)
    {
        _eventService = eventService;
        _enableDebugLogs = enableDebugLogs;
    }

    /// <summary>
    /// Reset service state (call when combat starts)
    /// </summary>
    public void Reset()
    {
        _playerWasStunned = false;
        _enemyWasStunned = false;
    }

    // === APPLY STATUS EFFECTS ===

    /// <summary>
    /// Apply a status effect to a combatant
    /// </summary>
    /// <param name="target">Target combatant</param>
    /// <param name="effect">Effect definition to apply</param>
    /// <param name="stacks">Number of stacks to apply</param>
    /// <param name="sourceIsPlayer">True if the effect was applied by the player</param>
    public void ApplyEffect(ICombatant target, StatusEffectDefinition effect, int stacks, bool sourceIsPlayer)
    {
        if (target == null || effect == null || stacks <= 0) return;

        // Get existing stacks before applying
        var existingEffect = target.GetStatusEffect(effect.EffectID);
        int previousStacks = existingEffect?.CurrentStacks ?? 0;

        // Apply the effect
        target.ApplyStatusEffect(effect, stacks);

        // Get new total stacks
        var newEffect = target.GetStatusEffect(effect.EffectID);
        int newTotalStacks = newEffect?.CurrentStacks ?? stacks;

        // Publish event
        _eventService.PublishStatusEffectApplied(
            target.IsPlayer,
            effect,
            stacks,
            newTotalStacks,
            sourceIsPlayer
        );

        // Handle stun specifically
        if (effect.PreventsActions)
        {
            _eventService.PublishStunApplied(target.IsPlayer, effect.Duration);

            if (target.IsPlayer)
                _playerWasStunned = true;
            else
                _enemyWasStunned = true;
        }

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatStatusEffectService: Applied {effect.GetDisplayName()} ({stacks} stacks) to {target.DisplayName}", Logger.LogCategory.General);
        }
    }

    /// <summary>
    /// Remove a status effect from a combatant
    /// </summary>
    public void RemoveEffect(ICombatant target, string effectId, string reason = "removed")
    {
        if (target == null || string.IsNullOrEmpty(effectId)) return;

        // Use GetStatusEffectIncludingExpired because expired effects may need to be removed
        // GetStatusEffect only returns active effects, which would skip expired ones
        var effect = target.GetStatusEffectIncludingExpired(effectId);
        if (effect == null) return;

        var definition = effect.GetDefinition();
        bool wasStunEffect = definition?.PreventsActions ?? false;

        target.RemoveStatusEffect(effectId);

        if (definition != null)
        {
            _eventService.PublishStatusEffectRemoved(target.IsPlayer, definition, reason);
        }

        // Check if stun ended
        if (wasStunEffect && !target.IsStunned)
        {
            _eventService.PublishStunEnded(target.IsPlayer);

            if (target.IsPlayer)
                _playerWasStunned = false;
            else
                _enemyWasStunned = false;
        }

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatStatusEffectService: Removed {effectId} from {target.DisplayName} ({reason})", Logger.LogCategory.General);
        }
    }

    /// <summary>
    /// Clear all status effects from a combatant
    /// </summary>
    public void ClearAllEffects(ICombatant target, string reason = "combat_ended")
    {
        if (target == null) return;

        // Publish removal events for each effect
        foreach (var effect in new List<ActiveStatusEffect>(target.ActiveEffects))
        {
            if (effect == null) continue;

            var definition = effect.GetDefinition();
            if (definition != null)
            {
                _eventService.PublishStatusEffectRemoved(target.IsPlayer, definition, reason);
            }
        }

        target.ClearAllStatusEffects();

        // Reset stun tracking
        if (target.IsPlayer)
            _playerWasStunned = false;
        else
            _enemyWasStunned = false;
    }

    // === UPDATE STATUS EFFECTS ===

    /// <summary>
    /// Process status effect ticks for a combatant.
    /// Call this each frame during combat.
    /// Returns total damage/healing applied this frame.
    /// </summary>
    public (float damageDealt, float healingDone) ProcessTicks(ICombatant target, float deltaTime)
    {
        if (target == null) return (0, 0);

        var activeEffects = target.ActiveEffects;
        if (activeEffects.Count == 0) return (0, 0);

        float totalDamage = 0;
        float totalHealing = 0;

        // Reuse list to avoid allocations - clear instead of new
        _effectsToRemoveCache.Clear();

        // Process each active effect
        for (int i = 0; i < activeEffects.Count; i++)
        {
            var effect = activeEffects[i];
            if (effect == null) continue;

            var definition = effect.GetDefinition();
            if (definition == null)
            {
                _effectsToRemoveCache.Add(effect.EffectId);
                continue;
            }

            // Cache CurrentStacks to avoid recalculating
            int stacksBefore = effect.CurrentStacks;

            // Update timers (this may expire some stack batches)
            bool shouldTick = effect.UpdateTimers(deltaTime);

            // Check how many stacks expired (reuse cached value)
            int stacksAfter = effect.CurrentStacks;
            int expiredStacks = stacksBefore - stacksAfter;

            // If some stacks expired but effect still active, notify UI of the change
            if (expiredStacks > 0 && stacksAfter > 0)
            {
                _eventService.PublishStatusEffectApplied(
                    target.IsPlayer,
                    definition,
                    0, // No new stacks applied
                    stacksAfter, // Current total
                    false // Not from player action
                );

                if (_enableDebugLogs)
                {
                    Logger.LogInfo($"CombatStatusEffectService: {expiredStacks} stacks of {definition.GetDisplayName()} expired on {target.DisplayName}, {stacksAfter} remaining", Logger.LogCategory.General);
                }
            }

            // Check if fully expired
            if (effect.IsExpired || stacksAfter <= 0)
            {
                _effectsToRemoveCache.Add(effect.EffectId);
                continue;
            }

            // Process tick if needed
            if (shouldTick && definition.TickInterval > 0)
            {
                float tickValue = effect.GetTickValue();

                if (definition.IsDamageOverTime && tickValue > 0)
                {
                    // Apply damage
                    float actualDamage = target.TakeDamage(tickValue, ignoreShield: false);
                    totalDamage += actualDamage;

                    // Publish tick event
                    _eventService.PublishStatusEffectTick(
                        target.IsPlayer,
                        definition,
                        actualDamage,
                        stacksAfter, // Use cached value
                        effect.RemainingDuration
                    );

                    // Publish health changed
                    _eventService.PublishHealthChanged(target);

                    if (_enableDebugLogs)
                    {
                        Logger.LogInfo($"CombatStatusEffectService: {target.DisplayName} took {actualDamage} {definition.EffectType} damage", Logger.LogCategory.General);
                    }
                }
                else if (definition.IsHealOverTime && tickValue > 0)
                {
                    // Apply healing
                    float actualHeal = target.Heal(tickValue);
                    totalHealing += actualHeal;

                    // Publish tick event
                    _eventService.PublishStatusEffectTick(
                        target.IsPlayer,
                        definition,
                        actualHeal,
                        stacksAfter, // Use cached value
                        effect.RemainingDuration
                    );

                    // Publish health changed
                    _eventService.PublishHealthChanged(target);

                    if (_enableDebugLogs)
                    {
                        Logger.LogInfo($"CombatStatusEffectService: {target.DisplayName} healed {actualHeal} from {definition.GetDisplayName()}", Logger.LogCategory.General);
                    }
                }

                // Process on-tick decay (if applicable)
                int stacksBeforeDecay = stacksAfter; // Reuse cached value
                effect.ProcessOnTickDecay();
                int stacksAfterDecay = effect.CurrentStacks;

                // Notify UI if stacks changed due to decay
                if (stacksAfterDecay < stacksBeforeDecay && stacksAfterDecay > 0)
                {
                    _eventService.PublishStatusEffectApplied(
                        target.IsPlayer,
                        definition,
                        0,
                        stacksAfterDecay,
                        false
                    );
                }
                else if (stacksAfterDecay <= 0)
                {
                    _effectsToRemoveCache.Add(effect.EffectId);
                }
            }
        }

        // Remove fully expired effects
        for (int i = 0; i < _effectsToRemoveCache.Count; i++)
        {
            RemoveEffect(target, _effectsToRemoveCache[i], "expired");
        }

        // Check if stun ended
        CheckStunState(target);

        return (totalDamage, totalHealing);
    }

    /// <summary>
    /// Check and update stun state for a combatant
    /// </summary>
    private void CheckStunState(ICombatant target)
    {
        if (target == null) return;

        bool isCurrentlyStunned = target.IsStunned;

        if (target.IsPlayer)
        {
            if (_playerWasStunned && !isCurrentlyStunned)
            {
                _eventService.PublishStunEnded(true);
                _playerWasStunned = false;
            }
        }
        else
        {
            if (_enemyWasStunned && !isCurrentlyStunned)
            {
                _eventService.PublishStunEnded(false);
                _enemyWasStunned = false;
            }
        }
    }

    // === QUERIES ===

    /// <summary>
    /// Check if a combatant is currently stunned
    /// </summary>
    public bool IsStunned(ICombatant combatant)
    {
        return combatant?.IsStunned ?? false;
    }

    /// <summary>
    /// Get total DoT damage per second for a combatant
    /// </summary>
    public float GetTotalDotDamagePerSecond(ICombatant combatant)
    {
        if (combatant == null) return 0;

        float total = 0;
        foreach (var effect in combatant.ActiveEffects)
        {
            if (effect == null || !effect.IsActive) continue;

            var def = effect.GetDefinition();
            if (def == null || !def.IsDamageOverTime || def.TickInterval <= 0) continue;

            float tickValue = effect.GetTickValue();
            float dps = tickValue / def.TickInterval;
            total += dps;
        }

        return total;
    }

    /// <summary>
    /// Get total HoT healing per second for a combatant
    /// </summary>
    public float GetTotalHotHealingPerSecond(ICombatant combatant)
    {
        if (combatant == null) return 0;

        float total = 0;
        foreach (var effect in combatant.ActiveEffects)
        {
            if (effect == null || !effect.IsActive) continue;

            var def = effect.GetDefinition();
            if (def == null || !def.IsHealOverTime || def.TickInterval <= 0) continue;

            float tickValue = effect.GetTickValue();
            float hps = tickValue / def.TickInterval;
            total += hps;
        }

        return total;
    }
}
