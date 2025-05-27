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

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Internal state
    private MapManager mapManager;
    private DataManager dataManager;
    private LocationRegistry locationRegistry;
    private TravelConfirmationPopup travelPopup;

    private bool isCurrentLocation = false;
    private bool canTravelHere = false;

    // NOUVEAU : Variables pour l'animation
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
        // NOUVEAU : Sauvegarder l'échelle originale
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
        // NOUVEAU : Arrêter toutes les animations LeanTween sur cet objet
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
    /// NOUVEAU : Lance l'animation de clic (effet punch scale)
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

    /// <summary>
    /// NOUVEAU : Version alternative avec une seule animation "punch"
    /// Tu peux remplacer PlayClickAnimation() par celle-ci si tu préfères
    /// </summary>
    private void PlayClickAnimationPunch()
    {
        if (!enableClickAnimation || isAnimating)
            return;

        isAnimating = true;

        // Annuler toute animation en cours
        LeanTween.cancel(gameObject);

        // Animation "punch" : utilise LeanTween.punch pour un effet plus naturel
        LeanTween.scale(gameObject, originalScale + Vector3.one * (clickScaleAmount - 1f), clickAnimationDuration)
            .setEase(clickAnimationEase)
            .setLoopPingPong(1) // Fait l'aller-retour automatiquement
            .setOnComplete(() =>
            {
                transform.localScale = originalScale; // S'assurer qu'on revient exactement à l'original
                isAnimating = false;
            });
    }

    private void HandleClick()
    {
        // NOUVEAU : Jouer l'effet d'animation en premier
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
    /// Affiche un message ou des détails quand le voyage n'est pas possible
    /// </summary>
    private void ShowTravelUnavailableMessage()
    {
        var destinationLocation = locationRegistry.GetLocationById(LocationID);
        string reason = "raison inconnue";

        if (mapManager.CurrentLocation == null)
        {
            reason = "aucune location de joueur définie.";
        }
        else if (dataManager.PlayerData.IsCurrentlyTraveling())
        {
            reason = $"déjà en voyage vers {dataManager.PlayerData.TravelDestinationId}.";
        }
        else if (destinationLocation == null)
        {
            reason = $"destination '{LocationID}' introuvable.";
        }
        else if (mapManager.CurrentLocation.LocationID == LocationID)
        {
            reason = $"déjà à '{destinationLocation.DisplayName}'.";
        }
        else if (!locationRegistry.CanTravelBetween(mapManager.CurrentLocation.LocationID, LocationID))
        {
            reason = $"pas connecté à '{destinationLocation.DisplayName}'.";
        }

        Logger.LogInfo($"POI ({LocationID}): Voyage impossible vers '{destinationLocation?.DisplayName ?? LocationID}'. Raison: {reason}", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// Optionnel - Affiche les détails de la location actuelle
    /// </summary>
    private void ShowLocationDetails()
    {
        var currentLocation = locationRegistry.GetLocationById(LocationID);
        if (currentLocation != null)
        {
            if (enableDebugLogs)
            {
                Logger.LogInfo($"POI ({LocationID}): Showing details for current location: {currentLocation.DisplayName}", Logger.LogCategory.MapLog);
            }
        }
    }

    private void UpdateVisualState()
    {
        if (spriteRenderer == null || mapManager == null || dataManager == null || locationRegistry == null)
            return;

        bool isPlayerCurrentlyTraveling = dataManager.PlayerData.IsCurrentlyTraveling();
        MapLocationDefinition referenceLocation = mapManager.CurrentLocation;

        isCurrentLocation = false;
        canTravelHere = false;

        if (isPlayerCurrentlyTraveling)
        {
            isCurrentLocation = false;
            canTravelHere = false;

            if (referenceLocation == null)
            {
                spriteRenderer.color = unavailableColor;
                return;
            }

            if (referenceLocation.LocationID == this.LocationID)
            {
                spriteRenderer.color = normalColor;
            }
            else
            {
                if (locationRegistry.CanTravelBetween(referenceLocation.LocationID, this.LocationID))
                {
                    spriteRenderer.color = normalColor;
                }
                else
                {
                    spriteRenderer.color = unavailableColor;
                }
            }
        }
        else
        {
            if (referenceLocation != null && referenceLocation.LocationID == this.LocationID)
            {
                isCurrentLocation = true;
                canTravelHere = false;
                spriteRenderer.color = highlightColor;
            }
            else
            {
                isCurrentLocation = false;
                canTravelHere = mapManager.CanTravelTo(this.LocationID);

                if (canTravelHere)
                {
                    spriteRenderer.color = normalColor;
                }
                else
                {
                    spriteRenderer.color = unavailableColor;
                }
            }
        }
    }

    private void OnPlayerLocationChanged(MapLocationDefinition newLocation)
    {
        if (enableDebugLogs && newLocation != null && newLocation.LocationID == LocationID)
        {
            Logger.LogInfo($"POI ({LocationID}): Player's location is now here.", Logger.LogCategory.MapLog);
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

        // NOUVEAU : Validation des paramètres d'animation
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
        Gizmos.color = (spriteRenderer != null && spriteRenderer.color == normalColor) ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Travel Start Point Gizmo
        if (showTravelStartPoint)
        {
            Vector3 startPos = GetTravelPathStartPosition();
            Gizmos.color = travelStartPointColor;
            Gizmos.DrawWireSphere(startPos, 0.3f);

            if (travelPathStartPoint != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawLine(transform.position, startPos);
            }
        }

#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(LocationID))
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, LocationID);

            if (travelPathStartPoint != null && showTravelStartPoint)
            {
                Vector3 startPos = GetTravelPathStartPosition();
                UnityEditor.Handles.color = travelStartPointColor;
                UnityEditor.Handles.Label(startPos + Vector3.up * 0.5f, "Start");
            }
        }
#endif
    }
}