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
    [SerializeField] private int weightsPerRow = 6;
    [SerializeField] private int padding = 5;
    [SerializeField] private float heightRatio = 1.0f; // Height as ratio of width (1.0 = square, 2.0 = combat style)

    [Header("Empty Slots")]
    [SerializeField] private GameObject emptySlotPrefab; // Prefab for empty slot (1:2 ratio)
    [SerializeField] private Color emptySlotColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Fallback if no prefab

    private RectTransform rectTransform;
    private List<GameObject> abilityDisplays = new List<GameObject>();
    private List<GameObject> emptySlots = new List<GameObject>();
    private List<GameObject> rows = new List<GameObject>();
    private List<float> rowWeightsUsed = new List<float>();

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

        // Note: If scrolling is needed, add ContentSizeFitter manually in editor
        // Don't add it here to avoid resizing the container at runtime
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

        // Calculate layout dimensions
        float availableWidth = rectTransform.rect.width - (padding * 2);
        if (availableWidth <= 0)
        {
            availableWidth = 300f - (padding * 2); // Fallback
        }

        float baseWidth = (availableWidth - (spacing * (weightsPerRow - 1))) / weightsPerRow;
        float rowHeight = baseWidth * heightRatio;

        if (AbilityManager.Instance == null)
        {
            // Create one empty row even with no abilities
            CreateRowWithEmptySlots(rowHeight, baseWidth, weightsPerRow);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            Canvas.ForceUpdateCanvases();
            return;
        }

        var ownedAbilities = AbilityManager.Instance.GetOwnedAbilities();

        if (ownedAbilities == null || ownedAbilities.Count == 0)
        {
            // Create one empty row
            CreateRowWithEmptySlots(rowHeight, baseWidth, weightsPerRow);
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            Canvas.ForceUpdateCanvases();
            return;
        }

        // First pass: calculate how many rows we need and assign abilities
        List<List<AbilityDefinition>> rowAssignments = new List<List<AbilityDefinition>>();
        List<int> rowWeightTotals = new List<int>();

        for (int i = 0; i < ownedAbilities.Count; i++)
        {
            var ability = ownedAbilities[i];
            if (ability == null) continue;

            int abilityWeight = ability.Weight > 0 ? ability.Weight : 1;

            // Find a row with space or create new
            int targetRow = -1;
            for (int r = 0; r < rowAssignments.Count; r++)
            {
                if (weightsPerRow - rowWeightTotals[r] >= abilityWeight)
                {
                    targetRow = r;
                    break;
                }
            }

            if (targetRow < 0)
            {
                rowAssignments.Add(new List<AbilityDefinition>());
                rowWeightTotals.Add(0);
                targetRow = rowAssignments.Count - 1;
            }

            rowAssignments[targetRow].Add(ability);
            rowWeightTotals[targetRow] += abilityWeight;
        }

        // Ensure at least one row
        if (rowAssignments.Count == 0)
        {
            rowAssignments.Add(new List<AbilityDefinition>());
            rowWeightTotals.Add(0);
        }

        // Create rows: abilities first, then empty slots at the end
        for (int r = 0; r < rowAssignments.Count; r++)
        {
            GameObject row = CreateRow(rowHeight);
            rows.Add(row);
            rowWeightsUsed.Add(rowWeightTotals[r]);

            // Create abilities first
            foreach (var ability in rowAssignments[r])
            {
                int abilityIndex = ownedAbilities.IndexOf(ability);
                GameObject abilityObj = CreateAbilityDisplay(ability, abilityIndex, row.transform, baseWidth, rowHeight);
                if (abilityObj != null)
                {
                    abilityDisplays.Add(abilityObj);
                    displayToAbilityId[abilityObj] = ability.AbilityID;
                }
            }

            // Then add empty slots to fill remaining space
            int remainingWeight = weightsPerRow - rowWeightTotals[r];
            for (int w = 0; w < remainingWeight; w++)
            {
                GameObject emptySlot = CreateEmptySlot(row.transform, baseWidth, rowHeight);
                emptySlots.Add(emptySlot);
            }
        }

        // Force layout update
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        Canvas.ForceUpdateCanvases();
    }

    /// <summary>
    /// Create a row filled with empty slots (used when no abilities)
    /// </summary>
    private void CreateRowWithEmptySlots(float rowHeight, float baseWidth, int slotCount)
    {
        GameObject row = CreateRow(rowHeight);
        rows.Add(row);
        rowWeightsUsed.Add(0f);

        for (int i = 0; i < slotCount; i++)
        {
            GameObject emptySlot = CreateEmptySlot(row.transform, baseWidth, rowHeight);
            emptySlots.Add(emptySlot);
        }
    }

    /// <summary>
    /// Create an empty slot background
    /// </summary>
    private GameObject CreateEmptySlot(Transform parent, float baseWidth, float rowHeight)
    {
        GameObject slot;

        if (emptySlotPrefab != null)
        {
            slot = Instantiate(emptySlotPrefab, parent);
        }
        else
        {
            // Fallback: create simple slot
            slot = new GameObject("EmptySlot", typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(parent, false);

            var image = slot.GetComponent<Image>();
            image.color = emptySlotColor;
        }

        RectTransform rect = slot.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(baseWidth, rowHeight);

        return slot;
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

        foreach (var slot in emptySlots)
        {
            if (slot != null)
                Destroy(slot);
        }
        emptySlots.Clear();

        foreach (var row in rows)
        {
            if (row != null)
                Destroy(row);
        }
        rows.Clear();
        rowWeightsUsed.Clear();
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
