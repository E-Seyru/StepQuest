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
    [SerializeField] private List<int> alwaysActivePanelIndices = new List<int>();
    [SerializeField] private GameObject mapPanel;

    [Header("Swipe Settings")]
    [SerializeField] private float minSwipeDistance = 50f;
    [SerializeField] private float swipeThreshold = 0.2f;

    [Header("Animation Settings")]
    [SerializeField] private float transitionSpeed = 10f;
    [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private Vector2 offScreenPosition = new Vector2(10000, 10000);

    [Header("Events")]
    public UnityEvent<int> OnPanelChanged;
    public UnityEvent<bool> OnMapStateChanged; // Nouvel evenement pour notifier les changements d'etat de la carte

    // Private variables
    private bool mapIsHidden = true;
    private int currentPanelIndex;
    private Vector2 touchStartPosition;
    private float touchStartTime;
    private bool isTransitioning;
    private RectTransform panelContainer;
    private Dictionary<int, Vector2> originalPositions = new Dictionary<int, Vector2>();
    private int previousPanelIndex = 0;

    // OPTIMISATION : Variables pour eviter les Update() inutiles
    private bool isInputActive = false;
    private Coroutine inputCoroutine;

    // Drag blocking - prevent swipes shortly after dragging
    private float lastDragEndTime = -1f;
    private const float DRAG_SWIPE_BLOCK_DURATION = 0.5f;

    public static PanelManager Instance { get; private set; }

    // Propriete publique pour connaître l'etat de la carte
    public bool IsMapVisible => !mapIsHidden;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

        }
        else
        {
            Logger.LogWarning("PanelManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.General);
            Destroy(gameObject);
        }

        panelContainer = GetComponent<RectTransform>();
        if (panelContainer == null)
        {
            panelContainer = gameObject.AddComponent<RectTransform>();
        }

        InitializePanels();
    }

    private void Start()
    {
        // Store original positions for all panels
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] == null) continue;

            RectTransform rectTransform = panels[i].GetComponent<RectTransform>();
            if (rectTransform == null) continue;

            if (!originalPositions.ContainsKey(i))
                originalPositions[i] = rectTransform.anchoredPosition;
        }

        ShowPanel(startingPanelIndex);

        // OPTIMISATION : Demarrer la detection d'input seulement quand necessaire
        StartInputDetection();
    }

    // OPTIMISATION : Remplacer Update() par une coroutine qui ne tourne que quand necessaire
    private void StartInputDetection()
    {
        if (inputCoroutine != null)
            StopCoroutine(inputCoroutine);

        inputCoroutine = StartCoroutine(InputDetectionCoroutine());
    }

    private void StopInputDetection()
    {
        if (inputCoroutine != null)
        {
            StopCoroutine(inputCoroutine);
            inputCoroutine = null;
        }
    }

    private IEnumerator InputDetectionCoroutine()
    {
        while (true)
        {
            // OPTIMISATION : Arreter la detection si la carte est visible ou en transition
            if (!mapIsHidden || isTransitioning)
            {
                yield return new WaitForSeconds(0.1f); // Verifier moins souvent
                continue;
            }

            HandleInput();

            // OPTIMISATION : Attendre un frame seulement si necessaire
            yield return null;
        }
    }

    private void InitializePanels()
    {
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] == null) continue;

            RectTransform rectTransform = panels[i].GetComponent<RectTransform>();
            if (rectTransform == null) continue;

            if (alwaysActivePanelIndices.Contains(i))
            {
                if (!originalPositions.ContainsKey(i))
                    originalPositions[i] = rectTransform.anchoredPosition;

                panels[i].SetActive(true);
                rectTransform.anchoredPosition = offScreenPosition;
            }
            else
            {
                panels[i].SetActive(false);
            }
        }
    }

    public void ShowPanel(int index)
    {
        if (index < 0 || index >= panels.Count || panels[index] == null)
        {
            Logger.LogWarning($"PanelManager: Invalid panel index: {index}", Logger.LogCategory.General);
            return;
        }

        HidePanel();

        if (alwaysActivePanelIndices.Contains(index))
        {
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
            panels[index].SetActive(true);
        }

        currentPanelIndex = index;
        OnPanelChanged?.Invoke(currentPanelIndex);
    }

    public void NextPanel()
    {
        if (isTransitioning) return; // OPTIMISATION : eviter les appels multiples

        int nextIndex = currentPanelIndex + 1;

        if (nextIndex >= panels.Count)
        {
            if (wrapAround)
                nextIndex = 0;
            else
                return;
        }

        StartTransition(currentPanelIndex, nextIndex, TransitionDirection.Right);
    }

    public void PreviousPanel()
    {
        if (isTransitioning) return; // OPTIMISATION : eviter les appels multiples

        int prevIndex = currentPanelIndex - 1;

        if (prevIndex < 0)
        {
            if (wrapAround)
                prevIndex = panels.Count - 1;
            else
                return;
        }

        StartTransition(currentPanelIndex, prevIndex, TransitionDirection.Left);
    }

    private void HandleInput()
    {
        // Skip swipe handling while dragging items in inventory
        if (DragDropManager.Instance != null && DragDropManager.Instance.IsDragging)
        {
            lastDragEndTime = Time.time;
            return;
        }

        // Block swipes for a short duration after dragging ends
        if (Time.time - lastDragEndTime < DRAG_SWIPE_BLOCK_DURATION)
            return;

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

                    if (swipeTime < swipeThreshold && Mathf.Abs(swipeDelta.x) > minSwipeDistance)
                    {
                        if (swipeDelta.x < 0)
                            NextPanel();
                        else
                            PreviousPanel();
                    }
                    break;
            }
        }

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.RightArrow))
            NextPanel();
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            PreviousPanel();
#endif
    }

    private enum TransitionDirection { Left, Right }

    private void StartTransition(int fromIndex, int toIndex, TransitionDirection direction)
    {
        if (isTransitioning) return;

        isTransitioning = true;

        RectTransform fromRect = panels[fromIndex].GetComponent<RectTransform>();
        RectTransform toRect = panels[toIndex].GetComponent<RectTransform>();

        if (!alwaysActivePanelIndices.Contains(fromIndex))
            panels[fromIndex].SetActive(true);

        if (!alwaysActivePanelIndices.Contains(toIndex))
            panels[toIndex].SetActive(true);

        fromRect.anchoredPosition = Vector2.zero;

        if (direction == TransitionDirection.Right)
            toRect.anchoredPosition = new Vector2(panelContainer.rect.width, 0);
        else
            toRect.anchoredPosition = new Vector2(-panelContainer.rect.width, 0);

        // OPTIMISATION : Utiliser LeanTween au lieu de coroutines pour de meilleures performances
        StartCoroutine(TransitionPanels(fromRect, toRect, direction, toIndex, fromIndex));
    }

    // OPTIMISATION : Version plus efficace de la transition
    private IEnumerator TransitionPanels(RectTransform fromRect, RectTransform toRect,
                                        TransitionDirection direction, int targetIndex, int fromIndex)
    {
        float transitionDuration = 1f / transitionSpeed;
        Vector2 fromStartPos = fromRect.anchoredPosition;
        Vector2 toStartPos = toRect.anchoredPosition;

        Vector2 fromTargetPos = direction == TransitionDirection.Right ?
            new Vector2(-panelContainer.rect.width, 0) : new Vector2(panelContainer.rect.width, 0);
        Vector2 toTargetPos = Vector2.zero;

        float elapsedTime = 0f;

        // OPTIMISATION : Utiliser WaitForEndOfFrame pour de meilleures performances
        while (elapsedTime < transitionDuration)
        {
            float t = transitionCurve.Evaluate(elapsedTime / transitionDuration);

            fromRect.anchoredPosition = Vector2.Lerp(fromStartPos, fromTargetPos, t);
            toRect.anchoredPosition = Vector2.Lerp(toStartPos, toTargetPos, t);

            elapsedTime += Time.deltaTime;
            yield return null; // Ou yield return new WaitForEndOfFrame() pour moins de calls
        }

        // Finaliser la transition
        fromRect.anchoredPosition = fromTargetPos;
        toRect.anchoredPosition = toTargetPos;
        currentPanelIndex = targetIndex;

        if (alwaysActivePanelIndices.Contains(fromIndex))
            fromRect.anchoredPosition = offScreenPosition;
        else
            fromRect.gameObject.SetActive(false);

        isTransitioning = false;
        OnPanelChanged?.Invoke(currentPanelIndex);
    }

    private void HidePanel()
    {
        if (currentPanelIndex >= 0 && currentPanelIndex < panels.Count && panels[currentPanelIndex] != null)
        {
            if (alwaysActivePanelIndices.Contains(currentPanelIndex))
            {
                RectTransform rectTransform = panels[currentPanelIndex].GetComponent<RectTransform>();
                if (rectTransform != null)
                    rectTransform.anchoredPosition = offScreenPosition;
            }
            else
            {
                panels[currentPanelIndex].SetActive(false);
            }
        }
    }

    // === NOUVELLES MeTHODES POUR LA GESTION DE LA CARTE ===

    /// <summary>
    /// Affiche la carte en cachant le panel actuel
    /// </summary>
    public void ShowMap()
    {
        if (!mapIsHidden) return; // Deja visible

        previousPanelIndex = currentPanelIndex;
        HidePanel();
        mapPanel.SetActive(true);
        mapIsHidden = false;

        // OPTIMISATION : Arreter la detection d'input quand la carte est visible
        StopInputDetection();

        // Notifier le changement d'etat
        OnMapStateChanged?.Invoke(true);
    }

    /// <summary>
    /// Cache la carte et affiche le panel specifie par son nom
    /// MODIFIe: Fonctionne maintenant depuis n'importe où (carte ou panel)
    /// </summary>
    /// <param name="panelName">Nom du GameObject panel a afficher</param>
    public void HideMapAndGoToPanel(string panelName)
    {
        int panelIndex = FindPanelIndexByName(panelName);
        if (panelIndex >= 0)
        {
            HideMapAndGoToPanel(panelIndex);
        }
        else
        {
            Logger.LogWarning($"PanelManager: Panel with name '{panelName}' not found!", Logger.LogCategory.General);
        }
    }

    /// <summary>
    /// Cache la carte et affiche le panel specifie par son index
    /// MODIFIe: Fonctionne maintenant depuis n'importe où + corrige les positions
    /// </summary>
    /// <param name="panelIndex">Index du panel a afficher</param>
    public void HideMapAndGoToPanel(int panelIndex)
    {
        if (panelIndex < 0 || panelIndex >= panels.Count || panels[panelIndex] == null)
        {
            Logger.LogWarning($"PanelManager: Invalid panel index for HideMapAndGoToPanel: {panelIndex}", Logger.LogCategory.General);
            return;
        }

        // NOUVEAU: Arreter toute transition en cours et nettoyer les positions
        CleanupTransitionsAndPositions();

        // NOUVEAU: Gerer les deux cas
        if (!mapIsHidden)
        {
            // CAS 1: On vient de la carte → cacher la carte d'abord
            mapPanel.SetActive(false);
            mapIsHidden = true;

            // Notifier le changement d'etat de la carte
            OnMapStateChanged?.Invoke(false);

            // OPTIMISATION : Relancer la detection d'input
            StartInputDetection();
        }
        else
        {
            // CAS 2: On vient d'un panel → cacher le panel actuel d'abord
            HidePanel();
        }

        // Dans tous les cas : afficher le panel demande avec sa position correcte
        ShowPanelAtCorrectPosition(panelIndex);

        currentPanelIndex = panelIndex;

        // Notifier le changement de panel
        OnPanelChanged?.Invoke(currentPanelIndex);

        Logger.LogInfo($"PanelManager: Navigated to panel {panelIndex} (from {(mapIsHidden ? "panel" : "map")})", Logger.LogCategory.General);
    }

    /// <summary>
    /// NOUVEAU: Nettoie toutes les transitions en cours et remet les panels a leur position correcte
    /// </summary>
    private void CleanupTransitionsAndPositions()
    {
        // Arreter toute transition en cours
        if (isTransitioning)
        {
            isTransitioning = false;

            // Arreter la coroutine de transition si elle existe
            StopAllCoroutines();

            Logger.LogInfo("PanelManager: Stopped ongoing transition", Logger.LogCategory.General);
        }

        // Remettre tous les panels a leur position correcte
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] == null) continue;

            RectTransform rectTransform = panels[i].GetComponent<RectTransform>();
            if (rectTransform == null) continue;

            if (alwaysActivePanelIndices.Contains(i))
            {
                // Les panels "always active" : soit a leur position originale, soit hors ecran
                if (i == currentPanelIndex && !isTransitioning)
                {
                    // Panel actuel : position originale
                    if (originalPositions.ContainsKey(i))
                        rectTransform.anchoredPosition = originalPositions[i];
                }
                else
                {
                    // Autres panels : hors ecran
                    rectTransform.anchoredPosition = offScreenPosition;
                }
            }
            else
            {
                // Les panels normaux : position originale s'ils sont actifs
                if (panels[i].activeSelf && originalPositions.ContainsKey(i))
                {
                    rectTransform.anchoredPosition = originalPositions[i];
                }
            }
        }
    }

    /// <summary>
    /// NOUVEAU: Affiche un panel a sa position correcte
    /// </summary>
    private void ShowPanelAtCorrectPosition(int panelIndex)
    {
        if (alwaysActivePanelIndices.Contains(panelIndex))
        {
            RectTransform rectTransform = panels[panelIndex].GetComponent<RectTransform>();
            if (rectTransform != null && originalPositions.ContainsKey(panelIndex))
            {
                rectTransform.anchoredPosition = originalPositions[panelIndex];
            }
        }
        else
        {
            panels[panelIndex].SetActive(true);

            // S'assurer que la position est correcte meme pour les panels normaux
            RectTransform rectTransform = panels[panelIndex].GetComponent<RectTransform>();
            if (rectTransform != null && originalPositions.ContainsKey(panelIndex))
            {
                rectTransform.anchoredPosition = originalPositions[panelIndex];
            }
        }
    }

    /// <summary>
    /// Cache la carte et retourne au panel precedent
    /// </summary>
    public void HideMapAndReturnToPrevious()
    {
        HideMapAndGoToPanel(previousPanelIndex);
    }

    /// <summary>
    /// Trouve l'index d'un panel par son nom
    /// </summary>
    /// <param name="panelName">Nom du GameObject panel a chercher</param>
    /// <returns>Index du panel ou -1 si non trouve</returns>
    private int FindPanelIndexByName(string panelName)
    {
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] != null && panels[i].name == panelName)
            {
                return i;
            }
        }
        return -1; // Not found
    }

    /// <summary>
    /// Toggle la carte (affiche si cachee, cache et retourne au precedent si visible)
    /// Methode de compatibilite avec l'ancien code
    /// </summary>
    public void ShowAndHideMapPanel()
    {
        if (mapIsHidden)
        {
            ShowMap();
        }
        else
        {
            HideMapAndReturnToPrevious();
        }
    }

    /// <summary>
    /// Navigate to a panel with slide animation (like swiping)
    /// </summary>
    /// <param name="panelName">Name of the panel to navigate to</param>
    public void GoToPanelAnimated(string panelName)
    {
        int panelIndex = FindPanelIndexByName(panelName);
        if (panelIndex >= 0)
        {
            GoToPanelAnimated(panelIndex);
        }
        else
        {
            Logger.LogWarning($"PanelManager: Panel with name '{panelName}' not found!", Logger.LogCategory.General);
        }
    }

    /// <summary>
    /// Navigate to a panel with slide animation (like swiping)
    /// </summary>
    /// <param name="panelIndex">Index of the panel to navigate to</param>
    public void GoToPanelAnimated(int panelIndex)
    {
        if (panelIndex < 0 || panelIndex >= panels.Count || panels[panelIndex] == null)
        {
            Logger.LogWarning($"PanelManager: Invalid panel index for GoToPanelAnimated: {panelIndex}", Logger.LogCategory.General);
            return;
        }

        // If transitioning, skip
        if (isTransitioning)
        {
            Logger.LogWarning("PanelManager: Already transitioning, skipping GoToPanelAnimated", Logger.LogCategory.General);
            return;
        }

        // If we're on the map, use slide-in only animation (no "from" panel to slide out)
        if (!mapIsHidden)
        {
            mapPanel.SetActive(false);
            mapIsHidden = true;
            OnMapStateChanged?.Invoke(false);
            StartInputDetection();

            // Slide in the target panel from the right
            StartSlideInTransition(panelIndex);
            Logger.LogInfo($"PanelManager: Slide-in transition from map to panel {panelIndex}", Logger.LogCategory.General);
            return;
        }

        // If already on target panel, do nothing
        if (panelIndex == currentPanelIndex)
        {
            Logger.LogInfo($"PanelManager: Already on panel {panelIndex}", Logger.LogCategory.General);
            return;
        }

        // Determine direction based on panel indices
        TransitionDirection direction = panelIndex > currentPanelIndex ? TransitionDirection.Right : TransitionDirection.Left;

        // Start the animated transition
        StartTransition(currentPanelIndex, panelIndex, direction);

        Logger.LogInfo($"PanelManager: Animated transition from {currentPanelIndex} to {panelIndex}", Logger.LogCategory.General);
    }

    /// <summary>
    /// Slide in a single panel (used when coming from map)
    /// </summary>
    private void StartSlideInTransition(int panelIndex)
    {
        isTransitioning = true;

        RectTransform toRect = panels[panelIndex].GetComponent<RectTransform>();

        // Activate the panel
        if (!alwaysActivePanelIndices.Contains(panelIndex))
            panels[panelIndex].SetActive(true);

        // Start off-screen to the right
        toRect.anchoredPosition = new Vector2(panelContainer.rect.width, 0);

        // Animate to center
        StartCoroutine(SlideInPanel(toRect, panelIndex));
    }

    /// <summary>
    /// Coroutine to slide in a single panel
    /// </summary>
    private IEnumerator SlideInPanel(RectTransform toRect, int targetIndex)
    {
        float transitionDuration = 1f / transitionSpeed;
        Vector2 startPos = toRect.anchoredPosition;
        Vector2 targetPos = Vector2.zero;

        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            float t = transitionCurve.Evaluate(elapsedTime / transitionDuration);
            toRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Finalize
        toRect.anchoredPosition = targetPos;
        currentPanelIndex = targetIndex;
        isTransitioning = false;
        OnPanelChanged?.Invoke(currentPanelIndex);
    }

    // OPTIMISATION : Nettoyer les coroutines a la destruction
    private void OnDestroy()
    {
        StopInputDetection();
    }

    // Public getters
    public int CurrentPanelIndex => currentPanelIndex;
    public int PanelCount => panels.Count;

    // Autres methodes restent identiques...
    public void AddAlwaysActivePanel(int panelIndex)
    {
        if (panelIndex >= 0 && panelIndex < panels.Count && !alwaysActivePanelIndices.Contains(panelIndex))
        {
            alwaysActivePanelIndices.Add(panelIndex);

            if (!panels[panelIndex].activeSelf)
            {
                panels[panelIndex].SetActive(true);
                RectTransform rectTransform = panels[panelIndex].GetComponent<RectTransform>();
                if (rectTransform != null)
                    rectTransform.anchoredPosition = offScreenPosition;
            }
        }
    }
}