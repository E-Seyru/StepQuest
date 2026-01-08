// Purpose: Main window for the visual NPC dialogue graph editor
// Filepath: Assets/Scripts/Editor/DialogueGraph/DialogueGraphWindow.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Main editor window for visually editing all dialogues for an NPC.
/// Contains a GraphView showing all dialogue trees and an inspector panel for editing.
/// </summary>
public class DialogueGraphWindow : EditorWindow
{
    private NPCDefinition currentNPC;
    private DialogueGraphView graphView;
    private VisualElement inspectorPanel;
    private VisualElement inspectorContent;
    private Label statusLabel;
    private Label npcNameLabel;
    private bool isDirty;

    // Currently selected elements
    private DialogueNode selectedNode;
    private DialogueHeaderNode selectedHeader;

    [MenuItem("WalkAndRPG/Social/Dialogue Graph Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<DialogueGraphWindow>();
        window.titleContent = new GUIContent("Dialogue Graph");
        window.minSize = new Vector2(900, 600);
        window.Show();
    }

    /// <summary>
    /// Open the graph editor for a specific NPC
    /// </summary>
    public static void OpenForNPC(NPCDefinition npc)
    {
        var window = GetWindow<DialogueGraphWindow>();
        window.titleContent = new GUIContent($"Dialogues: {npc.GetDisplayName()}");
        window.minSize = new Vector2(900, 600);
        window.LoadNPC(npc);
        window.Show();
        window.Focus();
    }

    private void CreateGUI()
    {
        var root = rootVisualElement;
        root.style.flexDirection = FlexDirection.Column;

        // Create toolbar
        CreateToolbar(root);

        // Create main content area (split view)
        var mainContent = new VisualElement();
        mainContent.style.flexDirection = FlexDirection.Row;
        mainContent.style.flexGrow = 1;
        root.Add(mainContent);

        // Create graph view (left side, takes most space)
        CreateGraphView(mainContent);

        // Create inspector panel (right side)
        CreateInspectorPanel(mainContent);

        // Create status bar
        CreateStatusBar(root);

        // If we already have an NPC, reload it
        if (currentNPC != null)
        {
            LoadNPC(currentNPC);
        }
    }

    private void CreateToolbar(VisualElement root)
    {
        var toolbar = new Toolbar();
        toolbar.style.height = 25;

        // NPC name label
        npcNameLabel = new Label("No NPC Loaded");
        npcNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        npcNameLabel.style.paddingLeft = 8;
        npcNameLabel.style.paddingRight = 16;
        toolbar.Add(npcNameLabel);

        // Spacer
        var spacer = new VisualElement();
        spacer.style.flexGrow = 1;
        toolbar.Add(spacer);

        // Save button
        var saveButton = new ToolbarButton(OnSave);
        saveButton.text = "Save All";
        saveButton.style.width = 70;
        toolbar.Add(saveButton);

        // Separator
        toolbar.Add(new ToolbarSpacer());

        // Add Dialogue button
        var addDialogueButton = new ToolbarButton(OnAddDialogue);
        addDialogueButton.text = "+ Dialogue";
        addDialogueButton.style.width = 80;
        toolbar.Add(addDialogueButton);

        // Separator
        toolbar.Add(new ToolbarSpacer());

        // Auto Layout button
        var autoLayoutButton = new ToolbarButton(OnAutoLayout);
        autoLayoutButton.text = "Auto Layout";
        autoLayoutButton.style.width = 80;
        toolbar.Add(autoLayoutButton);

        // Frame All button
        var frameAllButton = new ToolbarButton(OnFrameAll);
        frameAllButton.text = "Frame All";
        frameAllButton.style.width = 70;
        toolbar.Add(frameAllButton);

        // Separator
        toolbar.Add(new ToolbarSpacer());

        // Validate button
        var validateButton = new ToolbarButton(OnValidate);
        validateButton.text = "Validate All";
        validateButton.style.width = 80;
        toolbar.Add(validateButton);

        root.Add(toolbar);
    }

    private void CreateGraphView(VisualElement parent)
    {
        graphView = new DialogueGraphView();
        graphView.style.flexGrow = 1;
        graphView.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);

        // Subscribe to events
        graphView.OnNodeSelected += OnNodeSelected;
        graphView.OnHeaderSelected += OnHeaderSelected;
        graphView.OnGraphModified += OnGraphModified;

        parent.Add(graphView);
    }

