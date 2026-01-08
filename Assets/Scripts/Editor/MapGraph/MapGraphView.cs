// Purpose: Custom GraphView for editing world map locations and connections
// Filepath: Assets/Scripts/Editor/MapGraph/MapGraphView.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom GraphView for displaying and editing map locations and their connections.
/// Supports drag-and-drop node positioning and visual connection management.
/// </summary>
public class MapGraphView : GraphView
{
    // Current registry being edited
    public LocationRegistry CurrentRegistry { get; private set; }

    // Node tracking
    private Dictionary<string, LocationNode> nodesByLocationId = new Dictionary<string, LocationNode>();

    // Connection layer for drawing lines
    private MapConnectionLayer connectionLayer;

    // Background image
    private VisualElement backgroundImageElement;
    private Texture2D backgroundTexture;
    private float backgroundOpacity = 0.5f;
    private bool showBackground = true;
    private Vector2 backgroundOffset = Vector2.zero;
    private float backgroundScale = 1f;

    // Default world map path
    private const string DEFAULT_WORLD_MAP_PATH = "Assets/Art/une_carte_d_un_monde_fantasy_2D_vu_du_dessus_Au_Nord_des_for_ts_au_sud_des_montagnes.png";

    // Events
    public event Action<LocationNode> OnNodeSelected;
    public event Action OnGraphModified;
    public event Action<LocationNode, LocationNode> OnConnectionRequested;

    // Layout constants
    private const float NODE_WIDTH = 200f;
    private const float NODE_HEIGHT = 100f;
    private const float HORIZONTAL_SPACING = 250f;
    private const float VERTICAL_SPACING = 150f;

    // Track dirty state
    private bool isDirty = false;
    public bool IsDirty => isDirty;

    public MapGraphView()
    {
        // Add grid background
        var gridBackground = new GridBackground();
        Insert(0, gridBackground);
        gridBackground.StretchToParentSize();

        // Add connection layer
        connectionLayer = new MapConnectionLayer();

        // Add manipulators
        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        // Set up graph view changed callback
        graphViewChanged = OnGraphViewChanged;

        // Refresh connections when view transforms (pan/zoom)
        viewTransformChanged += OnViewTransformChanged;

        // Add context menu
        RegisterCallback<ContextualMenuPopulateEvent>(BuildContextMenu);

        // Style
        style.flexGrow = 1;

        // Set default zoom
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

        // Add connection layer after first layout
        RegisterCallback<GeometryChangedEvent>(OnFirstLayout);
    }

    private void OnViewTransformChanged(GraphView graphView)
    {
        connectionLayer?.Refresh();
    }

    /// <summary>
    /// Load and display a background image for the map
    /// </summary>
    public void LoadBackgroundImage(string path = null)
    {
        path = path ?? DEFAULT_WORLD_MAP_PATH;
        backgroundTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

        if (backgroundTexture == null)
        {
            Logger.LogInfo($"Could not load background image from: {path}", Logger.LogCategory.EditorLog);
            return;
        }

        SetupBackgroundElement();
    }

    private void SetupBackgroundElement()
    {
        if (backgroundTexture == null) return;

        // Remove existing background if any
        if (backgroundImageElement != null && backgroundImageElement.parent != null)
        {
            backgroundImageElement.RemoveFromHierarchy();
        }

        // Create new background element
        backgroundImageElement = new VisualElement();
        backgroundImageElement.name = "background-image";
        backgroundImageElement.style.position = Position.Absolute;
        backgroundImageElement.style.backgroundImage = new StyleBackground(backgroundTexture);
        backgroundImageElement.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
        backgroundImageElement.pickingMode = PickingMode.Ignore; // Don't block mouse events

        UpdateBackgroundDisplay();

        // Insert after grid but before connection layer (index 1)
        if (contentViewContainer != null)
        {
            contentViewContainer.Insert(0, backgroundImageElement);
        }
    }

    private void UpdateBackgroundDisplay()
    {
        if (backgroundImageElement == null || backgroundTexture == null) return;

        float width = backgroundTexture.width * backgroundScale;
        float height = backgroundTexture.height * backgroundScale;

        backgroundImageElement.style.width = width;
        backgroundImageElement.style.height = height;
        backgroundImageElement.style.left = backgroundOffset.x;
        backgroundImageElement.style.top = backgroundOffset.y;
        backgroundImageElement.style.opacity = showBackground ? backgroundOpacity : 0f;
        backgroundImageElement.style.display = showBackground ? DisplayStyle.Flex : DisplayStyle.None;
    }

    /// <summary>
    /// Set the background image opacity (0-1)
    /// </summary>
    public void SetBackgroundOpacity(float opacity)
    {
        backgroundOpacity = Mathf.Clamp01(opacity);
        UpdateBackgroundDisplay();
    }

