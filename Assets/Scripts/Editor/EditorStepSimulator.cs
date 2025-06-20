// Purpose: Simulateur de pas pour tester le jeu dans l'editeur Unity
// Filepath: Assets/Scripts/Editor/EditorStepSimulator.cs
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class EditorStepSimulator : EditorWindow
{
    [Header("Simulation Settings")]
    [SerializeField] private int stepsToAdd = 100;
    [SerializeField] private bool autoSimulation = false;
    [SerializeField] private float autoSimulationInterval = 2f;
    [SerializeField] private int autoStepsPerInterval = 10;

    [Header("Quick Actions")]
    [SerializeField] private int quickSteps1 = 50;
    [SerializeField] private int quickSteps2 = 200;
    [SerializeField] private int quickSteps3 = 1000;

    [Header("Status")]
    [SerializeField] private long currentTotalSteps = 0;
    [SerializeField] private long currentDailySteps = 0;

    private double lastAutoSimTime = 0;
    private DataManager dataManager;

    [MenuItem("StepQuest/Step Simulator")]
    public static void ShowWindow()
    {
        EditorStepSimulator window = GetWindow<EditorStepSimulator>();
        window.titleContent = new GUIContent("Step Simulator");
        window.Show();
    }

    void OnEnable()
    {
        EditorApplication.update += UpdateSimulation;
    }

    void OnDisable()
    {
        EditorApplication.update -= UpdateSimulation;
    }

    void UpdateSimulation()
    {
        if (!autoSimulation) return;
        if (!Application.isPlaying) return;

        if (EditorApplication.timeSinceStartup - lastAutoSimTime >= autoSimulationInterval)
        {
            if (AddStepsInternal(autoStepsPerInterval))
            {
                lastAutoSimTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }
    }

    void OnGUI()
    {
        GUILayout.Label("🦶 Step Quest - Simulateur de Pas", EditorStyles.boldLabel);
        GUILayout.Space(10);

        UpdateStatus();
        DrawStatusSection();
        GUILayout.Space(10);
        DrawManualControls();
        GUILayout.Space(10);
        DrawAutoSimulation();
        GUILayout.Space(10);
        DrawQuickActions();
        GUILayout.Space(10);
        DrawDebugSection();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("⚠️ Lancez le jeu (Play) pour utiliser le simulateur", MessageType.Warning);
        }
    }

    void UpdateStatus()
    {
        if (Application.isPlaying)
        {
            dataManager = DataManager.Instance;

            if (dataManager?.PlayerData != null)
            {
                currentTotalSteps = dataManager.PlayerData.TotalSteps;
                currentDailySteps = dataManager.PlayerData.DailySteps;
            }
        }
    }

    void DrawStatusSection()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("📊 Status Actuel", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Pas Totaux", currentTotalSteps.ToString("N0"));
        EditorGUILayout.LabelField("Pas Quotidiens", currentDailySteps.ToString("N0"));

        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            var progress = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);
            var required = dataManager.PlayerData.TravelRequiredSteps;
            var progressPercent = required > 0 ? (float)progress / required * 100f : 0f;
            EditorGUILayout.LabelField("🚶 Voyage", $"{progress}/{required} pas ({progressPercent:F1}%) vers {dataManager.PlayerData.TravelDestinationId}");
        }

        EditorGUILayout.EndVertical();
    }

    void DrawManualControls()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("✋ Contrôle Manuel", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        stepsToAdd = EditorGUILayout.IntField("Pas a ajouter", stepsToAdd);
        if (GUILayout.Button("Ajouter", GUILayout.Width(80)))
        {
            AddStepsInternal(stepsToAdd);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    void DrawAutoSimulation()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🤖 Simulation Automatique", EditorStyles.boldLabel);

        autoSimulation = EditorGUILayout.Toggle("Activer Auto-Sim", autoSimulation);

        if (autoSimulation)
        {
            autoSimulationInterval = EditorGUILayout.Slider("Intervalle (sec)", autoSimulationInterval, 0.5f, 10f);
            autoStepsPerInterval = EditorGUILayout.IntSlider("Pas par intervalle", autoStepsPerInterval, 1, 50);

            EditorGUILayout.HelpBox($"Ajoute {autoStepsPerInterval} pas toutes les {autoSimulationInterval:F1} secondes", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    void DrawQuickActions()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("⚡ Actions Rapides", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        quickSteps1 = EditorGUILayout.IntField(quickSteps1, GUILayout.Width(60));
        if (GUILayout.Button($"+{quickSteps1}"))
        {
            AddStepsInternal(quickSteps1);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        quickSteps2 = EditorGUILayout.IntField(quickSteps2, GUILayout.Width(60));
        if (GUILayout.Button($"+{quickSteps2}"))
        {
            AddStepsInternal(quickSteps2);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        quickSteps3 = EditorGUILayout.IntField(quickSteps3, GUILayout.Width(60));
        if (GUILayout.Button($"+{quickSteps3}"))
        {
            AddStepsInternal(quickSteps3);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    void DrawDebugSection()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("🛠️ Debug & Reset", EditorStyles.boldLabel);

        if (GUILayout.Button("Simuler Nouveau Jour"))
        {
            SimulateNewDay();
        }

        if (GUILayout.Button("Reset Pas Quotidiens"))
        {
            ResetDailySteps();
        }

        if (GUILayout.Button("Clear Travel State"))
        {
            ClearTravelState();
        }

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("⚠️ Reset Complet"))
        {
            if (EditorUtility.DisplayDialog("Confirmation", "Voulez-vous vraiment remettre tous les pas a zero ?", "Oui", "Annuler"))
            {
                ResetAllSteps();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndVertical();
    }

    // SIMPLIFIÉ: Logique d'ajout de pas plus directe
    bool AddStepsInternal(int steps)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Simulator: Lancez le jeu pour simuler les pas!");
            return false;
        }

        if (steps <= 0)
        {
            Debug.LogWarning("Simulator: Nombre de pas doit être positif!");
            return false;
        }

        if (dataManager?.PlayerData == null)
        {
            Debug.LogWarning("Simulator: DataManager ou PlayerData non trouve!");
            return false;
        }

        long oldTotal = dataManager.PlayerData.TotalSteps;
        long oldDaily = dataManager.PlayerData.DailySteps;

        // DIRECT: Ajouter les pas sans passer par des systèmes compliques
        dataManager.PlayerData.TotalPlayerSteps += steps;
        dataManager.PlayerData.DailySteps += steps;

        // Mettre a jour les timestamps
        long nowMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        dataManager.PlayerData.LastStepsChangeEpochMs = nowMs;
        dataManager.PlayerData.LastSyncEpochMs = nowMs;
        dataManager.PlayerData.LastPauseEpochMs = nowMs;

        // Sauvegarder immediatement
        dataManager.SaveGame();

        // Forcer la mise a jour de l'UI
        UpdateStatus();
        Repaint();

        var uiManager = UIManager.Instance;
        if (uiManager != null)
        {
            uiManager.ForceUIUpdate();
        }

        Debug.Log($"Simulator: Ajoute {steps} pas. Total: {oldTotal} → {dataManager.PlayerData.TotalSteps}, Quotidien: {oldDaily} → {dataManager.PlayerData.DailySteps}");
        return true;
    }

    void SimulateNewDay()
    {
        if (!Application.isPlaying || dataManager?.PlayerData == null)
        {
            Debug.LogWarning("Simulator: Impossible de simuler un nouveau jour!");
            return;
        }

        string tomorrow = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
        dataManager.PlayerData.LastDailyResetDate = tomorrow;
        dataManager.PlayerData.DailySteps = 0;
        dataManager.SaveGame();

        UpdateStatus();
        Repaint();

        Debug.Log($"Simulator: Nouveau jour simule! Date: {tomorrow}");
    }

    void ResetDailySteps()
    {
        if (!Application.isPlaying || dataManager?.PlayerData == null)
        {
            Debug.LogWarning("Simulator: Impossible de reset les pas quotidiens!");
            return;
        }

        dataManager.PlayerData.DailySteps = 0;
        dataManager.SaveGame();

        UpdateStatus();
        Repaint();

        Debug.Log("Simulator: Pas quotidiens remis a zero");
    }

    void ClearTravelState()
    {
        if (!Application.isPlaying || dataManager?.PlayerData == null)
        {
            Debug.LogWarning("Simulator: Impossible de clear travel state!");
            return;
        }

        dataManager.PlayerData.TravelDestinationId = null;
        dataManager.PlayerData.TravelStartSteps = 0;
        dataManager.PlayerData.TravelRequiredSteps = 0;
        dataManager.SaveGame();

        UpdateStatus();
        Repaint();

        Debug.Log("Simulator: Travel state cleared");
    }

    void ResetAllSteps()
    {
        if (!Application.isPlaying || dataManager?.PlayerData == null)
        {
            Debug.LogWarning("Simulator: Impossible de reset tous les pas!");
            return;
        }

        dataManager.PlayerData.TotalPlayerSteps = 0;
        dataManager.PlayerData.DailySteps = 0;
        dataManager.PlayerData.LastSyncEpochMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        dataManager.PlayerData.TravelDestinationId = null;
        dataManager.PlayerData.TravelStartSteps = 0;
        dataManager.PlayerData.TravelRequiredSteps = 0;
        dataManager.SaveGame();

        UpdateStatus();
        Repaint();

        Debug.Log("Simulator: Tous les pas remis a zero");
    }
}
#endif