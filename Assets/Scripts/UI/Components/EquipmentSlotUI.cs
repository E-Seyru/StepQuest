// Purpose: Equipment slot UI with drag and drop support
// Filepath: Assets/Scripts/UI/Components/EquipmentSlotUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI component for a single equipment slot with drag and drop support
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IDragDropSlot, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private Image background;
    [SerializeField] private Button slotButton;
    [SerializeField] private TextMeshProUGUI slotLabel;

    [Header("Visual Feedback")]
    [SerializeField] private Color dragHoverColor = Color.green;
    [SerializeField] private Color invalidDropColor = Color.red;

    // Data
    private EquipmentType slotType;
    private string equippedItemId;
    private EquipmentPanelUI parentPanel;

    // Drag state
    private bool isDragSource = false;
    private Color originalBackgroundColor;

    void Start()
    {
        // Sauvegarder la couleur originale du background
        if (background != null)
        {
            originalBackgroundColor = background.color;
        }
    }

    void OnDestroy()
    {
        // Plus besoin de g�rer l'enregistrement avec le syst�me event-driven
    }

    void OnDisable()
    {
        // S�curit� : se nettoyer du DragDropManager si on �tait survol�
        if (DragDropManager.Instance != null && DragDropManager.Instance.IsDragging)
        {
            DragDropManager.Instance.ClearHoveredSlot(this);
        }
    }

    /// <summary>
    /// Setup this slot with equipment type and parent panel
    /// </summary>
    public void Setup(EquipmentType type, EquipmentPanelUI parent)
    {
        slotType = type;
        parentPanel = parent;

        // Setup button click
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }

        // Set slot label
        if (slotLabel != null)
        {
            slotLabel.text = GetSlotDisplayName(slotType);
        }

        // Initialize as empty
        ClearEquippedItem();
    }

    /// <summary>
    /// Set an equipped item in this slot
    /// </summary>
    public void SetEquippedItem(string itemId)
    {
        equippedItemId = itemId;

        var itemDef = InventoryManager.Instance?.GetItemRegistry()?.GetItem(itemId);
        if (itemDef != null && itemIcon != null)
        {
            itemIcon.sprite = itemDef.ItemIcon;
            itemIcon.enabled = true; // Affiche l'ic�ne
        }
    }

    /// <summary>
    /// Clear the equipped item from this slot
    /// </summary>
    public void ClearEquippedItem()
    {
        equippedItemId = null;

        // Cache l'ic�ne quand il n'y a pas d'�quipement
        if (itemIcon != null)
        {
            itemIcon.enabled = false;
        }
    }

    /// <summary>
    /// Handle slot button click
    /// </summary>
    private void OnSlotClicked()
    {
        parentPanel?.OnSlotClicked(slotType);
    }

    /// <summary>
    /// Get display name for equipment slot type
    /// </summary>
    private string GetSlotDisplayName(EquipmentType type)
    {
        return type switch
        {
            EquipmentType.Weapon => "Arme",
            EquipmentType.Helmet => "Casque",
            EquipmentType.Legs => "Jambes",
            EquipmentType.Boots => "Bottes",
            EquipmentType.Backpack => "Sac",
            _ => type.ToString()
        };
    }

    // === IDragDropSlot Implementation ===

    public string GetItemId() => equippedItemId;

    public int GetQuantity() => string.IsNullOrEmpty(equippedItemId) ? 0 : 1;

    public bool CanAcceptItem(string itemID, int qty)
    {
        // V�rifier que c'est de l'�quipement
        var itemDef = InventoryManager.Instance?.GetItemRegistry()?.GetItem(itemID);
        if (itemDef == null || !itemDef.IsEquipment())
            return false;

        // V�rifier que c'est le bon type d'�quipement pour ce slot
        return itemDef.EquipmentSlot == slotType;
    }

    public bool TrySetItem(string itemID, int qty)
    {
        if (!CanAcceptItem(itemID, qty))
            return false;

        // Utiliser le syst�me d'�quipement existant
        if (parentPanel != null)
        {
            return parentPanel.TryEquipItem(itemID);
        }

        return false;
    }

    public bool TryRemoveItem(int qty)
    {
        if (string.IsNullOrEmpty(equippedItemId))
            return false;

        // Utiliser le syst�me d'�quipement existant
        if (parentPanel != null)
        {
            return parentPanel.UnequipItem(slotType);
        }

        return false;
    }

    public RectTransform GetRectTransform() => transform as RectTransform;

    public void OnDragEnter()
    {
        if (background != null && !isDragSource)
        {
            // V�rifier si l'item peut �tre accept�
            string draggedItemId = DragDropManager.Instance?.GetDraggedItemId();
            if (!string.IsNullOrEmpty(draggedItemId) && CanAcceptItem(draggedItemId, 1))
            {
                background.color = dragHoverColor;
            }
            else
            {
                background.color = invalidDropColor;
            }
        }
    }

    public void OnDragExit()
    {
        if (background != null && !isDragSource)
        {
            // Restaurer la couleur originale
            background.color = originalBackgroundColor;
        }
    }

    public bool IsEmpty() => string.IsNullOrEmpty(equippedItemId);

    // === Event System Handlers ===

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnSlotClicked();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsEmpty() || DragDropManager.Instance == null)
            return;

        // Commencer le drag depuis cet �quipement
        if (DragDropManager.Instance.StartDrag(this, equippedItemId, 1))
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
        // Le DragDropManager g�re la position du visual
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragSource) return;

        isDragSource = false;

        // IMPORTANT: Si le drag n'a pas �t� compl�t� via OnDrop(), l'annuler
        if (DragDropManager.Instance != null && DragDropManager.Instance.IsDragging)
        {
            DragDropManager.Instance.CancelDrag();
        }

        // Restore visual
        if (itemIcon != null && !string.IsNullOrEmpty(equippedItemId))
        {
            var itemDef = InventoryManager.Instance?.GetItemRegistry()?.GetItem(equippedItemId);
            if (itemDef != null)
            {
                itemIcon.color = itemDef.ItemColor;
            }
        }

        // Restaurer la couleur du background originale
        if (background != null)
        {
            background.color = originalBackgroundColor;
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
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Notifier le DragDropManager (event-driven)
        if (DragDropManager.Instance != null && DragDropManager.Instance.IsDragging)
        {
            DragDropManager.Instance.ClearHoveredSlot(this);
        }
    }
}