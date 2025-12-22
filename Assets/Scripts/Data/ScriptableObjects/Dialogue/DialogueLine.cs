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

    [Header("Line Options")]
    [Tooltip("If true, the dialogue ends after this line (click to close)")]
    public bool EndsDialogue = false;

    [Tooltip("If true, show any pending reward notification on this line")]
    public bool ShowReward = false;

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

    [Header("Rewards")]
    [Tooltip("Ability ID to grant when this choice is selected (leave empty for no ability)")]
    public string AbilityToGrant;

    [Tooltip("Items to grant when this choice is selected")]
    public List<DialogueItemReward> ItemsToGrant = new List<DialogueItemReward>();

    [Header("Navigation")]
    [Tooltip("Jump to specific line index after this choice (-1 = continue to next line)")]
    public int NextLineIndex = -1;

    /// <summary>
    /// Does this choice grant any rewards?
    /// </summary>
    public bool HasRewards => !string.IsNullOrEmpty(AbilityToGrant) || (ItemsToGrant != null && ItemsToGrant.Count > 0);

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

    /// <summary>
    /// Create a choice that grants an ability
    /// </summary>
    public static DialogueChoice CreateWithAbility(string text, string abilityId)
    {
        return new DialogueChoice
        {
            ChoiceText = text,
            AbilityToGrant = abilityId,
            NextLineIndex = -1
        };
    }

    /// <summary>
    /// Create a choice with full rewards
    /// </summary>
    public static DialogueChoice CreateWithRewards(string text, string flagToSet, int relationshipChange, string abilityId)
    {
        return new DialogueChoice
        {
            ChoiceText = text,
            FlagToSet = flagToSet,
            RelationshipChange = relationshipChange,
            AbilityToGrant = abilityId,
            NextLineIndex = -1
        };
    }
}

/// <summary>
/// Item reward from dialogue (item + quantity)
/// </summary>
[Serializable]
public class DialogueItemReward
{
    [Tooltip("Item ID to grant")]
    public string ItemId;

    [Tooltip("Quantity to grant")]
    public int Quantity = 1;

    public DialogueItemReward() { }

    public DialogueItemReward(string itemId, int quantity = 1)
    {
        ItemId = itemId;
        Quantity = quantity;
    }
}
