// ===============================================
// NOUVEAU AboveCanvasManager (Facade Pattern) - REFACTORED
// ===============================================
// Purpose: Manages the always-visible UI elements above the main canvas
// Filepath: Assets/Scripts/UI/AboveCanvasManager.cs

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AboveCanvasManager : MonoBehaviour
{
    public static AboveCanvasManager Instance { get; private set; }

    // === SAME PUBLIC API - ZERO BREAKING CHANGES ===
    [Header("UI References - Header")]
    [SerializeField] private GameObject headerContainer;
    [SerializeField] private TextMeshProUGUI currentLocationText;
    [SerializeField] private Button mapButton;

    [Header("UI References - Activity/Travel Bar")]
    [SerializeField] private GameObject activityBar;
    [SerializeField] private Image leftIcon;
    [SerializeField] private Image rightIcon;
    [SerializeField] private TextMeshProUGUI activityText;
    [SerializeField] private Image backgroundBar;
    [SerializeField] private Image fillBar;
    [SerializeField] private GameObject arrowIcon;

    [Header("UI References - Navigation Bar")]
    [SerializeField] private GameObject navigationBar;

    [Header("Settings")]
    [SerializeField] private bool hideNavigationOnMap = true;

    [Header("Animation Settings")]
    [SerializeField] private float progressAnimationDuration = 0.3f;
    [SerializeField] private LeanTweenType progressAnimationEase = LeanTweenType.easeOutQuart;
    [SerializeField] private float pulseScaleAmount = 1.08f;
    [SerializeField] private float pulseDuration = 0.2f;
    [SerializeField] private Color pulseColor = new Color(0.8f, 0.8f, 0.8f, 1f);

    [Header("Pop Settings (Reward Animation)")]
    [SerializeField] private float popScaleAmount = 1.3f;        // Grossit de 30%
    [SerializeField] private float popDuration = 0.35f;         // Animation rapide mais visible
    [SerializeField] private Color popBrightColor = new Color(1.2f, 1.2f, 1.2f, 1f); // Plus lumineux
    [SerializeField] private LeanTweenType popEaseType = LeanTweenType.easeOutBack; // Effet bounce satisfaisant

    // === INTERNAL SERVICES (NOUVEAU) ===
    private AboveCanvasInitializationService initializationService;
    private AboveCanvasDisplayService displayService;
    private AboveCanvasAnimationService animationService;
    private AboveCanvasEventService eventService;

    // Internal accessors for services
    internal AboveCanvasEventService EventService => eventService;
    internal AboveCanvasAnimationService AnimationService => animationService;
    internal AboveCanvasDisplayService DisplayService => displayService;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeServices();
        }
        else
        {
            Logger.LogWarning("AboveCanvasManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.General);
            Destroy(gameObject);
        }
    }

    void Start()
    {
        initializationService.StartInitialization();
    }

    void OnEnable()
    {
        // Brancher les événements quand le GameObject devient actif
        // MAIS seulement si l'initialisation est terminée
        if (eventService != null && initializationService?.IsInitialized == true)
        {
            eventService.SubscribeToEvents();
        }
    }

    void OnDisable()
    {
        // Débrancher les événements quand le GameObject devient inactif
        if (eventService != null)
        {
            eventService.UnsubscribeFromEvents();
        }
    }

    void OnDestroy()
    {
        eventService?.Cleanup();
        animationService?.Cleanup();
    }

    private void InitializeServices()
    {
        // Créer les services dans l'ordre (animation et display d'abord)
        animationService = new AboveCanvasAnimationService(this);
        displayService = new AboveCanvasDisplayService(this);
        eventService = new AboveCanvasEventService(this, displayService, animationService);
        initializationService = new AboveCanvasInitializationService(this, eventService, animationService);

        // Initialiser les services dans l'ordre
        animationService.Initialize();
        displayService.Initialize(); // Maintenant displayService peut récupérer animationService
    }

    // === PUBLIC API - SAME AS BEFORE ===
    public void RefreshDisplay()
    {
        displayService.RefreshDisplay();
    }

    // === INTERNAL ACCESSORS FOR SERVICES ===
    public GameObject HeaderContainer => headerContainer;
    public TextMeshProUGUI CurrentLocationText => currentLocationText;
    public Button MapButton => mapButton;
    public GameObject ActivityBar => activityBar;
    public Image LeftIcon => leftIcon;
    public Image RightIcon => rightIcon;
    public TextMeshProUGUI ActivityText => activityText;
    public Image BackgroundBar => backgroundBar;
    public Image FillBar => fillBar;
    public GameObject ArrowIcon => arrowIcon;
    public GameObject NavigationBar => navigationBar;
    public bool HideNavigationOnMap => hideNavigationOnMap;

    // Animation Settings Accessors
    public float ProgressAnimationDuration => progressAnimationDuration;
    public LeanTweenType ProgressAnimationEase => progressAnimationEase;
    public float PulseScaleAmount => pulseScaleAmount;
    public float PulseDuration => pulseDuration;
    public Color PulseColor => pulseColor;
    // Pop Settings Accessors (Reward Animation)
    public float PopScaleAmount => popScaleAmount;
    public float PopDuration => popDuration;
    public Color PopBrightColor => popBrightColor;
    public LeanTweenType PopEaseType => popEaseType;
}

