// Purpose: UI controller for the bank panel using UniversalSlotUI
// Filepath: Assets/Scripts/UI/Panels/BankPanelUI.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BankPanelUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject slotPrefab; // Should have UniversalSlotUI component
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button closeButton;

    [Header("Info Display")]
    [SerializeField] private TextMeshProUGUI capacityText;
    [SerializeField] private TextMeshProUGUI bankInfoText;

    [Header("Optional Features")]
    [SerializeField] private Button sortButton;
    [SerializeField] private Button expandButton;
    [SerializeField] private TextMeshProUGUI expandCostText;

    // Internal state
    private InventoryManager inventoryManager;
    private List<UniversalSlotUI> slotUIs = new List<UniversalSlotUI>();
    private UniversalSlotUI selectedSlot;
    private string currentContainerId = "bank";

    // Auto-deselection
    private bool isPointerOverBank = false;

    public static BankPanelUI Instance { get; private set; }

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
        // Get InventoryManager reference
        inventoryManager = InventoryManager.Instance;

        if (inventoryManager == null)
        {
            Logger.LogError("BankPanelUI: InventoryManager not found!", Logger.LogCategory.InventoryLog);
            return;
        }

        // Subscribe to inventory events
        inventoryManager.OnContainerChanged += OnContainerChanged;

        // Setup buttons
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (sortButton != null)
            sortButton.onClick.AddListener(SortBank);

        if (expandButton != null)
            expandButton.onClick.AddListener(ExpandBank);

        // Initial setup
        CreateSlotUIs();
        RefreshDisplay();

        // Start closed
        gameObject.SetActive(false);
    }

    void Update()
    {
        // Auto-deselection comme dans InventoryPanelUI
        CheckForDeselection();
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (inventoryManager != null)
        {
            inventoryManager.OnContainerChanged -= OnContainerChanged;
        }
    }

    /// <summary>
    /// Check for auto-deselection (mobile-friendly)
    /// </summary>
    private void CheckForDeselection()
    {
        if (selectedSlot == null) return;

        bool inputDetected = false;

        // Pour mobile (touch)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                inputDetected = true;
            }
        }
        // Pour desktop (mouse)
        else if (Input.GetMouseButtonDown(0))
        {
            inputDetected = true;
        }

        if (inputDetected)
        {
            if (!IsPointerOverBankArea())
            {
                DeselectCurrentSlot();
            }
        }
    }

    /// <summary>
    /// Check if pointer/touch is over bank area
    /// </summary>
    private bool IsPointerOverBankArea()
    {
        Vector2 screenPosition;

        if (Input.touchCount > 0)
        {
            screenPosition = Input.GetTouch(0).position;
        }
        else
        {
            screenPosition = Input.mousePosition;
        }

        // Check ItemActionPanel
        if (ItemActionPanel.Instance != null && ItemActionPanel.Instance.gameObject.activeInHierarchy)
        {
            RectTransform actionPanelRect = ItemActionPanel.Instance.GetComponent<RectTransform>();
            if (actionPanelRect != null && RectTransformUtility.RectangleContainsScreenPoint(
                actionPanelRect, screenPosition, Camera.main))
            {
                return true;
            }
        }

        // Check bank slots
        foreach (var slot in slotUIs)
        {
            if (slot != null && RectTransformUtility.RectangleContainsScreenPoint(
                slot.GetRectTransform(), screenPosition, Camera.main))
            {
                return true;
            }
        }

        // Check bank panel
        RectTransform bankRect = GetComponent<RectTransform>();
        if (bankRect != null)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                bankRect, screenPosition, Camera.main);
        }

        return false;
    }

    /// <summary>
    /// Deselect current slot
    /// </summary>
    private void DeselectCurrentSlot()
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
            RefreshInfo();
        }
    }

    /// <summary>
    /// Create UI slots based on bank capacity
    /// </summary>
    private void CreateSlotUIs()
    {
        if (slotsContainer == null || slotPrefab == null)
        {
            Logger.LogError("BankPanelUI: Missing slotsContainer or slotPrefab!", Logger.LogCategory.InventoryLog);
            return;
        }

        // Clear existing slots
        ClearSlotUIs();

        // Get bank container
        var container = inventoryManager?.GetContainer(currentContainerId);
        if (container == null)
        {
            Logger.LogError($"BankPanelUI: Container '{currentContainerId}' not found!", Logger.LogCategory.InventoryLog);
            return;
        }

        // Create slot UIs
        for (int i = 0; i < container.MaxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            UniversalSlotUI slotUI = slotObj.GetComponent<UniversalSlotUI>();

            if (slotUI != null)
            {
                // Setup with Bank context
                slotUI.Setup(container.Slots[i], i, currentContainerId, UniversalSlotUI.SlotContext.Bank);
                slotUI.OnSlotClicked += OnSlotClicked;
                slotUI.OnSlotRightClicked += OnSlotRightClicked;
                slotUIs.Add(slotUI);
            }
            else
            {
                Logger.LogError("BankPanelUI: SlotPrefab doesn't have UniversalSlotUI component!", Logger.LogCategory.InventoryLog);
            }
        }

        Logger.LogInfo($"BankPanelUI: Created {slotUIs.Count} slot UIs for bank", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Clear all slot UIs
    /// </summary>
    private void ClearSlotUIs()
    {
        foreach (var slotUI in slotUIs)
        {
            if (slotUI != null)
            {
                slotUI.OnSlotClicked -= OnSlotClicked;
                slotUI.OnSlotRightClicked -= OnSlotRightClicked;
                Destroy(slotUI.gameObject);
            }
        }
        slotUIs.Clear();
    }

    /// <summary>
    /// Refresh entire display
    /// </summary>
    private void RefreshDisplay()
    {
        RefreshSlots();
        RefreshInfo();
    }

    /// <summary>
    /// Refresh all slot displays
    /// </summary>
    private void RefreshSlots()
    {
        var container = inventoryManager?.GetContainer(currentContainerId);
        if (container == null) return;

        for (int i = 0; i < slotUIs.Count && i < container.Slots.Count; i++)
        {
            slotUIs[i].Setup(container.Slots[i], i, currentContainerId, UniversalSlotUI.SlotContext.Bank);
        }
    }

    /// <summary>
    /// Refresh info display
    /// </summary>
    private void RefreshInfo()
    {
        var container = inventoryManager?.GetContainer(currentContainerId);
        if (container == null) return;

        // Update capacity
        if (capacityText != null)
        {
            int used = container.GetUsedSlotsCount();
            int max = container.MaxSlots;
            capacityText.text = $"Capacite: {used}/{max}";
        }

        // Update title
        if (titleText != null)
        {
            titleText.text = "Coffre de Banque";
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

    /// <summary>
    /// Handle slot clicked
    /// </summary>
    private void OnSlotClicked(UniversalSlotUI slotUI, int index)
    {
        // Deselect previous
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        // Select new
        selectedSlot = slotUI;
        selectedSlot.SetSelected(true);

        RefreshInfo();
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
    /// Open the bank panel
    /// </summary>
    public void OpenPanel()
    {
        gameObject.SetActive(true);
        RefreshDisplay();



        Logger.LogInfo("BankPanelUI: Bank opened", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Close the bank panel
    /// </summary>
    public void ClosePanel()
    {
        gameObject.SetActive(false);

        // Deselect
        DeselectCurrentSlot();

        // Close ItemActionPanel if open
        if (ItemActionPanel.Instance != null)
        {
            ItemActionPanel.Instance.HidePanel();

        }

        Logger.LogInfo("BankPanelUI: Bank closed", Logger.LogCategory.InventoryLog);
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
        var container = inventoryManager?.GetContainer(currentContainerId);
        if (container == null) return;

        int currentSlots = container.MaxSlots;
        int expandCost = CalculateExpansionCost(currentSlots);
        int newSlots = currentSlots + 10; // Add 10 slots per expansion

        // TODO: Check player gold and deduct cost

        // Expand the container
        container.Resize(newSlots);
        inventoryManager.TriggerContainerChanged(currentContainerId);

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

    // Event handlers
    private void OnContainerChanged(string containerId)
    {
        if (containerId == currentContainerId)
        {
            RefreshDisplay();
        }
    }

    /// <summary>
    /// Get debug info
    /// </summary>
    public string GetDebugInfo()
    {
        var container = inventoryManager?.GetContainer(currentContainerId);
        return $"BankUI State: {slotUIs.Count} slots, Container: {container?.GetDebugInfo() ?? "null"}";
    }
}