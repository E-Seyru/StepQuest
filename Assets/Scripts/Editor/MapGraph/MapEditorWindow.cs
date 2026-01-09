// Purpose: Main editor window for visual map editing
// Filepath: Assets/Scripts/Editor/MapGraph/MapEditorWindow.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Main editor window for visually editing the world map.
/// Displays locations as draggable nodes with connections and a side inspector panel.
/// </summary>
public class MapEditorWindow : EditorWindow
{
    // EditorPrefs keys
    private const string PREF_BG_OPACITY = "MapEditor_BackgroundOpacity";
    private const string PREF_BG_SCALE = "MapEditor_BackgroundScale";
    private const string PREF_BG_SHOW = "MapEditor_ShowBackground";
    private const string PREF_SHOW_LABELS = "MapEditor_ShowLabels";

    // Data
    private LocationRegistry locationRegistry;
    private MapGraphView graphView;

    // Inspector panel
    private VisualElement inspectorPanel;
    private ScrollView inspectorContent;

    // Selection state
    private LocationNode selectedNode;

    // UI Elements
    private Label statusLabel;
    private Label statsLabel;
    private Toggle showLabelsToggle;
    private Toggle showBackgroundToggle;
    private Slider opacitySlider;
    private Slider scaleSlider;

    [MenuItem("StepQuest/World/Map Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<MapEditorWindow>();
        window.titleContent = new GUIContent("Map Editor");
        window.minSize = new Vector2(800, 500);
        window.Show();
    }

    private void CreateGUI()
    {
        // Load registry
        locationRegistry = MapGraphUtility.LoadLocationRegistry();

        // Root container
        var root = rootVisualElement;
        root.style.flexGrow = 1;

        // Create toolbar
        CreateToolbar(root);

        // Create main content (split view)
        var mainContent = new VisualElement();
        mainContent.style.flexDirection = FlexDirection.Row;
        mainContent.style.flexGrow = 1;
        root.Add(mainContent);

        // Create graph view (left side)
        CreateGraphView(mainContent);

        // Create inspector panel (right side)
        CreateInspectorPanel(mainContent);

        // Create status bar
        CreateStatusBar(root);

        // Load data
        if (locationRegistry != null)
        {
            graphView.LoadRegistry(locationRegistry);
            graphView.LoadBackgroundImage(); // Load world map background

            // Apply saved background settings
            ApplySavedBackgroundSettings();

            UpdateStats();
        }
        else
        {
            SetStatus("No LocationRegistry found. Create one first.");
        }
    }

    private void CreateToolbar(VisualElement root)
    {
        var toolbar = new Toolbar();
        toolbar.style.height = 25;
        toolbar.style.borderBottomWidth = 1;
        toolbar.style.borderBottomColor = new Color(0.2f, 0.2f, 0.2f);

        // Save button
        var saveButton = new ToolbarButton(OnSave);
        saveButton.text = "Save";
        saveButton.tooltip = "Save all node positions";
        toolbar.Add(saveButton);

        toolbar.Add(new ToolbarSpacer());

        // Create POI button
        var createPOIButton = new ToolbarButton(OnCreatePOI);
        createPOIButton.text = "+ Create POI";
        createPOIButton.tooltip = "Create a new POI with a new location";
        createPOIButton.style.backgroundColor = new Color(0.2f, 0.5f, 0.3f);
        toolbar.Add(createPOIButton);

        toolbar.Add(new ToolbarSpacer());

        // Auto-Layout button
        var autoLayoutButton = new ToolbarButton(OnAutoLayout);
        autoLayoutButton.text = "Auto-Layout";
        autoLayoutButton.tooltip = "Automatically arrange nodes using force-directed layout";
        toolbar.Add(autoLayoutButton);

        // Frame All button
        var frameAllButton = new ToolbarButton(OnFrameAll);
        frameAllButton.text = "Frame All";
        frameAllButton.tooltip = "Zoom to fit all nodes in view";
        toolbar.Add(frameAllButton);

        toolbar.Add(new ToolbarSpacer());

        // Show Labels toggle
        showLabelsToggle = new Toggle("Labels");
        showLabelsToggle.value = EditorPrefs.GetBool(PREF_SHOW_LABELS, true);
        showLabelsToggle.tooltip = "Show/hide step cost labels on connections";
        showLabelsToggle.RegisterValueChangedCallback(evt =>
        {
            graphView?.SetShowLabels(evt.newValue);
            EditorPrefs.SetBool(PREF_SHOW_LABELS, evt.newValue);
        });
        toolbar.Add(showLabelsToggle);

        toolbar.Add(new ToolbarSpacer());

        // Background controls separator
        var bgLabel = new Label("| Background:");
        bgLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
        bgLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        bgLabel.style.paddingLeft = 4;
        bgLabel.style.paddingRight = 4;
        toolbar.Add(bgLabel);

        // Show Background toggle
        showBackgroundToggle = new Toggle("Show");
        showBackgroundToggle.value = EditorPrefs.GetBool(PREF_BG_SHOW, true);
        showBackgroundToggle.tooltip = "Show/hide world map background";
        showBackgroundToggle.RegisterValueChangedCallback(evt =>
        {
            graphView?.SetShowBackground(evt.newValue);
            opacitySlider.SetEnabled(evt.newValue);
            scaleSlider.SetEnabled(evt.newValue);
            EditorPrefs.SetBool(PREF_BG_SHOW, evt.newValue);
        });
        toolbar.Add(showBackgroundToggle);

        // Opacity slider
        var opacityContainer = new VisualElement();
        opacityContainer.style.flexDirection = FlexDirection.Row;
        opacityContainer.style.alignItems = Align.Center;

        var opacityLabel = new Label("Opacity:");
        opacityLabel.style.paddingLeft = 8;
        opacityLabel.style.paddingRight = 4;
        opacityContainer.Add(opacityLabel);

        opacitySlider = new Slider(0.1f, 1f);
        opacitySlider.value = EditorPrefs.GetFloat(PREF_BG_OPACITY, 0.5f);
        opacitySlider.style.width = 80;
        opacitySlider.tooltip = "Background image opacity";
        opacitySlider.RegisterValueChangedCallback(evt =>
        {
            graphView?.SetBackgroundOpacity(evt.newValue);
            EditorPrefs.SetFloat(PREF_BG_OPACITY, evt.newValue);
        });
        opacityContainer.Add(opacitySlider);
        toolbar.Add(opacityContainer);

        // Scale slider
        var scaleContainer = new VisualElement();
        scaleContainer.style.flexDirection = FlexDirection.Row;
        scaleContainer.style.alignItems = Align.Center;

        var scaleLabel = new Label("Scale:");
        scaleLabel.style.paddingLeft = 8;
        scaleLabel.style.paddingRight = 4;
        scaleContainer.Add(scaleLabel);

        scaleSlider = new Slider(0.2f, 3f);
        scaleSlider.value = EditorPrefs.GetFloat(PREF_BG_SCALE, 1f);
        scaleSlider.style.width = 80;
        scaleSlider.tooltip = "Background image scale";
        scaleSlider.RegisterValueChangedCallback(evt =>
        {
            graphView?.SetBackgroundScale(evt.newValue);
            EditorPrefs.SetFloat(PREF_BG_SCALE, evt.newValue);
        });
        scaleContainer.Add(scaleSlider);
        toolbar.Add(scaleContainer);

        toolbar.Add(new ToolbarSpacer() { style = { flexGrow = 1 } });

        // Stats label
        statsLabel = new Label("Loading...");
        statsLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
        statsLabel.style.unityTextAlign = TextAnchor.MiddleRight;
        statsLabel.style.paddingRight = 8;
        toolbar.Add(statsLabel);

        root.Add(toolbar);
    }

    private void CreateGraphView(VisualElement parent)
    {
        graphView = new MapGraphView();
        graphView.style.flexGrow = 1;

        // Subscribe to events
        graphView.OnGraphModified += OnGraphModified;
        graphView.OnNodeSelected += SelectNode; // Handle node click selection

        // Handle selection
        graphView.RegisterCallback<MouseDownEvent>(evt =>
        {
            // Clear selection when clicking on empty space
            if (evt.target == graphView)
            {
                ClearSelection();
            }
        });

        // Handle node selection via GraphView selection
        graphView.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
            {
                // Prevent node deletion (locations should be deleted via registry)
                evt.StopPropagation();
            }
        });

