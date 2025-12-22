// Purpose: Service for selecting the best dialogue for an NPC
// Filepath: Assets/Scripts/Gameplay/Dialogue/Services/DialogueSelectionService.cs
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Selects the best dialogue for an NPC based on conditions and priority.
/// Higher priority dialogues are selected first when multiple dialogues match.
/// </summary>
public class DialogueSelectionService
{
    private readonly DialogueConditionService _conditionService;
    private readonly bool _enableDebugLogs;

    public DialogueSelectionService(DialogueConditionService conditionService, bool enableDebugLogs = false)
    {
        _conditionService = conditionService;
        _enableDebugLogs = enableDebugLogs;
    }

    /// <summary>
    /// Select the best matching dialogue for an NPC.
    /// Filters by conditions met, then picks highest priority.
    /// </summary>
    public DialogueDefinition SelectDialogue(NPCDefinition npc)
    {
        if (npc == null)
        {
            Logger.LogWarning("DialogueSelectionService: NPC is null", Logger.LogCategory.General);
            return null;
        }

        if (npc.Dialogues == null || npc.Dialogues.Count == 0)
        {
            if (_enableDebugLogs)
                Logger.LogInfo($"DialogueSelectionService: No dialogues for NPC '{npc.NPCID}'", Logger.LogCategory.General);
            return null;
        }

        DialogueDefinition bestDialogue = null;
        int highestPriority = int.MinValue;

        foreach (var dialogue in npc.Dialogues)
        {
            // Skip null or invalid dialogues
            if (dialogue == null || !dialogue.IsValid())
            {
                if (_enableDebugLogs)
                    Logger.LogInfo($"DialogueSelectionService: Skipping invalid dialogue", Logger.LogCategory.General);
                continue;
            }

            // Check if conditions are met
            if (!_conditionService.AreConditionsMet(dialogue))
            {
                if (_enableDebugLogs)
                    Logger.LogInfo($"DialogueSelectionService: Conditions not met for '{dialogue.DialogueID}'", Logger.LogCategory.General);
                continue;
            }

            // Check priority
            if (dialogue.Priority > highestPriority)
            {
                highestPriority = dialogue.Priority;
                bestDialogue = dialogue;

                if (_enableDebugLogs)
                    Logger.LogInfo($"DialogueSelectionService: New best dialogue '{dialogue.DialogueID}' (priority {dialogue.Priority})", Logger.LogCategory.General);
            }
        }

        if (bestDialogue != null && _enableDebugLogs)
        {
            Logger.LogInfo($"DialogueSelectionService: Selected '{bestDialogue.DialogueID}' for NPC '{npc.NPCID}'", Logger.LogCategory.General);
        }
        else if (bestDialogue == null && _enableDebugLogs)
        {
            Logger.LogInfo($"DialogueSelectionService: No valid dialogue found for NPC '{npc.NPCID}'", Logger.LogCategory.General);
        }

        return bestDialogue;
    }

    /// <summary>
    /// Get all available dialogues for an NPC (conditions met).
    /// Sorted by priority (highest first).
    /// </summary>
    public List<DialogueDefinition> GetAvailableDialogues(NPCDefinition npc)
    {
        var available = new List<DialogueDefinition>();

        if (npc?.Dialogues == null)
            return available;

        foreach (var dialogue in npc.Dialogues)
        {
            if (dialogue != null && dialogue.IsValid() && _conditionService.AreConditionsMet(dialogue))
            {
                available.Add(dialogue);
            }
        }

        // Sort by priority (highest first)
        return available.OrderByDescending(d => d.Priority).ToList();
    }

    /// <summary>
    /// Get all dialogues for an NPC with their availability status.
    /// Useful for debugging or showing locked dialogues.
    /// </summary>
    public List<DialogueAvailability> GetDialogueAvailability(NPCDefinition npc)
    {
        var result = new List<DialogueAvailability>();

        if (npc?.Dialogues == null)
            return result;

        foreach (var dialogue in npc.Dialogues)
        {
            if (dialogue == null || !dialogue.IsValid())
                continue;

            bool isAvailable = _conditionService.AreConditionsMet(dialogue);
            var unmetConditions = isAvailable ? new List<string>() : _conditionService.GetUnmetConditions(dialogue);

            result.Add(new DialogueAvailability
            {
                Dialogue = dialogue,
                IsAvailable = isAvailable,
                UnmetConditions = unmetConditions
            });
        }

        // Sort by availability (available first), then by priority
        return result
            .OrderByDescending(d => d.IsAvailable)
            .ThenByDescending(d => d.Dialogue.Priority)
            .ToList();
    }

    /// <summary>
    /// Check if an NPC has any available dialogue
    /// </summary>
    public bool HasAvailableDialogue(NPCDefinition npc)
    {
        return SelectDialogue(npc) != null;
    }
}

/// <summary>
/// Represents a dialogue's availability status
/// </summary>
public class DialogueAvailability
{
    public DialogueDefinition Dialogue { get; set; }
    public bool IsAvailable { get; set; }
    public List<string> UnmetConditions { get; set; }
}
