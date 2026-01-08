// Purpose: Editor tool that creates game content from JSON specifications
// This allows Claude Code to write JSON files that get processed into ScriptableObjects
// Filepath: Assets/Scripts/Editor/ClaudeContentCreator.cs

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates game content from JSON specification files.
/// JSON files are placed in Assets/Editor/ContentQueue/ and processed on demand.
/// </summary>
public class ClaudeContentCreator : EditorWindow
{
    private const string QUEUE_FOLDER = "Assets/Editor/ContentQueue";
    private Vector2 scrollPosition;
    private List<string> pendingFiles = new List<string>();
    private string lastResult = "";

    [MenuItem("StepQuest/Tools/Claude Content Creator")]
    public static void ShowWindow()
    {
        var window = GetWindow<ClaudeContentCreator>("Claude Content");
        window.minSize = new Vector2(400, 300);
        window.RefreshPendingFiles();
    }

    void OnEnable()
    {
        RefreshPendingFiles();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Claude Content Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool processes JSON content specifications created by Claude Code.\n" +
            $"Place JSON files in: {QUEUE_FOLDER}", MessageType.Info);

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh", GUILayout.Width(80)))
        {
            RefreshPendingFiles();
        }
        if (GUILayout.Button("Open Queue Folder", GUILayout.Width(120)))
        {
            EnsureQueueFolderExists();
            EditorUtility.RevealInFinder(QUEUE_FOLDER);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Pending files
        EditorGUILayout.LabelField($"Pending Files: {pendingFiles.Count}", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));

        if (pendingFiles.Count == 0)
        {
            EditorGUILayout.LabelField("No pending content files.", EditorStyles.miniLabel);
        }
        else
        {
            foreach (var file in pendingFiles)
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(Path.GetFileName(file), GUILayout.ExpandWidth(true));

                if (GUILayout.Button("Process", GUILayout.Width(60)))
                {
                    ProcessFile(file);
                }
                if (GUILayout.Button("View", GUILayout.Width(40)))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(file);
                    if (asset != null) Selection.activeObject = asset;
                }
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    DeleteFile(file);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        if (pendingFiles.Count > 0)
        {
            if (GUILayout.Button("Process All Files", GUILayout.Height(30)))
            {
                ProcessAllFiles();
            }
        }

        EditorGUILayout.Space();

