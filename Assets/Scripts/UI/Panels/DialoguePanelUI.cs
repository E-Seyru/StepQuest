// Purpose: UI panel for displaying dialogues with typewriter effect
// Filepath: Assets/Scripts/UI/Panels/DialoguePanelUI.cs
using DialogueEvents;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI panel for displaying dialogues with typewriter effect.
/// Handles text display, choices, and user input for dialogue progression.
/// </summary>
public class DialoguePanelUI : MonoBehaviour
{
    public static DialoguePanelUI Instance { get; private set; }

    [Header("Panel References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;

    [Header("NPC Display")]
    [SerializeField] private Image npcImage;
    [SerializeField] private GameObject npcContainer;

    [Header("Dialogue Display")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject continueIndicator;  // Arrow showing ready to continue
    [SerializeField] private GameObject endDialogueIndicator;  // "Click to leave" indicator

    [Header("Choices")]
    [SerializeField] private GameObject choicesContainer;
    [SerializeField] private GameObject choiceButtonPrefab;
    [SerializeField] private int maxChoiceButtons = 4;

    [Header("Typewriter Settings")]
    [SerializeField] private float charactersPerSecond = 40f;
    [SerializeField] private AudioClip typewriterSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private int soundPlayEveryNChars = 3;  // Play sound every N characters

    [Header("Click Area")]
    [SerializeField] private Button clickArea;  // Covers the panel for tap detection

    [Header("Background")]
    [SerializeField] private Image locationBackground;  // Optional location background

    // Runtime state
    private NPCDefinition _currentNPC;
    private DialogueLine _currentLine;
    private Coroutine _typewriterCoroutine;
    private bool _isTypewriterActive;
    private bool _isFullTextDisplayed;
    private bool _isDialogueEnding;
    private List<GameObject> _choiceButtons = new List<GameObject>();
    private int _charsSinceLastSound;

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

        SetupUI();
        SubscribeToEvents();
        HidePanel();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
        if (Instance == this)
            Instance = null;
    }

    // === SETUP ===

    private void SetupUI()
    {
        // Setup click area for advancing dialogue
        if (clickArea != null)
        {
            clickArea.onClick.AddListener(OnPanelClicked);
        }

        // Pre-create choice buttons
        if (choiceButtonPrefab != null && choicesContainer != null)
        {
            for (int i = 0; i < maxChoiceButtons; i++)
            {
                var button = Instantiate(choiceButtonPrefab, choicesContainer.transform);
                button.SetActive(false);
                _choiceButtons.Add(button);
            }
        }

        // Hide indicators initially
        if (continueIndicator != null)
            continueIndicator.SetActive(false);
        if (endDialogueIndicator != null)
            endDialogueIndicator.SetActive(false);
    }

    private void SubscribeToEvents()
    {
        EventBus.Subscribe<DialogueStartedEvent>(OnDialogueStarted);
        EventBus.Subscribe<DialogueLineAdvancedEvent>(OnLineAdvanced);
        EventBus.Subscribe<DialogueEndedEvent>(OnDialogueEnded);
    }

    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<DialogueStartedEvent>(OnDialogueStarted);
        EventBus.Unsubscribe<DialogueLineAdvancedEvent>(OnLineAdvanced);
        EventBus.Unsubscribe<DialogueEndedEvent>(OnDialogueEnded);
    }

    // === EVENT HANDLERS ===

    private void OnDialogueStarted(DialogueStartedEvent eventData)
    {
        _currentNPC = eventData.NPC;
        _isDialogueEnding = false;

        // Setup NPC display with default dialogue sprite
        if (npcImage != null && _currentNPC != null)
        {
            npcImage.sprite = _currentNPC.GetDialogueSprite();
            if (npcContainer != null)
                npcContainer.SetActive(npcImage.sprite != null);
        }

        // Setup location background from current location
        UpdateLocationBackground();

        ShowPanel();
    }

    private void UpdateLocationBackground()
    {
        if (locationBackground == null) return;

        // Get current location from MapManager
        var currentLocation = MapManager.Instance?.CurrentLocation;
        if (currentLocation != null && currentLocation.LocationImage != null)
        {
            locationBackground.sprite = currentLocation.LocationImage;
            locationBackground.gameObject.SetActive(true);
        }
        else
        {
            // Hide background if no location image available
            locationBackground.gameObject.SetActive(false);
        }
    }

    private void OnLineAdvanced(DialogueLineAdvancedEvent eventData)
    {
        _currentLine = eventData.Line;
        _isDialogueEnding = false;
        DisplayLine(_currentLine);
    }

    private void OnDialogueEnded(DialogueEndedEvent eventData)
    {
        HidePanel();
        _currentNPC = null;
        _currentLine = null;
        _isDialogueEnding = false;
    }

    // === DISPLAY METHODS ===

    private void DisplayLine(DialogueLine line)
    {
        if (line == null) return;

        // Update speaker name
        UpdateSpeakerDisplay(line.Speaker);

        // Update NPC emotion sprite
        UpdateEmotionSprite(line.Emotion);

        // Hide all indicators and choices
        HideChoices();
        HideIndicators();

        // Start typewriter effect
        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        _typewriterCoroutine = StartCoroutine(TypewriterEffect(line.Text, line.HasChoices));
    }

    private void UpdateEmotionSprite(NPCEmotion emotion)
    {
        if (npcImage == null || _currentNPC == null) return;

        Sprite emotionSprite = _currentNPC.GetEmotionSprite(emotion);
        if (emotionSprite != null)
        {
            npcImage.sprite = emotionSprite;
        }
    }

    private void UpdateSpeakerDisplay(string speaker)
    {
        if (speakerNameText == null) return;

        // Check if speaker is player
        bool isPlayer = string.IsNullOrEmpty(speaker) ||
                        speaker.Equals("Player", System.StringComparison.OrdinalIgnoreCase) ||
                        speaker.Equals("Joueur", System.StringComparison.OrdinalIgnoreCase);

        if (isPlayer)
        {
            speakerNameText.text = "Vous";
        }
        else
        {
            speakerNameText.text = speaker;
        }
    }

    private IEnumerator TypewriterEffect(string fullText, bool hasChoices)
    {
        _isTypewriterActive = true;
        _isFullTextDisplayed = false;
        _charsSinceLastSound = 0;

        if (dialogueText == null)
        {
            _isTypewriterActive = false;
            _isFullTextDisplayed = true;
            OnTypewriterComplete(hasChoices);
            yield break;
        }

        dialogueText.text = "";

        if (string.IsNullOrEmpty(fullText))
        {
            _isTypewriterActive = false;
            _isFullTextDisplayed = true;
            OnTypewriterComplete(hasChoices);
            yield break;
        }

        float delay = 1f / charactersPerSecond;

        for (int i = 0; i < fullText.Length; i++)
        {
            dialogueText.text = fullText.Substring(0, i + 1);

            // Play sound periodically (not for every character, not for spaces)
            char currentChar = fullText[i];
            if (currentChar != ' ' && currentChar != '\n')
            {
                _charsSinceLastSound++;
                if (_charsSinceLastSound >= soundPlayEveryNChars)
                {
                    PlayTypewriterSound();
                    _charsSinceLastSound = 0;
                }
            }

            yield return new WaitForSeconds(delay);
        }

        _isTypewriterActive = false;
        _isFullTextDisplayed = true;

        OnTypewriterComplete(hasChoices);
    }

    private void SkipTypewriter()
    {
        if (!_isTypewriterActive) return;

        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        // Show full text immediately
        if (dialogueText != null && _currentLine != null)
            dialogueText.text = _currentLine.Text;

        _isTypewriterActive = false;
        _isFullTextDisplayed = true;

        OnTypewriterComplete(_currentLine?.HasChoices ?? false);
    }

    private void OnTypewriterComplete(bool hasChoices)
    {
        if (hasChoices)
        {
            // Show choice buttons
            ShowChoices();
        }
        else
        {
            // Check if this is the last line
            bool isLastLine = DialogueManager.Instance?.IsLastLine ?? false;

            if (isLastLine)
            {
                // Show "click to leave" indicator
                if (endDialogueIndicator != null)
                    endDialogueIndicator.SetActive(true);
                _isDialogueEnding = true;
            }
            else
            {
                // Show continue indicator (arrow)
                if (continueIndicator != null)
                    continueIndicator.SetActive(true);
            }
        }
    }

    private void PlayTypewriterSound()
    {
        if (typewriterSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);  // Slight pitch variation
            audioSource.PlayOneShot(typewriterSound, 0.5f);
        }
    }

