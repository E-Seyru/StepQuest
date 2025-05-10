// Purpose: Manages the player's equipped gear and abilities.
// Filepath: Assets/Scripts/Gameplay/Player/EquipmentManager.cs
using UnityEngine;
using System.Collections.Generic; // For Dictionary/List
using System; // For Action

public class EquipmentManager : MonoBehaviour
{
    // TODO: Reference DataManager to access equipped data
    // private DataManager dataManager;
    // TODO: Reference InventoryManager to check if items exist
    // private InventoryManager inventoryManager;
    // TODO: Reference PlayerController to trigger stat recalculation
    // private PlayerController playerController;

    // TODO: Store currently equipped gear (e.g., Dictionary<EquipmentSlot, string> where string is ItemID)
    // private Dictionary<EquipmentSlot, string> equippedGear;
    // TODO: Store currently equipped abilities (e.g., List<string> where string is AbilityID)
    // private List<string> equippedAbilities;

    // TODO: Track current total ability weight
    // private int currentAbilityWeight;
    // TODO: Define max ability weight (maybe based on player level or skill?)
    // public int MaxAbilityWeight { get; private set; } = 10; // Example

    // TODO: Define events for equipment/ability changes
    // public event Action OnEquipmentChanged;
    // public event Action OnAbilitiesChanged;

    void Start()
    {
        // TODO: Get references to other managers
        // TODO: Load equipped gear/abilities data from DataManager
        // TODO: Calculate initial ability weight
    }

    public bool EquipGear(string itemId)
    {
        // TODO: Get item definition (from ItemRegistry?) to find its type/slot
        // TODO: Check if the item is in the inventory
        // TODO: Determine the correct EquipmentSlot based on item type
        // TODO: Unequip existing item in that slot (add back to inventory if needed)
        // TODO: Remove the item from inventory (or reduce count if needed)
        // TODO: Update equippedGear dictionary
        // TODO: Update DataManager's corresponding data structure
        // TODO: Trigger OnEquipmentChanged event
        // TODO: Trigger playerController.RecalculateStats()
        // TODO: Return true if successful
        Debug.Log($"EquipmentManager: EquipGear {itemId} (Placeholder)");
        return true; // Placeholder
    }

    public bool UnequipGear(EquipmentSlot slot)
    {
        // TODO: Check if an item is equipped in the slot
        // TODO: Get the ItemID of the equipped item
        // TODO: Add the item back to the inventory
        // TODO: Remove the item from equippedGear dictionary
        // TODO: Update DataManager
        // TODO: Trigger OnEquipmentChanged event
        // TODO: Trigger playerController.RecalculateStats()
        // TODO: Return true if successful
        Debug.Log($"EquipmentManager: UnequipGear from slot {slot} (Placeholder)");
        return true; // Placeholder
    }

    public bool EquipAbility(string abilityId)
    {
        // TODO: Get ability definition (from AbilityRegistry?) to find its weight
        // TODO: Check if ability is already equipped
        // TODO: Check if adding the ability exceeds MaxAbilityWeight
        // TODO: Add abilityId to equippedAbilities list
        // TODO: Update currentAbilityWeight
        // TODO: Update DataManager
        // TODO: Trigger OnAbilitiesChanged event
        // TODO: Return true if successful
        Debug.Log($"EquipmentManager: EquipAbility {abilityId} (Placeholder)");
        return true; // Placeholder
    }

    public bool UnequipAbility(string abilityId)
    {
        // TODO: Check if ability is equipped
        // TODO: Get ability definition to find its weight
        // TODO: Remove abilityId from equippedAbilities list
        // TODO: Update currentAbilityWeight
        // TODO: Update DataManager
        // TODO: Trigger OnAbilitiesChanged event
        // TODO: Return true if successful
        Debug.Log($"EquipmentManager: UnequipAbility {abilityId} (Placeholder)");
        return true; // Placeholder
    }

    // TODO: Add methods to get equipped item in a slot, get list of equipped abilities, get current weight etc.
    // public string GetEquippedItem(EquipmentSlot slot) { ... }
    // public List<string> GetEquippedAbilities() { ... }
    // public int GetCurrentAbilityWeight() { ... }

}

// Define equipment slots
public enum EquipmentSlot
{
    Weapon,
    Armor_Head,
    Armor_Chest,
    Armor_Legs,
    Armor_Feet,
    Accessory1,
    Accessory2
    // Add more as needed
}