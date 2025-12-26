using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject for persisting Logger configuration
/// Create via: Assets > Create > Logger Settings
/// </summary>
[CreateAssetMenu(fileName = "LoggerSettings", menuName = "StepQuest/Logger Settings", order = 100)]
public class LoggerSettings : ScriptableObject
{
    [Header("Global Settings")]
    [Tooltip("Enable or disable all logging")]
    public bool isEnabled = true;

    [Tooltip("Minimum log level to display")]
    public Logger.LogLevel defaultLogLevel = Logger.LogLevel.Info;

    [Header("Category Filtering")]
    [Tooltip("If true, only enabled categories will produce logs")]
    public bool useCategoryFiltering = false;

    [Tooltip("Categories that are enabled when category filtering is active")]
    public List<Logger.LogCategory> enabledCategories = new List<Logger.LogCategory>
    {
        Logger.LogCategory.General,
        Logger.LogCategory.StepLog,
        Logger.LogCategory.MapLog,
        Logger.LogCategory.CombatLog,
        Logger.LogCategory.InventoryLog,
        Logger.LogCategory.ActivityLog,
        Logger.LogCategory.UILog,
        Logger.LogCategory.DialogueLog,
        Logger.LogCategory.XpLog,
        Logger.LogCategory.DataLog,
        Logger.LogCategory.EditorLog
    };

    [Header("Build-Specific Overrides")]
    [Tooltip("Override settings for editor builds")]
    public bool useEditorOverride = true;

    [Tooltip("Log level for editor builds")]
    public Logger.LogLevel editorLogLevel = Logger.LogLevel.Info;

    [Tooltip("Override settings for development builds")]
    public bool useDevelopmentBuildOverride = true;

    [Tooltip("Log level for development builds")]
    public Logger.LogLevel developmentLogLevel = Logger.LogLevel.Info;

    [Tooltip("Override settings for production builds")]
    public bool useProductionOverride = true;

    [Tooltip("Log level for production builds")]
    public Logger.LogLevel productionLogLevel = Logger.LogLevel.Warning;

    /// <summary>
    /// Apply these settings to the Logger
    /// </summary>
    public void ApplySettings()
    {
        // Apply build-specific overrides
        Logger.LogLevel targetLevel = defaultLogLevel;

        #if UNITY_EDITOR
        if (useEditorOverride)
        {
            targetLevel = editorLogLevel;
        }
        #elif DEVELOPMENT_BUILD
        if (useDevelopmentBuildOverride)
        {
            targetLevel = developmentLogLevel;
        }
        #else
        if (useProductionOverride)
        {
            targetLevel = productionLogLevel;
        }
        #endif

        Logger.SetEnabled(isEnabled);
        Logger.SetLogLevel(targetLevel);

        if (useCategoryFiltering && enabledCategories != null && enabledCategories.Count > 0)
        {
            Logger.EnableCategoryFiltering(enabledCategories.ToArray());
        }
        else
        {
            Logger.DisableCategoryFiltering();
        }

        Debug.Log($"[LoggerSettings] Applied settings - Enabled: {isEnabled}, Level: {targetLevel}, Category Filtering: {useCategoryFiltering}");
    }

    /// <summary>
    /// Called when the asset is loaded or values change in the inspector
    /// </summary>
    private void OnValidate()
    {
        // Auto-apply when changed in inspector during play mode
        if (Application.isPlaying)
        {
            ApplySettings();
        }
    }
}
