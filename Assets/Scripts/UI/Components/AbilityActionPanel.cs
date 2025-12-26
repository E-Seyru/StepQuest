// Purpose: Contextual panel that appears when clicking on any ability
// Filepath: Assets/Scripts/UI/Components/AbilityActionPanel.cs

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel that displays ability details and allows equip/unequip actions
/// </summary>
public class AbilityActionPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform abilityIconContainer; // Container with border that holds the icon
    [SerializeField] private Image abilityIcon; // The actual icon image inside the container
    [SerializeField] private TextMeshProUGUI abilityNameText;
    [SerializeField] private TextMeshProUGUI abilityDescriptionText;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private TextMeshProUGUI weightText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI effectsText;

    [Header("Action Buttons")]
    [SerializeField] private Button actionButton; // Equip or Unequip
    [SerializeField] private TextMeshProUGUI actionButtonText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundButton;

    [Header("Button Colors")]
    [SerializeField] private Color equipColor = new Color(0.2f, 0.8f, 0.2f); // Green
    [SerializeField] private Color unequipColor = new Color(0.8f, 0.2f, 0.2f); // Red
    [SerializeField] private Color disabledButtonColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private LeanTweenType slideInEase = LeanTweenType.easeOutBack;
    [SerializeField] private LeanTweenType slideOutEase = LeanTweenType.easeInBack;

    // State
    private AbilityDefinition currentAbility;
    private bool isEquipped;
    private RectTransform rectTransform;
    private int currentTween = -1;

    public static AbilityActionPanel Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            rectTransform = GetComponent<RectTransform>();
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupButtons();
        gameObject.SetActive(false);
    }

    private void SetupButtons()
    {
        if (actionButton != null)
            actionButton.onClick.AddListener(OnActionClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(HidePanel);

        if (backgroundButton != null)
            backgroundButton.onClick.AddListener(HidePanel);
    }

    /// <summary>
    /// Show the panel with ability information
    /// </summary>
    public void ShowPanel(AbilityDefinition ability, bool equipped, Vector2 worldPosition)
    {
        if (ability == null) return;

        currentAbility = ability;
        isEquipped = equipped;

        // Update UI
        UpdateAbilityDisplay();
        UpdateActionButton();

        // Position panel near the clicked ability
        PositionPanel(worldPosition);

        // Show with animation
        gameObject.SetActive(true);
        AnimateIn();
    }

    private void UpdateAbilityDisplay()
    {
        if (currentAbility == null) return;

        // Icon Container - apply aspect ratio to the container (including border)
        if (abilityIconContainer != null)
        {
            // Set aspect ratio based on weight (1:2 ratio like in combat)
            int weight = currentAbility.Weight > 0 ? currentAbility.Weight : 1;
            float aspectRatio = weight / 2f; // Weight 1 = 1:2, Weight 2 = 2:2 (1:1), Weight 3 = 3:2, etc.

            var aspect = abilityIconContainer.GetComponent<AspectRatioFitter>();
            if (aspect == null)
            {
                aspect = abilityIconContainer.gameObject.AddComponent<AspectRatioFitter>();
            }
            aspect.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            aspect.aspectRatio = aspectRatio;
        }

        // Icon Image - just set the sprite
        if (abilityIcon != null)
        {
            abilityIcon.sprite = currentAbility.AbilityIcon;
            abilityIcon.color = Color.white;
        }

        // Name
        if (abilityNameText != null)
        {
            abilityNameText.text = currentAbility.GetDisplayName();
        }

        // Description
        if (abilityDescriptionText != null)
        {
            abilityDescriptionText.text = currentAbility.Description;
        }

        // Stats
        if (cooldownText != null)
        {
            cooldownText.text = $"Cooldown: {currentAbility.Cooldown}s";
        }

        if (weightText != null)
        {
            weightText.text = $"Weight: {currentAbility.Weight}";
        }

        // Damage (if applicable)
        if (damageText != null)
        {
            bool hasDamage = false;
            foreach (var effect in currentAbility.Effects)
            {
                if (effect.Type == AbilityEffectType.Damage)
                {
                    damageText.text = $"Damage: {effect.Value}";
                    damageText.gameObject.SetActive(true);
                    hasDamage = true;
                    break;
                }
            }

            if (!hasDamage)
            {
                damageText.gameObject.SetActive(false);
            }
        }

        // Effects summary
        if (effectsText != null)
        {
            string effectsSummary = "";
            foreach (var effect in currentAbility.Effects)
            {
                switch (effect.Type)
                {
                    case AbilityEffectType.Heal:
                        effectsSummary += $"• Heal: {effect.Value}\n";
                        break;
                    case AbilityEffectType.Shield:
                        effectsSummary += $"• Shield: {effect.Value}\n";
                        break;
                    case AbilityEffectType.StatusEffect:
                        if (effect.StatusEffect != null)
                        {
                            effectsSummary += $"• Apply: {effect.StatusEffect.EffectName}\n";
                        }
                        break;
                }
            }

            effectsText.text = effectsSummary;
        }
    }

    private void UpdateActionButton()
    {
        if (actionButton == null || actionButtonText == null) return;

        if (isEquipped)
        {
            // Unequip button
            actionButtonText.text = "Unequip";
            var buttonImage = actionButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = unequipColor;
            }
        }
        else
        {
            // Equip button
            bool canEquip = AbilityManager.Instance != null &&
                           AbilityManager.Instance.CanEquipAbility(currentAbility.AbilityID);

            actionButtonText.text = canEquip ? "Equip" : "Equip (No Space)";

            var buttonImage = actionButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = canEquip ? equipColor : disabledButtonColor;
            }

            actionButton.interactable = canEquip;
        }
    }

    private void PositionPanel(Vector2 worldPosition)
    {
        // Position panel near the clicked ability
        // Simple centering for now
        rectTransform.anchoredPosition = Vector2.zero;
    }

    private void AnimateIn()
    {
        if (currentTween >= 0)
        {
            LeanTween.cancel(currentTween);
        }

        // Scale from 0 to 1
        rectTransform.localScale = Vector3.zero;
        currentTween = LeanTween.scale(rectTransform, Vector3.one, animationDuration)
            .setEase(slideInEase)
            .id;
    }

    private void AnimateOut(System.Action onComplete = null)
    {
        if (currentTween >= 0)
        {
            LeanTween.cancel(currentTween);
        }

        currentTween = LeanTween.scale(rectTransform, Vector3.zero, animationDuration)
            .setEase(slideOutEase)
            .setOnComplete(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            })
            .id;
    }

    public void HidePanel()
    {
        AnimateOut();
    }

    private void OnActionClicked()
    {
        if (currentAbility == null || AbilityManager.Instance == null) return;

        if (isEquipped)
        {
            // Unequip
            if (AbilityManager.Instance.TryUnequipAbility(currentAbility.AbilityID))
            {
                Logger.LogInfo($"AbilityActionPanel: Unequipped '{currentAbility.GetDisplayName()}'", Logger.LogCategory.General);
                HidePanel();
            }
        }
        else
        {
            // Equip
            if (AbilityManager.Instance.TryEquipAbility(currentAbility.AbilityID))
            {
                Logger.LogInfo($"AbilityActionPanel: Equipped '{currentAbility.GetDisplayName()}'", Logger.LogCategory.General);
                HidePanel();
            }
        }
    }
}
