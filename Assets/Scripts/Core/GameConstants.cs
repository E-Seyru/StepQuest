// Purpose: Centralized constants for game configuration
// Filepath: Assets/Scripts/Core/GameConstants.cs

/// <summary>
/// Centralized constants for the entire game.
/// Modify values here instead of hunting through multiple files.
/// </summary>
public static class GameConstants
{
    // ============================================
    // STEP TRACKING & SENSOR
    // ============================================

    /// <summary>Maximum steps accepted in a single update to filter anomalies</summary>
    public const long MaxStepsPerUpdate = 100000;

    /// <summary>Threshold above which step spikes are filtered</summary>
    public const int SensorSpikeThreshold = 50;

    /// <summary>Seconds to wait before accepting another large step update</summary>
    public const int SensorDebounceSeconds = 3;

    /// <summary>Grace period after returning from background before counting steps</summary>
    public const float SensorGracePeriodSeconds = 5.0f;

    /// <summary>Padding to avoid overlap between sensor and API timestamps</summary>
    public const long SensorApiPaddingMs = 1500;

    /// <summary>Safety margin around midnight for step counting</summary>
    public const long MidnightSafetyMs = 500;

    // ============================================
    // VALIDATION THRESHOLDS
    // ============================================

    /// <summary>Maximum acceptable steps delta for anomaly detection</summary>
    public const long MaxAcceptableStepsDelta = 10000;

    /// <summary>Maximum acceptable daily steps for anomaly detection</summary>
    public const long MaxAcceptableDailySteps = 50000;

    // ============================================
    // SAVE INTERVALS
    // ============================================

    /// <summary>Interval between database saves during normal gameplay</summary>
    public const float DatabaseSaveIntervalSeconds = 3.0f;

    /// <summary>Interval between saves during active travel</summary>
    public const float TravelSaveIntervalSeconds = 20f;

    /// <summary>Default auto-save interval for managers</summary>
    public const float DefaultAutoSaveIntervalSeconds = 30f;

    // ============================================
    // SERVICE INITIALIZATION
    // ============================================

    /// <summary>Maximum time to wait for dependent services before timeout</summary>
    public const float ServiceTimeoutSeconds = 30f;

    /// <summary>Polling interval when waiting for services</summary>
    public const float ServicePollIntervalSeconds = 0.5f;

    // ============================================
    // API & NETWORK
    // ============================================

    /// <summary>Maximum retry attempts for API reads</summary>
    public const int MaxApiReadAttempts = 5;

    /// <summary>Base wait time between API retries</summary>
    public const float BaseApiWaitTimeSeconds = 0.5f;

    /// <summary>Frequency of API logging (every N calls)</summary>
    public const int ApiLogFrequency = 60;

    // ============================================
    // GAME DEFAULTS
    // ============================================

    /// <summary>Default starting location for new players</summary>
    public const string DefaultStartingLocationId = "Foret_01";

    // ============================================
    // DEBUG
    // ============================================

    /// <summary>Time window for multi-tap debug actions</summary>
    public const float DebugMultiTapTimeSeconds = 0.5f;

    /// <summary>Number of taps required for debug actions</summary>
    public const int DebugRequiredTaps = 5;
}
