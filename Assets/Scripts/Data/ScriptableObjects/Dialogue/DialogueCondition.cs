// Purpose: Enums and data structures for dialogue conditions
// Filepath: Assets/Scripts/Data/ScriptableObjects/Dialogue/DialogueCondition.cs
using System;
using UnityEngine;

/// <summary>
/// Type of condition to check for dialogue availability
/// </summary>
public enum ConditionType
{
    Flag,           // Boolean flag (e.g., "HasFedChild")
    Relationship,   // NPC relationship level (0-10)
    StoryProgress,  // Story progress markers
    Custom          // Extension point for game-specific conditions
}

/// <summary>
/// Comparison operator for condition evaluation
/// </summary>
public enum ComparisonOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterOrEqual,
    LessOrEqual
}

/// <summary>
/// A single condition that must be met for a dialogue to be available
/// </summary>
[Serializable]
public class DialogueCondition
{
    [Tooltip("Type of condition to check")]
    public ConditionType Type = ConditionType.Flag;

    [Tooltip("Key to check (flag name, NPC ID for relationship, etc.)")]
    public string Key;

    [Tooltip("Comparison operator")]
    public ComparisonOperator Operator = ComparisonOperator.Equals;

    [Tooltip("Value to compare against (for flags: 1=true, 0=false)")]
    public int Value = 1;

    /// <summary>
    /// Create a flag condition (must be set or must not be set)
    /// </summary>
    public static DialogueCondition FlagMustBeSet(string flagName)
    {
        return new DialogueCondition
        {
            Type = ConditionType.Flag,
            Key = flagName,
            Operator = ComparisonOperator.Equals,
            Value = 1
        };
    }

    /// <summary>
    /// Create a flag condition (must NOT be set)
    /// </summary>
    public static DialogueCondition FlagMustNotBeSet(string flagName)
    {
        return new DialogueCondition
        {
            Type = ConditionType.Flag,
            Key = flagName,
            Operator = ComparisonOperator.Equals,
            Value = 0
        };
    }

    /// <summary>
    /// Create a relationship condition
    /// </summary>
    public static DialogueCondition RelationshipAtLeast(string npcId, int minLevel)
    {
        return new DialogueCondition
        {
            Type = ConditionType.Relationship,
            Key = npcId,
            Operator = ComparisonOperator.GreaterOrEqual,
            Value = minLevel
        };
    }

    public override string ToString()
    {
        return $"{Type}:{Key} {Operator} {Value}";
    }
}
