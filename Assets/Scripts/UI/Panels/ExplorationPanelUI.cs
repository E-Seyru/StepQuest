// Purpose: Panel for exploration activities - shows discoverable content and exploration progress
// Filepath: Assets/Scripts/UI/Panels/ExplorationPanelUI.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel that displays exploration information for a location.
/// Shows discoverable content (enemies, NPCs, dungeons), discovery chances, and exploration progress.
/// </summary>
public class ExplorationPanelUI : MonoBehaviour
{
    [Header("UI References - Header")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI locationNameText;
    [SerializeField] private Image locationImage;
    [SerializeField] private Button closeButton;

    [Header("UI References - Progress")]
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI discoveredCountText;

    [Header("UI References - Discoverable Content")]
    [SerializeField] private Transform discoverableContainer;
    [SerializeField] private GameObject discoverableItemPrefab;
    [SerializeField] private TextMeshProUGUI noDiscoverablesText;

    [Header("UI References - Help Section")]
    [SerializeField] private GameObject helpSection;
    [SerializeField] private TextMeshProUGUI helpText;

    [Header("UI References - Action")]
    [SerializeField] private Button startExplorationButton;
    [SerializeField] private TextMeshProUGUI startButtonText;

    // Current state
    private MapLocationDefinition currentLocation;
    private LocationActivity currentActivity;
    private List<GameObject> instantiatedItems = new List<GameObject>();

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
        // Setup buttons
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        if (startExplorationButton != null)
        {
            startExplorationButton.onClick.AddListener(OnStartExplorationClicked);
        }

        // Start hidden
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
        UpdateProgress();
        UpdateDiscoverableContent();
        UpdateHelpSection();
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
        ClearInstantiatedItems();

        Logger.LogInfo("ExplorationPanelUI: Panel closed", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Refresh the panel with current data
    /// </summary>
    public void RefreshPanel()
    {
        if (currentLocation == null) return;

        UpdateProgress();
        UpdateDiscoverableContent();
        UpdateActionButton();
    }

    #endregion

    #region Private Methods - UI Updates

    /// <summary>
    /// Update header section (title, location name, image)
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

        if (locationImage != null && currentLocation.LocationImage != null)
        {
            locationImage.sprite = currentLocation.LocationImage;
            locationImage.color = Color.white;
        }
    }

    /// <summary>
    /// Update exploration progress section
    /// </summary>
    private void UpdateProgress()
    {
        // Get discovery stats from PlayerData
        int totalDiscoverable = GetTotalDiscoverableCount();
        int discovered = GetDiscoveredCount();
        float progressPercent = totalDiscoverable > 0 ? (float)discovered / totalDiscoverable : 0f;

        if (progressText != null)
        {
            if (totalDiscoverable == 0)
            {
                progressText.text = "Rien a decouvrir ici";
            }
            else if (discovered >= totalDiscoverable)
            {
                progressText.text = "Exploration complete !";
            }
            else
            {
                progressText.text = $"Progression: {progressPercent * 100f:F0}%";
            }
        }

        if (progressSlider != null)
        {
            progressSlider.value = progressPercent;
        }

        if (discoveredCountText != null)
        {
            discoveredCountText.text = $"{discovered} / {totalDiscoverable} decouvertes";
        }
    }

    /// <summary>
    /// Update discoverable content list
    /// </summary>
    private void UpdateDiscoverableContent()
    {
        ClearInstantiatedItems();

        if (discoverableContainer == null) return;

        // Get all discoverable content at this location
        var discoverables = GetAllDiscoverableContent();

        if (discoverables.Count == 0)
        {
            if (noDiscoverablesText != null)
            {
                noDiscoverablesText.gameObject.SetActive(true);
                noDiscoverablesText.text = "Aucun contenu a decouvrir a cet endroit.";
            }
            return;
        }

        if (noDiscoverablesText != null)
        {
            noDiscoverablesText.gameObject.SetActive(false);
        }

        // Create UI items for each discoverable
        foreach (var discoverable in discoverables)
        {
            CreateDiscoverableItem(discoverable);
        }

        // Force layout rebuild
        LayoutRebuilder.ForceRebuildLayoutImmediate(discoverableContainer.GetComponent<RectTransform>());
    }

    /// <summary>
    /// Update help section with discovery chances
    /// </summary>
    private void UpdateHelpSection()
    {
        if (helpSection == null || helpText == null) return;

        string help = "Chances de decouverte par tick:\n";
        help += $"- Commun: {GameConstants.DiscoveryChanceCommon * 100f:F1}%\n";
        help += $"- Peu commun: {GameConstants.DiscoveryChanceUncommon * 100f:F1}%\n";
        help += $"- Rare: {GameConstants.DiscoveryChanceRare * 100f:F1}%\n";
        help += $"- epique: {GameConstants.DiscoveryChanceEpic * 100f:F1}%\n";
        help += $"- Legendaire: {GameConstants.DiscoveryChanceLegendary * 100f:F1}%\n\n";
        help += "Le niveau d'Exploration augmente vos chances !";

        helpText.text = help;
    }

    /// <summary>
    /// Update action button state
    /// </summary>
    private void UpdateActionButton()
    {
        if (startExplorationButton == null) return;

        int totalDiscoverable = GetTotalDiscoverableCount();
        int discovered = GetDiscoveredCount();
        bool isFullyExplored = discovered >= totalDiscoverable && totalDiscoverable > 0;

        // Button is always available (can grind XP even after 100%)
        startExplorationButton.interactable = true;

        if (startButtonText != null)
        {
            if (totalDiscoverable == 0)
            {
                startButtonText.text = "Explorer";
            }
            else if (isFullyExplored)
            {
                startButtonText.text = "Continuer l'exploration (XP)";
            }
            else
            {
                startButtonText.text = "Commencer l'exploration";
            }
        }
    }

    #endregion

    #region Private Methods - Data

    /// <summary>
    /// Get all discoverable content at this location
    /// </summary>
    private List<DiscoverableInfo> GetAllDiscoverableContent()
    {
        var discoverables = new List<DiscoverableInfo>();

        if (currentLocation == null) return discoverables;

        // Add hidden enemies
        if (currentLocation.AvailableEnemies != null)
        {
            foreach (var enemy in currentLocation.AvailableEnemies)
            {
                if (enemy != null && enemy.IsHidden && enemy.EnemyReference != null)
                {
                    bool isDiscovered = IsDiscovered(enemy.GetDiscoveryID());
                    discoverables.Add(new DiscoverableInfo
                    {
                        Id = enemy.GetDiscoveryID(),
                        Name = isDiscovered ? enemy.EnemyReference.GetDisplayName() : "???",
                        Type = DiscoverableType.Enemy,
                        Rarity = enemy.Rarity,
                        BonusXP = enemy.GetDiscoveryBonusXP(),
                        IsDiscovered = isDiscovered,
                        Icon = isDiscovered ? enemy.EnemyReference.Avatar : null
                    });
                }
            }
        }

        // Add hidden NPCs
        if (currentLocation.AvailableNPCs != null)
        {
            foreach (var npc in currentLocation.AvailableNPCs)
            {
                if (npc != null && npc.IsHidden && npc.NPCReference != null)
                {
                    bool isDiscovered = IsDiscovered(npc.GetDiscoveryID());
                    discoverables.Add(new DiscoverableInfo
                    {
                        Id = npc.GetDiscoveryID(),
                        Name = isDiscovered ? npc.NPCReference.GetDisplayName() : "???",
                        Type = DiscoverableType.NPC,
                        Rarity = npc.Rarity,
                        BonusXP = npc.GetDiscoveryBonusXP(),
                        IsDiscovered = isDiscovered,
                        Icon = isDiscovered ? npc.NPCReference.Avatar : null
                    });
                }
            }
        }

        // Add hidden activities
        if (currentLocation.AvailableActivities != null)
        {
            foreach (var activity in currentLocation.AvailableActivities)
            {
                if (activity != null && activity.IsHidden && activity.ActivityReference != null)
                {
                    bool isDiscovered = IsDiscovered(activity.GetDiscoveryID());
                    discoverables.Add(new DiscoverableInfo
                    {
                        Id = activity.GetDiscoveryID(),
                        Name = isDiscovered ? activity.GetDisplayName() : "???",
                        Type = DiscoverableType.Activity,
                        Rarity = activity.Rarity,
                        BonusXP = activity.GetDiscoveryBonusXP(),
                        IsDiscovered = isDiscovered,
                        Icon = isDiscovered ? activity.GetIcon() : null
                    });
                }
            }
        }

        // TODO: Add dungeons when LocationDungeon is implemented

        // Sort by rarity (rarer items first)
        discoverables.Sort((a, b) => b.Rarity.CompareTo(a.Rarity));

        return discoverables;
    }

    /// <summary>
    /// Create UI item for a discoverable
    /// </summary>
    private void CreateDiscoverableItem(DiscoverableInfo info)
    {
        if (discoverableItemPrefab == null || discoverableContainer == null) return;

        GameObject item = Instantiate(discoverableItemPrefab, discoverableContainer);
        instantiatedItems.Add(item);

        // Setup the item (assuming it has DiscoverableItemUI component)
        var itemUI = item.GetComponent<DiscoverableItemUI>();
        if (itemUI != null)
        {
            itemUI.Setup(info);
        }
        else
        {
            // Fallback: try to set text directly if it has a TextMeshProUGUI
            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                string rarityColor = GetRarityColor(info.Rarity);
                string status = info.IsDiscovered ? "[Decouvert]" : "[???]";
                text.text = $"<color={rarityColor}>{info.Name}</color> {status}";
            }
        }
    }

