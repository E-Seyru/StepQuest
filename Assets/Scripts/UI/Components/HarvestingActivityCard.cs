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
        CheckLevelRequirement();

        Debug.Log($"HarvestingActivityCard: Setup completed for {variant.VariantName}");
    }

    /// <summary>
    /// Verifier le niveau requis
    /// </summary>
    private void CheckLevelRequirement()
    {
        hasRequiredLevel = true;

        if (activityVariant != null && activityVariant.UnlockRequirement > 0)
        {
            // Obtenir le niveau de la compétence principale
            string mainSkillId = activityVariant.GetMainSkillId();

            if (!string.IsNullOrEmpty(mainSkillId) && XpManager.Instance != null)
            {
                int playerLevel = XpManager.Instance.GetPlayerSkill(mainSkillId).Level;
                hasRequiredLevel = playerLevel >= activityVariant.UnlockRequirement;

                if (!hasRequiredLevel)
                {
                    Debug.Log($"HarvestingActivityCard: Level insufficient for {activityVariant.VariantName}. Required: {activityVariant.UnlockRequirement}, Player: {playerLevel}");
                }
            }
        }

        UpdateVisualState();
        UpdateLevelTextColor(); // Mettre à jour la couleur du texte de niveau
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
            // Vérifier le niveau - PAS d'animation si niveau insuffisant
            if (!hasRequiredLevel)
            {
                Debug.Log($"HarvestingActivityCard: Level requirement not met for {activityVariant.VariantName}");
                return; // Pas d'animation, juste ignorer le clic
            }

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

    /// <summary>
    /// Mettre a jour manuellement l'etat du niveau (a appeler quand le niveau change)
    /// </summary>
    public void RefreshLevelState()
    {
        CheckLevelRequirement();
    }

    /// <summary>
    /// Mettre à jour la couleur du texte de niveau
    /// </summary>
    private void UpdateLevelTextColor()
    {
        if (levelRequiredText != null)
        {
            levelRequiredText.color = hasRequiredLevel ? Color.white : Color.red;
        }
    }
}