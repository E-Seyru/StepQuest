using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TravelProgressUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject travelProgressPanelRoot;
    public Slider travelProgressBar;
    public TextMeshProUGUI destinationNameText;
    public TextMeshProUGUI stepsProgressText;
    public TextMeshProUGUI statusMessageText;

    // private MapManager mapManager; // Replaced with MapManager.Instance

    void Awake()
    {
        // MapManager is expected to be a Singleton accessible via MapManager.Instance
        // No FindObjectOfType needed if MapManager uses a static Instance property.
        if (MapManager.Instance == null)
        {
            Debug.LogError("TravelProgressUI: MapManager.Instance is null. UI will not function.");
            if (travelProgressPanelRoot != null)
            {
                travelProgressPanelRoot.SetActive(false);
            }
            return;
        }

        // Subscribe to MapManager events
        MapManager.Instance.OnTravelStarted += HandleTravelStarted;
        MapManager.Instance.OnTravelProgress += HandleTravelProgress;
        MapManager.Instance.OnTravelCompleted += HandleTravelCompleted;

        // Initially hide the panel
        if (travelProgressPanelRoot != null)
        {
            travelProgressPanelRoot.SetActive(false);
            Debug.Log("TravelProgressUI: Panel hidden on Awake.");
        }
        else
        {
            Debug.LogError("TravelProgressUI: travelProgressPanelRoot is not assigned in the Inspector.");
        }

        // Basic null checks for other UI elements
        if (travelProgressBar == null) Debug.LogError("TravelProgressUI: travelProgressBar is not assigned.");
        if (destinationNameText == null) Debug.LogError("TravelProgressUI: destinationNameText is not assigned.");
        if (stepsProgressText == null) Debug.LogError("TravelProgressUI: stepsProgressText is not assigned.");
        if (statusMessageText == null) Debug.LogError("TravelProgressUI: statusMessageText is not assigned.");
    }

    void OnDestroy()
    {
        // Ensure MapManager.Instance is not null before trying to unsubscribe
        if (MapManager.Instance != null)
        {
            MapManager.Instance.OnTravelStarted -= HandleTravelStarted;
            MapManager.Instance.OnTravelProgress -= HandleTravelProgress;
            MapManager.Instance.OnTravelCompleted -= HandleTravelCompleted;
            Debug.Log("TravelProgressUI: Unsubscribed from MapManager events.");
        }
    }

    private void HandleTravelStarted(string destinationId) // Signature changed
    {
        if (MapManager.Instance == null)
        {
            Debug.LogError("TravelProgressUI: MapManager.Instance is null in HandleTravelStarted. Cannot proceed.");
            if (travelProgressPanelRoot != null) travelProgressPanelRoot.SetActive(false);
            return;
        }

        // Retrieve requiredSteps from PlayerData
        if (MapManager.Instance.dataManager == null || MapManager.Instance.dataManager.PlayerData == null)
        {
            Debug.LogError("TravelProgressUI: dataManager or PlayerData is null. Cannot get requiredSteps.");
            if (travelProgressPanelRoot != null) travelProgressPanelRoot.SetActive(false);
            return;
        }
        int requiredSteps = MapManager.Instance.dataManager.PlayerData.TravelRequiredSteps;

        // Retrieve location definition
        if (MapManager.Instance.LocationRegistry == null)
        {
            Debug.LogError("TravelProgressUI: LocationRegistry is null. Cannot get location definition.");
            if (travelProgressPanelRoot != null) travelProgressPanelRoot.SetActive(false);
            return;
        }
        MapLocationDefinition destination = MapManager.Instance.LocationRegistry.GetLocationById(destinationId);

        if (destination == null)
        {
            Debug.LogError($"TravelProgressUI: Could not find location definition for ID: {destinationId}");
            if (travelProgressPanelRoot != null) travelProgressPanelRoot.SetActive(false);
            return;
        }

        if (travelProgressPanelRoot != null)
        {
            travelProgressPanelRoot.SetActive(true);
            Debug.Log($"TravelProgressUI: Travel started to {destination.DisplayName}. UI Visible. Required steps: {requiredSteps}");
        }

        if (destinationNameText != null)
        {
            destinationNameText.text = destination.DisplayName;
        }

        if (statusMessageText != null)
        {
            statusMessageText.text = "Traveling to...";
        }

        if (travelProgressBar != null)
        {
            travelProgressBar.value = 0;
            travelProgressBar.maxValue = requiredSteps;
        }

        if (stepsProgressText != null)
        {
            stepsProgressText.text = $"0 / {requiredSteps} steps";
        }
    }

    private void HandleTravelProgress(string destinationId, int currentSteps, int requiredSteps)
    {
        if (travelProgressPanelRoot == null || !travelProgressPanelRoot.activeSelf)
        {
            // Don't update if panel is not visible or not assigned
            return;
        }

        if (travelProgressBar != null)
        {
            travelProgressBar.value = currentSteps;
            travelProgressBar.maxValue = requiredSteps; // Ensure maxValue is up-to-date
        }

        if (stepsProgressText != null)
        {
            stepsProgressText.text = $"{currentSteps} / {requiredSteps} steps";
        }
        Debug.Log($"TravelProgressUI: Progress update - {currentSteps}/{requiredSteps} steps to destination {destinationId}.");
    }

    private void HandleTravelCompleted(string destinationId)
    {
        if (travelProgressPanelRoot != null)
        {
            travelProgressPanelRoot.SetActive(false);
            Debug.Log($"TravelProgressUI: Travel completed to {destinationId}. UI Hidden.");
        }

        // Optionally reset UI elements to a default state
        if (destinationNameText != null) destinationNameText.text = "Destination";
        if (stepsProgressText != null) stepsProgressText.text = "0 / 0 steps";
        if (statusMessageText != null) statusMessageText.text = "Arrived.";
        if (travelProgressBar != null)
        {
            travelProgressBar.value = 0;
            travelProgressBar.maxValue = 100; // Default max value
        }
    }
}
// Note: This script assumes the existence of MapManager as a Singleton (MapManager.Instance) with the following:
// - public event Action<string> OnTravelStarted; (destinationId) - Signature updated as per task.
// - public event Action<string, int, int> OnTravelProgress; (destinationId, currentSteps, requiredSteps)
// - public event Action<string> OnTravelCompleted; (destinationId)
// - public DataManager dataManager; (which has PlayerData with a public int TravelRequiredSteps property)
// - public LocationRegistryModule LocationRegistry; (which has a GetLocationById(string locationId) method)
// - MapLocationDefinition class/struct with a public string DisplayName property.
// Ensure MapManager.Instance is available and its members (dataManager, PlayerData, LocationRegistry) are initialized before travel starts.
// The UI elements (travelProgressPanelRoot, etc.) must be assigned in the Unity Inspector.
// HandleTravelStarted now retrieves requiredSteps from MapManager.Instance.dataManager.PlayerData.TravelRequiredSteps.
// Location is retrieved via MapManager.Instance.LocationRegistry.GetLocationById(destinationId).
// Null checks for MapManager.Instance, dataManager, PlayerData, and LocationRegistry have been added in HandleTravelStarted.
