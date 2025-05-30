// Purpose: Custom editor window for managing activities and variants
// Filepath: Assets/Scripts/Editor/ActivityManagerWindow.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ActivityManagerWindow : EditorWindow
{
    [MenuItem("StepQuest/Activity Manager")]
    public static void ShowWindow()
    {
        ActivityManagerWindow window = GetWindow<ActivityManagerWindow>();
        window.titleContent = new GUIContent("Activity Manager");
        window.Show();
    }

    // Data
    private ActivityRegistry activityRegistry;
    private LocationRegistry locationRegistry;
    private ItemRegistry itemRegistry;

    // UI State
    private Vector2 scrollPosition;
    private int selectedTab = 0;
    private string[] tabNames = { "Activities", "Create New", "Locations", "Settings" };

    // Create Activity State
    private string newActivityName = "";
    private string newActivityDescription = "";
    private Sprite newActivityIcon;
    private Color newActivityColor = Color.white;

    // Create Variant State
    private ActivityDefinition selectedParentActivity;
    private string newVariantName = "";
    private string newVariantDescription = "";
    private ItemDefinition newVariantPrimaryResource;
    private int newVariantActionCost = 10;
    private int newVariantSuccessRate = 100;

    // Search and Filter
    private string searchFilter = "";
    private bool showOnlyInvalidActivities = false;

    void OnEnable()
    {
        LoadRegistries();
    }

    void OnGUI()
    {
        DrawHeader();

        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        switch (selectedTab)
        {
            case 0: DrawActivitiesTab(); break;
            case 1: DrawCreateNewTab(); break;
            case 2: DrawLocationsTab(); break;
            case 3: DrawSettingsTab(); break;
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");

        GUILayout.Label("🎯 Activity Manager", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Registries", GUILayout.Width(120)))
        {
            LoadRegistries();
        }

        if (GUILayout.Button("Validate All", GUILayout.Width(100)))
        {
            ValidateAllActivities();
        }

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField($"Activities: {GetActivityCount()}", GUILayout.Width(100));
        EditorGUILayout.LabelField($"Variants: {GetVariantCount()}", GUILayout.Width(100));

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawActivitiesTab()
    {
        if (activityRegistry == null)
        {
            EditorGUILayout.HelpBox("No ActivityRegistry found! Create one first.", MessageType.Error);
            return;
        }

        DrawSearchAndFilter();

        EditorGUILayout.Space();

        var filteredActivities = GetFilteredActivities();

        foreach (var locationActivity in filteredActivities)
        {
            DrawActivityEntry(locationActivity);
        }

        if (filteredActivities.Count == 0)
        {
            EditorGUILayout.HelpBox("No activities found matching your filter.", MessageType.Info);
        }
    }

    private void DrawSearchAndFilter()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter);

        showOnlyInvalidActivities = EditorGUILayout.Toggle("Invalid Only", showOnlyInvalidActivities, GUILayout.Width(100));

        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            searchFilter = "";
            showOnlyInvalidActivities = false;
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawActivityEntry(LocationActivity locationActivity)
    {
        if (locationActivity?.ActivityReference == null) return;

        var activity = locationActivity.ActivityReference;
        bool isValid = activity.IsValidActivity();

        EditorGUILayout.BeginVertical("box");

        // Header with activity name and status
        EditorGUILayout.BeginHorizontal();

        // Status icon
        string statusIcon = isValid ? "✅" : "❌";
        GUILayout.Label(statusIcon, GUILayout.Width(20));

        // Activity name (clickable to select)
        if (GUILayout.Button(activity.GetDisplayName(), EditorStyles.linkLabel, GUILayout.ExpandWidth(false)))
        {
            Selection.activeObject = activity;
            EditorGUIUtility.PingObject(activity);
        }

        GUILayout.FlexibleSpace();

        // Activity ID
        EditorGUILayout.LabelField($"ID: {activity.ActivityID}", EditorStyles.miniLabel, GUILayout.Width(120));

        // Edit button
        if (GUILayout.Button("Edit", GUILayout.Width(50)))
        {
            Selection.activeObject = activity;
        }

        EditorGUILayout.EndHorizontal();

        // Description
        if (!string.IsNullOrEmpty(activity.BaseDescription))
        {
            EditorGUILayout.LabelField(activity.BaseDescription, EditorStyles.wordWrappedMiniLabel);
        }

        // Variants
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Variants:", EditorStyles.boldLabel, GUILayout.Width(60));

        var variants = locationActivity.ActivityVariants ?? new List<ActivityVariant>();
        if (variants.Count > 0)
        {
            foreach (var variant in variants)
            {
                if (variant != null)
                {
                    bool variantValid = variant.IsValidVariant();
                    string variantIcon = variantValid ? "🔸" : "🔹";

                    if (GUILayout.Button($"{variantIcon} {variant.VariantName}",
                        EditorStyles.miniButton, GUILayout.ExpandWidth(false)))
                    {
                        Selection.activeObject = variant;
                        EditorGUIUtility.PingObject(variant);
                    }
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("No variants", EditorStyles.miniLabel);
        }

        // Add variant button
        if (GUILayout.Button("+", GUILayout.Width(25)))
        {
            CreateVariantForActivity(activity);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawCreateNewTab()
    {
        EditorGUILayout.LabelField("🆕 Create New Activity", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        // Activity Creation
        EditorGUILayout.LabelField("Activity Definition", EditorStyles.boldLabel);

        newActivityName = EditorGUILayout.TextField("Activity Name", newActivityName);
        newActivityDescription = EditorGUILayout.TextField("Description", newActivityDescription, GUILayout.Height(60));
        newActivityIcon = (Sprite)EditorGUILayout.ObjectField("Icon", newActivityIcon, typeof(Sprite), false);
        newActivityColor = EditorGUILayout.ColorField("Color", newActivityColor);

        EditorGUILayout.Space();

        // Quick Variant Creation
        EditorGUILayout.LabelField("Quick Variant (Optional)", EditorStyles.boldLabel);

        newVariantName = EditorGUILayout.TextField("Variant Name", newVariantName);
        newVariantDescription = EditorGUILayout.TextField("Variant Description", newVariantDescription);
        newVariantPrimaryResource = (ItemDefinition)EditorGUILayout.ObjectField("Primary Resource", newVariantPrimaryResource, typeof(ItemDefinition), false);
        newVariantActionCost = EditorGUILayout.IntField("Action Cost (Steps)", newVariantActionCost);
        newVariantSuccessRate = EditorGUILayout.IntSlider("Success Rate %", newVariantSuccessRate, 0, 100);

        EditorGUILayout.Space();

        // Create buttons
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = !string.IsNullOrEmpty(newActivityName);
        if (GUILayout.Button("Create Activity Only"))
        {
            CreateActivity(false);
        }

        GUI.enabled = !string.IsNullOrEmpty(newActivityName) && !string.IsNullOrEmpty(newVariantName) && newVariantPrimaryResource != null;
        if (GUILayout.Button("Create Activity + Variant"))
        {
            CreateActivity(true);
        }

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Clear Form"))
        {
            ClearCreateForm();
        }

        EditorGUILayout.EndVertical();

        // Add variant to existing activity section
        DrawAddVariantSection();

        // Quick Templates
        DrawQuickTemplates();
    }

    private void DrawAddVariantSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("➕ Add Variant to Existing Activity", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        // Select parent activity
        selectedParentActivity = (ActivityDefinition)EditorGUILayout.ObjectField("Parent Activity", selectedParentActivity, typeof(ActivityDefinition), false);

        if (selectedParentActivity != null)
        {
            EditorGUILayout.LabelField($"Selected: {selectedParentActivity.GetDisplayName()}", EditorStyles.miniLabel);

            // Variant details
            newVariantName = EditorGUILayout.TextField("Variant Name", newVariantName);
            newVariantDescription = EditorGUILayout.TextField("Variant Description", newVariantDescription);
            newVariantPrimaryResource = (ItemDefinition)EditorGUILayout.ObjectField("Primary Resource", newVariantPrimaryResource, typeof(ItemDefinition), false);
            newVariantActionCost = EditorGUILayout.IntField("Action Cost (Steps)", newVariantActionCost);
            newVariantSuccessRate = EditorGUILayout.IntSlider("Success Rate %", newVariantSuccessRate, 0, 100);

            EditorGUILayout.Space();

            GUI.enabled = !string.IsNullOrEmpty(newVariantName) && newVariantPrimaryResource != null;
            if (GUILayout.Button($"Add Variant to {selectedParentActivity.GetDisplayName()}"))
            {
                CreateVariantForExistingActivity();
            }
            GUI.enabled = true;
        }
        else
        {
            EditorGUILayout.HelpBox("Select an activity above to add a variant to it", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawQuickTemplates()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("⚡ Quick Templates", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Mining Activity"))
        {
            FillTemplate("Mining", "Extract valuable ores and materials from the earth", "mining");
        }

        if (GUILayout.Button("Woodcutting Activity"))
        {
            FillTemplate("Woodcutting", "Chop down trees to gather wood resources", "woodcutting");
        }

        if (GUILayout.Button("Fishing Activity"))
        {
            FillTemplate("Fishing", "Catch fish and aquatic resources", "fishing");
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Exploration Activity"))
        {
            FillTemplate("Exploration", "Discover new areas and hidden treasures", "exploration");
        }

        if (GUILayout.Button("Crafting Activity"))
        {
            FillTemplate("Crafting", "Create items and equipment", "crafting");
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawLocationsTab()
    {
        if (locationRegistry == null)
        {
            EditorGUILayout.HelpBox("No LocationRegistry found!", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("🗺️ Location Activity Assignment", EditorStyles.boldLabel);

        foreach (var location in locationRegistry.AllLocations)
        {
            if (location == null) continue;

            EditorGUILayout.BeginVertical("box");

            // Location header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(location.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"({location.LocationID})", EditorStyles.miniLabel);

            if (GUILayout.Button("Edit", GUILayout.Width(50)))
            {
                Selection.activeObject = location;
            }

            EditorGUILayout.EndHorizontal();

            // Show assigned activities
            if (location.AvailableActivities != null && location.AvailableActivities.Count > 0)
            {
                foreach (var activity in location.AvailableActivities)
                {
                    if (activity?.ActivityReference != null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"  • {activity.ActivityReference.GetDisplayName()}", EditorStyles.miniLabel);
                        EditorGUILayout.LabelField($"({activity.ActivityVariants?.Count ?? 0} variants)", EditorStyles.miniLabel, GUILayout.Width(80));
                        EditorGUILayout.EndHorizontal();
                    }
                }
            }
            else
            {
                EditorGUILayout.LabelField("  No activities assigned", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }
    }

    private void DrawSettingsTab()
    {
        EditorGUILayout.LabelField("⚙️ Settings & Utilities", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField("Registries", EditorStyles.boldLabel);

        activityRegistry = (ActivityRegistry)EditorGUILayout.ObjectField("Activity Registry", activityRegistry, typeof(ActivityRegistry), false);
        locationRegistry = (LocationRegistry)EditorGUILayout.ObjectField("Location Registry", locationRegistry, typeof(LocationRegistry), false);
        itemRegistry = (ItemRegistry)EditorGUILayout.ObjectField("Item Registry", itemRegistry, typeof(ItemRegistry), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("Auto-Find Registries"))
        {
            LoadRegistries();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField("Batch Operations", EditorStyles.boldLabel);

        if (GUILayout.Button("Force Re-register All Activities"))
        {
            ForceReregisterAll();
        }

        if (GUILayout.Button("Clean Up Broken References"))
        {
            CleanUpBrokenReferences();
        }

        if (GUILayout.Button("Export Activity Report"))
        {
            ExportActivityReport();
        }

        if (GUILayout.Button("Fix Activity-Variant Mappings"))
        {
            FixActivityVariantMappings();
        }

        EditorGUILayout.EndVertical();
    }

    // Helper Methods
    private void LoadRegistries()
    {
        if (activityRegistry == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:ActivityRegistry");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                activityRegistry = AssetDatabase.LoadAssetAtPath<ActivityRegistry>(path);
            }
        }

        if (locationRegistry == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:LocationRegistry");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                locationRegistry = AssetDatabase.LoadAssetAtPath<LocationRegistry>(path);
            }
        }

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

    private List<LocationActivity> GetFilteredActivities()
    {
        if (activityRegistry == null) return new List<LocationActivity>();

        var filtered = activityRegistry.AllActivities.Where(a => a?.ActivityReference != null);

        if (!string.IsNullOrEmpty(searchFilter))
        {
            filtered = filtered.Where(a =>
                a.ActivityReference.GetDisplayName().ToLower().Contains(searchFilter.ToLower()) ||
                a.ActivityReference.ActivityID.ToLower().Contains(searchFilter.ToLower()));
        }

        if (showOnlyInvalidActivities)
        {
            filtered = filtered.Where(a => !a.ActivityReference.IsValidActivity());
        }

        return filtered.ToList();
    }

    private void CreateActivity(bool includeVariant)
    {
        // Create the activity asset
        ActivityDefinition newActivity = CreateInstance<ActivityDefinition>();
        newActivity.ActivityName = newActivityName;
        newActivity.ActivityID = newActivityName.ToLower().Replace(" ", "_");
        newActivity.BaseDescription = newActivityDescription;
        newActivity.ActivityIcon = newActivityIcon;
        newActivity.ActivityColor = newActivityColor;

        // Save the activity
        string activityPath = $"Assets/ScriptableObjects/Activities/{newActivityName}.asset";
        AssetDatabase.CreateAsset(newActivity, activityPath);

        ActivityVariant newVariant = null;

        if (includeVariant)
        {
            // Create the variant asset
            newVariant = CreateInstance<ActivityVariant>();
            newVariant.VariantName = newVariantName;
            newVariant.VariantDescription = newVariantDescription;
            newVariant.PrimaryResource = newVariantPrimaryResource;
            newVariant.ActionCost = newVariantActionCost;
            newVariant.SuccessRate = newVariantSuccessRate;

            // Save the variant in a subfolder
            string variantDir = $"Assets/ScriptableObjects/Activities/ActivitiesVariant/{newActivityName}";
            if (!AssetDatabase.IsValidFolder(variantDir))
            {
                string parentDir = $"Assets/ScriptableObjects/Activities/ActivitiesVariant";
                if (!AssetDatabase.IsValidFolder(parentDir))
                {
                    AssetDatabase.CreateFolder("Assets/ScriptableObjects/Activities", "ActivitiesVariant");
                }
                AssetDatabase.CreateFolder(parentDir, newActivityName);
            }

            string variantPath = $"{variantDir}/{newVariantName}.asset";
            AssetDatabase.CreateAsset(newVariant, variantPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select the new activity
        Selection.activeObject = newActivity;

        // Clear form
        ClearCreateForm();

        // Show success message
        Debug.Log($"✅ Created activity '{newActivityName}'" + (includeVariant ? $" with variant '{newVariantName}'" : ""));
    }

    private void CreateVariantForActivity(ActivityDefinition activity)
    {
        selectedParentActivity = activity;
        selectedTab = 1; // Switch to create tab
        newActivityName = ""; // Clear this since we're just adding variant
    }

    private void CreateVariantForExistingActivity()
    {
        if (selectedParentActivity == null)
        {
            Debug.LogError("No parent activity selected!");
            return;
        }

        // Create the variant asset
        ActivityVariant newVariant = CreateInstance<ActivityVariant>();
        newVariant.VariantName = newVariantName;
        newVariant.VariantDescription = newVariantDescription;
        newVariant.ParentActivityID = selectedParentActivity.ActivityID;
        newVariant.PrimaryResource = newVariantPrimaryResource;
        newVariant.ActionCost = newVariantActionCost;
        newVariant.SuccessRate = newVariantSuccessRate;

        // Save the variant in a subfolder
        string activityName = selectedParentActivity.ActivityName;
        string variantDir = $"Assets/ScriptableObjects/Activities/ActivitiesVariant/{activityName}";
        if (!AssetDatabase.IsValidFolder(variantDir))
        {
            string parentDir = $"Assets/ScriptableObjects/Activities/ActivitiesVariant";
            if (!AssetDatabase.IsValidFolder(parentDir))
            {
                AssetDatabase.CreateFolder("Assets/ScriptableObjects/Activities", "ActivitiesVariant");
            }
            AssetDatabase.CreateFolder(parentDir, activityName);
        }

        string variantPath = $"{variantDir}/{newVariantName}.asset";
        AssetDatabase.CreateAsset(newVariant, variantPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select the new variant
        Selection.activeObject = newVariant;

        // Clear variant form but keep parent activity selected
        ClearVariantForm();

        // Show success message
        Debug.Log($"✅ Created variant '{newVariantName}' for activity '{selectedParentActivity.GetDisplayName()}'");
    }

    private void ClearVariantForm()
    {
        newVariantName = "";
        newVariantDescription = "";
        newVariantPrimaryResource = null;
        newVariantActionCost = 10;
        newVariantSuccessRate = 100;
    }

    private void ClearCreateForm()
    {
        newActivityName = "";
        newActivityDescription = "";
        newActivityIcon = null;
        newActivityColor = Color.white;
        selectedParentActivity = null;
        ClearVariantForm();
    }

    private void FillTemplate(string activityName, string description, string type)
    {
        newActivityName = activityName;
        newActivityDescription = description;
        newVariantName = $"Basic {activityName}";
        newVariantDescription = $"Basic {type} variant";
        newVariantActionCost = 10;
        newVariantSuccessRate = 100;
    }

    private void ValidateAllActivities()
    {
        if (activityRegistry != null)
        {
            activityRegistry.ValidateRegistry();
        }
    }

    private void ForceReregisterAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:ActivityDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ActivityDefinition activity = AssetDatabase.LoadAssetAtPath<ActivityDefinition>(path);
            if (activity != null)
            {
                EditorUtility.SetDirty(activity);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("✅ Force re-registered all activities");
    }

    private void CleanUpBrokenReferences()
    {
        if (activityRegistry == null) return;

        int removed = activityRegistry.AllActivities.RemoveAll(a => a?.ActivityReference == null);
        if (removed > 0)
        {
            EditorUtility.SetDirty(activityRegistry);
            AssetDatabase.SaveAssets();
            Debug.Log($"🧹 Cleaned up {removed} broken activity references");
        }
    }

    private void ExportActivityReport()
    {
        // Simple report to console - could be expanded to file export
        Debug.Log("=== Activity Report ===");
        Debug.Log($"Total Activities: {GetActivityCount()}");
        Debug.Log($"Total Variants: {GetVariantCount()}");

        if (activityRegistry != null)
        {
            foreach (var activity in activityRegistry.AllActivities)
            {
                if (activity?.ActivityReference != null)
                {
                    var variants = activity.ActivityVariants ?? new List<ActivityVariant>();
                    Debug.Log($"  • {activity.ActivityReference.GetDisplayName()} ({variants.Count} variants)");
                }
            }
        }
    }

    private int GetActivityCount()
    {
        return activityRegistry?.AllActivities?.Count(a => a?.ActivityReference != null) ?? 0;
    }

    private void FixActivityVariantMappings()
    {
        int fixedCount = 0;

        // Find all ActivityVariant assets
        string[] variantGuids = AssetDatabase.FindAssets("t:ActivityVariant");

        foreach (string guid in variantGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ActivityVariant variant = AssetDatabase.LoadAssetAtPath<ActivityVariant>(path);

            if (variant != null)
            {
                string oldParentId = variant.ParentActivityID;

                // Force re-detection
                EditorUtility.SetDirty(variant);
                fixedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"🔧 Fixed {fixedCount} activity-variant mappings. Check console for results.");
    }

    private int GetVariantCount()
    {
        if (activityRegistry == null) return 0;

        return activityRegistry.AllActivities
            .Where(a => a?.ActivityReference != null)
            .Sum(a => a.ActivityVariants?.Count ?? 0);
    }
}
#endif