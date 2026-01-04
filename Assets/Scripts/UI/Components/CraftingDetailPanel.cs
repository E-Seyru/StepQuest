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
    [SerializeField] private List<RectTransform> rarityTabs; // Assign tabs manually (index 0 = Common, 1 = Uncommon, etc.)
    [SerializeField] private float selectedTabOffsetY = 10f; // How much selected tab raises up
    [SerializeField] private float tabAnimationSpeed = 10f; // Speed of tab sliding animation

    [Header("Stats Display")]
    [SerializeField] private GameObject statLinePrefab; // Used for stats AND abilities

    [Header("Rarity Background")]
    [SerializeField] private Image rarityBackground;
    [SerializeField] private Image rarityBorderBackground;
    [SerializeField] private Color[] rarityColors = new Color[]
    {
        Color.gray,                         // Common
        Color.green,                        // Uncommon
        Color.blue,                         // Rare
        new Color(0.6f, 0.0f, 1.0f),        // Epic (purple)
        new Color(1.0f, 0.6f, 0.0f)         // Legendary (orange)
    };
    [SerializeField, Range(0f, 1f)] private float rarityColorDim = 0.7f; // Dim factor for rarity colors

    [Header("Placeholder Texts")]
    [SerializeField] private string noDescriptionText = "Aucune description disponible.";
    [SerializeField] private string noStatsText = "Aucune statistique a afficher";

    // Current state
    private ActivityVariant currentVariant;
    private ItemDefinition currentOutputItem;
    private int selectedRarityTier = 1;
    private List<GameObject> instantiatedStatLines = new List<GameObject>();

    // Tab animation state
    private List<float> tabBasePositionsY = new List<float>();
    private List<float> tabTargetPositionsY = new List<float>();
    private bool tabsInitialized = false;

    void Start()
    {
        // Subscribe to variant selection events
        CraftingPanel.OnVariantSelected += OnVariantSelected;

        // Initialize tabs
        InitializeTabs();

        // Start with no item selected
        ClearPanel();
    }

    void Update()
    {
        // Animate tabs towards their target positions
        AnimateTabs();
    }

    private void InitializeTabs()
    {
        if (rarityTabs == null || rarityTabs.Count == 0) return;

        tabBasePositionsY.Clear();
        tabTargetPositionsY.Clear();

        for (int i = 0; i < rarityTabs.Count; i++)
        {
            if (rarityTabs[i] == null) continue;

            // Store the base Y position (unselected state - lower)
            float baseY = rarityTabs[i].anchoredPosition.y;
            tabBasePositionsY.Add(baseY);
            tabTargetPositionsY.Add(baseY);

            // Add click listener
            int tierIndex = i;
            Button tabButton = rarityTabs[i].GetComponent<Button>();
            if (tabButton != null)
            {
                tabButton.onClick.AddListener(() => OnRarityTabClicked(tierIndex + 1));
            }
        }

        tabsInitialized = true;
    }

    private void AnimateTabs()
    {
        if (!tabsInitialized || rarityTabs == null) return;

        for (int i = 0; i < rarityTabs.Count; i++)
        {
            if (rarityTabs[i] == null || i >= tabTargetPositionsY.Count) continue;

            Vector2 currentPos = rarityTabs[i].anchoredPosition;
            float targetY = tabTargetPositionsY[i];

            if (!Mathf.Approximately(currentPos.y, targetY))
            {
                float newY = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * tabAnimationSpeed);
                rarityTabs[i].anchoredPosition = new Vector2(currentPos.x, newY);
            }
        }
    }

    void OnDestroy()
    {
        CraftingPanel.OnVariantSelected -= OnVariantSelected;
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

        SetupBasicInfo();
        SetupRarityTabs(); // This will handle rarity background visibility
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
        if (rarityTabs == null || rarityTabs.Count == 0) return;

        if (currentOutputItem == null || !currentOutputItem.HasRarityStats())
        {
            // Hide all tabs if no rarity stats
            SetAllTabsVisible(false);
            selectedRarityTier = 0;
            UpdateRarityBackground();
            return;
        }

        List<int> availableRarities = currentOutputItem.GetAvailableRarityTiers();

        // Show/hide tabs based on available rarities
        for (int i = 0; i < rarityTabs.Count; i++)
        {
            if (rarityTabs[i] == null) continue;

            int tierForThisTab = i + 1; // Tab index 0 = tier 1 (Common)
            bool isAvailable = availableRarities.Contains(tierForThisTab);
            rarityTabs[i].gameObject.SetActive(isAvailable);
        }

        // Select the lowest available rarity by default
        if (availableRarities.Count > 0)
        {
            selectedRarityTier = availableRarities[0];
        }

        UpdateTabPositions();
        UpdateRarityBackground();
    }

    /// <summary>
    /// Handle rarity tab click
    /// </summary>
    private void OnRarityTabClicked(int rarityTier)
    {
        selectedRarityTier = rarityTier;
        UpdateTabPositions();
        UpdateRarityBackground();
        UpdateStatsDisplay();

        Logger.LogInfo($"CraftingDetailPanel: Switched to rarity tier {rarityTier}", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Update tab Y positions based on selection (selected tab stays up, others slide down)
    /// </summary>
    private void UpdateTabPositions()
    {
        if (!tabsInitialized || rarityTabs == null) return;

        for (int i = 0; i < rarityTabs.Count && i < tabTargetPositionsY.Count && i < tabBasePositionsY.Count; i++)
        {
            if (rarityTabs[i] == null) continue;

            int tierForThisTab = i + 1;
            bool isSelected = tierForThisTab == selectedRarityTier;

            // Selected tab goes to base position + offset (higher), others go to base position (lower)
            tabTargetPositionsY[i] = isSelected ? tabBasePositionsY[i] + selectedTabOffsetY : tabBasePositionsY[i];
        }
    }

    /// <summary>
    /// Set visibility of all tabs
    /// </summary>
    private void SetAllTabsVisible(bool visible)
    {
        if (rarityTabs == null) return;

        foreach (var tab in rarityTabs)
        {
            if (tab != null)
            {
                tab.gameObject.SetActive(visible);
            }
        }
    }

    /// <summary>
    /// Update the panel background color based on selected rarity
    /// </summary>
    private void UpdateRarityBackground()
    {
        if (currentOutputItem == null || selectedRarityTier == 0)
        {
            // Hide backgrounds when no rarity stats
            if (rarityBackground != null)
                rarityBackground.gameObject.SetActive(false);
            if (rarityBorderBackground != null)
                rarityBorderBackground.gameObject.SetActive(false);
            return;
        }

        // Show backgrounds and set color
        if (rarityBackground != null)
            rarityBackground.gameObject.SetActive(true);
        if (rarityBorderBackground != null)
            rarityBorderBackground.gameObject.SetActive(true);

        // Use Inspector-configured colors (tier 1 = index 0, etc.)
        int colorIndex = selectedRarityTier - 1;
        Color rarityColor = Color.white;
        if (rarityColors != null && colorIndex >= 0 && colorIndex < rarityColors.Length)
        {
            rarityColor = rarityColors[colorIndex];
        }

        if (rarityBackground != null)
            rarityBackground.color = rarityColor;

        // Apply transparency to border background
        if (rarityBorderBackground != null)
        {
            Color transparentColor = new Color(
                rarityColor.r,
                rarityColor.g,
                rarityColor.b,
                rarityColorDim
            );
            rarityBorderBackground.color = transparentColor;
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

        // Hide all tabs
        SetAllTabsVisible(false);

        // Hide rarity backgrounds when no item selected
        if (rarityBackground != null)
        {
            rarityBackground.gameObject.SetActive(false);
        }
        if (rarityBorderBackground != null)
        {
            rarityBorderBackground.gameObject.SetActive(false);
        }

        ClearStatLines();

        // Show "no stats" placeholder
        CreateStatLine(noStatsText);
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
