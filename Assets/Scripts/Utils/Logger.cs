using UnityEngine;

public static class Logger
{
    public enum LogLevel { Debug, Info, Warning, Error }
    public enum LogCategory { None, General, StepLog, MapLog, CombatLog, InventoryLog }
    public static LogLevel CurrentLogLevel = LogLevel.Warning;



    public static void Log(string message, LogCategory category = LogCategory.General, LogLevel level = LogLevel.Info, Object context = null)
    {
        if (level < CurrentLogLevel) return;

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