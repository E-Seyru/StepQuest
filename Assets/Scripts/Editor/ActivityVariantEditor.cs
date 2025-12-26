#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ActivityVariantEditor : EditorWindow
{
    [MenuItem("StepQuest/Activity Variant Manager")]
    public static void ShowWindow()
    {
        ActivityVariantEditor window = GetWindow<ActivityVariantEditor>();
        window.titleContent = new GUIContent("Activity Variant Manager");
        window.minSize = new Vector2(800, 600);
        window.Show();
    }

    private Vector2 scrollPosition;
    private List<ActivityVariant> allVariants = new List<ActivityVariant>();
    private string searchFilter = "";
    private string selectedActivityFilter = "All";
    private bool showOnlyMissingXP = false;

    // Cache des activites disponibles
    private List<string> availableActivities = new List<string>();

    void OnEnable()
    {
        RefreshVariantList();
    }

    void RefreshVariantList()
    {
        // Chercher tous les ActivityVariant dans le projet
        string[] guids = AssetDatabase.FindAssets("t:ActivityVariant");
        allVariants.Clear();
        availableActivities.Clear();
        availableActivities.Add("All");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ActivityVariant variant = AssetDatabase.LoadAssetAtPath<ActivityVariant>(path);
            if (variant != null)
            {
                allVariants.Add(variant);

                // Collecter les activites uniques
                string activityId = !string.IsNullOrEmpty(variant.ParentActivityID)
                    ? variant.ParentActivityID
                    : "Unknown";

                if (!availableActivities.Contains(activityId))
                {
                    availableActivities.Add(activityId);
                }
            }
        }

        // Trier les listes
        allVariants = allVariants.OrderBy(v => v.ParentActivityID).ThenBy(v => v.VariantName).ToList();
        availableActivities.Sort();
    }

    void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();

        // Header
        GUILayout.Label("Activity Variant Manager", EditorStyles.largeLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Refresh", GUILayout.Width(70)))
        {
            RefreshVariantList();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Filtres et options
        DrawFiltersAndOptions();

        EditorGUILayout.Space();

        // Actions rapides
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Auto-Setup Missing XP", GUILayout.Width(150)))
        {
            AutoSetupMissingXP();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Liste des variants
        DrawVariantList();
    }

    void DrawFiltersAndOptions()
    {
        EditorGUILayout.BeginHorizontal();

        // Filtre de recherche
        GUILayout.Label("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter, GUILayout.Width(200));

        GUILayout.Space(10);

        // Filtre par activite
        GUILayout.Label("Activity:", GUILayout.Width(60));
        int currentIndex = availableActivities.IndexOf(selectedActivityFilter);
        int newIndex = EditorGUILayout.Popup(currentIndex, availableActivities.ToArray(), GUILayout.Width(120));
        if (newIndex >= 0 && newIndex < availableActivities.Count)
        {
            selectedActivityFilter = availableActivities[newIndex];
        }

        GUILayout.Space(10);

        // Option pour ne montrer que ceux sans XP
        showOnlyMissingXP = EditorGUILayout.Toggle("Missing XP Only", showOnlyMissingXP, GUILayout.Width(120));

        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();

        // Statistiques
        var filteredVariants = GetFilteredVariants();
        EditorGUILayout.LabelField($"Showing {filteredVariants.Count} of {allVariants.Count} variants", EditorStyles.miniLabel);
    }

    void DrawVariantList()
    {
        var filteredVariants = GetFilteredVariants();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (var variant in filteredVariants)
        {
            DrawVariantRow(variant);
        }

        EditorGUILayout.EndScrollView();
    }

    void DrawVariantRow(ActivityVariant variant)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();

        // Image du variant (64x64)
        EditorGUILayout.BeginVertical(GUILayout.Width(70));
        Rect imageRect = GUILayoutUtility.GetRect(64, 64, GUILayout.Width(64), GUILayout.Height(64));

        EditorGUI.BeginChangeCheck();
        Sprite newIcon = (Sprite)EditorGUI.ObjectField(imageRect, variant.VariantIcon, typeof(Sprite), false);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(variant, "Change Variant Icon");
            variant.VariantIcon = newIcon;
            EditorUtility.SetDirty(variant);
        }

        // Nom de l'image en petit
        if (variant.VariantIcon != null)
        {
            EditorGUILayout.LabelField(variant.VariantIcon.name, EditorStyles.miniLabel, GUILayout.Width(64));
        }
        else
        {
            EditorGUILayout.LabelField("No Icon", EditorStyles.miniLabel, GUILayout.Width(64));
        }
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        // Informations de base
        EditorGUILayout.BeginVertical();

        // Premiere ligne : Nom + Type + Level Required
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"{variant.ParentActivityID} / {variant.VariantName}", EditorStyles.boldLabel, GUILayout.Width(200));

        // Type d'activite (bouton pour changer)
        EditorGUI.BeginChangeCheck();
        bool newIsTimeBased = variant.IsTimeBased;

        if (GUILayout.Button(variant.IsTimeBased ? "[TIME]" : "[STEP]", GUILayout.Width(60)))
        {
            newIsTimeBased = !variant.IsTimeBased;
        }

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(variant, "Change Activity Type");
            variant.IsTimeBased = newIsTimeBased;
            EditorUtility.SetDirty(variant);
        }

        // Valeur du coût/temps (editable)
        EditorGUI.BeginChangeCheck();
        if (variant.IsTimeBased)
        {
            int timeInSeconds = Mathf.RoundToInt(variant.CraftingTimeMs / 1000f);
            int newTimeInSeconds = EditorGUILayout.IntField(timeInSeconds, GUILayout.Width(50));
            GUILayout.Label("s", GUILayout.Width(15));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(variant, "Change Crafting Time");
                variant.CraftingTimeMs = newTimeInSeconds * 1000;
                EditorUtility.SetDirty(variant);
            }
        }
        else
        {
            int newActionCost = EditorGUILayout.IntField(variant.ActionCost, GUILayout.Width(50));
            GUILayout.Label("pas", GUILayout.Width(25));

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(variant, "Change Action Cost");
                variant.ActionCost = newActionCost;
                EditorUtility.SetDirty(variant);
            }
        }

        GUILayout.Space(10);

        // Niveau requis
        GUILayout.Label("Req Lvl:", GUILayout.Width(55));
        EditorGUI.BeginChangeCheck();
        int newUnlockReq = EditorGUILayout.IntField(variant.UnlockRequirement, GUILayout.Width(35));
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(variant, "Change Unlock Requirement");
            variant.UnlockRequirement = newUnlockReq;
            EditorUtility.SetDirty(variant);
        }

        GUILayout.FlexibleSpace();

        // Bouton pour ouvrir l'asset
        if (GUILayout.Button("Edit", GUILayout.Width(40)))
        {
            Selection.activeObject = variant;
            EditorGUIUtility.PingObject(variant);
        }

        EditorGUILayout.EndHorizontal();

        // Deuxieme ligne : XP Settings - Automatiques
        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("XP:", GUILayout.Width(25));

        EditorGUI.BeginChangeCheck();

        GUILayout.Label("Main:", GUILayout.Width(35));
        int newMainXP = EditorGUILayout.IntField(variant.MainSkillXPPerTick, GUILayout.Width(40));

        GUILayout.Label("Sub:", GUILayout.Width(30));
        int newSubXP = EditorGUILayout.IntField(variant.SubSkillXPPerTick, GUILayout.Width(40));

        // Competence automatique basee sur ParentActivityID
        string autoSkillId = !string.IsNullOrEmpty(variant.ParentActivityID) ? variant.ParentActivityID : "Unknown";
        GUILayout.Label($"→ {autoSkillId}", EditorStyles.miniLabel, GUILayout.Width(80));

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(variant, "Modify ActivityVariant XP");
            variant.MainSkillXPPerTick = newMainXP;
            variant.SubSkillXPPerTick = newSubXP;
            // Auto-assigner la competence principale
            variant.MainSkillId = autoSkillId;
            EditorUtility.SetDirty(variant);
        }

        // Indicateur de probleme
        if (variant.MainSkillXPPerTick == 0)
        {
            GUILayout.Label("⚠️ No XP", GUILayout.Width(50));
        }

        EditorGUILayout.EndHorizontal();

        // Troisieme ligne : Informations supplementaires
        EditorGUILayout.BeginHorizontal();
        if (variant.PrimaryResource != null)
        {
            GUILayout.Label($"→ {variant.PrimaryResource.name}", EditorStyles.miniLabel);
        }
        if (variant.SecondaryResources != null && variant.SecondaryResources.Length > 0)
        {
            var validSecondary = variant.SecondaryResources.Where(r => r != null);
            if (validSecondary.Any())
            {
                GUILayout.Label($"+ {validSecondary.Count()} bonus", EditorStyles.miniLabel);
            }
        }
        GUILayout.FlexibleSpace();

        // Preview de la competence calculee
        string previewSkill = variant.GetSubSkillId();
        if (!string.IsNullOrEmpty(previewSkill))
        {
            GUILayout.Label($"SubSkill: {previewSkill}", EditorStyles.miniLabel, GUILayout.Width(150));
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    List<ActivityVariant> GetFilteredVariants()
    {
        var filtered = allVariants.AsEnumerable();

        // Filtre par recherche
        if (!string.IsNullOrEmpty(searchFilter))
        {
            filtered = filtered.Where(v =>
                v.VariantName.ToLower().Contains(searchFilter.ToLower()) ||
                v.ParentActivityID.ToLower().Contains(searchFilter.ToLower())
            );
        }

        // Filtre par activite
        if (selectedActivityFilter != "All")
        {
            filtered = filtered.Where(v => v.ParentActivityID == selectedActivityFilter);
        }

        // Filtre XP manquant
        if (showOnlyMissingXP)
        {
            filtered = filtered.Where(v =>
                v.MainSkillXPPerTick == 0 ||
                v.SubSkillXPPerTick == 0
            );
        }

        return filtered.ToList();
    }

    void AutoSetupMissingXP()
    {
        var variantsNeedingXP = allVariants.Where(v =>
            v.MainSkillXPPerTick == 0 || v.SubSkillXPPerTick == 0
        ).ToArray();

        if (variantsNeedingXP.Length == 0)
        {
            Logger.LogInfo("No variants need XP setup", Logger.LogCategory.EditorLog);
            return;
        }

        Undo.RecordObjects(variantsNeedingXP, "Auto Setup Missing XP");

        foreach (var variant in variantsNeedingXP)
        {
            // Auto-assigner MainSkillId
            variant.MainSkillId = variant.ParentActivityID;

            // Valeurs par defaut intelligentes
            if (variant.MainSkillXPPerTick == 0)
            {
                variant.MainSkillXPPerTick = variant.IsTimeBased ? 15 : 10;
            }

            if (variant.SubSkillXPPerTick == 0)
            {
                variant.SubSkillXPPerTick = variant.IsTimeBased ? 8 : 5;
            }

            EditorUtility.SetDirty(variant);
        }

        AssetDatabase.SaveAssets();
        Logger.LogInfo($"Auto-setup XP values for {variantsNeedingXP.Length} variants", Logger.LogCategory.EditorLog);
    }
}
#endif