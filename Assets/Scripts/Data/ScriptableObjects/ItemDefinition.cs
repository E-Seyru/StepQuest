// Purpose: Simple ScriptableObject defining game items (materials, equipment, consumables, etc.)
// Filepath: Assets/Scripts/Data/ScriptableObjects/ItemDefinition.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "WalkAndRPG/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Unique identifier for this item")]
    public string ItemID;

    [Tooltip("Display name shown in UI")]
    public string ItemName;

    [Tooltip("Description of this item")]
    [TextArea(2, 4)]
    public string Description;

    [Header("Visual")]
    [Tooltip("Icon representing this item")]
    public Sprite ItemIcon;

    [Tooltip("Color theme for this item")]
    public Color ItemColor = Color.white;

    [Header("Game Values")]
    [Tooltip("Base value/price of this item")]
    public int BasePrice = 1;

    [Tooltip("Rarity tier (1=common, 5=legendary)")]
    [Range(1, 5)]
    public int RarityTier = 1;

    [Header("Inventory Behavior")]
    [Tooltip("What type of item this is")]
    public ItemType Type = ItemType.Material;

    [Tooltip("Can this item be stacked in inventory?")]
    public bool IsStackable = true;

    [Tooltip("Maximum stack size (only relevant if IsStackable is true)")]
    public int MaxStackSize = 99;

    [Header("Equipment (if applicable)")]
    [Tooltip("Equipment slot type (None if not equipment)")]
    public EquipmentType EquipmentSlot = EquipmentType.None;

    [Tooltip("Number of inventory slots this item provides (for backpacks only)")]
    public int InventorySlots = 0;

    /// <summary>
    /// Get display info for UI
    /// </summary>
    public string GetDisplayName()
    {
        return !string.IsNullOrEmpty(ItemName) ? ItemName : ItemID;
    }

    /// <summary>
    /// Get rarity display text
    /// </summary>
    public string GetRarityText()
    {
        return RarityTier switch
        {
            1 => "Commun",
            2 => "Peu commun",
            3 => "Rare",
            4 => "epique",
            5 => "Legendaire",
            _ => "Inconnu"
        };
    }

    /// <summary>
    /// Get rarity color
    /// </summary>
    public Color GetRarityColor()
    {
        return RarityTier switch
        {
            1 => Color.gray,
            2 => Color.green,
            3 => Color.blue,
            4 => new Color(0.6f, 0.0f, 1.0f), // Purple
            5 => new Color(1.0f, 0.6f, 0.0f), // Orange
            _ => Color.white
        };
    }

    /// <summary>
    /// Check if this item is equipment
    /// </summary>
    public bool IsEquipment()
    {
        return EquipmentSlot != EquipmentType.None;
    }

    /// <summary>
    /// Check if this item is a backpack
    /// </summary>
    public bool IsBackpack()
    {
        return EquipmentSlot == EquipmentType.Backpack;
    }

    /// <summary>
    /// Validate this item definition
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(ItemID))
        {
            Logger.LogError($"ItemDefinition '{name}': ItemID is empty!", Logger.LogCategory.General);
            return false;
        }

        if (IsStackable && MaxStackSize <= 0)
        {
            Logger.LogError($"ItemDefinition '{ItemID}': IsStackable is true but MaxStackSize is {MaxStackSize}!", Logger.LogCategory.General);
            return false;
        }

        if (IsBackpack() && InventorySlots <= 0)
        {
            Logger.LogWarning($"ItemDefinition '{ItemID}': Backpack has {InventorySlots} inventory slots!", Logger.LogCategory.General);
        }

        return true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-generate ItemID from name if empty
        if (string.IsNullOrEmpty(ItemID) && !string.IsNullOrEmpty(name))
        {
            ItemID = name.ToLower().Replace(" ", "_");
        }

        // Auto-generate ItemName from ItemID if empty
        if (string.IsNullOrEmpty(ItemName) && !string.IsNullOrEmpty(ItemID))
        {
            ItemName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ItemID.Replace("_", " "));
        }

        // Auto-set MaxStackSize to 1 for equipment
        if (IsEquipment() && MaxStackSize > 1)
        {
            MaxStackSize = 1;
            IsStackable = false;
        }

        // Ensure InventorySlots is 0 for non-backpack items
        if (!IsBackpack() && InventorySlots > 0)
        {
            InventorySlots = 0;
        }
    }
#endif
}

/// <summary>
/// Types of items available in the game
/// </summary>
public enum ItemType
{
    Equipment,   // Weapons, armor, etc.
    Material,    // Crafting materials
    Consumable,  // Health potions, food, etc. 
    Usable,      // Tools, keys, special items
    Currency,    // Gold, gems, etc.
    Quest,       // Quest items
    Miscellaneous // Other items
}

/// <summary>
/// Equipment slot types
/// </summary>
public enum EquipmentType
{
    None,        // Pas un equipement
    Weapon,      // Arme principale
    Tool,        // Outils (pioche, hache, canne a pêche)
    Helmet,      // Casque
    Armor,       // Armure de corps
    Legs,        // Jambières
    Boots,       // Bottes
    Gloves,      // Gants
    Backpack,    // Sac a dos (augmente l'inventaire)
    Ring,        // Bague
    Necklace     // Collier
}