    /// <summary>
    /// Clear all instantiated UI items
    /// </summary>
    private void ClearInstantiatedItems()
    {
        foreach (var item in instantiatedItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        instantiatedItems.Clear();
    }

    /// <summary>
    /// Get total count of discoverable content
    /// </summary>
    private int GetTotalDiscoverableCount()
    {
        if (currentLocation == null) return 0;

        int count = 0;

        // Count hidden enemies
        if (currentLocation.AvailableEnemies != null)
        {
            foreach (var enemy in currentLocation.AvailableEnemies)
            {
                if (enemy != null && enemy.IsHidden)
                    count++;
            }
        }

        // Count hidden NPCs
        if (currentLocation.AvailableNPCs != null)
        {
            foreach (var npc in currentLocation.AvailableNPCs)
            {
                if (npc != null && npc.IsHidden)
                    count++;
            }
        }

        // Count hidden activities
        if (currentLocation.AvailableActivities != null)
        {
            foreach (var activity in currentLocation.AvailableActivities)
            {
                if (activity != null && activity.IsHidden)
                    count++;
            }
        }

        // TODO: Add dungeons when implemented

        return count;
    }

    /// <summary>
    /// Get count of already discovered content at this location
    /// </summary>
    private int GetDiscoveredCount()
    {
        if (currentLocation == null) return 0;

        // Use ExplorationManager if available, otherwise check PlayerData directly
        if (ExplorationManager.Instance != null)
        {
            return ExplorationManager.Instance.GetDiscoveredCountAtLocation(currentLocation);
        }

        // Fallback to direct PlayerData check
        if (DataManager.Instance?.PlayerData != null)
        {
            return DataManager.Instance.PlayerData.GetDiscoveryCountAtLocation(currentLocation.LocationID);
        }

        return 0;
    }

    /// <summary>
    /// Check if a specific item has been discovered
    /// </summary>
    private bool IsDiscovered(string discoveryId)
    {
        if (currentLocation == null || string.IsNullOrEmpty(discoveryId)) return false;

        // Use ExplorationManager if available, otherwise check PlayerData directly
        if (ExplorationManager.Instance != null)
        {
            return ExplorationManager.Instance.IsDiscoveredAtLocation(currentLocation.LocationID, discoveryId);
        }

        // Fallback to direct PlayerData check
        if (DataManager.Instance?.PlayerData != null)
        {
            return DataManager.Instance.PlayerData.HasDiscoveredAtLocation(currentLocation.LocationID, discoveryId);
        }

        return false;
    }

    /// <summary>
    /// Get HTML color string for rarity
    /// </summary>
    private string GetRarityColor(DiscoveryRarity rarity)
    {
        switch (rarity)
        {
            case DiscoveryRarity.Common:
                return "#FFFFFF"; // White
            case DiscoveryRarity.Uncommon:
                return "#1EFF00"; // Green
            case DiscoveryRarity.Rare:
                return "#0070DD"; // Blue
            case DiscoveryRarity.Epic:
                return "#A335EE"; // Purple
            case DiscoveryRarity.Legendary:
                return "#FF8000"; // Orange
            default:
                return "#FFFFFF";
        }
    }

    #endregion

    #region Private Methods - Actions

    /// <summary>
    /// Handle start exploration button click
    /// </summary>
    private void OnStartExplorationClicked()
    {
        if (currentLocation == null || currentActivity == null) return;

        Logger.LogInfo($"ExplorationPanelUI: Starting exploration at {currentLocation.DisplayName}", Logger.LogCategory.ActivityLog);

        // TODO: Start exploration activity via ActivityManager
        // For now, just log
        Logger.LogWarning("ExplorationPanelUI: Exploration activity start not yet implemented!", Logger.LogCategory.ActivityLog);

        // Close panel after starting (or keep open for real-time updates?)
        // ClosePanel();
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
    Enemy,
    NPC,
    Dungeon,
    Activity
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
