﻿// Purpose: Clickable POI on the world map with intelligent pathfinding support
// Filepath: Assets/Scripts/Gameplay/World/POI.cs
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class POI : MonoBehaviour, IPointerClickHandler
{
    [Header("Location Settings")]
    [Tooltip("The ID that matches your MapLocationDefinition")]
    public string LocationID;

    [Header("Travel Path Settings")]
    [Tooltip("Custom starting point for travel path. If null, uses POI center.")]
    [SerializeField] private Transform travelPathStartPoint;
    [Tooltip("Visual representation of the travel start point in editor")]
    [SerializeField] private bool showTravelStartPoint = true;
    [SerializeField] private Color travelStartPointColor = Color.cyan;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Click Animation Settings")]
    [Tooltip("Enable click animation effect")]
    [SerializeField] private bool enableClickAnimation = true;
    [Tooltip("Scale multiplier for the click effect (1.0 = no change, 1.2 = 20% bigger)")]
    [SerializeField] private float clickScaleAmount = 1.2f;
    [Tooltip("Duration of the scale animation in seconds")]
    [SerializeField] private float clickAnimationDuration = 0.25f;
    [Tooltip("LeanTween ease type for the animation")]
    [SerializeField] private LeanTweenType clickAnimationEase = LeanTweenType.easeOutBack;

    [Header("Error Display")]
    [Tooltip("Le panel d'erreur sera géré automatiquement via ErrorPanel.Instance")]
    [SerializeField] private bool enableErrorMessages = true;

    [Header("Location Details")]
    [Tooltip("Nom du panel à afficher pour les détails de la location")]
    [SerializeField] private string locationDetailsPanelName = "LocationDetailsPanel";

    [Header("Pathfinding Settings - NOUVEAU")]
    [Tooltip("Afficher des informations détaillées sur le pathfinding")]
    [SerializeField] private bool showPathfindingDetails = true;
    [Tooltip("Afficher le nombre de segments dans le message de voyage")]
    [SerializeField] private bool showSegmentCount = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Internal state
    private MapManager mapManager;
    private DataManager dataManager;
    private LocationRegistry locationRegistry;
    private TravelConfirmationPopup travelPopup;
    private PanelManager panelManager;

    // Variables pour l'animation
    private Vector3 originalScale;
    private bool isAnimating = false;

    /// <summary>
    /// Retourne la position de départ pour le chemin de voyage
    /// </summary>
    public Vector3 GetTravelPathStartPosition()
    {
        if (travelPathStartPoint != null)
        {
            return travelPathStartPoint.position;
        }
        return transform.position;
    }

    void Start()
    {
        // Sauvegarder l'échelle originale pour l'animation
        originalScale = transform.localScale;

        // Initialiser les références
        mapManager = MapManager.Instance;
        dataManager = DataManager.Instance;
        panelManager = PanelManager.Instance;

        if (mapManager != null)
        {
            locationRegistry = mapManager.LocationRegistry;
        }

        // Trouver le TravelConfirmationPopup dans la scène
        travelPopup = FindObjectOfType<TravelConfirmationPopup>();

        // Valider les références critiques
        if (mapManager == null)
        {
            Logger.LogError($"POI ({LocationID}): MapManager.Instance is null!", Logger.LogCategory.MapLog);
            return;
        }

        if (dataManager == null)
        {
            Logger.LogError($"POI ({LocationID}): DataManager.Instance is null!", Logger.LogCategory.MapLog);
            return;
        }

        if (locationRegistry == null)
        {
            Logger.LogError($"POI ({LocationID}): LocationRegistry is null in MapManager!", Logger.LogCategory.MapLog);
            return;
        }

        if (enableDebugLogs)
        {
            Vector3 startPos = GetTravelPathStartPosition();
            Logger.LogInfo($"POI: Initialized POI for location '{LocationID}' at position ({transform.position.x}, {transform.position.y}). Travel start point: ({startPos.x}, {startPos.y})", Logger.LogCategory.MapLog);
        }
    }

    void OnDestroy()
    {
        // Arrêter toutes les animations LeanTween sur cet objet
        LeanTween.cancel(gameObject);
    }

    // Mobile-optimized click handling
    public void OnPointerClick(PointerEventData eventData)
    {
        HandleClick();
    }

    // Alternative for UI-based interaction
    public void OnPOIClicked()
    {
        HandleClick();
    }

    /// <summary>
    /// Lance l'animation de clic (effet punch scale)
    /// </summary>
    private void PlayClickAnimation()
    {
        // Ne pas jouer l'animation si elle est désactivée ou si une animation est déjà en cours
        if (!enableClickAnimation || isAnimating)
            return;

        isAnimating = true;

        // Annuler toute animation en cours sur cet objet
        LeanTween.cancel(gameObject);

        // Animation en 2 étapes :
        // 1. Grandir rapidement
        // 2. Revenir à la taille normale avec un effet bounce
        LeanTween.scale(gameObject, originalScale * clickScaleAmount, clickAnimationDuration * 0.4f)
            .setEase(LeanTweenType.easeOutQuart)
            .setOnComplete(() =>
            {
                LeanTween.scale(gameObject, originalScale, clickAnimationDuration * 0.6f)
                    .setEase(clickAnimationEase)
                    .setOnComplete(() =>
                    {
                        isAnimating = false;
                    });
            });
    }

    /// <summary>
    /// ENHANCED: Gère le clic sur le POI avec support pathfinding et CurrentLocation null
    /// </summary>
    private void HandleClick()
    {
        PlayClickAnimation();

        if (mapManager == null || locationRegistry == null)
        {
            Logger.LogError($"POI ({LocationID}): Missing required components for travel", Logger.LogCategory.MapLog);
            return;
        }

        // ⭐ NOUVEAU : Vérifier si on est en voyage AVANT toute autre vérification
        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            // Pendant le voyage, bloquer l'accès aux détails de TOUTES les locations
            string destinationName = dataManager.PlayerData.TravelDestinationId;
            var destinationLocation = locationRegistry.GetLocationById(destinationName);
            if (destinationLocation != null)
            {
                destinationName = destinationLocation.DisplayName;
            }

            if (enableErrorMessages && ErrorPanel.Instance != null)
            {
                ErrorPanel.Instance.ShowError($"Vous êtes en voyage vers {destinationName}. Attendez d'arriver à destination.");
            }

            Logger.LogInfo($"POI ({LocationID}): Click blocked - currently traveling to {destinationName}", Logger.LogCategory.MapLog);
            return;
        }

        // ⭐ NOUVEAU : Vérifier si CurrentLocation est null (normalement ça n'arrive plus maintenant qu'on vérifie IsCurrentlyTraveling() avant)
        if (mapManager.CurrentLocation == null)
        {
            if (enableErrorMessages && ErrorPanel.Instance != null)
            {
                ErrorPanel.Instance.ShowError("Position actuelle inconnue. Impossible d'accéder aux détails.");
            }

            Logger.LogWarning($"POI ({LocationID}): Click blocked - CurrentLocation is null", Logger.LogCategory.MapLog);
            return;
        }

        // Vérifier si on est déjà à cette location
        if (mapManager.CurrentLocation.LocationID == LocationID)
        {
            if (enableDebugLogs)
            {
                Logger.LogInfo($"POI ({LocationID}): Already at this location, showing details", Logger.LogCategory.MapLog);
            }

            ShowLocationDetails();
            return;
        }

        // ENHANCED: Vérifier si le voyage est possible (direct ou pathfinding)
        if (mapManager.CanTravelTo(LocationID))
        {
            if (travelPopup != null)
            {
                if (enableDebugLogs)
                {
                    Logger.LogInfo($"POI ({LocationID}): Opening travel confirmation popup", Logger.LogCategory.MapLog);
                }

                // NOUVEAU: Passer des informations de pathfinding au popup si disponible
                if (showPathfindingDetails)
                {
                    var travelInfo = mapManager.GetTravelInfo(LocationID);
                    ShowEnhancedTravelConfirmation(travelInfo);
                }
                else
                {
                    travelPopup.ShowTravelConfirmation(LocationID);
                }
            }
            else
            {
                Logger.LogWarning($"POI ({LocationID}): TravelConfirmationPopup not available, starting travel directly", Logger.LogCategory.MapLog);
                mapManager.StartTravel(LocationID);
            }
        }
        else
        {
            ShowTravelUnavailableMessage();
        }
    }

    /// <summary>
    /// NOUVEAU: Affiche une popup de confirmation avec détails pathfinding
    /// </summary>
    private void ShowEnhancedTravelConfirmation(MapManager.TravelInfo travelInfo)
    {
        if (travelInfo == null)
        {
            travelPopup.ShowTravelConfirmation(LocationID);
            return;
        }

        // Si TravelConfirmationPopup a des méthodes pour afficher des détails étendus, les utiliser
        // Sinon, utiliser la méthode standard
        travelPopup.ShowTravelConfirmation(LocationID);

        // TODO: Si tu veux modifier TravelConfirmationPopup pour supporter les détails pathfinding,
        // tu peux ajouter quelque chose comme :
        // travelPopup.ShowTravelConfirmationWithDetails(LocationID, travelInfo);
    }

    /// <summary>
    /// ENHANCED: Affiche un message d'erreur plus informatif
    /// </summary>
    private void ShowTravelUnavailableMessage()
    {
        var destinationLocation = locationRegistry.GetLocationById(LocationID);
        string errorMessage = "Impossible de voyager !";

        if (mapManager.CurrentLocation == null)
        {
            errorMessage = "Impossible - aucune location définie !";
        }
        else if (dataManager.PlayerData.IsCurrentlyTraveling())
        {
            errorMessage = "Impossible - vous êtes déjà en train de voyager !";
        }
        else if (ActivityManager.Instance?.HasActiveActivity() == true)
        {
            errorMessage = "Impossible - vous êtes en activité !";
        }
        else if (destinationLocation == null)
        {
            errorMessage = $"Impossible - destination '{LocationID}' introuvable !";
        }
        else if (mapManager.CurrentLocation.LocationID == LocationID)
        {
            errorMessage = $"Impossible - déjà à '{destinationLocation.DisplayName}' !";
        }
        else
        {
            // ENHANCED: Message plus détaillé basé sur le pathfinding
            string detailedReason = GetDetailedTravelBlockReason(destinationLocation);
            errorMessage = $"Impossible de voyager vers '{destinationLocation.DisplayName}' - {detailedReason}";
        }

        if (enableErrorMessages && ErrorPanel.Instance != null)
        {
            ErrorPanel.Instance.ShowError(errorMessage);
        }
        else
        {
            Logger.LogWarning($"POI ({LocationID}): {errorMessage}", Logger.LogCategory.MapLog);
        }
    }

    /// <summary>
    /// NOUVEAU: Retourne une raison détaillée pour l'impossibilité de voyager
    /// </summary>
    private string GetDetailedTravelBlockReason(MapLocationDefinition destination)
    {
        // Vérifier d'abord la connexion directe
        bool hasDirectConnection = locationRegistry.CanTravelBetween(mapManager.CurrentLocation.LocationID, LocationID);

        if (hasDirectConnection)
        {
            // Il y a une connexion directe mais autre problème
            return "problème technique";
        }

        // Vérifier le pathfinding si disponible
        if (mapManager.PathfindingService != null)
        {
            var pathResult = mapManager.PathfindingService.FindPath(mapManager.CurrentLocation.LocationID, LocationID);

            if (!pathResult.IsReachable)
            {
                return "aucun chemin disponible";
            }
            else
            {
                // Le chemin existe mais autre problème
                return "chemin trouvé mais voyage bloqué";
            }
        }

        return "pas de connexion directe";
    }

    /// <summary>
    /// Affiche les détails de la location
    /// </summary>
    private void ShowLocationDetails()
    {
        if (panelManager != null && !string.IsNullOrEmpty(locationDetailsPanelName))
        {
            // Utiliser l'API correcte du PanelManager pour naviguer vers le panel
            panelManager.HideMapAndGoToPanel(locationDetailsPanelName);

            // Le LocationDetailsPanel se mettra automatiquement à jour avec la location actuelle
            Logger.LogInfo($"POI ({LocationID}): Navigating to {locationDetailsPanelName}", Logger.LogCategory.MapLog);
        }
        else
        {
            Logger.LogWarning($"POI ({LocationID}): Cannot show location details - PanelManager or panel name not set", Logger.LogCategory.MapLog);
        }
    }

    void Awake()
    {
        // Validation des paramètres au démarrage
        if (string.IsNullOrEmpty(LocationID))
        {
            Logger.LogError($"POI: LocationID is not set on GameObject '{gameObject.name}'", Logger.LogCategory.MapLog);
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Validation des paramètres d'animation
        if (clickScaleAmount < 1.0f)
        {
            Debug.LogWarning($"POI ({LocationID}): clickScaleAmount should be >= 1.0 for a growing effect");
        }

        if (clickAnimationDuration <= 0f)
        {
            Debug.LogWarning($"POI ({LocationID}): clickAnimationDuration should be > 0");
        }

        if (string.IsNullOrEmpty(locationDetailsPanelName))
        {
            Debug.LogWarning($"POI ({LocationID}): locationDetailsPanelName is not set!");
        }
    }

    // Debug visualization in Scene view
    void OnDrawGizmosSelected()
    {
        // POI Gizmo - couleur simple
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);

        // Travel start point gizmo
        if (showTravelStartPoint)
        {
            Vector3 startPos = GetTravelPathStartPosition();
            Gizmos.color = travelStartPointColor;
            Gizmos.DrawWireSphere(startPos, 0.2f);
            Gizmos.DrawLine(transform.position, startPos);
        }
    }

    #region Debug Methods

    /// <summary>
    /// NOUVEAU: Debug le chemin vers cette destination
    /// </summary>
    [ContextMenu("Debug Path To This POI")]
    public void DebugPathToThisPOI()
    {
        if (mapManager != null && mapManager.CurrentLocation != null)
        {
            mapManager.DebugPath(LocationID);
        }
        else
        {
            Logger.LogWarning($"POI ({LocationID}): Cannot debug path - MapManager or CurrentLocation not available", Logger.LogCategory.MapLog);
        }
    }

    /// <summary>
    /// NOUVEAU: Affiche les informations de voyage dans la console
    /// </summary>
    [ContextMenu("Show Travel Info")]
    public void ShowTravelInfo()
    {
        if (mapManager?.CurrentLocation == null)
        {
            Logger.LogInfo($"POI ({LocationID}): No current location set", Logger.LogCategory.MapLog);
            return;
        }

        var travelInfo = mapManager.GetTravelInfo(LocationID);
        if (travelInfo == null)
        {
            Logger.LogInfo($"POI ({LocationID}): No travel info available", Logger.LogCategory.MapLog);
            return;
        }

        string travelType = travelInfo.RequiresPathfinding ? "PATHFINDING" : "DIRECT";
        string segmentInfo = travelInfo.RequiresPathfinding ? $" ({travelInfo.SegmentCount} segments)" : "";

        Logger.LogInfo($"POI ({LocationID}): {travelType} travel{segmentInfo} to {travelInfo.To?.DisplayName} - " +
                      $"{travelInfo.StepCost} steps - Can travel: {travelInfo.CanTravel}", Logger.LogCategory.MapLog);

        if (travelInfo.PathDetails != null && enableDebugLogs)
        {
            Logger.LogInfo($"POI ({LocationID}): Path details - Reachable: {travelInfo.PathDetails.IsReachable}, " +
                          $"Total cost: {travelInfo.PathDetails.TotalCost}, Segments: {travelInfo.PathDetails.Segments?.Count ?? 0}", Logger.LogCategory.MapLog);
        }
    }

    #endregion
}