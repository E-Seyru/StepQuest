// Purpose: UI component for displaying abilities in the abilities inventory
// Filepath: Assets/Scripts/UI/Components/AbilitySlotUI.cs

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI component for ability slots in the abilities inventory tab.
/// Displays ability icon and supports click/drag to equip.
/// </summary>
public class AbilitySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image abilityIcon;
    [SerializeField] private Image background;
    [SerializeField] private Image selectionHighlight;
    [SerializeField] private TextMeshProUGUI weightText;

    [Header("Visual Settings")]
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color emptySlotColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    // Data
    private string abilityId;
    private int slotIndex;
    private AbilityDefinition abilityDef;
    private bool isSelected = false;
    private bool isEmpty = true;

    // Drag
    private bool isDragging = false;
    private Color originalBackgroundColor;

    // Events
    public System.Action<AbilitySlotUI, int> OnSlotClicked;
    public System.Action<AbilitySlotUI, int> OnSlotDoubleClicked;

    // Public accessors
    public string AbilityId => abilityId;
    public int SlotIndex => slotIndex;
    public AbilityDefinition AbilityDefinition => abilityDef;
    public bool IsEmpty => isEmpty;

    private void Start()
    {
        if (background != null)
        {
            originalBackgroundColor = background.color;
        }

        RefreshVisuals();
    }

    /// <summary>
    /// Setup this slot with an ability
    /// </summary>
    public void Setup(string abilityId, int index)
    {
        this.abilityId = abilityId;
        this.slotIndex = index;

        if (string.IsNullOrEmpty(abilityId))
        {
            isEmpty = true;
            abilityDef = null;
        }
        else
        {
            abilityDef = AbilityManager.Instance?.GetAbilityDefinition(abilityId);
            isEmpty = abilityDef == null;
        }

        RefreshVisuals();
    }

    /// <summary>
    /// Setup this slot with an AbilityDefinition directly
    /// </summary>
    public void Setup(AbilityDefinition ability, int index)
    {
        this.slotIndex = index;
        this.abilityDef = ability;

        if (ability != null)
        {
            this.abilityId = ability.AbilityID;
            isEmpty = false;
        }
        else
        {
            this.abilityId = null;
            isEmpty = true;
        }

        RefreshVisuals();
    }

    /// <summary>
    /// Clear this slot
    /// </summary>
    public void Clear()
    {
        abilityId = null;
        abilityDef = null;
        isEmpty = true;
        RefreshVisuals();
    }

    /// <summary>
    /// Refresh visual display
    /// </summary>
    public void RefreshVisuals()
    {
        if (isEmpty || abilityDef == null)
        {
            // Empty slot
            if (abilityIcon != null)
            {
                abilityIcon.gameObject.SetActive(false);
            }
            if (weightText != null)
            {
                weightText.gameObject.SetActive(false);
            }
            if (background != null)
            {
                background.color = emptySlotColor;
            }
        }
        else
        {
            // Show ability
            if (abilityIcon != null)
            {
                abilityIcon.gameObject.SetActive(true);
                abilityIcon.sprite = abilityDef.AbilityIcon;
                abilityIcon.color = abilityDef.AbilityColor;
            }
            if (weightText != null)
            {
                weightText.gameObject.SetActive(true);
                weightText.text = abilityDef.Weight.ToString();
            }
            if (background != null)
            {
                background.color = originalBackgroundColor;
            }
        }

        // Update selection highlight
        if (selectionHighlight != null)
        {
            selectionHighlight.gameObject.SetActive(isSelected);
            if (isSelected)
            {
                selectionHighlight.color = selectedColor;
            }
        }
    }

    /// <summary>
    /// Set selection state
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectionHighlight != null)
        {
            selectionHighlight.gameObject.SetActive(selected);
            if (selected)
            {
                selectionHighlight.color = selectedColor;
            }
        }
    }

    /// <summary>
    /// Get the RectTransform
    /// </summary>
    public RectTransform GetRectTransform()
    {
        return GetComponent<RectTransform>();
    }

    // === POINTER EVENTS ===

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isEmpty) return;

        if (eventData.clickCount >= 2)
        {
            // Double click - try to equip
            OnSlotDoubleClicked?.Invoke(this, slotIndex);
            TryEquipAbility();
        }
        else
        {
            // Single click
            OnSlotClicked?.Invoke(this, slotIndex);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isDragging && background != null)
        {
            background.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isDragging && background != null)
        {
            background.color = isEmpty ? emptySlotColor : originalBackgroundColor;
        }
    }

    // === DRAG EVENTS ===

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isEmpty) return;

        isDragging = true;

        // Notify drag system
        if (DragDropManager.Instance != null)
        {
            DragDropManager.Instance.StartAbilityDrag(this, abilityId, abilityDef);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // Update drag visual position
        if (DragDropManager.Instance != null)
        {
            DragDropManager.Instance.UpdateDragPosition(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        // Complete the drag
        if (DragDropManager.Instance != null)
        {
            DragDropManager.Instance.EndDrag();
        }

        // Reset visuals
        if (background != null)
        {
            background.color = isEmpty ? emptySlotColor : originalBackgroundColor;
        }
    }

    // === ACTIONS ===

    /// <summary>
    /// Try to equip this ability
    /// </summary>
    private void TryEquipAbility()
    {
        if (isEmpty || string.IsNullOrEmpty(abilityId)) return;

        if (AbilityManager.Instance != null)
        {
            if (AbilityManager.Instance.TryEquipAbility(abilityId))
            {
                Logger.LogInfo($"AbilitySlotUI: Equipped ability '{abilityDef?.GetDisplayName()}'", Logger.LogCategory.General);
            }
            else
            {
                Logger.LogWarning($"AbilitySlotUI: Could not equip ability '{abilityDef?.GetDisplayName()}' - weight limit?", Logger.LogCategory.General);
            }
        }
    }
}
