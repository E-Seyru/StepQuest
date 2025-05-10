// Purpose: Script for the panel displaying active and completed quests.
// Filepath: Assets/Scripts/UI/Panels/QuestLogPanel.cs
using UnityEngine;
// using UnityEngine.UI; // Potential dependency
// using System.Collections.Generic; // Potential dependency

public class QuestLogPanel : MonoBehaviour
{
    // TODO: References to UI elements (Active quest list container, Completed quest list container, Quest details area)
    // public Transform activeQuestListContainer;
    // public Transform completedQuestListContainer;
    // public GameObject questListItemPrefab; // For displaying quest titles in the lists
    // public Text selectedQuestTitle;
    // public Text selectedQuestDescription;
    // public Transform selectedQuestObjectivesContainer;
    // public GameObject objectiveItemPrefab; // For displaying individual objectives

    // TODO: Reference QuestManager
    // private QuestManager questManager;
    // TODO: Reference QuestRegistry for definitions
    // private QuestRegistry questRegistry;

    // TODO: Store the currently selected quest ID
    // private string selectedQuestId;

    void OnEnable()
    {
        // TODO: Get references
        // TODO: Subscribe to QuestManager events (OnQuestStatusChanged, OnQuestObjectiveUpdate)
        // TODO: Populate quest lists
        // RefreshQuestLists();
        // ClearSelectedQuestDetails();
    }

    void OnDisable()
    {
        // TODO: Unsubscribe from events
    }

    void RefreshQuestLists()
    {
        // TODO: Clear both list containers
        // TODO: Get active quests from QuestManager
        // TODO: Instantiate questListItemPrefab into activeQuestListContainer for each active quest
        //      - Set text (quest title from definition)
        //      - Add listener to call OnQuestSelected(questId)
        // TODO: Get completed quests from QuestManager/DataManager
        // TODO: Instantiate questListItemPrefab into completedQuestListContainer for each completed quest
        //      - Set text
        //      - Add listener
        Debug.Log("QuestLogPanel: RefreshQuestLists (Placeholder)");
    }

    void OnQuestSelected(string questId)
    {
        // TODO: Store selectedQuestId
        // TODO: Get PlayerQuestState from QuestManager
        // TODO: Get QuestDefinition from QuestRegistry
        // TODO: Update selectedQuestTitle, selectedQuestDescription
        // TODO: Clear and populate selectedQuestObjectivesContainer
        //      - For each objective in definition:
        //          - Instantiate objectiveItemPrefab
        //          - Set objective text (from definition)
        //          - Get current progress from PlayerQuestState.ObjectiveProgress
        //          - Set progress text/indicator (e.g., "Wood Collected: 5/10")
        //          - Mark as complete if progress >= required
        Debug.Log($"QuestLogPanel: Quest selected {questId} (Placeholder)");
    }

    void ClearSelectedQuestDetails()
    {
        // TODO: Clear title, description, objectives container
        // selectedQuestId = null;
    }
}