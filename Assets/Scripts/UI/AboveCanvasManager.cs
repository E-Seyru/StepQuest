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
    [SerializeField] private Image leftIcon;          // POI d�part ou ic�ne activit�
    [SerializeField] private Image rightIcon;         // POI arriv�e (uniquement pour voyage)
    [SerializeField] private TextMeshProUGUI activityText;
    [SerializeField] private Image backgroundBar;     // Image de fond de la barre
    [SerializeField] private Image fillBar;           // Image de remplissage (fillAmount)
    [SerializeField] private GameObject arrowIcon;    // Fl�che entre les POIs

    [Header("UI References - Navigation Bar")]
    [SerializeField] private GameObject navigationBar;

    [Header("Settings")]
    [SerializeField] private bool hideNavigationOnMap = true;

    // R�f�rences aux managers
    private GameManager gameManager;
    private DataManager dataManager;
    private MapManager mapManager;
    private ActivityManager activityManager;
    private LocationRegistry locationRegistry;

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

        // R�cup�rer les r�f�rences
        gameManager = GameManager.Instance;
        dataManager = DataManager.Instance;
        mapManager = MapManager.Instance;
        activityManager = ActivityManager.Instance;
        locationRegistry = mapManager.LocationRegistry;

        // S'abonner aux �v�nements
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

        Logger.LogInfo("AboveCanvasManager: Initialized successfully", Logger.LogCategory.General);
    }

    private void SetupProgressBar()
    {
        // S'assurer que la fillBar est configur�e correctement
        if (fillBar != null)
        {
            // Configurer en mode Filled si ce n'est pas d�j� fait
            if (fillBar.type != Image.Type.Filled)
            {
                fillBar.type = Image.Type.Filled;
                fillBar.fillMethod = Image.FillMethod.Horizontal;
                Logger.LogInfo("AboveCanvasManager: Configured fillBar as Filled Horizontal", Logger.LogCategory.General);
            }

            // Commencer avec 0 progression
            fillBar.fillAmount = 0f;
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
        // �couter les changements d'�tat du jeu
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += OnGameStateChanged;
        }

        // �couter les �v�nements sp�cifiques pour les mises � jour
        if (mapManager != null)
        {
            mapManager.OnLocationChanged += OnLocationChanged;
            mapManager.OnTravelProgress += OnTravelProgress;
        }

        if (activityManager != null)
        {
            activityManager.OnActivityProgress += OnActivityProgress;
        }
    }

    // === GESTIONNAIRES D'�V�NEMENTS ===

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

    private void OnMapButtonClicked()
    {
        // G�rer l'affichage de la barre de navigation si besoin
        if (hideNavigationOnMap && navigationBar != null)
        {
            navigationBar.SetActive(false);
        }

        // Le reste sera g�r� par le PanelManager ou MapManager
        Logger.LogInfo("AboveCanvasManager: Map button clicked", Logger.LogCategory.General);
    }

    // === M�THODES DE MISE � JOUR ===

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

        switch (currentState)
        {
            case GameState.Traveling:
                SetupTravelDisplay();
                break;

            case GameState.DoingActivity:
                SetupActivityDisplay();
                break;

            case GameState.Idle:
            case GameState.Loading:
            case GameState.Paused:
            default:
                HideActivityBar();
                break;
        }
    }

    private void SetupTravelDisplay()
    {
        if (!dataManager.PlayerData.IsCurrentlyTraveling()) return;

        activityBar.SetActive(true);

        // R�cup�rer les infos de voyage
        string currentLocationId = dataManager.PlayerData.CurrentLocationId;
        string destinationId = dataManager.PlayerData.TravelDestinationId;

        var currentLocation = locationRegistry.GetLocationById(currentLocationId);
        var destinationLocation = locationRegistry.GetLocationById(destinationId);

        // Configurer les ic�nes
        if (leftIcon != null && currentLocation?.LocationIcon != null)
        {
            leftIcon.sprite = currentLocation.LocationIcon;
            leftIcon.gameObject.SetActive(true);
        }

        if (rightIcon != null && destinationLocation?.LocationIcon != null)
        {
            rightIcon.sprite = destinationLocation.LocationIcon;
            rightIcon.gameObject.SetActive(true);
        }

        // Afficher la fl�che
        if (arrowIcon != null)
        {
            arrowIcon.SetActive(true);
        }

        // Texte
        if (activityText != null)
        {
            activityText.text = $"Vers {destinationLocation?.DisplayName ?? destinationId}";
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

        // Configurer l'ic�ne de l'activit�
        if (leftIcon != null)
        {
            leftIcon.sprite = activityInfo.variant.GetIcon();
            leftIcon.gameObject.SetActive(true);
        }

        // Masquer l'ic�ne de droite et la fl�che (pas besoin pour une activit�)
        if (rightIcon != null)
        {
            rightIcon.gameObject.SetActive(false);
        }

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
        if (gameManager.CurrentState == GameState.Traveling)
        {
            UpdateProgressBar(currentSteps, requiredSteps);
        }
    }

    private void UpdateActivityProgress(ActivityData activity, ActivityVariant variant)
    {
        if (gameManager.CurrentState == GameState.DoingActivity && activity != null && variant != null)
        {
            float progress = activity.GetProgressToNextTick(variant);
            int currentSteps = activity.AccumulatedSteps;
            int requiredSteps = variant.ActionCost;

            UpdateProgressBar(currentSteps, requiredSteps);
        }
    }

    private void UpdateProgressBar(long current, long required)
    {
        if (fillBar == null || required <= 0) return;

        float progressValue = Mathf.Clamp01((float)current / required);
        fillBar.fillAmount = progressValue;


    }

    // === M�THODES PUBLIQUES ===

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
        // Se d�sabonner des �v�nements
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
        }
    }
}