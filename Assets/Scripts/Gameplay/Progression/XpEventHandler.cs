// Purpose: Handles XP rewards when activities complete
// Filepath: Assets/Scripts/Gameplay/Progression/XpEventHandler.cs
using ActivityEvents;
using UnityEngine;

public class XpEventHandler : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Performance")]
    [SerializeField] private bool batchLevelUpEvents = true; // Grouper les evenements de level up

    #region Unity Lifecycle

    void Start()
    {
        InitializeEventSubscriptions();
    }

    void OnDestroy()
    {
        CleanupEventSubscriptions();
    }

    #endregion

    #region Event Management

    /// <summary>
    /// Initialiser les abonnements aux evenements de maniere securisee
    /// </summary>
    private void InitializeEventSubscriptions()
    {
        if (!ValidateEventBus()) return;

        try
        {
            EventBus.Subscribe<ActivityTickEvent>(OnActivityTick);

            if (enableDebugLogs)
            {
                Logger.LogInfo("XpEventHandler: Successfully subscribed to ActivityTickEvent", Logger.LogCategory.General);
            }
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"XpEventHandler: Failed to subscribe to events - {ex.Message}", Logger.LogCategory.General);
        }
    }

    /// <summary>
    /// Nettoyer les abonnements aux evenements pour eviter les fuites memoire
    /// </summary>
    private void CleanupEventSubscriptions()
    {
        if (!ValidateEventBus()) return;

        try
        {
            EventBus.Unsubscribe<ActivityTickEvent>(OnActivityTick);

            if (enableDebugLogs)
            {
                Logger.LogInfo("XpEventHandler: Successfully unsubscribed from ActivityTickEvent", Logger.LogCategory.General);
            }
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"XpEventHandler: Failed to unsubscribe from events - {ex.Message}", Logger.LogCategory.General);
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Gerer les ticks d'activite et distribuer l'XP de maniere optimisee
    /// </summary>
    private void OnActivityTick(ActivityTickEvent eventData)
    {
        // Validation des donnees d'entree
        if (!ValidateActivityEvent(eventData)) return;

        // Validation du systeme XP
        if (!ValidateXpSystem()) return;

        // Calculer la recompense d'XP
        XPReward xpReward = CalculateXpReward(eventData);
        if (!xpReward.HasAnyXP())
        {
            if (enableDebugLogs)
            {
                Logger.LogInfo($"XpEventHandler: No XP configured for {eventData.Variant.VariantName}", Logger.LogCategory.General);
            }
            return;
        }

        // Appliquer la recompense d'XP de maniere optimisee
        ApplyXpReward(xpReward, eventData);
    }

    #endregion

    #region XP Processing

    /// <summary>
    /// Calculer la recompense d'XP basee sur le type d'activite
    /// </summary>
    private XPReward CalculateXpReward(ActivityTickEvent eventData)
    {
        if (eventData.Variant.IsTimeBased)
        {
            return XpManager.Instance.CalculateTimeBasedXP(eventData.TicksCompleted, eventData.Variant);
        }
        else
        {
            return XpManager.Instance.CalculateStepBasedXP(eventData.TicksCompleted, eventData.Variant);
        }
    }

    /// <summary>
    /// Appliquer la recompense d'XP et gerer les evenements de level up
    /// </summary>
    private void ApplyXpReward(XPReward xpReward, ActivityTickEvent eventData)
    {
        // Utiliser la methode optimisee de XpManager
        XpApplyResult result = XpManager.Instance.ApplyXPReward(xpReward);

        // Logging des resultats
        if (enableDebugLogs)
        {
            LogXpRewardResult(xpReward, eventData, result);
        }

        // Publier les evenements de level up si necessaire
        if (result.AnyLevelUp)
        {
            PublishLevelUpEvents(xpReward, result);
        }
    }

    /// <summary>
    /// Publier les evenements de level up de maniere optimisee
    /// </summary>
    private void PublishLevelUpEvents(XPReward xpReward, XpApplyResult result)
    {
        if (batchLevelUpEvents)
        {
            // Publier un seul evenement groupe pour de meilleures performances
            PublishBatchedLevelUpEvent(xpReward, result);
        }
        else
        {
            // Publier des evenements individuels
            PublishIndividualLevelUpEvents(xpReward, result);
        }
    }

    /// <summary>
    /// Publier un evenement de level up groupe
    /// </summary>
    private void PublishBatchedLevelUpEvent(XPReward xpReward, XpApplyResult result)
    {
        var levelUpData = new BatchedLevelUpEventData();

        if (result.MainSkillLeveledUp)
        {
            var mainSkill = XpManager.Instance.GetPlayerSkill(xpReward.MainSkillId);
            levelUpData.AddLevelUp(xpReward.MainSkillId, mainSkill.Level, false);
        }

        if (result.SubSkillLeveledUp)
        {
            var subSkill = XpManager.Instance.GetPlayerSubSkill(xpReward.SubSkillId);
            levelUpData.AddLevelUp(xpReward.SubSkillId, subSkill.Level, true);
        }

        // Publier l'evenement groupe (a implementer selon votre systeme d'evenements)
        // EventBus.Publish(new BatchedSkillLevelUpEvent(levelUpData));

        if (enableDebugLogs)
        {
            Logger.LogInfo($"🎉 Level up batch published: {levelUpData}", Logger.LogCategory.General);
        }
    }

    /// <summary>
    /// Publier des evenements de level up individuels
    /// </summary>
    private void PublishIndividualLevelUpEvents(XPReward xpReward, XpApplyResult result)
    {
        if (result.MainSkillLeveledUp)
        {
            var mainSkill = XpManager.Instance.GetPlayerSkill(xpReward.MainSkillId);
            PublishLevelUpEvent(xpReward.MainSkillId, mainSkill.Level, false);
        }

        if (result.SubSkillLeveledUp)
        {
            var subSkill = XpManager.Instance.GetPlayerSubSkill(xpReward.SubSkillId);
            PublishLevelUpEvent(xpReward.SubSkillId, subSkill.Level, true);
        }
    }

    /// <summary>
    /// Publier un evenement de level up individuel
    /// </summary>
    private void PublishLevelUpEvent(string skillId, int newLevel, bool isSubSkill)
    {
        // Vous pouvez creer un SkillLevelUpEvent si vous voulez des animations/notifications
        // EventBus.Publish(new SkillLevelUpEvent(skillId, newLevel, isSubSkill));

        if (enableDebugLogs)
        {
            string skillType = isSubSkill ? "SubSkill" : "Skill";
            Logger.LogInfo($"🎉 {skillType} {skillId} leveled up to {newLevel}!", Logger.LogCategory.General);
        }
    }

    #endregion

    #region Validation

    /// <summary>
    /// Valider l'evenement d'activite
    /// </summary>
    private bool ValidateActivityEvent(ActivityTickEvent eventData)
    {
        if (eventData == null)
        {
            if (enableDebugLogs)
            {
                Logger.LogWarning("XpEventHandler: ActivityTickEvent is null", Logger.LogCategory.General);
            }
            return false;
        }

        if (eventData.Variant == null)
        {
            if (enableDebugLogs)
            {
                Logger.LogWarning("XpEventHandler: ActivityTickEvent has null variant", Logger.LogCategory.General);
            }
            return false;
        }

        if (eventData.TicksCompleted <= 0)
        {
            if (enableDebugLogs)
            {
                Logger.LogWarning($"XpEventHandler: ActivityTickEvent has invalid ticks completed: {eventData.TicksCompleted}", Logger.LogCategory.General);
            }
            return false;
        }

        return true;
    }

    /// <summary>
    /// Valider que le systeme XP est disponible
    /// </summary>
    private bool ValidateXpSystem()
    {
        if (XpManager.Instance == null)
        {
            Logger.LogError("XpEventHandler: XpManager.Instance is null! Make sure XpManager is in the scene.", Logger.LogCategory.General);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Valider que l'EventBus est disponible
    /// </summary>
    private bool ValidateEventBus()
    {
        // EventBus est probablement une classe statique, donc pas besoin de verifier null
        // Si votre EventBus a une methode pour verifier s'il est initialise, utilisez-la ici
        // Par exemple : return EventBus.IsInitialized;

        // Pour l'instant, on assume qu'il est toujours disponible
        return true;
    }

    #endregion

    #region Logging

    /// <summary>
    /// Logger les resultats de la recompense d'XP
    /// </summary>
    private void LogXpRewardResult(XPReward xpReward, ActivityTickEvent eventData, XpApplyResult result)
    {
        string logMessage = $"XpEventHandler: {eventData.Variant.VariantName} completed {eventData.TicksCompleted} ticks → ";
        logMessage += $"{xpReward.MainSkillId} +{xpReward.MainSkillXP}XP, {xpReward.SubSkillId} +{xpReward.SubSkillXP}XP";

        if (result.AnyLevelUp)
        {
            logMessage += " 🎉 LEVEL UP!";
        }

        Logger.LogInfo(logMessage, Logger.LogCategory.General);
    }

    #endregion

    #region Testing

    /// <summary>
    /// Methode publique pour tester le systeme d'XP (version amelioree)
    /// </summary>
    [ContextMenu("Test XP System")]
    public void TestXPSystem()
    {
        if (!ValidateXpSystem())
        {
            Logger.LogError("XpManager not found!", Logger.LogCategory.XpLog);
            return;
        }

        // Creer un evenement de test avec des donnees plus realistes
        var testVariant = ScriptableObject.CreateInstance<ActivityVariant>();
        testVariant.VariantName = "Test Mining Iron";
        testVariant.ParentActivityID = "Mining";
        testVariant.MainSkillXPPerTick = 15;
        testVariant.SubSkillXPPerTick = 8;
        testVariant.IsTimeBased = false;

        var testActivity = new ActivityData
        {
            ActivityId = "Mining",
            VariantId = "Test_Mining_Iron"
        };

        var testEvent = new ActivityTickEvent(testActivity, testVariant, 5); // 5 ticks
        OnActivityTick(testEvent);

        Logger.LogInfo("Test XP event processed! Check console for results.", Logger.LogCategory.XpLog);
    }

    /// <summary>
    /// Tester le systeme avec plusieurs evenements pour simuler une session de jeu
    /// </summary>
    [ContextMenu("Test XP System - Multiple Events")]
    public void TestMultipleXPEvents()
    {
        if (!ValidateXpSystem())
        {
            Logger.LogError("XpManager not found!", Logger.LogCategory.XpLog);
            return;
        }

        Logger.LogInfo("Testing multiple XP events...", Logger.LogCategory.XpLog);

        // Test Mining
        TestSingleActivity("Mining Iron", "Mining", 12, 6, false, 3);

        // Test Crafting
        TestSingleActivity("Crafting Sword", "Crafting", 20, 10, true, 2);

        // Test Gathering
        TestSingleActivity("Gathering Wood", "Gathering", 8, 4, false, 7);

        Logger.LogInfo("Multiple XP events test completed!", Logger.LogCategory.XpLog);
    }

    private void TestSingleActivity(string variantName, string activityId, int mainXP, int subXP, bool isTimeBased, int ticks)
    {
        var testVariant = ScriptableObject.CreateInstance<ActivityVariant>();
        testVariant.VariantName = variantName;
        testVariant.ParentActivityID = activityId;
        testVariant.MainSkillXPPerTick = mainXP;
        testVariant.SubSkillXPPerTick = subXP;
        testVariant.IsTimeBased = isTimeBased;

        var testActivity = new ActivityData
        {
            ActivityId = activityId,
            VariantId = variantName.Replace(" ", "_")
        };

        var testEvent = new ActivityTickEvent(testActivity, testVariant, ticks);
        OnActivityTick(testEvent);
    }

    #endregion
}

/// <summary>
/// Donnees pour les evenements de level up groupes
/// </summary>
public class BatchedLevelUpEventData
{
    public System.Collections.Generic.List<LevelUpInfo> LevelUps = new System.Collections.Generic.List<LevelUpInfo>();

    public void AddLevelUp(string skillId, int newLevel, bool isSubSkill)
    {
        LevelUps.Add(new LevelUpInfo(skillId, newLevel, isSubSkill));
    }

    public override string ToString()
    {
        return $"BatchedLevelUp: {LevelUps.Count} skills leveled up";
    }
}

/// <summary>
/// Information sur une montee de niveau
/// </summary>
[System.Serializable]
public struct LevelUpInfo
{
    public string SkillId;
    public int NewLevel;
    public bool IsSubSkill;

    public LevelUpInfo(string skillId, int newLevel, bool isSubSkill)
    {
        SkillId = skillId;
        NewLevel = newLevel;
        IsSubSkill = isSubSkill;
    }
}