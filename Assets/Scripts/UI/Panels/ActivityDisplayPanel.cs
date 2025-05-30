// Purpose: UI Panel to display current activity progress and info
// Filepath: Assets/Scripts/UI/Panels/ActivityDisplayPanel.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActivityDisplayPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI activityNameText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button stopActivityButton; // Optionnel pour arrêter l'activité

    [Header("Custom Progress Bar")]
    [SerializeField] private Image progressBarBackground; // Image de fond de la barre
    [SerializeField] private Image progressBarFill; // Image qui se remplit
    [SerializeField] private Color progressFillColor = new Color(0.3f, 0.8f, 0.3f, 1f); // Vert sympa
    [SerializeField] private Color progressBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Gris foncé

    [Header("Visual")]
    [SerializeField] private Image activityIcon;

    // État actuel
    private ActivityData currentActivity;
    private ActivityVariant currentVariant;
    private bool isDisplaying = false;

    public static ActivityDisplayPanel Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Setup buttons
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HidePanel);
        }

        if (stopActivityButton != null)
        {
            stopActivityButton.onClick.AddListener(StopCurrentActivity);
        }

        // Setup custom progress bar
        SetupCustomProgressBar();

        // Commencer caché
        gameObject.SetActive(false);

        // S'abonner aux événements de l'ActivityManager
        SubscribeToActivityEvents();

    }

    /// <summary>
    /// Setup de la barre de progression custom
    /// </summary>
    private void SetupCustomProgressBar()
    {
        // Configurer l'image de fond
        if (progressBarBackground != null)
        {
            progressBarBackground.color = progressBackgroundColor;
            progressBarBackground.type = Image.Type.Sliced; // Pour des coins arrondis si vous utilisez un sprite approprié
        }

        // Configurer l'image de remplissage
        if (progressBarFill != null)
        {
            progressBarFill.color = progressFillColor;
            progressBarFill.type = Image.Type.Filled;
            progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            progressBarFill.fillOrigin = 0; // Commence à gauche
            progressBarFill.fillAmount = 0f; // Commence vide
        }


    }

    /// <summary>
    /// S'abonner aux événements de l'ActivityManager
    /// </summary>
    private void SubscribeToActivityEvents()
    {
        if (ActivityManager.Instance != null)
        {
            ActivityManager.Instance.OnActivityStarted += OnActivityStarted;
            ActivityManager.Instance.OnActivityStopped += OnActivityStopped;
            ActivityManager.Instance.OnActivityProgress += OnActivityProgress;
            ActivityManager.Instance.OnActivityTick += OnActivityTick;
        }
    }

    /// <summary>
    /// Se désabonner des événements (important pour éviter les erreurs)
    /// </summary>
    private void UnsubscribeFromActivityEvents()
    {
        if (ActivityManager.Instance != null)
        {
            ActivityManager.Instance.OnActivityStarted -= OnActivityStarted;
            ActivityManager.Instance.OnActivityStopped -= OnActivityStopped;
            ActivityManager.Instance.OnActivityProgress -= OnActivityProgress;
            ActivityManager.Instance.OnActivityTick -= OnActivityTick;
        }
    }

    // === ÉVÉNEMENTS DE L'ACTIVITYMANAGER ===

    /// <summary>
    /// Appelé quand une activité commence
    /// </summary>
    private void OnActivityStarted(ActivityData activity, ActivityVariant variant)
    {
        currentActivity = activity;
        currentVariant = variant;
        ShowPanel();
        UpdateDisplay();


    }

    /// <summary>
    /// Appelé quand une activité s'arrête
    /// </summary>
    private void OnActivityStopped(ActivityData activity, ActivityVariant variant)
    {
        HidePanel();

    }

    /// <summary>
    /// Appelé quand l'activité progresse (pas ajoutés)
    /// </summary>
    private void OnActivityProgress(ActivityData activity, ActivityVariant variant)
    {
        currentActivity = activity;
        currentVariant = variant;
        UpdateDisplay();
    }

    /// <summary>
    /// Appelé quand des ticks sont complétés (récompenses données)
    /// </summary>
    private void OnActivityTick(ActivityData activity, ActivityVariant variant, int ticksCompleted)
    {
        currentActivity = activity;
        currentVariant = variant;
        UpdateDisplay();

        // Optionnel : effet visuel pour les récompenses
        ShowTickRewardFeedback(ticksCompleted);
    }

    // === GESTION DE L'AFFICHAGE ===

    /// <summary>
    /// Afficher le panel
    /// </summary>
    public void ShowPanel()
    {
        if (!isDisplaying)
        {
            gameObject.SetActive(true);
            isDisplaying = true;
        }
    }

    /// <summary>
    /// Cacher le panel (n'arrête PAS l'activité)
    /// </summary>
    public void HidePanel()
    {
        if (isDisplaying)
        {
            gameObject.SetActive(false);
            isDisplaying = false;
        }
    }

    /// <summary>
    /// Arrêter l'activité en cours via l'ActivityManager
    /// </summary>
    private void StopCurrentActivity()
    {
        if (ActivityManager.Instance != null)
        {
            bool success = ActivityManager.Instance.StopActivity();

        }
    }

    /// <summary>
    /// Mettre à jour l'affichage avec les données actuelles
    /// </summary>
    private void UpdateDisplay()
    {
        if (currentActivity == null || currentVariant == null)
        {
            Logger.LogWarning("ActivityDisplayPanel: Cannot update display - missing activity data", Logger.LogCategory.General);
            return;
        }

        // Nom de l'activité
        if (activityNameText != null)
        {
            activityNameText.text = currentVariant.GetDisplayName();
        }

        // Progression (pas jusqu'au prochain tick)
        int stepsToNext = currentVariant.ActionCost - currentActivity.AccumulatedSteps;
        float progressPercent = currentActivity.GetProgressToNextTick(currentVariant);

        if (progressText != null)
        {
            progressText.text = $"Prochain tick dans {stepsToNext} pas\n" +
                               $"Progression: {currentActivity.AccumulatedSteps}/{currentVariant.ActionCost}";
        }

        // Barre de progression custom
        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = progressPercent;

            // Optionnel : changer la couleur selon la progression
            Color currentColor = Color.Lerp(progressFillColor, Color.yellow, progressPercent * 0.5f);
            progressBarFill.color = currentColor;
        }

        // Récompenses
        if (rewardText != null)
        {
            string rewardInfo = GetRewardInfo();
            rewardText.text = $"Récompense par tick:\n{rewardInfo}";
        }

        // Icône de l'activité
        if (activityIcon != null)
        {
            Sprite icon = currentVariant.GetIcon();
            if (icon != null)
            {
                activityIcon.sprite = icon;
                activityIcon.gameObject.SetActive(true);
            }
            else
            {
                activityIcon.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Obtenir les informations sur les récompenses
    /// </summary>
    private string GetRewardInfo()
    {
        if (currentVariant == null) return "Aucune récompense";

        if (currentVariant.PrimaryResource != null)
        {
            string resourceName = currentVariant.PrimaryResource.GetDisplayName();
            return $"1x {resourceName}";
        }

        return "Récompense inconnue";
    }

    /// <summary>
    /// Effet visuel quand des ticks sont complétés (optionnel)
    /// </summary>
    private void ShowTickRewardFeedback(int ticksCompleted)
    {
        // Ici vous pourriez ajouter des effets visuels :
        // - Animation de la barre de progression
        // - Texte qui apparaît pour montrer les récompenses
        // - Son ou vibration



        // Exemple simple : effet visuel sur la barre custom
        if (progressBarFill != null)
        {
            // L'UpdateDisplay() va être appelé après, donc la barre se remplira automatiquement
            // Ici on pourrait ajouter un effet de "flash" ou d'animation
        }
    }

    // === MÉTHODES PUBLIQUES POUR L'INTÉGRATION ===

    /// <summary>
    /// Vérifier s'il y a une activité en cours et afficher le panel si nécessaire
    /// Appelé quand on ouvre le panel de localisation
    /// </summary>
    public void CheckAndShowIfActivityActive()
    {
        if (ActivityManager.Instance != null)
        {
            var (activity, variant) = ActivityManager.Instance.GetCurrentActivityInfo();
            if (activity != null && variant != null)
            {
                // Il y a une activité en cours, afficher le panel
                currentActivity = activity;
                currentVariant = variant;
                ShowPanel();
                UpdateDisplay();


            }
        }
    }

    /// <summary>
    /// Vérifier si le panel est actuellement affiché
    /// </summary>
    public bool IsDisplaying()
    {
        return isDisplaying;
    }

    /// <summary>
    /// Forcer une mise à jour de l'affichage (si nécessaire)
    /// </summary>
    public void RefreshDisplay()
    {
        if (isDisplaying && ActivityManager.Instance != null)
        {
            var (activity, variant) = ActivityManager.Instance.GetCurrentActivityInfo();
            if (activity != null && variant != null)
            {
                currentActivity = activity;
                currentVariant = variant;
                UpdateDisplay();
            }
        }
    }

    /// <summary>
    /// Obtenir des informations de debug
    /// </summary>
    public string GetDebugInfo()
    {
        if (!isDisplaying) return "Panel not displayed";
        if (currentActivity == null || currentVariant == null) return "No activity data";

        return $"Displaying: {currentVariant.GetDisplayName()}\n" +
               $"Progress: {currentActivity.AccumulatedSteps}/{currentVariant.ActionCost}\n" +
               $"Panel Active: {gameObject.activeInHierarchy}";
    }

    // === CLEANUP ===

    void OnDestroy()
    {
        UnsubscribeFromActivityEvents();
    }

    void OnDisable()
    {
        isDisplaying = false;
    }
}