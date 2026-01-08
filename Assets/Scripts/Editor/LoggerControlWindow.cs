using UnityEditor;
using UnityEngine;
using System;

/// <summary>
/// Editor window for controlling Logger settings during development
/// Access via: Tools > Logger Control
/// </summary>
public class LoggerControlWindow : EditorWindow
{
    private Logger.LogLevel selectedLogLevel;
    private bool isLoggerEnabled = true;
    private bool[] categoryEnabled;

    [MenuItem("StepQuest/Debug/Logger Control")]
    public static void ShowWindow()
    {
        var window = GetWindow<LoggerControlWindow>("Logger Control");
        window.minSize = new Vector2(300, 400);
    }

    private void OnEnable()
    {
        selectedLogLevel = Logger.CurrentLogLevel;

        // Initialize category array
        var categories = Enum.GetValues(typeof(Logger.LogCategory));
        categoryEnabled = new bool[categories.Length];

        for (int i = 0; i < categories.Length; i++)
        {
            Logger.LogCategory category = (Logger.LogCategory)categories.GetValue(i);
            categoryEnabled[i] = Logger.IsCategoryEnabled(category);
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Logger Control Panel", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Logger enabled toggle
        EditorGUI.BeginChangeCheck();
        isLoggerEnabled = EditorGUILayout.Toggle("Logger Enabled", isLoggerEnabled);
        if (EditorGUI.EndChangeCheck())
        {
            Logger.SetEnabled(isLoggerEnabled);
        }

        EditorGUILayout.Space();

        // Log level dropdown
        EditorGUI.BeginChangeCheck();
        selectedLogLevel = (Logger.LogLevel)EditorGUILayout.EnumPopup("Log Level", selectedLogLevel);
        if (EditorGUI.EndChangeCheck())
        {
            Logger.SetLogLevel(selectedLogLevel);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Category Filtering", EditorStyles.boldLabel);

        // Quick preset buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("All Categories"))
        {
            Logger.DisableCategoryFiltering();
            for (int i = 0; i < categoryEnabled.Length; i++)
            {
                categoryEnabled[i] = true;
            }
        }
        if (GUILayout.Button("No Categories"))
        {
            Logger.DisableAllCategories();
            for (int i = 0; i < categoryEnabled.Length; i++)
            {
                categoryEnabled[i] = false;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Quick presets
        EditorGUILayout.LabelField("Quick Presets:", EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Combat"))
        {
            Logger.EnableCategoryFiltering(Logger.LogCategory.CombatLog);
            UpdateCategoryArrayFromLogger();
        }
        if (GUILayout.Button("Step"))
        {
            Logger.EnableCategoryFiltering(Logger.LogCategory.StepLog);
            UpdateCategoryArrayFromLogger();
        }
        if (GUILayout.Button("Map"))
        {
            Logger.EnableCategoryFiltering(Logger.LogCategory.MapLog);
            UpdateCategoryArrayFromLogger();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Activity"))
        {
            Logger.EnableCategoryFiltering(Logger.LogCategory.ActivityLog);
            UpdateCategoryArrayFromLogger();
        }
        if (GUILayout.Button("Inventory"))
        {
            Logger.EnableCategoryFiltering(Logger.LogCategory.InventoryLog);
            UpdateCategoryArrayFromLogger();
        }
        if (GUILayout.Button("Dialogue"))
        {
            Logger.EnableCategoryFiltering(Logger.LogCategory.DialogueLog);
            UpdateCategoryArrayFromLogger();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("XP"))
        {
            Logger.EnableCategoryFiltering(Logger.LogCategory.XpLog);
            UpdateCategoryArrayFromLogger();
        }
        if (GUILayout.Button("UI"))
        {
            Logger.EnableCategoryFiltering(Logger.LogCategory.UILog);
            UpdateCategoryArrayFromLogger();
        }
        if (GUILayout.Button("Data"))
        {
            Logger.EnableCategoryFiltering(Logger.LogCategory.DataLog);
            UpdateCategoryArrayFromLogger();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Individual category toggles
        EditorGUILayout.LabelField("Individual Categories:", EditorStyles.miniBoldLabel);
        var categories = Enum.GetValues(typeof(Logger.LogCategory));

        for (int i = 0; i < categories.Length; i++)
        {
            Logger.LogCategory category = (Logger.LogCategory)categories.GetValue(i);
            if (category == Logger.LogCategory.None) continue;

            EditorGUI.BeginChangeCheck();
            categoryEnabled[i] = EditorGUILayout.Toggle(category.ToString(), categoryEnabled[i]);
            if (EditorGUI.EndChangeCheck())
            {
                if (categoryEnabled[i])
                {
                    Logger.EnableCategory(category);
                }
                else
                {
                    Logger.DisableCategory(category);
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Category filtering status: " + (Logger.IsUsingCategoryFiltering() ? "ACTIVE" : "INACTIVE") +
            "\n\nWhen active, only enabled categories will produce logs.",
            MessageType.Info
        );

        EditorGUILayout.Space();

        // Status info
        EditorGUILayout.LabelField("Current Settings:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Enabled: {isLoggerEnabled}");
        EditorGUILayout.LabelField($"Log Level: {selectedLogLevel}");
        EditorGUILayout.LabelField($"Category Filtering: {(Logger.IsUsingCategoryFiltering() ? "Active" : "Inactive")}");

        EditorGUILayout.Space();

        // Reset button
        if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Reset Logger Settings",
                "This will reset all Logger settings to defaults. Continue?",
                "Reset", "Cancel"))
            {
                Logger.ResetToDefaults();
                OnEnable(); // Refresh UI
            }
        }
    }

    private void UpdateCategoryArrayFromLogger()
    {
        var categories = Enum.GetValues(typeof(Logger.LogCategory));
        for (int i = 0; i < categories.Length; i++)
        {
            Logger.LogCategory category = (Logger.LogCategory)categories.GetValue(i);
            categoryEnabled[i] = Logger.IsCategoryEnabled(category);
        }
        Repaint();
    }
}
