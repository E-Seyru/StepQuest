// Purpose: Visual component for displaying an item while it's being dragged
// Filepath: Assets/Scripts/UI/Components/DraggedItemVisual.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component for displaying an item while it's being dragged
/// This should be attached to a prefab that will be instantiated during drag operations
/// </summary>
public class DraggedItemVisual : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Visual Settings")]
    [SerializeField] private float dragAlpha = 0.8f;
    [SerializeField] private bool hideQuantityIfOne = true;

    void Awake()
    {
        // Configure le CanvasGroup pour ne pas bloquer les raycasts
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = dragAlpha;
        }
    }

    /// <summary>
    /// Configure this visual component with item data
    /// </summary>
    public void Setup(string itemId, int quantity)
    {
        var itemDef = InventoryManager.Instance?.GetItemRegistry()?.GetItem(itemId);
        if (itemDef == null)
        {
            Debug.LogError($"DraggedItemVisual: Item '{itemId}' not found in registry");
            return;
        }

        // Configure l'icône
        if (itemIcon != null)
        {
            itemIcon.sprite = itemDef.ItemIcon;
            itemIcon.color = itemDef.ItemColor;
        }

        // Configure la quantite
        if (quantityText != null)
        {
            if (quantity > 1 || !hideQuantityIfOne)
            {
                quantityText.text = quantity.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }

        Debug.Log($"DraggedItemVisual: Setup for {quantity}x {itemDef.GetDisplayName()}");
    }

    /// <summary>
    /// Update the quantity display (useful for partial drags)
    /// </summary>
    public void UpdateQuantity(int newQuantity)
    {
        if (quantityText != null)
        {
            if (newQuantity > 1 || !hideQuantityIfOne)
            {
                quantityText.text = newQuantity.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Set the alpha transparency of this visual
    /// </summary>
    public void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
    }
}