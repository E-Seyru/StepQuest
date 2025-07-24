// Purpose: Contextual panel that appears when clicking on any item, anywhere
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
    [SerializeField] private TextMeshProUGUI locationText; // Nouveau: affiche où est l'item

    [Header("Action Buttons")]
    [SerializeField] private Button discardButton;
    [SerializeField] private TextMeshProUGUI discardButtonText;
    [SerializeField] private Button transferButton; // Remplace bankButton
    [SerializeField] private TextMeshProUGUI transferButtonText;
    [SerializeField] private Button useButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private TextMeshProUGUI sellButtonText;
    [SerializeField] private Button buyButton; // Nouveau pour les shops
    [SerializeField] private TextMeshProUGUI buyButtonText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundButton;

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private LeanTweenType slideInEase = LeanTweenType.easeOutBack;
    [SerializeField] private LeanTweenType slideOutEase = LeanTweenType.easeInBack;

    // References
    private InventoryManager inventoryManager;
    private UniversalSlotUI sourceSlot;
    private InventorySlot itemSlot;
    private ItemDefinition itemDefinition;
    private string sourceContainerId;
    private UniversalSlotUI.SlotContext sourceContext;

    // Animation
    private RectTransform rectTransform;
    private int currentTween = -1;

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

        if (transferButton != null)
            transferButton.onClick.AddListener(OnTransferClicked);

        if (useButton != null)
            useButton.onClick.AddListener(OnUseClicked);

        if (sellButton != null)
            sellButton.onClick.AddListener(OnSellClicked);

        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        if (backgroundButton != null)
            backgroundButton.onClick.AddListener(OnCloseClicked);
    }

    /// <summary>
    /// Show panel for a specific item slot with context
    /// </summary>
    public void ShowPanel(UniversalSlotUI slotUI, InventorySlot slot, string containerId, UniversalSlotUI.SlotContext context, Vector2 worldPosition)
    {
        if (slot == null || slot.IsEmpty())
        {
            Logger.LogWarning("ItemActionPanel: Cannot show panel for empty slot", Logger.LogCategory.InventoryLog);
            return;
        }

        // Store references
        sourceSlot = slotUI;
        itemSlot = slot;
        sourceContainerId = containerId;
        sourceContext = context;
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

        Logger.LogInfo($"ItemActionPanel: Showing panel for {itemDefinition.GetDisplayName()} x{slot.Quantity} from {containerId} ({context})", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Position panel
    /// </summary>
    private void PositionPanel(Vector2 slotWorldPosition)
    {
        // Center the panel for now
        rectTransform.anchoredPosition = Vector2.zero;
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

        // Location
        if (locationText != null)
        {
            locationText.text = GetLocationDisplayName();
        }
    }

    /// <summary>
    /// Get display name for current location
    /// </summary>
    private string GetLocationDisplayName()
    {
        return sourceContext switch
        {
            UniversalSlotUI.SlotContext.PlayerInventory => "Inventaire",
            UniversalSlotUI.SlotContext.Bank => "Banque",
            UniversalSlotUI.SlotContext.Shop => "Magasin",
            UniversalSlotUI.SlotContext.Trade => "echange",
            UniversalSlotUI.SlotContext.Loot => "Butin",
            _ => sourceContainerId
        };
    }

    /// <summary>
    /// Setup buttons based on context and item type
    /// </summary>
    private void SetupContextualButtons()
    {
        // Reset all buttons
        discardButton?.gameObject.SetActive(false);
        transferButton?.gameObject.SetActive(false);
        useButton?.gameObject.SetActive(false);
        sellButton?.gameObject.SetActive(false);
        buyButton?.gameObject.SetActive(false);

        switch (sourceContext)
        {
            case UniversalSlotUI.SlotContext.PlayerInventory:
                SetupInventoryButtons();
                break;

            case UniversalSlotUI.SlotContext.Bank:
                SetupBankButtons();
                break;

            case UniversalSlotUI.SlotContext.Shop:
                SetupShopButtons();
                break;

            case UniversalSlotUI.SlotContext.Trade:
                SetupTradeButtons();
                break;

            case UniversalSlotUI.SlotContext.Loot:
                SetupLootButtons();
                break;
        }
    }

    private void SetupInventoryButtons()
    {
        // Discard - toujours disponible dans l'inventaire
        if (discardButton != null)
        {
            discardButton.gameObject.SetActive(true);
            if (discardButtonText != null)
            {
                discardButtonText.text = itemSlot.Quantity > 1 ? $"Jeter ({itemSlot.Quantity})" : "Jeter";
            }
        }

        // Transfer - si on a acces a un autre container
        if (transferButton != null && HasOtherContainerOpen())
        {
            transferButton.gameObject.SetActive(true);
            if (transferButtonText != null)
            {
                string targetName = GetTransferTargetName();
                transferButtonText.text = $"Deposer → {targetName}";
            }
        }

        // Use - pour les consommables
        if (useButton != null)
        {
            bool canUse = itemDefinition.Type == ItemType.Consumable ||
                         itemDefinition.Type == ItemType.Usable;
            useButton.gameObject.SetActive(canUse);
        }

        // Sell - si un shop est ouvert
        if (sellButton != null && IsShopOpen())
        {
            sellButton.gameObject.SetActive(true);
            if (sellButtonText != null)
            {
                int sellPrice = Mathf.Max(1, itemDefinition.BasePrice / 2);
                sellButtonText.text = $"Vendre ({sellPrice * itemSlot.Quantity} or)";
            }
        }
    }

    private void SetupBankButtons()
    {
        // Transfer vers l'inventaire
        if (transferButton != null)
        {
            transferButton.gameObject.SetActive(true);
            if (transferButtonText != null)
            {
                transferButtonText.text = $"Recuperer → Inventaire";
            }
        }

        // Pas de discard depuis la banque
        // Pas de use depuis la banque
        // Pas de sell depuis la banque
    }

    private void SetupShopButtons()
    {
        // Buy button pour acheter
        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(true);
            if (buyButtonText != null)
            {
                int totalPrice = itemDefinition.BasePrice * itemSlot.Quantity;
                buyButtonText.text = $"Acheter ({totalPrice} or)";
            }
        }

        // Pas de discard, transfer, use ou sell depuis le shop
    }

    private void SetupTradeButtons()
    {
        // TODO: Implementer les boutons pour le trade
    }

    private void SetupLootButtons()
    {
        // Prendre l'item
        if (transferButton != null)
        {
            transferButton.gameObject.SetActive(true);
            if (transferButtonText != null)
            {
                transferButtonText.text = "Prendre";
            }
        }
    }

    /// <summary>
    /// Check if another container is open
    /// </summary>
    private bool HasOtherContainerOpen()
    {
        // Check si la banque est ouverte
        if (BankPanelUI.Instance != null && BankPanelUI.Instance.gameObject.activeInHierarchy)
            return true;

        // TODO: Check autres containers (shop, trade, etc.)
        return false;
    }

    /// <summary>
    /// Get transfer target name
    /// </summary>
    private string GetTransferTargetName()
    {
        if (BankPanelUI.Instance != null && BankPanelUI.Instance.gameObject.activeInHierarchy)
            return "Banque";

        // TODO: Autres containers
        return "Container";
    }

    /// <summary>
    /// Check if shop is open
    /// </summary>
    private bool IsShopOpen()
    {
        // TODO: Implementer quand ShopPanelUI existe
        return false;
    }

    /// <summary>
    /// Show panel with animation
    /// </summary>
    private void ShowWithAnimation()
    {
        gameObject.SetActive(true);

        // Cancel any existing tween
        if (currentTween >= 0)
        {
            LeanTween.cancel(currentTween);
        }

        // Simple scale animation
        rectTransform.localScale = Vector3.zero;

        currentTween = LeanTween.scale(gameObject, Vector3.one, animationDuration)
            .setEase(slideInEase)
            .setOnComplete(() => currentTween = -1)
            .id;
    }

    /// <summary>
    /// Hide panel with animation
    /// </summary>
    public void HidePanel()
    {
        if (!gameObject.activeInHierarchy) return;

        // Cancel any existing tween
        if (currentTween >= 0)
        {
            LeanTween.cancel(currentTween);
        }

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
        sourceContainerId = null;
    }

    // === Button click handlers ===

    private void OnDiscardClicked()
    {
        if (itemSlot == null || inventoryManager == null || sourceSlot == null) return;

        // Obtenir le container et supprimer directement du slot
        var container = inventoryManager.GetContainer(sourceContainerId);
        if (container != null && sourceSlot.SlotIndex < container.Slots.Count)
        {
            var targetSlot = container.Slots[sourceSlot.SlotIndex];

            // Verifier que c'est bien le bon slot
            if (targetSlot.HasItem(itemSlot.ItemID))
            {
                // Supprimer la quantite complete du slot specifique
                int quantityToRemove = targetSlot.Quantity;
                targetSlot.Clear();

                // Declencher les evenements appropries
                inventoryManager.TriggerContainerChanged(sourceContainerId);

                // Sauvegarder les changements
                inventoryManager.ForceSave();

                Logger.LogInfo($"ItemActionPanel: Discarded {quantityToRemove}x {itemDefinition.GetDisplayName()} from slot {sourceSlot.SlotIndex} in {sourceContainerId}", Logger.LogCategory.InventoryLog);
            }
            else
            {
                Logger.LogError($"ItemActionPanel: Slot mismatch - expected {itemSlot.ItemID} but found {targetSlot.ItemID}", Logger.LogCategory.InventoryLog);
            }
        }
        else
        {
            Logger.LogError($"ItemActionPanel: Could not access container {sourceContainerId} or slot {sourceSlot.SlotIndex}", Logger.LogCategory.InventoryLog);
        }

        HidePanel();
    }

    private void OnTransferClicked()
    {
        if (itemSlot == null || inventoryManager == null) return;

        string targetContainerId = DetermineTransferTarget();
        if (string.IsNullOrEmpty(targetContainerId)) return;

        // Transfer item
        bool success = inventoryManager.TransferItem(sourceContainerId, targetContainerId, itemSlot.ItemID, itemSlot.Quantity);

        if (success)
        {
            Logger.LogInfo($"ItemActionPanel: Transferred {itemSlot.Quantity}x {itemDefinition.GetDisplayName()} from {sourceContainerId} to {targetContainerId}", Logger.LogCategory.InventoryLog);
        }

        HidePanel();
    }

    private string DetermineTransferTarget()
    {
        switch (sourceContext)
        {
            case UniversalSlotUI.SlotContext.PlayerInventory:
                // Si la banque est ouverte, transferer vers la banque
                if (BankPanelUI.Instance != null && BankPanelUI.Instance.gameObject.activeInHierarchy)
                    return "bank";
                break;

            case UniversalSlotUI.SlotContext.Bank:
                // Depuis la banque, toujours vers l'inventaire
                return "player";

            case UniversalSlotUI.SlotContext.Loot:
                // Depuis le loot, vers l'inventaire
                return "player";
        }

        return null;
    }

    private void OnUseClicked()
    {
        if (itemSlot == null || inventoryManager == null) return;

        // TODO: Implement item usage logic
        Logger.LogInfo($"ItemActionPanel: Used {itemDefinition.GetDisplayName()}", Logger.LogCategory.InventoryLog);

        // Remove one item
        bool success = inventoryManager.RemoveItem(sourceContainerId, itemSlot.ItemID, 1);

        if (success)
        {
            // TODO: Apply item effects
            Logger.LogInfo($"ItemActionPanel: Consumed 1x {itemDefinition.GetDisplayName()}", Logger.LogCategory.InventoryLog);
        }

        HidePanel();
    }

    private void OnSellClicked()
    {
        if (itemSlot == null || inventoryManager == null) return;

        // TODO: Implement selling logic with shop system
        int sellPrice = Mathf.Max(1, itemDefinition.BasePrice / 2);
        int totalPrice = sellPrice * itemSlot.Quantity;

        Logger.LogInfo($"ItemActionPanel: Would sell {itemSlot.Quantity}x {itemDefinition.GetDisplayName()} for {totalPrice} gold", Logger.LogCategory.InventoryLog);

        // Remove items and add gold
        bool success = inventoryManager.RemoveItem(sourceContainerId, itemSlot.ItemID, itemSlot.Quantity);
        if (success)
        {
            // TODO: Add gold to player currency
            Logger.LogInfo($"ItemActionPanel: Sold items for {totalPrice} gold", Logger.LogCategory.InventoryLog);
        }

        HidePanel();
    }

    private void OnBuyClicked()
    {
        if (itemSlot == null || inventoryManager == null) return;

        // TODO: Implement buying logic with shop system
        int totalPrice = itemDefinition.BasePrice * itemSlot.Quantity;

        Logger.LogInfo($"ItemActionPanel: Would buy {itemSlot.Quantity}x {itemDefinition.GetDisplayName()} for {totalPrice} gold", Logger.LogCategory.InventoryLog);

        // TODO: Check player gold
        // TODO: Remove gold and add items to inventory

        HidePanel();
    }

    private void OnCloseClicked()
    {
        HidePanel();
    }

    /// <summary>
    /// Get item definition via InventoryManager
    /// </summary>
    private ItemDefinition GetItemDefinition(string itemId)
    {
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