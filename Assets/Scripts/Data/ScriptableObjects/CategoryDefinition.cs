// Purpose: Defines a category with its display properties (icon, name, color)
// Filepath: Assets/Scripts/Data/ScriptableObjects/CategoryDefinition.cs
using UnityEngine;

/// <summary>
/// ScriptableObject defining a category for grouping activity variants.
/// Used in CraftingPanel tabs and other UI elements.
/// </summary>
[CreateAssetMenu(fileName = "NewCategory", menuName = "StepQuest/Category Definition")]
public class CategoryDefinition : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique identifier - must match the Category string in ActivityVariant")]
    public string CategoryID;

    [Tooltip("Display name shown in UI")]
    public string DisplayName;

    [Header("Visual")]
    [Tooltip("Icon shown in tab buttons")]
    public Sprite Icon;

    [Tooltip("Optional color for this category")]
    public Color CategoryColor = Color.white;

    [Header("Sorting")]
    [Tooltip("Lower values appear first in tab order")]
    public int SortOrder = 0;

    /// <summary>
    /// Get display name, falling back to CategoryID if not set
    /// </summary>
    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(DisplayName) ? CategoryID : DisplayName;
    }
}
