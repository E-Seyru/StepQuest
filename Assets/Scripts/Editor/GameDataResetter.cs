// Purpose: Outil editeur pour reinitialiser completement toutes les donnees de jeu
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

    // Options de reset selectives
    private bool resetPlayerData = true;
    private bool resetInventory = true;
    private bool resetStepData = true;
    private bool resetTravelData = true;
    private bool resetActivityData = true;
    private bool resetSkillsData = true;
    private bool resetLocationData = true;
    private bool resetAbilities = true;
    private bool resetNPCData = true;
    private bool resetDialogueFlags = true;

    void OnGUI()
    {
        GUILayout.Label(" Game Data Resetter", EditorStyles.largeLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(" ATTENTION : Cet outil va SUPPRIMER toutes tes donnees de jeu !\n" +
                               "Assure-toi d'avoir une sauvegarde si necessaire.", MessageType.Warning);

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
        EditorGUILayout.LabelField(" etat actuel du jeu:", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox(" Le jeu n'est pas en mode Play. Lance le jeu pour voir les donnees actuelles.", MessageType.Info);
            return;
        }

        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData == null)
        {
            EditorGUILayout.HelpBox(" DataManager non trouve ! Assure-toi que le jeu fonctionne correctement.", MessageType.Error);
            return;
        }

        var playerData = dataManager.PlayerData;

        EditorGUILayout.BeginVertical(GUI.skin.box);

        // Donnees de pas
        EditorGUILayout.LabelField($" Pas totaux: {playerData.TotalSteps:N0}");
        EditorGUILayout.LabelField($" Pas journaliers: {playerData.DailySteps:N0}");

        // etat de voyage
        if (playerData.IsCurrentlyTraveling())
        {
            long progress = playerData.GetTravelProgress(playerData.TotalSteps);
            EditorGUILayout.LabelField($" Voyage vers: {playerData.TravelDestinationId}");
            EditorGUILayout.LabelField($" Progres: {progress}/{playerData.TravelRequiredSteps} pas");
        }
        else
        {
            EditorGUILayout.LabelField(" Actuellement: Pas de voyage en cours");
        }

        // etat d'activite
        if (playerData.HasActiveActivity())
        {
            var activity = playerData.CurrentActivity;
            EditorGUILayout.LabelField($" Activite: {activity.ActivityId}/{activity.VariantId}");
            EditorGUILayout.LabelField($" Pas accumules: {activity.AccumulatedSteps}");
        }
        else
        {
            EditorGUILayout.LabelField(" Activite: Aucune");
        }

        // Localisation
        EditorGUILayout.LabelField($" Localisation: {(string.IsNullOrEmpty(playerData.CurrentLocationId) ? "Non definie" : playerData.CurrentLocationId)}");

        // Competences (aperçu)
        var skills = playerData.Skills;
        if (skills.Count > 0)
        {
            EditorGUILayout.LabelField($" Competences: {skills.Count} competences actives");
        }
        else
        {
            EditorGUILayout.LabelField(" Competences: Aucune");
        }

        // Inventaire (aperçu)
        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
        {
            EditorGUILayout.LabelField($" Inventaire: {inventoryManager.GetDebugInfo()}");
        }

        // Abilities (aperçu)
        var ownedAbilities = playerData.OwnedAbilities;
        var equippedAbilities = playerData.EquippedAbilities;
        EditorGUILayout.LabelField($" Abilities: {ownedAbilities.Count} possedee(s), {equippedAbilities.Count} equipee(s)");

        // NPC Data (aperçu)
        var discoveredNPCs = playerData.DiscoveredNPCs;
        var npcRelationships = playerData.NPCRelationships;
        EditorGUILayout.LabelField($" NPCs: {discoveredNPCs.Count} decouvert(s), {npcRelationships.Count} relation(s)");

        // Dialogue flags (aperçu)
        var dialogueFlags = playerData.DialogueFlags;
        EditorGUILayout.LabelField($" Dialogue: {dialogueFlags.Count} flag(s)");

        EditorGUILayout.EndVertical();
    }

    private void DrawResetOptions()
    {
        EditorGUILayout.LabelField(" Options de reset:", EditorStyles.boldLabel);

        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Options avancees (reset selectif)");

        if (showAdvancedOptions)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.HelpBox("Tu peux choisir quelles donnees reinitialiser. Par defaut, tout est selectionne.", MessageType.Info);

            resetPlayerData = EditorGUILayout.ToggleLeft(" Donnees joueur de base (ID, timestamps)", resetPlayerData);
            resetStepData = EditorGUILayout.ToggleLeft(" Donnees de pas (compteurs, historique)", resetStepData);
            resetTravelData = EditorGUILayout.ToggleLeft(" Donnees de voyage (destination, progres)", resetTravelData);
            resetActivityData = EditorGUILayout.ToggleLeft(" Donnees d'activite (activite courante)", resetActivityData);
            resetSkillsData = EditorGUILayout.ToggleLeft(" Competences et XP", resetSkillsData);
            resetLocationData = EditorGUILayout.ToggleLeft(" Localisation actuelle", resetLocationData);
            resetInventory = EditorGUILayout.ToggleLeft(" Inventaire complet", resetInventory);
            resetAbilities = EditorGUILayout.ToggleLeft(" Abilities (possedees et equipees)", resetAbilities);
            resetNPCData = EditorGUILayout.ToggleLeft(" Donnees NPC (decouverts et relations)", resetNPCData);
            resetDialogueFlags = EditorGUILayout.ToggleLeft(" Flags de dialogue", resetDialogueFlags);

            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("Mode rapide : Tout sera reinitialise", MessageType.Info);
            // En mode rapide, tout est selectionne
            resetPlayerData = resetInventory = resetStepData = resetTravelData =
            resetActivityData = resetSkillsData = resetLocationData =
            resetAbilities = resetNPCData = resetDialogueFlags = true;
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
        confirmReset = EditorGUILayout.ToggleLeft(" Je confirme vouloir reinitialiser les donnees selectionnees", confirmReset);

        GUI.enabled = confirmReset && Application.isPlaying;

        EditorGUILayout.Space();

        // Bouton principal de reset
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button(" ReINITIALISER MAINTENANT", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("Confirmation finale",
                "Es-tu ABSOLUMENT SÛR de vouloir reinitialiser les donnees selectionnees ?\n\n" +
                "Cette action est IRReVERSIBLE !",
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

        if (GUILayout.Button(" Arrêter activite en cours uniquement"))
        {
            StopCurrentActivityOnly();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        // Debug buttons
        EditorGUILayout.LabelField(" Actions de debug:", EditorStyles.boldLabel);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button(" Remplir l'inventaire avec des items"))
        {
            FillInventoryWithItems();
        }
        GUI.backgroundColor = Color.white;

        GUI.enabled = true;
    }

    private void PerformGameReset()
    {
        Logger.LogInfo(" === DeBUT DU RESET COMPLET DU JEU ===", Logger.LogCategory.EditorLog);

        try
        {
            var dataManager = DataManager.Instance;
            if (dataManager?.PlayerData == null)
            {
                Logger.LogError(" DataManager non disponible !", Logger.LogCategory.EditorLog);
                EditorUtility.DisplayDialog("Erreur", "DataManager non disponible !", "OK");
                return;
            }

            var playerData = dataManager.PlayerData;

            // 1. Reset des donnees de pas
            if (resetStepData)
            {
                Logger.LogInfo(" Reset des donnees de pas...", Logger.LogCategory.EditorLog);
                playerData.TotalPlayerSteps = 0;
                playerData.DailySteps = 0;
                playerData.LastSyncEpochMs = 0;
                playerData.LastPauseEpochMs = 0;
                playerData.LastStepsDelta = 0;
                playerData.LastStepsChangeEpochMs = 0;
                playerData.LastApiCatchUpEpochMs = 0;
                playerData.LastDailyResetDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
            }

            // 2. Reset des donnees de voyage
            if (resetTravelData)
            {
                Logger.LogInfo(" Reset des donnees de voyage...", Logger.LogCategory.EditorLog);
                playerData.TravelDestinationId = null;
                playerData.TravelStartSteps = 0;
                playerData.TravelRequiredSteps = 0;
                playerData.TravelFinalDestinationId = null;
                playerData.TravelOriginLocationId = null;
                playerData.IsMultiSegmentTravel = false;
            }

            // 3. Reset des donnees d'activite
            if (resetActivityData)
            {
                Logger.LogInfo(" Reset des donnees d'activite...", Logger.LogCategory.EditorLog);
                playerData.StopActivity();
            }

            // 4. Reset des competences
            if (resetSkillsData)
            {
                Logger.LogInfo(" Reset des competences...", Logger.LogCategory.EditorLog);
                playerData.Skills = new Dictionary<string, SkillData>();
                playerData.SubSkills = new Dictionary<string, SkillData>();
            }

            // 5. Reset de la localisation
            if (resetLocationData)
            {
                Logger.LogInfo(" Reset de la localisation...", Logger.LogCategory.EditorLog);
                playerData.CurrentLocationId = "";

                // Remettre le joueur au village de depart si possible
                var mapManager = MapManager.Instance;
                if (mapManager != null)
                {
                    // Essayer de remettre au village de depart
                    playerData.CurrentLocationId = "village_01"; // snake_case ID
                }
            }

            // 6. Reset de l'inventaire
            if (resetInventory)
            {
                Logger.LogInfo(" Reset de l'inventaire...", Logger.LogCategory.EditorLog);
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

            // 7. Reset des abilities
            if (resetAbilities)
            {
                Logger.LogInfo(" Reset des abilities...", Logger.LogCategory.EditorLog);
                playerData.OwnedAbilities = new List<string>();
                playerData.EquippedAbilities = new List<string>();

                // Notifier AbilityManager si present
                var abilityManager = AbilityManager.Instance;
                if (abilityManager != null)
                {
                    Logger.LogInfo(" Notification du reset a l'AbilityManager...", Logger.LogCategory.EditorLog);
                }
            }

            // 8. Reset des donnees NPC
            if (resetNPCData)
            {
                Logger.LogInfo(" Reset des donnees NPC...", Logger.LogCategory.EditorLog);
                playerData.DiscoveredNPCs = new List<string>();
                playerData.NPCRelationships = new Dictionary<string, int>();
            }

            // 9. Reset des flags de dialogue
            if (resetDialogueFlags)
            {
                Logger.LogInfo(" Reset des flags de dialogue...", Logger.LogCategory.EditorLog);
                playerData.DialogueFlags = new Dictionary<string, bool>();
            }

            // 10. Reset des donnees de base du joueur
            if (resetPlayerData)
            {
                Logger.LogInfo(" Reset des donnees de base...", Logger.LogCategory.EditorLog);
                playerData.Id = 1; // Garder l'ID a 1
            }

            // 11. Sauvegarder toutes les modifications
            Logger.LogInfo(" Sauvegarde des modifications...", Logger.LogCategory.EditorLog);
            dataManager.SaveGame();

            // 12. Notifier les autres managers du reset si necessaire
            NotifyManagersOfReset();

            Logger.LogInfo(" === RESET COMPLET TERMINe ===", Logger.LogCategory.EditorLog);

            EditorUtility.DisplayDialog("Reset termine !",
                " Toutes les donnees selectionnees ont ete reinitialisees avec succes !\n\n" +
                "Le jeu a ete remis a l'etat initial.", "Super !");

            // Reinitialiser la confirmation
            confirmReset = false;
        }
        catch (Exception ex)
        {
            Logger.LogError($" Erreur pendant le reset : {ex.Message}", Logger.LogCategory.EditorLog);
            EditorUtility.DisplayDialog("Erreur !",
                $"Une erreur s'est produite pendant le reset :\n{ex.Message}", "Merde !");
        }
    }

    private void StopCurrentTravelOnly()
    {
        Logger.LogInfo(" Arrêt du voyage en cours uniquement...", Logger.LogCategory.EditorLog);

        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData == null)
        {
            Logger.LogError("❌ DataManager non disponible !", Logger.LogCategory.EditorLog);
            return;
        }

        var playerData = dataManager.PlayerData;

        if (!playerData.IsCurrentlyTraveling())
        {
            Logger.LogInfo(" Aucun voyage en cours a arrêter.", Logger.LogCategory.EditorLog);
            EditorUtility.DisplayDialog("Info", "Aucun voyage en cours a arrêter.", "OK");
            return;
        }

        // Arrêter le voyage
        playerData.TravelDestinationId = null;
        playerData.TravelStartSteps = 0;
        playerData.TravelRequiredSteps = 0;
        playerData.TravelFinalDestinationId = null;
        playerData.TravelOriginLocationId = null;

        dataManager.SaveGame();

        Logger.LogInfo(" Voyage arrête avec succes !", Logger.LogCategory.EditorLog);
        EditorUtility.DisplayDialog("Voyage arrête", "Le voyage en cours a ete annule.", "OK");
    }

    private void StopCurrentActivityOnly()
    {
        Logger.LogInfo(" Arrêt de l'activite en cours uniquement...", Logger.LogCategory.EditorLog);

        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData == null)
        {
            Logger.LogError(" DataManager non disponible !", Logger.LogCategory.EditorLog);
            return;
        }

        var playerData = dataManager.PlayerData;

        if (!playerData.HasActiveActivity())
        {
            Logger.LogInfo(" Aucune activite en cours a arrêter.", Logger.LogCategory.EditorLog);
            EditorUtility.DisplayDialog("Info", "Aucune activite en cours a arrêter.", "OK");
            return;
        }

        // Arrêter l'activite
        playerData.StopActivity();
        dataManager.SaveGame();

        Logger.LogInfo(" Activite arrêtee avec succes !", Logger.LogCategory.EditorLog);
        EditorUtility.DisplayDialog("Activite arrêtee", "L'activite en cours a ete arrêtee.", "OK");
    }

    private void FillInventoryWithItems()
    {
        Logger.LogInfo(" Remplissage de l'inventaire...", Logger.LogCategory.EditorLog);

        var inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Logger.LogError(" InventoryManager non disponible !", Logger.LogCategory.EditorLog);
            EditorUtility.DisplayDialog("Erreur", "InventoryManager non disponible !", "OK");
            return;
        }

        var itemRegistry = inventoryManager.GetItemRegistry();
        if (itemRegistry == null)
        {
            Logger.LogError(" ItemRegistry non disponible !", Logger.LogCategory.EditorLog);
            EditorUtility.DisplayDialog("Erreur", "ItemRegistry non disponible !", "OK");
            return;
        }

        // Get all items from registry
        var allItems = itemRegistry.GetAllValidItems();
        if (allItems == null || allItems.Count == 0)
        {
            Logger.LogWarning(" Aucun item trouve dans le registre !", Logger.LogCategory.EditorLog);
            EditorUtility.DisplayDialog("Attention", "Aucun item trouve dans le registre !", "OK");
            return;
        }

        int addedCount = 0;
        foreach (var item in allItems)
        {
            if (item == null || string.IsNullOrEmpty(item.ItemID)) continue;

            // Add random quantity between 5 and 20
            int quantity = UnityEngine.Random.Range(5, 21);
            bool success = inventoryManager.AddItem("player", item.ItemID, quantity);

            if (success)
            {
                addedCount++;
                Logger.LogInfo($" Ajoute: {quantity}x {item.GetDisplayName()}", Logger.LogCategory.EditorLog);
            }
        }

        // Force save
        inventoryManager.ForceSave();

        Logger.LogInfo($" Inventaire rempli avec {addedCount} types d'items !", Logger.LogCategory.EditorLog);
        EditorUtility.DisplayDialog("Inventaire rempli !",
            $"{addedCount} types d'items ont ete ajoutes a l'inventaire.", "Super !");
    }

    private void NotifyManagersOfReset()
    {
        // Notifier les autres managers si necessaire
        // Par exemple, forcer une mise a jour de l'UI, etc.

        var mapManager = MapManager.Instance;
        if (mapManager != null)
        {
            // Le MapManager pourrait avoir besoin de se reinitialiser
            Logger.LogInfo(" Notification du reset au MapManager...", Logger.LogCategory.EditorLog);
        }

        var activityManager = ActivityManager.Instance;
        if (activityManager != null)
        {
            // L'ActivityManager pourrait avoir besoin de se reinitialiser
            Logger.LogInfo(" Notification du reset a l'ActivityManager...", Logger.LogCategory.EditorLog);
        }

        // Potentiellement notifier d'autres managers selon ton architecture
    }
}
#endif