// ===============================================
// SERVICE: Initialization Management
// ===============================================
public class AboveCanvasInitializationService
{
    private readonly AboveCanvasManager manager;
    private readonly AboveCanvasEventService eventService;
    private readonly AboveCanvasAnimationService animationService;
    private bool isInitialized = false;

    public AboveCanvasInitializationService(AboveCanvasManager manager, AboveCanvasEventService eventService, AboveCanvasAnimationService animationService)
    {
        this.manager = manager;
        this.eventService = eventService;
        this.animationService = animationService;
    }

    public void StartInitialization()
    {
        manager.StartCoroutine(InitializeWithProperOrder());
    }

    private System.Collections.IEnumerator InitializeWithProperOrder()
    {
        // Attendre que les managers critiques soient disponibles
        yield return WaitForCriticalManagers();

        // Configurer la UI
        SetupUI();

        // Abonner aux événements immédiatement (comme avant)
        eventService?.SubscribeToEvents();

        // Première mise à jour de l'affichage
        manager.RefreshDisplay();

        // RESTAURATION: Vérification retardée pour s'assurer que l'affichage est correct
        // (C'était dans l'ancien code et ça résolvait les problèmes d'ordre d'initialisation)
        manager.StartCoroutine(DelayedDisplayRefresh());

        isInitialized = true;
        Logger.LogInfo("AboveCanvasManager: Initialized successfully", Logger.LogCategory.General);
    }

    private System.Collections.IEnumerator DelayedDisplayRefresh()
    {
        // Attendre quelques frames pour que tous les managers soient complètement initialisés
        yield return new WaitForSeconds(1f);

        // Forcer une mise à jour de l'affichage
        manager.RefreshDisplay();

        Logger.LogInfo("AboveCanvasManager: Delayed display refresh completed", Logger.LogCategory.General);
    }

    private System.Collections.IEnumerator WaitForCriticalManagers()
    {
        // Version optimisée : WaitUntil() arrête immédiatement quand la condition est remplie
        // → Évite de boucler toutes les 0.1s ; tu gagnes quelques ms au lancement
        yield return new WaitUntil(() => DataManager.Instance != null && MapManager.Instance != null);

        // Attendre un frame supplémentaire pour la stabilité
        yield return null;
    }

    private void SetupUI()
    {
        SetupMapButton();
        SetupProgressBar();
    }

    private void SetupMapButton()
    {
        if (manager.MapButton != null)
        {
            manager.MapButton.onClick.AddListener(OnMapButtonClicked);
        }
    }

    private void SetupProgressBar()
    {
        animationService?.SetupProgressBar();
    }

