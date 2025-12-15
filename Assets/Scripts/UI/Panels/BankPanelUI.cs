// Purpose: UI controller for the bank panel using UniversalSlotUI
// Filepath: Assets/Scripts/UI/Panels/BankPanelUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BankPanelUI : ContainerPanelUI
{
    [Header("Bank-Specific")]
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI bankInfoText;

    [Header("Optional Features")]
    [SerializeField] private Button sortButton;
    [SerializeField] private Button expandButton;
    [SerializeField] private TextMeshProUGUI expandCostText;

    public static BankPanelUI Instance { get; private set; }

    // Base class abstract implementations
    protected override string ContainerId => GameConstants.ContainerIdBank;
    protected override UniversalSlotUI.SlotContext SlotContext => UniversalSlotUI.SlotContext.Bank;

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

    protected override void OnInitialize()
    {
        // Setup buttons
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (sortButton != null)
            sortButton.onClick.AddListener(SortBank);

        if (expandButton != null)
            expandButton.onClick.AddListener(ExpandBank);

        // Start closed
        gameObject.SetActive(false);
    }

    protected override void OnSlotCreated(UniversalSlotUI slotUI, int index)
    {
        slotUI.OnSlotRightClicked += OnSlotRightClicked;
    }

    protected override void OnSlotClearing(UniversalSlotUI slotUI)
    {
        slotUI.OnSlotRightClicked -= OnSlotRightClicked;
    }

    protected override void OnRefreshInfo(InventoryContainer container)
    {
        // Update title
        if (titleText != null)
        {
            titleText.text = "Coffre de Banque";
        }

        // Update capacity with label
        if (capacityText != null)
        {
            int used = container.GetUsedSlotsCount();
            int max = container.MaxSlots;
            capacityText.text = $"Capacite: {used}/{max}";
        }

        // Update bank info
        if (bankInfoText != null)
        {
            if (selectedSlot != null && !selectedSlot.IsEmpty())
            {
                var slot = selectedSlot.GetSlotData();
                var itemDef = inventoryManager.GetItemRegistry().GetItem(slot.ItemID);
                string itemName = itemDef?.GetDisplayName() ?? slot.ItemID;
                bankInfoText.text = $"Selectionne: {itemName} x{slot.Quantity}";
            }
            else
            {
                int totalItems = 0;
                foreach (var slot in container.Slots)
                {
                    if (!slot.IsEmpty())
                        totalItems += slot.Quantity;
                }
                bankInfoText.text = $"Total objets: {totalItems}";
            }
        }

        // Update expand button
        if (expandButton != null && expandCostText != null)
        {
            int currentSlots = container.MaxSlots;
            int expandCost = CalculateExpansionCost(currentSlots);
            expandCostText.text = $"Agrandir ({expandCost} or)";

            // TODO: Disable si pas assez d'or
            expandButton.interactable = true;
        }
    }

    protected override void OnPanelOpened()
    {
        Logger.LogInfo("BankPanelUI: Bank opened", Logger.LogCategory.InventoryLog);
    }

    protected override void OnPanelClosed()
    {
        Logger.LogInfo("BankPanelUI: Bank closed", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Handle slot right-clicked (quick transfer) - NOT USED ON MOBILE
    /// </summary>
    private void OnSlotRightClicked(UniversalSlotUI slotUI, int index)
    {
        // Cette methode n'est pas utilisee sur mobile
        // On pourrait implementer un long-press pour quick transfer
    }

    /// <summary>
    /// Sort bank items
    /// </summary>
    private void SortBank()
    {
        // TODO: Implement sorting logic
        Logger.LogInfo("BankPanelUI: Sorting bank items...", Logger.LogCategory.InventoryLog);

        // Ideas for sorting:
        // - By type (equipment, consumables, materials)
        // - By rarity
        // - By value
        // - Alphabetically
    }

    /// <summary>
    /// Expand bank capacity
    /// </summary>
    private void ExpandBank()
    {
        var container = inventoryManager?.GetContainer(ContainerId);
        if (container == null) return;

        int currentSlots = container.MaxSlots;
        int expandCost = CalculateExpansionCost(currentSlots);
        int newSlots = currentSlots + 10; // Add 10 slots per expansion

        // TODO: Check player gold and deduct cost

        // Expand the container
        container.Resize(newSlots);
        inventoryManager.TriggerContainerChanged(ContainerId);

        // Recreate UI to show new slots
        CreateSlotUIs();
        RefreshDisplay();

        Logger.LogInfo($"BankPanelUI: Expanded bank from {currentSlots} to {newSlots} slots", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Calculate expansion cost based on current size
    /// </summary>
    private int CalculateExpansionCost(int currentSlots)
    {
        // Progressive cost: 100 gold per 10 slots, increasing by 50 each time
        int expansions = (currentSlots - inventoryManager.DefaultBankSlots) / 10;
        return 100 + (expansions * 50);
    }
}
