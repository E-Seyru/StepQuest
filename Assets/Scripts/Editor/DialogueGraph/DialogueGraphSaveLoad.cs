// Purpose: Utility for syncing between GraphView and DialogueDefinition data
// Filepath: Assets/Scripts/Editor/DialogueGraph/DialogueGraphSaveLoad.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Utility class for saving and loading dialogue data to/from the graph view.
/// Handles synchronization between the visual representation and the underlying data.
/// </summary>
public static class DialogueGraphSaveLoad
{
    private const float NODE_WIDTH = 250f;
    private const float NODE_HEIGHT = 150f;
    private const float HORIZONTAL_SPACING = 150f;
    private const float VERTICAL_SPACING = 80f;

    /// <summary>
    /// Save node positions from the graph to the dialogue asset
    /// </summary>
    public static void SaveNodePositions(DialogueGraphView graphView, DialogueDefinition dialogue)
    {
        if (graphView == null || dialogue == null) return;

        Undo.RecordObject(dialogue, "Save Dialogue Node Positions");

        foreach (var node in graphView.GetAllNodes())
        {
            var rect = node.GetPosition();
            dialogue.SetNodePosition(node.LineIndex, new Vector2(rect.x, rect.y));
        }

        EditorUtility.SetDirty(dialogue);
    }

    /// <summary>
    /// Validate the dialogue structure for graph display
    /// </summary>
    public static List<string> ValidateDialogue(DialogueDefinition dialogue)
    {
        var issues = new List<string>();

        if (dialogue == null)
        {
            issues.Add("Dialogue is null");
            return issues;
        }

        if (dialogue.Lines == null || dialogue.Lines.Count == 0)
        {
            issues.Add("Dialogue has no lines");
            return issues;
        }

        // Check for orphaned lines (no path to them)
        var reachableLines = new HashSet<int> { 0 };
        var toProcess = new Queue<int>();
        toProcess.Enqueue(0);

        while (toProcess.Count > 0)
        {
            int current = toProcess.Dequeue();
            if (current < 0 || current >= dialogue.Lines.Count)
                continue;

            var line = dialogue.Lines[current];

            if (line.HasChoices)
            {
                foreach (var choice in line.Choices)
                {
                    int target = choice.NextLineIndex >= 0 ? choice.NextLineIndex : current + 1;
                    if (target >= 0 && target < dialogue.Lines.Count && !reachableLines.Contains(target))
                    {
                        reachableLines.Add(target);
                        toProcess.Enqueue(target);
                    }
                }
            }
            else if (!line.EndsDialogue && current + 1 < dialogue.Lines.Count)
            {
                int target = current + 1;
                if (!reachableLines.Contains(target))
                {
                    reachableLines.Add(target);
                    toProcess.Enqueue(target);
                }
            }
        }

        // Report orphaned lines
        for (int i = 0; i < dialogue.Lines.Count; i++)
        {
            if (!reachableLines.Contains(i))
            {
                issues.Add($"Line {i} is orphaned (not reachable from start)");
            }
        }

        // Check for invalid NextLineIndex references
        for (int i = 0; i < dialogue.Lines.Count; i++)
        {
            var line = dialogue.Lines[i];
            if (line.HasChoices)
            {
                for (int c = 0; c < line.Choices.Count; c++)
                {
                    var choice = line.Choices[c];
                    if (choice.NextLineIndex >= dialogue.Lines.Count)
                    {
                        issues.Add($"Line {i}, Choice {c}: NextLineIndex ({choice.NextLineIndex}) is out of bounds");
                    }
                }
            }
        }

        // Check for cycles that don't have an exit
        // Simple check: ensure at least one line ends the dialogue
        bool hasEnding = dialogue.Lines.Any(l => l.EndsDialogue);
        if (!hasEnding)
        {
            issues.Add("Warning: No line explicitly ends the dialogue");
        }

        return issues;
    }

