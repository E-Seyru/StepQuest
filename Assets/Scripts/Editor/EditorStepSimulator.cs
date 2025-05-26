// Purpose: Simulateur de pas pour tester le jeu dans l'éditeur Unity
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
    [SerializeField] private float autoSimulationInterval = 2f; // secondes
    [SerializeField] private int autoStepsPerInterval = 10;

    [Header("Quick Actions")]
    [SerializeField] private int quickSteps1 = 50;
    [SerializeField] private int quickSteps2 = 200;
    [SerializeField] private int quickSteps3 = 1000;

    [Header("Status")]
    [SerializeField] private long currentTotalSteps = 0;
    [SerializeField] private long currentDailySteps = 0;

    private double lastAutoSimTime = 0;
    private StepManager stepManager;
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
        // S'assurer que la simulation continue même quand la fenêtre n'est pas focusée
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
            AddSteps(autoStepsPerInterval);
            lastAutoSimTime = EditorApplication.timeSinceStartup;

            // NOUVEAU: Forcer le redessin pour la simulation auto aussi
            Repaint();
        }
    }

    void OnGUI()
    {
        GUILayout.Label("🦶 Step Quest - Simulateur de Pas", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Status actuel
        UpdateStatus();
        DrawStatusSection();

        GUILayout.Space(10);

        // Contrôles manuels
        DrawManualControls();

        GUILayout.Space(10);

        // Simulation automatique
        DrawAutoSimulation();

        GUILayout.Space(10);

        // Actions rapides
        DrawQuickActions();

        GUILayout.Space(10);

        // Debug et reset
        DrawDebugSection();

        // Warning si pas en mode Play
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("⚠️ Lancez le jeu (Play) pour utiliser le simulateur", MessageType.Warning);
        }
    }

    void UpdateStatus()
    {
        if (Application.isPlaying)
        {
            stepManager = StepManager.Instance;
            dataManager = DataManager.Instance;

            // AMÉLIORÉ: Récupérer directement depuis DataManager pour avoir les valeurs instantanées
            if (dataManager?.PlayerData != null)
            {
                currentTotalSteps = dataManager.PlayerData.TotalSteps;
                currentDailySteps = dataManager.PlayerData.DailySteps;
            }
            else if (stepManager != null)
            {
                // Fallback vers StepManager si DataManager pas disponible
                currentTotalSteps = stepManager.TotalSteps;
                currentDailySteps = stepManager.DailySteps;
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
            EditorGUILayout.LabelField("🚶 Voyage", $"{progress}/{required} pas vers {dataManager.PlayerData.TravelDestinationId}");
        }

        EditorGUILayout.EndVertical();
    }

    void DrawManualControls()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("✋ Contrôle Manuel", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        stepsToAdd = EditorGUILayout.IntField("Pas à ajouter", stepsToAdd);
        if (GUILayout.Button("Ajouter", GUILayout.Width(80)))
        {
            AddSteps(stepsToAdd);
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
            AddSteps(quickSteps1);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        quickSteps2 = EditorGUILayout.IntField(quickSteps2, GUILayout.Width(60));
        if (GUILayout.Button($"+{quickSteps2}"))
        {
            AddSteps(quickSteps2);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        quickSteps3 = EditorGUILayout.IntField(quickSteps3, GUILayout.Width(60));
        if (GUILayout.Button($"+{quickSteps3}"))
        {
            AddSteps(quickSteps3);
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

        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("⚠️ Reset Complet"))
        {
            if (EditorUtility.DisplayDialog("Confirmation", "Voulez-vous vraiment remettre tous les pas à zéro ?", "Oui", "Annuler"))
            {
                ResetAllSteps();
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndVertical();
    }

    void AddSteps(int steps)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Simulator: Lancez le jeu pour simuler les pas!");
            return;
        }

        if (steps <= 0)
        {
            Debug.LogWarning("Simulator: Nombre de pas doit être positif!");
            return;
        }

        if (dataManager?.PlayerData == null)
        {
            Debug.LogWarning("Simulator: DataManager ou PlayerData non trouvé!");
            return;
        }

        // AMÉLIORÉ: Ajouter directement aux pas totaux et quotidiens
        long oldTotal = dataManager.PlayerData.TotalSteps;
        long oldDaily = dataManager.PlayerData.DailySteps;

        dataManager.PlayerData.TotalSteps += steps;
        dataManager.PlayerData.DailySteps += steps;

        // Mettre à jour les timestamps
        long nowMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        dataManager.PlayerData.LastStepsChangeEpochMs = nowMs;
        dataManager.PlayerData.LastSyncEpochMs = nowMs;
        dataManager.PlayerData.LastPauseEpochMs = nowMs;

        // Sauvegarder
        dataManager.SaveGame();

        // NOUVEAU: Forcer une mise à jour immédiate du Status et de l'interface
        UpdateStatus();
        Repaint(); // Force le redessin de la fenêtre du simulateur

        // NOUVEAU: Forcer aussi une mise à jour de l'UIManager si disponible
        var uiManager = UIManager.Instance;
        if (uiManager != null)
        {
            uiManager.ForceUIUpdate();
        }

        Debug.Log($"Simulator: Ajouté {steps} pas. Total: {oldTotal} → {dataManager.PlayerData.TotalSteps}, Quotidien: {oldDaily} → {dataManager.PlayerData.DailySteps}");
    }

    void SimulateNewDay()
    {
        if (!Application.isPlaying || dataManager?.PlayerData == null)
        {
            Debug.LogWarning("Simulator: Impossible de simuler un nouveau jour!");
            return;
        }

        // Changer la date de reset pour forcer un nouveau jour
        string tomorrow = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
        dataManager.PlayerData.LastDailyResetDate = tomorrow;
        dataManager.PlayerData.DailySteps = 0;
        dataManager.SaveGame();

        // NOUVEAU: Mise à jour immédiate
        UpdateStatus();
        Repaint();

        Debug.Log($"Simulator: Nouveau jour simulé! Date: {tomorrow}");
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

        // NOUVEAU: Mise à jour immédiate
        UpdateStatus();
        Repaint();

        Debug.Log("Simulator: Pas quotidiens remis à zéro");
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
        dataManager.SaveGame();

        // NOUVEAU: Mise à jour immédiate
        UpdateStatus();
        Repaint();

        Debug.Log("Simulator: Tous les pas remis à zéro");
    }
}
#endif