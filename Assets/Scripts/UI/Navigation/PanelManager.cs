using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PanelManager : MonoBehaviour
{
    [Header("Panel Settings")]
    [SerializeField] private List<GameObject> panels = new List<GameObject>();
    [SerializeField] private int startingPanelIndex = 0;
    [SerializeField] private bool wrapAround = true;

    [Header("Swipe Settings")]
    [SerializeField] private float minSwipeDistance = 50f;
    [SerializeField] private float swipeThreshold = 0.2f;

    [Header("Animation Settings")]
    [SerializeField] private float transitionSpeed = 10f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Events")]
    public UnityEvent<int> OnPanelChanged;

    // Private variables
    private int currentPanelIndex;
    private Vector2 touchStartPosition;
    private float touchStartTime;
    private bool isTransitioning;
    private RectTransform panelContainer;
    private Vector2 containerStartPosition;
    private Vector2 containerTargetPosition;
    private float transitionStartTime;

    private void Awake()
    {
        // Get or create panel container
        panelContainer = GetComponent<RectTransform>();
        if (panelContainer == null)
        {
            panelContainer = gameObject.AddComponent<RectTransform>();
        }

        // Initialize panel visibility
        InitializePanels();
    }

    private void Start()
    {
        // Show the starting panel
        ShowPanel(startingPanelIndex);
    }

    private void Update()
    {
        // Don't process input during transitions
        if (isTransitioning)
        {
            UpdateTransition();
            return;
        }

        // Handle touch input
        HandleInput();
    }

    private void InitializePanels()
    {
        // Disable all panels initially
        foreach (GameObject panel in panels)
        {
            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }

    public void ShowPanel(int index)
    {
        // Validate index
        if (index < 0 || index >= panels.Count || panels[index] == null)
        {
            Debug.LogWarning("Invalid panel index: " + index);
            return;
        }

        // Disable current panel
        if (currentPanelIndex >= 0 && currentPanelIndex < panels.Count && panels[currentPanelIndex] != null)
        {
            panels[currentPanelIndex].SetActive(false);
        }

        // Enable new panel
        panels[index].SetActive(true);
        currentPanelIndex = index;

        // Invoke event
        OnPanelChanged?.Invoke(currentPanelIndex);
    }

    public void NextPanel()
    {
        int nextIndex = currentPanelIndex + 1;

        if (nextIndex >= panels.Count)
        {
            if (wrapAround)
            {
                nextIndex = 0;
            }
            else
            {
                return; // Don't go beyond last panel if wrapping is disabled
            }
        }

        StartTransition(currentPanelIndex, nextIndex, TransitionDirection.Right);
    }

    public void PreviousPanel()
    {
        int prevIndex = currentPanelIndex - 1;

        if (prevIndex < 0)
        {
            if (wrapAround)
            {
                prevIndex = panels.Count - 1;
            }
            else
            {
                return; // Don't go beyond first panel if wrapping is disabled
            }
        }

        StartTransition(currentPanelIndex, prevIndex, TransitionDirection.Left);
    }

    private void HandleInput()
    {
        // Touch input handling
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    touchStartPosition = touch.position;
                    touchStartTime = Time.time;
                    break;

                case TouchPhase.Ended:
                    Vector2 swipeDelta = touch.position - touchStartPosition;
                    float swipeTime = Time.time - touchStartTime;

                    // Check if swipe distance and time meet thresholds
                    if (swipeTime < swipeThreshold && Mathf.Abs(swipeDelta.x) > minSwipeDistance)
                    {
                        if (swipeDelta.x < 0)
                        {
                            // Swipe left -> next panel
                            NextPanel();
                        }
                        else
                        {
                            // Swipe right -> previous panel
                            PreviousPanel();
                        }
                    }
                    break;
            }
        }

        // For testing in editor with keyboard
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            NextPanel();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PreviousPanel();
        }
#endif
    }

    private enum TransitionDirection
    {
        Left,
        Right
    }

    private void StartTransition(int fromIndex, int toIndex, TransitionDirection direction)
    {
        // If already transitioning, ignore
        if (isTransitioning)
            return;

        // Setup transition
        isTransitioning = true;
        transitionStartTime = Time.time;

        // Enable both panels during transition
        panels[fromIndex].SetActive(true);
        panels[toIndex].SetActive(true);

        // Position panels side by side
        RectTransform fromRect = panels[fromIndex].GetComponent<RectTransform>();
        RectTransform toRect = panels[toIndex].GetComponent<RectTransform>();

        // Reset positions
        fromRect.anchoredPosition = Vector2.zero;

        // Position the target panel
        if (direction == TransitionDirection.Right)
        {
            // Coming from right side
            toRect.anchoredPosition = new Vector2(panelContainer.rect.width, 0);
        }
        else
        {
            // Coming from left side
            toRect.anchoredPosition = new Vector2(-panelContainer.rect.width, 0);
        }

        // Start coroutine for smooth transition
        StartCoroutine(TransitionPanels(fromRect, toRect, direction, toIndex));
    }

    private IEnumerator TransitionPanels(RectTransform fromRect, RectTransform toRect, TransitionDirection direction, int targetIndex)
    {
        float elapsedTime = 0f;
        float transitionDuration = 1f / transitionSpeed;
        Vector2 fromStartPos = fromRect.anchoredPosition;
        Vector2 toStartPos = toRect.anchoredPosition;

        // Determine target positions
        Vector2 fromTargetPos = direction == TransitionDirection.Right ?
            new Vector2(-panelContainer.rect.width, 0) : new Vector2(panelContainer.rect.width, 0);
        Vector2 toTargetPos = Vector2.zero;

        while (elapsedTime < transitionDuration)
        {
            float t = transitionCurve.Evaluate(elapsedTime / transitionDuration);

            // Move both panels
            fromRect.anchoredPosition = Vector2.Lerp(fromStartPos, fromTargetPos, t);
            toRect.anchoredPosition = Vector2.Lerp(toStartPos, toTargetPos, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure final positions are exact
        fromRect.anchoredPosition = fromTargetPos;
        toRect.anchoredPosition = toTargetPos;

        // Update current panel
        currentPanelIndex = targetIndex;

        // Disable old panel to save resources
        fromRect.gameObject.SetActive(false);

        // End transition state
        isTransitioning = false;

        // Invoke event
        OnPanelChanged?.Invoke(currentPanelIndex);
    }

    private void UpdateTransition()
    {
        // This method is reserved for additional transition effects if needed
    }

    // Public getters
    public int CurrentPanelIndex => currentPanelIndex;
    public int PanelCount => panels.Count;

    // Optional: Add panel programmatically
    public void AddPanel(GameObject panel)
    {
        if (panel != null && !panels.Contains(panel))
        {
            panels.Add(panel);
            panel.SetActive(false);
        }
    }

    // Optional: Remove panel programmatically
    public void RemovePanel(GameObject panel)
    {
        if (panel != null && panels.Contains(panel))
        {
            bool wasActive = panel.activeSelf;
            panels.Remove(panel);

            // If the removed panel was active, show the first available panel
            if (wasActive && panels.Count > 0)
            {
                ShowPanel(0);
            }
        }
    }
}