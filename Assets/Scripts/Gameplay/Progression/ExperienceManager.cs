// Purpose: Handles generic experience gain if needed outside specific skills (e.g., quest completion XP).
// Filepath: Assets/Scripts/Gameplay/Progression/ExperienceManager.cs
using UnityEngine;
using System; // For Action

// Note: This might be redundant if all XP goes directly into Skills.
// Keep it minimal unless a separate "Player Level" or generic XP pool is decided upon later.
public class ExperienceManager : MonoBehaviour
{
    // TODO: If there's an overall Player Level, manage its XP and Level here.
    // public long CurrentExperience { get; private set; }
    // public int CurrentLevel { get; private set; }
    // public event Action<int> OnPlayerLevelUp;

    // TODO: Reference DataManager if player level/XP needs saving/loading.

    void Start()
    {
        // TODO: Load level/XP if applicable.
    }

    public void AddGlobalXP(long amount)
    {
        if (amount <= 0) return;
        // TODO: Add XP to CurrentExperience.
        // TODO: Check for level up based on some formula/table.
        // TODO: If level up, increment CurrentLevel, trigger event, handle stat/point gains if any.
        Debug.Log($"ExperienceManager: Added {amount} global XP (Placeholder - consider using SkillManager instead)");
    }
}