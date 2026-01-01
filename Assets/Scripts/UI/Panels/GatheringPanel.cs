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

    [Header("Slide Animation Settings")]
    [SerializeField] private float slideAnimationDuration = 0.3f;
    [SerializeField] private LeanTweenType slideEaseType = LeanTweenType.easeOutBack;
    [SerializeField] private float slideOffset = 500f;

    // Current state
    private LocationActivity currentActivity;
    private List<GameObject> instantiatedCards = new List<GameObject>();

    // Animation state
    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private int currentTween = -1;

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

        // Cache RectTransform
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        // Setup close button
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        // Store original position
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
        }

        // Start hidden
        gameObject.SetActive(false);
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

        // Animate slide in from bottom
        AnimateSlideIn();

        Logger.LogInfo($"GatheringPanel: Opened for {activity.GetDisplayName()}", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Close the panel
    /// </summary>
    public void ClosePanel()
    {
        // Animate slide out then deactivate
        AnimateSlideOut(() =>
        {
            gameObject.SetActive(false);
            ClearVariantCards();
        });

        Logger.LogInfo("GatheringPanel: Panel closed", Logger.LogCategory.ActivityLog);
    }

    #endregion

    #region Animation Methods

    /// <summary>
    /// Animate panel sliding in from bottom
    /// </summary>
    private void AnimateSlideIn()
    {
        if (rectTransform == null) return;

        // Cancel any existing tween
        CancelCurrentTween();

        // Start from below the screen
        rectTransform.anchoredPosition = new Vector2(originalPosition.x, originalPosition.y - slideOffset);

        // Animate to original position
        currentTween = LeanTween.moveY(rectTransform, originalPosition.y, slideAnimationDuration)
            .setEase(slideEaseType)
            .setOnComplete(() => currentTween = -1)
            .id;
    }

    /// <summary>
    /// Animate panel sliding out to bottom
    /// </summary>
    private void AnimateSlideOut(Action onComplete = null)
    {
        if (rectTransform == null)
        {
            onComplete?.Invoke();
            return;
        }

        // Cancel any existing tween
        CancelCurrentTween();

        // Animate down
        float targetY = originalPosition.y - slideOffset;
        currentTween = LeanTween.moveY(rectTransform, targetY, slideAnimationDuration)
            .setEase(LeanTweenType.easeInBack)
            .setOnComplete(() =>
            {
                currentTween = -1;
                // Reset position for next open
                rectTransform.anchoredPosition = originalPosition;
                onComplete?.Invoke();
            })
            .id;
    }

    /// <summary>
    /// Cancel any running tween
    /// </summary>
    private void CancelCurrentTween()
    {
        if (currentTween >= 0)
        {
            LeanTween.cancel(currentTween);
            currentTween = -1;
        }
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
        CancelCurrentTween();
        ClearVariantCards();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePanel);
        }
    }
}
