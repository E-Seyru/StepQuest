// Purpose: Diagnostic et réparation des données joueur corrompues
// Filepath: Assets/Scripts/Debug/PlayerDataDebugger.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class PlayerDataDebugger : EditorWindow
{
    [MenuItem("StepQuest/Debug Player Data")]
    public static void ShowWindow()
    {
        PlayerDataDebugger window = GetWindow<PlayerDataDebugger>();
        window.titleContent = new GUIContent("Player Data Debugger");
        window.Show();
    }

    private Vector2 scrollPosition;

    void OnGUI()
    {
        GUILayout.Label("Player Data Debugger & Repair Tool", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("WARNING: Cette tool nécessite le mode Play pour accéder aux données joueur", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("DIAGNOSTIQUER L'ETAT JOUEUR", GUILayout.Height(30)))
        {
            DiagnosePlayerState();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("REPARER L'ETAT DE VOYAGE", GUILayout.Height(30)))
        {
            RepairTravelState();
        }

        if (GUILayout.Button("REPARER L'ETAT D'ACTIVITE", GUILayout.Height(30)))
        {
            RepairActivityState();
        }

        if (GUILayout.Button("REPARATION COMPLETE", GUILayout.Height(30)))
        {
            FullRepair();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("FORCER SAUVEGARDE", GUILayout.Height(25)))
        {
            ForceSave();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Etat Actuel:", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DisplayCurrentState();
        EditorGUILayout.EndScrollView();
    }

    private void DiagnosePlayerState()
    {
        Debug.Log("=== DIAGNOSTIC COMPLET DE L'ETAT JOUEUR ===");

        var dataManager = DataManager.Instance;
        var mapManager = MapManager.Instance;
        var activityManager = ActivityManager.Instance;

        if (dataManager?.PlayerData == null)
        {
            Debug.LogError("ERROR: DataManager ou PlayerData non disponible !");
            return;
        }

        var playerData = dataManager.PlayerData;

        Debug.Log("POSITION & VOYAGE:");
        Debug.Log($"   CurrentLocationId: '{playerData.CurrentLocationId}'");
        Debug.Log($"   TravelDestinationId: '{playerData.TravelDestinationId}' {(string.IsNullOrEmpty(playerData.TravelDestinationId) ? "(NULL - OK)" : "(NON-NULL - PROBLEME POTENTIEL)")}");
        Debug.Log($"   TravelStartSteps: {playerData.TravelStartSteps}");
        Debug.Log($"   TravelRequiredSteps: {playerData.TravelRequiredSteps}");
        Debug.Log($"   IsCurrentlyTraveling(): {playerData.IsCurrentlyTraveling()} {(playerData.IsCurrentlyTraveling() ? "PROBLEME !" : "OK")}");

        if (playerData.IsCurrentlyTraveling())
        {
            long progress = playerData.GetTravelProgress(playerData.TotalSteps);
            Debug.Log($"   Progres voyage: {progress}/{playerData.TravelRequiredSteps} steps");
            Debug.Log($"   Voyage termine?: {playerData.IsTravelComplete(playerData.TotalSteps)}");
        }

        Debug.Log("ACTIVITE:");
        Debug.Log($"   HasActiveActivity(): {playerData.HasActiveActivity()} {(playerData.HasActiveActivity() ? "Activite en cours" : "Pas d'activite")}");

        if (playerData.HasActiveActivity())
        {
            Debug.Log($"   Activite: {playerData.CurrentActivity.ActivityId}/{playerData.CurrentActivity.VariantId}");
            Debug.Log($"   Location: {playerData.CurrentActivity.LocationId}");
            Debug.Log($"   Steps accumules: {playerData.CurrentActivity.AccumulatedSteps}");
        }

        Debug.Log("CONDITIONS DE BLOCAGE:");
        Debug.Log($"   CanStartActivity(): {activityManager?.CanStartActivity()} {(activityManager?.CanStartActivity() == false ? "BLOQUE !" : "OK")}");

        if (mapManager?.CurrentLocation != null)
        {
            string currentLoc = mapManager.CurrentLocation.LocationID;
            bool canTravelToVillage = mapManager.CanTravelTo("Village");
            Debug.Log($"   CanTravelTo('Village'): {canTravelToVillage} {(canTravelToVillage ? "OK" : "BLOQUE !")}");
        }

        Debug.Log("STATS GENERALES:");
        Debug.Log($"   TotalSteps: {playerData.TotalSteps}");
        Debug.Log($"   DailySteps: {playerData.DailySteps}");
        Debug.Log($"   ID: {playerData.Id}");

        Debug.Log("=== FIN DIAGNOSTIC ===");
    }

    private void RepairTravelState()
    {
        Debug.Log("=== REPARATION DE L'ETAT DE VOYAGE ===");

        var dataManager = DataManager.Instance;
        var mapManager = MapManager.Instance;

        if (dataManager?.PlayerData == null)
        {
            Debug.LogError("ERROR: DataManager non disponible !");
            return;
        }

        var playerData = dataManager.PlayerData;

        if (playerData.IsCurrentlyTraveling())
        {
            Debug.Log("Etat de voyage detecte - Nettoyage...");

            string oldDest = playerData.TravelDestinationId;
            long oldStart = playerData.TravelStartSteps;
            int oldRequired = playerData.TravelRequiredSteps;

            // Clear travel state
            playerData.TravelDestinationId = null;
            playerData.TravelStartSteps = 0;
            playerData.TravelRequiredSteps = 0;

            Debug.Log($"   FIXED: Supprime: Destination='{oldDest}', StartSteps={oldStart}, RequiredSteps={oldRequired}");

            // Clear MapManager state too
            mapManager?.ClearTravelState();

            Debug.Log("SUCCESS: Etat de voyage nettoye avec succes !");
        }
        else
        {
            Debug.Log("INFO: Aucun etat de voyage a reparer");
        }

        Debug.Log("=== FIN REPARATION VOYAGE ===");
    }

    private void RepairActivityState()
    {
        Debug.Log("=== REPARATION DE L'ETAT D'ACTIVITE ===");

        var dataManager = DataManager.Instance;
        var activityManager = ActivityManager.Instance;

        if (dataManager?.PlayerData == null)
        {
            Debug.LogError("ERROR: DataManager non disponible !");
            return;
        }

        var playerData = dataManager.PlayerData;

        if (playerData.HasActiveActivity())
        {
            Debug.Log("Activite active detectee - Arret...");

            string oldActivity = $"{playerData.CurrentActivity.ActivityId}/{playerData.CurrentActivity.VariantId}";

            playerData.StopActivity();

            Debug.Log($"   FIXED: Arrete: {oldActivity}");
            Debug.Log("SUCCESS: Etat d'activite nettoye avec succes !");
        }
        else
        {
            Debug.Log("INFO: Aucune activite active a arreter");
        }

        Debug.Log("=== FIN REPARATION ACTIVITE ===");
    }

    private void FullRepair()
    {
        Debug.Log("=== REPARATION COMPLETE ===");

        RepairTravelState();
        RepairActivityState();
        ForceSave();

        Debug.Log("SUCCESS: REPARATION COMPLETE TERMINEE !");
        Debug.Log("INFO: Teste maintenant sur ton telephone - tu devrais pouvoir voyager et faire des activites !");
    }

    private void ForceSave()
    {
        Debug.Log("=== SAUVEGARDE FORCEE ===");

        var dataManager = DataManager.Instance;
        if (dataManager != null)
        {
            dataManager.ForceSave();
            Debug.Log("SUCCESS: Sauvegarde reussie !");
        }
        else
        {
            Debug.LogError("ERROR: DataManager non disponible !");
        }
    }

    private void DisplayCurrentState()
    {
        var dataManager = DataManager.Instance;
        var mapManager = MapManager.Instance;
        var activityManager = ActivityManager.Instance;

        if (dataManager?.PlayerData == null)
        {
            EditorGUILayout.LabelField("ERROR: DataManager non disponible", EditorStyles.miniLabel);
            return;
        }

        var playerData = dataManager.PlayerData;

        // État de voyage
        EditorGUILayout.LabelField("VOYAGE:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"   Location: {playerData.CurrentLocationId}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"   En voyage: {(playerData.IsCurrentlyTraveling() ? "PROBLEME - OUI" : "OK - Non")}", EditorStyles.miniLabel);

        if (playerData.IsCurrentlyTraveling())
        {
            EditorGUILayout.LabelField($"   Vers: {playerData.TravelDestinationId}", EditorStyles.miniLabel);
            long progress = playerData.GetTravelProgress(playerData.TotalSteps);
            EditorGUILayout.LabelField($"   Progres: {progress}/{playerData.TravelRequiredSteps}", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();

        // État d'activité
        EditorGUILayout.LabelField("ACTIVITE:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"   Activite active: {(playerData.HasActiveActivity() ? "Oui" : "Non")}", EditorStyles.miniLabel);

        if (playerData.HasActiveActivity())
        {
            var activity = playerData.CurrentActivity;
            EditorGUILayout.LabelField($"   Type: {activity.ActivityId}/{activity.VariantId}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"   Steps: {activity.AccumulatedSteps}", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();

        // Conditions
        EditorGUILayout.LabelField("CONDITIONS:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"   Peut commencer activite: {(activityManager?.CanStartActivity() == true ? "OK - Oui" : "PROBLEME - Non")}", EditorStyles.miniLabel);

        if (mapManager?.CurrentLocation != null)
        {
            bool canTravel = mapManager.CanTravelTo("Village");
            EditorGUILayout.LabelField($"   Peut voyager: {(canTravel ? "OK - Oui" : "PROBLEME - Non")}", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();

        // Stats
        EditorGUILayout.LabelField("STATS:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"   Total Steps: {playerData.TotalSteps:N0}", EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"   Daily Steps: {playerData.DailySteps:N0}", EditorStyles.miniLabel);
    }
}
#endif