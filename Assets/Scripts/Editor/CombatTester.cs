// Purpose: Quick test button to start combat from Editor with live combat log
// Filepath: Assets/Scripts/Editor/CombatTester.cs

#if UNITY_EDITOR
using CombatEvents;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CombatTester : EditorWindow
{
    private EnemyDefinition selectedEnemy;
    private Vector2 logScrollPosition;
    private List<string> combatLog = new List<string>();
    private const int MAX_LOG_LINES = 50;
    private bool isSubscribed = false;

    [MenuItem("StepQuest/Combat/Combat Tester")]
    public static void ShowWindow()
    {
        GetWindow<CombatTester>("Combat Tester");
    }

    private void OnEnable()
    {
        SubscribeToEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (isSubscribed) return;

        EventBus.Subscribe<CombatStartedEvent>(OnCombatStarted);
        EventBus.Subscribe<CombatEndedEvent>(OnCombatEnded);
        EventBus.Subscribe<CombatFledEvent>(OnCombatFled);
        EventBus.Subscribe<CombatAbilityUsedEvent>(OnAbilityUsed);
        EventBus.Subscribe<StatusEffectTickEvent>(OnStatusEffectTick);
        isSubscribed = true;
    }

    private void UnsubscribeFromEvents()
    {
        if (!isSubscribed) return;

        EventBus.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
        EventBus.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
        EventBus.Unsubscribe<CombatFledEvent>(OnCombatFled);
        EventBus.Unsubscribe<CombatAbilityUsedEvent>(OnAbilityUsed);
        EventBus.Unsubscribe<StatusEffectTickEvent>(OnStatusEffectTick);
        isSubscribed = false;
    }

    // === EVENT HANDLERS ===

    private void OnCombatStarted(CombatStartedEvent e)
    {
        combatLog.Clear();
        AddLog($"=== Combat Started vs {e.Enemy?.GetDisplayName()} ===");
    }

    private void OnCombatEnded(CombatEndedEvent e)
    {
        string result = e.PlayerWon ? "VICTORY!" : "DEFEAT...";
        AddLog($"=== {result} ===");
        if (e.PlayerWon && e.ExperienceGained > 0)
        {
            AddLog($"+{e.ExperienceGained} XP");
        }
    }

    private void OnCombatFled(CombatFledEvent e)
    {
        AddLog("=== FLED ===");
    }

    private void OnAbilityUsed(CombatAbilityUsedEvent e)
    {
        string source = e.IsPlayerAbility ? "Player" : "Enemy";
        string abilityName = e.Ability?.GetDisplayName() ?? "?";

        string effects = "";
        if (e.DamageDealt > 0) effects += $" -{e.DamageDealt:F0} dmg";
        if (e.HealingDone > 0) effects += $" +{e.HealingDone:F0} heal";
        if (e.ShieldAdded > 0) effects += $" +{e.ShieldAdded:F0} shield";

        AddLog($"{source}: {abilityName}{effects}");
    }

    private void OnStatusEffectTick(StatusEffectTickEvent e)
    {
        string target = e.IsTargetPlayer ? "Player" : "Enemy";
        string effectName = e.Effect?.GetDisplayName() ?? "Effect";
        AddLog($"{target} took {e.Value:F0} {effectName} damage");
    }

    private void AddLog(string message)
    {
        combatLog.Add($"[{Time.time:F1}s] {message}");

        // Trim old entries
        while (combatLog.Count > MAX_LOG_LINES)
        {
            combatLog.RemoveAt(0);
        }

        // Auto-scroll to bottom
        logScrollPosition.y = float.MaxValue;

        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Combat Tester", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to test combat!", MessageType.Warning);
            return;
        }

        // Make sure we're subscribed when entering play mode
        SubscribeToEvents();

        EditorGUILayout.Space(10);

        // Enemy selection
        selectedEnemy = (EnemyDefinition)EditorGUILayout.ObjectField(
            "Enemy to Fight",
            selectedEnemy,
            typeof(EnemyDefinition),
            false
        );

        EditorGUILayout.Space(10);

        // Combat status
        if (CombatManager.Instance != null)
        {
            EditorGUILayout.LabelField("Combat Active:", CombatManager.Instance.IsCombatActive.ToString());

            if (CombatManager.Instance.IsCombatActive)
            {
                EditorGUILayout.Space(5);

                // Live stats box
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.TextArea(CombatManager.Instance.GetDebugInfo(), GUILayout.Height(70));
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(5);
                if (GUILayout.Button("Flee Combat", GUILayout.Height(30)))
                {
                    CombatManager.Instance.FleeCombat();
                }
            }
            else
            {
                EditorGUILayout.Space(10);

                GUI.enabled = selectedEnemy != null;
                if (GUILayout.Button("Start Combat!", GUILayout.Height(40)))
                {
                    CombatManager.Instance.StartCombat(selectedEnemy);
                }
                GUI.enabled = true;

                if (selectedEnemy == null)
                {
                    EditorGUILayout.HelpBox("Select an enemy above to start combat", MessageType.Info);
                }
            }

            // Combat Log section
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Combat Log", EditorStyles.boldLabel);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                combatLog.Clear();
            }
            EditorGUILayout.EndHorizontal();

            // Scrollable log area
            logScrollPosition = EditorGUILayout.BeginScrollView(logScrollPosition, GUILayout.Height(150));

            GUIStyle logStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                richText = true,
                fontSize = 11
            };

            foreach (string logEntry in combatLog)
            {
                EditorGUILayout.LabelField(logEntry, logStyle);
            }

            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("CombatManager not found in scene!\nAdd a GameObject with CombatManager component.", MessageType.Error);
        }

        // Auto-refresh while playing
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}
#endif
