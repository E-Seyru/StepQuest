// Purpose: Manager for all XP calculations and skill progression
// Filepath: Assets/Scripts/Systems/XpManager.cs
using UnityEngine;

public class XpManager : MonoBehaviour
{
    public static XpManager Instance { get; private set; }

    [Header("XP Configuration")]
    [SerializeField] private int baseXpForLevel2 = 100;      // XP nécessaire pour passer au niveau 2
    [SerializeField] private float xpGrowthRate = 1.5f;      // Multiplicateur d'XP par niveau (1.5 = +50% par niveau)
    [SerializeField] private int maxLevel = 100;             // Niveau maximum

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // === CALCULS DE PROGRESSION ===

    /// <summary>
    /// XP nécessaire pour passer du niveau N au niveau N+1
    /// </summary>
    public int GetXPRequiredForLevelUp(int currentLevel)
    {
        if (currentLevel >= maxLevel) return 0;

        // Formule exponentielle : baseXP * (growthRate ^ (level - 1))
        // Niveau 1→2 : 100 XP
        // Niveau 2→3 : 100 * 1.5 = 150 XP  
        // Niveau 3→4 : 100 * 1.5² = 225 XP
        // etc.

        return Mathf.RoundToInt(baseXpForLevel2 * Mathf.Pow(xpGrowthRate, currentLevel - 1));
    }

    /// <summary>
    /// XP total nécessaire pour atteindre un niveau donné (cumulatif)
    /// </summary>
    public int GetTotalXPRequiredForLevel(int targetLevel)
    {
        if (targetLevel <= 1) return 0;

        int totalXP = 0;
        for (int level = 1; level < targetLevel; level++)
        {
            totalXP += GetXPRequiredForLevelUp(level);
        }
        return totalXP;
    }

    /// <summary>
    /// XP nécessaire pour passer au niveau suivant (depuis l'XP actuelle)
    /// </summary>
    public int GetXPNeededForNextLevel(SkillData skill)
    {
        if (skill.Level >= maxLevel) return 0;

        int totalXPForNextLevel = GetTotalXPRequiredForLevel(skill.Level + 1);
        return totalXPForNextLevel - skill.Experience;
    }

    /// <summary>
    /// Pourcentage de progression vers le niveau suivant (0.0 à 1.0)
    /// </summary>
    public float GetProgressToNextLevel(SkillData skill)
    {
        if (skill.Level >= maxLevel) return 1.0f;

        int currentLevelTotalXP = GetTotalXPRequiredForLevel(skill.Level);
        int nextLevelTotalXP = GetTotalXPRequiredForLevel(skill.Level + 1);
        int currentProgress = skill.Experience - currentLevelTotalXP;
        int xpNeededForThisLevel = nextLevelTotalXP - currentLevelTotalXP;

        return (float)currentProgress / xpNeededForThisLevel;
    }

    /// <summary>
    /// Vérifier si une compétence peut monter de niveau
    /// </summary>
    public bool CanLevelUp(SkillData skill)
    {
        if (skill.Level >= maxLevel) return false;
        return skill.Experience >= GetTotalXPRequiredForLevel(skill.Level + 1);
    }

    /// <summary>
    /// Faire monter une compétence de niveau (retourne le nombre de niveaux gagnés)
    /// </summary>
    public int ProcessLevelUps(SkillData skill)
    {
        int levelsGained = 0;

        while (CanLevelUp(skill))
        {
            skill.Level++;
            levelsGained++;

            Logger.LogInfo($"XpManager: {skill.SkillId} leveled up to {skill.Level}! (Total XP: {skill.Experience})", Logger.LogCategory.General);
        }

        return levelsGained;
    }

    // === CALCULS DE BONUS ===

    /// <summary>
    /// Calculer le bonus d'efficacité basé sur le niveau
    /// </summary>
    public float GetEfficiencyBonus(int level)
    {
        // Système de bonus progressif :
        // Niveaux 1-25 : Pas de bonus (apprentissage)
        // Niveaux 26-50 : +1% par niveau
        // Niveaux 51-75 : +2% par niveau  
        // Niveaux 76-100 : +3% par niveau

        if (level <= 25)
            return 1.0f; // Pas de bonus
        else if (level <= 50)
            return 1.0f + (level - 25) * 0.01f; // +1% par niveau
        else if (level <= 75)
            return 1.25f + (level - 50) * 0.02f; // +25% + 2% par niveau
        else
            return 1.75f + (level - 75) * 0.03f; // +75% + 3% par niveau
    }

    /// <summary>
    /// Description du niveau actuel pour l'UI
    /// </summary>
    public string GetLevelDescription(int level)
    {
        if (level <= 25)
            return "Apprenti";
        else if (level <= 50)
            return "Compétent";
        else if (level <= 75)
            return "Expert";
        else if (level <= 90)
            return "Maître";
        else
            return "Légendaire";
    }

