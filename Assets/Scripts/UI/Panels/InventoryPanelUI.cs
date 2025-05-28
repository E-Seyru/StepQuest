// Purpose: Main UI controller for the inventory panel
// Filepath: Assets/Scripts/UI/Panels/InventoryPanelUI.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanelUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button closeButton;


    [Header("Info Display")]
    [SerializeField] private TextMeshProUGUI capacityText;
    [SerializeField] private TextMeshProUGUI selectedItemText;

    // Internal state
    private InventoryManager inventoryManager;
    private List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();
    private InventorySlotUI selectedSlot;
    private string currentContainerId = "player";

    public static InventoryPanelUI Instance { get; private set; }

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

        SetupButtons();
    }

    void Start()
    {
        // Get InventoryManager reference
        inventoryManager = InventoryManager.Instance;

        if (inventoryManager == null)
        {
            Debug.LogError("InventoryPanelUI: InventoryManager not found!");
            return;
        }

        // Subscribe to inventory events
        inventoryManager.OnContainerChanged += OnInventoryChanged;
        inventoryManager.OnItemAdded += OnItemAdded;
        inventoryManager.OnItemRemoved += OnItemRemoved;

        // Initial setup
        CreateSlotUIs();
        RefreshDisplay();

        // Hide panel initially
        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (inventoryManager != null)
        {
            inventoryManager.OnContainerChanged -= OnInventoryChanged;
            inventoryManager.OnItemAdded -= OnItemAdded;
            inventoryManager.OnItemRemoved -= OnItemRemoved;
        }
    }

    /// <summary>
    /// Setup button listeners
    /// </summary>
    private void SetupButtons()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

   
    }

    /// <summary>
    /// Create UI slots based on container capacity
    /// </summary>
    private void CreateSlotUIs()
    {
        if (slotsContainer == null || slotPrefab == null)
        {
            Debug.LogError("InventoryPanelUI: Missing slotsContainer or slotPrefab!");
            return;
        }

        // Clear existing slots
        ClearSlotUIs();

        // Get container with safety check
        var container = inventoryManager?.GetContainer(currentContainerId);
        if (container == null)
        {
            Debug.LogError($"InventoryPanelUI: Container '{currentContainerId}' not found! Make sure InventoryManager is initialized.");
            return;
        }

        // Create slot UIs
        for (int i = 0; i < container.MaxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();

            if (slotUI != null)
            {
                slotUI.Setup(container.Slots[i], i);
                slotUI.OnSlotClicked += OnSlotClicked;
                slotUIs.Add(slotUI);
            }
            else
            {
                Debug.LogError("InventoryPanelUI: SlotPrefab doesn't have InventorySlotUI component!");
            }
        }

        Debug.Log($"InventoryPanelUI: Created {slotUIs.Count} slot UIs for container '{currentContainerId}'");
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
            slotUIs[i].Setup(container.Slots[i], i);
        }
    }

    /// <summary>
    /// Refresh info display (capacity, selected item)
    /// </summary>
    private void RefreshInfo()
    {
        var container = inventoryManager?.GetContainer(currentContainerId);
        if (container == null) return;

        // Update capacity display
        if (capacityText != null)
        {
            capacityText.text = $"{container.GetUsedSlotsCount()}/{container.MaxSlots}";
        }

        // Update title
        if (titleText != null)
        {
            titleText.text = $"Inventaire ({container.ContainerType})";
        }

        // Update selected item info
        if (selectedItemText != null)
        {
            if (selectedSlot != null && !selectedSlot.IsEmpty())
            {
                var slot = selectedSlot.GetSlotData();
                selectedItemText.text = $"Sélectionné: {slot.ItemID} x{slot.Quantity}";
            }
            else
            {
                selectedItemText.text = "Aucun objet sélectionné";
            }
        }
    }

    /// <summary>
    /// Handle slot clicked
    /// </summary>
    private void OnSlotClicked(InventorySlotUI slotUI, int index)
    {
        // Deselect previous slot
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        // Select new slot
        selectedSlot = slotUI;
        selectedSlot.SetSelected(true);

        RefreshInfo();
        Debug.Log($"Selected slot {index}: {slotUI.GetSlotData()?.ItemID ?? "Empty"}");
    }

    /// <summary>
    /// Open the panel
    /// </summary>
    public void OpenPanel()
    {
        gameObject.SetActive(true);
        RefreshDisplay();
        Debug.Log("InventoryPanelUI: Panel opened");
    }

    /// <summary>
    /// Close the panel
    /// </summary>
    public void ClosePanel()
    {
        gameObject.SetActive(false);

        // Deselect slot
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
        }

        Debug.Log("InventoryPanelUI: Panel closed");
    }

    // Event handlers
    private void OnInventoryChanged(string containerId)
    {
        if (containerId == currentContainerId)
        {
            RefreshDisplay();
        }
    }

    private void OnItemAdded(string containerId, string itemId, int quantity)
    {
        if (containerId == currentContainerId)
        {
            Debug.Log($"UI: Added {quantity}x {itemId} to {containerId}");
        }
    }

    private void OnItemRemoved(string containerId, string itemId, int quantity)
    {
        if (containerId == currentContainerId)
        {
            Debug.Log($"UI: Removed {quantity}x {itemId} from {containerId}");
        }
    }

    // Test methods (remove later)
    private void TestAddItem(string itemId, int quantity)
    {
        bool success = inventoryManager.AddItem(currentContainerId, itemId, quantity);
        Debug.Log($"Test Add: {itemId} x{quantity} - Success: {success}");
    }

    private void TestRemoveSelectedItem()
    {
        if (selectedSlot != null && !selectedSlot.IsEmpty())
        {
            var slot = selectedSlot.GetSlotData();
            bool success = inventoryManager.RemoveItem(currentContainerId, slot.ItemID, 1);
            Debug.Log($"Test Remove: {slot.ItemID} - Success: {success}");
        }
        else
        {
            Debug.Log("No item selected to remove");
        }
    }

    private void TestClearInventory()
    {
        var container = inventoryManager.GetContainer(currentContainerId);
        if (container != null)
        {
            container.Clear();
            inventoryManager.TriggerContainerChanged(currentContainerId);
            Debug.Log("Test: Inventory cleared");
        }
    }

    /// <summary>
    /// Get debug info
    /// </summary>
    public string GetDebugInfo()
    {
        var container = inventoryManager?.GetContainer(currentContainerId);
        return $"UI State: {slotUIs.Count} slots, Container: {container?.GetDebugInfo() ?? "null"}";
    }
}