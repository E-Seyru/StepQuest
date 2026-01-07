// Purpose: Manager for exploration activities - handles discovery logic and tracking
// Filepath: Assets/Scripts/Gameplay/Exploration/ExplorationManager.cs
using ActivityEvents;
using ExplorationEvents;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages exploration activities at locations.
/// Handles discovery rolls, progress tracking, and XP rewards.
/// </summary>
public class ExplorationManager : MonoBehaviour
{
    // Singleton
    public static ExplorationManager Instance { get; private set; }

    // References
    private DataManager dataManager;

    // Current exploration state
    private bool isExploring = false;
    private string currentLocationId;
    private MapLocationDefinition currentLocation;
    private int currentTick = 0;
    private int totalXPGained = 0;
    private int discoveriesThisSession = 0;
    private List<DiscoveryResult> sessionDiscoveries = new List<DiscoveryResult>();

    // Runtime modifiers (can be set by stats, equipment, buffs, etc.)
    private float explorationSpeedModifier = 1.0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        dataManager = DataManager.Instance;

        if (dataManager == null)
        {
            Logger.LogError("ExplorationManager: DataManager not found!", Logger.LogCategory.ActivityLog);
        }

        // Subscribe to activity events to auto-stop exploration when activity stops
        EventBus.Subscribe<ActivityStoppedEvent>(OnActivityStopped);
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<ActivityStoppedEvent>(OnActivityStopped);
    }

    /// <summary>
    /// Called when an activity stops - stop exploration if it was an exploration activity
    /// </summary>
    private void OnActivityStopped(ActivityStoppedEvent eventData)
    {
        if (isExploring)
        {
            // For exploration, always show results (even when manually stopped)
            // since the player walked and may have discovered things
            StopExploration(completed: true);
        }
    }

    #region Public Methods

    /// <summary>
    /// Start exploring at a specific location
    /// </summary>
    public void StartExploration(MapLocationDefinition location)
    {
        if (location == null)
        {
            Logger.LogError("ExplorationManager: Cannot start exploration with null location!", Logger.LogCategory.ActivityLog);
            return;
        }

        if (isExploring)
        {
            Logger.LogWarning("ExplorationManager: Already exploring! Stop current exploration first.", Logger.LogCategory.ActivityLog);
            return;
        }

        currentLocation = location;
        currentLocationId = location.LocationID;
        currentTick = 0;
        totalXPGained = 0;
        discoveriesThisSession = 0;
        sessionDiscoveries.Clear();
        isExploring = true;

        // Publish start event
        EventBus.Publish(new ExplorationStartedEvent(currentLocationId, currentLocation));

        Logger.LogInfo($"ExplorationManager: Started exploration at {location.DisplayName}", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Process an exploration tick (called when player takes steps while exploring)
    /// </summary>
    public void ProcessExplorationTick()
    {
        if (!isExploring || currentLocation == null) return;

        currentTick++;

        // Roll for discoveries
        ProcessDiscoveryRolls();

        // Award base exploration XP
        int baseXP = GameConstants.ExplorationBaseXPPerTick;
        totalXPGained += baseXP;

        // TODO: Add XP to player's Exploration skill via XpManager

        // Publish tick event
        EventBus.Publish(new ExplorationTickEvent(currentLocationId, currentTick, -1)); // -1 for unlimited ticks

        Logger.LogInfo($"ExplorationManager: Tick {currentTick} at {currentLocation.DisplayName}, total XP: {totalXPGained}", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Stop the current exploration
    /// </summary>
    public void StopExploration(bool completed = true)
    {
        if (!isExploring) return;

        // Publish end event
        EventBus.Publish(new ExplorationEndedEvent(
            currentLocationId,
            currentTick,
            discoveriesThisSession,
            totalXPGained,
            completed
        ));

        Logger.LogInfo($"ExplorationManager: Exploration {(completed ? "completed" : "cancelled")} at {currentLocation?.DisplayName ?? "unknown"}. " +
            $"Ticks: {currentTick}, Discoveries: {discoveriesThisSession}, XP: {totalXPGained}", Logger.LogCategory.ActivityLog);

        // Show results panel if exploration was completed (not cancelled)
        if (completed)
        {
            // Create a copy of discoveries to pass to the panel
            var discoveriesToShow = new List<DiscoveryResult>(sessionDiscoveries);

            // First open the ExplorationPanelUI, then show results
            if (ExplorationPanelUI.Instance != null && currentLocation != null)
            {
                ExplorationPanelUI.Instance.ShowResultsAfterExploration(currentLocation, discoveriesToShow);
            }
            else if (ExplorationResultsPanel.Instance != null)
            {
                // Fallback: show results panel directly
                ExplorationResultsPanel.Instance.ShowResults(discoveriesToShow);
            }
        }

        // Reset state
        isExploring = false;
        currentLocation = null;
        currentLocationId = null;
        currentTick = 0;
        totalXPGained = 0;
        discoveriesThisSession = 0;
        sessionDiscoveries.Clear();
    }

    /// <summary>
    /// Check if currently exploring
    /// </summary>
    public bool IsExploring()
    {
        return isExploring;
    }

    /// <summary>
    /// Get current exploration location
    /// </summary>
    public MapLocationDefinition GetCurrentLocation()
    {
        return currentLocation;
    }

    /// <summary>
    /// Get the current exploration speed modifier (1.0 = normal, lower = faster, higher = slower)
    /// </summary>
    public float GetExplorationSpeedModifier()
    {
        return explorationSpeedModifier;
    }

    /// <summary>
    /// Set the exploration speed modifier (affects steps per tick)
    /// Can be called by stat systems, equipment bonuses, buffs, etc.
    /// </summary>
    /// <param name="modifier">1.0 = normal speed, 0.5 = twice as fast, 2.0 = half speed</param>
    public void SetExplorationSpeedModifier(float modifier)
    {
        explorationSpeedModifier = Mathf.Max(0.1f, modifier); // Minimum 0.1x to prevent zero/negative
        Logger.LogInfo($"ExplorationManager: Speed modifier set to {explorationSpeedModifier:F2}", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Add to the current exploration speed modifier (stacks additively)
    /// </summary>
    /// <param name="bonus">Positive = slower, Negative = faster (e.g., -0.1 = 10% faster)</param>
    public void AddExplorationSpeedBonus(float bonus)
    {
        explorationSpeedModifier = Mathf.Max(0.1f, explorationSpeedModifier + bonus);
    }

    /// <summary>
    /// Reset the exploration speed modifier to default (1.0)
    /// </summary>
    public void ResetExplorationSpeedModifier()
    {
        explorationSpeedModifier = 1.0f;
    }

    /// <summary>
    /// Calculate the effective steps per tick for the current exploration
    /// Takes into account base value from variant and any active modifiers
    /// </summary>
    public int GetEffectiveStepsPerTick(ActivityVariant variant)
    {
        if (variant == null) return GameConstants.ExplorationStepsPerTick;
        return variant.GetEffectiveActionCost(explorationSpeedModifier);
    }

    /// <summary>
    /// Manually trigger a discovery (for testing or quest rewards)
    /// </summary>
    public bool TriggerDiscovery(string locationId, string discoveryId, DiscoverableType type,
        DiscoveryRarity rarity, string displayName, Sprite icon = null, string flavorText = null)
    {
        if (dataManager?.PlayerData == null) return false;

        // Check if already discovered
        if (dataManager.PlayerData.HasDiscoveredAtLocation(locationId, discoveryId))
        {
            Logger.LogInfo($"ExplorationManager: {discoveryId} already discovered at {locationId}", Logger.LogCategory.ActivityLog);
            return false;
        }

        // Add discovery
        dataManager.PlayerData.AddDiscoveryAtLocation(locationId, discoveryId);

        // Calculate bonus XP
        int bonusXP = GameConstants.GetDiscoveryBonusXP(rarity);

        // TODO: Award XP via XpManager

        // Track this discovery for the results panel
        sessionDiscoveries.Add(new DiscoveryResult
        {
            Id = discoveryId,
            Name = displayName,
            Type = type,
            Rarity = rarity,
            Icon = icon,
            FlavorText = flavorText,
            BonusXP = bonusXP
        });

        // Publish discovery event
        EventBus.Publish(new ExplorationDiscoveryEvent(
            locationId,
            discoveryId,
            type,
            rarity,
            bonusXP,
            displayName
        ));

        // Update progress event
        var location = MapManager.Instance?.LocationRegistry?.GetLocationById(locationId);
        if (location != null)
        {
            int discovered = GetDiscoveredCountAtLocation(location);
            int total = GetTotalDiscoverableAtLocation(location);
            EventBus.Publish(new ExplorationProgressChangedEvent(locationId, discovered, total));
        }

        Logger.LogInfo($"ExplorationManager: Discovered {type} '{displayName}' ({rarity}) at {locationId}! +{bonusXP} XP", Logger.LogCategory.ActivityLog);

        return true;
    }

    /// <summary>
    /// Get total discoverable content count at a location
    /// </summary>
    public int GetTotalDiscoverableAtLocation(MapLocationDefinition location)
    {
        if (location == null) return 0;

        int count = 0;

        // Count hidden enemies
        if (location.AvailableEnemies != null)
        {
            foreach (var enemy in location.AvailableEnemies)
            {
                if (enemy != null && enemy.IsHidden)
                    count++;
            }
        }

        // Count hidden NPCs
        if (location.AvailableNPCs != null)
        {
            foreach (var npc in location.AvailableNPCs)
            {
                if (npc != null && npc.IsHidden)
                    count++;
            }
        }

        // Count hidden activities
        if (location.AvailableActivities != null)
        {
            foreach (var activity in location.AvailableActivities)
            {
                if (activity != null && activity.IsHidden)
                    count++;
            }
        }

        // TODO: Add dungeons when implemented

        return count;
    }

    /// <summary>
    /// Get discovered count at a location
    /// </summary>
    public int GetDiscoveredCountAtLocation(MapLocationDefinition location)
    {
        if (location == null || dataManager?.PlayerData == null) return 0;

        return dataManager.PlayerData.GetDiscoveryCountAtLocation(location.LocationID);
    }

    /// <summary>
    /// Check if something is discovered at a location
    /// </summary>
    public bool IsDiscoveredAtLocation(string locationId, string discoveryId)
    {
        if (dataManager?.PlayerData == null) return false;
        return dataManager.PlayerData.HasDiscoveredAtLocation(locationId, discoveryId);
    }

    /// <summary>
    /// Get all undiscovered content at a location
    /// </summary>
    public List<DiscoverableInfo> GetUndiscoveredContent(MapLocationDefinition location)
    {
        var undiscovered = new List<DiscoverableInfo>();
        if (location == null || dataManager?.PlayerData == null) return undiscovered;

        string locationId = location.LocationID;

        // Check hidden enemies
        if (location.AvailableEnemies != null)
        {
            foreach (var enemy in location.AvailableEnemies)
            {
                if (enemy != null && enemy.IsHidden && enemy.EnemyReference != null)
                {
                    if (!dataManager.PlayerData.HasDiscoveredAtLocation(locationId, enemy.GetDiscoveryID()))
                    {
                        undiscovered.Add(new DiscoverableInfo
                        {
                            Id = enemy.GetDiscoveryID(),
                            Name = enemy.EnemyReference.GetDisplayName(),
                            Type = DiscoverableType.Enemy,
                            Rarity = enemy.Rarity,
                            BonusXP = enemy.GetDiscoveryBonusXP(),
                            IsDiscovered = false,
                            Icon = enemy.EnemyReference.Avatar
                        });
                    }
                }
            }
        }

        // Check hidden NPCs
        if (location.AvailableNPCs != null)
        {
            foreach (var npc in location.AvailableNPCs)
            {
                if (npc != null && npc.IsHidden && npc.NPCReference != null)
                {
                    if (!dataManager.PlayerData.HasDiscoveredAtLocation(locationId, npc.GetDiscoveryID()))
                    {
                        undiscovered.Add(new DiscoverableInfo
                        {
                            Id = npc.GetDiscoveryID(),
                            Name = npc.NPCReference.GetDisplayName(),
                            Type = DiscoverableType.NPC,
                            Rarity = npc.Rarity,
                            BonusXP = npc.GetDiscoveryBonusXP(),
                            IsDiscovered = false,
                            Icon = npc.NPCReference.Avatar
                        });
                    }
                }
            }
        }

        // Check hidden activities
        if (location.AvailableActivities != null)
        {
            foreach (var activity in location.AvailableActivities)
            {
                if (activity != null && activity.IsHidden && activity.ActivityReference != null)
                {
                    if (!dataManager.PlayerData.HasDiscoveredAtLocation(locationId, activity.GetDiscoveryID()))
                    {
                        undiscovered.Add(new DiscoverableInfo
                        {
                            Id = activity.GetDiscoveryID(),
                            Name = activity.GetDisplayName(),
                            Type = DiscoverableType.Activity,
                            Rarity = activity.Rarity,
                            BonusXP = activity.GetDiscoveryBonusXP(),
                            IsDiscovered = false,
                            Icon = activity.GetIcon()
                        });
                    }
                }
            }
        }

        return undiscovered;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Process discovery rolls for all undiscovered content
    /// </summary>
    private void ProcessDiscoveryRolls()
    {
        if (currentLocation == null || dataManager?.PlayerData == null) return;

        // Get player's exploration skill level for chance bonus
        int explorationLevel = dataManager.PlayerData.GetSkillLevel("exploration");
        float levelBonus = GetLevelBonus(explorationLevel);

        // Get undiscovered content
        var undiscovered = GetUndiscoveredContent(currentLocation);

        foreach (var discoverable in undiscovered)
        {
            // Calculate discovery chance
            float baseChance = GameConstants.GetBaseDiscoveryChance(discoverable.Rarity);
            float finalChance = baseChance * (1f + levelBonus);

            // Roll
            float roll = Random.value;

            if (roll < finalChance)
            {
                // Discovery!
                bool success = TriggerDiscovery(
                    currentLocationId,
                    discoverable.Id,
                    discoverable.Type,
                    discoverable.Rarity,
                    discoverable.Name,
                    discoverable.Icon // Pass the icon for the results panel
                );

                if (success)
                {
                    discoveriesThisSession++;
                    totalXPGained += discoverable.BonusXP;
                }
            }
        }
    }

    /// <summary>
    /// Calculate level bonus for discovery chance
    /// </summary>
    private float GetLevelBonus(int level)
    {
        // Each level gives 2% bonus, capped at 100% (level 50)
        return Mathf.Min(level * 0.02f, 1.0f);
    }

    #endregion
}
