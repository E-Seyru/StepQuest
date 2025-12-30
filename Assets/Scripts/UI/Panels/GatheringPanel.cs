// Purpose: Panel for step-based harvesting activities (mining, woodcutting, fishing)
// Filepath: Assets/Scripts/UI/Panels/GatheringPanel.cs
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel that displays harvesting activity variants in a grid layout.
/// Used for step-based activities like mining, woodcutting, and fishing.
/// </summary>
public class GatheringPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private ScrollRect scrollView;
    [SerializeField] private Transform cardsContainer;
    [SerializeField] private Button closeButton;

    [Header("Card Prefab")]
    [SerializeField] private GameObject harvestingCardPrefab;

    // Current state
    private LocationActivity currentActivity;
    private List<GameObject> instantiatedCards = new List<GameObject>();

    // Events
    public static event Action<ActivityVariant> OnVariantSelected;

    // Singleton
    public static GatheringPanel Instance { get; private set; }

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

        Logger.LogInfo("GatheringPanel: Grid layout configured", Logger.LogCategory.ActivityLog);
    }

    #region Public Methods

    /// <summary>
    /// Open panel with specific activity
    /// </summary>
    public void OpenWithActivity(LocationActivity activity)
    {
        if (activity == null)
        {
            Logger.LogWarning("GatheringPanel: Cannot open with null activity!", Logger.LogCategory.ActivityLog);
            return;
        }

        currentActivity = activity;
        UpdateTitle();
        PopulateVariantCards();
        gameObject.SetActive(true);

        Logger.LogInfo($"GatheringPanel: Opened for {activity.GetDisplayName()}", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Close the panel
    /// </summary>
    public void ClosePanel()
    {
        gameObject.SetActive(false);
        ClearVariantCards();

        // Slide activities section back in
        if (ActivitiesSectionPanel.Instance != null)
        {
            ActivitiesSectionPanel.Instance.SlideIn();
        }

        Logger.LogInfo("GatheringPanel: Panel closed", Logger.LogCategory.ActivityLog);
    }

    #endregion

    #region Private Methods

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
    /// Populate variants as cards in a grid
    /// </summary>
    private void PopulateVariantCards()
    {
        // Clear existing cards
        ClearVariantCards();

        if (currentActivity == null) return;

        // Get valid step-based variants only
        var validVariants = new List<ActivityVariant>();

        if (currentActivity.ActivityVariants != null)
        {
            foreach (var variant in currentActivity.ActivityVariants)
            {
                // Only include step-based (non-time-based) variants
                if (variant != null && variant.IsValidVariant() && !variant.IsTimeBased)
                {
                    validVariants.Add(variant);
                }
            }
        }

        Logger.LogInfo($"GatheringPanel: Found {validVariants.Count} valid harvesting variants", Logger.LogCategory.ActivityLog);

        // Create card for each variant
        foreach (var variant in validVariants)
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
    /// Create a card for a variant
    /// </summary>
    private void CreateVariantCard(ActivityVariant variant)
    {
        if (variant == null || cardsContainer == null) return;

        if (harvestingCardPrefab == null)
        {
            Logger.LogError("GatheringPanel: No harvestingCardPrefab assigned!", Logger.LogCategory.ActivityLog);
            return;
        }

        // Instantiate the card
        GameObject cardObj = Instantiate(harvestingCardPrefab, cardsContainer);
        instantiatedCards.Add(cardObj);

        // Setup the card
        HarvestingActivityCard harvestingCard = cardObj.GetComponent<HarvestingActivityCard>();
        if (harvestingCard == null)
        {
            Logger.LogError("GatheringPanel: HarvestingCardPrefab doesn't have HarvestingActivityCard component!", Logger.LogCategory.ActivityLog);
            return;
        }

        harvestingCard.Setup(variant);
        harvestingCard.OnCardClicked += OnVariantCardClicked;

        Logger.LogInfo($"GatheringPanel: Created card for {variant.VariantName}", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Handle variant card click
    /// </summary>
    private void OnVariantCardClicked(ActivityVariant variant)
    {
        Logger.LogInfo($"GatheringPanel: Variant selected: {variant.VariantName}", Logger.LogCategory.ActivityLog);

        // Start the step-based activity via ActivityManager
        if (ActivityManager.Instance != null && currentActivity != null)
        {
            string activityId = currentActivity.ActivityId;
            string variantId = ActivityRegistry.GenerateVariantId(variant.VariantName);

            bool success = ActivityManager.Instance.StartActivity(activityId, variantId);

            if (success)
            {
                Logger.LogInfo($"Successfully started harvesting activity: {variant.GetDisplayName()}", Logger.LogCategory.ActivityLog);
            }
            else
            {
                Logger.LogWarning($"Failed to start harvesting activity: {variant.GetDisplayName()}", Logger.LogCategory.ActivityLog);
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
                HarvestingActivityCard harvestingCard = card.GetComponent<HarvestingActivityCard>();
                if (harvestingCard != null)
                {
                    harvestingCard.OnCardClicked -= OnVariantCardClicked;
                }

                Destroy(card);
            }
        }
        instantiatedCards.Clear();
    }

    #endregion

    void OnDestroy()
    {
        ClearVariantCards();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
        }
    }
}
