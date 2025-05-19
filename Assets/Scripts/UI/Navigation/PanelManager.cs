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
    [SerializeField] private List<int> alwaysActivePanelIndices = new List<int>(); // Nouveaux indices pour les panneaux toujours actifs
    [SerializeField] private GameObject mapPanel; // Panneau de carte


    [Header("Swipe Settings")]
    [SerializeField] private float minSwipeDistance = 50f;
    [SerializeField] private float swipeThreshold = 0.2f;

    [Header("Animation Settings")]
    [SerializeField] private float transitionSpeed = 10f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private Vector2 offScreenPosition = new Vector2(10000, 10000); // Position hors écran

    [Header("Events")]
    public UnityEvent<int> OnPanelChanged;

    // Private variables
    private bool mapIsHidden = true;
    private int currentPanelIndex;
    private Vector2 touchStartPosition;
    private float touchStartTime;
    private bool isTransitioning;
    private RectTransform panelContainer;
    private Vector2 containerStartPosition;
    private Vector2 containerTargetPosition;
    private float transitionStartTime;
    private Dictionary<int, Vector2> originalPositions = new Dictionary<int, Vector2>(); // Pour stocker les positions d'origine
    private int previousPanelIndex = 0;
    public static PanelManager Instance { get; private set; }

    private void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

        }
        else
        {

            Destroy(gameObject);
        }

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
        // Store original positions for all panels
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] != null)
            {
                RectTransform rectTransform = panels[i].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    originalPositions[i] = rectTransform.anchoredPosition;
                }
            }
        }

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
        // Handle each panel based on whether it should stay active
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] != null)
            {
                if (alwaysActivePanelIndices.Contains(i))
                {
                    // Always active panels start active but moved off-screen
                    panels[i].SetActive(true);
                    RectTransform rectTransform = panels[i].GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        rectTransform.anchoredPosition = offScreenPosition;
                    }
                }
                else
                {
                    // Regular panels start inactive
                    panels[i].SetActive(false);
                }
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

        // Handle current panel (hide it)
        if (currentPanelIndex >= 0 && currentPanelIndex < panels.Count && panels[currentPanelIndex] != null)
        {
            if (alwaysActivePanelIndices.Contains(currentPanelIndex))
            {
                // Move to off-screen instead of deactivating
                RectTransform rectTransform = panels[currentPanelIndex].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = offScreenPosition;
                }
            }
            else
            {
                // Deactivate regular panels
                panels[currentPanelIndex].SetActive(false);
            }
        }

        // Handle new panel (show it)
        if (alwaysActivePanelIndices.Contains(index))
        {
            // Panel should already be active, just move it back to original position
            RectTransform rectTransform = panels[index].GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalPositions.ContainsKey(index)
                    ? originalPositions[index]
                    : Vector2.zero;
            }
        }
        else
        {
            // Activate regular panel
            panels[index].SetActive(true);
        }

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

        // Prepare both panels for transition
        RectTransform fromRect = panels[fromIndex].GetComponent<RectTransform>();
        RectTransform toRect = panels[toIndex].GetComponent<RectTransform>();

        // Ensure both panels are active for transition
        if (!alwaysActivePanelIndices.Contains(fromIndex))
        {
            panels[fromIndex].SetActive(true);
        }

        if (!alwaysActivePanelIndices.Contains(toIndex))
        {
            panels[toIndex].SetActive(true);
        }

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
        StartCoroutine(TransitionPanels(fromRect, toRect, direction, toIndex, fromIndex));
    }

    private IEnumerator TransitionPanels(RectTransform fromRect, RectTransform toRect,
                                        TransitionDirection direction, int targetIndex, int fromIndex)
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

        // Handle old panel after transition
        if (alwaysActivePanelIndices.Contains(fromIndex))
        {
            // Move off-screen but keep active
            fromRect.anchoredPosition = offScreenPosition;
        }
        else
        {
            // Disable old panel to save resources
            fromRect.gameObject.SetActive(false);
        }

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

    // Add panel to always active list
    public void AddAlwaysActivePanel(int panelIndex)
    {
        if (panelIndex >= 0 && panelIndex < panels.Count && !alwaysActivePanelIndices.Contains(panelIndex))
        {
            alwaysActivePanelIndices.Add(panelIndex);

            // If panel is currently inactive, activate it but move off-screen
            if (!panels[panelIndex].activeSelf)
            {
                panels[panelIndex].SetActive(true);
                RectTransform rectTransform = panels[panelIndex].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = offScreenPosition;
                }
            }
        }
    }

    // Remove panel from always active list
    public void RemoveAlwaysActivePanel(int panelIndex)
    {
        if (alwaysActivePanelIndices.Contains(panelIndex))
        {
            alwaysActivePanelIndices.Remove(panelIndex);

            // If this is not the current panel, deactivate it
            if (panelIndex != currentPanelIndex)
            {
                panels[panelIndex].SetActive(false);
            }
        }
    }

    // Optional: Add panel programmatically
    public void AddPanel(GameObject panel, bool alwaysActive = false)
    {
        if (panel != null && !panels.Contains(panel))
        {
            panels.Add(panel);
            int index = panels.Count - 1;

            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                originalPositions[index] = rectTransform.anchoredPosition;
            }

            if (alwaysActive)
            {
                alwaysActivePanelIndices.Add(index);
                panel.SetActive(true);
                if (rectTransform != null && index != currentPanelIndex)
                {
                    rectTransform.anchoredPosition = offScreenPosition;
                }
            }
            else if (index != currentPanelIndex)
            {
                panel.SetActive(false);
            }
        }
    }

    // Optional: Remove panel programmatically
    public void RemovePanel(GameObject panel)
    {
        if (panel != null && panels.Contains(panel))
        {
            int index = panels.IndexOf(panel);
            bool wasActive = panel.activeSelf;

            panels.Remove(panel);

            // Remove from always active list if it was there
            if (alwaysActivePanelIndices.Contains(index))
            {
                alwaysActivePanelIndices.Remove(index);
            }

            // Update indices in always active list that are greater than the removed index
            for (int i = 0; i < alwaysActivePanelIndices.Count; i++)
            {
                if (alwaysActivePanelIndices[i] > index)
                {
                    alwaysActivePanelIndices[i]--;
                }
            }

            // Remove from original positions
            if (originalPositions.ContainsKey(index))
            {
                originalPositions.Remove(index);
            }

            // If the removed panel was active, show the first available panel
            if (wasActive && panels.Count > 0)
            {
                ShowPanel(0);
            }
        }
    }


    public void ShowAndHideMapPanel()
    {
        if (mapIsHidden)
        {

            previousPanelIndex = currentPanelIndex; // Sauvegarde le panel actuel
                                                    // Masque l’UI
            foreach (var panel in panels)
            {
                panel.SetActive(false);
            }
            mapPanel.SetActive(true);

            mapIsHidden = false;

        }
        else
        {
            mapPanel.SetActive(false);
            ShowPanel(previousPanelIndex); // Ré-affiche le panel d’avant
            mapIsHidden = true;
        }



    }
}