// Purpose: Panel for displaying exploration results with sequential discovery reveals
// Filepath: Assets/Scripts/UI/Panels/ExplorationResultsPanel.cs
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel that displays exploration results, showing discoveries one at a time.
/// Flow: Summary -> Discovery 1 -> Discovery 2 -> ... -> Close
/// Uses a single layout - just swaps content between summary and discovery displays.
/// </summary>
public class ExplorationResultsPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelContainer;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image discoveryImage;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private Image rarityBackground;
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI continueButtonText;

    [Header("Rarity Colors")]
    [SerializeField] private Color commonColor = new Color(0.8f, 0.8f, 0.8f);
    [SerializeField] private Color uncommonColor = new Color(0.12f, 0.8f, 0f);
    [SerializeField] private Color rareColor = new Color(0f, 0.44f, 0.87f);
    [SerializeField] private Color epicColor = new Color(0.64f, 0.21f, 0.93f);
    [SerializeField] private Color legendaryColor = new Color(1f, 0.5f, 0f);

    [Header("Text Configuration")]
    [SerializeField] private string successTitleSingle = "Decouverte !";
    [SerializeField] private string successTitleMultiple = "Decouvertes !";
    [SerializeField] private string noDiscoveryTitle = "Exploration terminee";
    [SerializeField] private string noDiscoveryDescription = "Votre exploration n'a rien revele cette fois-ci. Continuez a explorer !";
    [SerializeField] private string continueText = "Continuer";
    [SerializeField] private string closeText = "Fermer";

    // State
    private List<DiscoveryResult> discoveries = new List<DiscoveryResult>();
    private int currentDiscoveryIndex = -1; // -1 = showing summary
    private Action onPanelClosed;

    // Singleton
    public static ExplorationResultsPanel Instance { get; private set; }

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
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        // Hide by default
        if (panelContainer != null)
        {
            panelContainer.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    #region Public Methods

    /// <summary>
    /// Show the results panel with discovered items
    /// </summary>
    /// <param name="discoveredItems">List of items discovered during exploration</param>
    /// <param name="onClosed">Callback when panel is closed</param>
    public void ShowResults(List<DiscoveryResult> discoveredItems, Action onClosed = null)
    {
        discoveries = discoveredItems ?? new List<DiscoveryResult>();
        onPanelClosed = onClosed;
        currentDiscoveryIndex = -1;

        // Show panel
        if (panelContainer != null)
        {
            panelContainer.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }

        // Show summary first
        ShowSummary();

        Logger.LogInfo($"ExplorationResultsPanel: Showing results with {discoveries.Count} discoveries", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Close the panel
    /// </summary>
    public void ClosePanel()
    {
        if (panelContainer != null)
        {
            panelContainer.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }

        // Invoke callback
        onPanelClosed?.Invoke();
        onPanelClosed = null;

        // Clear state
        discoveries.Clear();
        currentDiscoveryIndex = -1;

        Logger.LogInfo("ExplorationResultsPanel: Panel closed", Logger.LogCategory.ActivityLog);
    }

    #endregion

    #region Private Methods - View Management

    /// <summary>
    /// Show the summary view
    /// </summary>
    private void ShowSummary()
    {
        // Hide discovery-specific elements
        if (discoveryImage != null) discoveryImage.gameObject.SetActive(false);
        if (typeText != null) typeText.gameObject.SetActive(false);
        if (rarityBackground != null) rarityBackground.gameObject.SetActive(false);

        // Update text
        if (discoveries.Count == 0)
        {
            if (titleText != null) titleText.text = noDiscoveryTitle;
            if (descriptionText != null) descriptionText.text = noDiscoveryDescription;
        }
        else if (discoveries.Count == 1)
        {
            if (titleText != null) titleText.text = successTitleSingle;
            if (descriptionText != null)
            {
                descriptionText.text = "Votre exploration a porte ses fruits ! Vous avez fait une nouvelle decouverte !";
            }
        }
        else
        {
            if (titleText != null) titleText.text = successTitleMultiple;
            if (descriptionText != null)
            {
                descriptionText.text = $"Votre exploration a porte ses fruits ! Vous avez fait {discoveries.Count} nouvelles decouvertes !";
            }
        }

        UpdateButtonText();
    }

    /// <summary>
    /// Show a specific discovery
    /// </summary>
    private void ShowDiscovery(int index)
    {
        if (index < 0 || index >= discoveries.Count) return;

        var discovery = discoveries[index];

        // Show discovery-specific elements
        if (discoveryImage != null)
        {
            discoveryImage.gameObject.SetActive(true);
            if (discovery.Icon != null)
            {
                discoveryImage.sprite = discovery.Icon;
                discoveryImage.preserveAspect = true;
                discoveryImage.color = Color.white;
            }
        }

        if (typeText != null)
        {
            typeText.gameObject.SetActive(true);
            typeText.text = GetTypeText(discovery.Type);
        }

        if (rarityBackground != null)
        {
            rarityBackground.gameObject.SetActive(true);
            Color bgColor = GetRarityColor(discovery.Rarity);
            bgColor.a = 0.3f;
            rarityBackground.color = bgColor;
        }

        // Update title and description
        if (titleText != null)
        {
            titleText.text = discovery.Name;
        }

        if (descriptionText != null)
        {
            descriptionText.text = GetFlavorText(discovery);
        }

        UpdateButtonText();
    }

    /// <summary>
    /// Update the continue button text based on current state
    /// </summary>
    private void UpdateButtonText()
    {
        if (continueButtonText == null) return;

        bool isLastStep = (currentDiscoveryIndex >= discoveries.Count - 1);

        if (discoveries.Count == 0)
        {
            // No discoveries - just close
            continueButtonText.text = closeText;
        }
        else if (currentDiscoveryIndex == -1)
        {
            // On summary, will show first discovery
            continueButtonText.text = continueText;
        }
        else if (isLastStep)
        {
            // On last discovery - close
            continueButtonText.text = closeText;
        }
        else
        {
            // More discoveries to show
            continueButtonText.text = continueText;
        }
    }

    #endregion

    #region Private Methods - Event Handlers

    /// <summary>
    /// Handle continue button click
    /// </summary>
    private void OnContinueClicked()
    {
        // No discoveries - just close
        if (discoveries.Count == 0)
        {
            ClosePanel();
            return;
        }

        // Move to next step
        currentDiscoveryIndex++;

        if (currentDiscoveryIndex >= discoveries.Count)
        {
            // All discoveries shown - close
            ClosePanel();
        }
        else
        {
            // Show next discovery
            ShowDiscovery(currentDiscoveryIndex);
        }
    }

    #endregion

    #region Private Methods - Helpers

    /// <summary>
    /// Get flavor text for a discovery
    /// </summary>
    private string GetFlavorText(DiscoveryResult discovery)
    {
        // Use custom flavor text if provided
        if (!string.IsNullOrEmpty(discovery.FlavorText))
        {
            return discovery.FlavorText;
        }

        // Generate default flavor text based on type
        switch (discovery.Type)
        {
            case DiscoverableType.Enemy:
                return $"Vous avez decouvert un nouvel ennemi : {discovery.Name}. Preparez-vous au combat !";
            case DiscoverableType.NPC:
                return $"Vous avez rencontre {discovery.Name}. Cette personne pourrait avoir des choses interessantes a vous dire.";
            case DiscoverableType.Activity:
                return $"Vous avez decouvert une nouvelle activite : {discovery.Name}. De nouvelles opportunites s'offrent a vous !";
            case DiscoverableType.Dungeon:
                return $"Vous avez decouvert un nouveau lieu : {discovery.Name}. L'aventure vous attend !";
            default:
                return $"Vous avez decouvert : {discovery.Name}.";
        }
    }

    /// <summary>
    /// Get display text for discovery type
    /// </summary>
    private string GetTypeText(DiscoverableType type)
    {
        switch (type)
        {
            case DiscoverableType.Enemy:
                return "Ennemi";
            case DiscoverableType.NPC:
                return "Personnage";
            case DiscoverableType.Activity:
                return "Activite";
            case DiscoverableType.Dungeon:
                return "Lieu";
            default:
                return "Decouverte";
        }
    }

    /// <summary>
    /// Get color for rarity level
    /// </summary>
    private Color GetRarityColor(DiscoveryRarity rarity)
    {
        switch (rarity)
        {
            case DiscoveryRarity.Common:
                return commonColor;
            case DiscoveryRarity.Uncommon:
                return uncommonColor;
            case DiscoveryRarity.Rare:
                return rareColor;
            case DiscoveryRarity.Epic:
                return epicColor;
            case DiscoveryRarity.Legendary:
                return legendaryColor;
            default:
                return commonColor;
        }
    }

    #endregion

    void OnDestroy()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);
        }
    }
}

/// <summary>
/// Data structure for a discovery result
/// </summary>
[System.Serializable]
public class DiscoveryResult
{
    public string Id;
    public string Name;
    public DiscoverableType Type;
    public DiscoveryRarity Rarity;
    public Sprite Icon;
    public string FlavorText;
    public int BonusXP;
}
