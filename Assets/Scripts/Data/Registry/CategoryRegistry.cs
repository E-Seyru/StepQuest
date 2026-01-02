// Purpose: Registry for all category definitions with fast lookup
// Filepath: Assets/Scripts/Data/Registry/CategoryRegistry.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Registry containing all CategoryDefinition assets.
/// Provides fast lookup by CategoryID.
/// </summary>
[CreateAssetMenu(fileName = "CategoryRegistry", menuName = "StepQuest/Registries/Category Registry")]
public class CategoryRegistry : ScriptableObject
{
    [Header("Categories")]
    [SerializeField] private List<CategoryDefinition> categories = new List<CategoryDefinition>();

    // Cache for fast lookup
    private Dictionary<string, CategoryDefinition> categoryCache;
    private bool isCacheValid = false;

    /// <summary>
    /// Get all categories sorted by SortOrder
    /// </summary>
    public List<CategoryDefinition> AllCategories => categories.OrderBy(c => c.SortOrder).ToList();

    /// <summary>
    /// Get a category by its ID
    /// </summary>
    public CategoryDefinition GetCategory(string categoryId)
    {
        if (string.IsNullOrEmpty(categoryId))
            return null;

        EnsureCacheValid();

        if (categoryCache.TryGetValue(categoryId.ToLower(), out var category))
        {
            return category;
        }

        return null;
    }

    /// <summary>
    /// Get the icon for a category by ID
    /// </summary>
    public Sprite GetCategoryIcon(string categoryId)
    {
        var category = GetCategory(categoryId);
        return category?.Icon;
    }

    /// <summary>
    /// Get the display name for a category by ID
    /// </summary>
    public string GetCategoryDisplayName(string categoryId)
    {
        var category = GetCategory(categoryId);
        return category?.GetDisplayName() ?? categoryId;
    }

    /// <summary>
    /// Check if a category exists
    /// </summary>
    public bool HasCategory(string categoryId)
    {
        return GetCategory(categoryId) != null;
    }

    /// <summary>
    /// Rebuild the lookup cache
    /// </summary>
    public void RebuildCache()
    {
        categoryCache = new Dictionary<string, CategoryDefinition>();

        foreach (var category in categories)
        {
            if (category == null || string.IsNullOrEmpty(category.CategoryID))
                continue;

            string key = category.CategoryID.ToLower();
            if (!categoryCache.ContainsKey(key))
            {
                categoryCache[key] = category;
            }
            else
            {
                Logger.LogWarning($"CategoryRegistry: Duplicate CategoryID '{category.CategoryID}'", Logger.LogCategory.General);
            }
        }

        isCacheValid = true;
    }

    private void EnsureCacheValid()
    {
        if (!isCacheValid || categoryCache == null)
        {
            RebuildCache();
        }
    }

    private void OnEnable()
    {
        isCacheValid = false;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        isCacheValid = false;
    }
#endif
}
