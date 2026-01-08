// Purpose: Custom GraphView for editing NPC dialogue trees
// Filepath: Assets/Scripts/Editor/DialogueGraph/DialogueGraphView.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom GraphView for displaying and editing all dialogues for an NPC.
/// Shows each dialogue as a separate tree with a header node.
/// </summary>
public class DialogueGraphView : GraphView
{
    // Current NPC being edited
    public NPCDefinition CurrentNPC { get; private set; }

    // Track all dialogues
    private List<DialogueDefinition> loadedDialogues = new List<DialogueDefinition>();

    // Node tracking - keyed by "dialogueId:lineIndex"
    private Dictionary<string, DialogueNode> nodesByKey = new Dictionary<string, DialogueNode>();
    private Dictionary<string, DialogueHeaderNode> headersByDialogueId = new Dictionary<string, DialogueHeaderNode>();

    // Connection layer for drawing lines
    private DialogueConnectionLayer connectionLayer;

    // Currently selected dialogue (for adding lines)
    public DialogueDefinition SelectedDialogue { get; private set; }

    // Events
    public event Action<DialogueNode> OnNodeSelected;
    public event Action<DialogueHeaderNode> OnHeaderSelected;
    public event Action OnGraphModified;

    // Layout constants
    private const float NODE_WIDTH = 250f;
    private const float NODE_HEIGHT = 150f;
    private const float HORIZONTAL_SPACING = 150f;
    private const float VERTICAL_SPACING = 80f;
    private const float DIALOGUE_GROUP_SPACING = 300f;

    public DialogueGraphView()
    {
        // Add grid background
        var gridBackground = new GridBackground();
        Insert(0, gridBackground);
        gridBackground.StretchToParentSize();

        // Add connection layer (will be added to contentViewContainer later)
        connectionLayer = new DialogueConnectionLayer();

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
        // Refresh connection drawing when panning/zooming
        connectionLayer?.Refresh();
    }

    private void OnFirstLayout(GeometryChangedEvent evt)
    {
        UnregisterCallback<GeometryChangedEvent>(OnFirstLayout);

        // Add connection layer to the GraphView (not contentViewContainer)
        // so it can draw over the entire view using proper coordinates
        if (connectionLayer.parent == null)
        {
            // Add after grid but before content
            Insert(1, connectionLayer);
            connectionLayer.SetContentContainer(contentViewContainer);
        }
    }

    /// <summary>
    /// Load all dialogues for an NPC into the graph view
    /// </summary>
    public void LoadNPC(NPCDefinition npc)
    {
        CurrentNPC = npc;
        ClearGraph();

        if (npc == null || npc.Dialogues == null || npc.Dialogues.Count == 0)
            return;

        loadedDialogues = npc.Dialogues.Where(d => d != null).OrderByDescending(d => d.Priority).ToList();

        float currentY = 0;

        for (int dialogueIndex = 0; dialogueIndex < loadedDialogues.Count; dialogueIndex++)
        {
            var dialogue = loadedDialogues[dialogueIndex];

            // Create header node for this dialogue
            var headerNode = CreateHeaderNode(dialogue, dialogueIndex, new Vector2(0, currentY));

            // Create line nodes for this dialogue
            float dialogueHeight = CreateDialogueNodes(dialogue, dialogueIndex, currentY + NODE_HEIGHT + VERTICAL_SPACING);

            // Move to next dialogue group
            currentY += Math.Max(dialogueHeight, NODE_HEIGHT) + DIALOGUE_GROUP_SPACING;
        }

        // Rebuild connections after layout
        schedule.Execute(() =>
        {
            RebuildConnections();
            FrameAll();
        }).ExecuteLater(100);
    }

