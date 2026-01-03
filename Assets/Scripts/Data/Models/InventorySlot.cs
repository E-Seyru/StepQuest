// Purpose: Simple data structure representing a single slot in an inventory
// Filepath: Assets/Scripts/Data/Models/InventorySlot.cs
using System;

[Serializable]
public class InventorySlot
{
    public string ItemID;
    public int Quantity;

    /// <summary>
    /// The rarity tier of the item in this slot (1-5, or 0 for items without rarity variants)
    /// Items only stack if they have the same ItemID AND RarityTier
    /// </summary>
    public int RarityTier;

    /// <summary>
    /// Create an empty inventory slot
    /// </summary>
    public InventorySlot()
    {
        ItemID = string.Empty;
        Quantity = 0;
        RarityTier = 0;
    }

    /// <summary>
    /// Create an inventory slot with specific item and quantity
    /// </summary>
    public InventorySlot(string itemId, int quantity)
    {
        ItemID = itemId;
        Quantity = quantity;
        RarityTier = 0;
    }

    /// <summary>
    /// Create an inventory slot with specific item, quantity, and rarity
    /// </summary>
    public InventorySlot(string itemId, int quantity, int rarityTier)
    {
        ItemID = itemId;
        Quantity = quantity;
        RarityTier = rarityTier;
    }

    /// <summary>
    /// Check if this slot is empty
    /// </summary>
    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(ItemID) || Quantity <= 0;
    }

    /// <summary>
    /// Check if this slot contains a specific item (ignores rarity)
    /// </summary>
    public bool HasItem(string itemId)
    {
        return !IsEmpty() && ItemID == itemId;
    }

    /// <summary>
    /// Check if this slot contains a specific item with a specific rarity
    /// Use this for stacking checks
    /// </summary>
    public bool HasItemWithRarity(string itemId, int rarityTier)
    {
        return !IsEmpty() && ItemID == itemId && RarityTier == rarityTier;
    }

    /// <summary>
    /// Clear this slot
    /// </summary>
    public void Clear()
    {
        ItemID = string.Empty;
        Quantity = 0;
        RarityTier = 0;
    }

    /// <summary>
    /// Set the contents of this slot (without rarity)
    /// </summary>
    public void SetItem(string itemId, int quantity)
    {
        ItemID = itemId;
        Quantity = quantity;
        RarityTier = 0;
    }

    /// <summary>
    /// Set the contents of this slot with rarity
    /// </summary>
    public void SetItem(string itemId, int quantity, int rarityTier)
    {
        ItemID = itemId;
        Quantity = quantity;
        RarityTier = rarityTier;
    }

    /// <summary>
    /// Add quantity to this slot (assumes same item)
    /// </summary>
    public void AddQuantity(int amount)
    {
        if (amount > 0)
        {
            Quantity += amount;
        }
    }

    /// <summary>
    /// Remove quantity from this slot
    /// </summary>
    /// <returns>True if slot became empty</returns>
    public bool RemoveQuantity(int amount)
    {
        if (amount > 0)
        {
            Quantity -= amount;
            if (Quantity <= 0)
            {
                Clear();
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Get a copy of this slot
    /// </summary>
    public InventorySlot Clone()
    {
        return new InventorySlot(ItemID, Quantity, RarityTier);
    }

    /// <summary>
    /// String representation for debugging
    /// </summary>
    public override string ToString()
    {
        if (IsEmpty())
            return "[Empty Slot]";

        if (RarityTier > 0)
            return $"[{ItemID} x{Quantity} (R{RarityTier})]";

        return $"[{ItemID} x{Quantity}]";
    }
}