// Purpose: UI component for displaying a single ability in combat with vertical cooldown overlay
// Filepath: Assets/Scripts/UI/Combat/CombatAbilityUI.cs

using CombatEvents;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a single ability during combat with cooldown visualization.
/// Uses a vertical overlay that shrinks from top to bottom during cooldown.
/// </summary>
public class CombatAbilityUI : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private Image abilityImage;
    [SerializeField] private Image cooldownOverlay;

    [Header("Animation")]
    [SerializeField] private float pulseScale = 1.1f;
    [SerializeField] private float pulseDuration = 0.1f;

    // Runtime
    private AbilityDefinition ability;
    private int instanceIndex;      // Position in the ability list (0, 1, 2...)
    private int duplicateIndex;     // Index for duplicate abilities (0 for first occurrence, 1 for second, etc.)
    private bool isPlayerAbility;
    private bool isOnCooldown;
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Subscribe to events
        EventBus.Subscribe<CombatAbilityCooldownStartedEvent>(OnCooldownStarted);
        EventBus.Subscribe<CombatAbilityUsedEvent>(OnAbilityUsed);

        // Initialize cooldown overlay
        if (cooldownOverlay != null)
        {
            cooldownOverlay.gameObject.SetActive(false);
            SetupCooldownOverlay();
        }
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<CombatAbilityCooldownStartedEvent>(OnCooldownStarted);
        EventBus.Unsubscribe<CombatAbilityUsedEvent>(OnAbilityUsed);

        // Cancel any running tweens
        LeanTween.cancel(gameObject);
        if (cooldownOverlay != null)
            LeanTween.cancel(cooldownOverlay.gameObject);
    }

    private void SetupCooldownOverlay()
    {
        if (cooldownOverlay == null) return;

        RectTransform overlayRect = cooldownOverlay.rectTransform;

        // Setup anchors for vertical fill from top
        overlayRect.anchorMin = new Vector2(0, 0);
        overlayRect.anchorMax = new Vector2(1, 1);
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
    }

    /// <summary>
    /// Initialize this ability display
    /// </summary>
    /// <param name="abilityDef">The ability definition</param>
    /// <param name="listIndex">Position in the ability list</param>
    /// <param name="isPlayer">Is this a player ability</param>
    /// <param name="dupIndex">Index for duplicate abilities (0 for first occurrence of this ability)</param>
    public void Setup(AbilityDefinition abilityDef, int listIndex, bool isPlayer, int dupIndex = 0)
    {
        ability = abilityDef;
        instanceIndex = listIndex;
        duplicateIndex = dupIndex;
        isPlayerAbility = isPlayer;

        if (ability == null) return;

        // Set visuals
        if (abilityImage != null)
        {
            if (ability.AbilityIcon != null)
            {
                abilityImage.sprite = ability.AbilityIcon;
            }
            abilityImage.color = ability.AbilityColor;
        }

        // Initialize cooldown overlay
        if (cooldownOverlay != null)
        {
            cooldownOverlay.gameObject.SetActive(false);
            SetupCooldownOverlay();
        }

        isOnCooldown = false;
    }

    private void OnCooldownStarted(CombatAbilityCooldownStartedEvent eventData)
    {
        // Check if this event is for our ability
        // Match by ability reference and isPlayer flag
        // instanceIndex is used for duplicate abilities (same ability equipped multiple times)
        if (eventData.Ability != ability ||
            eventData.IsPlayerAbility != isPlayerAbility)
        {
            return;
        }

        // Only respond if instance index matches (for duplicate abilities)
        // instanceIndex tracks duplicates of the SAME ability, not position in list
        if (eventData.InstanceIndex != duplicateIndex)
        {
            return;
        }

        StartCooldown(eventData.CooldownDuration);
    }

    private void OnAbilityUsed(CombatAbilityUsedEvent eventData)
    {
        // Check if this event is for our ability
        if (eventData.Ability != ability ||
            eventData.IsPlayerAbility != isPlayerAbility)
        {
            return;
        }

        // Only respond if instance index matches (for duplicate abilities)
        if (eventData.InstanceIndex != duplicateIndex)
        {
            return;
        }

        // Pulse animation when ability fires
        AnimatePulse();
    }

    /// <summary>
    /// Start the cooldown visualization (vertical overlay shrinking from top to bottom)
    /// </summary>
    public void StartCooldown(float duration)
    {
        if (cooldownOverlay == null) return;

        isOnCooldown = true;
        cooldownOverlay.gameObject.SetActive(true);

        // Reset overlay to full size
        RectTransform overlayRect = cooldownOverlay.rectTransform;
        overlayRect.anchorMax = new Vector2(1, 1);
        overlayRect.offsetMax = Vector2.zero;

        // Animate the overlay shrinking from top to bottom using anchorMax.y
        // Goes from 1 (full) to 0 (empty)
        LeanTween.cancel(cooldownOverlay.gameObject);
        LeanTween.value(cooldownOverlay.gameObject, UpdateCooldownOverlay, 1f, 0f, duration)
            .setOnComplete(OnCooldownComplete);
    }

    private void UpdateCooldownOverlay(float progress)
    {
        if (!isOnCooldown || cooldownOverlay == null) return;

        RectTransform overlayRect = cooldownOverlay.rectTransform;

        // Shrink from top: anchorMax.y goes from 1 to 0
        overlayRect.anchorMax = new Vector2(1, progress);
        overlayRect.offsetMax = Vector2.zero;

        // Ensure minimum visibility
        float minSize = 1f;
        if (overlayRect.rect.height < minSize && progress > 0)
        {
            Vector2 sizeDelta = overlayRect.sizeDelta;
            sizeDelta.y = minSize;
            overlayRect.sizeDelta = sizeDelta;
        }
    }

    private void OnCooldownComplete()
    {
        isOnCooldown = false;

        if (cooldownOverlay != null)
        {
            cooldownOverlay.gameObject.SetActive(false);
        }

        // Pulse the ability image to show it's ready
        AnimatePulse();
    }

    private void AnimatePulse()
    {
        if (abilityImage == null) return;

        LeanTween.cancel(abilityImage.gameObject);
        LeanTween.scale(abilityImage.gameObject, Vector3.one * pulseScale, pulseDuration)
            .setLoopPingPong(1)
            .setEaseInOutQuad();
    }

    /// <summary>
    /// Reset the ability display
    /// </summary>
    public void ResetDisplay()
    {
        ability = null;
        instanceIndex = 0;
        isOnCooldown = false;

        LeanTween.cancel(gameObject);
        if (cooldownOverlay != null)
        {
            LeanTween.cancel(cooldownOverlay.gameObject);
            cooldownOverlay.gameObject.SetActive(false);
        }

        if (abilityImage != null)
        {
            abilityImage.sprite = null;
            abilityImage.color = Color.white;
            abilityImage.transform.localScale = Vector3.one;
        }
    }
}
