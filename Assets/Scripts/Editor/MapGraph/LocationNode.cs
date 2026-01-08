// Purpose: Custom GraphView node representing a map location
// Filepath: Assets/Scripts/Editor/MapGraph/LocationNode.cs
#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom GraphView node representing a MapLocationDefinition.
/// Displays location name, icon, and content badges (enemies, NPCs, activities).
/// </summary>
public class LocationNode : Node
{
    // Data
    public MapLocationDefinition Location { get; private set; }
    public string LocationId => Location?.LocationID ?? "";

    // Ports
    public Port InputPort { get; private set; }
    public Port OutputPort { get; private set; }

    // UI Elements
    private Label nameLabel;
    private Label idLabel;
    private VisualElement iconContainer;
    private VisualElement badgesContainer;

    // Colors for node states
    private static readonly Color DefaultColor = new Color(0.25f, 0.25f, 0.25f);
    private static readonly Color HasEnemiesColor = new Color(0.35f, 0.2f, 0.2f);
    private static readonly Color HasNPCsColor = new Color(0.2f, 0.25f, 0.35f);
    private static readonly Color StartingLocationColor = new Color(0.2f, 0.35f, 0.2f);

    // Badge colors
    private static readonly Color ConnectionBadgeColor = new Color(0.5f, 0.5f, 0.5f);
    private static readonly Color EnemyBadgeColor = new Color(0.8f, 0.3f, 0.3f);
    private static readonly Color NPCBadgeColor = new Color(0.3f, 0.5f, 0.8f);
    private static readonly Color ActivityBadgeColor = new Color(0.3f, 0.7f, 0.3f);

    // Events
    public event Action<LocationNode> OnNodeSelected;

    public LocationNode()
    {
        // Empty constructor for instantiation
    }

    /// <summary>
    /// Initialize the node with location data
    /// </summary>
    public void Initialize(MapLocationDefinition location)
    {
        Location = location;

        // Set title
        title = location?.DisplayName ?? "Unknown Location";

        // Create ports FIRST (before any UI modifications)
        CreatePorts();

        // Build node UI
        BuildNodeUI();

        // Apply styling based on location content
        UpdateStyling();

        // Set capabilities
        capabilities |= Capabilities.Selectable | Capabilities.Movable;

        // Refresh expanded state to show ports
        RefreshExpandedState();
        RefreshPorts();

        // Set initial position
        if (location != null && location.EditorPosition != Vector2.zero)
        {
            SetPosition(new Rect(location.EditorPosition, Vector2.zero));
        }
    }

    private void BuildNodeUI()
    {
        // Clear existing content
        mainContainer.Clear();
        extensionContainer.Clear();

        // Main content container
        var contentContainer = new VisualElement();
        contentContainer.style.paddingLeft = 8;
        contentContainer.style.paddingRight = 8;
        contentContainer.style.paddingTop = 4;
        contentContainer.style.paddingBottom = 8;
        contentContainer.style.minWidth = 180;

        // Header with icon and name
        var headerContainer = new VisualElement();
        headerContainer.style.flexDirection = FlexDirection.Row;
        headerContainer.style.alignItems = Align.Center;
        headerContainer.style.marginBottom = 4;

        // Icon container
        iconContainer = new VisualElement();
        iconContainer.style.width = 24;
        iconContainer.style.height = 24;
        iconContainer.style.marginRight = 8;
        iconContainer.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
        iconContainer.style.borderTopLeftRadius = 4;
        iconContainer.style.borderTopRightRadius = 4;
        iconContainer.style.borderBottomLeftRadius = 4;
        iconContainer.style.borderBottomRightRadius = 4;

        if (Location?.LocationIcon != null)
        {
            iconContainer.style.backgroundImage = new StyleBackground(Location.LocationIcon);
        }

        headerContainer.Add(iconContainer);

        // Name label
        nameLabel = new Label(Location?.DisplayName ?? "Unknown");
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.fontSize = 13;
        nameLabel.style.color = Color.white;
        headerContainer.Add(nameLabel);

        contentContainer.Add(headerContainer);

        // ID label
        idLabel = new Label($"ID: {Location?.LocationID ?? "unknown"}");
        idLabel.style.fontSize = 10;
        idLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
        idLabel.style.marginBottom = 8;
        contentContainer.Add(idLabel);

        // Badges container
        badgesContainer = new VisualElement();
        badgesContainer.style.flexDirection = FlexDirection.Row;
        badgesContainer.style.flexWrap = Wrap.Wrap;
        badgesContainer.style.marginTop = 4;

        AddBadges();
        contentContainer.Add(badgesContainer);

        mainContainer.Add(contentContainer);
    }

