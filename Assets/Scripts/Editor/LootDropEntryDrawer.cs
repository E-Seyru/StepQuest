// Purpose: Custom property drawer for LootDropEntry to show visual drop chance and item preview
// Filepath: Assets/Scripts/Editor/LootDropEntryDrawer.cs

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(LootDropEntry))]
public class LootDropEntryDrawer : PropertyDrawer
{
    private const float IconSize = 32f;
    private const float Spacing = 2f;
    private const float HelpBoxHeight = 36f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight; // Foldout

        if (property.isExpanded)
        {
            // Item field
            height += EditorGUIUtility.singleLineHeight + Spacing;
            // Help box
            height += HelpBoxHeight + Spacing;
            // Quantity row (min + max on same line)
            height += EditorGUIUtility.singleLineHeight + Spacing;
            // Drop chance slider
            height += EditorGUIUtility.singleLineHeight + Spacing;
            // Drop chance visualization bar
            height += EditorGUIUtility.singleLineHeight + Spacing;
        }

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var itemProp = property.FindPropertyRelative("Item");
        var minQtyProp = property.FindPropertyRelative("MinQuantity");
        var maxQtyProp = property.FindPropertyRelative("MaxQuantity");
        var dropChanceProp = property.FindPropertyRelative("DropChance");

        ItemDefinition item = itemProp.objectReferenceValue as ItemDefinition;
        float dropChance = dropChanceProp.floatValue;
        int minQty = minQtyProp.intValue;
        int maxQty = maxQtyProp.intValue;

        // Create summary for foldout
        string summary = GetSummary(item, minQty, maxQty, dropChance);

