// Purpose: Manages the always-visible UI elements above the main canvas
// Filepath: Assets/Scripts/UI/AboveCanvasManager.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AboveCanvasManager : MonoBehaviour
{
    public static AboveCanvasManager Instance { get; private set; }

    [Header("UI References - Header")]
    [SerializeField] private GameObject headerContainer;
    [SerializeField] private TextMeshProUGUI currentLocationText;
    [SerializeField] private Button mapButton;

    [Header("UI References - Activity/Travel Bar")]
    [SerializeField] private GameObject activityBar;
    [SerializeField] private Image leftIcon;          // POI départ ou icône activité
    [SerializeField] private Image rightIcon;         // POI arrivée (uniquement pour voyage)
    [SerializeField] private TextMeshProUGUI activityText;
    [SerializeField] private Image backgroundBar;     // Image de fond de la barre
    [SerializeField] private Image fillBar;           // Image de remplissage (fillAmount)
    [SerializeField] private GameObject arrowIcon;    // Flèche entre les POIs

    [Header("UI References - Navigation Bar")]
    [SerializeField] private GameObject navigationBar;

    [Header("Settings")]
    [SerializeField] private bool hideNavigationOnMap = true;

    [Header("Animation Settings")]
    [SerializeField] private float progressAnimationDuration = 0.3f;
    [SerializeField] private LeanTweenType progressAnimationEase = LeanTweenType.easeOutQuart;
    [SerializeField] private float pulseScaleAmount = 1.08f;
    [SerializeField] private float pulseDuration = 0.2f;
    [SerializeField] private Color pulseColor = new Color(0.8f, 0.8f, 0.8f, 1f); // Plus sombre

    // Références aux managers
    private GameManager gameManager;
    private DataManager dataManager;
    private MapManager mapManager;
    private ActivityManager activityManager;
    private LocationRegistry locationRegistry;

    // Variables privées pour l'animation
    private float lastProgressValue = -1f; // Cache de la dernière valeur
    private int currentAnimationId = -1;    // ID de l'animation LeanTween en cours
    private int currentPulseId = -1;        // ID de l'animation de pulse en cours
    private Color originalFillColor;        // Couleur originale de la barre
    private Vector3 originalFillScale;      // Échelle originale de la barre

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Logger.LogWarning("AboveCanvasManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.General);
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        StartCoroutine(InitializeAboveCanvas());
    }

    private System.Collections.IEnumerator InitializeAboveCanvas()
    {
        // Attendre que les managers soient disponibles
        while (GameManager.Instance == null ||
               DataManager.Instance == null ||
               MapManager.Instance == null ||
               ActivityManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // Récupérer les références
        gameManager = GameManager.Instance;
        dataManager = DataManager.Instance;
        mapManager = MapManager.Instance;
        activityManager = ActivityManager.Instance;
        locationRegistry = mapManager.LocationRegistry;

        // S'abonner aux événements
        SubscribeToEvents();

        // Configuration du bouton carte
        if (mapButton != null)
        {
            mapButton.onClick.AddListener(OnMapButtonClicked);
        }

        // Configuration de la barre de progression
        SetupProgressBar();

        // Initialiser l'affichage
        RefreshDisplay();

        // NOUVEAU: Vérification retardée pour s'assurer que l'affichage est correct
        StartCoroutine(DelayedDisplayRefresh());

        Logger.LogInfo("AboveCanvasManager: Initialized successfully", Logger.LogCategory.General);
    }

    /// <summary>
    /// Vérification retardée pour corriger les problèmes d'ordre d'initialisation
    /// </summary>
    private System.Collections.IEnumerator DelayedDisplayRefresh()
    {
        // Attendre quelques frames pour que tous les managers soient complètement initialisés
        yield return new WaitForSeconds(1f);

        // Forcer une mise à jour de l'affichage
        RefreshDisplay();

        // Si une activité est en cours mais que la barre n'est pas affichée, la corriger
        if (activityManager != null && activityManager.HasActiveActivity())
        {
            if (activityBar != null && !activityBar.activeSelf)
            {
                Logger.LogInfo("AboveCanvasManager: Correcting activity bar display after startup", Logger.LogCategory.General);
                RefreshDisplay();
            }
        }
    }

    private void SetupProgressBar()
    {
        // S'assurer que la fillBar est configurée correctement
        if (fillBar != null)
        {
            // Configurer en mode Filled si ce n'est pas déjà fait
            if (fillBar.type != Image.Type.Filled)
            {
                fillBar.type = Image.Type.Filled;
                fillBar.fillMethod = Image.FillMethod.Horizontal;
                Logger.LogInfo("AboveCanvasManager: Configured fillBar as Filled Horizontal", Logger.LogCategory.General);
            }

            // Sauvegarder les valeurs originales pour l'animation
            originalFillColor = fillBar.color;
            originalFillScale = fillBar.transform.localScale;

            // Commencer avec 0 progression
            fillBar.fillAmount = 0f;
            lastProgressValue = 0f;
        }

        // S'assurer que backgroundBar est en mode Simple ou Sliced
        if (backgroundBar != null && backgroundBar.type == Image.Type.Filled)
        {
            backgroundBar.type = Image.Type.Simple;
            Logger.LogInfo("AboveCanvasManager: Configured backgroundBar as Simple", Logger.LogCategory.General);
        }
    }

    private void SubscribeToEvents()
    {
        // Écouter les changements d'état du jeu
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += OnGameStateChanged;
        }

        // Écouter les événements spécifiques pour les mises à jour
        if (mapManager != null)
        {
            mapManager.OnLocationChanged += OnLocationChanged;
            mapManager.OnTravelProgress += OnTravelProgress;
        }

        if (activityManager != null)
        {
            activityManager.OnActivityProgress += OnActivityProgress;
            activityManager.OnActivityStopped += OnActivityStopped;
        }
    }

    // === GESTIONNAIRES D'ÉVÉNEMENTS ===

    private void OnGameStateChanged(GameState oldState, GameState newState)
    {
        Logger.LogInfo($"AboveCanvasManager: Game state changed from {oldState} to {newState}", Logger.LogCategory.General);
        RefreshDisplay();
    }

    private void OnLocationChanged(MapLocationDefinition newLocation)
    {
        UpdateLocationDisplay();
    }

    private void OnTravelProgress(string destinationId, int currentSteps, int requiredSteps)
    {
        UpdateTravelProgress(currentSteps, requiredSteps);
    }

    private void OnActivityProgress(ActivityData activity, ActivityVariant variant)
    {
        UpdateActivityProgress(activity, variant);
    }

    private void OnActivityStopped(ActivityData activity, ActivityVariant variant)
    {
        Logger.LogInfo("AboveCanvasManager: Activity stopped, refreshing display", Logger.LogCategory.General);
        RefreshDisplay();
    }

    private void OnMapButtonClicked()
    {
        // Gérer l'affichage de la barre de navigation si besoin
        if (hideNavigationOnMap && navigationBar != null)
        {
            navigationBar.SetActive(false);
        }

        // Le reste sera géré par le PanelManager ou MapManager
        Logger.LogInfo("AboveCanvasManager: Map button clicked", Logger.LogCategory.General);
    }

    // === MÉTHODES DE MISE À JOUR ===

    private void RefreshDisplay()
    {
        UpdateLocationDisplay();
        UpdateActivityBarDisplay();
    }

    private void UpdateLocationDisplay()
    {
        if (currentLocationText == null || mapManager?.CurrentLocation == null) return;

        currentLocationText.text = mapManager.CurrentLocation.DisplayName;
    }

    private void UpdateActivityBarDisplay()
    {
        if (activityBar == null) return;

        GameState currentState = gameManager.CurrentState;

        // NOUVEAU: Vérification directe de l'activité en cours pour pallier aux problèmes d'état
        bool hasActiveActivity = activityManager?.HasActiveActivity() == true;
        bool isCurrentlyTraveling = dataManager?.PlayerData?.IsCurrentlyTraveling() == true;

        if (isCurrentlyTraveling)
        {
            SetupTravelDisplay();
        }
        else if (hasActiveActivity)
        {
            SetupActivityDisplay();
        }
        else
        {
            HideActivityBar();
        }
    }

    private void SetupTravelDisplay()
    {
        if (dataManager?.PlayerData == null) return;

        activityBar.SetActive(true);

        // Configurer les icônes
        string currentLocationId = dataManager.PlayerData.CurrentLocationId;
        string destinationId = dataManager.PlayerData.TravelDestinationId;

        if (leftIcon != null && mapManager.LocationRegistry != null)
        {
            var currentLocation = mapManager.LocationRegistry.GetLocationById(currentLocationId);
            if (currentLocation?.LocationIcon != null)
            {
                leftIcon.sprite = currentLocation.LocationIcon;
            }
            leftIcon.gameObject.SetActive(true);
        }

        if (rightIcon != null && mapManager.LocationRegistry != null)
        {
            var destinationLocation = mapManager.LocationRegistry.GetLocationById(destinationId);
            if (destinationLocation?.LocationIcon != null)
            {
                rightIcon.sprite = destinationLocation.LocationIcon;
            }
            rightIcon.gameObject.SetActive(true);
        }

        // Afficher la flèche pour le voyage
        if (arrowIcon != null)
        {
            arrowIcon.SetActive(true);
        }

        // Texte
        if (activityText != null)
        {
            activityText.text = $"Voyage vers {destinationId}";
        }

        // Progression
        long currentTotalSteps = dataManager.PlayerData.TotalSteps;
        long progressSteps = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);
        int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;

        UpdateProgressBar(progressSteps, requiredSteps);
    }

    private void SetupActivityDisplay()
    {
        var activityInfo = activityManager.GetCurrentActivityInfo();
        if (activityInfo.activity == null || activityInfo.variant == null) return;

        activityBar.SetActive(true);

        // Configurer l'icône de gauche avec l'activité générale
        if (leftIcon != null)
        {
            // Récupérer l'ActivityDefinition via le registry
            var locationActivity = activityManager.ActivityRegistry?.GetActivity(activityInfo.activity.ActivityId);
            if (locationActivity?.ActivityReference != null && locationActivity.ActivityReference.GetIcon() != null)
            {
                leftIcon.sprite = locationActivity.ActivityReference.GetIcon();
            }
            else
            {
                // Fallback: icône du variant si pas d'icône d'activité
                leftIcon.sprite = activityInfo.variant.GetIcon();
            }
            leftIcon.gameObject.SetActive(true);
        }

        // NOUVEAU: Configurer l'icône de droite avec le variant spécifique
        if (rightIcon != null)
        {
            rightIcon.sprite = activityInfo.variant.GetIcon();
            rightIcon.gameObject.SetActive(true); // ✅ Maintenant activée !
        }

        // Masquer la flèche (pas de voyage entre lieux)
        if (arrowIcon != null)
        {
            arrowIcon.SetActive(false);
        }

        // Texte
        if (activityText != null)
        {
            activityText.text = activityInfo.variant.GetDisplayName();
        }

        // Progression
        UpdateActivityProgress(activityInfo.activity, activityInfo.variant);
    }

    private void HideActivityBar()
    {
        if (activityBar != null)
        {
            activityBar.SetActive(false);
        }
    }

    private void UpdateTravelProgress(int currentSteps, int requiredSteps)
    {
        // CORRIGÉ: Ne plus dépendre du GameState, vérifier directement le voyage
        if (dataManager?.PlayerData?.IsCurrentlyTraveling() == true)
        {
            UpdateProgressBar(currentSteps, requiredSteps);
        }
    }

    private void UpdateActivityProgress(ActivityData activity, ActivityVariant variant)
    {
        // CORRIGÉ: Ne plus dépendre du GameState, vérifier directement l'activité
        if (activity != null && variant != null && activityManager.HasActiveActivity())
        {
            float progress = activity.GetProgressToNextTick(variant);
            int currentSteps = activity.AccumulatedSteps;
            int requiredSteps = variant.ActionCost;

            UpdateProgressBar(currentSteps, requiredSteps);
        }
    }

    // === NOUVELLE VERSION ANIMÉE DE UpdateProgressBar ===

    private void UpdateProgressBar(long current, long required)
    {
        if (fillBar == null || required <= 0) return;

        float newProgressValue = Mathf.Clamp01((float)current / required);

        // OPTIMISATION: Éviter les animations inutiles
        // Seuil de tolérance pour éviter les micro-animations
        if (Mathf.Abs(newProgressValue - lastProgressValue) < 0.001f) return;

        // Déterminer si c'est une progression (pour déclencher le pulse)
        bool isProgression = newProgressValue > lastProgressValue && lastProgressValue >= 0f;

        // Annuler l'animation précédente si elle existe
        if (currentAnimationId >= 0)
        {
            LeanTween.cancel(currentAnimationId);
        }

        // Démarrer l'animation fluide
        float startValue = fillBar.fillAmount;

        currentAnimationId = LeanTween.value(gameObject, startValue, newProgressValue, progressAnimationDuration)
            .setEase(progressAnimationEase)
            .setOnUpdate((float value) =>
            {
                if (fillBar != null)
                {
                    fillBar.fillAmount = value;
                }
            })
            .setOnComplete(() =>
            {
                currentAnimationId = -1;

                // Déclencher l'effet de pulse seulement en cas de progression
                if (isProgression)
                {
                    TriggerProgressPulse();
                }
            }).id;

        // Mettre à jour le cache
        lastProgressValue = newProgressValue;
    }

    /// <summary>
    /// Déclenche un effet de pulse subtil quand la barre progresse
    /// </summary>
    private void TriggerProgressPulse()
    {
        if (fillBar == null) return;

        // Annuler le pulse précédent s'il existe
        if (currentPulseId >= 0)
        {
            LeanTween.cancel(currentPulseId);
        }

        // Animation de pulse d'échelle
        Vector3 targetScale = originalFillScale * pulseScaleAmount;

        LeanTween.scale(fillBar.gameObject, targetScale, pulseDuration * 0.5f)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnComplete(() =>
            {
                // Retour à l'échelle normale
                LeanTween.scale(fillBar.gameObject, originalFillScale, pulseDuration * 0.5f)
                    .setEase(LeanTweenType.easeInQuad);
            });

        // Animation de pulse de couleur (plus sombre puis retour)
        currentPulseId = LeanTween.value(gameObject, 0f, 1f, pulseDuration)
            .setEase(LeanTweenType.easeInOutQuad)
            .setOnUpdate((float t) =>
            {
                if (fillBar != null)
                {
                    // Interpolation vers la couleur plus sombre puis retour
                    Color currentColor = Color.Lerp(
                        originalFillColor,
                        pulseColor,
                        Mathf.Sin(t * Mathf.PI) // Effet de "cloche" pour aller-retour
                    );
                    fillBar.color = currentColor;
                }
            })
            .setOnComplete(() =>
            {
                currentPulseId = -1;
                // S'assurer que la couleur revient à l'original
                if (fillBar != null)
                {
                    fillBar.color = originalFillColor;
                }
            }).id;
    }

    // === MÉTHODES PUBLIQUES ===

    public void ShowNavigationBar()
    {
        if (navigationBar != null)
        {
            navigationBar.SetActive(true);
        }
    }

    public void HideNavigationBar()
    {
        if (navigationBar != null)
        {
            navigationBar.SetActive(false);
        }
    }

    public void ForceRefresh()
    {
        RefreshDisplay();
    }

    // === NETTOYAGE ===

    void OnDestroy()
    {
        // Arrêter toutes les animations LeanTween
        if (currentAnimationId >= 0)
        {
            LeanTween.cancel(currentAnimationId);
        }
        if (currentPulseId >= 0)
        {
            LeanTween.cancel(currentPulseId);
        }

        // Arrêter les animations sur les GameObjects
        if (fillBar != null)
        {
            LeanTween.cancel(fillBar.gameObject);
        }

        // Se désabonner des événements
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= OnGameStateChanged;
        }

        if (mapManager != null)
        {
            mapManager.OnLocationChanged -= OnLocationChanged;
            mapManager.OnTravelProgress -= OnTravelProgress;
        }

        if (activityManager != null)
        {
            activityManager.OnActivityProgress -= OnActivityProgress;
            activityManager.OnActivityStopped -= OnActivityStopped;
        }
    }
}