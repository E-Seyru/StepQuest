// Purpose: Enhanced ActivityDefinition with auto-registration capabilities - ROBUST VERSION
// Filepath: Assets/Scripts/Data/ScriptableObjects/ActivityDefinition.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Defines the type of activity and how it should be processed
/// </summary>
public enum ActivityType
{
    Harvesting,   // Step-based, produces resources (mining, woodcutting, fishing)
    Crafting,     // Time-based, consumes materials to produce items
    Exploration,  // Step-based, discovers hidden content at locations
    Merchant,     // Buy/sell with NPCs
    Bank          // Storage at bank locations
}

[CreateAssetMenu(fileName = "NewActivity", menuName = "WalkAndRPG/Activity Definition")]
public class ActivityDefinition : ScriptableObject
{
    [Header("Basic Info")]
    public string ActivityID;
    public string ActivityName;
    [TextArea(2, 4)]
    public string BaseDescription;

    [Header("Activity Type")]
    [Tooltip("Determines how this activity is processed and which UI to show")]
    public ActivityType Type = ActivityType.Harvesting;

    [Header("Visual")]
    public Sprite ActivityIcon;
    public Color ActivityColor = Color.white;

    [Header("Categories (for Crafting UI)")]
    [Tooltip("Categories available for this activity. Used to create tabs in CraftingPanel. Variants must have a matching Category to appear under that tab.")]
    public List<CategoryDefinition> AvailableCategories = new List<CategoryDefinition>();

    [Header("Availability")]
    public bool IsAvailable = true;
    public int UnlockRequirement = 0; // Could be skill level, quest completion, etc.

    [Header("Auto-Registration")]
    [Tooltip("Automatically find and register variants with this activity")]
    public bool AutoFindVariants = true;
    [Tooltip("Auto-discovered variants (read-only)")]
    [SerializeField] private List<ActivityVariant> discoveredVariants = new List<ActivityVariant>();

    [Header("Developer Notes")]
    [TextArea(2, 3)]
    public string DeveloperNotes;

