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
    [SerializeField] private GameObject abilitiesScrollViewPanel; // The scroll view panel to deactivate
    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Info Display")]
    [SerializeField] private TextMeshProUGUI selectedItemText;

    // Tab system state
    private InventoryTab currentTab = InventoryTab.Items;

    // Original colors for tabs
    private Color itemsTabTextOriginalColor;
    private Color abilitiesTabTextOriginalColor;
    private Color itemsTabButtonOriginalColor;
    private Color abilitiesTabButtonOriginalColor;

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
            var text = itemsTabButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) itemsTabTextOriginalColor = text.color;
            var image = itemsTabButton.GetComponent<Image>();
            if (image != null) itemsTabButtonOriginalColor = image.color;
        }

        if (abilitiesTabButton != null)
        {
            abilitiesTabButton.onClick.AddListener(() => SwitchToTab(InventoryTab.Abilities));
            var text = abilitiesTabButton.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) abilitiesTabTextOriginalColor = text.color;
            var image = abilitiesTabButton.GetComponent<Image>();
            if (image != null) abilitiesTabButtonOriginalColor = image.color;
        }
    }

    public void SwitchToTab(InventoryTab tab)
    {
        currentTab = tab;

        UpdateTabVisuals();

        bool isItemsTab = tab == InventoryTab.Items;

        // Deactivate one panel, activate the other
        if (slotsContainer != null)
        {
            slotsContainer.gameObject.SetActive(isItemsTab);
        }

        if (abilitiesScrollViewPanel != null)
        {
            abilitiesScrollViewPanel.SetActive(!isItemsTab);
        }

        // Refresh abilities display when switching to that tab
        if (!isItemsTab && abilitiesContainer != null)
        {
            abilitiesContainer.RefreshDisplay();
        }

        DeselectCurrentSlot();

        if (isItemsTab)
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
        UpdateTabButton(itemsTabButton, currentTab == InventoryTab.Items, itemsTabTextOriginalColor, itemsTabButtonOriginalColor);
        UpdateTabButton(abilitiesTabButton, currentTab == InventoryTab.Abilities, abilitiesTabTextOriginalColor, abilitiesTabButtonOriginalColor);
    }

    private void UpdateTabButton(Button button, bool isActive, Color originalTextColor, Color originalButtonColor)
    {
        if (button == null) return;

        float dimFactor = inactiveTabColor.r / activeTabColor.r; // Calculate dim ratio

        // Update button image color
        var image = button.GetComponent<Image>();
        if (image != null)
        {
            if (isActive)
            {
                image.color = originalButtonColor;
            }
            else
            {
                image.color = new Color(
                    originalButtonColor.r * dimFactor,
                    originalButtonColor.g * dimFactor,
                    originalButtonColor.b * dimFactor,
                    originalButtonColor.a
                );
            }
        }

        // Update child text color
        var text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            if (isActive)
            {
                text.color = originalTextColor;
            }
            else
            {
                text.color = new Color(
                    originalTextColor.r * dimFactor,
                    originalTextColor.g * dimFactor,
                    originalTextColor.b * dimFactor,
                    originalTextColor.a
                );
            }
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
            Logger.LogError("InventoryManager not initialized!", Logger.LogCategory.InventoryLog);
        }
    }
}