        // Track selection changes (for drag selection and multi-select)
        graphView.graphViewChanged += change =>
        {
            // Check for selection
            var selectedNodes = graphView.selection.OfType<LocationNode>().ToList();
            if (selectedNodes.Count == 1)
            {
                SelectNode(selectedNodes[0]);
            }
            else if (selectedNodes.Count == 0)
            {
                ClearSelection();
            }
            return change;
        };

        parent.Add(graphView);
    }

    private void CreateInspectorPanel(VisualElement parent)
    {
        inspectorPanel = new VisualElement();
        inspectorPanel.style.width = 320;
        inspectorPanel.style.borderLeftWidth = 1;
        inspectorPanel.style.borderLeftColor = new Color(0.2f, 0.2f, 0.2f);
        inspectorPanel.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);

        // Header
        var header = new Label("Inspector");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 14;
        header.style.paddingLeft = 10;
        header.style.paddingTop = 8;
        header.style.paddingBottom = 8;
        header.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
        inspectorPanel.Add(header);

        // Content scroll view
        inspectorContent = new ScrollView();
        inspectorContent.style.flexGrow = 1;
        inspectorContent.style.paddingLeft = 10;
        inspectorContent.style.paddingRight = 10;
        inspectorContent.style.paddingTop = 10;
        inspectorPanel.Add(inspectorContent);

        // Initial message
        ShowNoSelectionMessage();

        parent.Add(inspectorPanel);
    }

    private void CreateStatusBar(VisualElement root)
    {
        var statusBar = new VisualElement();
        statusBar.style.height = 22;
        statusBar.style.flexDirection = FlexDirection.Row;
        statusBar.style.alignItems = Align.Center;
        statusBar.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
        statusBar.style.borderTopWidth = 1;
        statusBar.style.borderTopColor = new Color(0.2f, 0.2f, 0.2f);
        statusBar.style.paddingLeft = 8;
        statusBar.style.paddingRight = 8;

        statusLabel = new Label("Ready");
        statusLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
        statusLabel.style.flexGrow = 1;
        statusBar.Add(statusLabel);

        root.Add(statusBar);
    }

    private void SelectNode(LocationNode node)
    {
        selectedNode = node;
        UpdateInspectorForLocation(node.Location);
    }

    private void ClearSelection()
    {
        selectedNode = null;
        ShowNoSelectionMessage();
    }

    private void ShowNoSelectionMessage()
    {
        inspectorContent.Clear();

        var message = new Label("Select a location node to view details");
        message.style.color = new Color(0.5f, 0.5f, 0.5f);
        message.style.whiteSpace = WhiteSpace.Normal;
        message.style.paddingTop = 20;
        inspectorContent.Add(message);
    }

    private void UpdateInspectorForLocation(MapLocationDefinition location)
    {
        inspectorContent.Clear();

        if (location == null)
        {
            ShowNoSelectionMessage();
            return;
        }

        // Location name
        var nameLabel = new Label(location.DisplayName);
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.fontSize = 16;
        nameLabel.style.marginBottom = 4;
        inspectorContent.Add(nameLabel);

        // Location ID
        var idLabel = new Label($"ID: {location.LocationID}");
        idLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
        idLabel.style.marginBottom = 12;
        inspectorContent.Add(idLabel);

        // Open in Inspector button
        var openButton = new Button(() =>
        {
            Selection.activeObject = location;
            EditorGUIUtility.PingObject(location);
        });
        openButton.text = "Open in Inspector";
        openButton.style.marginBottom = 8;
        inspectorContent.Add(openButton);

        // Delete POI button
        var deleteButton = new Button(() => ShowDeletePOIConfirmation(location));
        deleteButton.text = "Delete POI";
        deleteButton.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
        deleteButton.style.marginBottom = 12;
        deleteButton.tooltip = "Delete this POI, its connections, and the scene GameObject";
        inspectorContent.Add(deleteButton);

        // Separator
        AddSeparator();

        // Connections section
        AddConnectionsSection(location);

        // Separator
        AddSeparator();

        // Enemies section
        AddEnemiesSection(location);

        // Separator
        AddSeparator();

        // NPCs section
        AddNPCsSection(location);

        // Separator
        AddSeparator();

        // Activities section
        AddActivitiesSection(location);
    }

    private void AddSeparator()
    {
        var separator = new VisualElement();
        separator.style.height = 1;
        separator.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
        separator.style.marginTop = 8;
        separator.style.marginBottom = 8;
        inspectorContent.Add(separator);
    }

    private void AddConnectionsSection(MapLocationDefinition location)
    {
        var header = new Label($"Connections ({location.Connections?.Count ?? 0})");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.marginBottom = 4;
        inspectorContent.Add(header);

        if (location.Connections == null || location.Connections.Count == 0)
        {
            var empty = new Label("No connections");
            empty.style.color = new Color(0.5f, 0.5f, 0.5f);
            empty.style.marginLeft = 8;
            inspectorContent.Add(empty);
        }
        else
        {
            foreach (var conn in location.Connections.Where(c => c != null))
            {
                var connContainer = new VisualElement();
                connContainer.style.flexDirection = FlexDirection.Row;
                connContainer.style.alignItems = Align.Center;
                connContainer.style.marginLeft = 8;
                connContainer.style.marginBottom = 2;

                // Check if bidirectional
                bool isBidirectional = MapGraphUtility.IsBidirectional(locationRegistry, location.LocationID, conn.DestinationLocationID);
                string symbol = isBidirectional ? "=" : ">";

                var connLabel = new Label($"{symbol} {conn.DestinationLocationID} ({conn.StepCost} steps)");
                connLabel.style.flexGrow = 1;
                connContainer.Add(connLabel);

                var selectButton = new Button(() =>
                {
                    var node = graphView.GetNodeByLocationId(conn.DestinationLocationID);
                    if (node != null)
                    {
                        graphView.ClearSelection();
                        graphView.AddToSelection(node);
                        graphView.FrameSelection();
                    }
                });
                selectButton.text = "Go";
                selectButton.style.width = 30;
                connContainer.Add(selectButton);

                // Remove connection button
                var removeConnButton = new Button(() =>
                {
                    bool removeBoth = isBidirectional && EditorUtility.DisplayDialog(
                        "Remove Connection",
                        "This is a bidirectional connection. Remove both directions?",
                        "Remove Both", "Remove One-Way Only");

                    MapGraphUtility.RemoveConnection(location, conn.DestinationLocationID, removeBoth, locationRegistry);
                    graphView.RebuildConnections();
                    UpdateInspectorForLocation(location);
                    graphView.RefreshNode(location.LocationID);
                    graphView.MarkDirty();
                });
                removeConnButton.text = "X";
                removeConnButton.style.width = 20;
                removeConnButton.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
                removeConnButton.style.marginLeft = 4;
                connContainer.Add(removeConnButton);

                inspectorContent.Add(connContainer);
            }
        }

        // Add connection button with dropdown
        var addConnButton = new Button(() => ShowAddConnectionPopup(location));
        addConnButton.text = "+ Add Connection";
        addConnButton.style.marginTop = 4;
        addConnButton.style.marginLeft = 8;
        inspectorContent.Add(addConnButton);
    }

    private void ShowAddConnectionPopup(MapLocationDefinition fromLocation)
    {
        // Get all locations except the current one and already connected ones
        var existingConnections = new HashSet<string>(
            fromLocation.Connections?.Select(c => c.DestinationLocationID) ?? Enumerable.Empty<string>()
        );
        existingConnections.Add(fromLocation.LocationID); // Can't connect to self

        var availableLocations = locationRegistry.AllLocations
            .Where(l => l != null && !existingConnections.Contains(l.LocationID))
            .OrderBy(l => l.DisplayName)
            .ToList();

        if (availableLocations.Count == 0)
        {
            EditorUtility.DisplayDialog("No Available Locations",
                "All locations are already connected to this location.", "OK");
            return;
        }

        // Create a simple dropdown menu
        var menu = new GenericMenu();
        foreach (var targetLocation in availableLocations)
        {
            var target = targetLocation; // Capture for closure
            menu.AddItem(new GUIContent(target.DisplayName), false, () =>
            {
                // Show step cost dialog
                StepCostDialog.Show(fromLocation.DisplayName, target.DisplayName, (stepCost, bidirectional) =>
                {
                    MapGraphUtility.AddConnection(fromLocation, target.LocationID, stepCost, bidirectional, locationRegistry);
                    graphView.RebuildConnections();
                    UpdateInspectorForLocation(fromLocation);
                    graphView.RefreshNode(fromLocation.LocationID);
                    if (bidirectional)
                    {
                        graphView.RefreshNode(target.LocationID);
                    }
                    graphView.MarkDirty();
                    SetStatus($"Connected {fromLocation.LocationID} to {target.LocationID}");
                });
            });
        }
        menu.ShowAsContext();
    }

    private void AddEnemiesSection(MapLocationDefinition location)
    {
        var header = new Label($"Enemies ({location.AvailableEnemies?.Count ?? 0})");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.marginBottom = 4;
        inspectorContent.Add(header);

        if (location.AvailableEnemies == null || location.AvailableEnemies.Count == 0)
        {
            var empty = new Label("No enemies");
            empty.style.color = new Color(0.5f, 0.5f, 0.5f);
            empty.style.marginLeft = 8;
            inspectorContent.Add(empty);
        }
        else
        {
            foreach (var enemy in location.AvailableEnemies.Where(e => e?.EnemyReference != null))
            {
                var enemyContainer = new VisualElement();
                enemyContainer.style.flexDirection = FlexDirection.Row;
                enemyContainer.style.alignItems = Align.Center;
                enemyContainer.style.marginLeft = 8;
                enemyContainer.style.marginBottom = 2;

                string prefix = enemy.IsHidden ? "[H] " : "";
                var enemyLabel = new Label($"{prefix}{enemy.GetDisplayName()}");
                enemyLabel.style.flexGrow = 1;
                if (enemy.IsHidden)
                {
                    enemyLabel.style.color = new Color(0.6f, 0.4f, 1f);
                }
                enemyContainer.Add(enemyLabel);

                // Edit button - opens Enemy Manager
                var capturedEnemy = enemy; // Capture for closure
                var editButton = new Button(() =>
                {
                    EnemyManagerWindow.ShowWindowAndSelect(capturedEnemy.EnemyReference);
                });
                editButton.text = "E";
                editButton.tooltip = "Edit in Enemy Manager";
                editButton.style.width = 20;
                editButton.style.backgroundColor = new Color(0.3f, 0.4f, 0.5f);
                enemyContainer.Add(editButton);

                var removeButton = new Button(() =>
                {
                    location.AvailableEnemies.Remove(enemy);
                    EditorUtility.SetDirty(location);
                    UpdateInspectorForLocation(location);
                    graphView.RefreshNode(location.LocationID);
                });
                removeButton.text = "X";
                removeButton.tooltip = "Remove from location";
                removeButton.style.width = 20;
                removeButton.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
                removeButton.style.marginLeft = 2;
                enemyContainer.Add(removeButton);

                inspectorContent.Add(enemyContainer);
            }
        }

        // Add enemy button
        var addButton = new Button(() => ShowAddEnemyPopup(location));
        addButton.text = "+ Add Enemy";
        addButton.style.marginTop = 4;
        addButton.style.marginLeft = 8;
        inspectorContent.Add(addButton);
    }

    private void AddNPCsSection(MapLocationDefinition location)
    {
        var header = new Label($"NPCs ({location.AvailableNPCs?.Count ?? 0})");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.marginBottom = 4;
        inspectorContent.Add(header);

        if (location.AvailableNPCs == null || location.AvailableNPCs.Count == 0)
        {
            var empty = new Label("No NPCs");
            empty.style.color = new Color(0.5f, 0.5f, 0.5f);
            empty.style.marginLeft = 8;
            inspectorContent.Add(empty);
        }
        else
        {
            foreach (var npc in location.AvailableNPCs.Where(n => n?.NPCReference != null))
            {
                var npcContainer = new VisualElement();
                npcContainer.style.flexDirection = FlexDirection.Row;
                npcContainer.style.alignItems = Align.Center;
                npcContainer.style.marginLeft = 8;
                npcContainer.style.marginBottom = 2;

                string prefix = npc.IsHidden ? "[H] " : "";
                var npcLabel = new Label($"{prefix}{npc.GetDisplayName()}");
                npcLabel.style.flexGrow = 1;
                if (npc.IsHidden)
                {
                    npcLabel.style.color = new Color(0.6f, 0.4f, 1f);
                }
                npcContainer.Add(npcLabel);

                // Edit button - opens NPC Manager
                var capturedNPC = npc; // Capture for closure
                var editButton = new Button(() =>
                {
                    NPCManagerWindow.ShowWindowAndSelect(capturedNPC.NPCReference);
                });
                editButton.text = "E";
                editButton.tooltip = "Edit in NPC Manager";
                editButton.style.width = 20;
                editButton.style.backgroundColor = new Color(0.3f, 0.4f, 0.5f);
                npcContainer.Add(editButton);

                var removeButton = new Button(() =>
                {
                    location.AvailableNPCs.Remove(npc);
                    EditorUtility.SetDirty(location);
                    UpdateInspectorForLocation(location);
                    graphView.RefreshNode(location.LocationID);
                });
                removeButton.text = "X";
                removeButton.tooltip = "Remove from location";
                removeButton.style.width = 20;
                removeButton.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
                removeButton.style.marginLeft = 2;
                npcContainer.Add(removeButton);

                inspectorContent.Add(npcContainer);
            }
        }

        // Add NPC button
        var addButton = new Button(() => ShowAddNPCPopup(location));
        addButton.text = "+ Add NPC";
        addButton.style.marginTop = 4;
        addButton.style.marginLeft = 8;
        inspectorContent.Add(addButton);
    }

    private void AddActivitiesSection(MapLocationDefinition location)
    {
        var header = new Label($"Activities ({location.AvailableActivities?.Count ?? 0})");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.marginBottom = 4;
        inspectorContent.Add(header);

        if (location.AvailableActivities == null || location.AvailableActivities.Count == 0)
        {
            var empty = new Label("No activities");
            empty.style.color = new Color(0.5f, 0.5f, 0.5f);
            empty.style.marginLeft = 8;
            inspectorContent.Add(empty);
        }
        else
        {
            foreach (var activity in location.AvailableActivities.Where(a => a?.ActivityReference != null))
            {
                var actContainer = new VisualElement();
                actContainer.style.flexDirection = FlexDirection.Row;
                actContainer.style.alignItems = Align.Center;
                actContainer.style.marginLeft = 8;
                actContainer.style.marginBottom = 2;

                string prefix = activity.IsHidden ? "[H] " : "";
                var actLabel = new Label($"{prefix}{activity.GetDisplayName()}");
                actLabel.style.flexGrow = 1;
                if (activity.IsHidden)
                {
                    actLabel.style.color = new Color(0.6f, 0.4f, 1f);
                }
                actContainer.Add(actLabel);

                // Edit button - opens Activity Manager
                var capturedActivity = activity; // Capture for closure
                var editButton = new Button(() =>
                {
                    ActivityManagerWindow.ShowWindowAndSelect(capturedActivity.ActivityReference);
                });
                editButton.text = "E";
                editButton.tooltip = "Edit in Activity Manager";
                editButton.style.width = 20;
                editButton.style.backgroundColor = new Color(0.3f, 0.4f, 0.5f);
                actContainer.Add(editButton);

                var removeButton = new Button(() =>
                {
                    location.AvailableActivities.Remove(activity);
                    EditorUtility.SetDirty(location);
                    UpdateInspectorForLocation(location);
                    graphView.RefreshNode(location.LocationID);
                });
                removeButton.text = "X";
                removeButton.tooltip = "Remove from location";
                removeButton.style.width = 20;
                removeButton.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
                removeButton.style.marginLeft = 2;
                actContainer.Add(removeButton);

                inspectorContent.Add(actContainer);
            }
        }

        // Add activity button
        var addButton = new Button(() => ShowAddActivityPopup(location));
        addButton.text = "+ Add Activity";
        addButton.style.marginTop = 4;
        addButton.style.marginLeft = 8;
        inspectorContent.Add(addButton);
    }

    private void ShowAddActivityPopup(MapLocationDefinition location)
    {
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        EditorGUIUtility.ShowObjectPicker<ActivityDefinition>(null, false, "", controlID);

        EditorApplication.update += () =>
        {
            if (Event.current?.commandName == "ObjectSelectorUpdated")
            {
                var selected = EditorGUIUtility.GetObjectPickerObject() as ActivityDefinition;
                if (selected != null)
                {
                    if (location.AvailableActivities == null)
                        location.AvailableActivities = new List<LocationActivity>();

                    if (!location.AvailableActivities.Any(a => a?.ActivityReference == selected))
                    {
                        location.AvailableActivities.Add(new LocationActivity { ActivityReference = selected });
                        EditorUtility.SetDirty(location);
                        UpdateInspectorForLocation(location);
                        graphView.RefreshNode(location.LocationID);
                    }
                }
            }
        };
    }

    private void ShowAddEnemyPopup(MapLocationDefinition location)
    {
        // Use Unity's object picker
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        EditorGUIUtility.ShowObjectPicker<EnemyDefinition>(null, false, "", controlID);

        // We need to handle this in OnGUI since object picker is IMGUI-based
        EditorApplication.update += () =>
        {
            if (Event.current?.commandName == "ObjectSelectorUpdated")
            {
                var selected = EditorGUIUtility.GetObjectPickerObject() as EnemyDefinition;
                if (selected != null)
                {
                    if (location.AvailableEnemies == null)
                        location.AvailableEnemies = new List<LocationEnemy>();

                    // Check if already exists
                    if (!location.AvailableEnemies.Any(e => e?.EnemyReference == selected))
                    {
                        location.AvailableEnemies.Add(new LocationEnemy { EnemyReference = selected });
                        EditorUtility.SetDirty(location);
                        UpdateInspectorForLocation(location);
                        graphView.RefreshNode(location.LocationID);
                    }
                }
            }
        };
    }

    private void ShowAddNPCPopup(MapLocationDefinition location)
    {
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        EditorGUIUtility.ShowObjectPicker<NPCDefinition>(null, false, "", controlID);

        EditorApplication.update += () =>
        {
            if (Event.current?.commandName == "ObjectSelectorUpdated")
            {
                var selected = EditorGUIUtility.GetObjectPickerObject() as NPCDefinition;
                if (selected != null)
                {
                    if (location.AvailableNPCs == null)
                        location.AvailableNPCs = new List<LocationNPC>();

                    if (!location.AvailableNPCs.Any(n => n?.NPCReference == selected))
                    {
                        location.AvailableNPCs.Add(new LocationNPC { NPCReference = selected });
                        EditorUtility.SetDirty(location);
                        UpdateInspectorForLocation(location);
                        graphView.RefreshNode(location.LocationID);
                    }
                }
            }
        };
    }

    private void OnSave()
    {
        if (graphView != null)
        {
            graphView.SaveAllPositions();
            SetStatus("Saved all positions");
        }
    }

    private void OnAutoLayout()
    {
        if (graphView != null)
        {
            graphView.ApplyAutoLayout();
            SetStatus("Applied auto-layout");
        }
    }

    private void OnFrameAll()
    {
        if (graphView != null)
        {
            graphView.FrameAll();
        }
    }

    private void OnCreatePOI()
    {
        if (locationRegistry == null)
        {
            EditorUtility.DisplayDialog("No Registry", "No LocationRegistry found. Create one first.", "OK");
            return;
        }

        // Open the POI creation popup
        CreatePOIPopup.Show(locationRegistry, OnPOICreated);
    }

    private void OnPOICreated(string locationId)
    {
        // Reload the graph view to show the new node
        graphView.LoadRegistry(locationRegistry);
        UpdateStats();

        // Select and frame the new node
        var newNode = graphView.GetNodeByLocationId(locationId);
        if (newNode != null)
        {
            graphView.ClearSelection();
            graphView.AddToSelection(newNode);
            graphView.FrameSelection();
            SelectNode(newNode);
        }

        SetStatus($"Created POI '{locationId}'");
    }

    private void OnGraphModified()
    {
        UpdateStats();
        SetStatus("Modified *");
    }

    private void UpdateStats()
    {
        if (locationRegistry == null)
        {
            statsLabel.text = "No registry";
            return;
        }

        int locationCount = locationRegistry.AllLocations?.Count(l => l != null) ?? 0;
        int connectionCount = 0;

        if (locationRegistry.AllLocations != null)
        {
            foreach (var loc in locationRegistry.AllLocations.Where(l => l != null))
            {
                connectionCount += loc.Connections?.Count ?? 0;
            }
        }

        statsLabel.text = $"{locationCount} locations, {connectionCount} connections";
    }

    private void SetStatus(string message)
    {
        if (statusLabel != null)
        {
            statusLabel.text = message;
        }
    }

    private void ApplySavedBackgroundSettings()
    {
        if (graphView == null) return;

        // Apply saved values to the graph view
        float opacity = EditorPrefs.GetFloat(PREF_BG_OPACITY, 0.5f);
        float scale = EditorPrefs.GetFloat(PREF_BG_SCALE, 1f);
        bool showBg = EditorPrefs.GetBool(PREF_BG_SHOW, true);
        bool showLabels = EditorPrefs.GetBool(PREF_SHOW_LABELS, true);

        graphView.SetBackgroundOpacity(opacity);
        graphView.SetBackgroundScale(scale);
        graphView.SetShowBackground(showBg);
        graphView.SetShowLabels(showLabels);

        // Update slider enabled states
        opacitySlider?.SetEnabled(showBg);
        scaleSlider?.SetEnabled(showBg);
    }

    private void ShowDeletePOIConfirmation(MapLocationDefinition location)
    {
        if (location == null) return;

        // Count connections that will be removed
        int outgoingConnections = location.Connections?.Count ?? 0;
        int incomingConnections = 0;

        if (locationRegistry?.AllLocations != null)
        {
            foreach (var otherLoc in locationRegistry.AllLocations.Where(l => l != null && l != location))
            {
                if (otherLoc.Connections?.Any(c => c?.DestinationLocationID == location.LocationID) == true)
                {
                    incomingConnections++;
                }
            }
        }

        string message = $"Delete POI '{location.DisplayName}'?\n\n" +
                        $"This will:\n" +
                        $"- Remove {outgoingConnections} outgoing connection(s)\n" +
                        $"- Remove {incomingConnections} incoming connection(s)\n" +
                        $"- Delete the POI GameObject from the scene\n\n" +
                        $"Do you also want to delete the MapLocationDefinition asset?";

        int choice = EditorUtility.DisplayDialogComplex(
            "Delete POI",
            message,
            "Delete POI Only",      // Option 0 - keep the location asset
            "Cancel",               // Option 1 - cancel
            "Delete POI & Asset"    // Option 2 - delete everything including asset
        );

        if (choice == 1) return; // Cancel

        bool deleteAsset = (choice == 2);
        string locationId = location.LocationID;

        if (MapGraphUtility.DeletePOI(locationId, locationRegistry, deleteAsset))
        {
            // Clear selection since the node is gone
            ClearSelection();

            // Reload the graph view
            graphView.LoadRegistry(locationRegistry);
            UpdateStats();

            string assetInfo = deleteAsset ? " and its asset" : "";
            SetStatus($"Deleted POI '{locationId}'{assetInfo}");
        }
        else
        {
            EditorUtility.DisplayDialog("Delete Failed", $"Failed to delete POI '{locationId}'. Check the console for details.", "OK");
        }
    }

    private void OnDisable()
    {
        // Check for unsaved changes
        if (graphView != null && graphView.IsDirty)
        {
            if (EditorUtility.DisplayDialog("Unsaved Changes",
                "You have unsaved changes to node positions. Save before closing?",
                "Save", "Don't Save"))
            {
                graphView.SaveAllPositions();
            }
        }
    }
}
#endif
