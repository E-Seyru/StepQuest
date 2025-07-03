// Purpose: Interface for any slot that can participate in drag and drop operations
// Filepath: Assets/Scripts/UI/Interfaces/IDragDropSlot.cs
using UnityEngine;

/// <summary>
/// Interface for any UI slot that can participate in drag and drop operations
/// </summary>
public interface IDragDropSlot
{
    // === Data Access ===

    /// <summary>
    /// Get the item ID in this slot (null if empty)
    /// </summary>
    string GetItemId();

    /// <summary>
    /// Get the quantity of items in this slot
    /// </summary>
    int GetQuantity();

    /// <summary>
    /// Check if this slot is empty
    /// </summary>
    bool IsEmpty();

    // === Validation ===

    /// <summary>
    /// Check if this slot can accept a specific item
    /// </summary>
    bool CanAcceptItem(string itemID, int qty);

    // === Modification ===

    /// <summary>
    /// Try to set the item in this slot (replace current contents)
    /// </summary>
    bool TrySetItem(string itemID, int qty);

    /// <summary>
    /// Try to remove items from this slot
    /// </summary>
    bool TryRemoveItem(int qty);

    // === Visual Feedback ===

    /// <summary>
    /// Called when a drag enters this slot
    /// </summary>
    void OnDragEnter();

    /// <summary>
    /// Called when a drag exits this slot
    /// </summary>
    void OnDragExit();

    // === Unity Component Access ===

    /// <summary>
    /// Get the RectTransform for positioning calculations
    /// </summary>
    RectTransform GetRectTransform();
}