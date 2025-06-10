// Purpose: UI component for displaying a single inventory slot
// Filepath: Assets/Scripts/UI/Components/InventorySlotUI.cs
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Button slotButton;
    [SerializeField] private Image background;

    [Header("Visual Settings")]
    [SerializeField] private Color emptyColor = Color.gray;
    [SerializeField] private Color filledColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;
    [SerializeField] private Sprite emptySlotSprite; // Sprite à afficher quand le slot est vide

    // Data
    private InventorySlot slotData;
    private int slotIndex;
    private bool isSelected = false;

    // Events
    public event Action<InventorySlotUI, int> OnSlotClicked; // Slot, Index

    void Awake()
    {
        // Setup button click
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotButtonClicked);
        }

        // Validate references
        ValidateReferences();
    }

    /// <summary>
    /// Setup this slot with data and index
    /// </summary>
    public void Setup(InventorySlot slot, int index)
    {
        slotData = slot;
        slotIndex = index;
        RefreshDisplay();
    }

    /// <summary>
    /// Refresh the visual display based on current slot data
    /// </summary>
    public void RefreshDisplay()
    {
        if (slotData == null || slotData.IsEmpty())
        {
            ShowEmptySlot();
        }
        else
        {
            ShowFilledSlot();
        }
    }

    /// <summary>
    /// Show empty slot state
    /// </summary>
    private void ShowEmptySlot()
    {
        if (itemIcon != null)
        {
            // MODIFIÉ: Affiche un sprite vide ou rend transparent
            if (emptySlotSprite != null)
            {
                itemIcon.sprite = emptySlotSprite;
                itemIcon.color = new Color(1, 1, 1, 0.3f); // Semi-transparent
            }
            else
            {
                itemIcon.sprite = null;
                itemIcon.color = new Color(1, 1, 1, 0); // Complètement transparent
            }
        }

        if (quantityText != null)
        {
            quantityText.text = "";
        }

        if (background != null)
        {
            background.color = isSelected ? selectedColor : emptyColor;
        }
    }

    /// <summary>
    /// Show filled slot state
    /// </summary>
    private void ShowFilledSlot()
    {
        // MODIFIÉ: Récupère la vraie icône via ItemRegistry
        if (itemIcon != null)
        {
            var itemDefinition = GetItemDefinition(slotData.ItemID);
            if (itemDefinition != null && itemDefinition.ItemIcon != null)
            {
                // Affiche la vraie icône de l'objet
                itemIcon.sprite = itemDefinition.ItemIcon;
                itemIcon.color = itemDefinition.ItemColor; // Utilise la couleur de l'item
            }
            else
            {
                // Fallback: icône par défaut ou couleur unie
                itemIcon.sprite = emptySlotSprite;
                itemIcon.color = Color.white;
                Logger.LogWarning($"InventorySlotUI: No icon found for item '{slotData.ItemID}'", Logger.LogCategory.InventoryLog);
            }
        }

        // Show quantity if more than 1
        if (quantityText != null)
        {
            if (slotData.Quantity > 1)
            {
                quantityText.text = $"x{slotData.Quantity}";

                // NOUVEAU: Couleur du texte basée sur la rareté
                var itemDef = GetItemDefinition(slotData.ItemID);
                if (itemDef != null)
                {
                    quantityText.color = itemDef.GetRarityColor();
                }
            }
            else
            {
                quantityText.text = "";
            }
        }

        // MODIFIÉ: Couleur de fond basée sur la rareté
        if (background != null)
        {
            if (isSelected)
            {
                background.color = selectedColor;
            }
            else
            {
                var itemDef = GetItemDefinition(slotData.ItemID);
                if (itemDef != null)
                {
                    // Mélange la couleur de rareté avec la couleur de base
                    Color rarityColor = itemDef.GetRarityColor();
                    background.color = Color.Lerp(filledColor, rarityColor, 0.3f); // 30
                                                                                   // de couleur de rareté
                }
                else
                {
                    background.color = filledColor;
                }
            }
        }
    }

    /// <summary>
    /// Set selection state
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        RefreshDisplay();
    }

    /// <summary>
    /// Handle slot button click
    /// </summary>
    private void OnSlotButtonClicked()
    {
        OnSlotClicked?.Invoke(this, slotIndex);

        // MODIFIÉ: Log plus informatif avec le nom de l'objet
        if (slotData != null && !slotData.IsEmpty())
        {

            ShowItemActionPanel();

            var itemDef = GetItemDefinition(slotData.ItemID);
            string itemName = itemDef?.GetDisplayName() ?? slotData.ItemID;
            Debug.Log($"Slot {slotIndex} clicked - {itemName} x{slotData.Quantity}");
        }
        else
        {
            Debug.Log($"Slot {slotIndex} clicked - Empty");
        }
    }

    /// <summary>
    /// Get slot data
    /// </summary>
    public InventorySlot GetSlotData()
    {
        return slotData;
    }

    /// <summary>
    /// Get slot index
    /// </summary>
    public int GetSlotIndex()
    {
        return slotIndex;
    }

    /// <summary>
    /// Check if this slot is empty
    /// </summary>
    public bool IsEmpty()
    {
        return slotData?.IsEmpty() ?? true;
    }

    /// <summary>
    /// MODIFIÉ: Get item definition via InventoryManager's ItemRegistry
    /// </summary>
    private ItemDefinition GetItemDefinition(string itemId)
    {
        if (InventoryManager.Instance?.GetItemRegistry() != null)
        {
            return InventoryManager.Instance.GetItemRegistry().GetItem(itemId);
        }

        Logger.LogWarning("InventorySlotUI: Cannot get ItemDefinition - InventoryManager or ItemRegistry not available", Logger.LogCategory.InventoryLog);
        return null;
    }

    /// <summary>
    /// NOUVEAU: Get tooltip text for this slot
    /// </summary>
    public string GetTooltipText()
    {
        if (slotData == null || slotData.IsEmpty())
        {
            return "Slot vide";
        }

        var itemDef = GetItemDefinition(slotData.ItemID);
        if (itemDef == null)
        {
            return $"{slotData.ItemID} x{slotData.Quantity}";
        }

        // Crée un tooltip riche avec infos de l'objet
        string tooltip = $"<b>{itemDef.GetDisplayName()}</b>";

        if (slotData.Quantity > 1)
        {
            tooltip += $" x{slotData.Quantity}";
        }

        tooltip += $"\n<i>{itemDef.GetRarityText()}</i>";

        if (!string.IsNullOrEmpty(itemDef.Description))
        {
            tooltip += $"\n{itemDef.Description}";
        }

        if (itemDef.BasePrice > 0)
        {
            tooltip += $"\n<size=10>Valeur: {itemDef.BasePrice} or</size>";
        }

        return tooltip;
    }

    /// <summary>
    /// Validate that all required references are assigned
    /// </summary>
    private void ValidateReferences()
    {
        if (itemIcon == null)
            Debug.LogWarning($"InventorySlotUI: itemIcon not assigned on {gameObject.name}");

        if (quantityText == null)
            Debug.LogWarning($"InventorySlotUI: quantityText not assigned on {gameObject.name}");

        if (slotButton == null)
            Debug.LogWarning($"InventorySlotUI: slotButton not assigned on {gameObject.name}");

        if (background == null)
            Debug.LogWarning($"InventorySlotUI: background not assigned on {gameObject.name}");
    }

    // <summary>
    /// Show the item action panel for this slot
    /// </summary>
    private void ShowItemActionPanel()
    {
        if (ItemActionPanel.Instance == null)
        {
            Debug.LogError("InventorySlotUI: ItemActionPanel.Instance is NULL! Make sure ItemActionPanel exists in the scene!");
            return;
        }

        // Get world position of this slot for panel positioning
        Vector2 worldPosition = transform.position;

        // Show the action panel
        ItemActionPanel.Instance.ShowPanel(this, slotData, worldPosition);
        Debug.Log("InventorySlotUI: Called ShowPanel on ItemActionPanel");
    }



#if UNITY_EDITOR
    /// <summary>
    /// Auto-assign references in editor
    /// </summary>
    void Reset()
    {
        if (itemIcon == null)
        {
            // Cherche toutes les images et prend celle qui n'est pas le background
            Image[] images = GetComponentsInChildren<Image>();
            foreach (var img in images)
            {
                if (img != GetComponent<Image>()) // Pas l'image de background
                {
                    itemIcon = img;
                    break;
                }
            }
        }

        if (quantityText == null)
            quantityText = GetComponentInChildren<TextMeshProUGUI>();

        if (slotButton == null)
            slotButton = GetComponent<Button>();

        if (background == null)
            background = GetComponent<Image>();
    }
#endif
}