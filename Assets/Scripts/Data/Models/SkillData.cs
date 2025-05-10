// Purpose: Data structure representing a single skill's state for the player.
// Filepath: Assets/Scripts/Data/Models/SkillData.cs
using System.Collections.Generic; // For Dictionary

[System.Serializable]
public class AllSkillsData // Wrapper class to hold all skill data easily
{
    // Use a Dictionary to store level and XP for each skill type
    public Dictionary<SkillType, SkillProgress> SkillProgressData;

    public AllSkillsData()
    {
        SkillProgressData = new Dictionary<SkillType, SkillProgress>();
    }
}

[System.Serializable]
public class SkillProgress
{
    public int Level;
    public float CurrentXP;
    // Maybe add required XP for next level if calculated often
    // public float XpToNextLevel;

    public SkillProgress(int level = 1, float xp = 0)
    {
        Level = level;
        CurrentXP = xp;
    }
}

// Define the types of skills available in the game
public enum SkillType
{
    // Gathering
    Mining,
    Woodcutting,
    Fishing,
    // Crafting
    Alchemy,
    Cooking,
    Blacksmithing,
    // Combat
    CombatProficiency,
    // Other
    Exploration,
    // Add more as needed...
}