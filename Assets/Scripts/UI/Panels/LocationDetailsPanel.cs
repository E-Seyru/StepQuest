// Purpose: Panel that displays detailed information about the current location
// Filepath: Assets/Scripts/UI/Panels/LocationDetailsPanel.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocationDetailsPanel : MonoBehaviour
{
    [Header("UI References - Header")]
    [SerializeField] private TextMeshProUGUI locationNameText;
    [SerializeField] private Image locationImage;
    [SerializeField] private Button closeButton;

    [Header("UI References - Content")]
    [SerializeField] private TextMeshProUGUI locationDescriptionText;
    [SerializeField] private ScrollRect descriptionScrollRect;

    [Header("UI References - Activities Section")]
    [SerializeField] private GameObject activitiesSection;
    [SerializeField] private TextMeshProUGUI activitiesSectionTitle;
    [SerializeField] private Transform activitiesContainer;
    [SerializeField] private GameObject activityItemPrefab;
    [SerializeField] private TextMeshProUGUI noActivitiesText;

    [Header("UI References - Info Section")]
    [SerializeField] private TextMeshProUGUI locationInfoText; // Infos supplémentaires (connexions, etc.)

    [Header("Settings")]
    [SerializeField] private Color defaultImageColor = Color.gray;
    [SerializeField] private string defaultImageText = "Aucune image";

    // References
    private MapManager mapManager;
    private DataManager dataManager;
    private PanelManager panelManager;

    // Current state
    private MapLocationDefinition currentLocation;
    private List<GameObject> instantiatedActivityItems = new List<GameObject>();

    public static LocationDetailsPanel Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Logger.LogWarning("LocationDetailsPanel: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.General);
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Get references
        mapManager = MapManager.Instance;
        dataManager = DataManager.Instance;
        panelManager = PanelManager.Instance;

        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        // Validate references
        ValidateReferences();

        // Initialize panel (will be hidden by PanelManager)
        RefreshPanel();
    }

    /// <summary>
    /// Called when the panel becomes active - refresh with current location
    /// </summary>
    void OnEnable()
    {
        RefreshPanel();
    }

    /// <summary>
    /// Public method to open this panel and display current location
    /// </summary>
    public void OpenLocationDetails()
    {
        if (mapManager?.CurrentLocation == null)
        {
            Logger.LogWarning("LocationDetailsPanel: Cannot open - no current location!", Logger.LogCategory.General);
            return;
        }

        Logger.LogInfo($"LocationDetailsPanel: Opening details for {mapManager.CurrentLocation.DisplayName}", Logger.LogCategory.General);

        RefreshPanel();

        // Use PanelManager to show this panel - need to find our index in the panels list
        if (panelManager != null)
        {
            // TODO: The LocationDetailsPanel should be added to the panels list in PanelManager
            // Then we can call panelManager.ShowPanel(ourIndex);
            Logger.LogWarning("LocationDetailsPanel: Panel should be added to PanelManager.panels list to work with standard navigation", Logger.LogCategory.General);
        }
    }

    /// <summary>
    /// Refresh the panel with current location data
    /// </summary>
    public void RefreshPanel()
    {
        if (mapManager?.CurrentLocation == null)
        {
            ShowNoLocationMessage();
            return;
        }

        currentLocation = mapManager.CurrentLocation;
        PopulateLocationInfo();
    }

    /// <summary>
    /// Populate all UI elements with location data
    /// </summary>
    private void PopulateLocationInfo()
    {
        if (currentLocation == null) return;

        // Header - Location name
        if (locationNameText != null)
        {
            locationNameText.text = currentLocation.DisplayName;
        }

        // Header - Location image
        PopulateLocationImage();

        // Content - Description
        if (locationDescriptionText != null)
        {
            locationDescriptionText.text = currentLocation.GetBestDescription();
        }

        // Activities section
        PopulateActivitiesSection();

        // Info section
        PopulateInfoSection();

        Logger.LogInfo($"LocationDetailsPanel: Populated panel for {currentLocation.DisplayName}", Logger.LogCategory.General);
    }

    /// <summary>
    /// Set the location image or show default
    /// </summary>
    private void PopulateLocationImage()
    {
        if (locationImage == null) return;

        if (currentLocation.LocationImage != null)
        {
            locationImage.sprite = currentLocation.LocationImage;
            locationImage.color = Color.white;
        }
        else
        {
            locationImage.sprite = null;
            locationImage.color = defaultImageColor;
        }
    }

    /// <summary>
    /// Populate the activities section
    /// </summary>
    private void PopulateActivitiesSection()
    {
        Debug.Log("=== DEBUG PopulateActivitiesSection ===");

        // RÉACTIVER LA SECTION AU CAS OÙ ELLE AURAIT ÉTÉ DÉSACTIVÉE
        if (activitiesSection != null)
        {
            activitiesSection.SetActive(true);
        }

        // Clear existing activity items
        ClearActivityItems();

        if (activitiesSection == null)
        {
            Debug.Log("ERROR: activitiesSection is NULL!");
            return;
        }

        if (currentLocation == null)
        {
            Debug.Log("ERROR: currentLocation is NULL!");
            return;
        }

        Debug.Log($"Current location: {currentLocation.DisplayName}");
        Debug.Log($"Total AvailableActivities: {currentLocation.AvailableActivities?.Count ?? 0}");

        var availableActivities = currentLocation.GetAvailableActivities();
        Debug.Log($"Valid activities after filtering: {availableActivities.Count}");

        // Debug chaque activité
        if (currentLocation.AvailableActivities != null)
        {
            for (int i = 0; i < currentLocation.AvailableActivities.Count; i++)
            {
                var activity = currentLocation.AvailableActivities[i];
                Debug.Log($"Activity {i}: {activity?.GetDisplayName() ?? "NULL"}");
                if (activity != null)
                {
                    Debug.Log($"  - IsValidActivity: {activity.IsValidActivity()}");
                    Debug.Log($"  - VariantCount: {activity.ActivityVariants?.Count ?? 0}");
                }
            }
        }

        // Update section title
        if (activitiesSectionTitle != null)
        {
            activitiesSectionTitle.text = $"Activités disponibles ({availableActivities.Count})";
            Debug.Log($"Section title updated: Activités disponibles ({availableActivities.Count})");
        }

        // Show/hide based on availability
        if (availableActivities.Count == 0)
        {
            Debug.Log("No valid activities found - showing no activities message");
            ShowNoActivitiesMessage();
        }
        else
        {
            Debug.Log($"Found {availableActivities.Count} valid activities - creating activity items");
            HideNoActivitiesMessage();
            CreateActivityItems(availableActivities);
        }

        Debug.Log("=== END DEBUG PopulateActivitiesSection ===");
    }

    /// <summary>
    /// Create UI items for each activity
    /// </summary>
    private void CreateActivityItems(List<LocationActivity> activities)
    {
        if (activitiesContainer == null || activityItemPrefab == null) return;

        foreach (var activity in activities)
        {
            GameObject item = Instantiate(activityItemPrefab, activitiesContainer);
            instantiatedActivityItems.Add(item);

            // Setup the activity item
            SetupActivityItem(item, activity);
        }
    }

    /// <summary>
    /// Setup an individual activity item UI
    /// </summary>
    private void SetupActivityItem(GameObject item, LocationActivity activity)
    {
        if (item == null || activity == null) return;

        // Find components in the activity item (assuming standard structure)
        var nameText = item.GetComponentInChildren<TextMeshProUGUI>();
        var iconImage = item.GetComponentInChildren<Image>();
        var button = item.GetComponent<Button>();

        // Set activity name
        if (nameText != null)
        {
            nameText.text = activity.GetDisplayName();
        }

        // Set activity icon
        if (iconImage != null && activity.GetIcon() != null)
        {
            iconImage.sprite = activity.GetIcon();
        }

        // Set up click handler
        if (button != null)
        {
            button.onClick.AddListener(() => OnActivityClicked(activity));
        }

        // Tooltip or additional info setup could go here
        SetupActivityItemTooltip(item, activity);
    }

    /// <summary>
    /// Setup tooltip for activity item (optional)
    /// </summary>
    private void SetupActivityItemTooltip(GameObject item, LocationActivity activity)
    {
        // TODO: Implement tooltip system showing activity description and resources
        // For now, we could add the info to a secondary text component if it exists

        var descTexts = item.GetComponentsInChildren<TextMeshProUGUI>();
        if (descTexts.Length > 1) // If there's a secondary text component
        {
            descTexts[1].text = activity.GetResourcesText();
        }
    }

    /// <summary>
    /// Handle activity item clicked
    /// </summary>
    private void OnActivityClicked(LocationActivity activity)
    {
        Logger.LogInfo($"LocationDetailsPanel: Activity clicked: {activity.GetDisplayName()}", Logger.LogCategory.General);

        // Ouvrir le panel des variants
        if (ActivityVariantsPanel.Instance != null)
        {
            ActivityVariantsPanel.Instance.OpenWithActivity(activity);
        }
        else
        {
            Debug.LogWarning("ActivityVariantsPanel not found!");
        }
    }

    /// <summary>
    /// Populate the info section with additional location details
    /// </summary>
    private void PopulateInfoSection()
    {
        if (locationInfoText == null || currentLocation == null) return;

        List<string> infoLines = new List<string>();

        // Connection info
        if (currentLocation.Connections != null && currentLocation.Connections.Count > 0)
        {
            infoLines.Add($"Connexions: {currentLocation.Connections.Count} destination(s)");
        }

        // Activity summary
        infoLines.Add(currentLocation.GetActivitiesSummary());

        // Travel info if currently traveling
        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            string dest = dataManager.PlayerData.TravelDestinationId;
            long progress = dataManager.PlayerData.GetTravelProgress(dataManager.PlayerData.TotalSteps);
            int required = dataManager.PlayerData.TravelRequiredSteps;
            infoLines.Add($"En voyage vers {dest}: {progress}/{required} pas");
        }

        locationInfoText.text = string.Join("\n", infoLines);
    }

    /// <summary>
    /// Show message when no location is available
    /// </summary>
    private void ShowNoLocationMessage()
    {
        if (locationNameText != null)
        {
            locationNameText.text = "Aucune location";
        }

        if (locationDescriptionText != null)
        {
            locationDescriptionText.text = "Aucune information de location disponible.";
        }

        if (activitiesSection != null)
        {
            activitiesSection.SetActive(false);
        }
    }

    /// <summary>
    /// Show/hide no activities message
    /// </summary>
    private void ShowNoActivitiesMessage()
    {
        if (noActivitiesText != null)
        {
            noActivitiesText.gameObject.SetActive(true);
            noActivitiesText.text = "Aucune activité disponible dans cette location.";
        }
    }

    private void HideNoActivitiesMessage()
    {
        if (noActivitiesText != null)
        {
            noActivitiesText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Clear all instantiated activity items
    /// </summary>
    private void ClearActivityItems()
    {
        foreach (var item in instantiatedActivityItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        instantiatedActivityItems.Clear();
    }

    /// <summary>
    /// Close the panel
    /// </summary>
    private void ClosePanel()
    {
        Logger.LogInfo("LocationDetailsPanel: Closing panel", Logger.LogCategory.General);

        if (panelManager != null)
        {
            // Go back to previous panel or main panel
            // This should work automatically with PanelManager's standard navigation
            Logger.LogInfo("LocationDetailsPanel: Requesting return to previous panel", Logger.LogCategory.General);
            // panelManager.ShowPanel(previousPanelIndex); // This would be handled by PanelManager
        }
        else
        {
            // Fallback - just hide the panel
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Validate that all required UI references are assigned
    /// </summary>
    private void ValidateReferences()
    {
        List<string> missing = new List<string>();

        if (locationNameText == null) missing.Add("locationNameText");
        if (locationDescriptionText == null) missing.Add("locationDescriptionText");
        if (activitiesContainer == null) missing.Add("activitiesContainer");

        if (missing.Count > 0)
        {
            Logger.LogWarning($"LocationDetailsPanel: Missing UI references: {string.Join(", ", missing)}", Logger.LogCategory.General);
        }
    }

    /// <summary>
    /// Force refresh the panel (useful for debug)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceRefresh()
    {
        RefreshPanel();
    }
}