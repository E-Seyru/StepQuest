// Purpose: Provides a simple, centralized logging utility. Can be expanded for different log levels, outputs etc.
// Filepath: Assets/Scripts/Utils/Logger.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public static class Logger
{
    public enum LogLevel { Debug, Info, Warning, Error }


    public static LogLevel CurrentLogLevel = LogLevel.Debug;





    public static void Log(string message, LogLevel level = LogLevel.Info, Object context = null)
    {
        if (level < CurrentLogLevel) return;

        string formattedMessage = $"[{level}] {message}";

        switch (level)
        {
            case LogLevel.Debug:
            case LogLevel.Info:
                Debug.Log(formattedMessage, context);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(formattedMessage, context);
                break;
            case LogLevel.Error:
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