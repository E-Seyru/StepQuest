// Purpose: Data structure representing an item in the game (definition and instance).
// Filepath: Assets/Scripts/Data/Models/ItemData.cs
using System.Collections.Generic; // For stat dictionaries

// Use ScriptableObjects for item definitions to create them in the editor
// Create -> WalkAndRPG -> Item Definition
// using UnityEngine;
// [CreateAssetMenu(fileName = "NewItem", menuName = "WalkAndRPG/Item Definition")]
// public class ItemDefinition : ScriptableObject {
//     public string ItemID; // Unique identifier
//     public string DisplayName;
//     [TextArea] public string Description;
//     public ItemType Type;
//     // public Sprite Icon; // Assign in Inspector
//     public bool IsStackable;
//     public int MaxStackSize = 99;
//
//     // Gear specific stats
//     // public Dictionary<string, int> StatModifiers;
//
//     // Consumable specific effects
//     // public List<EffectData> Effects; // Reference effect scriptable objects?
// }

// This class represents an item instance in the player's inventory
[System.Serializable]
public class InventoryItemData
{
    public string ItemID; // Reference to the ItemDefinition's ID
    public int Quantity;
    // public string InstanceID; // Optional: For unique non-stackable items (e.g., specific sword)
    // public int CurrentDurability; // Optional: If items have durability

    public InventoryItemData(string itemId, int quantity = 1)
    {
        ItemID = itemId;
        Quantity = quantity;
    }
}

// Define item types
public enum ItemType
{
    Gear_Weapon,
    Gear_Armor,
    Gear_Accessory,
    Consumable_Potion,
    Consumable_Food,
    Material_Crafting,
    Material_Resource,
    QuestItem,
    Currency // Although currency might be handled separately
}