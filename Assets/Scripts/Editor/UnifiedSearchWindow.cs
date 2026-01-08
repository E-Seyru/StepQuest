// Purpose: Unified search window for searching across all game content types
// Filepath: Assets/Scripts/Editor/UnifiedSearchWindow.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unified search window that searches across all game content types:
/// Items, Abilities, Enemies, NPCs, Locations, Status Effects, Activities
/// </summary>
public class UnifiedSearchWindow : EditorWindow
{
    // Keyboard shortcut: Ctrl+Alt+F
    [MenuItem("StepQuest/Unified Search %&f")]
    public static void ShowWindow()
    {
        var window = GetWindow<UnifiedSearchWindow>();
        window.titleContent = new GUIContent("Unified Search");
        window.minSize = new Vector2(400, 500);
        window.Show();
        window.Focus();
        window.FocusSearchField();
    }

    /// <summary>
    /// Opens the search window with a pre-filled query
    /// </summary>
    public static void OpenWithQuery(string query)
    {
        var window = GetWindow<UnifiedSearchWindow>();
        window.titleContent = new GUIContent("Unified Search");
        window.minSize = new Vector2(400, 500);
        window.Show();
        window.Focus();

        if (!string.IsNullOrEmpty(query))
        {
            window.searchQuery = query;
            window.PerformSearch();
        }
        else
        {
            window.FocusSearchField();
        }
    }

    // Registries
    private ItemRegistry itemRegistry;
    private AbilityRegistry abilityRegistry;
    private StatusEffectRegistry statusEffectRegistry;
    private LocationRegistry locationRegistry;
    private NPCRegistry npcRegistry;
    private List<EnemyDefinition> allEnemies = new List<EnemyDefinition>();
    private List<ActivityDefinition> allActivities = new List<ActivityDefinition>();

    // UI State
    private string searchQuery = "";
    private Vector2 scrollPosition;
    private bool focusSearchField = false;

    // Search Results
    private List<SearchResult> searchResults = new List<SearchResult>();
    private bool hasSearched = false;

    // Recent Searches
    private const string RECENT_SEARCHES_KEY = "UnifiedSearch_RecentSearches";
    private const int MAX_RECENT_SEARCHES = 10;
    private List<string> recentSearches = new List<string>();
    private bool showRecentSearches = false;

    // Result type colors
    private readonly Dictionary<ContentType, Color> typeColors = new Dictionary<ContentType, Color>
    {
        { ContentType.Item, new Color(0.4f, 0.8f, 0.4f) },       // Green
        { ContentType.Ability, new Color(0.4f, 0.6f, 1.0f) },    // Blue
        { ContentType.Enemy, new Color(1.0f, 0.4f, 0.4f) },      // Red
        { ContentType.NPC, new Color(1.0f, 0.8f, 0.4f) },        // Gold
        { ContentType.Location, new Color(0.6f, 0.4f, 1.0f) },   // Purple
        { ContentType.StatusEffect, new Color(0.4f, 1.0f, 1.0f) }, // Cyan
        { ContentType.Activity, new Color(1.0f, 0.6f, 0.4f) }    // Orange
    };

    private enum ContentType
    {
        Item,
        Ability,
        Enemy,
        NPC,
        Location,
        StatusEffect,
        Activity
    }

    private class SearchResult
    {
        public ContentType Type;
        public string Name;
        public string ID;
        public string Description;
        public UnityEngine.Object Asset;
        public Sprite Icon;
    }

    void OnEnable()
    {
        LoadAllRegistries();
        LoadRecentSearches();
    }

    void OnGUI()
    {
        DrawSearchBar();
        EditorGUILayout.Space();

        if (!hasSearched && string.IsNullOrEmpty(searchQuery))
        {
            DrawRecentSearches();
            DrawQuickStats();
        }
        else
        {
            DrawSearchResults();
        }

        // Handle focus request
        if (focusSearchField)
        {
            EditorGUI.FocusTextInControl("SearchField");
            focusSearchField = false;
        }
    }

    public void FocusSearchField()
    {
        focusSearchField = true;
        Repaint();
    }

    #region UI Drawing

