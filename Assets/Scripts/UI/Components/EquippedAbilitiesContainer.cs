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

    private RectTransform rectTransform;
    private List<GameObject> abilityDisplays = new List<GameObject>();
    private Dictionary<GameObject, float> rowWeights = new Dictionary<GameObject, float>();

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

        if (AbilityManager.Instance == null) return;

        var equippedAbilities = AbilityManager.Instance.GetEquippedAbilities();
        int currentWeight = AbilityManager.Instance.GetCurrentEquippedWeight();
        int maxWeight = AbilityManager.Instance.MaxEquippedWeight;

        // Update weight text
        if (weightText != null)
        {
            weightText.text = $"{currentWeight}/{maxWeight}";
        }

        if (equippedAbilities == null || equippedAbilities.Count == 0) return;

        // Calculate layout dimensions
        float availableWidth = rectTransform.rect.width - (padding * 2);
        if (availableWidth <= 0)
        {
            availableWidth = 300f - (padding * 2); // Fallback
        }

        float baseWidth = (availableWidth - (spacing * (weightsPerRow - 1))) / weightsPerRow;
        float rowHeight = baseWidth * heightRatio;

        // Place abilities in rows based on weight
        for (int i = 0; i < equippedAbilities.Count; i++)
        {
            var ability = equippedAbilities[i];
            if (ability == null) continue;

            int abilityWeight = ability.Weight > 0 ? ability.Weight : 1;

            // Find or create a row with enough space
            GameObject targetRow = FindOrCreateRow(abilityWeight, rowHeight);

            if (targetRow == null)
            {
                // No space left
                Logger.LogWarning($"EquippedAbilitiesContainer: No space for ability '{ability.GetDisplayName()}'", Logger.LogCategory.General);
                continue;
            }

            // Create the ability display
            GameObject abilityObj = CreateAbilityDisplay(ability, i, targetRow.transform, baseWidth, rowHeight);
            if (abilityObj != null)
            {
                abilityDisplays.Add(abilityObj);
                displayToIndex[abilityObj] = i;
                rowWeights[targetRow] += abilityWeight;
            }
        }

        // Force layout update
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        Canvas.ForceUpdateCanvases();
    }

    /// <summary>
    /// Find a row with enough space, or create a new one
    /// Row fill rule: If ability doesn't fit remaining space on top row, place on bottom row
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

        // Need a new row - check if we can create one
        if (rowWeights.Count >= maxRows)
        {
            return null; // Max rows reached
        }

        // Create new row
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

        foreach (var row in rowWeights.Keys)
        {
            if (row != null)
                Destroy(row);
        }
        rowWeights.Clear();
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
