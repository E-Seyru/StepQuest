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

    // Cache the final destination to handle cases where TravelFinalDestinationId is not preserved between segments
    private string cachedFinalDestination = null;

    public AboveCanvasTravelPathService(AboveCanvasManager manager)
    {
        this.manager = manager;
    }

    /// <summary>
    /// Resets the cached final destination. Call this when starting a completely new travel.
    /// </summary>
    public void ResetFinalDestinationCache()
    {
        cachedFinalDestination = null;
        Logger.LogInfo("AboveCanvasTravelPathService: Final destination cache reset", Logger.LogCategory.General);
    }

    /// <summary>
    /// Builds the travel path UI from a list of location IDs.
    /// Automatically collapses paths with 4+ locations to show [Start] -> [...] -> [End]
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

        // Get collapsed display path (max 3 elements, with ellipsis for 4+ locations)
        var displayPath = GetDisplayPath(locationIds);
        Logger.LogInfo($"AboveCanvasTravelPathService: Full path has {locationIds.Count} locations, display path has {displayPath.Count} elements", Logger.LogCategory.General);

        // Build path: Location -> Arrow -> Location -> Arrow -> ... -> Location
        for (int i = 0; i < displayPath.Count; i++)
        {
            string locationId = displayPath[i];

            // Check if this is the ellipsis marker
            if (locationId == ELLIPSIS_MARKER)
            {
                AddEllipsisIcon();
                Logger.LogInfo($"AboveCanvasTravelPathService: Added ellipsis icon at position {i + 1}/{displayPath.Count}", Logger.LogCategory.General);
            }
            else
            {
                // Add location icon
                var location = locationRegistry.GetLocationById(locationId);
                if (location != null)
                {
                    AddLocationIcon(location, i == 0, i == displayPath.Count - 1);
                    Logger.LogInfo($"AboveCanvasTravelPathService: Added icon {i + 1}/{displayPath.Count} for {location.DisplayName}", Logger.LogCategory.General);
                }
                else
                {
                    Logger.LogWarning($"AboveCanvasTravelPathService: Location {locationId} not found in registry!", Logger.LogCategory.General);
                }
            }

            // Add arrow between elements (not after the last one)
            if (i < displayPath.Count - 1)
            {
                AddArrow();
                Logger.LogInfo($"AboveCanvasTravelPathService: Added arrow after element {i + 1}", Logger.LogCategory.General);
            }
        }

        Logger.LogInfo($"AboveCanvasTravelPathService: Built travel path - Total elements in list: {instantiatedPathElements.Count}", Logger.LogCategory.General);
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
            // Clear cache when not traveling
            cachedFinalDestination = null;
            Logger.LogWarning("AboveCanvasTravelPathService: Not currently traveling", Logger.LogCategory.General);
            return;
        }

        var playerData = dataManager.PlayerData;

        // Determine the final destination for this travel
        // We need to be careful to distinguish between:
        // 1. A new travel starting (should use fresh data, not cache)
        // 2. A multi-segment travel continuation (should use cache if TravelFinalDestinationId is cleared)
        if (!string.IsNullOrEmpty(playerData.TravelFinalDestinationId))
        {
            // TravelFinalDestinationId is set - use it and update cache
            cachedFinalDestination = playerData.TravelFinalDestinationId;
        }
        else if (playerData.IsMultiSegmentTravel && !string.IsNullOrEmpty(cachedFinalDestination))
        {
            // Multi-segment travel with no TravelFinalDestinationId but we have a cache
            // This is likely a segment continuation - keep using the cache
            Logger.LogInfo($"AboveCanvasTravelPathService: Using cached final destination for multi-segment continuation", Logger.LogCategory.General);
        }
        else
        {
            // Simple travel or no valid cache - use TravelDestinationId
            // This handles new travels that aren't multi-segment
            cachedFinalDestination = playerData.TravelDestinationId;
        }

        // Debug log available data
        Logger.LogWarning($"AboveCanvasTravelPathService: Travel data - " +
            $"CurrentLocationId={playerData.CurrentLocationId ?? "null"}, " +
            $"TravelDestinationId={playerData.TravelDestinationId ?? "null"}, " +
            $"TravelOriginLocationId={playerData.TravelOriginLocationId ?? "null"}, " +
            $"TravelFinalDestinationId={playerData.TravelFinalDestinationId ?? "null"}, " +
            $"cachedFinalDestination={cachedFinalDestination ?? "null"}, " +
            $"IsMultiSegmentTravel={playerData.IsMultiSegmentTravel}",
            Logger.LogCategory.General);

        // Get the travel path using cached final destination
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
    /// Gets the REMAINING travel path from current position to final destination.
    /// Uses cachedFinalDestination which is set in BuildTravelPathFromCurrentTravel.
    /// </summary>
    private List<string> GetCurrentTravelPath(DataManager dataManager, MapManager mapManager)
    {
        var playerData = dataManager.PlayerData;

        // Get current position (where we are now in the journey)
        // TravelOriginLocationId is where we started the current segment
        string currentPosition = playerData.TravelOriginLocationId;
        if (string.IsNullOrEmpty(currentPosition))
        {
            currentPosition = playerData.CurrentLocationId;
        }

        // Use the cached final destination (set in BuildTravelPathFromCurrentTravel)
        // This ensures we always use the true final destination even if TravelFinalDestinationId gets cleared
        string finalDestination = cachedFinalDestination;

        if (string.IsNullOrEmpty(currentPosition) || string.IsNullOrEmpty(finalDestination))
        {
            Logger.LogWarning($"AboveCanvasTravelPathService: Missing travel data - current={currentPosition}, final={finalDestination}", Logger.LogCategory.General);

            // Fallback: just show destination
            if (!string.IsNullOrEmpty(finalDestination))
            {
                return new List<string> { finalDestination };
            }
            return null;
        }

        // If current position equals final destination, just return destination
        if (currentPosition == finalDestination)
        {
            return new List<string> { finalDestination };
        }

        // Try pathfinding to get remaining path
        if (mapManager?.PathfindingService != null)
        {
            var pathResult = mapManager.PathfindingService.FindPath(currentPosition, finalDestination);
            if (pathResult != null && pathResult.IsReachable && pathResult.Path != null && pathResult.Path.Count > 0)
            {
                Logger.LogInfo($"AboveCanvasTravelPathService: Remaining path has {pathResult.Path.Count} locations", Logger.LogCategory.General);
                return pathResult.Path;
            }
        }

        // Fallback: simple origin -> destination
        return new List<string> { currentPosition, finalDestination };
    }

    /// <summary>
    /// Constant to mark that an ellipsis should be displayed instead of a location
    /// </summary>
    private const string ELLIPSIS_MARKER = "__ELLIPSIS__";

    /// <summary>
    /// Collapses a path to max 3 display elements if it has 4+ locations.
    /// Returns: [Start, Middle, End] for 3 or fewer locations
    /// Returns: [Start, ELLIPSIS_MARKER, End] for 4+ locations
    /// </summary>
    private List<string> GetDisplayPath(List<string> fullPath)
    {
        if (fullPath == null || fullPath.Count == 0)
            return fullPath;

        // 1-3 locations: show all
        if (fullPath.Count <= 3)
            return fullPath;

        // 4+ locations: collapse to [Start, ..., End]
        return new List<string>
        {
            fullPath[0],
            ELLIPSIS_MARKER,
            fullPath[fullPath.Count - 1]
        };
    }

    /// <summary>
    /// Adds an ellipsis icon (three dots) to indicate skipped locations
    /// </summary>
    private void AddEllipsisIcon()
    {
        if (manager.EllipsisSprite == null)
        {
            Logger.LogWarning("AboveCanvasTravelPathService: EllipsisSprite is null - assign it in inspector!", Logger.LogCategory.General);
            return;
        }

        if (manager.LocationIconPrefab == null)
        {
            Logger.LogWarning("AboveCanvasTravelPathService: LocationIconPrefab is null - cannot create ellipsis icon!", Logger.LogCategory.General);
            return;
        }

        Logger.LogInfo("AboveCanvasTravelPathService: Adding ellipsis icon", Logger.LogCategory.General);

        var iconObj = Object.Instantiate(manager.LocationIconPrefab, manager.TravelPathContainer);
        instantiatedPathElements.Add(iconObj);

        // Ensure the instantiated object and all children are active
        iconObj.SetActive(true);
        SetAllChildrenActive(iconObj.transform);

        // Apply configured size
        ApplySize(iconObj, manager.LocationIconSize);

        // Find the Image with no sprite assigned and set the ellipsis sprite
        Image iconImage = FindEmptyImage(iconObj.transform);
        if (iconImage != null)
        {
            iconImage.sprite = manager.EllipsisSprite;
            Logger.LogInfo("AboveCanvasTravelPathService: Set ellipsis sprite", Logger.LogCategory.General);
        }

        iconObj.name = "PathIcon_Ellipsis";
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
    /// Applies a target size to an object and all its children proportionally.
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

        // Calculate scale factor
        float scaleX = targetSize.x / originalSize.x;
        float scaleY = targetSize.y / originalSize.y;
        float uniformScale = Mathf.Min(scaleX, scaleY);

        // Set parent to target size
        rectTransform.sizeDelta = targetSize;
        rectTransform.localScale = Vector3.one;

        // Scale all children's sizeDelta proportionally
        ScaleChildrenSizes(rectTransform, uniformScale);

        // Add LayoutElement for layout group
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
    /// Recursively scales all children's sizeDelta by the given factor.
    /// </summary>
    private void ScaleChildrenSizes(Transform parent, float scale)
    {
        foreach (Transform child in parent)
        {
            var childRect = child.GetComponent<RectTransform>();
            if (childRect != null)
            {
                childRect.sizeDelta *= scale;
                childRect.anchoredPosition *= scale;
            }
            // Recurse into grandchildren
            ScaleChildrenSizes(child, scale);
        }
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

    /// <summary>
    /// Builds a single centered activity icon in the path container (no arrows, no other icons)
    /// </summary>
    /// <param name="activityIcon">The sprite to display for the activity</param>
    public void BuildActivityPath(Sprite activityIcon)
    {
        // Clear any existing path elements
        ClearTravelPath();

        if (manager.TravelPathContainer == null)
        {
            Logger.LogWarning("AboveCanvasTravelPathService: TravelPathContainer is null", Logger.LogCategory.General);
            return;
        }

        if (activityIcon == null)
        {
            Logger.LogWarning("AboveCanvasTravelPathService: Activity icon is null", Logger.LogCategory.General);
            return;
        }

        // Show the container
        manager.TravelPathContainer.gameObject.SetActive(true);

        // Add single centered activity icon
        AddActivityIcon(activityIcon);

        Logger.LogInfo($"AboveCanvasTravelPathService: Built activity path with centered icon", Logger.LogCategory.General);
    }

    /// <summary>
    /// Adds a single activity icon to the path container (centered)
    /// </summary>
    private void AddActivityIcon(Sprite activityIcon)
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

        Logger.LogInfo($"AboveCanvasTravelPathService: Instantiating activity icon", Logger.LogCategory.General);

        var iconObj = Object.Instantiate(manager.LocationIconPrefab, manager.TravelPathContainer);
        instantiatedPathElements.Add(iconObj);

        // Ensure the instantiated object and all children are active
        iconObj.SetActive(true);
        SetAllChildrenActive(iconObj.transform);

        // Apply configured size using scale (to resize children proportionally)
        // and LayoutElement (so layout system uses the correct size)
        ApplySize(iconObj, manager.LocationIconSize);

        // Find the Image with no sprite assigned - that's the one meant to receive the icon
        // (Background and Border have their sprites pre-assigned in the prefab)
        Image iconImage = FindEmptyImage(iconObj.transform);

        if (iconImage != null)
        {
            iconImage.sprite = activityIcon;
            Logger.LogInfo($"AboveCanvasTravelPathService: Set activity sprite on {iconImage.gameObject.name}", Logger.LogCategory.General);
        }

        iconObj.name = "PathIcon_Activity";

        Logger.LogInfo($"AboveCanvasTravelPathService: Added activity icon", Logger.LogCategory.General);
    }
}
