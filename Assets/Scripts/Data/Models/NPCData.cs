// Purpose: Data structure representing a Non-Player Character (definition and player affinity).
// Filepath: Assets/Scripts/Data/Models/NPCData.cs
// Use ScriptableObjects for NPC definitions
// Create -> WalkAndRPG -> NPC Definition
// using UnityEngine;
// [CreateAssetMenu(fileName = "NewNPC", menuName = "WalkAndRPG/NPC Definition")]
// public class NPCDefinition : ScriptableObject
// {
//     public string NpcID; // Unique identifier
//     public string DisplayName;
//     // public Sprite Portrait; // Assign in Inspector
//     public string DefaultLocationID; // Where they usually are
//
//     // TODO: Link to Dialogue Trees or files (e.g., using YarnSpinner, Ink, or custom system)
//     // public string StartingDialogueNode;
//
//     // TODO: Define quests this NPC can offer
//     // public List<string> OfferedQuestIDs;
//
//     // TODO: Define potential rewards for reaching affinity levels
// }

// This class represents the player's relationship with a specific NPC
[System.Serializable]
public class NPCAffinityData
{
    public string NpcID; // Reference to NPCDefinition
    public int AffinityLevel;
    public float CurrentAffinityPoints;
    // TODO: Store flags specific to this NPC relationship (e.g., completed personal quest)
    // public HashSet<string> RelationshipFlags;

    public NPCAffinityData(string npcId)
    {
        NpcID = npcId;
        AffinityLevel = 0;
        CurrentAffinityPoints = 0;
        // RelationshipFlags = new HashSet<string>();
    }
}