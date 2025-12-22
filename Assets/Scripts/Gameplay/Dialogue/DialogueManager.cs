// Purpose: Manages dialogue flow, selection, and state
// Filepath: Assets/Scripts/Gameplay/Dialogue/DialogueManager.cs
using DialogueEvents;
using System;
using UnityEngine;

/// <summary>
/// Manages dialogue flow, selection, and state.
/// Singleton pattern following existing manager conventions.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = false;

    // === SERVICES ===
    private DialogueConditionService _conditionService;
    private DialogueSelectionService _selectionService;

    // === RUNTIME STATE ===
    private NPCDefinition _currentNPC;
    private DialogueDefinition _currentDialogue;
    private int _currentLineIndex;
    private bool _isDialogueActive;

    // === PUBLIC ACCESSORS ===
    public bool IsDialogueActive => _isDialogueActive;
    public NPCDefinition CurrentNPC => _currentNPC;
    public DialogueDefinition CurrentDialogue => _currentDialogue;
    public int CurrentLineIndex => _currentLineIndex;
    public DialogueLine CurrentLine => GetCurrentLine();
    public bool IsLastLine => _currentDialogue != null && _currentLineIndex >= _currentDialogue.LineCount - 1;

    // === C# EVENTS (for direct subscription) ===
    public event Action<DialogueLine> OnLineChanged;
    public event Action OnDialogueEnded;

    // === UNITY LIFECYCLE ===

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeServices();
            Logger.LogInfo("DialogueManager: Initialized", Logger.LogCategory.General);
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
    }

    private void InitializeServices()
    {
        _conditionService = new DialogueConditionService(enableDebugLogs);
        _selectionService = new DialogueSelectionService(_conditionService, enableDebugLogs);
    }

    // === PUBLIC API ===

    /// <summary>
    /// Start dialogue with an NPC (selects best matching dialogue based on conditions)
    /// </summary>
    /// <returns>True if dialogue started successfully</returns>
    public bool StartDialogue(NPCDefinition npc)
    {
        if (npc == null)
        {
            Logger.LogWarning("DialogueManager: Cannot start dialogue with null NPC", Logger.LogCategory.General);
            return false;
        }

        if (_isDialogueActive)
        {
            Logger.LogWarning("DialogueManager: Cannot start new dialogue while one is active", Logger.LogCategory.General);
            return false;
        }

        // Select best dialogue based on conditions
        DialogueDefinition selectedDialogue = _selectionService.SelectDialogue(npc);

        if (selectedDialogue == null)
        {
            if (enableDebugLogs)
                Logger.LogInfo($"DialogueManager: No valid dialogue for NPC '{npc.NPCID}'", Logger.LogCategory.General);
            return false;
        }

        return StartDialogueInternal(npc, selectedDialogue);
    }

    /// <summary>
    /// Start a specific dialogue (for testing or forced dialogues)
    /// </summary>
    /// <returns>True if dialogue started successfully</returns>
    public bool StartDialogue(NPCDefinition npc, DialogueDefinition dialogue)
    {
        if (npc == null || dialogue == null)
        {
            Logger.LogWarning("DialogueManager: Cannot start dialogue with null NPC or dialogue", Logger.LogCategory.General);
            return false;
        }

        if (_isDialogueActive)
        {
            Logger.LogWarning("DialogueManager: Cannot start new dialogue while one is active", Logger.LogCategory.General);
            return false;
        }

        return StartDialogueInternal(npc, dialogue);
    }

    /// <summary>
    /// Advance to the next line or end dialogue.
    /// Does nothing if current line has choices (must use MakeChoice instead).
    /// </summary>
    public void AdvanceLine()
    {
        if (!_isDialogueActive)
        {
            if (enableDebugLogs)
                Logger.LogInfo("DialogueManager: No active dialogue to advance", Logger.LogCategory.General);
            return;
        }

        // Don't advance if current line has choices (must use MakeChoice)
        if (CurrentLine?.HasChoices == true)
        {
            if (enableDebugLogs)
                Logger.LogInfo("DialogueManager: Cannot advance - current line has choices", Logger.LogCategory.General);
            return;
        }

        _currentLineIndex++;

        if (_currentLineIndex >= _currentDialogue.LineCount)
        {
            // End of dialogue
            EndDialogue(true);
        }
        else
        {
            // Publish line advanced event
            PublishLineAdvanced();
        }
    }

    /// <summary>
    /// Make a choice (for lines with choices).
    /// Applies choice effects (flags, relationship changes) and navigates accordingly.
    /// </summary>
    public void MakeChoice(int choiceIndex)
    {
        if (!_isDialogueActive)
        {
            Logger.LogWarning("DialogueManager: No active dialogue for choice", Logger.LogCategory.General);
            return;
        }

        if (CurrentLine?.Choices == null || CurrentLine.Choices.Count == 0)
        {
            Logger.LogWarning("DialogueManager: Current line has no choices", Logger.LogCategory.General);
            return;
        }

        if (choiceIndex < 0 || choiceIndex >= CurrentLine.Choices.Count)
        {
            Logger.LogWarning($"DialogueManager: Invalid choice index {choiceIndex}", Logger.LogCategory.General);
            return;
        }

        DialogueChoice choice = CurrentLine.Choices[choiceIndex];

        // Apply choice effects
        ApplyChoiceEffects(choice);

        // Publish event
        EventBus.Publish(new DialogueChoiceMadeEvent(_currentNPC.NPCID, choiceIndex, choice));

        if (enableDebugLogs)
            Logger.LogInfo($"DialogueManager: Choice {choiceIndex} made: '{choice.ChoiceText}'", Logger.LogCategory.General);

        // Navigate to next line
        if (choice.NextLineIndex >= 0 && choice.NextLineIndex < _currentDialogue.LineCount)
        {
            // Jump to specific line
            _currentLineIndex = choice.NextLineIndex;
        }
        else
        {
            // Continue to next line
            _currentLineIndex++;
        }

        if (_currentLineIndex >= _currentDialogue.LineCount)
        {
            EndDialogue(true);
        }
        else
        {
            PublishLineAdvanced();
        }
    }

    /// <summary>
    /// End the current dialogue.
    /// Sets completion flags if dialogue completed naturally.
    /// </summary>
    /// <param name="completedNaturally">True if ended by reaching the end, false if cancelled</param>
    public void EndDialogue(bool completedNaturally = false)
    {
        if (!_isDialogueActive)
        {
            if (enableDebugLogs)
                Logger.LogInfo("DialogueManager: No active dialogue to end", Logger.LogCategory.General);
            return;
        }

        // Set completion flags if completed naturally
        if (completedNaturally && _currentDialogue.FlagsToSetOnCompletion != null)
        {
            foreach (var flag in _currentDialogue.FlagsToSetOnCompletion)
            {
                if (!string.IsNullOrEmpty(flag))
                {
                    DataManager.Instance?.PlayerData?.SetDialogueFlag(flag, true);
                    if (enableDebugLogs)
                        Logger.LogInfo($"DialogueManager: Set completion flag '{flag}'", Logger.LogCategory.General);
                }
            }
        }

        // Publish end event
        EventBus.Publish(new DialogueEndedEvent(_currentNPC.NPCID, _currentDialogue.DialogueID, completedNaturally));

        // Invoke C# event
        OnDialogueEnded?.Invoke();

        if (enableDebugLogs)
        {
            var status = completedNaturally ? "completed" : "cancelled";
            Logger.LogInfo($"DialogueManager: Dialogue {status} with {_currentNPC.GetDisplayName()}", Logger.LogCategory.General);
        }

        // Save data
        DataManager.Instance?.SaveGame();

        // Clear state
        _currentNPC = null;
        _currentDialogue = null;
        _currentLineIndex = 0;
        _isDialogueActive = false;
    }

    /// <summary>
    /// Check if an NPC has any available dialogue
    /// </summary>
    public bool HasDialogueAvailable(NPCDefinition npc)
    {
        return _selectionService.HasAvailableDialogue(npc);
    }

    // === PRIVATE METHODS ===

    private bool StartDialogueInternal(NPCDefinition npc, DialogueDefinition dialogue)
    {
        _currentNPC = npc;
        _currentDialogue = dialogue;
        _currentLineIndex = 0;
        _isDialogueActive = true;

        Debug.Log($"DialogueManager: Starting dialogue '{dialogue.DialogueID}' with {dialogue.LineCount} lines");
        Debug.Log($"DialogueManager: DialoguePanelUI.Instance is {(DialoguePanelUI.Instance != null ? "available" : "NULL")}");

        // Publish start event
        EventBus.Publish(new DialogueStartedEvent(npc.NPCID, dialogue.DialogueID, npc, dialogue));

        if (enableDebugLogs)
            Logger.LogInfo($"DialogueManager: Started dialogue '{dialogue.DialogueID}' with {npc.GetDisplayName()}", Logger.LogCategory.General);

        // Publish first line
        PublishLineAdvanced();

        return true;
    }

    private void PublishLineAdvanced()
    {
        var line = GetCurrentLine();

        // Publish EventBus event
        EventBus.Publish(new DialogueLineAdvancedEvent(_currentNPC.NPCID, _currentLineIndex, line));

        // Invoke C# event
        OnLineChanged?.Invoke(line);

        if (enableDebugLogs)
        {
            var choicesInfo = line?.HasChoices == true ? $" ({line.Choices.Count} choices)" : "";
            Logger.LogInfo($"DialogueManager: Line {_currentLineIndex}: '{line?.Text?.Substring(0, Math.Min(50, line?.Text?.Length ?? 0))}...'{choicesInfo}", Logger.LogCategory.General);
        }
    }

    private DialogueLine GetCurrentLine()
    {
        if (!_isDialogueActive || _currentDialogue?.Lines == null)
            return null;

        if (_currentLineIndex < 0 || _currentLineIndex >= _currentDialogue.Lines.Count)
            return null;

        return _currentDialogue.Lines[_currentLineIndex];
    }

    private void ApplyChoiceEffects(DialogueChoice choice)
    {
        if (choice == null) return;

        var playerData = DataManager.Instance?.PlayerData;
        if (playerData == null) return;

        // Set flag if specified
        if (!string.IsNullOrEmpty(choice.FlagToSet))
        {
            playerData.SetDialogueFlag(choice.FlagToSet, true);
            if (enableDebugLogs)
                Logger.LogInfo($"DialogueManager: Set flag '{choice.FlagToSet}'", Logger.LogCategory.General);
        }

        // Apply relationship change if specified
        if (choice.RelationshipChange != 0)
        {
            string npcId = string.IsNullOrEmpty(choice.RelationshipNPCId) ? _currentNPC.NPCID : choice.RelationshipNPCId;
            playerData.ModifyNPCRelationship(npcId, choice.RelationshipChange);
            if (enableDebugLogs)
                Logger.LogInfo($"DialogueManager: Modified relationship with '{npcId}' by {choice.RelationshipChange:+#;-#;0}", Logger.LogCategory.General);
        }
    }
}
