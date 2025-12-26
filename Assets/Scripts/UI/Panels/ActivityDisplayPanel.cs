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
    [SerializeField] private Button stopActivityButton; // Optionnel pour arreter l'activite

    [Header("Custom Progress Bar")]
    [SerializeField] private Image progressBarBackground; // Image de fond de la barre
    [SerializeField] private Image progressBarFill; // Image qui se remplit
    [SerializeField] private Color progressFillColor = new Color(0.3f, 0.8f, 0.3f, 1f); // Vert sympa
    [SerializeField] private Color progressBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Gris fonce

    [Header("Visual")]
    [SerializeField] private Image activityIcon;
    [SerializeField] private GameObject backgroundActivityImage; // GameObject to flash on tick completion

    [Header("Slide Animation")]
    [SerializeField] private float slideAnimationDuration = 0.3f;
    [SerializeField] private LeanTweenType slideEaseIn = LeanTweenType.easeOutBack;
    [SerializeField] private LeanTweenType slideEaseOut = LeanTweenType.easeInBack;
    [SerializeField] private float slideOffsetY = -500f; // How far below to start (negative = below)

    [Header("Progress Bar Animation")]
    [SerializeField] private float progressAnimationDuration = 0.2f;
    [SerializeField] private LeanTweenType progressAnimationEase = LeanTweenType.easeOutQuad;

    [Header("Tick Completion Animation")]
    [SerializeField] private float tickFlashDuration = 0.3f;
    [SerializeField] private float tickScalePunch = 1.1f;

    // Etat actuel
    private ActivityData currentActivity;
    private ActivityVariant currentVariant;
    private bool isDisplaying = false;
    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private int currentAnimationId = -1;
    private int progressBarAnimationId = -1;
    private float currentProgressBarFill = 0f;

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

        // Cache RectTransform and save original position
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalPosition = rectTransform.localPosition;
        }
    }

    void Start()
    {
        // Setup buttons (will also be called in OnEnable for panel switching)
        SetupButtonListeners();

        // Setup custom progress bar
        SetupCustomProgressBar();

        // Commencer cache
        gameObject.SetActive(false);

        // =====================================
        // EVENTBUS - S'abonner aux evenements
        // =====================================
        SubscribeToActivityEvents();

        CheckAndShowIfActivityActive();
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
    /// Appele quand une activite s'arrete
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
        if (stopActivityButton != null)
            stopActivityButton.interactable = true;
        HidePanel();
    }

    /// <summary>
    /// Appele quand l'activite progresse (pas ajoutes)
    /// </summary>
    private void OnActivityProgress(ActivityProgressEvent eventData)
    {
        currentActivity = eventData.Activity;
        currentVariant = eventData.Variant;

        if (!isDisplaying)        // NEW
            ShowPanel();          // NEW

        UpdateDisplay();
    }

    /// <summary>
    /// Appele quand des ticks sont completes (recompenses donnees)
    /// </summary>
    private void OnActivityTick(ActivityTickEvent eventData)
    {
        currentActivity = eventData.Activity;
        currentVariant = eventData.Variant;

        if (!isDisplaying)        // NEW
            ShowPanel();          // NEW
        UpdateDisplay();

        // Optionnel : effet visuel pour les recompenses
        ShowTickRewardFeedback(eventData.TicksCompleted);

        Logger.LogInfo($"ActivityDisplayPanel: Activity tick - {eventData.TicksCompleted} ticks, {eventData.Rewards.Length} rewards", Logger.LogCategory.General);
    }

    // === GESTION DE L'AFFICHAGE ===

    /// <summary>
    /// Afficher le panel avec animation de slide up
    /// </summary>
    public void ShowPanel()
    {
        if (!isDisplaying)
        {
            isDisplaying = true;

            // Cancel any ongoing animation
            if (currentAnimationId != -1)
            {
                LeanTween.cancel(currentAnimationId);
                currentAnimationId = -1;
            }

            // Set starting position (below screen)
            if (rectTransform != null)
            {
                Vector3 startPosition = originalPosition + new Vector3(0, slideOffsetY, 0);
                rectTransform.localPosition = startPosition;
            }

            // Activate and animate
            gameObject.SetActive(true);

            if (rectTransform != null)
            {
                currentAnimationId = LeanTween.moveLocal(gameObject, originalPosition, slideAnimationDuration)
                    .setEase(slideEaseIn)
                    .setOnComplete(() => currentAnimationId = -1)
                    .id;
            }
        }
    }

    /// <summary>
    /// Cacher le panel avec animation de slide down (n'arrete PAS l'activite)
    /// </summary>
    public void HidePanel()
    {
        if (isDisplaying)
        {
            isDisplaying = false;

            // Cancel any ongoing animation
            if (currentAnimationId != -1)
            {
                LeanTween.cancel(currentAnimationId);
                currentAnimationId = -1;
            }

            if (rectTransform != null)
            {
                Vector3 endPosition = originalPosition + new Vector3(0, slideOffsetY, 0);
                currentAnimationId = LeanTween.moveLocal(gameObject, endPosition, slideAnimationDuration)
                    .setEase(slideEaseOut)
                    .setOnComplete(() =>
                    {
                        currentAnimationId = -1;
                        gameObject.SetActive(false);
                        // Reset position for next show
                        rectTransform.localPosition = originalPosition;
                    })
                    .id;
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Arreter l'activite en cours via l'ActivityManager
    /// </summary>
    private void StopCurrentActivity()
    {
        if (ActivityManager.Instance == null) return;

        bool success = ActivityManager.Instance.StopActivity();

        // Si aucune activite n’etait active, on ferme quand meme l’UI
        if (!success)
            HidePanel();                 // <── AJOUT

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

        // Barre de progression avec animation
        if (progressBarFill != null)
        {
            AnimateProgressBar(progressPercent);

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

        // Barre de progression avec animation
        if (progressBarFill != null)
        {
            AnimateProgressBar(progressPercent);

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
    /// Effet visuel quand des ticks sont completes
    /// </summary>
    private void ShowTickRewardFeedback(int ticksCompleted)
    {
        if (ticksCompleted <= 0) return;

        // Flash/pulse animation sur le BackgroundActivityImage
        if (backgroundActivityImage == null)
        {
            Logger.LogWarning("ActivityDisplayPanel: backgroundActivityImage is null - assign it in inspector!", Logger.LogCategory.General);
            return;
        }

        Logger.LogInfo($"ActivityDisplayPanel: ShowTickRewardFeedback called with {ticksCompleted} ticks", Logger.LogCategory.General);

        // S'assurer que le GameObject est actif
        bool wasActive = backgroundActivityImage.activeSelf;
        if (!wasActive)
        {
            backgroundActivityImage.SetActive(true);
            Logger.LogInfo("ActivityDisplayPanel: Activated backgroundActivityImage", Logger.LogCategory.General);
        }

        // Animation de scale punch (agrandissement puis retour)
        Transform bgTransform = backgroundActivityImage.transform;
        Vector3 originalScale = bgTransform.localScale;

        LeanTween.cancel(backgroundActivityImage);
        LeanTween.scale(backgroundActivityImage, originalScale * tickScalePunch, tickFlashDuration * 0.5f)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnComplete(() =>
            {
                LeanTween.scale(backgroundActivityImage, originalScale, tickFlashDuration * 0.5f)
                    .setEase(LeanTweenType.easeInQuad)
                    .setOnComplete(() =>
                    {
                        // Remettre a l'etat initial si necessaire
                        if (!wasActive)
                        {
                            backgroundActivityImage.SetActive(false);
                        }
                    });
            });

        // Animation de flash sur l'Image component (si present)
        Image bgImage = backgroundActivityImage.GetComponent<Image>();
        if (bgImage != null)
        {
            Color originalColor = bgImage.color;
            Color flashColor = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);

            Logger.LogInfo($"ActivityDisplayPanel: Starting flash animation from color {originalColor}", Logger.LogCategory.General);

            LeanTween.value(backgroundActivityImage, 0f, 1f, tickFlashDuration)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnUpdate((float val) =>
                {
                    if (bgImage != null)
                    {
                        float alpha = Mathf.Lerp(1f, originalColor.a, val);
                        bgImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
                    }
                });
        }
        else
        {
            Logger.LogWarning("ActivityDisplayPanel: backgroundActivityImage has no Image component", Logger.LogCategory.General);
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
        // Cancel any ongoing animations
        if (currentAnimationId != -1)
        {
            LeanTween.cancel(currentAnimationId);
            currentAnimationId = -1;
        }
        if (progressBarAnimationId != -1)
        {
            LeanTween.cancel(progressBarAnimationId);
            progressBarAnimationId = -1;
        }

        UnsubscribeFromActivityEvents();
    }

    void OnEnable()
    {
        // Re-setup button listeners when panel is re-enabled (after panel switching)
        SetupButtonListeners();

        // Restore isDisplaying flag if the panel is visible (e.g., after panel switching)
        if (gameObject.activeInHierarchy)
        {
            isDisplaying = true;
        }
    }

    void OnDisable()
    {
        isDisplaying = false;
        // Clean up button listeners to prevent stale references
        CleanupButtonListeners();
    }

    private void SetupButtonListeners()
    {
        if (closeButton != null)
        {
            // Remove first to prevent duplicate listeners
            closeButton.onClick.RemoveListener(HidePanel);
            closeButton.onClick.AddListener(HidePanel);
        }

        if (stopActivityButton != null)
        {
            stopActivityButton.onClick.RemoveListener(StopCurrentActivity);
            stopActivityButton.onClick.AddListener(StopCurrentActivity);
        }
    }

    private void CleanupButtonListeners()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HidePanel);
        }

        if (stopActivityButton != null)
        {
            stopActivityButton.onClick.RemoveListener(StopCurrentActivity);
        }
    }

    /// <summary>
    /// Anime la barre de progression vers une nouvelle valeur cible
    /// </summary>
    private void AnimateProgressBar(float targetProgress)
    {
        if (progressBarFill == null) return;

        // Si la valeur est deja la meme, pas besoin d'animer
        if (Mathf.Approximately(targetProgress, currentProgressBarFill)) return;

        // Annuler l'animation precedente si elle existe
        if (progressBarAnimationId != -1)
        {
            LeanTween.cancel(progressBarAnimationId);
            progressBarAnimationId = -1;
        }

        // Animer vers la nouvelle valeur
        progressBarAnimationId = LeanTween.value(gameObject, currentProgressBarFill, targetProgress, progressAnimationDuration)
            .setEase(progressAnimationEase)
            .setOnUpdate((float val) =>
            {
                if (progressBarFill != null)
                {
                    progressBarFill.fillAmount = val;
                    currentProgressBarFill = val;
                }
            })
            .setOnComplete(() =>
            {
                progressBarAnimationId = -1;
            }).id;
    }
}