    private void CreateInspectorPanel(VisualElement parent)
    {
        inspectorPanel = new VisualElement();
        inspectorPanel.style.width = 300;
        inspectorPanel.style.minWidth = 250;
        inspectorPanel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        inspectorPanel.style.borderLeftWidth = 1;
        inspectorPanel.style.borderLeftColor = new Color(0.1f, 0.1f, 0.1f);

        // Inspector header
        var header = new Label("Inspector");
        header.style.unityFontStyleAndWeight = FontStyle.Bold;
        header.style.fontSize = 14;
        header.style.paddingLeft = 8;
        header.style.paddingTop = 8;
        header.style.paddingBottom = 8;
        header.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
        inspectorPanel.Add(header);

        // Scrollable content area
        var scrollView = new ScrollView(ScrollViewMode.Vertical);
        scrollView.style.flexGrow = 1;

        inspectorContent = new VisualElement();
        inspectorContent.style.paddingLeft = 8;
        inspectorContent.style.paddingRight = 8;
        inspectorContent.style.paddingTop = 8;

        var placeholder = new Label("Select a node to edit");
        placeholder.style.color = new Color(0.5f, 0.5f, 0.5f);
        inspectorContent.Add(placeholder);

        scrollView.Add(inspectorContent);
        inspectorPanel.Add(scrollView);

        parent.Add(inspectorPanel);
    }

    private void CreateStatusBar(VisualElement root)
    {
        var statusBar = new VisualElement();
        statusBar.style.height = 22;
        statusBar.style.flexDirection = FlexDirection.Row;
        statusBar.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
        statusBar.style.paddingLeft = 8;
        statusBar.style.alignItems = Align.Center;

        statusLabel = new Label("Ready");
        statusLabel.style.fontSize = 11;
        statusLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
        statusBar.Add(statusLabel);

        root.Add(statusBar);
    }

    /// <summary>
    /// Load an NPC and all their dialogues into the graph
    /// </summary>
    public void LoadNPC(NPCDefinition npc)
    {
        currentNPC = npc;

        if (npc == null)
        {
            npcNameLabel.text = "No NPC Loaded";
            statusLabel.text = "No NPC loaded";
            graphView?.ClearGraph();
            return;
        }

        npcNameLabel.text = npc.GetDisplayName();
        titleContent = new GUIContent($"Dialogues: {npc.GetDisplayName()}");

        graphView?.LoadNPC(npc);

        int dialogueCount = npc.Dialogues?.Count(d => d != null) ?? 0;
        int totalLines = npc.Dialogues?.Where(d => d != null).Sum(d => d.Lines?.Count ?? 0) ?? 0;
        statusLabel.text = $"{dialogueCount} dialogue(s), {totalLines} total lines";

        isDirty = false;
        ClearInspector();
    }

    private void OnNodeSelected(DialogueNode node)
    {
        selectedNode = node;
        selectedHeader = null;
        UpdateInspectorForNode();
    }

    private void OnHeaderSelected(DialogueHeaderNode header)
    {
        selectedNode = null;
        selectedHeader = header;
        UpdateInspectorForHeader();
    }

    private void OnGraphModified()
    {
        isDirty = true;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (currentNPC != null)
        {
            int dialogueCount = currentNPC.Dialogues?.Count(d => d != null) ?? 0;
            int totalLines = currentNPC.Dialogues?.Where(d => d != null).Sum(d => d.Lines?.Count ?? 0) ?? 0;
            statusLabel.text = $"{dialogueCount} dialogue(s), {totalLines} lines" + (isDirty ? " (modified)" : "");
        }
    }

    private void ClearInspector()
    {
        inspectorContent.Clear();
        var placeholder = new Label("Select a node or dialogue header to edit");
        placeholder.style.color = new Color(0.5f, 0.5f, 0.5f);
        inspectorContent.Add(placeholder);
        selectedNode = null;
        selectedHeader = null;
    }

