// Purpose: Universal UI component for inventory, bank, shop slots with drag and drop support
// Filepath: Assets/Scripts/UI/Components/UniversalSlotUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Universal UI component for any container slot (inventory, bank, shop) with full drag and drop support
/// </summary>
public class UniversalSlotUI : MonoBehaviour, IDragDropSlot, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public enum SlotContext
    {
        PlayerInventory,
        Bank,
        Shop,
        Trade,
        Loot
    }

    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private GameObject quantityTextContainer; // Optional: parent container for quantity text
    [SerializeField] private Image background;
    [SerializeField] private Image selectionHighlight;
    [SerializeField] private Image contextIndicator; // Optionnel: indicateur visuel du type de container

    [Header("Visual Settings")]
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Color dragHoverColor = Color.green;
    [SerializeField] private bool hideQuantityIfOne = true;

    [Header("Context Colors (Optional)")]
    [SerializeField] private Color bankSlotTint = new Color(0.8f, 0.9f, 1f, 1f);
    [SerializeField] private Color shopSlotTint = new Color(1f, 0.9f, 0.8f, 1f);

    // Data
    private InventorySlot slotData;
    private int slotIndex;
    private string containerId;
    private SlotContext context;
    private bool isSelected = false;

    // Drag and drop
    private bool isDragSource = false;
    private Color originalBackgroundColor;

    // Events
    public System.Action<UniversalSlotUI, int> OnSlotClicked;
    public System.Action<UniversalSlotUI, int> OnSlotRightClicked;

    // Public accessors
    public string ContainerId => containerId;
    public SlotContext Context => context;
    public int SlotIndex => slotIndex;

    void Start()
    {
        // Sauvegarder la couleur originale du background
        if (background != null)
        {
            originalBackgroundColor = background.color;
        }

        // Initialize empty if needed
        if (slotData == null)
        {
            slotData = new InventorySlot();
        }
        RefreshVisuals();
    }

    void OnDisable()
    {
        // Securite : se nettoyer du DragDropManager si on etait survole
        if (DragDropManager.Instance != null && DragDropManager.Instance.IsDragging)
        {
            DragDropManager.Instance.ClearHoveredSlot(this);
        }
    }

    /// <summary>
    /// Setup this slot with context information
    /// </summary>
    public void Setup(InventorySlot slot, int index, string containerId, SlotContext context)
    {
        this.slotData = slot;
        this.slotIndex = index;
        this.containerId = containerId;
        this.context = context;

        // Appliquer une teinte visuelle selon le contexte (optionnel)
        ApplyContextVisuals();

        RefreshVisuals();
    }

    /// <summary>
    /// Apply visual indicators based on context
    /// </summary>
    private void ApplyContextVisuals()
    {
        if (background != null)
        {
            switch (context)
            {
                case SlotContext.Bank:
                    originalBackgroundColor = bankSlotTint;
                    background.color = bankSlotTint;
                    break;
                case SlotContext.Shop:
                    originalBackgroundColor = shopSlotTint;
                    background.color = shopSlotTint;
                    break;
                default:
                    // Keep original color for player inventory
                    break;
            }
        }

        // Optionnel: afficher un indicateur de contexte
        if (contextIndicator != null)
        {
            contextIndicator.gameObject.SetActive(context != SlotContext.PlayerInventory);
        }
    }

    /// <summary>
    /// Get the slot data
    /// </summary>
    public InventorySlot GetSlotData()
    {
        return slotData;
    }

    /// <summary>
    /// Check if this slot is empty
    /// </summary>
    public bool IsEmpty()
    {
        return slotData == null || slotData.IsEmpty();
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
        }

        if (background != null && !isDragSource)
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
            // Empty slot - hide the entire ItemIcon GameObject (includes quantity text)
            if (itemIcon != null)
            {
                itemIcon.sprite = null;
                itemIcon.gameObject.SetActive(false);
            }

            if (quantityText != null)
            {
                quantityText.gameObject.SetActive(false);
            }

            // Also hide container if assigned
            if (quantityTextContainer != null)
            {
                quantityTextContainer.SetActive(false);
            }
        }
        else
        {
            // Slot has item
            var itemDef = InventoryManager.Instance?.GetItemRegistry()?.GetItem(slotData.ItemID);
            if (itemDef != null)
            {
                // Show and set icon
                if (itemIcon != null)
                {
                    itemIcon.gameObject.SetActive(true);
                    itemIcon.sprite = itemDef.ItemIcon;
                    itemIcon.color = itemDef.ItemColor;
                    itemIcon.enabled = true;
                }

                // Set quantity
                UpdateQuantityDisplay();
            }
            else
            {
                Logger.LogError($"UniversalSlotUI: Item '{slotData.ItemID}' not found in registry", Logger.LogCategory.InventoryLog);
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
            bool shouldShow = slotData.Quantity > 1 || (!hideQuantityIfOne && slotData.Quantity > 0);

            if (shouldShow)
            {
                quantityText.text = slotData.Quantity.ToString();
                quantityText.gameObject.SetActive(true);

                // Also activate container if assigned
                if (quantityTextContainer != null)
                {
                    quantityTextContainer.SetActive(true);
                }
            }
            else
            {
                quantityText.gameObject.SetActive(false);

                // Also hide container if assigned
                if (quantityTextContainer != null)
                {
                    quantityTextContainer.SetActive(false);
                }
            }
        }
    }

    // === IDragDropSlot Implementation ===

    public string GetItemId() => slotData?.ItemID;

    public int GetQuantity() => slotData?.Quantity ?? 0;

    public bool CanAcceptItem(string itemID, int qty)
    {
        // Verifications selon le contexte
        switch (context)
        {
            case SlotContext.Shop:
                // Les shops ne peuvent pas recevoir d'items (seulement vendre)
                return false;

            case SlotContext.Bank:
                // Verifier si l'item peut etre stocke en banque
                var itemDef = InventoryManager.Instance?.GetItemRegistry()?.GetItem(itemID);

                break;
        }

        // Verification generale
        return true;
    }

    public bool TrySetItem(string itemID, int qty)
    {
        if (slotData != null)
        {
            slotData.SetItem(itemID, qty);
            RefreshVisuals();

            // Notifier le changement avec le bon containerId
            InventoryManager.Instance?.TriggerContainerChanged(containerId);
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

            // Notifier le changement avec le bon containerId
            InventoryManager.Instance?.TriggerContainerChanged(containerId);
            return true;
        }
        return false;
    }

    public RectTransform GetRectTransform() => transform as RectTransform;

    public void OnDragEnter()
    {
        if (background != null && !isDragSource)
        {
            // Verifier si on peut accepter l'item
            string draggedItemId = DragDropManager.Instance?.GetDraggedItemId();
            if (!string.IsNullOrEmpty(draggedItemId) && CanAcceptItem(draggedItemId, 1))
            {
                background.color = dragHoverColor;
            }
            else
            {
                background.color = Color.red; // Indicateur de drop invalide
            }
        }
    }

    public void OnDragExit()
    {
        if (background != null && !isDragSource)
        {
            background.color = isSelected ? selectedColor : originalBackgroundColor;
        }
    }

    // === Event System Handlers ===

    public void OnPointerClick(PointerEventData eventData)
    {
        // Sur mobile, on a seulement le tap
        OnSlotClicked?.Invoke(this, slotIndex);

        // Ouvrir ItemActionPanel si le slot n'est pas vide
        if (!IsEmpty() && ItemActionPanel.Instance != null)
        {
            Vector2 worldPosition = transform.position;
            ItemActionPanel.Instance.ShowPanel(this, slotData, containerId, context, worldPosition);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsEmpty() || DragDropManager.Instance == null)
            return;

        // Verifier les permissions selon le contexte
        if (context == SlotContext.Shop)
        {
            Logger.LogInfo("UniversalSlotUI: Cannot drag items from shop", Logger.LogCategory.InventoryLog);
            return;
        }

        // Sur mobile, on peut detecter un "long press" pour split
        // mais pour l'instant on drag toute la quantite
        int dragQuantity = slotData.Quantity;

        if (DragDropManager.Instance.StartDrag(this, slotData.ItemID, dragQuantity))
        {
            isDragSource = true;

            // Visual feedback
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
        // Le DragDropManager gere la position du visual
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragSource) return;

        isDragSource = false;

        // Si le drag n'a pas ete complete, l'annuler
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

        // Restore background color
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
        // Notifier le DragDropManager
        if (DragDropManager.Instance != null && DragDropManager.Instance.IsDragging)
        {
            DragDropManager.Instance.SetHoveredSlot(this);
        }

        // TODO: Show tooltip avec info sur l'item
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Notifier le DragDropManager
        if (DragDropManager.Instance != null && DragDropManager.Instance.IsDragging)
        {
            DragDropManager.Instance.ClearHoveredSlot(this);
        }

        // TODO: Hide tooltip
    }

    /// <summary>
    /// Get debug info about this slot
    /// </summary>
    public string GetDebugInfo()
    {
        return $"Slot[{slotIndex}] in {containerId} ({context}): {slotData?.ToString() ?? "Empty"}";
    }
}