// Purpose: Main UI controller for the inventory panel using UniversalSlotUI
// Filepath: Assets/Scripts/UI/Panels/InventoryPanelUI.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanelUI : MonoBehaviour
{
    public enum InventoryTab
    {
        Items,
        Abilities
    }

    [Header("UI References")]
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private GameObject slotPrefab; // Should have UniversalSlotUI component
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button addWoodButton;

    [Header("Tab System")]
    [SerializeField] private Button itemsTabButton;
    [SerializeField] private Button abilitiesTabButton;
    [SerializeField] private AbilitiesInventoryContainer abilitiesContainer;
    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Info Display")]
    [SerializeField] private TextMeshProUGUI capacityText;
    [SerializeField] private TextMeshProUGUI selectedItemText;

    // Internal state
    private InventoryManager inventoryManager;
    private List<UniversalSlotUI> slotUIs = new List<UniversalSlotUI>();
    private UniversalSlotUI selectedSlot;
    private string currentContainerId = GameConstants.ContainerIdPlayer;

    // Tab system state
    private InventoryTab currentTab = InventoryTab.Items;

    // Auto-deselection
    private bool isPointerOverInventory = false;

    public static InventoryPanelUI Instance { get; private set; }
    public InventoryTab CurrentTab => currentTab;

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
            Debug.LogError("InventoryPanelUI: InventoryManager not found!");
            return;
        }

        // Subscribe to inventory events
        inventoryManager.OnContainerChanged += OnInventoryChanged;

        // Subscribe to ability events
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnOwnedAbilitiesChanged += OnOwnedAbilitiesChanged;
        }

        // Setup tab buttons
        SetupTabButtons();

        // Initial setup
        CreateSlotUIs();
        RefreshDisplay();

        // Start on Items tab
        SwitchToTab(InventoryTab.Items);
    }

    void Update()
    {
        // Deselectionner automatiquement si on clique/touche en dehors (mobile-friendly)
        CheckForDeselection();
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (inventoryManager != null)
        {
            inventoryManager.OnContainerChanged -= OnInventoryChanged;
        }

        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnOwnedAbilitiesChanged -= OnOwnedAbilitiesChanged;
        }
    }

    /// <summary>
    /// Verifier si on doit deselectionner automatiquement (mobile-friendly)
    /// </summary>
    private void CheckForDeselection()
    {
        if (selectedSlot == null) return;

        // Detecter clic/touch sur mobile et desktop
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
            // Verifier si le clic/touch est en dehors de l'inventaire
            if (!IsPointerOverInventoryArea())
            {
                DeselectCurrentSlot();
            }
        }
    }

    /// <summary>
    /// Verifier si le pointeur/touch est sur la zone d'inventaire
    /// </summary>
    private bool IsPointerOverInventoryArea()
    {
        Vector2 screenPosition;

        // Obtenir la position selon le type d'input
        if (Input.touchCount > 0)
        {
            screenPosition = Input.GetTouch(0).position;
        }
        else
        {
            screenPosition = Input.mousePosition;
        }

        // Verifier si on est sur l'ItemActionPanel (ne pas deselectionner si on clique dessus)
        if (ItemActionPanel.Instance != null && ItemActionPanel.Instance.gameObject.activeInHierarchy)
        {
            RectTransform actionPanelRect = ItemActionPanel.Instance.GetComponent<RectTransform>();
            if (actionPanelRect != null && RectTransformUtility.RectangleContainsScreenPoint(
                actionPanelRect, screenPosition, Camera.main))
            {
                return true; // On est sur l'ActionPanel, ne pas deselectionner
            }
        }

        // Verifier si on est sur un slot d'inventaire
        foreach (var slot in slotUIs)
        {
            if (slot != null && RectTransformUtility.RectangleContainsScreenPoint(
                slot.GetRectTransform(), screenPosition, Camera.main))
            {
                return true;
            }
        }

        // Verifier si on est sur la zone de l'inventaire en general
        RectTransform inventoryRect = GetComponent<RectTransform>();
        if (inventoryRect != null)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                inventoryRect, screenPosition, Camera.main);
        }

        return false;
    }

    /// <summary>
    /// Deselectionner le slot actuel
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
            UniversalSlotUI slotUI = slotObj.GetComponent<UniversalSlotUI>();

            if (slotUI != null)
            {
                // Setup with PlayerInventory context
                slotUI.Setup(container.Slots[i], i, currentContainerId, UniversalSlotUI.SlotContext.PlayerInventory);
                slotUI.OnSlotClicked += OnSlotClicked;
                slotUIs.Add(slotUI);
            }
            else
            {
                Debug.LogError("InventoryPanelUI: SlotPrefab doesn't have UniversalSlotUI component!");
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
            slotUIs[i].Setup(container.Slots[i], i, currentContainerId, UniversalSlotUI.SlotContext.PlayerInventory);
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
                var itemDef = inventoryManager.GetItemRegistry().GetItem(slot.ItemID);
                string itemName = itemDef?.GetDisplayName() ?? slot.ItemID;
                selectedItemText.text = $"Selectionne: {itemName} x{slot.Quantity}";
            }
            else
            {
                selectedItemText.text = "Aucun objet selectionne";
            }
        }
    }

    /// <summary>
    /// Handle slot clicked
    /// </summary>
    private void OnSlotClicked(UniversalSlotUI slotUI, int index)
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
    }

    /// <summary>
    /// Open the panel
    /// </summary>
    public void OpenPanel()
    {
        gameObject.SetActive(true);
        RefreshDisplay();
    }

    /// <summary>
    /// Close the panel
    /// </summary>
    public void ClosePanel()
    {
        gameObject.SetActive(false);

        // Deselect slot
        DeselectCurrentSlot();

        // Fermer ItemActionPanel si ouvert
        if (ItemActionPanel.Instance != null)
        {
            ItemActionPanel.Instance.HidePanel();
        }
    }

    // Event handlers
    private void OnInventoryChanged(string containerId)
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
        return $"UI State: {slotUIs.Count} slots, Container: {container?.GetDebugInfo() ?? "null"}";
    }

    // Test method for adding wood (temporary)
    public void AddWood()
    {
        if (inventoryManager != null)
        {
            inventoryManager.AddItem(currentContainerId, "Pin", 1);
        }
        else
        {
            Debug.LogError("InventoryManager not initialized!");
        }
    }

    // === TAB SYSTEM ===

    /// <summary>
    /// Setup tab button listeners
    /// </summary>
    private void SetupTabButtons()
    {
        if (itemsTabButton != null)
        {
            itemsTabButton.onClick.AddListener(() => SwitchToTab(InventoryTab.Items));
        }

        if (abilitiesTabButton != null)
        {
            abilitiesTabButton.onClick.AddListener(() => SwitchToTab(InventoryTab.Abilities));
        }
    }

    /// <summary>
    /// Switch to a specific tab
    /// </summary>
    public void SwitchToTab(InventoryTab tab)
    {
        currentTab = tab;

        // Update tab button visuals
        UpdateTabVisuals();

        // Show/hide appropriate containers
        if (slotsContainer != null)
        {
            slotsContainer.gameObject.SetActive(tab == InventoryTab.Items);
        }

        if (abilitiesContainer != null)
        {
            abilitiesContainer.gameObject.SetActive(tab == InventoryTab.Abilities);
            if (tab == InventoryTab.Abilities)
            {
                abilitiesContainer.RefreshDisplay();
            }
        }

        // Deselect any selected slots
        DeselectCurrentSlot();

        // Refresh the appropriate display
        if (tab == InventoryTab.Items)
        {
            RefreshDisplay();
        }

        // Update title and capacity text
        if (titleText != null)
        {
            titleText.text = tab == InventoryTab.Items ? "Inventaire" : "Abilities";
        }

        // Update capacity text
        UpdateCapacityText();

        Logger.LogInfo($"InventoryPanelUI: Switched to {tab} tab", Logger.LogCategory.General);
    }

    /// <summary>
    /// Update capacity text based on current tab
    /// </summary>
    private void UpdateCapacityText()
    {
        if (capacityText == null) return;

        if (currentTab == InventoryTab.Items)
        {
            var container = inventoryManager?.GetContainer(currentContainerId);
            if (container != null)
            {
                capacityText.text = $"{container.GetUsedSlotsCount()}/{container.MaxSlots}";
            }
        }
        else
        {
            if (AbilityManager.Instance != null)
            {
                int owned = AbilityManager.Instance.GetOwnedAbilities().Count;
                capacityText.text = $"{owned} abilities";
            }
        }
    }

    /// <summary>
    /// Update tab button visuals
    /// </summary>
    private void UpdateTabVisuals()
    {
        if (itemsTabButton != null)
        {
            var colors = itemsTabButton.colors;
            colors.normalColor = currentTab == InventoryTab.Items ? activeTabColor : inactiveTabColor;
            itemsTabButton.colors = colors;

            // Also update the image if there's one
            var image = itemsTabButton.GetComponent<Image>();
            if (image != null)
            {
                image.color = currentTab == InventoryTab.Items ? activeTabColor : inactiveTabColor;
            }
        }

        if (abilitiesTabButton != null)
        {
            var colors = abilitiesTabButton.colors;
            colors.normalColor = currentTab == InventoryTab.Abilities ? activeTabColor : inactiveTabColor;
            abilitiesTabButton.colors = colors;

            var image = abilitiesTabButton.GetComponent<Image>();
            if (image != null)
            {
                image.color = currentTab == InventoryTab.Abilities ? activeTabColor : inactiveTabColor;
            }
        }
    }

    /// <summary>
    /// Handle owned abilities changed event
    /// </summary>
    private void OnOwnedAbilitiesChanged()
    {
        if (currentTab == InventoryTab.Abilities)
        {
            UpdateCapacityText();
        }
    }
}