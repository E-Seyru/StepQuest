// Purpose: Central registry for all status effects, provides fast lookup and validation
// Filepath: Assets/Scripts/Data/Registry/StatusEffectRegistry.cs

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Central registry for all status effects in the game.
/// Provides fast lookup by ID and various filtering methods.
/// Follows the same pattern as ItemRegistry.
/// </summary>
[CreateAssetMenu(fileName = "StatusEffectRegistry", menuName = "WalkAndRPG/Registries/Status Effect Registry")]
public class StatusEffectRegistry : ScriptableObject
{
    /// <summary>
    /// Singleton instance for global access
    /// </summary>
    public static StatusEffectRegistry Instance { get; private set; }

    [Header("All Status Effects")]
    [Tooltip("Drag all your StatusEffectDefinition assets here")]
    public List<StatusEffectDefinition> AllEffects = new List<StatusEffectDefinition>();

    [Header("Robustness Settings")]
    [SerializeField] private bool enableFallbackSearch = true;
    [SerializeField] private bool logMissingEffects = false;

    [Header("Debug Info")]
    [Tooltip("Shows validation errors if any")]
    [TextArea(3, 6)]
    public string ValidationStatus = "Click 'Validate Registry' to check for issues";

    // Cache for fast lookup
    private Dictionary<string, StatusEffectDefinition> effectLookup;
    private Dictionary<StatusEffectType, List<StatusEffectDefinition>> effectsByType;
    private HashSet<string> loggedMissingEffects = new HashSet<string>();
    private const int MaxLoggedMissing = 100;

    // === INITIALIZATION ===

    /// <summary>
    /// Initialize the registry as singleton instance
    /// </summary>
    public void Initialize()
    {
        Instance = this;
        RefreshCache();
        Logger.LogInfo($"StatusEffectRegistry: Initialized with {AllEffects?.Count ?? 0} effects", Logger.LogCategory.General);
    }

    /// <summary>
    /// Runtime initialization - auto-cleans and validates
    /// </summary>
    void OnEnable()
    {
        CleanNullReferences();
        RefreshCache();

        // Set as instance if not already set
        if (Instance == null)
        {
            Instance = this;
        }
    }

    // === PUBLIC LOOKUP METHODS ===

    /// <summary>
    /// Get status effect by ID (fast lookup via cache)
    /// </summary>
    public StatusEffectDefinition GetEffect(string effectId)
    {
        if (effectLookup == null)
        {
            InitializeLookupCache();
        }

        if (string.IsNullOrEmpty(effectId))
        {
            return null;
        }

        // Try to get effect from cache
        if (effectLookup.TryGetValue(effectId, out StatusEffectDefinition effect))
        {
            return effect;
        }

        // Fallback search by name
        if (enableFallbackSearch)
        {
            effect = FindEffectByNameFallback(effectId);
            if (effect != null)
            {
                return effect;
            }
        }

        // Log once per missing effect (with cache limit to prevent unbounded growth)
        if (logMissingEffects && !loggedMissingEffects.Contains(effectId) && loggedMissingEffects.Count < MaxLoggedMissing)
        {
            Logger.LogWarning($"StatusEffectRegistry: Effect '{effectId}' not found in registry", Logger.LogCategory.General);
            loggedMissingEffects.Add(effectId);
        }

        return null;
    }

    /// <summary>
    /// Check if an effect exists in registry
    /// </summary>
    public bool HasEffect(string effectId)
    {
        return GetEffect(effectId) != null;
    }

    /// <summary>
    /// Get all effects of a specific type
    /// </summary>
    public List<StatusEffectDefinition> GetEffectsByType(StatusEffectType effectType)
    {
        if (effectsByType == null)
        {
            InitializeLookupCache();
        }

        if (effectsByType.TryGetValue(effectType, out var effects))
        {
            return new List<StatusEffectDefinition>(effects);
        }

        return new List<StatusEffectDefinition>();
    }

    /// <summary>
    /// Get all damage-over-time effects
    /// </summary>
    public List<StatusEffectDefinition> GetDamageOverTimeEffects()
    {
        return AllEffects?.Where(e => e != null && e.IsDamageOverTime).ToList()
            ?? new List<StatusEffectDefinition>();
    }

