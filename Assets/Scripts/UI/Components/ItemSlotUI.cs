// Purpose: Script for a reusable UI element representing an item slot in inventory or equipment.
// Filepath: Assets/Scripts/UI/Components/ItemSlotUI.cs
using UnityEngine;
// using UnityEngine.UI; // For Image, Text
// using UnityEngine.EventSystems; // For drag/drop or click interfaces

public class ItemSlotUI : MonoBehaviour // Potentially implement IPointerClickHandler, IDragHandler etc.
{
    // TODO: References to child UI elements (Item Icon Image, Quantity Text, Background Image?)
    // public Image itemIcon;
    // public Text quantityText;
    // public Image background; // Can change color if selected, etc.

    // TODO: Store the Item ID and quantity this slot represents
    // public string ItemID { get; private set; }
    // public int Quantity { get; private set; }

    public void Setup(/* ItemDefinition */ object itemDef, int quantity)
    {
        // TODO: Set ItemID and Quantity
        // TODO: Set itemIcon.sprite from definition (via ItemRegistry?)
        // TODO: Set quantityText.text (hide if quantity <= 1?)
        // TODO: Ensure slot is visible
        Debug.Log($"ItemSlotUI: Setup for {itemDef} x{quantity} (Placeholder)");
    }

    public void SetQuantity(int quantity)
    {
        // TODO: Update Quantity property and quantityText.text
    }

    public void SetSelected(bool isSelected)
    {
        // TODO: Change background color/border to indicate selection
        // background.color = isSelected ? Color.yellow : Color.white; // Example
    }

    public void SetEmpty()
    {
        // TODO: Clear icon, quantity text, reset background
        // ItemID = null;
        // Quantity = 0;
        // itemIcon.sprite = null; // Or default empty sprite
        // quantityText.text = "";
        // gameObject.SetActive(false); // Optionally hide empty slots in grid? Or show placeholder
    }

    // TODO: Implement event handling interfaces if needed (e.g., OnPointerClick)
    // public void OnPointerClick(PointerEventData eventData) { /* Notify parent panel */ }
}