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

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Internal state
    private MapManager mapManager;
    private DataManager dataManager;
    private LocationRegistry locationRegistry;
    private TravelConfirmationPopup travelPopup; // NOUVEAU : Référence vers la popup

    private bool isCurrentLocation = false;
    private bool canTravelHere = false;

    /// <summary>
    /// Retourne la position de départ pour le chemin de voyage
    /// </summary>
    public Vector3 GetTravelPathStartPosition()
    {
        if (travelPathStartPoint != null)
        {
            return travelPathStartPoint.position;
        }
        return transform.position; // Fallback vers le centre du POI
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

        // NOUVEAU : Obtenir la référence vers la popup de voyage
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

    private void HandleClick()
    {
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
            // TODO: Maybe open location panel instead of travel panel
            ShowLocationDetails(); // NOUVEAU : Optionnel - montrer les détails de la location actuelle
            return;
        }

        // MODIFIÉ : Vérifier si le voyage est possible avant d'ouvrir la popup
        if (mapManager.CanTravelTo(LocationID))
        {
            // NOUVEAU : Ouvrir la popup de confirmation au lieu de démarrer directement le voyage
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
                // Fallback : si la popup n'est pas disponible, utiliser l'ancien comportement
                Logger.LogWarning($"POI ({LocationID}): TravelConfirmationPopup not available, starting travel directly", Logger.LogCategory.MapLog);
                mapManager.StartTravel(LocationID);
            }
        }
        else
        {
            // AMÉLIORÉ : Donner plus de feedback quand le voyage n'est pas possible
            ShowTravelUnavailableMessage();
        }
    }

    /// <summary>
    /// NOUVEAU : Affiche un message ou des détails quand le voyage n'est pas possible
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

        // TODO: Optionnel - Afficher un message UI temporaire au joueur
        // ShowTemporaryMessage($"Impossible de voyager vers {destinationLocation?.DisplayName ?? LocationID}: {reason}");
    }

    /// <summary>
    /// NOUVEAU : Optionnel - Affiche les détails de la location actuelle
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

            // TODO: Ouvrir un panneau de détails de la location actuelle
            // ou afficher des informations sur les activités disponibles
            // LocationDetailsPanel.Instance?.ShowLocationDetails(currentLocation);
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
            // Player is traveling. No POI is "current". New travel cannot be initiated.
            isCurrentLocation = false;
            canTravelHere = false;

            if (referenceLocation == null)
            {
                spriteRenderer.color = unavailableColor;
                return;
            }

            // `referenceLocation` is the DEPARTURE point.
            if (referenceLocation.LocationID == this.LocationID)
            {
                // This POI is the departure POI.
                spriteRenderer.color = normalColor;
            }
            else
            {
                // This POI is not the departure POI.
                // Check connectivity to the departure POI.
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
        else // Player is NOT traveling
        {
            if (referenceLocation != null && referenceLocation.LocationID == this.LocationID)
            {
                // This POI is the player's current, actual location.
                isCurrentLocation = true;
                canTravelHere = false;
                spriteRenderer.color = highlightColor;
            }
            else
            {
                // This POI is not the player's current location.
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

            // Ligne entre le POI et son point de départ si différents
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

            // Label pour le point de départ si différent
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