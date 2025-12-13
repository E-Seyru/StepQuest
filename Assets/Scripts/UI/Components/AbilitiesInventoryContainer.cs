// Purpose: Container for displaying owned abilities with weight-based layout (no weight limit)
// Filepath: Assets/Scripts/UI/Components/AbilitiesInventoryContainer.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays owned abilities in a weight-based grid layout (same as combat).
/// No weight limit - creates as many rows as needed.
/// Click or drag to equip abilities.
/// </summary>
public class AbilitiesInventoryContainer : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private GameObject abilityDisplayPrefab; // CombatAbilityUI prefab

    [Header("Layout Settings")]
    [SerializeField] private float spacing = 5f;
    [SerializeField] private int weightsPerRow = 10; // Higher = smaller abilities (10 makes them ~60% smaller than 6)
    [SerializeField] private int padding = 5;
    [SerializeField] private float heightRatio = 1.0f; // Height as ratio of width (1.0 = square, 2.0 = combat style)

    private RectTransform rectTransform;
    private List<GameObject> abilityDisplays = new List<GameObject>();
    private Dictionary<GameObject, float> rowWeights = new Dictionary<GameObject, float>();

    // Track ability instances for click handling
    private Dictionary<GameObject, string> displayToAbilityId = new Dictionary<GameObject, string>();

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Ensure VerticalLayoutGroup exists
        var verticalLayout = GetComponent<VerticalLayoutGroup>();
        if (verticalLayout == null)
        {
            verticalLayout = gameObject.AddComponent<VerticalLayoutGroup>();
        }
        verticalLayout.spacing = spacing;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.childControlWidth = true;
        verticalLayout.childControlHeight = false;
        verticalLayout.padding = new RectOffset(padding, padding, padding, padding);

        // Add ContentSizeFitter for scrolling
        var sizeFitter = GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = gameObject.AddComponent<ContentSizeFitter>();
        }
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void Start()
    {
        // Subscribe to ability changes
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnOwnedAbilitiesChanged += RefreshDisplay;
            AbilityManager.Instance.OnEquippedAbilitiesChanged += RefreshDisplay; // To update equipped status
        }

        // Initial display
        RefreshDisplay();
    }

    private void OnDestroy()
    {
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnOwnedAbilitiesChanged -= RefreshDisplay;
            AbilityManager.Instance.OnEquippedAbilitiesChanged -= RefreshDisplay;
        }
    }

    /// <summary>
    /// Refresh the display of owned abilities
    /// </summary>
    public void RefreshDisplay()
    {
        ClearDisplays();

        if (AbilityManager.Instance == null) return;

        var ownedAbilities = AbilityManager.Instance.GetOwnedAbilities();

        if (ownedAbilities == null || ownedAbilities.Count == 0) return;

        // Calculate layout dimensions
        float availableWidth = rectTransform.rect.width - (padding * 2);
        if (availableWidth <= 0)
        {
            availableWidth = 300f - (padding * 2); // Fallback
        }

        float baseWidth = (availableWidth - (spacing * (weightsPerRow - 1))) / weightsPerRow;
        float rowHeight = baseWidth * heightRatio;

        // Place abilities in rows based on weight (no limit on rows)
        for (int i = 0; i < ownedAbilities.Count; i++)
        {
            var ability = ownedAbilities[i];
            if (ability == null) continue;

            int abilityWeight = ability.Weight > 0 ? ability.Weight : 1;

            // Find or create a row with enough space
            GameObject targetRow = FindOrCreateRow(abilityWeight, rowHeight);

            // Create the ability display
            GameObject abilityObj = CreateAbilityDisplay(ability, i, targetRow.transform, baseWidth, rowHeight);
            if (abilityObj != null)
            {
                abilityDisplays.Add(abilityObj);
                displayToAbilityId[abilityObj] = ability.AbilityID;
                rowWeights[targetRow] += abilityWeight;
            }
        }

        // Force layout update
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        Canvas.ForceUpdateCanvases();
    }

    /// <summary>
    /// Find a row with enough space, or create a new one (no row limit)
    /// </summary>
    private GameObject FindOrCreateRow(int abilityWeight, float rowHeight)
    {
        // Try to fit in existing rows (top to bottom)
        foreach (var rowEntry in rowWeights)
        {
            float remainingSpace = weightsPerRow - rowEntry.Value;
            if (remainingSpace >= abilityWeight)
            {
                return rowEntry.Key;
            }
        }

        // Create new row (no limit)
        GameObject newRow = CreateRow(rowHeight);
        rowWeights.Add(newRow, 0f);
        return newRow;
    }

    private GameObject CreateRow(float rowHeight)
    {
        GameObject row = new GameObject("AbilityRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(transform, false);

        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childAlignment = TextAnchor.UpperLeft;

        RectTransform rect = row.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.sizeDelta = new Vector2(0, rowHeight);

        // Add LayoutElement for proper sizing
        var layoutElement = row.AddComponent<LayoutElement>();
        layoutElement.minHeight = rowHeight;
        layoutElement.preferredHeight = rowHeight;

        return row;
    }

    private GameObject CreateAbilityDisplay(AbilityDefinition ability, int index, Transform parent, float baseWidth, float rowHeight)
    {
        GameObject abilityObj;

        if (abilityDisplayPrefab != null)
        {
            abilityObj = Instantiate(abilityDisplayPrefab, parent);
        }
        else
        {
            // Create simple visual if no prefab
            abilityObj = CreateSimpleAbilityDisplay(ability, parent);
        }

        // Setup click handler for equip
        var button = abilityObj.GetComponent<Button>();
        if (button == null)
        {
            button = abilityObj.AddComponent<Button>();
        }
        string capturedAbilityId = ability.AbilityID;
        button.onClick.AddListener(() => OnAbilityClicked(capturedAbilityId));

        // Setup CombatAbilityUI if present (reusing combat prefab)
        var combatAbilityUI = abilityObj.GetComponent<CombatAbilityUI>();
        if (combatAbilityUI != null)
        {
            combatAbilityUI.Setup(ability, index, true, 0);
            // Disable cooldown overlay for inventory display
            combatAbilityUI.HideCooldownOverlay();
        }
        else
        {
            // Simple visual setup
            var image = abilityObj.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = ability.AbilityIcon;
                image.color = ability.AbilityColor;
            }
        }

        // Calculate size based on weight
        RectTransform abilityRect = abilityObj.GetComponent<RectTransform>();
        if (abilityRect != null)
        {
            int weight = ability.Weight > 0 ? ability.Weight : 1;
            float width = weight * baseWidth + (weight - 1) * spacing;
            abilityRect.sizeDelta = new Vector2(width, rowHeight);
        }

        return abilityObj;
    }

    private GameObject CreateSimpleAbilityDisplay(AbilityDefinition ability, Transform parent)
    {
        GameObject obj = new GameObject("AbilityDisplay", typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);

        var image = obj.GetComponent<Image>();
        image.sprite = ability.AbilityIcon;
        image.color = ability.AbilityColor;

        return obj;
    }

    private void ClearDisplays()
    {
        foreach (var display in abilityDisplays)
        {
            if (display != null)
                Destroy(display);
        }
        abilityDisplays.Clear();
        displayToAbilityId.Clear();

        foreach (var row in rowWeights.Keys)
        {
            if (row != null)
                Destroy(row);
        }
        rowWeights.Clear();
    }

    /// <summary>
    /// Handle click on an owned ability (equip it)
    /// </summary>
    private void OnAbilityClicked(string abilityId)
    {
        if (AbilityManager.Instance == null) return;

        // If already equipped, unequip it
        if (AbilityManager.Instance.IsAbilityEquipped(abilityId))
        {
            AbilityManager.Instance.TryUnequipAbility(abilityId);
        }
        else
        {
            // Try to equip
            AbilityManager.Instance.TryEquipAbility(abilityId);
        }
    }

    // === DEBUG ===

    [ContextMenu("Debug: Refresh Display")]
    public void DebugRefresh()
    {
        RefreshDisplay();
    }
}
