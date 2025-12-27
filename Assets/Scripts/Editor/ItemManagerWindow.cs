// Purpose: Tool to easily create and manage items in ItemRegistry
// Filepath: Assets/Scripts/Editor/ItemManagerWindow.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ItemManagerWindow : EditorWindow
{
    [MenuItem("StepQuest/Item Manager")]
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

    void OnEnable()
    {
        LoadRegistry();
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

        // Rarity
        GUI.color = item.GetRarityColor();
        EditorGUILayout.LabelField($"{item.GetRarityText()}", EditorStyles.miniLabel, GUILayout.Width(80));
        GUI.color = oldColor;

        GUILayout.FlexibleSpace();

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

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
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
            (position.width - 450) / 2,
            (position.height - 500) / 2,
            450, 500);

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

        EditorGUILayout.LabelField("Rarity:", GUILayout.Width(50));
        newItemRarity = EditorGUILayout.IntSlider(newItemRarity, 1, 5);
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
        newItem.RarityTier = newItemRarity;
        newItem.IsStackable = newItemStackable;
        newItem.MaxStackSize = newItemStackable ? newItemMaxStack : 1;
        newItem.ItemIcon = newItemIcon;
        newItem.ItemColor = newItemColor;

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