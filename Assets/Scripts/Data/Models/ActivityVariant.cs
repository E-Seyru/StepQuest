// Purpose: ScriptableObject representing a specific variant of an activity (e.g., "Mine Iron" vs "Mine Gold")
// Filepath: Assets/Scripts/Data/ScriptableObjects/ActivityVariant.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Activity Variant", menuName = "WalkAndRPG/Activity Variant")]
public class ActivityVariant : ScriptableObject
{
    [Header("Variant Info")]
    [Tooltip("Name of this specific variant (e.g., 'Miner du fer', 'Pêcher la truite')")]
    public string VariantName;

    [Tooltip("Description of this specific variant")]
    [TextArea(1, 3)]
    public string VariantDescription;

    [Header("Resources")]
    [Tooltip("Primary resource obtained from this variant")]
    public ItemDefinition PrimaryResource;

    [Tooltip("Additional resources that can be obtained (with lower chances)")]
    public List<ItemDefinition> SecondaryResources = new List<ItemDefinition>();

    [Header("Requirements")]
    [Tooltip("Is this variant currently available?")]
    public bool IsAvailable = true;

    [Tooltip("Special requirements for this variant")]
    public string Requirements;

    [Tooltip("Minimum level or steps to unlock this variant")]
    public int UnlockRequirement = 0;

    [Header("Visual")]
    [Tooltip("Specific icon for this variant (optional - uses activity icon if null)")]
    public Sprite VariantIcon;

    [Tooltip("Color tint for this variant")]
    public Color VariantColor = Color.white;

    [Header("Game Balance")]
    [Tooltip("Time or steps required to complete this variant")]
    public int ActionCost = 1;

    [Tooltip("Success rate (0-100%)")]
    [Range(0, 100)]
    public int SuccessRate = 100;

    /// <summary>
    /// Get display name for this variant
    /// </summary>
    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(VariantName))
            return VariantName;

        if (PrimaryResource != null)
            return $"Obtenir {PrimaryResource.GetDisplayName()}";

        return "Variante inconnue";
    }

    /// <summary>
    /// Get description for this variant
    /// </summary>
    public string GetDescription()
    {
        if (!string.IsNullOrEmpty(VariantDescription))
            return VariantDescription;

        if (PrimaryResource != null)
            return $"Permet d'obtenir {PrimaryResource.GetDisplayName()}";

        return "Aucune description disponible.";
    }

    /// <summary>
    /// Get all resources available from this variant
    /// </summary>
    public List<ItemDefinition> GetAllResources()
    {
        var allResources = new List<ItemDefinition>();

        if (PrimaryResource != null)
            allResources.Add(PrimaryResource);

        if (SecondaryResources != null)
        {
            foreach (var resource in SecondaryResources)
            {
                if (resource != null && !allResources.Contains(resource))
                    allResources.Add(resource);
            }
        }

        return allResources;
    }

    /// <summary>
    /// Get formatted text of all available resources
    /// </summary>
    public string GetResourcesText()
    {
        var resources = GetAllResources();

        if (resources.Count == 0)
            return "Aucune ressource";

        if (resources.Count == 1)
            return resources[0].GetDisplayName();

        var resourceNames = new List<string>();
        foreach (var resource in resources)
        {
            resourceNames.Add(resource.GetDisplayName());
        }

        return string.Join(", ", resourceNames);
    }

    /// <summary>
    /// Check if this variant is valid and usable
    /// </summary>
    public bool IsValidVariant()
    {
        if (!IsAvailable)
            return false;

        if (PrimaryResource == null)
        {
            Debug.LogWarning($"ActivityVariant '{VariantName}': No primary resource assigned!");
            return false;
        }

        if (!PrimaryResource.IsValid())
            return false;

        return true;
    }

    /// <summary>
    /// Get icon for this variant (variant-specific or primary resource icon)
    /// </summary>
    public Sprite GetIcon()
    {
        if (VariantIcon != null)
            return VariantIcon;

        if (PrimaryResource != null && PrimaryResource.ItemIcon != null)
            return PrimaryResource.ItemIcon;

        return null;
    }

    /// <summary>
    /// Validate this variant definition
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(VariantName))
        {
            Debug.LogError($"ActivityVariant '{name}': VariantName is empty!");
            return false;
        }

        if (PrimaryResource == null)
        {
            Debug.LogError($"ActivityVariant '{name}': PrimaryResource is not assigned!");
            return false;
        }

        if (!PrimaryResource.IsValid())
        {
            Debug.LogError($"ActivityVariant '{name}': PrimaryResource is not valid!");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Get full info for display/debug purposes
    /// </summary>
    public string GetFullInfo()
    {
        string info = $"{GetDisplayName()}\n{GetDescription()}";

        var resources = GetAllResources();
        if (resources.Count > 0)
        {
            info += $"\nRessources: {GetResourcesText()}";
        }

        if (!string.IsNullOrEmpty(Requirements))
        {
            info += $"\nPré-requis: {Requirements}";
        }

        if (ActionCost > 1)
        {
            info += $"\nCoût: {ActionCost} action(s)";
        }

        if (SuccessRate < 100)
        {
            info += $"\nTaux de réussite: {SuccessRate}%";
        }

        return info;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-generate VariantName from primary resource if empty
        if (string.IsNullOrEmpty(VariantName) && PrimaryResource != null)
        {
            VariantName = $"Obtenir {PrimaryResource.GetDisplayName()}";
        }

        // Remove null entries from secondary resources
        if (SecondaryResources != null)
        {
            SecondaryResources.RemoveAll(r => r == null);
        }
    }
#endif
}