    /// <summary>
    /// Get current background opacity
    /// </summary>
    public float GetBackgroundOpacity()
    {
        return backgroundOpacity;
    }

    /// <summary>
    /// Toggle background visibility
    /// </summary>
    public void SetShowBackground(bool show)
    {
        showBackground = show;
        UpdateBackgroundDisplay();
    }

    /// <summary>
    /// Get background visibility state
    /// </summary>
    public bool GetShowBackground()
    {
        return showBackground;
    }

    /// <summary>
    /// Set background scale
    /// </summary>
    public void SetBackgroundScale(float scale)
    {
        backgroundScale = Mathf.Clamp(scale, 0.1f, 5f);
        UpdateBackgroundDisplay();
    }

    /// <summary>
    /// Get background scale
    /// </summary>
    public float GetBackgroundScale()
    {
        return backgroundScale;
    }

    /// <summary>
    /// Set background offset position
    /// </summary>
    public void SetBackgroundOffset(Vector2 offset)
    {
        backgroundOffset = offset;
        UpdateBackgroundDisplay();
    }

    /// <summary>
    /// Get background offset
    /// </summary>
    public Vector2 GetBackgroundOffset()
    {
        return backgroundOffset;
    }

    private void OnFirstLayout(GeometryChangedEvent evt)
    {
        UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);

