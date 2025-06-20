// Purpose: Central registry for all game items, provides fast lookup and validation - ROBUST VERSION
// Filepath: Assets/Scripts/Data/Registry/ItemRegistry.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemRegistry", menuName = "WalkAndRPG/Item Registry")]
public class ItemRegistry : ScriptableObject
{
    [Header("All Game Items")]
    [Tooltip("Drag all your ItemDefinition assets here")]
    public List<ItemDefinition> AllItems = new List<ItemDefinition>();

    [Header("Robustness Settings")]
    [SerializeField] private bool enableFallbackSearch = true;
    [SerializeField] private bool logMissingItems = false; // Desactive par defaut pour eviter le spam

    [Header("Debug Info")]
    [Tooltip("Shows validation errors if any")]
    [TextArea(3, 6)]
    public string ValidationStatus = "Click 'Validate Registry' to check for issues";

    // Cache for fast lookup
    private Dictionary<string, ItemDefinition> itemLookup;
    private HashSet<string> loggedMissingItems = new HashSet<string>(); // Pour eviter le spam de logs

    /// <summary>
    /// Get item by ID (fast lookup via cache) - ROBUST VERSION
    /// </summary>
    public ItemDefinition GetItem(string itemId)
    {
        // Initialize cache if needed
        if (itemLookup == null)
        {
            InitializeLookupCache();
        }

        if (string.IsNullOrEmpty(itemId))
        {
            return null; // Pas de log pour eviter le spam
        }

        // Try to get item from cache
        if (itemLookup.TryGetValue(itemId, out ItemDefinition item))
        {
            return item;
        }

        // NOUVEAU : Fallback intelligent - cherche par nom similaire
        if (enableFallbackSearch)
        {
            item = FindItemByNameFallback(itemId);
            if (item != null)
            {
                return item;
            }
        }

        // Log une seule fois par item manquant
        if (logMissingItems && !loggedMissingItems.Contains(itemId))
        {
            Logger.LogWarning($"ItemRegistry: Item '{itemId}' not found in registry", Logger.LogCategory.InventoryLog);
            loggedMissingItems.Add(itemId);
        }

        return null;
    }

    /// <summary>
    /// Cherche un item par nom si l'ID exact n'est pas trouve
    /// </summary>
    private ItemDefinition FindItemByNameFallback(string itemId)
    {
        foreach (var item in AllItems?.Where(i => i != null) ?? Enumerable.Empty<ItemDefinition>())
        {
            if (MatchesItemName(item, itemId))
            {
                return item;
            }
        }
        return null;
    }

    /// <summary>
    /// Matching flexible pour les noms d'items
    /// </summary>
    private bool MatchesItemName(ItemDefinition item, string searchName)
    {
        if (item?.ItemID == null) return false;

        string itemName = item.ItemID.ToLower().Replace(" ", "_");
        string itemDisplayName = item.ItemName?.ToLower().Replace(" ", "_") ?? "";
        string search = searchName.ToLower().Replace(" ", "_");

        return itemName == search ||
               itemDisplayName == search ||
               itemName.Contains(search) ||
               search.Contains(itemName);
    }

    /// <summary>
    /// Check if an item exists in registry - SAFE VERSION
    /// </summary>
    public bool HasItem(string itemId)
    {
        return GetItem(itemId) != null;
    }

    /// <summary>
    /// Get all items of a specific type
    /// </summary>
    public List<ItemDefinition> GetItemsByType(ItemType itemType)
    {
        return AllItems?.Where(item => item != null && item.Type == itemType).ToList() ?? new List<ItemDefinition>();
    }

    /// <summary>
    /// Get all items of a specific rarity
    /// </summary>
    public List<ItemDefinition> GetItemsByRarity(int rarityTier)
    {
        return AllItems?.Where(item => item != null && item.RarityTier == rarityTier).ToList() ?? new List<ItemDefinition>();
    }

    /// <summary>
    /// Get all equipment items for a specific slot
    /// </summary>
    public List<ItemDefinition> GetEquipmentBySlot(EquipmentType slotType)
    {
        return AllItems?
            .Where(item => item != null && item.EquipmentSlot == slotType)
            .ToList() ?? new List<ItemDefinition>();
    }

    /// <summary>
    /// Get all stackable items
    /// </summary>
    public List<ItemDefinition> GetStackableItems()
    {
        return AllItems?.Where(item => item != null && item.IsStackable).ToList() ?? new List<ItemDefinition>();
    }

