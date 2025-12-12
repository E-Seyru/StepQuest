// Purpose: Central registry for all abilities, provides fast lookup and validation
// Filepath: Assets/Scripts/Data/Registry/AbilityRegistry.cs

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Central registry for all abilities in the game.
/// Provides fast lookup by ID and various filtering methods.
/// Follows the same pattern as StatusEffectRegistry.
/// </summary>
[CreateAssetMenu(fileName = "AbilityRegistry", menuName = "WalkAndRPG/Registries/Ability Registry")]
public class AbilityRegistry : ScriptableObject
{
    /// <summary>
    /// Singleton instance for global access
    /// </summary>
    public static AbilityRegistry Instance { get; private set; }

    [Header("All Abilities")]
    [Tooltip("Drag all your AbilityDefinition assets here")]
    public List<AbilityDefinition> AllAbilities = new List<AbilityDefinition>();

    [Header("Robustness Settings")]
    [SerializeField] private bool enableFallbackSearch = true;
    [SerializeField] private bool logMissingAbilities = false;

    [Header("Debug Info")]
    [Tooltip("Shows validation errors if any")]
    [TextArea(3, 6)]
    public string ValidationStatus = "Click 'Validate Registry' to check for issues";

    // Cache for fast lookup
    private Dictionary<string, AbilityDefinition> abilityLookup;
    private Dictionary<AbilityEffectType, List<AbilityDefinition>> abilitiesByEffectType;
    private HashSet<string> loggedMissingAbilities = new HashSet<string>();

    // === INITIALIZATION ===

