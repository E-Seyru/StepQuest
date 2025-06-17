// Purpose: Complete UI component for inventory slots with drag and drop support
// Filepath: Assets/Scripts/UI/Components/InventorySlotUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI component for a single inventory slot with full drag and drop support
/// Compatible with existing InventoryPanelUI
/// </summary>
public class InventorySlotUI : MonoBehaviour, IDragDropSlot, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Image background;
    [SerializeField] private Image selectionHighlight;

    [Header("Visual Settings")]
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color dragHoverColor = Color.green;
    [SerializeField] private bool hideQuantityIfOne = true;

    // Data - compatible avec InventoryPanelUI
    private InventorySlot slotData;
    private int slotIndex;
    private bool isSelected = false;

    // Drag and drop
    private bool isDragSource = false;
    private Color originalBackgroundColor;

    // Events - compatible avec InventoryPanelUI
    public System.Action<InventorySlotUI, int> OnSlotClicked;
    public System.Action<InventorySlotUI, int> OnSlotRightClicked;

    void Start()
    {
        // Sauvegarder la couleur originale du background
        if (background != null)
        {
            originalBackgroundColor = background.color;
        }

        // Initialize empty
        if (slotData == null)
        {
            slotData = new InventorySlot();
        }
        RefreshVisuals();
    }

    void OnDestroy()
    {
        // Plus besoin de gérer l'enregistrement avec le système event-driven
    }

    void OnDisable()
    {
        // Sécurité : se nettoyer du DragDropManager si on était survolé
        if (DragDropManager.Instance != null && DragDropManager.Instance.IsDragging)
        {
            DragDropManager.Instance.ClearHoveredSlot(this);
        }
    }

    /// <summary>
    /// Setup this slot with slot data and index (compatible avec InventoryPanelUI)
    /// </summary>
    public void Setup(InventorySlot slot, int index)
    {
        slotData = slot;
        slotIndex = index;
        RefreshVisuals();
    }

    /// <summary>
    /// Get the slot data (compatible avec InventoryPanelUI)
    /// </summary>
    public InventorySlot GetSlotData()
    {
        return slotData;
    }

    /// <summary>
    /// Check if this slot is empty (compatible avec InventoryPanelUI)
    /// </summary>
    public bool IsEmpty()
    {
        return slotData == null || slotData.IsEmpty();
    }

    /// <summary>
    /// Set selection state (compatible avec InventoryPanelUI)
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectionHighlight != null)
        {
            selectionHighlight.gameObject.SetActive(selected);
        }

        if (background != null)
        {
            background.color = selected ? selectedColor : originalBackgroundColor;
        }
    }

    /// <summary>
    /// Refresh the visual display based on slot data
    /// </summary>
    private void RefreshVisuals()
    {
        if (IsEmpty())
        {
            // Empty slot
            if (itemIcon != null)
            {
                itemIcon.sprite = null;
                itemIcon.enabled = false;
            }

            if (quantityText != null)
            {
                quantityText.gameObject.SetActive(false);
            }
        }
        else
        {
            // Slot has item
            var itemDef = InventoryManager.Instance?.GetItemRegistry()?.GetItem(slotData.ItemID);
            if (itemDef != null)
            {
                // Set icon
                if (itemIcon != null)
                {
                    itemIcon.sprite = itemDef.ItemIcon;
                    itemIcon.color = itemDef.ItemColor;
                    itemIcon.enabled = true;
                }

                // Set quantity
                UpdateQuantityDisplay();
            }
            else
            {
                Debug.LogError($"InventorySlotUI: Item '{slotData.ItemID}' not found in registry");
            }
        }
    }

    /// <summary>
    /// Update quantity display
    /// </summary>
    private void UpdateQuantityDisplay()
    {
        if (quantityText != null)
        {
            if (slotData.Quantity > 1 || (!hideQuantityIfOne && slotData.Quantity > 0))
            {
                quantityText.text = slotData.Quantity.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }
    }

    // === IDragDropSlot Implementation ===

    public string GetItemId() => slotData?.ItemID;

    public int GetQuantity() => slotData?.Quantity ?? 0;

    public bool CanAcceptItem(string itemID, int qty)
    {
        // TODO: Add more validation logic here
        // For now, accept any item
        return true;
    }

    public bool TrySetItem(string itemID, int qty)
    {
        if (slotData != null)
        {
            slotData.SetItem(itemID, qty);
            RefreshVisuals();

            // Notify InventoryManager of the change
            InventoryManager.Instance?.TriggerContainerChanged("player"); // TODO: get actual container ID
            return true;
        }
        return false;
    }

    public bool TryRemoveItem(int qty)
    {
        if (slotData != null && !slotData.IsEmpty() && slotData.Quantity >= qty)
        {
            bool becameEmpty = slotData.RemoveQuantity(qty);
            RefreshVisuals();

            // Notify InventoryManager of the change
            InventoryManager.Instance?.TriggerContainerChanged("player"); // TODO: get actual container ID
            return true;
        }
        return false;
    }

    public RectTransform GetRectTransform() => transform as RectTransform;

    public void OnDragEnter()
    {
        if (background != null && !isDragSource)
        {
            background.color = dragHoverColor;
        }
    }

    public void OnDragExit()
    {
        if (background != null && !isDragSource)
        {
            // Restaurer la couleur originale ou sélectionnée
            background.color = isSelected ? selectedColor : originalBackgroundColor;
        }
    }

    // === Event System Handlers ===

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnSlotClicked?.Invoke(this, slotIndex);

            // Ouvrir ItemActionPanel si le slot n'est pas vide
            if (!IsEmpty() && ItemActionPanel.Instance != null)
            {
                Vector2 worldPosition = transform.position;
                ItemActionPanel.Instance.ShowPanel(this, slotData, worldPosition);
            }
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnSlotRightClicked?.Invoke(this, slotIndex);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsEmpty() || DragDropManager.Instance == null)
            return;

        // TODO: Handle split quantities with modifiers (Ctrl, Shift, etc.)
        int dragQuantity = slotData.Quantity;

        if (DragDropManager.Instance.StartDrag(this, slotData.ItemID, dragQuantity))
        {
            isDragSource = true;

            // Visual feedback for source slot
            if (itemIcon != null)
            {
                var color = itemIcon.color;
                color.a = 0.5f;
                itemIcon.color = color;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Le DragDropManager gère la position du visual
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragSource) return;

        isDragSource = false;

        // IMPORTANT: Si le drag n'a pas été complété via OnDrop(), l'annuler
        if (DragDropManager.Instance != null && DragDropManager.Instance.IsDragging)
        {
            DragDropManager.Instance.CancelDrag();
        }

        // Restore visual
        if (itemIcon != null && !IsEmpty())
        {
            var itemDef = InventoryManager.Instance?.GetItemRegistry()?.GetItem(slotData.ItemID);
            if (itemDef != null)
            {
                itemIcon.color = itemDef.ItemColor;
            }
        }

        // Restaurer la couleur du background
        if (background != null)
        {
            background.color = isSelected ? selectedColor : originalBackgroundColor;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (DragDropManager.Instance != null && DragDropManager.Instance.IsDragging)
        {
            DragDropManager.Instance.CompleteDrag(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Notifier le DragDropManager (event-driven)
        if (DragDropManager.Instance != null && DragDropManager.Instance.IsDragging)
        {
            DragDropManager.Instance.SetHoveredSlot(this);
        }

        // TODO: Show tooltip with item info quand pas en drag
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Notifier le DragDropManager (event-driven)
        if (DragDropManager.Instance != null && DragDropManager.Instance.IsDragging)
        {
            DragDropManager.Instance.ClearHoveredSlot(this);
        }

        // TODO: Hide tooltip
    }
}