    private void OnMapButtonClicked()
    {
        if (manager.HideNavigationOnMap && manager.NavigationBar != null)
        {
            manager.NavigationBar.SetActive(false);
        }
        Logger.LogInfo("AboveCanvasManager: Map button clicked", Logger.LogCategory.General);
    }

    public bool IsInitialized => isInitialized;
}

// ===============================================
// SERVICE: Display Management
// ===============================================
public class AboveCanvasDisplayService
{
    private readonly AboveCanvasManager manager;
    private AboveCanvasAnimationService animationService;

    public AboveCanvasDisplayService(AboveCanvasManager manager)
    {
        this.manager = manager;
    }

    public void Initialize()
    {
        // Récupérer la référence au service d'animation
        animationService = manager.AnimationService;
    }

    public void RefreshDisplay()
    {
        Logger.LogInfo("AboveCanvasManager: RefreshDisplay called", Logger.LogCategory.General);
        UpdateLocationDisplay();
        UpdateActivityBarDisplay();
    }

    public void UpdateLocationDisplay()
    {
        if (manager.CurrentLocationText == null)
        {
            Logger.LogWarning("AboveCanvasManager: CurrentLocationText is null", Logger.LogCategory.General);
            return;
        }

        var mapManager = MapManager.Instance;
        if (mapManager?.CurrentLocation != null)
        {
            manager.CurrentLocationText.text = mapManager.CurrentLocation.DisplayName;
            Logger.LogInfo($"AboveCanvasManager: Updated location display to {mapManager.CurrentLocation.DisplayName}", Logger.LogCategory.General);
        }
        else
        {
            Logger.LogWarning("AboveCanvasManager: MapManager or CurrentLocation is null", Logger.LogCategory.General);
        }
    }

    public void UpdateActivityBarDisplay()
    {
        if (manager.ActivityBar == null)
        {
            Logger.LogWarning("AboveCanvasManager: ActivityBar is null", Logger.LogCategory.General);
            return;
        }

        var activityManager = ActivityManager.Instance;
        var dataManager = DataManager.Instance;

        bool hasActiveActivity = activityManager?.HasActiveActivity() == true;
        bool isCurrentlyTraveling = dataManager?.PlayerData?.IsCurrentlyTraveling() == true;

        Logger.LogInfo($"AboveCanvasManager: hasActiveActivity={hasActiveActivity}, isCurrentlyTraveling={isCurrentlyTraveling}", Logger.LogCategory.General);

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
        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData == null)
        {
            Logger.LogWarning("AboveCanvasManager: DataManager or PlayerData is null in SetupTravelDisplay", Logger.LogCategory.General);
            return;
        }

        Logger.LogInfo("AboveCanvasManager: Setting up travel display", Logger.LogCategory.General);
        manager.ActivityBar.SetActive(true);

        var playerData = dataManager.PlayerData;
        string currentLocationId = playerData.CurrentLocationId;
        string destinationId = playerData.TravelDestinationId;

        Logger.LogInfo($"AboveCanvasManager: Travel from {currentLocationId} to {destinationId}", Logger.LogCategory.General);

        // Configurer les icônes
        SetupTravelIcons(currentLocationId, destinationId);

        // Configurer le texte
        var destinationLocation = MapManager.Instance?.LocationRegistry?.GetLocationById(destinationId);
        if (manager.ActivityText != null && destinationLocation != null)
        {
            manager.ActivityText.text = $"Voyage vers {destinationLocation.DisplayName}";
            Logger.LogInfo($"AboveCanvasManager: Set travel text to 'Voyage vers {destinationLocation.DisplayName}'", Logger.LogCategory.General);
        }

        // Configurer la progression
        long progress = playerData.GetTravelProgress(playerData.TotalSteps);
        float progressPercent = (float)progress / playerData.TravelRequiredSteps;

        Logger.LogInfo($"AboveCanvasManager: Travel progress {progress}/{playerData.TravelRequiredSteps} = {progressPercent:F2}", Logger.LogCategory.General);

        if (manager.FillBar != null)
        {
            manager.FillBar.fillAmount = Mathf.Clamp01(progressPercent);
        }

