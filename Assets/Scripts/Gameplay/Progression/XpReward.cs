// Purpose: Data structure for XP rewards from activities
// Filepath: Assets/Scripts/Data/Models/XPReward.cs
using System;

[Serializable]
public class XPReward
{
    public string MainSkillId;
    public string SubSkillId;
    public int MainSkillXP;
    public int SubSkillXP;

    public XPReward()
    {
        MainSkillId = "";
        SubSkillId = "";
        MainSkillXP = 0;
        SubSkillXP = 0;
    }

    public XPReward(string mainSkillId, string subSkillId, int mainSkillXP, int subSkillXP)
    {
        MainSkillId = mainSkillId;
        SubSkillId = subSkillId;
        MainSkillXP = mainSkillXP;
        SubSkillXP = subSkillXP;
    }

    public bool HasAnyXP()
    {
        return MainSkillXP > 0 || SubSkillXP > 0;
    }

    public override string ToString()
    {
        return $"XP Reward: {MainSkillId} +{MainSkillXP}, {SubSkillId} +{SubSkillXP}";
    }
}