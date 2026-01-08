// Purpose: Editor window to create and manage NPCs with location assignment
// Filepath: Assets/Scripts/Editor/NPCManagerWindow.cs

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class NPCManagerWindow : EditorWindow
{
    [MenuItem("WalkAndRPG/Social/NPC Manager")]
    public static void ShowWindow()
    {
        NPCManagerWindow window = GetWindow<NPCManagerWindow>();
        window.titleContent = new GUIContent("NPC Manager");
        window.minSize = new Vector2(600, 500);
        window.Show();
    }

    // Data
    private List<NPCDefinition> allNPCs = new List<NPCDefinition>();
    private NPCRegistry npcRegistry;
    private LocationRegistry locationRegistry;

    // UI State
    private Vector2 scrollPosition;
    private string searchFilter = "";
    private int selectedTab = 0;
    private readonly string[] tabNames = { "NPCs", "Create NPC", "Location Assignment", "Validation" };

    // Filter State
    private bool filterHasPortrait = false;
    private bool filterIsActive = false;
    private bool filterHasLocation = false;

    // Create NPC State
    private string newNPCName = "";
    private string newNPCDescription = "";
    private Sprite newNPCAvatar = null;
    private Sprite newNPCSilhouetteIcon = null;
    private Sprite newNPCIllustration = null;
    private Color newNPCColor = Color.white;
    private bool newNPCIsActive = true;
    private List<MapLocationDefinition> newNPCLocations = new List<MapLocationDefinition>();

    // Emotion sprites
    private bool showEmotionSprites = false;
    private Sprite newNPCEmotionNeutral = null;
    private Sprite newNPCEmotionJoy = null;
    private Sprite newNPCEmotionSadness = null;
    private Sprite newNPCEmotionAnger = null;
    private Sprite newNPCEmotionSurprise = null;
    private Sprite newNPCEmotionFear = null;
    private Sprite newNPCEmotionCuriosity = null;
    private Sprite newNPCEmotionEmbarrassment = null;
    private Sprite newNPCEmotionLove = null;

    // Location Assignment State
    private NPCDefinition selectedNPCForAssignment = null;
    private MapLocationDefinition selectedLocationForAssignment = null;

    void OnEnable()
    {
        RefreshNPCList();
        LoadRegistries();
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
                DrawNPCsTab();
                break;
            case 1:
                DrawCreateNPCTab();
                break;
            case 2:
                DrawLocationAssignmentTab();
                break;
            case 3:
                DrawValidationTab();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    #region Header
    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");

        GUILayout.Label("NPC Manager", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            RefreshNPCList();
            LoadRegistries();
        }

        EditorGUILayout.LabelField($"Total NPCs: {allNPCs.Count}", GUILayout.Width(100));

        // Registry status
        string registryStatus = npcRegistry != null ? "Registry OK" : "No Registry";
        string locationStatus = locationRegistry != null ? "Locations OK" : "No Locations";
        EditorGUILayout.LabelField($"[{registryStatus}] [{locationStatus}]", EditorStyles.miniLabel);

        EditorGUILayout.EndHorizontal();

        // Search and filters (only for NPCs tab)
        if (selectedTab == 0)
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            filterHasPortrait = EditorGUILayout.Toggle("Has Portrait", filterHasPortrait, GUILayout.Width(100));
            filterIsActive = EditorGUILayout.Toggle("Active Only", filterIsActive, GUILayout.Width(100));
            filterHasLocation = EditorGUILayout.Toggle("Has Location", filterHasLocation, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }
    #endregion

    #region NPCs Tab
    private void DrawNPCsTab()
    {
        // Create New NPC button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create New NPC", GUILayout.Width(150)))
        {
            selectedTab = 1; // Switch to Create tab
            ResetCreateNPCForm();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        var filteredNPCs = GetFilteredNPCs();

        if (filteredNPCs.Count == 0)
        {
            EditorGUILayout.HelpBox("No NPCs found matching current filters.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Showing: {filteredNPCs.Count} NPCs", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        foreach (var npc in filteredNPCs)
        {
            DrawNPCEntry(npc);
        }
    }

    private void DrawNPCEntry(NPCDefinition npc)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();

        // Avatar preview
        Sprite displaySprite = npc.Avatar != null ? npc.Avatar : npc.Illustration;
        if (displaySprite != null)
        {
            Rect iconRect = EditorGUILayout.GetControlRect(GUILayout.Width(40), GUILayout.Height(40));
            DrawSprite(iconRect, displaySprite);
        }
        else
        {
            EditorGUILayout.LabelField("[No Image]", GUILayout.Width(50), GUILayout.Height(40));
        }

        EditorGUILayout.BeginVertical();

        // Name and status
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(npc.GetDisplayName(), EditorStyles.boldLabel, GUILayout.Width(150));

        // Active status
        var oldColor = GUI.color;
        GUI.color = npc.IsActive ? Color.green : Color.gray;
        EditorGUILayout.LabelField(npc.IsActive ? "Active" : "Inactive", EditorStyles.miniLabel, GUILayout.Width(50));
        GUI.color = oldColor;

        // Feature badges
        if (npc.EmotionNeutral != null)
        {
            GUI.color = new Color(0.4f, 0.8f, 1f);
            EditorGUILayout.LabelField("[EMO]", EditorStyles.miniLabel, GUILayout.Width(35));
            GUI.color = oldColor;
        }

        if (npc.Dialogues != null && npc.Dialogues.Count > 0)
        {
            GUI.color = new Color(1f, 0.8f, 0.4f);
            EditorGUILayout.LabelField("[DLG]", EditorStyles.miniLabel, GUILayout.Width(35));
            GUI.color = oldColor;
        }

        // Color preview
        EditorGUI.DrawRect(EditorGUILayout.GetControlRect(GUILayout.Width(20), GUILayout.Height(16)), npc.ThemeColor);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Edit", GUILayout.Width(40)))
        {
            Selection.activeObject = npc;
            EditorGUIUtility.PingObject(npc);
        }

        if (GUILayout.Button("Delete", GUILayout.Width(50)))
        {
            DeleteNPC(npc);
        }

        EditorGUILayout.EndHorizontal();

        // Description
        if (!string.IsNullOrEmpty(npc.Description))
        {
            EditorGUILayout.LabelField(npc.Description, EditorStyles.wordWrappedMiniLabel);
        }

        // Locations
        var locations = GetLocationsForNPC(npc);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Locations:", EditorStyles.miniLabel, GUILayout.Width(60));
        if (locations.Count > 0)
        {
            string locationNames = string.Join(", ", locations.Select(l => l?.DisplayName ?? "(null)"));
            EditorGUILayout.LabelField(locationNames, EditorStyles.miniLabel);
        }
        else
        {
            var warnColor = GUI.color;
            GUI.color = Color.yellow;
            EditorGUILayout.LabelField("(Not assigned to any location)", EditorStyles.miniLabel);
            GUI.color = warnColor;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }
    #endregion

    #region Create NPC Tab
    private void DrawCreateNPCTab()
    {
        EditorGUILayout.LabelField("Create New NPC", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Basic Info
        EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
        newNPCName = EditorGUILayout.TextField("Name", newNPCName);
        newNPCDescription = EditorGUILayout.TextField("Description", newNPCDescription);

        EditorGUILayout.Space();

        // Visual
        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        newNPCAvatar = (Sprite)EditorGUILayout.ObjectField("Avatar (small)", newNPCAvatar, typeof(Sprite), false);
        newNPCSilhouetteIcon = (Sprite)EditorGUILayout.ObjectField("Silhouette Icon (exploration)", newNPCSilhouetteIcon, typeof(Sprite), false);
        newNPCIllustration = (Sprite)EditorGUILayout.ObjectField("Illustration (large)", newNPCIllustration, typeof(Sprite), false);
        newNPCColor = EditorGUILayout.ColorField("Theme Color", newNPCColor);

        EditorGUILayout.Space();

        // Emotion Sprites (collapsible)
        showEmotionSprites = EditorGUILayout.Foldout(showEmotionSprites, "Dialogue Emotion Sprites", true);
        if (showEmotionSprites)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.HelpBox("Optional: Add emotion sprites for dialogue expressions. Falls back to Illustration if not set.", MessageType.Info);
            newNPCEmotionNeutral = (Sprite)EditorGUILayout.ObjectField("Neutral", newNPCEmotionNeutral, typeof(Sprite), false);
            newNPCEmotionJoy = (Sprite)EditorGUILayout.ObjectField("Joy", newNPCEmotionJoy, typeof(Sprite), false);
            newNPCEmotionSadness = (Sprite)EditorGUILayout.ObjectField("Sadness", newNPCEmotionSadness, typeof(Sprite), false);
            newNPCEmotionAnger = (Sprite)EditorGUILayout.ObjectField("Anger", newNPCEmotionAnger, typeof(Sprite), false);
            newNPCEmotionSurprise = (Sprite)EditorGUILayout.ObjectField("Surprise", newNPCEmotionSurprise, typeof(Sprite), false);
            newNPCEmotionFear = (Sprite)EditorGUILayout.ObjectField("Fear", newNPCEmotionFear, typeof(Sprite), false);
            newNPCEmotionCuriosity = (Sprite)EditorGUILayout.ObjectField("Curiosity", newNPCEmotionCuriosity, typeof(Sprite), false);
            newNPCEmotionEmbarrassment = (Sprite)EditorGUILayout.ObjectField("Embarrassment", newNPCEmotionEmbarrassment, typeof(Sprite), false);
            newNPCEmotionLove = (Sprite)EditorGUILayout.ObjectField("Love", newNPCEmotionLove, typeof(Sprite), false);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // Status
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        newNPCIsActive = EditorGUILayout.Toggle("Is Active", newNPCIsActive);

        EditorGUILayout.Space();

        // Location Assignment
        EditorGUILayout.LabelField("Assign to Locations", EditorStyles.boldLabel);
        DrawLocationAssignmentList();

        EditorGUILayout.Space();

        // Create button
        GUI.enabled = !string.IsNullOrEmpty(newNPCName);
        if (GUILayout.Button("Create NPC", GUILayout.Height(30)))
        {
            CreateNewNPC();
        }
        GUI.enabled = true;

        if (string.IsNullOrEmpty(newNPCName))
        {
            EditorGUILayout.HelpBox("NPC must have a name.", MessageType.Warning);
        }
    }

    private void DrawLocationAssignmentList()
    {
        if (locationRegistry == null)
        {
            EditorGUILayout.HelpBox("No LocationRegistry found. NPCs can be assigned to locations later.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginVertical("helpBox");

        // Current assignments
        for (int i = newNPCLocations.Count - 1; i >= 0; i--)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(newNPCLocations[i]?.DisplayName ?? "(null)", GUILayout.Width(200));
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                newNPCLocations.RemoveAt(i);
            }
            EditorGUILayout.EndHorizontal();
        }

        // Add location field
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Add Location:", GUILayout.Width(80));
        var newLocation = (MapLocationDefinition)EditorGUILayout.ObjectField(null, typeof(MapLocationDefinition), false);
        if (newLocation != null && !newNPCLocations.Contains(newLocation))
        {
            newNPCLocations.Add(newLocation);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void CreateNewNPC()
    {
        var npc = CreateInstance<NPCDefinition>();
        npc.NPCName = newNPCName;
        npc.NPCID = newNPCName.ToLower().Replace(" ", "_").Replace("'", "");
        npc.Description = newNPCDescription;
        npc.Avatar = newNPCAvatar;
        npc.SilhouetteIcon = newNPCSilhouetteIcon;
        npc.Illustration = newNPCIllustration;
        npc.ThemeColor = newNPCColor;
        npc.IsActive = newNPCIsActive;

        // Emotion sprites
        npc.EmotionNeutral = newNPCEmotionNeutral;
        npc.EmotionJoy = newNPCEmotionJoy;
        npc.EmotionSadness = newNPCEmotionSadness;
        npc.EmotionAnger = newNPCEmotionAnger;
        npc.EmotionSurprise = newNPCEmotionSurprise;
        npc.EmotionFear = newNPCEmotionFear;
        npc.EmotionCuriosity = newNPCEmotionCuriosity;
        npc.EmotionEmbarrassment = newNPCEmotionEmbarrassment;
        npc.EmotionLove = newNPCEmotionLove;

        // Save NPC asset
        string folder = "Assets/ScriptableObjects/NPCs";
        EnsureFolderExists(folder);
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{npc.NPCName}.asset");
        AssetDatabase.CreateAsset(npc, assetPath);

        // Add to locations (using LocationNPC wrapper)
        foreach (var location in newNPCLocations)
        {
            if (location != null && !LocationContainsNPC(location, npc))
            {
                var locationNPC = new LocationNPC { NPCReference = npc, IsAvailable = true };
                location.AvailableNPCs.Add(locationNPC);
                EditorUtility.SetDirty(location);
            }
        }

        // Add to registry if available
        if (npcRegistry != null)
        {
            npcRegistry.AllNPCs.Add(npc);
            EditorUtility.SetDirty(npcRegistry);
        }

        AssetDatabase.SaveAssets();

        allNPCs.Add(npc);

        Selection.activeObject = npc;
        EditorGUIUtility.PingObject(npc);

        Logger.LogInfo($"Created NPC: {npc.NPCName} at {assetPath}", Logger.LogCategory.EditorLog);
        if (newNPCLocations.Count > 0)
        {
            Logger.LogInfo($"  Assigned to: {string.Join(", ", newNPCLocations.Select(l => l.DisplayName))}", Logger.LogCategory.EditorLog);
        }

        ResetCreateNPCForm();
        selectedTab = 0; // Go back to list
    }

    private bool LocationContainsNPC(MapLocationDefinition location, NPCDefinition npc)
    {
        if (location?.AvailableNPCs == null || npc == null) return false;
        return location.AvailableNPCs.Any(ln => ln?.NPCReference == npc);
    }

    private void ResetCreateNPCForm()
    {
        newNPCName = "";
        newNPCDescription = "";
        newNPCAvatar = null;
        newNPCSilhouetteIcon = null;
        newNPCIllustration = null;
        newNPCColor = Color.white;
        newNPCIsActive = true;
        newNPCLocations.Clear();

        // Reset emotion sprites
        showEmotionSprites = false;
        newNPCEmotionNeutral = null;
        newNPCEmotionJoy = null;
        newNPCEmotionSadness = null;
        newNPCEmotionAnger = null;
        newNPCEmotionSurprise = null;
        newNPCEmotionFear = null;
        newNPCEmotionCuriosity = null;
        newNPCEmotionEmbarrassment = null;
        newNPCEmotionLove = null;
    }
    #endregion

    #region Location Assignment Tab
    private void DrawLocationAssignmentTab()
    {
        EditorGUILayout.LabelField("NPC Location Assignment", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Assign NPCs to locations. Changes are automatically synced bidirectionally.", MessageType.Info);
        EditorGUILayout.Space();

        if (locationRegistry == null)
        {
            EditorGUILayout.HelpBox("No LocationRegistry found. Please create one first.", MessageType.Warning);
            return;
        }

        EditorGUILayout.BeginHorizontal();

        // Left panel - NPCs
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width / 2 - 20));
        EditorGUILayout.LabelField("Select NPC", EditorStyles.boldLabel);

        selectedNPCForAssignment = (NPCDefinition)EditorGUILayout.ObjectField(selectedNPCForAssignment, typeof(NPCDefinition), false);

        if (selectedNPCForAssignment != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current Locations:", EditorStyles.boldLabel);

            var currentLocations = GetLocationsForNPC(selectedNPCForAssignment);

            if (currentLocations.Count == 0)
            {
                EditorGUILayout.LabelField("  (None)", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var loc in currentLocations)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"  {loc.DisplayName}", GUILayout.Width(150));
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        RemoveNPCFromLocation(selectedNPCForAssignment, loc);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space();

            // Quick add to location
            EditorGUILayout.LabelField("Add to Location:", EditorStyles.boldLabel);
            var addToLocation = (MapLocationDefinition)EditorGUILayout.ObjectField(null, typeof(MapLocationDefinition), false);
            if (addToLocation != null)
            {
                AddNPCToLocation(selectedNPCForAssignment, addToLocation);
            }
        }

        EditorGUILayout.EndVertical();

        // Right panel - Locations
        EditorGUILayout.BeginVertical("box", GUILayout.Width(position.width / 2 - 20));
        EditorGUILayout.LabelField("Select Location", EditorStyles.boldLabel);

        selectedLocationForAssignment = (MapLocationDefinition)EditorGUILayout.ObjectField(selectedLocationForAssignment, typeof(MapLocationDefinition), false);

        if (selectedLocationForAssignment != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("NPCs at this location:", EditorStyles.boldLabel);

            var npcsAtLocation = selectedLocationForAssignment.GetAvailableNPCs();

            if (npcsAtLocation.Count == 0)
            {
                EditorGUILayout.LabelField("  (None)", EditorStyles.miniLabel);
            }
            else
            {
                foreach (var locationNpc in npcsAtLocation)
                {
                    if (locationNpc?.NPCReference == null) continue;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"  {locationNpc.GetDisplayName()}", GUILayout.Width(150));
                    if (GUILayout.Button("Remove", GUILayout.Width(60)))
                    {
                        RemoveNPCFromLocation(locationNpc.NPCReference, selectedLocationForAssignment);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space();

            // Quick add NPC
            EditorGUILayout.LabelField("Add NPC:", EditorStyles.boldLabel);
            var addNPC = (NPCDefinition)EditorGUILayout.ObjectField(null, typeof(NPCDefinition), false);
            if (addNPC != null)
            {
                AddNPCToLocation(addNPC, selectedLocationForAssignment);
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // All locations overview
        DrawAllLocationsNPCOverview();
    }

    private void DrawAllLocationsNPCOverview()
    {
        EditorGUILayout.LabelField("All Locations Overview", EditorStyles.boldLabel);

        if (locationRegistry?.AllLocations == null) return;

        foreach (var location in locationRegistry.AllLocations.Where(l => l != null))
        {
            var npcs = location.GetAvailableNPCs();

            EditorGUILayout.BeginHorizontal("helpBox");
            EditorGUILayout.LabelField(location.DisplayName, EditorStyles.boldLabel, GUILayout.Width(150));

            if (npcs.Count > 0)
            {
                string npcNames = string.Join(", ", npcs.Select(n => n.GetDisplayName()));
                EditorGUILayout.LabelField(npcNames, EditorStyles.miniLabel);
            }
            else
            {
                var oldColor = GUI.color;
                GUI.color = Color.gray;
                EditorGUILayout.LabelField("(No NPCs)", EditorStyles.miniLabel);
                GUI.color = oldColor;
            }

            if (GUILayout.Button("Edit", GUILayout.Width(40)))
            {
                Selection.activeObject = location;
                EditorGUIUtility.PingObject(location);
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void AddNPCToLocation(NPCDefinition npc, MapLocationDefinition location)
    {
        if (npc == null || location == null) return;

        if (LocationContainsNPC(location, npc))
        {
            Logger.LogInfo($"NPC '{npc.GetDisplayName()}' already at '{location.DisplayName}'", Logger.LogCategory.EditorLog);
            return;
        }

        var locationNPC = new LocationNPC { NPCReference = npc, IsAvailable = true };
        location.AvailableNPCs.Add(locationNPC);
        EditorUtility.SetDirty(location);
        AssetDatabase.SaveAssets();

        Logger.LogInfo($"Added NPC '{npc.GetDisplayName()}' to '{location.DisplayName}'", Logger.LogCategory.EditorLog);
    }

    private void RemoveNPCFromLocation(NPCDefinition npc, MapLocationDefinition location)
    {
        if (npc == null || location == null) return;

        var toRemove = location.AvailableNPCs.FirstOrDefault(ln => ln?.NPCReference == npc);
        if (toRemove != null && location.AvailableNPCs.Remove(toRemove))
        {
            EditorUtility.SetDirty(location);
            AssetDatabase.SaveAssets();
            Logger.LogInfo($"Removed NPC '{npc.GetDisplayName()}' from '{location.DisplayName}'", Logger.LogCategory.EditorLog);
        }
    }
    #endregion

    #region Validation Tab
    private void DrawValidationTab()
    {
        EditorGUILayout.LabelField("NPC Validation", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("Run Validation", GUILayout.Height(30)))
        {
            RefreshNPCList();
        }

        EditorGUILayout.Space();

        // Statistics
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total NPCs: {allNPCs.Count}");

        int withPortrait = allNPCs.Count(n => n.Avatar != null || n.Illustration != null);
        int withSilhouette = allNPCs.Count(n => n.SilhouetteIcon != null);
        int withEmotions = allNPCs.Count(n => n.EmotionNeutral != null);
        int activeNPCs = allNPCs.Count(n => n.IsActive);
        int withLocation = allNPCs.Count(n => GetLocationsForNPC(n).Count > 0);
        int withDialogues = allNPCs.Count(n => n.Dialogues != null && n.Dialogues.Count > 0);

        EditorGUILayout.LabelField($"With Portrait: {withPortrait}/{allNPCs.Count}");
        EditorGUILayout.LabelField($"With Silhouette: {withSilhouette}/{allNPCs.Count}");
        EditorGUILayout.LabelField($"With Emotion Sprites: {withEmotions}/{allNPCs.Count}");
        EditorGUILayout.LabelField($"With Dialogues: {withDialogues}/{allNPCs.Count}");
        EditorGUILayout.LabelField($"Active: {activeNPCs}/{allNPCs.Count}");
        EditorGUILayout.LabelField($"Assigned to Location: {withLocation}/{allNPCs.Count}");

        EditorGUILayout.Space();

        // Issues
        EditorGUILayout.LabelField("Issues", EditorStyles.boldLabel);

        var issues = FindValidationIssues();
        if (issues.Count == 0)
        {
            EditorGUILayout.HelpBox("No issues found!", MessageType.Info);
        }
        else
        {
            foreach (var issue in issues)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(issue.message, EditorStyles.wordWrappedLabel);
                if (issue.npc != null && GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    Selection.activeObject = issue.npc;
                    EditorGUIUtility.PingObject(issue.npc);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();

        // Registry sync
        EditorGUILayout.LabelField("Registry Sync", EditorStyles.boldLabel);
        if (npcRegistry != null)
        {
            int inRegistry = npcRegistry.AllNPCs.Count(n => n != null);
            int inAssets = allNPCs.Count;

            if (inRegistry != inAssets)
            {
                EditorGUILayout.HelpBox($"Registry has {inRegistry} NPCs, but {inAssets} NPC assets found.", MessageType.Warning);

                if (GUILayout.Button("Sync Registry with Assets"))
                {
                    SyncRegistryWithAssets();
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"Registry is in sync ({inRegistry} NPCs)", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No NPCRegistry found.", MessageType.Warning);
            if (GUILayout.Button("Create NPC Registry"))
            {
                CreateNPCRegistry();
            }
        }
    }

    private struct ValidationIssue
    {
        public NPCDefinition npc;
        public string message;
    }

    private List<ValidationIssue> FindValidationIssues()
    {
        var issues = new List<ValidationIssue>();

        foreach (var npc in allNPCs)
        {
            if (npc == null) continue;

            if (string.IsNullOrEmpty(npc.NPCID))
            {
                issues.Add(new ValidationIssue { npc = npc, message = $"{npc.name}: Missing NPCID" });
            }

            if (string.IsNullOrEmpty(npc.NPCName))
            {
                issues.Add(new ValidationIssue { npc = npc, message = $"{npc.name}: Missing NPCName" });
            }

            if (npc.Avatar == null && npc.Illustration == null)
            {
                issues.Add(new ValidationIssue { npc = npc, message = $"{npc.GetDisplayName()}: No avatar or illustration" });
            }

            if (GetLocationsForNPC(npc).Count == 0)
            {
                issues.Add(new ValidationIssue { npc = npc, message = $"{npc.GetDisplayName()}: Not assigned to any location" });
            }
        }

        return issues;
    }

    private void SyncRegistryWithAssets()
    {
        if (npcRegistry == null) return;

        npcRegistry.AllNPCs.Clear();
        npcRegistry.AllNPCs.AddRange(allNPCs.Where(n => n != null));
        npcRegistry.CleanNullReferences();

        EditorUtility.SetDirty(npcRegistry);
        AssetDatabase.SaveAssets();

        Logger.LogInfo($"Synced NPCRegistry with {npcRegistry.AllNPCs.Count} NPCs", Logger.LogCategory.EditorLog);
    }

    private void CreateNPCRegistry()
    {
        var registry = CreateInstance<NPCRegistry>();

        string folder = "Assets/ScriptableObjects/Registries";
        EnsureFolderExists(folder);

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/NPCRegistry.asset");
        AssetDatabase.CreateAsset(registry, assetPath);

        // Add all existing NPCs
        registry.AllNPCs.AddRange(allNPCs.Where(n => n != null));

        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();

        npcRegistry = registry;

        Selection.activeObject = registry;
        EditorGUIUtility.PingObject(registry);

        Logger.LogInfo($"Created NPCRegistry at {assetPath} with {registry.AllNPCs.Count} NPCs", Logger.LogCategory.EditorLog);
    }
    #endregion

    #region Utility Methods
    private void RefreshNPCList()
    {
        allNPCs.Clear();

        string[] guids = AssetDatabase.FindAssets("t:NPCDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var npc = AssetDatabase.LoadAssetAtPath<NPCDefinition>(path);
            if (npc != null)
            {
                allNPCs.Add(npc);
            }
        }

        allNPCs = allNPCs.OrderBy(n => n.GetDisplayName()).ToList();
    }

    private void LoadRegistries()
    {
        // Load NPC Registry
        if (npcRegistry == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:NPCRegistry");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                npcRegistry = AssetDatabase.LoadAssetAtPath<NPCRegistry>(path);
            }
        }

        // Load Location Registry
        if (locationRegistry == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:LocationRegistry");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                locationRegistry = AssetDatabase.LoadAssetAtPath<LocationRegistry>(path);
            }
        }
    }

    private List<NPCDefinition> GetFilteredNPCs()
    {
        var filtered = allNPCs.Where(n => n != null);

        // Search filter
        if (!string.IsNullOrEmpty(searchFilter))
        {
            filtered = filtered.Where(n =>
                n.GetDisplayName().ToLower().Contains(searchFilter.ToLower()) ||
                n.NPCID.ToLower().Contains(searchFilter.ToLower()) ||
                (n.Description != null && n.Description.ToLower().Contains(searchFilter.ToLower())));
        }

        // Has portrait filter (avatar or illustration)
        if (filterHasPortrait)
        {
            filtered = filtered.Where(n => n.Avatar != null || n.Illustration != null);
        }

        // Is active filter
        if (filterIsActive)
        {
            filtered = filtered.Where(n => n.IsActive);
        }

        // Has location filter
        if (filterHasLocation)
        {
            filtered = filtered.Where(n => GetLocationsForNPC(n).Count > 0);
        }

        return filtered.ToList();
    }

    private List<MapLocationDefinition> GetLocationsForNPC(NPCDefinition npc)
    {
        var locations = new List<MapLocationDefinition>();

        if (locationRegistry?.AllLocations == null || npc == null) return locations;

        foreach (var location in locationRegistry.AllLocations.Where(l => l != null))
        {
            if (LocationContainsNPC(location, npc))
            {
                locations.Add(location);
            }
        }

        return locations;
    }

    private void DeleteNPC(NPCDefinition npc)
    {
        if (npc == null) return;

        bool confirm = EditorUtility.DisplayDialog(
            "Delete NPC",
            $"Delete '{npc.GetDisplayName()}'?\n\nThis will also remove it from all locations.",
            "Delete", "Cancel");

        if (confirm)
        {
            // Remove from all locations
            if (locationRegistry?.AllLocations != null)
            {
                foreach (var location in locationRegistry.AllLocations.Where(l => l != null))
                {
                    var toRemove = location.AvailableNPCs.FirstOrDefault(ln => ln?.NPCReference == npc);
                    if (toRemove != null && location.AvailableNPCs.Remove(toRemove))
                    {
                        EditorUtility.SetDirty(location);
                    }
                }
            }

            // Remove from registry
            if (npcRegistry != null)
            {
                npcRegistry.AllNPCs.Remove(npc);
                EditorUtility.SetDirty(npcRegistry);
            }

            allNPCs.Remove(npc);

            string assetPath = AssetDatabase.GetAssetPath(npc);
            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
                Logger.LogInfo($"Deleted NPC '{npc.GetDisplayName()}' at {assetPath}", Logger.LogCategory.EditorLog);
            }

            AssetDatabase.SaveAssets();
        }
    }

    private void EnsureFolderExists(string fullPath)
    {
        string[] pathParts = fullPath.Split('/');
        string currentPath = pathParts[0];

        for (int i = 1; i < pathParts.Length; i++)
        {
            string nextPath = currentPath + "/" + pathParts[i];

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, pathParts[i]);
            }

            currentPath = nextPath;
        }
    }

    private void DrawSprite(Rect rect, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return;

        Texture2D tex = sprite.texture;
        Rect spriteRect = sprite.textureRect;

        Rect texCoords = new Rect(
            spriteRect.x / tex.width,
            spriteRect.y / tex.height,
            spriteRect.width / tex.width,
            spriteRect.height / tex.height
        );

        GUI.DrawTextureWithTexCoords(rect, tex, texCoords);
    }
    #endregion
}
#endif
