// Purpose: UI Panel to display current activity progress and info
// Filepath: Assets/Scripts/UI/Panels/ActivityDisplayPanel.cs
using ActivityEvents; // NOUVEAU: Import pour EventBus
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
    [SerializeField] private Button stopActivityButton; // Optionnel pour arrêter l'activite

    [Header("Custom Progress Bar")]
    [SerializeField] private Image progressBarBackground; // Image de fond de la barre
    [SerializeField] private Image progressBarFill; // Image qui se remplit
    [SerializeField] private Color progressFillColor = new Color(0.3f, 0.8f, 0.3f, 1f); // Vert sympa
    [SerializeField] private Color progressBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Gris fonce

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

        // Commencer cache
        gameObject.SetActive(false);

        // =====================================
        // EVENTBUS - S'abonner aux evenements
        // =====================================
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
            progressBarBackground.type = Image.Type.Sliced; // Pour des coins arrondis si vous utilisez un sprite approprie
        }

        // Configurer l'image de remplissage
        if (progressBarFill != null)
        {
            progressBarFill.color = progressFillColor;
            progressBarFill.type = Image.Type.Filled;
            progressBarFill.fillMethod = Image.FillMethod.Horizontal;
            progressBarFill.fillOrigin = 0; // Commence a gauche
            progressBarFill.fillAmount = 0f; // Commence vide
        }
    }

    /// <summary>
    /// S'abonner aux evenements via EventBus
    /// </summary>
    private void SubscribeToActivityEvents()
    {
        // =====================================
        // EVENTBUS - Plus besoin de verifier ActivityManager.Instance
        // =====================================
        EventBus.Subscribe<ActivityStartedEvent>(OnActivityStarted);
        EventBus.Subscribe<ActivityStoppedEvent>(OnActivityStopped);
        EventBus.Subscribe<ActivityProgressEvent>(OnActivityProgress);
        EventBus.Subscribe<ActivityTickEvent>(OnActivityTick);

        Logger.LogInfo("ActivityDisplayPanel: Subscribed to EventBus events", Logger.LogCategory.General);
    }

    /// <summary>
    /// Se desabonner des evenements (important pour eviter les erreurs)
    /// </summary>
    private void UnsubscribeFromActivityEvents()
    {
        // =====================================
        // EVENTBUS - Desabonnement simple et fiable
        // =====================================
        EventBus.Unsubscribe<ActivityStartedEvent>(OnActivityStarted);
        EventBus.Unsubscribe<ActivityStoppedEvent>(OnActivityStopped);
        EventBus.Unsubscribe<ActivityProgressEvent>(OnActivityProgress);
        EventBus.Unsubscribe<ActivityTickEvent>(OnActivityTick);

        Logger.LogInfo("ActivityDisplayPanel: Unsubscribed from EventBus events", Logger.LogCategory.General);
    }

    // === GESTIONNAIRES D'eVeNEMENTS - ADAPTeS POUR EVENTBUS ===

    /// <summary>
    /// Appele quand une activite commence
    /// </summary>
    private void OnActivityStarted(ActivityStartedEvent eventData)
    {
        currentActivity = eventData.Activity;
        currentVariant = eventData.Variant;
        ShowPanel();
        UpdateDisplay();

        Logger.LogInfo($"ActivityDisplayPanel: Activity started - {eventData.Activity?.ActivityId}/{eventData.Variant?.VariantName}", Logger.LogCategory.General);
    }

    /// <summary>
    /// Appele quand une activite s'arrête
    /// </summary>
    private void OnActivityStopped(ActivityStoppedEvent eventData)
    {
        Logger.LogInfo($"ActivityDisplayPanel: Activity stopped - {eventData.Activity?.ActivityId}/{eventData.Variant?.VariantName} (Completed: {eventData.WasCompleted})", Logger.LogCategory.General);

        // NOUVEAU: Si c'est une activite de crafting qui n'a PAS ete completee, rendre les materiaux
        if (eventData.Activity != null && eventData.Variant != null &&
            eventData.Variant.IsTimeBased && !eventData.WasCompleted)
        {
            Logger.LogInfo($"ActivityDisplayPanel: Crafting activity cancelled, attempting to refund materials for {eventData.Variant.GetDisplayName()}", Logger.LogCategory.General);

            // Rendre les materiaux consommes
            bool refunded = eventData.Variant.RefundCraftingMaterials(InventoryManager.Instance);

            if (refunded)
            {
                Logger.LogInfo($"ActivityDisplayPanel: Successfully refunded crafting materials for {eventData.Variant.GetDisplayName()}", Logger.LogCategory.General);

                // Optionnel: Afficher un message a l'utilisateur
                // Tu peux ajouter ici une notification ou un feedback visuel
                // Exemple: NotificationManager.ShowMessage("Materiaux rendus !");
            }
            else
            {
                Logger.LogWarning($"ActivityDisplayPanel: Failed to refund some or all materials for {eventData.Variant.GetDisplayName()}", Logger.LogCategory.General);
            }
        }

        HidePanel();
    }

    /// <summary>
    /// Appele quand l'activite progresse (pas ajoutes)
    /// </summary>
    private void OnActivityProgress(ActivityProgressEvent eventData)
    {
        currentActivity = eventData.Activity;
        currentVariant = eventData.Variant;
        UpdateDisplay();
    }

    /// <summary>
    /// Appele quand des ticks sont completes (recompenses donnees)
    /// </summary>
    private void OnActivityTick(ActivityTickEvent eventData)
    {
        currentActivity = eventData.Activity;
        currentVariant = eventData.Variant;
        UpdateDisplay();

        // Optionnel : effet visuel pour les recompenses
        ShowTickRewardFeedback(eventData.TicksCompleted);

        Logger.LogInfo($"ActivityDisplayPanel: Activity tick - {eventData.TicksCompleted} ticks, {eventData.Rewards.Length} rewards", Logger.LogCategory.General);
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
    /// Cacher le panel (n'arrête PAS l'activite)
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
    /// Arrêter l'activite en cours via l'ActivityManager
    /// </summary>
    private void StopCurrentActivity()
    {
        if (ActivityManager.Instance != null)
        {
            bool success = ActivityManager.Instance.StopActivity();
        }
    }

    /// <summary>
    /// Mettre a jour l'affichage avec les donnees actuelles
    /// </summary>
    private void UpdateDisplay()
    {
        if (currentActivity == null || currentVariant == null)
        {
            Logger.LogWarning("ActivityDisplayPanel: Cannot update display - missing activity data", Logger.LogCategory.General);
            return;
        }

        // Nom de l'activite
        if (activityNameText != null)
        {
            activityNameText.text = currentVariant.GetDisplayName();
        }

        // NOUVEAU : Affichage different selon le type d'activite
        if (currentActivity.IsTimeBased)
        {
            UpdateTimedActivityDisplay();
        }
        else
        {
            UpdateStepBasedActivityDisplay();
        }

        // Icône de l'activite (identique pour les deux types)
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
    /// NOUVEAU : Affichage pour les activites temporelles (crafting)
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

            // Couleur speciale pour les activites temporelles
            Color timeColor = Color.Lerp(Color.cyan, Color.yellow, progressPercent);
            progressBarFill.color = timeColor;
        }

        // Recompenses
        if (rewardText != null)
        {
            string rewardInfo = GetRewardInfo();
            rewardText.text = $"Recompense: {rewardInfo}";
        }
    }

    /// <summary>
    /// NOUVEAU : Affichage pour les activites basees sur les pas (existant)
    /// </summary>
    private void UpdateStepBasedActivityDisplay()
    {
        // Code existant pour les activites de pas
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

            // Couleur standard pour les activites de pas
            Color currentColor = Color.Lerp(progressFillColor, Color.yellow, progressPercent * 0.5f);
            progressBarFill.color = currentColor;
        }

        // Recompenses
        if (rewardText != null)
        {
            string rewardInfo = GetRewardInfo();
            rewardText.text = $"Recompense par tick:\n{rewardInfo}";
        }
    }

    /// <summary>
    /// Obtenir les informations sur les recompenses
    /// </summary>
    private string GetRewardInfo()
    {
        if (currentVariant == null) return "Aucune recompense";

        if (currentVariant.PrimaryResource != null)
        {
            string resourceName = currentVariant.PrimaryResource.GetDisplayName();
            return $"1x {resourceName}";
        }

        return "Recompense inconnue";
    }

    /// <summary>
    /// Effet visuel quand des ticks sont completes (optionnel)
    /// </summary>
    private void ShowTickRewardFeedback(int ticksCompleted)
    {
        // Ici vous pourriez ajouter des effets visuels :
        // - Animation de la barre de progression
        // - Texte qui apparaît pour montrer les recompenses
        // - Son ou vibration

        // Exemple simple : effet visuel sur la barre custom
        if (progressBarFill != null)
        {
            // L'UpdateDisplay() va être appele après, donc la barre se remplira automatiquement
            // Ici on pourrait ajouter un effet de "flash" ou d'animation
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

    // === METHODES PUBLIQUES POUR L'INTEGRATION ===

    /// <summary>
    /// Verifier s'il y a une activite en cours et afficher le panel si necessaire
    /// Appele quand on ouvre le panel de localisation
    /// </summary>
    public void CheckAndShowIfActivityActive()
    {
        if (ActivityManager.Instance != null)
        {
            var (activity, variant) = ActivityManager.Instance.GetCurrentActivityInfo();
            if (activity != null && variant != null)
            {
                // Il y a une activite en cours, afficher le panel
                currentActivity = activity;
                currentVariant = variant;
                ShowPanel();
                UpdateDisplay();
            }
        }
    }

    /// <summary>
    /// Verifier si le panel est actuellement affiche
    /// </summary>
    public bool IsDisplaying()
    {
        return isDisplaying;
    }

    /// <summary>
    /// Forcer une mise a jour de l'affichage (si necessaire)
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