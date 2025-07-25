﻿// ===============================================
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
    [SerializeField] private Image leftIcon;
    [SerializeField] private Image rightIcon;
    [SerializeField] private TextMeshProUGUI activityText;
    [SerializeField] private Image backgroundBar;
    [SerializeField] private Image fillBar;
    [SerializeField] private GameObject arrowIcon;

    [Header("UI References - Idle Bar")]
    [SerializeField] private GameObject idleBar;
    [SerializeField] private Image idleBarImage;  // NOUVEAU : L'image a l'interieur de l'IdleBar pour l'animation

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
    [SerializeField] private float idleSnoreDuration = 1.2f;        // Duree d'un ronflement complet
    [SerializeField] private float idleSnoreScale = 1.25f;          // Facteur d'agrandissement (25% plus grand)
    [SerializeField] private float idleShakeIntensity = 5f;         // Intensite de la vibration (en pixels)
    [SerializeField] private LeanTweenType idleInflateEase = LeanTweenType.easeInSine;   // Animation d'inspiration
    [SerializeField] private LeanTweenType idleDeflateEase = LeanTweenType.easeOutBounce; // Animation d'expiration

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

    // Internal accessors for services
    internal AboveCanvasEventService EventService => eventService;
    internal AboveCanvasAnimationService AnimationService => animationService;
    internal AboveCanvasDisplayService DisplayService => displayService;

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


