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

    [SerializeField] private Button mapButton;
    [SerializeField] private Button locationButton;
    [SerializeField] private Image locationButtonIcon; // NOUVEAU : Image du POI dans le LocationButton
    [SerializeField] private Image locationButtonBackground; // NOUVEAU : Background du LocationButton pour les effets
    [SerializeField] private Image locationButtonShadow; // NOUVEAU : Ombre du LocationButton (optionnel)
    [SerializeField] private Sprite travelIcon;

    [Header("UI References - Activity/Travel Bar")]
    [SerializeField] private GameObject activityBar;
    [SerializeField] private Button activityBarButton; // Button component for clicking on ActivityBar
    [SerializeField] private Image leftIcon;
    [SerializeField] private RectTransform leftIconContainer; // Parent container for left/origin icon (shown during travel)
    [SerializeField] private Image rightIcon;
    [SerializeField] private RectTransform activityIconContainer; // Parent container to animate for activity rewards
    [SerializeField] private TextMeshProUGUI activityText;
    [SerializeField] private Image backgroundBar;
    [SerializeField] private Image fillBar;
    [SerializeField] private GameObject arrowIcon;

    [Header("UI References - Idle Bar")]
    [SerializeField] private GameObject idleBar;
    [SerializeField] private Button idleBarButton; // Button component for clicking on IdleBar
    [SerializeField] private Image idleBarImage;  // Image de repos (sleeping) pour l'animation
    [SerializeField] private Image fightingBarImage;  // Image de combat (fighting)

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

    [Header("Icon Container Positions (Activity Mode)")]
    [SerializeField] private Vector2 rightContainerActivityPosition = new Vector2(0f, 0f); // Position during activity (centered)

    [Header("Travel Path Display")]
    [SerializeField] private RectTransform travelPathContainer; // Container with HorizontalLayoutGroup for travel path
    [SerializeField] private GameObject locationIconPrefab; // Prefab for location icons in path
    [SerializeField] private GameObject arrowPrefab; // Prefab for arrows between locations
    [SerializeField] private Sprite ellipsisSprite; // Sprite for "..." when path has 4+ locations
    [SerializeField] private Vector2 locationIconSize = new Vector2(50f, 50f); // Size of location icons in travel path
    [SerializeField] private Vector2 arrowSize = new Vector2(30f, 30f); // Size of arrows in travel path

    [Header("Pop Settings (Reward Animation)")]
    [SerializeField] private float popScaleAmount = 1.3f;        // Grossit de 30%
    [SerializeField] private float popDuration = 0.35f;         // Animation rapide mais visible
    [SerializeField] private Color popBrightColor = new Color(1.2f, 1.2f, 1.2f, 1f); // Plus lumineux
    [SerializeField] private LeanTweenType popEaseType = LeanTweenType.easeOutBack; // Effet bounce satisfaisant

    [Header("Idle Bar Animation Settings")]
    [SerializeField] private float idleAnimationInterval = 2.5f;    // Intervalle entre les ronflements (en secondes)
    [SerializeField] private float idleSnoreDuration = 1.2f;        // Duree d'un ronflement complet
    [SerializeField] private float idleSnoreScale = 1.25f;          // Facteur d'agrandissement (25% plus grand)
    [SerializeField] private float idleShakeIntensity = 5f;         // Intensite de la vibration (en pixels)
    [SerializeField] private LeanTweenType idleInflateEase = LeanTweenType.easeInSine;   // Animation d'inspiration
    [SerializeField] private LeanTweenType idleDeflateEase = LeanTweenType.easeOutBounce; // Animation d'expiration

    [Header("Combat Bar Animation Settings (Heartbeat)")]
    [SerializeField] private float combatHeartbeatInterval = 1.0f;  // Intervalle entre les battements (en secondes)
    [SerializeField] private float combatPulseScale = 1.15f;        // Facteur d'agrandissement du pulse (15% plus grand)
    [SerializeField] private float combatPulseDuration = 0.15f;     // Duree d'un pulse (rapide)
    [SerializeField] private float combatDoubleBeatDelay = 0.12f;   // Delai entre les deux battements

    [Header("LocationButton Settings")]
    [SerializeField] private float locationButtonClickScale = 0.95f;     // Facteur de retrecissement au clic (ex: 0.95 = 5% plus petit)
    [SerializeField] private float locationButtonClickDuration = 0.1f;   // Duree de l'animation de clic
    [SerializeField] private LeanTweenType locationButtonClickEase = LeanTweenType.easeOutQuart; // Type d'animation
    [SerializeField] private Vector2 shadowOffset = new Vector2(3f, -3f); // Decalage de l'ombre (x, y)
    [SerializeField] private Color shadowColor = new Color(0f, 0f, 0f, 0.3f); // Couleur de l'ombre

    // === INTERNAL SERVICES (NOUVEAU) ===
    private AboveCanvasInitializationService initializationService;
    private AboveCanvasDisplayService displayService;
    private AboveCanvasAnimationService animationService;
    private AboveCanvasEventService eventService;
    private AboveCanvasTravelPathService travelPathService;

    // Internal accessors for services
    internal AboveCanvasEventService EventService => eventService;
    internal AboveCanvasAnimationService AnimationService => animationService;
    internal AboveCanvasDisplayService DisplayService => displayService;
    internal AboveCanvasTravelPathService TravelPathService => travelPathService;

    public Sprite TravelIcon => travelIcon; // NOUVEAU : Accesseur pour l'icône de voyage

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
        // Brancher les evenements quand le GameObject devient actif
        // MAIS seulement si l'initialisation est terminee
        if (eventService != null && initializationService?.IsInitialized == true)
        {
            eventService.SubscribeToEvents();
        }
    }

    void OnDisable()
    {
        // Debrancher les evenements quand le GameObject devient inactif
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
        // Creer les services dans l'ordre (animation et display d'abord)
        animationService = new AboveCanvasAnimationService(this);
        travelPathService = new AboveCanvasTravelPathService(this);
        displayService = new AboveCanvasDisplayService(this);
        eventService = new AboveCanvasEventService(this, displayService, animationService);
        initializationService = new AboveCanvasInitializationService(this, eventService, animationService);

        // Initialiser les services dans l'ordre
        animationService.Initialize();
        displayService.Initialize(); // Maintenant displayService peut recuperer animationService
    }

    // === PUBLIC API - SAME AS BEFORE ===
    public void RefreshDisplay()
    {
        displayService.RefreshDisplay();
    }

    // === INTERNAL ACCESSORS FOR SERVICES ===
    public GameObject HeaderContainer => headerContainer;

    public Button MapButton => mapButton;
    public Button LocationButton => locationButton;
    public Image LocationButtonIcon => locationButtonIcon; // NOUVEAU : Accessor pour l'icône du LocationButton
    public Image LocationButtonBackground => locationButtonBackground; // NOUVEAU : Accessor pour le background
    public Image LocationButtonShadow => locationButtonShadow; // NOUVEAU : Accessor pour l'ombre
    public GameObject ActivityBar => activityBar;
    public Button ActivityBarButton => activityBarButton;
    public Image LeftIcon => leftIcon;
    public RectTransform LeftIconContainer => leftIconContainer;
    public Image RightIcon => rightIcon;
    public RectTransform ActivityIconContainer => activityIconContainer;
    public TextMeshProUGUI ActivityText => activityText;
    public Image BackgroundBar => backgroundBar;
    public Image FillBar => fillBar;
    public GameObject ArrowIcon => arrowIcon;

    // NOUVEAU : Accessor pour IdleBar
    public GameObject IdleBar => idleBar;
    public Button IdleBarButton => idleBarButton;
    public Image IdleBarImage => idleBarImage;  // Image de repos (sleeping)
    public Image FightingBarImage => fightingBarImage;  // Image de combat (fighting)

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

    // Icon Container Position Accessors
    public Vector2 RightContainerActivityPosition => rightContainerActivityPosition;

    // Travel Path Accessors
    public RectTransform TravelPathContainer => travelPathContainer;
    public GameObject LocationIconPrefab => locationIconPrefab;
    public GameObject ArrowPrefab => arrowPrefab;
    public Sprite EllipsisSprite => ellipsisSprite;
    public Vector2 LocationIconSize => locationIconSize;
    public Vector2 ArrowSize => arrowSize;
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

    // Combat Animation Settings Accessors (Heartbeat)
    public float CombatHeartbeatInterval => combatHeartbeatInterval;
    public float CombatPulseScale => combatPulseScale;
    public float CombatPulseDuration => combatPulseDuration;
    public float CombatDoubleBeatDelay => combatDoubleBeatDelay;

    // NOUVEAU : LocationButton Settings Accessors
    public float LocationButtonClickScale => locationButtonClickScale;
    public float LocationButtonClickDuration => locationButtonClickDuration;
    public LeanTweenType LocationButtonClickEase => locationButtonClickEase;
    public Vector2 ShadowOffset => shadowOffset;
    public Color ShadowColor => shadowColor;
}


