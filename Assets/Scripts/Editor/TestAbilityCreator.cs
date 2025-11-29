// Purpose: Editor tool to create test abilities for all status effects
// Filepath: Assets/Scripts/Editor/TestAbilityCreator.cs

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tool to create test abilities that apply each status effect.
/// </summary>
public class TestAbilityCreator : EditorWindow
{
    [MenuItem("WalkAndRPG/Combat/Create Test Abilities (All Status Effects)")]
    public static void CreateTestAbilities()
    {
        string abilityFolder = "Assets/ScriptableObjects/Abilities";
        string statusFolder = "Assets/ScriptableObjects/StatusEffects";

        // Create folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(abilityFolder))
        {
            string parent = "Assets/ScriptableObjects";
            if (!AssetDatabase.IsValidFolder(parent))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            AssetDatabase.CreateFolder(parent, "Abilities");
        }

        // Load status effects
        var poison = AssetDatabase.LoadAssetAtPath<StatusEffectDefinition>($"{statusFolder}/Poison.asset");
        var burn = AssetDatabase.LoadAssetAtPath<StatusEffectDefinition>($"{statusFolder}/Burn.asset");
        var stun = AssetDatabase.LoadAssetAtPath<StatusEffectDefinition>($"{statusFolder}/Stun.asset");
        var regen = AssetDatabase.LoadAssetAtPath<StatusEffectDefinition>($"{statusFolder}/Regeneration.asset");
        var attackUp = AssetDatabase.LoadAssetAtPath<StatusEffectDefinition>($"{statusFolder}/AttackUp.asset");
        var defenseDown = AssetDatabase.LoadAssetAtPath<StatusEffectDefinition>($"{statusFolder}/DefenseDown.asset");

        int created = 0;

        // 1. Burn Strike - applies burn to enemy
        if (burn != null)
        {
            created += CreateAbility(abilityFolder, "BurnStrike", "Burn Strike",
                "Sets the enemy on fire, dealing damage over time.",
                new Color(1f, 0.4f, 0f), 3f,
                new AbilityEffect { Type = AbilityEffectType.Damage, Value = 5, TargetsSelf = false },
                new AbilityEffect { Type = AbilityEffectType.StatusEffect, StatusEffect = burn, StatusEffectStacks = 1, TargetsSelf = false });
        }

        // 2. Stunning Blow - stuns enemy
        if (stun != null)
        {
            created += CreateAbility(abilityFolder, "StunningBlow", "Stunning Blow",
                "A powerful strike that stuns the enemy.",
                new Color(1f, 1f, 0f), 6f,
                new AbilityEffect { Type = AbilityEffectType.Damage, Value = 3, TargetsSelf = false },
                new AbilityEffect { Type = AbilityEffectType.StatusEffect, StatusEffect = stun, StatusEffectStacks = 1, TargetsSelf = false });
        }

        // 3. Regenerate - applies regen to self
        if (regen != null)
        {
            created += CreateAbility(abilityFolder, "Regenerate", "Regenerate",
                "Channel healing energy, regenerating health over time.",
                new Color(0f, 1f, 0f), 8f,
                new AbilityEffect { Type = AbilityEffectType.StatusEffect, StatusEffect = regen, StatusEffectStacks = 1, TargetsSelf = true });
        }

        // 4. Battle Cry - applies attack buff to self
        if (attackUp != null)
        {
            created += CreateAbility(abilityFolder, "BattleCry", "Battle Cry",
                "Let out a war cry, increasing attack power.",
                new Color(1f, 0f, 0f), 10f,
                new AbilityEffect { Type = AbilityEffectType.StatusEffect, StatusEffect = attackUp, StatusEffectStacks = 1, TargetsSelf = true });
        }

        // 5. Armor Break - applies defense down to enemy
        if (defenseDown != null)
        {
            created += CreateAbility(abilityFolder, "ArmorBreak", "Armor Break",
                "Shatters enemy armor, making them take more damage.",
                new Color(0f, 0.5f, 1f), 5f,
                new AbilityEffect { Type = AbilityEffectType.Damage, Value = 2, TargetsSelf = false },
                new AbilityEffect { Type = AbilityEffectType.StatusEffect, StatusEffect = defenseDown, StatusEffectStacks = 1, TargetsSelf = false });
        }

        // 6. Venom Strike - updated poison using new system
        if (poison != null)
        {
            created += CreateAbility(abilityFolder, "VenomStrike", "Venom Strike",
                "Injects deadly venom that stacks and deals increasing damage.",
                new Color(0.5f, 0f, 0.5f), 3f,
                new AbilityEffect { Type = AbilityEffectType.Damage, Value = 3, TargetsSelf = false },
                new AbilityEffect { Type = AbilityEffectType.StatusEffect, StatusEffect = poison, StatusEffectStacks = 2, TargetsSelf = false });
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"TestAbilityCreator: Created {created} test abilities in {abilityFolder}");
        EditorUtility.DisplayDialog("Test Abilities Created",
            $"Created {created} test abilities:\n" +
            "- Burn Strike (applies Burn)\n" +
            "- Stunning Blow (applies Stun)\n" +
            "- Regenerate (applies Regen to self)\n" +
            "- Battle Cry (applies Attack Up to self)\n" +
            "- Armor Break (applies Defense Down)\n" +
            "- Venom Strike (applies Poison stacks)\n\n" +
            $"Location: {abilityFolder}",
            "OK");
    }

    private static int CreateAbility(string folder, string fileName, string displayName, string description,
        Color color, float cooldown, params AbilityEffect[] effects)
    {
        string path = Path.Combine(folder, $"{fileName}.asset");

        // Check if already exists
        var existing = AssetDatabase.LoadAssetAtPath<AbilityDefinition>(path);
        if (existing != null)
        {
            Debug.Log($"TestAbilityCreator: {fileName} already exists, skipping.");
            return 0;
        }

        var ability = ScriptableObject.CreateInstance<AbilityDefinition>();
        ability.AbilityID = displayName.ToLower().Replace(" ", "_");
        ability.AbilityName = displayName;
        ability.Description = description;
        ability.AbilityColor = color;
        ability.Cooldown = cooldown;
        ability.Weight = 1;

        // Add effects using the new system
        ability.Effects = new System.Collections.Generic.List<AbilityEffect>();
        foreach (var effect in effects)
        {
            ability.Effects.Add(effect);
        }

        AssetDatabase.CreateAsset(ability, path);
        Debug.Log($"TestAbilityCreator: Created {fileName}");
        return 1;
    }
}
#endif
