// Purpose: Contextual panel that appears when clicking on any ability
// Filepath: Assets/Scripts/UI/Components/AbilityActionPanel.cs

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel that displays ability details and allows equip/unequip actions
/// </summary>
public class AbilityActionPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject abilityIconContainer_1x2; // Container for weight 1 abilities (1:2 ratio)
    [SerializeField] private Image abilityIcon_1x2; // Icon image for 1x2 container
    [SerializeField] private GameObject abilityIconContainer_2x2; // Container for weight 2 abilities (2:2 ratio)
    [SerializeField] private Image abilityIcon_2x2; // Icon image for 2x2 container
    [SerializeField] private GameObject abilityIconContainer_3x2; // Container for weight 3 abilities (3:2 ratio)
    [SerializeField] private Image abilityIcon_3x2; // Icon image for 3x2 container
    [SerializeField] private TextMeshProUGUI abilityNameText;
    [SerializeField] private TextMeshProUGUI abilityDescriptionText;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private TextMeshProUGUI weightText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI effectsText;
    [SerializeField] private Transform abilityTypeContainer; // Horizontal container for ability type badges

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

    [Header("Type Badge Settings")]
    [SerializeField] private GameObject typeBadgePrefab; // Single neutral prefab for all tags

    [Header("Stats Grid Settings")]
    [SerializeField] private Transform statsGridContainer; // Container for stat grid items
    [SerializeField] private GameObject statGridItemPrefab; // Prefab with Image + TextMeshProUGUI

    [Header("Effect Type Icons")]
    [Tooltip("Icon for Damage effects")]
    [SerializeField] private Sprite damageIcon;
    [Tooltip("Icon for Heal effects")]
    [SerializeField] private Sprite healIcon;
    [Tooltip("Icon for Shield effects")]
    [SerializeField] private Sprite shieldIcon;

    // State
    private AbilityDefinition currentAbility;
    private bool isEquipped;
    private RectTransform rectTransform;
    private int currentTween = -1;
    private List<GameObject> typeBadges = new List<GameObject>();
    private List<GameObject> statGridItems = new List<GameObject>();

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

        // Determine which container to show based on ability weight
        int weight = currentAbility.Weight > 0 ? currentAbility.Weight : 1;

        // Hide all containers first
        if (abilityIconContainer_1x2 != null) abilityIconContainer_1x2.SetActive(false);
        if (abilityIconContainer_2x2 != null) abilityIconContainer_2x2.SetActive(false);
        if (abilityIconContainer_3x2 != null) abilityIconContainer_3x2.SetActive(false);

        // Show and setup the appropriate container
        GameObject activeContainer = null;
        Image activeIcon = null;

        switch (weight)
        {
            case 1:
                activeContainer = abilityIconContainer_1x2;
                activeIcon = abilityIcon_1x2;
                break;
            case 2:
                activeContainer = abilityIconContainer_2x2;
                activeIcon = abilityIcon_2x2;
                break;
            case 3:
                activeContainer = abilityIconContainer_3x2;
                activeIcon = abilityIcon_3x2;
                break;
            default:
                // Fallback to 1x2 for unexpected weights
                activeContainer = abilityIconContainer_1x2;
                activeIcon = abilityIcon_1x2;
                break;
        }

        if (activeContainer != null)
        {
            activeContainer.SetActive(true);
        }

        if (activeIcon != null)
        {
            activeIcon.sprite = currentAbility.AbilityIcon;
            activeIcon.color = Color.white;
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
            cooldownText.text = $"{currentAbility.Cooldown}s";
        }

        if (weightText != null)
        {
            weightText.text = $"Weight: {currentAbility.Weight}";
        }

        // Update ability type badges
        UpdateAbilityTypeBadges();

        // Update stats grid
        UpdateStatsGrid();
    }

    /// <summary>
    /// Update the ability type badges in the horizontal container
    /// </summary>
    private void UpdateAbilityTypeBadges()
    {
        // Clear existing badges
        ClearTypeBadges();

        if (abilityTypeContainer == null || currentAbility == null || currentAbility.Effects == null)
            return;

        // Track which types we've already added (to avoid duplicates)
        HashSet<AbilityEffectType> addedTypes = new HashSet<AbilityEffectType>();
        HashSet<StatusEffectType> addedStatusEffects = new HashSet<StatusEffectType>();

        foreach (var effect in currentAbility.Effects)
        {
            if (effect == null)
                continue;

            // Handle status effects separately
            if (effect.Type == AbilityEffectType.StatusEffect)
            {
                if (effect.StatusEffect != null && !addedStatusEffects.Contains(effect.StatusEffect.EffectType))
                {
                    addedStatusEffects.Add(effect.StatusEffect.EffectType);
                    CreateStatusEffectBadge(effect.StatusEffect.EffectType);
                }
            }
            else if (!addedTypes.Contains(effect.Type))
            {
                addedTypes.Add(effect.Type);
                CreateTypeBadge(effect.Type);
            }
        }
    }

    /// <summary>
    /// Create a visual badge for an ability type
    /// </summary>
    private void CreateTypeBadge(AbilityEffectType effectType)
    {
        if (typeBadgePrefab == null)
        {
            Logger.LogWarning($"AbilityActionPanel: No type badge prefab assigned", Logger.LogCategory.General);
            return;
        }

        GameObject badge = Instantiate(typeBadgePrefab, abilityTypeContainer);

        // Set the text
        var textComponent = badge.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = GetLabelForEffectType(effectType);
            // Enable auto-sizing so text scales with container
            textComponent.enableAutoSizing = true;
        }

        // Set the color
        var imageComponent = badge.GetComponent<Image>();
        if (imageComponent != null)
        {
            imageComponent.color = GetColorForEffectType(effectType);
        }

        typeBadges.Add(badge);
    }

    /// <summary>
    /// Create a visual badge for a status effect
    /// </summary>
    private void CreateStatusEffectBadge(StatusEffectType statusEffectType)
    {
        if (typeBadgePrefab == null)
        {
            Logger.LogWarning($"AbilityActionPanel: No type badge prefab assigned", Logger.LogCategory.General);
            return;
        }

        GameObject badge = Instantiate(typeBadgePrefab, abilityTypeContainer);

        // Set the text
        var textComponent = badge.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent != null)
        {
            textComponent.text = GetLabelForStatusEffectType(statusEffectType);
            // Enable auto-sizing so text scales with container
            textComponent.enableAutoSizing = true;
        }

        // Set the color
        var imageComponent = badge.GetComponent<Image>();
        if (imageComponent != null)
        {
            imageComponent.color = GetColorForStatusEffectType(statusEffectType);
        }

        typeBadges.Add(badge);
    }

    /// <summary>
    /// Get the display label for an effect type (max 5 characters)
    /// </summary>
    private string GetLabelForEffectType(AbilityEffectType effectType)
    {
        switch (effectType)
        {
            case AbilityEffectType.Damage:
                return "DMG";
            case AbilityEffectType.Heal:
                return "HEAL";
            case AbilityEffectType.Shield:
                return "SHIELD";
            default:
                return effectType.ToString().Substring(0, Mathf.Min(5, effectType.ToString().Length));
        }
    }

    /// <summary>
    /// Get the display label for a status effect type (max 5 characters)
    /// </summary>
    private string GetLabelForStatusEffectType(StatusEffectType statusEffectType)
    {
        switch (statusEffectType)
        {
            case StatusEffectType.Poison:
                return "POISN";
            case StatusEffectType.Burn:
                return "BURN";
            case StatusEffectType.Bleed:
                return "BLEED";
            case StatusEffectType.Stun:
                return "STUN";
            case StatusEffectType.Regeneration:
                return "REGEN";
            case StatusEffectType.Shield:
                return "SHIELD";
            case StatusEffectType.AttackBuff:
                return "ATK+";
            case StatusEffectType.DefenseBuff:
                return "DEF+";
            case StatusEffectType.SpeedBuff:
                return "SPD+";
            case StatusEffectType.AttackDebuff:
                return "ATK-";
            case StatusEffectType.DefenseDebuff:
                return "DEF-";
            case StatusEffectType.SpeedDebuff:
                return "SPD-";
            default:
                return statusEffectType.ToString().Substring(0, Mathf.Min(5, statusEffectType.ToString().Length));
        }
    }

    /// <summary>
    /// Get the color for an effect type
    /// </summary>
    private Color GetColorForEffectType(AbilityEffectType effectType)
    {
        switch (effectType)
        {
            case AbilityEffectType.Damage:
                return new Color(0.85f, 0.2f, 0.2f); // Red
            case AbilityEffectType.Heal:
                return new Color(0.2f, 0.85f, 0.3f); // Green
            case AbilityEffectType.Shield:
                return new Color(0.3f, 0.6f, 0.9f); // Blue
            default:
                return Color.gray;
        }
    }

    /// <summary>
    /// Get the color for a status effect type
    /// </summary>
    private Color GetColorForStatusEffectType(StatusEffectType statusEffectType)
    {
        switch (statusEffectType)
        {
            case StatusEffectType.Poison:
                return new Color(0.5f, 0.8f, 0.2f); // Toxic green
            case StatusEffectType.Burn:
                return new Color(0.95f, 0.5f, 0.1f); // Orange/fire
            case StatusEffectType.Bleed:
                return new Color(0.8f, 0.1f, 0.1f); // Dark red
            case StatusEffectType.Stun:
                return new Color(0.9f, 0.9f, 0.3f); // Yellow
            case StatusEffectType.Regeneration:
                return new Color(0.3f, 0.9f, 0.5f); // Light green
            case StatusEffectType.Shield:
                return new Color(0.4f, 0.7f, 0.95f); // Light blue
            case StatusEffectType.AttackBuff:
                return new Color(0.9f, 0.3f, 0.3f); // Light red (attack)
            case StatusEffectType.DefenseBuff:
                return new Color(0.3f, 0.5f, 0.85f); // Medium blue (defense)
            case StatusEffectType.SpeedBuff:
                return new Color(0.95f, 0.85f, 0.3f); // Gold (speed)
            case StatusEffectType.AttackDebuff:
                return new Color(0.6f, 0.2f, 0.2f); // Dark red (attack down)
            case StatusEffectType.DefenseDebuff:
                return new Color(0.3f, 0.3f, 0.6f); // Dark blue (defense down)
            case StatusEffectType.SpeedDebuff:
                return new Color(0.6f, 0.5f, 0.2f); // Dark gold (speed down)
            default:
                return Color.gray;
        }
    }

    /// <summary>
    /// Clear all type badges
    /// </summary>
    private void ClearTypeBadges()
    {
        foreach (var badge in typeBadges)
        {
            if (badge != null)
                Destroy(badge);
        }
        typeBadges.Clear();
    }

    /// <summary>
    /// Update the stats grid with ability effects
    /// </summary>
    private void UpdateStatsGrid()
    {
        // Clear existing grid items
        ClearStatsGrid();

        if (statsGridContainer == null || currentAbility == null || currentAbility.Effects == null)
            return;

        foreach (var effect in currentAbility.Effects)
        {
            if (effect == null) continue;

            switch (effect.Type)
            {
                case AbilityEffectType.Damage:
                    CreateStatGridItem(damageIcon, effect.Value.ToString("F0"));
                    break;

                case AbilityEffectType.Heal:
                    CreateStatGridItem(healIcon, effect.Value.ToString("F0"));
                    break;

                case AbilityEffectType.Shield:
                    CreateStatGridItem(shieldIcon, effect.Value.ToString("F0"));
                    break;

                case AbilityEffectType.StatusEffect:
                    if (effect.StatusEffect != null)
                    {
                        Sprite icon = effect.StatusEffect.EffectIcon;
                        string value = effect.StatusEffectStacks.ToString();
                        CreateStatGridItem(icon, value);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Create a single stat grid item showing icon + value
    /// </summary>
    private void CreateStatGridItem(Sprite icon, string value)
    {
        if (statGridItemPrefab == null)
        {
            Logger.LogWarning($"AbilityActionPanel: No stat grid item prefab assigned", Logger.LogCategory.General);
            return;
        }

        GameObject gridItem = Instantiate(statGridItemPrefab, statsGridContainer);

        // Ensure LayoutElement exists so the grid item respects the GridLayoutGroup's cell size
        var layoutElement = gridItem.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gridItem.AddComponent<LayoutElement>();
        }
        // Don't override size - let GridLayoutGroup control it
        layoutElement.ignoreLayout = false;

        // Check if prefab has StatGridItem component (preferred approach)
        var statGridItemComponent = gridItem.GetComponent<StatGridItem>();
        if (statGridItemComponent != null)
        {
            // Use the assigned references from the prefab
            Image iconImage = statGridItemComponent.IconImage;
            TextMeshProUGUI valueText = statGridItemComponent.ValueText;

            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;

                // Make icon fill its container
                RectTransform iconRect = iconImage.GetComponent<RectTransform>();
                if (iconRect != null)
                {
                    iconRect.anchorMin = Vector2.zero;
                    iconRect.anchorMax = Vector2.one;
                    iconRect.sizeDelta = Vector2.zero;
                    iconRect.anchoredPosition = Vector2.zero;
                }
            }

            if (valueText != null)
            {
                valueText.text = value;

                // Make text stretch to fit and enable auto-sizing
                valueText.enableAutoSizing = true;

                RectTransform textRect = valueText.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;
                    textRect.anchoredPosition = Vector2.zero;
                }
            }
        }
        else
        {
            // Fallback: try to find components automatically
            Image iconImage = null;
            Transform iconTransform = gridItem.transform.Find("Icon");
            if (iconTransform != null)
            {
                iconImage = iconTransform.GetComponent<Image>();

                // Make icon fill its container
                RectTransform iconRect = iconImage.GetComponent<RectTransform>();
                if (iconRect != null)
                {
                    iconRect.anchorMin = Vector2.zero;
                    iconRect.anchorMax = Vector2.one;
                    iconRect.sizeDelta = Vector2.zero;
                    iconRect.anchoredPosition = Vector2.zero;
                }
            }

            // Fallback: get all Images and use the second one (skip root background)
            if (iconImage == null)
            {
                var allImages = gridItem.GetComponentsInChildren<Image>();
                if (allImages.Length > 1)
                {
                    iconImage = allImages[1]; // Skip first (usually background), use second

                    // Make icon fill its container
                    RectTransform iconRect = iconImage.GetComponent<RectTransform>();
                    if (iconRect != null)
                    {
                        iconRect.anchorMin = Vector2.zero;
                        iconRect.anchorMax = Vector2.one;
                        iconRect.sizeDelta = Vector2.zero;
                        iconRect.anchoredPosition = Vector2.zero;
                    }
                }
                else if (allImages.Length > 0)
                {
                    iconImage = allImages[0]; // Only one image, use it

                    // Make icon fill its container
                    RectTransform iconRect = iconImage.GetComponent<RectTransform>();
                    if (iconRect != null)
                    {
                        iconRect.anchorMin = Vector2.zero;
                        iconRect.anchorMax = Vector2.one;
                        iconRect.sizeDelta = Vector2.zero;
                        iconRect.anchoredPosition = Vector2.zero;
                    }
                }
            }

            if (iconImage != null && icon != null)
            {
                iconImage.sprite = icon;
            }

            // Set the value text
            var valueText = gridItem.GetComponentInChildren<TextMeshProUGUI>();
            if (valueText != null)
            {
                valueText.text = value;

                // Make text stretch to fit and enable auto-sizing
                valueText.enableAutoSizing = true;

                RectTransform textRect = valueText.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;
                    textRect.anchoredPosition = Vector2.zero;
                }
            }
        }

        statGridItems.Add(gridItem);
    }

    /// <summary>
    /// Clear all stat grid items
    /// </summary>
    private void ClearStatsGrid()
    {
        foreach (var item in statGridItems)
        {
            if (item != null)
                Destroy(item);
        }
        statGridItems.Clear();
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
