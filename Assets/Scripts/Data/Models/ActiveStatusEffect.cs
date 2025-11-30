// Purpose: Runtime data structure representing an active status effect on a combatant
// Filepath: Assets/Scripts/Data/Models/ActiveStatusEffect.cs

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a batch of stacks with their own expiration time.
/// Each application of stacks creates a new batch that expires independently.
/// </summary>
[Serializable]
public class StackBatch
{
    public int Stacks;
    public float RemainingDuration;

    public StackBatch(int stacks, float duration)
    {
        Stacks = stacks;
        RemainingDuration = duration;
    }
}

/// <summary>
/// Runtime instance of an active status effect on a combatant.
/// Serializable for persistence across app sessions.
/// Now supports independent stack expiration - each batch of stacks expires on its own timer.
/// </summary>
[Serializable]
public class ActiveStatusEffect
{
    // === SERIALIZED STATE ===

    /// <summary>Reference to the StatusEffectDefinition by ID</summary>
    public string EffectId;

    /// <summary>
    /// List of stack batches, each with their own expiration timer.
    /// When stacks are added, a new batch is created (or existing refreshed based on stacking behavior).
    /// </summary>
    public List<StackBatch> StackBatches = new List<StackBatch>();

    /// <summary>Time since the last tick was processed (seconds)</summary>
    public float TimeSinceLastTick;

    // === RUNTIME CACHE ===

    [NonSerialized]
    private StatusEffectDefinition _cachedDefinition;

    // Stack cache to avoid recalculating every access
    [NonSerialized]
    private int _cachedStackCount = -1;
    [NonSerialized]
    private int _lastBatchCount = -1;

    // === COMPUTED PROPERTIES ===

    /// <summary>Current total number of stacks across all batches (cached)</summary>
    public int CurrentStacks
    {
        get
        {
            // Invalidate cache if batch count changed
            if (_lastBatchCount != StackBatches.Count)
            {
                _cachedStackCount = -1;
                _lastBatchCount = StackBatches.Count;
            }

            // Recalculate if cache is invalid
            if (_cachedStackCount < 0)
            {
                int total = 0;
                for (int i = 0; i < StackBatches.Count; i++)
                {
                    total += StackBatches[i].Stacks;
                }
                _cachedStackCount = total;
            }
            return _cachedStackCount;
        }
    }

    /// <summary>Invalidate the stack cache (call after modifying stacks)</summary>
    private void InvalidateStackCache()
    {
        _cachedStackCount = -1;
    }

    /// <summary>Time remaining before the oldest batch expires (for UI display)</summary>
    public float RemainingDuration
    {
        get
        {
            if (StackBatches.Count == 0) return 0;
            float min = float.MaxValue;
            for (int i = 0; i < StackBatches.Count; i++)
            {
                float dur = StackBatches[i].RemainingDuration;
                if (dur < min) min = dur;
            }
            return min == float.MaxValue ? 0 : min;
        }
    }

    // === CONSTRUCTORS ===

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    public ActiveStatusEffect()
    {
        EffectId = string.Empty;
        StackBatches = new List<StackBatch>();
        TimeSinceLastTick = 0;
    }

    /// <summary>
    /// Create a new active status effect
    /// </summary>
    public ActiveStatusEffect(StatusEffectDefinition definition, int initialStacks = 1)
    {
        StackBatches = new List<StackBatch>();

        if (definition == null)
        {
            EffectId = string.Empty;
            TimeSinceLastTick = 0;
            return;
        }

        EffectId = definition.EffectID;
        int clampedStacks = Mathf.Clamp(initialStacks, 1, definition.EffectiveMaxStacks);

        // Duration only matters for time-based decay
        float batchDuration = definition.Decay == DecayBehavior.Time ? definition.Duration : float.MaxValue;
        StackBatches.Add(new StackBatch(clampedStacks, batchDuration));

        TimeSinceLastTick = 0;
        _cachedDefinition = definition;
    }

    // === PROPERTIES ===

    /// <summary>
    /// Check if this effect has completely expired (no stacks remaining)
    /// Effects with Duration = 0 are permanent and never expire naturally
    /// </summary>
    public bool IsExpired
    {
        get
        {
            var def = GetDefinition();
            if (def == null) return true;
            if (def.Duration <= 0) return false; // Permanent effect
            return StackBatches.Count == 0 || CurrentStacks <= 0;
        }
    }

