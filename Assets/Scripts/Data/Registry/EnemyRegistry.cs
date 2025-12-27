// Purpose: Central registry for all game enemies, provides fast lookup and validation
// Filepath: Assets/Scripts/Data/Registry/EnemyRegistry.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyRegistry", menuName = "WalkAndRPG/Combat/Enemy Registry")]
public class EnemyRegistry : ScriptableObject
{
    [Header("All Game Enemies")]
    [Tooltip("Drag all your EnemyDefinition assets here")]
    public List<EnemyDefinition> AllEnemies = new List<EnemyDefinition>();

    [Header("Robustness Settings")]
    [SerializeField] private bool enableFallbackSearch = true;
    [SerializeField] private bool logMissingEnemies = false;

    [Header("Debug Info")]
    [Tooltip("Shows validation errors if any")]
    [TextArea(3, 6)]
    public string ValidationStatus = "Click 'Validate Registry' to check for issues";

    // Cache for fast lookup
    private Dictionary<string, EnemyDefinition> enemyLookup;
    private HashSet<string> loggedMissingEnemies = new HashSet<string>();

    /// <summary>
    /// Get enemy by ID (fast lookup via cache)
    /// </summary>
    public EnemyDefinition GetEnemy(string enemyId)
    {
        if (enemyLookup == null)
        {
            InitializeLookupCache();
        }

        if (string.IsNullOrEmpty(enemyId))
        {
            return null;
        }

        if (enemyLookup.TryGetValue(enemyId, out EnemyDefinition enemy))
        {
            return enemy;
        }

        // Fallback: search by name
        if (enableFallbackSearch)
        {
            enemy = FindEnemyByNameFallback(enemyId);
            if (enemy != null)
            {
                return enemy;
            }
        }

        if (logMissingEnemies && !loggedMissingEnemies.Contains(enemyId))
        {
            Logger.LogWarning($"EnemyRegistry: Enemy '{enemyId}' not found in registry", Logger.LogCategory.General);
            loggedMissingEnemies.Add(enemyId);
        }

        return null;
    }

    /// <summary>
    /// Search for enemy by name if exact ID not found
    /// </summary>
    private EnemyDefinition FindEnemyByNameFallback(string enemyId)
    {
        foreach (var enemy in AllEnemies?.Where(e => e != null) ?? Enumerable.Empty<EnemyDefinition>())
        {
            if (MatchesEnemyName(enemy, enemyId))
            {
                return enemy;
            }
        }
        return null;
    }

    /// <summary>
    /// Flexible name matching
    /// </summary>
    private bool MatchesEnemyName(EnemyDefinition enemy, string searchName)
    {
        if (enemy?.EnemyID == null) return false;

        string enemyIdLower = enemy.EnemyID.ToLower().Replace(" ", "_");
        string enemyNameLower = enemy.EnemyName?.ToLower().Replace(" ", "_") ?? "";
        string search = searchName.ToLower().Replace(" ", "_");

        return enemyIdLower == search ||
               enemyNameLower == search ||
               enemyIdLower.Contains(search) ||
               search.Contains(enemyIdLower);
    }

    /// <summary>
    /// Check if an enemy exists in registry
    /// </summary>
    public bool HasEnemy(string enemyId)
    {
        return GetEnemy(enemyId) != null;
    }

    /// <summary>
    /// Get enemies by level range
    /// </summary>
    public List<EnemyDefinition> GetEnemiesByLevelRange(int minLevel, int maxLevel)
    {
        return AllEnemies?.Where(e => e != null && e.Level >= minLevel && e.Level <= maxLevel).ToList()
               ?? new List<EnemyDefinition>();
    }

    /// <summary>
    /// Initialize the lookup cache
    /// </summary>
    private void InitializeLookupCache()
    {
        enemyLookup = new Dictionary<string, EnemyDefinition>();

        if (AllEnemies == null) return;

        foreach (var enemy in AllEnemies.Where(e => e != null))
        {
            if (!string.IsNullOrEmpty(enemy.EnemyID))
            {
                if (!enemyLookup.ContainsKey(enemy.EnemyID))
                {
                    enemyLookup[enemy.EnemyID] = enemy;
                }
                else
                {
                    Logger.LogError($"EnemyRegistry: Duplicate EnemyID '{enemy.EnemyID}' found!", Logger.LogCategory.General);
                }
            }
        }

        Logger.LogInfo($"EnemyRegistry: Cache initialized with {enemyLookup.Count} enemies", Logger.LogCategory.General);
    }

