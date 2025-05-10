// Purpose: Manages player skills, tracks XP gain, handles leveling up.
// Filepath: Assets/Scripts/Gameplay/Progression/SkillManager.cs
using UnityEngine;
using System.Collections.Generic; // For Dictionary
using System; // For Action

public class SkillManager : MonoBehaviour
{
    // TODO: Reference DataManager to access AllSkillsData
    // private DataManager dataManager;

    // TODO: Store XP required per level (could be a formula or a lookup table/ScriptableObject)
    // private Dictionary<int, float> xpPerLevel;

    // TODO: Define events for skill XP gain and level up
    // public event Action<SkillType, float, float> OnSkillXPGained; // Skill, XP Gained, Current XP
    // public event Action<SkillType, int> OnSkillLeveledUp; // Skill, New Level

    void Start()
    {
        // TODO: Get reference to DataManager
        // TODO: Load skill data (AllSkillsData)
        // TODO: Initialize xpPerLevel lookup (e.g., from a config file or calculation)
    }

    public void AddXP(SkillType skill, float amount)
    {
        if (amount <= 0) return;

        // TODO: Get current skill progress from dataManager.CurrentSkillData
        // SkillProgress progress = GetSkillProgress(skill);
        // if (progress == null) { /* Initialize skill? */ return; }

        // TODO: Add XP
        // progress.CurrentXP += amount;

        // TODO: Trigger OnSkillXPGained event

        // TODO: Check for level up
        // float requiredXP = GetXPRequiredForLevel(progress.Level + 1);
        // while (progress.CurrentXP >= requiredXP && requiredXP > 0) // requiredXP > 0 prevents infinite loop if formula is bad
        // {
        //     progress.Level++;
        //     progress.CurrentXP -= requiredXP;
        //     // TODO: Trigger OnSkillLeveledUp event
        //     OnSkillLeveledUp?.Invoke(skill, progress.Level);
        //     Debug.Log($"{skill} leveled up to {progress.Level}!");
        //     requiredXP = GetXPRequiredForLevel(progress.Level + 1);
        // }

        // TODO: Update the skill progress in DataManager's data
        // dataManager.CurrentSkillData.SkillProgressData[skill] = progress;

        Debug.Log($"SkillManager: Added {amount} XP to {skill} (Placeholder)");
    }

    public SkillProgress GetSkillProgress(SkillType skill)
    {
        // TODO: Retrieve SkillProgress for the given skill from DataManager's data
        // dataManager.CurrentSkillData.SkillProgressData.TryGetValue(skill, out SkillProgress progress);
        // return progress ?? new SkillProgress(); // Return default if not found? Or null?
        return new SkillProgress(); // Placeholder
    }

    public int GetSkillLevel(SkillType skill)
    {
        // TODO: Retrieve level for the given skill
        // return GetSkillProgress(skill)?.Level ?? 1;
        return 1; // Placeholder
    }

    private float GetXPRequiredForLevel(int level)
    {
        // TODO: Implement lookup or formula for XP required for the *next* level (level param is the target level)
        // Example formula: return level * level * 100;
        if (level <= 1) return 100; // Base case
        return Mathf.Pow(level - 1, 2) * 100 + 100; // Example scaling formula
    }
}