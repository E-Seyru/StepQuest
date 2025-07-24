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
    [SerializeField] private VariantContainer variantContainer; // Reference au panel des variants

    [Header("Activity Registry")]
    [SerializeField] private ActivityRegistry activityRegistry; // Reference au registry des activites

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool autoRefreshOnStart = true;

    // Cache
    private List<IconContainer> iconContainers = new List<IconContainer>();
    private List<string> displayedActivities = new List<string>();

    #region Unity Lifecycle

    void Start()
    {
        // Verifier les references essentielles
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
        // S'abonner aux evenements de changement d'XP si necessaire
        if (ValidateReferences())
        {
            RefreshActivityIcons();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Actualiser tous les icônes d'activites
    /// </summary>
    [ContextMenu("Refresh Activity Icons")]
    public void RefreshActivityIcons()
    {
        if (!ValidateReferences())
        {
            return;
        }

        // Obtenir toutes les activites principales
        var mainActivities = GetAllMainActivities();

        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityXpContainer: Found {mainActivities.Count} main activities", Logger.LogCategory.General);
        }

        // Creer ou mettre a jour les icônes
        UpdateActivityIcons(mainActivities);
    }

    /// <summary>
    /// Forcer la mise a jour d'une activite specifique
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
    /// Obtenir le nombre d'activites affichees
    /// </summary>
    public int GetDisplayedActivityCount()
    {
        return displayedActivities.Count;
    }

    /// <summary>
    /// Definir le registry d'activites (peut etre appele depuis l'inspecteur ou le code)
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
    /// Valider que toutes les references necessaires sont presentes
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

            // Si toujours pas trouve, chercher dans les Resources
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
    /// Obtenir toutes les activites principales du jeu via le registry
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

            // Recuperer l'ActivityDefinition de la LocationActivity
            var activityDefinition = locationActivity.ActivityReference;

            // Verifier si c'est une activite principale et qu'on ne l'a pas deja ajoutee
            if (IsMainActivity(activityDefinition) &&
                !allActivities.Any(a => a.ActivityID == activityDefinition.ActivityID))
            {
                allActivities.Add(activityDefinition);
            }
        }

        // Trier par nom pour un affichage coherent
        return allActivities.OrderBy(a => a.GetDisplayName()).ToList();
    }

    /// <summary>
    /// Verifier si une activite est une activite principale
    /// (par opposition aux sous-activites ou variantes)
    /// </summary>
    private bool IsMainActivity(ActivityDefinition activity)
    {
        // Une activite principale a un ActivityID et n'est pas une sous-activite
        if (string.IsNullOrEmpty(activity.ActivityID))
            return false;

        // Verifier s'il y a des variantes associees (ce qui indique une activite principale)
        return HasActivityVariants(activity.ActivityID);
    }

    /// <summary>
    /// Verifier si une activite a des variantes via le registry
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

            // Si c'est l'activite qu'on cherche
            if (locationActivity.ActivityReference.ActivityID == activityId)
            {
                // Verifier si elle a des variantes dans la LocationActivity
                if (locationActivity.ActivityVariants != null && locationActivity.ActivityVariants.Count > 0)
                {
                    return true;
                }

                // Ou verifier dans l'ActivityDefinition elle-meme
                var variants = locationActivity.ActivityReference.GetAllVariants();
                if (variants != null && variants.Count > 0)
                {
                    return true;
                }
            }
        }

        // Si pas de variants trouves, considerer comme activite principale quand meme
        return !string.IsNullOrEmpty(activityId);
    }

    /// <summary>
    /// Mettre a jour les icônes d'activites
    /// </summary>
    private void UpdateActivityIcons(List<ActivityDefinition> activities)
    {
        // Nettoyer les icônes existants si necessaire
        ClearOldIcons(activities);

        // Creer de nouveaux icônes pour les activites manquantes
        foreach (var activity in activities)
        {
            if (!displayedActivities.Contains(activity.ActivityID))
            {
                CreateIconForActivity(activity);
            }
        }

        // Mettre a jour tous les icônes existants
        foreach (var iconContainer in iconContainers)
        {
            if (iconContainer != null)
            {
                iconContainer.RefreshDisplay();
            }
        }
    }

    /// <summary>
    /// Nettoyer les icônes qui ne correspondent plus aux activites
    /// </summary>
    private void ClearOldIcons(List<ActivityDefinition> currentActivities)
    {
        var currentActivityIds = currentActivities.Select(a => a.ActivityID).ToList();

        // Supprimer les icônes d'activites qui n'existent plus
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
    /// Creer un icône pour une activite
    /// </summary>
    private void CreateIconForActivity(ActivityDefinition activity)
    {
        // Verifier que le prefab et le container sont valides
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

        // Initialiser l'icône avec les donnees d'activite
        iconContainer.Initialize(activity);

        // Forcer une premiere mise a jour pour afficher les donnees existantes
        iconContainer.RefreshDisplay();

        // Configurer la reference au panel des variants
        if (variantContainer != null)
        {
            iconContainer.SetVariantContainer(variantContainer);

            // Configurer la reference au panel principal pour la gestion d'affichage
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
    /// Methode d'assistance pour l'editeur - auto-assignment du registry
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