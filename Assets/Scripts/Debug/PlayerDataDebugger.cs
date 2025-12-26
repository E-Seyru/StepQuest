// Purpose: Diagnostic et reparation des donnees joueur corrompues
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
            EditorGUILayout.HelpBox("WARNING: Cette tool necessite le mode Play pour acceder aux donnees joueur", MessageType.Warning);
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
        Logger.LogInfo("=== DIAGNOSTIC COMPLET DE L'ETAT JOUEUR ===", Logger.LogCategory.EditorLog);

        var dataManager = DataManager.Instance;
        var mapManager = MapManager.Instance;
        var activityManager = ActivityManager.Instance;

        if (dataManager?.PlayerData == null)
        {
            Logger.LogError("ERROR: DataManager ou PlayerData non disponible !", Logger.LogCategory.EditorLog);
            return;
        }

        var playerData = dataManager.PlayerData;

        Logger.LogInfo("POSITION & VOYAGE:", Logger.LogCategory.EditorLog);
        Logger.LogInfo($"   CurrentLocationId: '{playerData.CurrentLocationId}'", Logger.LogCategory.EditorLog);
        Logger.LogInfo($"   TravelDestinationId: '{playerData.TravelDestinationId}' {(string.IsNullOrEmpty(playerData.TravelDestinationId) ? "(NULL - OK)" : "(NON-NULL - PROBLEME POTENTIEL)")}", Logger.LogCategory.EditorLog);
        Logger.LogInfo($"   TravelStartSteps: {playerData.TravelStartSteps}", Logger.LogCategory.EditorLog);
        Logger.LogInfo($"   TravelRequiredSteps: {playerData.TravelRequiredSteps}", Logger.LogCategory.EditorLog);
        Logger.LogInfo($"   IsCurrentlyTraveling(): {playerData.IsCurrentlyTraveling()} {(playerData.IsCurrentlyTraveling() ? "PROBLEME !" : "OK")}", Logger.LogCategory.EditorLog);

        if (playerData.IsCurrentlyTraveling())
        {
            long progress = playerData.GetTravelProgress(playerData.TotalSteps);
            Logger.LogInfo($"   Progres voyage: {progress}/{playerData.TravelRequiredSteps} steps", Logger.LogCategory.EditorLog);
            Logger.LogInfo($"   Voyage termine?: {playerData.IsTravelComplete(playerData.TotalSteps)}", Logger.LogCategory.EditorLog);
        }

        Logger.LogInfo("ACTIVITE:", Logger.LogCategory.EditorLog);
        Logger.LogInfo($"   HasActiveActivity(): {playerData.HasActiveActivity()} {(playerData.HasActiveActivity() ? "Activite en cours" : "Pas d'activite")}", Logger.LogCategory.EditorLog);

        if (playerData.HasActiveActivity())
        {
            Logger.LogInfo($"   Activite: {playerData.CurrentActivity.ActivityId}/{playerData.CurrentActivity.VariantId}", Logger.LogCategory.EditorLog);
            Logger.LogInfo($"   Location: {playerData.CurrentActivity.LocationId}", Logger.LogCategory.EditorLog);
            Logger.LogInfo($"   Steps accumules: {playerData.CurrentActivity.AccumulatedSteps}", Logger.LogCategory.EditorLog);
        }

        Logger.LogInfo("CONDITIONS DE BLOCAGE:", Logger.LogCategory.EditorLog);
        Logger.LogInfo($"   CanStartActivity(): {activityManager?.CanStartActivity()} {(activityManager?.CanStartActivity() == false ? "BLOQUE !" : "OK")}", Logger.LogCategory.EditorLog);

        if (mapManager?.CurrentLocation != null)
        {
            string currentLoc = mapManager.CurrentLocation.LocationID;
            bool canTravelToVillage = mapManager.CanTravelTo("Village");
            Logger.LogInfo($"   CanTravelTo('Village'): {canTravelToVillage} {(canTravelToVillage ? "OK" : "BLOQUE !")}", Logger.LogCategory.EditorLog);
        }

        Logger.LogInfo("STATS GENERALES:", Logger.LogCategory.EditorLog);
        Logger.LogInfo($"   TotalSteps: {playerData.TotalSteps}", Logger.LogCategory.EditorLog);
        Logger.LogInfo($"   DailySteps: {playerData.DailySteps}", Logger.LogCategory.EditorLog);
        Logger.LogInfo($"   ID: {playerData.Id}", Logger.LogCategory.EditorLog);

        Logger.LogInfo("=== FIN DIAGNOSTIC ===", Logger.LogCategory.EditorLog);
    }

    private void RepairTravelState()
    {
        Logger.LogInfo("=== REPARATION DE L'ETAT DE VOYAGE ===", Logger.LogCategory.EditorLog);

        var dataManager = DataManager.Instance;
        var mapManager = MapManager.Instance;

        if (dataManager?.PlayerData == null)
        {
            Logger.LogError("ERROR: DataManager non disponible !", Logger.LogCategory.EditorLog);
            return;
        }

        var playerData = dataManager.PlayerData;

        if (playerData.IsCurrentlyTraveling())
        {
            Logger.LogInfo("Etat de voyage detecte - Nettoyage...", Logger.LogCategory.EditorLog);

            string oldDest = playerData.TravelDestinationId;
            long oldStart = playerData.TravelStartSteps;
            int oldRequired = playerData.TravelRequiredSteps;

            // Clear travel state
            playerData.TravelDestinationId = null;
            playerData.TravelStartSteps = 0;
            playerData.TravelRequiredSteps = 0;

            Logger.LogInfo($"   FIXED: Supprime: Destination='{oldDest}', StartSteps={oldStart}, RequiredSteps={oldRequired}", Logger.LogCategory.EditorLog);

            // Clear MapManager state too
            mapManager?.ClearTravelState();

            Logger.LogInfo("SUCCESS: Etat de voyage nettoye avec succes !", Logger.LogCategory.EditorLog);
        }
        else
        {
            Logger.LogInfo("INFO: Aucun etat de voyage a reparer", Logger.LogCategory.EditorLog);
        }

        Logger.LogInfo("=== FIN REPARATION VOYAGE ===", Logger.LogCategory.EditorLog);
    }

    private void RepairActivityState()
    {
        Logger.LogInfo("=== REPARATION DE L'ETAT D'ACTIVITE ===", Logger.LogCategory.EditorLog);

        var dataManager = DataManager.Instance;
        var activityManager = ActivityManager.Instance;

        if (dataManager?.PlayerData == null)
        {
            Logger.LogError("ERROR: DataManager non disponible !", Logger.LogCategory.EditorLog);
            return;
        }

        var playerData = dataManager.PlayerData;

        if (playerData.HasActiveActivity())
        {
            Logger.LogInfo("Activite active detectee - Arret...", Logger.LogCategory.EditorLog);

            string oldActivity = $"{playerData.CurrentActivity.ActivityId}/{playerData.CurrentActivity.VariantId}";

            playerData.StopActivity();

            Logger.LogInfo($"   FIXED: Arrete: {oldActivity}", Logger.LogCategory.EditorLog);
            Logger.LogInfo("SUCCESS: Etat d'activite nettoye avec succes !", Logger.LogCategory.EditorLog);
        }
        else
        {
            Logger.LogInfo("INFO: Aucune activite active a arreter", Logger.LogCategory.EditorLog);
        }

        Logger.LogInfo("=== FIN REPARATION ACTIVITE ===", Logger.LogCategory.EditorLog);
    }

    private void FullRepair()
    {
        Logger.LogInfo("=== REPARATION COMPLETE ===", Logger.LogCategory.EditorLog);

        RepairTravelState();
        RepairActivityState();
        ForceSave();

        Logger.LogInfo("SUCCESS: REPARATION COMPLETE TERMINEE !", Logger.LogCategory.EditorLog);
        Logger.LogInfo("INFO: Teste maintenant sur ton telephone - tu devrais pouvoir voyager et faire des activites !", Logger.LogCategory.EditorLog);
    }

    private void ForceSave()
    {
        Logger.LogInfo("=== SAUVEGARDE FORCEE ===", Logger.LogCategory.EditorLog);

        var dataManager = DataManager.Instance;
        if (dataManager != null)
        {
            dataManager.ForceSave();
            Logger.LogInfo("SUCCESS: Sauvegarde reussie !", Logger.LogCategory.EditorLog);
        }
        else
        {
            Logger.LogError("ERROR: DataManager non disponible !", Logger.LogCategory.EditorLog);
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

        // etat de voyage
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

        // etat d'activite
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