    /// <summary>
    /// Initialize the lookup cache for fast access - ROBUST VERSION
    /// </summary>
    private void InitializeLookupCache()
    {
        itemLookup = new Dictionary<string, ItemDefinition>();

        if (AllItems == null) return;

        foreach (var item in AllItems.Where(i => i != null))
        {
            if (!string.IsNullOrEmpty(item.ItemID))
            {
                if (!itemLookup.ContainsKey(item.ItemID))
                {
                    itemLookup[item.ItemID] = item;
                }
                else
                {
                    Logger.LogError($"ItemRegistry: Duplicate ItemID '{item.ItemID}' found! Check your ItemDefinitions.", Logger.LogCategory.InventoryLog);
                }
            }
        }

        Logger.LogInfo($"ItemRegistry: Cache initialized with {itemLookup.Count} items", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Clean null references automatically - SAFE VERSION
    /// </summary>
    public int CleanNullReferences()
    {
        if (AllItems == null) return 0;

        int removedCount = 0;

        // Clean null items
        for (int i = AllItems.Count - 1; i >= 0; i--)
        {
            if (AllItems[i] == null)
            {
                AllItems.RemoveAt(i);
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            RefreshCache();
        }

        return removedCount;
    }

    /// <summary>
    /// Force refresh the lookup cache (call this if you modify items at runtime)
    /// </summary>
    public void RefreshCache()
    {
        itemLookup = null;
        loggedMissingItems.Clear(); // Reset les logs pour permettre de nouveaux warnings
        InitializeLookupCache();
    }

    /// <summary>
    /// Validate all items in the registry and update status - ROBUST VERSION
    /// </summary>
    [ContextMenu("Validate Registry")]
    public void ValidateRegistry()
    {
        var issues = new List<string>();
        var itemIds = new HashSet<string>();

        // Auto-clean first
        int cleanedCount = CleanNullReferences();
        if (cleanedCount > 0)
        {
            issues.Add($"Auto-cleaned {cleanedCount} null references");
        }

        // Check for null items
        var nullCount = AllItems?.Count(item => item == null) ?? 0;
        if (nullCount > 0)
        {
            issues.Add($"{nullCount} null item(s) in registry");
        }

        // Validate each item
        foreach (var item in AllItems?.Where(i => i != null) ?? Enumerable.Empty<ItemDefinition>())
        {
            // Check if item definition itself is valid
            if (!item.IsValid())
            {
                issues.Add($"Item '{item.name}' failed validation");
            }

            // Check for duplicate IDs
            if (!string.IsNullOrEmpty(item.ItemID))
            {
                if (itemIds.Contains(item.ItemID))
                {
                    issues.Add($"Duplicate ItemID: '{item.ItemID}'");
                }
                else
                {
                    itemIds.Add(item.ItemID);
                }
            }
            else
            {
                issues.Add($"Item '{item.name}' has empty ItemID");
            }

            // Check for missing icon
            if (item.ItemIcon == null)
            {
                issues.Add($"Item '{item.ItemID}' missing icon");
            }

            // Check price consistency
            if (item.BasePrice <= 0 && item.Type != ItemType.Quest)
            {
                issues.Add($"Item '{item.ItemID}' has invalid price: {item.BasePrice}");
            }
        }

        // Update validation status
        if (issues.Count == 0)
        {
            ValidationStatus = $"✅ Registry is valid! ({AllItems?.Count ?? 0} items)";
        }
        else
        {
            ValidationStatus = $"⚠️ Issues found:\n• " + string.Join("\n• ", issues);
        }

        Logger.LogInfo($"ItemRegistry: Validation complete. {issues.Count} issue(s) found.", Logger.LogCategory.InventoryLog);

        // Refresh cache after validation
        RefreshCache();
    }

    /// <summary>
    /// Get a summary of item types for debugging
    /// </summary>
    private string GetItemTypesSummary()
    {
        var typeCounts = AllItems?
            .Where(item => item != null)
            .GroupBy(item => item.Type)
            .ToDictionary(g => g.Key, g => g.Count()) ?? new Dictionary<ItemType, int>();

        return string.Join(", ", typeCounts.Select(kvp => $"{kvp.Key}({kvp.Value})"));
    }

    /// <summary>
    /// Get debug info about the registry
    /// </summary>
    public string GetDebugInfo()
    {
        var validItems = AllItems?.Count(i => i != null) ?? 0;
        var totalItems = AllItems?.Count ?? 0;

        return $"ItemRegistry: {validItems}/{totalItems} valid items loaded\n" +
               $"Cache: {(itemLookup?.Count ?? 0)} items\n" +
               $"Types: {GetItemTypesSummary()}";
    }

    /// <summary>
    /// Get all valid items (filtered for null references)
    /// </summary>
    public List<ItemDefinition> GetAllValidItems()
    {
        return AllItems?.Where(i => i != null).ToList() ?? new List<ItemDefinition>();
    }

    /// <summary>
    /// Runtime initialization - auto-cleans and validates
    /// </summary>
    void OnEnable()
    {
        // Auto-clean silencieusement au demarrage
        CleanNullReferences();
        RefreshCache();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-validate when something changes in the editor
        if (AllItems != null && AllItems.Count > 0)
        {
            // Don't auto-validate too often to avoid performance issues
            UnityEditor.EditorApplication.delayCall += () => ValidateRegistry();
        }
    }
#endif
}