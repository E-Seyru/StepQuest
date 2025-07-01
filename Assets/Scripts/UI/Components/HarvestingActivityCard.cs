// Purpose: UI Card for Harvesting Activities (Step-based activities)
// Filepath: Assets/Scripts/UI/Components/HarvestingActivityCard.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HarvestingActivityCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI levelRequiredText;
    [SerializeField] private TextMeshProUGUI stepsRequiredText;
    [SerializeField] private Button cardButton;

    // Data
    private ActivityVariant activityVariant;

    // Events
    public System.Action<ActivityVariant> OnCardClicked;

    void Start()
    {
        if (cardButton != null)
        {
            cardButton.onClick.AddListener(OnCardButtonClicked);
        }
    }

    /// <summary>
    /// Setup the card with activity variant data
    /// </summary>
    public void Setup(ActivityVariant variant)
    {
        if (variant == null)
        {
            Debug.LogWarning("HarvestingActivityCard: Cannot setup with null variant!");
            return;
        }

        if (variant.IsTimeBased)
        {
            Debug.LogWarning($"HarvestingActivityCard: Variant '{variant.VariantName}' is time-based, not step-based!");
            return;
        }

        activityVariant = variant;

        SetupBasicInfo();
        SetupRequirements();

        Debug.Log($"HarvestingActivityCard: Setup completed for {variant.VariantName}");
    }

    /// <summary>
    /// Setup basic card information (title, icon)
    /// </summary>
    private void SetupBasicInfo()
    {
        // Title
        if (titleText != null)
        {
            titleText.text = activityVariant.GetDisplayName();
        }

        // Icon
        if (iconImage != null && activityVariant.VariantIcon != null)
        {
            iconImage.sprite = activityVariant.VariantIcon;
        }
    }

    /// <summary>
    /// Setup requirements (level, steps)
    /// </summary>
    private void SetupRequirements()
    {
        // Level requirement (using UnlockRequirement for now)
        if (levelRequiredText != null)
        {
            int level = activityVariant.UnlockRequirement > 0 ? activityVariant.UnlockRequirement : 1;
            levelRequiredText.text = $"Lvl : {level}";
        }

        // Steps requirement
        if (stepsRequiredText != null)
        {
            if (activityVariant.ActionCost > 0)
            {
                stepsRequiredText.text = $"{activityVariant.ActionCost} pas";
            }
            else
            {
                stepsRequiredText.text = "Instantané";
            }
        }
    }

    /// <summary>
    /// Handle card button click
    /// </summary>
    private void OnCardButtonClicked()
    {
        if (activityVariant != null)
        {
            Debug.Log($"HarvestingActivityCard: Card clicked for {activityVariant.VariantName}");
            OnCardClicked?.Invoke(activityVariant);
        }
    }

    /// <summary>
    /// Get the activity variant this card represents
    /// </summary>
    public ActivityVariant GetActivityVariant()
    {
        return activityVariant;
    }
}