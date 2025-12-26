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
    [SerializeField] private int fixedRowCount = 4; // Always show this many rows
    [SerializeField] private int paddingLeft = 5;
    [SerializeField] private int paddingRight = 5;
    [SerializeField] private int paddingTop = 5;
    [SerializeField] private int paddingBottom = 5;
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
        verticalLayout.padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);

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

        // Update VerticalLayoutGroup padding in case it changed in Inspector
        var verticalLayout = GetComponent<VerticalLayoutGroup>();
        if (verticalLayout != null)
        {
            verticalLayout.padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
            verticalLayout.spacing = spacing;
        }

        // Force layout update to get accurate rect width
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        // Calculate layout dimensions
        // Get width from parent (Viewport) if available, otherwise use own width
        float containerWidth = rectTransform.rect.width;

        // If parent exists (e.g., Viewport), use parent's width as constraint
        if (rectTransform.parent != null)
        {
            RectTransform parentRect = rectTransform.parent as RectTransform;
            if (parentRect != null && parentRect.rect.width > 0)
            {
                containerWidth = parentRect.rect.width;
            }
        }

        if (containerWidth <= 0)
        {
            containerWidth = 300f; // Fallback
        }
        float availableWidth = containerWidth - paddingLeft - paddingRight;

        // Base width calculation: total available space minus all gaps between items, divided by number of weight units
        // Total spacing between items = (weightsPerRow - 1) * spacing
        float totalSpacing = (weightsPerRow - 1) * spacing;
        // Floor to avoid floating point overflow causing items to exceed row width
        float baseWidth = Mathf.Floor((availableWidth - totalSpacing) / weightsPerRow);
        float rowHeight = Mathf.Floor(baseWidth * heightRatio);

        var ownedAbilities = AbilityManager.Instance?.GetOwnedAbilities() ?? new List<AbilityDefinition>();

        // Filter out equipped abilities - only show unequipped ones in inventory
        var unequippedAbilities = new List<AbilityDefinition>();
        foreach (var ability in ownedAbilities)
        {
            if (ability != null && AbilityManager.Instance != null && !AbilityManager.Instance.IsAbilityEquipped(ability.AbilityID))
            {
                unequippedAbilities.Add(ability);
            }
        }

        // Initialize fixed number of rows with their weight tracking
        List<List<AbilityDefinition>> rowAssignments = new List<List<AbilityDefinition>>();
        int[] rowWeightTotals = new int[fixedRowCount];
        for (int r = 0; r < fixedRowCount; r++)
        {
            rowAssignments.Add(new List<AbilityDefinition>());
        }

        // Assign abilities to rows
        for (int i = 0; i < unequippedAbilities.Count; i++)
        {
            var ability = unequippedAbilities[i];
            if (ability == null) continue;

            int abilityWeight = ability.Weight > 0 ? ability.Weight : 1;

            // Find a row with space
            int targetRow = -1;
            for (int r = 0; r < fixedRowCount; r++)
            {
                if (weightsPerRow - rowWeightTotals[r] >= abilityWeight)
                {
                    targetRow = r;
                    break;
                }
            }

            if (targetRow >= 0)
            {
                rowAssignments[targetRow].Add(ability);
                rowWeightTotals[targetRow] += abilityWeight;
            }
            // If no space, ability won't be displayed (shouldn't happen with enough rows)
        }

        // Create all fixed rows: abilities first, then empty slots
        for (int r = 0; r < fixedRowCount; r++)
        {
            GameObject row = CreateRow(rowHeight);
            rows.Add(row);
            rowWeightsUsed.Add(rowWeightTotals[r]);

            // Create abilities first
            foreach (var ability in rowAssignments[r])
            {
                int abilityIndex = unequippedAbilities.IndexOf(ability);
                GameObject abilityObj = CreateAbilityDisplay(ability, abilityIndex, row.transform, baseWidth, rowHeight);
                if (abilityObj != null)
                {
                    abilityDisplays.Add(abilityObj);
                    displayToAbilityId[abilityObj] = ability.AbilityID;
                }
            }

            // Fill remaining space with empty slots
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

        // Reset scroll position to top
        ScrollRect scrollRect = GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f; // 1 = top, 0 = bottom
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
        layout.padding = new RectOffset(0, 0, 0, 0); // No padding on rows
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
        layoutElement.flexibleHeight = 0; // Don't expand

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

        // Setup CombatAbilityUI if present (reusing combat prefab)
        var combatAbilityUI = abilityObj.GetComponent<CombatAbilityUI>();
        if (combatAbilityUI != null)
        {
            combatAbilityUI.Setup(ability, index, true, 0);
            // Disable cooldown overlay for inventory display (also enables dragging)
            combatAbilityUI.HideCooldownOverlay();

            // Click handler will be handled by CombatAbilityUI to show AbilityActionPanel
        }
        else
        {
            // Fallback: add button and setup visuals if no CombatAbilityUI
            var image = abilityObj.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = ability.AbilityIcon;
                image.color = ability.AbilityColor;
            }

            var button = abilityObj.GetComponent<Button>();
            if (button == null)
            {
                button = abilityObj.AddComponent<Button>();
            }
            string capturedAbilityId = ability.AbilityID;
            button.onClick.AddListener(() => OnAbilityClicked(capturedAbilityId));
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
