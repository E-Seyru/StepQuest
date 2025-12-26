using UnityEngine;

/// <summary>
/// Attach this to a GameObject in your startup scene to automatically configure Logger from LoggerSettings
/// </summary>
public class LoggerInitializer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private LoggerSettings loggerSettings;

    [Header("Runtime Overrides (Optional)")]
    [SerializeField] private bool overrideSettings = false;
    [SerializeField] private Logger.LogLevel runtimeLogLevel = Logger.LogLevel.Info;

    private void Awake()
    {
        InitializeLogger();
    }

    private void InitializeLogger()
    {
        if (loggerSettings != null)
        {
            loggerSettings.ApplySettings();
            Debug.Log("[LoggerInitializer] Applied LoggerSettings");
        }
        else
        {
            Debug.LogWarning("[LoggerInitializer] No LoggerSettings assigned! Using default Logger configuration.");
        }

        // Apply runtime overrides if specified
        if (overrideSettings)
        {
            Logger.SetLogLevel(runtimeLogLevel);
            Debug.Log($"[LoggerInitializer] Applied runtime override: LogLevel = {runtimeLogLevel}");
        }
    }

    [ContextMenu("Reload Logger Settings")]
    private void ReloadSettings()
    {
        InitializeLogger();
    }
}
