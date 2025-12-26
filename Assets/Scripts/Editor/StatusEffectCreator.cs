// Purpose: Editor tool to create test status effect assets
// Filepath: Assets/Scripts/Editor/StatusEffectCreator.cs

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor tool to create test status effect assets for combat system.
/// </summary>
public class StatusEffectCreator : EditorWindow
{
    [MenuItem("WalkAndRPG/Combat/Create Test Status Effects")]
    public static void CreateTestStatusEffects()
    {
        string folderPath = "Assets/ScriptableObjects/StatusEffects";

        // Create folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string parent = "Assets/ScriptableObjects";
            if (!AssetDatabase.IsValidFolder(parent))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            AssetDatabase.CreateFolder(parent, "StatusEffects");
        }

        // Create test status effects
        CreatePoison(folderPath);
        CreateBurn(folderPath);
        CreateStun(folderPath);
        CreateRegeneration(folderPath);
        CreateAttackUp(folderPath);
        CreateDefenseDown(folderPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Logger.LogInfo("StatusEffectCreator: Created 6 test status effect assets in " + folderPath, Logger.LogCategory.EditorLog);
        EditorUtility.DisplayDialog("Status Effects Created",
            "Created 6 test status effect assets:\n" +
            "- Poison\n" +
            "- Burn\n" +
            "- Stun\n" +
            "- Regeneration\n" +
            "- Attack Up\n" +
            "- Defense Down\n\n" +
            "Location: " + folderPath,
            "OK");
    }

    private static void CreatePoison(string folder)
    {
        var effect = ScriptableObject.CreateInstance<StatusEffectDefinition>();
        effect.EffectID = "poison";
        effect.EffectName = "Poison";
        effect.Description = "Deals damage over time. Stacks increase damage per tick.";
        effect.EffectType = StatusEffectType.Poison;
        effect.Behavior = EffectBehavior.DamageOverTime;
        effect.Stacking = StackingBehavior.Stacking;
        effect.MaxStacks = 10;
        effect.Decay = DecayBehavior.Time;
        effect.Duration = 10f;
        effect.TickInterval = 1f;
        effect.BaseValue = 1f;
        effect.ScalesWithStacks = true;
        effect.PreventsActions = false;
        effect.EffectColor = new Color(0.5f, 0f, 0.5f, 1f); // Purple

        SaveAsset(effect, folder, "Poison.asset");
    }

    private static void CreateBurn(string folder)
    {
        var effect = ScriptableObject.CreateInstance<StatusEffectDefinition>();
        effect.EffectID = "burn";
        effect.EffectName = "Burn";
        effect.Description = "Fire damage over time.";
        effect.EffectType = StatusEffectType.Burn;
        effect.Behavior = EffectBehavior.DamageOverTime;
        effect.Stacking = StackingBehavior.Stacking;
        effect.MaxStacks = 5;
        effect.Decay = DecayBehavior.Time;
        effect.Duration = 6f;
        effect.TickInterval = 2f;
        effect.BaseValue = 3f;
        effect.ScalesWithStacks = false;
        effect.PreventsActions = false;
        effect.EffectColor = new Color(1f, 0.4f, 0f, 1f); // Orange

        SaveAsset(effect, folder, "Burn.asset");
    }

    private static void CreateStun(string folder)
    {
        var effect = ScriptableObject.CreateInstance<StatusEffectDefinition>();
        effect.EffectID = "stun";
        effect.EffectName = "Stun";
        effect.Description = "Unable to use abilities. Cooldowns are paused.";
        effect.EffectType = StatusEffectType.Stun;
        effect.Behavior = EffectBehavior.ControlEffect;
        effect.Stacking = StackingBehavior.NoStacking;
        effect.MaxStacks = 1;
        effect.Decay = DecayBehavior.Time;
        effect.Duration = 2f;
        effect.TickInterval = 0f; // No ticking
        effect.BaseValue = 0f;
        effect.ScalesWithStacks = false;
        effect.PreventsActions = true;
        effect.EffectColor = new Color(1f, 1f, 0f, 1f); // Yellow

        SaveAsset(effect, folder, "Stun.asset");
    }

