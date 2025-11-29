// Purpose: Displays abilities during combat with weight-based layout
// Filepath: Assets/Scripts/UI/Combat/CombatAbilityDisplay.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the display of abilities during combat.
/// Creates ability UI elements dynamically based on ability weight.
/// </summary>
public class CombatAbilityDisplay : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject abilityPrefab;

    [Header("Layout Settings")]
    [SerializeField] private float maxHeight = 150f;
    [SerializeField] private float spacing = 5f;
    [SerializeField] private int weightsPerRow = 6;
    [SerializeField] private int padding = 10;

    private RectTransform rectTransform;
    private List<GameObject> currentAbilityDisplays = new List<GameObject>();
    private Dictionary<GameObject, float> rowWeights = new Dictionary<GameObject, float>();

    // Ability UI references for cooldown tracking
    private struct AbilityKey
    {
        public AbilityDefinition ability;
        public int index;

        public AbilityKey(AbilityDefinition ability, int index)
        {
            this.ability = ability;
            this.index = index;
        }
    }

    private Dictionary<AbilityKey, CombatAbilityUI> abilityUIReferences = new Dictionary<AbilityKey, CombatAbilityUI>();

    // Track duplicate ability counts for correct event matching
    private Dictionary<AbilityDefinition, int> abilityDuplicateCounts = new Dictionary<AbilityDefinition, int>();

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// Get the UI component for a specific ability instance
    /// </summary>
    public CombatAbilityUI GetAbilityUI(AbilityDefinition ability, int instanceIndex = 0)
    {
        foreach (var kvp in abilityUIReferences)
        {
            if (kvp.Key.ability == ability && kvp.Key.index == instanceIndex)
            {
                return kvp.Value;
            }
        }

        // Fallback: try to find any instance of this ability
        foreach (var kvp in abilityUIReferences)
        {
            if (kvp.Key.ability == ability)
            {
                return kvp.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Display a list of abilities in the combat panel
    /// </summary>
    public void DisplayCombatAbilities(List<AbilityDefinition> abilities, bool isPlayer)
    {
        ClearDisplays();

        if (abilities == null || abilities.Count == 0) return;

        // Reset duplicate counts for this display session
        abilityDuplicateCounts.Clear();

        float availableWidth = rectTransform.rect.width - (padding * 2);
        if (availableWidth <= 0)
        {
            // Fallback if rect not ready
            availableWidth = 300f;
        }

        float baseWidth = (availableWidth - (spacing * (weightsPerRow - 1))) / weightsPerRow;

        for (int i = 0; i < abilities.Count; i++)
        {
            var ability = abilities[i];
            if (ability == null) continue;

            // Calculate duplicate index for this ability (matches CombatManager logic)
            int duplicateIndex = GetAndIncrementDuplicateCount(ability);

            // Find or create a row with enough space
            GameObject targetRow = null;
            int abilityWeight = ability.Weight > 0 ? ability.Weight : 1;

            foreach (var rowEntry in rowWeights)
            {
                if (rowEntry.Value + abilityWeight <= weightsPerRow)
                {
                    targetRow = rowEntry.Key;
                    break;
                }
            }

            if (targetRow == null)
            {
                targetRow = CreateRow();
                rowWeights.Add(targetRow, 0f);
            }

            // Create the ability display with correct duplicate index
            GameObject abilityObj = CreateAbilityDisplay(ability, i, isPlayer, targetRow.transform, baseWidth, duplicateIndex);
            currentAbilityDisplays.Add(abilityObj);
            rowWeights[targetRow] += abilityWeight;
        }

        // Force layout update
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        Canvas.ForceUpdateCanvases();
    }

    private int GetAndIncrementDuplicateCount(AbilityDefinition ability)
    {
        if (!abilityDuplicateCounts.ContainsKey(ability))
        {
            abilityDuplicateCounts[ability] = 0;
        }
        int currentCount = abilityDuplicateCounts[ability];
        abilityDuplicateCounts[ability]++;
        return currentCount;
    }

    private GameObject CreateRow()
    {
        GameObject row = new GameObject("AbilityRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(transform, false);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.padding = new RectOffset(padding, padding, 0, 0);

        RectTransform rect = row.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.sizeDelta = new Vector2(0, maxHeight);

        return row;
    }

    private GameObject CreateAbilityDisplay(AbilityDefinition ability, int index, bool isPlayer, Transform parent, float baseWidth, int duplicateIndex = 0)
    {
        if (abilityPrefab == null)
        {
            Logger.LogError("CombatAbilityDisplay: abilityPrefab is not assigned!", Logger.LogCategory.General);
            return null;
        }

        GameObject abilityObj = Instantiate(abilityPrefab, parent);
        CombatAbilityUI abilityUI = abilityObj.GetComponent<CombatAbilityUI>();

        if (abilityUI != null)
        {
            abilityUI.Setup(ability, index, isPlayer, duplicateIndex);

            // Store reference
            var key = new AbilityKey(ability, index);
            abilityUIReferences[key] = abilityUI;
        }

        // Calculate size based on weight - use fixed height for consistency
        RectTransform abilityRect = abilityObj.GetComponent<RectTransform>();
        if (abilityRect != null)
        {
            int weight = ability.Weight > 0 ? ability.Weight : 1;
            float width = weight * baseWidth + (weight - 1) * spacing;
            // Use fixed maxHeight for all abilities so they have consistent size
            float height = maxHeight;

            abilityRect.sizeDelta = new Vector2(width, height);
        }

        return abilityObj;
    }

    /// <summary>
    /// Clear all ability displays
    /// </summary>
    public void ClearDisplays()
    {
        foreach (var display in currentAbilityDisplays)
        {
            if (display != null)
                Destroy(display);
        }
        currentAbilityDisplays.Clear();

        foreach (var row in rowWeights.Keys)
        {
            if (row != null)
                Destroy(row);
        }
        rowWeights.Clear();

        abilityUIReferences.Clear();
    }
}