    /// <summary>
    /// Calculate auto-layout positions for all lines
    /// </summary>
    public static Dictionary<int, Vector2> CalculateAutoLayout(DialogueDefinition dialogue)
    {
        var positions = new Dictionary<int, Vector2>();

        if (dialogue == null || dialogue.Lines == null || dialogue.Lines.Count == 0)
            return positions;

        var visited = new HashSet<int>();
        var depths = new Dictionary<int, int>();
        var verticalOffsets = new Dictionary<int, float>();

        // Calculate depths from line 0
        CalculateNodeDepth(0, 0, dialogue, depths, visited);

        // Handle any orphaned nodes
        for (int i = 0; i < dialogue.Lines.Count; i++)
        {
            if (!depths.ContainsKey(i))
            {
                depths[i] = 0;
            }
        }

        // Group by depth
        var nodesByDepth = depths
            .GroupBy(kvp => kvp.Value)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Key).OrderBy(i => i).ToList());

        // Calculate positions
        foreach (var depthGroup in nodesByDepth)
        {
            int depth = depthGroup.Key;
            var nodesAtDepth = depthGroup.Value;

            float x = depth * (NODE_WIDTH + HORIZONTAL_SPACING);

            for (int i = 0; i < nodesAtDepth.Count; i++)
            {
                int lineIndex = nodesAtDepth[i];
                float y = i * (NODE_HEIGHT + VERTICAL_SPACING);
                positions[lineIndex] = new Vector2(x, y);
            }
        }

        return positions;
    }

    private static void CalculateNodeDepth(int lineIndex, int depth, DialogueDefinition dialogue,
        Dictionary<int, int> depths, HashSet<int> visited)
    {
        if (lineIndex < 0 || lineIndex >= dialogue.Lines.Count)
            return;

        // If already visited with a smaller depth, skip
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
                CalculateNodeDepth(target, depth + 1, dialogue, depths, new HashSet<int>(visited));
            }
        }
        else if (!line.EndsDialogue && lineIndex + 1 < dialogue.Lines.Count)
        {
            CalculateNodeDepth(lineIndex + 1, depth + 1, dialogue, depths, visited);
        }
    }

    /// <summary>
    /// Apply auto-layout to a dialogue and save the positions
    /// </summary>
    public static void ApplyAutoLayout(DialogueDefinition dialogue)
    {
        if (dialogue == null) return;

        Undo.RecordObject(dialogue, "Auto Layout Dialogue");

        dialogue.ClearNodePositions();

        var positions = CalculateAutoLayout(dialogue);
        foreach (var kvp in positions)
        {
            dialogue.SetNodePosition(kvp.Key, kvp.Value);
        }

        EditorUtility.SetDirty(dialogue);
    }

    /// <summary>
    /// Get a summary of the dialogue structure
    /// </summary>
    public static string GetDialogueSummary(DialogueDefinition dialogue)
    {
        if (dialogue == null)
            return "No dialogue loaded";

        int lineCount = dialogue.Lines?.Count ?? 0;
        int choiceLines = dialogue.Lines?.Count(l => l.HasChoices) ?? 0;
        int endingLines = dialogue.Lines?.Count(l => l.EndsDialogue) ?? 0;

        return $"{lineCount} lines, {choiceLines} with choices, {endingLines} endings";
    }

    /// <summary>
    /// Reorder lines based on their visual position in the graph
    /// </summary>
    public static void ReorderLinesByPosition(DialogueDefinition dialogue)
    {
        if (dialogue == null || dialogue.Lines == null || dialogue.Lines.Count <= 1)
            return;

        Undo.RecordObject(dialogue, "Reorder Dialogue Lines");

        // Get current positions
        var linePositions = new List<(int index, float x, float y)>();
        for (int i = 0; i < dialogue.Lines.Count; i++)
        {
            var pos = dialogue.GetNodePosition(i);
            linePositions.Add((i, pos.x, pos.y));
        }

        // Sort by position (left to right, top to bottom)
        var sorted = linePositions
            .OrderBy(p => p.x)
            .ThenBy(p => p.y)
            .Select(p => p.index)
            .ToList();

        // Create index mapping (old to new)
        var indexMap = new Dictionary<int, int>();
        for (int newIndex = 0; newIndex < sorted.Count; newIndex++)
        {
            indexMap[sorted[newIndex]] = newIndex;
        }

        // Reorder lines
        var newLines = sorted.Select(oldIndex => dialogue.Lines[oldIndex]).ToList();

        // Update NextLineIndex references
        foreach (var line in newLines)
        {
            if (line.HasChoices)
            {
                foreach (var choice in line.Choices)
                {
                    if (choice.NextLineIndex >= 0 && indexMap.ContainsKey(choice.NextLineIndex))
                    {
                        choice.NextLineIndex = indexMap[choice.NextLineIndex];
                    }
                }
            }
        }

        // Update positions
        dialogue.ClearNodePositions();
        for (int newIndex = 0; newIndex < sorted.Count; newIndex++)
        {
            int oldIndex = sorted[newIndex];
            var oldPos = linePositions.First(p => p.index == oldIndex);
            dialogue.SetNodePosition(newIndex, new Vector2(oldPos.x, oldPos.y));
        }

        dialogue.Lines = newLines;
        EditorUtility.SetDirty(dialogue);
    }
}
#endif