    // === CALCULS D'XP GAGNÉE ===

    /// <summary>
    /// Calculer l'XP gagnée pour une activité step-based
    /// </summary>
    public XPReward CalculateStepBasedXP(int ticksCompleted, ActivityVariant variant)
    {
        if (variant == null)
            return new XPReward("Unknown", "Unknown", 0, 0);

        int mainSkillXP = variant.MainSkillXPPerTick * ticksCompleted;
        int subSkillXP = variant.SubSkillXPPerTick * ticksCompleted;

        return new XPReward(
            variant.GetMainSkillId(),
            variant.GetSubSkillId(),
            mainSkillXP,
            subSkillXP
        );
    }

    /// <summary>
    /// Calculer l'XP gagnée pour une activité time-based
    /// </summary>
    public XPReward CalculateTimeBasedXP(int completedCrafts, ActivityVariant variant)
    {
        if (variant == null)
            return new XPReward("Unknown", "Unknown", 0, 0);

        int mainSkillXP = variant.MainSkillXPPerTick * completedCrafts;
        int subSkillXP = variant.SubSkillXPPerTick * completedCrafts;

        return new XPReward(
            variant.GetMainSkillId(),
            variant.GetSubSkillId(),
            mainSkillXP,
            subSkillXP
        );
    }

    /// <summary>
    /// Appliquer une récompense d'XP au joueur
    /// </summary>
    public void ApplyXPReward(XPReward xpReward)
    {
        if (xpReward.MainSkillXP > 0)
        {
            AddSkillXP(xpReward.MainSkillId, xpReward.MainSkillXP);
        }

        if (xpReward.SubSkillXP > 0)
        {
            AddSubSkillXP(xpReward.SubSkillId, xpReward.SubSkillXP);
        }
    }

    // === INTÉGRATION AVEC PLAYERDATA ===

    /// <summary>
    /// Ajouter de l'XP à une compétence principale et traiter les montées de niveau
    /// </summary>
    public bool AddSkillXP(string skillId, int xpGained)
    {
        if (DataManager.Instance?.PlayerData == null) return false;

        var playerData = DataManager.Instance.PlayerData;
        var skills = playerData.Skills;

        if (!skills.ContainsKey(skillId))
        {
            skills[skillId] = new SkillData(skillId, 1, 0);
        }

        var skill = skills[skillId];
        skill.Experience += xpGained;

        // Traiter les montées de niveau
        int levelsGained = ProcessLevelUps(skill);

        skills[skillId] = skill;
        playerData.Skills = skills; // Sauvegarder

        Logger.LogInfo($"XpManager: {skillId} gained {xpGained} XP (Level {skill.Level})", Logger.LogCategory.General);

        return levelsGained > 0;
    }

    /// <summary>
    /// Ajouter de l'XP à une sous-compétence et traiter les montées de niveau
    /// </summary>
    public bool AddSubSkillXP(string variantId, int xpGained)
    {
        if (DataManager.Instance?.PlayerData == null) return false;

        var playerData = DataManager.Instance.PlayerData;
        var subSkills = playerData.SubSkills;

        if (!subSkills.ContainsKey(variantId))
        {
            subSkills[variantId] = new SkillData(variantId, 1, 0);
        }

        var subSkill = subSkills[variantId];
        subSkill.Experience += xpGained;

        // Traiter les montées de niveau
        int levelsGained = ProcessLevelUps(subSkill);

        subSkills[variantId] = subSkill;
        playerData.SubSkills = subSkills; // Sauvegarder

        Logger.LogInfo($"XpManager: {variantId} sub-skill gained {xpGained} XP (Level {subSkill.Level})", Logger.LogCategory.General);

        return levelsGained > 0;
    }

    // === MÉTHODES UTILITAIRES PUBLIQUES ===

    /// <summary>
    /// Obtenir une compétence du joueur (créée si n'existe pas)
    /// </summary>
    public SkillData GetPlayerSkill(string skillId)
    {
        if (DataManager.Instance?.PlayerData == null) return new SkillData(skillId);

        var skills = DataManager.Instance.PlayerData.Skills;
        if (!skills.ContainsKey(skillId))
        {
            skills[skillId] = new SkillData(skillId, 1, 0);
            DataManager.Instance.PlayerData.Skills = skills;
        }

        return skills[skillId];
    }

    /// <summary>
    /// Obtenir une sous-compétence du joueur (créée si n'existe pas)
    /// </summary>
    public SkillData GetPlayerSubSkill(string variantId)
    {
        if (DataManager.Instance?.PlayerData == null) return new SkillData(variantId);

        var subSkills = DataManager.Instance.PlayerData.SubSkills;
        if (!subSkills.ContainsKey(variantId))
        {
            subSkills[variantId] = new SkillData(variantId, 1, 0);
            DataManager.Instance.PlayerData.SubSkills = subSkills;
        }

        return subSkills[variantId];
    }
}