using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class Logger
{
    public enum LogLevel { Debug, Info, Warning, Error }
    public enum LogCategory
    {
        None,
        General,
        StepLog,
        MapLog,
        CombatLog,
        InventoryLog,
        ActivityLog,
        UILog,
        DialogueLog,
        XpLog,
        DataLog,
        EditorLog
    }

    // PlayerPrefs keys for persistence
    private const string PREF_LOG_LEVEL = "Logger_LogLevel";
    private const string PREF_ENABLED = "Logger_Enabled";
    private const string PREF_USE_FILTERING = "Logger_UseFiltering";
    private const string PREF_ENABLED_CATEGORIES = "Logger_EnabledCategories";

    // Default log level based on build type
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static LogLevel defaultLogLevel = LogLevel.Info;
    #else
    private static LogLevel defaultLogLevel = LogLevel.Warning;
    #endif

    private static LogLevel currentLogLevel;
    private static bool isEnabled = true;
    private static bool useCategoryFiltering = false;
    private static HashSet<LogCategory> enabledCategories = new HashSet<LogCategory>();
    private static bool isInitialized = false;

    // Public property to access current log level
    public static LogLevel CurrentLogLevel
    {
        get
        {
            EnsureInitialized();
            return currentLogLevel;
        }
        private set => currentLogLevel = value;
    }

    // Initialize from PlayerPrefs on first access
    private static void EnsureInitialized()
    {
        if (isInitialized) return;

        // Load log level
        if (PlayerPrefs.HasKey(PREF_LOG_LEVEL))
        {
            currentLogLevel = (LogLevel)PlayerPrefs.GetInt(PREF_LOG_LEVEL);
        }
        else
        {
            currentLogLevel = defaultLogLevel;
        }

        // Load enabled state
        if (PlayerPrefs.HasKey(PREF_ENABLED))
        {
            isEnabled = PlayerPrefs.GetInt(PREF_ENABLED) == 1;
        }

        // Load filtering state - default to TRUE (always use filtering)
        if (PlayerPrefs.HasKey(PREF_USE_FILTERING))
        {
            useCategoryFiltering = PlayerPrefs.GetInt(PREF_USE_FILTERING) == 1;
        }
        else
        {
            useCategoryFiltering = true; // Default to filtering enabled
        }

        // Load enabled categories
        if (PlayerPrefs.HasKey(PREF_ENABLED_CATEGORIES))
        {
            string categoriesString = PlayerPrefs.GetString(PREF_ENABLED_CATEGORIES);
            if (!string.IsNullOrEmpty(categoriesString))
            {
                string[] categoryNames = categoriesString.Split(',');
                foreach (string catName in categoryNames)
                {
                    if (System.Enum.TryParse<LogCategory>(catName, out var category))
                    {
                        enabledCategories.Add(category);
                    }
                }
            }
        }
        else
        {
            // Default: enable all categories
            foreach (LogCategory category in System.Enum.GetValues(typeof(LogCategory)))
            {
                if (category != LogCategory.None)
                {
                    enabledCategories.Add(category);
                }
            }
        }

        isInitialized = true;
    }

    private static void SaveSettings()
    {
        PlayerPrefs.SetInt(PREF_LOG_LEVEL, (int)currentLogLevel);
        PlayerPrefs.SetInt(PREF_ENABLED, isEnabled ? 1 : 0);
        PlayerPrefs.SetInt(PREF_USE_FILTERING, useCategoryFiltering ? 1 : 0);

        // Save enabled categories as comma-separated string
        string categoriesString = string.Join(",", enabledCategories.Select(c => c.ToString()));
        PlayerPrefs.SetString(PREF_ENABLED_CATEGORIES, categoriesString);

        PlayerPrefs.Save();
    }

    #region Configuration Methods

    /// <summary>
    /// Enable or disable the entire logger
    /// </summary>
    public static void SetEnabled(bool enabled)
    {
        EnsureInitialized();
        isEnabled = enabled;
        SaveSettings();
    }

    /// <summary>
    /// Set the minimum log level to display
    /// </summary>
    public static void SetLogLevel(LogLevel level)
    {
        EnsureInitialized();
        currentLogLevel = level;
        SaveSettings();
    }

    /// <summary>
    /// Enable filtering by specific categories. Only enabled categories will log.
    /// </summary>
    public static void EnableCategoryFiltering(params LogCategory[] categories)
    {
        EnsureInitialized();
        useCategoryFiltering = true;
        enabledCategories.Clear();
        foreach (var category in categories)
        {
            enabledCategories.Add(category);
        }
        SaveSettings();
    }

    /// <summary>
    /// Disable category filtering - all categories will log (subject to log level)
    /// </summary>
    public static void DisableCategoryFiltering()
    {
        EnsureInitialized();
        useCategoryFiltering = true; // Changed: always use filtering
        enabledCategories.Clear();

        // Add all categories to enabled list
        foreach (LogCategory category in System.Enum.GetValues(typeof(LogCategory)))
        {
            if (category != LogCategory.None)
            {
                enabledCategories.Add(category);
            }
        }
        SaveSettings();
    }

    /// <summary>
    /// Add a category to the enabled list
    /// </summary>
    public static void EnableCategory(LogCategory category)
    {
        EnsureInitialized();
        useCategoryFiltering = true;
        enabledCategories.Add(category);
        SaveSettings();
    }

    /// <summary>
    /// Remove a category from the enabled list
    /// </summary>
    public static void DisableCategory(LogCategory category)
    {
        EnsureInitialized();
        enabledCategories.Remove(category);
        // Keep filtering active even if no categories are enabled
        // This allows "disable all" to actually disable all logging
        SaveSettings();
    }

    /// <summary>
    /// Check if a specific category is currently enabled
    /// </summary>
    public static bool IsCategoryEnabled(LogCategory category)
    {
        EnsureInitialized();
        if (!useCategoryFiltering) return true;
        return enabledCategories.Contains(category);
    }

    /// <summary>
    /// Get current enabled categories (for debug UI)
    /// </summary>
    public static IEnumerable<LogCategory> GetEnabledCategories()
    {
        EnsureInitialized();
        return enabledCategories;
    }

    /// <summary>
    /// Check if category filtering is active
    /// </summary>
    public static bool IsUsingCategoryFiltering()
    {
        EnsureInitialized();
        return useCategoryFiltering;
    }

    /// <summary>
    /// Disable all categories (enable filtering with empty category list)
    /// </summary>
    public static void DisableAllCategories()
    {
        EnsureInitialized();
        useCategoryFiltering = true;
        enabledCategories.Clear();
        SaveSettings();
    }

    /// <summary>
    /// Reset logger to default settings
    /// </summary>
    public static void ResetToDefaults()
    {
        PlayerPrefs.DeleteKey(PREF_LOG_LEVEL);
        PlayerPrefs.DeleteKey(PREF_ENABLED);
        PlayerPrefs.DeleteKey(PREF_USE_FILTERING);
        PlayerPrefs.DeleteKey(PREF_ENABLED_CATEGORIES);
        PlayerPrefs.Save();

        isInitialized = false;
        EnsureInitialized();
    }

    #endregion

    public static void Log(string message, LogCategory category = LogCategory.General, LogLevel level = LogLevel.Info, Object context = null)
    {
        EnsureInitialized();

        // Early exit if logger is disabled
        if (!isEnabled) return;

        // Filter by log level
        if (level < currentLogLevel) return;

        // Filter by category
        if (useCategoryFiltering && !enabledCategories.Contains(category)) return;

        string logTag;
        string categoryTag = "";
        string formattedMessage;

        if (category != LogCategory.None && category != LogCategory.General)
        {
            categoryTag = $"[{category}]";
        }

        switch (level)
        {
            case LogLevel.Debug:
                logTag = "[Debug]";
                formattedMessage = $"{logTag}{categoryTag} {message}";
                Debug.Log(formattedMessage, context);
                break;
            case LogLevel.Info:
                logTag = $"[Info]";
                formattedMessage = $"{logTag}{categoryTag} {message}";
                Debug.Log(formattedMessage, context);
                break;
            case LogLevel.Warning:
                logTag = $"[Warning]";
                formattedMessage = $"{logTag}{categoryTag} {message}";
                Debug.LogWarning(formattedMessage, context);
                break;
            case LogLevel.Error:
                logTag = $"[Error]";
                formattedMessage = $"{logTag}{categoryTag} {message}";
                Debug.LogError(formattedMessage, context);
                break;
        }
    }

    public static void LogDebug(string message, LogCategory category = LogCategory.General, Object context = null)
    {
        Log(message, category, LogLevel.Debug, context);
    }

    public static void LogInfo(string message, LogCategory category = LogCategory.General, Object context = null)
    {
        Log(message, category, LogLevel.Info, context);
    }

    public static void LogWarning(string message, LogCategory category = LogCategory.General, Object context = null)
    {
        Log(message, category, LogLevel.Warning, context);
    }

    public static void LogError(string message, LogCategory category = LogCategory.General, Object context = null)
    {
        Log(message, category, LogLevel.Error, context);
    }
}