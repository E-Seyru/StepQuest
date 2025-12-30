// Purpose: Panel for bank storage activity with player inventory and bank sections
// Filepath: Assets/Scripts/UI/Panels/BankPanel.cs
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel for bank storage access.
/// Shows player inventory (top) and bank storage (bottom) with filtering and quick actions.
/// </summary>
public class BankPanel : MonoBehaviour
{
    [Header("UI References - Header")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button closeButton;

    [Header("UI References - Player Inventory Section")]
    [SerializeField] private Transform playerSlotsContainer;
    [SerializeField] private TextMeshProUGUI playerCapacityText;

    [Header("UI References - Bank Section")]
    [SerializeField] private Transform bankSlotsContainer;
    [SerializeField] private TextMeshProUGUI bankCapacityText;

    [Header("UI References - Quick Actions")]
    [SerializeField] private Button depositAllButton;
    [SerializeField] private Button withdrawAllButton;

    [Header("UI References - Filter Sidebar")]
    [SerializeField] private Button filterAllButton;
    [SerializeField] private Button filterNoneButton;

    [Header("UI References - Filter Type Buttons")]
    [SerializeField] private Button filterEquipmentButton;
    [SerializeField] private Button filterMaterialButton;
    [SerializeField] private Button filterConsumableButton;
    [SerializeField] private Button filterToolButton;
    [SerializeField] private Button filterQuestButton;
    [SerializeField] private Button filterMiscButton;

    [Header("UI References - Footer")]
    [SerializeField] private Button leaveButton;

    [Header("Prefab")]
    [SerializeField] private GameObject slotPrefab;

    [Header("Filter Settings")]
    [SerializeField] private Color activeFilterColor = Color.white;
    [SerializeField] private Color inactiveFilterColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    // State
    private LocationActivity currentActivity;
    private InventoryManager inventoryManager;
    private List<UniversalSlotUI> playerSlotUIs = new List<UniversalSlotUI>();
    private List<UniversalSlotUI> bankSlotUIs = new List<UniversalSlotUI>();
    private HashSet<ItemType> activeFilters = new HashSet<ItemType>();
    private bool showAllItems = true;

    // Singleton
    public static BankPanel Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        inventoryManager = InventoryManager.Instance;

        // Setup buttons
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (leaveButton != null)
            leaveButton.onClick.AddListener(ClosePanel);

        if (depositAllButton != null)
            depositAllButton.onClick.AddListener(DepositAll);

        if (withdrawAllButton != null)
            withdrawAllButton.onClick.AddListener(WithdrawAll);

        if (filterAllButton != null)
            filterAllButton.onClick.AddListener(ShowAllItems);

        if (filterNoneButton != null)
            filterNoneButton.onClick.AddListener(ShowNoItems);

        // Setup individual filter buttons
        SetupFilterButton(filterEquipmentButton, ItemType.Equipment);
        SetupFilterButton(filterMaterialButton, ItemType.Material);
        SetupFilterButton(filterConsumableButton, ItemType.Consumable);
        SetupFilterButton(filterToolButton, ItemType.Usable);
        SetupFilterButton(filterQuestButton, ItemType.Quest);
        SetupFilterButton(filterMiscButton, ItemType.Miscellaneous);

        // Start hidden
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnContainerChanged += OnContainerChanged;
        }
    }

    void OnDisable()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnContainerChanged -= OnContainerChanged;
        }
    }

    #region Public Methods

    /// <summary>
    /// Open panel with bank activity
    /// </summary>
    public void OpenWithActivity(LocationActivity activity)
    {
        if (activity == null)
        {
            Logger.LogWarning("BankPanel: Cannot open with null activity!", Logger.LogCategory.InventoryLog);
            return;
        }

        currentActivity = activity;

        // Ensure we have InventoryManager reference
        if (inventoryManager == null)
        {
            inventoryManager = InventoryManager.Instance;
        }

        // Reset filter state
        showAllItems = true;
        activeFilters.Clear();

        UpdateTitle();
        CreateSlotUIs();
        RefreshDisplay();
        UpdateFilterVisuals();

        gameObject.SetActive(true);

        Logger.LogInfo($"BankPanel: Opened for {activity.GetDisplayName()}", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Close the panel
    /// </summary>
    public void ClosePanel()
    {
        gameObject.SetActive(false);
        ClearSlotUIs();

        // Slide activities section back in
        if (ActivitiesSectionPanel.Instance != null)
        {
            ActivitiesSectionPanel.Instance.SlideIn();
        }

        Logger.LogInfo("BankPanel: Panel closed", Logger.LogCategory.InventoryLog);
    }

    #endregion

    #region Private Methods - Setup

    /// <summary>
    /// Update the title text
    /// </summary>
    private void UpdateTitle()
    {
        if (titleText != null && currentActivity != null)
        {
            titleText.text = currentActivity.GetDisplayName();
        }
    }

    /// <summary>
    /// Create slot UIs for both player inventory and bank
    /// </summary>
    private void CreateSlotUIs()
    {
        ClearSlotUIs();

        if (inventoryManager == null)
        {
            Logger.LogError("BankPanel: InventoryManager is null!", Logger.LogCategory.InventoryLog);
            return;
        }

        if (slotPrefab == null)
        {
            Logger.LogError("BankPanel: slotPrefab is null! Assign it in inspector.", Logger.LogCategory.InventoryLog);
            return;
        }

        if (playerSlotsContainer == null)
        {
            Logger.LogError("BankPanel: playerSlotsContainer is null! Assign it in inspector.", Logger.LogCategory.InventoryLog);
        }

        if (bankSlotsContainer == null)
        {
            Logger.LogError("BankPanel: bankSlotsContainer is null! Assign it in inspector.", Logger.LogCategory.InventoryLog);
        }

        // Create player inventory slots
        var playerContainer = inventoryManager.GetContainer(GameConstants.ContainerIdPlayer);
        if (playerContainer != null && playerSlotsContainer != null)
        {
            for (int i = 0; i < playerContainer.MaxSlots; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, playerSlotsContainer);
                UniversalSlotUI slotUI = slotObj.GetComponent<UniversalSlotUI>();

                if (slotUI != null)
                {
                    slotUI.Setup(playerContainer.Slots[i], i, GameConstants.ContainerIdPlayer, UniversalSlotUI.SlotContext.PlayerInventory);
                    slotUI.OnSlotClicked += OnSlotClicked;
                    playerSlotUIs.Add(slotUI);
                }
            }
        }

        // Create bank slots
        var bankContainer = inventoryManager.GetContainer(GameConstants.ContainerIdBank);
        if (bankContainer != null && bankSlotsContainer != null)
        {
            for (int i = 0; i < bankContainer.MaxSlots; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab, bankSlotsContainer);
                UniversalSlotUI slotUI = slotObj.GetComponent<UniversalSlotUI>();

                if (slotUI != null)
                {
                    slotUI.Setup(bankContainer.Slots[i], i, GameConstants.ContainerIdBank, UniversalSlotUI.SlotContext.Bank);
                    slotUI.OnSlotClicked += OnSlotClicked;
                    bankSlotUIs.Add(slotUI);
                }
            }
        }

        Logger.LogInfo($"BankPanel: Created {playerSlotUIs.Count} player slots and {bankSlotUIs.Count} bank slots", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Clear all slot UIs
    /// </summary>
    private void ClearSlotUIs()
    {
        foreach (var slotUI in playerSlotUIs)
        {
            if (slotUI != null)
            {
                slotUI.OnSlotClicked -= OnSlotClicked;
                Destroy(slotUI.gameObject);
            }
        }
        playerSlotUIs.Clear();

        foreach (var slotUI in bankSlotUIs)
        {
            if (slotUI != null)
            {
                slotUI.OnSlotClicked -= OnSlotClicked;
                Destroy(slotUI.gameObject);
            }
        }
        bankSlotUIs.Clear();
    }

    /// <summary>
    /// Setup a filter button for a specific item type
    /// </summary>
    private void SetupFilterButton(Button button, ItemType itemType)
    {
        if (button == null) return;

        button.onClick.AddListener(() => ToggleFilter(itemType));
    }

    #endregion

    #region Private Methods - Display

    /// <summary>
    /// Refresh all displays
    /// </summary>
    private void RefreshDisplay()
    {
        RefreshPlayerSlots();
        RefreshBankSlots();
        RefreshCapacityTexts();
    }

    /// <summary>
    /// Refresh player inventory slots (always show all)
    /// </summary>
    private void RefreshPlayerSlots()
    {
        var playerContainer = inventoryManager?.GetContainer(GameConstants.ContainerIdPlayer);
        if (playerContainer == null) return;

        for (int i = 0; i < playerSlotUIs.Count && i < playerContainer.Slots.Count; i++)
        {
            playerSlotUIs[i].Setup(playerContainer.Slots[i], i, GameConstants.ContainerIdPlayer, UniversalSlotUI.SlotContext.PlayerInventory);
        }
    }

    /// <summary>
    /// Refresh bank slots with filtering applied
    /// </summary>
    private void RefreshBankSlots()
    {
        var bankContainer = inventoryManager?.GetContainer(GameConstants.ContainerIdBank);
        if (bankContainer == null) return;

        for (int i = 0; i < bankSlotUIs.Count && i < bankContainer.Slots.Count; i++)
        {
            var slot = bankContainer.Slots[i];
            var slotUI = bankSlotUIs[i];

            // Apply filtering
            bool shouldShow = ShouldShowSlot(slot);
            slotUI.gameObject.SetActive(shouldShow);

            if (shouldShow)
            {
                slotUI.Setup(slot, i, GameConstants.ContainerIdBank, UniversalSlotUI.SlotContext.Bank);
            }
        }
    }

    /// <summary>
    /// Check if a slot should be shown based on current filters
    /// </summary>
    private bool ShouldShowSlot(InventorySlot slot)
    {
        // Always show empty slots
        if (slot == null || slot.IsEmpty()) return true;

        // Show all if showAllItems is true
        if (showAllItems) return true;

        // Show none if no filters active
        if (activeFilters.Count == 0) return false;

        // Get item definition to check type
        var itemDef = inventoryManager?.GetItemRegistry()?.GetItem(slot.ItemID);
        if (itemDef == null) return true; // Show if we can't determine type

        return activeFilters.Contains(itemDef.Type);
    }

    /// <summary>
    /// Refresh capacity text displays
    /// </summary>
    private void RefreshCapacityTexts()
    {
        var playerContainer = inventoryManager?.GetContainer(GameConstants.ContainerIdPlayer);
        if (playerContainer != null && playerCapacityText != null)
        {
            int used = playerContainer.GetUsedSlotsCount();
            int max = playerContainer.MaxSlots;
            playerCapacityText.text = $"{used}/{max}";
        }

        var bankContainer = inventoryManager?.GetContainer(GameConstants.ContainerIdBank);
        if (bankContainer != null && bankCapacityText != null)
        {
            int used = bankContainer.GetUsedSlotsCount();
            int max = bankContainer.MaxSlots;
            bankCapacityText.text = $"{used}/{max}";
        }
    }

    #endregion

    #region Private Methods - Filters

    /// <summary>
    /// Toggle a filter on/off
    /// </summary>
    private void ToggleFilter(ItemType itemType)
    {
        showAllItems = false;

        if (activeFilters.Contains(itemType))
        {
            activeFilters.Remove(itemType);
        }
        else
        {
            activeFilters.Add(itemType);
        }

        UpdateFilterVisuals();
        RefreshBankSlots();

        Logger.LogInfo($"BankPanel: Toggled filter {itemType}, active filters: {activeFilters.Count}", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Show all items (clear filters)
    /// </summary>
    private void ShowAllItems()
    {
        showAllItems = true;
        activeFilters.Clear();
        UpdateFilterVisuals();
        RefreshBankSlots();

        Logger.LogInfo("BankPanel: Showing all items", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Show no items (hide all)
    /// </summary>
    private void ShowNoItems()
    {
        showAllItems = false;
        activeFilters.Clear();
        UpdateFilterVisuals();
        RefreshBankSlots();

        Logger.LogInfo("BankPanel: Showing no items", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Update filter button visuals
    /// </summary>
    private void UpdateFilterVisuals()
    {
        // Update All button
        UpdateButtonVisual(filterAllButton, showAllItems);

        // Update None button
        UpdateButtonVisual(filterNoneButton, !showAllItems && activeFilters.Count == 0);

        // Update individual filter buttons
        UpdateButtonVisual(filterEquipmentButton, activeFilters.Contains(ItemType.Equipment));
        UpdateButtonVisual(filterMaterialButton, activeFilters.Contains(ItemType.Material));
        UpdateButtonVisual(filterConsumableButton, activeFilters.Contains(ItemType.Consumable));
        UpdateButtonVisual(filterToolButton, activeFilters.Contains(ItemType.Usable));
        UpdateButtonVisual(filterQuestButton, activeFilters.Contains(ItemType.Quest));
        UpdateButtonVisual(filterMiscButton, activeFilters.Contains(ItemType.Miscellaneous));
    }

    /// <summary>
    /// Update a single button's visual state
    /// </summary>
    private void UpdateButtonVisual(Button button, bool isActive)
    {
        if (button == null) return;

        var img = button.GetComponent<Image>();
        if (img != null)
        {
            img.color = isActive ? activeFilterColor : inactiveFilterColor;
        }
    }

    #endregion

    #region Private Methods - Quick Actions

    /// <summary>
    /// Deposit all items from player inventory to bank
    /// </summary>
    private void DepositAll()
    {
        if (inventoryManager == null) return;

        var playerContainer = inventoryManager.GetContainer(GameConstants.ContainerIdPlayer);
        if (playerContainer == null) return;

        int depositedCount = 0;

        // Iterate through player slots and transfer non-empty ones
        foreach (var slot in playerContainer.Slots.ToList())
        {
            if (!slot.IsEmpty())
            {
                bool success = inventoryManager.TransferItem(
                    GameConstants.ContainerIdPlayer,
                    GameConstants.ContainerIdBank,
                    slot.ItemID,
                    slot.Quantity
                );

                if (success)
                {
                    depositedCount++;
                }
            }
        }

        Logger.LogInfo($"BankPanel: Deposited {depositedCount} item stacks to bank", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Withdraw all items from bank to player inventory
    /// </summary>
    private void WithdrawAll()
    {
        if (inventoryManager == null) return;

        var bankContainer = inventoryManager.GetContainer(GameConstants.ContainerIdBank);
        if (bankContainer == null) return;

        int withdrawnCount = 0;

        // Iterate through bank slots and transfer non-empty ones
        foreach (var slot in bankContainer.Slots.ToList())
        {
            if (!slot.IsEmpty())
            {
                bool success = inventoryManager.TransferItem(
                    GameConstants.ContainerIdBank,
                    GameConstants.ContainerIdPlayer,
                    slot.ItemID,
                    slot.Quantity
                );

                if (success)
                {
                    withdrawnCount++;
                }
            }
        }

        Logger.LogInfo($"BankPanel: Withdrew {withdrawnCount} item stacks from bank", Logger.LogCategory.InventoryLog);
    }

    #endregion

    #region Private Methods - Events

    /// <summary>
    /// Handle slot click
    /// </summary>
    private void OnSlotClicked(UniversalSlotUI slotUI, int index)
    {
        // The UniversalSlotUI handles drag-and-drop internally via DragDropManager
        // We can add additional click behavior here if needed
        Logger.LogInfo($"BankPanel: Slot clicked - Container: {slotUI.ContainerId}, Index: {index}", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Handle container changes
    /// </summary>
    private void OnContainerChanged(string containerId)
    {
        if (containerId == GameConstants.ContainerIdPlayer || containerId == GameConstants.ContainerIdBank)
        {
            RefreshDisplay();
        }
    }

    #endregion

    void OnDestroy()
    {
        ClearSlotUIs();

        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePanel);

        if (leaveButton != null)
            leaveButton.onClick.RemoveListener(ClosePanel);

        if (depositAllButton != null)
            depositAllButton.onClick.RemoveListener(DepositAll);

        if (withdrawAllButton != null)
            withdrawAllButton.onClick.RemoveListener(WithdrawAll);

        if (filterAllButton != null)
            filterAllButton.onClick.RemoveListener(ShowAllItems);

        if (filterNoneButton != null)
            filterNoneButton.onClick.RemoveListener(ShowNoItems);
    }
}
