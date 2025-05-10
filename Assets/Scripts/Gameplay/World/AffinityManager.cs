// Purpose: Manages player relationships (affinity levels) with NPCs.
// Filepath: Assets/Scripts/Gameplay/World/AffinityManager.cs
using UnityEngine;
// using System.Collections.Generic; // Potential dependency
using System; // For Action

public class AffinityManager : MonoBehaviour
{
    // TODO: Reference DataManager to access NPCAffinityData
    // private DataManager dataManager;
    // TODO: Reference NPC definitions (Registry or ScriptableObjects)
    // TODO: Reference TaskManager if affinity is gained through specific tasks (e.g., "Walk with NPC")

    // TODO: Store affinity points required per level (lookup table/config?)
    // private Dictionary<int, float> pointsPerAffinityLevel;

    // TODO: Event for affinity point gain / level up
    // public event Action<string, int, float> OnAffinityGained; // NpcID, NewLevel, CurrentPoints
    // public event Action<string, int> OnAffinityLevelUp; // NpcID, NewLevel

    void Start()
    {
        // TODO: Get references
        // TODO: Load affinity data from DataManager
        // TODO: Initialize pointsPerAffinityLevel lookup
    }

    public void AddAffinityPoints(string npcId, float amount)
    {
        if (amount <= 0) return;

        // TODO: Get current NPCAffinityData for npcId from DataManager (or create if new)
        // TODO: Add points to CurrentAffinityPoints
        // TODO: Trigger OnAffinityGained event (optional, maybe only trigger level up)

        // TODO: Check for level up
        // float requiredPoints = GetPointsRequiredForLevel(affinityData.AffinityLevel + 1);
        // while (affinityData.CurrentAffinityPoints >= requiredPoints && requiredPoints > 0)
        // {
        //     affinityData.AffinityLevel++;
        //     affinityData.CurrentAffinityPoints -= requiredPoints; // Or reset to 0 for next level? Decide logic.
        //     // TODO: Trigger OnAffinityLevelUp event
        //     OnAffinityLevelUp?.Invoke(npcId, affinityData.AffinityLevel);
        //     Debug.Log($"Affinity with {npcId} increased to level {affinityData.AffinityLevel}!");
        //     // TODO: Check for unlocking rewards at this level
        //     requiredPoints = GetPointsRequiredForLevel(affinityData.AffinityLevel + 1);
        // }

        // TODO: Save updated NPCAffinityData
        Debug.Log($"AffinityManager: Added {amount} points to NPC {npcId} (Placeholder)");
    }

    public NPCAffinityData GetAffinityData(string npcId)
    {
        // TODO: Retrieve NPCAffinityData from DataManager
        return null; // Placeholder
    }

    public int GetAffinityLevel(string npcId)
    {
        // TODO: Retrieve level from NPCAffinityData
        return 0; // Placeholder
    }

    private float GetPointsRequiredForLevel(int level)
    {
        // TODO: Implement lookup or formula for points required for the *next* level
        return level * 100; // Example simple formula
    }

    // Method called by TaskManager when an affinity-related task completes/updates
    public void OnAffinityTaskProgress(string npcId, /* progress data, e.g., steps walked */ int steps)
    {
        // TODO: Calculate affinity points gained based on task progress (e.g., 1 point per 100 steps)
        // float pointsGained = steps / 100f;
        // AddAffinityPoints(npcId, pointsGained);
    }
}