        // Foldout with icon
        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        // Draw item icon inline with foldout if available
        if (item != null && item.ItemIcon != null)
        {
            Rect iconRect = new Rect(position.x + 15, position.y, IconSize * 0.6f, EditorGUIUtility.singleLineHeight);
            DrawSprite(iconRect, item.ItemIcon);

            Rect labelRect = new Rect(position.x + IconSize * 0.6f + 18, position.y, position.width - IconSize * 0.6f - 18, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, 15, EditorGUIUtility.singleLineHeight), property.isExpanded, "", true);
            EditorGUI.LabelField(labelRect, summary, GetSummaryStyle(dropChance));
        }
        else
        {
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, summary, true);
        }

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float y = position.y + EditorGUIUtility.singleLineHeight + Spacing;

            // Item field
            Rect itemRect = new Rect(position.x, y, position.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(itemRect, itemProp, new GUIContent("Item", "The item that can drop"));
            y += EditorGUIUtility.singleLineHeight + Spacing;

            // Help box
            Rect helpRect = new Rect(position.x + EditorGUI.indentLevel * 15, y, position.width - EditorGUI.indentLevel * 15, HelpBoxHeight);
            string helpText = GetHelpText(item, dropChance, minQty, maxQty);
            EditorGUI.HelpBox(helpRect, helpText, MessageType.Info);
            y += HelpBoxHeight + Spacing;

            // Quantity row - min and max on same line
            Rect qtyLabelRect = new Rect(position.x, y, 60, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(qtyLabelRect, "Quantity:");

            float fieldWidth = (position.width - 140) / 2;

            Rect minLabelRect = new Rect(position.x + 65, y, 30, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(minLabelRect, "Min:");
            Rect minRect = new Rect(position.x + 95, y, fieldWidth, EditorGUIUtility.singleLineHeight);
            minQtyProp.intValue = Mathf.Max(1, EditorGUI.IntField(minRect, minQtyProp.intValue));

            Rect maxLabelRect = new Rect(position.x + 100 + fieldWidth, y, 35, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(maxLabelRect, "Max:");
            Rect maxRect = new Rect(position.x + 135 + fieldWidth, y, fieldWidth, EditorGUIUtility.singleLineHeight);
            maxQtyProp.intValue = Mathf.Max(minQtyProp.intValue, EditorGUI.IntField(maxRect, maxQtyProp.intValue));
            y += EditorGUIUtility.singleLineHeight + Spacing;

            // Drop chance slider with percentage display
            Rect chanceLabelRect = new Rect(position.x, y, 80, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(chanceLabelRect, "Drop Chance:");

            Rect sliderRect = new Rect(position.x + 85, y, position.width - 145, EditorGUIUtility.singleLineHeight);
            dropChanceProp.floatValue = EditorGUI.Slider(sliderRect, dropChanceProp.floatValue, 0f, 1f);

            Rect percentRect = new Rect(position.x + position.width - 55, y, 55, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(percentRect, $"{dropChanceProp.floatValue * 100:F0}%", EditorStyles.boldLabel);
            y += EditorGUIUtility.singleLineHeight + Spacing;

            // Visual drop chance bar
            Rect barBackgroundRect = new Rect(position.x + EditorGUI.indentLevel * 15, y, position.width - EditorGUI.indentLevel * 15, EditorGUIUtility.singleLineHeight);
            EditorGUI.DrawRect(barBackgroundRect, new Color(0.2f, 0.2f, 0.2f));

            float barWidth = (position.width - EditorGUI.indentLevel * 15) * dropChanceProp.floatValue;
            Rect barFillRect = new Rect(position.x + EditorGUI.indentLevel * 15, y, barWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.DrawRect(barFillRect, GetChanceColor(dropChanceProp.floatValue));

            // Chance text overlay
            GUIStyle centerStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            centerStyle.normal.textColor = Color.white;
            EditorGUI.LabelField(barBackgroundRect, GetChanceLabel(dropChanceProp.floatValue), centerStyle);

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private string GetSummary(ItemDefinition item, int minQty, int maxQty, float dropChance)
    {
        string itemName = item != null ? item.GetDisplayName() : "(No Item)";
        string qty = minQty == maxQty ? $"x{minQty}" : $"x{minQty}-{maxQty}";
        string chance = $"{dropChance * 100:F0}%";

        return $"{itemName} {qty} @ {chance}";
    }

    private GUIStyle GetSummaryStyle(float dropChance)
    {
        GUIStyle style = new GUIStyle(EditorStyles.label);

        if (dropChance >= 0.75f)
            style.normal.textColor = new Color(0.3f, 0.8f, 0.3f); // Green - common
        else if (dropChance >= 0.25f)
            style.normal.textColor = new Color(0.8f, 0.8f, 0.3f); // Yellow - uncommon
        else if (dropChance >= 0.05f)
            style.normal.textColor = new Color(0.8f, 0.5f, 0.2f); // Orange - rare
        else
            style.normal.textColor = new Color(0.8f, 0.3f, 0.8f); // Purple - very rare

        return style;
    }

    private string GetHelpText(ItemDefinition item, float dropChance, int minQty, int maxQty)
    {
        if (item == null)
            return "Select an item to configure this loot drop.";

        string avgQty = minQty == maxQty ? $"{minQty}" : $"{(minQty + maxQty) / 2f:F1}";
        float expectedDrops = dropChance * ((minQty + maxQty) / 2f);

        return $"Expected per kill: {expectedDrops:F2} {item.GetDisplayName()}\n" +
               $"Rarity: {GetRarityText(dropChance)}";
    }

    private string GetRarityText(float dropChance)
    {
        if (dropChance >= 0.75f) return "Common (75%+)";
        if (dropChance >= 0.5f) return "Uncommon (50-74%)";
        if (dropChance >= 0.25f) return "Rare (25-49%)";
        if (dropChance >= 0.1f) return "Very Rare (10-24%)";
        if (dropChance >= 0.01f) return "Epic (1-9%)";
        return "Legendary (<1%)";
    }

    private Color GetChanceColor(float dropChance)
    {
        if (dropChance >= 0.75f) return new Color(0.2f, 0.7f, 0.2f); // Green
        if (dropChance >= 0.5f) return new Color(0.7f, 0.7f, 0.2f); // Yellow
        if (dropChance >= 0.25f) return new Color(0.8f, 0.5f, 0.2f); // Orange
        if (dropChance >= 0.1f) return new Color(0.7f, 0.3f, 0.7f); // Purple
        return new Color(0.9f, 0.3f, 0.3f); // Red for very rare
    }

    private string GetChanceLabel(float dropChance)
    {
        if (dropChance >= 0.75f) return "COMMON";
        if (dropChance >= 0.5f) return "UNCOMMON";
        if (dropChance >= 0.25f) return "RARE";
        if (dropChance >= 0.1f) return "VERY RARE";
        if (dropChance >= 0.01f) return "EPIC";
        return "LEGENDARY";
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
}
#endif
