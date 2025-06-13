// ===============================================
// 1. NOUVEAU InventoryManager (Facade Pattern)
// ===============================================
// Purpose: Main manager for all inventory operations - REFACTORED
// Filepath: Assets/Scripts/Gameplay/Player/InventoryManager.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int defaultPlayerSlots = 20;
    [SerializeField] private int defaultBankSlots = 50;
    [Header("Item Registry")]
    [SerializeField] private ItemRegistry itemRegistry;
    [Header("Auto-save Settings")]
    [SerializeField] private float autoSaveInterval = 30f;
    [SerializeField] private bool enableAutoSave = true;

    // === PUBLIC ACCESSORS FOR SERVICES ===
    public int DefaultPlayerSlots => defaultPlayerSlots;
    public int DefaultBankSlots => defaultBankSlots;

    // === SAME PUBLIC API - ZERO BREAKING CHANGES ===
    public event Action<string> OnContainerChanged;
    public event Action<string, string, int> OnItemAdded;
    public event Action<string, string, int> OnItemRemoved;

    // === INTERNAL SERVICES (NOUVEAU) ===
    private InventoryContainerService containerService;
    private InventoryPersistenceService persistenceService;
    private InventoryCapacityService capacityService;
    private InventoryValidationService validationService;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeServices();
        }
        else
        {
            Logger.LogWarning("InventoryManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.InventoryLog);
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (DataManager.Instance == null)
        {
            Logger.LogError("InventoryManager: DataManager not found!", Logger.LogCategory.InventoryLog);
            return;
        }

        if (itemRegistry == null)
        {
            Logger.LogError("InventoryManager: ItemRegistry not assigned!", Logger.LogCategory.InventoryLog);
            return;
        }

        // Initialize all services
        containerService.Initialize(defaultPlayerSlots, defaultBankSlots);

        // CRITIQUE: Load data PUIS ensure default containers
        persistenceService.LoadInventoryData();
        persistenceService.EnsureDefaultContainers();

        Logger.LogInfo($"InventoryManager: Initialized with {itemRegistry.AllItems.Count} item definitions", Logger.LogCategory.InventoryLog);
    }

    private void InitializeServices()
    {
        // Create services with dependency injection
        containerService = new InventoryContainerService();
        capacityService = new InventoryCapacityService();
        validationService = new InventoryValidationService();
        persistenceService = new InventoryPersistenceService(
            containerService,
            autoSaveInterval,
            enableAutoSave
        );

        // Wire events
        containerService.OnContainerChanged += (id) => OnContainerChanged?.Invoke(id);
        containerService.OnItemAdded += (cId, iId, qty) => OnItemAdded?.Invoke(cId, iId, qty);
        containerService.OnItemRemoved += (cId, iId, qty) => OnItemRemoved?.Invoke(cId, iId, qty);
    }

    // === PUBLIC API - EXACTLY THE SAME ===
    public bool AddItem(string containerId, string itemId, int quantity)
    {
        if (!validationService.ValidateAddItem(containerId, itemId, quantity, itemRegistry))
            return false;

        bool success = containerService.AddItem(containerId, itemId, quantity, itemRegistry);
        if (success)
        {
            persistenceService.MarkDirty(containerId);
        }
        return success;
    }

    public bool RemoveItem(string containerId, string itemId, int quantity)
    {
        if (!validationService.ValidateRemoveItem(containerId, itemId, quantity))
            return false;

        bool success = containerService.RemoveItem(containerId, itemId, quantity, itemRegistry);
        if (success)
        {
            persistenceService.MarkDirty(containerId);
        }
        return success;
    }

    public bool TransferItem(string fromContainerId, string toContainerId, string itemId, int quantity)
    {
        return containerService.TransferItem(fromContainerId, toContainerId, itemId, quantity, itemRegistry);
    }

    public bool CanAddItem(string containerId, string itemId, int quantity)
    {
        return containerService.CanAddItem(containerId, itemId, quantity, itemRegistry);
    }

    public InventoryContainer GetContainer(string containerId)
    {
        return containerService.GetContainer(containerId);
    }

    public InventoryContainer CreateContainer(InventoryContainerType type, string id, int maxSlots)
    {
        return containerService.CreateContainer(type, id, maxSlots);
    }

    public void UpdatePlayerInventoryCapacity()
    {
        int newCapacity = capacityService.CalculatePlayerInventoryCapacity(defaultPlayerSlots);
        containerService.UpdateContainerCapacity("player", newCapacity);
        persistenceService.MarkDirty("player");
    }

    public ItemRegistry GetItemRegistry()
    {
        return itemRegistry;
    }

    // === PERSISTENCE METHODS - SAME SIGNATURE ===
    public async Task SaveInventoryDataAsync()
    {
        await persistenceService.SaveInventoryDataAsync();
    }

    public void SaveInventoryData()
    {
        persistenceService.SaveInventoryData();
    }

    public void ForceSave()
    {
        persistenceService.ForceSave();
    }

    // === MISSING METHODS - COMPATIBILITY ===
    public void TriggerContainerChanged(string containerId)
    {
        OnContainerChanged?.Invoke(containerId);
    }

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

        info.AppendLine($"Auto-save: {(enableAutoSave ? "Enabled" : "Disabled")}, Interval: {autoSaveInterval}s");
        info.AppendLine($"Unsaved changes: {persistenceService.HasUnsavedChanges()}");
        info.AppendLine("");

        var containers = containerService.GetAllContainers();
        foreach (var kvp in containers)
        {
            info.AppendLine(kvp.Value.GetDebugInfo());
        }

        return info.ToString();
    }

    // === LIFECYCLE ===
    void OnEnable() => persistenceService?.StartAutoSave();
    void OnDisable() => persistenceService?.StopAutoSave();
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) ForceSave();
    }
    void OnApplicationQuit()
    {
        ForceSave();
    }
}

