// Purpose: Popup panel to display rewards from dialogue
// Filepath: Assets/Scripts/UI/Panels/RewardPopupPanel.cs
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Popup panel that displays rewards (abilities, items) from dialogue.
/// Shows when a reward is granted and closes on click.
/// </summary>
public class RewardPopupPanel : MonoBehaviour
{
    public static RewardPopupPanel Instance { get; private set; }

    [Header("Panel References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Content")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI rewardText;
    [SerializeField] private Image rewardIcon;
    [SerializeField] private GameObject iconContainer;

    [Header("Button")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundButton;  // Click anywhere to close
    [SerializeField] private Button clickZone;  // Full panel click zone to close

    [Header("Animation")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float scaleAnimDuration = 0.2f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Input")]
    [SerializeField] private float closeDelay = 0.5f;  // Delay before clicks can close the popup

    // Events
    public event Action OnPopupClosed;

    // Runtime
    private bool _isVisible;
    private bool _canClose;  // True after closeDelay has passed
    private Coroutine _animCoroutine;

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

        SetupButtons();
        HideImmediate();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void SetupButtons()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (backgroundButton != null)
            backgroundButton.onClick.AddListener(Close);

        if (clickZone != null)
            clickZone.onClick.AddListener(Close);
    }

    /// <summary>
    /// Show the reward popup with ability reward
    /// </summary>
    public void ShowAbilityReward(string abilityId, string abilityName, Sprite icon = null)
    {
        if (titleText != null)
            titleText.text = "Nouvelle Competence !";

        if (rewardText != null)
            rewardText.text = abilityName;

        SetupIcon(icon);
        Show();
    }

    /// <summary>
    /// Show the reward popup with item reward
    /// </summary>
    public void ShowItemReward(string itemName, int quantity, Sprite icon = null)
    {
        if (titleText != null)
            titleText.text = "Objet Obtenu !";

        if (rewardText != null)
        {
            if (quantity > 1)
                rewardText.text = $"{quantity}x {itemName}";
            else
                rewardText.text = itemName;
        }

        SetupIcon(icon);
        Show();
    }

    /// <summary>
    /// Show the reward popup with multiple rewards
    /// </summary>
    public void ShowRewards(string abilityName, List<DialogueItemReward> items, Sprite icon = null)
    {
        var rewardLines = new List<string>();

        if (!string.IsNullOrEmpty(abilityName))
        {
            rewardLines.Add($"<b>Competence:</b> {abilityName}");
        }

        if (items != null)
        {
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(item.ItemId) || item.Quantity <= 0) continue;

                if (item.Quantity > 1)
                    rewardLines.Add($"<b>Objet:</b> {item.Quantity}x {item.ItemId}");
                else
                    rewardLines.Add($"<b>Objet:</b> {item.ItemId}");
            }
        }

        if (titleText != null)
            titleText.text = rewardLines.Count > 1 ? "Recompenses !" : "Recompense !";

        if (rewardText != null)
            rewardText.text = string.Join("\n", rewardLines);

        SetupIcon(icon);
        Show();
    }

    private void SetupIcon(Sprite icon)
    {
        if (iconContainer != null)
            iconContainer.SetActive(icon != null);

        if (rewardIcon != null && icon != null)
        {
            rewardIcon.sprite = icon;

            // Preserve aspect ratio - resize to fit within container
            if (icon.rect.width > 0 && icon.rect.height > 0)
            {
                float aspectRatio = icon.rect.width / icon.rect.height;
                var rectTransform = rewardIcon.rectTransform;

                // Get container size (or use current size as max)
                float maxWidth = rectTransform.rect.width > 0 ? rectTransform.rect.width : 100f;
                float maxHeight = rectTransform.rect.height > 0 ? rectTransform.rect.height : 100f;

                // Calculate size that fits within container while preserving aspect ratio
                float targetWidth, targetHeight;
                if (aspectRatio >= 1f)
                {
                    // Wider than tall - fit to width
                    targetWidth = maxWidth;
                    targetHeight = maxWidth / aspectRatio;
                }
                else
                {
                    // Taller than wide - fit to height
                    targetHeight = maxHeight;
                    targetWidth = maxHeight * aspectRatio;
                }

                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
            }
        }
    }

    private void Show()
    {
        if (popupPanel != null)
            popupPanel.SetActive(true);

        _isVisible = true;
        _canClose = false;  // Reset close permission

        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);

        _animCoroutine = StartCoroutine(AnimateIn());
    }

    private System.Collections.IEnumerator AnimateIn()
    {
        // Setup initial state
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        var panelTransform = popupPanel?.transform;
        if (panelTransform != null)
            panelTransform.localScale = Vector3.one * 0.8f;

        // Animate
        float elapsed = 0f;
        float duration = Mathf.Max(fadeInDuration, scaleAnimDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Clamp01(t / fadeInDuration * duration);

            if (panelTransform != null)
            {
                float scaleT = Mathf.Clamp01(t / scaleAnimDuration * duration);
                float scale = Mathf.LerpUnclamped(0.8f, 1f, scaleCurve.Evaluate(scaleT));
                panelTransform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        // Ensure final state
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (panelTransform != null)
            panelTransform.localScale = Vector3.one;

        // Wait for close delay before allowing clicks to close
        yield return new WaitForSeconds(closeDelay);
        _canClose = true;
    }

    /// <summary>
    /// Close the popup and fire the OnPopupClosed event
    /// </summary>
    public void Close()
    {
        if (!_isVisible) return;

        // Don't close if delay hasn't passed yet (prevent reflex clicks)
        if (!_canClose) return;

        _isVisible = false;
        _canClose = false;

        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);

        HideImmediate();

        // Fire event so DialoguePanelUI knows to close the dialogue
        OnPopupClosed?.Invoke();
    }

    private void HideImmediate()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Check if popup is currently visible
    /// </summary>
    public bool IsVisible => _isVisible;
}