    private void UpdateInspectorForHeader()
    {
        inspectorContent.Clear();

        if (selectedHeader == null || selectedHeader.Dialogue == null)
        {
            ClearInspector();
            return;
        }

        var dialogue = selectedHeader.Dialogue;

        // Dialogue header
        var headerLabel = new Label("DIALOGUE");
        headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        headerLabel.style.fontSize = 11;
        headerLabel.style.color = new Color(0.6f, 0.8f, 0.6f);
        headerLabel.style.marginBottom = 4;
        inspectorContent.Add(headerLabel);

        // Dialogue name
        var nameLabel = new Label(dialogue.GetDisplayName());
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.fontSize = 14;
        nameLabel.style.marginBottom = 8;
        inspectorContent.Add(nameLabel);

        // Dialogue ID
        AddTextField("Dialogue ID", dialogue.DialogueID, value =>
        {
            RecordUndo(dialogue, "Change Dialogue ID");
            dialogue.DialogueID = value;
            selectedHeader.RefreshFromData();
        });

        // Dialogue Name
        AddTextField("Display Name", dialogue.DialogueName, value =>
        {
            RecordUndo(dialogue, "Change Dialogue Name");
            dialogue.DialogueName = value;
            selectedHeader.RefreshFromData();
        });

        // Priority
        AddIntField("Priority", dialogue.Priority, value =>
        {
            RecordUndo(dialogue, "Change Priority");
            dialogue.Priority = value;
            selectedHeader.RefreshFromData();
        });

        AddSeparator();

        // Conditions info
        var conditionsLabel = new Label("Conditions");
        conditionsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        conditionsLabel.style.marginBottom = 4;
        inspectorContent.Add(conditionsLabel);

        if (dialogue.HasConditions)
        {
            var conditionsText = new Label(dialogue.GetConditionsSummary());
            conditionsText.style.fontSize = 11;
            conditionsText.style.whiteSpace = WhiteSpace.Normal;
            conditionsText.style.color = new Color(0.7f, 0.7f, 0.7f);
            inspectorContent.Add(conditionsText);
        }
        else
        {
            var noConditions = new Label("No conditions (always available)");
            noConditions.style.fontSize = 11;
            noConditions.style.color = new Color(0.5f, 0.5f, 0.5f);
            inspectorContent.Add(noConditions);
        }

        var editConditionsButton = new Button(() =>
        {
            Selection.activeObject = dialogue;
            EditorGUIUtility.PingObject(dialogue);
        });
        editConditionsButton.text = "Edit in Inspector";
        editConditionsButton.style.marginTop = 8;
        inspectorContent.Add(editConditionsButton);

        AddSeparator();

        // Add line button
        var addLineButton = new Button(() =>
        {
            var viewCenter = graphView.contentViewContainer.WorldToLocal(graphView.worldBound.center);
            graphView.AddNewLineToDialogue(dialogue, viewCenter);
        });
        addLineButton.text = "+ Add Line to this Dialogue";
        addLineButton.style.marginTop = 8;
        inspectorContent.Add(addLineButton);

        // Stats
        AddSeparator();
        var statsLabel = new Label($"Lines: {dialogue.Lines?.Count ?? 0}");
        statsLabel.style.fontSize = 11;
        statsLabel.style.color = new Color(0.6f, 0.6f, 0.6f);
        inspectorContent.Add(statsLabel);
    }

    private void UpdateInspectorForNode()
    {
        inspectorContent.Clear();

        if (selectedNode == null || selectedNode.Line == null)
        {
            ClearInspector();
            return;
        }

        var line = selectedNode.Line;
        var dialogue = graphView.GetDialogueById(selectedNode.DialogueId);

        // Dialogue context
        var contextLabel = new Label($"Dialogue: {dialogue?.GetDisplayName() ?? "Unknown"}");
        contextLabel.style.fontSize = 10;
        contextLabel.style.color = new Color(0.5f, 0.7f, 0.5f);
        contextLabel.style.marginBottom = 4;
        inspectorContent.Add(contextLabel);

        // Line index
        var indexLabel = new Label($"Line {selectedNode.LineIndex}");
        indexLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        indexLabel.style.fontSize = 13;
        indexLabel.style.marginBottom = 8;
        inspectorContent.Add(indexLabel);

        // Speaker field
        AddTextField("Speaker", line.Speaker, value =>
        {
            RecordUndo(dialogue, "Change Speaker");
            line.Speaker = value;
            selectedNode.RefreshFromData();
        });

        // Emotion dropdown
        AddEnumField("Emotion", line.Emotion, (NPCEmotion value) =>
        {
            RecordUndo(dialogue, "Change Emotion");
            line.Emotion = value;
            selectedNode.RefreshFromData();
        });

        // Text area
        AddTextArea("Text", line.Text, value =>
        {
            RecordUndo(dialogue, "Change Text");
            line.Text = value;
            selectedNode.RefreshFromData();
        });

        // Ends Dialogue toggle
        AddToggle("Ends Dialogue", line.EndsDialogue, value =>
        {
            RecordUndo(dialogue, "Change Ends Dialogue");
            line.EndsDialogue = value;
            selectedNode.RefreshFromData();
            graphView.LoadNPC(currentNPC);
        });

        // Show Reward toggle
        AddToggle("Show Reward", line.ShowReward, value =>
        {
            RecordUndo(dialogue, "Change Show Reward");
            line.ShowReward = value;
            selectedNode.RefreshFromData();
        });

        // Separator
        AddSeparator();

        // Choices section
        var choicesHeader = new Label("Choices");
        choicesHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
        choicesHeader.style.marginTop = 8;
        choicesHeader.style.marginBottom = 4;
        inspectorContent.Add(choicesHeader);

        if (line.Choices != null && line.Choices.Count > 0)
        {
            for (int i = 0; i < line.Choices.Count; i++)
            {
                int choiceIndex = i;
                var choice = line.Choices[i];
                AddChoiceEditor(dialogue, choice, choiceIndex);
            }
        }
        else
        {
            var noChoicesLabel = new Label("No choices (continues to next line)");
            noChoicesLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
            noChoicesLabel.style.fontSize = 11;
            inspectorContent.Add(noChoicesLabel);
        }

        // Add choice button
        var addChoiceButton = new Button(() =>
        {
            RecordUndo(dialogue, "Add Choice");
            if (line.Choices == null)
                line.Choices = new List<DialogueChoice>();

            line.Choices.Add(new DialogueChoice { ChoiceText = "New choice", NextLineIndex = -1 });
            selectedNode.RefreshFromData();
            UpdateInspectorForNode();
            graphView.LoadNPC(currentNPC);
        });
        addChoiceButton.text = "+ Add Choice";
        addChoiceButton.style.marginTop = 8;
        inspectorContent.Add(addChoiceButton);

        // Separator
        AddSeparator();

        // Delete line button
        var deleteButton = new Button(() =>
        {
            if (EditorUtility.DisplayDialog("Delete Line",
                $"Are you sure you want to delete Line {selectedNode.LineIndex}?\nThis cannot be undone.",
                "Delete", "Cancel"))
            {
                int indexToDelete = selectedNode.LineIndex;
                ClearInspector();
                graphView.DeleteLine(dialogue, indexToDelete);
            }
        });
        deleteButton.text = "Delete Line";
        deleteButton.style.marginTop = 16;
        deleteButton.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
        inspectorContent.Add(deleteButton);
    }

