﻿// Purpose: Manages the variant display panel for a selected main activity
// Filepath: Assets/Scripts/UI/VariantContainer.cs
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VariantContainer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel; // Le panel qui contient tout
    [SerializeField] private Transform variantsContainer; // Container pour les icônes de variants
    [SerializeField] private GameObject variantIconPrefab; // Prefab pour chaque variant
    [SerializeField] private TextMeshProUGUI titleText; // Titre du panel (ex: "Mining Variants")
    [SerializeField] private Image activityHeaderIcon;

    [Header("Activity Registry")]
    [SerializeField] private ActivityRegistry activityRegistry; // Reference au registry des activites

    [Header("Panel Management")]
    [SerializeField] private GameObject activityXpContainer; // Reference au panel des activites principales

    [Header("Unknown Activity Settings")] // NOUVEAU
    [SerializeField] private Sprite unknownActivityIcon; // Icône "?" pour activite non decouverte
    [SerializeField] private string unknownActivityTitle = "Inconnu"; // Titre pour activite non decouverte

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Data
    private ActivityDefinition currentActivity;
    private List<VariantIconContainer> variantIcons = new List<VariantIconContainer>();

    #region Unity Lifecycle

    void Start()
    {
        // Essayer de trouver l'ActivityRegistry automatiquement s'il n'est pas assigne
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
        // Detecter les clics en dehors du panel quand il est ouvert
        if (panel != null && panel.activeInHierarchy)
        {
            DetectClickOutside();
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Afficher les variants pour une activite donnee
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

        // NOUVEAU : Gerer l'affichage selon si l'activite est decouverte ou non
        bool isActivityDiscovered = IsActivityDiscovered(activity.ActivityID);

        if (activityHeaderIcon != null)
        {
            // Mettre a jour l'icône de l'activite
            if (isActivityDiscovered)
            {
                activityHeaderIcon.sprite = activity.GetActivityIcon();
            }
            else
            {
                activityHeaderIcon.sprite = unknownActivityIcon; // Afficher le "?"
            }
        }

        // Mettre a jour le titre
        if (titleText != null)
        {
            if (isActivityDiscovered)
            {
                titleText.text = $"{activity.GetDisplayName()} Variants";
            }
            else
            {
                titleText.text = $"{unknownActivityTitle} Variants"; // Afficher "Inconnu Variants"
            }
        }

        // MODIFIe : Utiliser directement GetAllVariants() de l'ActivityDefinition !
        var variants = GetVariantsForActivity(activity);

        if (enableDebugLogs)
        {
            Debug.Log($"VariantContainer: Found {variants.Count} variants for {activity.GetDisplayName()} (Discovered: {isActivityDiscovered})");
        }

        // Creer les icônes de variants
        CreateVariantIcons(variants);

        // Masquer le panel des activites principales et afficher celui des variants
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

        // Reafficher le panel des activites principales
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
    /// Definir le registry d'activites
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
    /// Masquer le panel des activites principales
    /// </summary>
    private void HideActivityXpContainer()
    {
        if (activityXpContainer != null)
        {
            activityXpContainer.SetActive(false);
        }
    }

    /// <summary>
    /// Afficher le panel des activites principales
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
    /// MeTHODE CORRIGeE : Verifier si une activite a ete decouverte
    /// </summary>
    private bool IsActivityDiscovered(string activityId)
    {
        if (string.IsNullOrEmpty(activityId) || DataManager.Instance?.PlayerData == null)
        {
            return false;
        }

        // AJOUT : Normaliser l'ID comme dans IconContainer
        string normalizedActivityId = activityId.Trim().Replace(" ", "_");

        if (enableDebugLogs)
        {
            Debug.Log($"VariantContainer: Checking discovery for '{activityId}' (normalized: '{normalizedActivityId}')");
        }

        // Verifier si l'activite existe dans les Skills ET a de l'XP > 0
        var playerData = DataManager.Instance.PlayerData;
        bool hasSkill = playerData.Skills.ContainsKey(normalizedActivityId);
        int xp = hasSkill ? playerData.GetSkillXP(normalizedActivityId) : 0;
        bool isDiscovered = hasSkill && xp > 0;

        if (enableDebugLogs)
        {
            Debug.Log($"VariantContainer: Activity '{activityId}' - HasSkill: {hasSkill}, XP: {xp}, Discovered: {isDiscovered}");

            if (!hasSkill)
            {
                Debug.Log($"Available skills: [{string.Join(", ", playerData.Skills.Keys)}]");
            }
        }

        return isDiscovered;
    }

    /// <summary>
    /// MeTHODE SIMPLIFIeE : Obtenir tous les variants d'une activite - UTILISE GetAllVariants() !
    /// </summary>
    private List<ActivityVariant> GetVariantsForActivity(ActivityDefinition activity)
    {
        if (activity == null)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("VariantContainer: Cannot get variants for null activity");
            }
            return new List<ActivityVariant>();
        }

        // SUPER SIMPLE ! On utilise directement la methode de l'ActivityDefinition
        var variants = activity.GetAllVariants();

        // Trier par UnlockRequirement puis par nom pour un affichage coherent
        var sortedVariants = variants
            .Where(v => v != null) // Enlever les nulls
            .OrderBy(v => v.UnlockRequirement)
            .ThenBy(v => v.VariantName)
            .ToList();

        if (enableDebugLogs)
        {
            Debug.Log($"VariantContainer: Activity '{activity.ActivityID}' has {sortedVariants.Count} variants (from GetAllVariants())");
        }

        return sortedVariants;
    }

    /// <summary>
    /// Creer les icônes pour tous les variants - VERSION OPTIMISeE
    /// </summary>
    private void CreateVariantIcons(List<ActivityVariant> variants)
    {
        // Nettoyer les icônes existants de maniere optimisee
        ClearVariantIconsOptimized();

        if (variants.Count == 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log("VariantContainer: No variants to display");
            }
            return;
        }

        // Pre-allouer la liste pour eviter les reallocations
        variantIcons.Capacity = variants.Count;

        // Creer un icône pour chaque variant
        foreach (var variant in variants)
        {
            CreateVariantIconOptimized(variant);
        }
    }

    /// <summary>
    /// Creer un icône pour un variant specifique - VERSION OPTIMISeE
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

        // Initialiser avec les donnees du variant
        variantIcon.Initialize(variant);

        // Ajouter a la liste
        variantIcons.Add(variantIcon);

        // Logs reduits pour eviter le spam
        if (enableDebugLogs && variantIcons.Count == 1)
        {
            Debug.Log($"VariantContainer: Creating {variantIcons.Capacity} variant icons...");
        }
    }

    /// <summary>
    /// Nettoyer tous les icônes de variants existants - VERSION OPTIMISeE
    /// </summary>
    private void ClearVariantIconsOptimized()
    {
        // Batch destroy pour etre plus efficace
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
    /// Configurer la reference au ActivityXpContainer (appele par ActivityXpContainer)
    /// </summary>
    public void SetActivityXpContainer(GameObject container)
    {
        activityXpContainer = container;
    }

    /// <summary>
    /// Detecter les touches en dehors du panel pour le fermer
    /// </summary>
    private void DetectClickOutside()
    {
        // Verifier s'il y a un touch (priorite sur mobile)
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Vector2 touchPosition = Input.GetTouch(0).position;

            // Verifier si le touch est en dehors du panel
            if (!IsClickInsidePanel(touchPosition))
            {
                HidePanel();
            }
        }
        // Fallback pour les tests en editeur (souris)
#if UNITY_EDITOR
        else if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Input.mousePosition;

            // Verifier si le clic est en dehors du panel
            if (!IsClickInsidePanel(mousePosition))
            {
                HidePanel();
            }
        }
#endif
    }

    /// <summary>
    /// Verifier si le touch est a l'interieur du panel
    /// </summary>
    private bool IsClickInsidePanel(Vector2 screenPosition)
    {
        if (panel == null) return false;

        // Convertir la position d'ecran en position Canvas
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect == null) return false;

        // Utiliser RectTransformUtility pour verifier si le point est dans le rectangle
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas == null) return false;

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPosition, cam);
    }

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