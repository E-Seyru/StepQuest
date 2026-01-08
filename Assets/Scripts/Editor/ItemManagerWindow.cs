// Purpose: Tool to easily create and manage items in ItemRegistry
// Filepath: Assets/Scripts/Editor/ItemManagerWindow.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ItemManagerWindow : EditorWindow
{
    [MenuItem("StepQuest/World/Item Manager")]
    public static void ShowWindow()
    {
        ItemManagerWindow window = GetWindow<ItemManagerWindow>();
        window.titleContent = new GUIContent("Item Manager");
        window.Show();
    }

    // Data
    private ItemRegistry itemRegistry;

    // UI State
    private Vector2 scrollPosition;
    private string searchFilter = "";
    private ItemType filterType = ItemType.Material; // Default filter
    private bool showAllTypes = true;

    // Creation Dialog State
    private bool showCreateItemDialog = false;

    private string newItemName = "";
    private string newItemDescription = "";
    private ItemType newItemType = ItemType.Material;
    private int newItemPrice = 1;
    private int newItemRarity = 1;
    private bool newItemStackable = true;
    private int newItemMaxStack = 99;
    private Sprite newItemIcon = null;
    private Color newItemColor = Color.white;

    // Equipment specific fields
    private EquipmentType newEquipmentSlot = EquipmentType.Weapon;
    private int newInventorySlots = 0;

    // Category field
    private CategoryDefinition newItemCategory = null;

    // Rarity stats fields for creation
    private bool[] enabledRarityTiers = new bool[5]; // Index 0 = tier 1, etc.
    private List<ItemStat>[] rarityStats = new List<ItemStat>[5];

    // Expanded items for inline editing
    private HashSet<ItemDefinition> expandedItems = new HashSet<ItemDefinition>();

    void OnEnable()
    {
        LoadRegistry();
        InitializeRarityStats();
    }

    private void InitializeRarityStats()
    {
        for (int i = 0; i < 5; i++)
        {
            if (rarityStats[i] == null)
                rarityStats[i] = new List<ItemStat>();
        }
        // Default to Common enabled
        enabledRarityTiers[0] = true;
    }

    void OnGUI()
    {
        DrawHeader();

        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawItemsTab();
        EditorGUILayout.EndScrollView();

        // Handle creation dialog
        if (showCreateItemDialog)
            DrawCreateItemDialog();
    }

    #region UI Drawing
    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");

        GUILayout.Label("Item Manager", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        // Registry selection
        itemRegistry = (ItemRegistry)EditorGUILayout.ObjectField("Item Registry", itemRegistry, typeof(ItemRegistry), false);

        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            LoadRegistry();
        }

        if (GUILayout.Button("Validate", GUILayout.Width(60)))
        {
            ValidateRegistry();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Search and filters
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        showAllTypes = EditorGUILayout.Toggle("Show All Types", showAllTypes, GUILayout.Width(120));

        if (!showAllTypes)
        {
            EditorGUILayout.LabelField("Filter:", GUILayout.Width(50));
            filterType = (ItemType)EditorGUILayout.EnumPopup(filterType);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawItemsTab()
    {
        if (itemRegistry == null)
        {
            EditorGUILayout.HelpBox("Select an ItemRegistry to manage items.", MessageType.Info);
            return;
        }

        // Create New Item button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create New Item", GUILayout.Width(150)))
        {
            showCreateItemDialog = true;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        var filteredItems = GetFilteredItems();

        if (filteredItems.Count == 0)
        {
            EditorGUILayout.HelpBox("No items found matching current filters.", MessageType.Info);
            return;
        }

        // Items stats summary
        EditorGUILayout.LabelField($"Items Found: {filteredItems.Count}", EditorStyles.boldLabel);
        DrawItemTypesSummary(filteredItems);
        EditorGUILayout.Space();

        // Draw items
        foreach (var item in filteredItems)
        {
            DrawItemEntry(item);
        }
    }

    private void DrawItemTypesSummary(List<ItemDefinition> items)
    {
        var typeCounts = items.GroupBy(i => i.Type).ToDictionary(g => g.Key, g => g.Count());

        EditorGUILayout.BeginHorizontal();
        foreach (var kvp in typeCounts)
        {
            EditorGUILayout.LabelField($"{kvp.Key}: {kvp.Value}", EditorStyles.miniLabel, GUILayout.Width(80));
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawItemEntry(ItemDefinition item)
    {
        EditorGUILayout.BeginVertical("box");

        // Item header
        EditorGUILayout.BeginHorizontal();

        // Icon preview
        if (item.ItemIcon != null && item.ItemIcon.texture != null)
        {
            Rect iconRect = EditorGUILayout.GetControlRect(GUILayout.Width(32), GUILayout.Height(32));
            EditorGUI.DrawPreviewTexture(iconRect, item.ItemIcon.texture);
        }
        else
        {
            EditorGUILayout.LabelField("[No Icon]", GUILayout.Width(32));
        }

        EditorGUILayout.BeginVertical();

        // Name and type
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(item.GetDisplayName(), EditorStyles.boldLabel, GUILayout.Width(150));

        // Type with color
        var oldColor = GUI.color;
        GUI.color = GetTypeColor(item.Type);
        EditorGUILayout.LabelField($"[{item.Type}]", EditorStyles.miniLabel, GUILayout.Width(80));
        GUI.color = oldColor;

        // Category badge
        if (item.Category != null)
        {
            GUI.color = item.Category.CategoryColor;
            EditorGUILayout.LabelField($"[{item.Category.GetDisplayName()}]", EditorStyles.miniLabel, GUILayout.Width(80));
            GUI.color = oldColor;
        }

        // Show available rarities instead of single rarity
        DrawRarityBadges(item);

        GUILayout.FlexibleSpace();

        // Expand/Collapse button for rarity stats
        bool isExpanded = expandedItems.Contains(item);
        string expandLabel = isExpanded ? "▼" : "►";
        if (GUILayout.Button(expandLabel, GUILayout.Width(25)))
        {
            if (isExpanded)
                expandedItems.Remove(item);
            else
                expandedItems.Add(item);
        }

        if (GUILayout.Button("Edit", GUILayout.Width(40)))
        {
            Selection.activeObject = item;
            EditorGUIUtility.PingObject(item);
        }

        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            RemoveItemFromRegistry(item);
        }

        EditorGUILayout.EndHorizontal();

        // ID and price
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"ID: {item.ItemID}", EditorStyles.miniLabel, GUILayout.Width(150));
        EditorGUILayout.LabelField($"Price: {item.BasePrice}", EditorStyles.miniLabel, GUILayout.Width(80));

        if (item.IsStackable)
        {
            EditorGUILayout.LabelField($"Stack: {item.MaxStackSize}", EditorStyles.miniLabel, GUILayout.Width(80));
        }

        if (item.IsEquipment())
        {
            EditorGUILayout.LabelField($"Slot: {item.EquipmentSlot}", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        // Description
        if (!string.IsNullOrEmpty(item.Description))
        {
            EditorGUILayout.LabelField(item.Description, EditorStyles.wordWrappedMiniLabel);
        }

        // Expanded rarity stats section
        if (isExpanded)
        {
            DrawItemRarityStatsSection(item);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawRarityBadges(ItemDefinition item)
    {
        var oldColor = GUI.color;

        if (item.HasRarityStats())
        {
            var tiers = item.GetAvailableRarityTiers();
            foreach (int tier in tiers)
            {
                var rarityStats = item.GetStatsForRarity(tier);
                if (rarityStats != null)
                {
                    GUI.color = rarityStats.GetRarityColor();
                    string shortName = GetRarityShortName(tier);
                    EditorGUILayout.LabelField(shortName, EditorStyles.miniLabel, GUILayout.Width(20));
                }
            }
        }
        else
        {
            // Fallback to base rarity
            GUI.color = item.GetRarityColor();
            EditorGUILayout.LabelField($"{item.GetRarityText()}", EditorStyles.miniLabel, GUILayout.Width(80));
        }

        GUI.color = oldColor;
    }

    private string GetRarityShortName(int tier)
    {
        return tier switch
        {
            1 => "C",
            2 => "U",
            3 => "R",
            4 => "E",
            5 => "L",
            _ => "?"
        };
    }

    private void DrawItemRarityStatsSection(ItemDefinition item)
    {
        EditorGUI.indentLevel++;
        EditorGUILayout.BeginVertical("helpBox");

        EditorGUILayout.LabelField("Rarity Stats", EditorStyles.boldLabel);

        if (!item.HasRarityStats())
        {
            EditorGUILayout.HelpBox("No rarity stats defined. Use the Inspector to add rarity tiers.", MessageType.Info);

            if (GUILayout.Button("Add Rarity Stats", GUILayout.Width(120)))
            {
                // Add a default common rarity
                if (item.RarityStats == null)
                    item.RarityStats = new List<ItemRarityStats>();

                item.RarityStats.Add(new ItemRarityStats(1));
                EditorUtility.SetDirty(item);
            }
        }
        else
        {
            var tiers = item.GetAvailableRarityTiers();

            foreach (int tier in tiers)
            {
                var rarityStats = item.GetStatsForRarity(tier);
                if (rarityStats == null) continue;

                var oldColor = GUI.color;
                GUI.color = rarityStats.GetRarityColor();

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField($"[{rarityStats.GetRarityDisplayName()}]", EditorStyles.boldLabel, GUILayout.Width(100));

                GUI.color = oldColor;

                // Stats count
                int statCount = rarityStats.Stats?.Count ?? 0;
                EditorGUILayout.LabelField($"{statCount} stats", EditorStyles.miniLabel, GUILayout.Width(60));

                // Ability
                if (rarityStats.UnlockedAbility != null)
                {
                    EditorGUILayout.LabelField($"Ability: {rarityStats.UnlockedAbility.AbilityName}", EditorStyles.miniLabel);
                }

                GUILayout.FlexibleSpace();

                // Remove rarity button
                GUI.color = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    if (EditorUtility.DisplayDialog("Remove Rarity",
                        $"Remove {rarityStats.GetRarityDisplayName()} tier from this item?",
                        "Remove", "Cancel"))
                    {
                        item.RarityStats.Remove(rarityStats);
                        EditorUtility.SetDirty(item);
                    }
                }
                GUI.color = oldColor;

                EditorGUILayout.EndHorizontal();

                // Draw stats
                if (rarityStats.Stats != null && rarityStats.Stats.Count > 0)
                {
                    EditorGUI.indentLevel++;
                    foreach (var stat in rarityStats.Stats)
                    {
                        EditorGUILayout.LabelField($"• {stat.GetDisplayString()}", EditorStyles.miniLabel);
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            // Add new rarity tier
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Add Tier:", GUILayout.Width(60));

            for (int t = 1; t <= 5; t++)
            {
                bool exists = tiers.Contains(t);
                GUI.enabled = !exists;

                var color = new ItemRarityStats(t).GetRarityColor();
                GUI.color = exists ? Color.gray : color;

                if (GUILayout.Button(GetRarityShortName(t), GUILayout.Width(25)))
                {
                    item.RarityStats.Add(new ItemRarityStats(t));
                    EditorUtility.SetDirty(item);
                }
            }

            GUI.enabled = true;
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        // Base unlocked ability
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Base Ability:", GUILayout.Width(80));

        var newBaseAbility = (AbilityDefinition)EditorGUILayout.ObjectField(
            item.BaseUnlockedAbility, typeof(AbilityDefinition), false);

        if (newBaseAbility != item.BaseUnlockedAbility)
        {
            item.BaseUnlockedAbility = newBaseAbility;
            EditorUtility.SetDirty(item);
        }
        EditorGUILayout.EndHorizontal();

        // Category
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Category:", GUILayout.Width(80));

        var newCategory = (CategoryDefinition)EditorGUILayout.ObjectField(
            item.Category, typeof(CategoryDefinition), false);

        if (newCategory != item.Category)
        {
            item.Category = newCategory;
            EditorUtility.SetDirty(item);
        }
        EditorGUILayout.EndHorizontal();

        // Inventory Behavior section
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Inventory Behavior", EditorStyles.boldLabel);

        // Item Type
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Item Type:", GUILayout.Width(80));
        var newType = (ItemType)EditorGUILayout.EnumPopup(item.Type);
        if (newType != item.Type)
        {
            item.Type = newType;
            EditorUtility.SetDirty(item);
        }
        EditorGUILayout.EndHorizontal();

        // Is Stackable
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Stackable:", GUILayout.Width(80));
        var newStackable = EditorGUILayout.Toggle(item.IsStackable);
        if (newStackable != item.IsStackable)
        {
            item.IsStackable = newStackable;
            EditorUtility.SetDirty(item);
        }
        EditorGUILayout.EndHorizontal();

        // Max Stack Size (only if stackable)
        if (item.IsStackable)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max Stack:", GUILayout.Width(80));
            var newMaxStack = EditorGUILayout.IntField(item.MaxStackSize, GUILayout.Width(60));
            if (newMaxStack != item.MaxStackSize)
            {
                item.MaxStackSize = newMaxStack;
                EditorUtility.SetDirty(item);
            }
            EditorGUILayout.EndHorizontal();
        }

        // Equipment section (only if Equipment type)
        if (item.Type == ItemType.Equipment)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Equipment Settings", EditorStyles.boldLabel);

            // Equipment Slot
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Slot:", GUILayout.Width(80));
            var newSlot = (EquipmentType)EditorGUILayout.EnumPopup(item.EquipmentSlot);
            if (newSlot != item.EquipmentSlot)
            {
                item.EquipmentSlot = newSlot;
                EditorUtility.SetDirty(item);
            }
            EditorGUILayout.EndHorizontal();

            // Inventory Slots (only for Backpack)
            if (item.EquipmentSlot == EquipmentType.Backpack)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Inv. Slots:", GUILayout.Width(80));
                var newInvSlots = EditorGUILayout.IntField(item.InventorySlots, GUILayout.Width(60));
                if (newInvSlots != item.InventorySlots)
                {
                    item.InventorySlots = newInvSlots;
                    EditorUtility.SetDirty(item);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndVertical();
        EditorGUI.indentLevel--;
    }

    private Color GetTypeColor(ItemType type)
    {
        return type switch
        {
            ItemType.Equipment => Color.cyan,
            ItemType.Material => Color.green,
            ItemType.Consumable => Color.magenta,
            ItemType.Currency => Color.yellow,
            ItemType.Quest => new Color(1f, 0.5f, 0f), // Orange
            ItemType.Usable => Color.blue,
            ItemType.Miscellaneous => Color.gray,
            _ => Color.white
        };
    }

    private void DrawCreateItemDialog()
    {
        // Create semi-transparent overlay
        var overlayRect = new Rect(0, 0, position.width, position.height);
        EditorGUI.DrawRect(overlayRect, new Color(0, 0, 0, 0.5f));

        // Center the dialog
        var dialogRect = new Rect(
            (position.width - 500) / 2,
            (position.height - 550) / 2,
            500, 550);

        GUILayout.BeginArea(dialogRect);

        // Use a window-style background
        EditorGUILayout.BeginVertical(GUI.skin.window);

        EditorGUILayout.LabelField("Create New Item", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Basic info
        EditorGUILayout.LabelField("Item Name:");
        newItemName = EditorGUILayout.TextField(newItemName);

        EditorGUILayout.LabelField("Description:");
        newItemDescription = EditorGUILayout.TextArea(newItemDescription, GUILayout.Height(60));

        EditorGUILayout.Space();

        // Type and properties
        EditorGUILayout.LabelField("Item Type:");
        newItemType = (ItemType)EditorGUILayout.EnumPopup(newItemType);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Base Price:", GUILayout.Width(80));
        newItemPrice = EditorGUILayout.IntField(newItemPrice, GUILayout.Width(80));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Rarity Tiers
        EditorGUILayout.LabelField("Available Rarity Tiers:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        string[] rarityNames = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
        Color[] rarityColors = {
            Color.gray,
            Color.green,
            Color.blue,
            new Color(0.6f, 0.0f, 1.0f),
            new Color(1.0f, 0.6f, 0.0f)
        };

        for (int i = 0; i < 5; i++)
        {
            var oldColor = GUI.color;
            GUI.color = enabledRarityTiers[i] ? rarityColors[i] : Color.gray;
            enabledRarityTiers[i] = GUILayout.Toggle(enabledRarityTiers[i], rarityNames[i], "Button", GUILayout.Width(80));
            GUI.color = oldColor;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Stacking
        newItemStackable = EditorGUILayout.Toggle("Is Stackable", newItemStackable);
        if (newItemStackable)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max Stack Size:", GUILayout.Width(100));
            newItemMaxStack = EditorGUILayout.IntField(newItemMaxStack, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        // Category
        EditorGUILayout.LabelField("Category (for crafting panels):");
        newItemCategory = (CategoryDefinition)EditorGUILayout.ObjectField(newItemCategory, typeof(CategoryDefinition), false);

        EditorGUILayout.Space();

        // Visual
        EditorGUILayout.LabelField("Visual:");
        newItemIcon = (Sprite)EditorGUILayout.ObjectField("Icon", newItemIcon, typeof(Sprite), false);
        newItemColor = EditorGUILayout.ColorField("Color", newItemColor);

        EditorGUILayout.Space();

        // Equipment specific (only show if Equipment type)
        if (newItemType == ItemType.Equipment)
        {
            EditorGUILayout.LabelField("Equipment Settings:", EditorStyles.boldLabel);
            newEquipmentSlot = (EquipmentType)EditorGUILayout.EnumPopup("Equipment Slot", newEquipmentSlot);

            if (newEquipmentSlot == EquipmentType.Backpack)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Inventory Slots:", GUILayout.Width(100));
                newInventorySlots = EditorGUILayout.IntField(newInventorySlots, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }

            // Force non-stackable for equipment
            if (newItemStackable)
            {
                newItemStackable = false;
                newItemMaxStack = 1;
            }
        }

        EditorGUILayout.Space();

        // Buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create"))
        {
            if (!string.IsNullOrEmpty(newItemName))
            {
                CreateNewItem();
                ResetCreateItemDialog();
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Input", "Item name cannot be empty.", "OK");
            }
        }

        if (GUILayout.Button("Cancel"))
        {
            ResetCreateItemDialog();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();

        // Handle escape key to close dialog
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            ResetCreateItemDialog();
            Event.current.Use();
        }
    }
    #endregion

    #region Creation Logic
    private void CreateNewItem()
    {
        // Generate ItemID from name
        string itemID = GenerateIDFromName(newItemName);

        // Create the ScriptableObject
        ItemDefinition newItem = CreateInstance<ItemDefinition>();
        newItem.ItemID = itemID;
        newItem.ItemName = newItemName;
        newItem.Description = newItemDescription;
        newItem.Type = newItemType;
        newItem.BasePrice = newItemPrice;
        newItem.IsStackable = newItemStackable;
        newItem.MaxStackSize = newItemStackable ? newItemMaxStack : 1;
        newItem.ItemIcon = newItemIcon;
        newItem.ItemColor = newItemColor;
        newItem.Category = newItemCategory;

        // Add rarity stats for enabled tiers
        newItem.RarityStats = new List<ItemRarityStats>();
        for (int i = 0; i < 5; i++)
        {
            if (enabledRarityTiers[i])
            {
                newItem.RarityStats.Add(new ItemRarityStats(i + 1));
            }
        }

        // Set base rarity tier to lowest enabled tier (for backwards compatibility)
        newItem.RarityTier = 1;
        for (int i = 0; i < 5; i++)
        {
            if (enabledRarityTiers[i])
            {
                newItem.RarityTier = i + 1;
                break;
            }
        }

        // Equipment specific settings
        if (newItemType == ItemType.Equipment)
        {
            newItem.EquipmentSlot = newEquipmentSlot;
            newItem.IsStackable = false;
            newItem.MaxStackSize = 1;

            if (newEquipmentSlot == EquipmentType.Backpack)
            {
                newItem.InventorySlots = newInventorySlots;
            }
        }
        else
        {
            newItem.EquipmentSlot = EquipmentType.None;
            newItem.InventorySlots = 0;
        }

        // Determine save path and ensure folder exists
        string folder = GetFolderForItemType(newItemType);
        string fullFolderPath = $"Assets/ScriptableObjects/{folder}";

        // Create nested folders if they don't exist
        EnsureFolderExists(fullFolderPath);

        string assetPath = $"{fullFolderPath}/{newItemName}.asset";
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        AssetDatabase.CreateAsset(newItem, assetPath);
        AssetDatabase.SaveAssets();

        // Add to ItemRegistry
        if (itemRegistry != null)
        {
            itemRegistry.AllItems.Add(newItem);
            EditorUtility.SetDirty(itemRegistry);
            AssetDatabase.SaveAssets();
        }

        // Select the new item
        Selection.activeObject = newItem;
        EditorGUIUtility.PingObject(newItem);

        Logger.LogInfo($"Created new item: {newItemName} (ID: {itemID}) in {assetPath}", Logger.LogCategory.EditorLog);
        LoadRegistry(); // Refresh
    }

    private string GetFolderForItemType(ItemType type)
    {
        return type switch
        {
            ItemType.Material => "Ressources/Materials",
            ItemType.Equipment => "Ressources/Equipment",
            ItemType.Consumable => "Ressources/Consumables",
            ItemType.Currency => "Ressources/Currency",
            ItemType.Quest => "Ressources/Quest",
            ItemType.Usable => "Ressources/Usables",
            ItemType.Miscellaneous => "Ressources/Miscellaneous",
            _ => "Ressources/Items"
        };
    }

    private void ResetCreateItemDialog()
    {
        showCreateItemDialog = false;
        newItemName = "";
        newItemDescription = "";
        newItemType = ItemType.Material;
        newItemPrice = 1;
        newItemRarity = 1;
        newItemStackable = true;
        newItemMaxStack = 99;
        newItemIcon = null;
        newItemColor = Color.white;
        newEquipmentSlot = EquipmentType.Weapon;
        newInventorySlots = 0;
        newItemCategory = null;

        // Reset rarity tiers (default to Common enabled)
        for (int i = 0; i < 5; i++)
        {
            enabledRarityTiers[i] = (i == 0); // Only Common enabled by default
            rarityStats[i] = new List<ItemStat>();
        }
    }

    private string GenerateIDFromName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";

        return name.ToLower()
                  .Replace(" ", "_")
                  .Replace("'", "")
                  .Replace("-", "_")
                  .Replace("(", "")
                  .Replace(")", "");
    }

    /// <summary>
    /// Ensures that a folder path exists, creating nested folders as needed
    /// </summary>
    private void EnsureFolderExists(string fullPath)
    {
        // Split the path and build it step by step
        string[] pathParts = fullPath.Split('/');
        string currentPath = pathParts[0]; // Start with "Assets"

        for (int i = 1; i < pathParts.Length; i++)
        {
            string nextPath = currentPath + "/" + pathParts[i];

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, pathParts[i]);
                Logger.LogInfo($"Created folder: {nextPath}", Logger.LogCategory.EditorLog);
            }

            currentPath = nextPath;
        }
    }
    #endregion

    #region Utility Methods
    private void LoadRegistry()
    {
        if (itemRegistry == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemRegistry");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                itemRegistry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(path);
            }
        }
    }

    private void ValidateRegistry()
    {
        if (itemRegistry != null)
        {
            itemRegistry.ValidateRegistry();
            Logger.LogInfo("ItemRegistry validation triggered", Logger.LogCategory.EditorLog);
        }
    }

    private List<ItemDefinition> GetFilteredItems()
    {
        if (itemRegistry == null) return new List<ItemDefinition>();

        var items = itemRegistry.AllItems.Where(i => i != null);

        // Filter by type
        if (!showAllTypes)
        {
            items = items.Where(i => i.Type == filterType);
        }

        // Filter by search
        if (!string.IsNullOrEmpty(searchFilter))
        {
            string searchLower = searchFilter.ToLower();
            items = items.Where(i =>
                (i.GetDisplayName()?.ToLower().Contains(searchLower) ?? false) ||
                (i.ItemID?.ToLower().Contains(searchLower) ?? false) ||
                (i.Description?.ToLower().Contains(searchLower) ?? false));
        }

        return items.OrderBy(i => i.Type).ThenBy(i => i.GetDisplayName()).ToList();
    }

    private void RemoveItemFromRegistry(ItemDefinition item)
    {
        if (itemRegistry != null && itemRegistry.AllItems.Contains(item))
        {
            bool confirm = EditorUtility.DisplayDialog(
                "Remove Item",
                $"Remove '{item.GetDisplayName()}' from ItemRegistry?\n\n(This will NOT delete the asset file)",
                "Remove", "Cancel");

            if (confirm)
            {
                itemRegistry.AllItems.Remove(item);
                EditorUtility.SetDirty(itemRegistry);
                AssetDatabase.SaveAssets();
                Logger.LogInfo($"Removed item '{item.GetDisplayName()}' from ItemRegistry", Logger.LogCategory.EditorLog);
            }
        }
    }
    #endregion
}
#endif