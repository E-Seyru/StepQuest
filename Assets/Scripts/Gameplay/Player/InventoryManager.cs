// Purpose: Main manager for all inventory operations across different container types
// Filepath: Assets/Scripts/Gameplay/Player/InventoryManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int defaultPlayerSlots = 20;
    [SerializeField] private int defaultBankSlots = 50;

    [Header("Item Registry")]
    [SerializeField] private ItemRegistry itemRegistry;

    // Container storage
    private Dictionary<string, InventoryContainer> containers;

    // References
    private DataManager dataManager;
    private EquipmentManager equipmentManager;

    // Events
    public event Action<string> OnContainerChanged; // ContainerID
    public event Action<string, string, int> OnItemAdded; // ContainerID, ItemID, Quantity
    public event Action<string, string, int> OnItemRemoved; // ContainerID, ItemID, Quantity

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            containers = new Dictionary<string, InventoryContainer>();

            // Initialize containers early in Awake to ensure they exist
            InitializeContainers();
        }
        else
        {
            Logger.LogWarning("InventoryManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.InventoryLog);
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Get references
        dataManager = DataManager.Instance;
        // equipmentManager = EquipmentManager.Instance; // TODO: Add Singleton pattern to EquipmentManager

        if (dataManager == null)
        {
            Logger.LogError("InventoryManager: DataManager not found!", Logger.LogCategory.InventoryLog);
            return;
        }

        // Validate ItemRegistry
        if (itemRegistry == null)
        {
            Logger.LogError("InventoryManager: ItemRegistry not assigned! Please assign it in the inspector.", Logger.LogCategory.InventoryLog);
            return;
        }

        // Load inventory data (containers already created in Awake)
        LoadInventoryData();

        Logger.LogInfo($"InventoryManager: Initialized with {itemRegistry.AllItems.Count} item definitions", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Initialize default containers
    /// </summary>
    private void InitializeContainers()
    {
        // Create player inventory with dynamic capacity
        int playerCapacity = CalculatePlayerInventoryCapacity();
        CreateContainer(InventoryContainerType.Player, "player", playerCapacity);

        // Create bank inventory
        CreateContainer(InventoryContainerType.Bank, "bank", defaultBankSlots);

        Logger.LogInfo($"InventoryManager: Initialized with player capacity: {playerCapacity}", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Calculate player inventory capacity based on equipment and upgrades
    /// </summary>
    private int CalculatePlayerInventoryCapacity()
    {
        int baseCapacity = defaultPlayerSlots;
        int equipmentBonus = 0;
        int upgradeBonus = 0;

        // Get backpack bonus from equipment manager
        // TODO: Uncomment when EquipmentManager has Singleton pattern
        // if (equipmentManager != null)
        // {
        //     equipmentBonus = equipmentManager.GetInventoryCapacity();
        // }

        // Get upgrade bonus from player data
        if (dataManager?.PlayerData != null)
        {
            // TODO: Add InventoryUpgrades field to PlayerData
            // upgradeBonus = dataManager.PlayerData.InventoryUpgrades;
        }

        int totalCapacity = baseCapacity + equipmentBonus + upgradeBonus;
        Logger.LogInfo($"InventoryManager: Player capacity calculation - Base: {baseCapacity}, Equipment: {equipmentBonus}, Upgrades: {upgradeBonus}, Total: {totalCapacity}", Logger.LogCategory.InventoryLog);

        return totalCapacity;
    }

    /// <summary>
    /// Create a new container
    /// </summary>
    public InventoryContainer CreateContainer(InventoryContainerType type, string id, int maxSlots)
    {
        if (containers.ContainsKey(id))
        {
            Logger.LogWarning($"InventoryManager: Container '{id}' already exists!", Logger.LogCategory.InventoryLog);
            return containers[id];
        }

        var container = new InventoryContainer(type, id, maxSlots);
        containers[id] = container;

        Logger.LogInfo($"InventoryManager: Created container '{id}' ({type}) with {maxSlots} slots", Logger.LogCategory.InventoryLog);
        return container;
    }

    /// <summary>
    /// Get a container by ID
    /// </summary>
    public InventoryContainer GetContainer(string containerId)
    {
        containers.TryGetValue(containerId, out InventoryContainer container);
        return container;
    }

    /// <summary>
    /// Add item to specific container
    /// </summary>
    public bool AddItem(string containerId, string itemId, int quantity)
    {
        if (quantity <= 0) return false;

        var container = GetContainer(containerId);
        if (container == null)
        {
            Logger.LogError($"InventoryManager: Container '{containerId}' not found!", Logger.LogCategory.InventoryLog);
            return false;
        }

        // MODIFIÉ: Utilise le nouveau ItemRegistry
        var itemDef = GetItemDefinition(itemId);
        if (itemDef == null)
        {
            Logger.LogError($"InventoryManager: Item '{itemId}' definition not found in ItemRegistry!", Logger.LogCategory.InventoryLog);
            return false;
        }

        int remainingToAdd = quantity;

        // If item is stackable, try to add to existing stacks first
        if (itemDef.IsStackable)
        {
            foreach (var slot in container.Slots)
            {
                if (slot.HasItem(itemId))
                {
                    int canAddToStack = itemDef.MaxStackSize - slot.Quantity;
                    int toAdd = Mathf.Min(remainingToAdd, canAddToStack);

                    if (toAdd > 0)
                    {
                        slot.AddQuantity(toAdd);
                        remainingToAdd -= toAdd;

                        if (remainingToAdd <= 0) break;
                    }
                }
            }
        }

        // Add remaining items to empty slots
        while (remainingToAdd > 0)
        {
            int emptySlotIndex = container.FindFirstEmptySlot();
            if (emptySlotIndex == -1)
            {
                Logger.LogWarning($"InventoryManager: Container '{containerId}' is full! Could not add {remainingToAdd} of '{itemId}'", Logger.LogCategory.InventoryLog);
                break;
            }

            int toAdd = itemDef.IsStackable ? Mathf.Min(remainingToAdd, itemDef.MaxStackSize) : 1;
            container.Slots[emptySlotIndex].SetItem(itemId, toAdd);
            remainingToAdd -= toAdd;
        }

        int actuallyAdded = quantity - remainingToAdd;
        if (actuallyAdded > 0)
        {
            OnItemAdded?.Invoke(containerId, itemId, actuallyAdded);
            OnContainerChanged?.Invoke(containerId);
            Logger.LogInfo($"InventoryManager: Added {actuallyAdded} of '{itemDef.GetDisplayName()}' to '{containerId}'", Logger.LogCategory.InventoryLog);
        }

        return remainingToAdd == 0; // Return true if all items were added
    }

    /// <summary>
    /// Remove item from specific container
    /// </summary>
    public bool RemoveItem(string containerId, string itemId, int quantity)
    {
        if (quantity <= 0) return false;

        var container = GetContainer(containerId);
        if (container == null)
        {
            Logger.LogError($"InventoryManager: Container '{containerId}' not found!", Logger.LogCategory.InventoryLog);
            return false;
        }

        if (!container.HasItem(itemId, quantity))
        {
            Logger.LogWarning($"InventoryManager: Not enough '{itemId}' in '{containerId}' (need {quantity}, have {container.GetItemQuantity(itemId)})", Logger.LogCategory.InventoryLog);
            return false;
        }

        int remainingToRemove = quantity;

        // Remove from slots containing the item
        for (int i = 0; i < container.Slots.Count && remainingToRemove > 0; i++)
        {
            var slot = container.Slots[i];
            if (slot.HasItem(itemId))
            {
                int toRemove = Mathf.Min(remainingToRemove, slot.Quantity);
                slot.RemoveQuantity(toRemove);
                remainingToRemove -= toRemove;
            }
        }

        OnItemRemoved?.Invoke(containerId, itemId, quantity);
        OnContainerChanged?.Invoke(containerId);

        // Get item name for better logging
        var itemDef = GetItemDefinition(itemId);
        string itemName = itemDef?.GetDisplayName() ?? itemId;
        Logger.LogInfo($"InventoryManager: Removed {quantity} of '{itemName}' from '{containerId}'", Logger.LogCategory.InventoryLog);

        return true;
    }

    /// <summary>
    /// Transfer item between containers
    /// </summary>
    public bool TransferItem(string fromContainerId, string toContainerId, string itemId, int quantity)
    {
        var fromContainer = GetContainer(fromContainerId);
        var toContainer = GetContainer(toContainerId);

        if (fromContainer == null || toContainer == null)
        {
            Logger.LogError($"InventoryManager: Cannot transfer - invalid containers '{fromContainerId}' or '{toContainerId}'", Logger.LogCategory.InventoryLog);
            return false;
        }

        if (!fromContainer.HasItem(itemId, quantity))
        {
            Logger.LogWarning($"InventoryManager: Cannot transfer - not enough '{itemId}' in '{fromContainerId}'", Logger.LogCategory.InventoryLog);
            return false;
        }

        // Check if destination can accept the items
        if (!CanAddItem(toContainerId, itemId, quantity))
        {
            Logger.LogWarning($"InventoryManager: Cannot transfer - '{toContainerId}' cannot accept {quantity} of '{itemId}'", Logger.LogCategory.InventoryLog);
            return false;
        }

        // Perform the transfer
        if (RemoveItem(fromContainerId, itemId, quantity))
        {
            if (AddItem(toContainerId, itemId, quantity))
            {
                var itemDef = GetItemDefinition(itemId);
                string itemName = itemDef?.GetDisplayName() ?? itemId;
                Logger.LogInfo($"InventoryManager: Transferred {quantity} of '{itemName}' from '{fromContainerId}' to '{toContainerId}'", Logger.LogCategory.InventoryLog);
                return true;
            }
            else
            {
                // Rollback if add failed
                AddItem(fromContainerId, itemId, quantity);
                Logger.LogError($"InventoryManager: Transfer failed - rollback performed", Logger.LogCategory.InventoryLog);
            }
        }

        return false;
    }

    /// <summary>
    /// Check if container can accept specific item and quantity
    /// </summary>
    public bool CanAddItem(string containerId, string itemId, int quantity)
    {
        var container = GetContainer(containerId);
        if (container == null) return false;

        var itemDef = GetItemDefinition(itemId);
        if (itemDef == null) return false;

        // Simple check - this could be more sophisticated
        int availableSlots = container.GetAvailableSlots();
        if (itemDef.IsStackable)
        {
            // Calculate how much space is available in existing stacks
            int spaceInExistingStacks = 0;
            foreach (var slot in container.Slots)
            {
                if (slot.HasItem(itemId))
                {
                    spaceInExistingStacks += (itemDef.MaxStackSize - slot.Quantity);
                }
            }

            int spaceNeeded = Mathf.Max(0, quantity - spaceInExistingStacks);
            int slotsNeeded = Mathf.CeilToInt((float)spaceNeeded / itemDef.MaxStackSize);

            return slotsNeeded <= availableSlots;
        }
        else
        {
            return quantity <= availableSlots;
        }
    }

    /// <summary>
    /// MODIFIÉ: Get item definition via ItemRegistry
    /// </summary>
    private ItemDefinition GetItemDefinition(string itemId)
    {
        if (itemRegistry == null)
        {
            Logger.LogError("InventoryManager: ItemRegistry is null! Cannot get item definition.", Logger.LogCategory.InventoryLog);
            return null;
        }

        return itemRegistry.GetItem(itemId);
    }

    /// <summary>
    /// NOUVEAU: Get ItemRegistry reference (for other systems)
    /// </summary>
    public ItemRegistry GetItemRegistry()
    {
        return itemRegistry;
    }

    /// <summary>
    /// Update player inventory capacity when equipment changes
    /// </summary>
    public void UpdatePlayerInventoryCapacity()
    {
        var playerContainer = GetContainer("player");
        if (playerContainer != null)
        {
            int newCapacity = CalculatePlayerInventoryCapacity();
            playerContainer.Resize(newCapacity);
            OnContainerChanged?.Invoke("player");
        }
    }

    /// <summary>
    /// Load inventory data from save
    /// </summary>
    private void LoadInventoryData()
    {
        // TODO: Implement loading from database
        Logger.LogInfo("InventoryManager: Loading inventory data (TODO)", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Save inventory data
    /// </summary>
    public void SaveInventoryData()
    {
        // TODO: Implement saving to database
        Logger.LogInfo("InventoryManager: Saving inventory data (TODO)", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Trigger container changed event (for external use)
    /// </summary>
    public void TriggerContainerChanged(string containerId)
    {
        OnContainerChanged?.Invoke(containerId);
    }

    /// <summary>
    /// Get debug info for all containers
    /// </summary>
    public string GetDebugInfo()
    {
        var info = new System.Text.StringBuilder();
        info.AppendLine("=== Inventory Manager Debug ===");

        if (itemRegistry != null)
        {
            info.AppendLine($"ItemRegistry: {itemRegistry.AllItems.Count} items loaded");
        }
        else
        {
            info.AppendLine("❌ ItemRegistry: NOT ASSIGNED");
        }

        foreach (var kvp in containers)
        {
            info.AppendLine(kvp.Value.GetDebugInfo());
        }

        return info.ToString();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveInventoryData();
        }
    }

    void OnApplicationQuit()
    {
        SaveInventoryData();
    }
}