    /// <summary>
    /// Get all heal-over-time effects
    /// </summary>
    public List<StatusEffectDefinition> GetHealOverTimeEffects()
    {
        return AllEffects?.Where(e => e != null && e.IsHealOverTime).ToList()
            ?? new List<StatusEffectDefinition>();
    }

    /// <summary>
    /// Get all buff effects
    /// </summary>
    public List<StatusEffectDefinition> GetBuffs()
    {
        return AllEffects?.Where(e => e != null && e.IsBuff).ToList()
            ?? new List<StatusEffectDefinition>();
    }

    /// <summary>
    /// Get all debuff effects
    /// </summary>
    public List<StatusEffectDefinition> GetDebuffs()
    {
        return AllEffects?.Where(e => e != null && e.IsDebuff).ToList()
            ?? new List<StatusEffectDefinition>();
    }

    /// <summary>
    /// Get all control effects (stun, etc.)
    /// </summary>
    public List<StatusEffectDefinition> GetControlEffects()
    {
        return AllEffects?.Where(e => e != null && e.IsControlEffect).ToList()
            ?? new List<StatusEffectDefinition>();
    }

    /// <summary>
    /// Get all valid effects (filtered for null references)
    /// </summary>
    public List<StatusEffectDefinition> GetAllValidEffects()
    {
        return AllEffects?.Where(e => e != null).ToList()
            ?? new List<StatusEffectDefinition>();
    }

    // === CACHE MANAGEMENT ===

    /// <summary>
    /// Initialize the lookup caches for fast access
    /// </summary>
    private void InitializeLookupCache()
    {
        effectLookup = new Dictionary<string, StatusEffectDefinition>();
        effectsByType = new Dictionary<StatusEffectType, List<StatusEffectDefinition>>();

        if (AllEffects == null) return;

        foreach (var effect in AllEffects.Where(e => e != null))
        {
            // Add to ID lookup
            if (!string.IsNullOrEmpty(effect.EffectID))
            {
                if (!effectLookup.ContainsKey(effect.EffectID))
                {
                    effectLookup[effect.EffectID] = effect;
                }
                else
                {
                    Logger.LogError($"StatusEffectRegistry: Duplicate EffectID '{effect.EffectID}' found!", Logger.LogCategory.General);
                }
            }

            // Add to type lookup
            if (!effectsByType.ContainsKey(effect.EffectType))
            {
                effectsByType[effect.EffectType] = new List<StatusEffectDefinition>();
            }
            effectsByType[effect.EffectType].Add(effect);
        }

        Logger.LogInfo($"StatusEffectRegistry: Cache initialized with {effectLookup.Count} effects", Logger.LogCategory.General);
    }

    /// <summary>
    /// Force refresh the lookup cache
    /// </summary>
    public void RefreshCache()
    {
        effectLookup = null;
        effectsByType = null;
        loggedMissingEffects.Clear();
        InitializeLookupCache();
    }

    /// <summary>
    /// Fallback search by name if ID not found
    /// </summary>
    private StatusEffectDefinition FindEffectByNameFallback(string effectId)
    {
        foreach (var effect in AllEffects?.Where(e => e != null) ?? Enumerable.Empty<StatusEffectDefinition>())
        {
            if (MatchesEffectName(effect, effectId))
            {
                return effect;
            }
        }
        return null;
    }

    /// <summary>
    /// Flexible matching for effect names
    /// </summary>
    private bool MatchesEffectName(StatusEffectDefinition effect, string searchName)
    {
        if (effect?.EffectID == null) return false;

        string effectId = effect.EffectID.ToLower().Replace(" ", "_");
        string effectDisplayName = effect.EffectName?.ToLower().Replace(" ", "_") ?? "";
        string search = searchName.ToLower().Replace(" ", "_");

        return effectId == search ||
               effectDisplayName == search ||
               effectId.Contains(search) ||
               search.Contains(effectId);
    }

    // === MAINTENANCE ===

