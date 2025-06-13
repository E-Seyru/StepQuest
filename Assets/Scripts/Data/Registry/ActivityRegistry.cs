// Purpose: Central registry for all game activities and their variants - ROBUST VERSION
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

    [Header("Robustness Settings")]
    [SerializeField] private bool enableFallbackSearch = true;
    [SerializeField] private bool logMissingVariants = false; // Désactivé par défaut pour éviter le spam

    [Header("Debug Info")]
    [Tooltip("Shows validation errors if any")]
    [TextArea(3, 6)]
    public string ValidationStatus = "Click 'Validate Registry' to check for issues";

    // Cache for fast lookup
    private Dictionary<string, LocationActivity> activityLookup;
    private Dictionary<string, ActivityVariant> variantLookup; // Key format: "activityId_variantId"

    /// <summary>
    /// Get activity by ID (fast lookup via cache) - ROBUST VERSION
    /// </summary>
    public LocationActivity GetActivity(string activityId)
    {
        InitializeLookupCache();

        if (string.IsNullOrEmpty(activityId))
        {
            return null; // Pas de log pour éviter le spam
        }

        // Try to get activity from cache
        if (activityLookup.TryGetValue(activityId, out LocationActivity activity))
        {
            return activity;
        }

        // Fallback: cherche par nom si ID pas trouvé
        if (enableFallbackSearch)
        {
            activity = FindActivityByNameFallback(activityId);
            if (activity != null)
            {
                return activity;
            }
        }

        if (logMissingVariants)
        {
            Logger.LogWarning($"ActivityRegistry: Activity '{activityId}' not found in registry", Logger.LogCategory.General);
        }
        return null;
    }

    /// <summary>
    /// Get activity variant by activity ID and variant ID - ROBUST VERSION
    /// </summary>
    public ActivityVariant GetActivityVariant(string activityId, string variantId)
    {
        InitializeLookupCache();

        if (string.IsNullOrEmpty(activityId) || string.IsNullOrEmpty(variantId))
        {
            return null; // Pas de log pour éviter le spam
        }

        // Create lookup key
        string lookupKey = $"{activityId}_{variantId}";

        // Try cache first
        if (variantLookup.TryGetValue(lookupKey, out ActivityVariant variant))
        {
            return variant;
        }

        // NOUVEAU : Fallback intelligent - cherche par nom
        if (enableFallbackSearch)
        {
            variant = FindVariantByNameFallback(activityId, variantId);
            if (variant != null)
            {
                return variant;
            }
        }

        if (logMissingVariants)
        {
            Logger.LogWarning($"ActivityRegistry: Activity variant '{activityId}/{variantId}' not found in registry", Logger.LogCategory.General);
        }
        return null;
    }

    /// <summary>
    /// Cherche un variant par nom si l'ID exact n'est pas trouvé
    /// </summary>
    private ActivityVariant FindVariantByNameFallback(string activityId, string variantId)
    {
        foreach (var activity in AllActivities?.Where(a => a?.ActivityReference != null) ?? Enumerable.Empty<LocationActivity>())
        {
            if (activity.ActivityReference.ActivityID == activityId && activity.ActivityVariants != null)
            {
                foreach (var variant in activity.ActivityVariants.Where(v => v != null))
                {
                    // Essaie plusieurs variations du nom
                    if (MatchesVariantName(variant, variantId))
                    {
                        return variant;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Cherche une activité par nom si l'ID exact n'est pas trouvé
    /// </summary>
    private LocationActivity FindActivityByNameFallback(string activityId)
    {
        foreach (var activity in AllActivities?.Where(a => a?.ActivityReference != null) ?? Enumerable.Empty<LocationActivity>())
        {
            if (MatchesActivityName(activity.ActivityReference, activityId))
            {
                return activity;
            }
        }
        return null;
    }

    /// <summary>
    /// Matching flexible pour les noms de variants
    /// </summary>
    private bool MatchesVariantName(ActivityVariant variant, string searchName)
    {
        if (variant?.VariantName == null) return false;

        string variantName = variant.VariantName.ToLower().Replace(" ", "_");
        string search = searchName.ToLower().Replace(" ", "_");

        return variantName == search ||
               variant.VariantName.Equals(searchName, System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Matching flexible pour les noms d'activités
    /// </summary>
    private bool MatchesActivityName(ActivityDefinition activity, string searchName)
    {
        if (activity == null) return false;

        return activity.ActivityID.Equals(searchName, System.StringComparison.OrdinalIgnoreCase) ||
               (!string.IsNullOrEmpty(activity.ActivityName) &&
                activity.ActivityName.Equals(searchName, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Initialize lookup cache - ROBUST VERSION qui ignore les nulls silencieusement
    /// </summary>
    private void InitializeLookupCache()
    {
        if (activityLookup != null) return;

        activityLookup = new Dictionary<string, LocationActivity>();
        variantLookup = new Dictionary<string, ActivityVariant>();

        if (AllActivities == null) return;

        foreach (var activity in AllActivities.Where(a => a?.ActivityReference != null))
        {
            var activityRef = activity.ActivityReference;

            // Add to activity lookup
            if (!string.IsNullOrEmpty(activityRef.ActivityID))
            {
                if (!activityLookup.ContainsKey(activityRef.ActivityID))
                {
                    activityLookup[activityRef.ActivityID] = activity;
                }
            }

            // Add variants to lookup - IGNORE silencieusement les nulls
            if (activity.ActivityVariants != null)
            {
                foreach (var variant in activity.ActivityVariants.Where(v => v != null))
                {
                    if (!string.IsNullOrEmpty(variant.VariantName))
                    {
                        // Use variant name as ID (normalized)
                        string variantId = variant.VariantName.ToLower().Replace(" ", "_");
                        string lookupKey = $"{activityRef.ActivityID}_{variantId}";

                        if (!variantLookup.ContainsKey(lookupKey))
                        {
                            variantLookup[lookupKey] = variant;
                        }
                    }
                }
            }
        }
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
    /// Clean null references automatically - SAFE VERSION
    /// </summary>
    public int CleanNullReferences()
    {
        if (AllActivities == null) return 0;

        int removedCount = 0;

        // Clean null LocationActivities
        for (int i = AllActivities.Count - 1; i >= 0; i--)
        {
            if (AllActivities[i] == null || AllActivities[i].ActivityReference == null)
            {
                AllActivities.RemoveAt(i);
                removedCount++;
            }
        }

        // Clean null variants in each activity
        foreach (var activity in AllActivities.Where(a => a?.ActivityVariants != null))
        {
            int beforeCount = activity.ActivityVariants.Count;
            activity.ActivityVariants.RemoveAll(v => v == null);
            removedCount += beforeCount - activity.ActivityVariants.Count;
        }

        if (removedCount > 0)
        {
            RefreshCache();
        }

        return removedCount;
    }

    /// <summary>
    /// Validate all activities in the registry and update status - ROBUST VERSION
    /// </summary>
    [ContextMenu("Validate Registry")]
    public void ValidateRegistry()
    {
        var issues = new List<string>();
        var activityIds = new HashSet<string>();

        // Auto-clean first
        int cleanedCount = CleanNullReferences();
        if (cleanedCount > 0)
        {
            issues.Add($"Auto-cleaned {cleanedCount} null references");
        }

        // Check for null activities
        var nullCount = AllActivities?.Count(activity => activity == null) ?? 0;
        if (nullCount > 0)
        {
            issues.Add($"{nullCount} null activity(ies) in registry");
        }

        // Validate each activity
        foreach (var activity in AllActivities?.Where(a => a != null) ?? Enumerable.Empty<LocationActivity>())
        {
            if (activity.ActivityReference == null)
            {
                issues.Add($"Activity with null reference found");
                continue;
            }

            // Check if activity is valid
            if (!activity.ActivityReference.IsValidActivity())
            {
                issues.Add($"Activity '{activity.ActivityReference.GetDisplayName()}' failed validation");
            }

            // Check for duplicate IDs
            if (!string.IsNullOrEmpty(activity.ActivityReference.ActivityID))
            {
                if (activityIds.Contains(activity.ActivityReference.ActivityID))
                {
                    issues.Add($"Duplicate activity ID: '{activity.ActivityReference.ActivityID}'");
                }
                else
                {
                    activityIds.Add(activity.ActivityReference.ActivityID);
                }
            }

            // Validate variants
            if (activity.ActivityVariants != null)
            {
                foreach (var variant in activity.ActivityVariants.Where(v => v != null))
                {
                    if (!variant.IsValidVariant())
                    {
                        issues.Add($"Variant '{variant.VariantName}' in activity '{activity.ActivityReference.ActivityID}' failed validation");
                    }
                }
            }
        }

        // Update validation status
        if (issues.Count == 0)
        {
            ValidationStatus = $"✅ Registry is valid! ({AllActivities?.Count ?? 0} activities)";
        }
        else
        {
            ValidationStatus = $"⚠️ Issues found:\n• " + string.Join("\n• ", issues);
        }

        // Force refresh cache
        RefreshCache();

        Logger.LogInfo($"ActivityRegistry validation complete. {issues.Count} issues found.", Logger.LogCategory.General);
    }

    /// <summary>
    /// Get all activities (filtered for null references)
    /// </summary>
    public List<LocationActivity> GetAllValidActivities()
    {
        return AllActivities?.Where(a => a?.ActivityReference != null).ToList() ?? new List<LocationActivity>();
    }

    /// <summary>
    /// Generate a normalized variant ID from variant name
    /// </summary>
    public static string GenerateVariantId(string variantName)
    {
        if (string.IsNullOrEmpty(variantName))
            return string.Empty;

        return variantName.ToLower().Replace(" ", "_").Replace("'", "").Replace("-", "_");
    }

    /// <summary>
    /// Runtime initialization - auto-cleans and validates
    /// </summary>
    void OnEnable()
    {
        // Auto-clean silencieusement au démarrage
        CleanNullReferences();
        RefreshCache();
    }
}