        // Result log
        EditorGUILayout.LabelField("Last Result:", EditorStyles.boldLabel);
        EditorGUILayout.TextArea(lastResult, GUILayout.Height(80));
    }

    private void RefreshPendingFiles()
    {
        pendingFiles.Clear();
        EnsureQueueFolderExists();

        string fullPath = Path.Combine(Application.dataPath, "Editor/ContentQueue");
        if (Directory.Exists(fullPath))
        {
            var files = Directory.GetFiles(fullPath, "*.json");
            foreach (var file in files)
            {
                string assetPath = "Assets" + file.Substring(Application.dataPath.Length).Replace("\\", "/");
                pendingFiles.Add(assetPath);
            }
        }
    }

    private void EnsureQueueFolderExists()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Editor"))
        {
            AssetDatabase.CreateFolder("Assets", "Editor");
        }
        if (!AssetDatabase.IsValidFolder(QUEUE_FOLDER))
        {
            AssetDatabase.CreateFolder("Assets/Editor", "ContentQueue");
        }
    }

    private void ProcessFile(string assetPath)
    {
        try
        {
            string fullPath = Path.Combine(Application.dataPath, assetPath.Substring(7)); // Remove "Assets/"
            string json = File.ReadAllText(fullPath);

            var spec = JsonUtility.FromJson<ContentSpecification>(json);
            var results = new List<string>();

            // Process enemies
            if (spec.enemies != null)
            {
                foreach (var enemy in spec.enemies)
                {
                    var result = CreateEnemy(enemy);
                    results.Add(result);
                }
            }

            // Process abilities
            if (spec.abilities != null)
            {
                foreach (var ability in spec.abilities)
                {
                    var result = CreateAbility(ability);
                    results.Add(result);
                }
            }

            // Process items
            if (spec.items != null)
            {
                foreach (var item in spec.items)
                {
                    var result = CreateItem(item);
                    results.Add(result);
                }
            }

            // Process NPCs
            if (spec.npcs != null)
            {
                foreach (var npc in spec.npcs)
                {
                    var result = CreateNPC(npc);
                    results.Add(result);
                }
            }

            // Process location assignments
            if (spec.locationAssignments != null)
            {
                foreach (var assignment in spec.locationAssignments)
                {
                    var result = AssignToLocation(assignment);
                    results.Add(result);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            lastResult = $"Processed {assetPath}:\n" + string.Join("\n", results);

            // Move to processed folder or delete
            MoveToProcessed(assetPath);
            RefreshPendingFiles();
        }
        catch (Exception e)
        {
            lastResult = $"Error processing {assetPath}:\n{e.Message}\n{e.StackTrace}";
            Debug.LogError(lastResult);
        }
    }

    private void ProcessAllFiles()
    {
        var filesToProcess = new List<string>(pendingFiles);
        var allResults = new List<string>();

        foreach (var file in filesToProcess)
        {
            ProcessFile(file);
            allResults.Add(lastResult);
        }

        lastResult = string.Join("\n---\n", allResults);
    }

    private void DeleteFile(string assetPath)
    {
        AssetDatabase.DeleteAsset(assetPath);
        RefreshPendingFiles();
    }

    private void MoveToProcessed(string assetPath)
    {
        // Simply delete processed files
        AssetDatabase.DeleteAsset(assetPath);
    }

    #region Content Creation Methods

    private string CreateEnemy(EnemySpec spec)
    {
        string folder = "Assets/ScriptableObjects/Enemies";
        EnsureFolderExists(folder);

        string assetPath = $"{folder}/{spec.filename ?? spec.id}.asset";

        // Check if exists
        var existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(assetPath);
        EnemyDefinition enemy = existing ?? ScriptableObject.CreateInstance<EnemyDefinition>();

        enemy.EnemyID = spec.id;
        enemy.EnemyName = spec.name;
        enemy.Description = spec.description ?? "";
        enemy.Level = spec.level > 0 ? spec.level : 1;
        enemy.MaxHealth = spec.maxHealth > 0 ? spec.maxHealth : 100;
        enemy.ExperienceReward = spec.xpReward;
        enemy.EnemyColor = ParseColor(spec.color);
        enemy.VictoryTitle = spec.victoryTitle ?? $"{spec.name} vaincu !";
        enemy.VictoryDescription = spec.victoryDescription ?? $"Vous avez vaincu le {spec.name.ToLower()}.";

        // Assign abilities
        if (spec.abilityIds != null && spec.abilityIds.Length > 0)
        {
            enemy.Abilities = new List<AbilityDefinition>();
            foreach (var abilityId in spec.abilityIds)
            {
                var ability = FindAbilityById(abilityId);
                if (ability != null)
                {
                    enemy.Abilities.Add(ability);
                }
                else
                {
                    Debug.LogWarning($"Ability not found: {abilityId}");
                }
            }
        }

        if (existing == null)
        {
            AssetDatabase.CreateAsset(enemy, assetPath);
            return $"Created enemy: {spec.name} ({spec.id})";
        }
        else
        {
            EditorUtility.SetDirty(enemy);
            return $"Updated enemy: {spec.name} ({spec.id})";
        }
    }

    private string CreateAbility(AbilitySpec spec)
    {
        string folder = spec.isEnemyAbility
            ? "Assets/ScriptableObjects/Abilities/Enemies Abilities"
            : "Assets/ScriptableObjects/Abilities";
        EnsureFolderExists(folder);

        string assetPath = $"{folder}/{spec.filename ?? spec.id}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<AbilityDefinition>(assetPath);
        AbilityDefinition ability = existing ?? ScriptableObject.CreateInstance<AbilityDefinition>();

        ability.AbilityID = spec.id;
        ability.AbilityName = spec.name;
        ability.Description = spec.description ?? "";
        ability.Cooldown = spec.cooldown > 0 ? spec.cooldown : 2f;
        ability.Weight = spec.weight > 0 ? spec.weight : 1;
        ability.AbilityColor = ParseColor(spec.color);

        // Build effects list
        ability.Effects = new List<AbilityEffect>();

        if (spec.damage > 0)
        {
            ability.Effects.Add(new AbilityEffect
            {
                Type = AbilityEffectType.Damage,
                Value = spec.damage
            });
        }

        if (spec.heal > 0)
        {
            ability.Effects.Add(new AbilityEffect
            {
                Type = AbilityEffectType.Heal,
                Value = spec.heal,
                TargetsSelf = true
            });
        }

        if (spec.shield > 0)
        {
            ability.Effects.Add(new AbilityEffect
            {
                Type = AbilityEffectType.Shield,
                Value = spec.shield,
                TargetsSelf = true
            });
        }

        if (!string.IsNullOrEmpty(spec.statusEffectId))
        {
            var statusEffect = FindStatusEffectById(spec.statusEffectId);
            if (statusEffect != null)
            {
                ability.Effects.Add(new AbilityEffect
                {
                    Type = AbilityEffectType.StatusEffect,
                    StatusEffect = statusEffect,
                    StatusEffectStacks = spec.statusEffectStacks > 0 ? spec.statusEffectStacks : 1
                });
            }
        }

        if (existing == null)
        {
            AssetDatabase.CreateAsset(ability, assetPath);
            return $"Created ability: {spec.name} ({spec.id})";
        }
        else
        {
            EditorUtility.SetDirty(ability);
            return $"Updated ability: {spec.name} ({spec.id})";
        }
    }

    private string CreateItem(ItemSpec spec)
    {
        string subfolder = spec.itemType?.ToLower() switch
        {
            "consumable" => "Consumables",
            "equipment" => "Equipment",
            _ => "Materials"
        };

        string folder = $"Assets/ScriptableObjects/Ressources/{subfolder}";
        EnsureFolderExists(folder);

        string assetPath = $"{folder}/{spec.filename ?? spec.id}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
        ItemDefinition item = existing ?? ScriptableObject.CreateInstance<ItemDefinition>();

        item.ItemID = spec.id;
        item.ItemName = spec.name;
        item.Description = spec.description ?? "";
        item.MaxStackSize = spec.maxStack > 0 ? spec.maxStack : 99;

        // Set item type
        if (Enum.TryParse<ItemType>(spec.itemType, true, out var itemType))
        {
            item.Type = itemType;
        }

        if (existing == null)
        {
            AssetDatabase.CreateAsset(item, assetPath);
            return $"Created item: {spec.name} ({spec.id})";
        }
        else
        {
            EditorUtility.SetDirty(item);
            return $"Updated item: {spec.name} ({spec.id})";
        }
    }

    private string CreateNPC(NPCSpec spec)
    {
        string folder = "Assets/ScriptableObjects/NPCs";
        EnsureFolderExists(folder);

        string assetPath = $"{folder}/{spec.filename ?? spec.id}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<NPCDefinition>(assetPath);
        NPCDefinition npc = existing ?? ScriptableObject.CreateInstance<NPCDefinition>();

        npc.NPCID = spec.id;
        npc.NPCName = spec.name;
        npc.Description = spec.description ?? "";
        npc.ThemeColor = ParseColor(spec.color);
        npc.IsActive = true;

        if (existing == null)
        {
            AssetDatabase.CreateAsset(npc, assetPath);
            return $"Created NPC: {spec.name} ({spec.id})";
        }
        else
        {
            EditorUtility.SetDirty(npc);
            return $"Updated NPC: {spec.name} ({spec.id})";
        }
    }

    private string AssignToLocation(LocationAssignmentSpec spec)
    {
        var location = FindLocationById(spec.locationId);
        if (location == null)
        {
            return $"Location not found: {spec.locationId}";
        }

        var results = new List<string>();

        // Assign enemies
        if (spec.enemyIds != null)
        {
            foreach (var enemyAssignment in spec.enemyIds)
            {
                var enemy = FindEnemyById(enemyAssignment.id);
                if (enemy == null)
                {
                    results.Add($"  Enemy not found: {enemyAssignment.id}");
                    continue;
                }

                // Check if already assigned
                bool alreadyExists = location.AvailableEnemies != null &&
                    location.AvailableEnemies.Any(e => e.EnemyReference == enemy);

                if (!alreadyExists)
                {
                    if (location.AvailableEnemies == null)
                        location.AvailableEnemies = new List<LocationEnemy>();

                    var locationEnemy = new LocationEnemy
                    {
                        EnemyReference = enemy,
                        IsAvailable = true,
                        IsHidden = enemyAssignment.isHidden,
                        Rarity = (DiscoveryRarity)enemyAssignment.rarity
                    };
                    location.AvailableEnemies.Add(locationEnemy);
                    results.Add($"  Added enemy {enemyAssignment.id} to {spec.locationId}");
                }
                else
                {
                    results.Add($"  Enemy {enemyAssignment.id} already at {spec.locationId}");
                }
            }
        }

        // Assign NPCs
        if (spec.npcIds != null)
        {
            foreach (var npcAssignment in spec.npcIds)
            {
                var npc = FindNPCById(npcAssignment.id);
                if (npc == null)
                {
                    results.Add($"  NPC not found: {npcAssignment.id}");
                    continue;
                }

                bool alreadyExists = location.AvailableNPCs != null &&
                    location.AvailableNPCs.Any(n => n.NPCReference == npc);

                if (!alreadyExists)
                {
                    if (location.AvailableNPCs == null)
                        location.AvailableNPCs = new List<LocationNPC>();

                    var locationNPC = new LocationNPC
                    {
                        NPCReference = npc,
                        IsAvailable = true,
                        IsHidden = npcAssignment.isHidden,
                        Rarity = (DiscoveryRarity)npcAssignment.rarity
                    };
                    location.AvailableNPCs.Add(locationNPC);
                    results.Add($"  Added NPC {npcAssignment.id} to {spec.locationId}");
                }
                else
                {
                    results.Add($"  NPC {npcAssignment.id} already at {spec.locationId}");
                }
            }
        }

        EditorUtility.SetDirty(location);
        return $"Location {spec.locationId}:\n" + string.Join("\n", results);
    }

    #endregion

    #region Helper Methods

    private AbilityDefinition FindAbilityById(string id)
    {
        string[] guids = AssetDatabase.FindAssets("t:AbilityDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ability = AssetDatabase.LoadAssetAtPath<AbilityDefinition>(path);
            if (ability != null && ability.AbilityID == id)
                return ability;
        }
        return null;
    }

    private StatusEffectDefinition FindStatusEffectById(string id)
    {
        string[] guids = AssetDatabase.FindAssets("t:StatusEffectDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var effect = AssetDatabase.LoadAssetAtPath<StatusEffectDefinition>(path);
            if (effect != null && effect.EffectID == id)
                return effect;
        }
        return null;
    }

    private EnemyDefinition FindEnemyById(string id)
    {
        string[] guids = AssetDatabase.FindAssets("t:EnemyDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
            if (enemy != null && enemy.EnemyID == id)
                return enemy;
        }
        return null;
    }

    private NPCDefinition FindNPCById(string id)
    {
        string[] guids = AssetDatabase.FindAssets("t:NPCDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var npc = AssetDatabase.LoadAssetAtPath<NPCDefinition>(path);
            if (npc != null && npc.NPCID == id)
                return npc;
        }
        return null;
    }

    private MapLocationDefinition FindLocationById(string id)
    {
        string[] guids = AssetDatabase.FindAssets("t:MapLocationDefinition");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var location = AssetDatabase.LoadAssetAtPath<MapLocationDefinition>(path);
            if (location != null && location.LocationID == id)
                return location;
        }
        return null;
    }

    private Color ParseColor(string hex)
    {
        if (string.IsNullOrEmpty(hex))
            return Color.white;

        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
            return color;

        return Color.white;
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

    #endregion
}

#region JSON Data Structures

[Serializable]
public class ContentSpecification
{
    public EnemySpec[] enemies;
    public AbilitySpec[] abilities;
    public ItemSpec[] items;
    public NPCSpec[] npcs;
    public LocationAssignmentSpec[] locationAssignments;
}

[Serializable]
public class EnemySpec
{
    public string id;
    public string name;
    public string filename;
    public string description;
    public int level;
    public float maxHealth;
    public int xpReward;
    public string color;
    public string[] abilityIds;
    public string victoryTitle;
    public string victoryDescription;
}

[Serializable]
public class AbilitySpec
{
    public string id;
    public string name;
    public string filename;
    public string description;
    public float cooldown;
    public int weight;
    public string color;
    public float damage;
    public float heal;
    public float shield;
    public string statusEffectId;
    public int statusEffectStacks;
    public bool isEnemyAbility;
}

[Serializable]
public class ItemSpec
{
    public string id;
    public string name;
    public string filename;
    public string description;
    public string itemType; // Material, Consumable, Equipment
    public int maxStack;
}

[Serializable]
public class NPCSpec
{
    public string id;
    public string name;
    public string filename;
    public string description;
    public string color;
}

[Serializable]
public class LocationAssignmentSpec
{
    public string locationId;
    public ContentAssignment[] enemyIds;
    public ContentAssignment[] npcIds;
}

[Serializable]
public class ContentAssignment
{
    public string id;
    public bool isHidden;
    public int rarity; // 0=Common, 1=Uncommon, 2=Rare, 3=Epic, 4=Legendary
}

#endregion

#endif
