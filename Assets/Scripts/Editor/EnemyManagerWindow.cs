// Purpose: Editor window to create and manage enemies
// Filepath: Assets/Scripts/Editor/EnemyManagerWindow.cs

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class EnemyManagerWindow : EditorWindow
{
    [MenuItem("WalkAndRPG/Combat/Enemy Manager")]
    public static void ShowWindow()
    {
        EnemyManagerWindow window = GetWindow<EnemyManagerWindow>();
        window.titleContent = new GUIContent("Enemy Manager");
        window.minSize = new Vector2(550, 450);
        window.Show();
    }

    // Data
    private List<EnemyDefinition> allEnemies = new List<EnemyDefinition>();
    private AbilityRegistry abilityRegistry;

    // UI State
    private Vector2 scrollPosition;
    private string searchFilter = "";
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Enemies", "Quick Create", "Validation" };

    // Filter State
    private bool filterHasLoot = false;
    private bool filterHasAbilities = false;
    private int filterMinLevel = 0;
    private int filterMaxLevel = 100;

    // Creation Dialog State
    private bool showCreateEnemyDialog = false;
    private Vector2 dialogScrollPosition;

    // New enemy fields
    private string newEnemyName = "";
    private string newEnemyDescription = "";
    private Sprite newEnemySprite = null;
    private Sprite newEnemyAvatar = null;
    private Color newEnemyColor = Color.white;
    private int newEnemyLevel = 1;
    private float newEnemyMaxHealth = 100f;
    private int newEnemyXP = 10;
    private List<AbilityDefinition> newEnemyAbilities = new List<AbilityDefinition>();

    void OnEnable()
    {
        RefreshEnemyList();
        LoadAbilityRegistry();
    }

    void OnGUI()
    {
        DrawHeader();

        EditorGUILayout.Space();

        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        switch (selectedTab)
        {
            case 0:
                DrawEnemiesTab();
                break;
            case 1:
                DrawQuickCreateTab();
                break;
            case 2:
                DrawValidationTab();
                break;
        }

        EditorGUILayout.EndScrollView();

        if (showCreateEnemyDialog)
            DrawCreateEnemyDialog();
    }

    #region Header
    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");

        GUILayout.Label("Enemy Manager", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            RefreshEnemyList();
        }

        EditorGUILayout.LabelField($"Total Enemies: {allEnemies.Count}", GUILayout.Width(120));

        EditorGUILayout.EndHorizontal();

        // Search and filters (only for Enemies tab)
        if (selectedTab == 0)
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            filterHasLoot = EditorGUILayout.Toggle("Has Loot", filterHasLoot, GUILayout.Width(80));
            filterHasAbilities = EditorGUILayout.Toggle("Has Abilities", filterHasAbilities, GUILayout.Width(100));

            EditorGUILayout.LabelField("Level:", GUILayout.Width(40));
            filterMinLevel = EditorGUILayout.IntField(filterMinLevel, GUILayout.Width(40));
            EditorGUILayout.LabelField("-", GUILayout.Width(10));
            filterMaxLevel = EditorGUILayout.IntField(filterMaxLevel, GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }
    #endregion

    #region Enemies Tab
    private void DrawEnemiesTab()
    {
        // Create New Enemy button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create New Enemy", GUILayout.Width(150)))
        {
            showCreateEnemyDialog = true;
            ResetCreateEnemyDialog();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        var filteredEnemies = GetFilteredEnemies();

        if (filteredEnemies.Count == 0)
        {
            EditorGUILayout.HelpBox("No enemies found matching current filters.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Showing: {filteredEnemies.Count} enemies", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        foreach (var enemy in filteredEnemies)
        {
            DrawEnemyEntry(enemy);
        }
    }

    private void DrawEnemyEntry(EnemyDefinition enemy)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();

        // Avatar/Sprite preview
        Sprite displaySprite = enemy.Avatar != null ? enemy.Avatar : enemy.EnemySprite;
        if (displaySprite != null)
        {
            Rect iconRect = EditorGUILayout.GetControlRect(GUILayout.Width(40), GUILayout.Height(40));
            DrawSprite(iconRect, displaySprite);
        }
        else
        {
            EditorGUILayout.LabelField("[No Image]", GUILayout.Width(50), GUILayout.Height(40));
        }

        EditorGUILayout.BeginVertical();

        // Name and level
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(enemy.GetDisplayName(), EditorStyles.boldLabel, GUILayout.Width(150));

        var oldColor = GUI.color;
        GUI.color = GetLevelColor(enemy.Level);
        EditorGUILayout.LabelField($"Lv.{enemy.Level}", EditorStyles.miniLabel, GUILayout.Width(40));
        GUI.color = oldColor;

        EditorGUILayout.LabelField($"HP: {enemy.MaxHealth}", EditorStyles.miniLabel, GUILayout.Width(70));
        EditorGUILayout.LabelField($"XP: {enemy.ExperienceReward}", EditorStyles.miniLabel, GUILayout.Width(60));

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Edit", GUILayout.Width(40)))
        {
            Selection.activeObject = enemy;
            EditorGUIUtility.PingObject(enemy);
        }

        if (GUILayout.Button("Delete", GUILayout.Width(50)))
        {
            DeleteEnemy(enemy);
        }

        EditorGUILayout.EndHorizontal();

        // Abilities row
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Abilities:", EditorStyles.miniLabel, GUILayout.Width(55));
        if (enemy.Abilities != null && enemy.Abilities.Count > 0)
        {
            string abilityNames = string.Join(", ", enemy.Abilities.Where(a => a != null).Select(a => a.GetDisplayName()));
            EditorGUILayout.LabelField(abilityNames, EditorStyles.miniLabel);
        }
        else
        {
            var oldCol = GUI.color;
            GUI.color = Color.yellow;
            EditorGUILayout.LabelField("(No abilities!)", EditorStyles.miniLabel);
            GUI.color = oldCol;
        }
        EditorGUILayout.EndHorizontal();

        // Loot row
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Loot:", EditorStyles.miniLabel, GUILayout.Width(55));
        if (enemy.LootTable != null && enemy.LootTable.Count > 0)
        {
            int lootCount = enemy.LootTable.Count(l => l != null && l.Item != null);
            EditorGUILayout.LabelField($"{lootCount} item(s)", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.LabelField("(No loot)", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }
    #endregion

    #region Quick Create Tab
    private void DrawQuickCreateTab()
    {
        EditorGUILayout.LabelField("Quick Create Templates", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Click a button to create a pre-configured enemy with sensible defaults.", MessageType.Info);
        EditorGUILayout.Space();

        // Weak enemies
        EditorGUILayout.LabelField("Weak Enemies (Lv 1-5)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Slime\n(30 HP, 5 XP)", GUILayout.Height(40)))
        {
            QuickCreateEnemy("Slime", 1, 30, 5, new Color(0.2f, 0.8f, 0.2f));
        }
        if (GUILayout.Button("Rat\n(20 HP, 3 XP)", GUILayout.Height(40)))
        {
            QuickCreateEnemy("Rat", 1, 20, 3, new Color(0.5f, 0.4f, 0.3f));
        }
        if (GUILayout.Button("Bat\n(25 HP, 4 XP)", GUILayout.Height(40)))
        {
            QuickCreateEnemy("Bat", 2, 25, 4, new Color(0.3f, 0.3f, 0.3f));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Medium enemies
        EditorGUILayout.LabelField("Medium Enemies (Lv 5-15)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Goblin\n(50 HP, 15 XP)", GUILayout.Height(40)))
        {
            QuickCreateEnemy("Goblin", 5, 50, 15, new Color(0.2f, 0.6f, 0.2f));
        }
        if (GUILayout.Button("Wolf\n(70 HP, 20 XP)", GUILayout.Height(40)))
        {
            QuickCreateEnemy("Wolf", 8, 70, 20, new Color(0.5f, 0.5f, 0.5f));
        }
        if (GUILayout.Button("Skeleton\n(60 HP, 18 XP)", GUILayout.Height(40)))
        {
            QuickCreateEnemy("Skeleton", 7, 60, 18, new Color(0.9f, 0.9f, 0.8f));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Strong enemies
        EditorGUILayout.LabelField("Strong Enemies (Lv 15-30)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Orc\n(150 HP, 50 XP)", GUILayout.Height(40)))
        {
            QuickCreateEnemy("Orc", 15, 150, 50, new Color(0.4f, 0.5f, 0.3f));
        }
        if (GUILayout.Button("Troll\n(250 HP, 80 XP)", GUILayout.Height(40)))
        {
            QuickCreateEnemy("Troll", 20, 250, 80, new Color(0.3f, 0.4f, 0.3f));
        }
        if (GUILayout.Button("Dark Knight\n(200 HP, 70 XP)", GUILayout.Height(40)))
        {
            QuickCreateEnemy("Dark Knight", 18, 200, 70, new Color(0.2f, 0.2f, 0.3f));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Boss enemies
        EditorGUILayout.LabelField("Boss Enemies", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Dragon\n(500 HP, 200 XP)", GUILayout.Height(40)))
        {
            QuickCreateEnemy("Dragon", 30, 500, 200, new Color(0.8f, 0.2f, 0.2f));
        }
        if (GUILayout.Button("Demon Lord\n(750 HP, 350 XP)", GUILayout.Height(40)))
        {
            QuickCreateEnemy("Demon Lord", 40, 750, 350, new Color(0.6f, 0.1f, 0.1f));
        }
        EditorGUILayout.EndHorizontal();
    }

    private void QuickCreateEnemy(string name, int level, float maxHealth, int xp, Color color)
    {
        var enemy = CreateInstance<EnemyDefinition>();
        enemy.EnemyName = name;
        enemy.EnemyID = name.ToLower().Replace(" ", "_");
        enemy.Level = level;
        enemy.MaxHealth = maxHealth;
        enemy.ExperienceReward = xp;
        enemy.EnemyColor = color;
        enemy.VictoryTitle = $"{name} Defeated!";
        enemy.VictoryDescription = $"You defeated the {name.ToLower()}!";

        SaveEnemyAsset(enemy);
    }
    #endregion

    #region Validation Tab
    private void DrawValidationTab()
    {
        EditorGUILayout.LabelField("Enemy Validation", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("Run Validation", GUILayout.Height(30)))
        {
            RefreshEnemyList();
        }

        EditorGUILayout.Space();

        // Statistics
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Enemies: {allEnemies.Count}");

        int withAbilities = allEnemies.Count(e => e.Abilities != null && e.Abilities.Any(a => a != null));
        int withLoot = allEnemies.Count(e => e.LootTable != null && e.LootTable.Any(l => l != null && l.Item != null));
        int withSprite = allEnemies.Count(e => e.EnemySprite != null || e.Avatar != null);

        EditorGUILayout.LabelField($"With Abilities: {withAbilities}/{allEnemies.Count}");
        EditorGUILayout.LabelField($"With Loot: {withLoot}/{allEnemies.Count}");
        EditorGUILayout.LabelField($"With Sprite: {withSprite}/{allEnemies.Count}");

        EditorGUILayout.Space();

        // Issues
        EditorGUILayout.LabelField("Issues", EditorStyles.boldLabel);

        var issues = FindValidationIssues();
        if (issues.Count == 0)
        {
            EditorGUILayout.HelpBox("No issues found!", MessageType.Info);
        }
        else
        {
            foreach (var issue in issues)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(issue.message, EditorStyles.wordWrappedLabel);
                if (issue.enemy != null && GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    Selection.activeObject = issue.enemy;
                    EditorGUIUtility.PingObject(issue.enemy);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();

        // Level distribution
        EditorGUILayout.LabelField("Level Distribution", EditorStyles.boldLabel);
        var levelGroups = allEnemies.GroupBy(e => GetLevelBracket(e.Level)).OrderBy(g => g.Key);
        foreach (var group in levelGroups)
        {
            EditorGUILayout.LabelField($"  {group.Key}: {group.Count()} enemies");
        }
    }

    private struct ValidationIssue
    {
        public EnemyDefinition enemy;
        public string message;
    }

    private List<ValidationIssue> FindValidationIssues()
    {
        var issues = new List<ValidationIssue>();

        foreach (var enemy in allEnemies)
        {
            if (enemy == null) continue;

            if (string.IsNullOrEmpty(enemy.EnemyID))
            {
                issues.Add(new ValidationIssue { enemy = enemy, message = $"{enemy.name}: Missing EnemyID" });
            }

            if (string.IsNullOrEmpty(enemy.EnemyName))
            {
                issues.Add(new ValidationIssue { enemy = enemy, message = $"{enemy.name}: Missing EnemyName" });
            }

            if (enemy.MaxHealth <= 0)
            {
                issues.Add(new ValidationIssue { enemy = enemy, message = $"{enemy.GetDisplayName()}: Invalid MaxHealth ({enemy.MaxHealth})" });
            }

            if (enemy.Abilities == null || !enemy.Abilities.Any(a => a != null))
            {
                issues.Add(new ValidationIssue { enemy = enemy, message = $"{enemy.GetDisplayName()}: No abilities assigned" });
            }

            if (enemy.EnemySprite == null && enemy.Avatar == null)
            {
                issues.Add(new ValidationIssue { enemy = enemy, message = $"{enemy.GetDisplayName()}: No sprite or avatar" });
            }

            // Check for null ability references
            if (enemy.Abilities != null)
            {
                int nullCount = enemy.Abilities.Count(a => a == null);
                if (nullCount > 0)
                {
                    issues.Add(new ValidationIssue { enemy = enemy, message = $"{enemy.GetDisplayName()}: {nullCount} null ability reference(s)" });
                }
            }

            // Check loot table
            if (enemy.LootTable != null)
            {
                int nullLoot = enemy.LootTable.Count(l => l != null && l.Item == null);
                if (nullLoot > 0)
                {
                    issues.Add(new ValidationIssue { enemy = enemy, message = $"{enemy.GetDisplayName()}: {nullLoot} loot entry(ies) with null item" });
                }
            }
        }

        return issues;
    }

    private string GetLevelBracket(int level)
    {
        if (level <= 5) return "Lv 1-5 (Weak)";
        if (level <= 15) return "Lv 6-15 (Medium)";
        if (level <= 30) return "Lv 16-30 (Strong)";
        return "Lv 31+ (Boss)";
    }
    #endregion

    #region Create Enemy Dialog
    private void DrawCreateEnemyDialog()
    {
        var overlayRect = new Rect(0, 0, position.width, position.height);
        EditorGUI.DrawRect(overlayRect, new Color(0, 0, 0, 0.5f));

        float dialogWidth = 450;
        float dialogHeight = 500;
        var dialogRect = new Rect(
            (position.width - dialogWidth) / 2,
            (position.height - dialogHeight) / 2,
            dialogWidth, dialogHeight);

        GUILayout.BeginArea(dialogRect);
        EditorGUILayout.BeginVertical(GUI.skin.window);

        EditorGUILayout.LabelField("Create New Enemy", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        dialogScrollPosition = EditorGUILayout.BeginScrollView(dialogScrollPosition, GUILayout.Height(dialogHeight - 80));

        // Basic info
        EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
        newEnemyName = EditorGUILayout.TextField("Name", newEnemyName);
        newEnemyDescription = EditorGUILayout.TextField("Description", newEnemyDescription);

        EditorGUILayout.Space();

        // Visual
        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        newEnemySprite = (Sprite)EditorGUILayout.ObjectField("Combat Sprite", newEnemySprite, typeof(Sprite), false);
        newEnemyAvatar = (Sprite)EditorGUILayout.ObjectField("Avatar", newEnemyAvatar, typeof(Sprite), false);
        newEnemyColor = EditorGUILayout.ColorField("Color", newEnemyColor);

        EditorGUILayout.Space();

        // Stats
        EditorGUILayout.LabelField("Combat Stats", EditorStyles.boldLabel);
        newEnemyLevel = EditorGUILayout.IntSlider("Level", newEnemyLevel, 1, 100);
        newEnemyMaxHealth = EditorGUILayout.FloatField("Max Health", newEnemyMaxHealth);
        newEnemyXP = EditorGUILayout.IntField("XP Reward", newEnemyXP);

        EditorGUILayout.Space();

        // Abilities (simple list for now)
        EditorGUILayout.LabelField("Abilities", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Add abilities after creation in the Inspector.", MessageType.Info);

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create"))
        {
            if (!string.IsNullOrEmpty(newEnemyName))
            {
                CreateNewEnemy();
                showCreateEnemyDialog = false;
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Input", "Enemy must have a name.", "OK");
            }
        }

        if (GUILayout.Button("Cancel"))
        {
            showCreateEnemyDialog = false;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            showCreateEnemyDialog = false;
            Event.current.Use();
        }
    }

    private void CreateNewEnemy()
    {
        var enemy = CreateInstance<EnemyDefinition>();
        enemy.EnemyName = newEnemyName;
        enemy.EnemyID = newEnemyName.ToLower().Replace(" ", "_").Replace("'", "");
        enemy.Description = newEnemyDescription;
        enemy.EnemySprite = newEnemySprite;
        enemy.Avatar = newEnemyAvatar;
        enemy.EnemyColor = newEnemyColor;
        enemy.Level = newEnemyLevel;
        enemy.MaxHealth = newEnemyMaxHealth;
        enemy.ExperienceReward = newEnemyXP;
        enemy.VictoryTitle = $"{newEnemyName} Defeated!";
        enemy.VictoryDescription = $"You defeated the {newEnemyName.ToLower()}!";

        SaveEnemyAsset(enemy);
        ResetCreateEnemyDialog();
    }

    private void ResetCreateEnemyDialog()
    {
        newEnemyName = "";
        newEnemyDescription = "";
        newEnemySprite = null;
        newEnemyAvatar = null;
        newEnemyColor = Color.white;
        newEnemyLevel = 1;
        newEnemyMaxHealth = 100f;
        newEnemyXP = 10;
        newEnemyAbilities = new List<AbilityDefinition>();
    }
    #endregion

    #region Utility Methods
    private void RefreshEnemyList()
    {
        allEnemies.Clear();

        string[] guids = AssetDatabase.FindAssets("t:EnemyDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (enemy != null)
            {
                allEnemies.Add(enemy);
            }
        }

        allEnemies = allEnemies.OrderBy(e => e.Level).ThenBy(e => e.GetDisplayName()).ToList();
    }

    private void LoadAbilityRegistry()
    {
        string[] guids = AssetDatabase.FindAssets("t:AbilityRegistry");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            abilityRegistry = AssetDatabase.LoadAssetAtPath<AbilityRegistry>(path);
        }
    }

    private List<EnemyDefinition> GetFilteredEnemies()
    {
        var filtered = allEnemies.Where(e => e != null);

        // Search filter
        if (!string.IsNullOrEmpty(searchFilter))
        {
            filtered = filtered.Where(e =>
                e.GetDisplayName().ToLower().Contains(searchFilter.ToLower()) ||
                e.EnemyID.ToLower().Contains(searchFilter.ToLower()) ||
                (e.Description != null && e.Description.ToLower().Contains(searchFilter.ToLower())));
        }

        // Has loot filter
        if (filterHasLoot)
        {
            filtered = filtered.Where(e => e.LootTable != null && e.LootTable.Any(l => l != null && l.Item != null));
        }

        // Has abilities filter
        if (filterHasAbilities)
        {
            filtered = filtered.Where(e => e.Abilities != null && e.Abilities.Any(a => a != null));
        }

        // Level filter
        filtered = filtered.Where(e => e.Level >= filterMinLevel && e.Level <= filterMaxLevel);

        return filtered.ToList();
    }

    private void SaveEnemyAsset(EnemyDefinition enemy)
    {
        string folder = "Assets/ScriptableObjects/Enemies";
        EnsureFolderExists(folder);

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{enemy.EnemyName}.asset");
        AssetDatabase.CreateAsset(enemy, assetPath);
        AssetDatabase.SaveAssets();

        allEnemies.Add(enemy);

        Selection.activeObject = enemy;
        EditorGUIUtility.PingObject(enemy);

        Debug.Log($"Created enemy: {enemy.EnemyName} at {assetPath}");
    }

    private void DeleteEnemy(EnemyDefinition enemy)
    {
        if (enemy == null) return;

        bool confirm = EditorUtility.DisplayDialog(
            "Delete Enemy",
            $"Delete '{enemy.GetDisplayName()}'?\n\nThis will permanently delete the asset file.",
            "Delete", "Cancel");

        if (confirm)
        {
            allEnemies.Remove(enemy);

            string assetPath = AssetDatabase.GetAssetPath(enemy);
            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
                Debug.Log($"Deleted enemy '{enemy.GetDisplayName()}' at {assetPath}");
            }

            AssetDatabase.SaveAssets();
        }
    }

    private Color GetLevelColor(int level)
    {
        if (level <= 5) return new Color(0.6f, 0.6f, 0.6f); // Gray - weak
        if (level <= 15) return new Color(0.3f, 0.8f, 0.3f); // Green - medium
        if (level <= 30) return new Color(0.8f, 0.6f, 0.2f); // Orange - strong
        return new Color(0.8f, 0.3f, 0.3f); // Red - boss
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
}
#endif
