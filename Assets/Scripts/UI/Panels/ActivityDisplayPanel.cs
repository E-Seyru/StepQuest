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
    [SerializeField] private Button stopActivityButton; // Optionnel pour arr�ter l'activit�

    [Header("Custom Progress Bar")]
    [SerializeField] private Image progressBarBackground; // Image de fond de la barre
    [SerializeField] private Image progressBarFill; // Image qui se remplit
    [SerializeField] private Color progressFillColor = new Color(0.3f, 0.8f, 0.3f, 1f); // Vert sympa
    [SerializeField] private Color progressBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Gris fonc�

    [Header("Visual")]
    [SerializeField] private Image activityIcon;

    // Etat actuel
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

        // Commencer cach�
        gameObject.SetActive(false);

        // S'abonner aux �v�nements de l'ActivityManager
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
            progressBarBackground.type = Image.Type.Sliced; // Pour des coins arrondis si vous utilisez un sprite appropri�
        }

        // Configurer l'image de remplissage
        if (progressBarFill != null)
        {
            progressBarFill.color = progressFillColor;
            progressBarFill.type = Image.Type.Filled;
            progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            progressBarFill.fillOrigin = 0; // Commence � gauche
            progressBarFill.fillAmount = 0f; // Commence vide
        }


    }

    /// <summary>
    /// S'abonner aux �v�nements de l'ActivityManager
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
    /// Se d�sabonner des �v�nements (important pour �viter les erreurs)
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

    // === EVENEMENTS DE L'ACTIVITYMANAGER ===

    /// <summary>
    /// Appel� quand une activit� commence
    /// </summary>
    private void OnActivityStarted(ActivityData activity, ActivityVariant variant)
    {
        currentActivity = activity;
        currentVariant = variant;
        ShowPanel();
        UpdateDisplay();


    }

    /// <summary>
    /// Appel� quand une activit� s'arr�te
    /// </summary>
    private void OnActivityStopped(ActivityData activity, ActivityVariant variant)
    {
        HidePanel();

    }

    /// <summary>
    /// Appel� quand l'activit� progresse (pas ajout�s)
    /// </summary>
    private void OnActivityProgress(ActivityData activity, ActivityVariant variant)
    {
        currentActivity = activity;
        currentVariant = variant;
        UpdateDisplay();
    }

    /// <summary>
    /// Appel� quand des ticks sont compl�t�s (r�compenses donn�es)
    /// </summary>
    private void OnActivityTick(ActivityData activity, ActivityVariant variant, int ticksCompleted)
    {
        currentActivity = activity;
        currentVariant = variant;
        UpdateDisplay();

        // Optionnel : effet visuel pour les r�compenses
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
    /// Cacher le panel (n'arr�te PAS l'activit�)
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
    /// Arr�ter l'activit� en cours via l'ActivityManager
    /// </summary>
    private void StopCurrentActivity()
    {
        if (ActivityManager.Instance != null)
        {
            bool success = ActivityManager.Instance.StopActivity();

        }
    }

    /// <summary>
    /// Mettre � jour l'affichage avec les donn�es actuelles
    /// </summary>
    private void UpdateDisplay()
    {
        if (currentActivity == null || currentVariant == null)
        {
            Logger.LogWarning("ActivityDisplayPanel: Cannot update display - missing activity data", Logger.LogCategory.General);
            return;
        }

        // Nom de l'activit�
        if (activityNameText != null)
        {
            activityNameText.text = currentVariant.GetDisplayName();
        }

        // NOUVEAU : Affichage diff�rent selon le type d'activit�
        if (currentActivity.IsTimeBased)
        {
            UpdateTimedActivityDisplay();
        }
        else
        {
            UpdateStepBasedActivityDisplay();
        }

        // Ic�ne de l'activit� (identique pour les deux types)
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
    /// Obtenir les informations sur les r�compenses
    /// </summary>
    private string GetRewardInfo()
    {
        if (currentVariant == null) return "Aucune r�compense";

        if (currentVariant.PrimaryResource != null)
        {
            string resourceName = currentVariant.PrimaryResource.GetDisplayName();
            return $"1x {resourceName}";
        }

        return "R�compense inconnue";
    }

    /// <summary>
    /// Effet visuel quand des ticks sont compl�t�s (optionnel)
    /// </summary>
    private void ShowTickRewardFeedback(int ticksCompleted)
    {
        // Ici vous pourriez ajouter des effets visuels :
        // - Animation de la barre de progression
        // - Texte qui appara�t pour montrer les r�compenses
        // - Son ou vibration



        // Exemple simple : effet visuel sur la barre custom
        if (progressBarFill != null)
        {
            // L'UpdateDisplay() va �tre appel� apr�s, donc la barre se remplira automatiquement
            // Ici on pourrait ajouter un effet de "flash" ou d'animation
        }
    }

    // === METHODES PUBLIQUES POUR L'INTEGRATION ===

    /// <summary>
    /// V�rifier s'il y a une activit� en cours et afficher le panel si n�cessaire
    /// Appel� quand on ouvre le panel de localisation
    /// </summary>
    public void CheckAndShowIfActivityActive()
    {
        if (ActivityManager.Instance != null)
        {
            var (activity, variant) = ActivityManager.Instance.GetCurrentActivityInfo();
            if (activity != null && variant != null)
            {
                // Il y a une activit� en cours, afficher le panel
                currentActivity = activity;
                currentVariant = variant;
                ShowPanel();
                UpdateDisplay();


            }
        }
    }

    /// <summary>
    /// V�rifier si le panel est actuellement affich�
    /// </summary>
    public bool IsDisplaying()
    {
        return isDisplaying;
    }

    /// <summary>
    /// Forcer une mise � jour de l'affichage (si n�cessaire)
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
    /// <summary>
    /// NOUVEAU : Affichage pour les activit�s temporelles (crafting)
    /// </summary>
    private void UpdateTimedActivityDisplay()
    {
        float progressPercent = currentActivity.GetProgressToNextTick(currentVariant);

        // Temps restant
        long remainingTimeMs = currentActivity.RequiredTimeMs - currentActivity.AccumulatedTimeMs;
        string timeRemaining = FormatTime(remainingTimeMs);
        string totalTime = FormatTime(currentActivity.RequiredTimeMs);

        if (progressText != null)
        {
            progressText.text = $"Temps restant: {timeRemaining}\n" +
                               $"Progression: {currentActivity.AccumulatedTimeMs}ms / {totalTime}";
        }

        // Barre de progression
        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = progressPercent;

            // Couleur sp�ciale pour les activit�s temporelles
            Color timeColor = Color.Lerp(Color.cyan, Color.yellow, progressPercent);
            progressBarFill.color = timeColor;
        }

        // R�compenses
        if (rewardText != null)
        {
            string rewardInfo = GetRewardInfo();
            progressText.text += $"\nR�compense: {rewardInfo}";
        }
    }

    /// <summary>
    /// NOUVEAU : Affichage pour les activit�s bas�es sur les pas (existant)
    /// </summary>
    private void UpdateStepBasedActivityDisplay()
    {
        // Code existant pour les activit�s de pas
        int stepsToNext = currentVariant.ActionCost - currentActivity.AccumulatedSteps;
        float progressPercent = currentActivity.GetProgressToNextTick(currentVariant);

        if (progressText != null)
        {
            progressText.text = $"Prochain tick dans {stepsToNext} pas\n" +
                               $"Progression: {currentActivity.AccumulatedSteps}/{currentVariant.ActionCost}";
        }

        // Barre de progression
        if (progressBarFill != null)
        {
            progressBarFill.fillAmount = progressPercent;

            // Couleur standard pour les activit�s de pas
            Color currentColor = Color.Lerp(progressFillColor, Color.yellow, progressPercent * 0.5f);
            progressBarFill.color = currentColor;
        }

        // R�compenses
        if (rewardText != null)
        {
            string rewardInfo = GetRewardInfo();
            rewardText.text = $"R�compense par tick:\n{rewardInfo}";
        }
    }

    /// <summary>
    /// NOUVEAU : Formater le temps en millisecondes en format lisible
    /// </summary>
    private string FormatTime(long timeMs)
    {
        if (timeMs <= 0) return "0s";

        if (timeMs < 1000)
            return $"{timeMs}ms";
        else if (timeMs < 60000)
            return $"{timeMs / 1000f:F1}s";
        else
            return $"{timeMs / 60000f:F1}min";
    }
    void OnDestroy()
    {
        UnsubscribeFromActivityEvents();
    }

    void OnDisable()
    {
        isDisplaying = false;
    }
}