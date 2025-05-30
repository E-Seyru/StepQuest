// Purpose: Enhanced tool to manage activities, variants, and POI assignments
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

    // UI State
    private Vector2 scrollPosition;
    private string searchFilter = "";
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Activities", "POI Management" };

    // POI Management
    private POI[] allPOIs;
    private Dictionary<string, MapLocationDefinition> locationLookup = new Dictionary<string, MapLocationDefinition>();

    void OnEnable()
    {
        LoadRegistries();
        RefreshPOIList();
    }

    void OnGUI()
    {
        DrawHeader();

        // Tab selection
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        switch (selectedTab)
        {
            case 0:
                DrawActivitiesTab();
                break;
            case 1:
                DrawPOIManagementTab();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");

        GUILayout.Label("Activity Manager", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        // Registry selection
        activityRegistry = (ActivityRegistry)EditorGUILayout.ObjectField("Activity Registry", activityRegistry, typeof(ActivityRegistry), false);
        locationRegistry = (LocationRegistry)EditorGUILayout.ObjectField("Location Registry", locationRegistry, typeof(LocationRegistry), false);

        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            LoadRegistries();
            RefreshPOIList();
        }

        if (GUILayout.Button("Validate", GUILayout.Width(60)))
        {
            ValidateRegistries();
        }

        EditorGUILayout.EndHorizontal();

        // Search
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    #region Activities Tab (Existing Code)
    private void DrawActivitiesTab()
    {
        if (activityRegistry == null)
        {
            EditorGUILayout.HelpBox("Select an ActivityRegistry to manage activities.", MessageType.Info);
            return;
        }

        var filteredActivities = GetFilteredActivities();

        foreach (var locationActivity in filteredActivities)
        {
            DrawActivityEntry(locationActivity);
        }

        if (filteredActivities.Count == 0)
        {
            EditorGUILayout.HelpBox("No activities found.", MessageType.Info);
        }
    }

    private void DrawActivityEntry(LocationActivity locationActivity)
    {
        if (locationActivity?.ActivityReference == null) return;

        var activity = locationActivity.ActivityReference;

        EditorGUILayout.BeginVertical("box");

        // Activity header
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(activity.GetDisplayName(), EditorStyles.boldLabel, GUILayout.Width(200));
        EditorGUILayout.LabelField($"ID: {activity.ActivityID}", EditorStyles.miniLabel);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Edit Activity", GUILayout.Width(80)))
        {
            Selection.activeObject = activity;
            EditorGUIUtility.PingObject(activity);
        }

        EditorGUILayout.EndHorizontal();

        // Description
        if (!string.IsNullOrEmpty(activity.BaseDescription))
        {
            EditorGUILayout.LabelField(activity.BaseDescription, EditorStyles.wordWrappedMiniLabel);
        }

        EditorGUILayout.Space();

        // Variants section
        EditorGUILayout.LabelField("Associated Variants:", EditorStyles.boldLabel);

        // Current variants list
        if (locationActivity.ActivityVariants != null && locationActivity.ActivityVariants.Count > 0)
        {
            for (int i = 0; i < locationActivity.ActivityVariants.Count; i++)
            {
                DrawVariantEntry(locationActivity, i);
            }
        }
        else
        {
            EditorGUILayout.LabelField("  No variants assigned", EditorStyles.miniLabel);
        }

        // Add new variant
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Add Variant:", GUILayout.Width(80));

        ActivityVariant newVariant = (ActivityVariant)EditorGUILayout.ObjectField(null, typeof(ActivityVariant), false);

        if (newVariant != null)
        {
            AddVariantToActivity(locationActivity, newVariant);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawVariantEntry(LocationActivity locationActivity, int index)
    {
        var variant = locationActivity.ActivityVariants[index];

        if (variant == null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("  [Missing Variant]", EditorStyles.miniLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                RemoveVariantFromActivity(locationActivity, index);
            }

            EditorGUILayout.EndHorizontal();
            return;
        }

        EditorGUILayout.BeginHorizontal();

        // Variant info
        EditorGUILayout.LabelField($"  • {variant.VariantName}", EditorStyles.miniLabel, GUILayout.Width(150));

        if (variant.PrimaryResource != null)
        {
            EditorGUILayout.LabelField($"→ {variant.PrimaryResource.ItemName}", EditorStyles.miniLabel, GUILayout.Width(100));
        }

        EditorGUILayout.LabelField($"{variant.ActionCost} steps", EditorStyles.miniLabel, GUILayout.Width(60));

        GUILayout.FlexibleSpace();

        // Buttons
        if (GUILayout.Button("Edit", GUILayout.Width(40)))
        {
            Selection.activeObject = variant;
            EditorGUIUtility.PingObject(variant);
        }

        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            RemoveVariantFromActivity(locationActivity, index);
        }

        EditorGUILayout.EndHorizontal();
    }

    private List<LocationActivity> GetFilteredActivities()
    {
        if (activityRegistry == null) return new List<LocationActivity>();

        var activities = activityRegistry.AllActivities.Where(a => a?.ActivityReference != null);

        if (!string.IsNullOrEmpty(searchFilter))
        {
            activities = activities.Where(a =>
                a.ActivityReference.GetDisplayName().ToLower().Contains(searchFilter.ToLower()) ||
                a.ActivityReference.ActivityID.ToLower().Contains(searchFilter.ToLower()));
        }

        return activities.ToList();
    }
    #endregion

    #region POI Management Tab (New)
    private void DrawPOIManagementTab()
    {
        if (locationRegistry == null)
        {
            EditorGUILayout.HelpBox("Select a LocationRegistry to manage POI assignments.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh POI List", GUILayout.Width(120)))
        {
            RefreshPOIList();
        }

        if (GUILayout.Button("Find All POIs in Scene", GUILayout.Width(150)))
        {
            FindAllPOIsInScene();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (allPOIs == null || allPOIs.Length == 0)
        {
            EditorGUILayout.HelpBox("No POIs found in the current scene. Make sure your scene contains POI objects.", MessageType.Warning);

            if (GUILayout.Button("Scan Scene for POIs"))
            {
                FindAllPOIsInScene();
            }
            return;
        }

        EditorGUILayout.LabelField($"POIs Found: {allPOIs.Length}", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Filter POIs by search
        var filteredPOIs = FilterPOIs();

        foreach (var poi in filteredPOIs)
        {
            DrawPOIEntry(poi);
        }
    }

    private void DrawPOIEntry(POI poi)
    {
        if (poi == null) return;

        EditorGUILayout.BeginVertical("box");

        // POI Header
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField($"🏛️ {poi.gameObject.name}", EditorStyles.boldLabel, GUILayout.Width(200));
        EditorGUILayout.LabelField($"Location ID: {poi.LocationID}", EditorStyles.miniLabel);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Select POI", GUILayout.Width(80)))
        {
            Selection.activeObject = poi.gameObject;
            EditorGUIUtility.PingObject(poi.gameObject);
        }

        EditorGUILayout.EndHorizontal();

        // Location info
        var location = GetLocationForPOI(poi);
        if (location != null)
        {
            EditorGUILayout.LabelField($"📍 Location: {location.DisplayName}", EditorStyles.miniLabel);

            if (!string.IsNullOrEmpty(location.Description))
            {
                EditorGUILayout.LabelField($"Description: {location.Description}", EditorStyles.wordWrappedMiniLabel);
            }
        }
        else
        {
            EditorGUILayout.LabelField($"❌ Location '{poi.LocationID}' not found in LocationRegistry!", EditorStyles.miniLabel);

            if (GUILayout.Button("Fix Missing Location"))
            {
                CreateMissingLocation(poi.LocationID);
            }
        }

        EditorGUILayout.Space();

        // Activities management
        DrawPOIActivitiesSection(poi, location);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawPOIActivitiesSection(POI poi, MapLocationDefinition location)
    {
        if (location == null) return;

        EditorGUILayout.LabelField("🎯 Available Activities:", EditorStyles.boldLabel);

        // Current activities
        if (location.AvailableActivities != null && location.AvailableActivities.Count > 0)
        {
            for (int i = 0; i < location.AvailableActivities.Count; i++)
            {
                DrawLocationActivityEntry(location, i);
            }
        }
        else
        {
            EditorGUILayout.LabelField("  No activities assigned", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();

        // Add new activity
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Add Activity:", GUILayout.Width(80));

        ActivityDefinition newActivity = (ActivityDefinition)EditorGUILayout.ObjectField(null, typeof(ActivityDefinition), false);

        if (newActivity != null)
        {
            AddActivityToLocation(location, newActivity);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawLocationActivityEntry(MapLocationDefinition location, int activityIndex)
    {
        var locationActivity = location.AvailableActivities[activityIndex];

        if (locationActivity?.ActivityReference == null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("  [Missing Activity Reference]", EditorStyles.miniLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                RemoveActivityFromLocation(location, activityIndex);
            }

            EditorGUILayout.EndHorizontal();
            return;
        }

        var activity = locationActivity.ActivityReference;

        EditorGUILayout.BeginVertical("helpBox");

        // Activity header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"⚡ {activity.GetDisplayName()}", EditorStyles.boldLabel, GUILayout.Width(150));

        locationActivity.IsAvailable = EditorGUILayout.Toggle("Available", locationActivity.IsAvailable, GUILayout.Width(80));

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Edit", GUILayout.Width(40)))
        {
            Selection.activeObject = activity;
            EditorGUIUtility.PingObject(activity);
        }

        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            RemoveActivityFromLocation(location, activityIndex);
        }

        EditorGUILayout.EndHorizontal();

        // Variants management
        EditorGUILayout.LabelField("Variants:", EditorStyles.miniLabel);

        if (locationActivity.ActivityVariants != null && locationActivity.ActivityVariants.Count > 0)
        {
            for (int i = 0; i < locationActivity.ActivityVariants.Count; i++)
            {
                DrawLocationVariantEntry(locationActivity, i);
            }
        }
        else
        {
            EditorGUILayout.LabelField("    No variants assigned", EditorStyles.miniLabel);
        }

        // Add variant
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("    Add Variant:", GUILayout.Width(100));

        ActivityVariant newVariant = (ActivityVariant)EditorGUILayout.ObjectField(null, typeof(ActivityVariant), false);

        if (newVariant != null)
        {
            AddVariantToLocationActivity(locationActivity, newVariant);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawLocationVariantEntry(LocationActivity locationActivity, int variantIndex)
    {
        var variant = locationActivity.ActivityVariants[variantIndex];

        if (variant == null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("    [Missing Variant]", EditorStyles.miniLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                RemoveVariantFromLocationActivity(locationActivity, variantIndex);
            }

            EditorGUILayout.EndHorizontal();
            return;
        }

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField($"    • {variant.VariantName}", EditorStyles.miniLabel, GUILayout.Width(120));

        if (variant.PrimaryResource != null)
        {
            EditorGUILayout.LabelField($"→ {variant.PrimaryResource.ItemName}", EditorStyles.miniLabel, GUILayout.Width(80));
        }

        EditorGUILayout.LabelField($"{variant.ActionCost} steps", EditorStyles.miniLabel, GUILayout.Width(60));

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Edit", GUILayout.Width(40)))
        {
            Selection.activeObject = variant;
            EditorGUIUtility.PingObject(variant);
        }

        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            RemoveVariantFromLocationActivity(locationActivity, variantIndex);
        }

        EditorGUILayout.EndHorizontal();
    }
    #endregion

    #region POI Management Logic
    private void RefreshPOIList()
    {
        FindAllPOIsInScene();
        BuildLocationLookup();
    }

    private void FindAllPOIsInScene()
    {
        allPOIs = FindObjectsOfType<POI>();
        Debug.Log($"ActivityManager: Found {allPOIs.Length} POIs in the current scene");
    }

    private void BuildLocationLookup()
    {
        locationLookup.Clear();

        if (locationRegistry?.AllLocations != null)
        {
            foreach (var location in locationRegistry.AllLocations)
            {
                if (location != null && !string.IsNullOrEmpty(location.LocationID))
                {
                    locationLookup[location.LocationID] = location;
                }
            }
        }
    }

    private POI[] FilterPOIs()
    {
        if (string.IsNullOrEmpty(searchFilter))
            return allPOIs;

        return allPOIs.Where(poi =>
            poi.gameObject.name.ToLower().Contains(searchFilter.ToLower()) ||
            poi.LocationID.ToLower().Contains(searchFilter.ToLower())
        ).ToArray();
    }

    private MapLocationDefinition GetLocationForPOI(POI poi)
    {
        if (poi == null || string.IsNullOrEmpty(poi.LocationID))
            return null;

        locationLookup.TryGetValue(poi.LocationID, out MapLocationDefinition location);
        return location;
    }

    private void CreateMissingLocation(string locationId)
    {
        // This would create a new MapLocationDefinition - for now just log
        Debug.LogWarning($"Feature not implemented: Create new location '{locationId}'");
        EditorUtility.DisplayDialog("Feature Coming Soon",
            $"Creating new locations automatically is not implemented yet.\n\nPlease create a MapLocationDefinition for '{locationId}' manually.", "OK");
    }

    private void AddActivityToLocation(MapLocationDefinition location, ActivityDefinition activity)
    {
        if (location.AvailableActivities == null)
        {
            location.AvailableActivities = new List<LocationActivity>();
        }

        // Check if already exists
        bool alreadyExists = location.AvailableActivities.Any(la =>
            la?.ActivityReference == activity);

        if (alreadyExists)
        {
            EditorUtility.DisplayDialog("Already Added",
                $"Activity '{activity.GetDisplayName()}' is already assigned to this location.", "OK");
            return;
        }

        // Create new LocationActivity
        var newLocationActivity = new LocationActivity
        {
            ActivityReference = activity,
            IsAvailable = true,
            ActivityVariants = new List<ActivityVariant>()
        };

        location.AvailableActivities.Add(newLocationActivity);

        MarkLocationDirty(location);
        Debug.Log($"Added activity '{activity.GetDisplayName()}' to location '{location.DisplayName}'");
    }

    private void RemoveActivityFromLocation(MapLocationDefinition location, int index)
    {
        if (location.AvailableActivities != null && index >= 0 && index < location.AvailableActivities.Count)
        {
            var activityName = location.AvailableActivities[index]?.ActivityReference?.GetDisplayName() ?? "Unknown";
            location.AvailableActivities.RemoveAt(index);

            MarkLocationDirty(location);
            Debug.Log($"Removed activity '{activityName}' from location '{location.DisplayName}'");
        }
    }

    private void AddVariantToLocationActivity(LocationActivity locationActivity, ActivityVariant variant)
    {
        if (locationActivity.ActivityVariants == null)
        {
            locationActivity.ActivityVariants = new List<ActivityVariant>();
        }

        if (locationActivity.ActivityVariants.Contains(variant))
        {
            EditorUtility.DisplayDialog("Already Added",
                $"Variant '{variant.VariantName}' is already assigned to this activity.", "OK");
            return;
        }

        locationActivity.ActivityVariants.Add(variant);
        MarkLocationDirty(FindLocationContaining(locationActivity));
        Debug.Log($"Added variant '{variant.VariantName}' to activity '{locationActivity.ActivityReference.GetDisplayName()}'");
    }

    private void RemoveVariantFromLocationActivity(LocationActivity locationActivity, int index)
    {
        if (locationActivity.ActivityVariants != null && index >= 0 && index < locationActivity.ActivityVariants.Count)
        {
            var variantName = locationActivity.ActivityVariants[index]?.VariantName ?? "Unknown";
            locationActivity.ActivityVariants.RemoveAt(index);

            MarkLocationDirty(FindLocationContaining(locationActivity));
            Debug.Log($"Removed variant '{variantName}' from activity '{locationActivity.ActivityReference.GetDisplayName()}'");
        }
    }

    private MapLocationDefinition FindLocationContaining(LocationActivity locationActivity)
    {
        if (locationRegistry?.AllLocations == null) return null;

        return locationRegistry.AllLocations.FirstOrDefault(loc =>
            loc?.AvailableActivities?.Contains(locationActivity) == true);
    }

    private void MarkLocationDirty(MapLocationDefinition location)
    {
        if (location != null)
        {
            EditorUtility.SetDirty(location);
        }

        if (locationRegistry != null)
        {
            EditorUtility.SetDirty(locationRegistry);
        }

        AssetDatabase.SaveAssets();
    }
    #endregion

    #region Utility Methods
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

        BuildLocationLookup();
    }

    private void ValidateRegistries()
    {
        if (activityRegistry != null)
        {
            activityRegistry.ValidateRegistry();
            Debug.Log("ActivityRegistry validation triggered");
        }

        if (locationRegistry != null)
        {
            locationRegistry.ValidateRegistry();
            Debug.Log("LocationRegistry validation triggered");
        }
    }

    private void AddVariantToActivity(LocationActivity locationActivity, ActivityVariant variant)
    {
        if (locationActivity.ActivityVariants == null)
        {
            locationActivity.ActivityVariants = new List<ActivityVariant>();
        }

        if (locationActivity.ActivityVariants.Contains(variant))
        {
            EditorUtility.DisplayDialog("Already Added", $"Variant '{variant.VariantName}' is already associated with this activity.", "OK");
            return;
        }

        locationActivity.ActivityVariants.Add(variant);

        EditorUtility.SetDirty(activityRegistry);
        AssetDatabase.SaveAssets();

        if (activityRegistry != null)
        {
            activityRegistry.ValidateRegistry();
        }

        Debug.Log($"Added variant '{variant.VariantName}' to activity '{locationActivity.ActivityReference.GetDisplayName()}'");
    }

    private void RemoveVariantFromActivity(LocationActivity locationActivity, int index)
    {
        if (locationActivity.ActivityVariants != null && index >= 0 && index < locationActivity.ActivityVariants.Count)
        {
            var variantName = locationActivity.ActivityVariants[index]?.VariantName ?? "Unknown";
            locationActivity.ActivityVariants.RemoveAt(index);

            EditorUtility.SetDirty(activityRegistry);
            AssetDatabase.SaveAssets();

            if (activityRegistry != null)
            {
                activityRegistry.ValidateRegistry();
            }

            Debug.Log($"Removed variant '{variantName}' from activity '{locationActivity.ActivityReference.GetDisplayName()}'");
        }
    }
    #endregion
}
#endif