        // Montrer la flèche pour le voyage
        if (manager.ArrowIcon != null)
        {
            manager.ArrowIcon.SetActive(true);
        }
    }

    private void SetupActivityDisplay()
    {
        var activityManager = ActivityManager.Instance;
        if (activityManager == null)
        {
            Logger.LogWarning("AboveCanvasManager: ActivityManager is null in SetupActivityDisplay", Logger.LogCategory.General);
            return;
        }

        var (activity, variant) = activityManager.GetCurrentActivityInfo();
        if (activity == null || variant == null)
        {
            Logger.LogWarning("AboveCanvasManager: Activity or variant is null", Logger.LogCategory.General);
            return;
        }

        Logger.LogInfo($"AboveCanvasManager: Setting up activity display for {variant.GetDisplayName()}", Logger.LogCategory.General);
        manager.ActivityBar.SetActive(true);

        // CORRECTION: Récupérer l'activité principale pour l'icône gauche
        var activityDefinition = activityManager.ActivityRegistry?.GetActivity(activity.ActivityId);

        // Configurer l'icône gauche avec l'ACTIVITÉ PRINCIPALE
        if (manager.LeftIcon != null)
        {
            var activityIcon = activityDefinition?.ActivityReference?.GetIcon();
            manager.LeftIcon.sprite = activityIcon;
            manager.LeftIcon.gameObject.SetActive(true);
            Logger.LogInfo($"AboveCanvasManager: Set left icon to ACTIVITY {(activityIcon != null ? activityIcon.name : "null")}", Logger.LogCategory.General);
        }

        // CORRECTION: Afficher l'icône droite avec le VARIANT
        if (manager.RightIcon != null)
        {
            var variantIcon = variant.GetIcon();
            manager.RightIcon.sprite = variantIcon;
            manager.RightIcon.gameObject.SetActive(true);
            Logger.LogInfo($"AboveCanvasManager: Set right icon to VARIANT {(variantIcon != null ? variantIcon.name : "null")}", Logger.LogCategory.General);
        }

        // NOUVEAU : Texte adapté selon le type d'activité
        if (manager.ActivityText != null)
        {
            if (activity.IsTimeBased)
            {
                // Pour les activités temporelles, afficher le temps restant
                long remainingTimeMs = activity.RequiredTimeMs - activity.AccumulatedTimeMs;
                string timeRemaining = FormatTime(remainingTimeMs);
                manager.ActivityText.text = $"{variant.GetDisplayName()} ({timeRemaining})";
            }
            else
            {
                // Pour les activités de pas, affichage standard
                manager.ActivityText.text = variant.GetDisplayName();
            }
            Logger.LogInfo($"AboveCanvasManager: Set activity text to '{manager.ActivityText.text}'", Logger.LogCategory.General);
        }

        // Configurer la progression
        float progressPercent = activity.GetProgressToNextTick(variant);
        Logger.LogInfo($"AboveCanvasManager: Activity progress = {progressPercent:F2}", Logger.LogCategory.General);

        if (manager.FillBar != null)
        {
            manager.FillBar.fillAmount = Mathf.Clamp01(progressPercent);

            // NOUVEAU : Couleur différente pour les activités temporelles
            if (activity.IsTimeBased)
            {
                manager.FillBar.color = Color.Lerp(Color.cyan, Color.yellow, progressPercent);
            }

        }

        // Masquer la flèche pour les activités (la flèche sert seulement pour les voyages)
        if (manager.ArrowIcon != null)
        {
            manager.ArrowIcon.SetActive(false);
        }
    }
    private string FormatTime(long timeMs)
    {
        if (timeMs <= 0) return "Terminé";

        if (timeMs < 1000)
            return $"{timeMs}ms";
        else if (timeMs < 60000)
            return $"{timeMs / 1000f:F0}s";
        else
            return $"{timeMs / 60000f:F1}min";
    }

    private void SetupTravelIcons(string currentLocationId, string destinationId)
    {
        var locationRegistry = MapManager.Instance?.LocationRegistry;
        if (locationRegistry == null)
        {
            Logger.LogWarning("AboveCanvasManager: LocationRegistry is null in SetupTravelIcons", Logger.LogCategory.General);
            return;
        }

        // Icône de départ
        if (manager.LeftIcon != null)
        {
            var currentLocation = locationRegistry.GetLocationById(currentLocationId);
            if (currentLocation != null)
            {
                var icon = currentLocation.GetIcon();
                manager.LeftIcon.sprite = icon;
                Logger.LogInfo($"AboveCanvasManager: Set left travel icon to {(icon != null ? icon.name : "null")} for location {currentLocationId}", Logger.LogCategory.General);
            }
            else
            {
                Logger.LogWarning($"AboveCanvasManager: Current location {currentLocationId} not found", Logger.LogCategory.General);
            }
            manager.LeftIcon.gameObject.SetActive(true);
        }

        // Icône d'arrivée
        if (manager.RightIcon != null)
        {
            var destinationLocation = locationRegistry.GetLocationById(destinationId);
            if (destinationLocation != null)
            {
                var icon = destinationLocation.GetIcon();
                manager.RightIcon.sprite = icon;
                Logger.LogInfo($"AboveCanvasManager: Set right travel icon to {(icon != null ? icon.name : "null")} for destination {destinationId}", Logger.LogCategory.General);
            }
            else
            {
                Logger.LogWarning($"AboveCanvasManager: Destination location {destinationId} not found", Logger.LogCategory.General);
            }
            manager.RightIcon.gameObject.SetActive(true);
        }
    }

    private void HideActivityBar()
    {
        if (manager.ActivityBar != null)
        {
            manager.ActivityBar.SetActive(false);
        }
    }

    public void UpdateTravelProgress(int currentSteps, int requiredSteps)
    {
        if (manager.FillBar == null) return;

        float progressPercent = (float)currentSteps / requiredSteps;
        animationService?.AnimateProgressBar(progressPercent);
    }

    public void UpdateActivityProgress(ActivityData activity, ActivityVariant variant)
    {
        if (manager.FillBar == null || activity == null || variant == null) return;

        float progressPercent = activity.GetProgressToNextTick(variant);
        animationService?.AnimateProgressBar(progressPercent);

        // NOUVEAU : Mettre à jour le texte pour les activités temporelles
        if (activity.IsTimeBased && manager.ActivityText != null)
        {
            long remainingTimeMs = activity.RequiredTimeMs - activity.AccumulatedTimeMs;
            string timeRemaining = FormatTime(remainingTimeMs);
            manager.ActivityText.text = $"{variant.GetDisplayName()} ({timeRemaining})";
        }
    }
}

