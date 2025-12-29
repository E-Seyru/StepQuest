// Purpose: Panel for time-based crafting activities with optional category tabs
// Filepath: Assets/Scripts/UI/Panels/CraftingPanel.cs
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel that displays crafting activity variants with automatic category tabs.
/// Used for time-based activities like forging, cooking, etc.
/// </summary>
public class CraftingPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private ScrollRect scrollView;
    [SerializeField] private Transform cardsContainer;
    [SerializeField] private Button closeButton;

    [Header("Tabs")]
    [SerializeField] private Transform tabsContainer;
    [SerializeField] private GameObject tabButtonPrefab;
    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Card Prefab")]
    [SerializeField] private GameObject craftingCardPrefab;

    // Current state
    private LocationActivity currentActivity;
    private List<GameObject> instantiatedCards = new List<GameObject>();
    private List<GameObject> instantiatedTabs = new List<GameObject>();
    private Dictionary<string, List<ActivityVariant>> variantsByCategory = new Dictionary<string, List<ActivityVariant>>();
    private string currentCategory = null; // null means show all

    // Events
    public static event Action<ActivityVariant> OnVariantSelected;

    // Singleton
    public static CraftingPanel Instance { get; private set; }

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
        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        // Setup grid layout for cards container
        SetupGridLayout();

        // Start hidden
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        // Subscribe to inventory changes
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnContainerChanged += OnInventoryChanged;
        }
    }

    void OnDisable()
    {
        // Unsubscribe from inventory changes
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnContainerChanged -= OnInventoryChanged;
        }
    }

    /// <summary>
    /// Setup the grid layout for the cards container
    /// </summary>
    private void SetupGridLayout()
    {
        if (cardsContainer == null) return;

        // Add GridLayoutGroup if not present
        GridLayoutGroup gridLayout = cardsContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = cardsContainer.gameObject.AddComponent<GridLayoutGroup>();
        }

        // Configure grid layout
        gridLayout.childAlignment = TextAnchor.UpperCenter;
        gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
        gridLayout.spacing = new Vector2(30f, 30f);
        gridLayout.padding = new RectOffset(30, 30, 30, 30);
        gridLayout.cellSize = new Vector2(200f, 280f);

        Logger.LogInfo("CraftingPanel: Grid layout configured", Logger.LogCategory.ActivityLog);
    }

    #region Public Methods

    /// <summary>
    /// Open panel with specific activity
    /// </summary>
    public void OpenWithActivity(LocationActivity activity)
    {
        if (activity == null)
        {
            Logger.LogWarning("CraftingPanel: Cannot open with null activity!", Logger.LogCategory.ActivityLog);
            return;
        }

        currentActivity = activity;
        currentCategory = null; // Reset to show all

        UpdateTitle();
        OrganizeVariantsByCategory();
        CreateCategoryTabs();
        PopulateVariantCards();

        gameObject.SetActive(true);

        Logger.LogInfo($"CraftingPanel: Opened for {activity.GetDisplayName()}", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Close the panel
    /// </summary>
    public void ClosePanel()
    {
        gameObject.SetActive(false);
        ClearVariantCards();
        ClearTabs();
        variantsByCategory.Clear();
        Logger.LogInfo("CraftingPanel: Panel closed", Logger.LogCategory.ActivityLog);
    }

    #endregion

    #region Private Methods - Setup

    /// <summary>
    /// Update the title text
    /// </summary>
    private void UpdateTitle()
    {
        if (titleText != null && currentActivity != null)
        {
            titleText.text = currentActivity.GetDisplayName();
        }
    }

    /// <summary>
    /// Organize variants by their Category field
    /// </summary>
    private void OrganizeVariantsByCategory()
    {
        variantsByCategory.Clear();

        if (currentActivity == null || currentActivity.ActivityVariants == null) return;

        foreach (var variant in currentActivity.ActivityVariants)
        {
            // Only include time-based variants
            if (variant == null || !variant.IsValidVariant() || !variant.IsTimeBased)
                continue;

            string category = string.IsNullOrEmpty(variant.Category) ? "General" : variant.Category;

            if (!variantsByCategory.ContainsKey(category))
            {
                variantsByCategory[category] = new List<ActivityVariant>();
            }

            variantsByCategory[category].Add(variant);
        }

        Logger.LogInfo($"CraftingPanel: Organized {variantsByCategory.Values.Sum(l => l.Count)} variants into {variantsByCategory.Count} categories", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Create category tabs if there are multiple categories
    /// </summary>
    private void CreateCategoryTabs()
    {
        ClearTabs();

        // Only show tabs if there are multiple categories
        if (variantsByCategory.Count <= 1)
        {
            if (tabsContainer != null)
            {
                tabsContainer.gameObject.SetActive(false);
            }
            return;
        }

        if (tabsContainer == null || tabButtonPrefab == null)
        {
            Logger.LogWarning("CraftingPanel: tabsContainer or tabButtonPrefab not assigned!", Logger.LogCategory.ActivityLog);
            return;
        }

        tabsContainer.gameObject.SetActive(true);

        // Create "All" tab first
        CreateTabButton("Tout", null);

        // Create tab for each category
        foreach (var category in variantsByCategory.Keys.OrderBy(k => k))
        {
            CreateTabButton(category, category);
        }

        UpdateTabVisuals();
    }

    /// <summary>
    /// Create a single tab button
    /// </summary>
    private void CreateTabButton(string label, string categoryValue)
    {
        GameObject tabObj = Instantiate(tabButtonPrefab, tabsContainer);
        instantiatedTabs.Add(tabObj);

        // Setup button text
        TextMeshProUGUI tabText = tabObj.GetComponentInChildren<TextMeshProUGUI>();
        if (tabText != null)
        {
            tabText.text = label;
        }

        // Setup button click
        Button tabButton = tabObj.GetComponent<Button>();
        if (tabButton != null)
        {
            string capturedCategory = categoryValue; // Capture for closure
            tabButton.onClick.AddListener(() => OnTabClicked(capturedCategory));
        }
    }

    /// <summary>
    /// Handle tab click
    /// </summary>
    private void OnTabClicked(string category)
    {
        currentCategory = category;
        UpdateTabVisuals();
        PopulateVariantCards();

        Logger.LogInfo($"CraftingPanel: Switched to category '{category ?? "All"}'", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Update tab visual states (active/inactive colors)
    /// </summary>
    private void UpdateTabVisuals()
    {
        for (int i = 0; i < instantiatedTabs.Count; i++)
        {
            var tab = instantiatedTabs[i];
            if (tab == null) continue;

            Image tabImage = tab.GetComponent<Image>();
            if (tabImage == null) continue;

            // First tab is "All" (null category), rest are category-specific
            bool isActive;
            if (i == 0)
            {
                isActive = currentCategory == null;
            }
            else
            {
                string tabCategory = variantsByCategory.Keys.OrderBy(k => k).ElementAtOrDefault(i - 1);
                isActive = currentCategory == tabCategory;
            }

            tabImage.color = isActive ? activeTabColor : inactiveTabColor;
        }
    }

    #endregion

    #region Private Methods - Cards

    /// <summary>
    /// Populate variants as cards in a grid
    /// </summary>
    private void PopulateVariantCards()
    {
        ClearVariantCards();

        if (currentActivity == null) return;

        // Get variants to display based on current category filter
        List<ActivityVariant> variantsToShow = GetFilteredVariants();

        Logger.LogInfo($"CraftingPanel: Showing {variantsToShow.Count} crafting variants", Logger.LogCategory.ActivityLog);

        // Create card for each variant
        foreach (var variant in variantsToShow)
        {
            CreateVariantCard(variant);
        }

        // Force layout rebuild
        if (cardsContainer != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardsContainer.GetComponent<RectTransform>());
        }
    }

    /// <summary>
    /// Get variants filtered by current category
    /// </summary>
    private List<ActivityVariant> GetFilteredVariants()
    {
        if (currentCategory == null)
        {
            // Show all variants
            return variantsByCategory.Values.SelectMany(v => v).ToList();
        }
        else
        {
            // Show only variants from selected category
            if (variantsByCategory.TryGetValue(currentCategory, out var variants))
            {
                return variants;
            }
            return new List<ActivityVariant>();
        }
    }

    /// <summary>
    /// Create a card for a variant
    /// </summary>
    private void CreateVariantCard(ActivityVariant variant)
    {
        if (variant == null || cardsContainer == null) return;

        if (craftingCardPrefab == null)
        {
            Logger.LogError("CraftingPanel: No craftingCardPrefab assigned!", Logger.LogCategory.ActivityLog);
            return;
        }

        // Instantiate the card
        GameObject cardObj = Instantiate(craftingCardPrefab, cardsContainer);
        instantiatedCards.Add(cardObj);

        // Setup the card
        CraftingActivityCard craftingCard = cardObj.GetComponent<CraftingActivityCard>();
        if (craftingCard == null)
        {
            Logger.LogError("CraftingPanel: CraftingCardPrefab doesn't have CraftingActivityCard component!", Logger.LogCategory.ActivityLog);
            return;
        }

        craftingCard.Setup(variant);
        craftingCard.OnCardClicked += OnVariantCardClicked;

        Logger.LogInfo($"CraftingPanel: Created card for {variant.VariantName}", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Handle variant card click
    /// </summary>
    private void OnVariantCardClicked(ActivityVariant variant)
    {
        Logger.LogInfo($"CraftingPanel: Variant selected: {variant.VariantName}", Logger.LogCategory.ActivityLog);

        // Start the time-based activity via ActivityManager
        if (ActivityManager.Instance != null && currentActivity != null)
        {
            string activityId = currentActivity.ActivityId;
            string variantId = ActivityRegistry.GenerateVariantId(variant.VariantName);

            bool success = ActivityManager.Instance.StartTimedActivity(activityId, variantId);

            if (success)
            {
                Logger.LogInfo($"Successfully started crafting activity: {variant.GetDisplayName()}", Logger.LogCategory.ActivityLog);
            }
            else
            {
                Logger.LogWarning($"Failed to start crafting activity: {variant.GetDisplayName()}", Logger.LogCategory.ActivityLog);
            }
        }

        // Notify listeners
        OnVariantSelected?.Invoke(variant);

        // Close panel
        ClosePanel();
    }

    /// <summary>
    /// Clear all variant cards
    /// </summary>
    private void ClearVariantCards()
    {
        foreach (var card in instantiatedCards)
        {
            if (card != null)
            {
                // Unsubscribe from events before destroying
                CraftingActivityCard craftingCard = card.GetComponent<CraftingActivityCard>();
                if (craftingCard != null)
                {
                    craftingCard.OnCardClicked -= OnVariantCardClicked;
                }

                Destroy(card);
            }
        }
        instantiatedCards.Clear();
    }

    /// <summary>
    /// Clear all tabs
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

    #endregion

    #region Private Methods - Inventory

    /// <summary>
    /// Handle inventory changes to refresh card states
    /// </summary>
    private void OnInventoryChanged(string containerId)
    {
        // Only care about player inventory changes
        if (containerId != GameConstants.ContainerIdPlayer) return;

        // Refresh all card ingredient states
        RefreshAllCardStates();
    }

    /// <summary>
    /// Refresh the visual state of all cards (ingredient availability)
    /// </summary>
    private void RefreshAllCardStates()
    {
        foreach (var cardObj in instantiatedCards)
        {
            if (cardObj == null) continue;

            CraftingActivityCard craftingCard = cardObj.GetComponent<CraftingActivityCard>();
            if (craftingCard != null)
            {
                craftingCard.RefreshIngredientsState();
            }
        }
    }

    #endregion

    void OnDestroy()
    {
        ClearVariantCards();
        ClearTabs();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
        }
    }
}
