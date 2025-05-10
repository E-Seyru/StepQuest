// Purpose: Manages the player's inventory (items and quantities).
// Filepath: Assets/Scripts/Gameplay/Player/InventoryManager.cs
using UnityEngine;
using System.Collections.Generic; // For List/Dictionary
using System; // For Action

public class InventoryManager : MonoBehaviour
{
    // TODO: Reference DataManager to access inventory data (List<InventoryItemData>)
    // private DataManager dataManager;

    // TODO: Potentially use a Dictionary<string, InventoryItemData> for faster lookups by ItemID
    // private Dictionary<string, InventoryItemData> inventory;

    // TODO: Define event for inventory changes
    // public event Action OnInventoryChanged;

    void Start()
    {
        // TODO: Get reference to DataManager
        // TODO: Load inventory data from DataManager and populate internal structure
        // TODO: Subscribe to DataManager loaded event if needed
    }

    public void AddItem(string itemId, int quantity = 1)
    {
        // TODO: Check if item definition exists (e.g., via an ItemRegistry service)
        // TODO: Check if item is stackable
        // TODO: If stackable and already exists, increment quantity
        // TODO: If stackable and new, add new InventoryItemData
        // TODO: If not stackable, add new InventoryItemData for each quantity (or handle unique IDs)
        // TODO: Update the DataManager's inventory list/data
        // TODO: Trigger OnInventoryChanged event
        Debug.Log($"InventoryManager: AddItem {itemId} x{quantity} (Placeholder)");
    }

    public bool RemoveItem(string itemId, int quantity = 1)
    {
        // TODO: Find the item in the inventory
        // TODO: Check if sufficient quantity exists
        // TODO: Decrease quantity or remove item entry if quantity reaches zero
        // TODO: Update the DataManager's inventory list/data
        // TODO: Trigger OnInventoryChanged event
        // TODO: Return true if successful, false otherwise
        Debug.Log($"InventoryManager: RemoveItem {itemId} x{quantity} (Placeholder)");
        return true; // Placeholder
    }

    public int GetItemCount(string itemId)
    {
        // TODO: Find the item and return its quantity
        return 0; // Placeholder
    }

    public bool HasItem(string itemId, int quantity = 1)
    {
        // TODO: Check if the player has at least the specified quantity of the item
        return false; // Placeholder
    }

    public List<InventoryItemData> GetAllItems()
    {
        // TODO: Return a copy or reference to the current inventory list
        // return dataManager?.CurrentInventory ?? new List<InventoryItemData>();
        return new List<InventoryItemData>(); // Placeholder
    }
}