    private static void CreateRegeneration(string folder)
    {
        var effect = ScriptableObject.CreateInstance<StatusEffectDefinition>();
        effect.EffectID = "regeneration";
        effect.EffectName = "Regeneration";
        effect.Description = "Heals over time. Stacks for more healing per tick.";
        effect.EffectType = StatusEffectType.Regeneration;
        effect.Behavior = EffectBehavior.HealOverTime;
        effect.Stacking = StackingBehavior.Stacking;
        effect.MaxStacks = 3;
        effect.Decay = DecayBehavior.Time;
        effect.Duration = 8f;
        effect.TickInterval = 1f;
        effect.BaseValue = 2f;
        effect.ScalesWithStacks = true;
        effect.PreventsActions = false;
        effect.EffectColor = new Color(0f, 1f, 0f, 1f); // Green

        SaveAsset(effect, folder, "Regeneration.asset");
    }

    private static void CreateAttackUp(string folder)
    {
        var effect = ScriptableObject.CreateInstance<StatusEffectDefinition>();
        effect.EffectID = "attack_up";
        effect.EffectName = "Attack Up";
        effect.Description = "Increases damage dealt by 25%.";
        effect.EffectType = StatusEffectType.AttackBuff;
        effect.Behavior = EffectBehavior.StatModifier;
        effect.Stacking = StackingBehavior.NoStacking;
        effect.MaxStacks = 1;
        effect.Decay = DecayBehavior.Time;
        effect.Duration = 10f;
        effect.TickInterval = 0f; // No ticking
        effect.BaseValue = 0.25f; // +25%
        effect.ScalesWithStacks = false;
        effect.PreventsActions = false;
        effect.EffectColor = new Color(1f, 0f, 0f, 1f); // Red

        SaveAsset(effect, folder, "AttackUp.asset");
    }

    private static void CreateDefenseDown(string folder)
    {
        var effect = ScriptableObject.CreateInstance<StatusEffectDefinition>();
        effect.EffectID = "defense_down";
        effect.EffectName = "Defense Down";
        effect.Description = "Takes 10% more damage per stack.";
        effect.EffectType = StatusEffectType.DefenseDebuff;
        effect.Behavior = EffectBehavior.StatModifier;
        effect.Stacking = StackingBehavior.Stacking;
        effect.MaxStacks = 3;
        effect.Decay = DecayBehavior.Time;
        effect.Duration = 8f;
        effect.TickInterval = 0f; // No ticking
        effect.BaseValue = 0.10f; // +10% damage taken per stack (defense multiplier increases)
        effect.ScalesWithStacks = true;
        effect.PreventsActions = false;
        effect.EffectColor = new Color(0f, 0.5f, 1f, 1f); // Blue

        SaveAsset(effect, folder, "DefenseDown.asset");
    }

    private static void SaveAsset(StatusEffectDefinition effect, string folder, string fileName)
    {
        string path = Path.Combine(folder, fileName);

        // Check if asset already exists
        var existing = AssetDatabase.LoadAssetAtPath<StatusEffectDefinition>(path);
        if (existing != null)
        {
            Logger.LogInfo($"StatusEffectCreator: {fileName} already exists, skipping.", Logger.LogCategory.EditorLog);
            return;
        }

        AssetDatabase.CreateAsset(effect, path);
        Logger.LogInfo($"StatusEffectCreator: Created {fileName}", Logger.LogCategory.EditorLog);
    }

    [MenuItem("WalkAndRPG/Combat/Create Status Effect Registry")]
    public static void CreateStatusEffectRegistry()
    {
        string folderPath = "Assets/ScriptableObjects/Registries";

        // Create folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string parent = "Assets/ScriptableObjects";
            if (!AssetDatabase.IsValidFolder(parent))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }
            AssetDatabase.CreateFolder(parent, "Registries");
        }

        string path = Path.Combine(folderPath, "StatusEffectRegistry.asset");

        // Check if already exists
        var existing = AssetDatabase.LoadAssetAtPath<StatusEffectRegistry>(path);
        if (existing != null)
        {
            Logger.LogInfo("StatusEffectCreator: Registry already exists at " + path, Logger.LogCategory.EditorLog);
            Selection.activeObject = existing;
            return;
        }

        var registry = ScriptableObject.CreateInstance<StatusEffectRegistry>();
        AssetDatabase.CreateAsset(registry, path);
        AssetDatabase.SaveAssets();

        Logger.LogInfo("StatusEffectCreator: Created StatusEffectRegistry at " + path, Logger.LogCategory.EditorLog);
        Logger.LogInfo("Don't forget to add your StatusEffectDefinition assets to the registry!", Logger.LogCategory.EditorLog);

        Selection.activeObject = registry;
        EditorGUIUtility.PingObject(registry);
    }
}
#endif
