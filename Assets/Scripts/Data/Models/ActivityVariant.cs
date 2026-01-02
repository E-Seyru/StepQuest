// Purpose: Enhanced ActivityVariant with crafting support - EXTENDED VERSION
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

    [Header("Activity Type")]
    [Tooltip("Is this a time-based activity (crafting) or step-based activity (gathering)?")]
    public bool IsTimeBased = false;

    [Header("Category (for UI grouping)")]
    [Tooltip("Category for grouping in panels (e.g., 'Bars', 'Weapons', 'Armor'). Must match a CategoryID in CategoryRegistry.")]
    public string Category = "";

    [Header("Results/Products")]
    public ItemDefinition PrimaryResource;
    public ItemDefinition[] SecondaryResources;

    [Header("Step-Based Settings")]
    [Tooltip("How many steps needed for one completion (ignored for time-based activities)")]
    public int ActionCost = 10;
    [Tooltip("Success rate percentage (0-100)")]
    [Range(0, 100)]
    public int SuccessRate = 100;

    [Header("Experience & Progression")]
    [Tooltip("XP gagnee par tick/completion pour la competence principale (ex: Mining)")]
    public int MainSkillXPPerTick = 10;

    [Tooltip("XP gagnee par tick/completion pour cette sous-competence specifique")]
    public int SubSkillXPPerTick = 5;

    [Tooltip("ID de la competence principale que cette activite entraîne (ex: Mining, Woodcutting, Crafting)")]
    public string MainSkillId = "";

    [Header("Time-Based Settings (Crafting)")]
    [Tooltip("Time required to complete crafting in milliseconds (30000 = 30 seconds)")]
    public long CraftingTimeMs = 30000;
    [Tooltip("Materials required for crafting")]
    public ItemDefinition[] RequiredMaterials;
    [Tooltip("Quantities needed for each material (must match RequiredMaterials array length)")]
    public int[] RequiredQuantities;

    [Header("Requirements & Availability")]
    public bool IsAvailable = true;
    public string Requirements;
    public int UnlockRequirement = 0;

    [Header("Visual")]
    public Sprite VariantIcon;
    public Color VariantColor = Color.white;


    /// <summary>
    /// Obtenir l'ID de la competence principale base sur ParentActivityID si MainSkillId n'est pas defini
    /// Normalise en minuscules pour correspondre aux ActivityID
    /// </summary>
    public string GetMainSkillId()
    {
        if (!string.IsNullOrEmpty(MainSkillId))
            return MainSkillId.ToLower();

        // Fallback sur ParentActivityID si MainSkillId n'est pas defini
        if (!string.IsNullOrEmpty(ParentActivityID))
            return ParentActivityID.ToLower();

        return "unknown";
    }

    /// <summary>
    /// Obtenir l'ID de la sous-competence (base sur le nom de la variante)
    /// </summary>
    public string GetSubSkillId()
    {
        // Format: ParentActivity_VariantName (ex: "Mining_Iron", "Woodcutting_Oak")
        string parentId = GetMainSkillId();
        string variantName = !string.IsNullOrEmpty(VariantName) ? VariantName : name;

        return $"{parentId}_{variantName}";
    }

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
    /// NOUVEAU: Verifie si on peut crafter (pour les activites temporelles)
    /// </summary>
    public bool CanCraft(InventoryManager inventoryManager, string playerId = GameConstants.ContainerIdPlayer)
    {
        if (!IsTimeBased) return false;
        if (RequiredMaterials == null || RequiredQuantities == null) return false;
        if (RequiredMaterials.Length != RequiredQuantities.Length) return false;

        var container = inventoryManager.GetContainer(playerId);
        if (container == null) return false;

        // Verifier qu'on a tous les materiaux necessaires
        for (int i = 0; i < RequiredMaterials.Length; i++)
        {
            if (RequiredMaterials[i] == null) continue;

            // Utiliser HasItem pour verifier si on a assez de ce materiau
            if (!container.HasItem(RequiredMaterials[i].ItemID, RequiredQuantities[i]))
            {
                return false; // Pas assez de ce materiau
            }
        }

        return true;
    }

    /// <summary>
    /// NOUVEAU: Consomme les materiaux necessaires pour le crafting
    /// </summary>
    public bool ConsumeCraftingMaterials(InventoryManager inventoryManager, string playerId = GameConstants.ContainerIdPlayer)
    {
        if (!CanCraft(inventoryManager, playerId)) return false;

        // Consommer tous les materiaux
        for (int i = 0; i < RequiredMaterials.Length; i++)
        {
            if (RequiredMaterials[i] == null) continue;

            bool consumed = inventoryManager.RemoveItem(playerId, RequiredMaterials[i].ItemID, RequiredQuantities[i]);
            if (!consumed)
            {
                Logger.LogError($"ActivityVariant: Failed to consume {RequiredQuantities[i]} {RequiredMaterials[i].GetDisplayName()} for crafting", Logger.LogCategory.General);
                return false;
            }
        }

        Logger.LogInfo($"ActivityVariant: Consumed materials for crafting {GetDisplayName()}", Logger.LogCategory.General);
        return true;
    }

    /// <summary>
    /// NOUVEAU: Obtient la liste des materiaux requis sous forme de texte
    /// </summary>
    public string GetRequiredMaterialsText()
    {
        if (!IsTimeBased || RequiredMaterials == null || RequiredQuantities == null) return "";
        if (RequiredMaterials.Length != RequiredQuantities.Length) return "Error: Material/Quantity mismatch";

        var materialTexts = new List<string>();
        for (int i = 0; i < RequiredMaterials.Length; i++)
        {
            if (RequiredMaterials[i] != null)
            {
                materialTexts.Add($"{RequiredQuantities[i]}x {RequiredMaterials[i].GetDisplayName()}");
            }
        }

        return materialTexts.Count > 0 ? string.Join(", ", materialTexts) : "No materials required";
    }

    /// <summary>
    /// NOUVEAU: Rend les materiaux consommes pour le crafting (quand on annule)
    /// </summary>
    public bool RefundCraftingMaterials(InventoryManager inventoryManager, string playerId = GameConstants.ContainerIdPlayer)
    {
        if (!IsTimeBased) return false;
        if (RequiredMaterials == null || RequiredQuantities == null) return false;
        if (RequiredMaterials.Length != RequiredQuantities.Length) return false;

        // Rendre tous les materiaux
        for (int i = 0; i < RequiredMaterials.Length; i++)
        {
            if (RequiredMaterials[i] == null) continue;

            bool added = inventoryManager.AddItem(playerId, RequiredMaterials[i].ItemID, RequiredQuantities[i]);
            if (!added)
            {
                Logger.LogError($"ActivityVariant: Failed to refund {RequiredQuantities[i]} {RequiredMaterials[i].GetDisplayName()}", Logger.LogCategory.General);
                // On continue quand meme pour essayer de rendre les autres materiaux
            }
        }

        Logger.LogInfo($"ActivityVariant: Refunded materials for cancelled crafting {GetDisplayName()}", Logger.LogCategory.General);
        return true;
    }

    /// <summary>
    /// NOUVEAU: Obtient le temps de crafting sous forme lisible
    /// </summary>
    public string GetCraftingTimeText()
    {
        if (!IsTimeBased) return "";

        if (CraftingTimeMs < 1000)
            return $"{CraftingTimeMs}ms";
        else if (CraftingTimeMs < 60000)
            return $"{CraftingTimeMs / 1000f:F1}s";
        else
            return $"{CraftingTimeMs / 60000f:F1}min";
    }

    /// <summary>
    /// MODIFIE: Validate this variant (now includes crafting validation)
    /// </summary>
    public bool IsValidVariant()
    {
        if (string.IsNullOrEmpty(VariantName)) return false;
        if (PrimaryResource == null) return false;
        if (string.IsNullOrEmpty(GetParentActivityID())) return false;

        if (IsTimeBased)
        {
            // Validation pour les activites temporelles
            if (CraftingTimeMs <= 0) return false;
            if (RequiredMaterials != null && RequiredQuantities != null)
            {
                if (RequiredMaterials.Length != RequiredQuantities.Length) return false;
            }
        }
        else
        {
            // Validation pour les activites basees sur les pas
            if (ActionCost <= 0) return false;
        }

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

        // Validation des arrays pour le crafting
        if (IsTimeBased && RequiredMaterials != null && RequiredQuantities != null)
        {
            if (RequiredMaterials.Length != RequiredQuantities.Length)
            {
                Logger.LogWarning($"ActivityVariant: {VariantName} has mismatched RequiredMaterials and RequiredQuantities arrays", Logger.LogCategory.General);
            }
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

            // Normaliser en minuscules pour correspondre aux ActivityID (qui sont en snake_case minuscules)
            ParentActivityID = folderName.ToLower();
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