    /// <summary>
    /// Check if this effect is currently active (has stacks)
    /// </summary>
    public bool IsActive => CurrentStacks > 0;

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
    /// Add stacks to this effect based on its stacking and decay behavior.
    /// </summary>
    public void AddStacks(int amount, StatusEffectDefinition definition = null)
    {
        var def = definition ?? GetDefinition();
        if (def == null || amount <= 0) return;

        int currentTotal = CurrentStacks;
        int maxStacks = def.EffectiveMaxStacks; // Handles unlimited (0 -> int.MaxValue)

        switch (def.Stacking)
        {
            case StackingBehavior.Stacking:
                // Add new batch, capped at max total stacks
                int stacksToAdd = Mathf.Min(amount, maxStacks - currentTotal);
                if (stacksToAdd > 0)
                {
                    // Duration only matters for time-based decay
                    float batchDuration = def.Decay == DecayBehavior.Time ? def.Duration : float.MaxValue;
                    StackBatches.Add(new StackBatch(stacksToAdd, batchDuration));
                }
                break;

            case StackingBehavior.NoStacking:
                // Clear all batches and create a single one with 1 stack
                StackBatches.Clear();
                float duration = def.Decay == DecayBehavior.Time ? def.Duration : float.MaxValue;
                StackBatches.Add(new StackBatch(1, duration));
                TimeSinceLastTick = 0;
                break;
        }
    }

    /// <summary>
    /// Remove stacks from this effect (removes from oldest batches first)
    /// </summary>
    public void RemoveStacks(int amount)
    {
        int remaining = amount;
        for (int i = 0; i < StackBatches.Count && remaining > 0; i++)
        {
            if (StackBatches[i].Stacks <= remaining)
            {
                remaining -= StackBatches[i].Stacks;
                StackBatches[i].Stacks = 0;
            }
            else
            {
                StackBatches[i].Stacks -= remaining;
                remaining = 0;
            }
        }
        // Remove empty batches
        StackBatches.RemoveAll(b => b.Stacks <= 0);
        InvalidateStackCache();
    }

    /// <summary>
    /// Update timers (called each frame during combat).
    /// Handles time-based decay by decreasing duration on batches.
    /// Returns true if a tick should be processed.
    /// </summary>
    public bool UpdateTimers(float deltaTime)
    {
        var def = GetDefinition();
        if (def == null) return false;

        // Only process time-based decay
        if (def.Decay == DecayBehavior.Time)
        {
            for (int i = 0; i < StackBatches.Count; i++)
            {
                StackBatches[i].RemainingDuration -= deltaTime;
            }
            // Remove expired batches
            int removedCount = StackBatches.RemoveAll(b => b.RemainingDuration <= 0);
            if (removedCount > 0) InvalidateStackCache();
        }

        // Update tick timer
        TimeSinceLastTick += deltaTime;

        // Check if we should tick (only if we still have stacks)
        if (CurrentStacks > 0 && def.TickInterval > 0 && TimeSinceLastTick >= def.TickInterval)
        {
            TimeSinceLastTick -= def.TickInterval;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Process on-tick decay - removes one stack when the effect ticks.
    /// Call this after processing the tick damage/heal.
    /// </summary>
    public void ProcessOnTickDecay()
    {
        var def = GetDefinition();
        if (def == null || def.Decay != DecayBehavior.OnTick) return;

        RemoveStacks(1);
    }

    /// <summary>
    /// Process on-hit decay - removes one stack when the target takes damage.
    /// Call this from damage processing.
    /// </summary>
    public void ProcessOnHitDecay()
    {
        var def = GetDefinition();
        if (def == null || def.Decay != DecayBehavior.OnHit) return;

        RemoveStacks(1);
    }

    /// <summary>
    /// Check if any stacks expired this frame (call after UpdateTimers)
    /// Returns the number of stacks that expired
    /// </summary>
    public int GetExpiredStackCount(int previousStackCount)
    {
        return Mathf.Max(0, previousStackCount - CurrentStacks);
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
        StackBatches.Clear();
        TimeSinceLastTick = 0;
        _cachedDefinition = null;
        InvalidateStackCache();
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
        var clone = new ActiveStatusEffect
        {
            EffectId = this.EffectId,
            TimeSinceLastTick = this.TimeSinceLastTick,
            _cachedDefinition = this._cachedDefinition
        };

        // Deep copy stack batches
        foreach (var batch in this.StackBatches)
        {
            clone.StackBatches.Add(new StackBatch(batch.Stacks, batch.RemainingDuration));
        }

        return clone;
    }

    public override string ToString()
    {
        var def = GetDefinition();
        string name = def?.GetDisplayName() ?? EffectId;

        if (!IsActive)
            return $"[{name}: Inactive]";

        if (def?.Duration > 0)
            return $"[{name}: {CurrentStacks} stacks in {StackBatches.Count} batches, oldest expires in {RemainingDuration:F1}s]";
        else
            return $"[{name}: {CurrentStacks} stacks]";
    }
}