    /// <summary>
    /// Initialize the registry as singleton instance
    /// </summary>
    public void Initialize()
    {
        Instance = this;
        RefreshCache();
        Logger.LogInfo($"AbilityRegistry: Initialized with {AllAbilities?.Count ?? 0} abilities", Logger.LogCategory.General);
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
    /// Get ability by ID (fast lookup via cache)
    /// </summary>
    public AbilityDefinition GetAbility(string abilityId)
    {
        if (abilityLookup == null)
        {
            InitializeLookupCache();
        }

        if (string.IsNullOrEmpty(abilityId))
        {
            return null;
        }

        // Try to get ability from cache
        if (abilityLookup.TryGetValue(abilityId, out AbilityDefinition ability))
        {
            return ability;
        }

        // Fallback search by name
        if (enableFallbackSearch)
        {
            ability = FindAbilityByNameFallback(abilityId);
            if (ability != null)
            {
                return ability;
            }
        }

        // Log once per missing ability
        if (logMissingAbilities && !loggedMissingAbilities.Contains(abilityId))
        {
            Logger.LogWarning($"AbilityRegistry: Ability '{abilityId}' not found in registry", Logger.LogCategory.General);
            loggedMissingAbilities.Add(abilityId);
        }

        return null;
    }

    /// <summary>
    /// Check if an ability exists in registry
    /// </summary>
    public bool HasAbility(string abilityId)
    {
        return GetAbility(abilityId) != null;
    }

    /// <summary>
    /// Get all abilities that have a specific effect type
    /// </summary>
    public List<AbilityDefinition> GetAbilitiesByEffectType(AbilityEffectType effectType)
    {
        if (abilitiesByEffectType == null)
        {
            InitializeLookupCache();
        }

        if (abilitiesByEffectType.TryGetValue(effectType, out var abilities))
        {
            return new List<AbilityDefinition>(abilities);
        }

        return new List<AbilityDefinition>();
    }

    /// <summary>
    /// Get all abilities that apply a specific status effect
    /// </summary>
    public List<AbilityDefinition> GetAbilitiesWithStatusEffect(string statusEffectId)
    {
        if (AllAbilities == null) return new List<AbilityDefinition>();

        return AllAbilities.Where(a => a != null && a.UsesNewEffectSystem && a.Effects != null &&
            a.Effects.Any(e => e.Type == AbilityEffectType.StatusEffect &&
                              e.StatusEffect != null &&
                              e.StatusEffect.EffectID == statusEffectId))
            .ToList();
    }

    /// <summary>
    /// Get all damage abilities
    /// </summary>
    public List<AbilityDefinition> GetDamageAbilities()
    {
        return GetAbilitiesByEffectType(AbilityEffectType.Damage);
    }

    /// <summary>
    /// Get all healing abilities
    /// </summary>
    public List<AbilityDefinition> GetHealingAbilities()
    {
        return GetAbilitiesByEffectType(AbilityEffectType.Heal);
    }

    /// <summary>
    /// Get all shield abilities
    /// </summary>
    public List<AbilityDefinition> GetShieldAbilities()
    {
        return GetAbilitiesByEffectType(AbilityEffectType.Shield);
    }

    /// <summary>
    /// Get all abilities that apply status effects
    /// </summary>
    public List<AbilityDefinition> GetStatusEffectAbilities()
    {
        return GetAbilitiesByEffectType(AbilityEffectType.StatusEffect);
    }

    /// <summary>
    /// Get all valid abilities (filtered for null references)
    /// </summary>
    public List<AbilityDefinition> GetAllValidAbilities()
    {
        return AllAbilities?.Where(a => a != null).ToList()
            ?? new List<AbilityDefinition>();
    }

    // === CACHE MANAGEMENT ===

    /// <summary>
    /// Initialize the lookup caches for fast access
    /// </summary>
    private void InitializeLookupCache()
    {
        abilityLookup = new Dictionary<string, AbilityDefinition>();
        abilitiesByEffectType = new Dictionary<AbilityEffectType, List<AbilityDefinition>>();

        if (AllAbilities == null) return;

        foreach (var ability in AllAbilities.Where(a => a != null))
        {
            // Add to ID lookup
            if (!string.IsNullOrEmpty(ability.AbilityID))
            {
                if (!abilityLookup.ContainsKey(ability.AbilityID))
                {
                    abilityLookup[ability.AbilityID] = ability;
                }
                else
                {
                    Logger.LogError($"AbilityRegistry: Duplicate AbilityID '{ability.AbilityID}' found!", Logger.LogCategory.General);
                }
            }

            // Add to effect type lookup
            var effectTypes = GetAbilityEffectTypes(ability);
            foreach (var effectType in effectTypes)
            {
                if (!abilitiesByEffectType.ContainsKey(effectType))
                {
                    abilitiesByEffectType[effectType] = new List<AbilityDefinition>();
                }
                if (!abilitiesByEffectType[effectType].Contains(ability))
                {
                    abilitiesByEffectType[effectType].Add(ability);
                }
            }
        }

        Logger.LogInfo($"AbilityRegistry: Cache initialized with {abilityLookup.Count} abilities", Logger.LogCategory.General);
    }

    /// <summary>
    /// Get all effect types used by an ability
    /// </summary>
    private HashSet<AbilityEffectType> GetAbilityEffectTypes(AbilityDefinition ability)
    {
        var types = new HashSet<AbilityEffectType>();

        if (ability.UsesNewEffectSystem && ability.Effects != null)
        {
            foreach (var effect in ability.Effects)
            {
                types.Add(effect.Type);
            }
        }
        else if (ability.EffectTypes != null)
        {
            foreach (var effectType in ability.EffectTypes)
            {
                types.Add(effectType);
            }
        }

        return types;
    }

    /// <summary>
    /// Force refresh the lookup cache
    /// </summary>
    public void RefreshCache()
    {
        abilityLookup = null;
        abilitiesByEffectType = null;
        loggedMissingAbilities.Clear();
        InitializeLookupCache();
    }

    /// <summary>
    /// Fallback search by name if ID not found
    /// </summary>
    private AbilityDefinition FindAbilityByNameFallback(string abilityId)
    {
        foreach (var ability in AllAbilities?.Where(a => a != null) ?? Enumerable.Empty<AbilityDefinition>())
        {
            if (MatchesAbilityName(ability, abilityId))
            {
                return ability;
            }
        }
        return null;
    }

    /// <summary>
    /// Flexible matching for ability names
    /// </summary>
    private bool MatchesAbilityName(AbilityDefinition ability, string searchName)
    {
        if (ability?.AbilityID == null) return false;

        string abilityId = ability.AbilityID.ToLower().Replace(" ", "_");
        string abilityDisplayName = ability.AbilityName?.ToLower().Replace(" ", "_") ?? "";
        string search = searchName.ToLower().Replace(" ", "_");

        return abilityId == search ||
               abilityDisplayName == search ||
               abilityId.Contains(search) ||
               search.Contains(abilityId);
    }

    // === MAINTENANCE ===

    /// <summary>
    /// Clean null references automatically
    /// </summary>
    public int CleanNullReferences()
    {
        if (AllAbilities == null) return 0;

        int removedCount = 0;

        for (int i = AllAbilities.Count - 1; i >= 0; i--)
        {
            if (AllAbilities[i] == null)
            {
                AllAbilities.RemoveAt(i);
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
    /// Validate all abilities in the registry and update status
    /// </summary>
    [ContextMenu("Validate Registry")]
    public void ValidateRegistry()
    {
        var issues = new List<string>();
        var abilityIds = new HashSet<string>();

        // Auto-clean first
        int cleanedCount = CleanNullReferences();
        if (cleanedCount > 0)
        {
            issues.Add($"Auto-cleaned {cleanedCount} null references");
        }

        // Check for null abilities
        var nullCount = AllAbilities?.Count(a => a == null) ?? 0;
        if (nullCount > 0)
        {
            issues.Add($"{nullCount} null ability(ies) in registry");
        }

        // Validate each ability
        foreach (var ability in AllAbilities?.Where(a => a != null) ?? Enumerable.Empty<AbilityDefinition>())
        {
            // Check if ability definition itself is valid
            if (!ability.IsValid())
            {
                issues.Add($"Ability '{ability.name}' failed validation");
            }

            // Check for duplicate IDs
            if (!string.IsNullOrEmpty(ability.AbilityID))
            {
                if (abilityIds.Contains(ability.AbilityID))
                {
                    issues.Add($"Duplicate AbilityID: '{ability.AbilityID}'");
                }
                else
                {
                    abilityIds.Add(ability.AbilityID);
                }
            }
            else
            {
                issues.Add($"Ability '{ability.name}' has empty AbilityID");
            }

            // Check for missing icon
            if (ability.AbilityIcon == null)
            {
                issues.Add($"Ability '{ability.AbilityID}' missing icon");
            }

            // Check for missing status effect references
            if (ability.UsesNewEffectSystem && ability.Effects != null)
            {
                foreach (var effect in ability.Effects)
                {
                    if (effect.Type == AbilityEffectType.StatusEffect && effect.StatusEffect == null)
                    {
                        issues.Add($"Ability '{ability.AbilityID}' has StatusEffect type but no StatusEffect reference");
                    }
                }
            }
        }

        // Update validation status
        if (issues.Count == 0)
        {
            ValidationStatus = $"Registry is valid! ({AllAbilities?.Count ?? 0} abilities)";
        }
        else
        {
            ValidationStatus = $"! Issues found:\n- " + string.Join("\n- ", issues);
        }

        Logger.LogInfo($"AbilityRegistry: Validation complete. {issues.Count} issue(s) found.", Logger.LogCategory.General);

        RefreshCache();
    }

    /// <summary>
    /// Get debug info about the registry
    /// </summary>
    public string GetDebugInfo()
    {
        var validAbilities = AllAbilities?.Count(a => a != null) ?? 0;
        var totalAbilities = AllAbilities?.Count ?? 0;

        var effectTypeCounts = new Dictionary<AbilityEffectType, int>();
        foreach (var ability in AllAbilities?.Where(a => a != null) ?? Enumerable.Empty<AbilityDefinition>())
        {
            var types = GetAbilityEffectTypes(ability);
            foreach (var type in types)
            {
                if (!effectTypeCounts.ContainsKey(type))
                    effectTypeCounts[type] = 0;
                effectTypeCounts[type]++;
            }
        }

        string typeSummary = string.Join(", ", effectTypeCounts.Select(kvp => $"{kvp.Key}({kvp.Value})"));

        return $"AbilityRegistry: {validAbilities}/{totalAbilities} valid abilities\n" +
               $"Cache: {abilityLookup?.Count ?? 0} abilities\n" +
               $"Effect Types: {typeSummary}";
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (AllAbilities != null && AllAbilities.Count > 0)
        {
            UnityEditor.EditorApplication.delayCall += () => ValidateRegistry();
        }
    }

    /// <summary>
    /// Auto-populate registry by finding all AbilityDefinition assets in the project
    /// </summary>
    [ContextMenu("Auto-Populate Registry")]
    public void AutoPopulateRegistry()
    {
        var guids = UnityEditor.AssetDatabase.FindAssets("t:AbilityDefinition");
        var foundAbilities = new List<AbilityDefinition>();

        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var ability = UnityEditor.AssetDatabase.LoadAssetAtPath<AbilityDefinition>(path);
            if (ability != null)
            {
                foundAbilities.Add(ability);
            }
        }

        AllAbilities = foundAbilities;
        UnityEditor.EditorUtility.SetDirty(this);

        RefreshCache();
        ValidateRegistry();

        Debug.Log($"AbilityRegistry: Auto-populated with {foundAbilities.Count} ability(ies)");
    }
#endif
}
