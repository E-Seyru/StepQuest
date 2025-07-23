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
    [SerializeField] private VariantContainer variantContainer; // R�f�rence au panel des variants

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
        // S'abonner aux �v�nements de changement d'XP si n�cessaire
        RefreshActivityIcons();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Actualiser tous les ic�nes d'activit�s
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

        // Obtenir toutes les activit�s principales
        var mainActivities = GetAllMainActivities();

        if (enableDebugLogs)
        {
            Logger.LogInfo($"ActivityXpContainer: Found {mainActivities.Count} main activities", Logger.LogCategory.General);
        }

        // Cr�er ou mettre � jour les ic�nes
        UpdateActivityIcons(mainActivities);
    }

    /// <summary>
    /// Forcer la mise � jour d'une activit� sp�cifique
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
    /// Obtenir le nombre d'activit�s affich�es
    /// </summary>
    public int GetDisplayedActivityCount()
    {
        return displayedActivities.Count;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Obtenir toutes les activit�s principales du jeu
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

        // Trier par nom pour un affichage coh�rent
        return allActivities.OrderBy(a => a.GetDisplayName()).ToList();
    }

    /// <summary>
    /// V�rifier si une activit� est une activit� principale
    /// (par opposition aux sous-activit�s ou variantes)
    /// </summary>
    private bool IsMainActivity(ActivityDefinition activity)
    {
        // Une activit� principale a un ActivityID et n'est pas une sous-activit�
        if (string.IsNullOrEmpty(activity.ActivityID))
            return false;

        // V�rifier s'il y a des variantes associ�es (ce qui indique une activit� principale)
        return HasActivityVariants(activity.ActivityID);
    }

    /// <summary>
    /// V�rifier si une activit� a des variantes
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
    /// Mettre � jour les ic�nes d'activit�s
    /// </summary>
    private void UpdateActivityIcons(List<ActivityDefinition> activities)
    {
        // Nettoyer les ic�nes existants si n�cessaire
        ClearOldIcons(activities);

        // Cr�er de nouveaux ic�nes pour les activit�s manquantes
        foreach (var activity in activities)
        {
            if (!displayedActivities.Contains(activity.ActivityID))
            {
                CreateIconForActivity(activity);
            }
        }

        // Mettre � jour tous les ic�nes existants
        foreach (var iconContainer in iconContainers)
        {
            if (iconContainer != null)
            {
                iconContainer.RefreshDisplay();
            }
        }
    }

    /// <summary>
    /// Nettoyer les ic�nes qui ne correspondent plus aux activit�s
    /// </summary>
    private void ClearOldIcons(List<ActivityDefinition> currentActivities)
    {
        var currentActivityIds = currentActivities.Select(a => a.ActivityID).ToList();

        // Supprimer les ic�nes d'activit�s qui n'existent plus
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
    /// Cr�er un ic�ne pour une activit�
    /// </summary>
    private void CreateIconForActivity(ActivityDefinition activity)
    {
        // Instancier le prefab d'ic�ne
        GameObject iconObject = Instantiate(iconContainerPrefab, iconsContainer);
        iconObject.name = $"Icon_{activity.ActivityID}";

        // Configurer le composant IconContainer
        var iconContainer = iconObject.GetComponent<IconContainer>();
        if (iconContainer == null)
        {
            iconContainer = iconObject.AddComponent<IconContainer>();
        }

        // Initialiser l'ic�ne avec les donn�es d'activit�
        iconContainer.Initialize(activity);

        // Forcer une premi�re mise � jour pour afficher les donn�es existantes
        iconContainer.RefreshDisplay();

        // Configurer la r�f�rence au panel des variants
        iconContainer.SetVariantContainer(variantContainer);

        // Configurer la r�f�rence au panel principal pour la gestion d'affichage
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