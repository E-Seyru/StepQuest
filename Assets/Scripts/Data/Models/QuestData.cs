// Purpose: Data structure representing a quest (definition and player progress).
// Filepath: Assets/Scripts/Data/Models/QuestData.cs
using System.Collections.Generic; // For objectives/rewards lists

// Use ScriptableObjects for Quest definitions
// Create -> WalkAndRPG -> Quest Definition
// using UnityEngine;
// [CreateAssetMenu(fileName = "NewQuest", menuName = "WalkAndRPG/Quest Definition")]
// public class QuestDefinition : ScriptableObject
// {
//     public string QuestID; // Unique identifier
//     public string Title;
//     [TextArea] public string Description;
//     public string StartNPC; // Optional: NPC ID who gives the quest
//
//     // TODO: Define prerequisite conditions (other quests, skill levels?)
//
//     // TODO: Define objectives (e.g., collect items, defeat monster, visit location)
//     // public List<QuestObjective> Objectives;
//
//     // TODO: Define rewards (XP, currency, items, ability unlocks?)
//     // public QuestRewards Rewards;
// }

// This class represents the player's progress on a specific quest
[System.Serializable]
public class PlayerQuestState
{
    public string QuestID; // Reference to QuestDefinition
    public QuestStatus Status;
    // TODO: Track progress on individual objectives
    // public Dictionary<string, int> ObjectiveProgress; // e.g., {"CollectWood": 5}

    public PlayerQuestState(string questId)
    {
        QuestID = questId;
        Status = QuestStatus.NotStarted;
        // ObjectiveProgress = new Dictionary<string, int>();
    }
}

public enum QuestStatus
{
    NotStarted,
    InProgress,
    ReadyToComplete, // All objectives met, needs final turn-in
    Completed
}

// Placeholder structures for objectives and rewards (could be more complex)
// [System.Serializable]
// public class QuestObjective {
//     public string ObjectiveID;
//     public ObjectiveType Type;
//     public string TargetID; // e.g., ItemID, MonsterID, LocationID
//     public int RequiredAmount;
//     public string Description;
// }
public enum ObjectiveType { Collect, Defeat, Visit, TalkTo }

// [System.Serializable]
// public class QuestRewards {
//     public int Currency;
//     public int Experience; // Maybe skill-specific XP?
//     public List<InventoryItemData> Items; // ItemID and Quantity
//     // public List<string> AbilityUnlocks; // AbilityIDs
// }