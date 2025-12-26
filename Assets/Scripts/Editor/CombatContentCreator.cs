// Purpose: Editor window to quickly create test abilities and enemies for combat system
// Filepath: Assets/Scripts/Editor/CombatContentCreator.cs

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CombatContentCreator : EditorWindow
{
    private Vector2 scrollPosition;

    [MenuItem("WalkAndRPG/Combat Content Creator")]
    public static void ShowWindow()
    {
        GetWindow<CombatContentCreator>("Combat Content");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Combat Content Creator", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Use this tool to quickly create test content for the combat system.", MessageType.Info);

        EditorGUILayout.Space(20);

        // Create Basic Abilities
        EditorGUILayout.LabelField("Create Test Abilities", EditorStyles.boldLabel);
        if (GUILayout.Button("Create Basic Attack Ability", GUILayout.Height(30)))
        {
            CreateBasicAttackAbility();
        }
        if (GUILayout.Button("Create Heal Ability", GUILayout.Height(30)))
        {
            CreateHealAbility();
        }
        if (GUILayout.Button("Create Poison Ability", GUILayout.Height(30)))
        {
            CreatePoisonAbility();
        }
        if (GUILayout.Button("Create Shield Ability", GUILayout.Height(30)))
        {
            CreateShieldAbility();
        }

        EditorGUILayout.Space(20);

        // Create Basic Enemies
        EditorGUILayout.LabelField("Create Test Enemies", EditorStyles.boldLabel);
        if (GUILayout.Button("Create Slime Enemy", GUILayout.Height(30)))
        {
            CreateSlimeEnemy();
        }
        if (GUILayout.Button("Create Goblin Enemy", GUILayout.Height(30)))
        {
            CreateGoblinEnemy();
        }
        if (GUILayout.Button("Create Wolf Enemy", GUILayout.Height(30)))
        {
            CreateWolfEnemy();
        }

        EditorGUILayout.Space(20);

        // Create All Test Content
        EditorGUILayout.LabelField("Bulk Creation", EditorStyles.boldLabel);
        if (GUILayout.Button("Create ALL Test Content", GUILayout.Height(40)))
        {
            CreateAllTestContent();
        }

        EditorGUILayout.EndScrollView();
    }

    private void CreateBasicAttackAbility()
    {
        var ability = CreateInstance<AbilityDefinition>();
        ability.AbilityID = "basic_attack";
        ability.AbilityName = "Basic Attack";
        ability.Description = "A simple attack that deals damage.";
        ability.Cooldown = 2f;
        ability.Weight = 1;
        ability.Effects = new List<AbilityEffect>
        {
            new AbilityEffect { Type = AbilityEffectType.Damage, Value = 10f }
        };
        ability.AbilityColor = new Color(0.8f, 0.2f, 0.2f); // Red

        SaveAbility(ability, "BasicAttack");
    }

    private void CreateHealAbility()
    {
        var ability = CreateInstance<AbilityDefinition>();
        ability.AbilityID = "heal";
        ability.AbilityName = "Heal";
        ability.Description = "Restore health.";
        ability.Cooldown = 5f;
        ability.Weight = 2;
        ability.Effects = new List<AbilityEffect>
        {
            new AbilityEffect { Type = AbilityEffectType.Heal, Value = 15f, TargetsSelf = true }
        };
        ability.AbilityColor = new Color(0.2f, 0.8f, 0.2f); // Green

        SaveAbility(ability, "Heal");
    }

    private void CreatePoisonAbility()
    {
        // Try to load the Poison status effect
        var poisonEffect = AssetDatabase.LoadAssetAtPath<StatusEffectDefinition>("Assets/ScriptableObjects/StatusEffects/Poison.asset");

        var ability = CreateInstance<AbilityDefinition>();
        ability.AbilityID = "poison_strike";
        ability.AbilityName = "Poison Strike";
        ability.Description = "Apply poison that deals damage over time.";
        ability.Cooldown = 4f;
        ability.Weight = 2;
        ability.Effects = new List<AbilityEffect>
        {
            new AbilityEffect { Type = AbilityEffectType.Damage, Value = 5f }
        };

        // Add poison status effect if available
        if (poisonEffect != null)
        {
            ability.Effects.Add(new AbilityEffect
            {
                Type = AbilityEffectType.StatusEffect,
                StatusEffect = poisonEffect,
                StatusEffectStacks = 3
            });
        }
        else
        {
            Logger.LogWarning("Poison status effect not found. Create it via WalkAndRPG/Combat/Status Effect Manager first.", Logger.LogCategory.EditorLog);
        }

        ability.AbilityColor = new Color(0.4f, 0.8f, 0.2f); // Yellow-green

        SaveAbility(ability, "PoisonStrike");
    }

    private void CreateShieldAbility()
    {
        var ability = CreateInstance<AbilityDefinition>();
        ability.AbilityID = "shield_bash";
        ability.AbilityName = "Shield Bash";
        ability.Description = "Gain shield and deal minor damage.";
        ability.Cooldown = 6f;
        ability.Weight = 2;
        ability.Effects = new List<AbilityEffect>
        {
            new AbilityEffect { Type = AbilityEffectType.Shield, Value = 20f, TargetsSelf = true },
            new AbilityEffect { Type = AbilityEffectType.Damage, Value = 5f }
        };
        ability.AbilityColor = new Color(0.8f, 0.8f, 0.2f); // Yellow

        SaveAbility(ability, "ShieldBash");
    }

    private void CreateSlimeEnemy()
    {
        // First ensure we have a basic attack for the enemy
        var attackAbility = AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/ScriptableObjects/Abilities/BasicAttack.asset");
        if (attackAbility == null)
        {
            CreateBasicAttackAbility();
            attackAbility = AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/ScriptableObjects/Abilities/BasicAttack.asset");
        }

        var enemy = CreateInstance<EnemyDefinition>();
        enemy.EnemyID = "slime";
        enemy.EnemyName = "Slime";
        enemy.Description = "A weak gelatinous creature.";
        enemy.MaxHealth = 30f;
        enemy.ExperienceReward = 5;
        enemy.VictoryTitle = "Slime Defeated!";
        enemy.VictoryDescription = "The slime dissolves into goo.";
        enemy.EnemyColor = new Color(0.2f, 0.8f, 0.2f);

        if (attackAbility != null)
        {
            enemy.Abilities = new List<AbilityDefinition> { attackAbility };
        }

        SaveEnemy(enemy, "Slime");
    }

    private void CreateGoblinEnemy()
    {
        var attackAbility = AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/ScriptableObjects/Abilities/BasicAttack.asset");
        var poisonAbility = AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/ScriptableObjects/Abilities/PoisonStrike.asset");

        var enemy = CreateInstance<EnemyDefinition>();
        enemy.EnemyID = "goblin";
        enemy.EnemyName = "Goblin";
        enemy.Description = "A cunning little creature with poisoned daggers.";
        enemy.MaxHealth = 50f;
        enemy.ExperienceReward = 15;
        enemy.VictoryTitle = "Goblin Slain!";
        enemy.VictoryDescription = "The goblin falls with a screech.";
        enemy.EnemyColor = new Color(0.2f, 0.6f, 0.2f);

        enemy.Abilities = new List<AbilityDefinition>();
        if (attackAbility != null) enemy.Abilities.Add(attackAbility);
        if (poisonAbility != null) enemy.Abilities.Add(poisonAbility);

        SaveEnemy(enemy, "Goblin");
    }

    private void CreateWolfEnemy()
    {
        var attackAbility = AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/ScriptableObjects/Abilities/BasicAttack.asset");

        // Create a fast attack for wolf
        var fastAttack = CreateInstance<AbilityDefinition>();
        fastAttack.AbilityID = "bite";
        fastAttack.AbilityName = "Bite";
        fastAttack.Description = "A quick biting attack.";
        fastAttack.Cooldown = 1.5f;
        fastAttack.Weight = 1;
        fastAttack.Effects = new List<AbilityEffect>
        {
            new AbilityEffect { Type = AbilityEffectType.Damage, Value = 8f }
        };
        fastAttack.AbilityColor = new Color(0.6f, 0.3f, 0.3f);
        SaveAbility(fastAttack, "Bite");

        var enemy = CreateInstance<EnemyDefinition>();
        enemy.EnemyID = "wolf";
        enemy.EnemyName = "Wolf";
        enemy.Description = "A fierce predator with sharp fangs.";
        enemy.MaxHealth = 70f;
        enemy.ExperienceReward = 20;
        enemy.VictoryTitle = "Wolf Defeated!";
        enemy.VictoryDescription = "The wolf retreats into the forest.";
        enemy.EnemyColor = new Color(0.5f, 0.5f, 0.5f);

        var biteAbility = AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/ScriptableObjects/Abilities/Bite.asset");
        enemy.Abilities = new List<AbilityDefinition>();
        if (biteAbility != null) enemy.Abilities.Add(biteAbility);
        if (biteAbility != null) enemy.Abilities.Add(biteAbility); // Wolf has two bite attacks

        SaveEnemy(enemy, "Wolf");
    }

    private void CreateAllTestContent()
    {
        // Create abilities first
        CreateBasicAttackAbility();
        CreateHealAbility();
        CreatePoisonAbility();
        CreateShieldAbility();

        // Refresh asset database so abilities are available
        AssetDatabase.Refresh();

        // Then create enemies
        CreateSlimeEnemy();
        CreateGoblinEnemy();
        CreateWolfEnemy();

        EditorUtility.DisplayDialog("Complete", "All test combat content has been created!", "OK");
    }

    private void SaveAbility(AbilityDefinition ability, string filename)
    {
        string folderPath = "Assets/ScriptableObjects/Abilities";
        EnsureFolderExists(folderPath);

        string assetPath = $"{folderPath}/{filename}.asset";

        // Check if already exists
        var existing = AssetDatabase.LoadAssetAtPath<AbilityDefinition>(assetPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(ability, existing);
            EditorUtility.SetDirty(existing);
            Logger.LogInfo($"Updated existing ability: {assetPath}", Logger.LogCategory.EditorLog);
        }
        else
        {
            AssetDatabase.CreateAsset(ability, assetPath);
            Logger.LogInfo($"Created new ability: {assetPath}", Logger.LogCategory.EditorLog);
        }

        AssetDatabase.SaveAssets();
    }

    private void SaveEnemy(EnemyDefinition enemy, string filename)
    {
        string folderPath = "Assets/ScriptableObjects/Enemies";
        EnsureFolderExists(folderPath);

        string assetPath = $"{folderPath}/{filename}.asset";

        // Check if already exists
        var existing = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(assetPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(enemy, existing);
            EditorUtility.SetDirty(existing);
            Logger.LogInfo($"Updated existing enemy: {assetPath}", Logger.LogCategory.EditorLog);
        }
        else
        {
            AssetDatabase.CreateAsset(enemy, assetPath);
            Logger.LogInfo($"Created new enemy: {assetPath}", Logger.LogCategory.EditorLog);
        }

        AssetDatabase.SaveAssets();
    }

    private void EnsureFolderExists(string path)
    {
        string[] folders = path.Split('/');
        string currentPath = folders[0];

        for (int i = 1; i < folders.Length; i++)
        {
            string nextFolder = folders[i];
            string nextPath = currentPath + "/" + nextFolder;

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, nextFolder);
            }

            currentPath = nextPath;
        }
    }
}
#endif