// ===============================================
// SERVICE: Animation Management
// ===============================================
public class AboveCanvasAnimationService
{
    private readonly AboveCanvasManager manager;
    private float lastProgressValue = -1f;
    private int currentAnimationId = -1;
    private int currentPulseId = -1;
    private int currentPopId = -1;        // Renommé pour le pop
    private Color originalFillColor;
    private Vector3 originalFillScale;
    private Vector3 originalRightIconScale;  // Échelle originale de l'icône droite
    private Color originalRightIconColor;    // Couleur originale de l'icône droite

    public AboveCanvasAnimationService(AboveCanvasManager manager)
    {
        this.manager = manager;
    }

    public void Initialize()
    {
        // Configuration des animations peut se faire ici
    }

    public void SetupProgressBar()
    {
        if (manager.FillBar != null)
        {
            // Configurer en mode Filled
            if (manager.FillBar.type != Image.Type.Filled)
            {
                manager.FillBar.type = Image.Type.Filled;
                manager.FillBar.fillMethod = Image.FillMethod.Horizontal;
            }

            // Sauvegarder les valeurs originales
            originalFillColor = manager.FillBar.color;
            originalFillScale = manager.FillBar.transform.localScale;
            manager.FillBar.fillAmount = 0f;
            lastProgressValue = 0f;
        }

        if (manager.BackgroundBar != null && manager.BackgroundBar.type == Image.Type.Filled)
        {
            manager.BackgroundBar.type = Image.Type.Simple;
        }

        // Sauvegarder les valeurs originales de l'icône droite pour l'animation de pop
        if (manager.RightIcon != null)
        {
            originalRightIconScale = manager.RightIcon.transform.localScale;
            originalRightIconColor = manager.RightIcon.color;
        }
    }

