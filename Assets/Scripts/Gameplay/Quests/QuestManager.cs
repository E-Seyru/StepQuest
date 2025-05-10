// Purpose: Manages player quests, tracking status, objectives, and rewards.
// Filepath: Assets/Scripts/Gameplay/Quests/QuestManager.cs
using UnityEngine;
using System.Collections.Generic; // For Dictionary/List
using System; // For Action

public class QuestManager : MonoBehaviour
{
    // TODO: Reference DataManager to access PlayerQuestState data
    // private DataManager dataManager;
    // TODO: Reference Quest definitions (Registry or ScriptableObjects)
    // private QuestRegistry questRegistry;
    // TODO: Reference InventoryManager (for item objectives/rewards)
    // private InventoryManager inventoryManager;
    // TODO: Reference SkillManager (for skill objectives/rewards)
    // private SkillManager skillManager;
    // TODO: Reference ExperienceManager (for XP rewards) or SkillManager directly
    // private ExperienceManager experienceManager;

    // TODO: Event for when a quest status changes
    // public event Action<string, QuestStatus> OnQuestStatusChanged; // QuestID, NewStatus
    // TODO: Event for when an objective updates
    // public event Action<string, string, int, int> OnQuestObjectiveUpdate; // QuestID, ObjectiveID, CurrentAmount, RequiredAmount

    void Start()
    {
        // TODO: Get references
        // TODO: Load quest states from DataManager
        // TODO: Subscribe to relevant events (e.g., InventoryChanged, MonsterDefeated, LocationEntered) to check objective progress
    }

    public bool CanStartQuest(string questId)
    {
        // TODO: Get quest definition
        // TODO: Check prerequisites (other quests completed, skill levels, player level?)
        // TODO: Check if quest is already started or completed
        return true; // Placeholder
    }

    public void StartQuest(string questId)
    {
        // TODO: If CanStartQuest is true:
        // TODO: Create new PlayerQuestState for this questId
        // TODO: Set status to InProgress
        // TODO: Add state to DataManager's quest data
        // TODO: Trigger OnQuestStatusChanged event
        // TODO: Display quest started notification?
        Debug.Log($"QuestManager: Starting quest {questId} (Placeholder)");
    }

    public void CompleteQuest(string questId)
    {
        // TODO: Get PlayerQuestState and QuestDefinition
        // TODO: Check if status is ReadyToComplete
        // TODO: Grant rewards (call InventoryManager.AddItem, SkillManager.AddXP, etc.)
        // TODO: Set status to Completed
        // TODO: Trigger OnQuestStatusChanged event
        // TODO: Add to PlayerData completed quests set?
        // TODO: Save game state?
        Debug.Log($"QuestManager: Completing quest {questId} (Placeholder)");
    }

    // Called by other systems when relevant actions occur
    public void UpdateObjectiveProgress(ObjectiveType type, string targetId, int amount = 1)
    {
        // TODO: Iterate through all InProgress quests in DataManager
        // TODO: For each quest, check its objectives
        // TODO: If an objective matches the type and targetId:
        //      - Increment the progress in PlayerQuestState.ObjectiveProgress
        //      - Trigger OnQuestObjectiveUpdate event
        //      - Check if this objective is now complete
        //      - If all objectives for the quest are complete, change status to ReadyToComplete
        //      - Trigger OnQuestStatusChanged event if status changes
        Debug.Log($"QuestManager: UpdateObjectiveProgress for {type} {targetId} x{amount} (Placeholder)");
    }

    public QuestStatus GetQuestStatus(string questId)
    {
        // TODO: Get PlayerQuestState from DataManager and return status
        return QuestStatus.NotStarted; // Placeholder
    }

    public PlayerQuestState GetQuestState(string questId)
    {
        // TODO: Get PlayerQuestState from DataManager
        return null; // Placeholder
    }

    public List<PlayerQuestState> GetActiveQuests()
    {
        // TODO: Filter DataManager's quest states for InProgress or ReadyToComplete
        return new List<PlayerQuestState>(); // Placeholder
    }
}