    /// <summary>
    /// Get display name for UI
    /// </summary>
    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(ActivityName) ? ActivityID : ActivityName;
    }

    /// <summary>
    /// Get the activity icon or a default one
    /// </summary>
    public Sprite GetIcon()
    {
        return ActivityIcon; // Could return a default icon if null
    }

    /// <summary>
    /// Validate this activity definition
    /// </summary>
    public bool IsValidActivity()
    {
        if (string.IsNullOrEmpty(ActivityID)) return false;
        if (string.IsNullOrEmpty(ActivityName)) return false;
        return true;
    }

    /// <summary>
    /// Alias for IsValidActivity() - for compatibility with existing code
    /// </summary>
    public bool IsValid()
    {
        return IsValidActivity();
    }

    /// <summary>
    /// Check if this is an exploration activity
    /// </summary>
    public bool IsExploration()
    {
        return Type == ActivityType.Exploration;
    }

    /// <summary>
    /// Check if this is a crafting activity
    /// </summary>
    public bool IsCrafting()
    {
        return Type == ActivityType.Crafting;
    }

    /// <summary>
    /// Check if this is a harvesting activity
    /// </summary>
    public bool IsHarvesting()
    {
        return Type == ActivityType.Harvesting;
    }

    /// <summary>
    /// Check if this is a merchant activity
    /// </summary>
    public bool IsMerchant()
    {
        return Type == ActivityType.Merchant;
    }

    /// <summary>
    /// Check if this is a bank activity
    /// </summary>
    public bool IsBank()
    {
        return Type == ActivityType.Bank;
    }

    /// <summary>
    /// Get all variants for this activity (including auto-discovered) - FIXED VERSION
    /// </summary>
    public List<ActivityVariant> GetAllVariants()
    {
        if (AutoFindVariants)
        {
#if UNITY_EDITOR
            RefreshVariantList();
#endif
        }
        return discoveredVariants.Where(v => v != null).ToList();
    }

    /// <summary>
    /// Get activity icon with fallback
    /// </summary>
    public Sprite GetActivityIcon()
    {
        return ActivityIcon;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Auto-registration and validation in editor
    /// </summary>
    void OnValidate()
    {
        // Auto-generate ActivityID if empty
        if (string.IsNullOrEmpty(ActivityID) && !string.IsNullOrEmpty(ActivityName))
        {
            ActivityID = ActivityName.ToLower().Replace(" ", "_").Replace("'", "");
        }

        if (AutoFindVariants)
        {
            RefreshVariantList();
        }

        // Auto-register this activity to the registry
        AutoRegisterToRegistry();
    }

    /// <summary>
    /// Find all variants that reference this activity
    /// </summary>
    private void RefreshVariantList()
    {
        discoveredVariants.Clear();

        // Find all ActivityVariant assets in the project
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ActivityVariant");

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ActivityVariant variant = UnityEditor.AssetDatabase.LoadAssetAtPath<ActivityVariant>(path);

            if (variant != null && variant.GetParentActivityID() == ActivityID)
            {
                discoveredVariants.Add(variant);
            }
        }

        // Sort by name for consistency
        discoveredVariants = discoveredVariants.OrderBy(v => v.VariantName).ToList();

        Logger.LogInfo($"ActivityDefinition: Found {discoveredVariants.Count} variants for {ActivityID}", Logger.LogCategory.General);
    }

    /// <summary>
    /// Auto-register this activity to the ActivityRegistry
    /// </summary>
    private void AutoRegisterToRegistry()
    {
        // Find the ActivityRegistry in the project
        string[] registryGuids = UnityEditor.AssetDatabase.FindAssets("t:ActivityRegistry");

        if (registryGuids.Length == 0)
        {
            Logger.LogWarning($"ActivityDefinition: No ActivityRegistry found for auto-registration of {ActivityID}", Logger.LogCategory.General);
            return;
        }

        if (registryGuids.Length > 1)
        {
            Logger.LogWarning($"ActivityDefinition: Multiple ActivityRegistries found. Using the first one for {ActivityID}", Logger.LogCategory.General);
        }

        string registryPath = UnityEditor.AssetDatabase.GUIDToAssetPath(registryGuids[0]);
        ActivityRegistry registry = UnityEditor.AssetDatabase.LoadAssetAtPath<ActivityRegistry>(registryPath);

        if (registry == null) return;

        // Check if this activity is already in the registry
        bool alreadyExists = registry.AllActivities.Any(a =>
            a.ActivityReference == this ||
            (a.ActivityReference != null && a.ActivityReference.ActivityID == this.ActivityID));

        if (!alreadyExists)
        {
            // Create new LocationActivity entry
            LocationActivity newActivity = new LocationActivity
            {
                ActivityReference = this,
                ActivityVariants = GetAllVariants(),
                IsAvailable = true
            };

            registry.AllActivities.Add(newActivity);

            // Mark the registry as dirty so it saves
            UnityEditor.EditorUtility.SetDirty(registry);

            Logger.LogInfo($"ActivityDefinition: Auto-registered {ActivityID} to ActivityRegistry", Logger.LogCategory.General);
        }
        else
        {
            // Update existing entry with new variants
            var existingActivity = registry.AllActivities.FirstOrDefault(a =>
                a.ActivityReference == this ||
                (a.ActivityReference != null && a.ActivityReference.ActivityID == this.ActivityID));

            if (existingActivity != null)
            {
                existingActivity.ActivityVariants = GetAllVariants();
                UnityEditor.EditorUtility.SetDirty(registry);
            }
        }
    }

    /// <summary>
    /// Context menu to manually refresh variants
    /// </summary>
    [UnityEditor.MenuItem("CONTEXT/ActivityDefinition/Refresh Variants")]
    private static void RefreshVariantsContextMenu(UnityEditor.MenuCommand command)
    {
        ActivityDefinition activity = (ActivityDefinition)command.context;
        activity.RefreshVariantList();
        UnityEditor.EditorUtility.SetDirty(activity);
    }

    /// <summary>
    /// Context menu to force re-register to registry
    /// </summary>
    [UnityEditor.MenuItem("CONTEXT/ActivityDefinition/Force Re-register")]
    private static void ForceReregisterContextMenu(UnityEditor.MenuCommand command)
    {
        ActivityDefinition activity = (ActivityDefinition)command.context;
        activity.AutoRegisterToRegistry();
    }
#endif
}