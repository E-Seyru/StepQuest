// Purpose: Panel that displays detailed information about the current location
// Filepath: Assets/Scripts/UI/Panels/LocationDetailsPanel.cs
using System.Collections;
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
    [SerializeField] private TextMeshProUGUI locationInfoText; // Infos supplementaires (connexions, etc.)

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
    private Queue<GameObject> activityItemPool = new Queue<GameObject>();

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

        // NOUVEAU : S'abonner aux evenements du MapManager pour mise à jour automatique
        if (mapManager != null)
        {
            mapManager.OnLocationChanged += OnLocationChanged;
            mapManager.OnTravelCompleted += OnTravelCompleted;
            mapManager.OnTravelStarted += OnTravelStarted;
            mapManager.OnTravelProgress += OnTravelProgress;
        }

        // Validate references
        ValidateReferences();

        // Initialize panel (will be hidden by PanelManager)
        RefreshPanel();
    }

    void OnDestroy()
    {
        // NOUVEAU : Se desabonner des evenements pour eviter les erreurs
        if (mapManager != null)
        {
            mapManager.OnLocationChanged -= OnLocationChanged;
            mapManager.OnTravelCompleted -= OnTravelCompleted;
            mapManager.OnTravelStarted -= OnTravelStarted;
            mapManager.OnTravelProgress -= OnTravelProgress;
        }
    }

    // NOUVEAU : Methodes appelees automatiquement par les evenements MapManager
    private void OnLocationChanged(MapLocationDefinition newLocation)
    {
        if (gameObject.activeInHierarchy)
        {

            StartCoroutine(RefreshPanelSmooth()); // MODIFIE : Refresh smooth sans flash
        }
    }

    private void OnTravelCompleted(string arrivedLocationId)
    {
        if (gameObject.activeInHierarchy)
        {

            StartCoroutine(RefreshPanelSmooth()); // MODIFIE : Refresh smooth sans flash
        }
    }

    private void OnTravelStarted(string destinationId)
    {
        if (gameObject.activeInHierarchy)
        {

            StartCoroutine(RefreshPanelSmooth()); // MODIFIE : Refresh smooth sans flash
        }
    }

    private void OnTravelProgress(string destinationId, int currentSteps, int requiredSteps)
    {
        if (gameObject.activeInHierarchy)
        {
            // Mettre à jour seulement la section info pour eviter de tout recalculer
            PopulateInfoSection();
        }
    }

    /// <summary>
    /// NOUVEAU : Refresh le panel sans flash visuel desagreable
    /// </summary>
    private IEnumerator RefreshPanelSmooth()
    {
        // 1. Cacher temporairement le contenu principal pour eviter le flash
        if (locationDescriptionText != null)
            locationDescriptionText.gameObject.SetActive(false);
        if (activitiesSection != null)
            activitiesSection.SetActive(false);

        // 2. Faire le refresh du contenu
        RefreshPanel();

        // 3. Attendre que Unity recalcule tout
        yield return null;
        Canvas.ForceUpdateCanvases();

        // 4. Fix du ScrollRect
        if (descriptionScrollRect != null && descriptionScrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(descriptionScrollRect.content);
            descriptionScrollRect.verticalNormalizedPosition = 1f;
        }

        // 5. Reafficher le contenu maintenant que tout est bien place
        if (locationDescriptionText != null)
            locationDescriptionText.gameObject.SetActive(true);
        if (activitiesSection != null)
            activitiesSection.SetActive(true);
    }

    /// <summary>
    /// Called when the panel becomes active - refresh with current location
    /// </summary>
    void OnEnable()
    {

        if (ActivityDisplayPanel.Instance != null)
        {
            ActivityDisplayPanel.Instance.CheckAndShowIfActivityActive();
        }
        RefreshPanel();
        StartCoroutine(DelayedLayoutFix());
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
        // Clear existing activity items
        RecycleActivityItems();

        if (activitiesSection == null)
        {
            Debug.LogError("ERROR: activitiesSection is NULL!");
            return;
        }

        if (currentLocation == null)
        {
            Debug.LogError("ERROR: currentLocation is NULL!");
            return;
        }

        var availableActivities = currentLocation.GetAvailableActivities();

        // Debug chaque activite
        if (currentLocation.AvailableActivities != null)
        {
            for (int i = 0; i < currentLocation.AvailableActivities.Count; i++)
            {
                var activity = currentLocation.AvailableActivities[i];
            }
        }

        // Update section title
        if (activitiesSectionTitle != null)
        {
            activitiesSectionTitle.text = $"Activites disponibles ({availableActivities.Count})";
        }

        // Show/hide based on availability
        if (availableActivities.Count == 0)
        {

            ShowNoActivitiesMessage();
        }
        else
        {
            HideNoActivitiesMessage();
            CreateActivityItems(availableActivities);
        }
    }

    /// <summary>
    /// Create UI items for each activity
    /// </summary>
    private void CreateActivityItems(List<LocationActivity> activities)
    {
        if (activitiesContainer == null || activityItemPrefab == null) return;

        foreach (var activity in activities)
        {
            GameObject item = GetPooledItem();
            if (item.transform.parent != activitiesContainer)     // s'il vient du pool
                item.transform.SetParent(activitiesContainer, false);

            instantiatedActivityItems.Add(item);
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
            button.onClick.RemoveAllListeners();
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

        // Note: activitiesSection visibility is now managed by RefreshPanelSmooth()
    }

    /// <summary>
    /// Show/hide no activities message
    /// </summary>
    private void ShowNoActivitiesMessage()
    {
        if (noActivitiesText != null)
        {
            noActivitiesText.gameObject.SetActive(true);
            noActivitiesText.text = "Aucune activite disponible dans cette location.";
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
    private void RecycleActivityItems()
    {
        foreach (var item in instantiatedActivityItems)
        {
            if (item != null)
            {
                item.SetActive(false);          // on ne detruit plus
                activityItemPool.Enqueue(item); // on stocke pour re-emploi
            }
        }
        instantiatedActivityItems.Clear();
    }
    private GameObject GetPooledItem()
    {
        if (activityItemPool.Count > 0)
        {
            var item = activityItemPool.Dequeue();
            item.SetActive(true);
            return item;
        }
        // - sinon ancienne logique
        return Instantiate(activityItemPrefab, activitiesContainer);
    }
    /// <summary>
    /// Close the panel
    /// </summary>
    private void ClosePanel()
    {


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

    /// <summary>
    /// Attendre une frame que le Canvas soit actif, puis forcer le layout.
    /// Evite que texte et image se superposent et que le ScrollRect reste bloque.
    /// </summary>
    private IEnumerator DelayedLayoutFix()
    {
        yield return null;                    // attendre la fin de frame courante
        Canvas.ForceUpdateCanvases();         // pousse Unity à recalculer immediatement

        if (descriptionScrollRect != null && descriptionScrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(descriptionScrollRect.content);
            descriptionScrollRect.verticalNormalizedPosition = 1f; // facultatif : scroller en haut
        }
    }
}