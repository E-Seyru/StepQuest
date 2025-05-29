// Purpose: Central registry for all game activities and their variants
// Filepath: Assets/Scripts/Data/Registry/ActivityRegistry.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "ActivityRegistry", menuName = "WalkAndRPG/Activity Registry")]
public class ActivityRegistry : ScriptableObject
{
    [Header("All Game Activities")]
    [Tooltip("Drag all your LocationActivity assets here")]
    public List<LocationActivity> AllActivities = new List<LocationActivity>();

    [Header("Debug Info")]
    [Tooltip("Shows validation errors if any")]
    [TextArea(3, 6)]
    public string ValidationStatus = "Click 'Validate Registry' to check for issues";

    // Cache for fast lookup
    private Dictionary<string, LocationActivity> activityLookup;
    private Dictionary<string, ActivityVariant> variantLookup; // Key format: "activityId_variantId"

    /// <summary>
    /// Get activity by ID (fast lookup via cache)
    /// </summary>
    public LocationActivity GetActivity(string activityId)
    {
        // Initialize cache if needed
        if (activityLookup == null)
        {
            InitializeLookupCache();
        }

        if (string.IsNullOrEmpty(activityId))
        {
            Logger.LogError("ActivityRegistry: GetActivity called with null/empty activityId", Logger.LogCategory.General);
            return null;
        }

        // Try to get activity from cache
        if (activityLookup.TryGetValue(activityId, out LocationActivity activity))
        {
            return activity;
        }

        Logger.LogWarning($"ActivityRegistry: Activity '{activityId}' not found in registry", Logger.LogCategory.General);
        return null;
    }

    /// <summary>
    /// Get activity variant by activity ID and variant ID
    /// </summary>
    public ActivityVariant GetActivityVariant(string activityId, string variantId)
    {
        // Initialize cache if needed
        if (variantLookup == null)
        {
            InitializeLookupCache();
        }

        if (string.IsNullOrEmpty(activityId) || string.IsNullOrEmpty(variantId))
        {
            Logger.LogError("ActivityRegistry: GetActivityVariant called with null/empty parameters", Logger.LogCategory.General);
            return null;
        }

        // Create lookup key
        string lookupKey = $"{activityId}_{variantId}";

        // Try to get variant from cache
        if (variantLookup.TryGetValue(lookupKey, out ActivityVariant variant))
        {
            return variant;
        }

        Logger.LogWarning($"ActivityRegistry: Activity variant '{activityId}/{variantId}' not found in registry", Logger.LogCategory.General);
        return null;
    }

    /// <summary>
    /// Check if an activity exists in registry
    /// </summary>
    public bool HasActivity(string activityId)
    {
        return GetActivity(activityId) != null;
    }

    /// <summary>
    /// Check if an activity variant exists in registry
    /// </summary>
    public bool HasActivityVariant(string activityId, string variantId)
    {
        return GetActivityVariant(activityId, variantId) != null;
    }

    /// <summary>
    /// Get all variants for a specific activity
    /// </summary>
    public List<ActivityVariant> GetVariantsForActivity(string activityId)
    {
        var activity = GetActivity(activityId);
        if (activity == null) return new List<ActivityVariant>();

        return activity.ActivityVariants?.Where(v => v != null && v.IsValidVariant()).ToList() ?? new List<ActivityVariant>();
    }

    /// <summary>
    /// Get all available activities (those that are valid and available)
    /// </summary>
    public List<LocationActivity> GetAvailableActivities()
    {
        return AllActivities.Where(activity => activity != null && activity.IsValidActivity()).ToList();
    }

    /// <summary>
    /// Initialize the lookup cache for fast access
    /// </summary>
    private void InitializeLookupCache()
    {
        activityLookup = new Dictionary<string, LocationActivity>();
        variantLookup = new Dictionary<string, ActivityVariant>();

        foreach (var activity in AllActivities)
        {
            if (activity != null && !string.IsNullOrEmpty(activity.ActivityId))
            {
                // Add activity to lookup
                if (!activityLookup.ContainsKey(activity.ActivityId))
                {
                    activityLookup[activity.ActivityId] = activity;
                }
                else
                {
                    Logger.LogError($"ActivityRegistry: Duplicate ActivityId '{activity.ActivityId}' found! Check your LocationActivity assets.", Logger.LogCategory.General);
                }

                // Add variants to lookup
                if (activity.ActivityVariants != null)
                {
                    foreach (var variant in activity.ActivityVariants)
                    {
                        if (variant != null && !string.IsNullOrEmpty(variant.VariantName))
                        {
                            // Use variant name as ID (you might want to add a VariantId field to ActivityVariant)
                            string variantId = variant.VariantName.ToLower().Replace(" ", "_");
                            string lookupKey = $"{activity.ActivityId}_{variantId}";

                            if (!variantLookup.ContainsKey(lookupKey))
                            {
                                variantLookup[lookupKey] = variant;
                            }
                            else
                            {
                                Logger.LogError($"ActivityRegistry: Duplicate variant key '{lookupKey}' found!", Logger.LogCategory.General);
                            }
                        }
                    }
                }
            }
        }

        Logger.LogInfo($"ActivityRegistry: Cache initialized with {activityLookup.Count} activities and {variantLookup.Count} variants", Logger.LogCategory.General);
    }

