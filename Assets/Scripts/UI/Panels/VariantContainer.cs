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

    /// <summary>
    /// Obtenir tous les variants d'une activit�
    /// </summary>
    private List<ActivityVariant> GetVariantsForActivity(string activityId)
    {
        var variants = new List<ActivityVariant>();

#if UNITY_EDITOR
        // Rechercher tous les ActivityVariant dans le projet
        var variantGuids = UnityEditor.AssetDatabase.FindAssets("t:ActivityVariant");

        foreach (string guid in variantGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var variant = UnityEditor.AssetDatabase.LoadAssetAtPath<ActivityVariant>(path);

            if (variant != null && variant.ParentActivityID == activityId)
            {
                variants.Add(variant);
            }
        }
#endif

        // Trier par nom pour un affichage coh�rent
        return variants.OrderBy(v => v.VariantName).ToList();
    }

    /// <summary>
    /// Cr�er les ic�nes pour tous les variants
    /// </summary>
    private void CreateVariantIcons(List<ActivityVariant> variants)
    {
        // Nettoyer les ic�nes existants
        ClearVariantIcons();

        // Cr�er un ic�ne pour chaque variant
        foreach (var variant in variants)
        {
            CreateVariantIcon(variant);
        }
    }

    /// <summary>
    /// Cr�er un ic�ne pour un variant sp�cifique
    /// </summary>
    private void CreateVariantIcon(ActivityVariant variant)
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

        if (enableDebugLogs)
        {
            Debug.Log($"VariantContainer: Created icon for variant {variant.VariantName}");
        }
    }

    /// <summary>
    /// Nettoyer tous les ic�nes de variants existants
    /// </summary>
    private void ClearVariantIcons()
    {
        // D�truire tous les ic�nes existants
        foreach (var variantIcon in variantIcons)
        {
            if (variantIcon != null)
            {
                DestroyImmediate(variantIcon.gameObject);
            }
        }

        // Vider la liste
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

    #endregion
}