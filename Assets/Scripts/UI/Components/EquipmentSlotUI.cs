using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component for a single equipment slot
/// </summary>
public class EquipmentSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private Image background;
    [SerializeField] private Button slotButton;
    [SerializeField] private TextMeshProUGUI slotLabel;



    // Data
    private EquipmentType slotType;
    private string equippedItemId;
    private EquipmentPanelUI parentPanel;

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

        }

    }

    /// <summary>
    /// Clear the equipped item from this slot
    /// </summary>
    public void ClearEquippedItem()
    {
        equippedItemId = null;



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
}