// Purpose: Runtime data structure representing an active status effect on a combatant
// Filepath: Assets/Scripts/Data/Models/ActiveStatusEffect.cs

using System;
using UnityEngine;

/// <summary>
/// Runtime instance of an active status effect on a combatant.
/// Serializable for persistence across app sessions.
/// </summary>
[Serializable]
public class ActiveStatusEffect
{
    // === SERIALIZED STATE ===

    /// <summary>Reference to the StatusEffectDefinition by ID</summary>
    public string EffectId;

    /// <summary>Current number of stacks</summary>
    public int CurrentStacks;

    /// <summary>Time remaining before this effect expires (seconds)</summary>
    public float RemainingDuration;

    /// <summary>Time since the last tick was processed (seconds)</summary>
    public float TimeSinceLastTick;

    // === RUNTIME CACHE ===

    [NonSerialized]
    private StatusEffectDefinition _cachedDefinition;

    // === CONSTRUCTORS ===

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    public ActiveStatusEffect()
    {
        EffectId = string.Empty;
        CurrentStacks = 0;
        RemainingDuration = 0;
        TimeSinceLastTick = 0;
    }

    /// <summary>
    /// Create a new active status effect
    /// </summary>
    public ActiveStatusEffect(StatusEffectDefinition definition, int initialStacks = 1)
    {
        if (definition == null)
        {
            EffectId = string.Empty;
            CurrentStacks = 0;
            RemainingDuration = 0;
            TimeSinceLastTick = 0;
            return;
        }

        EffectId = definition.EffectID;
        CurrentStacks = Mathf.Clamp(initialStacks, 1, definition.MaxStacks);
        RemainingDuration = definition.Duration;
        TimeSinceLastTick = 0;
        _cachedDefinition = definition;
    }

    // === PROPERTIES ===

    /// <summary>
    /// Check if this effect has expired (duration ended)
    /// Effects with Duration = 0 are permanent and never expire naturally
    /// </summary>
    public bool IsExpired
    {
        get
        {
            var def = GetDefinition();
            if (def == null) return true;
            if (def.Duration <= 0) return false; // Permanent effect
            return RemainingDuration <= 0;
        }
    }

    /// <summary>
    /// Check if this effect is currently active (not expired and has stacks)
    /// </summary>
    public bool IsActive => !IsExpired && CurrentStacks > 0;

    /// <summary>
    /// Get the effect type from definition
    /// </summary>
    public StatusEffectType EffectType => GetDefinition()?.EffectType ?? StatusEffectType.Poison;

    /// <summary>
    /// Check if this effect prevents actions (is a stun)
    /// </summary>
    public bool PreventsActions => GetDefinition()?.PreventsActions ?? false;

    // === PUBLIC METHODS ===

    /// <summary>
    /// Get the StatusEffectDefinition for this effect.
    /// Uses cached reference when available.
    /// </summary>
    public StatusEffectDefinition GetDefinition()
    {
        if (_cachedDefinition != null) return _cachedDefinition;

        if (string.IsNullOrEmpty(EffectId)) return null;

        // Try to get from registry
        var registry = StatusEffectRegistry.Instance;
        if (registry != null)
        {
            _cachedDefinition = registry.GetEffect(EffectId);
        }

        return _cachedDefinition;
    }

    /// <summary>
    /// Set the cached definition (used when creating the effect)
    /// </summary>
    public void SetDefinition(StatusEffectDefinition definition)
    {
        _cachedDefinition = definition;
        if (definition != null)
        {
            EffectId = definition.EffectID;
        }
    }

    /// <summary>
    /// Add stacks to this effect based on its stacking behavior
    /// </summary>
    public void AddStacks(int amount, StatusEffectDefinition definition = null)
    {
        var def = definition ?? GetDefinition();
        if (def == null || amount <= 0) return;

        switch (def.Stacking)
        {
            case StackingBehavior.Additive:
                // Just add stacks, capped at max
                CurrentStacks = Mathf.Min(CurrentStacks + amount, def.MaxStacks);
                break;

            case StackingBehavior.RefreshDuration:
                // Don't add stacks, just refresh duration
                RemainingDuration = def.Duration;
                break;

            case StackingBehavior.MaxStacks:
                // Add stacks up to max, and refresh duration
                CurrentStacks = Mathf.Min(CurrentStacks + amount, def.MaxStacks);
                RemainingDuration = def.Duration;
                break;

            case StackingBehavior.Replace:
                // Replace entirely
                CurrentStacks = Mathf.Min(amount, def.MaxStacks);
                RemainingDuration = def.Duration;
                TimeSinceLastTick = 0;
                break;
        }
    }

    /// <summary>
    /// Remove stacks from this effect
    /// </summary>
    public void RemoveStacks(int amount)
    {
        CurrentStacks = Mathf.Max(0, CurrentStacks - amount);
    }

    /// <summary>
    /// Update timers (called each frame during combat)
    /// Returns true if a tick should be processed
    /// </summary>
    public bool UpdateTimers(float deltaTime)
    {
        var def = GetDefinition();
        if (def == null) return false;

        // Update duration (if not permanent)
        if (def.Duration > 0)
        {
            RemainingDuration -= deltaTime;
        }

        // Update tick timer
        TimeSinceLastTick += deltaTime;

        // Check if we should tick
        if (def.TickInterval > 0 && TimeSinceLastTick >= def.TickInterval)
        {
            TimeSinceLastTick -= def.TickInterval;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculate the current tick value based on stacks
    /// </summary>
    public float GetTickValue()
    {
        var def = GetDefinition();
        if (def == null) return 0;
        return def.CalculateTickValue(CurrentStacks);
    }

    /// <summary>
    /// Clear all data
    /// </summary>
    public void Clear()
    {
        EffectId = string.Empty;
        CurrentStacks = 0;
        RemainingDuration = 0;
        TimeSinceLastTick = 0;
        _cachedDefinition = null;
    }

    /// <summary>
    /// Check if this effect data is valid
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(EffectId)) return false;
        if (CurrentStacks < 0) return false;
        return true;
    }

    /// <summary>
    /// Create a copy of this effect
    /// </summary>
    public ActiveStatusEffect Clone()
    {
        return new ActiveStatusEffect
        {
            EffectId = this.EffectId,
            CurrentStacks = this.CurrentStacks,
            RemainingDuration = this.RemainingDuration,
            TimeSinceLastTick = this.TimeSinceLastTick,
            _cachedDefinition = this._cachedDefinition
        };
    }

    public override string ToString()
    {
        var def = GetDefinition();
        string name = def?.GetDisplayName() ?? EffectId;

        if (!IsActive)
            return $"[{name}: Inactive]";

        if (def?.Duration > 0)
            return $"[{name}: {CurrentStacks} stacks, {RemainingDuration:F1}s remaining]";
        else
            return $"[{name}: {CurrentStacks} stacks]";
    }
}
