// Purpose: Individual variant icon with XP progress ring and level display
// Filepath: Assets/Scripts/UI/VariantIconContainer.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VariantIconContainer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image variantIcon;
    [SerializeField] private Image progressRing;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI variantNameText; // Nom du variant

    [Header("Settings")]
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private float refreshInterval = 1f;

    [Header("Visual Settings")]
    [SerializeField] private Color maxLevelColor = Color.yellow;
    [SerializeField] private Sprite defaultIcon;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Data
    private ActivityVariant activityVariant;
    private string variantId;

    // Cache pour optimisation
    private SkillData cachedSubSkillData;
    private float lastProgressValue = -1f;
    private int lastLevel = -1;

    #region Unity Lifecycle

    void Start()
    {
        // Attendre que XpManager soit prêt avant l'initialisation
        StartCoroutine(DelayedInitialization());
    }

    /// <summary>
    /// Initialisation retardée pour s'assurer que XpManager est prêt
    /// </summary>
    private System.Collections.IEnumerator DelayedInitialization()
    {
        // Attendre que XpManager soit disponible
        while (XpManager.Instance == null)
        {
            yield return new WaitForEndOfFrame();
        }

        // Maintenant on peut faire l'initialisation
        SetupProgressRing();
        RefreshDisplay();

        if (autoRefresh)
        {
            InvokeRepeating(nameof(RefreshDisplay), refreshInterval, refreshInterval);
        }
    }

    void OnDestroy()
    {
        // Nettoyer les InvokeRepeating
        CancelInvoke();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Initialiser l'icône avec un ActivityVariant
    /// </summary>
    public void Initialize(ActivityVariant variant)
    {
        activityVariant = variant;

        if (variant != null)
        {
            // CORRECTION : Utiliser une méthode standardisée pour générer l'ID
            variantId = GenerateStandardizedVariantId(variant);

            if (enableDebugLogs)
            {
                Debug.Log($"VariantIconContainer: Initialized with ID '{variantId}' for variant '{variant.VariantName}'");
            }

            // Configuration visuelle initiale
            SetupVisualElements();

            // Première mise à jour
            RefreshDisplay();
        }
    }

    /// <summary>
    /// Actualiser l'affichage (niveau, progression, icône)
    /// </summary>
    public void RefreshDisplay()
    {
        if (string.IsNullOrEmpty(variantId) || XpManager.Instance == null)
            return;

        // Obtenir les données de sous-compétence actuelles (variants utilisent SubSkills)
        var subSkillData = XpManager.Instance.GetPlayerSubSkill(variantId);

        if (enableDebugLogs && (subSkillData.Level != lastLevel || Mathf.Abs(GetProgressToNextLevel(subSkillData) - lastProgressValue) > 0.01f))
        {
            Debug.Log($"VariantIconContainer: Refreshing display for '{variantId}' - Level: {subSkillData.Level}, XP: {subSkillData.Experience}");
        }

        // Mettre à jour le niveau
        UpdateLevelDisplay(subSkillData);

        // Mettre à jour la progression
        UpdateProgressRing(subSkillData);

        // Sauvegarder les valeurs en cache
        cachedSubSkillData = subSkillData;
        lastLevel = subSkillData.Level;
        lastProgressValue = GetProgressToNextLevel(subSkillData);
    }

    /// <summary>
    /// Obtenir l'ID du variant
    /// </summary>
    public string GetVariantId()
    {
        return variantId;
    }

    /// <summary>
    /// Obtenir les données de sous-compétence actuelles
    /// </summary>
    public SkillData GetCurrentSubSkillData()
    {
        if (cachedSubSkillData.SkillId == variantId)
        {
            return cachedSubSkillData;
        }

        return XpManager.Instance?.GetPlayerSubSkill(variantId) ?? new SkillData(variantId, 1, 0);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// NOUVELLE MÉTHODE : Générer un ID standardisé pour le variant
    /// Cette méthode assure la cohérence avec XpManager et XpReward
    /// </summary>
    private string GenerateStandardizedVariantId(ActivityVariant variant)
    {
        if (variant == null) return "";

        // Priorité 1 : Utiliser la méthode GetSubSkillId() du variant si elle existe
        string skillId = variant.GetSubSkillId();

        if (!string.IsNullOrEmpty(skillId))
        {
            // Appliquer la même logique de validation que XpReward
            return ValidateAndNormalizeSkillId(skillId);
        }

        // Priorité 2 : Générer à partir du nom du variant
        if (!string.IsNullOrEmpty(variant.VariantName))
        {
            return ValidateAndNormalizeSkillId(variant.VariantName);
        }

        // Priorité 3 : Fallback avec ParentActivityID + nom
        return ValidateAndNormalizeSkillId($"{variant.ParentActivityID}_{variant.name}");
    }

    /// <summary>
    /// NOUVELLE MÉTHODE : Valider et normaliser un ID de compétence
    /// Utilise la même logique que XpReward.ValidateSkillId()
    /// </summary>
    private string ValidateAndNormalizeSkillId(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId)) return "";

        // Nettoyer l'ID : supprimer les espaces de début/fin, convertir espaces internes en underscores
        string normalizedId = skillId.Trim().Replace(" ", "_");

        if (enableDebugLogs)
        {
            Debug.Log($"VariantIconContainer: Normalized '{skillId}' to '{normalizedId}'");
        }

        return normalizedId;
    }

    /// <summary>
    /// Configuration initiale de l'anneau de progression
    /// </summary>
    private void SetupProgressRing()
    {
        if (progressRing != null)
        {
            progressRing.type = Image.Type.Filled;
            progressRing.fillMethod = Image.FillMethod.Radial360;
            progressRing.fillOrigin = (int)Image.Origin360.Top;
            progressRing.fillClockwise = true;
            progressRing.fillAmount = 0f;
        }
    }

    /// <summary>
    /// Configuration des éléments visuels de base
    /// </summary>
    private void SetupVisualElements()
    {
        if (activityVariant == null) return;

        // Configurer l'icône du variant
        if (variantIcon != null)
        {
            var iconSprite = GetVariantIcon();
            variantIcon.sprite = iconSprite ?? defaultIcon;
        }

        // Configurer le nom du variant
        if (variantNameText != null)
        {
            variantNameText.text = activityVariant.VariantName;
        }

        // Configurer le texte de niveau
        if (levelText != null)
        {
            levelText.text = "Lvl. 1";
        }
    }

    /// <summary>
    /// Mettre à jour l'affichage du niveau
    /// </summary>
    private void UpdateLevelDisplay(SkillData subSkillData)
    {
        if (levelText == null) return;

        levelText.text = $"Lvl. {subSkillData.Level}";

        // Changer la couleur si niveau maximum
        if (subSkillData.Level >= XpManager.Instance.MaxLevel)
        {
            levelText.color = maxLevelColor;
        }
    }

    /// <summary>
    /// Mettre à jour l'anneau de progression
    /// </summary>
    private void UpdateProgressRing(SkillData subSkillData)
    {
        if (progressRing == null) return;

        // Calculer la progression vers le niveau suivant
        float progress = GetProgressToNextLevel(subSkillData);

        // Animer la progression si elle a changé
        if (!Mathf.Approximately(progress, progressRing.fillAmount))
        {
            progressRing.fillAmount = progress;
        }

        // Changer la couleur selon le niveau
        if (subSkillData.Level >= XpManager.Instance.MaxLevel)
        {
            progressRing.color = maxLevelColor;
            progressRing.fillAmount = 1f; // Complet au niveau max
        }
    }

    /// <summary>
    /// Calculer la progression vers le niveau suivant (0.0 à 1.0)
    /// </summary>
    private float GetProgressToNextLevel(SkillData subSkillData)
    {
        if (XpManager.Instance == null) return 0f;

        return XpManager.Instance.GetProgressToNextLevel(subSkillData);
    }

    /// <summary>
    /// Obtenir l'icône du variant
    /// </summary>
    private Sprite GetVariantIcon()
    {
        // Si le variant a sa propre icône, l'utiliser
        if (activityVariant != null && activityVariant.VariantIcon != null)
        {
            return activityVariant.VariantIcon;
        }

        // Sinon, essayer de charger une icône basée sur le nom du variant
        return LoadIconByVariantName(activityVariant?.VariantName);
    }

    /// <summary>
    /// Charger une icône basée sur le nom du variant
    /// </summary>
    private Sprite LoadIconByVariantName(string variantName)
    {
        if (string.IsNullOrEmpty(variantName)) return null;

        // Chercher dans un dossier d'icônes de variants
        string iconPath = $"UI/Icons/Variants/{variantName.Replace(" ", "_")}";
        return Resources.Load<Sprite>(iconPath);
    }

    #endregion
}