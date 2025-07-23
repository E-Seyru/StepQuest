using UnityEngine;



// ===============================================
// SERVICE: Display Management
// ===============================================
public class AboveCanvasDisplayService
{
    private readonly AboveCanvasManager manager;
    private AboveCanvasAnimationService animationService;
    private bool isInitializing = true; // NOUVEAU : Flag pour eviter animations pendant init

    public AboveCanvasDisplayService(AboveCanvasManager manager)
    {
        this.manager = manager;
    }

    public void Initialize()
    {
        // Recuperer la reference au service d'animation
        animationService = manager.AnimationService;
    }

    // NOUVEAU : Methode pour marquer la fin de l'initialisation
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
        var dataManager = DataManager.Instance;

        // ⭐ NOUVEAU : Verifier si on est en voyage
        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            // Pendant le voyage : afficher "En voyage vers X" et icône de voyage
            ShowTravelingState(dataManager.PlayerData.TravelDestinationId);
        }
        else if (mapManager?.CurrentLocation != null)
        {
            // etat normal : afficher la location actuelle
            ShowCurrentLocationState(mapManager.CurrentLocation);
        }
        else
        {
            Logger.LogWarning("AboveCanvasManager: MapManager or CurrentLocation is null", Logger.LogCategory.General);
        }
    }

    private void ShowTravelingState(string destinationId)
    {
        // Texte "En voyage vers X"
        var destinationLocation = MapManager.Instance?.LocationRegistry?.GetLocationById(destinationId);
        string destinationName = destinationLocation?.DisplayName ?? destinationId;
        manager.CurrentLocationText.text = $"En voyage vers {destinationName}";

        // Icône de voyage
        UpdateLocationButtonForTravel();

        Logger.LogInfo($"AboveCanvasManager: Updated location display to traveling state towards {destinationName}", Logger.LogCategory.General);
    }
    // ⭐ NOUVEAU : Affiche l'etat "location actuelle"
    private void ShowCurrentLocationState(MapLocationDefinition location)
    {
        // Texte normal
        manager.CurrentLocationText.text = location.DisplayName;

        // Icône normale de la location
        UpdateLocationButtonForLocation(location);

        Logger.LogInfo($"AboveCanvasManager: Updated location display to {location.DisplayName}", Logger.LogCategory.General);
    }

    // ⭐ NOUVEAU : Configure le LocationButton pour l'etat voyage
    private void UpdateLocationButtonForTravel()
    {
        if (manager.LocationButtonIcon == null) return;

        if (manager.TravelIcon != null)
        {
            manager.LocationButtonIcon.sprite = manager.TravelIcon;
            manager.LocationButtonIcon.color = Color.white;
            Logger.LogInfo("AboveCanvasManager: Set LocationButton to travel icon", Logger.LogCategory.General);
        }
        else
        {
            // Pas d'icône de voyage definie, cacher l'icône
            manager.LocationButtonIcon.sprite = null;
            manager.LocationButtonIcon.color = Color.clear;
            Logger.LogWarning("AboveCanvasManager: No travel icon assigned, hiding LocationButton icon", Logger.LogCategory.General);
        }
    }

    // NOUVEAU : Methode pour mettre a jour l'icône du LocationButton
    private void UpdateLocationButtonForLocation(MapLocationDefinition location)
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
            // Image par defaut ou cacher l'icône si pas d'image
            manager.LocationButtonIcon.sprite = null;
            manager.LocationButtonIcon.color = Color.clear; // Ou utiliser une image par defaut
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
            HideIdleBar(); // NOUVEAU : Cacher IdleBar pendant activite
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

        // CORRECTION: Recuperer l'activite principale pour l'icône gauche
        var activityDefinition = activityManager.ActivityRegistry?.GetActivity(activity.ActivityId);

        // Configurer l'icône gauche avec l'ACTIVITe PRINCIPALE
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

        // NOUVEAU : Affichage du texte avec progression detaillee
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

            // NOUVEAU : Couleur differente pour les activites temporelles
            if (activity.IsTimeBased)
            {
                manager.FillBar.color = Color.Lerp(Color.cyan, Color.yellow, progressPercent);
            }

        }

        // Masquer la flèche pour les activites (la flèche sert seulement pour les voyages)
        if (manager.ArrowIcon != null)
        {
            manager.ArrowIcon.SetActive(false);
        }
    }

    // NOUVEAU : Methode pour formater l'affichage de progression d'activite
    private string FormatActivityProgress(ActivityData activity, ActivityVariant variant)
    {
        if (activity.IsTimeBased)
        {
            // Pour les activites temporelles : "5 min 30 s / 10 min"
            string currentTime = FormatTimeForProgress(activity.AccumulatedTimeMs);
            string totalTime = FormatTimeForProgress(activity.RequiredTimeMs);
            return $"{currentTime} / {totalTime}";
        }
        else
        {
            // Pour les activites a pas : "x / x"
            int currentSteps = (int)activity.AccumulatedSteps;
            int totalSteps = variant.ActionCost; // ActionCost = pas requis par tick
            return $"{currentSteps} / {totalSteps}";
        }
    }

    // NOUVEAU : Methode pour formater le temps de manière intelligente
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
        if (timeMs <= 0) return "Termine";

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

        // Icône de depart
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

        // Icône d'arrivee
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
    // NOUVELLES MeTHODES POUR IDLEBAR
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

        // NOUVEAU : Demarrer l'animation repetitive d'inactivite
        animationService?.StartIdleBarAnimation();
    }

    private void HideIdleBar()
    {
        // NOUVEAU : Arrêter l'animation repetitive d'inactivite
        animationService?.StopIdleBarAnimation();
        animationService?.HideBar(manager.IdleBar);
    }

    public void UpdateTravelProgress(int currentSteps, int requiredSteps)
    {
        if (manager.FillBar == null) return;

        float progressPercent = (float)currentSteps / requiredSteps;
        animationService?.AnimateProgressBar(progressPercent);

        // NOUVEAU : Mettre a jour le texte de progression pour les voyages
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

        // NOUVEAU : Mettre a jour le texte avec la progression detaillee
        if (manager.ActivityText != null)
        {
            string progressText = FormatActivityProgress(activity, variant);
            manager.ActivityText.text = progressText; // Juste la progression, sans le nom
        }
    }
}