    private void AddChoiceEditor(DialogueDefinition dialogue, DialogueChoice choice, int choiceIndex)
    {
        var container = new VisualElement();
        container.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
        container.style.paddingLeft = 8;
        container.style.paddingRight = 8;
        container.style.paddingTop = 4;
        container.style.paddingBottom = 4;
        container.style.marginBottom = 4;
        container.style.borderTopLeftRadius = 4;
        container.style.borderTopRightRadius = 4;
        container.style.borderBottomLeftRadius = 4;
        container.style.borderBottomRightRadius = 4;

        // Choice header with index and remove button
        var header = new VisualElement();
        header.style.flexDirection = FlexDirection.Row;
        header.style.justifyContent = Justify.SpaceBetween;
        header.style.marginBottom = 4;

        var choiceLabel = new Label($"Choice {choiceIndex + 1}");
        choiceLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        choiceLabel.style.fontSize = 11;
        header.Add(choiceLabel);

        var removeButton = new Button(() =>
        {
            RecordUndo(dialogue, "Remove Choice");
            selectedNode.Line.Choices.RemoveAt(choiceIndex);
            selectedNode.RefreshFromData();
            UpdateInspectorForNode();
            graphView.LoadNPC(currentNPC);
        });
        removeButton.text = "X";
        removeButton.style.width = 20;
        removeButton.style.height = 18;
        removeButton.style.fontSize = 10;
        header.Add(removeButton);

        container.Add(header);

        // Choice text
        var textField = new TextField("Text");
        textField.value = choice.ChoiceText ?? "";
        textField.RegisterValueChangedCallback(evt =>
        {
            RecordUndo(dialogue, "Change Choice Text");
            choice.ChoiceText = evt.newValue;
            selectedNode.RefreshFromData();
        });
        container.Add(textField);

        // Next line index
        var nextLineField = new IntegerField("Next Line");
        nextLineField.value = choice.NextLineIndex;
        nextLineField.tooltip = "-1 = continue to next line";
        nextLineField.RegisterValueChangedCallback(evt =>
        {
            RecordUndo(dialogue, "Change Next Line");
            choice.NextLineIndex = evt.newValue;
            graphView.LoadNPC(currentNPC);
        });
        container.Add(nextLineField);

        // Flag to set
        var flagField = new TextField("Flag to Set");
        flagField.value = choice.FlagToSet ?? "";
        flagField.RegisterValueChangedCallback(evt =>
        {
            RecordUndo(dialogue, "Change Flag");
            choice.FlagToSet = evt.newValue;
            selectedNode.RefreshFromData();
        });
        container.Add(flagField);

        // Relationship change
        var relField = new IntegerField("Relationship Change");
        relField.value = choice.RelationshipChange;
        relField.RegisterValueChangedCallback(evt =>
        {
            RecordUndo(dialogue, "Change Relationship");
            choice.RelationshipChange = evt.newValue;
            selectedNode.RefreshFromData();
        });
        container.Add(relField);

        inspectorContent.Add(container);
    }

