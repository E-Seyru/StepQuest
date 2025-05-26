// Purpose: Simple ScriptableObject defining game resources (materials, items, etc.)
// Filepath: Assets/Scripts/Data/ScriptableObjects/ResourceDefinition.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New Resource", menuName = "WalkAndRPG/Resource Definition")]
public class ResourceDefinition : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Unique identifier for this resource")]
    public string ResourceID;

    [Tooltip("Display name shown in UI")]
    public string ResourceName;

    [Tooltip("Short description of this resource")]
    [TextArea(1, 3)]
    public string Description;

    [Header("Visual")]
    [Tooltip("Icon representing this resource")]
    public Sprite ResourceIcon;

    [Tooltip("Color theme for this resource")]
    public Color ResourceColor = Color.white;

    [Header("Game Values")]
    [Tooltip("Base value/price of this resource")]
    public int BaseValue = 1;

    [Tooltip("Rarity tier (1=common, 5=legendary)")]
    [Range(1, 5)]
    public int RarityTier = 1;

    [Header("Categories")]
    [Tooltip("What type of resource this is")]
    public ResourceType Type = ResourceType.Material;

    [Tooltip("Can this resource be stacked in inventory?")]
    public bool IsStackable = true;

    /// <summary>
    /// Get display info for UI
    /// </summary>
    public string GetDisplayName()
    {
        return !string.IsNullOrEmpty(ResourceName) ? ResourceName : ResourceID;
    }

    /// <summary>
    /// Get rarity display text
    /// </summary>
    public string GetRarityText()
    {
        return RarityTier switch
        {
            1 => "Commun",
            2 => "Peu commun",
            3 => "Rare",
            4 => "Épique",
            5 => "Légendaire",
            _ => "Inconnu"
        };
    }

    /// <summary>
    /// Get rarity color
    /// </summary>
    public Color GetRarityColor()
    {
        return RarityTier switch
        {
            1 => Color.gray,
            2 => Color.green,
            3 => Color.blue,
            4 => new Color(0.6f, 0.0f, 1.0f), // Purple
            5 => new Color(1.0f, 0.6f, 0.0f), // Orange
            _ => Color.white
        };
    }

    /// <summary>
    /// Validate this resource definition
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(ResourceID))
        {
            Debug.LogError($"ResourceDefinition '{name}': ResourceID is empty!");
            return false;
        }

        return true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-generate ResourceID from name if empty
        if (string.IsNullOrEmpty(ResourceID) && !string.IsNullOrEmpty(name))
        {
            ResourceID = name.ToLower().Replace(" ", "_");
        }

        // Auto-generate ResourceName from ResourceID if empty
        if (string.IsNullOrEmpty(ResourceName) && !string.IsNullOrEmpty(ResourceID))
        {
            ResourceName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ResourceID.Replace("_", " "));
        }
    }
#endif
}

/// <summary>
/// Types of resources available in the game
/// </summary>
public enum ResourceType
{
    Material,    // Matériaux de base (fer, bois, etc.)
    Food,        // Nourriture (poisson, fruits, etc.)
    Tool,        // Outils
    Consumable,  // Consommables (potions, etc.)
    Currency,    // Monnaies
    Special      // Objets spéciaux/quête
}