// ===============================================
// 2. SERVICE: Container Management
// ===============================================
public class InventoryContainerService
{
    private Dictionary<string, InventoryContainer> containers;

    public event Action<string> OnContainerChanged;
    public event Action<string, string, int> OnItemAdded;
    public event Action<string, string, int> OnItemRemoved;

    public InventoryContainerService()
    {
        containers = new Dictionary<string, InventoryContainer>();
    }

    public void Initialize(int defaultPlayerSlots, int defaultBankSlots)
    {
        // IMPORTANT: Ne pas créer les containers ici !
        // Ils seront créés dans EnsureDefaultContainers() après le chargement
        Logger.LogInfo("InventoryContainerService: Initialized (containers will be loaded/created later)", Logger.LogCategory.InventoryLog);
    }

    public InventoryContainer CreateContainer(InventoryContainerType type, string id, int maxSlots)
    {
        if (containers.ContainsKey(id))
        {
            Logger.LogWarning($"InventoryContainerService: Container '{id}' already exists!", Logger.LogCategory.InventoryLog);
            return containers[id];
        }

        var container = new InventoryContainer(type, id, maxSlots);
        containers[id] = container;

        Logger.LogInfo($"InventoryContainerService: Created container '{id}' ({type}) with {maxSlots} slots", Logger.LogCategory.InventoryLog);
        return container;
    }

    public InventoryContainer GetContainer(string containerId)
    {
        containers.TryGetValue(containerId, out InventoryContainer container);
        return container;
    }

    public bool AddItem(string containerId, string itemId, int quantity, ItemRegistry registry)
    {
        var container = GetContainer(containerId);
        if (container == null) return false;

        var itemDef = registry.GetItem(itemId);
        if (itemDef == null) return false;

        int remainingToAdd = quantity;

        // Try to add to existing stacks first
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

        // Add to empty slots
        while (remainingToAdd > 0)
        {
            int emptySlotIndex = container.FindFirstEmptySlot();
            if (emptySlotIndex == -1) break;

            int toAdd = itemDef.IsStackable ? Mathf.Min(remainingToAdd, itemDef.MaxStackSize) : 1;
            container.Slots[emptySlotIndex].SetItem(itemId, toAdd);
            remainingToAdd -= toAdd;
        }

        int actuallyAdded = quantity - remainingToAdd;
        if (actuallyAdded > 0)
        {
            OnItemAdded?.Invoke(containerId, itemId, actuallyAdded);
            OnContainerChanged?.Invoke(containerId);
        }

        return remainingToAdd == 0;
    }

    public bool RemoveItem(string containerId, string itemId, int quantity, ItemRegistry registry)
    {
        var container = GetContainer(containerId);
        if (container == null || !container.HasItem(itemId, quantity)) return false;

        int remainingToRemove = quantity;
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
        return true;
    }

    public bool TransferItem(string fromId, string toId, string itemId, int quantity, ItemRegistry registry)
    {
        if (CanAddItem(toId, itemId, quantity, registry) &&
            GetContainer(fromId)?.HasItem(itemId, quantity) == true)
        {
            if (RemoveItem(fromId, itemId, quantity, registry))
            {
                if (AddItem(toId, itemId, quantity, registry))
                {
                    return true;
                }
                else
                {
                    // Rollback
                    AddItem(fromId, itemId, quantity, registry);
                }
            }
        }
        return false;
    }

