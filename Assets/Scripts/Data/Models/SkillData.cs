// Purpose: Data structure for skills (main skills and sub-skills) - DATA ONLY
// Filepath: Assets/Scripts/Data/Models/SkillData.cs
using System;

[Serializable]
public class SkillData
{
    public string SkillId;
    public int Level;
    public int Experience;

    // Constructeur par défaut pour JSON
    public SkillData()
    {
        SkillId = "";
        Level = 1;
        Experience = 0;
    }

    // Constructeur avec paramètres
    public SkillData(string skillId, int level = 1, int experience = 0)
    {
        SkillId = skillId;
        Level = level;
        Experience = experience;
    }

    /// <summary>
    /// Information basique pour debug
    /// </summary>
    public override string ToString()
    {
        return $"{SkillId}: Level {Level} ({Experience} XP)";
    }
}