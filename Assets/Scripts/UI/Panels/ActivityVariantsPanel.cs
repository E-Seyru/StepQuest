// Purpose: Panel to select activity variants with card-based grid layout
// Filepath: Assets/Scripts/UI/Panels/ActivityVariantsPanel.cs
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActivityVariantsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private ScrollRect scrollView;
    [SerializeField] private Transform cardsContainer;
    [SerializeField] private Button closeButton;

    [Header("Card Prefabs")]
    [SerializeField] private GameObject craftingCardPrefab;
    [SerializeField] private GameObject harvestingCardPrefab;

    // Current state
    private LocationActivity currentActivity;
    private List<GameObject> instantiatedCards = new List<GameObject>();

    // Events
    public static event Action<ActivityVariant> OnVariantSelected;

    public static ActivityVariantsPanel Instance { get; private set; }

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
        gridLayout.spacing = new Vector2(30f, 30f); // Plus d'écart entre les cartes
        gridLayout.padding = new RectOffset(30, 30, 30, 30);

        // Set reasonable cell size (you might need to adjust this based on your card design)
        gridLayout.cellSize = new Vector2(200f, 280f);

        Debug.Log("ActivityVariantsPanel: Grid layout configured");
    }

    /// <summary>
    /// Open panel with specific activity variants
    /// </summary>
    public void OpenWithActivity(LocationActivity activity)
    {
        if (activity == null)
        {
            Debug.LogWarning("ActivityVariantsPanel: Cannot open with null activity!");
            return;
        }

        currentActivity = activity;
        PopulateVariantCards();
        gameObject.SetActive(true);

        Debug.Log($"ActivityVariantsPanel: Opened for {activity.GetDisplayName()}");
    }

    /// <summary>
    /// Populate variants as cards in a grid
    /// </summary>
    private void PopulateVariantCards()
    {
        // Clear existing cards
        ClearVariantCards();

        if (currentActivity == null) return;

        // Update title
        if (titleText != null)
        {
            titleText.text = $"Choisir une variante - {currentActivity.GetDisplayName()}";
        }

        // Get valid variants
        var validVariants = new List<ActivityVariant>();

        if (currentActivity.ActivityVariants != null)
        {
            foreach (var variant in currentActivity.ActivityVariants)
            {
                if (variant != null && variant.IsValidVariant())
                {
                    validVariants.Add(variant);
                }
            }
        }

        Debug.Log($"ActivityVariantsPanel: Found {validVariants.Count} valid variants");

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
    /// Create a card for a variant (crafting or harvesting)
    /// </summary>
    private void CreateVariantCard(ActivityVariant variant)
    {
        if (variant == null || cardsContainer == null) return;

        GameObject cardPrefab = variant.IsTimeBased ? craftingCardPrefab : harvestingCardPrefab;

        if (cardPrefab == null)
        {
            Debug.LogError($"ActivityVariantsPanel: No prefab assigned for {(variant.IsTimeBased ? "crafting" : "harvesting")} card!");
            return;
        }

        // Instantiate the appropriate card
        GameObject cardObj = Instantiate(cardPrefab, cardsContainer);
        instantiatedCards.Add(cardObj);

        // Setup the card based on its type
        if (variant.IsTimeBased)
        {
            SetupCraftingCard(cardObj, variant);
        }
        else
        {
            SetupHarvestingCard(cardObj, variant);
        }

        Debug.Log($"ActivityVariantsPanel: Created {(variant.IsTimeBased ? "crafting" : "harvesting")} card for {variant.VariantName}");
    }

    /// <summary>
    /// Setup a crafting card
    /// </summary>
    private void SetupCraftingCard(GameObject cardObj, ActivityVariant variant)
    {
        CraftingActivityCard craftingCard = cardObj.GetComponent<CraftingActivityCard>();
        if (craftingCard == null)
        {
            Debug.LogError("ActivityVariantsPanel: CraftingCardPrefab doesn't have CraftingActivityCard component!");
            return;
        }

        craftingCard.Setup(variant);
        craftingCard.OnCardClicked += OnVariantCardClicked;
    }

    /// <summary>
    /// Setup a harvesting card
    /// </summary>
    private void SetupHarvestingCard(GameObject cardObj, ActivityVariant variant)
    {
        HarvestingActivityCard harvestingCard = cardObj.GetComponent<HarvestingActivityCard>();
        if (harvestingCard == null)
        {
            Debug.LogError("ActivityVariantsPanel: HarvestingCardPrefab doesn't have HarvestingActivityCard component!");
            return;
        }

        harvestingCard.Setup(variant);
        harvestingCard.OnCardClicked += OnVariantCardClicked;
    }

    /// <summary>
    /// Handle variant card click
    /// </summary>
    private void OnVariantCardClicked(ActivityVariant variant)
    {
        Debug.Log($"ActivityVariantsPanel: Variant selected: {variant.VariantName}");

        // Start the activity via ActivityManager with automatic type detection
        if (ActivityManager.Instance != null && currentActivity != null)
        {
            string activityId = currentActivity.ActivityId;
            string variantId = ActivityRegistry.GenerateVariantId(variant.VariantName);

            bool success;

            // Check activity type and call the appropriate method
            if (variant.IsTimeBased)
            {
                Debug.Log($"Starting time-based activity: {variant.GetDisplayName()}");
                success = ActivityManager.Instance.StartTimedActivity(activityId, variantId);
            }
            else
            {
                Debug.Log($"Starting step-based activity: {variant.GetDisplayName()}");
                success = ActivityManager.Instance.StartActivity(activityId, variantId);
            }

            if (success)
            {
                Debug.Log($"Successfully started activity: {variant.GetDisplayName()}");
            }
            else
            {
                Debug.LogWarning($"Failed to start activity: {variant.GetDisplayName()}");
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

    /// <summary>
    /// Close the panel
    /// </summary>
    public void ClosePanel()
    {
        gameObject.SetActive(false);
        ClearVariantCards();
        Debug.Log("ActivityVariantsPanel: Panel closed");
    }

    void OnDestroy()
    {
        ClearVariantCards();
    }
}