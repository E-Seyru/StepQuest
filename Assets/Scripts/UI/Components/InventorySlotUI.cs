// Purpose: UI component for displaying a single inventory slot
// Filepath: Assets/Scripts/UI/Components/InventorySlotUI.cs
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Button slotButton;
    [SerializeField] private Image background;

    [Header("Visual Settings")]
    [SerializeField] private Color emptyColor = Color.gray;
    [SerializeField] private Color filledColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    // Data
    private InventorySlot slotData;
    private int slotIndex;
    private bool isSelected = false;

    // Events
    public event Action<InventorySlotUI, int> OnSlotClicked; // Slot, Index

    void Awake()
    {
        // Setup button click
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotButtonClicked);
        }

        // Validate references
        ValidateReferences();
    }

    /// <summary>
    /// Setup this slot with data and index
    /// </summary>
    public void Setup(InventorySlot slot, int index)
    {
        slotData = slot;
        slotIndex = index;
        RefreshDisplay();
    }

    /// <summary>
    /// Refresh the visual display based on current slot data
    /// </summary>
    public void RefreshDisplay()
    {
        if (slotData == null || slotData.IsEmpty())
        {
            ShowEmptySlot();
        }
        else
        {
            ShowFilledSlot();
        }
    }

    /// <summary>
    /// Show empty slot state
    /// </summary>
    private void ShowEmptySlot()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.color = new Color(1, 1, 1, 0); // Transparent
        }

        if (quantityText != null)
        {
            quantityText.text = "";
        }

        if (background != null)
        {
            background.color = emptyColor;
        }
    }

    /// <summary>
    /// Show filled slot state
    /// </summary>
    private void ShowFilledSlot()
    {
        // Get item definition to show icon
        // TODO: Use ItemRegistry when available
        if (itemIcon != null)
        {
            // For now, just show that slot is filled
            itemIcon.color = Color.white;
            // itemIcon.sprite = GetItemIcon(slotData.ItemID);
        }

        // Show quantity if more than 1
        if (quantityText != null)
        {
            if (slotData.Quantity > 1)
            {
                quantityText.text = $"x{slotData.Quantity}";
            }
            else
            {
                quantityText.text = "";
            }
        }

        if (background != null)
        {
            background.color = isSelected ? selectedColor : filledColor;
        }
    }

    /// <summary>
    /// Set selection state
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RefreshDisplay();
    }

    /// <summary>
    /// Handle slot button click
    /// </summary>
    private void OnSlotButtonClicked()
    {
        OnSlotClicked?.Invoke(this, slotIndex);
        Debug.Log($"Slot {slotIndex} clicked - Item: {slotData?.ItemID ?? "Empty"} x{slotData?.Quantity ?? 0}");
    }

    /// <summary>
    /// Get slot data
    /// </summary>
    public InventorySlot GetSlotData()
    {
        return slotData;
    }

    /// <summary>
    /// Get slot index
    /// </summary>
    public int GetSlotIndex()
    {
        return slotIndex;
    }

    /// <summary>
    /// Check if this slot is empty
    /// </summary>
    public bool IsEmpty()
    {
        return slotData?.IsEmpty() ?? true;
    }

    /// <summary>
    /// Validate that all required references are assigned
    /// </summary>
    private void ValidateReferences()
    {
        if (itemIcon == null)
            Debug.LogWarning($"InventorySlotUI: itemIcon not assigned on {gameObject.name}");

        if (quantityText == null)
            Debug.LogWarning($"InventorySlotUI: quantityText not assigned on {gameObject.name}");

        if (slotButton == null)
            Debug.LogWarning($"InventorySlotUI: slotButton not assigned on {gameObject.name}");
    }

    /// <summary>
    /// Get item icon (placeholder for now)
    /// </summary>
    private Sprite GetItemIcon(string itemId)
    {
        // TODO: Implement with ItemRegistry
        // return ItemRegistry.Instance?.GetItem(itemId)?.ItemIcon;
        return null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Auto-assign references in editor
    /// </summary>
    void Reset()
    {
        if (itemIcon == null)
            itemIcon = GetComponentInChildren<Image>();

        if (quantityText == null)
            quantityText = GetComponentInChildren<TextMeshProUGUI>();

        if (slotButton == null)
            slotButton = GetComponent<Button>();

        if (background == null)
            background = GetComponent<Image>();
    }
#endif
}