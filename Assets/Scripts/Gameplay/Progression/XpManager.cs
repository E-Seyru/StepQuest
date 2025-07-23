// Purpose: Manages player experience and skill progression
// Filepath: Assets/Scripts/Gameplay/Progression/XpManager.cs
using System.Collections.Generic;
using UnityEngine;

public class XpManager : MonoBehaviour
{
    [Header("XP Settings")]

    [SerializeField] private int maxLevel = 100;
    public int MaxLevel => maxLevel; // Propriété publique en lecture seule
    [SerializeField] private int baseXpForLevel2 = 100;
    [SerializeField] private float xpGrowthRate = 1.2f;

    [Header("Performance")]
    [SerializeField] private float saveDelay = 5f; // Delai avant sauvegarde automatique

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    public static XpManager Instance { get; private set; }

    // Cache pour optimiser les calculs repetes
    private Dictionary<int, int> xpRequiredCache = new Dictionary<int, int>();
    private bool pendingSave = false;

    #region Unity Lifecycle

    private void Awake()
    {
        // Singleton pattern securise
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeXpCache();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            // Sauvegarder avant destruction si necessaire
            if (pendingSave)
            {
                SaveGameData();
            }
            Instance = null;
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Precalculer les XP requis pour optimiser les performances
    /// </summary>
    private void InitializeXpCache()
    {
        for (int level = 1; level <= maxLevel; level++)
        {
            xpRequiredCache[level] = CalculateXPRequiredForLevelUp(level);
        }

        if (enableDebugLogs)
        {
            Logger.LogInfo("XpManager: XP cache initialized", Logger.LogCategory.General);
        }
    }

    #endregion

    #region Public API - Skill Management

    /// <summary>
    /// Ajouter de l'XP a une competence principale de manière securisee
    /// </summary>
    public bool AddSkillXP(string skillId, int xpGained)
    {
        if (!ValidateXpInput(skillId, xpGained)) return false;
        if (!EnsurePlayerDataExists()) return false;

        var playerData = DataManager.Instance.PlayerData;
        var skills = playerData.Skills;

        // Creer la competence si elle n'existe pas
        if (!skills.ContainsKey(skillId))
        {
            skills[skillId] = new SkillData(skillId, 1, 0);
        }

        var skill = skills[skillId];
        skill.Experience += xpGained;

        // Traiter les montees de niveau
        int levelsGained = ProcessLevelUps(skill);
        bool leveledUp = levelsGained > 0;

        // Sauvegarder les modifications
        skills[skillId] = skill;
        playerData.Skills = skills;

        // Programmer une sauvegarde differee
        ScheduleSave();

        if (enableDebugLogs)
        {
            Logger.LogInfo($"XpManager: {skillId} gained {xpGained} XP (Level {skill.Level}, Total: {skill.Experience})",
                Logger.LogCategory.General);
        }

        return leveledUp;
    }

    /// <summary>
    /// Ajouter de l'XP a une sous-competence de manière securisee
    /// </summary>
    public bool AddSubSkillXP(string subSkillId, int xpGained)
    {
        if (!ValidateXpInput(subSkillId, xpGained)) return false;
        if (!EnsurePlayerDataExists()) return false;

        var playerData = DataManager.Instance.PlayerData;
        var subSkills = playerData.SubSkills;

        // Creer la sous-competence si elle n'existe pas
        if (!subSkills.ContainsKey(subSkillId))
        {
            subSkills[subSkillId] = new SkillData(subSkillId, 1, 0);
        }

        var subSkill = subSkills[subSkillId];
        subSkill.Experience += xpGained;

        // Traiter les montees de niveau
        int levelsGained = ProcessLevelUps(subSkill);
        bool leveledUp = levelsGained > 0;

        // Sauvegarder les modifications
        subSkills[subSkillId] = subSkill;
        playerData.SubSkills = subSkills;

        // Programmer une sauvegarde differee
        ScheduleSave();

        if (enableDebugLogs)
        {
            Logger.LogInfo($"XpManager: SubSkill {subSkillId} gained {xpGained} XP (Level {subSkill.Level}, Total: {subSkill.Experience})",
                Logger.LogCategory.General);
        }

        return leveledUp;
    }

    /// <summary>
    /// Appliquer une recompense d'XP complète de manière optimisee
    /// </summary>
    public XpApplyResult ApplyXPReward(XPReward xpReward)
    {
        if (xpReward == null || !xpReward.HasAnyXP())
        {
            return new XpApplyResult(false, false);
        }

        bool mainSkillLeveledUp = false;
        bool subSkillLeveledUp = false;

        // Appliquer l'XP principale
        if (xpReward.MainSkillXP > 0)
        {
            mainSkillLeveledUp = AddSkillXP(xpReward.MainSkillId, xpReward.MainSkillXP);
        }

        // Appliquer l'XP secondaire
        if (xpReward.SubSkillXP > 0)
        {
            subSkillLeveledUp = AddSubSkillXP(xpReward.SubSkillId, xpReward.SubSkillXP);
        }

        return new XpApplyResult(mainSkillLeveledUp, subSkillLeveledUp);
    }

    #endregion

    #region Public API - Data Access

    /// <summary>
    /// Obtenir les donnees d'une competence principale de manière securisee
    /// </summary>
    public SkillData GetPlayerSkill(string skillId)
    {
        if (!EnsurePlayerDataExists() || string.IsNullOrEmpty(skillId))
        {
            return new SkillData(skillId ?? "Unknown", 1, 0);
        }

        var skills = DataManager.Instance.PlayerData.Skills;

        if (skills.ContainsKey(skillId))
        {
            return skills[skillId];
        }

        // Creer une nouvelle competence si elle n'existe pas
        var newSkill = new SkillData(skillId, 1, 0);
        skills[skillId] = newSkill;
        ScheduleSave();
        return newSkill;
    }

    /// <summary>
    /// Obtenir les donnees d'une sous-competence de manière securisee
    /// </summary>
    public SkillData GetPlayerSubSkill(string subSkillId)
    {
        if (!EnsurePlayerDataExists() || string.IsNullOrEmpty(subSkillId))
        {
            return new SkillData(subSkillId ?? "Unknown", 1, 0);
        }

        var subSkills = DataManager.Instance.PlayerData.SubSkills;

        if (subSkills.ContainsKey(subSkillId))
        {
            return subSkills[subSkillId];
        }

        // Creer une nouvelle sous-competence si elle n'existe pas
        var newSubSkill = new SkillData(subSkillId, 1, 0);
        subSkills[subSkillId] = newSubSkill;
        ScheduleSave();
        return newSubSkill;
    }

    #endregion

    #region XP Calculation Methods

    /// <summary>
    /// Calculer l'XP gagnee pour une activite step-based avec bonus de niveau
    /// </summary>
    public XPReward CalculateStepBasedXP(int ticksCompleted, ActivityVariant variant)
    {
        if (variant == null || ticksCompleted <= 0)
        {
            return new XPReward("Unknown", "Unknown", 0, 0);
        }

        // Obtenir les niveaux actuels pour calculer les bonus
        var mainSkill = GetPlayerSkill(variant.GetMainSkillId());
        var subSkill = GetPlayerSubSkill(variant.GetSubSkillId());

        // Calculer l'XP de base
        int baseMainXP = variant.MainSkillXPPerTick * ticksCompleted;
        int baseSubXP = variant.SubSkillXPPerTick * ticksCompleted;

        // Appliquer les bonus de niveau (legèrement reduits pour equilibrer)
        float mainBonus = GetLevelBonus(mainSkill.Level);
        float subBonus = GetLevelBonus(subSkill.Level);

        int finalMainXP = Mathf.RoundToInt(baseMainXP * mainBonus);
        int finalSubXP = Mathf.RoundToInt(baseSubXP * subBonus);

        return new XPReward(
            variant.GetMainSkillId(),
            variant.GetSubSkillId(),
            finalMainXP,
            finalSubXP
        );
    }

    /// <summary>
    /// Calculer l'XP gagnee pour une activite time-based avec bonus de niveau
    /// </summary>
    public XPReward CalculateTimeBasedXP(int completedCrafts, ActivityVariant variant)
    {
        if (variant == null || completedCrafts <= 0)
        {
            return new XPReward("Unknown", "Unknown", 0, 0);
        }

        // Obtenir les niveaux actuels pour calculer les bonus
        var mainSkill = GetPlayerSkill(variant.GetMainSkillId());
        var subSkill = GetPlayerSubSkill(variant.GetSubSkillId());

        // Calculer l'XP de base
        int baseMainXP = variant.MainSkillXPPerTick * completedCrafts;
        int baseSubXP = variant.SubSkillXPPerTick * completedCrafts;

        // Appliquer les bonus de niveau
        float mainBonus = GetLevelBonus(mainSkill.Level);
        float subBonus = GetLevelBonus(subSkill.Level);

        int finalMainXP = Mathf.RoundToInt(baseMainXP * mainBonus);
        int finalSubXP = Mathf.RoundToInt(baseSubXP * subBonus);

        return new XPReward(
            variant.GetMainSkillId(),
            variant.GetSubSkillId(),
            finalMainXP,
            finalSubXP
        );
    }

    #endregion

    #region Level System

    /// <summary>
    /// XP necessaire pour passer du niveau actuel au suivant
    /// </summary>
    public int GetXPRequiredForLevelUp(int currentLevel)
    {
        if (currentLevel >= maxLevel) return 0;

        if (xpRequiredCache.ContainsKey(currentLevel))
        {
            return xpRequiredCache[currentLevel];
        }

        return CalculateXPRequiredForLevelUp(currentLevel);
    }

    private int CalculateXPRequiredForLevelUp(int currentLevel)
    {
        if (currentLevel <= 0) return baseXpForLevel2;
        return Mathf.RoundToInt(baseXpForLevel2 * Mathf.Pow(xpGrowthRate, currentLevel - 1));
    }

    /// <summary>
    /// XP total necessaire pour atteindre un niveau donne (cumulatif)
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
    /// XP necessaire pour passer au niveau suivant (depuis l'XP actuelle)
    /// </summary>
    public int GetXPNeededForNextLevel(SkillData skill)
    {
        if (skill.Level >= maxLevel) return 0;

        int totalXPForNextLevel = GetTotalXPRequiredForLevel(skill.Level + 1);
        return totalXPForNextLevel - skill.Experience;
    }

    /// <summary>
    /// Pourcentage de progression vers le niveau suivant (0.0 a 1.0)
    /// </summary>
    public float GetProgressToNextLevel(SkillData skill)
    {
        if (skill.Level >= maxLevel) return 1.0f;

        int currentLevelTotalXP = GetTotalXPRequiredForLevel(skill.Level);
        int nextLevelTotalXP = GetTotalXPRequiredForLevel(skill.Level + 1);
        int currentProgress = skill.Experience - currentLevelTotalXP;
        int xpNeededForThisLevel = nextLevelTotalXP - currentLevelTotalXP;

        return Mathf.Clamp01((float)currentProgress / xpNeededForThisLevel);
    }

    /// <summary>
    /// Faire monter une competence de niveau (retourne le nombre de niveaux gagnes)
    /// </summary>
    public int ProcessLevelUps(SkillData skill)
    {
        int levelsGained = 0;

        while (CanLevelUp(skill) && skill.Level < maxLevel)
        {
            skill.Level++;
            levelsGained++;

            if (enableDebugLogs)
            {
                Logger.LogInfo($"XpManager: {skill.SkillId} leveled up to {skill.Level}! (Total XP: {skill.Experience})",
                    Logger.LogCategory.General);
            }
        }

        return levelsGained;
    }

    /// <summary>
    /// Verifier si une competence peut monter de niveau
    /// </summary>
    public bool CanLevelUp(SkillData skill)
    {
        if (skill.Level >= maxLevel) return false;
        return skill.Experience >= GetTotalXPRequiredForLevel(skill.Level + 1);
    }

    #endregion

    #region Bonus System

    /// <summary>
    /// Calculer le bonus d'efficacite base sur le niveau (version equilibree)
    /// </summary>
    public float GetEfficiencyBonus(int level)
    {
        // Système de bonus progressif equilibre :
        // Niveaux 1-25 : Pas de bonus (apprentissage)
        // Niveaux 26-50 : +0.5% par niveau
        // Niveaux 51-75 : +1% par niveau  
        // Niveaux 76-100 : +1.5% par niveau

        if (level <= 25)
            return 1.0f; // Pas de bonus
        else if (level <= 50)
            return 1.0f + (level - 25) * 0.005f; // +0.5% par niveau
        else if (level <= 75)
            return 1.125f + (level - 50) * 0.01f; // +12.5% + 1% par niveau
        else
            return 1.375f + (level - 75) * 0.015f; // +37.5% + 1.5% par niveau
    }

    /// <summary>
    /// Calculer le bonus d'XP base sur le niveau (nouveau)
    /// </summary>
    private float GetLevelBonus(int level)
    {
        // Petit bonus d'XP pour recompenser la progression
        if (level <= 10) return 1.0f;
        if (level <= 25) return 1.0f + (level - 10) * 0.01f; // +1% par niveau après 10
        if (level <= 50) return 1.15f + (level - 25) * 0.005f; // +0.5% par niveau après 25
        return 1.275f + (level - 50) * 0.002f; // +0.2% par niveau après 50
    }

    /// <summary>
    /// Description du niveau actuel pour l'UI
    /// </summary>
    public string GetLevelDescription(int level)
    {
        if (level <= 25) return "Apprenti";
        if (level <= 50) return "Competent";
        if (level <= 75) return "Expert";
        if (level <= 90) return "Maître";
        return "Legendaire";
    }

    #endregion

    #region Validation & Safety

    /// <summary>
    /// Valider les paramètres d'entree pour l'XP
    /// </summary>
    private bool ValidateXpInput(string skillId, int xpGained)
    {
        if (string.IsNullOrEmpty(skillId))
        {
            if (enableDebugLogs)
            {
                Logger.LogWarning("XpManager: Cannot add XP - skillId is null or empty", Logger.LogCategory.General);
            }
            return false;
        }

        if (xpGained <= 0)
        {
            if (enableDebugLogs)
            {
                Logger.LogWarning($"XpManager: Cannot add XP - invalid amount: {xpGained}", Logger.LogCategory.General);
            }
            return false;
        }

        return true;
    }

    /// <summary>
    /// S'assurer que les donnees du joueur existent
    /// </summary>
    private bool EnsurePlayerDataExists()
    {
        if (DataManager.Instance == null)
        {
            if (enableDebugLogs)
            {
                Logger.LogError("XpManager: DataManager.Instance is null!", Logger.LogCategory.General);
            }
            return false;
        }

        if (DataManager.Instance.PlayerData == null)
        {
            if (enableDebugLogs)
            {
                Logger.LogError("XpManager: PlayerData is null!", Logger.LogCategory.General);
            }
            return false;
        }

        return true;
    }

    #endregion

    #region Save Management

    /// <summary>
    /// Programmer une sauvegarde differee pour optimiser les performances
    /// </summary>
    private void ScheduleSave()
    {
        if (!pendingSave)
        {
            pendingSave = true;
            Invoke(nameof(SaveGameData), saveDelay);
        }
    }

    /// <summary>
    /// Sauvegarder les donnees du jeu
    /// </summary>
    private void SaveGameData()
    {
        pendingSave = false;

        if (DataManager.Instance != null)
        {
            DataManager.Instance.SaveGame();

            if (enableDebugLogs)
            {
                Logger.LogInfo("XpManager: Game data saved", Logger.LogCategory.General);
            }
        }
    }

    /// <summary>
    /// Forcer une sauvegarde immediate (pour les cas critiques)
    /// </summary>
    public void ForceSave()
    {
        if (pendingSave)
        {
            CancelInvoke(nameof(SaveGameData));
        }
        SaveGameData();
    }

    #endregion
}

/// <summary>
/// Resultat de l'application d'une recompense d'XP
/// </summary>
public struct XpApplyResult
{
    public bool MainSkillLeveledUp;
    public bool SubSkillLeveledUp;

    public XpApplyResult(bool mainSkillLeveledUp, bool subSkillLeveledUp)
    {
        MainSkillLeveledUp = mainSkillLeveledUp;
        SubSkillLeveledUp = subSkillLeveledUp;
    }

    public bool AnyLevelUp => MainSkillLeveledUp || SubSkillLeveledUp;
}