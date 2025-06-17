// Purpose: Interface for slots that can handle drag and drop operations
// Filepath: Assets/Scripts/UI/Interfaces/IDragDropSlot.cs
using UnityEngine;

/// <summary>
/// Interface for UI slots that can participate in drag and drop operations
/// </summary>
public interface IDragDropSlot
{
    /// <summary>
    /// Get the item ID currently in this slot (null if empty)
    /// </summary>
    string GetItemId();

    /// <summary>
    /// Get the quantity of the item in this slot
    /// </summary>
    int GetQuantity();

    /// <summary>
    /// Check if this slot can accept a specific item for dropping
    /// </summary>
    bool CanAcceptItem(string itemId, int quantity);

    /// <summary>
    /// Try to set an item in this slot (for drop operation)
    /// </summary>
    /// <returns>True if successful</returns>
    bool TrySetItem(string itemId, int quantity);

    /// <summary>
    /// Try to remove an item from this slot (for drag operation)
    /// </summary>
    /// <returns>True if successful</returns>
    bool TryRemoveItem(int quantity);

    /// <summary>
    /// Get the RectTransform of this slot for positioning
    /// </summary>
    RectTransform GetRectTransform();

    /// <summary>
    /// Visual feedback when an item is being dragged over this slot
    /// </summary>
    void OnDragEnter();

    /// <summary>
    /// Visual feedback when an item stops being dragged over this slot
    /// </summary>
    void OnDragExit();

    /// <summary>
    /// Check if this slot is currently empty
    /// </summary>
    bool IsEmpty();
}