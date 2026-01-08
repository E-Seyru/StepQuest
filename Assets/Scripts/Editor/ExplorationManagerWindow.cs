// Purpose: Editor window for managing and testing exploration system
// Filepath: Assets/Scripts/Editor/ExplorationManagerWindow.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ExplorationManagerWindow : EditorWindow
{
    [MenuItem("StepQuest/World/Exploration Manager")]
    public static void ShowWindow()
    {
        ExplorationManagerWindow window = GetWindow<ExplorationManagerWindow>();
        window.titleContent = new GUIContent("Exploration Manager");
        window.minSize = new Vector2(550, 500);
        window.Show();
    }

    // Data
    private LocationRegistry locationRegistry;

    // UI State
    private Vector2 scrollPosition;
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Location Overview", "Content Setup & Testing" };

    // Location Overview
    private MapLocationDefinition selectedLocation;
    private bool showOnlyWithHiddenContent = false;

    // Content Setup & Testing
    private MapLocationDefinition contentSetupLocation;
    private Vector2 contentScrollPosition;

    void OnEnable()
    {
        LoadRegistries();
    }

    void OnGUI()
    {
        DrawHeader();

        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        switch (selectedTab)
        {
            case 0:
                DrawLocationOverviewTab();
                break;
            case 1:
                DrawContentSetupAndTestingTab();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    #region Header
    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label("Exploration Manager", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        // Show play mode indicator
        if (Application.isPlaying)
        {
            GUI.color = Color.green;
            GUILayout.Label("[PLAY MODE - Testing Enabled]", EditorStyles.boldLabel);
            GUI.color = Color.white;
        }

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            LoadRegistries();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }
    #endregion

    #region Location Overview Tab
    private void DrawLocationOverviewTab()
    {
        EditorGUILayout.LabelField("Location Exploration Overview", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        showOnlyWithHiddenContent = EditorGUILayout.Toggle("Show only locations with hidden content", showOnlyWithHiddenContent);
        EditorGUILayout.Space();

        if (locationRegistry == null || locationRegistry.AllLocations == null)
        {
            EditorGUILayout.HelpBox("Location Registry not found. Create one first.", MessageType.Warning);
            return;
        }

        // Draw location list
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Locations", EditorStyles.boldLabel);

        foreach (var location in locationRegistry.AllLocations)
        {
            if (location == null) continue;

            int hiddenEnemies = CountHiddenEnemies(location);
            int hiddenNPCs = CountHiddenNPCs(location);
            int hiddenActivities = CountHiddenActivities(location);
            int totalHidden = hiddenEnemies + hiddenNPCs + hiddenActivities;

            if (showOnlyWithHiddenContent && totalHidden == 0) continue;

            EditorGUILayout.BeginHorizontal();

            // Location name with color based on hidden content
            GUI.color = totalHidden > 0 ? Color.cyan : Color.white;
            if (GUILayout.Button(location.DisplayName, EditorStyles.label, GUILayout.Width(130)))
            {
                selectedLocation = location;
            }
            GUI.color = Color.white;

            // Stats
            EditorGUILayout.LabelField($"E: {hiddenEnemies}", GUILayout.Width(40));
            EditorGUILayout.LabelField($"N: {hiddenNPCs}", GUILayout.Width(40));
            EditorGUILayout.LabelField($"A: {hiddenActivities}", GUILayout.Width(40));

            if (GUILayout.Button("Setup & Test", GUILayout.Width(80)))
            {
                contentSetupLocation = location;
                selectedTab = 1; // Switch to Content Setup & Testing tab
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();

        // Selected location details
        if (selectedLocation != null)
        {
            EditorGUILayout.Space();
            DrawLocationDetails(selectedLocation);
        }
    }

    private void DrawLocationDetails(MapLocationDefinition location)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"Location: {location.DisplayName}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"ID: {location.LocationID}");

        // Hidden Enemies
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hidden Enemies:", EditorStyles.boldLabel);

        if (location.AvailableEnemies != null)
        {
            foreach (var enemy in location.AvailableEnemies)
            {
                if (enemy != null && enemy.IsHidden && enemy.EnemyReference != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"  - {enemy.EnemyReference.GetDisplayName()}", GUILayout.Width(150));
                    GUI.color = GetRarityColor(enemy.Rarity);
                    EditorGUILayout.LabelField($"[{enemy.Rarity}]", GUILayout.Width(80));
                    GUI.color = Color.white;
                    EditorGUILayout.LabelField($"+{enemy.GetDiscoveryBonusXP()} XP", GUILayout.Width(60));
                    EditorGUILayout.LabelField($"{enemy.GetBaseDiscoveryChance() * 100f:F1}%", GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        // Hidden NPCs
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hidden NPCs:", EditorStyles.boldLabel);

        if (location.AvailableNPCs != null)
        {
            foreach (var npc in location.AvailableNPCs)
            {
                if (npc != null && npc.IsHidden && npc.NPCReference != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"  - {npc.NPCReference.GetDisplayName()}", GUILayout.Width(150));
                    GUI.color = GetRarityColor(npc.Rarity);
                    EditorGUILayout.LabelField($"[{npc.Rarity}]", GUILayout.Width(80));
                    GUI.color = Color.white;
                    EditorGUILayout.LabelField($"+{npc.GetDiscoveryBonusXP()} XP", GUILayout.Width(60));
                    EditorGUILayout.LabelField($"{npc.GetBaseDiscoveryChance() * 100f:F1}%", GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        // Hidden Activities
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hidden Activities:", EditorStyles.boldLabel);

        if (location.AvailableActivities != null)
        {
            foreach (var activity in location.AvailableActivities)
            {
                if (activity != null && activity.IsHidden && activity.ActivityReference != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"  - {activity.GetDisplayName()}", GUILayout.Width(150));
                    GUI.color = GetRarityColor(activity.Rarity);
                    EditorGUILayout.LabelField($"[{activity.Rarity}]", GUILayout.Width(80));
                    GUI.color = Color.white;
                    EditorGUILayout.LabelField($"+{activity.GetDiscoveryBonusXP()} XP", GUILayout.Width(60));
                    EditorGUILayout.LabelField($"{activity.GetBaseDiscoveryChance() * 100f:F1}%", GUILayout.Width(50));
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        EditorGUILayout.EndVertical();
    }
    #endregion

    #region Content Setup & Testing Tab
    private void DrawContentSetupAndTestingTab()
    {
        EditorGUILayout.LabelField("Hidden Content Setup & Testing", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        contentSetupLocation = (MapLocationDefinition)EditorGUILayout.ObjectField(
            "Location", contentSetupLocation, typeof(MapLocationDefinition), false);

        if (contentSetupLocation == null)
        {
            EditorGUILayout.HelpBox("Select a location to configure and test hidden content.", MessageType.Info);
            DrawDiscoveryChancesReference();
            return;
        }

        // Runtime status and global actions
        DrawRuntimeStatus();

        EditorGUILayout.Space();
        contentScrollPosition = EditorGUILayout.BeginScrollView(contentScrollPosition, GUILayout.Height(350));

        // Enemies Section
        DrawEnemiesSection();

        EditorGUILayout.Space();

        // NPCs Section
        DrawNPCsSection();

        EditorGUILayout.Space();

        // Activities Section
        DrawActivitiesSection();

        EditorGUILayout.EndScrollView();

        // Save button
        EditorGUILayout.Space();
        if (GUILayout.Button("Save Changes", GUILayout.Height(30)))
        {
            EditorUtility.SetDirty(contentSetupLocation);
            AssetDatabase.SaveAssets();
            Logger.LogInfo($"Saved exploration settings for {contentSetupLocation.DisplayName}", Logger.LogCategory.EditorLog);
        }
    }

    private void DrawRuntimeStatus()
    {
        EditorGUILayout.BeginVertical("box");

        if (Application.isPlaying)
        {
            EditorGUILayout.BeginHorizontal();
            GUI.color = Color.green;
            EditorGUILayout.LabelField("Runtime Testing Active", EditorStyles.boldLabel);
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();

            // Show discovery count
            if (DataManager.Instance?.PlayerData != null)
            {
                var discoveries = DataManager.Instance.PlayerData.GetDiscoveriesAtLocation(contentSetupLocation.LocationID);
                EditorGUILayout.LabelField($"Discoveries: {discoveries.Count}", GUILayout.Width(100));
            }

            if (GUILayout.Button("Clear Discoveries", GUILayout.Width(120)))
            {
                if (DataManager.Instance?.PlayerData != null)
                {
                    DataManager.Instance.PlayerData.ClearDiscoveriesAtLocation(contentSetupLocation.LocationID);
                    Logger.LogInfo($"Cleared discoveries at {contentSetupLocation.DisplayName}", Logger.LogCategory.EditorLog);
                }
            }
            EditorGUILayout.EndHorizontal();

            // Show current discoveries
            if (DataManager.Instance?.PlayerData != null)
            {
                var discoveries = DataManager.Instance.PlayerData.GetDiscoveriesAtLocation(contentSetupLocation.LocationID);
                if (discoveries.Count > 0)
                {
                    EditorGUILayout.LabelField("Discovered: " + string.Join(", ", discoveries), EditorStyles.miniLabel);
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("Enter Play Mode to test discoveries", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawEnemiesSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Enemies", EditorStyles.boldLabel);

        if (contentSetupLocation.AvailableEnemies != null && contentSetupLocation.AvailableEnemies.Count > 0)
        {
            for (int i = 0; i < contentSetupLocation.AvailableEnemies.Count; i++)
            {
                var enemy = contentSetupLocation.AvailableEnemies[i];
                if (enemy == null || enemy.EnemyReference == null) continue;

                DrawEnemyItem(enemy);
            }
        }
        else
        {
            EditorGUILayout.LabelField("No enemies at this location", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawEnemyItem(LocationEnemy enemy)
    {
        EditorGUILayout.BeginVertical("box");

        // Header row with name, hidden toggle, and discovery button
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(enemy.EnemyReference.GetDisplayName(), EditorStyles.boldLabel, GUILayout.Width(130));

        EditorGUI.BeginChangeCheck();
        enemy.IsHidden = EditorGUILayout.Toggle("Hidden", enemy.IsHidden, GUILayout.Width(70));
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(contentSetupLocation);
        }

        GUILayout.FlexibleSpace();

        // Discovery status and button (only in play mode and if hidden)
        if (enemy.IsHidden && Application.isPlaying)
        {
            string discoveryId = enemy.GetDiscoveryID();
            bool isDiscovered = IsDiscovered(contentSetupLocation.LocationID, discoveryId);

            if (isDiscovered)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("[DISCOVERED]", GUILayout.Width(90));
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.yellow;
                if (GUILayout.Button("Trigger Discovery", GUILayout.Width(110)))
                {
                    TriggerDiscovery(
                        contentSetupLocation.LocationID,
                        discoveryId,
                        DiscoverableType.Enemy,
                        enemy.Rarity,
                        enemy.EnemyReference.GetDisplayName()
                    );
                }
                GUI.color = Color.white;
            }
        }

        EditorGUILayout.EndHorizontal();

        // Settings row (only if hidden)
        if (enemy.IsHidden)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Rarity:", GUILayout.Width(45));
            enemy.Rarity = (DiscoveryRarity)EditorGUILayout.EnumPopup(enemy.Rarity, GUILayout.Width(90));

            EditorGUILayout.LabelField("XP Override:", GUILayout.Width(70));
            enemy.BonusXPOverride = EditorGUILayout.IntField(enemy.BonusXPOverride, GUILayout.Width(50));

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(contentSetupLocation);
            }

            GUILayout.FlexibleSpace();

            // Show computed values
            GUI.color = GetRarityColor(enemy.Rarity);
            EditorGUILayout.LabelField($"{enemy.GetBaseDiscoveryChance() * 100f:F1}%", GUILayout.Width(40));
            GUI.color = Color.white;
            EditorGUILayout.LabelField($"+{enemy.GetDiscoveryBonusXP()} XP", GUILayout.Width(55));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawNPCsSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("NPCs", EditorStyles.boldLabel);

        if (contentSetupLocation.AvailableNPCs != null && contentSetupLocation.AvailableNPCs.Count > 0)
        {
            for (int i = 0; i < contentSetupLocation.AvailableNPCs.Count; i++)
            {
                var npc = contentSetupLocation.AvailableNPCs[i];
                if (npc == null || npc.NPCReference == null) continue;

                DrawNPCItem(npc);
            }
        }
        else
        {
            EditorGUILayout.LabelField("No NPCs at this location", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawNPCItem(LocationNPC npc)
    {
        EditorGUILayout.BeginVertical("box");

        // Header row with name, hidden toggle, and discovery button
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(npc.NPCReference.GetDisplayName(), EditorStyles.boldLabel, GUILayout.Width(130));

        EditorGUI.BeginChangeCheck();
        npc.IsHidden = EditorGUILayout.Toggle("Hidden", npc.IsHidden, GUILayout.Width(70));
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(contentSetupLocation);
        }

        GUILayout.FlexibleSpace();

        // Discovery status and button (only in play mode and if hidden)
        if (npc.IsHidden && Application.isPlaying)
        {
            string discoveryId = npc.GetDiscoveryID();
            bool isDiscovered = IsDiscovered(contentSetupLocation.LocationID, discoveryId);

            if (isDiscovered)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("[DISCOVERED]", GUILayout.Width(90));
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.yellow;
                if (GUILayout.Button("Trigger Discovery", GUILayout.Width(110)))
                {
                    TriggerDiscovery(
                        contentSetupLocation.LocationID,
                        discoveryId,
                        DiscoverableType.NPC,
                        npc.Rarity,
                        npc.NPCReference.GetDisplayName()
                    );
                }
                GUI.color = Color.white;
            }
        }

        EditorGUILayout.EndHorizontal();

        // Settings row (only if hidden)
        if (npc.IsHidden)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Rarity:", GUILayout.Width(45));
            npc.Rarity = (DiscoveryRarity)EditorGUILayout.EnumPopup(npc.Rarity, GUILayout.Width(90));

            EditorGUILayout.LabelField("XP Override:", GUILayout.Width(70));
            npc.BonusXPOverride = EditorGUILayout.IntField(npc.BonusXPOverride, GUILayout.Width(50));

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(contentSetupLocation);
            }

            GUILayout.FlexibleSpace();

            // Show computed values
            GUI.color = GetRarityColor(npc.Rarity);
            EditorGUILayout.LabelField($"{npc.GetBaseDiscoveryChance() * 100f:F1}%", GUILayout.Width(40));
            GUI.color = Color.white;
            EditorGUILayout.LabelField($"+{npc.GetDiscoveryBonusXP()} XP", GUILayout.Width(55));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawActivitiesSection()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Activities", EditorStyles.boldLabel);

        if (contentSetupLocation.AvailableActivities != null && contentSetupLocation.AvailableActivities.Count > 0)
        {
            for (int i = 0; i < contentSetupLocation.AvailableActivities.Count; i++)
            {
                var activity = contentSetupLocation.AvailableActivities[i];
                if (activity == null || activity.ActivityReference == null) continue;

                DrawActivityItem(activity);
            }
        }
        else
        {
            EditorGUILayout.LabelField("No activities at this location", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawActivityItem(LocationActivity activity)
    {
        EditorGUILayout.BeginVertical("box");

        // Header row with name, hidden toggle, and discovery button
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(activity.GetDisplayName(), EditorStyles.boldLabel, GUILayout.Width(130));

        EditorGUI.BeginChangeCheck();
        activity.IsHidden = EditorGUILayout.Toggle("Hidden", activity.IsHidden, GUILayout.Width(70));
        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(contentSetupLocation);
        }

        GUILayout.FlexibleSpace();

        // Discovery status and button (only in play mode and if hidden)
        if (activity.IsHidden && Application.isPlaying)
        {
            string discoveryId = activity.GetDiscoveryID();
            bool isDiscovered = IsDiscovered(contentSetupLocation.LocationID, discoveryId);

            if (isDiscovered)
            {
                GUI.color = Color.green;
                EditorGUILayout.LabelField("[DISCOVERED]", GUILayout.Width(90));
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.yellow;
                if (GUILayout.Button("Trigger Discovery", GUILayout.Width(110)))
                {
                    TriggerDiscovery(
                        contentSetupLocation.LocationID,
                        discoveryId,
                        DiscoverableType.Activity,
                        activity.Rarity,
                        activity.GetDisplayName()
                    );
                }
                GUI.color = Color.white;
            }
        }

        EditorGUILayout.EndHorizontal();

        // Settings row (only if hidden)
        if (activity.IsHidden)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Rarity:", GUILayout.Width(45));
            activity.Rarity = (DiscoveryRarity)EditorGUILayout.EnumPopup(activity.Rarity, GUILayout.Width(90));

            EditorGUILayout.LabelField("XP Override:", GUILayout.Width(70));
            activity.BonusXPOverride = EditorGUILayout.IntField(activity.BonusXPOverride, GUILayout.Width(50));

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(contentSetupLocation);
            }

            GUILayout.FlexibleSpace();

            // Show computed values
            GUI.color = GetRarityColor(activity.Rarity);
            EditorGUILayout.LabelField($"{activity.GetBaseDiscoveryChance() * 100f:F1}%", GUILayout.Width(40));
            GUI.color = Color.white;
            EditorGUILayout.LabelField($"+{activity.GetDiscoveryBonusXP()} XP", GUILayout.Width(55));

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawDiscoveryChancesReference()
    {
        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Discovery Chances Reference:", EditorStyles.boldLabel);

        DrawRarityRow("Common", GameConstants.DiscoveryChanceCommon, GameConstants.DiscoveryXPCommon, DiscoveryRarity.Common);
        DrawRarityRow("Uncommon", GameConstants.DiscoveryChanceUncommon, GameConstants.DiscoveryXPUncommon, DiscoveryRarity.Uncommon);
        DrawRarityRow("Rare", GameConstants.DiscoveryChanceRare, GameConstants.DiscoveryXPRare, DiscoveryRarity.Rare);
        DrawRarityRow("Epic", GameConstants.DiscoveryChanceEpic, GameConstants.DiscoveryXPEpic, DiscoveryRarity.Epic);
        DrawRarityRow("Legendary", GameConstants.DiscoveryChanceLegendary, GameConstants.DiscoveryXPLegendary, DiscoveryRarity.Legendary);

        EditorGUILayout.EndVertical();
    }

    private void DrawRarityRow(string name, float chance, int xp, DiscoveryRarity rarity)
    {
        EditorGUILayout.BeginHorizontal();
        GUI.color = GetRarityColor(rarity);
        EditorGUILayout.LabelField(name, GUILayout.Width(80));
        GUI.color = Color.white;
        EditorGUILayout.LabelField($"{chance * 100f:F1}%", GUILayout.Width(50));
        EditorGUILayout.LabelField($"+{xp} XP");
        EditorGUILayout.EndHorizontal();
    }
    #endregion

    #region Helper Methods
    private void LoadRegistries()
    {
        string[] guids = AssetDatabase.FindAssets("t:LocationRegistry");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            locationRegistry = AssetDatabase.LoadAssetAtPath<LocationRegistry>(path);
        }
    }

    private int CountHiddenEnemies(MapLocationDefinition location)
    {
        if (location?.AvailableEnemies == null) return 0;
        return location.AvailableEnemies.Count(e => e != null && e.IsHidden);
    }

    private int CountHiddenNPCs(MapLocationDefinition location)
    {
        if (location?.AvailableNPCs == null) return 0;
        return location.AvailableNPCs.Count(n => n != null && n.IsHidden);
    }

    private int CountHiddenActivities(MapLocationDefinition location)
    {
        if (location?.AvailableActivities == null) return 0;
        return location.AvailableActivities.Count(a => a != null && a.IsHidden);
    }

    private Color GetRarityColor(DiscoveryRarity rarity)
    {
        return rarity switch
        {
            DiscoveryRarity.Common => Color.white,
            DiscoveryRarity.Uncommon => Color.green,
            DiscoveryRarity.Rare => Color.cyan,
            DiscoveryRarity.Epic => new Color(0.6f, 0.2f, 0.9f), // Purple
            DiscoveryRarity.Legendary => new Color(1f, 0.5f, 0f), // Orange
            _ => Color.white
        };
    }

    private bool IsDiscovered(string locationId, string discoveryId)
    {
        if (ExplorationManager.Instance != null)
        {
            return ExplorationManager.Instance.IsDiscoveredAtLocation(locationId, discoveryId);
        }

        if (DataManager.Instance?.PlayerData != null)
        {
            return DataManager.Instance.PlayerData.HasDiscoveredAtLocation(locationId, discoveryId);
        }

        return false;
    }

    private void TriggerDiscovery(string locationId, string discoveryId, DiscoverableType type, DiscoveryRarity rarity, string displayName)
    {
        if (ExplorationManager.Instance != null)
        {
            bool success = ExplorationManager.Instance.TriggerDiscovery(locationId, discoveryId, type, rarity, displayName);
            Logger.LogInfo($"Discovery trigger: {displayName} - {(success ? "Success" : "Already discovered")}", Logger.LogCategory.EditorLog);
        }
        else
        {
            Logger.LogError("ExplorationManager not found in scene!", Logger.LogCategory.EditorLog);
        }
    }
    #endregion
}
#endif
