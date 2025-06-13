// Purpose: Enhanced ActivityVariant with auto-registration capabilities - ROBUST VERSION
// Filepath: Assets/Scripts/Data/Models/ActivityVariant.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NewActivityVariant", menuName = "WalkAndRPG/Activity Variant")]
public class ActivityVariant : ScriptableObject
{
    [Header("Basic Info")]
    public string VariantName;
    [TextArea(2, 3)]
    public string VariantDescription;

    [Header("Parent Activity")]
    [Tooltip("Which activity does this variant belong to? (Auto-detected from folder structure)")]
    public string ParentActivityID;
    [Tooltip("Auto-detect parent from folder name")]
    public bool AutoDetectParent = true;

    [Header("Resources")]
    public ItemDefinition PrimaryResource;
    public ItemDefinition[] SecondaryResources;

    [Header("Requirements & Availability")]
    public bool IsAvailable = true;
    public string Requirements;
    public int UnlockRequirement = 0;

    [Header("Visual")]
    public Sprite VariantIcon;
    public Color VariantColor = Color.white;

    [Header("Gameplay")]
    [Tooltip("How many steps needed for one completion")]
    public int ActionCost = 10;
    [Tooltip("Success rate percentage (0-100)")]
    [Range(0, 100)]
    public int SuccessRate = 100;

    /// <summary>
    /// Get display name for UI
    /// </summary>
    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(VariantName) ? name : VariantName;
    }

    /// <summary>
    /// Get the variant icon or fall back to primary resource icon
    /// </summary>
    public Sprite GetIcon()
    {
        if (VariantIcon != null) return VariantIcon;
        if (PrimaryResource != null && PrimaryResource.ItemIcon != null) return PrimaryResource.ItemIcon;
        return null;
    }

    /// <summary>
    /// Get the parent activity ID - FIXED VERSION with editor guards
    /// </summary>
    public string GetParentActivityID()
    {
        if (AutoDetectParent)
        {
#if UNITY_EDITOR
            DetectParentFromPath();
#endif
        }
        return ParentActivityID;
    }

    /// <summary>
    /// Validate this variant
    /// </summary>
    public bool IsValidVariant()
    {
        if (string.IsNullOrEmpty(VariantName)) return false;
        if (ActionCost <= 0) return false;
        if (PrimaryResource == null) return false;
        if (string.IsNullOrEmpty(GetParentActivityID())) return false;
        return true;
    }

    /// <summary>
    /// Get text description of resources this variant provides
    /// </summary>
    public string GetResourcesText()
    {
        if (PrimaryResource == null) return "No resources";

        string text = PrimaryResource.GetDisplayName();

        if (SecondaryResources != null && SecondaryResources.Length > 0)
        {
            var validSecondary = SecondaryResources.Where(r => r != null).Select(r => r.GetDisplayName());
            if (validSecondary.Any())
            {
                text += " + " + string.Join(", ", validSecondary);
            }
        }

        return text;
    }

    /// <summary>
    /// Get all resources (primary + secondary) - for compatibility with existing code
    /// </summary>
    public List<ItemDefinition> GetAllResources()
    {
        var allResources = new List<ItemDefinition>();

        if (PrimaryResource != null)
            allResources.Add(PrimaryResource);

        if (SecondaryResources != null)
        {
            allResources.AddRange(SecondaryResources.Where(r => r != null));
        }

        return allResources;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Auto-detection and registration in editor
    /// </summary>
    void OnValidate()
    {
        if (AutoDetectParent)
        {
            DetectParentFromPath();
        }

        // Auto-register this variant
        AutoRegisterVariant();
    }

    /// <summary>
    /// Detect parent activity from folder structure
    /// Expected structure: .../ActivitiesVariant/ParentActivityName/ThisVariant.asset
    /// </summary>
    private void DetectParentFromPath()
    {
        string assetPath = UnityEditor.AssetDatabase.GetAssetPath(this);
        if (string.IsNullOrEmpty(assetPath)) return;

        string[] pathParts = assetPath.Split('/');

        // Look for the parent folder of this variant
        // Assume structure: .../ActivitiesVariant/ParentActivity/Variant.asset
        for (int i = pathParts.Length - 2; i >= 0; i--)
        {
            string folderName = pathParts[i];

            // Skip common folder names
            if (folderName == "ActivitiesVariant" || folderName == "Activities" || folderName == "ScriptableObjects")
                continue;

            // MODIFIE: Garder exactement le nom du dossier sans transformation
            ParentActivityID = folderName;
            break;
        }

        // If we couldn't detect from path, try to find a matching activity
        if (string.IsNullOrEmpty(ParentActivityID))
        {
            TryFindParentActivity();
        }
    }

    /// <summary>
    /// Try to find parent activity by searching all activities
    /// </summary>
    private void TryFindParentActivity()
    {
        string[] activityGuids = UnityEditor.AssetDatabase.FindAssets("t:ActivityDefinition");

        foreach (string guid in activityGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ActivityDefinition activity = UnityEditor.AssetDatabase.LoadAssetAtPath<ActivityDefinition>(path);

            if (activity != null)
            {
                // Check if this variant's name suggests it belongs to this activity
                string variantLower = VariantName.ToLower();
                string activityLower = activity.ActivityName.ToLower();

                if (variantLower.Contains(activityLower) || activityLower.Contains(variantLower))
                {
                    ParentActivityID = activity.ActivityID;
                    Logger.LogInfo($"ActivityVariant: Auto-detected parent '{ParentActivityID}' for variant '{VariantName}'", Logger.LogCategory.General);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Auto-register this variant with its parent activity
    /// </summary>
    private void AutoRegisterVariant()
    {
        if (string.IsNullOrEmpty(ParentActivityID)) return;

        // Find the parent activity
        string[] activityGuids = UnityEditor.AssetDatabase.FindAssets("t:ActivityDefinition");
        ActivityDefinition parentActivity = null;

        foreach (string guid in activityGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ActivityDefinition activity = UnityEditor.AssetDatabase.LoadAssetAtPath<ActivityDefinition>(path);

            if (activity != null && activity.ActivityID == ParentActivityID)
            {
                parentActivity = activity;
                break;
            }
        }

        if (parentActivity != null)
        {
            // Trigger the parent activity to refresh its variants
            UnityEditor.EditorUtility.SetDirty(parentActivity);
            Logger.LogInfo($"ActivityVariant: Registered '{VariantName}' with parent activity '{ParentActivityID}'", Logger.LogCategory.General);
        }
        else
        {
            Logger.LogWarning($"ActivityVariant: Could not find parent activity '{ParentActivityID}' for variant '{VariantName}'", Logger.LogCategory.General);
        }
    }

    /// <summary>
    /// Context menu to manually detect parent
    /// </summary>
    [UnityEditor.MenuItem("CONTEXT/ActivityVariant/Detect Parent Activity")]
    private static void DetectParentContextMenu(UnityEditor.MenuCommand command)
    {
        ActivityVariant variant = (ActivityVariant)command.context;
        variant.DetectParentFromPath();
        if (string.IsNullOrEmpty(variant.ParentActivityID))
        {
            variant.TryFindParentActivity();
        }
        UnityEditor.EditorUtility.SetDirty(variant);
    }

    /// <summary>
    /// Context menu to force re-register
    /// </summary>
    [UnityEditor.MenuItem("CONTEXT/ActivityVariant/Force Re-register")]
    private static void ForceReregisterContextMenu(UnityEditor.MenuCommand command)
    {
        ActivityVariant variant = (ActivityVariant)command.context;
        variant.AutoRegisterVariant();
    }
#endif
}