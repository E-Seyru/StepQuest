// Purpose: Service for evaluating dialogue conditions
// Filepath: Assets/Scripts/Gameplay/Dialogue/Services/DialogueConditionService.cs
using System.Collections.Generic;

/// <summary>
/// Evaluates dialogue conditions against player state.
/// Used by DialogueSelectionService to determine which dialogues are available.
/// </summary>
public class DialogueConditionService
{
    private readonly bool _enableDebugLogs;

    public DialogueConditionService(bool enableDebugLogs = false)
    {
        _enableDebugLogs = enableDebugLogs;
    }

    /// <summary>
    /// Check if all conditions for a dialogue are met
    /// </summary>
    public bool AreConditionsMet(DialogueDefinition dialogue)
    {
        if (dialogue == null) return false;
        if (dialogue.Conditions == null || dialogue.Conditions.Count == 0)
            return true; // No conditions = always available

        foreach (var condition in dialogue.Conditions)
        {
            if (!EvaluateCondition(condition))
            {
                if (_enableDebugLogs)
                    Logger.LogInfo($"DialogueConditionService: Condition not met: {condition}", Logger.LogCategory.General);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Evaluate a single condition
    /// </summary>
    public bool EvaluateCondition(DialogueCondition condition)
    {
        if (condition == null) return true;

        var playerData = DataManager.Instance?.PlayerData;
        if (playerData == null)
        {
            Logger.LogWarning("DialogueConditionService: PlayerData is null, cannot evaluate condition", Logger.LogCategory.General);
            return false;
        }

        switch (condition.Type)
        {
            case ConditionType.Flag:
                return EvaluateFlagCondition(condition, playerData);

            case ConditionType.Relationship:
                return EvaluateRelationshipCondition(condition, playerData);

            case ConditionType.StoryProgress:
                return EvaluateStoryProgressCondition(condition, playerData);

            case ConditionType.Custom:
                return EvaluateCustomCondition(condition, playerData);

            default:
                Logger.LogWarning($"DialogueConditionService: Unknown condition type: {condition.Type}", Logger.LogCategory.General);
                return false;
        }
    }

    /// <summary>
    /// Evaluate a flag condition (boolean flag check)
    /// </summary>
    private bool EvaluateFlagCondition(DialogueCondition condition, PlayerData playerData)
    {
        bool flagValue = playerData.GetDialogueFlag(condition.Key);
        int flagAsInt = flagValue ? 1 : 0;
        return CompareValues(flagAsInt, condition.Operator, condition.Value);
    }

    /// <summary>
    /// Evaluate a relationship condition (NPC relationship level 0-10)
    /// </summary>
    private bool EvaluateRelationshipCondition(DialogueCondition condition, PlayerData playerData)
    {
        int relationship = playerData.GetNPCRelationship(condition.Key);
        return CompareValues(relationship, condition.Operator, condition.Value);
    }

    /// <summary>
    /// Evaluate a story progress condition
    /// Currently treated as a flag, can be extended for story tracking
    /// </summary>
    private bool EvaluateStoryProgressCondition(DialogueCondition condition, PlayerData playerData)
    {
        // For now, treat story progress as flags
        // Can be extended to check a dedicated story progress system
        return EvaluateFlagCondition(condition, playerData);
    }

    /// <summary>
    /// Evaluate a custom condition (extension point)
    /// Can be extended to check inventory, skills, location, etc.
    /// </summary>
    private bool EvaluateCustomCondition(DialogueCondition condition, PlayerData playerData)
    {
        // Extension point for game-specific conditions
        // Examples:
        // - Check if player has a specific item
        // - Check player skill level
        // - Check current location
        // - Check time of day

        // Default: treat as flag
        return EvaluateFlagCondition(condition, playerData);
    }

    /// <summary>
    /// Compare two values using the specified operator
    /// </summary>
    private bool CompareValues(int actual, ComparisonOperator op, int expected)
    {
        switch (op)
        {
            case ComparisonOperator.Equals:
                return actual == expected;
            case ComparisonOperator.NotEquals:
                return actual != expected;
            case ComparisonOperator.GreaterThan:
                return actual > expected;
            case ComparisonOperator.LessThan:
                return actual < expected;
            case ComparisonOperator.GreaterOrEqual:
                return actual >= expected;
            case ComparisonOperator.LessOrEqual:
                return actual <= expected;
            default:
                return false;
        }
    }

    /// <summary>
    /// Get a human-readable description of why a dialogue is unavailable
    /// </summary>
    public List<string> GetUnmetConditions(DialogueDefinition dialogue)
    {
        var unmet = new List<string>();
        if (dialogue?.Conditions == null) return unmet;

        var playerData = DataManager.Instance?.PlayerData;
        if (playerData == null)
        {
            unmet.Add("PlayerData unavailable");
            return unmet;
        }

        foreach (var condition in dialogue.Conditions)
        {
            if (!EvaluateCondition(condition))
            {
                unmet.Add(GetConditionDescription(condition, playerData));
            }
        }

        return unmet;
    }

    /// <summary>
    /// Get a human-readable description of a condition
    /// </summary>
    private string GetConditionDescription(DialogueCondition condition, PlayerData playerData)
    {
        switch (condition.Type)
        {
            case ConditionType.Flag:
                var flagValue = playerData.GetDialogueFlag(condition.Key);
                return $"Flag '{condition.Key}' is {flagValue}, needs {condition.Operator} {condition.Value}";

            case ConditionType.Relationship:
                var relationship = playerData.GetNPCRelationship(condition.Key);
                return $"Relationship with '{condition.Key}' is {relationship}, needs {condition.Operator} {condition.Value}";

            case ConditionType.StoryProgress:
                return $"Story progress '{condition.Key}' not met";

            case ConditionType.Custom:
                return $"Custom condition '{condition.Key}' not met";

            default:
                return $"Unknown condition: {condition}";
        }
    }
}
