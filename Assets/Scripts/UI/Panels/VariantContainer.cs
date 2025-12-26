// Purpose: Manages the variant display panel for a selected main activity
// Filepath: Assets/Scripts/UI/VariantContainer.cs
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VariantContainer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform variantsContainer;
    [SerializeField] private GameObject variantIconPrefab;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image activityHeaderIcon;

    [Header("Activity Registry")]
    [SerializeField] private ActivityRegistry activityRegistry;

    [Header("Panel Management")]
    [SerializeField] private GameObject activityXpContainer;

    [Header("Unknown Activity Settings")]
    [SerializeField] private Sprite unknownActivityIcon;
    [SerializeField] private string unknownActivityTitle = "Inconnu";

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Data
    private ActivityDefinition currentActivity;

    // MODIFICATION : La liste devient notre pool d'objets.
    private List<VariantIconContainer> variantIconPool = new List<VariantIconContainer>();

    #region Unity Lifecycle

    void Start()
    {
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
        if (panel != null && panel.activeInHierarchy)
        {
            DetectClickOutside();
        }
    }

    #endregion

    #region Public Methods

    public void ShowVariantsForActivity(ActivityDefinition activity)
    {
        if (activity == null)
        {
            if (enableDebugLogs)
            {
                Logger.LogWarning("VariantContainer: Cannot show variants for null activity", Logger.LogCategory.ActivityLog);
            }
            return;
        }

        currentActivity = activity;
        bool isActivityDiscovered = IsActivityDiscovered(activity.ActivityID);

        if (activityHeaderIcon != null)
        {
            activityHeaderIcon.sprite = isActivityDiscovered ? activity.GetActivityIcon() : unknownActivityIcon;
        }

        if (titleText != null)
        {
            titleText.text = isActivityDiscovered ? $"{activity.GetDisplayName()}" : $"{unknownActivityTitle}";
        }

        var variants = GetVariantsForActivity(activity);

        if (enableDebugLogs)
        {
            Logger.LogInfo($"VariantContainer: Found {variants.Count} variants for {activity.GetDisplayName()}", Logger.LogCategory.ActivityLog);
        }

        // MODIFICATION : On appelle notre nouvelle méthode de gestion du pool.
        PopulateVariantIcons(variants);

        HideActivityXpContainer();
        ShowPanel();
    }

    public void HidePanel()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
        ShowActivityXpContainer();
    }

    public void ShowPanel()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    public void SetActivityXpContainer(GameObject container)
    {
        activityXpContainer = container;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// NOUVELLE MÉTHODE : Peuple le conteneur avec des icônes depuis le pool.
    /// </summary>
    private void PopulateVariantIcons(List<ActivityVariant> variants)
    {
        // 1. Désactiver tous les icônes actuellement visibles dans le pool.
        foreach (var icon in variantIconPool)
        {
            if (icon.gameObject.activeSelf)
            {
                icon.gameObject.SetActive(false);
            }
        }

        // 2. Parcourir les variants et activer/configurer les icônes nécessaires.
        for (int i = 0; i < variants.Count; i++)
        {
            // Si on a besoin de plus d'icônes que ce que le pool contient, on en crée de nouveaux.
            if (i >= variantIconPool.Count)
            {
                CreateAndPoolNewIcon();
            }

            // On récupère l'icône depuis le pool, on le configure et on l'active.
            VariantIconContainer pooledIcon = variantIconPool[i];
            pooledIcon.Initialize(variants[i]);
            pooledIcon.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// NOUVELLE MÉTHODE : Crée un icône, l'ajoute au pool et le prépare.
    /// </summary>
    private void CreateAndPoolNewIcon()
    {
        if (variantIconPrefab == null || variantsContainer == null)
        {
            Logger.LogError("VariantContainer: variantIconPrefab or variantsContainer is null!", Logger.LogCategory.ActivityLog);
            return;
        }

        GameObject iconObject = Instantiate(variantIconPrefab, variantsContainer);
        iconObject.name = $"VariantIcon_Pooled_{variantIconPool.Count}";

        var newIcon = iconObject.GetComponent<VariantIconContainer>();
        variantIconPool.Add(newIcon);
        newIcon.gameObject.SetActive(false); // On le désactive en attendant son utilisation.
    }

    private bool IsActivityDiscovered(string activityId)
    {
        if (string.IsNullOrEmpty(activityId) || DataManager.Instance?.PlayerData == null)
        {
            return false;
        }

        string normalizedActivityId = activityId.Trim().Replace(" ", "_");

        var playerData = DataManager.Instance.PlayerData;
        return playerData.Skills.ContainsKey(normalizedActivityId) && playerData.GetSkillXP(normalizedActivityId) > 0;
    }

    private List<ActivityVariant> GetVariantsForActivity(ActivityDefinition activity)
    {
        if (activity == null)
        {
            return new List<ActivityVariant>();
        }

        return activity.GetAllVariants()
            .Where(v => v != null)
            .OrderBy(v => v.UnlockRequirement)
            .ThenBy(v => v.VariantName)
            .ToList();
    }

    private void HideActivityXpContainer()
    {
        if (activityXpContainer != null)
        {
            activityXpContainer.SetActive(false);
        }
    }

    private void ShowActivityXpContainer()
    {
        if (activityXpContainer != null)
        {
            activityXpContainer.SetActive(true);
        }
    }

    private void DetectClickOutside()
    {
        bool clickDetected = false;
        Vector2 clickPosition = Vector2.zero;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            clickDetected = true;
            clickPosition = Input.GetTouch(0).position;
        }
        else if (Input.GetMouseButtonDown(0))
        {
            clickDetected = true;
            clickPosition = Input.mousePosition;
        }

        if (clickDetected)
        {
            if (!IsClickInsidePanel(clickPosition))
            {
                HidePanel();
            }
        }
    }

    private bool IsClickInsidePanel(Vector2 screenPosition)
    {
        if (panel == null) return false;

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        if (panelRect == null) return false;

        Canvas canvas = panel.GetComponentInParent<Canvas>();
        if (canvas == null) return false;

        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(panelRect, screenPosition, cam);
    }

    #endregion
}