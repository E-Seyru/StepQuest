// Purpose: Simple data structure representing a single slot in an inventory
// Filepath: Assets/Scripts/Data/Models/InventorySlot.cs
using System;

[Serializable]
public class InventorySlot
{
    public string ItemID;
    public int Quantity;

    /// <summary>
    /// Create an empty inventory slot
    /// </summary>
    public InventorySlot()
    {
        ItemID = string.Empty;
        Quantity = 0;
    }

    /// <summary>
    /// Create an inventory slot with specific item and quantity
    /// </summary>
    public InventorySlot(string itemId, int quantity)
    {
        ItemID = itemId;
        Quantity = quantity;
    }

    /// <summary>
    /// Check if this slot is empty
    /// </summary>
    public bool IsEmpty()
    {
        return string.IsNullOrEmpty(ItemID) || Quantity <= 0;
    }

    /// <summary>
    /// Check if this slot contains a specific item
    /// </summary>
    public bool HasItem(string itemId)
    {
        return !IsEmpty() && ItemID == itemId;
    }

    /// <summary>
    /// Clear this slot
    /// </summary>
    public void Clear()
    {
        ItemID = string.Empty;
        Quantity = 0;
    }

    /// <summary>
    /// Set the contents of this slot
    /// </summary>
    public void SetItem(string itemId, int quantity)
    {
        ItemID = itemId;
        Quantity = quantity;
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
        return new InventorySlot(ItemID, Quantity);
    }

    /// <summary>
    /// String representation for debugging
    /// </summary>
    public override string ToString()
    {
        if (IsEmpty())
            return "[Empty Slot]";

        return $"[{ItemID} x{Quantity}]";
    }
}