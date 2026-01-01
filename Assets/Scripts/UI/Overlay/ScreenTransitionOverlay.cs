// Purpose: Full-screen transition overlay that fades to black and back
// Filepath: Assets/Scripts/UI/Overlay/ScreenTransitionOverlay.cs
using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen overlay for transitions (fade to black, then fade back in).
/// Used for immersive transitions like entering the bank.
/// </summary>
public class ScreenTransitionOverlay : MonoBehaviour
{
    public static ScreenTransitionOverlay Instance { get; private set; }

    [Header("References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image overlayImage;

    [Header("Settings")]
    [SerializeField] private Color fadeColor = Color.black;
    [SerializeField] private float fadeOutDuration = 0.3f;  // Fade to black
    [SerializeField] private float fadeInDuration = 0.3f;   // Fade from black
    [SerializeField] private float holdDuration = 0.1f;     // Time to hold at black
    [SerializeField] private LeanTweenType fadeEaseType = LeanTweenType.easeInOutQuad;

    // State
    private bool _isTransitioning;
    private int _fadeOutTweenId = -1;
    private int _fadeInTweenId = -1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        CancelTweens();
    }

    private void Initialize()
    {
        // Ensure we have required components
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (overlayImage != null)
        {
            overlayImage.color = fadeColor;
        }

        // Start fully transparent and non-blocking
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    /// <summary>
    /// Perform a fade-out, execute action at peak darkness, then fade-in.
    /// </summary>
    /// <param name="onFadedOut">Action to execute when screen is fully black</param>
    /// <param name="onComplete">Action to execute when transition is complete</param>
    public void DoTransition(Action onFadedOut, Action onComplete = null)
    {
        if (_isTransitioning)
        {
            Logger.LogWarning("ScreenTransitionOverlay: Already transitioning, ignoring request", Logger.LogCategory.General);
            return;
        }

        _isTransitioning = true;

        // Block input during transition
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        // Fade out (to black)
        _fadeOutTweenId = LeanTween.alphaCanvas(canvasGroup, 1f, fadeOutDuration)
            .setEase(fadeEaseType)
            .setOnComplete(() =>
            {
                // Execute the action at peak darkness
                onFadedOut?.Invoke();

                // Hold at black briefly, then fade in
                LeanTween.delayedCall(holdDuration, () =>
                {
                    FadeIn(onComplete);
                });
            })
            .id;
    }

    /// <summary>
    /// Perform only a fade-out (to black), useful for custom transitions
    /// </summary>
    public void FadeOut(Action onComplete = null)
    {
        if (_isTransitioning)
        {
            Logger.LogWarning("ScreenTransitionOverlay: Already transitioning, ignoring request", Logger.LogCategory.General);
            return;
        }

        _isTransitioning = true;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        _fadeOutTweenId = LeanTween.alphaCanvas(canvasGroup, 1f, fadeOutDuration)
            .setEase(fadeEaseType)
            .setOnComplete(() =>
            {
                onComplete?.Invoke();
            })
            .id;
    }

    /// <summary>
    /// Perform only a fade-in (from current state to transparent)
    /// </summary>
    public void FadeIn(Action onComplete = null)
    {
        _fadeInTweenId = LeanTween.alphaCanvas(canvasGroup, 0f, fadeInDuration)
            .setEase(fadeEaseType)
            .setOnComplete(() =>
            {
                _isTransitioning = false;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
                onComplete?.Invoke();
            })
            .id;
    }

    /// <summary>
    /// Cancel any ongoing transition and reset state
    /// </summary>
    public void CancelTransition()
    {
        CancelTweens();

        _isTransitioning = false;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void CancelTweens()
    {
        if (_fadeOutTweenId != -1)
        {
            LeanTween.cancel(_fadeOutTweenId);
            _fadeOutTweenId = -1;
        }

        if (_fadeInTweenId != -1)
        {
            LeanTween.cancel(_fadeInTweenId);
            _fadeInTweenId = -1;
        }
    }

    /// <summary>
    /// Check if currently transitioning
    /// </summary>
    public bool IsTransitioning => _isTransitioning;
}
