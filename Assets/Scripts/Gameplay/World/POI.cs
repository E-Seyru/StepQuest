// Purpose: Clickable POI on the world map that triggers travel
// Filepath: Assets/Scripts/Gameplay/World/POI.cs
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class POI : MonoBehaviour, IPointerClickHandler
{
    [Header("Location Settings")]
    [Tooltip("The ID that matches your MapLocationDefinition")]
    public string LocationID;


    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;
    [SerializeField] private Color unavailableColor = Color.gray;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Internal state
    private MapManager mapManager;
    private DataManager dataManager; // Added DataManager reference
    private LocationRegistry locationRegistry; // Added LocationRegistry reference

    private bool isCurrentLocation = false;
    private bool canTravelHere = false;

    void Start()
    {
        // Get MapManager reference
        mapManager = MapManager.Instance;
        dataManager = DataManager.Instance; // Get DataManager reference

        if (mapManager == null)
        {
            Logger.LogError($"POI ({LocationID}): MapManager not found!", Logger.LogCategory.MapLog);
            // gameObject.SetActive(false); // Optionally disable if critical
            return;
        }

        if (dataManager == null)
        {
            Logger.LogError($"POI ({LocationID}): DataManager not found!", Logger.LogCategory.MapLog);
            // gameObject.SetActive(false); // Optionally disable if critical
            return;
        }

        locationRegistry = mapManager.LocationRegistry; // Get LocationRegistry from MapManager
        if (locationRegistry == null)
        {
            Logger.LogError($"POI ({LocationID}): LocationRegistry not found via MapManager!", Logger.LogCategory.MapLog);
            // gameObject.SetActive(false); // Optionally disable if critical
            return;
        }


        // Get SpriteRenderer if not assigned
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Logger.LogError($"POI ({LocationID}): SpriteRenderer component not found!", Logger.LogCategory.MapLog);
                // gameObject.SetActive(false); // Optionally disable if critical
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
            Logger.LogInfo($"POI: Initialized POI for location '{LocationID}' at position ({transform.position.x}, {transform.position.y})", Logger.LogCategory.MapLog);
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
            return;
        }

        // Call MapManager to handle the click
        mapManager.OnPOIClicked(LocationID);
    }

    private void UpdateVisualState()
    {
        if (spriteRenderer == null || mapManager == null || dataManager == null || locationRegistry == null)
            return;

        bool isPlayerCurrentlyTraveling = dataManager.PlayerData.IsCurrentlyTraveling();
        MapLocationDefinition referenceLocation = mapManager.CurrentLocation; // Actual current location or departure location

        isCurrentLocation = false; // Default to false
        canTravelHere = false;   // Default to false

        if (isPlayerCurrentlyTraveling)
        {
            // Player is traveling. No POI is "current". New travel cannot be initiated.
            isCurrentLocation = false;
            canTravelHere = false;

            if (referenceLocation == null) // Should not happen if traveling, but good practice
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
                canTravelHere = false; // Cannot travel to where you currently are.
                spriteRenderer.color = highlightColor;
            }
            else
            {
                // This POI is not the player's current location.
                isCurrentLocation = false;
                // Check if travel can be initiated from the player's actual current location to this POI.
                // `mapManager.CanTravelTo` implicitly uses `mapManager.CurrentLocation` (which is actual current)
                canTravelHere = mapManager.CanTravelTo(this.LocationID);

                if (canTravelHere)
                {
                    spriteRenderer.color = normalColor;
                }
                else
                {
                    // Not connected to player's current location, or other rule prevents travel.
                    spriteRenderer.color = unavailableColor;
                }
            }
        }
    }

    private void OnPlayerLocationChanged(MapLocationDefinition newLocation)
    {
        // This event fires when travel completes and player is at a new location.
        // Or if the location changes by other means (e.g. teleport).
        if (enableDebugLogs && newLocation != null && newLocation.LocationID == LocationID)
        {
            Logger.LogInfo($"POI ({LocationID}): Player's location is now here.", Logger.LogCategory.MapLog);
        }
        UpdateVisualState();
    }

    private void OnTravelStarted(string destinationId)
    {
        // This event fires when travel *begins*.
        if (enableDebugLogs && destinationId == LocationID)
        {
            Logger.LogInfo($"POI ({LocationID}): Travel started TOWARD this location!", Logger.LogCategory.MapLog);
        }
        // All POIs need to update their state because player is now "in transit".
        UpdateVisualState();
    }

    private void OnTravelCompleted(string arrivedLocationId)
    {
        // This event fires when travel *ends*.
        if (enableDebugLogs && arrivedLocationId == LocationID)
        {
            Logger.LogInfo($"POI ({LocationID}): Player ARRIVED at this location!", Logger.LogCategory.MapLog);
        }
        // All POIs need to update their state, especially the one arrived at.
        UpdateVisualState();
    }

    // Visual feedback for mouse hover (optional)
    void OnMouseEnter()
    {
        // Only apply hover effect if not the current location and sprite is available
        if (spriteRenderer != null && !isCurrentLocation && !dataManager.PlayerData.IsCurrentlyTraveling())
        {
            if (spriteRenderer.color == normalColor) // Only highlight if it's normally interactable
            {
                // Slightly brighten the sprite
                Color currentColor = spriteRenderer.color;
                spriteRenderer.color = new Color(currentColor.r * 1.2f, currentColor.g * 1.2f, currentColor.b * 1.2f, currentColor.a);
            }
        }
    }

    void OnMouseExit()
    {
        // Restore normal color state, don't just assume normalColor
        UpdateVisualState();
    }

    // Editor utility to help set up POIs
    void OnValidate()
    {
        if (string.IsNullOrEmpty(LocationID))
        {
            // Using Unity's Debug.LogWarning for OnValidate as Logger might not be fully initialized in editor.
            Debug.LogWarning($"POI on GameObject '{gameObject.name}': LocationID is not set!");
        }

        // Auto-assign SpriteRenderer if missing
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    // Debug visualization in Scene view
    void OnDrawGizmosSelected()
    {
        // Draw a circle around the POI when selected
        // Gizmos color might not perfectly reflect runtime 'canTravelHere' due to editor context
        Gizmos.color = (spriteRenderer != null && spriteRenderer.color == normalColor) ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Draw location ID as text in scene view


#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(LocationID))
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, LocationID);
        }
#endif
    }
}