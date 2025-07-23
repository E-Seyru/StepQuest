// Purpose: Handles XP rewards when activities complete
// Filepath: Assets/Scripts/Systems/XpEventHandler.cs
using ActivityEvents;
using UnityEngine;

public class XpEventHandler : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    void Start()
    {
        // S'abonner aux événements d'activité
        EventBus.Subscribe<ActivityTickEvent>(OnActivityTick);

        if (enableDebugLogs)
        {
            Logger.LogInfo("XpEventHandler: Subscribed to ActivityTickEvent", Logger.LogCategory.General);
        }
    }

    void OnDestroy()
    {
        // Se désabonner pour éviter les fuites mémoire
        EventBus.Unsubscribe<ActivityTickEvent>(OnActivityTick);

        if (enableDebugLogs)
        {
            Logger.LogInfo("XpEventHandler: Unsubscribed from ActivityTickEvent", Logger.LogCategory.General);
        }
    }

    /// <summary>
    /// Gérer les ticks d'activité et donner de l'XP
    /// </summary>
    private void OnActivityTick(ActivityTickEvent eventData)
    {
        // Vérifier que nous avons les données nécessaires
        if (eventData.Variant == null || eventData.TicksCompleted <= 0)
        {
            if (enableDebugLogs)
            {
                Logger.LogWarning("XpEventHandler: ActivityTickEvent has null variant or no ticks completed", Logger.LogCategory.General);
            }
            return;
        }

        // Vérifier que XpManager existe
        if (XpManager.Instance == null)
        {
            Logger.LogError("XpEventHandler: XpManager.Instance is null! Make sure XpManager is in the scene.", Logger.LogCategory.General);
            return;
        }

        // Calculer l'XP gagnée selon le type d'activité
        XPReward xpReward;

        if (eventData.Variant.IsTimeBased)
        {
            // Activité time-based (crafting)
            xpReward = XpManager.Instance.CalculateTimeBasedXP(eventData.TicksCompleted, eventData.Variant);
        }
        else
        {
            // Activité step-based (gathering)
            xpReward = XpManager.Instance.CalculateStepBasedXP(eventData.TicksCompleted, eventData.Variant);
        }

        // Vérifier qu'on a de l'XP à donner
        if (!xpReward.HasAnyXP())
        {
            if (enableDebugLogs)
            {
                Logger.LogInfo($"XpEventHandler: No XP configured for {eventData.Variant.VariantName}", Logger.LogCategory.General);
            }
            return;
        }

        // Appliquer l'XP au joueur
        bool mainSkillLeveledUp = false;
        bool subSkillLeveledUp = false;

        if (xpReward.MainSkillXP > 0)
        {
            mainSkillLeveledUp = XpManager.Instance.AddSkillXP(xpReward.MainSkillId, xpReward.MainSkillXP);
        }

        if (xpReward.SubSkillXP > 0)
        {
            subSkillLeveledUp = XpManager.Instance.AddSubSkillXP(xpReward.SubSkillId, xpReward.SubSkillXP);
        }

        // Log des résultats
        if (enableDebugLogs)
        {
            string logMessage = $"XpEventHandler: {eventData.Variant.VariantName} completed {eventData.TicksCompleted} ticks → ";
            logMessage += $"{xpReward.MainSkillId} +{xpReward.MainSkillXP}XP, {xpReward.SubSkillId} +{xpReward.SubSkillXP}XP";

            if (mainSkillLeveledUp || subSkillLeveledUp)
            {
                logMessage += " 🎉 LEVEL UP!";
            }

            Logger.LogInfo(logMessage, Logger.LogCategory.General);
        }

        // Optionnel : Publier un événement de level up si nécessaire
        if (mainSkillLeveledUp)
        {
            PublishLevelUpEvent(xpReward.MainSkillId, XpManager.Instance.GetPlayerSkill(xpReward.MainSkillId).Level, false);
        }

        if (subSkillLeveledUp)
        {
            PublishLevelUpEvent(xpReward.SubSkillId, XpManager.Instance.GetPlayerSubSkill(xpReward.SubSkillId).Level, true);
        }

        // Sauvegarder les données du joueur
        DataManager.Instance?.SaveGame();
    }

    /// <summary>
    /// Publier un événement de level up (optionnel, pour l'UI par exemple)
    /// </summary>
    private void PublishLevelUpEvent(string skillId, int newLevel, bool isSubSkill)
    {
        // Vous pouvez créer un SkillLevelUpEvent si vous voulez des animations/notifications
        // EventBus.Publish(new SkillLevelUpEvent(skillId, newLevel, isSubSkill));

        Logger.LogInfo($"🎉 {skillId} leveled up to {newLevel}!", Logger.LogCategory.General);
    }

    /// <summary>
    /// Méthode publique pour tester le système d'XP
    /// </summary>
    [ContextMenu("Test XP System")]
    public void TestXPSystem()
    {
        if (XpManager.Instance == null)
        {
            Debug.LogError("XpManager not found!");
            return;
        }

        // Créer un événement de test
        var testVariant = ScriptableObject.CreateInstance<ActivityVariant>();
        testVariant.VariantName = "Test Mining";
        testVariant.ParentActivityID = "Mining";
        testVariant.MainSkillXPPerTick = 10;
        testVariant.SubSkillXPPerTick = 5;
        testVariant.IsTimeBased = false;

        var testActivity = new ActivityData
        {
            ActivityId = "Mining",
            VariantId = "Test_Mining"
        };

        var testEvent = new ActivityTickEvent(testActivity, testVariant, 3); // 3 ticks
        OnActivityTick(testEvent);

        Debug.Log("Test XP event processed! Check console for results.");
    }
}