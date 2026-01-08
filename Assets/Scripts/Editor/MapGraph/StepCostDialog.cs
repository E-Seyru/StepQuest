// Purpose: Dialog window for setting step cost when creating connections
// Filepath: Assets/Scripts/Editor/MapGraph/StepCostDialog.cs
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Small dialog window for setting the step cost when creating a new connection.
/// </summary>
public class StepCostDialog : EditorWindow
{
    // Input fields
    private int stepCost = 50;
    private bool isBidirectional = true;

    // Location info
    private string fromLocationName;
    private string toLocationName;

    // Callback
    private Action<int, bool> onConfirm;

    /// <summary>
    /// Show the dialog for creating a connection
    /// </summary>
    public static void Show(string fromLocation, string toLocation, Action<int, bool> onConfirm)
    {
        var window = CreateInstance<StepCostDialog>();
        window.fromLocationName = fromLocation;
        window.toLocationName = toLocation;
        window.onConfirm = onConfirm;
        window.titleContent = new GUIContent("New Connection");

        // Size and center
        window.minSize = new Vector2(280, 140);
        window.maxSize = new Vector2(280, 140);

        // Position near mouse
        Vector2 mousePos = GUIUtility.GUIToScreenPoint(Event.current?.mousePosition ?? Vector2.zero);
        if (mousePos == Vector2.zero)
        {
            // Center on main window if no mouse position
            mousePos = new Vector2(Screen.width / 2, Screen.height / 2);
        }
        window.position = new Rect(mousePos.x - 140, mousePos.y - 70, 280, 140);

        window.ShowUtility();
        window.Focus();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        // Header
        EditorGUILayout.LabelField($"Connect: {fromLocationName}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"      To: {toLocationName}", EditorStyles.boldLabel);

        EditorGUILayout.Space(10);

        // Step cost input
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Step Cost:", GUILayout.Width(80));
        stepCost = EditorGUILayout.IntField(stepCost, GUILayout.Width(80));
        stepCost = Mathf.Max(1, stepCost); // Minimum 1 step
        EditorGUILayout.EndHorizontal();

        // Bidirectional toggle
        isBidirectional = EditorGUILayout.Toggle("Bidirectional", isBidirectional);

        EditorGUILayout.Space(10);

        // Buttons
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Cancel", GUILayout.Width(80)))
        {
            Close();
        }

        if (GUILayout.Button("Create", GUILayout.Width(80)))
        {
            onConfirm?.Invoke(stepCost, isBidirectional);
            Close();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void OnLostFocus()
    {
        // Close when losing focus
        Close();
    }
}
#endif
