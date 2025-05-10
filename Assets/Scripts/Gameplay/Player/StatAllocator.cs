// Purpose: Handles the allocation of stat points gained from Combat Proficiency skill level ups.
// Filepath: Assets/Scripts/Gameplay/Player/StatAllocator.cs
using UnityEngine;
using System; // For Action

public class StatAllocator : MonoBehaviour
{
    // TODO: Reference DataManager to access PlayerData (base stats) and SkillData
    // private DataManager dataManager;
    // TODO: Reference PlayerController to trigger stat recalculation after allocation
    // private PlayerController playerController;
    // TODO: Reference SkillManager to know when Combat Proficiency levels up
    // private SkillManager skillManager;

    // TODO: Track available stat points
     public int AvailableStatPoints { get; private set; }

    // TODO: Define event for when available points change
    // public event Action<int> OnAvailableStatPointsChanged;

    void Start()
    {
        // TODO: Get references to other managers
        // TODO: Subscribe to SkillManager's SkillLeveledUp event (specifically for Combat Proficiency)
        // TODO: Calculate initial available points based on loaded data (e.g., Total Points from Levels - Points Spent in BaseStats)
    }

    private void OnCombatProficiencyLeveledUp(SkillType skill, int newLevel)
    {
        if (skill == SkillType.CombatProficiency)
        {
            // TODO: Calculate points gained for this level up (e.g., +1 point per level?)
            int pointsGained = 1; // Example
            AvailableStatPoints += pointsGained;
            // TODO: Trigger OnAvailableStatPointsChanged event
            // OnAvailableStatPointsChanged?.Invoke(AvailableStatPoints);
            Debug.Log($"StatAllocator: Gained {pointsGained} points. Total Available: {AvailableStatPoints}");
        }
    }

    public bool AllocatePoint(string statName)
    {
        // TODO: Check if AvailableStatPoints > 0
        // TODO: Check if statName is a valid allocatable stat (e.g., "Strength", "Intelligence")
        // TODO: Increment the base stat in PlayerData
        // dataManager.CurrentPlayerData.BaseStats[statName]++;
        // TODO: Decrement AvailableStatPoints
        // AvailableStatPoints--;
        // TODO: Trigger OnAvailableStatPointsChanged event
        // TODO: Trigger playerController.RecalculateStats()
        // TODO: Save the game or flag for saving? (Stat allocation is usually permanent)
        // dataManager.SaveGame(); // Or maybe save less frequently
        // TODO: Return true if successful
        Debug.Log($"StatAllocator: AllocatePoint to {statName} (Placeholder)");
        return true; // Placeholder
    }

    // TODO: Add method to get the current value of a base stat from PlayerData
    // public int GetBaseStatValue(string statName) { ... }
}