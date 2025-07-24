// Purpose: Individual skill icon with XP progress ring and level display
// Filepath: Assets/Scripts/UI/IconContainer.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class IconContainer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image skillIcon;
    [SerializeField] private Image progressRing;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button iconButton;

    [Header("Panel References")]
    [SerializeField] private VariantContainer variantContainer; // Reference directe

    [Header("Settings")]
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private float refreshInterval = 1f;

    [Header("Visual Settings")]
    [SerializeField] private Color maxLevelColor = Color.yellow;
    [SerializeField] private Sprite defaultIcon;
    [SerializeField] private Sprite unknownActivityIcon; // NOUVEAU : Icône "?" pour activités non découvertes

    // Data
    private ActivityDefinition activityDefinition;
    private string activityId;
    private string mainSkillId;

    // Cache pour optimisation
    private SkillData cachedSkillData;
    private float lastProgressValue = -1f;
    private int lastLevel = -1;

    #region Unity Lifecycle

    void Start()
    {
        // Attendre que XpManager soit pret avant l'initialisation
        StartCoroutine(DelayedInitialization());
    }

    /// <summary>
    /// Initialisation retardee pour s'assurer que XpManager est pret
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
        SetupButton();
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
    /// Initialiser l'icône avec une ActivityDefinition
    /// </summary>
    public void Initialize(ActivityDefinition activity)
    {
        activityDefinition = activity;

        if (activity != null)
        {
            activityId = activity.ActivityID;
            mainSkillId = GetMainSkillForActivity(activityId);

            // Configuration visuelle initiale
            SetupVisualElements();

            // Premiere mise a jour
            RefreshDisplay();
        }
    }

    /// <summary>
    /// Actualiser l'affichage (niveau, progression, icône)
    /// </summary>
    public void RefreshDisplay()
    {
        if (string.IsNullOrEmpty(mainSkillId) || XpManager.Instance == null)
            return;

        // Obtenir les donnees de competence actuelles
        var skillData = XpManager.Instance.GetPlayerSkill(mainSkillId);

        // NOUVEAU : Mettre à jour l'icône en cas de découverte
        UpdateIconDisplay();

        // Mettre a jour le niveau
        UpdateLevelDisplay(skillData);

        // Mettre a jour la progression
        UpdateProgressRing(skillData);

        // Sauvegarder les valeurs en cache
        cachedSkillData = skillData;
        lastLevel = skillData.Level;
        lastProgressValue = GetProgressToNextLevel(skillData);
    }

    /// <summary>
    /// Obtenir l'ID de l'activite associee
    /// </summary>
    public string GetActivityId()
    {
        return activityId;
    }

    /// <summary>
    /// Obtenir les donnees de competence actuelles
    /// </summary>
    public SkillData GetCurrentSkillData()
    {
        if (cachedSkillData.SkillId == mainSkillId)
        {
            return cachedSkillData;
        }

        return XpManager.Instance?.GetPlayerSkill(mainSkillId) ?? default;
    }

    /// <summary>
    /// Configurer la reference au VariantContainer (appele par ActivityXpContainer)
    /// </summary>
    public void SetVariantContainer(VariantContainer container)
    {
        variantContainer = container;
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
    /// Configuration du bouton pour ouvrir les variants
    /// </summary>
    private void SetupButton()
    {
        if (iconButton != null)
        {
            iconButton.onClick.AddListener(OnIconClicked);
        }
    }

    /// <summary>
    /// Gestion du clic sur l'icône d'activite
    /// </summary>
    private void OnIconClicked()
    {
        if (activityDefinition != null && variantContainer != null)
        {
            // Ouvrir le panel des variants pour cette activite
            variantContainer.ShowVariantsForActivity(activityDefinition);
        }
        else if (variantContainer == null)
        {
            Debug.LogWarning($"IconContainer: VariantContainer reference not set in {gameObject.name}!");
        }
    }

    /// <summary>
    /// Configuration des elements visuels de base
    /// </summary>
    private void SetupVisualElements()
    {
        if (activityDefinition == null) return;

        // Configurer l'icône principale
        if (skillIcon != null)
        {
            // MODIFIÉ : Vérifier si l'activité a été découverte
            var iconSprite = GetDisplayIcon();
            skillIcon.sprite = iconSprite ?? defaultIcon;
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
    private void UpdateLevelDisplay(SkillData skillData)
    {
        if (levelText == null) return;

        levelText.text = $"Lvl. {skillData.Level}";

        // Changer la couleur si niveau maximum
        if (skillData.Level >= 100) // Utiliser une valeur fixe ou creer une propriete publique
        {
            levelText.color = maxLevelColor;
        }
    }

    /// <summary>
    /// Mettre a jour l'anneau de progression
    /// </summary>
    private void UpdateProgressRing(SkillData skillData)
    {
        if (progressRing == null) return;

        // Calculer la progression vers le niveau suivant
        float progress = GetProgressToNextLevel(skillData);

        // Animer la progression si elle a change
        if (!Mathf.Approximately(progress, progressRing.fillAmount))
        {
            // Animation simple (vous pouvez ameliorer avec DOTween si disponible)
            progressRing.fillAmount = progress;
        }

        // Changer la couleur selon le niveau
        if (skillData.Level >= 100) // Utiliser une valeur fixe ou creer une propriete publique
        {
            progressRing.color = maxLevelColor;
            progressRing.fillAmount = 1f; // Complet au niveau max
        }

    }

    /// <summary>
    /// Obtenir l'icône à afficher (normale ou "?" si pas découverte)
    /// </summary>
    private Sprite GetDisplayIcon()
    {
        // Vérifier si l'activité a été découverte
        if (!IsActivityDiscovered())
        {
            return unknownActivityIcon; // Afficher le "?"
        }

        // Activité découverte : afficher l'icône normale
        return GetActivityIcon();
    }

    /// <summary>
    /// Vérifier si une activité a été découverte (pratiquée au moins une fois)
    /// </summary>
    private bool IsActivityDiscovered()
    {
        if (string.IsNullOrEmpty(mainSkillId) || DataManager.Instance?.PlayerData == null)
        {
            return false; // Pas découverte si pas d'ID ou pas de données
        }

        // Vérifier si l'activité existe dans les Skills ET a de l'XP > 0
        var playerData = DataManager.Instance.PlayerData;
        return playerData.Skills.ContainsKey(mainSkillId) &&
               playerData.GetSkillXP(mainSkillId) > 0;
    }

    /// <summary>
    /// Mettre à jour l'icône affichée selon l'état de découverte
    /// </summary>
    private void UpdateIconDisplay()
    {
        if (skillIcon != null)
        {
            var iconSprite = GetDisplayIcon();
            skillIcon.sprite = iconSprite ?? defaultIcon;
        }
    }

    /// <summary>
    /// Calculer la progression vers le niveau suivant (0.0 a 1.0)
    /// </summary>
    private float GetProgressToNextLevel(SkillData skillData)
    {
        if (XpManager.Instance == null) return 0f;

        return XpManager.Instance.GetProgressToNextLevel(skillData);
    }

    /// <summary>
    /// Obtenir la competence principale associee a cette activite
    /// MODIFIÉ : Normaliser l'ID pour éviter les problèmes avec les espaces
    /// </summary>
    private string GetMainSkillForActivity(string activityId)
    {
        if (string.IsNullOrEmpty(activityId))
            return "";

        // Normaliser l'ID comme dans VariantIconContainer
        string normalizedId = activityId.Trim().Replace(" ", "_");

        return normalizedId;
    }

    /// <summary>
    /// Obtenir l'icône de l'activite
    /// </summary>
    private Sprite GetActivityIcon()
    {
        // Si l'ActivityDefinition a une icône, l'utiliser
        if (activityDefinition != null && activityDefinition.ActivityIcon != null)
        {
            return activityDefinition.ActivityIcon;
        }

        // Sinon, essayer de charger une icône basee sur l'ID
        return LoadIconByActivityId(activityId);
    }

    /// <summary>
    /// Charger une icône basee sur l'ID d'activite
    /// </summary>
    private Sprite LoadIconByActivityId(string activityId)
    {
        // Chercher dans un dossier d'icônes (adaptez le chemin selon votre projet)
        string iconPath = $"UI/Icons/Activities/{activityId}";
        return Resources.Load<Sprite>(iconPath);
    }

    #endregion
}