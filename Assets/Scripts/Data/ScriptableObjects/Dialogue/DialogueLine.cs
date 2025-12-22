// Purpose: Data structures for dialogue lines and choices
// Filepath: Assets/Scripts/Data/ScriptableObjects/Dialogue/DialogueLine.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A single line of dialogue in a conversation
/// </summary>
[Serializable]
public class DialogueLine
{
    [Tooltip("Speaker name - NPC name or 'Player' for player lines")]
    public string Speaker;

    [TextArea(2, 5)]
    [Tooltip("The dialogue text to display")]
    public string Text;

    [Tooltip("Emotion/expression for the NPC during this line")]
    public NPCEmotion Emotion = NPCEmotion.Neutral;

    [Tooltip("Optional choices at this line (if empty, dialogue continues normally)")]
    public List<DialogueChoice> Choices = new List<DialogueChoice>();

    /// <summary>
    /// Does this line have choices for the player?
    /// </summary>
    public bool HasChoices => Choices != null && Choices.Count > 0;

    /// <summary>
    /// Create a simple dialogue line without choices
    /// </summary>
    public static DialogueLine Create(string speaker, string text)
    {
        return new DialogueLine
        {
            Speaker = speaker,
            Text = text,
            Choices = new List<DialogueChoice>()
        };
    }

    /// <summary>
    /// Create a dialogue line with choices
    /// </summary>
    public static DialogueLine CreateWithChoices(string speaker, string text, params DialogueChoice[] choices)
    {
        return new DialogueLine
        {
            Speaker = speaker,
            Text = text,
            Choices = new List<DialogueChoice>(choices)
        };
    }
}

/// <summary>
/// A choice the player can make during dialogue
/// </summary>
[Serializable]
public class DialogueChoice
{
    [Tooltip("Text displayed on the choice button")]
    public string ChoiceText;

    [Header("Effects")]
    [Tooltip("Optional flag to set when this choice is selected (leave empty for no flag)")]
    public string FlagToSet;

    [Tooltip("Relationship change when this choice is selected (can be negative)")]
    public int RelationshipChange = 0;

    [Tooltip("NPC ID for relationship change (leave empty to use current NPC)")]
    public string RelationshipNPCId;

    [Header("Navigation")]
    [Tooltip("Jump to specific line index after this choice (-1 = continue to next line)")]
    public int NextLineIndex = -1;

    /// <summary>
    /// Create a simple choice that just continues the dialogue
    /// </summary>
    public static DialogueChoice Create(string text)
    {
        return new DialogueChoice
        {
            ChoiceText = text,
            NextLineIndex = -1
        };
    }

    /// <summary>
    /// Create a choice that sets a flag
    /// </summary>
    public static DialogueChoice CreateWithFlag(string text, string flagToSet)
    {
        return new DialogueChoice
        {
            ChoiceText = text,
            FlagToSet = flagToSet,
            NextLineIndex = -1
        };
    }

    /// <summary>
    /// Create a choice that affects relationship
    /// </summary>
    public static DialogueChoice CreateWithRelationship(string text, int relationshipChange)
    {
        return new DialogueChoice
        {
            ChoiceText = text,
            RelationshipChange = relationshipChange,
            NextLineIndex = -1
        };
    }

    /// <summary>
    /// Create a choice with both flag and relationship effects
    /// </summary>
    public static DialogueChoice CreateWithEffects(string text, string flagToSet, int relationshipChange)
    {
        return new DialogueChoice
        {
            ChoiceText = text,
            FlagToSet = flagToSet,
            RelationshipChange = relationshipChange,
            NextLineIndex = -1
        };
    }

    /// <summary>
    /// Create a choice that jumps to a specific line
    /// </summary>
    public static DialogueChoice CreateWithJump(string text, int nextLineIndex)
    {
        return new DialogueChoice
        {
            ChoiceText = text,
            NextLineIndex = nextLineIndex
        };
    }
}
