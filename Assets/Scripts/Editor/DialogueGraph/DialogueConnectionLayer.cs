// Purpose: IMGUI-based layer that draws all dialogue connections
// Filepath: Assets/Scripts/Editor/DialogueGraph/DialogueConnectionLayer.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Draws connection lines between dialogue nodes using IMGUI (Handles).
/// This is the most reliable way to draw custom lines in Unity Editor.
/// </summary>
public class DialogueConnectionLayer : IMGUIContainer
{
    private List<ConnectionInfo> connections = new List<ConnectionInfo>();
    private VisualElement contentContainer;

    public struct ConnectionInfo
    {
        public VisualElement FromNode;
        public VisualElement ToNode;
        public Color Color;
    }

    public DialogueConnectionLayer()
    {
        style.position = Position.Absolute;
        style.left = 0;
        style.top = 0;
        style.right = 0;
        style.bottom = 0;
        pickingMode = PickingMode.Ignore;

        onGUIHandler = DrawConnections;
    }

    public void SetContentContainer(VisualElement container)
    {
        contentContainer = container;
    }

    public void ClearConnections()
    {
        connections.Clear();
        MarkDirtyRepaint();
    }

    public void AddConnection(VisualElement fromNode, VisualElement toNode, Color color)
    {
        connections.Add(new ConnectionInfo
        {
            FromNode = fromNode,
            ToNode = toNode,
            Color = color
        });
    }

    public void Refresh()
    {
        MarkDirtyRepaint();
    }

    private void DrawConnections()
    {
        if (connections.Count == 0 || contentContainer == null)
            return;

        foreach (var conn in connections)
        {
            if (conn.FromNode == null || conn.ToNode == null)
                continue;

            // Get node layout in content container space
            var fromLayout = conn.FromNode.layout;
            var toLayout = conn.ToNode.layout;

            if (fromLayout.width < 1 || toLayout.width < 1)
                continue;

            // Calculate start and end points in content space
            Vector2 startInContent = new Vector2(fromLayout.xMax, fromLayout.center.y);
            Vector2 endInContent = new Vector2(toLayout.xMin, toLayout.center.y);

            // Convert from content container space to this element's local space
            Vector2 start = contentContainer.ChangeCoordinatesTo(this, startInContent);
            Vector2 end = contentContainer.ChangeCoordinatesTo(this, endInContent);

            // Calculate control point offset for bezier
            float tangentOffset = Mathf.Max(50f, Mathf.Abs(end.x - start.x) * 0.5f);

            Vector3 startVec = new Vector3(start.x, start.y, 0);
            Vector3 endVec = new Vector3(end.x, end.y, 0);
            Vector3 startTangent = startVec + new Vector3(tangentOffset, 0, 0);
            Vector3 endTangent = endVec - new Vector3(tangentOffset, 0, 0);

            // Draw using Handles.DrawBezier (works reliably in Unity Editor)
            Handles.DrawBezier(
                startVec,
                endVec,
                startTangent,
                endTangent,
                conn.Color,
                null,
                3f
            );
        }
    }
}
#endif
