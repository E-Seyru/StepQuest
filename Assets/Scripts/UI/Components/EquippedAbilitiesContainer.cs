// Purpose: Container component for displaying equipped abilities with weight-based layout
// Filepath: Assets/Scripts/UI/Components/EquippedAbilitiesContainer.cs

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Displays equipped abilities in a weight-based grid layout (2 rows x 6 weight).
/// Similar to CombatAbilityDisplay but for the equipment panel.
/// Supports drop to equip and click to unequip.
/// </summary>
public class EquippedAbilitiesContainer : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Prefab")]
    [SerializeField] private GameObject abilityDisplayPrefab;

    [Header("Layout Settings")]
    [SerializeField] private float spacing = 5f;
    [SerializeField] private int weightsPerRow = 6;
    [SerializeField] private int maxRows = 2;
    [SerializeField] private int padding = 5;
    [SerializeField] private float heightRatio = 1.0f; // Height per weight unit (1.0 = square, 2.0 = combat style)

    [Header("Weight Display")]
    [SerializeField] private TextMeshProUGUI weightText;

    [Header("Visual Feedback")]
    [SerializeField] private Image dropHighlight;
    [SerializeField] private Color validDropColor = new Color(0f, 1f, 0f, 0.3f);
    [SerializeField] private Color invalidDropColor = new Color(1f, 0f, 0f, 0.3f);

    [Header("Slot Prefabs")]
    [SerializeField] private GameObject emptySlotPrefab; // Prefab for empty slot (1:2 ratio)
    [SerializeField] private GameObject lockedSlotPrefab; // Prefab for locked slot (shows lock icon)
    [SerializeField] private Color emptySlotColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Fallback if no prefab

    [Header("Unlock System")]
    [SerializeField] private int defaultUnlockedWeight = 6; // Start with 1 row unlocked

    private RectTransform rectTransform;
    private List<GameObject> abilityDisplays = new List<GameObject>();
    private List<GameObject> emptySlots = new List<GameObject>();
    private List<GameObject> lockedSlots = new List<GameObject>();
    private List<GameObject> rows = new List<GameObject>();
    private int[] rowWeightUsed;

    // Track ability instances for click handling
    private Dictionary<GameObject, int> displayToIndex = new Dictionary<GameObject, int>();

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

        // Hide drop highlight initially
        if (dropHighlight != null)
        {
            dropHighlight.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        // Subscribe to ability changes
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnEquippedAbilitiesChanged += RefreshDisplay;
        }

        // Initial display
        RefreshDisplay();
    }

    private void OnDestroy()
    {
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnEquippedAbilitiesChanged -= RefreshDisplay;
        }
    }

    /// <summary>
    /// Refresh the display of equipped abilities
    /// </summary>
    public void RefreshDisplay()
    {
        ClearDisplays();

        int currentWeight = 0;
        int maxWeight = 12;

        if (AbilityManager.Instance != null)
        {
            currentWeight = AbilityManager.Instance.GetCurrentEquippedWeight();
            maxWeight = AbilityManager.Instance.MaxEquippedWeight;
        }

        // Update weight text
        if (weightText != null)
        {
            weightText.text = $"{currentWeight}/{maxWeight}";
        }

        // Calculate layout dimensions
        float availableWidth = rectTransform.rect.width - (padding * 2);
        if (availableWidth <= 0)
        {
            availableWidth = 300f - (padding * 2); // Fallback
        }

        float baseWidth = (availableWidth - (spacing * (weightsPerRow - 1))) / weightsPerRow;

        // Calculate row height from container height to prevent overflow
        float availableHeight = rectTransform.rect.height - (padding * 2) - (spacing * (maxRows - 1));
        float rowHeight = availableHeight / maxRows;
        if (rowHeight <= 0) rowHeight = baseWidth * heightRatio; // Fallback

        // Initialize row weight tracking
        rowWeightUsed = new int[maxRows];

        // First pass: assign abilities to rows
        List<List<int>> rowAbilityIndices = new List<List<int>>();
        for (int r = 0; r < maxRows; r++)
        {
            rowAbilityIndices.Add(new List<int>());
        }

        var equippedAbilities = AbilityManager.Instance?.GetEquippedAbilities();
        if (equippedAbilities != null)
        {
            for (int i = 0; i < equippedAbilities.Count; i++)
            {
                var ability = equippedAbilities[i];
                if (ability == null) continue;

                int abilityWeight = ability.Weight > 0 ? ability.Weight : 1;

                // Find a row with enough space
                int targetRow = -1;
                for (int r = 0; r < maxRows; r++)
                {
                    if (weightsPerRow - rowWeightUsed[r] >= abilityWeight)
                    {
                        targetRow = r;
                        break;
                    }
                }

                if (targetRow < 0)
                {
                    Logger.LogWarning($"EquippedAbilitiesContainer: No space for ability '{ability.GetDisplayName()}'", Logger.LogCategory.General);
                    continue;
                }

                rowAbilityIndices[targetRow].Add(i);
                rowWeightUsed[targetRow] += abilityWeight;
            }
        }

        // Create rows: abilities first, then empty slots
        for (int r = 0; r < maxRows; r++)
        {
            GameObject row = CreateRow(rowHeight);
            rows.Add(row);

            // Create abilities first
            if (equippedAbilities != null)
            {
                foreach (int abilityIndex in rowAbilityIndices[r])
                {
                    var ability = equippedAbilities[abilityIndex];
                    GameObject abilityObj = CreateAbilityDisplay(ability, abilityIndex, row.transform, baseWidth, rowHeight);
                    if (abilityObj != null)
                    {
                        abilityDisplays.Add(abilityObj);
                        displayToIndex[abilityObj] = abilityIndex;
                    }
                }
            }

            // Calculate unlocked and locked slots for this row
            int unlockedWeight = GetUnlockedWeight();
            int rowStartWeight = r * weightsPerRow;
            int rowEndWeight = rowStartWeight + weightsPerRow;

            // Add empty slots (unlocked) and locked slots
            for (int w = rowWeightUsed[r]; w < weightsPerRow; w++)
            {
                int absoluteWeight = rowStartWeight + w;

                if (absoluteWeight < unlockedWeight)
                {
                    // Unlocked empty slot
                    GameObject emptySlot = CreateEmptySlot(row.transform, baseWidth, rowHeight);
                    emptySlots.Add(emptySlot);
                }
                else
                {
                    // Locked slot
                    GameObject lockedSlot = CreateLockedSlot(row.transform, baseWidth, rowHeight);
                    lockedSlots.Add(lockedSlot);
                }
            }
        }

        // Force layout update
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        Canvas.ForceUpdateCanvases();
    }

    /// <summary>
    /// Get unlocked weight capacity (from PlayerData or default)
    /// </summary>
    private int GetUnlockedWeight()
    {
        // TODO: Get from PlayerData when implemented
        // if (DataManager.Instance?.PlayerData != null)
        //     return DataManager.Instance.PlayerData.UnlockedAbilityWeight;
        return defaultUnlockedWeight;
    }

    /// <summary>
    /// Create an empty slot (unlocked)
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

    /// <summary>
    /// Create a locked slot
    /// </summary>
    private GameObject CreateLockedSlot(Transform parent, float baseWidth, float rowHeight)
    {
        GameObject slot;

        if (lockedSlotPrefab != null)
        {
            slot = Instantiate(lockedSlotPrefab, parent);
        }
        else
        {
            // Fallback: create simple locked slot (darker)
            slot = new GameObject("LockedSlot", typeof(RectTransform), typeof(Image));
            slot.transform.SetParent(parent, false);

            var image = slot.GetComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
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

        // Setup click handler for unequip
        var button = abilityObj.GetComponent<Button>();
        if (button == null)
        {
            button = abilityObj.AddComponent<Button>();
        }
        int capturedIndex = index;
        button.onClick.AddListener(() => OnAbilityClicked(capturedIndex));

        // Setup CombatAbilityUI if present (reusing combat prefab)
        var combatAbilityUI = abilityObj.GetComponent<CombatAbilityUI>();
        if (combatAbilityUI != null)
        {
            combatAbilityUI.Setup(ability, index, true, 0);
            // Disable cooldown overlay for equipment display
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
        displayToIndex.Clear();

        foreach (var slot in emptySlots)
        {
            if (slot != null)
                Destroy(slot);
        }
        emptySlots.Clear();

        foreach (var slot in lockedSlots)
        {
            if (slot != null)
                Destroy(slot);
        }
        lockedSlots.Clear();

        foreach (var row in rows)
        {
            if (row != null)
                Destroy(row);
        }
        rows.Clear();

        rowWeightUsed = null;
    }

    /// <summary>
    /// Handle click on an equipped ability (unequip)
    /// </summary>
    private void OnAbilityClicked(int index)
    {
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.TryUnequipAbilityAtIndex(index);
        }
    }

    // === DROP HANDLER ===

    public void OnDrop(PointerEventData eventData)
    {
        // Hide highlight
        if (dropHighlight != null)
        {
            dropHighlight.gameObject.SetActive(false);
        }

        // Check if this is an ability drag
        if (DragDropManager.Instance != null && DragDropManager.Instance.IsAbilityDrag)
        {
            DragDropManager.Instance.CompleteAbilityDrag();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Show drop highlight if dragging ability
        if (DragDropManager.Instance != null && DragDropManager.Instance.IsAbilityDrag)
        {
            if (dropHighlight != null)
            {
                dropHighlight.gameObject.SetActive(true);

                // Check if can equip
                string abilityId = DragDropManager.Instance.GetDraggedAbilityId();
                if (AbilityManager.Instance != null && AbilityManager.Instance.CanEquipAbility(abilityId))
                {
                    dropHighlight.color = validDropColor;
                }
                else
                {
                    dropHighlight.color = invalidDropColor;
                }
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Hide drop highlight
        if (dropHighlight != null)
        {
            dropHighlight.gameObject.SetActive(false);
        }
    }

    // === DEBUG ===

    [ContextMenu("Debug: Refresh Display")]
    public void DebugRefresh()
    {
        RefreshDisplay();
    }
}