    private void AddTextField(string label, string value, System.Action<string> onChange)
    {
        var field = new TextField(label);
        field.value = value ?? "";
        field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
        field.style.marginBottom = 4;
        inspectorContent.Add(field);
    }

    private void AddIntField(string label, int value, System.Action<int> onChange)
    {
        var field = new IntegerField(label);
        field.value = value;
        field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
        field.style.marginBottom = 4;
        inspectorContent.Add(field);
    }

    private void AddTextArea(string label, string value, System.Action<string> onChange)
    {
        var labelElement = new Label(label);
        labelElement.style.marginBottom = 2;
        inspectorContent.Add(labelElement);

        var field = new TextField();
        field.value = value ?? "";
        field.multiline = true;
        field.style.height = 60;
        field.style.marginBottom = 4;
        field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
        inspectorContent.Add(field);
    }

    private void AddEnumField<T>(string label, T value, System.Action<T> onChange) where T : System.Enum
    {
        var field = new EnumField(label, value);
        field.RegisterValueChangedCallback(evt => onChange((T)evt.newValue));
        field.style.marginBottom = 4;
        inspectorContent.Add(field);
    }

    private void AddToggle(string label, bool value, System.Action<bool> onChange)
    {
        var field = new Toggle(label);
        field.value = value;
        field.RegisterValueChangedCallback(evt => onChange(evt.newValue));
        field.style.marginBottom = 4;
        inspectorContent.Add(field);
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

    private void RecordUndo(DialogueDefinition dialogue, string actionName)
    {
        if (dialogue != null)
        {
            Undo.RecordObject(dialogue, actionName);
            EditorUtility.SetDirty(dialogue);
            isDirty = true;
        }
    }

    // Toolbar button callbacks

    private void OnSave()
    {
        if (currentNPC == null) return;

        graphView.SaveAll();

        isDirty = false;
        UpdateStatus();
        statusLabel.text += " (saved)";

        Logger.LogInfo($"DialogueGraphWindow: Saved all dialogues for '{currentNPC.NPCName}'", Logger.LogCategory.General);
    }

    private void OnAddDialogue()
    {
        if (currentNPC == null) return;

        // Open the dialogue editor to create a new dialogue for this NPC
        DialogueEditorWindow.ShowWindow();
        EditorUtility.DisplayDialog("Add Dialogue",
            $"Use the 'Create Dialogue' tab in the Dialogue Editor to create a new dialogue for {currentNPC.NPCName}.\n\nSelect the NPC from the dropdown and the dialogue will be automatically assigned.",
            "OK");
    }

    private void OnAutoLayout()
    {
        if (graphView == null || currentNPC == null) return;

        graphView.AutoLayoutAll();
        statusLabel.text = "Auto layout applied";
    }

    private void OnFrameAll()
    {
        graphView?.FrameAll();
    }

    private void OnValidate()
    {
        if (currentNPC == null || currentNPC.Dialogues == null)
        {
            statusLabel.text = "No dialogues to validate";
            return;
        }

        var allIssues = new List<string>();

        foreach (var dialogue in currentNPC.Dialogues.Where(d => d != null))
        {
            var issues = DialogueGraphSaveLoad.ValidateDialogue(dialogue);
            foreach (var issue in issues)
            {
                allIssues.Add($"[{dialogue.GetDisplayName()}] {issue}");
            }
        }

        if (allIssues.Count == 0)
        {
            EditorUtility.DisplayDialog("Validation", "All dialogues are valid!", "OK");
            statusLabel.text = "Validation passed";
        }
        else
        {
            string message = $"Found {allIssues.Count} issue(s):\n\n";
            foreach (var issue in allIssues.Take(10))
            {
                message += $"- {issue}\n";
            }
            if (allIssues.Count > 10)
            {
                message += $"\n... and {allIssues.Count - 10} more";
            }
            EditorUtility.DisplayDialog("Validation Issues", message, "OK");
            statusLabel.text = $"{allIssues.Count} validation issue(s) found";
        }
    }

    private void OnDisable()
    {
        if (isDirty && currentNPC != null)
        {
            if (EditorUtility.DisplayDialog("Unsaved Changes",
                $"Dialogues for '{currentNPC.GetDisplayName()}' have unsaved changes. Save before closing?",
                "Save", "Discard"))
            {
                OnSave();
            }
        }
    }
}
#endif
