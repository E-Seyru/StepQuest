// Purpose: Custom editor for StatusEffectDefinition with live preview and validation
// Filepath: Assets/Scripts/Editor/StatusEffectDefinitionEditor.cs

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StatusEffectDefinition))]
public class StatusEffectDefinitionEditor : Editor
{
    private bool showPreview = true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var effect = (StatusEffectDefinition)target;

        // Draw default inspector
        DrawDefaultInspector();

        EditorGUILayout.Space();

        // Effect Preview Section
        showPreview = EditorGUILayout.Foldout(showPreview, "Effect Preview", true, EditorStyles.foldoutHeader);

        if (showPreview)
        {
            DrawEffectPreview(effect);
        }

        // Validation warnings
        DrawValidationWarnings(effect);

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawEffectPreview(StatusEffectDefinition effect)
    {
        EditorGUILayout.BeginVertical("box");

        // Header with icon and name
        EditorGUILayout.BeginHorizontal();
        if (effect.EffectIcon != null)
        {
            Rect iconRect = EditorGUILayout.GetControlRect(GUILayout.Width(32), GUILayout.Height(32));
            DrawSprite(iconRect, effect.EffectIcon, effect.EffectColor);
        }

        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(effect.GetDisplayName(), EditorStyles.boldLabel);
        EditorGUILayout.LabelField(GetEffectTypeLabel(effect), EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Effect summary
        EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(effect.GetEffectSummary(), MessageType.None);

        EditorGUILayout.Space();

        // Detailed breakdown
        EditorGUILayout.LabelField("Behavior Breakdown", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        // What it does
        string behaviorDesc = GetBehaviorDescription(effect);
        EditorGUILayout.LabelField("What it does:", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField(behaviorDesc, EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space();

        // Stacking info
        EditorGUILayout.LabelField("Stacking:", EditorStyles.miniBoldLabel);
        string stackingDesc = GetStackingDescription(effect);
        EditorGUILayout.LabelField(stackingDesc, EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space();

        // Duration/Decay info
        EditorGUILayout.LabelField("Duration:", EditorStyles.miniBoldLabel);
        string durationDesc = GetDurationDescription(effect);
        EditorGUILayout.LabelField(durationDesc, EditorStyles.wordWrappedLabel);

        EditorGUILayout.EndVertical();

        // Example calculations
        if (effect.Behavior == EffectBehavior.DamageOverTime || effect.Behavior == EffectBehavior.HealOverTime)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Example Calculations", EditorStyles.boldLabel);
            DrawExampleCalculations(effect);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawExampleCalculations(StatusEffectDefinition effect)
    {
        EditorGUILayout.BeginVertical("box");

        string valueType = effect.Behavior == EffectBehavior.DamageOverTime ? "damage" : "healing";

        // Show calculations for 1, 3, 5 stacks
        int[] exampleStacks = { 1, 3, 5, 10 };

        foreach (int stacks in exampleStacks)
        {
            if (effect.MaxStacks > 0 && stacks > effect.MaxStacks) continue;

            float tickValue = effect.CalculateTickValue(stacks);
            float totalOverDuration = 0;

            if (effect.Duration > 0 && effect.TickInterval > 0)
            {
                int ticks = Mathf.FloorToInt(effect.Duration / effect.TickInterval);
                totalOverDuration = tickValue * ticks;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{stacks} stack(s):", GUILayout.Width(80));
            EditorGUILayout.LabelField($"{tickValue:F1} {valueType}/tick", GUILayout.Width(120));

            if (totalOverDuration > 0)
            {
                EditorGUILayout.LabelField($"({totalOverDuration:F0} total over {effect.Duration}s)", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawValidationWarnings(StatusEffectDefinition effect)
    {
        // Check for common configuration issues
        if (string.IsNullOrEmpty(effect.EffectID))
        {
            EditorGUILayout.HelpBox("Effect ID is empty. It will be auto-generated from the name.", MessageType.Info);
        }

        if (effect.EffectIcon == null)
        {
            EditorGUILayout.HelpBox("No icon assigned. Effect will not display properly in combat UI.", MessageType.Warning);
        }

        // DoT/HoT without tick interval
        if ((effect.Behavior == EffectBehavior.DamageOverTime || effect.Behavior == EffectBehavior.HealOverTime)
            && effect.TickInterval <= 0)
        {
            EditorGUILayout.HelpBox("DoT/HoT effect requires TickInterval > 0 to deal damage/heal.", MessageType.Error);
        }

        // Time decay without duration
        if (effect.Decay == DecayBehavior.Time && effect.Duration <= 0)
        {
            EditorGUILayout.HelpBox("Time-based decay requires Duration > 0.", MessageType.Error);
        }

        // Stun without PreventsActions
        if (effect.EffectType == StatusEffectType.Stun && !effect.PreventsActions)
        {
            EditorGUILayout.HelpBox("Stun effects should have PreventsActions = true.", MessageType.Warning);
        }

        // StatModifier with tick interval
        if (effect.Behavior == EffectBehavior.StatModifier && effect.TickInterval > 0)
        {
            EditorGUILayout.HelpBox("Stat modifiers typically don't need a TickInterval (set to 0).", MessageType.Info);
        }

        // Very high damage values
        if (effect.Behavior == EffectBehavior.DamageOverTime && effect.BaseValue > 50)
        {
            EditorGUILayout.HelpBox($"High base damage ({effect.BaseValue}). With stacks, this could be very powerful.", MessageType.Info);
        }
    }

    private string GetEffectTypeLabel(StatusEffectDefinition effect)
    {
        switch (effect.Behavior)
        {
            case EffectBehavior.DamageOverTime:
                return $"Damage Over Time ({effect.EffectType})";
            case EffectBehavior.HealOverTime:
                return "Heal Over Time";
            case EffectBehavior.StatModifier:
                return effect.IsBuff ? "Buff" : "Debuff";
            case EffectBehavior.ControlEffect:
                return "Control Effect";
            default:
                return effect.EffectType.ToString();
        }
    }

    private string GetBehaviorDescription(StatusEffectDefinition effect)
    {
        switch (effect.Behavior)
        {
            case EffectBehavior.DamageOverTime:
                return $"Deals {effect.BaseValue} {effect.EffectType} damage every {effect.TickInterval}s" +
                       (effect.ScalesWithStacks ? " (scales with stacks)" : " (flat)");

            case EffectBehavior.HealOverTime:
                return $"Heals {effect.BaseValue} health every {effect.TickInterval}s" +
                       (effect.ScalesWithStacks ? " (scales with stacks)" : " (flat)");

            case EffectBehavior.StatModifier:
                string percent = (effect.BaseValue * 100).ToString("F0");
                string sign = effect.BaseValue >= 0 ? "+" : "";
                return $"Modifies stat by {sign}{percent}%";

            case EffectBehavior.ControlEffect:
                if (effect.PreventsActions)
                    return "Prevents target from using abilities. Cooldowns are paused.";
                return "Special control effect (configure PreventsActions for stun)";

            default:
                return "Unknown behavior";
        }
    }

    private string GetStackingDescription(StatusEffectDefinition effect)
    {
        string desc = "";

        if (effect.Stacking == StackingBehavior.Stacking)
        {
            desc = "Can stack. ";
            if (effect.MaxStacks == 0)
                desc += "No stack limit.";
            else
                desc += $"Max {effect.MaxStacks} stacks.";

            if (effect.ScalesWithStacks)
                desc += " Effect strength scales with stacks.";
        }
        else
        {
            desc = "Cannot stack. Reapplying refreshes the effect.";
        }

        return desc;
    }

    private string GetDurationDescription(StatusEffectDefinition effect)
    {
        switch (effect.Decay)
        {
            case DecayBehavior.None:
                return "Permanent until cleansed or combat ends.";

            case DecayBehavior.Time:
                return $"Each stack batch expires after {effect.Duration} seconds.";

            case DecayBehavior.OnTick:
                return $"Loses 1 stack every {effect.TickInterval}s tick.";

            case DecayBehavior.OnHit:
                return "Loses 1 stack each time target takes damage.";

            default:
                return "Unknown decay behavior";
        }
    }

    private void DrawSprite(Rect rect, Sprite sprite, Color tint)
    {
        if (sprite == null || sprite.texture == null) return;

        var oldColor = GUI.color;
        GUI.color = tint;

        Texture2D tex = sprite.texture;
        Rect spriteRect = sprite.textureRect;

        Rect texCoords = new Rect(
            spriteRect.x / tex.width,
            spriteRect.y / tex.height,
            spriteRect.width / tex.width,
            spriteRect.height / tex.height
        );

        GUI.DrawTextureWithTexCoords(rect, tex, texCoords);
        GUI.color = oldColor;
    }
}
#endif
