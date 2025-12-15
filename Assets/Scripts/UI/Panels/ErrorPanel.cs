// Purpose: Simple error panel that appears when travel is not possible
// Filepath: Assets/Scripts/UI/Panels/ErrorPanel.cs
using TMPro;
using UnityEngine;

public class ErrorPanel : SingletonMonoBehaviour<ErrorPanel>
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Display Settings")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private LeanTweenType fadeInEase = LeanTweenType.easeOutQuad;
    [SerializeField] private LeanTweenType fadeOutEase = LeanTweenType.easeInQuad;

    [Header("Position Settings")]
    [SerializeField] private Vector2 poiOffset = new Vector2(100f, 50f);
    [Tooltip("Offset en pixels par rapport au POI (X = droite, Y = haut)")]

    private bool isDisplaying = false;
    private Transform currentPOI = null;
    private Vector2 currentOffset;

    void Start()
    {
        gameObject.SetActive(false);

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    void Update()
    {
        if (isDisplaying && currentPOI != null)
        {
            UpdatePosition();
        }
    }

    protected override void OnSingletonDestroyed()
    {
        LeanTween.cancel(gameObject);
        currentPOI = null;
    }

    public void ShowError(string message, Transform poiTransform = null)
    {
        if (isDisplaying) return;

        if (errorText != null)
        {
            errorText.text = message;
        }

        currentPOI = poiTransform;
        CalculateAdaptedOffset();
        UpdatePosition();

        canvasGroup.alpha = 0f;
        gameObject.SetActive(true);
        isDisplaying = true;

        LeanTween.cancel(gameObject);

        LeanTween.alphaCanvas(canvasGroup, 1f, fadeInDuration)
            .setEase(fadeInEase)
            .setOnComplete(() =>
            {
                LeanTween.delayedCall(displayDuration, () =>
                {
                    HideError();
                });
            });
    }

    public void HideError()
    {
        if (!isDisplaying) return;

        LeanTween.cancel(gameObject);

        LeanTween.alphaCanvas(canvasGroup, 0f, fadeOutDuration)
            .setEase(fadeOutEase)
            .setOnComplete(() =>
            {
                gameObject.SetActive(false);
                isDisplaying = false;
                currentPOI = null;
            });
    }

    private void CalculateAdaptedOffset()
    {
        currentOffset = poiOffset;

        if (currentPOI == null) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null) return;

        float panelWidth = rectTransform.rect.width;
        Vector3 poiScreenPosition = mainCamera.WorldToScreenPoint(currentPOI.position);
        Vector3 targetScreenPosition = poiScreenPosition + new Vector3(poiOffset.x, poiOffset.y, 0f);

        float hiddenWidth = (targetScreenPosition.x + panelWidth) - Screen.width;

        if (hiddenWidth > 0 && hiddenWidth > panelWidth * 0.25f)
        {
            currentOffset.x = -Mathf.Abs(poiOffset.x);
        }
    }

    private void UpdatePosition()
    {
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null) return;

        if (currentPOI == null)
        {
            rectTransform.anchoredPosition = new Vector2(0f, 200f);
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("ErrorPanel: Main camera not found!");
            rectTransform.anchoredPosition = new Vector2(0f, 200f);
            return;
        }

        Vector3 poiScreenPosition = mainCamera.WorldToScreenPoint(currentPOI.position);
        Vector3 targetScreenPosition = poiScreenPosition + new Vector3(currentOffset.x, currentOffset.y, 0f);

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            Vector2 localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                targetScreenPosition,
                parentCanvas.worldCamera,
                out localPosition
            );

            rectTransform.anchoredPosition = localPosition;
        }
        else
        {
            rectTransform.position = targetScreenPosition;
        }
    }
}
