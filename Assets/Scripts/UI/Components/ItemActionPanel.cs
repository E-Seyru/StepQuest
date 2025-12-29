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
    [SerializeField] private Button transferButton;
    [SerializeField] private TextMeshProUGUI transferButtonText;
    [SerializeField] private Button useButton;
    [SerializeField] private TextMeshProUGUI useButtonText;
    [SerializeField] private Button sellButton;
    [SerializeField] private TextMeshProUGUI sellButtonText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI buyButtonText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundButton;

    [Header("Button Colors")]
    [SerializeField] private Color enabledButtonColor = Color.white;
    [SerializeField] private Color disabledButtonColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

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
    private Canvas parentCanvas;
    private int currentTween = -1;
    private Vector2 animationOrigin; // Where the panel animates from (item position)
    private Vector2 finalPosition;   // Where the panel ends up (near the item)

    public static ItemActionPanel Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            rectTransform = GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
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
    /// Convert world position to canvas local position and calculate final position near the item
    /// </summary>
    private void PositionPanel(Vector2 slotWorldPosition)
    {
        if (parentCanvas == null)
        {
            animationOrigin = Vector2.zero;
            finalPosition = Vector2.zero;
            return;
        }

        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;

        // Convert world position to screen position first
        Vector2 screenPosition;
        if (cam != null)
        {
            screenPosition = cam.WorldToScreenPoint(slotWorldPosition);
        }
        else
        {
            // For overlay canvas, world position IS screen position for UI elements
            screenPosition = slotWorldPosition;
        }

        // Convert screen position to canvas local position
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            cam,
            out animationOrigin
        );

        // Calculate panel size - use rect.size instead of sizeDelta to handle stretched anchors
        Vector2 panelSize = rectTransform.rect.size;
        Vector2 canvasSize = canvasRect.rect.size;

        // DEBUG: Log all coordinates
        Logger.LogWarning($"[ItemActionPanel] World: {slotWorldPosition}, Screen: {screenPosition}, Canvas Local: {animationOrigin}", Logger.LogCategory.InventoryLog);
        Logger.LogWarning($"[ItemActionPanel] Canvas size: {canvasSize}, Panel size: {panelSize}", Logger.LogCategory.InventoryLog);

        // Offset from the item (panel appears at top corner of the item)
        float offsetX = panelSize.x * 0.5f + 10f; // Half panel width + small gap
        float offsetY = panelSize.y * 0.5f + 10f; // Position panel's bottom edge near item's top

        // Canvas local coordinates: (0,0) is center, positive X is right, positive Y is up
        // Right edge of canvas is at canvasSize.x * 0.5f
        // Check if placing panel to the right would keep it within bounds
        float rightEdge = canvasSize.x * 0.5f;
        float spaceOnRight = rightEdge - animationOrigin.x;
        float spaceNeededOnRight = offsetX + panelSize.x * 0.5f + 10f; // offset + half panel + margin

        bool placeOnRight = spaceOnRight >= spaceNeededOnRight;

        Logger.LogWarning($"[ItemActionPanel] RightEdge: {rightEdge}, SpaceOnRight: {spaceOnRight}, SpaceNeeded: {spaceNeededOnRight}, PlaceOnRight: {placeOnRight}", Logger.LogCategory.InventoryLog);

        if (placeOnRight)
        {
            finalPosition = new Vector2(animationOrigin.x + offsetX, animationOrigin.y + offsetY);
        }
        else
        {
            finalPosition = new Vector2(animationOrigin.x - offsetX, animationOrigin.y + offsetY);
        }

        Logger.LogWarning($"[ItemActionPanel] Final position: {finalPosition}", Logger.LogCategory.InventoryLog);

        // Clamp to stay within screen bounds
        float halfWidth = panelSize.x * 0.5f;
        float halfHeight = panelSize.y * 0.5f;
        float maxX = canvasSize.x * 0.5f - halfWidth - 10f;
        float maxY = canvasSize.y * 0.5f - halfHeight - 10f;

        finalPosition.x = Mathf.Clamp(finalPosition.x, -maxX, maxX);
        finalPosition.y = Mathf.Clamp(finalPosition.y, -maxY, maxY);
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

    /// <summary>
    /// Set a button's enabled/disabled state with visual feedback
    /// </summary>
    private void SetButtonState(Button button, TextMeshProUGUI buttonText, bool enabled, string text = null)
    {
        if (button == null) return;

        button.interactable = enabled;

        // Update button image color
        var buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = enabled ? enabledButtonColor : disabledButtonColor;
        }

        // Update text if provided
        if (buttonText != null && text != null)
        {
            buttonText.text = text;
        }

        // Grey out text when disabled
        if (buttonText != null)
        {
            buttonText.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        }
    }

    private void SetupInventoryButtons()
    {
        // Discard - toujours disponible dans l'inventaire
        string discardText = itemSlot.Quantity > 1 ? $"Jeter ({itemSlot.Quantity})" : "Jeter";
        SetButtonState(discardButton, discardButtonText, true, discardText);

        // Transfer - disponible si un autre container est ouvert
        bool canTransfer = HasOtherContainerOpen();
        string transferText = canTransfer ? $"Deposer → {GetTransferTargetName()}" : "Deposer";
        SetButtonState(transferButton, transferButtonText, canTransfer, transferText);

        // Use - pour les consommables
        bool canUse = itemDefinition.Type == ItemType.Consumable || itemDefinition.Type == ItemType.Usable;
        SetButtonState(useButton, useButtonText, canUse, "Utiliser");

        // Sell - si un shop est ouvert
        bool canSell = IsShopOpen();
        int sellPrice = Mathf.Max(1, itemDefinition.BasePrice / 2);
        string sellText = canSell ? $"Vendre ({sellPrice * itemSlot.Quantity} or)" : "Vendre";
        SetButtonState(sellButton, sellButtonText, canSell, sellText);

        // Buy - pas disponible depuis l'inventaire
        SetButtonState(buyButton, buyButtonText, false, "Acheter");
    }

    private void SetupBankButtons()
    {
        // Discard - pas disponible depuis la banque
        SetButtonState(discardButton, discardButtonText, false, "Jeter");

        // Transfer vers l'inventaire - toujours disponible
        SetButtonState(transferButton, transferButtonText, true, "Recuperer → Inventaire");

        // Use - pas disponible depuis la banque
        SetButtonState(useButton, useButtonText, false, "Utiliser");

        // Sell - pas disponible depuis la banque
        SetButtonState(sellButton, sellButtonText, false, "Vendre");

        // Buy - pas disponible depuis la banque
        SetButtonState(buyButton, buyButtonText, false, "Acheter");
    }

    private void SetupShopButtons()
    {
        // Discard - pas disponible depuis le shop
        SetButtonState(discardButton, discardButtonText, false, "Jeter");

        // Transfer - pas disponible depuis le shop
        SetButtonState(transferButton, transferButtonText, false, "Deposer");

        // Use - pas disponible depuis le shop
        SetButtonState(useButton, useButtonText, false, "Utiliser");

        // Sell - pas disponible depuis le shop
        SetButtonState(sellButton, sellButtonText, false, "Vendre");

        // Buy - disponible
        int totalPrice = itemDefinition.BasePrice * itemSlot.Quantity;
        SetButtonState(buyButton, buyButtonText, true, $"Acheter ({totalPrice} or)");
    }

    private void SetupTradeButtons()
    {
        // TODO: Implementer les boutons pour le trade
        SetButtonState(discardButton, discardButtonText, false, "Jeter");
        SetButtonState(transferButton, transferButtonText, false, "Deposer");
        SetButtonState(useButton, useButtonText, false, "Utiliser");
        SetButtonState(sellButton, sellButtonText, false, "Vendre");
        SetButtonState(buyButton, buyButtonText, false, "Acheter");
    }

    private void SetupLootButtons()
    {
        // Discard - pas disponible depuis le loot
        SetButtonState(discardButton, discardButtonText, false, "Jeter");

        // Transfer - prendre l'item
        SetButtonState(transferButton, transferButtonText, true, "Prendre");

        // Use - pas disponible depuis le loot
        SetButtonState(useButton, useButtonText, false, "Utiliser");

        // Sell - pas disponible depuis le loot
        SetButtonState(sellButton, sellButtonText, false, "Vendre");

        // Buy - pas disponible depuis le loot
        SetButtonState(buyButton, buyButtonText, false, "Acheter");
    }

    /// <summary>
    /// Check if another container is open
    /// </summary>
    private bool HasOtherContainerOpen()
    {
        // Check si la banque est ouverte
        if (BankPanel.Instance != null && BankPanel.Instance.gameObject.activeInHierarchy)
            return true;

        // TODO: Check autres containers (shop, trade, etc.)
        return false;
    }

    /// <summary>
    /// Get transfer target name
    /// </summary>
    private string GetTransferTargetName()
    {
        if (BankPanel.Instance != null && BankPanel.Instance.gameObject.activeInHierarchy)
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
    /// Show panel with animation - scales up while moving from item to final position
    /// </summary>
    private void ShowWithAnimation()
    {
        gameObject.SetActive(true);

        // Cancel any existing tween
        if (currentTween >= 0)
        {
            LeanTween.cancel(currentTween);
        }

        // Start at the item position with scale 0
        rectTransform.anchoredPosition = animationOrigin;
        rectTransform.localScale = Vector3.zero;

        // Animate scale from 0 to 1
        LeanTween.scale(gameObject, Vector3.one, animationDuration)
            .setEase(slideInEase);

        // Animate position from item to final position
        currentTween = LeanTween.value(gameObject, animationOrigin, finalPosition, animationDuration)
            .setEase(slideInEase)
            .setOnUpdate((Vector2 pos) => rectTransform.anchoredPosition = pos)
            .setOnComplete(() => currentTween = -1)
            .id;
    }

    /// <summary>
    /// Hide panel with animation - scales down while moving back to item position
    /// </summary>
    public void HidePanel()
    {
        if (!gameObject.activeInHierarchy) return;

        // Cancel any existing tween
        if (currentTween >= 0)
        {
            LeanTween.cancel(currentTween);
        }

        // Animate scale from 1 to 0
        LeanTween.scale(gameObject, Vector3.zero, animationDuration)
            .setEase(slideOutEase);

        // Animate position from final position back to item
        currentTween = LeanTween.value(gameObject, rectTransform.anchoredPosition, animationOrigin, animationDuration)
            .setEase(slideOutEase)
            .setOnUpdate((Vector2 pos) => rectTransform.anchoredPosition = pos)
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
                if (BankPanel.Instance != null && BankPanel.Instance.gameObject.activeInHierarchy)
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