// Purpose: Script for the panel displaying player inventory and equipped items.
// Filepath: Assets/Scripts/UI/Panels/InventoryPanel.cs
using UnityEngine;
// using UnityEngine.UI; // Potential dependency for slots, buttons
// using System.Collections.Generic; // Potential dependency

public class InventoryPanel : MonoBehaviour
{
    // TODO: References to UI containers/elements (Inventory grid, Equipment slots, Item details area?)
    // public Transform inventoryGridContainer;
    // public GameObject itemSlotPrefab; // Reusable prefab for inventory/equipment slots
    // public Transform weaponSlotContainer; // Specific containers for equipment slots
    // public Transform headSlotContainer;
    // ... etc.
    // public Text itemDetailsName; // Area to show info when slot is selected
    // public Text itemDetailsDescription;
    // public Button equipButton;
    // public Button unequipButton;
    // public Button useButton; // For consumables

    // TODO: Reference InventoryManager
    // private InventoryManager inventoryManager;
    // TODO: Reference EquipmentManager
    // private EquipmentManager equipmentManager;
    // TODO: Store the currently selected item slot (if any)
    // private ItemSlotUI selectedSlot;

    void OnEnable()
    {
        // TODO: Get manager references
        // TODO: Subscribe to InventoryManager.OnInventoryChanged, EquipmentManager.OnEquipmentChanged events
        // TODO: Populate inventory and equipment slots
        // RefreshInventory();
        // RefreshEquipment();
        // ClearItemDetails(); // Initially no item selected
    }

    void OnDisable()
    {
        // TODO: Unsubscribe from events
    }

    void RefreshInventory()
    {
        // TODO: Clear inventoryGridContainer
        // TODO: Get all items from InventoryManager
        // TODO: For each item stack:
        //      - Instantiate itemSlotPrefab into inventoryGridContainer
        //      - Get ItemSlotUI component from prefab instance
        //      - Call itemSlotUI.Setup(...) with item data (ID, quantity, icon from registry?)
        //      - Add listener to slot's button to handle selection (e.g., OnSlotSelected(itemSlotUI))
        Debug.Log("InventoryPanel: RefreshInventory (Placeholder)");
    }

    void RefreshEquipment()
    {
        // TODO: For each equipment slot (Weapon, Head, etc.):
        //      - Get the equipped item ID from EquipmentManager for that slot
        //      - Find the corresponding slot container (e.g., weaponSlotContainer)
        //      - Clear the container
        //      - If an item is equipped:
        //          - Instantiate itemSlotPrefab into the container
        //          - Setup the slot UI with item data (quantity likely 1)
        //          - Add listener for selection
        //      - Else (slot is empty):
        //          - Maybe show a default empty slot graphic?
        Debug.Log("InventoryPanel: RefreshEquipment (Placeholder)");
    }

    void OnSlotSelected(ItemSlotUI slot)
    {
        // TODO: Store the selectedSlot reference
        // TODO: Get item definition based on slot.ItemID
        // TODO: Update itemDetailsName, itemDetailsDescription etc.
        // TODO: Set visibility/interactability of Equip/Unequip/Use buttons based on context
        //      - Equip: If selected slot is in inventory and item is equippable
        //      - Unequip: If selected slot is an equipment slot with an item
        //      - Use: If selected slot is in inventory and item is consumable
        Debug.Log($"InventoryPanel: Slot selected {slot?.gameObject.name} (Placeholder)");
    }

    public void OnEquipButtonClicked()
    {
        // TODO: Check if selectedSlot is valid and is an equippable item from inventory
        // TODO: Call EquipmentManager.EquipGear(selectedSlot.ItemID)
        // TODO: Refresh UI or rely on event callbacks
    }

    public void OnUnequipButtonClicked()
    {
        // TODO: Check if selectedSlot is valid and is an equipment slot
        // TODO: Determine the EquipmentSlot enum based on the selectedSlot's container/parent
        // TODO: Call EquipmentManager.UnequipGear(slotEnum)
        // TODO: Refresh UI or rely on event callbacks
    }

    public void OnUseButtonClicked()
    {
        // TODO: Check if selectedSlot is valid and is a consumable item from inventory
        // TODO: Call InventoryManager.RemoveItem(selectedSlot.ItemID, 1)
        // TODO: Apply consumable effect (e.g., call PlayerController.Heal, apply buff)
        // TODO: Refresh UI or rely on event callbacks
    }

    void ClearItemDetails()
    {
        // TODO: Clear item details text fields
        // TODO: Disable Equip/Unequip/Use buttons
        // selectedSlot = null;
    }
}