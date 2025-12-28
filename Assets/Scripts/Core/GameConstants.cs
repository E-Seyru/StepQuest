// Purpose: Centralized constants for game configuration
// Filepath: Assets/Scripts/Core/GameConstants.cs

/// <summary>
/// Rarity levels for discoverable content (enemies, NPCs, dungeons, etc.)
/// Higher rarity = lower discovery chance, higher bonus XP
/// </summary>
public enum DiscoveryRarity
{
    Common,     // ~8% base chance per tick
    Uncommon,   // ~4% base chance per tick
    Rare,       // ~2% base chance per tick
    Epic,       // ~0.8% base chance per tick
    Legendary   // ~0.3% base chance per tick
}

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
    public const float SensorGracePeriodSeconds = 1.0f;

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
    public const string DefaultStartingLocationId = "foret_01";

    // ============================================
    // INVENTORY CONTAINERS
    // ============================================

    /// <summary>Player's main inventory container ID</summary>
    public const string ContainerIdPlayer = "player";

    /// <summary>Bank storage container ID</summary>
    public const string ContainerIdBank = "bank";

    // ============================================
    // DEBUG
    // ============================================

    /// <summary>Time window for multi-tap debug actions</summary>
    public const float DebugMultiTapTimeSeconds = 0.5f;

    /// <summary>Number of taps required for debug actions</summary>
    public const int DebugRequiredTaps = 5;

    // ============================================
    // EXPLORATION & DISCOVERY
    // ============================================

    /// <summary>Base discovery chance per tick for Common rarity (0-1)</summary>
    public const float DiscoveryChanceCommon = 0.08f;

    /// <summary>Base discovery chance per tick for Uncommon rarity (0-1)</summary>
    public const float DiscoveryChanceUncommon = 0.04f;

    /// <summary>Base discovery chance per tick for Rare rarity (0-1)</summary>
    public const float DiscoveryChanceRare = 0.02f;

    /// <summary>Base discovery chance per tick for Epic rarity (0-1)</summary>
    public const float DiscoveryChanceEpic = 0.008f;

    /// <summary>Base discovery chance per tick for Legendary rarity (0-1)</summary>
    public const float DiscoveryChanceLegendary = 0.003f;

    /// <summary>Bonus XP multiplier for Common discovery</summary>
    public const int DiscoveryXPCommon = 10;

    /// <summary>Bonus XP multiplier for Uncommon discovery</summary>
    public const int DiscoveryXPUncommon = 25;

    /// <summary>Bonus XP multiplier for Rare discovery</summary>
    public const int DiscoveryXPRare = 50;

    /// <summary>Bonus XP multiplier for Epic discovery</summary>
    public const int DiscoveryXPEpic = 150;

    /// <summary>Bonus XP multiplier for Legendary discovery</summary>
    public const int DiscoveryXPLegendary = 500;

    /// <summary>Base XP gained per exploration tick</summary>
    public const int ExplorationBaseXPPerTick = 5;

    /// <summary>Steps required per exploration tick</summary>
    public const int ExplorationStepsPerTick = 50;

    /// <summary>
    /// Get base discovery chance for a given rarity
    /// </summary>
    public static float GetBaseDiscoveryChance(DiscoveryRarity rarity)
    {
        return rarity switch
        {
            DiscoveryRarity.Common => DiscoveryChanceCommon,
            DiscoveryRarity.Uncommon => DiscoveryChanceUncommon,
            DiscoveryRarity.Rare => DiscoveryChanceRare,
            DiscoveryRarity.Epic => DiscoveryChanceEpic,
            DiscoveryRarity.Legendary => DiscoveryChanceLegendary,
            _ => DiscoveryChanceCommon
        };
    }

    /// <summary>
    /// Get bonus XP for discovering content of a given rarity
    /// </summary>
    public static int GetDiscoveryBonusXP(DiscoveryRarity rarity)
    {
        return rarity switch
        {
            DiscoveryRarity.Common => DiscoveryXPCommon,
            DiscoveryRarity.Uncommon => DiscoveryXPUncommon,
            DiscoveryRarity.Rare => DiscoveryXPRare,
            DiscoveryRarity.Epic => DiscoveryXPEpic,
            DiscoveryRarity.Legendary => DiscoveryXPLegendary,
            _ => DiscoveryXPCommon
        };
    }
}
