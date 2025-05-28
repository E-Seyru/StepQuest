// Purpose: Represents a location-specific version of an activity
// Filepath: Assets/Scripts/Data/Models/LocationActivity.cs
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LocationActivity
{
    [Header("Activity Reference")]
    [Tooltip("Reference to the general activity definition")]
    public ActivityDefinition ActivityReference;

    [Tooltip("Override name specific to this location (e.g., 'Miner du fer' vs 'Miner de l'or')")]
    public string LocationSpecificName;

    [Header("Location-Specific Info")]
    [Tooltip("Description specific to this location and its resources")]
    [TextArea(2, 4)]
    public string LocationDescription;

    [Tooltip("Specific variants of this activity available at this location")]
    public List<ActivityVariant> ActivityVariants = new List<ActivityVariant>();

    [Header("Availability")]
    [Tooltip("Is this activity available at this location?")]
    public bool IsAvailable = true;

    [Tooltip("Special requirements for this activity at this location")]
    public string SpecialRequirements;

    [Header("Visual Override")]
    [Tooltip("Location-specific icon (optional - uses ActivityReference.ActivityIcon if null)")]
    public Sprite LocationSpecificIcon;

    /// <summary>
    /// Get the display name for this activity (location-specific or general)
    /// </summary>
    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(LocationSpecificName))
            return LocationSpecificName;

        if (ActivityReference != null)
            return ActivityReference.ActivityName;

        return "Activité inconnue";
    }

    /// <summary>
    /// Get the description for this activity (location-specific or general)
    /// </summary>
    public string GetDescription()
    {
        if (!string.IsNullOrEmpty(LocationDescription))
            return LocationDescription;

        if (ActivityReference != null)
            return ActivityReference.BaseDescription;

        return "Aucune description disponible.";
    }

    /// <summary>
    /// Get the icon for this activity (location-specific or general)
    /// </summary>
    public Sprite GetIcon()
    {
        if (LocationSpecificIcon != null)
            return LocationSpecificIcon;

        if (ActivityReference != null)
            return ActivityReference.ActivityIcon;

        return null;
    }

    /// <summary>
    /// Get the activity ID
    /// </summary>
    public string GetActivityID()
    {
        if (ActivityReference != null)
            return ActivityReference.ActivityID;

        return "unknown";
    }

    /// <summary>
    /// Get all valid activity variants
    /// </summary>
    public List<ActivityVariant> GetAvailableVariants()
    {
        if (ActivityVariants == null)
            return new List<ActivityVariant>();

        var validVariants = new List<ActivityVariant>();
        foreach (var variant in ActivityVariants)
        {
            if (variant != null && variant.IsValidVariant())
                validVariants.Add(variant);
        }

        return validVariants;
    }

    /// <summary>
    /// Get count of available variants
    /// </summary>
    public int GetVariantCount()
    {
        return GetAvailableVariants().Count;
    }

    /// <summary>
    /// Get formatted list of available resources from all variants
    /// </summary>
    public string GetResourcesText()
    {
        var allResources = new List<ItemDefinition>();
        var availableVariants = GetAvailableVariants();

        foreach (var variant in availableVariants)
        {
            var variantResources = variant.GetAllResources();
            foreach (var resource in variantResources)
            {
                if (!allResources.Contains(resource))
                    allResources.Add(resource);
            }
        }

        if (allResources.Count == 0)
            return "Aucune ressource spécifiée";

        var resourceNames = new List<string>();
        foreach (var resource in allResources)
        {
            resourceNames.Add(resource.GetDisplayName());
        }

        return resourceNames.Count > 0 ? string.Join(", ", resourceNames) : "Ressources invalides";
    }

    /// <summary>
    /// Check if this activity is valid and can be used
    /// </summary>
    public bool IsValidActivity()
    {
        if (ActivityReference == null)
        {
            Debug.LogWarning("LocationActivity: ActivityReference is null!");
            return false;
        }

        if (!ActivityReference.IsValid())
        {
            Debug.LogWarning($"LocationActivity: Referenced activity '{ActivityReference.name}' is not valid!");
            return false;
        }

        if (!IsAvailable)
        {
            return false;
        }

        if (!ActivityReference.IsAvailable)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Get full activity info for display purposes
    /// </summary>
    public string GetFullInfo()
    {
        string info = $"{GetDisplayName()}\n{GetDescription()}";

        var variants = GetAvailableVariants();
        if (variants.Count > 0)
        {
            info += $"\n{variants.Count} option(s) disponible(s):";
            foreach (var variant in variants)
            {
                info += $"\n- {variant.GetDisplayName()}";
            }
        }

        if (!string.IsNullOrEmpty(SpecialRequirements))
        {
            info += $"\nPré-requis: {SpecialRequirements}";
        }

        return info;
    }
}