    /// <summary>
    /// Rebuild all visual connections between nodes
    /// </summary>
    private void RebuildConnections()
    {
        connectionLayer.ClearConnections();

        foreach (var dialogue in loadedDialogues)
        {
            if (dialogue.Lines == null) continue;

            int colorIndex = loadedDialogues.IndexOf(dialogue);
            Color lineColor = GetDialogueColor(colorIndex);
            // Brighten the color
            lineColor.r = Mathf.Min(1f, lineColor.r + 0.4f);
            lineColor.g = Mathf.Min(1f, lineColor.g + 0.4f);
            lineColor.b = Mathf.Min(1f, lineColor.b + 0.4f);

            // Connection from header to first line
            if (dialogue.Lines.Count > 0)
            {
                if (headersByDialogueId.TryGetValue(dialogue.DialogueID, out var header))
                {
                    string firstLineKey = GetNodeKey(dialogue.DialogueID, 0);
                    if (nodesByKey.TryGetValue(firstLineKey, out var firstLineNode))
                    {
                        connectionLayer.AddConnection(header, firstLineNode, lineColor);
                    }
                }
            }

            // Connections between lines
            for (int i = 0; i < dialogue.Lines.Count; i++)
            {
                var line = dialogue.Lines[i];
                string sourceKey = GetNodeKey(dialogue.DialogueID, i);

                if (!nodesByKey.TryGetValue(sourceKey, out var sourceNode))
                    continue;

                if (line.HasChoices)
                {
                    // Connect to each choice target
                    for (int c = 0; c < line.Choices.Count; c++)
                    {
                        var choice = line.Choices[c];
                        int targetIndex = choice.NextLineIndex >= 0 ? choice.NextLineIndex : i + 1;

                        string targetKey = GetNodeKey(dialogue.DialogueID, targetIndex);
                        if (nodesByKey.TryGetValue(targetKey, out var targetNode))
                        {
                            connectionLayer.AddConnection(sourceNode, targetNode, lineColor);
                        }
                    }
                }
                else if (!line.EndsDialogue && i + 1 < dialogue.Lines.Count)
                {
                    // Connect to next line
                    string targetKey = GetNodeKey(dialogue.DialogueID, i + 1);
                    if (nodesByKey.TryGetValue(targetKey, out var targetNode))
                    {
                        connectionLayer.AddConnection(sourceNode, targetNode, lineColor);
                    }
                }
            }
        }

        connectionLayer.Refresh();
    }

    /// <summary>
    /// Create a header node for a dialogue
    /// </summary>
    private DialogueHeaderNode CreateHeaderNode(DialogueDefinition dialogue, int colorIndex, Vector2 position)
    {
        var headerNode = new DialogueHeaderNode();
        headerNode.Initialize(dialogue, colorIndex);
        headerNode.SetPosition(new Rect(position, Vector2.zero));

        headerNode.OnHeaderSelected += HandleHeaderSelected;

        AddElement(headerNode);
        headersByDialogueId[dialogue.DialogueID] = headerNode;

        return headerNode;
    }

    /// <summary>
    /// Create all line nodes for a dialogue, returns the total height used
    /// </summary>
    private float CreateDialogueNodes(DialogueDefinition dialogue, int colorIndex, float startY)
    {
        if (dialogue.Lines == null || dialogue.Lines.Count == 0)
            return 0;

        var positions = CalculateTreeLayoutForDialogue(dialogue, startY);
        float maxY = startY;

        for (int i = 0; i < dialogue.Lines.Count; i++)
        {
            var line = dialogue.Lines[i];
            Vector2 position = positions.ContainsKey(i)
                ? positions[i]
                : new Vector2(NODE_WIDTH + HORIZONTAL_SPACING, startY + i * (NODE_HEIGHT + VERTICAL_SPACING));

            var node = CreateDialogueNode(dialogue, line, i, position, colorIndex);

            if (position.y + NODE_HEIGHT > maxY)
                maxY = position.y + NODE_HEIGHT;
        }

        return maxY - startY;
    }

    /// <summary>
    /// Create a dialogue node
    /// </summary>
    public DialogueNode CreateDialogueNode(DialogueDefinition dialogue, DialogueLine line, int index, Vector2 position, int colorIndex)
    {
        var node = new DialogueNode();
        node.Initialize(line, index, dialogue.DialogueID);
        node.SetPosition(new Rect(position, Vector2.zero));

        // Apply group color tint
        var groupColor = GetDialogueColor(colorIndex);
        node.style.borderLeftWidth = 3;
        node.style.borderLeftColor = groupColor;

        node.OnNodeSelected += HandleNodeSelected;

        AddElement(node);

        string key = GetNodeKey(dialogue.DialogueID, index);
        nodesByKey[key] = node;

        return node;
    }

