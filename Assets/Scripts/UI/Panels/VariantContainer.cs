// Purpose: Manages the variant display panel for a selected main activity
// Filepath: Assets/Scripts/UI/VariantContainer.cs
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class VariantContainer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel; // Le panel qui contient tout
    [SerializeField] private Transform variantsContainer; // Container pour les icônes de variants
    [SerializeField] private GameObject variantIconPrefab; // Prefab pour chaque variant
    [SerializeField] private TextMeshProUGUI titleText; // Titre du panel (ex: "Mining Variants")

    [Header("Activity Registry")]
    [SerializeField] private ActivityRegistry activityRegistry; // Référence au registry des activités

    [Header("Panel Management")]
    [SerializeField] private GameObject activityXpContainer; // Référence au panel des activités principales

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Data
    private ActivityDefinition currentActivity;
    private List<VariantIconContainer> variantIcons = new List<VariantIconContainer>();

    #region Unity Lifecycle

    void Start()
    {
        // Essayer de trouver l'ActivityRegistry automatiquement s'il n'est pas assigné
        if (activityRegistry == null)
        {
            activityRegistry = FindObjectOfType<ActivityRegistry>();
            if (activityRegistry == null)
            {
                activityRegistry = Resources.Load<ActivityRegistry>("ActivityRegistry");
            }
        }

        HidePanel();
    }

    void Update()
    {
        // Détecter les clics en dehors du panel quand il est ouvert
        if (panel != null && panel.activeInHierarchy)
        {
            DetectClickOutside();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Afficher les variants pour une activité donnée
    /// </summary>
    public void ShowVariantsForActivity(ActivityDefinition activity)
    {
        if (activity == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("VariantContainer: Cannot show variants for null activity");
            }
            return;
        }

        currentActivity = activity;

        // Mettre à jour le titre
        if (titleText != null)
        {
            titleText.text = $"{activity.GetDisplayName()} Variants";
        }

        // Obtenir tous les variants pour cette activité
        var variants = GetVariantsForActivity(activity.ActivityID);

        if (enableDebugLogs)
        {
            Debug.Log($"VariantContainer: Found {variants.Count} variants for {activity.GetDisplayName()}");
        }

        // Créer les icônes de variants
        CreateVariantIcons(variants);

        // Masquer le panel des activités principales et afficher celui des variants
        HideActivityXpContainer();
        ShowPanel();
    }

    /// <summary>
    /// Masquer le panel des variants
    /// </summary>
    public void HidePanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        // Réafficher le panel des activités principales
        ShowActivityXpContainer();
    }

    /// <summary>
    /// Afficher le panel des variants
    /// </summary>
    public void ShowPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    /// <summary>
    /// Définir le registry d'activités
    /// </summary>
    public void SetActivityRegistry(ActivityRegistry registry)
    {
        activityRegistry = registry;
        if (enableDebugLogs)
        {
            Debug.Log($"VariantContainer: Activity registry set to {(registry != null ? registry.name : "null")}");
        }
    }

    /// <summary>
    /// Masquer le panel des activités principales
    /// </summary>
    private void HideActivityXpContainer()
    {
        if (activityXpContainer != null)
        {
            activityXpContainer.SetActive(false);
        }
    }

    /// <summary>
    /// Afficher le panel des activités principales
    /// </summary>
    private void ShowActivityXpContainer()
    {
        if (activityXpContainer != null)
        {
            activityXpContainer.SetActive(true);
        }
    }

    #endregion

    #region Private Methods

    // Cache pour éviter de recalculer les variants à chaque fois
    private Dictionary<string, List<ActivityVariant>> variantsCache = new Dictionary<string, List<ActivityVariant>>();

    /// <summary>
    /// Obtenir tous les variants d'une activité via le registry - VERSION OPTIMISÉE
    /// </summary>
    private List<ActivityVariant> GetVariantsForActivity(string activityId)
    {
        // Vérifier le cache d'abord
        if (variantsCache.ContainsKey(activityId))
        {
            if (enableDebugLogs)
            {
                Debug.Log($"VariantContainer: Using cached variants for {activityId}");
            }
            return variantsCache[activityId];
        }

        var variants = new List<ActivityVariant>();

        if (activityRegistry == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("VariantContainer: ActivityRegistry is null! Cannot load variants.");
            }
            return variants;
        }

        if (activityRegistry.AllActivities == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("VariantContainer: ActivityRegistry.AllActivities is null!");
            }
            return variants;
        }

        // Chercher dans les LocationActivity du registry
        foreach (var locationActivity in activityRegistry.AllActivities)
        {
            if (locationActivity?.ActivityReference == null)
                continue;

            // Si c'est l'activité qu'on cherche
            if (locationActivity.ActivityReference.ActivityID == activityId)
            {
                // Récupérer les variants de cette LocationActivity (plus rapide)
                if (locationActivity.ActivityVariants != null)
                {
                    foreach (var variant in locationActivity.ActivityVariants)
                    {
                        if (variant != null) // Enlever IsValidVariant() qui peut être lent
                        {
                            variants.Add(variant);
                        }
                    }
                }

                // On a trouvé l'activité, pas besoin de continuer
                break;
            }
        }

        // Mettre en cache pour les prochaines fois
        variantsCache[activityId] = variants.OrderBy(v => v.VariantName).ToList();

        if (enableDebugLogs)
        {
            Debug.Log($"VariantContainer: Cached {variants.Count} variants for activity {activityId}");
        }

        return variantsCache[activityId];
    }

    /// <summary>
    /// Vider le cache des variants (à appeler si les données changent)
    /// </summary>
    public void ClearVariantsCache()
    {
        variantsCache.Clear();
        if (enableDebugLogs)
        {
            Debug.Log("VariantContainer: Variants cache cleared");
        }
    }

    /// <summary>
    /// Créer les icônes pour tous les variants - VERSION OPTIMISÉE
    /// </summary>
    private void CreateVariantIcons(List<ActivityVariant> variants)
    {
        // Nettoyer les icônes existants de manière optimisée
        ClearVariantIconsOptimized();

        if (variants.Count == 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log("VariantContainer: No variants to display");
            }
            return;
        }

        // Pré-allouer la liste pour éviter les réallocations
        variantIcons.Capacity = variants.Count;

        // Créer un icône pour chaque variant
        foreach (var variant in variants)
        {
            CreateVariantIconOptimized(variant);
        }
    }

    /// <summary>
    /// Créer un icône pour un variant spécifique - VERSION OPTIMISÉE
    /// </summary>
    private void CreateVariantIconOptimized(ActivityVariant variant)
    {
        if (variantIconPrefab == null || variantsContainer == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogError("VariantContainer: variantIconPrefab or variantsContainer is null!");
            }
            return;
        }

        // Instancier le prefab
        GameObject iconObject = Instantiate(variantIconPrefab, variantsContainer);
        iconObject.name = $"VariantIcon_{variant.VariantName}";

        // Configurer le composant VariantIconContainer
        var variantIcon = iconObject.GetComponent<VariantIconContainer>();
        if (variantIcon == null)
        {
            variantIcon = iconObject.AddComponent<VariantIconContainer>();
        }

        // Initialiser avec les données du variant
        variantIcon.Initialize(variant);

        // Ajouter à la liste
        variantIcons.Add(variantIcon);

        // Logs réduits pour éviter le spam
        if (enableDebugLogs && variantIcons.Count == 1)
        {
            Debug.Log($"VariantContainer: Creating {variantIcons.Capacity} variant icons...");
        }
    }

    /// <summary>
    /// Nettoyer tous les icônes de variants existants - VERSION OPTIMISÉE
    /// </summary>
    private void ClearVariantIconsOptimized()
    {
        // Batch destroy pour être plus efficace
        for (int i = variantIcons.Count - 1; i >= 0; i--)
        {
            var variantIcon = variantIcons[i];
            if (variantIcon != null)
            {
                // Utiliser Destroy au lieu de DestroyImmediate en runtime
                if (Application.isPlaying)
                {
                    Destroy(variantIcon.gameObject);
                }
                else
                {
#if UNITY_EDITOR
                    DestroyImmediate(variantIcon.gameObject);
#endif
                }
            }
        }

        // Vider la liste en une fois
        variantIcons.Clear();
    }

    /// <summary>
    /// Configurer la référence au ActivityXpContainer (appelé par ActivityXpContainer)
    /// </summary>
    public void SetActivityXpContainer(GameObject container)
    {
        activityXpContainer = container;
    }

    /// <summary>
    /// Détecter les clics en dehors du panel pour le fermer
    /// </summary>
    private void DetectClickOutside()
    {
        // Vérifier s'il y a un clic de souris ou un touch
        if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
        {
            Vector2 clickPosition;

            // Obtenir la position du clic/touch
            if (Input.touchCount > 0)
            {
                clickPosition = Input.GetTouch(0).position;
            }
            else
            {
                clickPosition = Input.mousePosition;
            }

            // Vérifier si le clic est en dehors du panel
            if (!IsClickInsidePanel(clickPosition))
            {
                HidePanel();
            }
        }
    }

    /// <summary>
    /// Vérifier si le clic est à l'intérieur du panel
    /// </summary>
    private bool IsClickInsidePanel(Vector2 screenPosition)
    {
        if (panel == null) return false;

        // Convertir la position d'écran en position Canvas
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect == null) return false;

        // Utiliser RectTransformUtility pour vérifier si le point est dans le rectangle
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas == null) return false;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPosition, cam);
    }

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

                if (activityRegistry != null && enableDebugLogs)
                {
                    Debug.Log($"VariantContainer: Auto-assigned ActivityRegistry: {activityRegistry.name}");
                }
            }
        }
    }
#endif

    #endregion
}