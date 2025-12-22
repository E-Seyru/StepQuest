// Purpose: ScriptableObject defining a complete dialogue/conversation
// Filepath: Assets/Scripts/Data/ScriptableObjects/Dialogue/DialogueDefinition.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject defining a complete dialogue/conversation with an NPC.
/// Each dialogue has conditions that must be met and a priority for selection.
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "WalkAndRPG/Social/Dialogue Definition")]
public class DialogueDefinition : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Unique identifier for this dialogue")]
    public string DialogueID;

    [Tooltip("Display name for editor reference")]
    public string DialogueName;

    [Header("Priority & Conditions")]
    [Tooltip("Higher priority dialogues are selected first when multiple match (default: 0)")]
    public int Priority = 0;

    [Tooltip("All conditions must be met for this dialogue to be available")]
    public List<DialogueCondition> Conditions = new List<DialogueCondition>();

    [Header("Content")]
    [Tooltip("The dialogue lines in order")]
    public List<DialogueLine> Lines = new List<DialogueLine>();

    [Header("Completion Effects")]
    [Tooltip("Flags to set when this dialogue is completed (useful for one-time dialogues)")]
    public List<string> FlagsToSetOnCompletion = new List<string>();

    [Header("Completion Rewards")]
    [Tooltip("Ability ID to grant when this dialogue is completed (leave empty for no ability)")]
    public string AbilityToGrantOnCompletion;

    [Tooltip("Items to grant when this dialogue is completed")]
    public List<DialogueItemReward> ItemsToGrantOnCompletion = new List<DialogueItemReward>();

    [Header("Debug")]
    [TextArea(1, 2)]
    public string DeveloperNotes;

    /// <summary>
    /// Check if this dialogue definition is valid
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(DialogueID)) return false;
        if (Lines == null || Lines.Count == 0) return false;
        return true;
    }

    /// <summary>
    /// Get display name for UI/editor
    /// </summary>
    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(DialogueName) ? DialogueID : DialogueName;
    }

    /// <summary>
    /// Get total line count
    /// </summary>
    public int LineCount => Lines?.Count ?? 0;

    /// <summary>
    /// Get a line by index (safe)
    /// </summary>
    public DialogueLine GetLine(int index)
    {
        if (Lines == null || index < 0 || index >= Lines.Count)
            return null;
        return Lines[index];
    }

    /// <summary>
    /// Check if dialogue has any conditions
    /// </summary>
    public bool HasConditions => Conditions != null && Conditions.Count > 0;

    /// <summary>
    /// Check if dialogue grants any rewards on completion
    /// </summary>
    public bool HasCompletionRewards => !string.IsNullOrEmpty(AbilityToGrantOnCompletion) ||
                                        (ItemsToGrantOnCompletion != null && ItemsToGrantOnCompletion.Count > 0);

    /// <summary>
    /// Get a summary of conditions for display
    /// </summary>
    public string GetConditionsSummary()
    {
        if (!HasConditions) return "No conditions";

        var summary = new System.Text.StringBuilder();
        foreach (var condition in Conditions)
        {
            if (summary.Length > 0) summary.Append(", ");
            summary.Append(condition.ToString());
        }
        return summary.ToString();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-generate DialogueID from name if empty
        if (string.IsNullOrEmpty(DialogueID) && !string.IsNullOrEmpty(DialogueName))
        {
            DialogueID = DialogueName.ToLower().Replace(" ", "_").Replace("'", "");
        }

        // Ensure lists are initialized
        if (Conditions == null) Conditions = new List<DialogueCondition>();
        if (Lines == null) Lines = new List<DialogueLine>();
        if (FlagsToSetOnCompletion == null) FlagsToSetOnCompletion = new List<string>();
        if (ItemsToGrantOnCompletion == null) ItemsToGrantOnCompletion = new List<DialogueItemReward>();
    }
#endif
}