    /// <summary>
    /// Calculate tree layout positions for a single dialogue
    /// </summary>
    private Dictionary<int, Vector2> CalculateTreeLayoutForDialogue(DialogueDefinition dialogue, float startY)
    {
        var positions = new Dictionary<int, Vector2>();
        var visited = new HashSet<int>();
        var depths = new Dictionary<int, int>();

        // Calculate depths from line 0
        CalculateDepths(dialogue, 0, 0, depths, visited);

        // Handle orphaned nodes
        for (int i = 0; i < dialogue.Lines.Count; i++)
        {
            if (!depths.ContainsKey(i))
                depths[i] = 0;
        }

        // Group by depth
        var nodesByDepth = depths
            .GroupBy(kvp => kvp.Value)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Key).OrderBy(idx => idx).ToList());

        // Calculate positions (offset X to leave room for header)
        foreach (var depthGroup in nodesByDepth)
        {
            int depth = depthGroup.Key;
            var nodesAtDepth = depthGroup.Value;

            // +1 to depth to leave room for header
            float x = (depth + 1) * (NODE_WIDTH + HORIZONTAL_SPACING);

            for (int i = 0; i < nodesAtDepth.Count; i++)
            {
                int lineIndex = nodesAtDepth[i];
                float y = startY + i * (NODE_HEIGHT + VERTICAL_SPACING);
                positions[lineIndex] = new Vector2(x, y);
            }
        }

        return positions;
    }

    private void CalculateDepths(DialogueDefinition dialogue, int lineIndex, int depth,
        Dictionary<int, int> depths, HashSet<int> visited)
    {
        if (lineIndex < 0 || dialogue.Lines == null || lineIndex >= dialogue.Lines.Count)
            return;

        if (depths.TryGetValue(lineIndex, out int existingDepth) && existingDepth <= depth)
            return;

        if (visited.Contains(lineIndex))
            return;

        visited.Add(lineIndex);
        depths[lineIndex] = depth;

        var line = dialogue.Lines[lineIndex];

        if (line.HasChoices)
        {
            foreach (var choice in line.Choices)
            {
                int target = choice.NextLineIndex >= 0 ? choice.NextLineIndex : lineIndex + 1;
                CalculateDepths(dialogue, target, depth + 1, depths, new HashSet<int>(visited));
            }
        }
        else if (!line.EndsDialogue && lineIndex + 1 < dialogue.Lines.Count)
        {
            CalculateDepths(dialogue, lineIndex + 1, depth + 1, depths, visited);
        }
    }

    /// <summary>
    /// Clear all nodes and connections from the graph
    /// </summary>
    public void ClearGraph()
    {
        // Clear connection layer
        connectionLayer?.ClearConnections();

        // Remove all edges
        var edgeList = edges.ToList();
        foreach (var edge in edgeList)
        {
            edge.input?.Disconnect(edge);
            edge.output?.Disconnect(edge);
            RemoveElement(edge);
        }

        // Remove all nodes
        var nodeList = nodes.ToList();
        foreach (var node in nodeList)
        {
            RemoveElement(node);
        }

        nodesByKey.Clear();
        headersByDialogueId.Clear();
        loadedDialogues.Clear();
        SelectedDialogue = null;
    }

    /// <summary>
    /// Get node key from dialogue ID and line index
    /// </summary>
    private string GetNodeKey(string dialogueId, int lineIndex)
    {
        return $"{dialogueId}:{lineIndex}";
    }

    /// <summary>
    /// Get dialogue color by index
    /// </summary>
    private Color GetDialogueColor(int index)
    {
        Color[] colors = new Color[]
        {
            new Color(0.4f, 0.2f, 0.5f),
            new Color(0.2f, 0.4f, 0.5f),
            new Color(0.5f, 0.3f, 0.2f),
            new Color(0.2f, 0.5f, 0.3f),
            new Color(0.5f, 0.2f, 0.3f),
            new Color(0.3f, 0.3f, 0.5f),
            new Color(0.5f, 0.4f, 0.2f),
            new Color(0.3f, 0.5f, 0.5f),
        };
        return colors[index % colors.Length];
    }

    /// <summary>
    /// Handle graph view changes
    /// </summary>
    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        bool modified = false;
        bool needsFullReload = false;

        // Handle element removal (node deletion)
        if (change.elementsToRemove != null)
        {
            foreach (var element in change.elementsToRemove)
            {
                if (element is DialogueNode node)
                {
                    var dialogue = GetDialogueById(node.DialogueId);
                    if (dialogue != null && node.LineIndex >= 0 && node.LineIndex < dialogue.Lines.Count)
                    {
                        Undo.RecordObject(dialogue, "Delete Dialogue Line");

                        // Remove the line
                        dialogue.Lines.RemoveAt(node.LineIndex);
                        dialogue.OnLineRemoved(node.LineIndex);

                        // Update NextLineIndex references in remaining lines
                        foreach (var line in dialogue.Lines)
                        {
                            if (line.HasChoices)
                            {
                                foreach (var choice in line.Choices)
                                {
                                    if (choice.NextLineIndex == node.LineIndex)
                                        choice.NextLineIndex = -1;
                                    else if (choice.NextLineIndex > node.LineIndex)
                                        choice.NextLineIndex--;
                                }
                            }
                        }

                        EditorUtility.SetDirty(dialogue);
                        modified = true;
                        needsFullReload = true;
                    }

                    // Remove from tracking dictionary
                    string key = GetNodeKey(node.DialogueId, node.LineIndex);
                    nodesByKey.Remove(key);
                }
            }
        }

        // Handle node movement
        if (change.movedElements != null)
        {
            foreach (var element in change.movedElements)
            {
                if (element is DialogueNode node)
                {
                    var dialogue = GetDialogueById(node.DialogueId);
                    if (dialogue != null)
                    {
                        var pos = node.GetPosition();
                        dialogue.SetNodePosition(node.LineIndex, new Vector2(pos.x, pos.y));
                        EditorUtility.SetDirty(dialogue);
                        modified = true;
                    }
                }
            }

            // Rebuild connections after nodes move
            RebuildConnections();
        }

        if (modified)
        {
            OnGraphModified?.Invoke();
        }

        // Full reload needed after deletion to fix indices
        if (needsFullReload)
        {
            // Schedule reload to happen after this change is processed
            schedule.Execute(() => LoadNPC(CurrentNPC)).ExecuteLater(50);
        }

        return change;
    }

    /// <summary>
    /// Get dialogue by ID
    /// </summary>
    public DialogueDefinition GetDialogueById(string dialogueId)
    {
        return loadedDialogues.FirstOrDefault(d => d.DialogueID == dialogueId);
    }

    /// <summary>
    /// Build context menu
    /// </summary>
    private void BuildContextMenu(ContextualMenuPopulateEvent evt)
    {
        var mousePosition = evt.localMousePosition;

        // If we have a selected dialogue, allow adding lines to it
        if (SelectedDialogue != null)
        {
            evt.menu.AppendAction($"Add Line to '{SelectedDialogue.GetDisplayName()}'", action =>
            {
                AddNewLineToDialogue(SelectedDialogue, mousePosition);
            });
            evt.menu.AppendSeparator();
        }

        // Add line to each dialogue
        foreach (var dialogue in loadedDialogues)
        {
            evt.menu.AppendAction($"Add Line to '{dialogue.GetDisplayName()}'", action =>
            {
                AddNewLineToDialogue(dialogue, mousePosition);
            });
        }

        evt.menu.AppendSeparator();

        evt.menu.AppendAction("Refresh Connections", action =>
        {
            RebuildConnections();
        });

        evt.menu.AppendAction("Auto Layout All", action =>
        {
            AutoLayoutAll();
        });

        evt.menu.AppendAction("Frame All", action =>
        {
            FrameAll();
        });
    }

    /// <summary>
    /// Add a new line to a specific dialogue
    /// </summary>
    public DialogueNode AddNewLineToDialogue(DialogueDefinition dialogue, Vector2 position)
    {
        if (dialogue == null) return null;

        Undo.RecordObject(dialogue, "Add Dialogue Line");

        var newLine = new DialogueLine
        {
            Speaker = CurrentNPC?.NPCName ?? "Speaker",
            Text = "New dialogue line...",
            Emotion = NPCEmotion.Neutral,
            Choices = new List<DialogueChoice>()
        };

        dialogue.Lines.Add(newLine);
        int newIndex = dialogue.Lines.Count - 1;

        // Find color index for this dialogue
        int colorIndex = loadedDialogues.IndexOf(dialogue);
        if (colorIndex < 0) colorIndex = 0;

        var node = CreateDialogueNode(dialogue, newLine, newIndex, position, colorIndex);

        dialogue.SetNodePosition(newIndex, position);
        EditorUtility.SetDirty(dialogue);

        OnGraphModified?.Invoke();

        return node;
    }

    /// <summary>
    /// Delete a dialogue line
    /// </summary>
    public void DeleteLine(DialogueDefinition dialogue, int lineIndex)
    {
        if (dialogue == null || lineIndex < 0 || lineIndex >= dialogue.Lines.Count)
            return;

        Undo.RecordObject(dialogue, "Delete Dialogue Line");

        dialogue.Lines.RemoveAt(lineIndex);
        dialogue.OnLineRemoved(lineIndex);

        // Update NextLineIndex references
        foreach (var line in dialogue.Lines)
        {
            if (line.HasChoices)
            {
                foreach (var choice in line.Choices)
                {
                    if (choice.NextLineIndex == lineIndex)
                        choice.NextLineIndex = -1;
                    else if (choice.NextLineIndex > lineIndex)
                        choice.NextLineIndex--;
                }
            }
        }

        EditorUtility.SetDirty(dialogue);

        // Reload the entire graph
        LoadNPC(CurrentNPC);

        OnGraphModified?.Invoke();
    }

    /// <summary>
    /// Auto layout all dialogues
    /// </summary>
    public void AutoLayoutAll()
    {
        LoadNPC(CurrentNPC);
    }

    /// <summary>
    /// Get a node by dialogue ID and line index
    /// </summary>
    public DialogueNode GetNode(string dialogueId, int lineIndex)
    {
        string key = GetNodeKey(dialogueId, lineIndex);
        return nodesByKey.TryGetValue(key, out var node) ? node : null;
    }

    /// <summary>
    /// Get all nodes
    /// </summary>
    public IEnumerable<DialogueNode> GetAllNodes()
    {
        return nodesByKey.Values;
    }

    private void HandleNodeSelected(DialogueNode node)
    {
        SelectedDialogue = GetDialogueById(node.DialogueId);
        OnNodeSelected?.Invoke(node);
    }

    private void HandleHeaderSelected(DialogueHeaderNode header)
    {
        SelectedDialogue = header.Dialogue;
        OnHeaderSelected?.Invoke(header);
    }

    /// <summary>
    /// Get compatible ports for edge creation (kept for future use)
    /// </summary>
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return new List<Port>();
    }

    /// <summary>
    /// Refresh all nodes
    /// </summary>
    public void RefreshAllNodes()
    {
        foreach (var node in nodesByKey.Values)
        {
            node.RefreshFromData();
        }
        foreach (var header in headersByDialogueId.Values)
        {
            header.RefreshFromData();
        }
        RebuildConnections();
    }

    /// <summary>
    /// Save all changes
    /// </summary>
    public void SaveAll()
    {
        foreach (var dialogue in loadedDialogues)
        {
            EditorUtility.SetDirty(dialogue);
        }
        AssetDatabase.SaveAssets();
    }
}
#endif
