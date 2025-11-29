// Purpose: Displays status effects (poison, etc.) in a status bar during combat
// Filepath: Assets/Scripts/UI/Combat/StatusEffectUI.cs

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays status effects for a combatant.
/// Controlled by CombatPanelUI - no event subscriptions, just public methods.
/// Instantiates status icons as children of this transform.
/// </summary>
public class StatusEffectUI : MonoBehaviour
{
    private Dictionary<StatusEffectType, GameObject> prefabs;
    private Dictionary<StatusEffectType, ActiveStatusEffect> activeEffects = new Dictionary<StatusEffectType, ActiveStatusEffect>();

    private class ActiveStatusEffect
    {
        public GameObject instance;
        public TextMeshProUGUI stackText;
        public float stacks;
    }

    /// <summary>
    /// Initialize with prefabs from CombatPanelUI
    /// </summary>
    public void Initialize(Dictionary<StatusEffectType, GameObject> statusPrefabs)
    {
        prefabs = statusPrefabs;
    }

    /// <summary>
    /// Add stacks to a status effect
    /// </summary>
    public void AddEffect(StatusEffectType type, float amount)
    {
        if (activeEffects.TryGetValue(type, out var effect))
        {
            effect.stacks += amount;
            UpdateStackText(effect);
        }
        else
        {
            CreateEffect(type, amount);
        }
    }

    /// <summary>
    /// Update stacks for a status effect (set exact value)
    /// </summary>
    public void UpdateEffect(StatusEffectType type, float stacks)
    {
        if (stacks <= 0)
        {
            RemoveEffect(type);
            return;
        }

        if (activeEffects.TryGetValue(type, out var effect))
        {
            effect.stacks = stacks;
            UpdateStackText(effect);
        }
        else
        {
            CreateEffect(type, stacks);
        }
    }

    /// <summary>
    /// Remove a status effect
    /// </summary>
    public void RemoveEffect(StatusEffectType type)
    {
        if (activeEffects.TryGetValue(type, out var effect))
        {
            if (effect.instance != null)
            {
                Destroy(effect.instance);
            }
            activeEffects.Remove(type);
        }
    }

    /// <summary>
    /// Clear all status effects
    /// </summary>
    public void ClearAll()
    {
        foreach (var kvp in activeEffects)
        {
            if (kvp.Value.instance != null)
            {
                Destroy(kvp.Value.instance);
            }
        }
        activeEffects.Clear();
    }

    private void CreateEffect(StatusEffectType type, float stacks)
    {
        if (prefabs == null || !prefabs.TryGetValue(type, out var prefab) || prefab == null)
        {
            Logger.LogWarning($"StatusEffectUI: No prefab for {type}", Logger.LogCategory.General);
            return;
        }

        var instance = Instantiate(prefab, transform);

        var effect = new ActiveStatusEffect
        {
            instance = instance,
            stackText = instance.GetComponentInChildren<TextMeshProUGUI>(),
            stacks = stacks
        };

        UpdateStackText(effect);
        activeEffects[type] = effect;
    }

    private void UpdateStackText(ActiveStatusEffect effect)
    {
        if (effect.stackText != null)
        {
            effect.stackText.text = Mathf.CeilToInt(effect.stacks).ToString();
            effect.stackText.ForceMeshUpdate();
        }
    }
}
