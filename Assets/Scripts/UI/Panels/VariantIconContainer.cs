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
    [SerializeField] private Image lockIcon; // NOUVEAU : Ic�ne cadenas
    [SerializeField] private TextMeshProUGUI requiredLevelText; // NOUVEAU : "Req. Lvl X" - toujours affiche

    [Header("Settings")]
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private float refreshInterval = 1f;
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Visual Settings")]
    [SerializeField] private Color maxLevelColor = Color.yellow;
    [SerializeField] private Sprite defaultIcon;
    [SerializeField] private Sprite unknownVariantIcon; // Ic�ne "?" pour variants non decouverts
    [SerializeField] private Sprite lockIconSprite; // NOUVEAU : Sprite du cadenas

    // Data
    private ActivityVariant activityVariant;
    private string variantId;

    // Cache pour optimisation
    private SkillData cachedSubSkillData;
    private float lastProgressValue = -1f;
    private int lastLevel = -1;
    private bool isInitialized = false;

    // Cache statique pour eviter les recalculs d'ID - avec methode de nettoyage
    private static readonly System.Collections.Generic.Dictionary<ActivityVariant, string> variantIdCache =
        new System.Collections.Generic.Dictionary<ActivityVariant, string>();

    #region Unity Lifecycle

    void Start()
    {
        // Initialisation simplifiee - pas de coroutine co�teuse
        InitializeOptimized();
    }

    void OnDestroy()
    {
        // Nettoyer les InvokeRepeating
        CancelInvoke();
    }

    void OnDisable()
    {
        // Arreter le timer des que l'ic�ne est masquee
        CancelInvoke();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Initialiser l'ic�ne avec un ActivityVariant - VERSION OPTIMISeE
    /// </summary>
    public void Initialize(ActivityVariant variant)
    {
        activityVariant = variant;

        if (variant != null)
        {
            // Utiliser le cache pour eviter de recalculer l'ID
            variantId = GetCachedVariantId(variant);

            // Configuration visuelle initiale (seulement si pas encore fait)
            if (!isInitialized)
            {
                SetupVisualElements();
                SetupProgressRing();
                isInitialized = true;
            }

            // Premiere mise a jour seulement si XpManager est disponible
            if (XpManager.Instance != null)
            {
                RefreshDisplay();

                // Demarrer l'auto-refresh seulement maintenant ET verifier si pas deja en cours
                if (autoRefresh && !IsInvoking(nameof(RefreshDisplay)))
                {
                    InvokeRepeating(nameof(RefreshDisplay), refreshInterval, refreshInterval);
                }
            }
        }
    }

    /// <summary>
    /// Actualiser l'affichage (niveau, progression, ic�ne) - VERSION OPTIMISeE
    /// </summary>
    public void RefreshDisplay()
    {
        if (string.IsNullOrEmpty(variantId) || XpManager.Instance == null)
            return;

        // Obtenir les donnees de sous-competence actuelles (variants utilisent SubSkills)
        var subSkillData = XpManager.Instance.GetPlayerSubSkill(variantId);

        // Mettre a jour l'ic�ne et le cadenas
        UpdateIconDisplay();
        UpdateLockDisplay(); // NOUVEAU

        // Optimisation : seulement mettre a jour si les donnees ont change
        bool hasLevelChanged = subSkillData.Level != lastLevel;
        float currentProgress = GetProgressToNextLevel(subSkillData);
        bool hasProgressChanged = Mathf.Abs(currentProgress - lastProgressValue) > 0.01f;

        if (!hasLevelChanged && !hasProgressChanged)
        {
            return; // Rien a mettre a jour
        }

        // Mettre a jour seulement ce qui a change
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
    /// Obtenir les donnees de sous-competence actuelles
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
    /// Methode statique pour nettoyer le cache (a appeler si les variants changent)
    /// </summary>
    public static void ClearVariantIdCache()
    {
        variantIdCache.Clear();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Initialisation optimisee sans coroutine
    /// </summary>
    private void InitializeOptimized()
    {
        // Pas besoin d'attendre XpManager dans une coroutine co�teuse
        // L'initialisation se fera quand Initialize() sera appele

        // Setup de base qui ne depend pas de XpManager
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
    /// Generer un ID standardise pour le variant - VERSION OPTIMISeE
    /// </summary>
    private string GenerateStandardizedVariantId(ActivityVariant variant)
    {
        if (variant == null) return "";

        // Priorite 1 : Utiliser la methode GetSubSkillId() du variant si elle existe
        string skillId = variant.GetSubSkillId();

        if (!string.IsNullOrEmpty(skillId))
        {
            return ValidateAndNormalizeSkillId(skillId);
        }

        // Priorite 2 : Generer a partir du nom du variant
        if (!string.IsNullOrEmpty(variant.VariantName))
        {
            return ValidateAndNormalizeSkillId(variant.VariantName);
        }

        // Priorite 3 : Fallback avec ParentActivityID + nom
        return ValidateAndNormalizeSkillId($"{variant.ParentActivityID}_{variant.name}");
    }

    /// <summary>
    /// Valider et normaliser un ID de competence - VERSION OPTIMISeE
    /// </summary>
    private string ValidateAndNormalizeSkillId(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId)) return "";

        // Nettoyer l'ID : supprimer les espaces de debut/fin, convertir espaces internes en underscores
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
    /// Configuration des elements visuels de base - VERSION OPTIMISeE
    /// </summary>
    private void SetupVisualElements()
    {
        if (activityVariant == null) return;

        // Configurer l'ic�ne du variant
        if (variantIcon != null)
        {
            var iconSprite = GetDisplayIcon();
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

        // Configurer le cadenas et le texte de requirement
        UpdateLockDisplay();
    }

    /// <summary>
    /// Mettre a jour l'affichage du niveau
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
    /// Mettre a jour l'anneau de progression
    /// </summary>
    private void UpdateProgressRing(SkillData subSkillData)
    {
        if (progressRing == null) return;

        // Calculer la progression vers le niveau suivant
        float progress = GetProgressToNextLevel(subSkillData);

        // Mettre a jour la progression
        progressRing.fillAmount = progress;

        // Changer la couleur selon le niveau
        if (subSkillData.Level >= XpManager.Instance.MaxLevel)
        {
            progressRing.color = maxLevelColor;
            progressRing.fillAmount = 1f; // Complet au niveau max
        }
    }

    /// <summary>
    /// NOUVELLE MeTHODE : Obtenir l'ic�ne a afficher
    /// </summary>
    private Sprite GetDisplayIcon()
    {
        // Verifier si le variant a ete decouvert
        if (!IsVariantDiscovered())
        {
            return unknownVariantIcon; // Afficher le "?"
        }

        // Variant decouvert : afficher l'ic�ne normale
        return GetVariantIconOptimized();
    }

    /// <summary>
    /// Verifier si un variant a ete decouvert (pratique au moins une fois)
    /// </summary>
    private bool IsVariantDiscovered()
    {
        // Si le variant est verrouille par niveau, il ne peut pas �tre decouvert
        if (IsVariantLockedByLevel())
        {
            return false;
        }

        if (string.IsNullOrEmpty(variantId) || DataManager.Instance?.PlayerData == null)
        {
            return false; // Pas decouvert si pas d'ID ou pas de donnees
        }

        // Verifier si le variant existe dans les SubSkills ET a de l'XP > 0
        var playerData = DataManager.Instance.PlayerData;
        return playerData.SubSkills.ContainsKey(variantId) &&
               playerData.GetSubSkillXP(variantId) > 0;
    }

    /// <summary>
    /// NOUVELLE MeTHODE : Verifier si le variant est verrouille par niveau
    /// </summary>
    private bool IsVariantLockedByLevel()
    {
        if (activityVariant == null || string.IsNullOrEmpty(activityVariant.ParentActivityID))
            return false;

        // Obtenir le niveau requis pour ce variant
        int requiredLevel = GetRequiredLevelForVariant();
        if (requiredLevel <= 1) // Pas de niveau requis
            return false;

        // Obtenir le niveau actuel de l'activite principale
        int mainActivityLevel = GetMainActivityLevel();

        bool isLocked = mainActivityLevel < requiredLevel;

        if (enableDebugLogs && isLocked)
        {
            Debug.Log($"Variant '{activityVariant.VariantName}' locked - Required: {requiredLevel}, Current: {mainActivityLevel}");
        }

        return isLocked;
    }

    /// <summary>
    /// NOUVELLE MeTHODE : Obtenir le niveau requis pour ce variant
    /// </summary>
    private int GetRequiredLevelForVariant()
    {
        // Utiliser la propriete UnlockRequirement
        if (activityVariant.UnlockRequirement > 0)
        {
            return activityVariant.UnlockRequirement;
        }

        // Fallback : Systeme base sur le nom (exemple)
        return GetRequiredLevelByName(activityVariant.VariantName);
    }

    /// <summary>
    /// NOUVELLE MeTHODE : Obtenir le niveau requis base sur le nom (exemple)
    /// </summary>
    private int GetRequiredLevelByName(string variantName)
    {
        // Exemple de logique - adaptez selon vos besoins
        if (variantName.Contains("Advanced") || variantName.Contains("Avance"))
            return 10;
        if (variantName.Contains("Expert") || variantName.Contains("Ma�tre"))
            return 25;
        if (variantName.Contains("Master") || variantName.Contains("Legendaire"))
            return 50;

        // Variants de base : pas de niveau requis
        return 1;
    }

    /// <summary>
    /// NOUVELLE MeTHODE : Obtenir le niveau actuel de l'activite principale
    /// </summary>
    private int GetMainActivityLevel()
    {
        if (string.IsNullOrEmpty(activityVariant.ParentActivityID) || XpManager.Instance == null)
            return 1;

        // Normaliser l'ID de l'activite principale
        string normalizedMainActivityId = activityVariant.ParentActivityID.Trim().Replace(" ", "_");

        // Obtenir les donnees de l'activite principale
        var mainSkillData = XpManager.Instance.GetPlayerSkill(normalizedMainActivityId);
        return mainSkillData.Level;
    }

    /// <summary>
    /// NOUVELLE MeTHODE : Mettre a jour l'ic�ne de cadenas et le texte de requirement
    /// </summary>
    private void UpdateLockDisplay()
    {
        bool isLocked = IsVariantLockedByLevel();
        int requiredLevel = GetRequiredLevelForVariant();

        // Activer/desactiver l'ic�ne de cadenas
        if (lockIcon != null)
        {
            lockIcon.gameObject.SetActive(isLocked);

            if (isLocked && lockIconSprite != null)
            {
                lockIcon.sprite = lockIconSprite;
            }
        }

        // Afficher le niveau requis (toujours affiche)
        if (requiredLevelText != null)
        {
            requiredLevelText.text = $"Lvl {requiredLevel}";
        }
    }

    /// <summary>
    /// Mettre a jour l'ic�ne affichee selon l'etat de decouverte
    /// </summary>
    private void UpdateIconDisplay()
    {
        if (variantIcon != null)
        {
            var iconSprite = GetDisplayIcon();
            variantIcon.sprite = iconSprite ?? defaultIcon;
        }
    }

    /// <summary>
    /// Calculer la progression vers le niveau suivant (0.0 a 1.0)
    /// </summary>
    private float GetProgressToNextLevel(SkillData subSkillData)
    {
        if (XpManager.Instance == null) return 0f;

        return XpManager.Instance.GetProgressToNextLevel(subSkillData);
    }

    /// <summary>
    /// Obtenir l'ic�ne du variant - VERSION OPTIMISeE
    /// </summary>
    private Sprite GetVariantIconOptimized()
    {
        // Si le variant a sa propre ic�ne, l'utiliser (pas de Resources.Load)
        if (activityVariant != null && activityVariant.VariantIcon != null)
        {
            return activityVariant.VariantIcon;
        }

        // Pas de Resources.Load co�teux - utiliser l'ic�ne par defaut
        return defaultIcon;
    }

    #endregion
}