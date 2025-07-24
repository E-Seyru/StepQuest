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
        // Attendre que XpManager soit pr�t avant l'initialisation
        StartCoroutine(DelayedInitialization());
    }

    /// <summary>
    /// Initialisation retard�e pour s'assurer que XpManager est pr�t
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
    /// Initialiser l'ic�ne avec un ActivityVariant
    /// </summary>
    public void Initialize(ActivityVariant variant)
    {
        activityVariant = variant;

        if (variant != null)
        {
            // CORRECTION : Utiliser une m�thode standardis�e pour g�n�rer l'ID
            variantId = GenerateStandardizedVariantId(variant);

            if (enableDebugLogs)
            {
                Debug.Log($"VariantIconContainer: Initialized with ID '{variantId}' for variant '{variant.VariantName}'");
            }

            // Configuration visuelle initiale
            SetupVisualElements();

            // Premi�re mise � jour
            RefreshDisplay();
        }
    }

    /// <summary>
    /// Actualiser l'affichage (niveau, progression, ic�ne)
    /// </summary>
    public void RefreshDisplay()
    {
        if (string.IsNullOrEmpty(variantId) || XpManager.Instance == null)
            return;

        // Obtenir les donn�es de sous-comp�tence actuelles (variants utilisent SubSkills)
        var subSkillData = XpManager.Instance.GetPlayerSubSkill(variantId);

        if (enableDebugLogs && (subSkillData.Level != lastLevel || Mathf.Abs(GetProgressToNextLevel(subSkillData) - lastProgressValue) > 0.01f))
        {
            Debug.Log($"VariantIconContainer: Refreshing display for '{variantId}' - Level: {subSkillData.Level}, XP: {subSkillData.Experience}");
        }

        // Mettre � jour le niveau
        UpdateLevelDisplay(subSkillData);

        // Mettre � jour la progression
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
    /// Obtenir les donn�es de sous-comp�tence actuelles
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
    /// NOUVELLE M�THODE : G�n�rer un ID standardis� pour le variant
    /// Cette m�thode assure la coh�rence avec XpManager et XpReward
    /// </summary>
    private string GenerateStandardizedVariantId(ActivityVariant variant)
    {
        if (variant == null) return "";

        // Priorit� 1 : Utiliser la m�thode GetSubSkillId() du variant si elle existe
        string skillId = variant.GetSubSkillId();

        if (!string.IsNullOrEmpty(skillId))
        {
            // Appliquer la m�me logique de validation que XpReward
            return ValidateAndNormalizeSkillId(skillId);
        }

        // Priorit� 2 : G�n�rer � partir du nom du variant
        if (!string.IsNullOrEmpty(variant.VariantName))
        {
            return ValidateAndNormalizeSkillId(variant.VariantName);
        }

        // Priorit� 3 : Fallback avec ParentActivityID + nom
        return ValidateAndNormalizeSkillId($"{variant.ParentActivityID}_{variant.name}");
    }

    /// <summary>
    /// NOUVELLE M�THODE : Valider et normaliser un ID de comp�tence
    /// Utilise la m�me logique que XpReward.ValidateSkillId()
    /// </summary>
    private string ValidateAndNormalizeSkillId(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId)) return "";

        // Nettoyer l'ID : supprimer les espaces de d�but/fin, convertir espaces internes en underscores
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
    /// Configuration des �l�ments visuels de base
    /// </summary>
    private void SetupVisualElements()
    {
        if (activityVariant == null) return;

        // Configurer l'ic�ne du variant
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
    /// Mettre � jour l'affichage du niveau
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
    /// Mettre � jour l'anneau de progression
    /// </summary>
    private void UpdateProgressRing(SkillData subSkillData)
    {
        if (progressRing == null) return;

        // Calculer la progression vers le niveau suivant
        float progress = GetProgressToNextLevel(subSkillData);

        // Animer la progression si elle a chang�
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
    /// Calculer la progression vers le niveau suivant (0.0 � 1.0)
    /// </summary>
    private float GetProgressToNextLevel(SkillData subSkillData)
    {
        if (XpManager.Instance == null) return 0f;

        return XpManager.Instance.GetProgressToNextLevel(subSkillData);
    }

    /// <summary>
    /// Obtenir l'ic�ne du variant
    /// </summary>
    private Sprite GetVariantIcon()
    {
        // Si le variant a sa propre ic�ne, l'utiliser
        if (activityVariant != null && activityVariant.VariantIcon != null)
        {
            return activityVariant.VariantIcon;
        }

        // Sinon, essayer de charger une ic�ne bas�e sur le nom du variant
        return LoadIconByVariantName(activityVariant?.VariantName);
    }

    /// <summary>
    /// Charger une ic�ne bas�e sur le nom du variant
    /// </summary>
    private Sprite LoadIconByVariantName(string variantName)
    {
        if (string.IsNullOrEmpty(variantName)) return null;

        // Chercher dans un dossier d'ic�nes de variants
        string iconPath = $"UI/Icons/Variants/{variantName.Replace(" ", "_")}";
        return Resources.Load<Sprite>(iconPath);
    }

    #endregion
}