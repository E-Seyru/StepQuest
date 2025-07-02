// Purpose: Contextual panel that appears when clicking on an inventory item
// Filepath: Assets/Scripts/UI/Components/ItemActionPanel.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemActionPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private TextMeshProUGUI quantityText;

    [Header("Action Buttons")]
    [SerializeField] private Button discardButton;
    [SerializeField] private TextMeshProUGUI discardButtonText;
    [SerializeField] private Button bankButton;
    [SerializeField] private TextMeshProUGUI bankButtonText;
    [SerializeField] private Button useButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private TextMeshProUGUI sellButtonText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundButton; // Background button to close panel

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private LeanTweenType slideInEase = LeanTweenType.easeOutBack;
    [SerializeField] private LeanTweenType slideOutEase = LeanTweenType.easeInBack;

    // References
    private InventoryManager inventoryManager;
    private InventorySlotUI sourceSlot;
    private InventorySlot itemSlot;
    private ItemDefinition itemDefinition;

    // Animation
    private RectTransform rectTransform;
    private int currentTween = -1;

    // Context
    private bool isInBank = false;
    private bool isInShop = false;

    public static ItemActionPanel Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            rectTransform = GetComponent<RectTransform>();
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupButtons();
        gameObject.SetActive(false);
    }

    void Start()
    {
        inventoryManager = InventoryManager.Instance;

        if (inventoryManager == null)
        {
            Logger.LogError("ItemActionPanel: InventoryManager not found!", Logger.LogCategory.InventoryLog);
        }
    }

    /// <summary>
    /// Setup button click listeners
    /// </summary>
    private void SetupButtons()
    {
        if (discardButton != null)
            discardButton.onClick.AddListener(OnDiscardClicked);

        if (bankButton != null)
            bankButton.onClick.AddListener(OnBankClicked);

        if (useButton != null)
            useButton.onClick.AddListener(OnUseClicked);

        if (sellButton != null)
            sellButton.onClick.AddListener(OnSellClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        if (backgroundButton != null)
            backgroundButton.onClick.AddListener(OnCloseClicked);
    }

    /// <summary>
    /// Show panel for a specific item slot
    /// </summary>
    public void ShowPanel(InventorySlotUI slotUI, InventorySlot slot, Vector2 worldPosition)
    {
        if (slot == null || slot.IsEmpty())
        {
            Logger.LogWarning("ItemActionPanel: Cannot show panel for empty slot", Logger.LogCategory.InventoryLog);
            return;
        }

        // Store references
        sourceSlot = slotUI;
        itemSlot = slot;
        itemDefinition = GetItemDefinition(slot.ItemID);

        if (itemDefinition == null)
        {
            Logger.LogError($"ItemActionPanel: ItemDefinition not found for '{slot.ItemID}'", Logger.LogCategory.InventoryLog);
            return;
        }

        // Position panel
        PositionPanel(worldPosition);

        // Setup panel content
        SetupPanelContent();

        // Setup context-sensitive buttons
        SetupContextualButtons();

        // Show with animation
        ShowWithAnimation();

        Logger.LogInfo($"ItemActionPanel: Showing panel for {itemDefinition.GetDisplayName()} x{slot.Quantity}", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Position panel (simplified version)
    /// </summary>
    private void PositionPanel(Vector2 slotWorldPosition)
    {
        // Simple version: center the panel
        rectTransform.anchoredPosition = Vector2.zero;

        // Alternative: position near slot (uncomment if you want this)
        /*
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, slotWorldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                screenPoint,
                parentCanvas.worldCamera,
                out Vector2 localPoint);
            rectTransform.anchoredPosition = localPoint;
        }
        */
    }

    /// <summary>
    /// Setup panel visual content
    /// </summary>
    private void SetupPanelContent()
    {
        // Item icon
        if (itemIcon != null && itemDefinition.ItemIcon != null)
        {
            itemIcon.sprite = itemDefinition.ItemIcon;
            itemIcon.color = itemDefinition.ItemColor;
        }

        // Item name
        if (itemNameText != null)
        {
            itemNameText.text = itemDefinition.GetDisplayName();
            itemNameText.color = itemDefinition.GetRarityColor();
        }

        // Item description
        if (itemDescriptionText != null)
        {
            string description = itemDefinition.Description;
            if (string.IsNullOrEmpty(description))
            {
                description = "Aucune description disponible.";
            }
            itemDescriptionText.text = description;
        }

        // Quantity
        if (quantityText != null)
        {
            quantityText.text = $"Quantite: {itemSlot.Quantity}";
        }
    }

    /// <summary>
    /// Setup buttons based on context and item type
    /// </summary>
    private void SetupContextualButtons()
    {
        // Discard button - always available
        if (discardButton != null)
        {
            discardButton.gameObject.SetActive(true);
            if (discardButtonText != null)
            {
                discardButtonText.text = $"Jeter ({itemSlot.Quantity})";
            }
        }

        // Bank button - only if in bank and not already in bank container
        if (bankButton != null)
        {
            bool canUseBank = isInBank && inventoryManager.GetContainer("bank") != null;
            bankButton.gameObject.SetActive(canUseBank);

            if (canUseBank && bankButtonText != null)
            {
                bankButtonText.text = $"Banque ({itemSlot.Quantity})";
            }
        }

        // Use button - only for consumables and usable items
        if (useButton != null)
        {
            bool canUse = itemDefinition.Type == ItemType.Consumable ||
                         itemDefinition.Type == ItemType.Usable;
            useButton.gameObject.SetActive(canUse);
        }

        // Sell button - only if in shop
        if (sellButton != null)
        {
            sellButton.gameObject.SetActive(isInShop);

            if (isInShop && sellButtonText != null)
            {
                int sellPrice = Mathf.Max(1, itemDefinition.BasePrice / 2); // Sell for half price
                sellButtonText.text = $"Vendre ({sellPrice * itemSlot.Quantity} or)";
            }
        }
    }

    /// <summary>
    /// Show panel with simple scale animation
    /// </summary>
    private void ShowWithAnimation()
    {
        gameObject.SetActive(true);

        // Cancel any existing tween
        if (currentTween >= 0)
        {
            LeanTween.cancel(currentTween);
        }

        // Animation simple : Scale de 0 a 1 (pop effect)
        rectTransform.localScale = Vector3.zero;

        currentTween = LeanTween.scale(gameObject, Vector3.one, animationDuration)
            .setEase(slideInEase)
            .setOnComplete(() => currentTween = -1)
            .id;
    }

    /// <summary>
    /// Hide panel with simple scale animation
    /// </summary>
    public void HidePanel()
    {
        if (!gameObject.activeInHierarchy) return;

        // Cancel any existing tween
        if (currentTween >= 0)
        {
            LeanTween.cancel(currentTween);
        }

        // Animation simple : Scale de 1 a 0
        currentTween = LeanTween.scale(gameObject, Vector3.zero, animationDuration)
            .setEase(slideOutEase)
            .setOnComplete(() =>
            {
                currentTween = -1;
                gameObject.SetActive(false);
                ClearReferences();
            })
            .id;
    }

    /// <summary>
    /// Clear stored references
    /// </summary>
    private void ClearReferences()
    {
        sourceSlot = null;
        itemSlot = null;
        itemDefinition = null;
    }

    /// <summary>
    /// Set context flags for showing appropriate buttons
    /// </summary>
    public void SetContext(bool inBank, bool inShop)
    {
        isInBank = inBank;
        isInShop = inShop;
    }

    // Button click handlers
    private void OnDiscardClicked()
    {
        if (itemSlot == null || inventoryManager == null) return;

        // Ask for confirmation for valuable items
        if (itemDefinition.BasePrice > 100)
        {
            // TODO: Show confirmation dialog
            Logger.LogInfo($"ItemActionPanel: Would show confirmation for discarding valuable item {itemDefinition.GetDisplayName()}", Logger.LogCategory.InventoryLog);
        }

        // Remove all of this item from inventory
        bool success = inventoryManager.RemoveItem("player", itemSlot.ItemID, itemSlot.Quantity);

        if (success)
        {
            Logger.LogInfo($"ItemActionPanel: Discarded {itemSlot.Quantity}x {itemDefinition.GetDisplayName()}", Logger.LogCategory.InventoryLog);
        }

        HidePanel();
    }

    private void OnBankClicked()
    {
        if (itemSlot == null || inventoryManager == null) return;

        // Transfer to bank
        bool success = inventoryManager.TransferItem("player", "bank", itemSlot.ItemID, itemSlot.Quantity);

        if (success)
        {
            Logger.LogInfo($"ItemActionPanel: Moved {itemSlot.Quantity}x {itemDefinition.GetDisplayName()} to bank", Logger.LogCategory.InventoryLog);
        }

        HidePanel();
    }

    private void OnUseClicked()
    {
        if (itemSlot == null || inventoryManager == null) return;

        // TODO: Implement item usage logic based on item type
        // For now, just consume one item
        Logger.LogInfo($"ItemActionPanel: Used {itemDefinition.GetDisplayName()}", Logger.LogCategory.InventoryLog);

        // Remove one item
        bool success = inventoryManager.RemoveItem("player", itemSlot.ItemID, 1);

        if (success)
        {
            // TODO: Apply item effects (healing, buffs, etc.)
            Logger.LogInfo($"ItemActionPanel: Consumed 1x {itemDefinition.GetDisplayName()}", Logger.LogCategory.InventoryLog);
        }

        HidePanel();
    }

    private void OnSellClicked()
    {
        if (itemSlot == null || inventoryManager == null) return;

        // TODO: Implement selling logic
        int sellPrice = Mathf.Max(1, itemDefinition.BasePrice / 2);
        int totalPrice = sellPrice * itemSlot.Quantity;

        Logger.LogInfo($"ItemActionPanel: Would sell {itemSlot.Quantity}x {itemDefinition.GetDisplayName()} for {totalPrice} gold", Logger.LogCategory.InventoryLog);

        // Remove items and add gold
        bool success = inventoryManager.RemoveItem("player", itemSlot.ItemID, itemSlot.Quantity);
        if (success)
        {
            // TODO: Add gold to player currency
            Logger.LogInfo($"ItemActionPanel: Sold items for {totalPrice} gold", Logger.LogCategory.InventoryLog);
        }

        HidePanel();
    }

    private void OnCloseClicked()
    {
        HidePanel();
    }



    /// <summary>
    /// Get item definition via InventoryManager (fixed version)
    /// </summary>
    private ItemDefinition GetItemDefinition(string itemId)
    {
        // Use static Instance to avoid timing issues
        if (InventoryManager.Instance?.GetItemRegistry() != null)
        {
            return InventoryManager.Instance.GetItemRegistry().GetItem(itemId);
        }

        Logger.LogError("ItemActionPanel: InventoryManager.Instance or ItemRegistry is NULL!", Logger.LogCategory.InventoryLog);
        return null;
    }

    void OnDestroy()
    {
        // Cancel any running animations
        if (currentTween >= 0)
        {
            LeanTween.cancel(currentTween);
        }
    }
}