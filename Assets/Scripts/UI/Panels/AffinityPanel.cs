// Purpose: Script for the panel displaying NPC affinity levels and details.
// Filepath: Assets/Scripts/UI/Panels/AffinityPanel.cs
using UnityEngine;
// using UnityEngine.UI; // Potential dependency
// using System.Collections.Generic; // Potential dependency

public class AffinityPanel : MonoBehaviour
{
    // TODO: References to UI elements (NPC list container, NPC details area)
    // public Transform npcListContainer;
    // public GameObject npcListItemPrefab; // Displays NPC name, portrait, current affinity level
    // public Text selectedNpcName;
    // public Image selectedNpcPortrait;
    // public Text selectedNpcAffinityLevelText;
    // public Slider selectedNpcAffinityProgressBar;
    // public Text selectedNpcDescription; // Maybe story/bio unlocked via affinity?
    // public Transform unlockedRewardsContainer; // Show rewards for levels

    // TODO: Reference AffinityManager
    // private AffinityManager affinityManager;
    // TODO: Reference NPC definitions (Registry?)
    // private NpcRegistry npcRegistry;
    // TODO: Reference DataManager (to know which NPCs have been met/have affinity data)
    // private DataManager dataManager;

    // TODO: Store the currently selected NPC ID
    // private string selectedNpcId;

    void OnEnable()
    {
        // TODO: Get references
        // TODO: Subscribe to AffinityManager events (OnAffinityLevelUp)
        // TODO: Populate NPC list
        // RefreshNpcList();
        // ClearSelectedNpcDetails();
    }

    void OnDisable()
    {
        // TODO: Unsubscribe from events
    }

    void RefreshNpcList()
    {
        // TODO: Clear npcListContainer
        // TODO: Get all known/met NPCs (e.g., iterate through DataManager.GetAllAffinityData())
        // TODO: For each known NPC:
        //      - Instantiate npcListItemPrefab
        //      - Get NPCDefinition from registry using NpcID
        //      - Get NPCAffinityData from AffinityManager/DataManager
        //      - Setup prefab UI (Name, Portrait, Level)
        //      - Add listener to call OnNpcSelected(npcId)
        Debug.Log("AffinityPanel: RefreshNpcList (Placeholder)");
    }

    void OnNpcSelected(string npcId)
    {
        // TODO: Store selectedNpcId
        // TODO: Get NPCDefinition and NPCAffinityData
        // TODO: Update selectedNpcName, selectedNpcPortrait
        // TODO: Update selectedNpcAffinityLevelText
        // TODO: Calculate progress towards next level and update selectedNpcAffinityProgressBar
        // TODO: Update description/bio based on affinity level?
        // TODO: Populate unlockedRewardsContainer based on current level and definition rewards
        Debug.Log($"AffinityPanel: NPC selected {npcId} (Placeholder)");
    }

    void ClearSelectedNpcDetails()
    {
        // TODO: Clear name, portrait, progress bar, description, rewards
        // selectedNpcId = null;
    }
}