// Purpose: Editor window for managing dialogue assets
// Filepath: Assets/Scripts/Editor/DialogueEditorWindow.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor window for creating and managing dialogue assets.
/// Provides tools for creating dialogues, assigning them to NPCs, and validation.
/// </summary>
public class DialogueEditorWindow : EditorWindow
{
    [MenuItem("StepQuest/Social/Dialogue Editor")]
    public static void ShowWindow()
    {
        var window = GetWindow<DialogueEditorWindow>();
        window.titleContent = new GUIContent("Dialogue Editor");
        window.minSize = new Vector2(700, 600);
        window.Show();
    }

    // State
    private List<DialogueDefinition> allDialogues = new List<DialogueDefinition>();
    private List<NPCDefinition> allNPCs = new List<NPCDefinition>();
    private Vector2 scrollPosition;
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Dialogues", "Create Dialogue", "NPC Assignment", "Validation", "Tree View" };
    private string searchFilter = "";

    // Create state
    private string newDialogueName = "";
    private string newDialogueId = "";
    private int newDialoguePriority = 0;
    private NPCDefinition selectedNPCForCreate = null;

    // Selection state
    private DialogueDefinition selectedDialogue = null;

    // Colors
    private static readonly Color HeaderColor = new Color(0.2f, 0.6f, 0.8f);
    private static readonly Color ValidColor = new Color(0.2f, 0.8f, 0.2f);
    private static readonly Color InvalidColor = new Color(0.8f, 0.2f, 0.2f);

    void OnEnable()
    {
        RefreshDialogueList();
        RefreshNPCList();
    }

