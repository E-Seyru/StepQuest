// Purpose: Dialog window for editing existing connection step costs
// Filepath: Assets/Scripts/Editor/MapGraph/EditConnectionDialog.cs
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Dialog window for editing an existing connection's step cost.
/// Allows saving changes or deleting the connection.
/// </summary>
public class EditConnectionDialog : EditorWindow
{
    // Input fields
    private int stepCost;
    private bool isBidirectional;

    // Location info
    private string fromLocationName;
    private string toLocationName;

    // Callbacks
    private Action<int> onSave;
    private Action onDelete;

    /// <summary>
    /// Show the dialog for editing a connection
    /// </summary>
    public static void Show(string fromLocation, string toLocation, int currentStepCost, bool isBidirectional,
        Action<int> onSave, Action onDelete)
    {
        var window = CreateInstance<EditConnectionDialog>();
        window.fromLocationName = fromLocation;
        window.toLocationName = toLocation;
        window.stepCost = currentStepCost;
        window.isBidirectional = isBidirectional;
        window.onSave = onSave;
        window.onDelete = onDelete;
        window.titleContent = new GUIContent("Edit Connection");

        // Size and center
        window.minSize = new Vector2(300, 160);
        window.maxSize = new Vector2(300, 160);

        // Position near mouse
        Vector2 mousePos = GUIUtility.GUIToScreenPoint(Event.current?.mousePosition ?? Vector2.zero);
        if (mousePos == Vector2.zero)
        {
            mousePos = new Vector2(Screen.width / 2, Screen.height / 2);
        }
        window.position = new Rect(mousePos.x - 150, mousePos.y - 80, 300, 160);

        window.ShowUtility();
        window.Focus();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        // Header
        string symbol = isBidirectional ? "=" : ">";
        EditorGUILayout.LabelField($"{fromLocationName} {symbol} {toLocationName}", EditorStyles.boldLabel);

        EditorGUILayout.Space(5);

        // Connection type info
        string connectionType = isBidirectional ? "Bidirectional connection" : "One-way connection";
        EditorGUILayout.LabelField(connectionType, EditorStyles.miniLabel);

        EditorGUILayout.Space(10);

        // Step cost input
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Step Cost:", GUILayout.Width(80));
        stepCost = EditorGUILayout.IntField(stepCost, GUILayout.Width(80));
        stepCost = Mathf.Max(1, stepCost); // Minimum 1 step
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(15);

        // Buttons
        EditorGUILayout.BeginHorizontal();

        // Delete button (left side, red)
        GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
        if (GUILayout.Button("Delete", GUILayout.Width(70)))
        {
            onDelete?.Invoke();
            Close();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Cancel", GUILayout.Width(70)))
        {
            Close();
        }

        GUI.backgroundColor = new Color(0.3f, 0.6f, 0.3f);
        if (GUILayout.Button("Save", GUILayout.Width(70)))
        {
            onSave?.Invoke(stepCost);
            Close();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();
    }

    private void OnLostFocus()
    {
        // Close when losing focus
        Close();
    }
}
#endif
