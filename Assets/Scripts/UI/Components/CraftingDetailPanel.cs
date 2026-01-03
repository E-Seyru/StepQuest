// Purpose: Detail panel showing selected crafting item info with rarity tabs
// Filepath: Assets/Scripts/UI/Components/CraftingDetailPanel.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel that displays detailed information about a selected crafting variant.
/// Shows item icon, description, and stats with rarity tabs for preview.
/// </summary>
public class CraftingDetailPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private Transform statsContainer;
    [SerializeField] private Sprite noItemSelectedSprite; // Placeholder sprite shown when no item selected

    [Header("Rarity Tabs")]
    [SerializeField] private Transform rarityTabsContainer;
    [SerializeField] private GameObject rarityTabPrefab;
    [SerializeField] private Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Stats Display")]
    [SerializeField] private GameObject statLinePrefab; // Used for stats AND abilities

    [Header("Rarity Background")]
    [SerializeField] private Image rarityBackground;

    [Header("Placeholder Texts")]
    [SerializeField] private string noDescriptionText = "Aucune description disponible.";
    [SerializeField] private string noStatsText = "Aucune statistique a afficher";

    // Current state
    private ActivityVariant currentVariant;
    private ItemDefinition currentOutputItem;
    private int selectedRarityTier = 1;
    private List<GameObject> instantiatedTabs = new List<GameObject>();
    private List<GameObject> instantiatedStatLines = new List<GameObject>();

    void Start()
    {
        // Subscribe to variant selection events
        CraftingPanel.OnVariantSelected += OnVariantSelected;

        // Start with no item selected
        ClearPanel();
    }

    void OnDestroy()
    {
        CraftingPanel.OnVariantSelected -= OnVariantSelected;
        ClearTabs();
        ClearStatLines();
    }

    #region Event Handlers

    /// <summary>
    /// Called when a crafting variant is selected in the CraftingPanel
    /// </summary>
    private void OnVariantSelected(ActivityVariant variant)
    {
        if (variant == null)
        {
            ClearPanel();
            return;
        }

        currentVariant = variant;
        currentOutputItem = variant.PrimaryResource;

        if (currentOutputItem == null)
        {
            ClearPanel();
            return;
        }

        // Show rarity background when item is selected
        if (rarityBackground != null)
        {
            rarityBackground.gameObject.SetActive(true);
        }

        SetupBasicInfo();
        SetupRarityTabs();
        UpdateStatsDisplay();
    }

    #endregion

    #region Setup Methods

    /// <summary>
    /// Setup basic item info (icon, name, description)
    /// </summary>
    private void SetupBasicInfo()
    {
        if (currentOutputItem == null) return;

        // Icon
        if (itemIcon != null && currentOutputItem.ItemIcon != null)
        {
            itemIcon.sprite = currentOutputItem.ItemIcon;
        }

        // Name
        if (itemNameText != null)
        {
            itemNameText.text = currentOutputItem.GetDisplayName();
        }

        // Description
        if (itemDescriptionText != null)
        {
            string description = currentOutputItem.Description;
            itemDescriptionText.text = string.IsNullOrEmpty(description) ? noDescriptionText : description;
        }
    }

    /// <summary>
    /// Setup rarity tabs based on available rarities for this item
    /// </summary>
    private void SetupRarityTabs()
    {
        ClearTabs();

        if (currentOutputItem == null || !currentOutputItem.HasRarityStats())
        {
            // Hide tabs container if no rarity stats
            if (rarityTabsContainer != null)
            {
                rarityTabsContainer.gameObject.SetActive(false);
            }
            selectedRarityTier = 0;
            UpdateRarityBackground();
            return;
        }

        if (rarityTabsContainer != null)
        {
            rarityTabsContainer.gameObject.SetActive(true);
        }

        List<int> availableRarities = currentOutputItem.GetAvailableRarityTiers();

        // Select the lowest rarity by default
        if (availableRarities.Count > 0)
        {
            selectedRarityTier = availableRarities[0];
        }

        // Create a tab for each available rarity
        foreach (int rarityTier in availableRarities)
        {
            CreateRarityTab(rarityTier);
        }

        UpdateTabVisuals();
        UpdateRarityBackground();
    }

    /// <summary>
    /// Create a single rarity tab
    /// </summary>
    private void CreateRarityTab(int rarityTier)
    {
        if (rarityTabPrefab == null || rarityTabsContainer == null) return;

        GameObject tabObj = Instantiate(rarityTabPrefab, rarityTabsContainer);
        instantiatedTabs.Add(tabObj);

        // Get rarity info
        ItemRarityStats rarityStats = currentOutputItem.GetStatsForRarity(rarityTier);
        string rarityName = rarityStats?.GetRarityDisplayName() ?? $"R{rarityTier}";
        Color rarityColor = rarityStats?.GetRarityColor() ?? Color.white;

        // Setup tab text
        TextMeshProUGUI tabText = tabObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tabText != null)
        {
            tabText.text = rarityName;
        }

        // Setup click handler
        Button tabButton = tabObj.GetComponent<Button>();
        if (tabButton != null)
        {
            int capturedRarity = rarityTier;
            tabButton.onClick.AddListener(() => OnRarityTabClicked(capturedRarity));
        }

        // Store rarity tier reference
        RarityTabData tabData = tabObj.GetComponent<RarityTabData>();
        if (tabData == null)
        {
            tabData = tabObj.AddComponent<RarityTabData>();
        }
        tabData.RarityTier = rarityTier;
        tabData.RarityColor = rarityColor;
    }

    /// <summary>
    /// Handle rarity tab click
    /// </summary>
    private void OnRarityTabClicked(int rarityTier)
    {
        selectedRarityTier = rarityTier;
        UpdateTabVisuals();
        UpdateRarityBackground();
        UpdateStatsDisplay();

        Logger.LogInfo($"CraftingDetailPanel: Switched to rarity tier {rarityTier}", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Update the panel background color based on selected rarity
    /// </summary>
    private void UpdateRarityBackground()
    {
        if (rarityBackground == null) return;

        if (currentOutputItem == null || selectedRarityTier == 0)
        {
            rarityBackground.color = Color.white;
            return;
        }

        ItemRarityStats rarityStats = currentOutputItem.GetStatsForRarity(selectedRarityTier);
        rarityBackground.color = rarityStats?.GetRarityColor() ?? Color.white;
    }

    /// <summary>
    /// Update tab visual states (active/inactive)
    /// </summary>
    private void UpdateTabVisuals()
    {
        foreach (var tabObj in instantiatedTabs)
        {
            if (tabObj == null) continue;

            RarityTabData tabData = tabObj.GetComponent<RarityTabData>();
            if (tabData == null) continue;

            bool isActive = tabData.RarityTier == selectedRarityTier;

            // Update background color
            Image tabImage = tabObj.GetComponent<Image>();
            if (tabImage != null)
            {
                tabImage.color = isActive ? tabData.RarityColor : inactiveTabColor;
            }

            // Update text color
            TextMeshProUGUI tabText = tabObj.GetComponentInChildren<TextMeshProUGUI>();
            if (tabText != null)
            {
                tabText.color = isActive ? Color.white : Color.gray;
            }
        }
    }

    #endregion

    #region Stats Display

    /// <summary>
    /// Update the stats display for the selected rarity
    /// </summary>
    private void UpdateStatsDisplay()
    {
        ClearStatLines();

        if (currentOutputItem == null)
        {
            CreateStatLine(noStatsText);
            return;
        }

        // Get stats for selected rarity
        ItemRarityStats rarityStats = currentOutputItem.GetStatsForRarity(selectedRarityTier);

        bool hasAnyContent = false;

        // Add stat lines
        if (rarityStats != null && rarityStats.Stats != null && rarityStats.Stats.Count > 0)
        {
            foreach (var stat in rarityStats.Stats)
            {
                CreateStatLine(stat.GetDisplayString());
                hasAnyContent = true;
            }
        }

        // Add unlocked ability as a stat line
        AbilityDefinition unlockedAbility = currentOutputItem.GetUnlockedAbilityForRarity(selectedRarityTier);
        if (unlockedAbility != null)
        {
            CreateStatLine($"Capacite: {unlockedAbility.AbilityName}");
            hasAnyContent = true;
        }

        // Show "no stats" if nothing to display
        if (!hasAnyContent)
        {
            CreateStatLine(noStatsText);
        }
    }

    /// <summary>
    /// Create a single stat line with text
    /// </summary>
    private void CreateStatLine(string text)
    {
        if (statLinePrefab == null || statsContainer == null) return;

        GameObject lineObj = Instantiate(statLinePrefab, statsContainer);
        instantiatedStatLines.Add(lineObj);

        TextMeshProUGUI lineText = lineObj.GetComponentInChildren<TextMeshProUGUI>();
        if (lineText != null)
        {
            lineText.text = text;
        }
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Clear the panel to "no item selected" state
    /// </summary>
    private void ClearPanel()
    {
        currentVariant = null;
        currentOutputItem = null;
        selectedRarityTier = 0;

        // Show placeholder sprite on icon
        if (itemIcon != null && noItemSelectedSprite != null)
        {
            itemIcon.sprite = noItemSelectedSprite;
        }

        // Reset texts to placeholders
        if (itemNameText != null)
        {
            itemNameText.text = "";
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = noDescriptionText;
        }

        // Hide tabs
        if (rarityTabsContainer != null)
        {
            rarityTabsContainer.gameObject.SetActive(false);
        }

        // Hide rarity background when no item selected
        if (rarityBackground != null)
        {
            rarityBackground.gameObject.SetActive(false);
        }

        ClearTabs();
        ClearStatLines();

        // Show "no stats" placeholder
        CreateStatLine(noStatsText);
    }

    /// <summary>
    /// Clear all instantiated tabs
    /// </summary>
    private void ClearTabs()
    {
        foreach (var tab in instantiatedTabs)
        {
            if (tab != null)
            {
                Destroy(tab);
            }
        }
        instantiatedTabs.Clear();
    }

    /// <summary>
    /// Clear all instantiated stat lines
    /// </summary>
    private void ClearStatLines()
    {
        foreach (var line in instantiatedStatLines)
        {
            if (line != null)
            {
                Destroy(line);
            }
        }
        instantiatedStatLines.Clear();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Manually set a variant to display
    /// </summary>
    public void SetVariant(ActivityVariant variant)
    {
        OnVariantSelected(variant);
    }

    /// <summary>
    /// Clear the panel
    /// </summary>
    public void Clear()
    {
        ClearPanel();
    }

    /// <summary>
    /// Get the currently displayed item
    /// </summary>
    public ItemDefinition GetCurrentItem()
    {
        return currentOutputItem;
    }

    /// <summary>
    /// Get the currently selected rarity tier
    /// </summary>
    public int GetSelectedRarityTier()
    {
        return selectedRarityTier;
    }

    #endregion
}

/// <summary>
/// Helper component to store rarity data on tab GameObjects
/// </summary>
public class RarityTabData : MonoBehaviour
{
    public int RarityTier;
    public Color RarityColor;
}
