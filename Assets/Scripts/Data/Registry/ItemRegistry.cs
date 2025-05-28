// Purpose: Central registry for all game items, provides fast lookup and validation
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

    [Header("Debug Info")]
    [Tooltip("Shows validation errors if any")]
    [TextArea(3, 6)]
    public string ValidationStatus = "Click 'Validate Registry' to check for issues";

    // Cache for fast lookup - sera rempli automatiquement
    private Dictionary<string, ItemDefinition> itemLookup;

    /// <summary>
    /// Get item by ID (fast lookup via cache)
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
            Logger.LogError("ItemRegistry: GetItem called with null/empty itemId", Logger.LogCategory.InventoryLog);
            return null;
        }

        // Try to get item from cache
        if (itemLookup.TryGetValue(itemId, out ItemDefinition item))
        {
            return item;
        }

        Logger.LogWarning($"ItemRegistry: Item '{itemId}' not found in registry", Logger.LogCategory.InventoryLog);
        return null;
    }

    /// <summary>
    /// Check if an item exists in registry
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
        return AllItems.Where(item => item != null && item.Type == itemType).ToList();
    }

    /// <summary>
    /// Get all items of a specific rarity
    /// </summary>
    public List<ItemDefinition> GetItemsByRarity(int rarityTier)
    {
        return AllItems.Where(item => item != null && item.RarityTier == rarityTier).ToList();
    }

    /// <summary>
    /// Get all equipment items for a specific slot
    /// </summary>
    public List<ItemDefinition> GetEquipmentBySlot(EquipmentType slotType)
    {
        return AllItems
            .Where(item => item != null && item.EquipmentSlot == slotType)
            .ToList();
    }

    /// <summary>
    /// Get all stackable items
    /// </summary>
    public List<ItemDefinition> GetStackableItems()
    {
        return AllItems.Where(item => item != null && item.IsStackable).ToList();
    }

    /// <summary>
    /// Initialize the lookup cache for fast access
    /// </summary>
    private void InitializeLookupCache()
    {
        itemLookup = new Dictionary<string, ItemDefinition>();

        foreach (var item in AllItems)
        {
            if (item != null && !string.IsNullOrEmpty(item.ItemID))
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
    /// Force refresh the lookup cache (call this if you modify items at runtime)
    /// </summary>
    public void RefreshCache()
    {
        itemLookup = null;
        InitializeLookupCache();
    }

    /// <summary>
    /// Validate all items in the registry and update status
    /// </summary>
    [ContextMenu("Validate Registry")]
    public void ValidateRegistry()
    {
        var issues = new List<string>();
        var itemIds = new HashSet<string>();

        // Check for null items
        var nullCount = AllItems.Count(item => item == null);
        if (nullCount > 0)
        {
            issues.Add($"{nullCount} null item(s) in registry");
        }

        // Validate each item
        foreach (var item in AllItems)
        {
            if (item == null) continue;

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
            ValidationStatus = $"✅ Registry validation passed!\n" +
                             $"Found {AllItems.Count(i => i != null)} valid item(s).\n" +
                             $"Types: {GetItemTypesSummary()}";
        }
        else
        {
            ValidationStatus = $"❌ Registry validation failed ({issues.Count} issue(s)):\n\n" +
                             string.Join("\n", issues.Take(10)); // Limit to first 10 issues

            if (issues.Count > 10)
            {
                ValidationStatus += $"\n... and {issues.Count - 10} more issue(s)";
            }
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
        var typeCounts = AllItems
            .Where(item => item != null)
            .GroupBy(item => item.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        return string.Join(", ", typeCounts.Select(kvp => $"{kvp.Key}({kvp.Value})"));
    }

    /// <summary>
    /// Get debug info about the registry
    /// </summary>
    public string GetDebugInfo()
    {
        var validItems = AllItems.Count(i => i != null);
        var totalItems = AllItems.Count;

        return $"ItemRegistry: {validItems}/{totalItems} valid items loaded\n" +
               $"Cache: {(itemLookup?.Count ?? 0)} items\n" +
               $"Types: {GetItemTypesSummary()}";
    }

    /// <summary>
    /// Create some example items for testing (call this from a test script)
    /// </summary>
    [ContextMenu("Log Example Items For Testing")]
    public void LogExampleItems()
    {
        Debug.Log("=== Example Items for Testing ===");
        Debug.Log("Create these ItemDefinitions in your project:");
        Debug.Log("• wood (Material, stackable x99)");
        Debug.Log("• iron_ore (Material, stackable x99)");
        Debug.Log("• iron_sword (Equipment/Weapon, not stackable)");
        Debug.Log("• leather_backpack (Equipment/Backpack, +10 slots)");
        Debug.Log("• health_potion (Consumable, stackable x20)");
        Debug.Log("• gold_coin (Currency, stackable x999)");
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