// Purpose: Manages the XP display container for all main activities
// Filepath: Assets/Scripts/UI/ActivityXpContainer.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActivityXpContainer : MonoBehaviour
{
    [Header("Container Settings")]
    [SerializeField] private Transform iconsContainer;
    [SerializeField] private GameObject iconContainerPrefab;

    [Header("Panel References")]
    [SerializeField] private VariantContainer variantContainer; // Référence au panel des variants

    [Header("Activity Registry")]
    [SerializeField] private ActivityRegistry activityRegistry; // Référence au registry des activités

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool autoRefreshOnStart = true;

    // Cache
    private List<IconContainer> iconContainers = new List<IconContainer>();
    private List<string> displayedActivities = new List<string>();

    #region Unity Lifecycle

    void Start()
    {
        // Vérifier les références essentielles
        if (!ValidateReferences())
        {
            return;
        }

        if (autoRefreshOnStart)
        {
            RefreshActivityIcons();
        }
    }

    void OnEnable()
    {
        // S'abonner aux événements de changement d'XP si nécessaire
        if (ValidateReferences())
        {
            RefreshActivityIcons();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Actualiser tous les icônes d'activités
    /// </summary>
    [ContextMenu("Refresh Activity Icons")]
    public void RefreshActivityIcons()
    {
        if (!ValidateReferences())
        {
            return;
        }

        // Obtenir toutes les activités principales
        var mainActivities = GetAllMainActivities();

        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityXpContainer: Found {mainActivities.Count} main activities", Logger.LogCategory.General);
        }

        // Créer ou mettre à jour les icônes
        UpdateActivityIcons(mainActivities);
    }

    /// <summary>
    /// Forcer la mise à jour d'une activité spécifique
    /// </summary>
    public void RefreshActivity(string activityId)
    {
        var iconContainer = iconContainers.FirstOrDefault(ic => ic.GetActivityId() == activityId);
        if (iconContainer != null)
        {
            iconContainer.RefreshDisplay();
        }
    }

    /// <summary>
    /// Obtenir le nombre d'activités affichées
    /// </summary>
    public int GetDisplayedActivityCount()
    {
        return displayedActivities.Count;
    }

    /// <summary>
    /// Définir le registry d'activités (peut être appelé depuis l'inspecteur ou le code)
    /// </summary>
    public void SetActivityRegistry(ActivityRegistry registry)
    {
        activityRegistry = registry;
        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityXpContainer: Activity registry set to {(registry != null ? registry.name : "null")}", Logger.LogCategory.General);
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Valider que toutes les références nécessaires sont présentes
    /// </summary>
    private bool ValidateReferences()
    {
        if (iconsContainer == null)
        {
            if (enableDebugLogs)
            {
                Logger.LogError("ActivityXpContainer: iconsContainer is null! Assign it in the inspector.", Logger.LogCategory.General);
            }
            return false;
        }

        if (iconContainerPrefab == null)
        {
            if (enableDebugLogs)
            {
                Logger.LogError("ActivityXpContainer: iconContainerPrefab is null! Assign it in the inspector.", Logger.LogCategory.General);
            }
            return false;
        }

        if (activityRegistry == null)
        {
            // Tenter de trouver le registry automatiquement
            activityRegistry = FindObjectOfType<ActivityRegistry>();

            // Si toujours pas trouvé, chercher dans les Resources
            if (activityRegistry == null)
            {
                activityRegistry = Resources.Load<ActivityRegistry>("ActivityRegistry");
            }

            if (activityRegistry == null)
            {
                if (enableDebugLogs)
                {
                    Logger.LogError("ActivityXpContainer: ActivityRegistry not found! Please assign it in the inspector or place it in Resources folder.", Logger.LogCategory.General);
                }
                return false;
            }
            else
            {
                if (enableDebugLogs)
                {
                    Logger.LogInfo($"ActivityXpContainer: Auto-found ActivityRegistry: {activityRegistry.name}", Logger.LogCategory.General);
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Obtenir toutes les activités principales du jeu via le registry
    /// </summary>
    private List<ActivityDefinition> GetAllMainActivities()
    {
        var allActivities = new List<ActivityDefinition>();

        if (activityRegistry == null || activityRegistry.AllActivities == null)
        {
            if (enableDebugLogs)
            {
                Logger.LogWarning("ActivityXpContainer: ActivityRegistry or AllActivities is null", Logger.LogCategory.General);
            }
            return allActivities;
        }

        // Parcourir toutes les LocationActivity du registry
        foreach (var locationActivity in activityRegistry.AllActivities)
        {
            if (locationActivity == null || locationActivity.ActivityReference == null)
                continue;

            // Récupérer l'ActivityDefinition de la LocationActivity
            var activityDefinition = locationActivity.ActivityReference;

            // Vérifier si c'est une activité principale et qu'on ne l'a pas déjà ajoutée
            if (IsMainActivity(activityDefinition) &&
                !allActivities.Any(a => a.ActivityID == activityDefinition.ActivityID))
            {
                allActivities.Add(activityDefinition);
            }
        }

        // Trier par nom pour un affichage cohérent
        return allActivities.OrderBy(a => a.GetDisplayName()).ToList();
    }

    /// <summary>
    /// Vérifier si une activité est une activité principale
    /// (par opposition aux sous-activités ou variantes)
    /// </summary>
    private bool IsMainActivity(ActivityDefinition activity)
    {
        // Une activité principale a un ActivityID et n'est pas une sous-activité
        if (string.IsNullOrEmpty(activity.ActivityID))
            return false;

        // Vérifier s'il y a des variantes associées (ce qui indique une activité principale)
        return HasActivityVariants(activity.ActivityID);
    }

    /// <summary>
    /// Vérifier si une activité a des variantes via le registry
    /// </summary>
    private bool HasActivityVariants(string activityId)
    {
        if (activityRegistry == null || activityRegistry.AllActivities == null)
        {
            return false;
        }

        // Chercher dans les LocationActivity du registry
        foreach (var locationActivity in activityRegistry.AllActivities)
        {
            if (locationActivity?.ActivityReference == null)
                continue;

            // Si c'est l'activité qu'on cherche
            if (locationActivity.ActivityReference.ActivityID == activityId)
            {
                // Vérifier si elle a des variantes dans la LocationActivity
                if (locationActivity.ActivityVariants != null && locationActivity.ActivityVariants.Count > 0)
                {
                    return true;
                }

                // Ou vérifier dans l'ActivityDefinition elle-même
                var variants = locationActivity.ActivityReference.GetAllVariants();
                if (variants != null && variants.Count > 0)
                {
                    return true;
                }
            }
        }

        // Si pas de variants trouvés, considérer comme activité principale quand même
        return !string.IsNullOrEmpty(activityId);
    }

    /// <summary>
    /// Mettre à jour les icônes d'activités
    /// </summary>
    private void UpdateActivityIcons(List<ActivityDefinition> activities)
    {
        // Nettoyer les icônes existants si nécessaire
        ClearOldIcons(activities);

        // Créer de nouveaux icônes pour les activités manquantes
        foreach (var activity in activities)
        {
            if (!displayedActivities.Contains(activity.ActivityID))
            {
                CreateIconForActivity(activity);
            }
        }

        // Mettre à jour tous les icônes existants
        foreach (var iconContainer in iconContainers)
        {
            if (iconContainer != null)
            {
                iconContainer.RefreshDisplay();
            }
        }
    }

    /// <summary>
    /// Nettoyer les icônes qui ne correspondent plus aux activités
    /// </summary>
    private void ClearOldIcons(List<ActivityDefinition> currentActivities)
    {
        var currentActivityIds = currentActivities.Select(a => a.ActivityID).ToList();

        // Supprimer les icônes d'activités qui n'existent plus
        for (int i = iconContainers.Count - 1; i >= 0; i--)
        {
            var iconContainer = iconContainers[i];

            if (iconContainer == null || !currentActivityIds.Contains(iconContainer.GetActivityId()))
            {
                if (iconContainer != null)
                {
                    displayedActivities.Remove(iconContainer.GetActivityId());

                    // Utiliser Destroy au lieu de DestroyImmediate en runtime
                    if (Application.isPlaying)
                    {
                        Destroy(iconContainer.gameObject);
                    }
                    else
                    {
#if UNITY_EDITOR
                        DestroyImmediate(iconContainer.gameObject);
#endif
                    }
                }

                iconContainers.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Créer un icône pour une activité
    /// </summary>
    private void CreateIconForActivity(ActivityDefinition activity)
    {
        // Vérifier que le prefab et le container sont valides
        if (iconContainerPrefab == null || iconsContainer == null)
        {
            if (enableDebugLogs)
            {
                Logger.LogError($"ActivityXpContainer: Cannot create icon for {activity.GetDisplayName()} - missing prefab or container", Logger.LogCategory.General);
            }
            return;
        }

        // Instancier le prefab d'icône
        GameObject iconObject = Instantiate(iconContainerPrefab, iconsContainer);
        iconObject.name = $"Icon_{activity.ActivityID}";

        // Configurer le composant IconContainer
        var iconContainer = iconObject.GetComponent<IconContainer>();
        if (iconContainer == null)
        {
            iconContainer = iconObject.AddComponent<IconContainer>();
        }

        // Initialiser l'icône avec les données d'activité
        iconContainer.Initialize(activity);

        // Forcer une première mise à jour pour afficher les données existantes
        iconContainer.RefreshDisplay();

        // Configurer la référence au panel des variants
        if (variantContainer != null)
        {
            iconContainer.SetVariantContainer(variantContainer);

            // Configurer la référence au panel principal pour la gestion d'affichage
            variantContainer.SetActivityXpContainer(gameObject);
        }

        // Ajouter aux listes
        iconContainers.Add(iconContainer);
        displayedActivities.Add(activity.ActivityID);

        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityXpContainer: Created icon for {activity.GetDisplayName()}", Logger.LogCategory.General);
        }
    }

    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// Méthode d'assistance pour l'éditeur - auto-assignment du registry
    /// </summary>
    void OnValidate()
    {
        if (activityRegistry == null)
        {
            // Chercher le ActivityRegistry dans le projet
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ActivityRegistry");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                activityRegistry = UnityEditor.AssetDatabase.LoadAssetAtPath<ActivityRegistry>(path);

                if (activityRegistry != null)
                {
                    Logger.LogInfo($"ActivityXpContainer: Auto-assigned ActivityRegistry: {activityRegistry.name}", Logger.LogCategory.General);
                }
            }
        }
    }
#endif
}