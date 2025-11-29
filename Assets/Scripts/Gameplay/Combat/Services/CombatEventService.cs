// Purpose: Service responsible for publishing all combat-related events
// Filepath: Assets/Scripts/Gameplay/Combat/Services/CombatEventService.cs

using CombatEvents;
using System.Collections.Generic;

/// <summary>
/// Service responsible for publishing all combat-related events via EventBus.
/// Centralizes event publishing to ensure consistency and make it easier to track.
/// </summary>
public class CombatEventService
{
    private readonly bool _enableDebugLogs;

    public CombatEventService(bool enableDebugLogs = false)
    {
        _enableDebugLogs = enableDebugLogs;
    }

    // === COMBAT LIFECYCLE EVENTS ===

    public void PublishCombatStarted(CombatData combat, EnemyDefinition enemy)
    {
        var evt = new CombatStartedEvent(combat, enemy);
        EventBus.Publish(evt);

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatEventService: {evt}", Logger.LogCategory.General);
        }
    }

    public void PublishCombatEnded(bool playerWon, EnemyDefinition enemy, int experienceGained,
        Dictionary<ItemDefinition, int> lootDropped, string endReason)
    {
        var evt = new CombatEndedEvent(playerWon, enemy, experienceGained, lootDropped, endReason);
        EventBus.Publish(evt);

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatEventService: {evt}", Logger.LogCategory.General);
        }
    }

    public void PublishCombatFled(EnemyDefinition enemy)
    {
        var evt = new CombatFledEvent(enemy);
        EventBus.Publish(evt);

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatEventService: {evt}", Logger.LogCategory.General);
        }
    }

    // === HEALTH EVENTS ===

    public void PublishHealthChanged(ICombatant combatant)
    {
        if (combatant == null) return;

        var evt = new CombatHealthChangedEvent(
            combatant.IsPlayer,
            combatant.CurrentHealth,
            combatant.MaxHealth,
            combatant.CurrentShield
        );
        EventBus.Publish(evt);

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatEventService: {evt}", Logger.LogCategory.General);
        }
    }

    public void PublishHealthChanged(bool isPlayer, float currentHealth, float maxHealth, float currentShield)
    {
        var evt = new CombatHealthChangedEvent(isPlayer, currentHealth, maxHealth, currentShield);
        EventBus.Publish(evt);

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatEventService: {evt}", Logger.LogCategory.General);
        }
    }

    // === ABILITY EVENTS ===

    public void PublishAbilityUsed(bool isPlayerAbility, AbilityDefinition ability, int instanceIndex,
        float damageDealt = 0, float healingDone = 0, float shieldAdded = 0, float poisonApplied = 0)
    {
        var evt = new CombatAbilityUsedEvent(
            isPlayerAbility, ability, instanceIndex,
            damageDealt, healingDone, shieldAdded, poisonApplied
        );
        EventBus.Publish(evt);

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatEventService: {evt}", Logger.LogCategory.General);
        }
    }

    public void PublishAbilityCooldownStarted(bool isPlayerAbility, AbilityDefinition ability,
        int instanceIndex, float cooldownDuration)
    {
        var evt = new CombatAbilityCooldownStartedEvent(isPlayerAbility, ability, instanceIndex, cooldownDuration);
        EventBus.Publish(evt);

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatEventService: {evt}", Logger.LogCategory.General);
        }
    }

    // === STATUS EFFECT EVENTS (NEW GENERIC SYSTEM) ===

    public void PublishStatusEffectApplied(bool isTargetPlayer, StatusEffectDefinition effect,
        int stacksApplied, int totalStacks, bool wasAppliedByPlayer)
    {
        var evt = new StatusEffectAppliedEvent(isTargetPlayer, effect, stacksApplied, totalStacks, wasAppliedByPlayer);
        EventBus.Publish(evt);

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatEventService: {evt}", Logger.LogCategory.General);
        }
    }

    public void PublishStatusEffectTick(bool isTargetPlayer, StatusEffectDefinition effect,
        float value, int remainingStacks, float remainingDuration)
    {
        var evt = new StatusEffectTickEvent(isTargetPlayer, effect, value, remainingStacks, remainingDuration);
        EventBus.Publish(evt);

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatEventService: {evt}", Logger.LogCategory.General);
        }
    }

    public void PublishStatusEffectRemoved(bool isTargetPlayer, StatusEffectDefinition effect, string reason = "expired")
    {
        var evt = new StatusEffectRemovedEvent(isTargetPlayer, effect, reason);
        EventBus.Publish(evt);

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatEventService: {evt}", Logger.LogCategory.General);
        }
    }

    // === STUN EVENTS ===

    public void PublishStunApplied(bool isTargetPlayer, float duration)
    {
        var evt = new CombatStunAppliedEvent(isTargetPlayer, duration);
        EventBus.Publish(evt);

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatEventService: {evt}", Logger.LogCategory.General);
        }
    }

    public void PublishStunEnded(bool isTargetPlayer)
    {
        var evt = new CombatStunEndedEvent(isTargetPlayer);
        EventBus.Publish(evt);

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatEventService: {evt}", Logger.LogCategory.General);
        }
    }

    // === LEGACY POISON EVENT (for backwards compatibility) ===

    public void PublishPoisonTick(bool isPlayer, float poisonDamage, float remainingStacks)
    {
        var evt = new CombatPoisonTickEvent(isPlayer, poisonDamage, remainingStacks);
        EventBus.Publish(evt);

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatEventService: {evt}", Logger.LogCategory.General);
        }
    }
}
