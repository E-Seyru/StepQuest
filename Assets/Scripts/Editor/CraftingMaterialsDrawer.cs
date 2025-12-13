// Purpose: Custom editor for ActivityVariant to keep RequiredMaterials and RequiredQuantities in sync
// Filepath: Assets/Scripts/Editor/CraftingMaterialsDrawer.cs

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(ActivityVariant))]
public class ActivityVariantInspector : Editor
{
    private SerializedProperty isTimeBasedProp;
    private SerializedProperty requiredMaterialsProp;
    private SerializedProperty requiredQuantitiesProp;

    private bool showMaterialsSection = true;

    void OnEnable()
    {
        isTimeBasedProp = serializedObject.FindProperty("IsTimeBased");
        requiredMaterialsProp = serializedObject.FindProperty("RequiredMaterials");
        requiredQuantitiesProp = serializedObject.FindProperty("RequiredQuantities");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw default inspector up to RequiredMaterials
        DrawPropertiesExcluding(serializedObject, "RequiredMaterials", "RequiredQuantities");

        // Only show crafting materials section for time-based activities
        if (isTimeBasedProp.boolValue)
        {
            EditorGUILayout.Space();
            DrawCraftingMaterialsSection();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCraftingMaterialsSection()
    {
        EditorGUILayout.BeginVertical("box");

        // Section header with foldout
        EditorGUILayout.BeginHorizontal();
        showMaterialsSection = EditorGUILayout.Foldout(showMaterialsSection, "Crafting Materials", true, EditorStyles.foldoutHeader);

        // Sync status indicator
        bool isSynced = requiredMaterialsProp.arraySize == requiredQuantitiesProp.arraySize;
        var oldColor = GUI.color;
        GUI.color = isSynced ? Color.green : Color.red;
        EditorGUILayout.LabelField(isSynced ? "[Synced]" : "[OUT OF SYNC!]", EditorStyles.miniLabel, GUILayout.Width(90));
        GUI.color = oldColor;

        EditorGUILayout.EndHorizontal();

        if (showMaterialsSection)
        {
            // Help box
            EditorGUILayout.HelpBox(
                "Add materials required for crafting.\n" +
                "Each row: Item + Quantity needed.",
                MessageType.Info);

            // Ensure arrays are synced
            SyncArraySizes();

            // Draw each material entry
            for (int i = 0; i < requiredMaterialsProp.arraySize; i++)
            {
                DrawMaterialEntry(i);
            }

            EditorGUILayout.Space();

            // Add/Remove buttons
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("+ Add Material", GUILayout.Width(120)))
            {
                AddMaterial();
            }

            GUI.enabled = requiredMaterialsProp.arraySize > 0;
            if (GUILayout.Button("- Remove Last", GUILayout.Width(100)))
            {
                RemoveLastMaterial();
            }
            GUI.enabled = true;

            if (GUILayout.Button("Clear All", GUILayout.Width(80)))
            {
                if (EditorUtility.DisplayDialog("Clear Materials", "Remove all crafting materials?", "Yes", "No"))
                {
                    ClearAllMaterials();
                }
            }

            EditorGUILayout.EndHorizontal();

            // Summary
            if (requiredMaterialsProp.arraySize > 0)
            {
                EditorGUILayout.Space();
                DrawMaterialsSummary();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawMaterialEntry(int index)
    {
        EditorGUILayout.BeginHorizontal("box");

        // Index label
        EditorGUILayout.LabelField($"#{index + 1}", GUILayout.Width(25));

        // Material field
        var materialProp = requiredMaterialsProp.GetArrayElementAtIndex(index);
        EditorGUILayout.PropertyField(materialProp, GUIContent.none, GUILayout.MinWidth(150));

        // Quantity field
        EditorGUILayout.LabelField("x", GUILayout.Width(12));
        var quantityProp = requiredQuantitiesProp.GetArrayElementAtIndex(index);
        quantityProp.intValue = Mathf.Max(1, EditorGUILayout.IntField(quantityProp.intValue, GUILayout.Width(50)));

        // Item preview
        var item = materialProp.objectReferenceValue as ItemDefinition;
        if (item != null)
        {
            if (item.ItemIcon != null)
            {
                Rect iconRect = EditorGUILayout.GetControlRect(GUILayout.Width(20), GUILayout.Height(20));
                DrawSprite(iconRect, item.ItemIcon);
            }
        }
        else
        {
            var oldCol = GUI.color;
            GUI.color = Color.yellow;
            EditorGUILayout.LabelField("?", GUILayout.Width(20));
            GUI.color = oldCol;
        }

        // Remove button
        if (GUILayout.Button("X", GUILayout.Width(22)))
        {
            RemoveMaterialAt(index);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawMaterialsSummary()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);

        var summary = new List<string>();
        int totalItems = 0;

        for (int i = 0; i < requiredMaterialsProp.arraySize; i++)
        {
            var materialProp = requiredMaterialsProp.GetArrayElementAtIndex(i);
            var quantityProp = requiredQuantitiesProp.GetArrayElementAtIndex(i);

            var item = materialProp.objectReferenceValue as ItemDefinition;
            if (item != null)
            {
                summary.Add($"{quantityProp.intValue}x {item.GetDisplayName()}");
                totalItems += quantityProp.intValue;
            }
            else
            {
                summary.Add($"{quantityProp.intValue}x (missing item)");
            }
        }

        EditorGUILayout.LabelField($"Materials: {string.Join(", ", summary)}");
        EditorGUILayout.LabelField($"Total items needed: {totalItems}");

        EditorGUILayout.EndVertical();
    }

    private void SyncArraySizes()
    {
        // Ensure both arrays have the same size
        while (requiredQuantitiesProp.arraySize < requiredMaterialsProp.arraySize)
        {
            requiredQuantitiesProp.InsertArrayElementAtIndex(requiredQuantitiesProp.arraySize);
            requiredQuantitiesProp.GetArrayElementAtIndex(requiredQuantitiesProp.arraySize - 1).intValue = 1;
        }

        while (requiredQuantitiesProp.arraySize > requiredMaterialsProp.arraySize)
        {
            requiredQuantitiesProp.DeleteArrayElementAtIndex(requiredQuantitiesProp.arraySize - 1);
        }
    }

    private void AddMaterial()
    {
        requiredMaterialsProp.InsertArrayElementAtIndex(requiredMaterialsProp.arraySize);
        requiredMaterialsProp.GetArrayElementAtIndex(requiredMaterialsProp.arraySize - 1).objectReferenceValue = null;

        requiredQuantitiesProp.InsertArrayElementAtIndex(requiredQuantitiesProp.arraySize);
        requiredQuantitiesProp.GetArrayElementAtIndex(requiredQuantitiesProp.arraySize - 1).intValue = 1;
    }

    private void RemoveLastMaterial()
    {
        if (requiredMaterialsProp.arraySize > 0)
        {
            requiredMaterialsProp.DeleteArrayElementAtIndex(requiredMaterialsProp.arraySize - 1);
        }
        if (requiredQuantitiesProp.arraySize > 0)
        {
            requiredQuantitiesProp.DeleteArrayElementAtIndex(requiredQuantitiesProp.arraySize - 1);
        }
    }

    private void RemoveMaterialAt(int index)
    {
        // For object references, we need to set to null first then delete
        if (requiredMaterialsProp.GetArrayElementAtIndex(index).objectReferenceValue != null)
        {
            requiredMaterialsProp.GetArrayElementAtIndex(index).objectReferenceValue = null;
        }
        requiredMaterialsProp.DeleteArrayElementAtIndex(index);
        requiredQuantitiesProp.DeleteArrayElementAtIndex(index);
    }

    private void ClearAllMaterials()
    {
        requiredMaterialsProp.ClearArray();
        requiredQuantitiesProp.ClearArray();
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
