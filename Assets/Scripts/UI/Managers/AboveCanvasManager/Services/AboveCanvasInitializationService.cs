
// ===============================================
// SERVICE: Initialization Management
// ===============================================
using UnityEngine;

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

        // Abonner aux evenements immediatement (comme avant)
        eventService?.SubscribeToEvents();

        // Premiere mise a jour de l'affichage
        manager.RefreshDisplay();

        // RESTAURATION: Verification retardee pour s'assurer que l'affichage est correct
        // (C'etait dans l'ancien code et ça resolvait les problemes d'ordre d'initialisation)
        manager.StartCoroutine(DelayedDisplayRefresh());

        isInitialized = true;
        Logger.LogInfo("AboveCanvasManager: Initialized successfully", Logger.LogCategory.General);
    }

    private System.Collections.IEnumerator DelayedDisplayRefresh()
    {
        // Attendre quelques frames pour que tous les managers soient completement initialises
        yield return new WaitForSeconds(1f);

        // Forcer une mise a jour de l'affichage
        manager.RefreshDisplay();

        // NOUVEAU : Marquer la fin de l'initialisation pour activer les animations
        manager.DisplayService.FinishInitialization();

        Logger.LogInfo("AboveCanvasManager: Delayed display refresh completed", Logger.LogCategory.General);
    }

    private System.Collections.IEnumerator WaitForCriticalManagers()
    {
        // Version optimisee : WaitUntil() arrete immediatement quand la condition est remplie
        // → evite de boucler toutes les 0.1s ; tu gagnes quelques ms au lancement
        yield return new WaitUntil(() => DataManager.Instance != null && MapManager.Instance != null);

        // Attendre un frame supplementaire pour la stabilite
        yield return null;
    }

    private void SetupUI()
    {
        SetupMapButton();
        SetupLocationButton(); // NOUVEAU: Ajouter cette ligne
        SetupActivityBarButton(); // Setup click handler for ActivityBar
        SetupIdleBarButton(); // Setup click handler for IdleBar (combat mode)
        SetupProgressBar();
    }

    private void SetupMapButton()
    {
        if (manager.MapButton != null)
        {
            manager.MapButton.onClick.AddListener(OnMapButtonClicked);
        }
    }

    // NOUVEAU: Methode pour configurer le LocationButton
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

    // NOUVEAU : Methode pour initialiser l'ombre du LocationButton
    private void InitializeLocationButtonShadow()
    {
        if (manager.LocationButtonShadow == null) return;

        // Configuration de l'ombre
        manager.LocationButtonShadow.color = manager.ShadowColor;

        // Positionner l'ombre avec le decalage
        RectTransform shadowRect = manager.LocationButtonShadow.GetComponent<RectTransform>();
        if (shadowRect != null)
        {
            shadowRect.anchoredPosition = manager.ShadowOffset;
        }

        Logger.LogInfo("AboveCanvasManager: LocationButton shadow configured", Logger.LogCategory.General);
    }

    // NOUVEAU : Methode pour initialiser l'icône du LocationButton
    private void InitializeLocationButtonIcon()
    {
        if (manager.LocationButtonIcon == null)
        {
            Logger.LogWarning("AboveCanvasManager: LocationButtonIcon is null - make sure to assign it in the inspector", Logger.LogCategory.General);
            return;
        }

        // Configuration initiale de l'image
        manager.LocationButtonIcon.preserveAspect = true; // S'assurer que Preserve Aspect est active

        // L'icône sera mise a jour quand UpdateLocationDisplay() sera appele
        Logger.LogInfo("AboveCanvasManager: LocationButtonIcon initialized", Logger.LogCategory.General);
    }

    private void SetupActivityBarButton()
    {
        if (manager.ActivityBarButton != null)
        {
            manager.ActivityBarButton.onClick.AddListener(OnActivityBarClicked);
            Logger.LogInfo("AboveCanvasManager: ActivityBarButton configured", Logger.LogCategory.General);
        }
        else
        {
            Logger.LogWarning("AboveCanvasManager: ActivityBarButton is null - assign a Button component to ActivityBar for click handling", Logger.LogCategory.General);
        }
    }

    private void OnActivityBarClicked()
    {
        // Check if currently traveling - block access during travel
        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            string destinationName = dataManager.PlayerData.TravelDestinationId;
            var destinationLocation = MapManager.Instance?.LocationRegistry?.GetLocationById(destinationName);
            if (destinationLocation != null)
            {
                destinationName = destinationLocation.DisplayName;
            }

            if (ErrorPanel.Instance != null)
            {
                ErrorPanel.Instance.ShowError($"Vous etes en voyage vers {destinationName}. Attendez d'arriver a destination.");
            }

            Logger.LogInfo("AboveCanvasManager: ActivityBar clicked during travel - access blocked", Logger.LogCategory.General);
            return;
        }

        // Check that PanelManager is available
        if (PanelManager.Instance == null)
        {
            Logger.LogWarning("AboveCanvasManager: PanelManager.Instance is null", Logger.LogCategory.General);
            return;
        }

        // Navigate to LocationDetailsPanel (ActivityDisplayPanel will show automatically if activity is active)
        PanelManager.Instance.HideMapAndGoToPanel("LocationDetailsPanel");

        Logger.LogInfo("AboveCanvasManager: ActivityBar clicked - navigating to LocationDetailsPanel", Logger.LogCategory.General);
    }

    private void SetupIdleBarButton()
    {
        if (manager.IdleBarButton != null)
        {
            manager.IdleBarButton.onClick.AddListener(OnIdleBarClicked);
            Logger.LogInfo("AboveCanvasManager: IdleBarButton configured", Logger.LogCategory.General);
        }
        else
        {
            Logger.LogWarning("AboveCanvasManager: IdleBarButton is null - assign a Button component to IdleBar for click handling", Logger.LogCategory.General);
        }
    }

    private void OnIdleBarClicked()
    {
        var gameManager = GameManager.Instance;

        // Check if we're in combat - open CombatPanel
        if (gameManager?.CurrentState == GameState.InCombat)
        {
            if (CombatPanelUI.Instance != null)
            {
                CombatPanelUI.Instance.ShowActiveCombat();
                Logger.LogInfo("AboveCanvasManager: IdleBar clicked during combat - opening CombatPanel", Logger.LogCategory.General);
            }
            else
            {
                Logger.LogWarning("AboveCanvasManager: CombatPanelUI.Instance is null", Logger.LogCategory.General);
            }
            return;
        }

        // If not in combat (idle state), do nothing or optionally navigate somewhere
        Logger.LogInfo("AboveCanvasManager: IdleBar clicked in idle state - no action", Logger.LogCategory.General);
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
        // ⭐ NOUVEAU : Verifier si on est en voyage AVANT tout le reste
        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            // Pendant le voyage : jouer effet de clic mais ne pas naviguer
            PlayLocationButtonClickEffect();

            // Optionnel : afficher un message d'information
            string destinationName = dataManager.PlayerData.TravelDestinationId;
            var destinationLocation = MapManager.Instance?.LocationRegistry?.GetLocationById(destinationName);
            if (destinationLocation != null)
            {
                destinationName = destinationLocation.DisplayName;
            }

            // Utiliser ErrorPanel pour afficher le message
            if (ErrorPanel.Instance != null)
            {
                ErrorPanel.Instance.ShowError($"Vous etes en voyage vers {destinationName}. Attendez d'arriver a destination.");
            }

            Logger.LogInfo("AboveCanvasManager: LocationButton clicked during travel - access blocked", Logger.LogCategory.General);
            return;
        }

        // etat normal : jouer l'effet de clic
        PlayLocationButtonClickEffect();

        // Verifier que PanelManager est disponible
        if (PanelManager.Instance == null)
        {
            Logger.LogWarning("AboveCanvasManager: PanelManager.Instance is null", Logger.LogCategory.General);
            return;
        }

        // Naviguer vers le LocationDetailsPanel
        PanelManager.Instance.HideMapAndGoToPanel("LocationDetailsPanel");

        Logger.LogInfo("AboveCanvasManager: Navigating to LocationDetailsPanel", Logger.LogCategory.General);
    }

    // NOUVEAU : Methode pour l'effet de clic du LocationButton
    private void PlayLocationButtonClickEffect()
    {
        if (manager.LocationButton == null) return;

        // Annuler toute animation en cours sur le bouton
        LeanTween.cancel(manager.LocationButton.gameObject);

        // Effet de "squeeze" sur tout le bouton : retrecissement rapide puis retour a la normale
        LeanTween.scale(manager.LocationButton.gameObject, Vector3.one * manager.LocationButtonClickScale, manager.LocationButtonClickDuration)
            .setEase(manager.LocationButtonClickEase)
            .setOnComplete(() =>
            {
                // Retour a la taille normale
                LeanTween.scale(manager.LocationButton.gameObject, Vector3.one, manager.LocationButtonClickDuration)
                    .setEase(manager.LocationButtonClickEase);
            });

        Logger.LogInfo("AboveCanvasManager: LocationButton click effect triggered", Logger.LogCategory.General);
    }

    public bool IsInitialized => isInitialized;
}