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

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool autoRefreshOnStart = true;

    // Cache
    private List<IconContainer> iconContainers = new List<IconContainer>();
    private List<string> displayedActivities = new List<string>();

    #region Unity Lifecycle

    void Start()
    {
        if (autoRefreshOnStart)
        {
            RefreshActivityIcons();
        }
    }

    void OnEnable()
    {
        // S'abonner aux événements de changement d'XP si nécessaire
        RefreshActivityIcons();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Actualiser tous les icônes d'activités
    /// </summary>
    [ContextMenu("Refresh Activity Icons")]
    public void RefreshActivityIcons()
    {
        if (iconsContainer == null)
        {
            if (enableDebugLogs)
            {
                Logger.LogError("ActivityXpContainer: iconsContainer is null!", Logger.LogCategory.General);
            }
            return;
        }

        if (iconContainerPrefab == null)
        {
            if (enableDebugLogs)
            {
                Logger.LogError("ActivityXpContainer: iconContainerPrefab is null!", Logger.LogCategory.General);
            }
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

    #endregion

    #region Private Methods

    /// <summary>
    /// Obtenir toutes les activités principales du jeu
    /// </summary>
    private List<ActivityDefinition> GetAllMainActivities()
    {
        var allActivities = new List<ActivityDefinition>();

        // Rechercher tous les ActivityDefinition dans le projet
        var activityGuids = UnityEditor.AssetDatabase.FindAssets("t:ActivityDefinition");

        foreach (string guid in activityGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var activity = UnityEditor.AssetDatabase.LoadAssetAtPath<ActivityDefinition>(path);

            if (activity != null && IsMainActivity(activity))
            {
                allActivities.Add(activity);
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
    /// Vérifier si une activité a des variantes
    /// </summary>
    private bool HasActivityVariants(string activityId)
    {
        var variantGuids = UnityEditor.AssetDatabase.FindAssets("t:ActivityVariant");

        foreach (string guid in variantGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var variant = UnityEditor.AssetDatabase.LoadAssetAtPath<ActivityVariant>(path);

            if (variant != null && variant.ParentActivityID == activityId)
            {
                return true;
            }
        }

        return false;
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
                    DestroyImmediate(iconContainer.gameObject);
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
        iconContainer.SetVariantContainer(variantContainer);

        // Configurer la référence au panel principal pour la gestion d'affichage
        if (variantContainer != null)
        {
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
}