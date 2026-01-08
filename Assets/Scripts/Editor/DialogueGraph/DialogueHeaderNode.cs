// Purpose: Header node representing a DialogueDefinition in the graph
// Filepath: Assets/Scripts/Editor/DialogueGraph/DialogueHeaderNode.cs
#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Special node that represents a DialogueDefinition entry point.
/// Shows dialogue name, priority, and conditions as a header for the dialogue tree.
/// </summary>
public class DialogueHeaderNode : Node
{
    public DialogueDefinition Dialogue { get; private set; }
    public string DialogueId => Dialogue?.DialogueID ?? "";

    // Output port connects to the first line of the dialogue
    public Port OutputPort { get; private set; }

    // UI Elements
    private Label nameLabel;
    private Label priorityLabel;
    private Label conditionsLabel;

    // Color for this dialogue group
    public Color GroupColor { get; private set; }

    // Events
    public event Action<DialogueHeaderNode> OnHeaderSelected;

    // Predefined colors for different dialogues
    private static readonly Color[] DialogueColors = new Color[]
    {
        new Color(0.4f, 0.2f, 0.5f),  // Purple
        new Color(0.2f, 0.4f, 0.5f),  // Teal
        new Color(0.5f, 0.3f, 0.2f),  // Brown
        new Color(0.2f, 0.5f, 0.3f),  // Green
        new Color(0.5f, 0.2f, 0.3f),  // Red
        new Color(0.3f, 0.3f, 0.5f),  // Blue
        new Color(0.5f, 0.4f, 0.2f),  // Gold
        new Color(0.3f, 0.5f, 0.5f),  // Cyan
    };

    public DialogueHeaderNode()
    {
        // Empty constructor
    }

    /// <summary>
    /// Initialize the header node with dialogue data
    /// </summary>
    public void Initialize(DialogueDefinition dialogue, int colorIndex)
    {
        Dialogue = dialogue;
        GroupColor = DialogueColors[colorIndex % DialogueColors.Length];

        // Set title
        title = "DIALOGUE";

        // Build UI
        BuildNodeUI();

        // Create output port
        CreatePorts();

        // Apply styling
        UpdateStyling();

        // Set capabilities
        capabilities |= Capabilities.Selectable | Capabilities.Movable;
        capabilities &= ~Capabilities.Deletable; // Can't delete dialogue headers

        RefreshExpandedState();
        RefreshPorts();
    }

    private void BuildNodeUI()
    {
        mainContainer.Clear();

        // Create main content
        var contentContainer = new VisualElement();
        contentContainer.style.paddingLeft = 10;
        contentContainer.style.paddingRight = 10;
        contentContainer.style.paddingTop = 8;
        contentContainer.style.paddingBottom = 8;

        // Dialogue name
        nameLabel = new Label(Dialogue?.GetDisplayName() ?? "Unknown");
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.fontSize = 14;
        nameLabel.style.color = Color.white;
        nameLabel.style.marginBottom = 4;
        contentContainer.Add(nameLabel);

        // Priority
        priorityLabel = new Label($"Priority: {Dialogue?.Priority ?? 0}");
        priorityLabel.style.fontSize = 11;
        priorityLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
        priorityLabel.style.marginBottom = 4;
        contentContainer.Add(priorityLabel);

        // Conditions summary
        string conditionText = Dialogue?.HasConditions == true
            ? Dialogue.GetConditionsSummary()
            : "No conditions (default)";

        conditionsLabel = new Label(conditionText);
        conditionsLabel.style.fontSize = 10;
        conditionsLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
        conditionsLabel.style.whiteSpace = WhiteSpace.Normal;
        conditionsLabel.style.maxWidth = 200;
        contentContainer.Add(conditionsLabel);

        // Line count info
        int lineCount = Dialogue?.Lines?.Count ?? 0;
        var lineCountLabel = new Label($"{lineCount} line(s)");
        lineCountLabel.style.fontSize = 10;
        lineCountLabel.style.color = new Color(0.6f, 0.8f, 0.6f);
        lineCountLabel.style.marginTop = 4;
        contentContainer.Add(lineCountLabel);

        mainContainer.Add(contentContainer);
    }

    private void CreatePorts()
    {
        outputContainer.Clear();

        // Create output port (connects to first line)
        OutputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(int));
        OutputPort.portName = "Start";
        OutputPort.portColor = GroupColor;
        outputContainer.Add(OutputPort);
    }

    private void UpdateStyling()
    {
        // Apply group color to background
        mainContainer.style.backgroundColor = GroupColor;

        // Add border
        style.borderTopWidth = 3;
        style.borderBottomWidth = 3;
        style.borderLeftWidth = 3;
        style.borderRightWidth = 3;
        style.borderTopColor = new Color(GroupColor.r * 1.3f, GroupColor.g * 1.3f, GroupColor.b * 1.3f);
        style.borderBottomColor = style.borderTopColor;
        style.borderLeftColor = style.borderTopColor;
        style.borderRightColor = style.borderTopColor;

        // Size
        style.minWidth = 220;
        style.maxWidth = 250;
    }

    public override void OnSelected()
    {
        base.OnSelected();
        OnHeaderSelected?.Invoke(this);
    }

    /// <summary>
    /// Refresh the display from data
    /// </summary>
    public void RefreshFromData()
    {
        if (Dialogue == null) return;

        nameLabel.text = Dialogue.GetDisplayName();
        priorityLabel.text = $"Priority: {Dialogue.Priority}";
        conditionsLabel.text = Dialogue.HasConditions
            ? Dialogue.GetConditionsSummary()
            : "No conditions (default)";
    }
}
#endif