    private void AddBadges()
    {
        badgesContainer.Clear();

        if (Location == null) return;

        // Connection count badge
        int connectionCount = Location.Connections?.Count ?? 0;
        if (connectionCount > 0)
        {
            AddBadge($"= {connectionCount}", ConnectionBadgeColor, "Connections");
        }

        // Enemy count badge
        int enemyCount = Location.AvailableEnemies?.Count ?? 0;
        if (enemyCount > 0)
        {
            AddBadge($"X {enemyCount}", EnemyBadgeColor, "Enemies");
        }

        // NPC count badge
        int npcCount = Location.AvailableNPCs?.Count ?? 0;
        if (npcCount > 0)
        {
            AddBadge($"@ {npcCount}", NPCBadgeColor, "NPCs");
        }

        // Activity count badge
        int activityCount = Location.AvailableActivities?.Count ?? 0;
        if (activityCount > 0)
        {
            AddBadge($"* {activityCount}", ActivityBadgeColor, "Activities");
        }
    }

    private void AddBadge(string text, Color color, string tooltip)
    {
        var badge = new Label(text);
        badge.tooltip = tooltip;
        badge.style.backgroundColor = color;
        badge.style.color = Color.white;
        badge.style.fontSize = 9;
        badge.style.unityFontStyleAndWeight = FontStyle.Bold;
        badge.style.paddingLeft = 4;
        badge.style.paddingRight = 4;
        badge.style.paddingTop = 2;
        badge.style.paddingBottom = 2;
        badge.style.marginRight = 4;
        badge.style.marginBottom = 2;
        badge.style.borderTopLeftRadius = 3;
        badge.style.borderTopRightRadius = 3;
        badge.style.borderBottomLeftRadius = 3;
        badge.style.borderBottomRightRadius = 3;

        badgesContainer.Add(badge);
    }

    private void CreatePorts()
    {
        // Clear existing ports first
        inputContainer.Clear();
        outputContainer.Clear();

        // Create input port using InstantiatePort (recommended method)
        InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
        InputPort.portName = "In";
        InputPort.portColor = new Color(0.4f, 0.8f, 0.4f);
        inputContainer.Add(InputPort);

        // Create output port using InstantiatePort (recommended method)
        OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
        OutputPort.portName = "Out";
        OutputPort.portColor = new Color(0.8f, 0.8f, 0.4f);
        outputContainer.Add(OutputPort);
    }

    private void UpdateStyling()
    {
        if (Location == null) return;

        // Determine background color based on content
        Color bgColor = DefaultColor;

        // Priority: Starting location > Has enemies > Has NPCs
        if (Location.LocationID == "village_01")
        {
            bgColor = StartingLocationColor;
        }
        else if (Location.AvailableEnemies != null && Location.AvailableEnemies.Count > 0)
        {
            bgColor = HasEnemiesColor;
        }
        else if (Location.AvailableNPCs != null && Location.AvailableNPCs.Count > 0)
        {
            bgColor = HasNPCsColor;
        }

        // Apply to main container
        mainContainer.style.backgroundColor = bgColor;

        // Apply theme color to title container if available
        if (Location.LocationThemeColor != Color.white && Location.LocationThemeColor.a > 0)
        {
            titleContainer.style.backgroundColor = new Color(
                Location.LocationThemeColor.r * 0.5f,
                Location.LocationThemeColor.g * 0.5f,
                Location.LocationThemeColor.b * 0.5f,
                0.8f
            );
        }

        // Set minimum width for node (like DialogueNode)
        style.minWidth = 200;
    }

    /// <summary>
    /// Refresh the node's visual state from data
    /// </summary>
    public void RefreshFromData()
    {
        if (Location == null) return;

        nameLabel.text = Location.DisplayName;
        idLabel.text = $"ID: {Location.LocationID}";

        if (Location.LocationIcon != null)
        {
            iconContainer.style.backgroundImage = new StyleBackground(Location.LocationIcon);
        }

        AddBadges();
        UpdateStyling();
    }

    /// <summary>
    /// Get the current position of this node
    /// </summary>
    public Vector2 GetCurrentPosition()
    {
        return GetPosition().position;
    }

    /// <summary>
    /// Save the current position to the location's EditorPosition
    /// </summary>
    public void SavePosition()
    {
        if (Location != null)
        {
            Location.EditorPosition = GetPosition().position;
        }
    }

    /// <summary>
    /// Called when the node is selected - fires the OnNodeSelected event
    /// </summary>
    public override void OnSelected()
    {
        base.OnSelected();
        OnNodeSelected?.Invoke(this);
    }
}
#endif
