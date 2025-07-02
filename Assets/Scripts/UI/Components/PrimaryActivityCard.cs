// Purpose: UI Card for Primary Activities (Activity definitions)
// Filepath: Assets/Scripts/UI/Components/PrimaryActivityCard.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PrimaryActivityCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button cardButton;

    // Data
    private ActivityDefinition activityDefinition;

    // Events
    public System.Action<ActivityDefinition> OnCardClicked;

    void Start()
    {
        if (cardButton != null)
        {
            cardButton.onClick.AddListener(OnCardButtonClicked);
        }
    }

    /// <summary>
    /// Setup the card with activity definition data
    /// </summary>
    public void Setup(ActivityDefinition activity)
    {
        if (activity == null)
        {
            Debug.LogWarning("PrimaryActivityCard: Cannot setup with null activity!");
            return;
        }

        if (!activity.IsValidActivity())
        {
            Debug.LogWarning($"PrimaryActivityCard: Activity '{activity.name}' is not valid!");
            return;
        }

        activityDefinition = activity;

        SetupBasicInfo();

        Debug.Log($"PrimaryActivityCard: Setup completed for {activity.GetDisplayName()}");
    }

    /// <summary>
    /// Setup basic card information (title, icon, description)
    /// </summary>
    private void SetupBasicInfo()
    {
        // Title
        if (titleText != null)
        {
            titleText.text = activityDefinition.GetDisplayName();
        }

        // Icon
        if (iconImage != null && activityDefinition.GetIcon() != null)
        {
            iconImage.sprite = activityDefinition.GetIcon();
        }

        // Description
        if (descriptionText != null)
        {
            if (!string.IsNullOrEmpty(activityDefinition.BaseDescription))
            {
                descriptionText.text = activityDefinition.BaseDescription;
            }
            else
            {
                descriptionText.text = "Aucune description disponible.";
            }
        }
    }

    /// <summary>
    /// Handle card button click
    /// </summary>
    private void OnCardButtonClicked()
    {
        if (activityDefinition != null)
        {
            Debug.Log($"PrimaryActivityCard: Card clicked for {activityDefinition.GetDisplayName()}");
            OnCardClicked?.Invoke(activityDefinition);
        }
    }

    /// <summary>
    /// Get the activity definition this card represents
    /// </summary>
    public ActivityDefinition GetActivityDefinition()
    {
        return activityDefinition;
    }

    /// <summary>
    /// Update the card's visual state (useful for selection, availability, etc.)
    /// </summary>
    public void UpdateCardState(bool isAvailable = true, bool isSelected = false)
    {
        if (cardButton != null)
        {
            cardButton.interactable = isAvailable;
        }

        // You can add visual feedback here for selection state
        // For example, changing the card's background color or border
        if (isSelected)
        {
            // Visual feedback for selected state
            // e.g., change background color, add border, etc.
        }
    }
}