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
    [SerializeField] private Transform variantsContainer; // Container pour les ic�nes de variants
    [SerializeField] private GameObject variantIconPrefab; // Prefab pour chaque variant
    [SerializeField] private TextMeshProUGUI titleText; // Titre du panel (ex: "Mining Variants")

    [Header("Activity Registry")]
    [SerializeField] private ActivityRegistry activityRegistry; // R�f�rence au registry des activit�s

    [Header("Panel Management")]
    [SerializeField] private GameObject activityXpContainer; // R�f�rence au panel des activit�s principales

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Data
    private ActivityDefinition currentActivity;
    private List<VariantIconContainer> variantIcons = new List<VariantIconContainer>();

    #region Unity Lifecycle

    void Start()
    {
        // Essayer de trouver l'ActivityRegistry automatiquement s'il n'est pas assign�
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
        // D�tecter les clics en dehors du panel quand il est ouvert
        if (panel != null && panel.activeInHierarchy)
        {
            DetectClickOutside();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Afficher les variants pour une activit� donn�e
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

        // Mettre � jour le titre
        if (titleText != null)
        {
            titleText.text = $"{activity.GetDisplayName()} Variants";
        }

        // Obtenir tous les variants pour cette activit�
        var variants = GetVariantsForActivity(activity.ActivityID);

        if (enableDebugLogs)
        {
            Debug.Log($"VariantContainer: Found {variants.Count} variants for {activity.GetDisplayName()}");
        }

        // Cr�er les ic�nes de variants
        CreateVariantIcons(variants);

        // Masquer le panel des activit�s principales et afficher celui des variants
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

        // R�afficher le panel des activit�s principales
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
    /// D�finir le registry d'activit�s
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
    /// Masquer le panel des activit�s principales
    /// </summary>
    private void HideActivityXpContainer()
    {
        if (activityXpContainer != null)
        {
            activityXpContainer.SetActive(false);
        }
    }

    /// <summary>
    /// Afficher le panel des activit�s principales
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

    // Cache pour �viter de recalculer les variants � chaque fois
    private Dictionary<string, List<ActivityVariant>> variantsCache = new Dictionary<string, List<ActivityVariant>>();

    /// <summary>
    /// Obtenir tous les variants d'une activit� via le registry - VERSION OPTIMIS�E
    /// </summary>
    private List<ActivityVariant> GetVariantsForActivity(string activityId)
    {
        // V�rifier le cache d'abord
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

            // Si c'est l'activit� qu'on cherche
            if (locationActivity.ActivityReference.ActivityID == activityId)
            {
                // R�cup�rer les variants de cette LocationActivity (plus rapide)
                if (locationActivity.ActivityVariants != null)
                {
                    foreach (var variant in locationActivity.ActivityVariants)
                    {
                        if (variant != null) // Enlever IsValidVariant() qui peut �tre lent
                        {
                            variants.Add(variant);
                        }
                    }
                }

                // On a trouv� l'activit�, pas besoin de continuer
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
    /// Vider le cache des variants (� appeler si les donn�es changent)
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
    /// Cr�er les ic�nes pour tous les variants - VERSION OPTIMIS�E
    /// </summary>
    private void CreateVariantIcons(List<ActivityVariant> variants)
    {
        // Nettoyer les ic�nes existants de mani�re optimis�e
        ClearVariantIconsOptimized();

        if (variants.Count == 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log("VariantContainer: No variants to display");
            }
            return;
        }

        // Pr�-allouer la liste pour �viter les r�allocations
        variantIcons.Capacity = variants.Count;

        // Cr�er un ic�ne pour chaque variant
        foreach (var variant in variants)
        {
            CreateVariantIconOptimized(variant);
        }
    }

    /// <summary>
    /// Cr�er un ic�ne pour un variant sp�cifique - VERSION OPTIMIS�E
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

        // Initialiser avec les donn�es du variant
        variantIcon.Initialize(variant);

        // Ajouter � la liste
        variantIcons.Add(variantIcon);

        // Logs r�duits pour �viter le spam
        if (enableDebugLogs && variantIcons.Count == 1)
        {
            Debug.Log($"VariantContainer: Creating {variantIcons.Capacity} variant icons...");
        }
    }

    /// <summary>
    /// Nettoyer tous les ic�nes de variants existants - VERSION OPTIMIS�E
    /// </summary>
    private void ClearVariantIconsOptimized()
    {
        // Batch destroy pour �tre plus efficace
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
    /// Configurer la r�f�rence au ActivityXpContainer (appel� par ActivityXpContainer)
    /// </summary>
    public void SetActivityXpContainer(GameObject container)
    {
        activityXpContainer = container;
    }

    /// <summary>
    /// D�tecter les clics en dehors du panel pour le fermer
    /// </summary>
    private void DetectClickOutside()
    {
        // V�rifier s'il y a un clic de souris ou un touch
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

            // V�rifier si le clic est en dehors du panel
            if (!IsClickInsidePanel(clickPosition))
            {
                HidePanel();
            }
        }
    }

    /// <summary>
    /// V�rifier si le clic est � l'int�rieur du panel
    /// </summary>
    private bool IsClickInsidePanel(Vector2 screenPosition)
    {
        if (panel == null) return false;

        // Convertir la position d'�cran en position Canvas
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect == null) return false;

        // Utiliser RectTransformUtility pour v�rifier si le point est dans le rectangle
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas == null) return false;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPosition, cam);
    }

#if UNITY_EDITOR
    /// <summary>
    /// M�thode d'assistance pour l'�diteur - auto-assignment du registry
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