    /// <summary>
    /// Clean null references
    /// </summary>
    public int CleanNullReferences()
    {
        if (AllEnemies == null) return 0;

        int removedCount = 0;

        for (int i = AllEnemies.Count - 1; i >= 0; i--)
        {
            if (AllEnemies[i] == null)
            {
                AllEnemies.RemoveAt(i);
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
    /// Force refresh the lookup cache
    /// </summary>
    public void RefreshCache()
    {
        enemyLookup = null;
        loggedMissingEnemies.Clear();
        InitializeLookupCache();
    }

    /// <summary>
    /// Validate all enemies in the registry
    /// </summary>
    [ContextMenu("Validate Registry")]
    public void ValidateRegistry()
    {
        var issues = new List<string>();
        var enemyIds = new HashSet<string>();

        int cleanedCount = CleanNullReferences();
        if (cleanedCount > 0)
        {
            issues.Add($"Auto-cleaned {cleanedCount} null references");
        }

        var nullCount = AllEnemies?.Count(e => e == null) ?? 0;
        if (nullCount > 0)
        {
            issues.Add($"{nullCount} null enemy(s) in registry");
        }

        foreach (var enemy in AllEnemies?.Where(e => e != null) ?? Enumerable.Empty<EnemyDefinition>())
        {
            if (!enemy.IsValid())
            {
                issues.Add($"Enemy '{enemy.name}' failed validation");
            }

            if (!string.IsNullOrEmpty(enemy.EnemyID))
            {
                if (enemyIds.Contains(enemy.EnemyID))
                {
                    issues.Add($"Duplicate EnemyID: '{enemy.EnemyID}'");
                }
                else
                {
                    enemyIds.Add(enemy.EnemyID);
                }
            }
            else
            {
                issues.Add($"Enemy '{enemy.name}' has empty EnemyID");
            }

            if (enemy.EnemySprite == null && enemy.Avatar == null)
            {
                issues.Add($"Enemy '{enemy.EnemyID}' missing sprite/avatar");
            }

            if (enemy.Abilities == null || enemy.Abilities.Count == 0)
            {
                issues.Add($"Enemy '{enemy.EnemyID}' has no abilities");
            }

            // Check for null items in loot table
            if (enemy.LootTable != null)
            {
                foreach (var loot in enemy.LootTable)
                {
                    if (loot != null && loot.Item == null)
                    {
                        issues.Add($"Enemy '{enemy.EnemyID}' has null item in loot table");
                        break;
                    }
                }
            }
        }

        if (issues.Count == 0)
        {
            ValidationStatus = $"✓ Registry is valid! ({AllEnemies?.Count ?? 0} enemies)";
        }
        else
        {
            ValidationStatus = $"Issues found:\n- " + string.Join("\n- ", issues);
        }

        Logger.LogInfo($"EnemyRegistry: Validation complete. {issues.Count} issue(s) found.", Logger.LogCategory.General);

        RefreshCache();
    }

    /// <summary>
    /// Get debug info about the registry
    /// </summary>
    public string GetDebugInfo()
    {
        var validEnemies = AllEnemies?.Count(e => e != null) ?? 0;
        var totalEnemies = AllEnemies?.Count ?? 0;

        return $"EnemyRegistry: {validEnemies}/{totalEnemies} valid enemies loaded\n" +
               $"Cache: {(enemyLookup?.Count ?? 0)} enemies";
    }

    /// <summary>
    /// Get all valid enemies
    /// </summary>
    public List<EnemyDefinition> GetAllValidEnemies()
    {
        return AllEnemies?.Where(e => e != null).ToList() ?? new List<EnemyDefinition>();
    }

    void OnEnable()
    {
        CleanNullReferences();
        RefreshCache();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (AllEnemies != null && AllEnemies.Count > 0)
        {
            UnityEditor.EditorApplication.delayCall += () => ValidateRegistry();
        }
    }
#endif
}
