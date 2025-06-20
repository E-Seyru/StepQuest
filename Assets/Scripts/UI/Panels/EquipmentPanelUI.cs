// Purpose: UI controller for the equipment panel (manages equipped gear slots)
// Filepath: Assets/Scripts/UI/Panels/EquipmentPanelUI.cs
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EquipmentPanelUI : MonoBehaviour
{
    [Header("Equipment Slot UI References")]
    [SerializeField] private EquipmentSlotUI weaponSlot;
    [SerializeField] private EquipmentSlotUI helmetSlot;
    [SerializeField] private EquipmentSlotUI legsSlot;
    [SerializeField] private EquipmentSlotUI bootsSlot;
    [SerializeField] private EquipmentSlotUI backpackSlot;

    [Header("Stats Display")]
    [SerializeField] private TextMeshProUGUI totalStatsText;
    [SerializeField] private TextMeshProUGUI equipmentInfoText;

    // Equipment data storage (simplified - you might want to move this to PlayerData later)
    private Dictionary<EquipmentType, string> equippedItems = new Dictionary<EquipmentType, string>();

    // References
    private InventoryManager inventoryManager;

    // Events
    public event Action<EquipmentType, string> OnItemEquipped;   // EquipmentType, ItemID
    public event Action<EquipmentType, string> OnItemUnequipped; // EquipmentType, ItemID

    public static EquipmentPanelUI Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        inventoryManager = InventoryManager.Instance;

        if (inventoryManager == null)
        {
            Debug.LogError("EquipmentPanelUI: InventoryManager not found!");
            return;
        }

        // Initialize equipment slots
        InitializeEquipmentSlots();

        // Load equipped items from save data
        LoadEquippedItems();

        // Initial display refresh
        RefreshDisplay();

        Debug.Log("EquipmentPanelUI: Initialized successfully");
    }

    /// <summary>
    /// Initialize all equipment slot UIs
    /// </summary>
    private void InitializeEquipmentSlots()
    {
        // Setup each slot with its corresponding equipment type
        if (weaponSlot != null)
        {
            weaponSlot.Setup(EquipmentType.Weapon, this);
        }

        if (helmetSlot != null)
        {
            helmetSlot.Setup(EquipmentType.Helmet, this);
        }

        if (legsSlot != null)
        {
            legsSlot.Setup(EquipmentType.Legs, this);
        }

        if (bootsSlot != null)
        {
            bootsSlot.Setup(EquipmentType.Boots, this);
        }

        if (backpackSlot != null)
        {
            backpackSlot.Setup(EquipmentType.Backpack, this);
        }
    }

    /// <summary>
    /// Try to equip an item from inventory
    /// </summary>
    public bool TryEquipItem(string itemId)
    {
        var itemDef = inventoryManager.GetItemRegistry().GetItem(itemId);
        if (itemDef == null || !itemDef.IsEquipment())
        {
            Debug.LogWarning($"EquipmentPanelUI: Item '{itemId}' is not equipment or doesn't exist");
            return false;
        }

        EquipmentType slotType = itemDef.EquipmentSlot;

        // Check if player has this item in inventory
        var playerContainer = inventoryManager.GetContainer("player");
        if (playerContainer == null || !playerContainer.HasItem(itemId))
        {
            Debug.LogWarning($"EquipmentPanelUI: Player doesn't have item '{itemId}' in inventory");
            return false;
        }

        // Unequip current item in this slot if any
        if (equippedItems.ContainsKey(slotType))
        {
            UnequipItem(slotType);
        }

        // Remove item from inventory
        bool removedFromInventory = inventoryManager.RemoveItem("player", itemId, 1);
        if (!removedFromInventory)
        {
            Debug.LogError($"EquipmentPanelUI: Failed to remove item '{itemId}' from inventory");
            return false;
        }

        // Equip the item
        equippedItems[slotType] = itemId;

        // Update slot UI
        var slot = GetSlotByType(slotType);
        if (slot != null)
        {
            slot.SetEquippedItem(itemId);
        }

        // Trigger events and update display
        OnItemEquipped?.Invoke(slotType, itemId);
        RefreshDisplay();
        SaveEquippedItems();

        Debug.Log($"EquipmentPanelUI: Equipped {itemDef.GetDisplayName()} in {slotType} slot");
        return true;
    }

    /// <summary>
    /// Unequip an item and return it to inventory
    /// </summary>
    public bool UnequipItem(EquipmentType slotType)
    {
        if (!equippedItems.ContainsKey(slotType))
        {
            Debug.LogWarning($"EquipmentPanelUI: No item equipped in {slotType} slot");
            return false;
        }

        string itemId = equippedItems[slotType];
        var itemDef = inventoryManager.GetItemRegistry().GetItem(itemId);

        // Add item back to inventory
        bool addedToInventory = inventoryManager.AddItem("player", itemId, 1);
        if (!addedToInventory)
        {
            Debug.LogError($"EquipmentPanelUI: Failed to add item '{itemId}' back to inventory (inventory full?)");
            return false;
        }

        // Remove from equipped items
        equippedItems.Remove(slotType);

        // Update slot UI
        var slot = GetSlotByType(slotType);
        if (slot != null)
        {
            slot.ClearEquippedItem();
        }

        // Trigger events and update display
        OnItemUnequipped?.Invoke(slotType, itemId);
        RefreshDisplay();
        SaveEquippedItems();

        Debug.Log($"EquipmentPanelUI: Unequipped {itemDef?.GetDisplayName() ?? itemId} from {slotType} slot");
        return true;
    }

    /// <summary>
    /// Get equipped item ID for a specific slot
    /// </summary>
    public string GetEquippedItem(EquipmentType slotType)
    {
        return equippedItems.ContainsKey(slotType) ? equippedItems[slotType] : null;
    }

    /// <summary>
    /// Check if a specific slot has an item equipped
    /// </summary>
    public bool IsSlotEquipped(EquipmentType slotType)
    {
        return equippedItems.ContainsKey(slotType);
    }

    /// <summary>
    /// Get all equipped items
    /// </summary>
    public Dictionary<EquipmentType, string> GetAllEquippedItems()
    {
        return new Dictionary<EquipmentType, string>(equippedItems);
    }

    /// <summary>
    /// Handle slot clicked (for unequipping)
    /// </summary>
    public void OnSlotClicked(EquipmentType slotType)
    {
        if (IsSlotEquipped(slotType))
        {
            UnequipItem(slotType);
        }
        else
        {
            Debug.Log($"EquipmentPanelUI: {slotType} slot is empty");
        }
    }

    /// <summary>
    /// Refresh the entire equipment display
    /// </summary>
    private void RefreshDisplay()
    {
        RefreshStatsDisplay();
        RefreshEquipmentInfo();
    }

    /// <summary>
    /// Refresh stats display (placeholder for now)
    /// </summary>
    private void RefreshStatsDisplay()
    {
        if (totalStatsText != null)
        {
            // TODO: Calculate total stats from equipped items
            int totalItems = equippedItems.Count;
            totalStatsText.text = $"Objets equipes: {totalItems}";
        }
    }

    /// <summary>
    /// Refresh equipment info display
    /// </summary>
    private void RefreshEquipmentInfo()
    {
        if (equipmentInfoText != null)
        {
            if (equippedItems.Count == 0)
            {
                equipmentInfoText.text = "Aucun equipement";
            }
            else
            {
                var info = "";
                foreach (var kvp in equippedItems)
                {
                    var itemDef = inventoryManager.GetItemRegistry().GetItem(kvp.Value);
                    string itemName = itemDef?.GetDisplayName() ?? kvp.Value;
                    info += $"{kvp.Key}: {itemName}\n";
                }
                equipmentInfoText.text = info.TrimEnd();
            }
        }
    }

    /// <summary>
    /// Get slot UI component by equipment type
    /// </summary>
    private EquipmentSlotUI GetSlotByType(EquipmentType slotType)
    {
        return slotType switch
        {
            EquipmentType.Weapon => weaponSlot,
            EquipmentType.Helmet => helmetSlot,
            EquipmentType.Legs => legsSlot,
            EquipmentType.Boots => bootsSlot,
            EquipmentType.Backpack => backpackSlot,
            _ => null
        };
    }

    /// <summary>
    /// Save equipped items to persistent storage
    /// </summary>
    private void SaveEquippedItems()
    {
        // TODO: Save to PlayerData when available
        // For now, using PlayerPrefs as temporary solution
        foreach (var kvp in equippedItems)
        {
            PlayerPrefs.SetString($"Equipped_{kvp.Key}", kvp.Value);
        }

        // Clear unused slots
        foreach (EquipmentType slotType in System.Enum.GetValues(typeof(EquipmentType)))
        {
            if (slotType != EquipmentType.None && !equippedItems.ContainsKey(slotType))
            {
                PlayerPrefs.DeleteKey($"Equipped_{slotType}");
            }
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Load equipped items from persistent storage
    /// </summary>
    private void LoadEquippedItems()
    {
        equippedItems.Clear();

        // TODO: Load from PlayerData when available
        // For now, using PlayerPrefs as temporary solution
        foreach (EquipmentType slotType in System.Enum.GetValues(typeof(EquipmentType)))
        {
            if (slotType == EquipmentType.None) continue;

            string itemId = PlayerPrefs.GetString($"Equipped_{slotType}", "");
            if (!string.IsNullOrEmpty(itemId))
            {
                // Validate that the item still exists in registry
                var itemDef = inventoryManager?.GetItemRegistry()?.GetItem(itemId);
                if (itemDef != null && itemDef.IsEquipment() && itemDef.EquipmentSlot == slotType)
                {
                    equippedItems[slotType] = itemId;

                    // Update slot UI
                    var slot = GetSlotByType(slotType);
                    if (slot != null)
                    {
                        slot.SetEquippedItem(itemId);
                    }
                }
                else
                {
                    // Item no longer valid, clear the save
                    PlayerPrefs.DeleteKey($"Equipped_{slotType}");
                }
            }
        }
    }

    /// <summary>
    /// Clear all equipped items (for testing/debugging)
    /// </summary>
    public void ClearAllEquipment()
    {
        var slotsToUnequip = new List<EquipmentType>(equippedItems.Keys);
        foreach (var slotType in slotsToUnequip)
        {
            UnequipItem(slotType);
        }
    }

    /// <summary>
    /// Get debug info about current equipment state
    /// </summary>
    public string GetDebugInfo()
    {
        var info = new System.Text.StringBuilder();
        info.AppendLine("=== Equipment Panel Debug ===");
        info.AppendLine($"Equipped items: {equippedItems.Count}");

        foreach (var kvp in equippedItems)
        {
            var itemDef = inventoryManager?.GetItemRegistry()?.GetItem(kvp.Value);
            string itemName = itemDef?.GetDisplayName() ?? kvp.Value;
            info.AppendLine($"  {kvp.Key}: {itemName}");
        }

        return info.ToString();
    }
}

