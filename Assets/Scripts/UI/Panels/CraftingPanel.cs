// Purpose: Panel for time-based crafting activities with optional category tabs
// Filepath: Assets/Scripts/UI/Panels/CraftingPanel.cs
using ActivityEvents;
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
    [SerializeField] private GameObject craftingPanelContainer; // The container with all UI content (hidden during crafting)
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private ScrollRect scrollView;
    [SerializeField] private Transform cardsContainer;
    [SerializeField] private Button closeButton;

    [Header("Tabs")]
    [SerializeField] private Transform tabsContainer;
    [SerializeField] private GameObject tabButtonPrefab; // Must have CategoryTabButton component
    [SerializeField] private Color activeTabColor = Color.white;
    [SerializeField] private Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Sprite allTabIcon; // Icon for the "All" tab
    [SerializeField] private Sprite defaultCategoryIcon; // Fallback icon when category has no icon

    [Header("Card Prefab")]
    [SerializeField] private GameObject craftingCardPrefab;

    [Header("Start Button")]
    [SerializeField] private Button startCraftingButton;

    [Header("Fade Animation")]
    [SerializeField] private float fadeOutDuration = 0.15f;

    // Current state
    private LocationActivity currentActivity;
    private ActivityVariant selectedVariant = null;
    private List<GameObject> instantiatedCards = new List<GameObject>();
    private List<CategoryTabButton> instantiatedTabs = new List<CategoryTabButton>();
    private Dictionary<CategoryDefinition, List<ActivityVariant>> variantsByCategory = new Dictionary<CategoryDefinition, List<ActivityVariant>>();
    private CategoryDefinition currentCategory = null; // null means show all

    // Animation state
    private CanvasGroup panelCanvasGroup;
    private int panelFadeTween = -1;

    // Events
    public static event Action<ActivityVariant> OnVariantSelected;

    // Singleton
    public static CraftingPanel Instance { get; private set; }

    /// <summary>
    /// Returns true when the panel is open
    /// </summary>
    public bool IsOpen => gameObject.activeInHierarchy;

    /// <summary>
    /// Check if a screen position is over the tabs area (used by PanelManager to block swipes)
    /// </summary>
    public bool IsPositionOverTabs(Vector2 screenPosition)
    {
        if (!IsOpen || tabsContainer == null || !tabsContainer.gameObject.activeInHierarchy)
            return false;

        RectTransform tabsRect = tabsContainer.GetComponent<RectTransform>();
        if (tabsRect == null)
            return false;

        // Check if screen position is within the tabs RectTransform
        return RectTransformUtility.RectangleContainsScreenPoint(tabsRect, screenPosition, null);
    }

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

        // Setup start crafting button
        if (startCraftingButton != null)
        {
            startCraftingButton.onClick.AddListener(OnStartCraftingClicked);
        }

        // Get or add CanvasGroup for fade animations
        panelCanvasGroup = GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Subscribe to activity events
        EventBus.Subscribe<ActivityStartedEvent>(OnActivityStarted);
        EventBus.Subscribe<ActivityStoppedEvent>(OnActivityStopped);

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
        selectedVariant = null; // Reset selection

        UpdateTitle();
        OrganizeVariantsByCategory();
        CreateCategoryTabs();
        PopulateVariantCards();
        UpdateStartButtonState();

        // Ensure container is visible when opening
        if (craftingPanelContainer != null)
        {
            craftingPanelContainer.SetActive(true);
        }

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

        // Slide the activities section back in
        if (LocationDetailsPanel.Instance != null)
        {
            LocationDetailsPanel.Instance.SlideInActivitiesSection();
        }

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
    /// Organize variants by their Category field, filtering by activity's available categories
    /// </summary>
    private void OrganizeVariantsByCategory()
    {
        variantsByCategory.Clear();

        if (currentActivity == null || currentActivity.ActivityVariants == null) return;

        // Get the activity's defined categories (if any)
        var activityCategories = currentActivity.ActivityReference?.AvailableCategories;
        bool hasDefinedCategories = activityCategories != null && activityCategories.Count > 0;

        foreach (var variant in currentActivity.ActivityVariants)
        {
            // Only include time-based variants
            if (variant == null || !variant.IsValidVariant() || !variant.IsTimeBased)
                continue;

            // Get category from the item being crafted (PrimaryResource)
            CategoryDefinition category = variant.PrimaryResource?.Category;

            // If activity has defined categories, only include variants with matching categories
            if (hasDefinedCategories)
            {
                if (category == null || !activityCategories.Contains(category))
                    continue;
            }

            if (!variantsByCategory.ContainsKey(category))
            {
                variantsByCategory[category] = new List<ActivityVariant>();
            }

            variantsByCategory[category].Add(variant);
        }

        Logger.LogInfo($"CraftingPanel: Organized {variantsByCategory.Values.Sum(l => l.Count)} variants into {variantsByCategory.Count} categories", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Create category tabs based on activity's defined categories
    /// </summary>
    private void CreateCategoryTabs()
    {
        ClearTabs();

        // Get the activity's defined categories
        var activityCategories = currentActivity?.ActivityReference?.AvailableCategories;
        bool hasDefinedCategories = activityCategories != null && activityCategories.Count > 0;

        // Only show tabs if the activity has multiple defined categories
        if (!hasDefinedCategories || activityCategories.Count <= 1)
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

        // Create "All" tab first (null = all)
        CreateTab(null);

        // Create tab for each category defined by the activity, sorted by SortOrder
        var sortedCategories = GetSortedCategories();
        foreach (var category in sortedCategories)
        {
            CreateTab(category);
        }

        UpdateTabVisuals();
    }

    /// <summary>
    /// Get categories from the activity, sorted by their SortOrder
    /// </summary>
    private List<CategoryDefinition> GetSortedCategories()
    {
        var activityCategories = currentActivity?.ActivityReference?.AvailableCategories;
        if (activityCategories == null || activityCategories.Count == 0)
            return new List<CategoryDefinition>();

        return activityCategories
            .Where(cat => cat != null)
            .OrderBy(cat => cat.SortOrder)
            .ThenBy(cat => cat.GetDisplayName()) // Alphabetical as tiebreaker
            .ToList();
    }

    /// <summary>
    /// Create a tab - pass CategoryDefinition (null for "All" tab)
    /// </summary>
    private void CreateTab(CategoryDefinition category)
    {
        GameObject tabObj = Instantiate(tabButtonPrefab, tabsContainer);
        CategoryTabButton tabButton = tabObj.GetComponent<CategoryTabButton>();

        if (tabButton != null)
        {
            // Get icon and display name directly from CategoryDefinition
            Sprite icon;
            string label;
            string categoryId;

            if (category == null)
            {
                // "All" tab
                icon = allTabIcon;
                label = "Tout";
                categoryId = null;
            }
            else
            {
                // Category tab - get data directly from the definition
                icon = category.Icon ?? defaultCategoryIcon;
                label = category.GetDisplayName();
                categoryId = category.CategoryID;
            }

            tabButton.Setup(categoryId, icon, label);
            instantiatedTabs.Add(tabButton);
        }
        else
        {
            Logger.LogWarning("CraftingPanel: tabButtonPrefab missing CategoryTabButton component!", Logger.LogCategory.ActivityLog);
            Destroy(tabObj);
            return;
        }

        // Setup click handler
        Button button = tabObj.GetComponent<Button>();
        if (button != null)
        {
            CategoryDefinition capturedCategory = category;
            button.onClick.AddListener(() => OnTabClicked(capturedCategory));
        }
    }

    /// <summary>
    /// Handle tab click
    /// </summary>
    private void OnTabClicked(CategoryDefinition category)
    {
        currentCategory = category;
        UpdateTabVisuals();

        // Deselect any previously selected variant when switching tabs
        DeselectAllCards();

        PopulateVariantCards();

        Logger.LogInfo($"CraftingPanel: Switched to category '{(category != null ? category.GetDisplayName() : "All")}'", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Deselect all cards and notify listeners that no variant is selected
    /// </summary>
    private void DeselectAllCards()
    {
        foreach (var cardObj in instantiatedCards)
        {
            if (cardObj == null) continue;

            CraftingActivityCard card = cardObj.GetComponent<CraftingActivityCard>();
            if (card != null)
            {
                card.SetSelected(false);
                card.SetDimmed(false);
            }
        }

        // Clear selected variant
        selectedVariant = null;
        UpdateStartButtonState();

        // Notify listeners that no variant is selected
        OnVariantSelected?.Invoke(null);
    }

    /// <summary>
    /// Update tab visual states (active/inactive colors)
    /// </summary>
    private void UpdateTabVisuals()
    {
        string currentCategoryId = currentCategory?.CategoryID;

        foreach (var tab in instantiatedTabs)
        {
            if (tab == null) continue;

            bool isActive = tab.GetCategoryId() == currentCategoryId;
            tab.SetSelected(isActive, activeTabColor, inactiveTabColor);
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
    /// Get variants filtered by current category, sorted by level requirement (ascending)
    /// </summary>
    private List<ActivityVariant> GetFilteredVariants()
    {
        if (currentCategory == null)
        {
            // Show all variants, sorted by level requirement
            return variantsByCategory.Values
                .SelectMany(v => v)
                .OrderBy(v => v.UnlockRequirement)
                .ToList();
        }
        else
        {
            // Show only variants from selected category, sorted by level requirement
            if (variantsByCategory.TryGetValue(currentCategory, out var variants))
            {
                return variants.OrderBy(v => v.UnlockRequirement).ToList();
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

        // Update selected variant
        selectedVariant = variant;

        // Deselect previous card and select the new one
        SelectCardForVariant(variant);

        // Update start button state
        UpdateStartButtonState();

        // Notify listeners
        OnVariantSelected?.Invoke(variant);
    }

    /// <summary>
    /// Select a card and deselect all others
    /// </summary>
    private void SelectCardForVariant(ActivityVariant variant)
    {
        foreach (var cardObj in instantiatedCards)
        {
            if (cardObj == null) continue;

            CraftingActivityCard card = cardObj.GetComponent<CraftingActivityCard>();
            if (card != null)
            {
                bool isSelected = card.GetActivityVariant() == variant;
                card.SetSelected(isSelected);
                card.SetDimmed(!isSelected); // Dim non-selected cards
            }
        }
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
                Destroy(tab.gameObject);
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

    #region Activity Event Handlers

    /// <summary>
    /// Called when an activity starts - hide the container but keep background
    /// </summary>
    private void OnActivityStarted(ActivityStartedEvent eventData)
    {
        if (!gameObject.activeInHierarchy) return;

        // Only react to time-based activities (crafting)
        if (eventData.Variant == null || !eventData.Variant.IsTimeBased) return;

        HideContainer();
    }

    /// <summary>
    /// Called when an activity stops (completed or cancelled) - show the container again
    /// </summary>
    private void OnActivityStopped(ActivityStoppedEvent eventData)
    {
        if (!gameObject.activeInHierarchy) return;

        // Only react to time-based activities (crafting)
        if (eventData.Variant == null || !eventData.Variant.IsTimeBased) return;

        ShowContainer();
    }

    /// <summary>
    /// Hide the crafting container (keep background visible)
    /// </summary>
    private void HideContainer()
    {
        if (craftingPanelContainer != null)
        {
            craftingPanelContainer.SetActive(false);
        }

        Logger.LogInfo("CraftingPanel: Hiding container, keeping background", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Show the crafting container again
    /// </summary>
    private void ShowContainer()
    {
        if (craftingPanelContainer != null)
        {
            craftingPanelContainer.SetActive(true);
        }

        // Refresh card states since inventory may have changed
        RefreshAllCardStates();

        // Deselect any previously selected card
        DeselectAllCards();

        Logger.LogInfo("CraftingPanel: Showing container after activity ended", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Cancel panel fade tween
    /// </summary>
    private void CancelPanelFadeTween()
    {
        if (panelFadeTween >= 0)
        {
            LeanTween.cancel(panelFadeTween);
            panelFadeTween = -1;
        }
    }

    #endregion

    #region Start Button

    /// <summary>
    /// Update the start button state based on selection
    /// </summary>
    private void UpdateStartButtonState()
    {
        if (startCraftingButton == null) return;

        // Button is enabled only when a variant is selected
        startCraftingButton.interactable = selectedVariant != null;
    }

    /// <summary>
    /// Handle start crafting button click
    /// </summary>
    private void OnStartCraftingClicked()
    {
        if (selectedVariant == null)
        {
            Logger.LogWarning("CraftingPanel: No variant selected when trying to start crafting!", Logger.LogCategory.ActivityLog);
            return;
        }

        // Validation 1: Check level requirement
        if (selectedVariant.UnlockRequirement > 0)
        {
            string mainSkillId = selectedVariant.GetMainSkillId();
            int playerLevel = 1;

            if (XpManager.Instance != null)
            {
                var skillData = XpManager.Instance.GetPlayerSkill(mainSkillId);
                playerLevel = skillData?.Level ?? 1;
            }

            if (playerLevel < selectedVariant.UnlockRequirement)
            {
                // Show error: level too low
                string errorMessage = $"Niveau {selectedVariant.UnlockRequirement} requis en {mainSkillId}! (Actuel: {playerLevel})";
                ShowCraftingError(errorMessage);
                Logger.LogInfo($"CraftingPanel: Cannot craft - level too low ({playerLevel} < {selectedVariant.UnlockRequirement})", Logger.LogCategory.ActivityLog);
                return;
            }
        }

        // Validation 2: Check materials
        if (InventoryManager.Instance != null)
        {
            if (!selectedVariant.CanCraft(InventoryManager.Instance))
            {
                // Show error: missing materials
                string errorMessage = $"Materiaux insuffisants!\nRequis: {selectedVariant.GetRequiredMaterialsText()}";
                ShowCraftingError(errorMessage);
                Logger.LogInfo($"CraftingPanel: Cannot craft - missing materials for {selectedVariant.GetDisplayName()}", Logger.LogCategory.ActivityLog);
                return;
            }
        }

        // Validation passed - start crafting
        if (ActivityManager.Instance != null && currentActivity != null)
        {
            string activityId = currentActivity.ActivityReference?.ActivityID;
            string locationId = MapManager.Instance?.CurrentLocation?.LocationID;

            bool started = ActivityManager.Instance.StartTimedActivity(activityId, selectedVariant.name, locationId);

            if (started)
            {
                Logger.LogInfo($"CraftingPanel: Started crafting {selectedVariant.GetDisplayName()}", Logger.LogCategory.ActivityLog);
                // Panel will close automatically via OnActivityStarted event
            }
            else
            {
                ShowCraftingError("Impossible de demarrer la fabrication!");
                Logger.LogError($"CraftingPanel: Failed to start timed activity for {selectedVariant.GetDisplayName()}", Logger.LogCategory.ActivityLog);
            }
        }
        else
        {
            ShowCraftingError("Erreur systeme!");
            Logger.LogError("CraftingPanel: ActivityManager or currentActivity is null!", Logger.LogCategory.ActivityLog);
        }
    }

    /// <summary>
    /// Show a crafting error using the ErrorPanel, positioned above the start button
    /// </summary>
    private void ShowCraftingError(string message)
    {
        if (ErrorPanel.Instance != null)
        {
            // Position error above the start button
            if (startCraftingButton != null)
            {
                RectTransform buttonRect = startCraftingButton.GetComponent<RectTransform>();
                ErrorPanel.Instance.ShowErrorAboveUI(message, buttonRect);
            }
            else
            {
                ErrorPanel.Instance.ShowError(message);
            }
        }
        else
        {
            Logger.LogWarning($"CraftingPanel: ErrorPanel not available. Error was: {message}", Logger.LogCategory.ActivityLog);
        }
    }

    #endregion

    void OnDestroy()
    {
        CancelPanelFadeTween();
        ClearVariantCards();
        ClearTabs();

        // Unsubscribe from events
        EventBus.Unsubscribe<ActivityStartedEvent>(OnActivityStarted);
        EventBus.Unsubscribe<ActivityStoppedEvent>(OnActivityStopped);

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
        }

        if (startCraftingButton != null)
        {
            startCraftingButton.onClick.RemoveListener(OnStartCraftingClicked);
        }
    }
}