    public bool CanAddItem(string containerId, string itemId, int quantity, ItemRegistry registry)
    {
        var container = GetContainer(containerId);
        if (container == null) return false;

        var itemDef = registry.GetItem(itemId);
        if (itemDef == null) return false;

        int availableSlots = container.GetAvailableSlots();
        if (!itemDef.IsStackable)
        {
            return quantity <= availableSlots;
        }

        // Calculate space in existing stacks
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

    public void UpdateContainerCapacity(string containerId, int newCapacity)
    {
        var container = GetContainer(containerId);
        if (container != null)
        {
            container.Resize(newCapacity);
            OnContainerChanged?.Invoke(containerId);
        }
    }

    public Dictionary<string, InventoryContainer> GetAllContainers()
    {
        // CRITIQUE: Retourner la référence directe, pas une copie !
        // Sinon les modifications ne sont pas persistées
        return containers;
    }
}

// ===============================================
// 3. SERVICE: Persistence
// ===============================================
public class InventoryPersistenceService
{
    private InventoryContainerService containerService;
    private HashSet<string> dirtyContainers;
    private Coroutine autoSaveCoroutine;
    private float autoSaveInterval;
    private bool enableAutoSave;

    public InventoryPersistenceService(InventoryContainerService containerService, float interval, bool enabled)
    {
        this.containerService = containerService;
        this.autoSaveInterval = interval;
        this.enableAutoSave = enabled;
        this.dirtyContainers = new HashSet<string>();
    }

    public void MarkDirty(string containerId)
    {
        dirtyContainers.Add(containerId);
    }

    public void StartAutoSave()
    {
        if (enableAutoSave && autoSaveCoroutine == null)
        {
            autoSaveCoroutine = InventoryManager.Instance.StartCoroutine(AutoSaveLoop());
        }
    }

    public void StopAutoSave()
    {
        if (autoSaveCoroutine != null)
        {
            InventoryManager.Instance.StopCoroutine(autoSaveCoroutine);
            autoSaveCoroutine = null;
        }
    }

    private IEnumerator AutoSaveLoop()
    {
        var wait = new WaitForSeconds(autoSaveInterval);
        while (true)
        {
            yield return wait;
            // CRITIQUE: Sauvegarder seulement si on a des changements non sauvegardés
            if (dirtyContainers.Count > 0)
            {
                Logger.LogInfo("InventoryPersistenceService: Auto-save triggered", Logger.LogCategory.InventoryLog);
                _ = SaveInventoryDataAsync();
            }
        }
    }

    public async Task SaveInventoryDataAsync()
    {
        if (dirtyContainers.Count == 0) return;

        var snapshot = new List<InventoryContainerData>();
        var containers = containerService.GetAllContainers();

        foreach (var containerId in dirtyContainers)
        {
            if (containers.TryGetValue(containerId, out var container))
            {
                snapshot.Add(InventoryContainerData.FromInventoryContainer(container));
            }
        }

        try
        {
            await Task.Run(() =>
            {
                foreach (var data in snapshot)
                {
                    DataManager.Instance.LocalDatabase.SaveInventoryContainer(data);
                }
            });

            dirtyContainers.Clear();
        }
        catch (Exception ex)
        {
            Logger.LogError($"InventoryPersistenceService: Save error: {ex.Message}", Logger.LogCategory.InventoryLog);
        }
    }

    public void SaveInventoryData()
    {
        // CRITIQUE: Sauvegarder TOUS les containers, pas seulement les dirty
        // pour maintenir la compatibilité avec l'ancien comportement
        Logger.LogInfo("InventoryPersistenceService: Saving inventory data to database...", Logger.LogCategory.InventoryLog);

        if (DataManager.Instance?.LocalDatabase == null)
        {
            Logger.LogError("InventoryPersistenceService: LocalDatabase not available!", Logger.LogCategory.InventoryLog);
            return;
        }

        try
        {
            var containers = containerService.GetAllContainers();
            int savedCount = 0;

            foreach (var container in containers.Values)
            {
                var data = InventoryContainerData.FromInventoryContainer(container);
                DataManager.Instance.LocalDatabase.SaveInventoryContainer(data);
                savedCount++;
            }

            dirtyContainers.Clear();
            Logger.LogInfo($"InventoryPersistenceService: Successfully saved {savedCount} containers", Logger.LogCategory.InventoryLog);
        }
        catch (Exception ex)
        {
            Logger.LogError($"InventoryPersistenceService: Save error: {ex.Message}", Logger.LogCategory.InventoryLog);
        }
    }

