// Purpose: Script pour débugger les données de joueur et trouver les références Mining
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

    void OnGUI()
    {
        GUILayout.Label("Player Data Debugger", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("Ce script va analyser tes données sauvegardées pour trouver des références à 'Mining'", MessageType.Info);

        if (GUILayout.Button("Analyser les données de joueur"))
        {
            AnalyzePlayerData();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Nettoyer l'activité corrompue"))
        {
            CleanCorruptedActivity();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Reset complet des données de joueur"))
        {
            if (EditorUtility.DisplayDialog("Attention",
                "Cela va supprimer TOUTES tes données de joueur sauvegardées. Es-tu sûr ?",
                "Oui, supprimer", "Annuler"))
            {
                ResetPlayerData();
            }
        }
    }

    private void AnalyzePlayerData()
    {
        Debug.Log("=== ANALYSE DES DONNÉES DE JOUEUR ===");

        if (!Application.isPlaying)
        {
            Debug.LogWarning("Lance le jeu en mode Play pour analyser les données !");
            EditorUtility.DisplayDialog("Mode Play requis",
                "Tu dois être en mode Play pour analyser les données de joueur.", "OK");
            return;
        }

        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData == null)
        {
            Debug.LogError("DataManager ou PlayerData non trouvé !");
            return;
        }

        var playerData = dataManager.PlayerData;

        Debug.Log($"=== DONNÉES ACTUELLES ===");
        Debug.Log($"Location actuelle : {playerData.CurrentLocationId}");
        Debug.Log($"En voyage : {playerData.IsCurrentlyTraveling()}");

        if (playerData.IsCurrentlyTraveling())
        {
            Debug.Log($"Destination : {playerData.TravelDestinationId}");
        }

        // VÉRIFIER L'ACTIVITÉ COURANTE
        if (playerData.HasActiveActivity())
        {
            var activity = playerData.CurrentActivity;
            Debug.Log($"=== ACTIVITÉ ACTIVE TROUVÉE ===");
            Debug.Log($"ActivityId : '{activity.ActivityId}'");
            Debug.Log($"VariantId : '{activity.VariantId}'");
            Debug.Log($"LocationId : '{activity.LocationId}'");

            // BINGO ! Si l'ActivityId contient "Mining", on a trouvé le problème !
            if (activity.ActivityId.Contains("Mining"))
            {
                Debug.LogError($"❌ PROBLÈME TROUVÉ ! L'activité active référence '{activity.ActivityId}' au lieu de 'Miner' !");
                Debug.LogError($"C'est ça qui cause l'erreur dans GetActivityVariant() !");
            }
            else
            {
                Debug.Log($"✅ L'activité active semble correcte : '{activity.ActivityId}'");
            }
        }
        else
        {
            Debug.Log("Aucune activité active");
        }

        // Vérifier les données JSON brutes
        string activityJson = playerData.CurrentActivityJson;
        if (!string.IsNullOrEmpty(activityJson))
        {
            Debug.Log($"JSON brut de l'activité : {activityJson}");

            if (activityJson.Contains("Mining"))
            {
                Debug.LogError($"❌ Le JSON contient 'Mining' ! Voici le JSON complet : {activityJson}");
            }
        }
    }

    private void CleanCorruptedActivity()
    {
        Debug.Log("=== NETTOYAGE DE L'ACTIVITÉ CORROMPUE ===");

        if (!Application.isPlaying)
        {
            Debug.LogWarning("Lance le jeu en mode Play pour nettoyer les données !");
            EditorUtility.DisplayDialog("Mode Play requis",
                "Tu dois être en mode Play pour nettoyer les données.", "OK");
            return;
        }

        var dataManager = DataManager.Instance;
        if (dataManager?.PlayerData == null)
        {
            Debug.LogError("DataManager ou PlayerData non trouvé !");
            return;
        }

        var playerData = dataManager.PlayerData;

        if (playerData.HasActiveActivity())
        {
            var activity = playerData.CurrentActivity;

            if (activity.ActivityId.Contains("Mining"))
            {
                Debug.Log($"Nettoyage de l'activité corrompue : {activity.ActivityId}");

                // Arrêter l'activité corrompue
                playerData.StopActivity();

                // Sauvegarder
                dataManager.SaveGame();

                Debug.Log("✅ Activité corrompue supprimée et données sauvegardées !");
                EditorUtility.DisplayDialog("Nettoyage terminé",
                    "L'activité corrompue a été supprimée. L'erreur devrait disparaître.", "OK");
            }
            else
            {
                Debug.Log("Aucune activité corrompue trouvée à nettoyer.");
                EditorUtility.DisplayDialog("Rien à nettoyer",
                    "Aucune activité corrompue n'a été trouvée.", "OK");
            }
        }
        else
        {
            Debug.Log("Aucune activité active à nettoyer.");
        }
    }

    private void ResetPlayerData()
    {
        Debug.Log("=== RESET COMPLET DES DONNÉES ===");

        if (!Application.isPlaying)
        {
            Debug.LogWarning("Lance le jeu en mode Play pour reset les données !");
            EditorUtility.DisplayDialog("Mode Play requis",
                "Tu dois être en mode Play pour reset les données.", "OK");
            return;
        }

        var dataManager = DataManager.Instance;
        if (dataManager?.LocalDatabase == null)
        {
            Debug.LogError("DataManager ou LocalDatabase non trouvé !");
            return;
        }

        // Créer nouvelles données propres
        var newPlayerData = new PlayerData();

        // Sauvegarder les nouvelles données
        dataManager.LocalDatabase.SavePlayerData(newPlayerData);

        Debug.Log("✅ Données de joueur complètement resetées !");
        EditorUtility.DisplayDialog("Reset terminé",
            "Les données de joueur ont été complètement resetées. Redémarre le jeu.", "OK");
    }
}
#endif