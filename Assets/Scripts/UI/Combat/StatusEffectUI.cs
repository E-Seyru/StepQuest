// Purpose: Displays status effects in a status bar during combat
// Filepath: Assets/Scripts/UI/Combat/StatusEffectUI.cs

using CombatEvents;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays status effects for a combatant.
/// Supports both event-driven updates (new system) and direct method calls (legacy).
/// Instantiates status icons as children of this transform.
/// </summary>
public class StatusEffectUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool isForPlayer = true;
    [SerializeField] private bool subscribeToEvents = true;
    [SerializeField] private GameObject defaultStatusEffectPrefab;

    // Legacy prefab mapping
    private Dictionary<StatusEffectType, GameObject> _legacyPrefabs;

    // Active effects tracking
    private Dictionary<string, ActiveEffect> _activeEffects = new Dictionary<string, ActiveEffect>();

    private class ActiveEffect
    {
        public string EffectId;
        public StatusEffectDefinition Definition;
        public GameObject Instance;
        public TextMeshProUGUI StackText;
        public Image IconImage;
        public int Stacks;
    }

    // === UNITY LIFECYCLE ===

    void Start()
    {
        if (subscribeToEvents)
        {
            EventBus.Subscribe<StatusEffectAppliedEvent>(OnStatusEffectApplied);
            EventBus.Subscribe<StatusEffectTickEvent>(OnStatusEffectTick);
            EventBus.Subscribe<StatusEffectRemovedEvent>(OnStatusEffectRemoved);
            EventBus.Subscribe<CombatStartedEvent>(OnCombatStarted);
            EventBus.Subscribe<CombatEndedEvent>(OnCombatEnded);
        }
    }

    void OnDestroy()
    {
        if (subscribeToEvents)
        {
            EventBus.Unsubscribe<StatusEffectAppliedEvent>(OnStatusEffectApplied);
            EventBus.Unsubscribe<StatusEffectTickEvent>(OnStatusEffectTick);
            EventBus.Unsubscribe<StatusEffectRemovedEvent>(OnStatusEffectRemoved);
            EventBus.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
            EventBus.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
        }
    }

    // === EVENT HANDLERS ===

    private void OnStatusEffectApplied(StatusEffectAppliedEvent evt)
    {
        Debug.Log($"StatusEffectUI: Received apply event for {evt.Effect?.EffectID}, IsTargetPlayer={evt.IsTargetPlayer}, isForPlayer={isForPlayer}");

        if (evt.IsTargetPlayer != isForPlayer) return;
        if (evt.Effect == null) return;

        Debug.Log($"StatusEffectUI: Adding/updating effect {evt.Effect.EffectID} with {evt.TotalStacks} stacks");
        AddOrUpdateEffect(evt.Effect, evt.TotalStacks);
    }

    private void OnStatusEffectTick(StatusEffectTickEvent evt)
    {
        if (evt.IsTargetPlayer != isForPlayer) return;
        if (evt.Effect == null) return;

        UpdateEffectStacks(evt.Effect.EffectID, evt.RemainingStacks);
    }

    private void OnStatusEffectRemoved(StatusEffectRemovedEvent evt)
    {
        Debug.Log($"StatusEffectUI: Received removal event for {evt.Effect?.EffectID}, IsTargetPlayer={evt.IsTargetPlayer}, isForPlayer={isForPlayer}");

        if (evt.IsTargetPlayer != isForPlayer) return;
        if (evt.Effect == null) return;

        Debug.Log($"StatusEffectUI: Removing effect {evt.Effect.EffectID}, activeEffects contains: {string.Join(", ", _activeEffects.Keys)}");
        RemoveEffect(evt.Effect.EffectID);
    }

    private void OnCombatStarted(CombatStartedEvent evt)
    {
        ClearAll();
    }

    private void OnCombatEnded(CombatEndedEvent evt)
    {
        ClearAll();
    }

    // === PUBLIC API (New System) ===

    /// <summary>
    /// Add or update an effect using StatusEffectDefinition
    /// </summary>
    public void AddOrUpdateEffect(StatusEffectDefinition definition, int stacks)
    {
        if (definition == null || stacks <= 0) return;

        string effectId = definition.EffectID;

        if (_activeEffects.TryGetValue(effectId, out var existing))
        {
            existing.Stacks = stacks;
            UpdateStackText(existing);
        }
        else
        {
            CreateEffect(definition, stacks);
        }
    }

    /// <summary>
    /// Update stacks for an effect by ID
    /// </summary>
    public void UpdateEffectStacks(string effectId, int stacks)
    {
        if (string.IsNullOrEmpty(effectId)) return;

        if (stacks <= 0)
        {
            RemoveEffect(effectId);
            return;
        }

        if (_activeEffects.TryGetValue(effectId, out var effect))
        {
            effect.Stacks = stacks;
            UpdateStackText(effect);
        }
    }

    /// <summary>
    /// Remove an effect by ID
    /// </summary>
    public void RemoveEffect(string effectId)
    {
        if (string.IsNullOrEmpty(effectId)) return;

        if (_activeEffects.TryGetValue(effectId, out var effect))
        {
            if (effect.Instance != null)
            {
                Destroy(effect.Instance);
            }
            _activeEffects.Remove(effectId);
        }
    }

    /// <summary>
    /// Clear all status effects
    /// </summary>
    public void ClearAll()
    {
        foreach (var kvp in _activeEffects)
        {
            if (kvp.Value.Instance != null)
            {
                Destroy(kvp.Value.Instance);
            }
        }
        _activeEffects.Clear();
    }

    // === LEGACY API (Backwards Compatibility) ===

    /// <summary>
    /// Initialize with prefabs from CombatPanelUI (legacy)
    /// </summary>
    public void Initialize(Dictionary<StatusEffectType, GameObject> statusPrefabs)
    {
        _legacyPrefabs = statusPrefabs;
    }

    /// <summary>
    /// Add stacks to a status effect (legacy - by type)
    /// </summary>
    public void AddEffect(StatusEffectType type, float amount)
    {
        // Try to find effect in registry first
        var registry = StatusEffectRegistry.Instance;
        if (registry != null)
        {
            var effects = registry.GetEffectsByType(type);
            if (effects.Count > 0)
            {
                var definition = effects[0];
                int stacks = Mathf.CeilToInt(amount);

                if (_activeEffects.TryGetValue(definition.EffectID, out var existing))
                {
                    existing.Stacks += stacks;
                    UpdateStackText(existing);
                }
                else
                {
                    CreateEffect(definition, stacks);
                }
                return;
            }
        }

        // Fallback to legacy prefab system
        AddLegacyEffect(type, amount);
    }

    /// <summary>
    /// Update stacks for a status effect (legacy - by type)
    /// </summary>
    public void UpdateEffect(StatusEffectType type, float stacks)
    {
        if (stacks <= 0)
        {
            RemoveEffect(type);
            return;
        }

        // Try to find effect in registry first
        var registry = StatusEffectRegistry.Instance;
        if (registry != null)
        {
            var effects = registry.GetEffectsByType(type);
            if (effects.Count > 0)
            {
                var definition = effects[0];
                if (_activeEffects.TryGetValue(definition.EffectID, out var existing))
                {
                    existing.Stacks = Mathf.CeilToInt(stacks);
                    UpdateStackText(existing);
                    return;
                }
                else
                {
                    CreateEffect(definition, Mathf.CeilToInt(stacks));
                    return;
                }
            }
        }

        // Fallback to legacy system
        UpdateLegacyEffect(type, stacks);
    }

    /// <summary>
    /// Remove a status effect (legacy - by type)
    /// </summary>
    public void RemoveEffect(StatusEffectType type)
    {
        // Find any effect with this type and remove it
        string effectIdToRemove = null;
        foreach (var kvp in _activeEffects)
        {
            if (kvp.Value.Definition != null && kvp.Value.Definition.EffectType == type)
            {
                effectIdToRemove = kvp.Key;
                break;
            }
        }

        if (effectIdToRemove != null)
        {
            RemoveEffect(effectIdToRemove);
            return;
        }

        // Try legacy key
        string legacyKey = $"legacy_{type}";
        if (_activeEffects.ContainsKey(legacyKey))
        {
            RemoveEffect(legacyKey);
        }
    }

    // === PRIVATE METHODS ===

    private void CreateEffect(StatusEffectDefinition definition, int stacks)
    {
        if (definition == null || stacks <= 0) return;

        GameObject prefab = defaultStatusEffectPrefab;

        // Try to get legacy prefab if available
        if (_legacyPrefabs != null && _legacyPrefabs.TryGetValue(definition.EffectType, out var legacyPrefab))
        {
            prefab = legacyPrefab;
        }

        if (prefab == null)
        {
            Logger.LogWarning($"StatusEffectUI: No prefab for {definition.GetDisplayName()}", Logger.LogCategory.General);
            return;
        }

        var instance = Instantiate(prefab, transform);

        var effect = new ActiveEffect
        {
            EffectId = definition.EffectID,
            Definition = definition,
            Instance = instance,
            StackText = instance.GetComponentInChildren<TextMeshProUGUI>(),
            IconImage = instance.GetComponentInChildren<Image>(),
            Stacks = stacks
        };

        // Set icon and color from definition
        if (effect.IconImage != null && definition.EffectIcon != null)
        {
            effect.IconImage.sprite = definition.EffectIcon;
            effect.IconImage.color = definition.EffectColor;
        }

        UpdateStackText(effect);
        _activeEffects[definition.EffectID] = effect;
    }

    private void AddLegacyEffect(StatusEffectType type, float amount)
    {
        string legacyKey = $"legacy_{type}";

        if (_activeEffects.TryGetValue(legacyKey, out var existing))
        {
            existing.Stacks += Mathf.CeilToInt(amount);
            UpdateStackText(existing);
        }
        else
        {
            CreateLegacyEffect(type, Mathf.CeilToInt(amount));
        }
    }

    private void UpdateLegacyEffect(StatusEffectType type, float stacks)
    {
        string legacyKey = $"legacy_{type}";

        if (_activeEffects.TryGetValue(legacyKey, out var existing))
        {
            existing.Stacks = Mathf.CeilToInt(stacks);
            UpdateStackText(existing);
        }
        else
        {
            CreateLegacyEffect(type, Mathf.CeilToInt(stacks));
        }
    }

    private void CreateLegacyEffect(StatusEffectType type, int stacks)
    {
        if (_legacyPrefabs == null || !_legacyPrefabs.TryGetValue(type, out var prefab) || prefab == null)
        {
            // Try default prefab
            prefab = defaultStatusEffectPrefab;
            if (prefab == null)
            {
                Logger.LogWarning($"StatusEffectUI: No prefab for legacy {type}", Logger.LogCategory.General);
                return;
            }
        }

        var instance = Instantiate(prefab, transform);
        string legacyKey = $"legacy_{type}";

        var effect = new ActiveEffect
        {
            EffectId = legacyKey,
            Definition = null,
            Instance = instance,
            StackText = instance.GetComponentInChildren<TextMeshProUGUI>(),
            IconImage = instance.GetComponentInChildren<Image>(),
            Stacks = stacks
        };

        UpdateStackText(effect);
        _activeEffects[legacyKey] = effect;
    }

    private void UpdateStackText(ActiveEffect effect)
    {
        if (effect.StackText != null)
        {
            effect.StackText.text = effect.Stacks.ToString();
            effect.StackText.ForceMeshUpdate();
        }
    }
}
