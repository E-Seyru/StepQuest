// Purpose: Custom property drawer for AbilityEffect to show contextual help
// Filepath: Assets/Scripts/Editor/AbilityEffectDrawer.cs

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AbilityEffect))]
public class AbilityEffectDrawer : PropertyDrawer
{
    private const float HelpBoxHeight = 42f;
    private const float Spacing = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight; // Foldout

        if (property.isExpanded)
        {
            var typeProp = property.FindPropertyRelative("Type");
            var effectType = (AbilityEffectType)typeProp.enumValueIndex;

            // Type field
            height += EditorGUIUtility.singleLineHeight + Spacing;

            // Help box
            height += HelpBoxHeight + Spacing;

            // Value field (only for Damage, Heal, Shield)
            if (effectType != AbilityEffectType.StatusEffect)
            {
                height += EditorGUIUtility.singleLineHeight + Spacing;
            }

            // StatusEffect fields
            if (effectType == AbilityEffectType.StatusEffect)
            {
                height += EditorGUIUtility.singleLineHeight + Spacing; // StatusEffect reference
                height += EditorGUIUtility.singleLineHeight + Spacing; // Stacks
            }

            // TargetsSelf field
            height += EditorGUIUtility.singleLineHeight + Spacing;
        }

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var typeProp = property.FindPropertyRelative("Type");
        var valueProp = property.FindPropertyRelative("Value");
        var statusEffectProp = property.FindPropertyRelative("StatusEffect");
        var stacksProp = property.FindPropertyRelative("StatusEffectStacks");
        var targetsSelfProp = property.FindPropertyRelative("TargetsSelf");

        var effectType = (AbilityEffectType)typeProp.enumValueIndex;

        // Foldout with summary
        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        string summary = GetEffectSummary(effectType, valueProp.floatValue, statusEffectProp.objectReferenceValue as StatusEffectDefinition, stacksProp.intValue, targetsSelfProp.boolValue);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, $"{label.text}: {summary}", true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float y = position.y + EditorGUIUtility.singleLineHeight + Spacing;

            // Type field
            Rect typeRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(typeRect, typeProp);
            y += EditorGUIUtility.singleLineHeight + Spacing;

            // Help box based on type
            Rect helpRect = new Rect(position.x + EditorGUI.indentLevel * 15, y, position.width - EditorGUI.indentLevel * 15, HelpBoxHeight);
            EditorGUI.HelpBox(helpRect, GetHelpText(effectType), MessageType.Info);
            y += HelpBoxHeight + Spacing;

            // Type-specific fields
            switch (effectType)
            {
                case AbilityEffectType.Damage:
                    Rect damageRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(damageRect, valueProp, new GUIContent("Damage Amount", "Instant damage dealt to the target"));
                    y += EditorGUIUtility.singleLineHeight + Spacing;
                    break;

                case AbilityEffectType.Heal:
                    Rect healRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(healRect, valueProp, new GUIContent("Heal Amount", "Instant healing applied"));
                    y += EditorGUIUtility.singleLineHeight + Spacing;
                    break;

                case AbilityEffectType.Shield:
                    Rect shieldRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(shieldRect, valueProp, new GUIContent("Shield Amount", "Shield points that absorb damage"));
                    y += EditorGUIUtility.singleLineHeight + Spacing;
                    break;

                case AbilityEffectType.StatusEffect:
                    Rect effectRefRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(effectRefRect, statusEffectProp, new GUIContent("Status Effect", "The status effect to apply (Poison, Burn, Stun, etc.)"));
                    y += EditorGUIUtility.singleLineHeight + Spacing;

                    Rect stacksRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(stacksRect, stacksProp, new GUIContent("Stacks to Apply", "Number of stacks - more stacks = stronger effect"));
                    y += EditorGUIUtility.singleLineHeight + Spacing;
                    break;
            }

            // TargetsSelf field with contextual label
            Rect targetRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            string targetLabel = GetTargetLabel(effectType);
            string targetTooltip = GetTargetTooltip(effectType);
            EditorGUI.PropertyField(targetRect, targetsSelfProp, new GUIContent(targetLabel, targetTooltip));

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private string GetEffectSummary(AbilityEffectType type, float value, StatusEffectDefinition statusEffect, int stacks, bool targetsSelf)
    {
        string target = targetsSelf ? " (self)" : "";

        switch (type)
        {
            case AbilityEffectType.Damage:
                return $"{value} damage{target}";
            case AbilityEffectType.Heal:
                return $"{value} heal{target}";
            case AbilityEffectType.Shield:
                return $"{value} shield{target}";
            case AbilityEffectType.StatusEffect:
                if (statusEffect != null)
                    return $"{statusEffect.GetDisplayName()} x{stacks}{target}";
                return $"(No effect selected){target}";
            default:
                return "Unknown";
        }
    }

    private string GetHelpText(AbilityEffectType type)
    {
        switch (type)
        {
            case AbilityEffectType.Damage:
                return "DAMAGE: Deals instant damage to the target.\nSet 'Damage Amount' to the HP to remove.\nUsually targets enemy (TargetsSelf = false).";

            case AbilityEffectType.Heal:
                return "HEAL: Restores health instantly.\nSet 'Heal Amount' to the HP to restore.\nUsually targets self (TargetsSelf = true).";

            case AbilityEffectType.Shield:
                return "SHIELD: Adds a damage-absorbing barrier.\nShield absorbs damage before health.\nUsually targets self (TargetsSelf = true).";

            case AbilityEffectType.StatusEffect:
                return "STATUS EFFECT: Applies an effect over time.\nSelect a StatusEffect (Poison, Burn, Stun, etc.)\nStacks multiply the effect's power.";

            default:
                return "Select an effect type.";
        }
    }

    private string GetTargetLabel(AbilityEffectType type)
    {
        switch (type)
        {
            case AbilityEffectType.Damage:
                return "Damage Self? (unusual)";
            case AbilityEffectType.Heal:
                return "Heal Self";
            case AbilityEffectType.Shield:
                return "Shield Self";
            case AbilityEffectType.StatusEffect:
                return "Apply to Self";
            default:
                return "Targets Self";
        }
    }

    private string GetTargetTooltip(AbilityEffectType type)
    {
        switch (type)
        {
            case AbilityEffectType.Damage:
                return "FALSE = damage enemy (normal)\nTRUE = damage yourself (rare, for special abilities)";
            case AbilityEffectType.Heal:
                return "TRUE = heal yourself (normal)\nFALSE = heal enemy (very rare)";
            case AbilityEffectType.Shield:
                return "TRUE = shield yourself (normal)\nFALSE = shield enemy (very rare)";
            case AbilityEffectType.StatusEffect:
                return "FALSE = apply debuff/DoT to enemy\nTRUE = apply buff/HoT to yourself";
            default:
                return "Who receives this effect";
        }
    }
}
#endif
