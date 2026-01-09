// Purpose: Enhanced tool to manage activities and variants with creation capabilities
// Filepath: Assets/Scripts/Editor/ActivityManagerWindow.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ActivityManagerWindow : EditorWindow
{
    [MenuItem("StepQuest/World/Activity Manager")]
    public static void ShowWindow()
    {
        ActivityManagerWindow window = GetWindow<ActivityManagerWindow>();
        window.titleContent = new GUIContent("Activity Manager");
        window.Show();
    }

    /// <summary>
    /// Opens the Activity Manager window and selects the specified activity definition
    /// </summary>
    public static void ShowWindowAndSelect(ActivityDefinition activity)
    {
        var window = GetWindow<ActivityManagerWindow>();
        window.titleContent = new GUIContent("Activity Manager");
        window.Show();

        if (activity != null)
        {
            window.searchFilter = activity.ActivityName; // Set search to find the activity
            window.Repaint();

            // Also select in Unity's Project window
            Selection.activeObject = activity;
            EditorGUIUtility.PingObject(activity);
        }
    }

    // Data
    private ActivityRegistry activityRegistry;
    private LocationRegistry locationRegistry;

    // UI State
    private Vector2 scrollPosition;
    private string searchFilter = "";

    // Location lookup for variant synchronization
    private Dictionary<string, MapLocationDefinition> locationLookup = new Dictionary<string, MapLocationDefinition>();

    // Creation Dialog State
    private bool showCreateActivityDialog = false;
    private bool showCreateVariantDialog = false;

    private string newActivityName = "";
    private string newActivityDescription = "";
    private ActivityType newActivityType = ActivityType.Harvesting;
    private Sprite newActivityIcon = null;
    private Sprite newActivitySilhouetteIcon = null;
    private Color newActivityColor = Color.white;
    private string newVariantName = "";
    private string newVariantDescription = "";

    private ActivityDefinition targetActivityForVariant = null;

    void OnEnable()
    {
        LoadRegistries();
    }

    void OnGUI()
    {
        DrawHeader();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DrawActivitiesTab();
        EditorGUILayout.EndScrollView();

        // Handle creation dialogs
        HandleCreationDialogs();
    }

    #region Creation Dialogs
    private void HandleCreationDialogs()
    {
        if (showCreateActivityDialog)
            DrawCreateActivityDialog();

        if (showCreateVariantDialog)
            DrawCreateVariantDialog();
    }

    private void DrawCreateActivityDialog()
    {
        GUILayout.BeginArea(new Rect(50, 100, 400, 420));
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField("Create New Activity", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Activity Name:");
        newActivityName = EditorGUILayout.TextField(newActivityName);

        EditorGUILayout.LabelField("Activity Type:");
        newActivityType = (ActivityType)EditorGUILayout.EnumPopup(newActivityType);

        // Show type description
        string typeDescription = newActivityType switch
        {
            ActivityType.Harvesting => "Step-based gathering (mining, woodcutting, fishing)",
            ActivityType.Crafting => "Time-based crafting (forging, cooking)",
            ActivityType.Exploration => "Step-based discovery of hidden content",
            ActivityType.Merchant => "Buy/sell with NPCs",
            ActivityType.Bank => "Storage management",
            _ => ""
        };
        EditorGUILayout.LabelField(typeDescription, EditorStyles.miniLabel);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Description:");
        newActivityDescription = EditorGUILayout.TextArea(newActivityDescription, GUILayout.Height(60));

        EditorGUILayout.Space();

        // Visual fields
        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        newActivityIcon = (Sprite)EditorGUILayout.ObjectField("Activity Icon", newActivityIcon, typeof(Sprite), false);
        newActivitySilhouetteIcon = (Sprite)EditorGUILayout.ObjectField("Silhouette Icon", newActivitySilhouetteIcon, typeof(Sprite), false);
        EditorGUILayout.HelpBox("Silhouette Icon is shown for undiscovered activities. Falls back to Activity Icon if not set.", MessageType.None);
        newActivityColor = EditorGUILayout.ColorField("Activity Color", newActivityColor);

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create"))
        {
            if (!string.IsNullOrEmpty(newActivityName))
            {
                CreateNewActivity(newActivityName, newActivityDescription);
                ResetCreateActivityDialog();
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Input", "Activity name cannot be empty.", "OK");
            }
        }

        if (GUILayout.Button("Cancel"))
        {
            ResetCreateActivityDialog();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawCreateVariantDialog()
    {
        GUILayout.BeginArea(new Rect(50, 100, 400, 250));
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField("Create New Variant", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Target Activity:");
        EditorGUILayout.LabelField(targetActivityForVariant?.GetDisplayName() ?? "None selected", EditorStyles.miniLabel);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Variant Name:");
        newVariantName = EditorGUILayout.TextField(newVariantName);

        EditorGUILayout.LabelField("Description:");
        newVariantDescription = EditorGUILayout.TextArea(newVariantDescription, GUILayout.Height(60));

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create"))
        {
            if (!string.IsNullOrEmpty(newVariantName) && targetActivityForVariant != null)
            {
                CreateNewVariant(targetActivityForVariant, newVariantName, newVariantDescription);
                ResetCreateVariantDialog();
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Input", "Variant name cannot be empty and target activity must be selected.", "OK");
            }
        }

        if (GUILayout.Button("Cancel"))
        {
            ResetCreateVariantDialog();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void ResetCreateActivityDialog()
    {
        showCreateActivityDialog = false;
        newActivityName = "";
        newActivityDescription = "";
        newActivityType = ActivityType.Harvesting;
        newActivityIcon = null;
        newActivitySilhouetteIcon = null;
        newActivityColor = Color.white;
    }

    private void ResetCreateVariantDialog()
    {
        showCreateVariantDialog = false;
        newVariantName = "";
        newVariantDescription = "";
        targetActivityForVariant = null;
    }
    #endregion

    #region Creation Logic
    private void CreateNewActivity(string activityName, string description)
    {
        // Generate ActivityID from name
        string activityID = GenerateIDFromName(activityName);

        // Create the ScriptableObject
        ActivityDefinition newActivity = CreateInstance<ActivityDefinition>();
        newActivity.ActivityName = activityName;
        newActivity.ActivityID = activityID;
        newActivity.BaseDescription = description;
        newActivity.Type = newActivityType;
        newActivity.IsAvailable = true;
        newActivity.ActivityIcon = newActivityIcon;
        newActivity.SilhouetteIcon = newActivitySilhouetteIcon;
        newActivity.ActivityColor = newActivityColor;

        // Save to appropriate folder
        string assetPath = $"Assets/ScriptableObjects/Activities/{activityName}.asset";
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        AssetDatabase.CreateAsset(newActivity, assetPath);
        AssetDatabase.SaveAssets();

        // Create folder for variants
        string variantFolderPath = $"Assets/ScriptableObjects/Activities/ActivitiesVariant/{activityName}";
        if (!AssetDatabase.IsValidFolder(variantFolderPath))
        {
            AssetDatabase.CreateFolder("Assets/ScriptableObjects/Activities/ActivitiesVariant", activityName);
        }

        // Create LocationActivity wrapper and add to registry
        if (activityRegistry != null)
        {
            LocationActivity locationActivity = new LocationActivity
            {
                ActivityReference = newActivity,
                IsAvailable = true,
                ActivityVariants = new List<ActivityVariant>()
            };

            // Automatically find and assign existing variants that match this activity
            AssignExistingVariants(locationActivity, activityID);

            activityRegistry.AllActivities.Add(locationActivity);
            EditorUtility.SetDirty(activityRegistry);
            AssetDatabase.SaveAssets();
        }

        // Select the new activity
        Selection.activeObject = newActivity;
        EditorGUIUtility.PingObject(newActivity);

        Logger.LogInfo($"Created new activity: {activityName} (ID: {activityID}, Logger.LogCategory.EditorLog) with variant folder");
        LoadRegistries(); // Refresh
    }

    private void CreateNewVariant(ActivityDefinition parentActivity, string variantName, string description)
    {
        // Create the variant ScriptableObject
        ActivityVariant newVariant = CreateInstance<ActivityVariant>();
        newVariant.VariantName = variantName;
        newVariant.VariantDescription = description;
        // Note: L'utilisateur peut configurer le reste dans l'Inspector

        // Save to appropriate folder (inside the parent activity's folder)
        string activityFolderPath = $"Assets/ScriptableObjects/Activities/ActivitiesVariant/{parentActivity.ActivityName}";

        // Create folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(activityFolderPath))
        {
            AssetDatabase.CreateFolder("Assets/ScriptableObjects/Activities/ActivitiesVariant", parentActivity.ActivityName);
        }

        string assetPath = $"{activityFolderPath}/{variantName}.asset";
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        AssetDatabase.CreateAsset(newVariant, assetPath);
        AssetDatabase.SaveAssets();

        // Select the new variant for editing
        Selection.activeObject = newVariant;
        EditorGUIUtility.PingObject(newVariant);

        Logger.LogInfo($"Created new variant: {variantName} for activity {parentActivity.GetDisplayName()} in folder {activityFolderPath}", Logger.LogCategory.EditorLog);
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

    private void AssignExistingVariants(LocationActivity locationActivity, string activityID)
    {
        // Find all ActivityVariant assets in the project
        string[] variantGuids = AssetDatabase.FindAssets("t:ActivityVariant");
        int assignedCount = 0;

        foreach (string guid in variantGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ActivityVariant variant = AssetDatabase.LoadAssetAtPath<ActivityVariant>(path);

            if (variant != null && variant.GetParentActivityID() == activityID)
            {
                // Check if not already in the list
                if (!locationActivity.ActivityVariants.Contains(variant))
                {
                    locationActivity.ActivityVariants.Add(variant);
                    assignedCount++;
                    Logger.LogInfo($"Auto-assigned existing variant '{variant.VariantName}' to activity '{activityID}'", Logger.LogCategory.EditorLog);
                }
            }
        }

        // NOUVEAU : Aussi synchroniser ces variants avec toutes les autres locations qui utilisent cette activite
        if (assignedCount > 0 && locationRegistry != null)
        {
            foreach (var variant in locationActivity.ActivityVariants)
            {
                SynchronizeVariantAcrossAllLocations(locationActivity.ActivityReference, variant, true);
            }
        }

        if (assignedCount > 0)
        {
            Logger.LogInfo($"Successfully auto-assigned {assignedCount} existing variants to activity '{activityID}' and synchronized across all locations", Logger.LogCategory.EditorLog);
        }
        else
        {
            Logger.LogInfo($"No existing variants found for activity '{activityID}'", Logger.LogCategory.EditorLog);
        }
    }
    #endregion

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

    #region Activities Tab (Enhanced)
    private void DrawActivitiesTab()
    {
        if (activityRegistry == null)
        {
            EditorGUILayout.HelpBox("Select an ActivityRegistry to manage activities.", MessageType.Info);
            return;
        }

        // Create New Activity button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create New Activity", GUILayout.Width(150)))
        {
            showCreateActivityDialog = true;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

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

        // Show activity type with color coding
        string typeLabel = activity.Type.ToString();
        Color typeColor = activity.Type switch
        {
            ActivityType.Harvesting => new Color(0.4f, 0.8f, 0.4f), // Green
            ActivityType.Crafting => new Color(0.8f, 0.6f, 0.2f),   // Orange
            ActivityType.Exploration => new Color(0.4f, 0.6f, 0.9f), // Blue
            ActivityType.Merchant => new Color(0.9f, 0.8f, 0.2f),   // Yellow
            ActivityType.Bank => new Color(0.7f, 0.7f, 0.7f),       // Gray
            _ => Color.white
        };
        GUIStyle typeStyle = new GUIStyle(EditorStyles.miniLabel);
        typeStyle.normal.textColor = typeColor;
        EditorGUILayout.LabelField($"[{typeLabel}]", typeStyle, GUILayout.Width(80));

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
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Associated Variants:", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create Variant", GUILayout.Width(100)))
        {
            targetActivityForVariant = activity;
            showCreateVariantDialog = true;
        }
        EditorGUILayout.EndHorizontal();

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

        // Add existing variant
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Add Existing Variant:", GUILayout.Width(130));

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
            Logger.LogInfo("ActivityRegistry validation triggered", Logger.LogCategory.EditorLog);
        }

        if (locationRegistry != null)
        {
            locationRegistry.ValidateRegistry();
            Logger.LogInfo("LocationRegistry validation triggered", Logger.LogCategory.EditorLog);
        }
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
                    if (!locationLookup.ContainsKey(location.LocationID))
                    {
                        locationLookup[location.LocationID] = location;
                    }
                }
            }
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

        // NOUVEAU : Synchroniser avec toutes les autres LocationActivity qui utilisent la meme ActivityDefinition
        SynchronizeVariantAcrossAllLocations(locationActivity.ActivityReference, variant, true);

        EditorUtility.SetDirty(activityRegistry);
        AssetDatabase.SaveAssets();

        if (activityRegistry != null)
        {
            activityRegistry.ValidateRegistry();
        }

        Logger.LogInfo($"Added variant '{variant.VariantName}' to activity '{locationActivity.ActivityReference.GetDisplayName()}' and synchronized across all locations", Logger.LogCategory.EditorLog);
    }

    private void RemoveVariantFromActivity(LocationActivity locationActivity, int index)
    {
        if (locationActivity.ActivityVariants != null && index >= 0 && index < locationActivity.ActivityVariants.Count)
        {
            var variantName = locationActivity.ActivityVariants[index]?.VariantName ?? "Unknown";
            var variant = locationActivity.ActivityVariants[index];
            locationActivity.ActivityVariants.RemoveAt(index);

            // NOUVEAU : Synchroniser la suppression avec toutes les autres LocationActivity
            if (variant != null)
            {
                SynchronizeVariantAcrossAllLocations(locationActivity.ActivityReference, variant, false);
            }

            EditorUtility.SetDirty(activityRegistry);
            AssetDatabase.SaveAssets();

            if (activityRegistry != null)
            {
                activityRegistry.ValidateRegistry();
            }

            Logger.LogInfo($"Removed variant '{variantName}' from activity '{locationActivity.ActivityReference.GetDisplayName()}' and synchronized across all locations", Logger.LogCategory.EditorLog);
        }
    }

    /// <summary>
    /// Synchronise l'ajout/suppression d'un variant a travers toutes les LocationActivity qui utilisent la meme ActivityDefinition
    /// </summary>
    private void SynchronizeVariantAcrossAllLocations(ActivityDefinition activityDef, ActivityVariant variant, bool add)
    {
        if (activityDef == null || variant == null || locationRegistry == null) return;

        int syncCount = 0;

        // Parcourir tous les MapLocationDefinition du LocationRegistry
        foreach (var location in locationRegistry.AllLocations.Where(l => l != null))
        {
            if (location.AvailableActivities == null) continue;

            // Trouver les LocationActivity qui utilisent cette ActivityDefinition
            foreach (var locActivity in location.AvailableActivities.Where(la => la?.ActivityReference == activityDef))
            {
                if (locActivity.ActivityVariants == null)
                {
                    locActivity.ActivityVariants = new List<ActivityVariant>();
                }

                if (add)
                {
                    // Ajouter le variant s'il n'existe pas deja
                    if (!locActivity.ActivityVariants.Contains(variant))
                    {
                        locActivity.ActivityVariants.Add(variant);
                        syncCount++;
                        EditorUtility.SetDirty(location);
                    }
                }
                else
                {
                    // Supprimer le variant s'il existe
                    if (locActivity.ActivityVariants.Remove(variant))
                    {
                        syncCount++;
                        EditorUtility.SetDirty(location);
                    }
                }
            }
        }

        if (syncCount > 0)
        {
            AssetDatabase.SaveAssets();
            Logger.LogInfo($"Synchronized variant '{variant.VariantName}' across {syncCount} location activities", Logger.LogCategory.EditorLog);
        }
    }
    #endregion
}
#endif