    private void DrawSearchBar()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search All Content", EditorStyles.boldLabel);

        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            LoadAllRegistries();
            if (!string.IsNullOrEmpty(searchQuery))
                PerformSearch();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);

        EditorGUILayout.BeginHorizontal();

        // Search field
        GUI.SetNextControlName("SearchField");
        string newQuery = EditorGUILayout.TextField(searchQuery, GUILayout.Height(22));

        if (newQuery != searchQuery)
        {
            searchQuery = newQuery;
            if (searchQuery.Length >= 2)
            {
                PerformSearch();
            }
            else if (string.IsNullOrEmpty(searchQuery))
            {
                searchResults.Clear();
                hasSearched = false;
            }
        }

        // Clear button
        if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(22)))
        {
            searchQuery = "";
            searchResults.Clear();
            hasSearched = false;
            FocusSearchField();
        }

        EditorGUILayout.EndHorizontal();

        // Search hint
        if (string.IsNullOrEmpty(searchQuery))
        {
            EditorGUILayout.LabelField("Type at least 2 characters to search...", EditorStyles.miniLabel);
        }
        else if (searchQuery.Length < 2)
        {
            EditorGUILayout.LabelField("Keep typing...", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.LabelField($"Found {searchResults.Count} result(s)", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();

        // Handle Enter key to save recent search
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            if (!string.IsNullOrEmpty(searchQuery) && searchQuery.Length >= 2)
            {
                AddRecentSearch(searchQuery);
            }
        }
    }

    private void DrawRecentSearches()
    {
        if (recentSearches.Count == 0) return;

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        showRecentSearches = EditorGUILayout.Foldout(showRecentSearches, "Recent Searches", true);

        if (showRecentSearches && GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            recentSearches.Clear();
            SaveRecentSearches();
        }
        EditorGUILayout.EndHorizontal();

        if (showRecentSearches)
        {
            foreach (var search in recentSearches)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(search, EditorStyles.linkLabel))
                {
                    searchQuery = search;
                    PerformSearch();
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawQuickStats()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Content Overview", EditorStyles.boldLabel);

        int itemCount = itemRegistry?.AllItems?.Count(i => i != null) ?? 0;
        int abilityCount = abilityRegistry?.AllAbilities?.Count(a => a != null) ?? 0;
        int enemyCount = allEnemies.Count;
        int npcCount = npcRegistry?.AllNPCs?.Count(n => n != null) ?? 0;
        int locationCount = locationRegistry?.AllLocations?.Count(l => l != null) ?? 0;
        int statusEffectCount = statusEffectRegistry?.AllEffects?.Count(s => s != null) ?? 0;
        int activityCount = allActivities.Count;

        EditorGUILayout.BeginHorizontal();
        DrawStatBadge("Items", itemCount, ContentType.Item);
        DrawStatBadge("Abilities", abilityCount, ContentType.Ability);
        DrawStatBadge("Enemies", enemyCount, ContentType.Enemy);
        DrawStatBadge("NPCs", npcCount, ContentType.NPC);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        DrawStatBadge("Locations", locationCount, ContentType.Location);
        DrawStatBadge("Status Effects", statusEffectCount, ContentType.StatusEffect);
        DrawStatBadge("Activities", activityCount, ContentType.Activity);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawStatBadge(string label, int count, ContentType type)
    {
        var oldColor = GUI.color;
        GUI.color = typeColors[type];
        EditorGUILayout.LabelField($"{label}: {count}", EditorStyles.miniLabel, GUILayout.Width(100));
        GUI.color = oldColor;
    }

    private void DrawSearchResults()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (searchResults.Count == 0)
        {
            EditorGUILayout.HelpBox($"No results found for \"{searchQuery}\"", MessageType.Info);
        }
        else
        {
            // Group results by type
            var groupedResults = searchResults.GroupBy(r => r.Type).OrderBy(g => g.Key);

            foreach (var group in groupedResults)
            {
                DrawResultGroup(group.Key, group.ToList());
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawResultGroup(ContentType type, List<SearchResult> results)
    {
        EditorGUILayout.BeginVertical("box");

        // Group header
        var oldColor = GUI.color;
        GUI.color = typeColors[type];
        EditorGUILayout.LabelField($"{type.ToString().ToUpper()} ({results.Count})", EditorStyles.boldLabel);
        GUI.color = oldColor;

        // Results
        foreach (var result in results)
        {
            DrawResultEntry(result);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    private void DrawResultEntry(SearchResult result)
    {
        EditorGUILayout.BeginHorizontal("helpBox");

        // Icon
        if (result.Icon != null && result.Icon.texture != null)
        {
            Rect iconRect = EditorGUILayout.GetControlRect(GUILayout.Width(24), GUILayout.Height(24));
            EditorGUI.DrawPreviewTexture(iconRect, result.Icon.texture);
        }
        else
        {
            // Color indicator instead of icon
            var oldColor = GUI.color;
            GUI.color = typeColors[result.Type];
            EditorGUILayout.LabelField("", GUILayout.Width(24), GUILayout.Height(24));
            GUI.color = oldColor;
        }

        // Name and ID
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(result.Name, EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"ID: {result.ID}", EditorStyles.miniLabel, GUILayout.Width(150));

        if (!string.IsNullOrEmpty(result.Description))
        {
            string shortDesc = result.Description.Length > 40
                ? result.Description.Substring(0, 40) + "..."
                : result.Description;
            EditorGUILayout.LabelField(shortDesc, EditorStyles.miniLabel);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        // Action buttons
        if (GUILayout.Button("Select", GUILayout.Width(50)))
        {
            Selection.activeObject = result.Asset;
            EditorGUIUtility.PingObject(result.Asset);
        }

        if (GUILayout.Button("Open", GUILayout.Width(50)))
        {
            OpenInManager(result);
        }

        EditorGUILayout.EndHorizontal();
    }

    #endregion

    #region Search Logic

    private void PerformSearch()
    {
        searchResults.Clear();
        hasSearched = true;

        if (string.IsNullOrEmpty(searchQuery) || searchQuery.Length < 2)
            return;

        string queryLower = searchQuery.ToLower();

        // Search Items
        if (itemRegistry?.AllItems != null)
        {
            foreach (var item in itemRegistry.AllItems.Where(i => i != null))
            {
                if (MatchesQuery(item.GetDisplayName(), item.ItemID, item.Description, queryLower))
                {
                    searchResults.Add(new SearchResult
                    {
                        Type = ContentType.Item,
                        Name = item.GetDisplayName(),
                        ID = item.ItemID,
                        Description = item.Description,
                        Asset = item,
                        Icon = item.ItemIcon
                    });
                }
            }
        }

        // Search Abilities
        if (abilityRegistry?.AllAbilities != null)
        {
            foreach (var ability in abilityRegistry.AllAbilities.Where(a => a != null))
            {
                if (MatchesQuery(ability.AbilityName, ability.AbilityID, ability.Description, queryLower))
                {
                    searchResults.Add(new SearchResult
                    {
                        Type = ContentType.Ability,
                        Name = ability.AbilityName,
                        ID = ability.AbilityID,
                        Description = ability.Description,
                        Asset = ability,
                        Icon = ability.AbilityIcon
                    });
                }
            }
        }

        // Search Enemies
        foreach (var enemy in allEnemies)
        {
            if (MatchesQuery(enemy.EnemyName, enemy.EnemyID, null, queryLower))
            {
                searchResults.Add(new SearchResult
                {
                    Type = ContentType.Enemy,
                    Name = enemy.EnemyName,
                    ID = enemy.EnemyID,
                    Description = $"Level {enemy.Level} - HP: {enemy.MaxHealth}",
                    Asset = enemy,
                    Icon = enemy.EnemySprite
                });
            }
        }

        // Search NPCs
        if (npcRegistry?.AllNPCs != null)
        {
            foreach (var npc in npcRegistry.AllNPCs.Where(n => n != null))
            {
                if (MatchesQuery(npc.NPCName, npc.NPCID, npc.Description, queryLower))
                {
                    searchResults.Add(new SearchResult
                    {
                        Type = ContentType.NPC,
                        Name = npc.NPCName,
                        ID = npc.NPCID,
                        Description = npc.Description,
                        Asset = npc,
                        Icon = npc.Avatar
                    });
                }
            }
        }

        // Search Locations
        if (locationRegistry?.AllLocations != null)
        {
            foreach (var location in locationRegistry.AllLocations.Where(l => l != null))
            {
                if (MatchesQuery(location.DisplayName, location.LocationID, location.Description, queryLower))
                {
                    searchResults.Add(new SearchResult
                    {
                        Type = ContentType.Location,
                        Name = location.DisplayName,
                        ID = location.LocationID,
                        Description = location.Description,
                        Asset = location,
                        Icon = location.LocationIcon
                    });
                }
            }
        }

        // Search Status Effects
        if (statusEffectRegistry?.AllEffects != null)
        {
            foreach (var effect in statusEffectRegistry.AllEffects.Where(s => s != null))
            {
                if (MatchesQuery(effect.EffectName, effect.EffectID, effect.Description, queryLower))
                {
                    searchResults.Add(new SearchResult
                    {
                        Type = ContentType.StatusEffect,
                        Name = effect.EffectName,
                        ID = effect.EffectID,
                        Description = effect.Description,
                        Asset = effect,
                        Icon = effect.EffectIcon
                    });
                }
            }
        }

        // Search Activities
        foreach (var activity in allActivities)
        {
            if (MatchesQuery(activity.ActivityName, activity.ActivityID, activity.BaseDescription, queryLower))
            {
                searchResults.Add(new SearchResult
                {
                    Type = ContentType.Activity,
                    Name = activity.ActivityName,
                    ID = activity.ActivityID,
                    Description = activity.BaseDescription,
                    Asset = activity,
                    Icon = activity.ActivityIcon
                });
            }
        }

        // Sort results: exact matches first, then by name
        searchResults = searchResults
            .OrderBy(r => !r.Name.ToLower().StartsWith(queryLower))
            .ThenBy(r => !r.ID.ToLower().StartsWith(queryLower))
            .ThenBy(r => r.Name)
            .ToList();
    }

    private bool MatchesQuery(string name, string id, string description, string queryLower)
    {
        return (name?.ToLower().Contains(queryLower) ?? false) ||
               (id?.ToLower().Contains(queryLower) ?? false) ||
               (description?.ToLower().Contains(queryLower) ?? false);
    }

    #endregion

    #region Manager Integration

    private void OpenInManager(SearchResult result)
    {
        switch (result.Type)
        {
            case ContentType.Item:
                OpenItemManager(result.Asset as ItemDefinition);
                break;
            case ContentType.Ability:
                OpenAbilityManager(result.Asset as AbilityDefinition);
                break;
            case ContentType.Enemy:
                OpenEnemyManager(result.Asset as EnemyDefinition);
                break;
            case ContentType.NPC:
                OpenNPCManager(result.Asset as NPCDefinition);
                break;
            case ContentType.Location:
                OpenConnectionManager(result.Asset as MapLocationDefinition);
                break;
            case ContentType.StatusEffect:
                OpenStatusEffectManager(result.Asset as StatusEffectDefinition);
                break;
            case ContentType.Activity:
                OpenActivityManager(result.Asset as ActivityDefinition);
                break;
        }
    }

    private void OpenItemManager(ItemDefinition item)
    {
        var window = EditorWindow.GetWindow<ItemManagerWindow>();
        window.Show();
        window.Focus();
        Selection.activeObject = item;
        EditorGUIUtility.PingObject(item);
    }

    private void OpenAbilityManager(AbilityDefinition ability)
    {
        var window = EditorWindow.GetWindow<AbilityManagerWindow>();
        window.Show();
        window.Focus();
        Selection.activeObject = ability;
        EditorGUIUtility.PingObject(ability);
    }

    private void OpenEnemyManager(EnemyDefinition enemy)
    {
        var window = EditorWindow.GetWindow<EnemyManagerWindow>();
        window.Show();
        window.Focus();
        Selection.activeObject = enemy;
        EditorGUIUtility.PingObject(enemy);
    }

    private void OpenNPCManager(NPCDefinition npc)
    {
        var window = EditorWindow.GetWindow<NPCManagerWindow>();
        window.Show();
        window.Focus();
        Selection.activeObject = npc;
        EditorGUIUtility.PingObject(npc);
    }

    private void OpenConnectionManager(MapLocationDefinition location)
    {
        var window = EditorWindow.GetWindow<ConnectionManagerWindow>();
        window.Show();
        window.Focus();
        Selection.activeObject = location;
        EditorGUIUtility.PingObject(location);
    }

    private void OpenStatusEffectManager(StatusEffectDefinition effect)
    {
        var window = EditorWindow.GetWindow<StatusEffectManagerWindow>();
        window.Show();
        window.Focus();
        Selection.activeObject = effect;
        EditorGUIUtility.PingObject(effect);
    }

    private void OpenActivityManager(ActivityDefinition activity)
    {
        var window = EditorWindow.GetWindow<ActivityManagerWindow>();
        window.Show();
        window.Focus();
        Selection.activeObject = activity;
        EditorGUIUtility.PingObject(activity);
    }

    #endregion

    #region Registry Loading

    private void LoadAllRegistries()
    {
        // Load ItemRegistry
        string[] itemGuids = AssetDatabase.FindAssets("t:ItemRegistry");
        if (itemGuids.Length > 0)
        {
            itemRegistry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(
                AssetDatabase.GUIDToAssetPath(itemGuids[0]));
        }

        // Load AbilityRegistry
        string[] abilityGuids = AssetDatabase.FindAssets("t:AbilityRegistry");
        if (abilityGuids.Length > 0)
        {
            abilityRegistry = AssetDatabase.LoadAssetAtPath<AbilityRegistry>(
                AssetDatabase.GUIDToAssetPath(abilityGuids[0]));
        }

        // Load StatusEffectRegistry
        string[] statusGuids = AssetDatabase.FindAssets("t:StatusEffectRegistry");
        if (statusGuids.Length > 0)
        {
            statusEffectRegistry = AssetDatabase.LoadAssetAtPath<StatusEffectRegistry>(
                AssetDatabase.GUIDToAssetPath(statusGuids[0]));
        }

        // Load LocationRegistry
        string[] locationGuids = AssetDatabase.FindAssets("t:LocationRegistry");
        if (locationGuids.Length > 0)
        {
            locationRegistry = AssetDatabase.LoadAssetAtPath<LocationRegistry>(
                AssetDatabase.GUIDToAssetPath(locationGuids[0]));
        }

        // Load NPCRegistry
        string[] npcGuids = AssetDatabase.FindAssets("t:NPCRegistry");
        if (npcGuids.Length > 0)
        {
            npcRegistry = AssetDatabase.LoadAssetAtPath<NPCRegistry>(
                AssetDatabase.GUIDToAssetPath(npcGuids[0]));
        }

        // Load all EnemyDefinitions directly (no registry)
        allEnemies.Clear();
        string[] enemyGuids = AssetDatabase.FindAssets("t:EnemyDefinition");
        foreach (var guid in enemyGuids)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
                AssetDatabase.GUIDToAssetPath(guid));
            if (enemy != null)
                allEnemies.Add(enemy);
        }

        // Load all ActivityDefinitions directly (no registry used for search)
        allActivities.Clear();
        string[] activityGuids = AssetDatabase.FindAssets("t:ActivityDefinition");
        foreach (var guid in activityGuids)
        {
            var activity = AssetDatabase.LoadAssetAtPath<ActivityDefinition>(
                AssetDatabase.GUIDToAssetPath(guid));
            if (activity != null)
                allActivities.Add(activity);
        }
    }

    #endregion

    #region Recent Searches

    private void LoadRecentSearches()
    {
        string saved = EditorPrefs.GetString(RECENT_SEARCHES_KEY, "");
        recentSearches = string.IsNullOrEmpty(saved)
            ? new List<string>()
            : saved.Split('|').Where(s => !string.IsNullOrEmpty(s)).ToList();
        showRecentSearches = recentSearches.Count > 0;
    }

    private void SaveRecentSearches()
    {
        EditorPrefs.SetString(RECENT_SEARCHES_KEY, string.Join("|", recentSearches));
    }

    private void AddRecentSearch(string query)
    {
        // Remove if already exists (will re-add at top)
        recentSearches.Remove(query);

        // Add at beginning
        recentSearches.Insert(0, query);

        // Trim to max
        while (recentSearches.Count > MAX_RECENT_SEARCHES)
        {
            recentSearches.RemoveAt(recentSearches.Count - 1);
        }

        SaveRecentSearches();
    }

    #endregion
}
#endif
