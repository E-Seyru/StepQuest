// Purpose: IMGUI-based layer that draws map location connections with step costs
// Filepath: Assets/Scripts/Editor/MapGraph/MapConnectionLayer.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Draws connection lines between location nodes using IMGUI (Handles).
/// Shows step costs on connections and indicates bidirectional vs unidirectional routes.
/// Supports clicking on step cost labels to edit connections.
/// </summary>
public class MapConnectionLayer : IMGUIContainer
{
    private List<MapConnectionInfo> connections = new List<MapConnectionInfo>();
    private VisualElement contentContainer;
    private bool showLabels = true;

    // Track clickable label areas
    private List<ClickableLabel> clickableLabels = new List<ClickableLabel>();

    public struct MapConnectionInfo
    {
        public VisualElement FromNode;
        public VisualElement ToNode;
        public int StepCost;
        public bool IsBidirectional;
        public bool IsAvailable;
        public string FromLocationId;
        public string ToLocationId;
    }

    private struct ClickableLabel
    {
        public Rect Rect;
        public string FromLocationId;
        public string ToLocationId;
        public int StepCost;
        public bool IsBidirectional;
    }

    // Event for when a connection label is clicked
    public event Action<string, string, int, bool> OnConnectionClicked;

    // Colors
    private static readonly Color BidirectionalColor = new Color(0.4f, 0.7f, 0.4f);
    private static readonly Color UnidirectionalColor = new Color(0.7f, 0.7f, 0.4f);
    private static readonly Color UnavailableColor = new Color(0.4f, 0.4f, 0.4f);
    private static readonly Color LabelHoverColor = new Color(0.3f, 0.5f, 0.7f, 0.9f);

    // Track hover state
    private int hoveredLabelIndex = -1;

    public MapConnectionLayer()
    {
        style.position = Position.Absolute;
        style.left = 0;
        style.top = 0;
        style.right = 0;
        style.bottom = 0;
        pickingMode = PickingMode.Position; // Enable mouse interaction

        onGUIHandler = DrawConnections;

        // Register for mouse events
        RegisterCallback<MouseMoveEvent>(OnMouseMove);
        RegisterCallback<MouseDownEvent>(OnMouseDown);
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        int newHovered = -1;
        Vector2 mousePos = evt.localMousePosition;

        for (int i = 0; i < clickableLabels.Count; i++)
        {
            if (clickableLabels[i].Rect.Contains(mousePos))
            {
                newHovered = i;
                break;
            }
        }

        if (newHovered != hoveredLabelIndex)
        {
            hoveredLabelIndex = newHovered;
            MarkDirtyRepaint();
        }
    }

    private void OnMouseDown(MouseDownEvent evt)
    {
        if (evt.button != 0) return; // Left click only

        Vector2 mousePos = evt.localMousePosition;

        for (int i = 0; i < clickableLabels.Count; i++)
        {
            if (clickableLabels[i].Rect.Contains(mousePos))
            {
                var label = clickableLabels[i];
                OnConnectionClicked?.Invoke(label.FromLocationId, label.ToLocationId, label.StepCost, label.IsBidirectional);
                evt.StopPropagation();
                break;
            }
        }
    }

    public void SetContentContainer(VisualElement container)
    {
        contentContainer = container;
    }

    public void SetShowLabels(bool show)
    {
        showLabels = show;
        MarkDirtyRepaint();
    }

    public bool GetShowLabels() => showLabels;

    public void ClearConnections()
    {
        connections.Clear();
        MarkDirtyRepaint();
    }

    public void AddConnection(VisualElement fromNode, VisualElement toNode, int stepCost,
        bool isBidirectional, bool isAvailable, string fromLocationId, string toLocationId)
    {
        connections.Add(new MapConnectionInfo
        {
            FromNode = fromNode,
            ToNode = toNode,
            StepCost = stepCost,
            IsBidirectional = isBidirectional,
            IsAvailable = isAvailable,
            FromLocationId = fromLocationId,
            ToLocationId = toLocationId
        });
    }

    public void Refresh()
    {
        MarkDirtyRepaint();
    }

