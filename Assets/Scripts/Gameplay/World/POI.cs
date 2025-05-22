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
    private bool isCurrentLocation = false;
    private bool canTravelHere = false;

    void Start()
    {
        // Get MapManager reference
        mapManager = MapManager.Instance;

        if (mapManager == null)
        {
            Logger.LogError($"POI ({LocationID}): MapManager not found!", Logger.LogCategory.MapLog);
            return;
        }

        // Get SpriteRenderer if not assigned
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
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

        // Check if this is the current location
        if (isCurrentLocation)
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
        if (spriteRenderer == null || mapManager == null)
            return;

        // Check if this is the current location
        isCurrentLocation = (mapManager.CurrentLocation != null &&
                           mapManager.CurrentLocation.LocationID == LocationID);

        // Check if we can travel here
        canTravelHere = mapManager.CanTravelTo(LocationID);

        // Update visual appearance
        if (isCurrentLocation)
        {
            // Player is here - maybe use a special indicator
            spriteRenderer.color = highlightColor;
            if (enableDebugLogs)
            {
                Logger.LogInfo($"POI ({LocationID}): This is the current location", Logger.LogCategory.MapLog);
            }
        }
        else if (canTravelHere)
        {
            // Can travel here
            spriteRenderer.color = normalColor;
        }
        else
        {
            // Cannot travel here
            spriteRenderer.color = unavailableColor;
        }
    }

    private void OnPlayerLocationChanged(MapLocationDefinition newLocation)
    {
        if (!enableDebugLogs) return;

        // On ne logue que si CE POI est impliqué
        if (newLocation != null && newLocation.LocationID == LocationID)
        {
            Logger.LogInfo($"POI ({LocationID}): Le joueur est maintenant ici.", Logger.LogCategory.MapLog);
        }
        UpdateVisualState();
    }

    private void OnTravelStarted(string destinationId)
    {
        if (enableDebugLogs && destinationId == LocationID)
        {
            Logger.LogInfo($"POI ({LocationID}): Travel started toward this location!", Logger.LogCategory.MapLog);
        }
        UpdateVisualState();
    }

    private void OnTravelCompleted(string arrivedLocationId)
    {
        if (enableDebugLogs && arrivedLocationId == LocationID)
        {
            Logger.LogInfo($"POI ({LocationID}): Player arrived at this location!", Logger.LogCategory.MapLog);
        }
        UpdateVisualState();
    }

    // Visual feedback for mouse hover (optional)
    void OnMouseEnter()
    {
        if (spriteRenderer != null && !isCurrentLocation)
        {
            // Slightly brighten the sprite
            Color currentColor = spriteRenderer.color;
            spriteRenderer.color = new Color(currentColor.r * 1.2f, currentColor.g * 1.2f, currentColor.b * 1.2f, currentColor.a);
        }
    }

    void OnMouseExit()
    {
        UpdateVisualState(); // Restore normal color
    }

    // Editor utility to help set up POIs
    void OnValidate()
    {
        if (string.IsNullOrEmpty(LocationID))
        {
            Logger.LogWarning($"POI on GameObject '{gameObject.name}': LocationID is not set!", Logger.LogCategory.MapLog);
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
        Gizmos.color = canTravelHere ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Draw location ID as text in scene view
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, LocationID);
#endif
    }
}