// Purpose: Outil éditeur pour réinitialiser complètement toutes les données de jeu
// Filepath: Assets/Scripts/Debug/GameDataResetter.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GameDataResetter : EditorWindow
{
    [MenuItem("StepQuest/ Game Data Resetter")]
    public static void ShowWindow()
    {
        GameDataResetter window = GetWindow<GameDataResetter>();
        window.titleContent = new GUIContent(" Game Data Resetter");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }

    private Vector2 scrollPosition;
    private bool showAdvancedOptions = false;
    private bool confirmReset = false;

    // Options de reset sélectives
    private bool resetPlayerData = true;
    private bool resetInventory = true;
    private bool resetStepData = true;
    private bool resetTravelData = true;
    private bool resetActivityData = true;
    private bool resetSkillsData = true;
    private bool resetLocationData = true;

    void OnGUI()
    {
        GUILayout.Label(" Game Data Resetter", EditorStyles.largeLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(" ATTENTION : Cet outil va SUPPRIMER toutes tes données de jeu !\n" +
                               "Assure-toi d'avoir une sauvegarde si nécessaire.", MessageType.Warning);

        EditorGUILayout.Space();

        // Status du jeu
        DrawGameStatus();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();

        // Options de reset
        DrawResetOptions();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.Space();

        // Boutons d'action
        DrawActionButtons();
    }

    private void DrawGameStatus()
    {
        EditorGUILayout.LabelField(" État actuel du jeu:", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(" Le jeu n'est pas en mode Play. Lance le jeu pour voir les données actuelles.", MessageType.Info);
            return;
        }

        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData == null)
        {
            EditorGUILayout.HelpBox(" DataManager non trouvé ! Assure-toi que le jeu fonctionne correctement.", MessageType.Error);
            return;
        }

        var playerData = dataManager.PlayerData;

        EditorGUILayout.BeginVertical(GUI.skin.box);

        // Données de pas
        EditorGUILayout.LabelField($" Pas totaux: {playerData.TotalSteps:N0}");
        EditorGUILayout.LabelField($" Pas journaliers: {playerData.DailySteps:N0}");

        // État de voyage
        if (playerData.IsCurrentlyTraveling())
        {
            long progress = playerData.GetTravelProgress(playerData.TotalSteps);
            EditorGUILayout.LabelField($" Voyage vers: {playerData.TravelDestinationId}");
            EditorGUILayout.LabelField($" Progrès: {progress}/{playerData.TravelRequiredSteps} pas");
        }
        else
        {
            EditorGUILayout.LabelField(" Actuellement: Pas de voyage en cours");
        }

        // État d'activité
        if (playerData.HasActiveActivity())
        {
            var activity = playerData.CurrentActivity;
            EditorGUILayout.LabelField($" Activité: {activity.ActivityId}/{activity.VariantId}");
            EditorGUILayout.LabelField($" Pas accumulés: {activity.AccumulatedSteps}");
        }
        else
        {
            EditorGUILayout.LabelField(" Activité: Aucune");
        }

        // Localisation
        EditorGUILayout.LabelField($" Localisation: {(string.IsNullOrEmpty(playerData.CurrentLocationId) ? "Non définie" : playerData.CurrentLocationId)}");

        // Compétences (aperçu)
        var skills = playerData.Skills;
        if (skills.Count > 0)
        {
            EditorGUILayout.LabelField($" Compétences: {skills.Count} compétences actives");
        }
        else
        {
            EditorGUILayout.LabelField(" Compétences: Aucune");
        }

        // Inventaire (aperçu)
        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
        {
            EditorGUILayout.LabelField($" Inventaire: {inventoryManager.GetDebugInfo()}");
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawResetOptions()
    {
        EditorGUILayout.LabelField(" Options de reset:", EditorStyles.boldLabel);

        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Options avancées (reset sélectif)");

        if (showAdvancedOptions)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.HelpBox("Tu peux choisir quelles données réinitialiser. Par défaut, tout est sélectionné.", MessageType.Info);

            resetPlayerData = EditorGUILayout.ToggleLeft(" Données joueur de base (ID, timestamps)", resetPlayerData);
            resetStepData = EditorGUILayout.ToggleLeft(" Données de pas (compteurs, historique)", resetStepData);
            resetTravelData = EditorGUILayout.ToggleLeft(" Données de voyage (destination, progrès)", resetTravelData);
            resetActivityData = EditorGUILayout.ToggleLeft(" Données d'activité (activité courante)", resetActivityData);
            resetSkillsData = EditorGUILayout.ToggleLeft(" Compétences et XP", resetSkillsData);
            resetLocationData = EditorGUILayout.ToggleLeft(" Localisation actuelle", resetLocationData);
            resetInventory = EditorGUILayout.ToggleLeft(" Inventaire complet", resetInventory);

            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("Mode rapide : Tout sera réinitialisé", MessageType.Info);
            // En mode rapide, tout est sélectionné
            resetPlayerData = resetInventory = resetStepData = resetTravelData =
            resetActivityData = resetSkillsData = resetLocationData = true;
        }
    }

    private void DrawActionButtons()
    {
        EditorGUILayout.LabelField(" Actions:", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Lance le jeu en mode Play pour pouvoir effectuer un reset.", MessageType.Warning);
            GUI.enabled = false;
        }

        // Checkbox de confirmation
        confirmReset = EditorGUILayout.ToggleLeft(" Je confirme vouloir réinitialiser les données sélectionnées", confirmReset);

        GUI.enabled = confirmReset && Application.isPlaying;

        EditorGUILayout.Space();

        // Bouton principal de reset
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button(" RÉINITIALISER MAINTENANT", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Confirmation finale",
                "Es-tu ABSOLUMENT SÛR de vouloir réinitialiser les données sélectionnées ?\n\n" +
                "Cette action est IRRÉVERSIBLE !",
                "OUI, RESET !", "Annuler"))
            {
                PerformGameReset();
            }
        }
        GUI.backgroundColor = Color.white;

        GUI.enabled = Application.isPlaying;

        EditorGUILayout.Space();

        // Boutons d'urgence
        EditorGUILayout.LabelField(" Actions d'urgence:", EditorStyles.boldLabel);

        GUI.backgroundColor = Color.yellow;
        if (GUILayout.Button(" Arrêter voyage en cours uniquement"))
        {
            StopCurrentTravelOnly();
        }

        if (GUILayout.Button(" Arrêter activité en cours uniquement"))
        {
            StopCurrentActivityOnly();
        }
        GUI.backgroundColor = Color.white;

        GUI.enabled = true;
    }

    private void PerformGameReset()
    {
        Debug.Log(" === DÉBUT DU RESET COMPLET DU JEU ===");

        try
        {
            var dataManager = DataManager.Instance;
            if (dataManager?.PlayerData == null)
            {
                Debug.LogError(" DataManager non disponible !");
                EditorUtility.DisplayDialog("Erreur", "DataManager non disponible !", "OK");
                return;
            }

            var playerData = dataManager.PlayerData;

            // 1. Reset des données de pas
            if (resetStepData)
            {
                Debug.Log(" Reset des données de pas...");
                playerData.TotalPlayerSteps = 0;
                playerData.DailySteps = 0;
                playerData.LastSyncEpochMs = 0;
                playerData.LastPauseEpochMs = 0;
                playerData.LastStepsDelta = 0;
                playerData.LastStepsChangeEpochMs = 0;
                playerData.LastApiCatchUpEpochMs = 0;
                playerData.LastDailyResetDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            }

            // 2. Reset des données de voyage
            if (resetTravelData)
            {
                Debug.Log(" Reset des données de voyage...");
                playerData.TravelDestinationId = null;
                playerData.TravelStartSteps = 0;
                playerData.TravelRequiredSteps = 0;
                playerData.TravelFinalDestinationId = null;
                playerData.TravelOriginLocationId = null;
            }

            // 3. Reset des données d'activité
            if (resetActivityData)
            {
                Debug.Log(" Reset des données d'activité...");
                playerData.StopActivity();
            }

            // 4. Reset des compétences
            if (resetSkillsData)
            {
                Debug.Log(" Reset des compétences...");
                playerData.Skills = new Dictionary<string, SkillData>();
                playerData.SubSkills = new Dictionary<string, SkillData>();
            }

            // 5. Reset de la localisation
            if (resetLocationData)
            {
                Debug.Log(" Reset de la localisation...");
                playerData.CurrentLocationId = "";

                // Remettre le joueur au village de départ si possible
                var mapManager = MapManager.Instance;
                if (mapManager != null)
                {
                    // Essayer de remettre au village de départ
                    playerData.CurrentLocationId = "Village_01"; // Ajuste selon ton jeu
                }
            }

            // 6. Reset de l'inventaire
            if (resetInventory)
            {
                Debug.Log(" Reset de l'inventaire...");
                var inventoryManager = InventoryManager.Instance;
                if (inventoryManager != null)
                {
                    // Vider tous les conteneurs
                    var playerContainer = inventoryManager.GetContainer("player");
                    var bankContainer = inventoryManager.GetContainer("bank");

                    playerContainer?.Clear();
                    bankContainer?.Clear();

                    // Forcer la sauvegarde de l'inventaire
                    inventoryManager.ForceSave();
                }
            }

            // 7. Reset des données de base du joueur
            if (resetPlayerData)
            {
                Debug.Log(" Reset des données de base...");
                playerData.Id = 1; // Garder l'ID à 1
            }

            // 8. Sauvegarder toutes les modifications
            Debug.Log(" Sauvegarde des modifications...");
            dataManager.SaveGame();

            // 9. Notifier les autres managers du reset si nécessaire
            NotifyManagersOfReset();

            Debug.Log(" === RESET COMPLET TERMINÉ ===");

            EditorUtility.DisplayDialog("Reset terminé !",
                " Toutes les données sélectionnées ont été réinitialisées avec succès !\n\n" +
                "Le jeu a été remis à l'état initial.", "Super !");

            // Réinitialiser la confirmation
            confirmReset = false;
        }
        catch (Exception ex)
        {
            Debug.LogError($" Erreur pendant le reset : {ex.Message}");
            EditorUtility.DisplayDialog("Erreur !",
                $"Une erreur s'est produite pendant le reset :\n{ex.Message}", "Merde !");
        }
    }

    private void StopCurrentTravelOnly()
    {
        Debug.Log(" Arrêt du voyage en cours uniquement...");

        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData == null)
        {
            Debug.LogError("❌ DataManager non disponible !");
            return;
        }

        var playerData = dataManager.PlayerData;

        if (!playerData.IsCurrentlyTraveling())
        {
            Debug.Log(" Aucun voyage en cours à arrêter.");
            EditorUtility.DisplayDialog("Info", "Aucun voyage en cours à arrêter.", "OK");
            return;
        }

        // Arrêter le voyage
        playerData.TravelDestinationId = null;
        playerData.TravelStartSteps = 0;
        playerData.TravelRequiredSteps = 0;
        playerData.TravelFinalDestinationId = null;
        playerData.TravelOriginLocationId = null;

        dataManager.SaveGame();

        Debug.Log(" Voyage arrêté avec succès !");
        EditorUtility.DisplayDialog("Voyage arrêté", "Le voyage en cours a été annulé.", "OK");
    }

    private void StopCurrentActivityOnly()
    {
        Debug.Log(" Arrêt de l'activité en cours uniquement...");

        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData == null)
        {
            Debug.LogError(" DataManager non disponible !");
            return;
        }

        var playerData = dataManager.PlayerData;

        if (!playerData.HasActiveActivity())
        {
            Debug.Log(" Aucune activité en cours à arrêter.");
            EditorUtility.DisplayDialog("Info", "Aucune activité en cours à arrêter.", "OK");
            return;
        }

        // Arrêter l'activité
        playerData.StopActivity();
        dataManager.SaveGame();

        Debug.Log(" Activité arrêtée avec succès !");
        EditorUtility.DisplayDialog("Activité arrêtée", "L'activité en cours a été arrêtée.", "OK");
    }

    private void NotifyManagersOfReset()
    {
        // Notifier les autres managers si nécessaire
        // Par exemple, forcer une mise à jour de l'UI, etc.

        var mapManager = MapManager.Instance;
        if (mapManager != null)
        {
            // Le MapManager pourrait avoir besoin de se réinitialiser
            Debug.Log(" Notification du reset au MapManager...");
        }

        var activityManager = ActivityManager.Instance;
        if (activityManager != null)
        {
            // L'ActivityManager pourrait avoir besoin de se réinitialiser
            Debug.Log(" Notification du reset à l'ActivityManager...");
        }

        // Potentiellement notifier d'autres managers selon ton architecture
    }
}
#endif