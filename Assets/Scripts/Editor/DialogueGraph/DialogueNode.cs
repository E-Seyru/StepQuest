// Purpose: Custom GraphView node representing a DialogueLine
// Filepath: Assets/Scripts/Editor/DialogueGraph/DialogueNode.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom GraphView node representing a single DialogueLine.
/// Displays speaker, text preview, emotion, and connection ports.
/// </summary>
public class DialogueNode : Node
{
    // Data
    public int LineIndex { get; private set; }
    public DialogueLine Line { get; private set; }
    public string DialogueId { get; set; }

    // Ports
    public Port InputPort { get; private set; }
    public Port DefaultOutputPort { get; private set; }
    public List<Port> ChoiceOutputPorts { get; private set; } = new List<Port>();

    // UI Elements
    private Label speakerLabel;
    private Label textPreviewLabel;
    private Label emotionLabel;
    private VisualElement indicatorsContainer;
    private VisualElement choicesContainer;

    // Colors
    private static readonly Color DefaultColor = new Color(0.2f, 0.2f, 0.2f);
    private static readonly Color ChoiceColor = new Color(0.15f, 0.25f, 0.4f);
    private static readonly Color EndColor = new Color(0.15f, 0.35f, 0.2f);
    private static readonly Color RewardColor = new Color(0.4f, 0.35f, 0.15f);

    // Events
    public event Action<DialogueNode> OnNodeSelected;
    public event Action<DialogueNode> OnNodeModified;

    public DialogueNode()
    {
        // Empty constructor for instantiation
    }

    /// <summary>
    /// Initialize the node with dialogue line data
    /// </summary>
    public void Initialize(DialogueLine line, int index, string dialogueId)
    {
        Line = line;
        LineIndex = index;
        DialogueId = dialogueId;

        // Set title
        title = $"Line {index}";

        // Build node UI
        BuildNodeUI();

        // Create ports
        CreatePorts();

        // Apply styling based on line type
        UpdateStyling();

        // Set capabilities
        capabilities |= Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable;

        // Refresh expanded state
        RefreshExpandedState();
        RefreshPorts();
    }

    private void BuildNodeUI()
    {
        // Clear existing content
        mainContainer.Clear();
        extensionContainer.Clear();

        // Create header with speaker and emotion
        var headerContainer = new VisualElement();
        headerContainer.style.flexDirection = FlexDirection.Row;
        headerContainer.style.justifyContent = Justify.SpaceBetween;
        headerContainer.style.paddingLeft = 8;
        headerContainer.style.paddingRight = 8;
        headerContainer.style.paddingTop = 4;
        headerContainer.style.paddingBottom = 4;

        speakerLabel = new Label(Line?.Speaker ?? "Unknown");
        speakerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        speakerLabel.style.fontSize = 12;
        headerContainer.Add(speakerLabel);

        emotionLabel = new Label(GetEmotionDisplay());
        emotionLabel.style.fontSize = 10;
        emotionLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
        headerContainer.Add(emotionLabel);

        mainContainer.Add(headerContainer);

        // Create text preview
        var textContainer = new VisualElement();
        textContainer.style.paddingLeft = 8;
        textContainer.style.paddingRight = 8;
        textContainer.style.paddingBottom = 8;
        textContainer.style.maxWidth = 250;

        string textPreview = GetTextPreview();
        textPreviewLabel = new Label(textPreview);
        textPreviewLabel.style.whiteSpace = WhiteSpace.Normal;
        textPreviewLabel.style.fontSize = 11;
        textPreviewLabel.style.color = new Color(0.85f, 0.85f, 0.85f);
        textContainer.Add(textPreviewLabel);

        mainContainer.Add(textContainer);

        // Create indicators container
        indicatorsContainer = new VisualElement();
        indicatorsContainer.style.flexDirection = FlexDirection.Row;
        indicatorsContainer.style.flexWrap = Wrap.Wrap;
        indicatorsContainer.style.paddingLeft = 8;
        indicatorsContainer.style.paddingRight = 8;
        indicatorsContainer.style.paddingBottom = 4;

        AddIndicators();
        mainContainer.Add(indicatorsContainer);

        // Create choices container for choice port labels
        choicesContainer = new VisualElement();
        choicesContainer.style.paddingLeft = 8;
        choicesContainer.style.paddingRight = 8;
        extensionContainer.Add(choicesContainer);
    }

    private void CreatePorts()
    {
        // Clear existing ports
        inputContainer.Clear();
        outputContainer.Clear();
        ChoiceOutputPorts.Clear();

        // Create input port (always present)
        InputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(int));
        InputPort.portName = "";
        InputPort.portColor = new Color(0.4f, 0.8f, 0.4f);
        inputContainer.Add(InputPort);