    private void DrawConnections()
    {
        // Clear clickable labels at start of each draw
        clickableLabels.Clear();

        if (connections.Count == 0 || contentContainer == null)
            return;

        // Track drawn connections to avoid duplicates for bidirectional
        var drawnPairs = new HashSet<string>();

        foreach (var conn in connections)
        {
            if (conn.FromNode == null || conn.ToNode == null)
                continue;

            // Create a unique key for this connection pair
            string pairKey = conn.FromLocationId.CompareTo(conn.ToLocationId) < 0
                ? $"{conn.FromLocationId}:{conn.ToLocationId}"
                : $"{conn.ToLocationId}:{conn.FromLocationId}";

            // Skip if we've already drawn this bidirectional connection
            if (conn.IsBidirectional && drawnPairs.Contains(pairKey))
                continue;

            if (conn.IsBidirectional)
                drawnPairs.Add(pairKey);

            // Get node layout in content container space
            var fromLayout = conn.FromNode.layout;
            var toLayout = conn.ToNode.layout;

            if (fromLayout.width < 1 || toLayout.width < 1)
                continue;

            // Calculate start and end points in content space
            Vector2 startInContent = new Vector2(fromLayout.xMax, fromLayout.center.y);
            Vector2 endInContent = new Vector2(toLayout.xMin, toLayout.center.y);

            // Adjust if nodes are vertically stacked
            if (Mathf.Abs(fromLayout.center.x - toLayout.center.x) < fromLayout.width)
            {
                // Vertical connection
                if (fromLayout.center.y < toLayout.center.y)
                {
                    startInContent = new Vector2(fromLayout.center.x, fromLayout.yMax);
                    endInContent = new Vector2(toLayout.center.x, toLayout.yMin);
                }
                else
                {
                    startInContent = new Vector2(fromLayout.center.x, fromLayout.yMin);
                    endInContent = new Vector2(toLayout.center.x, toLayout.yMax);
                }
            }

            // Convert from content container space to this element's local space
            Vector2 start = contentContainer.ChangeCoordinatesTo(this, startInContent);
            Vector2 end = contentContainer.ChangeCoordinatesTo(this, endInContent);

            // Calculate control point offset for bezier
            float distance = Vector2.Distance(start, end);
            float tangentOffset = Mathf.Max(50f, distance * 0.3f);

            // Determine direction for tangents
            Vector2 direction = (end - start).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);

            Vector3 startVec = new Vector3(start.x, start.y, 0);
            Vector3 endVec = new Vector3(end.x, end.y, 0);

            Vector3 startTangent, endTangent;

            // Horizontal-ish connection
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                startTangent = startVec + new Vector3(tangentOffset * Mathf.Sign(direction.x), 0, 0);
                endTangent = endVec - new Vector3(tangentOffset * Mathf.Sign(direction.x), 0, 0);
            }
            else
            {
                // Vertical-ish connection
                startTangent = startVec + new Vector3(0, tangentOffset * Mathf.Sign(direction.y), 0);
                endTangent = endVec - new Vector3(0, tangentOffset * Mathf.Sign(direction.y), 0);
            }

            // Select color based on connection type
            Color lineColor;
            if (!conn.IsAvailable)
                lineColor = UnavailableColor;
            else if (conn.IsBidirectional)
                lineColor = BidirectionalColor;
            else
                lineColor = UnidirectionalColor;

            // Draw the bezier curve
            float lineWidth = conn.IsBidirectional ? 3f : 2f;
            Handles.DrawBezier(startVec, endVec, startTangent, endTangent, lineColor, null, lineWidth);

            // Draw arrow for unidirectional connections
            if (!conn.IsBidirectional && conn.IsAvailable)
            {
                DrawArrow(endVec, direction, lineColor);
            }

            // Draw step cost label at midpoint
            if (showLabels)
            {
                Vector2 midpoint = CalculateBezierMidpoint(startVec, startTangent, endTangent, endVec);
                DrawStepCostLabel(midpoint, conn.StepCost, conn.IsBidirectional, conn.IsAvailable,
                    conn.FromLocationId, conn.ToLocationId);
            }
        }
    }

    private Vector2 CalculateBezierMidpoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // Cubic bezier at t=0.5
        float t = 0.5f;
        float u = 1 - t;
        Vector3 point = u * u * u * p0 +
                       3 * u * u * t * p1 +
                       3 * u * t * t * p2 +
                       t * t * t * p3;
        return new Vector2(point.x, point.y);
    }

    private void DrawStepCostLabel(Vector2 position, int stepCost, bool isBidirectional, bool isAvailable,
        string fromLocationId, string toLocationId)
    {
        string symbol = isBidirectional ? "=" : ">";
        string text = $"{symbol} {stepCost}";

        // Create style for label
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 10;
        style.fontStyle = FontStyle.Bold;

        // Calculate label size
        Vector2 size = style.CalcSize(new GUIContent(text));
        size.x += 12; // Extra padding for click area
        size.y += 6;

        Rect labelRect = new Rect(
            position.x - size.x / 2,
            position.y - size.y / 2,
            size.x,
            size.y
        );

        // Store clickable area
        int labelIndex = clickableLabels.Count;
        clickableLabels.Add(new ClickableLabel
        {
            Rect = labelRect,
            FromLocationId = fromLocationId,
            ToLocationId = toLocationId,
            StepCost = stepCost,
            IsBidirectional = isBidirectional
        });

        // Determine background color (with hover effect)
        Color bgColor;
        if (hoveredLabelIndex == labelIndex)
        {
            bgColor = LabelHoverColor;
            EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.Link);
        }
        else if (!isAvailable)
        {
            bgColor = new Color(0.3f, 0.2f, 0.2f, 0.9f);
        }
        else
        {
            bgColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        }

        EditorGUI.DrawRect(labelRect, bgColor);

        // Draw border when hovered
        if (hoveredLabelIndex == labelIndex)
        {
            Handles.color = new Color(0.5f, 0.7f, 1f);
            Handles.DrawSolidRectangleWithOutline(labelRect, Color.clear, new Color(0.5f, 0.7f, 1f));
        }

        // Draw text
        style.normal.textColor = isAvailable ? Color.white : new Color(0.7f, 0.5f, 0.5f);
        GUI.Label(labelRect, text, style);
    }

    private void DrawArrow(Vector3 tip, Vector2 direction, Color color)
    {
        float arrowSize = 8f;
        Vector2 perp = new Vector2(-direction.y, direction.x);

        Vector3 left = tip - new Vector3(direction.x * arrowSize + perp.x * arrowSize * 0.5f,
                                          direction.y * arrowSize + perp.y * arrowSize * 0.5f, 0);
        Vector3 right = tip - new Vector3(direction.x * arrowSize - perp.x * arrowSize * 0.5f,
                                           direction.y * arrowSize - perp.y * arrowSize * 0.5f, 0);

        Handles.color = color;
        Handles.DrawAAConvexPolygon(tip, left, right);
    }
}
#endif
