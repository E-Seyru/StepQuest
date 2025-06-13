// Purpose: Clickable POI on the world map that shows travel confirmation popup
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
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color unavailableColor = Color.gray;

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

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Internal state
    private MapManager mapManager;
    private DataManager dataManager;
    private LocationRegistry locationRegistry;
    private TravelConfirmationPopup travelPopup;

    private bool isCurrentLocation = false;
    private bool canTravelHere = false;

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

    /// <summary>
    /// Retourne la position actuelle du point de départ (pour vérification dans l'éditeur)
    /// </summary>
    public Vector3 GetTravelPathStartPoint()
    {
        return GetTravelPathStartPosition();
    }

    /// <summary>
    /// Définit un nouveau point de départ pour le chemin de voyage
    /// </summary>
    public void SetTravelPathStartPoint(Transform newStartPoint)
    {
        travelPathStartPoint = newStartPoint;
    }

    void Start()
    {
        // Sauvegarder l'échelle originale
        originalScale = transform.localScale;

        // Get MapManager reference
        mapManager = MapManager.Instance;
        dataManager = DataManager.Instance;

        if (mapManager == null)
        {
            Logger.LogError($"POI ({LocationID}): MapManager not found!", Logger.LogCategory.MapLog);
            return;
        }

        if (dataManager == null)
        {
            Logger.LogError($"POI ({LocationID}): DataManager not found!", Logger.LogCategory.MapLog);
            return;
        }

        locationRegistry = mapManager.LocationRegistry;
        if (locationRegistry == null)
        {
            Logger.LogError($"POI ({LocationID}): LocationRegistry not found via MapManager!", Logger.LogCategory.MapLog);
            return;
        }

        travelPopup = TravelConfirmationPopup.Instance;
        if (travelPopup == null)
        {
            Logger.LogWarning($"POI ({LocationID}): TravelConfirmationPopup not found! POI will use direct travel instead.", Logger.LogCategory.MapLog);
        }

        // Get SpriteRenderer if not assigned
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Logger.LogError($"POI ({LocationID}): SpriteRenderer component not found!", Logger.LogCategory.MapLog);
                return;
            }
        }

        // Subscribe to MapManager events
        mapManager.OnLocationChanged += OnPlayerLocationChanged;
        mapManager.OnTravelStarted += OnTravelStarted;
        mapManager.OnTravelCompleted += OnTravelCompleted;

        // Set initial state
        UpdateVisualState();

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

        // Unsubscribe from events
        if (mapManager != null)
        {
            mapManager.OnLocationChanged -= OnPlayerLocationChanged;
            mapManager.OnTravelStarted -= OnTravelStarted;
            mapManager.OnTravelCompleted -= OnTravelCompleted;
        }
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
        // 2. Revenir à la taille normale

        // Étape 1 : Grandir (la moitié du temps total)
        LeanTween.scale(gameObject, originalScale * clickScaleAmount, clickAnimationDuration * 0.5f)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnComplete(() =>
            {
                // Étape 2 : Rétrécir vers la taille normale (l'autre moitié du temps)
                LeanTween.scale(gameObject, originalScale, clickAnimationDuration * 0.5f)
                    .setEase(clickAnimationEase)
                    .setOnComplete(() =>
                    {
                        isAnimating = false;
                    });
            });

        if (enableDebugLogs)
        {
            Logger.LogInfo($"POI ({LocationID}): Playing click animation", Logger.LogCategory.MapLog);
        }
    }

    private void HandleClick()
    {
        // Jouer l'effet d'animation en premier
        PlayClickAnimation();

        if (mapManager == null)
        {
            Logger.LogError($"POI ({LocationID}): MapManager is null!", Logger.LogCategory.MapLog);
            return;
        }

        if (string.IsNullOrEmpty(LocationID))
        {
            Logger.LogError($"POI: LocationID is not set!", Logger.LogCategory.MapLog);
            return;
        }

        if (enableDebugLogs)
        {
            Logger.LogInfo($"POI: Clicked on POI '{LocationID}'", Logger.LogCategory.MapLog);
        }

        // Check if this is the current location (when not traveling)
        if (!dataManager.PlayerData.IsCurrentlyTraveling() && isCurrentLocation)
        {
            if (enableDebugLogs)
            {
                Logger.LogInfo($"POI ({LocationID}): Already at this location!", Logger.LogCategory.MapLog);
            }
            ShowLocationDetails();
            return;
        }

        // Vérifier si le voyage est possible avant d'ouvrir la popup
        if (mapManager.CanTravelTo(LocationID))
        {
            if (travelPopup != null)
            {
                if (enableDebugLogs)
                {
                    Logger.LogInfo($"POI ({LocationID}): Opening travel confirmation popup", Logger.LogCategory.MapLog);
                }
                travelPopup.ShowTravelConfirmation(LocationID);
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
    /// Affiche un panel d'erreur quand le voyage n'est pas possible
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
        else if (!locationRegistry.CanTravelBetween(mapManager.CurrentLocation.LocationID, LocationID))
        {
            errorMessage = $"Impossible - pas connecté à '{destinationLocation.DisplayName}' !";
        }

        // Afficher le panel d'erreur
        ShowErrorPanel(errorMessage);

        // Garder le log pour debug
        Logger.LogInfo($"POI ({LocationID}): {errorMessage}", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// Affiche le panel d'erreur via le singleton
    /// </summary>
    private void ShowErrorPanel(string message)
    {
        if (!enableErrorMessages)
        {
            return;
        }

        if (ErrorPanel.Instance != null)
        {
            ErrorPanel.Instance.ShowError(message, transform);

            if (enableDebugLogs)
            {
                Logger.LogInfo($"POI ({LocationID}): Showing error panel with message: {message}", Logger.LogCategory.MapLog);
            }
        }
        else
        {
            Logger.LogWarning($"POI ({LocationID}): ErrorPanel.Instance not found!", Logger.LogCategory.MapLog);
        }
    }

    /// <summary>
    /// Show location details popup when clicking on current location
    /// </summary>
    private void ShowLocationDetails()
    {
        // Implementation would open location details panel
        if (enableDebugLogs)
        {
            Logger.LogInfo($"POI ({LocationID}): Showing location details", Logger.LogCategory.MapLog);
        }

        // You could trigger a location details panel here if you have one
        // LocationDetailsPanel.Instance?.ShowLocation(LocationID);
    }

    private void UpdateVisualState()
    {
        if (spriteRenderer == null || mapManager?.CurrentLocation == null) return;

        // Check if this POI represents the current location
        isCurrentLocation = (mapManager.CurrentLocation.LocationID == LocationID);

        // Check if we can travel here
        canTravelHere = mapManager.CanTravelTo(LocationID);

        // Set color based on state
        if (isCurrentLocation && !dataManager.PlayerData.IsCurrentlyTraveling())
        {
            spriteRenderer.color = highlightColor; // Highlight current location
        }
        else if (canTravelHere)
        {
            spriteRenderer.color = normalColor; // Available for travel
        }
        else
        {
            spriteRenderer.color = unavailableColor; // Cannot travel here
        }
    }

    private void OnPlayerLocationChanged(MapLocationDefinition newLocation)
    {
        if (enableDebugLogs && newLocation?.LocationID == LocationID)
        {
            Logger.LogInfo($"POI ({LocationID}): Player ARRIVED at this location!", Logger.LogCategory.MapLog);
        }
        UpdateVisualState();
    }

    private void OnTravelStarted(string destinationId)
    {
        if (enableDebugLogs && destinationId == LocationID)
        {
            Logger.LogInfo($"POI ({LocationID}): Travel started TOWARD this location!", Logger.LogCategory.MapLog);
        }
        UpdateVisualState();
    }

    private void OnTravelCompleted(string arrivedLocationId)
    {
        if (enableDebugLogs && arrivedLocationId == LocationID)
        {
            Logger.LogInfo($"POI ({LocationID}): Player ARRIVED at this location!", Logger.LogCategory.MapLog);
        }
        UpdateVisualState();
    }

    // Visual feedback for mouse hover (optional)
    void OnMouseEnter()
    {
        if (spriteRenderer != null && !isCurrentLocation && !dataManager.PlayerData.IsCurrentlyTraveling())
        {
            if (spriteRenderer.color == normalColor)
            {
                Color currentColor = spriteRenderer.color;
                spriteRenderer.color = new Color(currentColor.r * 1.2f, currentColor.g * 1.2f, currentColor.b * 1.2f, currentColor.a);
            }
        }
    }

    void OnMouseExit()
    {
        UpdateVisualState();
    }

    // Editor utility to help set up POIs
    void OnValidate()
    {
        if (string.IsNullOrEmpty(LocationID))
        {
            Debug.LogWarning($"POI on GameObject '{gameObject.name}': LocationID is not set!");
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
    }

    // Debug visualization in Scene view
    void OnDrawGizmosSelected()
    {
        // POI Gizmo
        Gizmos.color = (spriteRenderer != null && spriteRenderer.color == normalColor) ?
                      Color.green : Color.red;
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
}