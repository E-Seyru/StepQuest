// Purpose: Panel to select activity variants
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
    [SerializeField] private Transform variantsContainer;
    [SerializeField] private GameObject variantButtonPrefab;
    [SerializeField] private Button closeButton;

    // Current state
    private LocationActivity currentActivity;
    private List<GameObject> instantiatedButtons = new List<GameObject>();

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

        // Start hidden
        gameObject.SetActive(false);
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
        PopulateVariants();
        gameObject.SetActive(true);

        Debug.Log($"ActivityVariantsPanel: Opened for {activity.GetDisplayName()}");
    }

    /// <summary>
    /// Populate variants as buttons
    /// </summary>
    private void PopulateVariants()
    {
        // Clear existing buttons
        ClearVariantButtons();

        if (currentActivity == null) return;

        // Update title
        if (titleText != null)
        {
            titleText.text = $"Choisir une variante - {currentActivity.GetDisplayName()}";
        }

        // Get valid variants - utilise ActivityVariants directement
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

        Debug.Log($"Found {validVariants.Count} valid variants");

        // Create button for each variant
        foreach (var variant in validVariants)
        {
            CreateVariantButton(variant);
        }
    }

    /// <summary>
    /// Create a button for a variant
    /// </summary>
    private void CreateVariantButton(ActivityVariant variant)
    {
        if (variantButtonPrefab == null || variantsContainer == null || variant == null) return;

        // Instantiate button
        GameObject buttonObj = Instantiate(variantButtonPrefab, variantsContainer);
        instantiatedButtons.Add(buttonObj);

        // Get button component
        Button button = buttonObj.GetComponent<Button>();
        if (button == null) button = buttonObj.GetComponentInChildren<Button>();

        // Get image component for icon
        Image iconImage = buttonObj.GetComponentInChildren<Image>();

        // Get text component for name
        TextMeshProUGUI nameText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

        // Setup button data
        if (nameText != null)
        {
            nameText.text = variant.VariantName;
        }

        if (iconImage != null && variant.VariantIcon != null)
        {
            iconImage.sprite = variant.VariantIcon;
        }

        // Setup click handler
        if (button != null)
        {
            button.onClick.AddListener(() => OnVariantButtonClicked(variant));
        }

        Debug.Log($"Created button for variant: {variant.VariantName}");
    }

    /// <summary>
    /// Handle variant button click
    /// </summary>
    // Dans OnVariantButtonClicked()
    /// <summary>
    /// Handle variant button click
    /// </summary>
    private void OnVariantButtonClicked(ActivityVariant variant)
    {
        Debug.Log($"Variant selected: {variant.VariantName}");

        // MODIFIE : Démarrer l'activité via ActivityManager avec détection automatique du type
        if (ActivityManager.Instance != null && currentActivity != null)
        {
            string activityId = currentActivity.ActivityId;
            string variantId = ActivityRegistry.GenerateVariantId(variant.VariantName);

            bool success;

            // NOUVEAU : Vérifier le type d'activité et appeler la bonne méthode
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

        OnVariantSelected?.Invoke(variant);
        ClosePanel();
    }

    /// <summary>
    /// Clear all variant buttons
    /// </summary>
    private void ClearVariantButtons()
    {
        foreach (var button in instantiatedButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        instantiatedButtons.Clear();
    }

    /// <summary>
    /// Close the panel
    /// </summary>
    public void ClosePanel()
    {
        gameObject.SetActive(false);
        ClearVariantButtons();
        Debug.Log("ActivityVariantsPanel: Panel closed");
    }
}