// Purpose: Editor window to create and manage crafting categories
// Filepath: Assets/Scripts/Editor/CategoryManagerWindow.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CategoryManagerWindow : EditorWindow
{
    [MenuItem("WalkAndRPG/Content/Category Manager")]
    public static void ShowWindow()
    {
        CategoryManagerWindow window = GetWindow<CategoryManagerWindow>();
        window.titleContent = new GUIContent("Category Manager");
        window.minSize = new Vector2(500, 400);
        window.Show();
    }

    // Data
    private CategoryRegistry categoryRegistry;

    // UI State
    private Vector2 scrollPosition;
    private string searchFilter = "";
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Categories", "Quick Create", "Suggestions" };

    // Creation Dialog State
    private bool showCreateDialog = false;
    private Vector2 dialogScrollPosition;

    // New category fields
    private string newCategoryId = "";
    private string newCategoryDisplayName = "";
    private Sprite newCategoryIcon = null;
    private Color newCategoryColor = Color.white;
    private int newCategorySortOrder = 0;

    // Predefined category suggestions organized by crafting profession
    private static readonly Dictionary<string, List<CategorySuggestion>> CategorySuggestions = new Dictionary<string, List<CategorySuggestion>>
    {
        // ===== FORGING / BLACKSMITHING =====
        { "Forging", new List<CategorySuggestion>
            {
                new CategorySuggestion("bars", "Lingots", "Raw metal bars (iron, steel, gold, mithril)", new Color(0.7f, 0.7f, 0.7f)),
                new CategorySuggestion("weapons_swords", "Epees", "One-handed and two-handed swords", new Color(0.8f, 0.8f, 0.9f)),
                new CategorySuggestion("weapons_axes", "Haches", "Battle axes and throwing axes", new Color(0.6f, 0.5f, 0.4f)),
                new CategorySuggestion("weapons_maces", "Masses", "Maces, hammers, and blunt weapons", new Color(0.5f, 0.5f, 0.5f)),
                new CategorySuggestion("weapons_daggers", "Dagues", "Daggers and knives", new Color(0.4f, 0.4f, 0.5f)),
                new CategorySuggestion("weapons_spears", "Lances", "Spears and polearms", new Color(0.6f, 0.6f, 0.7f)),
                new CategorySuggestion("armor_helmets", "Casques", "Head protection", new Color(0.7f, 0.6f, 0.5f)),
                new CategorySuggestion("armor_chestplates", "Plastrons", "Chest armor", new Color(0.6f, 0.6f, 0.7f)),
                new CategorySuggestion("armor_gauntlets", "Gantelets", "Hand and arm protection", new Color(0.5f, 0.5f, 0.6f)),
                new CategorySuggestion("armor_boots", "Bottes", "Metal boots and greaves", new Color(0.4f, 0.4f, 0.5f)),
                new CategorySuggestion("armor_shields", "Boucliers", "Shields of all types", new Color(0.6f, 0.5f, 0.4f)),
                new CategorySuggestion("tools", "Outils", "Pickaxes, hammers, tongs", new Color(0.5f, 0.4f, 0.3f)),
                new CategorySuggestion("nails_fittings", "Clous & Ferrures", "Nails, hinges, fittings", new Color(0.4f, 0.4f, 0.4f)),
                new CategorySuggestion("horseshoes", "Fers a Cheval", "Horseshoes and horse gear", new Color(0.6f, 0.5f, 0.4f)),
            }
        },

        // ===== COOKING =====
        { "Cooking", new List<CategorySuggestion>
            {
                new CategorySuggestion("meals_meat", "Viandes", "Cooked meat dishes", new Color(0.8f, 0.4f, 0.3f)),
                new CategorySuggestion("meals_fish", "Poissons", "Cooked fish dishes", new Color(0.4f, 0.6f, 0.8f)),
                new CategorySuggestion("meals_vegetarian", "Vegetarien", "Vegetable-based meals", new Color(0.4f, 0.7f, 0.3f)),
                new CategorySuggestion("soups_stews", "Soupes & Ragouts", "Soups, stews, broths", new Color(0.7f, 0.5f, 0.3f)),
                new CategorySuggestion("bread_pastry", "Pains & Patisseries", "Bread, cakes, pastries", new Color(0.9f, 0.8f, 0.5f)),
                new CategorySuggestion("desserts", "Desserts", "Sweet treats and desserts", new Color(0.9f, 0.6f, 0.7f)),
                new CategorySuggestion("drinks", "Boissons", "Drinks, juices, teas", new Color(0.5f, 0.7f, 0.9f)),
                new CategorySuggestion("preserves", "Conserves", "Preserved foods, jams, pickles", new Color(0.8f, 0.6f, 0.4f)),
                new CategorySuggestion("spices_seasonings", "Epices", "Spice blends and seasonings", new Color(0.9f, 0.7f, 0.2f)),
                new CategorySuggestion("rations", "Rations", "Travel food and rations", new Color(0.6f, 0.5f, 0.4f)),
            }
        },

        // ===== ALCHEMY / POTIONS =====
        { "Alchemy", new List<CategorySuggestion>
            {
                new CategorySuggestion("potions_health", "Potions de Vie", "Health restoration potions", new Color(0.9f, 0.2f, 0.2f)),
                new CategorySuggestion("potions_mana", "Potions de Mana", "Mana restoration potions", new Color(0.2f, 0.4f, 0.9f)),
                new CategorySuggestion("potions_stamina", "Potions d'Endurance", "Stamina restoration potions", new Color(0.2f, 0.8f, 0.3f)),
                new CategorySuggestion("potions_buff", "Potions de Buff", "Stat-boosting potions", new Color(0.9f, 0.7f, 0.2f)),
                new CategorySuggestion("potions_resistance", "Potions de Resistance", "Elemental resistance potions", new Color(0.6f, 0.4f, 0.8f)),
                new CategorySuggestion("poisons", "Poisons", "Weapon coatings and poisons", new Color(0.4f, 0.8f, 0.2f)),
                new CategorySuggestion("antidotes", "Antidotes", "Cure poisons and diseases", new Color(0.8f, 0.8f, 0.3f)),
                new CategorySuggestion("elixirs", "Elixirs", "Powerful long-duration effects", new Color(0.7f, 0.3f, 0.9f)),
                new CategorySuggestion("oils", "Huiles", "Weapon oils and enhancements", new Color(0.6f, 0.5f, 0.2f)),
                new CategorySuggestion("bombs_grenades", "Bombes", "Throwable explosives", new Color(0.9f, 0.5f, 0.2f)),
                new CategorySuggestion("reagents", "Reactifs", "Crafted alchemical reagents", new Color(0.5f, 0.6f, 0.7f)),
            }
        },

        // ===== LEATHERWORKING =====
        { "Leatherworking", new List<CategorySuggestion>
            {
                new CategorySuggestion("leather_processed", "Cuirs Traites", "Processed leather materials", new Color(0.6f, 0.4f, 0.2f)),
                new CategorySuggestion("armor_light", "Armure Legere", "Light leather armor pieces", new Color(0.7f, 0.5f, 0.3f)),
                new CategorySuggestion("gloves", "Gants", "Leather gloves", new Color(0.5f, 0.4f, 0.3f)),
                new CategorySuggestion("boots_leather", "Bottes Cuir", "Leather boots", new Color(0.4f, 0.3f, 0.2f)),
                new CategorySuggestion("belts", "Ceintures", "Belts and straps", new Color(0.5f, 0.3f, 0.2f)),
                new CategorySuggestion("bags", "Sacs", "Bags, pouches, backpacks", new Color(0.6f, 0.5f, 0.3f)),
                new CategorySuggestion("quivers", "Carquois", "Arrow quivers", new Color(0.5f, 0.4f, 0.2f)),
                new CategorySuggestion("saddles", "Selles", "Horse saddles and tack", new Color(0.7f, 0.5f, 0.2f)),
                new CategorySuggestion("sheaths", "Fourreaux", "Weapon sheaths and holsters", new Color(0.4f, 0.3f, 0.2f)),
            }
        },

        // ===== TAILORING / CLOTHCRAFT =====
        { "Tailoring", new List<CategorySuggestion>
            {
                new CategorySuggestion("cloth_processed", "Tissus", "Processed cloth materials", new Color(0.8f, 0.8f, 0.9f)),
                new CategorySuggestion("robes", "Robes", "Mage robes and gowns", new Color(0.5f, 0.4f, 0.8f)),
                new CategorySuggestion("cloaks", "Capes", "Cloaks and capes", new Color(0.4f, 0.3f, 0.6f)),
                new CategorySuggestion("hats", "Chapeaux", "Hats and hoods", new Color(0.6f, 0.5f, 0.7f)),
                new CategorySuggestion("shirts", "Chemises", "Shirts and tunics", new Color(0.7f, 0.7f, 0.8f)),
                new CategorySuggestion("pants", "Pantalons", "Pants and leggings", new Color(0.5f, 0.5f, 0.6f)),
                new CategorySuggestion("bandages", "Bandages", "Medical bandages", new Color(0.9f, 0.9f, 0.9f)),
                new CategorySuggestion("bags_cloth", "Sacs en Tissu", "Cloth bags and pouches", new Color(0.7f, 0.6f, 0.5f)),
            }
        },

        // ===== JEWELCRAFTING =====
        { "Jewelcrafting", new List<CategorySuggestion>
            {
                new CategorySuggestion("gems_cut", "Gemmes Taillees", "Cut and polished gems", new Color(0.8f, 0.3f, 0.8f)),
                new CategorySuggestion("rings", "Anneaux", "Finger rings", new Color(0.9f, 0.8f, 0.3f)),
                new CategorySuggestion("necklaces", "Colliers", "Necklaces and pendants", new Color(0.8f, 0.7f, 0.4f)),
                new CategorySuggestion("earrings", "Boucles d'Oreilles", "Earrings", new Color(0.7f, 0.6f, 0.8f)),
                new CategorySuggestion("bracelets", "Bracelets", "Bracelets and bangles", new Color(0.6f, 0.7f, 0.8f)),
                new CategorySuggestion("crowns", "Couronnes", "Crowns and tiaras", new Color(0.9f, 0.85f, 0.2f)),
                new CategorySuggestion("amulets", "Amulettes", "Magical amulets", new Color(0.5f, 0.3f, 0.7f)),
                new CategorySuggestion("trinkets", "Bibelots", "Misc trinkets and charms", new Color(0.6f, 0.5f, 0.6f)),
            }
        },

        // ===== WOODWORKING / CARPENTRY =====
        { "Woodworking", new List<CategorySuggestion>
            {
                new CategorySuggestion("planks", "Planches", "Processed wood planks", new Color(0.7f, 0.5f, 0.3f)),
                new CategorySuggestion("bows", "Arcs", "Bows and crossbows", new Color(0.6f, 0.5f, 0.3f)),
                new CategorySuggestion("staves", "Batons", "Magic staves and walking sticks", new Color(0.5f, 0.4f, 0.3f)),
                new CategorySuggestion("arrows", "Fleches", "Arrow shafts and arrows", new Color(0.6f, 0.4f, 0.2f)),
                new CategorySuggestion("shields_wood", "Boucliers Bois", "Wooden shields", new Color(0.7f, 0.5f, 0.2f)),
                new CategorySuggestion("handles", "Manches", "Tool and weapon handles", new Color(0.5f, 0.4f, 0.2f)),
                new CategorySuggestion("furniture", "Mobilier", "Furniture pieces", new Color(0.6f, 0.4f, 0.3f)),
                new CategorySuggestion("instruments", "Instruments", "Musical instruments", new Color(0.8f, 0.6f, 0.4f)),
                new CategorySuggestion("carts", "Chariots", "Carts and wagon parts", new Color(0.5f, 0.4f, 0.3f)),
            }
        },

        // ===== ENCHANTING =====
        { "Enchanting", new List<CategorySuggestion>
            {
                new CategorySuggestion("scrolls", "Parchemins", "Magic scrolls", new Color(0.9f, 0.9f, 0.7f)),
                new CategorySuggestion("runes", "Runes", "Enchantment runes", new Color(0.4f, 0.5f, 0.9f)),
                new CategorySuggestion("enchant_weapon", "Enchant. Armes", "Weapon enchantments", new Color(0.8f, 0.3f, 0.3f)),
                new CategorySuggestion("enchant_armor", "Enchant. Armures", "Armor enchantments", new Color(0.3f, 0.5f, 0.8f)),
                new CategorySuggestion("enchant_jewelry", "Enchant. Bijoux", "Jewelry enchantments", new Color(0.8f, 0.6f, 0.9f)),
                new CategorySuggestion("glyphs", "Glyphes", "Magical glyphs", new Color(0.5f, 0.4f, 0.7f)),
                new CategorySuggestion("wands", "Baguettes", "Magic wands", new Color(0.7f, 0.5f, 0.9f)),
            }
        },

        // ===== FISHING (crafting aspect) =====
        { "Fishing Crafts", new List<CategorySuggestion>
            {
                new CategorySuggestion("lures", "Appats", "Fishing lures and bait", new Color(0.5f, 0.7f, 0.4f)),
                new CategorySuggestion("rods", "Cannes", "Fishing rods", new Color(0.6f, 0.5f, 0.4f)),
                new CategorySuggestion("nets", "Filets", "Fishing nets", new Color(0.5f, 0.6f, 0.5f)),
                new CategorySuggestion("traps_fish", "Pieges", "Fish traps", new Color(0.4f, 0.5f, 0.4f)),
            }
        },

        // ===== CONSTRUCTION / MASONRY =====
        { "Construction", new List<CategorySuggestion>
            {
                new CategorySuggestion("bricks", "Briques", "Bricks and blocks", new Color(0.8f, 0.5f, 0.3f)),
                new CategorySuggestion("stone_processed", "Pierres Taillees", "Processed stone", new Color(0.6f, 0.6f, 0.6f)),
                new CategorySuggestion("foundations", "Fondations", "Building foundations", new Color(0.5f, 0.5f, 0.5f)),
                new CategorySuggestion("walls", "Murs", "Wall sections", new Color(0.6f, 0.5f, 0.4f)),
                new CategorySuggestion("roofing", "Toitures", "Roof materials", new Color(0.7f, 0.4f, 0.3f)),
                new CategorySuggestion("decorations", "Decorations", "Decorative elements", new Color(0.7f, 0.6f, 0.5f)),
            }
        },

        // ===== GENERAL / MISC =====
        { "General", new List<CategorySuggestion>
            {
                new CategorySuggestion("components", "Composants", "Crafting components", new Color(0.6f, 0.6f, 0.6f)),
                new CategorySuggestion("materials", "Materiaux", "Processed materials", new Color(0.5f, 0.5f, 0.5f)),
                new CategorySuggestion("containers", "Conteneurs", "Bottles, jars, boxes", new Color(0.7f, 0.6f, 0.5f)),
                new CategorySuggestion("rope_string", "Cordes & Ficelles", "Rope and string products", new Color(0.6f, 0.5f, 0.4f)),
                new CategorySuggestion("candles", "Bougies", "Candles and light sources", new Color(0.9f, 0.8f, 0.5f)),
                new CategorySuggestion("paper", "Papier", "Paper and parchment", new Color(0.9f, 0.9f, 0.8f)),
                new CategorySuggestion("dyes", "Teintures", "Dyes and pigments", new Color(0.7f, 0.3f, 0.5f)),
            }
        },
    };

    void OnEnable()
    {
        LoadRegistry();
    }

    void OnGUI()
    {
        DrawHeader();

        EditorGUILayout.Space();

        // Tab selection
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        switch (selectedTab)
        {
            case 0:
                DrawCategoriesTab();
                break;
            case 1:
                DrawQuickCreateTab();
                break;
            case 2:
                DrawSuggestionsTab();
                break;
        }

        EditorGUILayout.EndScrollView();

        // Handle creation dialog
        if (showCreateDialog)
            DrawCreateDialog();
    }

    #region Header
    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");

        GUILayout.Label("Category Manager", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        // Registry selection
        categoryRegistry = (CategoryRegistry)EditorGUILayout.ObjectField("Category Registry", categoryRegistry, typeof(CategoryRegistry), false);

        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            LoadRegistry();
        }

        EditorGUILayout.EndHorizontal();

        // Search filter (only for Categories tab)
        if (selectedTab == 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }
    #endregion

    #region Categories Tab
    private void DrawCategoriesTab()
    {
        if (categoryRegistry == null)
        {
            EditorGUILayout.HelpBox("Select a CategoryRegistry to manage categories.", MessageType.Info);

            if (GUILayout.Button("Create New Category Registry"))
            {
                CreateCategoryRegistry();
            }
            return;
        }

        // Create New Category button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create New Category", GUILayout.Width(160)))
        {
            showCreateDialog = true;
            ResetCreateDialog();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        var categories = GetFilteredCategories();

        if (categories.Count == 0)
        {
            EditorGUILayout.HelpBox("No categories found. Create some using the Quick Create or Suggestions tabs!", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Categories Found: {categories.Count}", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Draw categories
        foreach (var category in categories)
        {
            DrawCategoryEntry(category);
        }
    }

    private void DrawCategoryEntry(CategoryDefinition category)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();

        // Color indicator
        var oldColor = GUI.backgroundColor;
        GUI.backgroundColor = category.CategoryColor;
        EditorGUILayout.LabelField("", GUILayout.Width(5), GUILayout.Height(32));
        GUI.backgroundColor = oldColor;

        // Icon preview
        if (category.Icon != null)
        {
            Rect iconRect = EditorGUILayout.GetControlRect(GUILayout.Width(32), GUILayout.Height(32));
            DrawSprite(iconRect, category.Icon);
        }
        else
        {
            EditorGUILayout.LabelField("[No Icon]", GUILayout.Width(50), GUILayout.Height(32));
        }

        EditorGUILayout.BeginVertical();

        // Name and ID
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(category.GetDisplayName(), EditorStyles.boldLabel, GUILayout.Width(150));
        EditorGUILayout.LabelField($"ID: {category.CategoryID}", EditorStyles.miniLabel, GUILayout.Width(150));
        EditorGUILayout.LabelField($"Sort: {category.SortOrder}", EditorStyles.miniLabel, GUILayout.Width(60));
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Edit", GUILayout.Width(40)))
        {
            Selection.activeObject = category;
            EditorGUIUtility.PingObject(category);
        }

        if (GUILayout.Button("Delete", GUILayout.Width(50)))
        {
            DeleteCategory(category);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }
    #endregion

    #region Quick Create Tab
    private void DrawQuickCreateTab()
    {
        if (categoryRegistry == null)
        {
            EditorGUILayout.HelpBox("Select a CategoryRegistry first.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Quick Create Category", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Fill in the fields and click Create.", MessageType.Info);
        EditorGUILayout.Space();

        newCategoryId = EditorGUILayout.TextField("Category ID", newCategoryId);
        newCategoryDisplayName = EditorGUILayout.TextField("Display Name", newCategoryDisplayName);
        newCategoryIcon = (Sprite)EditorGUILayout.ObjectField("Icon", newCategoryIcon, typeof(Sprite), false);
        newCategoryColor = EditorGUILayout.ColorField("Color", newCategoryColor);
        newCategorySortOrder = EditorGUILayout.IntField("Sort Order", newCategorySortOrder);

        EditorGUILayout.Space();

        // Auto-generate ID from display name
        if (!string.IsNullOrEmpty(newCategoryDisplayName) && string.IsNullOrEmpty(newCategoryId))
        {
            string suggestedId = GenerateIDFromName(newCategoryDisplayName);
            EditorGUILayout.LabelField($"Suggested ID: {suggestedId}", EditorStyles.miniLabel);
            if (GUILayout.Button("Use Suggested ID", GUILayout.Width(120)))
            {
                newCategoryId = suggestedId;
            }
        }

        EditorGUILayout.Space();

        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newCategoryId));
        if (GUILayout.Button("Create Category", GUILayout.Height(30)))
        {
            CreateCategory(newCategoryId, newCategoryDisplayName, newCategoryIcon, newCategoryColor, newCategorySortOrder);
            ResetCreateDialog();
        }
        EditorGUI.EndDisabledGroup();
    }
    #endregion

    #region Suggestions Tab
    private void DrawSuggestionsTab()
    {
        if (categoryRegistry == null)
        {
            EditorGUILayout.HelpBox("Select a CategoryRegistry first.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Category Suggestions by Profession", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Click a category to create it. Already existing categories are greyed out.", MessageType.Info);
        EditorGUILayout.Space();

        foreach (var profession in CategorySuggestions)
        {
            DrawProfessionSuggestions(profession.Key, profession.Value);
        }
    }

    private void DrawProfessionSuggestions(string professionName, List<CategorySuggestion> suggestions)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField(professionName, EditorStyles.boldLabel);

        int columns = 3;
        int currentColumn = 0;

        EditorGUILayout.BeginHorizontal();

        foreach (var suggestion in suggestions)
        {
            bool exists = categoryRegistry.HasCategory(suggestion.Id);

            EditorGUI.BeginDisabledGroup(exists);

            var oldBgColor = GUI.backgroundColor;
            GUI.backgroundColor = exists ? Color.gray : suggestion.Color;

            string buttonText = exists ? $"{suggestion.DisplayName}\n(exists)" : $"{suggestion.DisplayName}";

            if (GUILayout.Button(buttonText, GUILayout.Height(40), GUILayout.Width(150)))
            {
                CreateCategory(suggestion.Id, suggestion.DisplayName, null, suggestion.Color, suggestions.IndexOf(suggestion));
            }

            GUI.backgroundColor = oldBgColor;
            EditorGUI.EndDisabledGroup();

            currentColumn++;
            if (currentColumn >= columns)
            {
                currentColumn = 0;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
            }
        }

        EditorGUILayout.EndHorizontal();

        // Add All button
        EditorGUILayout.Space();
        int existingCount = suggestions.Count(s => categoryRegistry.HasCategory(s.Id));
        int remainingCount = suggestions.Count - existingCount;

        if (remainingCount > 0)
        {
            if (GUILayout.Button($"Add All {remainingCount} Missing Categories", GUILayout.Height(25)))
            {
                int sortBase = categoryRegistry.AllCategories.Count > 0
                    ? categoryRegistry.AllCategories.Max(c => c.SortOrder) + 10
                    : 0;

                int created = 0;
                foreach (var suggestion in suggestions)
                {
                    if (!categoryRegistry.HasCategory(suggestion.Id))
                    {
                        CreateCategory(suggestion.Id, suggestion.DisplayName, null, suggestion.Color, sortBase + created);
                        created++;
                    }
                }

                Logger.LogInfo($"Created {created} categories for {professionName}", Logger.LogCategory.EditorLog);
            }
        }
        else
        {
            EditorGUILayout.LabelField("All categories from this profession already exist.", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }
    #endregion

    #region Create Dialog
    private void DrawCreateDialog()
    {
        // Create semi-transparent overlay
        var overlayRect = new Rect(0, 0, position.width, position.height);
        EditorGUI.DrawRect(overlayRect, new Color(0, 0, 0, 0.5f));

        // Center the dialog
        float dialogWidth = 400;
        float dialogHeight = 300;
        var dialogRect = new Rect(
            (position.width - dialogWidth) / 2,
            (position.height - dialogHeight) / 2,
            dialogWidth, dialogHeight);

        GUILayout.BeginArea(dialogRect);
        EditorGUILayout.BeginVertical(GUI.skin.window);

        EditorGUILayout.LabelField("Create New Category", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        dialogScrollPosition = EditorGUILayout.BeginScrollView(dialogScrollPosition, GUILayout.Height(dialogHeight - 80));

        newCategoryId = EditorGUILayout.TextField("Category ID", newCategoryId);
        newCategoryDisplayName = EditorGUILayout.TextField("Display Name", newCategoryDisplayName);
        newCategoryIcon = (Sprite)EditorGUILayout.ObjectField("Icon", newCategoryIcon, typeof(Sprite), false);
        newCategoryColor = EditorGUILayout.ColorField("Color", newCategoryColor);
        newCategorySortOrder = EditorGUILayout.IntField("Sort Order", newCategorySortOrder);

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create"))
        {
            if (!string.IsNullOrEmpty(newCategoryId))
            {
                CreateCategory(newCategoryId, newCategoryDisplayName, newCategoryIcon, newCategoryColor, newCategorySortOrder);
                showCreateDialog = false;
                ResetCreateDialog();
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Input", "Category must have an ID.", "OK");
            }
        }

        if (GUILayout.Button("Cancel"))
        {
            showCreateDialog = false;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();

        // Handle escape key
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            showCreateDialog = false;
            Event.current.Use();
        }
    }

    private void ResetCreateDialog()
    {
        newCategoryId = "";
        newCategoryDisplayName = "";
        newCategoryIcon = null;
        newCategoryColor = Color.white;
        newCategorySortOrder = categoryRegistry != null && categoryRegistry.AllCategories.Count > 0
            ? categoryRegistry.AllCategories.Max(c => c.SortOrder) + 1
            : 0;
    }
    #endregion

    #region Utility Methods
    private void LoadRegistry()
    {
        if (categoryRegistry == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:CategoryRegistry");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                categoryRegistry = AssetDatabase.LoadAssetAtPath<CategoryRegistry>(path);
            }
        }
    }

    private void CreateCategoryRegistry()
    {
        var registry = CreateInstance<CategoryRegistry>();

        string folder = "Assets/ScriptableObjects/Registries";
        EnsureFolderExists(folder);

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/CategoryRegistry.asset");
        AssetDatabase.CreateAsset(registry, assetPath);
        AssetDatabase.SaveAssets();

        categoryRegistry = registry;
        Selection.activeObject = registry;
        EditorGUIUtility.PingObject(registry);

        Logger.LogInfo($"Created CategoryRegistry at {assetPath}", Logger.LogCategory.EditorLog);
    }

    private void CreateCategory(string id, string displayName, Sprite icon, Color color, int sortOrder)
    {
        if (categoryRegistry.HasCategory(id))
        {
            EditorUtility.DisplayDialog("Duplicate ID", $"A category with ID '{id}' already exists.", "OK");
            return;
        }

        var category = CreateInstance<CategoryDefinition>();
        category.CategoryID = id;
        category.DisplayName = string.IsNullOrEmpty(displayName) ? id : displayName;
        category.Icon = icon;
        category.CategoryColor = color;
        category.SortOrder = sortOrder;

        string folder = "Assets/ScriptableObjects/Categories";
        EnsureFolderExists(folder);

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{category.DisplayName}.asset");
        AssetDatabase.CreateAsset(category, assetPath);

        // Add to registry using SerializedObject for proper undo support
        var serializedRegistry = new SerializedObject(categoryRegistry);
        var categoriesProperty = serializedRegistry.FindProperty("categories");
        categoriesProperty.InsertArrayElementAtIndex(categoriesProperty.arraySize);
        categoriesProperty.GetArrayElementAtIndex(categoriesProperty.arraySize - 1).objectReferenceValue = category;
        serializedRegistry.ApplyModifiedProperties();

        AssetDatabase.SaveAssets();

        Logger.LogInfo($"Created category: {category.DisplayName} (ID: {id}) at {assetPath}", Logger.LogCategory.EditorLog);
    }

    private void DeleteCategory(CategoryDefinition category)
    {
        if (category == null) return;

        bool confirm = EditorUtility.DisplayDialog(
            "Delete Category",
            $"Delete '{category.GetDisplayName()}'?\n\nThis will permanently delete the asset file.",
            "Delete", "Cancel");

        if (confirm)
        {
            // Remove from registry using SerializedObject
            var serializedRegistry = new SerializedObject(categoryRegistry);
            var categoriesProperty = serializedRegistry.FindProperty("categories");

            for (int i = 0; i < categoriesProperty.arraySize; i++)
            {
                if (categoriesProperty.GetArrayElementAtIndex(i).objectReferenceValue == category)
                {
                    // Set to null first (required for object references)
                    categoriesProperty.GetArrayElementAtIndex(i).objectReferenceValue = null;
                    // Then delete the element
                    categoriesProperty.DeleteArrayElementAtIndex(i);
                    break;
                }
            }

            serializedRegistry.ApplyModifiedProperties();

            // Delete the asset
            string assetPath = AssetDatabase.GetAssetPath(category);
            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
                Logger.LogInfo($"Deleted category '{category.GetDisplayName()}' at {assetPath}", Logger.LogCategory.EditorLog);
            }

            AssetDatabase.SaveAssets();
        }
    }

    private List<CategoryDefinition> GetFilteredCategories()
    {
        if (categoryRegistry == null) return new List<CategoryDefinition>();

        var categories = categoryRegistry.AllCategories.Where(c => c != null);

        // Filter by search
        if (!string.IsNullOrEmpty(searchFilter))
        {
            categories = categories.Where(c =>
                c.GetDisplayName().ToLower().Contains(searchFilter.ToLower()) ||
                c.CategoryID.ToLower().Contains(searchFilter.ToLower()));
        }

        return categories.OrderBy(c => c.SortOrder).ThenBy(c => c.GetDisplayName()).ToList();
    }

    private string GenerateIDFromName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";

        return name.ToLower()
                  .Replace(" ", "_")
                  .Replace("'", "")
                  .Replace("-", "_")
                  .Replace("(", "")
                  .Replace(")", "")
                  .Replace("&", "and")
                  .Replace("é", "e")
                  .Replace("è", "e")
                  .Replace("ê", "e")
                  .Replace("à", "a")
                  .Replace("â", "a")
                  .Replace("ô", "o")
                  .Replace("î", "i")
                  .Replace("û", "u")
                  .Replace("ç", "c");
    }

    private void EnsureFolderExists(string fullPath)
    {
        string[] pathParts = fullPath.Split('/');
        string currentPath = pathParts[0];

        for (int i = 1; i < pathParts.Length; i++)
        {
            string nextPath = currentPath + "/" + pathParts[i];

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, pathParts[i]);
            }

            currentPath = nextPath;
        }
    }

    private void DrawSprite(Rect rect, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return;

        Texture2D tex = sprite.texture;
        Rect spriteRect = sprite.textureRect;

        Rect texCoords = new Rect(
            spriteRect.x / tex.width,
            spriteRect.y / tex.height,
            spriteRect.width / tex.width,
            spriteRect.height / tex.height
        );

        GUI.DrawTextureWithTexCoords(rect, tex, texCoords);
    }
    #endregion

    // Helper class for category suggestions
    private class CategorySuggestion
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public Color Color { get; }

        public CategorySuggestion(string id, string displayName, string description, Color color)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Color = color;
        }
    }
}
#endif
