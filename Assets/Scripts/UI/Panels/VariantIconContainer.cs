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
            variantId = GetVariantId(variant);

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

        // Forcer la mise � jour si c'est la premi�re fois ou si les donn�es ont chang�
        bool forceUpdate = lastLevel == -1 || lastProgressValue < 0f;
        bool hasChanged = subSkillData.Level != lastLevel ||
                         !Mathf.Approximately(GetProgressToNextLevel(subSkillData), lastProgressValue);

        if (!forceUpdate && !hasChanged)
        {
            return; // Pas de changement, pas besoin de mettre � jour
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

        return XpManager.Instance?.GetPlayerSubSkill(variantId) ?? default;
    }

    #endregion

    #region Private Methods

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
        if (subSkillData.Level >= 100) // Utiliser une valeur fixe
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
        if (subSkillData.Level >= 100) // Utiliser une valeur fixe
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
    /// Obtenir l'ID unique pour ce variant (utilis� pour les SubSkills)
    /// </summary>
    private string GetVariantId(ActivityVariant variant)
    {
        // Cr�er un ID unique pour ce variant
        // Vous pouvez ajuster cette logique selon votre syst�me
        if (!string.IsNullOrEmpty(variant.VariantName))
        {
            return variant.VariantName.Replace(" ", "_");
        }

        return $"{variant.ParentActivityID}_{variant.name}";
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