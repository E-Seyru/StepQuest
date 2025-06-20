// ===============================================
// NOUVEAU AboveCanvasManager (Facade Pattern) - REFACTORED
// ===============================================
// Purpose: Manages the always-visible UI elements above the main canvas
// Filepath: Assets/Scripts/UI/AboveCanvasManager.cs

using ActivityEvents;
using GameEvents;
using MapEvents;
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
    [SerializeField] private Button locationButton;
    [SerializeField] private Image locationButtonIcon; // NOUVEAU : Image du POI dans le LocationButton
    [SerializeField] private Image locationButtonBackground; // NOUVEAU : Background du LocationButton pour les effets
    [SerializeField] private Image locationButtonShadow; // NOUVEAU : Ombre du LocationButton (optionnel)

    [Header("UI References - Activity/Travel Bar")]
    [SerializeField] private GameObject activityBar;
    [SerializeField] private Image leftIcon;
    [SerializeField] private Image rightIcon;
    [SerializeField] private TextMeshProUGUI activityText;
    [SerializeField] private Image backgroundBar;
    [SerializeField] private Image fillBar;
    [SerializeField] private GameObject arrowIcon;

    [Header("UI References - Idle Bar")]
    [SerializeField] private GameObject idleBar;
    [SerializeField] private Image idleBarImage;  // NOUVEAU : L'image à l'intérieur de l'IdleBar pour l'animation

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

    [Header("Slide Animation Settings")]
    [SerializeField] private float slideAnimationDuration = 0.25f;
    [SerializeField] private LeanTweenType slideAnimationEase = LeanTweenType.easeOutQuart;

    [Header("Pop Settings (Reward Animation)")]
    [SerializeField] private float popScaleAmount = 1.3f;        // Grossit de 30%
    [SerializeField] private float popDuration = 0.35f;         // Animation rapide mais visible
    [SerializeField] private Color popBrightColor = new Color(1.2f, 1.2f, 1.2f, 1f); // Plus lumineux
    [SerializeField] private LeanTweenType popEaseType = LeanTweenType.easeOutBack; // Effet bounce satisfaisant

    [Header("Idle Bar Animation Settings")]
    [SerializeField] private float idleAnimationInterval = 2.5f;    // Intervalle entre les ronflements (en secondes)
    [SerializeField] private float idleSnoreDuration = 1.2f;        // Durée d'un ronflement complet
    [SerializeField] private float idleSnoreScale = 1.25f;          // Facteur d'agrandissement (25% plus grand)
    [SerializeField] private float idleShakeIntensity = 5f;         // Intensité de la vibration (en pixels)
    [SerializeField] private LeanTweenType idleInflateEase = LeanTweenType.easeInSine;   // Animation d'inspiration
    [SerializeField] private LeanTweenType idleDeflateEase = LeanTweenType.easeOutBounce; // Animation d'expiration

    [Header("LocationButton Settings")]
    [SerializeField] private float locationButtonClickScale = 0.95f;     // Facteur de rétrécissement au clic (ex: 0.95 = 5% plus petit)
    [SerializeField] private float locationButtonClickDuration = 0.1f;   // Durée de l'animation de clic
    [SerializeField] private LeanTweenType locationButtonClickEase = LeanTweenType.easeOutQuart; // Type d'animation
    [SerializeField] private Vector2 shadowOffset = new Vector2(3f, -3f); // Décalage de l'ombre (x, y)
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.3f); // Couleur de l'ombre

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
    public Button LocationButton => locationButton;
    public Image LocationButtonIcon => locationButtonIcon; // NOUVEAU : Accessor pour l'icône du LocationButton
    public Image LocationButtonBackground => locationButtonBackground; // NOUVEAU : Accessor pour le background
    public Image LocationButtonShadow => locationButtonShadow; // NOUVEAU : Accessor pour l'ombre
    public GameObject ActivityBar => activityBar;
    public Image LeftIcon => leftIcon;
    public Image RightIcon => rightIcon;
    public TextMeshProUGUI ActivityText => activityText;
    public Image BackgroundBar => backgroundBar;
    public Image FillBar => fillBar;
    public GameObject ArrowIcon => arrowIcon;

    // NOUVEAU : Accessor pour IdleBar
    public GameObject IdleBar => idleBar;
    public Image IdleBarImage => idleBarImage;  // NOUVEAU : Accessor pour l'image de l'IdleBar

    public GameObject NavigationBar => navigationBar;
    public bool HideNavigationOnMap => hideNavigationOnMap;

    // Animation Settings Accessors
    public float ProgressAnimationDuration => progressAnimationDuration;
    public LeanTweenType ProgressAnimationEase => progressAnimationEase;
    public float PulseScaleAmount => pulseScaleAmount;
    public float PulseDuration => pulseDuration;
    public Color PulseColor => pulseColor;
    // Slide Animation Settings Accessors
    public float SlideAnimationDuration => slideAnimationDuration;
    public LeanTweenType SlideAnimationEase => slideAnimationEase;
    // Pop Settings Accessors (Reward Animation)
    public float PopScaleAmount => popScaleAmount;
    public float PopDuration => popDuration;
    public Color PopBrightColor => popBrightColor;
    public LeanTweenType PopEaseType => popEaseType;

    // NOUVEAU : Idle Animation Settings Accessors
    public float IdleAnimationInterval => idleAnimationInterval;
    public float IdleSnoreDuration => idleSnoreDuration;
    public float IdleSnoreScale => idleSnoreScale;
    public float IdleShakeIntensity => idleShakeIntensity;
    public LeanTweenType IdleInflateEase => idleInflateEase;
    public LeanTweenType IdleDeflateEase => idleDeflateEase;

    // NOUVEAU : LocationButton Settings Accessors
    public float LocationButtonClickScale => locationButtonClickScale;
    public float LocationButtonClickDuration => locationButtonClickDuration;
    public LeanTweenType LocationButtonClickEase => locationButtonClickEase;
    public Vector2 ShadowOffset => shadowOffset;
    public Color ShadowColor => shadowColor;
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

        // NOUVEAU : Marquer la fin de l'initialisation pour activer les animations
        manager.DisplayService.FinishInitialization();

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
        SetupLocationButton(); // NOUVEAU: Ajouter cette ligne
        SetupProgressBar();
    }

    private void SetupMapButton()
    {
        if (manager.MapButton != null)
        {
            manager.MapButton.onClick.AddListener(OnMapButtonClicked);
        }
    }

    // NOUVEAU: Méthode pour configurer le LocationButton
    private void SetupLocationButton()
    {
        if (manager.LocationButton != null)
        {
            manager.LocationButton.onClick.AddListener(OnLocationButtonClicked);

            // NOUVEAU : Initialiser l'ombre du LocationButton
            InitializeLocationButtonShadow();

            // NOUVEAU : Initialiser l'icône du LocationButton
            InitializeLocationButtonIcon();

            Logger.LogInfo("AboveCanvasManager: LocationButton configured", Logger.LogCategory.General);
        }
        else
        {
            Logger.LogWarning("AboveCanvasManager: LocationButton is null", Logger.LogCategory.General);
        }
    }

    // NOUVEAU : Méthode pour initialiser l'ombre du LocationButton
    private void InitializeLocationButtonShadow()
    {
        if (manager.LocationButtonShadow == null) return;

        // Configuration de l'ombre
        manager.LocationButtonShadow.color = manager.ShadowColor;

        // Positionner l'ombre avec le décalage
        RectTransform shadowRect = manager.LocationButtonShadow.GetComponent<RectTransform>();
        if (shadowRect != null)
        {
            shadowRect.anchoredPosition = manager.ShadowOffset;
        }

        Logger.LogInfo("AboveCanvasManager: LocationButton shadow configured", Logger.LogCategory.General);
    }

    // NOUVEAU : Méthode pour initialiser l'icône du LocationButton
    private void InitializeLocationButtonIcon()
    {
        if (manager.LocationButtonIcon == null)
        {
            Logger.LogWarning("AboveCanvasManager: LocationButtonIcon is null - make sure to assign it in the inspector", Logger.LogCategory.General);
            return;
        }

        // Configuration initiale de l'image
        manager.LocationButtonIcon.preserveAspect = true; // S'assurer que Preserve Aspect est activé

        // L'icône sera mise à jour quand UpdateLocationDisplay() sera appelé
        Logger.LogInfo("AboveCanvasManager: LocationButtonIcon initialized", Logger.LogCategory.General);
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

    // NOUVEAU: Gestionnaire pour le clic sur LocationButton
    private void OnLocationButtonClicked()
    {
        // NOUVEAU : Jouer l'effet de clic d'abord
        PlayLocationButtonClickEffect();

        // Vérifier que PanelManager est disponible
        if (PanelManager.Instance == null)
        {
            Logger.LogWarning("AboveCanvasManager: PanelManager.Instance is null", Logger.LogCategory.General);
            return;
        }

        // Naviguer vers le LocationDetailsPanel
        // Le nom doit correspondre exactement au nom du GameObject dans Unity
        PanelManager.Instance.HideMapAndGoToPanel("LocationDetailsPanel");

        Logger.LogInfo("AboveCanvasManager: Navigating to LocationDetailsPanel", Logger.LogCategory.General);
    }

    // NOUVEAU : Méthode pour l'effet de clic du LocationButton
    private void PlayLocationButtonClickEffect()
    {
        if (manager.LocationButton == null) return;

        // Annuler toute animation en cours sur le bouton
        LeanTween.cancel(manager.LocationButton.gameObject);

        // Effet de "squeeze" sur tout le bouton : rétrécissement rapide puis retour à la normale
        LeanTween.scale(manager.LocationButton.gameObject, Vector3.one * manager.LocationButtonClickScale, manager.LocationButtonClickDuration)
            .setEase(manager.LocationButtonClickEase)
            .setOnComplete(() =>
            {
                // Retour à la taille normale
                LeanTween.scale(manager.LocationButton.gameObject, Vector3.one, manager.LocationButtonClickDuration)
                    .setEase(manager.LocationButtonClickEase);
            });

        Logger.LogInfo("AboveCanvasManager: LocationButton click effect triggered", Logger.LogCategory.General);
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
    private bool isInitializing = true; // NOUVEAU : Flag pour éviter animations pendant init

    public AboveCanvasDisplayService(AboveCanvasManager manager)
    {
        this.manager = manager;
    }

    public void Initialize()
    {
        // Récupérer la référence au service d'animation
        animationService = manager.AnimationService;
    }

    // NOUVEAU : Méthode pour marquer la fin de l'initialisation
    public void FinishInitialization()
    {
        isInitializing = false;
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

            // NOUVEAU : Mettre à jour l'icône du LocationButton
            UpdateLocationButtonIcon(mapManager.CurrentLocation);

            Logger.LogInfo($"AboveCanvasManager: Updated location display to {mapManager.CurrentLocation.DisplayName}", Logger.LogCategory.General);
        }
        else
        {
            Logger.LogWarning("AboveCanvasManager: MapManager or CurrentLocation is null", Logger.LogCategory.General);
        }
    }

    // NOUVEAU : Méthode pour mettre à jour l'icône du LocationButton
    private void UpdateLocationButtonIcon(MapLocationDefinition location)
    {
        if (manager.LocationButtonIcon == null) return;

        var locationIcon = location?.GetIcon();
        if (locationIcon != null)
        {
            manager.LocationButtonIcon.sprite = locationIcon;
            manager.LocationButtonIcon.color = Color.white; // S'assurer que l'image est visible
            Logger.LogInfo($"AboveCanvasManager: Updated LocationButton icon to {locationIcon.name}", Logger.LogCategory.General);
        }
        else
        {
            // Image par défaut ou cacher l'icône si pas d'image
            manager.LocationButtonIcon.sprite = null;
            manager.LocationButtonIcon.color = Color.clear; // Ou utiliser une image par défaut
            Logger.LogInfo("AboveCanvasManager: No icon available for current location", Logger.LogCategory.General);
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
            HideIdleBar(); // NOUVEAU : Cacher IdleBar pendant voyage
        }
        else if (hasActiveActivity)
        {
            SetupActivityDisplay();
            HideIdleBar(); // NOUVEAU : Cacher IdleBar pendant activité
        }
        else
        {
            HideActivityBar();
            ShowIdleBar(); // NOUVEAU : Afficher IdleBar quand inactif
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

        if (isInitializing)
        {
            manager.ActivityBar.SetActive(true);
        }
        else
        {
            animationService?.SlideInBar(manager.ActivityBar);
        }

        var playerData = dataManager.PlayerData;
        string currentLocationId = playerData.CurrentLocationId;
        string destinationId = playerData.TravelDestinationId;

        Logger.LogInfo($"AboveCanvasManager: Travel from {currentLocationId} to {destinationId}", Logger.LogCategory.General);

        // Configurer les icônes
        SetupTravelIcons(currentLocationId, destinationId);

        // Calculer la progression une seule fois
        long progress = playerData.GetTravelProgress(playerData.TotalSteps);
        float progressPercent = (float)progress / playerData.TravelRequiredSteps;

        // Configurer le texte avec progression de voyage
        if (manager.ActivityText != null)
        {
            string progressText = $"{progress} / {playerData.TravelRequiredSteps}";
            manager.ActivityText.text = progressText;
            Logger.LogInfo($"AboveCanvasManager: Set travel text to '{progressText}'", Logger.LogCategory.General);
        }

        // Configurer la progression
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

        if (isInitializing)
        {
            manager.ActivityBar.SetActive(true);
        }
        else
        {
            animationService?.SlideInBar(manager.ActivityBar);
        }

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

        // NOUVEAU : Affichage du texte avec progression détaillée
        if (manager.ActivityText != null)
        {
            string progressText = FormatActivityProgress(activity, variant);
            manager.ActivityText.text = progressText; // Juste la progression, sans le nom
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

    // NOUVEAU : Méthode pour formater l'affichage de progression d'activité
    private string FormatActivityProgress(ActivityData activity, ActivityVariant variant)
    {
        if (activity.IsTimeBased)
        {
            // Pour les activités temporelles : "5 min 30 s / 10 min"
            string currentTime = FormatTimeForProgress(activity.AccumulatedTimeMs);
            string totalTime = FormatTimeForProgress(activity.RequiredTimeMs);
            return $"{currentTime} / {totalTime}";
        }
        else
        {
            // Pour les activités à pas : "x / x"
            int currentSteps = (int)activity.AccumulatedSteps;
            int totalSteps = variant.ActionCost; // ActionCost = pas requis par tick
            return $"{currentSteps} / {totalSteps}";
        }
    }

    // NOUVEAU : Méthode pour formater le temps de manière intelligente
    private string FormatTimeForProgress(long timeMs)
    {
        if (timeMs <= 0) return "0 s";

        int totalSeconds = Mathf.RoundToInt(timeMs / 1000f);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;

        if (hours > 0)
        {
            // Format avec heures : "1 h 30 min 45 s" ou "2 h 15 min" ou "3 h"
            if (minutes > 0 && seconds > 0)
                return $"{hours} h {minutes} min {seconds} s";
            else if (minutes > 0)
                return $"{hours} h {minutes} min";
            else
                return $"{hours} h";
        }
        else if (minutes > 0)
        {
            // Format avec minutes : "5 min 30 s" ou "10 min"
            if (seconds > 0)
                return $"{minutes} min {seconds} s";
            else
                return $"{minutes} min";
        }
        else
        {
            // Format avec secondes seulement : "45 s"
            return $"{seconds} s";
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
        animationService?.HideBar(manager.ActivityBar);
    }

    // ===============================================
    // NOUVELLES MÉTHODES POUR IDLEBAR
    // ===============================================

    private void ShowIdleBar()
    {
        if (manager.IdleBar == null) return;

        Logger.LogInfo("AboveCanvasManager: Showing idle bar", Logger.LogCategory.General);

        if (isInitializing)
        {
            manager.IdleBar.SetActive(true);
        }
        else
        {
            animationService?.SlideInBar(manager.IdleBar);
        }

        // NOUVEAU : Démarrer l'animation répétitive d'inactivité
        animationService?.StartIdleBarAnimation();
    }

    private void HideIdleBar()
    {
        // NOUVEAU : Arrêter l'animation répétitive d'inactivité
        animationService?.StopIdleBarAnimation();
        animationService?.HideBar(manager.IdleBar);
    }

    public void UpdateTravelProgress(int currentSteps, int requiredSteps)
    {
        if (manager.FillBar == null) return;

        float progressPercent = (float)currentSteps / requiredSteps;
        animationService?.AnimateProgressBar(progressPercent);

        // NOUVEAU : Mettre à jour le texte de progression pour les voyages
        if (manager.ActivityText != null)
        {
            string progressText = $"{currentSteps} / {requiredSteps}";
            manager.ActivityText.text = progressText;
        }
    }

    public void UpdateActivityProgress(ActivityData activity, ActivityVariant variant)
    {
        if (manager.FillBar == null || activity == null || variant == null) return;

        float progressPercent = activity.GetProgressToNextTick(variant);
        animationService?.AnimateProgressBar(progressPercent);

        // NOUVEAU : Mettre à jour le texte avec la progression détaillée
        if (manager.ActivityText != null)
        {
            string progressText = FormatActivityProgress(activity, variant);
            manager.ActivityText.text = progressText; // Juste la progression, sans le nom
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

    // NOUVEAU : Positions originales pour les animations de slide
    private Vector3 activityBarOriginalPosition;
    private Vector3 idleBarOriginalPosition;
    private bool positionsSaved = false;

    // NOUVEAU : Variables pour l'animation de l'IdleBar
    private Vector3 idleBarImageOriginalScale;    // Échelle originale de l'image IdleBar
    private Vector3 idleBarImageOriginalPosition; // Position originale de l'image IdleBar
    private int idleAnimationTimerId = -1;        // ID du timer pour répéter l'animation
    private int idleSnoreAnimationId = -1;        // ID de l'animation de ronflement
    private int idleShakeAnimationId = -1;        // ID de l'animation de vibration
    private bool isIdleAnimationActive = false;   // Flag pour savoir si l'animation est active

    public AboveCanvasAnimationService(AboveCanvasManager manager)
    {
        this.manager = manager;
    }

    public void Initialize()
    {
        // Configuration des animations peut se faire ici
        SaveOriginalPositions();
        SaveIdleBarImageOriginalValues();
    }

    private void SaveOriginalPositions()
    {
        if (positionsSaved) return;

        if (manager.ActivityBar != null)
        {
            activityBarOriginalPosition = manager.ActivityBar.transform.localPosition;
        }

        if (manager.IdleBar != null)
        {
            idleBarOriginalPosition = manager.IdleBar.transform.localPosition;
        }

        positionsSaved = true;
    }

    // NOUVEAU : Sauvegarder les valeurs originales de l'image IdleBar
    private void SaveIdleBarImageOriginalValues()
    {
        if (manager.IdleBarImage != null)
        {
            idleBarImageOriginalScale = manager.IdleBarImage.transform.localScale;
            idleBarImageOriginalPosition = manager.IdleBarImage.transform.localPosition;
        }
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

    // NOUVEAU : Animation de slide pour les barres
    public void SlideInBar(GameObject bar)
    {
        if (bar == null) return;

        // Activer la barre d'abord
        bar.SetActive(true);

        // Déterminer la position originale
        Vector3 originalPos;
        if (bar == manager.ActivityBar)
        {
            originalPos = activityBarOriginalPosition;
        }
        else if (bar == manager.IdleBar)
        {
            originalPos = idleBarOriginalPosition;
        }
        else
        {
            return; // Barre inconnue
        }

        // Position de départ (au-dessus, cachée)
        Vector3 startPos = originalPos;
        startPos.y += 100f; // Décaler vers le haut

        // Positionner la barre en position de départ
        bar.transform.localPosition = startPos;

        // Animer vers la position originale
        LeanTween.moveLocal(bar, originalPos, manager.SlideAnimationDuration)
            .setEase(manager.SlideAnimationEase);
    }

    public void HideBar(GameObject bar)
    {
        if (bar == null) return;

        bar.SetActive(false);

        // Remettre en position cachée pour la prochaine animation
        Vector3 originalPos;
        if (bar == manager.ActivityBar)
        {
            originalPos = activityBarOriginalPosition;
        }
        else if (bar == manager.IdleBar)
        {
            originalPos = idleBarOriginalPosition;
        }
        else
        {
            return; // Barre inconnue
        }

        Vector3 hiddenPos = originalPos;
        hiddenPos.y += 100f; // Position cachée au-dessus
        bar.transform.localPosition = hiddenPos;
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

    // ===============================================
    // NOUVEAU : GESTION DE L'ANIMATION IDLE BAR
    // ===============================================

    /// <summary>
    /// Démarre l'animation répétitive de ronflement de l'IdleBar
    /// </summary>
    public void StartIdleBarAnimation()
    {
        if (manager.IdleBarImage == null || isIdleAnimationActive) return;

        isIdleAnimationActive = true;
        Logger.LogInfo("AboveCanvasManager: Starting idle bar snore animation", Logger.LogCategory.General);

        // Démarrer immédiatement le premier ronflement
        PlayIdleSnoreAnimation();

        // Puis programmer les répétitions
        ScheduleNextIdleAnimation();
    }

    /// <summary>
    /// Arrête l'animation répétitive de ronflement de l'IdleBar
    /// </summary>
    public void StopIdleBarAnimation()
    {
        if (!isIdleAnimationActive) return;

        isIdleAnimationActive = false;
        Logger.LogInfo("AboveCanvasManager: Stopping idle bar snore animation", Logger.LogCategory.General);

        // Annuler le timer de répétition
        if (idleAnimationTimerId != -1)
        {
            LeanTween.cancel(idleAnimationTimerId);
            idleAnimationTimerId = -1;
        }

        // Annuler les animations en cours
        if (idleSnoreAnimationId != -1)
        {
            LeanTween.cancel(idleSnoreAnimationId);
            idleSnoreAnimationId = -1;
        }

        if (idleShakeAnimationId != -1)
        {
            LeanTween.cancel(idleShakeAnimationId);
            idleShakeAnimationId = -1;
        }

        // Remettre l'image à ses valeurs originales
        if (manager.IdleBarImage != null)
        {
            manager.IdleBarImage.transform.localScale = idleBarImageOriginalScale;
            manager.IdleBarImage.transform.localPosition = idleBarImageOriginalPosition;
        }
    }

    /// <summary>
    /// Programme la prochaine animation de ronflement
    /// </summary>
    private void ScheduleNextIdleAnimation()
    {
        if (!isIdleAnimationActive) return;

        // Programmer le prochain ronflement après l'intervalle défini
        idleAnimationTimerId = LeanTween.delayedCall(manager.IdleAnimationInterval, () =>
        {
            if (isIdleAnimationActive) // Vérifier qu'on n'a pas arrêté entre temps
            {
                PlayIdleSnoreAnimation();
                ScheduleNextIdleAnimation(); // Programmer la suivante (récursion)
            }
        }).id;
    }

    /// <summary>
    /// Joue une animation de "ronflement" sur l'image de l'IdleBar
    /// Phase 1: Inspiration (grossissement lent)
    /// Phase 2: Expiration avec vibration (rétrécissement + shake)
    /// </summary>
    private void PlayIdleSnoreAnimation()
    {
        if (manager.IdleBarImage == null || !isIdleAnimationActive) return;

        float inflateDuration = manager.IdleSnoreDuration * 0.7f;  // 70% du temps pour l'inspiration
        float deflateDuration = manager.IdleSnoreDuration * 0.3f;  // 30% du temps pour l'expiration + shake

        // Phase 1 : Inspiration (grossissement lent et profond)
        idleSnoreAnimationId = LeanTween.scale(manager.IdleBarImage.gameObject, idleBarImageOriginalScale * manager.IdleSnoreScale, inflateDuration)
            .setEase(manager.IdleInflateEase)  // easeInSine pour une inspiration progressive
            .setOnComplete(() =>
            {
                if (manager.IdleBarImage != null && isIdleAnimationActive)
                {
                    // Phase 2 : Expiration avec shake (ronflement!)
                    PlaySnoreDeflateWithShake(deflateDuration);
                }
            }).id;
    }

    /// <summary>
    /// Joue l'animation d'expiration avec vibration (la partie "ronflement")
    /// </summary>
    private void PlaySnoreDeflateWithShake(float duration)
    {
        if (manager.IdleBarImage == null || !isIdleAnimationActive) return;

        // Animation de rétrécissement avec bounce (comme un ronflement qui "expire")
        idleSnoreAnimationId = LeanTween.scale(manager.IdleBarImage.gameObject, idleBarImageOriginalScale, duration)
            .setEase(manager.IdleDeflateEase)  // easeOutBounce pour l'effet ronflement
            .setOnComplete(() =>
            {
                idleSnoreAnimationId = -1;
            }).id;

        // EN PARALLÈLE : Animation de shake/vibration pour simuler le ronflement
        PlaySnoreShakeEffect(duration);
    }

    /// <summary>
    /// Crée l'effet de vibration pendant l'expiration (simule le bruit du ronflement)
    /// VRAIES vibrations continues et rapides !
    /// </summary>
    private void PlaySnoreShakeEffect(float duration)
    {
        if (manager.IdleBarImage == null || !isIdleAnimationActive) return;

        // VRAIE vibration : mouvement rapide et continu en X (horizontal)
        // Vibration rapide de gauche à droite pendant toute la durée
        float vibrateFrequency = 15f; // Hz - très rapide pour effet vibration
        float totalCycles = duration * vibrateFrequency;

        idleShakeAnimationId = LeanTween.value(manager.IdleBarImage.gameObject, 0f, totalCycles * 2f * Mathf.PI, duration)
            .setOnUpdate((float value) =>
            {
                if (manager.IdleBarImage != null && isIdleAnimationActive)
                {
                    // Calculer l'intensité qui diminue progressivement
                    float progress = value / (totalCycles * 2f * Mathf.PI);
                    float currentIntensity = manager.IdleShakeIntensity * (1f - progress * 0.7f); // Diminue de 70%

                    // Position oscillante rapide (vibration)
                    float xOffset = Mathf.Sin(value) * currentIntensity;
                    Vector3 vibratePosition = idleBarImageOriginalPosition + new Vector3(xOffset, 0f, 0f);

                    manager.IdleBarImage.transform.localPosition = vibratePosition;
                }
            })
            .setOnComplete(() =>
            {
                idleShakeAnimationId = -1;
                // Remettre en position originale
                if (manager.IdleBarImage != null)
                {
                    manager.IdleBarImage.transform.localPosition = idleBarImageOriginalPosition;
                }
            }).id;
    }

    public void Cleanup()
    {
        // Arrêter l'animation de l'IdleBar
        StopIdleBarAnimation();

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

        if (manager.IdleBarImage != null)
        {
            LeanTween.cancel(manager.IdleBarImage.gameObject);
        }

        // Reset des IDs pour sécurité
        currentAnimationId = -1;
        currentPulseId = -1;
        currentPopId = -1;
        idleAnimationTimerId = -1;
        idleSnoreAnimationId = -1;
        idleShakeAnimationId = -1;
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
        // =====================================
        // EVENTBUS - Fini les managers !
        // =====================================

        // GameManager events → GameEvents
        EventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);

        // MapManager events → MapEvents  
        EventBus.Subscribe<LocationChangedEvent>(OnLocationChanged);
        EventBus.Subscribe<TravelProgressEvent>(OnTravelProgress);

        // ActivityManager events → ActivityEvents
        EventBus.Subscribe<ActivityProgressEvent>(OnActivityProgress);
        EventBus.Subscribe<ActivityStoppedEvent>(OnActivityStopped);
        EventBus.Subscribe<ActivityTickEvent>(OnActivityTick);

        Logger.LogInfo("AboveCanvasEventService: Subscribed to EventBus events", Logger.LogCategory.General);
    }

    public void UnsubscribeFromEvents()
    {
        // =====================================
        // EVENTBUS - Désabonnement simple
        // =====================================

        EventBus.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        EventBus.Unsubscribe<LocationChangedEvent>(OnLocationChanged);
        EventBus.Unsubscribe<TravelProgressEvent>(OnTravelProgress);
        EventBus.Unsubscribe<ActivityProgressEvent>(OnActivityProgress);
        EventBus.Unsubscribe<ActivityStoppedEvent>(OnActivityStopped);
        EventBus.Unsubscribe<ActivityTickEvent>(OnActivityTick);

        Logger.LogInfo("AboveCanvasEventService: Unsubscribed from EventBus events", Logger.LogCategory.General);
    }

    public void Cleanup()
    {
        // Désabonner de tous les événements
        UnsubscribeFromEvents();

        // Nettoyer les animations
        animationService?.Cleanup();
    }

    // =====================================
    // EVENT HANDLERS - Adaptés pour EventBus
    // =====================================

    private void OnGameStateChanged(GameStateChangedEvent eventData)
    {
        Logger.LogInfo($"AboveCanvasManager: Game state changed from {eventData.PreviousState} to {eventData.NewState}", Logger.LogCategory.General);
        displayService.RefreshDisplay();
    }

    private void OnLocationChanged(LocationChangedEvent eventData)
    {
        Logger.LogInfo($"AboveCanvasManager: Location changed from {eventData.PreviousLocation?.DisplayName ?? "None"} to {eventData.NewLocation?.DisplayName ?? "None"}", Logger.LogCategory.General);
        displayService.UpdateLocationDisplay();
    }

    private void OnTravelProgress(TravelProgressEvent eventData)
    {
        Logger.LogInfo($"AboveCanvasManager: Travel progress {eventData.CurrentSteps}/{eventData.RequiredSteps} to {eventData.DestinationLocationId}", Logger.LogCategory.General);
        displayService.UpdateTravelProgress(eventData.CurrentSteps, eventData.RequiredSteps);
    }

    private void OnActivityProgress(ActivityProgressEvent eventData)
    {
        Logger.LogInfo($"AboveCanvasManager: Activity progress {eventData.Activity?.ActivityId}/{eventData.Variant?.VariantName} ({eventData.ProgressPercentage:F1}%)", Logger.LogCategory.General);
        displayService.UpdateActivityProgress(eventData.Activity, eventData.Variant);
    }

    private void OnActivityStopped(ActivityStoppedEvent eventData)
    {
        Logger.LogInfo($"AboveCanvasManager: Activity stopped {eventData.Activity?.ActivityId}/{eventData.Variant?.VariantName} (Completed: {eventData.WasCompleted})", Logger.LogCategory.General);
        displayService.RefreshDisplay();
    }

    private void OnActivityTick(ActivityTickEvent eventData)
    {
        if (eventData.TicksCompleted > 0)
        {
            animationService?.ShakeRightIcon(); // Animation de satisfaction !
            Logger.LogInfo($"AboveCanvasManager: Activity tick completed - {eventData.TicksCompleted} ticks, {eventData.Rewards.Length} rewards", Logger.LogCategory.General);
        }
    }
}