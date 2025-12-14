// ===============================================
// SERVICE: Travel Path Display Management
// ===============================================
// Purpose: Dynamically builds the travel path UI showing all locations in the journey
// Path: Location -> Arrow -> Location -> Arrow -> ... -> Destination

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AboveCanvasTravelPathService
{
    private readonly AboveCanvasManager manager;
    private readonly List<GameObject> instantiatedPathElements = new List<GameObject>();

    public AboveCanvasTravelPathService(AboveCanvasManager manager)
    {
        this.manager = manager;
    }

    /// <summary>
    /// Builds the travel path UI from a list of location IDs
    /// </summary>
    /// <param name="locationIds">List of location IDs in order (origin -> ... -> destination)</param>
    public void BuildTravelPath(List<string> locationIds)
    {
        // Clear any existing path elements
        ClearTravelPath();

        if (manager.TravelPathContainer == null)
        {
            Logger.LogWarning("AboveCanvasTravelPathService: TravelPathContainer is null", Logger.LogCategory.General);
            return;
        }

        if (locationIds == null || locationIds.Count == 0)
        {
            Logger.LogWarning("AboveCanvasTravelPathService: No locations to display", Logger.LogCategory.General);
            return;
        }

        // Show the container
        manager.TravelPathContainer.gameObject.SetActive(true);

        var locationRegistry = MapManager.Instance?.LocationRegistry;
        if (locationRegistry == null)
        {
            Logger.LogWarning("AboveCanvasTravelPathService: LocationRegistry is null", Logger.LogCategory.General);
            return;
        }

        // Build path: Location -> Arrow -> Location -> Arrow -> ... -> Location
        for (int i = 0; i < locationIds.Count; i++)
        {
            // Add location icon
            var location = locationRegistry.GetLocationById(locationIds[i]);
            if (location != null)
            {
                AddLocationIcon(location, i == 0, i == locationIds.Count - 1);
                Logger.LogWarning($"AboveCanvasTravelPathService: Added icon {i + 1}/{locationIds.Count} for {location.DisplayName}", Logger.LogCategory.General);
            }
            else
            {
                Logger.LogWarning($"AboveCanvasTravelPathService: Location {locationIds[i]} not found in registry!", Logger.LogCategory.General);
            }

            // Add arrow between locations (not after the last one)
            if (i < locationIds.Count - 1)
            {
                AddArrow();
                Logger.LogWarning($"AboveCanvasTravelPathService: Added arrow after icon {i + 1}", Logger.LogCategory.General);
            }
        }

        Logger.LogWarning($"AboveCanvasTravelPathService: Built travel path - Total elements in list: {instantiatedPathElements.Count}", Logger.LogCategory.General);
    }

    /// <summary>
    /// Builds travel path from current travel state (gets path from PlayerData/MapManager)
    /// </summary>
    public void BuildTravelPathFromCurrentTravel()
    {
        var dataManager = DataManager.Instance;
        var mapManager = MapManager.Instance;

        if (dataManager?.PlayerData == null || !dataManager.PlayerData.IsCurrentlyTraveling())
        {
            Logger.LogWarning("AboveCanvasTravelPathService: Not currently traveling", Logger.LogCategory.General);
            return;
        }

        var playerData = dataManager.PlayerData;

        // Debug log available data
        Logger.LogWarning($"AboveCanvasTravelPathService: Travel data - " +
            $"CurrentLocationId={playerData.CurrentLocationId ?? "null"}, " +
            $"TravelDestinationId={playerData.TravelDestinationId ?? "null"}, " +
            $"TravelOriginLocationId={playerData.TravelOriginLocationId ?? "null"}, " +
            $"TravelFinalDestinationId={playerData.TravelFinalDestinationId ?? "null"}, " +
            $"IsMultiSegmentTravel={playerData.IsMultiSegmentTravel}",
            Logger.LogCategory.General);

        // Get the travel path
        List<string> pathLocations = GetCurrentTravelPath(dataManager, mapManager);

        if (pathLocations != null && pathLocations.Count > 0)
        {
            Logger.LogWarning($"AboveCanvasTravelPathService: Building path with {pathLocations.Count} locations: {string.Join(" -> ", pathLocations)}", Logger.LogCategory.General);
            BuildTravelPath(pathLocations);
        }
        else
        {
            Logger.LogWarning("AboveCanvasTravelPathService: Could not determine travel path", Logger.LogCategory.General);
        }
    }

    /// <summary>
    /// Gets the current travel path from MapManager or reconstructs it
    /// </summary>
    private List<string> GetCurrentTravelPath(DataManager dataManager, MapManager mapManager)
    {
        var playerData = dataManager.PlayerData;

        // Try to get the full path if it's a multi-segment travel
        if (playerData.IsMultiSegmentTravel)
        {
            var originId = playerData.TravelOriginLocationId;
            var finalDestId = playerData.TravelFinalDestinationId;

            if (!string.IsNullOrEmpty(originId) && !string.IsNullOrEmpty(finalDestId) && mapManager?.PathfindingService != null)
            {
                var pathResult = mapManager.PathfindingService.FindPath(originId, finalDestId);
                if (pathResult != null && pathResult.IsReachable && pathResult.Path != null)
                {
                    return pathResult.Path;
                }
            }
        }

        // For simple travel: find origin and destination
        // Priority for origin: TravelOriginLocationId > CurrentLocationId
        // Priority for destination: TravelFinalDestinationId > TravelDestinationId
        string origin = null;
        string destination = null;

        // Get destination (should always be available during travel)
        destination = playerData.TravelFinalDestinationId;
        if (string.IsNullOrEmpty(destination))
        {
            destination = playerData.TravelDestinationId;
        }

        // Get origin - for simple travel, we might need to find it from the destination's connections
        origin = playerData.TravelOriginLocationId;
        if (string.IsNullOrEmpty(origin))
        {
            origin = playerData.CurrentLocationId;
        }

        // If origin is still null but we have destination, try to find connected location
        if (string.IsNullOrEmpty(origin) && !string.IsNullOrEmpty(destination) && mapManager?.LocationRegistry != null)
        {
            var destLocation = mapManager.LocationRegistry.GetLocationById(destination);
            if (destLocation?.Connections != null && destLocation.Connections.Count > 0)
            {
                // Use first connected location as fallback origin
                origin = destLocation.Connections[0].DestinationLocationID;
                Logger.LogWarning($"AboveCanvasTravelPathService: Using connected location as origin: {origin}", Logger.LogCategory.General);
            }
        }

        if (!string.IsNullOrEmpty(origin) && !string.IsNullOrEmpty(destination))
        {
            return new List<string> { origin, destination };
        }

        // Last resort: just show destination
        if (!string.IsNullOrEmpty(destination))
        {
            return new List<string> { destination };
        }

        return null;
    }

    /// <summary>
    /// Adds a location icon to the path
    /// </summary>
    private void AddLocationIcon(MapLocationDefinition location, bool isOrigin, bool isDestination)
    {
        if (manager.LocationIconPrefab == null)
        {
            Logger.LogWarning("AboveCanvasTravelPathService: LocationIconPrefab is null - assign it in inspector!", Logger.LogCategory.General);
            return;
        }

        if (manager.TravelPathContainer == null)
        {
            Logger.LogWarning("AboveCanvasTravelPathService: TravelPathContainer is null - assign it in inspector!", Logger.LogCategory.General);
            return;
        }

        Logger.LogInfo($"AboveCanvasTravelPathService: Instantiating location icon for {location.DisplayName}", Logger.LogCategory.General);

        var iconObj = Object.Instantiate(manager.LocationIconPrefab, manager.TravelPathContainer);
        instantiatedPathElements.Add(iconObj);

        // Ensure the instantiated object and all children are active
        iconObj.SetActive(true);
        SetAllChildrenActive(iconObj.transform);

        // Apply configured size using scale (to resize children proportionally)
        // and LayoutElement (so layout system uses the correct size)
        ApplySize(iconObj, manager.LocationIconSize);

        // Find the Image with no sprite assigned - that's the one meant to receive the location icon
        // (Background and Border have their sprites pre-assigned in the prefab)
        Image iconImage = FindEmptyImage(iconObj.transform);

        if (iconImage != null && location.GetIcon() != null)
        {
            iconImage.sprite = location.GetIcon();
            Logger.LogInfo($"AboveCanvasTravelPathService: Set sprite on {iconImage.gameObject.name}", Logger.LogCategory.General);
        }

        iconObj.name = $"PathIcon_{location.LocationID}";

        Logger.LogInfo($"AboveCanvasTravelPathService: Added location icon for {location.DisplayName}", Logger.LogCategory.General);
    }

    /// <summary>
    /// Recursively activates all children of a transform
    /// </summary>
    private void SetAllChildrenActive(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(true);
            SetAllChildrenActive(child);
        }
    }

    /// <summary>
    /// Finds an Image component with no sprite assigned (the slot for dynamic icon)
    /// </summary>
    private Image FindEmptyImage(Transform root)
    {
        var allImages = root.GetComponentsInChildren<Image>(true);
        foreach (var img in allImages)
        {
            if (img.sprite == null)
            {
                return img;
            }
        }
        // Fallback: return first image if none are empty
        return allImages.Length > 0 ? allImages[0] : null;
    }

    /// <summary>
    /// Adds an arrow separator between locations
    /// </summary>
    private void AddArrow()
    {
        if (manager.ArrowPrefab == null)
        {
            Logger.LogWarning("AboveCanvasTravelPathService: ArrowPrefab is null", Logger.LogCategory.General);
            return;
        }

        var arrowObj = Object.Instantiate(manager.ArrowPrefab, manager.TravelPathContainer);
        instantiatedPathElements.Add(arrowObj);
        arrowObj.name = "PathArrow";

        // Apply configured size using scale and LayoutElement
        ApplySize(arrowObj, manager.ArrowSize);
    }

    /// <summary>
    /// Applies a target size to an object by scaling it proportionally and adding a LayoutElement.
    /// This preserves child proportions (including 9-slice borders) while making layout work correctly.
    /// </summary>
    private void ApplySize(GameObject obj, Vector2 targetSize)
    {
        var rectTransform = obj.GetComponent<RectTransform>();
        if (rectTransform == null) return;

        // Reset anchors and pivot for proper layout group behavior
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        Vector2 originalSize = rectTransform.sizeDelta;

        // Avoid division by zero
        if (originalSize.x <= 0 || originalSize.y <= 0) return;

        // Calculate uniform scale factor (use the smaller ratio to maintain aspect ratio)
        float scaleX = targetSize.x / originalSize.x;
        float scaleY = targetSize.y / originalSize.y;
        float uniformScale = Mathf.Min(scaleX, scaleY);

        // Apply scale to resize everything proportionally (including children and 9-slice borders)
        rectTransform.localScale = new Vector3(uniformScale, uniformScale, 1f);

        // Set sizeDelta to target size (layout group will use this)
        rectTransform.sizeDelta = targetSize;

        // Add LayoutElement so the layout system uses the target size
        var layoutElement = obj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = obj.AddComponent<LayoutElement>();
        }
        layoutElement.preferredWidth = targetSize.x;
        layoutElement.preferredHeight = targetSize.y;
        layoutElement.minWidth = targetSize.x;
        layoutElement.minHeight = targetSize.y;
    }

    /// <summary>
    /// Clears all instantiated path elements
    /// </summary>
    public void ClearTravelPath()
    {
        foreach (var element in instantiatedPathElements)
        {
            if (element != null)
            {
                Object.Destroy(element);
            }
        }
        instantiatedPathElements.Clear();

        // Hide the container when cleared
        if (manager.TravelPathContainer != null)
        {
            manager.TravelPathContainer.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Checks if travel path is currently displayed
    /// </summary>
    public bool IsPathDisplayed()
    {
        return instantiatedPathElements.Count > 0;
    }
}
