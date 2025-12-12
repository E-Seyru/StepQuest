// Purpose: Displays status effects in a status bar during combat
// Filepath: Assets/Scripts/UI/Combat/StatusEffectUI.cs

using CombatEvents;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays status effects for a combatant.
/// Uses event-driven updates via StatusEffectAppliedEvent and StatusEffectRemovedEvent.
/// Instantiates status icons as children of this transform.
/// </summary>
public class StatusEffectUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool isForPlayer = true;
    [SerializeField] private bool subscribeToEvents = true;
    [SerializeField] private GameObject defaultStatusEffectPrefab;

    // Prefab mapping by type (optional - for type-specific prefabs)
    private Dictionary<StatusEffectType, GameObject> _typePrefabs;

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
            EventBus.Unsubscribe<StatusEffectRemovedEvent>(OnStatusEffectRemoved);
            EventBus.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
            EventBus.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
        }
    }

    // === EVENT HANDLERS ===

    private void OnStatusEffectApplied(StatusEffectAppliedEvent evt)
    {
        if (evt.IsTargetPlayer != isForPlayer) return;
        if (evt.Effect == null) return;

        AddOrUpdateEffect(evt.Effect, evt.TotalStacks);
    }

    private void OnStatusEffectRemoved(StatusEffectRemovedEvent evt)
    {
        if (evt.IsTargetPlayer != isForPlayer) return;
        if (evt.Effect == null) return;

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

    // === PUBLIC API ===

    /// <summary>
    /// Initialize with optional type-specific prefabs
    /// </summary>
    public void Initialize(Dictionary<StatusEffectType, GameObject> statusPrefabs)
    {
        _typePrefabs = statusPrefabs;
    }

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

    // === PRIVATE METHODS ===

    private void CreateEffect(StatusEffectDefinition definition, int stacks)
    {
        if (definition == null || stacks <= 0) return;

        GameObject prefab = defaultStatusEffectPrefab;

        // Try to get type-specific prefab if available
        if (_typePrefabs != null && _typePrefabs.TryGetValue(definition.EffectType, out var typePrefab))
        {
            prefab = typePrefab;
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

    private void UpdateStackText(ActiveEffect effect)
    {
        if (effect.StackText != null)
        {
            effect.StackText.text = effect.Stacks.ToString();
            effect.StackText.ForceMeshUpdate();
        }
    }
}
