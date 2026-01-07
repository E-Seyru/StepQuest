// Purpose: Panel for exploration activities - shows discoverable content grouped by category
// Filepath: Assets/Scripts/UI/Panels/ExplorationPanelUI.cs
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel that displays exploration information for a location.
/// Shows discoverable content (activities, enemies, NPCs) in three fixed category sections.
/// </summary>
public class ExplorationPanelUI : MonoBehaviour
{
    [Header("UI References - Header")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI locationNameText;
    [SerializeField] private Button closeButton;

    [Header("UI References - Category Sections")]
    [SerializeField] private ExplorationCategorySection activitiesSection;
    [SerializeField] private ExplorationCategorySection enemiesSection;
    [SerializeField] private ExplorationCategorySection npcsSection;

    [Header("UI References - Discovery Chance")]
    [SerializeField] private TextMeshProUGUI discoveryChanceText;

    [Header("UI References - Action")]
    [SerializeField] private Button startExplorationButton;
    [SerializeField] private TextMeshProUGUI startButtonText;

    [Header("Discovery Chance Colors")]
    [SerializeField] private Color colorImpossible = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] private Color colorExtremementFaible = new Color(0.8f, 0.2f, 0.2f);
    [SerializeField] private Color colorTresFaible = new Color(0.9f, 0.4f, 0.2f);
    [SerializeField] private Color colorFaible = new Color(0.9f, 0.6f, 0.2f);
    [SerializeField] private Color colorMoyenne = new Color(0.9f, 0.9f, 0.2f);
    [SerializeField] private Color colorBonne = new Color(0.6f, 0.9f, 0.3f);
    [SerializeField] private Color colorExcellente = new Color(0.2f, 0.9f, 0.2f);

    // Current state
    private MapLocationDefinition currentLocation;
    private LocationActivity currentActivity;

    // Singleton
    public static ExplorationPanelUI Instance { get; private set; }

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
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        if (startExplorationButton != null)
        {
            startExplorationButton.onClick.AddListener(OnStartExplorationClicked);
        }