    /// <summary>
    /// Clean null references automatically
    /// </summary>
    public int CleanNullReferences()
    {
        if (AllEffects == null) return 0;

        int removedCount = 0;

        for (int i = AllEffects.Count - 1; i >= 0; i--)
        {
            if (AllEffects[i] == null)
            {
                AllEffects.RemoveAt(i);
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            RefreshCache();
        }

        return removedCount;
    }

    /// <summary>
    /// Validate all effects in the registry and update status
    /// </summary>
    [ContextMenu("Validate Registry")]
    public void ValidateRegistry()
    {
        var issues = new List<string>();
        var effectIds = new HashSet<string>();

        // Auto-clean first
        int cleanedCount = CleanNullReferences();
        if (cleanedCount > 0)
        {
            issues.Add($"Auto-cleaned {cleanedCount} null references");
        }

        // Check for null effects
        var nullCount = AllEffects?.Count(e => e == null) ?? 0;
        if (nullCount > 0)
        {
            issues.Add($"{nullCount} null effect(s) in registry");
        }

        // Validate each effect
        foreach (var effect in AllEffects?.Where(e => e != null) ?? Enumerable.Empty<StatusEffectDefinition>())
        {
            // Check if effect definition itself is valid
            if (!effect.IsValid())
            {
                issues.Add($"Effect '{effect.name}' failed validation");
            }

            // Check for duplicate IDs
            if (!string.IsNullOrEmpty(effect.EffectID))
            {
                if (effectIds.Contains(effect.EffectID))
                {
                    issues.Add($"Duplicate EffectID: '{effect.EffectID}'");
                }
                else
                {
                    effectIds.Add(effect.EffectID);
                }
            }
            else
            {
                issues.Add($"Effect '{effect.name}' has empty EffectID");
            }

            // Check for missing icon
            if (effect.EffectIcon == null)
            {
                issues.Add($"Effect '{effect.EffectID}' missing icon");
            }
        }

        // Update validation status
        if (issues.Count == 0)
        {
            ValidationStatus = $"✓ Registry is valid! ({AllEffects?.Count ?? 0} effects)";
        }
        else
        {
            ValidationStatus = $"! Issues found:\n- " + string.Join("\n- ", issues);
        }

        Logger.LogInfo($"StatusEffectRegistry: Validation complete. {issues.Count} issue(s) found.", Logger.LogCategory.General);

        RefreshCache();
    }

    /// <summary>
    /// Get debug info about the registry
    /// </summary>
    public string GetDebugInfo()
    {
        var validEffects = AllEffects?.Count(e => e != null) ?? 0;
        var totalEffects = AllEffects?.Count ?? 0;

        var typeCounts = AllEffects?
            .Where(e => e != null)
            .GroupBy(e => e.EffectType)
            .ToDictionary(g => g.Key, g => g.Count())
            ?? new Dictionary<StatusEffectType, int>();

        string typeSummary = string.Join(", ", typeCounts.Select(kvp => $"{kvp.Key}({kvp.Value})"));

        return $"StatusEffectRegistry: {validEffects}/{totalEffects} valid effects\n" +
               $"Cache: {effectLookup?.Count ?? 0} effects\n" +
               $"Types: {typeSummary}";
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (AllEffects != null && AllEffects.Count > 0)
        {
            UnityEditor.EditorApplication.delayCall += () => ValidateRegistry();
        }
    }

    /// <summary>
    /// Auto-populate registry by finding all StatusEffectDefinition assets in the project
    /// </summary>
    [ContextMenu("Auto-Populate Registry")]
    public void AutoPopulateRegistry()
    {
        var guids = UnityEditor.AssetDatabase.FindAssets("t:StatusEffectDefinition");
        var foundEffects = new List<StatusEffectDefinition>();

        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var effect = UnityEditor.AssetDatabase.LoadAssetAtPath<StatusEffectDefinition>(path);
            if (effect != null)
            {
                foundEffects.Add(effect);
            }
        }

        AllEffects = foundEffects;
        UnityEditor.EditorUtility.SetDirty(this);

        RefreshCache();
        ValidateRegistry();

        Logger.LogInfo($"StatusEffectRegistry: Auto-populated with {foundEffects.Count} status effect(s, Logger.LogCategory.DataLog)");
    }
#endif
}