    private void HideIndicators()
    {
        if (continueIndicator != null)
            continueIndicator.SetActive(false);
        if (endDialogueIndicator != null)
            endDialogueIndicator.SetActive(false);
    }

    // === INPUT HANDLING ===

    private void OnPanelClicked()
    {
        if (_isTypewriterActive)
        {
            // First tap while typewriter is running: skip to full text
            SkipTypewriter();
        }
        else if (_isFullTextDisplayed)
        {
            // Text is fully displayed

            if (_currentLine != null && _currentLine.HasChoices)
            {
                // Has choices - do nothing, wait for choice button click
                return;
            }

            if (_isDialogueEnding)
            {
                // This is the end - close dialogue
                DialogueManager.Instance?.EndDialogue(true);
            }
            else
            {
                // Advance to next line
                DialogueManager.Instance?.AdvanceLine();
            }
        }
    }

    // === CHOICES ===

    private void ShowChoices()
    {
        if (choicesContainer == null || _currentLine?.Choices == null)
            return;

        choicesContainer.SetActive(true);

        for (int i = 0; i < _choiceButtons.Count; i++)
        {
            if (i < _currentLine.Choices.Count)
            {
                var choice = _currentLine.Choices[i];
                var buttonGO = _choiceButtons[i];
                buttonGO.SetActive(true);

                // Setup button text
                var buttonText = buttonGO.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                    buttonText.text = choice.ChoiceText;

                // Setup button click
                var button = buttonGO.GetComponent<Button>();
                if (button != null)
                {
                    int choiceIndex = i;  // Capture for closure
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => OnChoiceSelected(choiceIndex));
                }
            }
            else
            {
                _choiceButtons[i].SetActive(false);
            }
        }
    }

    private void HideChoices()
    {
        if (choicesContainer != null)
            choicesContainer.SetActive(false);

        foreach (var button in _choiceButtons)
        {
            if (button != null)
                button.SetActive(false);
        }
    }

    private void OnChoiceSelected(int choiceIndex)
    {
        HideChoices();
        DialogueManager.Instance?.MakeChoice(choiceIndex);
    }

    // === PANEL VISIBILITY ===

    private void ShowPanel()
    {
        Debug.Log($"DialoguePanelUI: ShowPanel called. dialoguePanel is {(dialoguePanel != null ? "assigned" : "NULL")}");

        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);
        else
            Debug.LogError("DialoguePanelUI: dialoguePanel reference is not assigned in Inspector!");

        gameObject.SetActive(true);

        // Fade in if canvas group exists
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
            panelCanvasGroup.interactable = true;
            panelCanvasGroup.blocksRaycasts = true;
        }
    }

    private void HidePanel()
    {
        // Stop any running coroutine
        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        HideChoices();
        HideIndicators();

        // Clear text
        if (dialogueText != null)
            dialogueText.text = "";
        if (speakerNameText != null)
            speakerNameText.text = "";

        // Fade out if canvas group exists
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
            panelCanvasGroup.interactable = false;
            panelCanvasGroup.blocksRaycasts = false;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    /// <summary>
    /// Check if dialogue panel is currently visible
    /// </summary>
    public bool IsPanelVisible()
    {
        return dialoguePanel != null && dialoguePanel.activeInHierarchy;
    }

    /// <summary>
    /// Set the location background image
    /// </summary>
    public void SetLocationBackground(Sprite background)
    {
        if (locationBackground != null)
        {
            locationBackground.sprite = background;
            locationBackground.gameObject.SetActive(background != null);
        }
    }
}