    public void AnimateProgressBar(float targetProgress)
    {
        if (manager.FillBar == null) return;
        if (Mathf.Approximately(targetProgress, lastProgressValue)) return;

        // Arrêter les animations précédentes
        if (currentAnimationId != -1)
        {
            LeanTween.cancel(currentAnimationId);
        }
        if (currentPulseId != -1)
        {
            LeanTween.cancel(currentPulseId);
        }

        // 1. Animer le remplissage de la barre
        currentAnimationId = LeanTween.value(manager.gameObject, lastProgressValue, targetProgress, manager.ProgressAnimationDuration)
            .setEase(manager.ProgressAnimationEase)
            .setOnUpdate((float val) =>
            {
                if (manager.FillBar != null)
                {
                    manager.FillBar.fillAmount = val;
                }
            })
            .setOnComplete(() =>
            {
                currentAnimationId = -1;
            }).id;

        // 2. IMMÉDIATEMENT déclencher le pulse en parallèle (pas à la fin !)
        PulseFillBarParallel();

        lastProgressValue = targetProgress;
    }

    private void PulseFillBarParallel()
    {
        if (manager.FillBar == null) return;

        // Pulse d'échelle EN MÊME TEMPS que l'animation de remplissage
        currentPulseId = LeanTween.scale(manager.FillBar.gameObject, originalFillScale * manager.PulseScaleAmount, manager.PulseDuration)
            .setEase(LeanTweenType.easeOutQuart)
            .setLoopPingPong(1)
            .setOnComplete(() =>
            {
                currentPulseId = -1;
                if (manager.FillBar != null)
                {
                    manager.FillBar.transform.localScale = originalFillScale;
                }
            }).id;

        // Animation de couleur EN PARALLÈLE (pas d'ID à stocker, courte durée)
        LeanTween.color(manager.FillBar.rectTransform, manager.PulseColor, manager.PulseDuration)
            .setEase(LeanTweenType.easeOutQuart)
            .setLoopPingPong(1)
            .setOnComplete(() =>
            {
                if (manager.FillBar != null)
                {
                    manager.FillBar.color = originalFillColor;
                }
            });
    }

    public void PulseFillBar()
    {
        // Méthode séparée pour les cas où on veut juste un pulse sans animation de remplissage
        PulseFillBarParallel();
    }

    public void ShakeRightIcon()
    {
        // NOUVEAU : Pop de récompense au lieu de shake d'erreur !
        PopRightIcon();
    }

    private void PopRightIcon()
    {
        if (manager.RightIcon == null) return;

        // Arrêter le pop précédent
        if (currentPopId != -1)
        {
            LeanTween.cancel(currentPopId);
        }

        // Animation de POP satisfaisante :
        // 1. Grossit rapidement avec couleur plus lumineuse
        // 2. Retourne à la normale avec bounce

        // Scale + couleur en parallèle
        currentPopId = LeanTween.scale(manager.RightIcon.gameObject, originalRightIconScale * manager.PopScaleAmount, manager.PopDuration * 0.4f)
            .setEase(LeanTweenType.easeOutQuart)
            .setOnComplete(() =>
            {
                // Phase 2: Retour à la normale avec bounce satisfaisant
                LeanTween.scale(manager.RightIcon.gameObject, originalRightIconScale, manager.PopDuration * 0.6f)
                    .setEase(manager.PopEaseType) // easeOutBack pour l'effet bounce
                    .setOnComplete(() =>
                    {
                        currentPopId = -1;
                    });
            }).id;

        // Animation de couleur en parallèle (illumination)
        LeanTween.color(manager.RightIcon.rectTransform, manager.PopBrightColor, manager.PopDuration * 0.4f)
            .setEase(LeanTweenType.easeOutQuart)
            .setOnComplete(() =>
            {
                // Retour couleur normale
                LeanTween.color(manager.RightIcon.rectTransform, originalRightIconColor, manager.PopDuration * 0.6f)
                    .setEase(LeanTweenType.easeOutQuart);
            });
    }

