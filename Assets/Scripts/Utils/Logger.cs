using UnityEngine;

public static class Logger
{
    public enum LogLevel { Debug, Info, Warning, Error }
    public static LogLevel CurrentLogLevel = LogLevel.Debug;

    // Color definitions (matching those in DebugLogPanel)
    private static readonly string infoColor = "#3399FF";     // Blue
    private static readonly string warningColor = "#FFCC00";  // Yellow
    private static readonly string errorColor = "#FF3333";    // Red

    public static void Log(string message, LogLevel level = LogLevel.Info, Object context = null)
    {
        if (level < CurrentLogLevel) return;

        string logTag;
        string formattedMessage;

        switch (level)
        {
            case LogLevel.Debug:
                logTag = "[Debug]";
                formattedMessage = $"{logTag} {message}";
                Debug.Log(formattedMessage, context);
                break;
            case LogLevel.Info:
                logTag = $"[Info]";
                formattedMessage = $"{logTag} {message}";
                Debug.Log(formattedMessage, context);
                break;
            case LogLevel.Warning:
                logTag = $"[Warning]";
                formattedMessage = $"{logTag} {message}";
                Debug.LogWarning(formattedMessage, context);
                break;
            case LogLevel.Error:
                logTag = $"[Error]";
                formattedMessage = $"{logTag} {message}";
                Debug.LogError(formattedMessage, context);
                break;
        }
    }

    public static void LogDebug(string message, Object context = null)
    {
        Log(message, LogLevel.Debug, context);
    }

    public static void LogInfo(string message, Object context = null)
    {
        Log(message, LogLevel.Info, context);
    }

    public static void LogWarning(string message, Object context = null)
    {
        Log(message, LogLevel.Warning, context);
    }

    public static void LogError(string message, Object context = null)
    {
        Log(message, LogLevel.Error, context);
    }
}