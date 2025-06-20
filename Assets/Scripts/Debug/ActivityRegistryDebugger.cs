// Purpose: Script de debug pour identifier les references fantômes dans ActivityRegistry
// Filepath: Assets/Scripts/Debug/ActivityRegistryDebugger.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class ActivityRegistryDebugger : EditorWindow
{
    [MenuItem("StepQuest/Debug Activity Registry")]
    public static void ShowWindow()
    {
        ActivityRegistryDebugger window = GetWindow<ActivityRegistryDebugger>();
        window.titleContent = new GUIContent("Activity Registry Debugger");
        window.Show();
    }

    private ActivityRegistry activityRegistry;
    private Vector2 scrollPosition;

    void OnGUI()
    {
        GUILayout.Label("Activity Registry Debugger", EditorStyles.boldLabel);

        activityRegistry = (ActivityRegistry)EditorGUILayout.ObjectField("Activity Registry", activityRegistry, typeof(ActivityRegistry), false);

        if (activityRegistry == null)
        {
            EditorGUILayout.HelpBox("Selectionnez votre ActivityRegistry pour le debugger", MessageType.Info);
            return;
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Analyser les References"))
        {
            AnalyzeRegistry();
        }

        if (GUILayout.Button("Nettoyer les References Nulles"))
        {
            CleanNullReferences();
        }

        if (GUILayout.Button("Reconstruire le Cache"))
        {
            RebuildCache();
        }

        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DisplayRegistryContents();
        EditorGUILayout.EndScrollView();
    }

    private void AnalyzeRegistry()
    {
        Debug.Log("=== ANALYSE DU REGISTRY ===");

        if (activityRegistry.AllActivities == null)
        {
            Debug.LogError("AllActivities est null !");
            return;
        }

        Debug.Log($"Nombre total d'activites : {activityRegistry.AllActivities.Count}");

        for (int i = 0; i < activityRegistry.AllActivities.Count; i++)
        {
            var locationActivity = activityRegistry.AllActivities[i];

            if (locationActivity == null)
            {
                Debug.LogWarning($"LocationActivity[{i}] est null !");
                continue;
            }

            if (locationActivity.ActivityReference == null)
            {
                Debug.LogWarning($"LocationActivity[{i}].ActivityReference est null !");
                continue;
            }

            var activityDef = locationActivity.ActivityReference;
            Debug.Log($"Activite[{i}] : '{activityDef.ActivityID}' - '{activityDef.ActivityName}'");

            if (locationActivity.ActivityVariants != null)
            {
                for (int j = 0; j < locationActivity.ActivityVariants.Count; j++)
                {
                    var variant = locationActivity.ActivityVariants[j];

                    if (variant == null)
                    {
                        Debug.LogWarning($"  Variant[{j}] est null pour l'activite '{activityDef.ActivityID}' !");
                        continue;
                    }

                    // Verifier le ParentActivityID
                    string parentId = variant.GetParentActivityID();
                    string expectedId = activityDef.ActivityID;

                    Debug.Log($"  Variant[{j}] : '{variant.VariantName}' - Parent: '{parentId}' (attendu: '{expectedId}')");

                    if (parentId != expectedId)
                    {
                        Debug.LogError($"  ❌ PROBLÈME : Le variant '{variant.VariantName}' a un ParentActivityID '{parentId}' " +
                                     $"mais il est associe a l'activite '{expectedId}' !");
                    }

                    // Verifier les ressources
                    if (variant.PrimaryResource == null)
                    {
                        Debug.LogWarning($"  ⚠️ Le variant '{variant.VariantName}' n'a pas de ressource primaire !");
                    }
                }
            }
            else
            {
                Debug.LogWarning($"  L'activite '{activityDef.ActivityID}' n'a aucun variant !");
            }
        }

        // Chercher des references a "Mining"
        SearchForMiningReferences();
    }

    private void SearchForMiningReferences()
    {
        Debug.Log("=== RECHERCHE DE RÉFÉRENCES À 'MINING' ===");

        // Chercher dans tous les ActivityVariant du projet
        string[] variantGuids = AssetDatabase.FindAssets("t:ActivityVariant");

        foreach (string guid in variantGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ActivityVariant variant = AssetDatabase.LoadAssetAtPath<ActivityVariant>(path);

            if (variant != null)
            {
                string parentId = variant.GetParentActivityID();

                if (parentId.Contains("Mining") || parentId.Contains("mining"))
                {
                    Debug.LogError($"❌ TROUVÉ : Le variant '{variant.VariantName}' dans '{path}' " +
                                 $"reference '{parentId}' qui contient 'Mining' !");
                }
            }
        }

        // Chercher dans tous les ActivityDefinition
        string[] activityGuids = AssetDatabase.FindAssets("t:ActivityDefinition");

        foreach (string guid in activityGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ActivityDefinition activity = AssetDatabase.LoadAssetAtPath<ActivityDefinition>(path);

            if (activity != null)
            {
                if (activity.ActivityID.Contains("Mining") || activity.ActivityID.Contains("mining"))
                {
                    Debug.LogError($"❌ TROUVÉ : L'activite '{activity.ActivityName}' dans '{path}' " +
                                 $"a un ID '{activity.ActivityID}' qui contient 'Mining' !");
                }
            }
        }
    }

    private void CleanNullReferences()
    {
        Debug.Log("=== NETTOYAGE DES RÉFÉRENCES NULLES ===");

        bool hasChanges = false;

        // Nettoyer les LocationActivity nulles
        for (int i = activityRegistry.AllActivities.Count - 1; i >= 0; i--)
        {
            if (activityRegistry.AllActivities[i] == null ||
                activityRegistry.AllActivities[i].ActivityReference == null)
            {
                Debug.Log($"Suppression de LocationActivity[{i}] (null)");
                activityRegistry.AllActivities.RemoveAt(i);
                hasChanges = true;
            }
        }

        // Nettoyer les variants nulls dans chaque activite
        foreach (var locationActivity in activityRegistry.AllActivities)
        {
            if (locationActivity.ActivityVariants != null)
            {
                for (int i = locationActivity.ActivityVariants.Count - 1; i >= 0; i--)
                {
                    if (locationActivity.ActivityVariants[i] == null)
                    {
                        Debug.Log($"Suppression de variant null dans '{locationActivity.ActivityReference.ActivityID}'");
                        locationActivity.ActivityVariants.RemoveAt(i);
                        hasChanges = true;
                    }
                }
            }
        }

        if (hasChanges)
        {
            EditorUtility.SetDirty(activityRegistry);
            AssetDatabase.SaveAssets();
            Debug.Log("✅ Nettoyage termine. Registry sauvegarde.");
        }
        else
        {
            Debug.Log("Aucune reference nulle trouvee.");
        }
    }

    private void RebuildCache()
    {
        Debug.Log("=== RECONSTRUCTION DU CACHE ===");

        activityRegistry.RefreshCache();
        activityRegistry.ValidateRegistry();

        EditorUtility.SetDirty(activityRegistry);
        AssetDatabase.SaveAssets();

        Debug.Log("✅ Cache reconstruit et registry valide.");
    }

    private void DisplayRegistryContents()
    {
        if (activityRegistry.AllActivities == null) return;

        EditorGUILayout.LabelField("Contenu du Registry:", EditorStyles.boldLabel);

        foreach (var locationActivity in activityRegistry.AllActivities)
        {
            if (locationActivity?.ActivityReference == null) continue;

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField($"Activite: {locationActivity.ActivityReference.ActivityID}", EditorStyles.boldLabel);

            if (locationActivity.ActivityVariants != null)
            {
                foreach (var variant in locationActivity.ActivityVariants)
                {
                    if (variant == null)
                    {
                        EditorGUILayout.LabelField("  • [NULL VARIANT]", EditorStyles.miniLabel);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"  • {variant.VariantName} (Parent: {variant.GetParentActivityID()})", EditorStyles.miniLabel);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }
    }
}
#endif