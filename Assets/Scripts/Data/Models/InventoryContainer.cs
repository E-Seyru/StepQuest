// Purpose: Main container class that holds inventory slots and manages capacity
// Filepath: Assets/Scripts/Data/Models/InventoryContainer.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class InventoryContainer
{
    [Header("Container Info")]
    public InventoryContainerType ContainerType;
    public string ContainerID;

    [Header("Capacity")]
    public int MaxSlots;

    [Header("Slots")]
    public List<InventorySlot> Slots;

    [Header("Metadata")]
    public Dictionary<string, string> Metadata;

    /// <summary>
    /// Create a new inventory container
    /// </summary>
    public InventoryContainer(InventoryContainerType type, string id, int maxSlots)
    {
        ContainerType = type;
        ContainerID = id;
        MaxSlots = maxSlots;
        Slots = new List<InventorySlot>();
        Metadata = new Dictionary<string, string>();

        // Initialize empty slots
        for (int i = 0; i < maxSlots; i++)
        {
            Slots.Add(new InventorySlot());
        }
    }

    /// <summary>
    /// Get the number of used (non-empty) slots
    /// </summary>
    public int GetUsedSlotsCount()
    {
        return Slots.Count(slot => !slot.IsEmpty());
    }

    /// <summary>
    /// Get the number of available slots
    /// </summary>
    public int GetAvailableSlots()
    {
        return MaxSlots - GetUsedSlotsCount();
    }

    /// <summary>
    /// Check if container is full
    /// </summary>
    public bool IsFull()
    {
        return GetAvailableSlots() <= 0;
    }

    /// <summary>
    /// Find first empty slot index
    /// </summary>
    /// <returns>Slot index or -1 if no empty slots</returns>
    public int FindFirstEmptySlot()
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].IsEmpty())
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Find first slot containing specific item
    /// </summary>
    /// <returns>Slot index or -1 if item not found</returns>
    public int FindItemSlot(string itemId)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            if (Slots[i].HasItem(itemId))
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Get total quantity of specific item in container
    /// </summary>
    public int GetItemQuantity(string itemId)
    {
        int total = 0;
        foreach (var slot in Slots)
        {
            if (slot.HasItem(itemId))
                total += slot.Quantity;
        }
        return total;
    }

    /// <summary>
    /// Check if container has enough of specific item
    /// </summary>
    public bool HasItem(string itemId, int requiredQuantity = 1)
    {
        return GetItemQuantity(itemId) >= requiredQuantity;
    }

    /// <summary>
    /// Get all non-empty slots
    /// </summary>
    public List<InventorySlot> GetNonEmptySlots()
    {
        return Slots.Where(slot => !slot.IsEmpty()).ToList();
    }

    /// <summary>
    /// Get all slots containing a specific item
    /// </summary>
    public List<InventorySlot> GetItemSlots(string itemId)
    {
        return Slots.Where(slot => slot.HasItem(itemId)).ToList();
    }

    /// <summary>
    /// Resize the container (add or remove slots)
    /// </summary>
    public void Resize(int newMaxSlots)
    {
        if (newMaxSlots < 0) return;

        // If expanding
        if (newMaxSlots > MaxSlots)
        {
            int slotsToAdd = newMaxSlots - MaxSlots;
            for (int i = 0; i < slotsToAdd; i++)
            {
                Slots.Add(new InventorySlot());
            }
        }
        // If shrinking
        else if (newMaxSlots < MaxSlots)
        {
            // Only shrink if we won't lose items
            int usedSlots = GetUsedSlotsCount();
            if (newMaxSlots >= usedSlots)
            {
                // Remove empty slots from the end
                while (Slots.Count > newMaxSlots)
                {
                    if (Slots[Slots.Count - 1].IsEmpty())
                    {
                        Slots.RemoveAt(Slots.Count - 1);
                    }
                    else
                    {
                        break; // Can't shrink further without losing items
                    }
                }
            }
            else
            {
                Logger.LogWarning($"InventoryContainer: Cannot resize to {newMaxSlots} slots - would lose items (used: {usedSlots})", Logger.LogCategory.InventoryLog);
                return;
            }
        }

        MaxSlots = newMaxSlots;
        Logger.LogInfo($"InventoryContainer: Resized {ContainerID} to {MaxSlots} slots", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Clear all slots in the container
    /// </summary>
    public void Clear()
    {
        foreach (var slot in Slots)
        {
            slot.Clear();
        }
        Logger.LogInfo($"InventoryContainer: Cleared all slots in {ContainerID}", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Get or set metadata value
    /// </summary>
    public string GetMetadata(string key, string defaultValue = "")
    {
        return Metadata.TryGetValue(key, out string value) ? value : defaultValue;
    }

    public void SetMetadata(string key, string value)
    {
        Metadata[key] = value;
    }

    /// <summary>
    /// Get container info for debugging
    /// </summary>
    public string GetDebugInfo()
    {
        return $"Container '{ContainerID}' ({ContainerType}): {GetUsedSlotsCount()}/{MaxSlots} slots used";
    }

    /// <summary>
    /// String representation for debugging
    /// </summary>
    public override string ToString()
    {
        return GetDebugInfo();
    }
}

/// <summary>
/// Types of inventory containers
/// </summary>
public enum InventoryContainerType
{
    Player,     // Player's main inventory
    Bank,       // Bank storage
    Shop,       // Shop inventory
    Temporary   // Temporary containers (trades, etc.)
}