        gameObject.SetActive(false);
    }

    #region Public Methods

    /// <summary>
    /// Open the exploration panel for a specific location
    /// </summary>
    public void OpenWithLocation(MapLocationDefinition location, LocationActivity activity)
    {
        if (location == null)
        {
            Logger.LogWarning("ExplorationPanelUI: Cannot open with null location!", Logger.LogCategory.ActivityLog);
            return;
        }

        currentLocation = location;
        currentActivity = activity;

        UpdateHeader();
        UpdateContent();
        UpdateDiscoveryChance();
        UpdateActionButton();

        gameObject.SetActive(true);

        Logger.LogInfo($"ExplorationPanelUI: Opened for {location.DisplayName}", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Close the panel
    /// </summary>
    public void ClosePanel()
    {
        gameObject.SetActive(false);
        ClearAllSections();

        // Slide activities section back in
        if (LocationDetailsPanel.Instance != null)
        {
            LocationDetailsPanel.Instance.SlideInActivitiesSection();
        }

        Logger.LogInfo("ExplorationPanelUI: Panel closed", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Refresh the panel with current data
    /// </summary>
    public void RefreshPanel()
    {
        if (currentLocation == null) return;

        UpdateContent();
        UpdateDiscoveryChance();
        UpdateActionButton();
    }

    #endregion

    #region Private Methods - UI Updates

    /// <summary>
    /// Update header section
    /// </summary>
    private void UpdateHeader()
    {
        if (titleText != null)
        {
            titleText.text = "Exploration";
        }

        if (locationNameText != null)
        {
            locationNameText.text = currentLocation.DisplayName;
        }
    }

    /// <summary>
    /// Update content in all category sections
    /// </summary>
    private void UpdateContent()
    {
        // Get discoverable content for each category
        var activities = GetDiscoverableActivities();
        var enemies = GetDiscoverableEnemies();
        var npcs = GetDiscoverableNPCs();

        // Populate each section (sections handle their own empty state display)
        activitiesSection?.Populate(activities);
        enemiesSection?.Populate(enemies);
        npcsSection?.Populate(npcs);
    }

    /// <summary>
    /// Update discovery chance display based on best available chance
    /// </summary>
    private void UpdateDiscoveryChance()
    {
        if (discoveryChanceText == null) return;

        // Get all undiscovered items
        var allUndiscovered = GetAllUndiscoveredContent();

        if (allUndiscovered.Count == 0)
        {
            // Everything discovered
            discoveryChanceText.text = "Exploration : <color=#33E633>Complete !</color>";
            return;
        }

        // Find the best (highest) discovery chance among undiscovered items
        float bestChance = 0f;
        foreach (var item in allUndiscovered)
        {
            float baseChance = GameConstants.GetBaseDiscoveryChance(item.Rarity);
            float modifiedChance = GetModifiedDiscoveryChance(baseChance);
            if (modifiedChance > bestChance)
            {
                bestChance = modifiedChance;
            }
        }

        // Convert to display text and color
        var (chanceText, chanceColor) = GetChanceDisplayInfo(bestChance);
        string colorHex = ColorUtility.ToHtmlStringRGB(chanceColor);

        discoveryChanceText.text = $"Chances de decouverte : <color=#{colorHex}>{chanceText}</color>";
    }

    /// <summary>
    /// Get modified discovery chance based on player's exploration skill
    /// </summary>
    private float GetModifiedDiscoveryChance(float baseChance)
    {
        // Get exploration skill level
        int explorationLevel = 1;
        if (DataManager.Instance?.PlayerData != null)
        {
            var skills = DataManager.Instance.PlayerData.Skills;
            if (skills != null && skills.TryGetValue("exploration", out var skillData))
            {
                explorationLevel = skillData.Level;
            }
        }

        // +2% per level, capped at +100%
        float bonus = Mathf.Min((explorationLevel - 1) * 0.02f, 1.0f);
        return baseChance * (1f + bonus);
    }

    /// <summary>
    /// Convert chance value to display text and color
    /// </summary>
    private (string text, Color color) GetChanceDisplayInfo(float chance)
    {
        // Thresholds for chance levels
        if (chance < 0.001f)
            return ("Impossible", colorImpossible);
        else if (chance < 0.005f)
            return ("Extremement faible", colorExtremementFaible);
        else if (chance < 0.015f)
            return ("Tres faible", colorTresFaible);
        else if (chance < 0.03f)
            return ("Faible", colorFaible);
        else if (chance < 0.06f)
            return ("Moyenne", colorMoyenne);
        else if (chance < 0.10f)
            return ("Bonne", colorBonne);
        else
            return ("Excellente", colorExcellente);
    }

    /// <summary>
    /// Update action button state
    /// </summary>
    private void UpdateActionButton()
    {
        if (startExplorationButton == null) return;

        var allUndiscovered = GetAllUndiscoveredContent();
        bool hasUndiscovered = allUndiscovered.Count > 0;

        startExplorationButton.interactable = true;

        if (startButtonText != null)
        {
            if (!hasUndiscovered)
            {
                startButtonText.text = "Continuer (XP)";
            }
            else
            {
                startButtonText.text = "Commencer l'exploration";
            }
        }
    }

    /// <summary>
    /// Clear all section content
    /// </summary>
    private void ClearAllSections()
    {
        activitiesSection?.ClearItems();
        enemiesSection?.ClearItems();
        npcsSection?.ClearItems();
    }

    #endregion

    #region Private Methods - Data Retrieval

    /// <summary>
    /// Get discoverable activities at this location
    /// </summary>
    private List<DiscoverableInfo> GetDiscoverableActivities()
    {
        var list = new List<DiscoverableInfo>();
        if (currentLocation?.AvailableActivities == null) return list;

        foreach (var activity in currentLocation.AvailableActivities)
        {
            if (activity == null || !activity.IsHidden || activity.ActivityReference == null) continue;

            bool isDiscovered = IsDiscovered(activity.GetDiscoveryID());
            list.Add(new DiscoverableInfo
            {
                Id = activity.GetDiscoveryID(),
                Name = activity.GetDisplayName(),
                Type = DiscoverableType.Activity,
                Rarity = activity.Rarity,
                BonusXP = activity.GetDiscoveryBonusXP(),
                IsDiscovered = isDiscovered,
                Icon = activity.GetIcon()
            });
        }
        return list;
    }

    /// <summary>
    /// Get discoverable enemies at this location
    /// </summary>
    private List<DiscoverableInfo> GetDiscoverableEnemies()
    {
        var list = new List<DiscoverableInfo>();
        if (currentLocation?.AvailableEnemies == null) return list;

        foreach (var enemy in currentLocation.AvailableEnemies)
        {
            if (enemy == null || !enemy.IsHidden || enemy.EnemyReference == null) continue;

            bool isDiscovered = IsDiscovered(enemy.GetDiscoveryID());
            list.Add(new DiscoverableInfo
            {
                Id = enemy.GetDiscoveryID(),
                Name = enemy.EnemyReference.GetDisplayName(),
                Type = DiscoverableType.Enemy,
                Rarity = enemy.Rarity,
                BonusXP = enemy.GetDiscoveryBonusXP(),
                IsDiscovered = isDiscovered,
                Icon = enemy.EnemyReference.GetSilhouetteIcon()
            });
        }
        return list;
    }

    /// <summary>
    /// Get discoverable NPCs at this location
    /// </summary>
    private List<DiscoverableInfo> GetDiscoverableNPCs()
    {
        var list = new List<DiscoverableInfo>();
        if (currentLocation?.AvailableNPCs == null) return list;

        foreach (var npc in currentLocation.AvailableNPCs)
        {
            if (npc == null || !npc.IsHidden || npc.NPCReference == null) continue;

            bool isDiscovered = IsDiscovered(npc.GetDiscoveryID());
            list.Add(new DiscoverableInfo
            {
                Id = npc.GetDiscoveryID(),
                Name = npc.NPCReference.GetDisplayName(),
                Type = DiscoverableType.NPC,
                Rarity = npc.Rarity,
                BonusXP = npc.GetDiscoveryBonusXP(),
                IsDiscovered = isDiscovered,
                Icon = npc.NPCReference.GetSilhouetteIcon()
            });
        }
        return list;
    }

    /// <summary>
    /// Get all undiscovered content across all categories
    /// </summary>
    private List<DiscoverableInfo> GetAllUndiscoveredContent()
    {
        var all = new List<DiscoverableInfo>();
        all.AddRange(GetDiscoverableActivities().Where(i => !i.IsDiscovered));
        all.AddRange(GetDiscoverableEnemies().Where(i => !i.IsDiscovered));
        all.AddRange(GetDiscoverableNPCs().Where(i => !i.IsDiscovered));
        return all;
    }

    /// <summary>
    /// Check if a specific item has been discovered
    /// </summary>
    private bool IsDiscovered(string discoveryId)
    {
        if (currentLocation == null || string.IsNullOrEmpty(discoveryId)) return false;

        if (ExplorationManager.Instance != null)
        {
            return ExplorationManager.Instance.IsDiscoveredAtLocation(currentLocation.LocationID, discoveryId);
        }

        if (DataManager.Instance?.PlayerData != null)
        {
            return DataManager.Instance.PlayerData.HasDiscoveredAtLocation(currentLocation.LocationID, discoveryId);
        }

        return false;
    }

    #endregion

    #region Private Methods - Actions

    /// <summary>
    /// Handle start exploration button click
    /// </summary>
    private void OnStartExplorationClicked()
    {
        if (currentLocation == null || currentActivity == null) return;

        // Check if we can start an activity
        if (ActivityManager.Instance == null)
        {
            Logger.LogError("ExplorationPanelUI: ActivityManager not found!", Logger.LogCategory.ActivityLog);
            return;
        }

        if (!ActivityManager.Instance.CanStartActivity())
        {
            Logger.LogWarning("ExplorationPanelUI: Cannot start activity - another activity is in progress", Logger.LogCategory.ActivityLog);
            return;
        }

        // Get activity and variant IDs
        string activityId = currentActivity.ActivityReference?.ActivityID;
        string variantId = GetExplorationVariantId();

        if (string.IsNullOrEmpty(activityId) || string.IsNullOrEmpty(variantId))
        {
            Logger.LogError("ExplorationPanelUI: Invalid activity or variant ID!", Logger.LogCategory.ActivityLog);
            return;
        }

        // Start the exploration activity via ActivityManager
        bool success = ActivityManager.Instance.StartActivity(activityId, variantId);

        if (success)
        {
            // Also notify ExplorationManager to track discovery state
            ExplorationManager.Instance?.StartExploration(currentLocation);

            Logger.LogInfo($"ExplorationPanelUI: Started exploration at {currentLocation.DisplayName}", Logger.LogCategory.ActivityLog);

            // Close the panel - ActivityDisplayPanel will show progress
            ClosePanel();
        }
        else
        {
            Logger.LogWarning("ExplorationPanelUI: Failed to start exploration activity", Logger.LogCategory.ActivityLog);
        }
    }

    /// <summary>
    /// Get a valid exploration variant ID for this location
    /// </summary>
    private string GetExplorationVariantId()
    {
        // Try to get from the LocationActivity's ActivityVariants
        if (currentActivity.ActivityVariants != null && currentActivity.ActivityVariants.Count > 0)
        {
            var firstVariant = currentActivity.ActivityVariants[0];
            if (firstVariant != null && !string.IsNullOrEmpty(firstVariant.VariantName))
            {
                return ActivityRegistry.GenerateVariantId(firstVariant.VariantName);
            }
        }

        // Last resort: use a default exploration variant name
        return "exploration_default";
    }

    #endregion

    void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
        }

        if (startExplorationButton != null)
        {
            startExplorationButton.onClick.RemoveListener(OnStartExplorationClicked);
        }
    }
}

/// <summary>
/// Types of content that can be discovered through exploration
/// </summary>
public enum DiscoverableType
{
    Activity,
    Enemy,
    NPC,
    Dungeon
}

/// <summary>
/// Information about a discoverable item for UI display
/// </summary>
public struct DiscoverableInfo
{
    public string Id;
    public string Name;
    public DiscoverableType Type;
    public DiscoveryRarity Rarity;
    public int BonusXP;
    public bool IsDiscovered;
    public Sprite Icon;
}