        if (connectionLayer.parent == null)
        {
            Insert(1, connectionLayer);
            connectionLayer.SetContentContainer(contentViewContainer);

            // Subscribe to connection click events
            connectionLayer.OnConnectionClicked += OnConnectionLabelClicked;
        }
    }

    private void OnConnectionLabelClicked(string fromLocationId, string toLocationId, int currentStepCost, bool isBidirectional)
    {
        // Find the locations
        var fromLocation = CurrentRegistry?.GetLocationById(fromLocationId);
        var toLocation = CurrentRegistry?.GetLocationById(toLocationId);

        if (fromLocation == null || toLocation == null)
            return;

        // Show edit dialog
        EditConnectionDialog.Show(fromLocation.DisplayName, toLocation.DisplayName, currentStepCost, isBidirectional,
            (newStepCost) =>
            {
                // Update the connection step cost
                UpdateConnectionStepCost(fromLocation, toLocationId, newStepCost, isBidirectional);
                RebuildConnections();
                isDirty = true;
                OnGraphModified?.Invoke();

                Logger.LogInfo($"Updated connection: {fromLocationId} -> {toLocationId} to {newStepCost} steps", Logger.LogCategory.EditorLog);
            },
            () =>
            {
                // Delete the connection
                bool removeBoth = isBidirectional && EditorUtility.DisplayDialog(
                    "Remove Connection",
                    "This is a bidirectional connection. Remove both directions?",
                    "Remove Both", "Remove One-Way Only");

                MapGraphUtility.RemoveConnection(fromLocation, toLocationId, removeBoth, CurrentRegistry);
                RebuildConnections();
                isDirty = true;
                OnGraphModified?.Invoke();

                Logger.LogInfo($"Removed connection: {fromLocationId} -> {toLocationId}", Logger.LogCategory.EditorLog);
            });
    }

    private void UpdateConnectionStepCost(MapLocationDefinition fromLocation, string toLocationId, int newStepCost, bool isBidirectional)
    {
        // Update the forward connection
        if (fromLocation?.Connections != null)
        {
            var conn = fromLocation.Connections.FirstOrDefault(c => c?.DestinationLocationID == toLocationId);
            if (conn != null)
            {
                conn.StepCost = newStepCost;
                EditorUtility.SetDirty(fromLocation);
            }
        }

        // Update the reverse connection if bidirectional
        if (isBidirectional)
        {
            var toLocation = CurrentRegistry?.GetLocationById(toLocationId);
            if (toLocation?.Connections != null)
            {
                var reverseConn = toLocation.Connections.FirstOrDefault(c => c?.DestinationLocationID == fromLocation.LocationID);
                if (reverseConn != null)
                {
                    reverseConn.StepCost = newStepCost;
                    EditorUtility.SetDirty(toLocation);
                }
            }
        }

        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Load a location registry into the graph view
    /// </summary>
    public void LoadRegistry(LocationRegistry registry)
    {
        CurrentRegistry = registry;
        ClearGraph();

        if (registry == null || registry.AllLocations == null || registry.AllLocations.Count == 0)
            return;

        // Create nodes for each location
        var locations = registry.AllLocations.Where(l => l != null).ToList();

        for (int i = 0; i < locations.Count; i++)
        {
            var location = locations[i];
            CreateLocationNode(location, i);
        }

        // Apply auto-layout if positions are not set
        bool needsAutoLayout = locations.All(l => l.EditorPosition == Vector2.zero);
        if (needsAutoLayout)
        {
            ApplyAutoLayout();
        }

        // Rebuild connections after layout
        schedule.Execute(() =>
        {
            RebuildConnections();
            FrameAll();
        }).ExecuteLater(100);

        isDirty = false;
    }

    /// <summary>
    /// Create a node for a location
    /// </summary>
    private LocationNode CreateLocationNode(MapLocationDefinition location, int index)
    {
        var node = new LocationNode();
        node.Initialize(location);

        // Subscribe to node selection event
        node.OnNodeSelected += (selectedNode) => OnNodeSelected?.Invoke(selectedNode);

        // Set position
        if (location.EditorPosition != Vector2.zero)
        {
            node.SetPosition(new Rect(location.EditorPosition, Vector2.zero));
        }
        else
        {
            // Default grid position
            int col = index % 4;
            int row = index / 4;
            Vector2 defaultPos = new Vector2(col * HORIZONTAL_SPACING + 50, row * VERTICAL_SPACING + 50);
            node.SetPosition(new Rect(defaultPos, Vector2.zero));
        }

        // Track the node
        nodesByLocationId[location.LocationID] = node;

        // Add to graph
        AddElement(node);

        return node;
    }

    /// <summary>
    /// Clear the graph
    /// </summary>
    private void ClearGraph()
    {
        // Remove all nodes
        foreach (var node in nodesByLocationId.Values)
        {
            RemoveElement(node);
        }
        nodesByLocationId.Clear();

        // Clear connections
        connectionLayer?.ClearConnections();
    }

    /// <summary>
    /// Rebuild all visual connections between nodes
    /// </summary>
    public void RebuildConnections()
    {
        connectionLayer.ClearConnections();

        if (CurrentRegistry == null) return;

        // Build a lookup for bidirectional detection
        var connectionPairs = new Dictionary<string, bool>();

        foreach (var location in CurrentRegistry.AllLocations.Where(l => l != null))
        {
            if (location.Connections == null) continue;

            foreach (var connection in location.Connections.Where(c => c != null))
            {
                string destId = connection.DestinationLocationID;
                if (string.IsNullOrEmpty(destId)) continue;

                // Create pair key
                string pairKey = location.LocationID.CompareTo(destId) < 0
                    ? $"{location.LocationID}:{destId}"
                    : $"{destId}:{location.LocationID}";

                // Check if reverse connection exists (bidirectional)
                var destLocation = CurrentRegistry.GetLocationById(destId);
                bool isBidirectional = false;

                if (destLocation?.Connections != null)
                {
                    isBidirectional = destLocation.Connections.Any(c =>
                        c.DestinationLocationID == location.LocationID);
                }

                // Skip if we've already added this bidirectional pair
                if (isBidirectional && connectionPairs.ContainsKey(pairKey))
                    continue;

                connectionPairs[pairKey] = true;

                // Get nodes
                if (!nodesByLocationId.TryGetValue(location.LocationID, out var fromNode))
                    continue;
                if (!nodesByLocationId.TryGetValue(destId, out var toNode))
                    continue;

                // Add connection to layer
                connectionLayer.AddConnection(
                    fromNode,
                    toNode,
                    connection.StepCost,
                    isBidirectional,
                    connection.IsAvailable,
                    location.LocationID,
                    destId
                );
            }
        }

        connectionLayer.Refresh();
    }

    /// <summary>
    /// Handle graph view changes (node movement, edge creation, etc.)
    /// </summary>
    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        // Handle node movement
        if (change.movedElements != null)
        {
            foreach (var element in change.movedElements)
            {
                if (element is LocationNode locationNode)
                {
                    // Update the location's editor position
                    var pos = locationNode.GetPosition();
                    locationNode.Location.EditorPosition = pos.position;
                    isDirty = true;
                }
            }

            // Refresh connections after node movement
            schedule.Execute(() => connectionLayer?.Refresh()).ExecuteLater(10);
            OnGraphModified?.Invoke();
        }

        // Handle edge creation (drag-to-connect)
        if (change.edgesToCreate != null)
        {
            foreach (var edge in change.edgesToCreate)
            {
                var outputNode = edge.output?.node as LocationNode;
                var inputNode = edge.input?.node as LocationNode;

                if (outputNode != null && inputNode != null)
                {
                    // Schedule showing the dialog after this frame
                    var fromLoc = outputNode.Location;
                    var toLoc = inputNode.Location;

                    schedule.Execute(() =>
                    {
                        ShowConnectionDialog(fromLoc, toLoc);
                    }).ExecuteLater(10);
                }
            }

            // Don't actually create the GraphView edges - we use our custom connection layer
            change.edgesToCreate.Clear();
        }

        return change;
    }

    /// <summary>
    /// Show the step cost dialog for creating a new connection
    /// </summary>
    private void ShowConnectionDialog(MapLocationDefinition fromLocation, MapLocationDefinition toLocation)
    {
        if (fromLocation == null || toLocation == null)
            return;

        // Check if connection already exists
        if (MapGraphUtility.HasConnection(fromLocation, toLocation.LocationID))
        {
            EditorUtility.DisplayDialog("Connection Exists",
                $"A connection from '{fromLocation.DisplayName}' to '{toLocation.DisplayName}' already exists.",
                "OK");
            return;
        }

        StepCostDialog.Show(fromLocation.DisplayName, toLocation.DisplayName, (stepCost, bidirectional) =>
        {
            // Create the connection
            MapGraphUtility.AddConnection(fromLocation, toLocation.LocationID, stepCost, bidirectional, CurrentRegistry);

            // Refresh the visual connections
            RebuildConnections();
            isDirty = true;
            OnGraphModified?.Invoke();

            Logger.LogInfo($"Created connection: {fromLocation.LocationID} -> {toLocation.LocationID} ({stepCost} steps, bidirectional: {bidirectional})", Logger.LogCategory.EditorLog);
        });
    }

    /// <summary>
    /// Build context menu
    /// </summary>
    private void BuildContextMenu(ContextualMenuPopulateEvent evt)
    {
        evt.menu.AppendAction("Refresh Connections", _ => RebuildConnections());
        evt.menu.AppendAction("Auto Layout", _ => ApplyAutoLayout());
        evt.menu.AppendAction("Frame All", _ => FrameAll());
        evt.menu.AppendSeparator();
        evt.menu.AppendAction("Toggle Labels", _ =>
        {
            connectionLayer.SetShowLabels(!connectionLayer.GetShowLabels());
        });
    }

    /// <summary>
    /// Apply force-directed auto-layout to all nodes
    /// </summary>
    public void ApplyAutoLayout()
    {
        if (CurrentRegistry == null || nodesByLocationId.Count == 0)
            return;

        var positions = MapGraphUtility.CalculateAutoLayout(CurrentRegistry, NODE_WIDTH, NODE_HEIGHT);

        foreach (var kvp in positions)
        {
            if (nodesByLocationId.TryGetValue(kvp.Key, out var node))
            {
                node.SetPosition(new Rect(kvp.Value, Vector2.zero));
                node.Location.EditorPosition = kvp.Value;
            }
        }

        isDirty = true;
        OnGraphModified?.Invoke();

        schedule.Execute(() =>
        {
            RebuildConnections();
            FrameAll();
        }).ExecuteLater(50);
    }

    /// <summary>
    /// Save all node positions
    /// </summary>
    public void SaveAllPositions()
    {
        if (CurrentRegistry == null) return;

        foreach (var kvp in nodesByLocationId)
        {
            var node = kvp.Value;
            var location = node.Location;

            if (location != null)
            {
                var pos = node.GetPosition();
                location.EditorPosition = pos.position;
                EditorUtility.SetDirty(location);
            }
        }

        AssetDatabase.SaveAssets();
        isDirty = false;

        Logger.LogInfo("Saved all map node positions", Logger.LogCategory.EditorLog);
    }

    /// <summary>
    /// Get a node by location ID
    /// </summary>
    public LocationNode GetNodeByLocationId(string locationId)
    {
        nodesByLocationId.TryGetValue(locationId, out var node);
        return node;
    }

    /// <summary>
    /// Get all location nodes
    /// </summary>
    public IEnumerable<LocationNode> GetAllNodes()
    {
        return nodesByLocationId.Values;
    }

    /// <summary>
    /// Toggle showing connection labels
    /// </summary>
    public void SetShowLabels(bool show)
    {
        connectionLayer?.SetShowLabels(show);
    }

    /// <summary>
    /// Get current label visibility state
    /// </summary>
    public bool GetShowLabels()
    {
        return connectionLayer?.GetShowLabels() ?? true;
    }

    /// <summary>
    /// Override to handle port compatibility for edge creation
    /// </summary>
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();

        foreach (var port in ports)
        {
            // Don't connect to self
            if (port.node == startPort.node)
                continue;

            // Don't connect same direction (input to input, output to output)
            if (port.direction == startPort.direction)
                continue;

            compatiblePorts.Add(port);
        }

        return compatiblePorts;
    }

    /// <summary>
    /// Refresh a specific node
    /// </summary>
    public void RefreshNode(string locationId)
    {
        if (nodesByLocationId.TryGetValue(locationId, out var node))
        {
            node.RefreshFromData();
        }
    }

    /// <summary>
    /// Mark the graph as dirty
    /// </summary>
    public void MarkDirty()
    {
        isDirty = true;
        OnGraphModified?.Invoke();
    }

    /// <summary>
    /// Clear dirty flag
    /// </summary>
    public void ClearDirty()
    {
        isDirty = false;
    }
}
#endif
