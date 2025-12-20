// Purpose: Tab system for switching between Activities, Combat, and Social sections
// Filepath: Assets/Scripts/UI/Panels/LocationDetailsTabs.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LocationDetailsTabs : MonoBehaviour
{
    [System.Serializable]
    public class TabItem
    {
        public Button button;
        public TextMeshProUGUI label;
        public GameObject contentSection;
        public int tabIndex;
    }

    [Header("Tab Items")]
    [SerializeField] private List<TabItem> tabItems = new List<TabItem>();

    [Header("Selection Style")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color normalColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private float animationDuration = 0.2f;

    private int currentSelectedIndex = 0;
    private bool isInitialized = false;

    void Awake()
    {
        // CRITICAL: Hide all content sections immediately to prevent them all showing at once
        for (int i = 0; i < tabItems.Count; i++)
        {
            if (tabItems[i].contentSection != null)
            {
                tabItems[i].contentSection.SetActive(false);
            }
        }
    }

    void Start()
    {
        // Setup button click handlers
        for (int i = 0; i < tabItems.Count; i++)
        {
            int index = i; // Capture for closure
            if (tabItems[i].button != null)
            {
                tabItems[i].button.onClick.AddListener(() => OnTabClicked(index));
            }
        }

        // Initialize - hide all tabs first, then select the first one
        InitializeTabs();
        isInitialized = true;
    }

    void OnEnable()
    {
        // Ensure all tabs are hidden first (in case they were re-enabled by their panels)
        for (int i = 0; i < tabItems.Count; i++)
        {
            if (tabItems[i].contentSection != null)
            {
                tabItems[i].contentSection.SetActive(false);
            }
        }

        // Only re-select if we've already been initialized
        if (isInitialized && currentSelectedIndex >= 0 && currentSelectedIndex < tabItems.Count)
        {
            SelectTab(currentSelectedIndex);
        }
    }

    /// <summary>
    /// Initialize all tabs - hide them all first, then select the first one
    /// </summary>
    private void InitializeTabs()
    {
        // First, hide all content sections and set all tabs to deselected state
        for (int i = 0; i < tabItems.Count; i++)
        {
            if (tabItems[i].contentSection != null)
            {
                tabItems[i].contentSection.SetActive(false);
            }

            // Set initial color to normal (deselected) state for button and all child images
            if (tabItems[i].button != null)
            {
                SetButtonImagesColor(tabItems[i].button, normalColor);
            }
        }

        // Then select the first tab
        currentSelectedIndex = -1; // Reset to force selection
        SelectTab(0);
    }

    void OnDestroy()
    {
        // Clean up button listeners
        foreach (var item in tabItems)
        {
            if (item.button != null)
            {
                item.button.onClick.RemoveAllListeners();
            }
        }
    }

    private void OnTabClicked(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabItems.Count) return;
        if (tabIndex == currentSelectedIndex) return;

        SelectTab(tabIndex);
    }

    /// <summary>
    /// Selects a tab by index
    /// </summary>
    public void SelectTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabItems.Count) return;

        // Deselect previous
        if (currentSelectedIndex >= 0 && currentSelectedIndex < tabItems.Count)
        {
            SetTabSelected(currentSelectedIndex, false);
        }

        // Select new
        SetTabSelected(tabIndex, true);

        currentSelectedIndex = tabIndex;
    }

    private void SetTabSelected(int tabIndex, bool selected)
    {
        TabItem item = tabItems[tabIndex];
        if (item == null) return;

        // Update button and child images color (grey out when not selected)
        if (item.button != null)
        {
            Color targetColor = selected ? selectedColor : normalColor;
            AnimateButtonImagesColor(item.button, targetColor);
        }

        // Show/hide content section
        if (item.contentSection != null)
        {
            item.contentSection.SetActive(selected);
        }
    }

    /// <summary>
    /// Set color for button's Image and all child Images immediately
    /// </summary>
    private void SetButtonImagesColor(Button button, Color color)
    {
        // Set button's own Image
        var buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = color;
        }

        // Set all child Images
        var childImages = button.GetComponentsInChildren<Image>(true);
        foreach (var img in childImages)
        {
            if (img != buttonImage) // Don't set the button's image twice
            {
                img.color = color;
            }
        }
    }

    /// <summary>
    /// Animate color for button's Image and all child Images
    /// </summary>
    private void AnimateButtonImagesColor(Button button, Color targetColor)
    {
        // Animate button's own Image
        var buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            LeanTween.cancel(buttonImage.gameObject);
            LeanTween.value(buttonImage.gameObject, buttonImage.color, targetColor, animationDuration)
                .setOnUpdate((Color val) => { buttonImage.color = val; })
                .setEase(LeanTweenType.easeOutQuad);
        }

        // Animate all child Images
        var childImages = button.GetComponentsInChildren<Image>(true);
        foreach (var img in childImages)
        {
            if (img != buttonImage) // Don't animate the button's image twice
            {
                LeanTween.cancel(img.gameObject);
                LeanTween.value(img.gameObject, img.color, targetColor, animationDuration)
                    .setOnUpdate((Color val) => { img.color = val; })
                    .setEase(LeanTweenType.easeOutQuad);
            }
        }
    }

    /// <summary>
    /// Get the currently selected tab index
    /// </summary>
    public int CurrentSelectedIndex => currentSelectedIndex;
}