    /// <summary>
    /// Force refresh the lookup cache (call this if you modify activities at runtime)
    /// </summary>
    public void RefreshCache()
    {
        activityLookup = null;
        variantLookup = null;
        InitializeLookupCache();
    }

    /// <summary>
    /// Validate all activities in the registry and update status
    /// </summary>
    [ContextMenu("Validate Registry")]
    public void ValidateRegistry()
    {
        var issues = new List<string>();
        var activityIds = new HashSet<string>();

        // Check for null activities
        var nullCount = AllActivities.Count(activity => activity == null);
        if (nullCount > 0)
        {
            issues.Add($"{nullCount} null activity(ies) in registry");
        }

        // Validate each activity
        foreach (var activity in AllActivities)
        {
            if (activity == null) continue;

            // Check if activity is valid
            if (!activity.IsValidActivity())
            {
                issues.Add($"Activity '{activity.GetDisplayName()}' failed validation");
            }

            // Check for duplicate IDs
            if (!string.IsNullOrEmpty(activity.ActivityId))
            {
                if (activityIds.Contains(activity.ActivityId))
                {
                    issues.Add($"Duplicate ActivityId: '{activity.ActivityId}'");
                }
                else
                {
                    activityIds.Add(activity.ActivityId);
                }
            }
            else
            {
                issues.Add($"Activity '{activity.GetDisplayName()}' has empty ActivityId");
            }

            // Validate variants
            if (activity.ActivityVariants != null)
            {
                var variantNames = new HashSet<string>();
                foreach (var variant in activity.ActivityVariants)
                {
                    if (variant == null)
                    {
                        issues.Add($"Activity '{activity.ActivityId}' has null variant");
                        continue;
                    }

                    if (!variant.IsValidVariant())
                    {
                        issues.Add($"Activity '{activity.ActivityId}' has invalid variant '{variant.VariantName}'");
                    }

                    // Check for duplicate variant names within activity
                    if (!string.IsNullOrEmpty(variant.VariantName))
                    {
                        if (variantNames.Contains(variant.VariantName))
                        {
                            issues.Add($"Activity '{activity.ActivityId}' has duplicate variant name: '{variant.VariantName}'");
                        }
                        else
                        {
                            variantNames.Add(variant.VariantName);
                        }
                    }
                }
            }
            else
            {
                issues.Add($"Activity '{activity.ActivityId}' has no variants");
            }
        }

        // Update validation status
        if (issues.Count == 0)
        {
            ValidationStatus = $"✅ Registry validation passed!\n" +
                             $"Found {AllActivities.Count(a => a != null)} valid activity(ies).\n" +
                             $"Total variants: {GetTotalVariantCount()}";
        }
        else
        {
            ValidationStatus = $"❌ Registry validation failed ({issues.Count} issue(s)):\n\n" +
                             string.Join("\n", issues.Take(10)); // Limit to first 10 issues

            if (issues.Count > 10)
            {
                ValidationStatus += $"\n... and {issues.Count - 10} more issue(s)";
            }
        }

        Logger.LogInfo($"ActivityRegistry: Validation complete. {issues.Count} issue(s) found.", Logger.LogCategory.General);

        // Refresh cache after validation
        RefreshCache();
    }

    /// <summary>
    /// Get total number of variants across all activities
    /// </summary>
    private int GetTotalVariantCount()
    {
        int count = 0;
        foreach (var activity in AllActivities)
        {
            if (activity?.ActivityVariants != null)
            {
                count += activity.ActivityVariants.Count(v => v != null);
            }
        }
        return count;
    }

    /// <summary>
    /// Get debug info about the registry
    /// </summary>
    public string GetDebugInfo()
    {
        var validActivities = AllActivities.Count(a => a != null);
        var totalActivities = AllActivities.Count;
        var totalVariants = GetTotalVariantCount();

        return $"ActivityRegistry: {validActivities}/{totalActivities} valid activities loaded\n" +
               $"Cache: {(activityLookup?.Count ?? 0)} activities, {(variantLookup?.Count ?? 0)} variants\n" +
               $"Total variants: {totalVariants}";
    }

    /// <summary>
    /// Helper method to generate variant ID from variant name (for consistent lookup)
    /// </summary>
    public static string GenerateVariantId(string variantName)
    {
        if (string.IsNullOrEmpty(variantName)) return "";
        return variantName.ToLower().Replace(" ", "_").Replace("'", "");
    }

    /// <summary>
    /// Get all activities that can be performed at a specific location
    /// </summary>
    public List<LocationActivity> GetActivitiesForLocation(string locationId)
    {
        // This would require integration with your location system
        // For now, return all available activities
        return GetAvailableActivities();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-validate when something changes in the editor
        if (AllActivities != null && AllActivities.Count > 0)
        {
            // Don't auto-validate too often to avoid performance issues
            UnityEditor.EditorApplication.delayCall += () => ValidateRegistry();
        }
    }
#endif
}