    public void LoadInventoryData()
    {
        Logger.LogInfo("InventoryPersistenceService: Loading inventory data from database...", Logger.LogCategory.InventoryLog);

        if (DataManager.Instance?.LocalDatabase == null)
        {
            Logger.LogError("InventoryPersistenceService: LocalDatabase not available!", Logger.LogCategory.InventoryLog);
            return;
        }

        try
        {
            var containerDataList = DataManager.Instance.LocalDatabase.LoadAllInventoryContainers();
            var containers = containerService.GetAllContainers();

            foreach (var containerData in containerDataList)
            {
                var container = containerData.ToInventoryContainer();
                // CRITIQUE: Remplacer le container en mémoire avec les données chargées
                containers[container.ContainerID] = container;
                Logger.LogInfo($"InventoryPersistenceService: Loaded container '{container.ContainerID}' with {container.GetUsedSlotsCount()}/{container.MaxSlots} used slots", Logger.LogCategory.InventoryLog);
            }

            Logger.LogInfo($"InventoryPersistenceService: Successfully loaded {containerDataList.Count} containers", Logger.LogCategory.InventoryLog);
        }
        catch (Exception ex)
        {
            Logger.LogError($"InventoryPersistenceService: Load error: {ex.Message}", Logger.LogCategory.InventoryLog);
        }
    }

    public void EnsureDefaultContainers()
    {
        var containers = containerService.GetAllContainers();

        // CRITIQUE: Vérifier si les containers par défaut existent
        if (!containers.ContainsKey("player"))
        {
            Logger.LogInfo("InventoryPersistenceService: Creating missing player container", Logger.LogCategory.InventoryLog);
            int playerCapacity = InventoryManager.Instance.DefaultPlayerSlots;
            containerService.CreateContainer(InventoryContainerType.Player, "player", playerCapacity);
            MarkDirty("player");
        }

        if (!containers.ContainsKey("bank"))
        {
            Logger.LogInfo("InventoryPersistenceService: Creating missing bank container", Logger.LogCategory.InventoryLog);
            int bankCapacity = InventoryManager.Instance.DefaultBankSlots;
            containerService.CreateContainer(InventoryContainerType.Bank, "bank", bankCapacity);
            MarkDirty("bank");
        }
    }

    public void ForceSave()
    {
        // CRITIQUE: Force save = sauvegarder immédiatement TOUS les containers
        Logger.LogInfo("InventoryPersistenceService: Force save requested", Logger.LogCategory.InventoryLog);
        SaveInventoryData(); // Sauvegarde tous les containers
    }

    public bool HasUnsavedChanges()
    {
        return dirtyContainers.Count > 0;
    }
}

// ===============================================
// 4. SERVICE: Capacity Calculation
// ===============================================
public class InventoryCapacityService
{
    public int CalculatePlayerInventoryCapacity(int baseCapacity)
    {
        int equipmentBonus = 0;
        int upgradeBonus = 0;

        // TODO: Get bonuses from equipment and upgrades when available
        // if (EquipmentManager.Instance != null)
        // {
        //     equipmentBonus = EquipmentManager.Instance.GetInventoryCapacity();
        // }

        return baseCapacity + equipmentBonus + upgradeBonus;
    }
}

// ===============================================
// 5. SERVICE: Validation
// ===============================================
public class InventoryValidationService
{
    public bool ValidateAddItem(string containerId, string itemId, int quantity, ItemRegistry registry)
    {
        if (quantity <= 0)
        {
            Logger.LogWarning("InventoryValidationService: Invalid quantity", Logger.LogCategory.InventoryLog);
            return false;
        }

        if (string.IsNullOrEmpty(containerId) || string.IsNullOrEmpty(itemId))
        {
            Logger.LogWarning("InventoryValidationService: Invalid IDs", Logger.LogCategory.InventoryLog);
            return false;
        }

        if (registry?.GetItem(itemId) == null)
        {
            Logger.LogError($"InventoryValidationService: Item '{itemId}' not found in registry", Logger.LogCategory.InventoryLog);
            return false;
        }

        return true;
    }

    public bool ValidateRemoveItem(string containerId, string itemId, int quantity)
    {
        if (quantity <= 0 || string.IsNullOrEmpty(containerId) || string.IsNullOrEmpty(itemId))
        {
            Logger.LogWarning("InventoryValidationService: Invalid parameters", Logger.LogCategory.InventoryLog);
            return false;
        }

        return true;
    }
}