    public void Cleanup()
    {
        // Version sécurisée : annuler par GameObject plutôt que par ID
        // Évite les "orphan tweens" quand l'objet est détruit
        if (manager.FillBar != null)
        {
            LeanTween.cancel(manager.FillBar.gameObject);
        }

        if (manager.RightIcon != null)
        {
            LeanTween.cancel(manager.RightIcon.gameObject);
        }

        // Reset des IDs pour sécurité
        currentAnimationId = -1;
        currentPulseId = -1;
        currentPopId = -1;
    }
}

// ===============================================
// SERVICE: Event Management
// ===============================================
public class AboveCanvasEventService
{
    private readonly AboveCanvasManager manager;
    private readonly AboveCanvasDisplayService displayService;
    private readonly AboveCanvasAnimationService animationService;

    public AboveCanvasEventService(AboveCanvasManager manager, AboveCanvasDisplayService displayService, AboveCanvasAnimationService animationService)
    {
        this.manager = manager;
        this.displayService = displayService;
        this.animationService = animationService;
    }

    public void SubscribeToEvents()
    {
        var gameManager = GameManager.Instance;
        var mapManager = MapManager.Instance;
        var activityManager = ActivityManager.Instance;

        // S'abonner aux événements
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += OnGameStateChanged;
        }

        if (mapManager != null)
        {
            mapManager.OnLocationChanged += OnLocationChanged;
            mapManager.OnTravelProgress += OnTravelProgress;
        }

        if (activityManager != null)
        {
            activityManager.OnActivityProgress += OnActivityProgress;
            activityManager.OnActivityStopped += OnActivityStopped;
            activityManager.OnActivityTick += OnActivityTick;
        }
    }

    public void UnsubscribeFromEvents()
    {
        var gameManager = GameManager.Instance;
        var mapManager = MapManager.Instance;
        var activityManager = ActivityManager.Instance;

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
            activityManager.OnActivityTick -= OnActivityTick;
        }
    }

    public void Cleanup()
    {
        // Désabonner de tous les événements
        UnsubscribeFromEvents();

        // Nettoyer les animations
        animationService?.Cleanup();
    }

    // === EVENT HANDLERS ===
    private void OnGameStateChanged(GameState oldState, GameState newState)
    {
        Logger.LogInfo($"AboveCanvasManager: Game state changed from {oldState} to {newState}", Logger.LogCategory.General);
        displayService.RefreshDisplay();
    }

    private void OnLocationChanged(MapLocationDefinition newLocation)
    {
        displayService.UpdateLocationDisplay();
    }

    private void OnTravelProgress(string destinationId, int currentSteps, int requiredSteps)
    {
        displayService.UpdateTravelProgress(currentSteps, requiredSteps);
    }

    private void OnActivityProgress(ActivityData activity, ActivityVariant variant)
    {
        displayService.UpdateActivityProgress(activity, variant);
    }

    private void OnActivityStopped(ActivityData activity, ActivityVariant variant)
    {
        Logger.LogInfo("AboveCanvasManager: Activity stopped, refreshing display", Logger.LogCategory.General);
        displayService.RefreshDisplay();
    }

    private void OnActivityTick(ActivityData activity, ActivityVariant variant, int ticksCompleted)
    {
        if (ticksCompleted > 0)
        {
            animationService?.ShakeRightIcon(); // Maintenant c'est un pop satisfaisant !
            Logger.LogInfo($"AboveCanvasManager: Activity tick completed - reward pop animation", Logger.LogCategory.General);
        }
    }
}