        // Create output ports based on line type
        if (Line != null && Line.HasChoices)
        {
            // Create one output port per choice
            for (int i = 0; i < Line.Choices.Count; i++)
            {
                var choice = Line.Choices[i];
                var choicePort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(int));

                // Truncate choice text for port name
                string choiceText = choice.ChoiceText ?? "Choice";
                if (choiceText.Length > 25)
                    choiceText = choiceText.Substring(0, 22) + "...";

                choicePort.portName = choiceText;
                choicePort.portColor = new Color(0.6f, 0.6f, 1f);
                outputContainer.Add(choicePort);
                ChoiceOutputPorts.Add(choicePort);
            }
        }
        else if (Line == null || !Line.EndsDialogue)
        {
            // Create default "Next" output port
            DefaultOutputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(int));
            DefaultOutputPort.portName = "Next";
            DefaultOutputPort.portColor = new Color(0.8f, 0.8f, 0.4f);
            outputContainer.Add(DefaultOutputPort);
        }
    }

    private void AddIndicators()
    {
        indicatorsContainer.Clear();

        if (Line == null) return;

        // Ends dialogue indicator
        if (Line.EndsDialogue)
        {
            AddIndicatorBadge("END", new Color(0.2f, 0.6f, 0.3f));
        }

        // Has choices indicator
        if (Line.HasChoices)
        {
            AddIndicatorBadge($"{Line.Choices.Count} CHOICES", new Color(0.3f, 0.4f, 0.7f));
        }

        // Show reward indicator
        if (Line.ShowReward)
        {
            AddIndicatorBadge("REWARD", new Color(0.7f, 0.6f, 0.2f));
        }

        // Check for flag effects in choices
        bool hasFlags = false;
        bool hasRelationship = false;
        bool hasAbility = false;
        bool hasItems = false;

        if (Line.HasChoices)
        {
            foreach (var choice in Line.Choices)
            {
                if (!string.IsNullOrEmpty(choice.FlagToSet)) hasFlags = true;
                if (choice.RelationshipChange != 0) hasRelationship = true;
                if (!string.IsNullOrEmpty(choice.AbilityToGrant)) hasAbility = true;
                if (choice.ItemsToGrant != null && choice.ItemsToGrant.Count > 0) hasItems = true;
            }
        }

        if (hasFlags)
            AddIndicatorBadge("FLAG", new Color(0.5f, 0.3f, 0.6f));
        if (hasRelationship)
            AddIndicatorBadge("REL", new Color(0.7f, 0.3f, 0.4f));
        if (hasAbility)
            AddIndicatorBadge("ABL", new Color(0.3f, 0.6f, 0.7f));
        if (hasItems)
            AddIndicatorBadge("ITEM", new Color(0.6f, 0.5f, 0.3f));
    }

    private void AddIndicatorBadge(string text, Color color)
    {
        var badge = new Label(text);
        badge.style.fontSize = 9;
        badge.style.backgroundColor = color;
        badge.style.color = Color.white;
        badge.style.paddingLeft = 4;
        badge.style.paddingRight = 4;
        badge.style.paddingTop = 1;
        badge.style.paddingBottom = 1;
        badge.style.marginRight = 4;
        badge.style.marginBottom = 2;
        badge.style.borderTopLeftRadius = 3;
        badge.style.borderTopRightRadius = 3;
        badge.style.borderBottomLeftRadius = 3;
        badge.style.borderBottomRightRadius = 3;
        indicatorsContainer.Add(badge);
    }

    private void UpdateStyling()
    {
        Color nodeColor = DefaultColor;

        if (Line != null)
        {
            if (Line.EndsDialogue)
                nodeColor = EndColor;
            else if (Line.HasChoices)
                nodeColor = ChoiceColor;
            else if (Line.ShowReward)
                nodeColor = RewardColor;
        }

        // Apply background color
        mainContainer.style.backgroundColor = nodeColor;

        // Set minimum width
        style.minWidth = 200;
        style.maxWidth = 300;
    }

    private string GetTextPreview()
    {
        if (Line == null || string.IsNullOrEmpty(Line.Text))
            return "(No text)";

        string text = Line.Text;
        if (text.Length > 80)
            text = text.Substring(0, 77) + "...";

        return $"\"{text}\"";
    }

    private string GetEmotionDisplay()
    {
        if (Line == null) return "";
        return Line.Emotion.ToString();
    }

    /// <summary>
    /// Update the node display from its data
    /// </summary>
    public void RefreshFromData()
    {
        if (Line == null) return;

        title = $"Line {LineIndex}";
        speakerLabel.text = Line.Speaker ?? "Unknown";
        textPreviewLabel.text = GetTextPreview();
        emotionLabel.text = GetEmotionDisplay();

        AddIndicators();
        CreatePorts();
        UpdateStyling();

        RefreshExpandedState();
        RefreshPorts();
    }

    /// <summary>
    /// Update the line index (used when lines are reordered)
    /// </summary>
    public void UpdateLineIndex(int newIndex)
    {
        LineIndex = newIndex;
        title = $"Line {newIndex}";
    }

    /// <summary>
    /// Get the output port for a specific choice index
    /// </summary>
    public Port GetChoicePort(int choiceIndex)
    {
        if (choiceIndex >= 0 && choiceIndex < ChoiceOutputPorts.Count)
            return ChoiceOutputPorts[choiceIndex];
        return null;
    }

    /// <summary>
    /// Get the appropriate output port for this node
    /// </summary>
    public Port GetOutputPort(int choiceIndex = -1)
    {
        if (Line != null && Line.HasChoices && choiceIndex >= 0)
            return GetChoicePort(choiceIndex);
        return DefaultOutputPort;
    }

    public override void OnSelected()
    {
        base.OnSelected();
        OnNodeSelected?.Invoke(this);
    }
}
#endif
