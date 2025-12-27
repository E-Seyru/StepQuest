// Purpose: UI Card for Harvesting Activities (Step-based activities) with gray overlay
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

    [Header("Gray Overlay")]
    [SerializeField] private GameObject grayOverlay; // Voile gris pour niveau insuffisant

    // Data
    private ActivityVariant activityVariant;
    private bool hasRequiredLevel = true;

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
            Logger.LogWarning("HarvestingActivityCard: Cannot setup with null variant!", Logger.LogCategory.ActivityLog);
            return;
        }

        if (variant.IsTimeBased)
        {
            Logger.LogWarning($"HarvestingActivityCard: Variant '{variant.VariantName}' is time-based, not step-based!", Logger.LogCategory.ActivityLog);
            return;
        }

        activityVariant = variant;

        SetupBasicInfo();
        SetupRequirements();
        CheckLevelRequirement();

        Logger.LogInfo($"HarvestingActivityCard: Setup completed for {variant.VariantName}", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Verifier le niveau requis
    /// </summary>
    private void CheckLevelRequirement()
    {
        hasRequiredLevel = true;

        if (activityVariant != null && activityVariant.UnlockRequirement > 0)
        {
            // Obtenir le niveau de la comp�tence principale
            string mainSkillId = activityVariant.GetMainSkillId();

            if (!string.IsNullOrEmpty(mainSkillId) && XpManager.Instance != null)
            {
                var skill = XpManager.Instance.GetPlayerSkill(mainSkillId);
                int playerLevel = skill?.Level ?? 0;
                hasRequiredLevel = playerLevel >= activityVariant.UnlockRequirement;

                if (!hasRequiredLevel)
                {
                    Logger.LogInfo($"HarvestingActivityCard: Level insufficient for {activityVariant.VariantName}. Required: {activityVariant.UnlockRequirement}, Player: {playerLevel}", Logger.LogCategory.ActivityLog);
                }
            }
        }

        UpdateVisualState();
        UpdateLevelTextColor(); // Mettre � jour la couleur du texte de niveau
    }

    /// <summary>
    /// Mettre a jour l'etat visuel de la carte (overlay gris)
    /// </summary>
    private void UpdateVisualState()
    {
        if (grayOverlay != null)
        {
            grayOverlay.SetActive(!hasRequiredLevel);
        }
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
                stepsRequiredText.text = "Instantane";
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
            // V�rifier le niveau - PAS d'animation si niveau insuffisant
            if (!hasRequiredLevel)
            {
                Logger.LogInfo($"HarvestingActivityCard: Level requirement not met for {activityVariant.VariantName}", Logger.LogCategory.ActivityLog);
                return; // Pas d'animation, juste ignorer le clic
            }

            Logger.LogInfo($"HarvestingActivityCard: Card clicked for {activityVariant.VariantName}", Logger.LogCategory.ActivityLog);
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

    /// <summary>
    /// Mettre a jour manuellement l'etat du niveau (a appeler quand le niveau change)
    /// </summary>
    public void RefreshLevelState()
    {
        CheckLevelRequirement();
    }

    /// <summary>
    /// Mettre � jour la couleur du texte de niveau
    /// </summary>
    private void UpdateLevelTextColor()
    {
        if (levelRequiredText != null)
        {
            levelRequiredText.color = hasRequiredLevel ? Color.white : Color.red;
        }
    }

    private void OnDestroy()
    {
        if (cardButton != null)
        {
            cardButton.onClick.RemoveListener(OnCardButtonClicked);
        }
    }
}