// Purpose: Main UI controller for the inventory panel using UniversalSlotUI
// Filepath: Assets/Scripts/UI/Panels/InventoryPanelUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryPanelUI : ContainerPanelUI
{
    public enum InventoryTab
    {
        Items,
        Abilities
    }

    [Header("Inventory-Specific")]
    [SerializeField] private Button addWoodButton;

    [Header("Tab System")]
    [SerializeField] private Button itemsTabButton;
    [SerializeField] private Button abilitiesTabButton;
    [SerializeField] private AbilitiesInventoryContainer abilitiesContainer;
    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Info Display")]
    [SerializeField] private TextMeshProUGUI selectedItemText;

    // Tab system state
    private InventoryTab currentTab = InventoryTab.Items;

    public static InventoryPanelUI Instance { get; private set; }
    public InventoryTab CurrentTab => currentTab;

    // Base class abstract implementations
    protected override string ContainerId => GameConstants.ContainerIdPlayer;
    protected override UniversalSlotUI.SlotContext SlotContext => UniversalSlotUI.SlotContext.PlayerInventory;

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
        // Subscribe to ability events
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnOwnedAbilitiesChanged += OnOwnedAbilitiesChanged;
        }

        // Setup tab buttons
        SetupTabButtons();

        // Start on Items tab
        SwitchToTab(InventoryTab.Items);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.OnOwnedAbilitiesChanged -= OnOwnedAbilitiesChanged;
        }
    }

    protected override void OnRefreshInfo(InventoryContainer container)
    {
        // Update title based on tab
        if (titleText != null)
        {
            titleText.text = currentTab == InventoryTab.Items ? "Inventaire" : "Abilities";
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

        // Update capacity based on current tab
        UpdateCapacityText();
    }

    // === TAB SYSTEM ===

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

    public void SwitchToTab(InventoryTab tab)
    {
        currentTab = tab;

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

        DeselectCurrentSlot();

        if (tab == InventoryTab.Items)
        {
            RefreshDisplay();
        }

        UpdateCapacityText();

        Logger.LogInfo($"InventoryPanelUI: Switched to {tab} tab", Logger.LogCategory.General);
    }

    private void UpdateCapacityText()
    {
        if (capacityText == null) return;

        if (currentTab == InventoryTab.Items)
        {
            var container = inventoryManager?.GetContainer(ContainerId);
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

    private void UpdateTabVisuals()
    {
        UpdateTabButton(itemsTabButton, currentTab == InventoryTab.Items);
        UpdateTabButton(abilitiesTabButton, currentTab == InventoryTab.Abilities);
    }

    private void UpdateTabButton(Button button, bool isActive)
    {
        if (button == null) return;

        var colors = button.colors;
        colors.normalColor = isActive ? activeTabColor : inactiveTabColor;
        button.colors = colors;

        var image = button.GetComponent<Image>();
        if (image != null)
        {
            image.color = isActive ? activeTabColor : inactiveTabColor;
        }
    }

    private void OnOwnedAbilitiesChanged()
    {
        if (currentTab == InventoryTab.Abilities)
        {
            UpdateCapacityText();
        }
    }

    // Test method for adding wood (temporary)
    public void AddWood()
    {
        if (inventoryManager != null)
        {
            inventoryManager.AddItem(ContainerId, "Pin", 1);
        }
        else
        {
            Debug.LogError("InventoryManager not initialized!");
        }
    }
}
