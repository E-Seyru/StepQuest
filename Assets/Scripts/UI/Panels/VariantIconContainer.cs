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
    private bool isInitialized = false;

    // Cache statique pour éviter les recalculs d'ID - avec méthode de nettoyage
    private static readonly System.Collections.Generic.Dictionary<ActivityVariant, string> variantIdCache =
        new System.Collections.Generic.Dictionary<ActivityVariant, string>();

    #region Unity Lifecycle

    void Start()
    {
        // Initialisation simplifiée - pas de coroutine coûteuse
        InitializeOptimized();
    }

    void OnDestroy()
    {
        // Nettoyer les InvokeRepeating
        CancelInvoke();
    }

    void OnDisable()
    {
        // Arrêter le timer dès que l'icône est masquée
        CancelInvoke();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Initialiser l'icône avec un ActivityVariant - VERSION OPTIMISÉE
    /// </summary>
    public void Initialize(ActivityVariant variant)
    {
        activityVariant = variant;

        if (variant != null)
        {
            // Utiliser le cache pour éviter de recalculer l'ID
            variantId = GetCachedVariantId(variant);

            // Configuration visuelle initiale (seulement si pas encore fait)
            if (!isInitialized)
            {
                SetupVisualElements();
                SetupProgressRing();
                isInitialized = true;
            }

            // Première mise à jour seulement si XpManager est disponible
            if (XpManager.Instance != null)
            {
                RefreshDisplay();

                // Démarrer l'auto-refresh seulement maintenant ET vérifier si pas déjà en cours
                if (autoRefresh && !IsInvoking(nameof(RefreshDisplay)))
                {
                    InvokeRepeating(nameof(RefreshDisplay), refreshInterval, refreshInterval);
                }
            }
        }
    }

    /// <summary>
    /// Actualiser l'affichage (niveau, progression, icône) - VERSION OPTIMISÉE
    /// </summary>
    public void RefreshDisplay()
    {
        if (string.IsNullOrEmpty(variantId) || XpManager.Instance == null)
            return;

        // Obtenir les données de sous-compétence actuelles (variants utilisent SubSkills)
        var subSkillData = XpManager.Instance.GetPlayerSubSkill(variantId);

        // Optimisation : seulement mettre à jour si les données ont changé
        bool hasLevelChanged = subSkillData.Level != lastLevel;
        float currentProgress = GetProgressToNextLevel(subSkillData);
        bool hasProgressChanged = Mathf.Abs(currentProgress - lastProgressValue) > 0.01f;

        if (!hasLevelChanged && !hasProgressChanged)
        {
            return; // Rien à mettre à jour
        }

        // Mettre à jour seulement ce qui a changé
        if (hasLevelChanged)
        {
            UpdateLevelDisplay(subSkillData);
            lastLevel = subSkillData.Level;
        }

        if (hasProgressChanged)
        {
            UpdateProgressRing(subSkillData);
            lastProgressValue = currentProgress;
        }

        // Sauvegarder les valeurs en cache
        cachedSubSkillData = subSkillData;
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

    /// <summary>
    /// Méthode statique pour nettoyer le cache (à appeler si les variants changent)
    /// </summary>
    public static void ClearVariantIdCache()
    {
        variantIdCache.Clear();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Initialisation optimisée sans coroutine
    /// </summary>
    private void InitializeOptimized()
    {
        // Pas besoin d'attendre XpManager dans une coroutine coûteuse
        // L'initialisation se fera quand Initialize() sera appelé

        // Setup de base qui ne dépend pas de XpManager
        if (!isInitialized)
        {
            SetupProgressRing();
        }
    }

    /// <summary>
    /// Obtenir l'ID du variant depuis le cache ou le calculer
    /// </summary>
    private string GetCachedVariantId(ActivityVariant variant)
    {
        if (variantIdCache.TryGetValue(variant, out string cachedId))
        {
            return cachedId;
        }

        // Calculer et mettre en cache
        string newId = GenerateStandardizedVariantId(variant);
        variantIdCache[variant] = newId;
        return newId;
    }

    /// <summary>
    /// Générer un ID standardisé pour le variant - VERSION OPTIMISÉE
    /// </summary>
    private string GenerateStandardizedVariantId(ActivityVariant variant)
    {
        if (variant == null) return "";

        // Priorité 1 : Utiliser la méthode GetSubSkillId() du variant si elle existe
        string skillId = variant.GetSubSkillId();

        if (!string.IsNullOrEmpty(skillId))
        {
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
    /// Valider et normaliser un ID de compétence - VERSION OPTIMISÉE
    /// </summary>
    private string ValidateAndNormalizeSkillId(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId)) return "";

        // Nettoyer l'ID : supprimer les espaces de début/fin, convertir espaces internes en underscores
        string normalizedId = skillId.Trim().Replace(" ", "_");

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
    /// Configuration des éléments visuels de base - VERSION OPTIMISÉE
    /// </summary>
    private void SetupVisualElements()
    {
        if (activityVariant == null) return;

        // Configurer l'icône du variant (optimisé)
        if (variantIcon != null)
        {
            var iconSprite = GetVariantIconOptimized();
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

        // Mettre à jour la progression
        progressRing.fillAmount = progress;

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
    /// Obtenir l'icône du variant - VERSION OPTIMISÉE
    /// </summary>
    private Sprite GetVariantIconOptimized()
    {
        // Si le variant a sa propre icône, l'utiliser (pas de Resources.Load)
        if (activityVariant != null && activityVariant.VariantIcon != null)
        {
            return activityVariant.VariantIcon;
        }

        // Pas de Resources.Load coûteux - utiliser l'icône par défaut
        return defaultIcon;
    }

    #endregion
}