    void OnGUI()
    {
        DrawHeader();
        EditorGUILayout.Space();

        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        switch (selectedTab)
        {
            case 0:
                DrawDialoguesTab();
                break;
            case 1:
                DrawCreateDialogueTab();
                break;
            case 2:
                DrawNPCAssignmentTab();
                break;
            case 3:
                DrawValidationTab();
                break;
            case 4:
                DrawTreeViewTab();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label("Dialogue Editor", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            RefreshDialogueList();
            RefreshNPCList();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"Dialogues: {allDialogues.Count}", EditorStyles.miniLabel);
        GUILayout.Label($"NPCs: {allNPCs.Count}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    // === DIALOGUES TAB ===

    private void DrawDialoguesTab()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        var filteredDialogues = allDialogues
            .Where(d => string.IsNullOrEmpty(searchFilter) ||
                        d.DialogueID.ToLower().Contains(searchFilter.ToLower()) ||
                        d.DialogueName.ToLower().Contains(searchFilter.ToLower()))
            .OrderBy(d => d.GetDisplayName())
            .ToList();

        EditorGUILayout.LabelField($"Showing {filteredDialogues.Count} dialogues", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        foreach (var dialogue in filteredDialogues)
        {
            DrawDialogueEntry(dialogue);
        }

        if (filteredDialogues.Count == 0)
        {
            EditorGUILayout.HelpBox("No dialogues found. Create some using the 'Create Dialogue' tab.", MessageType.Info);
        }
    }

    private void DrawDialogueEntry(DialogueDefinition dialogue)
    {
        bool isValid = dialogue.IsValid();
        GUI.backgroundColor = isValid ? Color.white : new Color(1f, 0.8f, 0.8f);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();

        // Validation indicator
        GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel);
        labelStyle.normal.textColor = isValid ? ValidColor : InvalidColor;
        EditorGUILayout.LabelField(isValid ? "✓" : "✗", labelStyle, GUILayout.Width(20));

        // Name and ID
        EditorGUILayout.LabelField(dialogue.GetDisplayName(), EditorStyles.boldLabel, GUILayout.Width(200));
        EditorGUILayout.LabelField($"ID: {dialogue.DialogueID}", GUILayout.Width(150));
        EditorGUILayout.LabelField($"Priority: {dialogue.Priority}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"Lines: {dialogue.LineCount}", GUILayout.Width(60));

        GUILayout.FlexibleSpace();

        // Select button
        if (GUILayout.Button("Select", GUILayout.Width(60)))
        {
            Selection.activeObject = dialogue;
            EditorGUIUtility.PingObject(dialogue);
        }

        EditorGUILayout.EndHorizontal();

        // Conditions summary
        if (dialogue.HasConditions)
        {
            EditorGUILayout.LabelField($"Conditions: {dialogue.GetConditionsSummary()}", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();

        GUI.backgroundColor = Color.white;
    }

    // === CREATE DIALOGUE TAB ===

    private void DrawCreateDialogueTab()
    {
        EditorGUILayout.LabelField("Create New Dialogue", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // NPC selection
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("For NPC:", GUILayout.Width(100));
        int selectedIndex = selectedNPCForCreate != null ? allNPCs.IndexOf(selectedNPCForCreate) + 1 : 0;
        string[] npcOptions = new string[] { "-- Select NPC --" }.Concat(allNPCs.Select(n => n.GetDisplayName())).ToArray();
        int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, npcOptions);
        selectedNPCForCreate = newSelectedIndex > 0 ? allNPCs[newSelectedIndex - 1] : null;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Dialogue name
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Dialogue Name:", GUILayout.Width(100));
        newDialogueName = EditorGUILayout.TextField(newDialogueName);
        EditorGUILayout.EndHorizontal();

        // Dialogue ID (auto-generated)
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Dialogue ID:", GUILayout.Width(100));
        string suggestedId = GenerateDialogueId(selectedNPCForCreate, newDialogueName);
        newDialogueId = EditorGUILayout.TextField(string.IsNullOrEmpty(newDialogueId) ? suggestedId : newDialogueId);
        if (GUILayout.Button("Auto", GUILayout.Width(50)))
        {
            newDialogueId = suggestedId;
        }
        EditorGUILayout.EndHorizontal();

        // Priority
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Priority:", GUILayout.Width(100));
        newDialoguePriority = EditorGUILayout.IntField(newDialoguePriority);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Create button
        EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newDialogueName) || string.IsNullOrEmpty(newDialogueId));
        if (GUILayout.Button("Create Dialogue", GUILayout.Height(30)))
        {
            CreateDialogue();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "After creating the dialogue, select it in the Project window to add lines and conditions.\n\n" +
            "Tip: Organize dialogues in folders like 'Dialogues/NPCName/' for better organization.",
            MessageType.Info);
    }

    private string GenerateDialogueId(NPCDefinition npc, string dialogueName)
    {
        string prefix = npc != null ? npc.NPCID + "_" : "";
        string name = dialogueName.ToLower().Replace(" ", "_").Replace("'", "");
        return prefix + name;
    }

    private void CreateDialogue()
    {
        // Determine folder path
        string folderPath = "Assets/ScriptableObjects/Dialogues";
        if (selectedNPCForCreate != null)
        {
            folderPath = $"{folderPath}/{selectedNPCForCreate.NPCID}";
        }

        // Create folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string parentFolder = Path.GetDirectoryName(folderPath).Replace("\\", "/");
            string newFolderName = Path.GetFileName(folderPath);

            // Create parent folders recursively
            CreateFolderRecursive(folderPath);
        }

        // Create the dialogue asset
        DialogueDefinition dialogue = ScriptableObject.CreateInstance<DialogueDefinition>();
        dialogue.DialogueID = newDialogueId;
        dialogue.DialogueName = newDialogueName;
        dialogue.Priority = newDialoguePriority;

        // Add a default line
        dialogue.Lines = new List<DialogueLine>
        {
            new DialogueLine
            {
                Speaker = selectedNPCForCreate?.NPCName ?? "NPC",
                Text = "Hello! This is a placeholder dialogue.",
                Choices = new List<DialogueChoice>()
            }
        };

        string assetPath = $"{folderPath}/{newDialogueName.Replace(" ", "_")}.asset";
        AssetDatabase.CreateAsset(dialogue, assetPath);

        // Assign to NPC if selected
        if (selectedNPCForCreate != null)
        {
            if (selectedNPCForCreate.Dialogues == null)
                selectedNPCForCreate.Dialogues = new List<DialogueDefinition>();

            selectedNPCForCreate.Dialogues.Add(dialogue);
            EditorUtility.SetDirty(selectedNPCForCreate);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select the new asset
        Selection.activeObject = dialogue;
        EditorGUIUtility.PingObject(dialogue);

        // Refresh lists
        RefreshDialogueList();

        // Clear form
        newDialogueName = "";
        newDialogueId = "";
        newDialoguePriority = 0;

        Logger.LogInfo($"DialogueEditorWindow: Created dialogue '{dialogue.DialogueID}'", Logger.LogCategory.General);
    }

    private void CreateFolderRecursive(string path)
    {
        string[] parts = path.Split('/');
        string currentPath = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string newPath = currentPath + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(newPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            }
            currentPath = newPath;
        }
    }

    // === NPC ASSIGNMENT TAB ===

    private void DrawNPCAssignmentTab()
    {
        EditorGUILayout.LabelField("NPC Dialogue Assignment", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        foreach (var npc in allNPCs)
        {
            DrawNPCDialogueAssignment(npc);
        }

        if (allNPCs.Count == 0)
        {
            EditorGUILayout.HelpBox("No NPCs found. Create some using WalkAndRPG > Social > NPC Manager.", MessageType.Info);
        }
    }

    private void DrawNPCDialogueAssignment(NPCDefinition npc)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(npc.GetDisplayName(), EditorStyles.boldLabel, GUILayout.Width(150));

        int dialogueCount = npc.Dialogues?.Count ?? 0;
        EditorGUILayout.LabelField($"{dialogueCount} dialogues", GUILayout.Width(80));

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Select NPC", GUILayout.Width(80)))
        {
            Selection.activeObject = npc;
            EditorGUIUtility.PingObject(npc);
        }

        EditorGUILayout.EndHorizontal();

        // List assigned dialogues
        if (npc.Dialogues != null && npc.Dialogues.Count > 0)
        {
            EditorGUI.indentLevel++;
            foreach (var dialogue in npc.Dialogues.Where(d => d != null).OrderByDescending(d => d.Priority))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"• {dialogue.GetDisplayName()} (P:{dialogue.Priority})", EditorStyles.miniLabel);

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    npc.Dialogues.Remove(dialogue);
                    EditorUtility.SetDirty(npc);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        // Add dialogue button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("+ Add Dialogue", GUILayout.Width(100)))
        {
            ShowAddDialogueMenu(npc);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void ShowAddDialogueMenu(NPCDefinition npc)
    {
        GenericMenu menu = new GenericMenu();

        var unassigned = allDialogues
            .Where(d => npc.Dialogues == null || !npc.Dialogues.Contains(d))
            .OrderBy(d => d.GetDisplayName());

        foreach (var dialogue in unassigned)
        {
            menu.AddItem(new GUIContent(dialogue.GetDisplayName()), false, () =>
            {
                if (npc.Dialogues == null)
                    npc.Dialogues = new List<DialogueDefinition>();

                npc.Dialogues.Add(dialogue);
                EditorUtility.SetDirty(npc);
            });
        }

        if (!unassigned.Any())
        {
            menu.AddDisabledItem(new GUIContent("No available dialogues"));
        }

        menu.ShowAsContext();
    }

    // === VALIDATION TAB ===

    private void DrawValidationTab()
    {
        EditorGUILayout.LabelField("Dialogue Validation", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Check for issues
        var issues = new List<string>();

        // Invalid dialogues
        var invalidDialogues = allDialogues.Where(d => !d.IsValid()).ToList();
        foreach (var d in invalidDialogues)
        {
            issues.Add($"Invalid dialogue: {d.name} (missing ID or lines)");
        }

        // NPCs without dialogues
        var npcsWithoutDialogues = allNPCs.Where(n => n.Dialogues == null || n.Dialogues.Count == 0).ToList();
        foreach (var n in npcsWithoutDialogues)
        {
            issues.Add($"NPC without dialogues: {n.GetDisplayName()}");
        }

        // Null references in NPC dialogue lists
        foreach (var npc in allNPCs)
        {
            if (npc.Dialogues != null)
            {
                int nullCount = npc.Dialogues.Count(d => d == null);
                if (nullCount > 0)
                {
                    issues.Add($"NPC {npc.GetDisplayName()} has {nullCount} null dialogue reference(s)");
                }
            }
        }

        // Display results
        if (issues.Count == 0)
        {
            EditorGUILayout.HelpBox("All dialogues are valid!", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox($"Found {issues.Count} issue(s):", MessageType.Warning);

            foreach (var issue in issues)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("⚠", GUILayout.Width(20));
                EditorGUILayout.LabelField(issue);
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();

        // Fix button
        if (issues.Count > 0)
        {
            if (GUILayout.Button("Fix Null References", GUILayout.Height(25)))
            {
                foreach (var npc in allNPCs)
                {
                    if (npc.Dialogues != null)
                    {
                        npc.Dialogues.RemoveAll(d => d == null);
                        EditorUtility.SetDirty(npc);
                    }
                }
                AssetDatabase.SaveAssets();
                RefreshDialogueList();
                RefreshNPCList();
            }
        }
    }

    // === TREE VIEW TAB ===

    private void DrawTreeViewTab()
    {
        EditorGUILayout.LabelField("Visual Dialogue Tree Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "The Tree View shows ALL dialogues for an NPC in a single graph.\n" +
            "Each dialogue appears as a separate tree with a colored header.\n" +
            "Select an NPC below and click 'Open Graph' to view and edit all their dialogues.",
            MessageType.Info);

        EditorGUILayout.Space();

        // Search filter
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // List NPCs with dialogue counts
        var filteredNPCs = allNPCs
            .Where(n => string.IsNullOrEmpty(searchFilter) ||
                        n.NPCID.ToLower().Contains(searchFilter.ToLower()) ||
                        n.NPCName.ToLower().Contains(searchFilter.ToLower()))
            .OrderBy(n => n.GetDisplayName())
            .ToList();

        EditorGUILayout.LabelField($"Showing {filteredNPCs.Count} NPCs", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        foreach (var npc in filteredNPCs)
        {
            DrawTreeViewNPCEntry(npc);
        }

        if (filteredNPCs.Count == 0)
        {
            EditorGUILayout.HelpBox("No NPCs found. Create some using WalkAndRPG > Social > NPC Manager.", MessageType.Info);
        }

        EditorGUILayout.Space();

        // Quick open standalone window button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Open Standalone Graph Editor", GUILayout.Height(25), GUILayout.Width(200)))
        {
            DialogueGraphWindow.ShowWindow();
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTreeViewNPCEntry(NPCDefinition npc)
    {
        int dialogueCount = npc.Dialogues?.Count(d => d != null) ?? 0;
        int totalLines = npc.Dialogues?.Where(d => d != null).Sum(d => d.Lines?.Count ?? 0) ?? 0;
        bool hasDialogues = dialogueCount > 0;

        GUI.backgroundColor = hasDialogues ? Color.white : new Color(1f, 0.9f, 0.8f);
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUI.backgroundColor = Color.white;

        // NPC avatar (small preview)
        if (npc.Avatar != null)
        {
            GUILayout.Label(npc.Avatar.texture, GUILayout.Width(32), GUILayout.Height(32));
        }
        else
        {
            GUILayout.Label("", GUILayout.Width(32), GUILayout.Height(32));
        }

        // NPC info
        EditorGUILayout.BeginVertical();

        EditorGUILayout.LabelField(npc.GetDisplayName(), EditorStyles.boldLabel);

        // Dialogue stats
        string statsText;
        if (hasDialogues)
        {
            int branchCount = npc.Dialogues
                .Where(d => d != null && d.Lines != null)
                .Sum(d => d.Lines.Count(l => l.HasChoices));
            statsText = $"{dialogueCount} dialogue(s), {totalLines} lines, {branchCount} branches";
        }
        else
        {
            statsText = "No dialogues";
        }

        GUIStyle statsStyle = new GUIStyle(EditorStyles.miniLabel);
        statsStyle.normal.textColor = hasDialogues ? new Color(0.6f, 0.6f, 0.6f) : new Color(0.8f, 0.5f, 0.3f);
        EditorGUILayout.LabelField(statsText, statsStyle);

        EditorGUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        // Open Graph button
        EditorGUI.BeginDisabledGroup(!hasDialogues);
        if (GUILayout.Button("Open Graph", GUILayout.Width(90), GUILayout.Height(30)))
        {
            DialogueGraphWindow.OpenForNPC(npc);
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();
    }

    // === DATA LOADING ===

    private void RefreshDialogueList()
    {
        allDialogues.Clear();
        string[] guids = AssetDatabase.FindAssets("t:DialogueDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var dialogue = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(path);
            if (dialogue != null)
                allDialogues.Add(dialogue);
        }
        allDialogues = allDialogues.OrderBy(d => d.GetDisplayName()).ToList();
    }

    private void RefreshNPCList()
    {
        allNPCs.Clear();
        string[] guids = AssetDatabase.FindAssets("t:NPCDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var npc = AssetDatabase.LoadAssetAtPath<NPCDefinition>(path);
            if (npc != null)
                allNPCs.Add(npc);
        }
        allNPCs = allNPCs.OrderBy(n => n.GetDisplayName()).ToList();
    }
}
#endif
