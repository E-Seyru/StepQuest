// Purpose: Automatically wire up the imported CombatPanel from the old project
// Filepath: Assets/Scripts/Editor/CombatUIWirer.cs

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatUIWirer : EditorWindow
{
    private GameObject combatPanel;

    [MenuItem("WalkAndRPG/Combat UI Wirer (Import Helper)")]
    public static void ShowWindow()
    {
        GetWindow<CombatUIWirer>("Combat UI Wirer");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Combat UI Wirer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tool automatically wires up the CombatPanel imported from 'Systeme de combat parfait'.\n\n1. Select your imported CombatPanel\n2. Click 'Wire Everything'", MessageType.Info);

        EditorGUILayout.Space(10);

        combatPanel = (GameObject)EditorGUILayout.ObjectField("Combat Panel", combatPanel, typeof(GameObject), true);

        if (combatPanel == null)
        {
            // Try to find it automatically
            if (GUILayout.Button("Find CombatPanel in Scene"))
            {
                combatPanel = GameObject.Find("CombatPanel");
                if (combatPanel != null)
                {
                    EditorGUIUtility.PingObject(combatPanel);
                }
                else
                {
                    EditorUtility.DisplayDialog("Not Found", "CombatPanel not found in scene. Please assign it manually.", "OK");
                }
            }
        }

        EditorGUILayout.Space(20);

        GUI.enabled = combatPanel != null;

        if (GUILayout.Button("1. Create Ability Prefab", GUILayout.Height(30)))
        {
            CreateAbilityPrefab();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("2. Add Scripts to GameObjects", GUILayout.Height(30)))
        {
            AddScripts();
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("3. Wire All References", GUILayout.Height(30)))
        {
            WireReferences();
        }

        EditorGUILayout.Space(20);

        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("DO EVERYTHING", GUILayout.Height(50)))
        {
            CreateAbilityPrefab();
            AddScripts();
            WireReferences();
            EditorUtility.DisplayDialog("Done!", "Combat UI has been wired up!\n\nYou can now test with the Combat Tester.", "OK");
        }
        GUI.backgroundColor = Color.white;

        GUI.enabled = true;
    }

    private void CreateAbilityPrefab()
    {
        string prefabPath = "Assets/Prefabs/UI/Combat";
        string fullPath = prefabPath + "/CombatAbilityUI.prefab";

        // Check if already exists
        if (AssetDatabase.LoadAssetAtPath<GameObject>(fullPath) != null)
        {
            Debug.Log("CombatAbilityUI prefab already exists at " + fullPath);
            return;
        }

        // Create folders if needed
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        if (!AssetDatabase.IsValidFolder(prefabPath))
            AssetDatabase.CreateFolder("Assets/Prefabs/UI", "Combat");

        // Create prefab structure - root IS the ability image
        GameObject prefabRoot = new GameObject("CombatAbilityUI");
        RectTransform rootRect = prefabRoot.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(80, 80);

        // Root has the ability image
        Image abilityImg = prefabRoot.AddComponent<Image>();
        abilityImg.color = Color.white;

        // Cooldown overlay as child
        GameObject cooldownObj = new GameObject("CooldownOverlay", typeof(RectTransform));
        cooldownObj.transform.SetParent(prefabRoot.transform, false);
        RectTransform cooldownRect = cooldownObj.GetComponent<RectTransform>();
        cooldownRect.anchorMin = Vector2.zero;
        cooldownRect.anchorMax = Vector2.one;
        cooldownRect.offsetMin = Vector2.zero;
        cooldownRect.offsetMax = Vector2.zero;

        Image cooldownImg = cooldownObj.AddComponent<Image>();
        cooldownImg.color = new Color(0f, 0f, 0f, 0.6f);
        cooldownObj.SetActive(false);

        // Add script
        CombatAbilityUI abilityUI = prefabRoot.AddComponent<CombatAbilityUI>();

        // Wire references
        SerializedObject so = new SerializedObject(abilityUI);
        so.FindProperty("abilityImage").objectReferenceValue = abilityImg;
        so.FindProperty("cooldownOverlay").objectReferenceValue = cooldownImg;
        so.ApplyModifiedProperties();

        // Save prefab
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, fullPath);
        DestroyImmediate(prefabRoot);

        Debug.Log("Created CombatAbilityUI prefab at " + fullPath);
    }

    private void AddScripts()
    {
        Undo.SetCurrentGroupName("Add Combat Scripts");

        // Add CombatPanelUI to root
        if (combatPanel.GetComponent<CombatPanelUI>() == null)
        {
            Undo.AddComponent<CombatPanelUI>(combatPanel);
            Debug.Log("Added CombatPanelUI to " + combatPanel.name);
        }

        // Find and add CombatAbilityDisplay to player abilities panel
        Transform playerPanel = FindChildRecursive(combatPanel.transform, "PlayerPanel");
        if (playerPanel != null)
        {
            Transform playerAbilities = FindChildRecursive(playerPanel, "EquippedAbilitiesPanel");
            if (playerAbilities != null && playerAbilities.GetComponent<CombatAbilityDisplay>() == null)
            {
                Undo.AddComponent<CombatAbilityDisplay>(playerAbilities.gameObject);
                Debug.Log("Added CombatAbilityDisplay to PlayerPanel > EquippedAbilitiesPanel");
            }
        }

        // Find and add CombatAbilityDisplay to enemy abilities panel
        Transform aiPanel = FindChildRecursive(combatPanel.transform, "AIPanel");
        if (aiPanel != null)
        {
            Transform enemyAbilities = FindChildRecursive(aiPanel, "EquippedAbilitiesPanel");
            if (enemyAbilities != null && enemyAbilities.GetComponent<CombatAbilityDisplay>() == null)
            {
                Undo.AddComponent<CombatAbilityDisplay>(enemyAbilities.gameObject);
                Debug.Log("Added CombatAbilityDisplay to AIPanel > EquippedAbilitiesPanel");
            }
        }

        Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
    }

    private void WireReferences()
    {
        CombatPanelUI panelUI = combatPanel.GetComponent<CombatPanelUI>();
        if (panelUI == null)
        {
            EditorUtility.DisplayDialog("Error", "CombatPanelUI script not found on CombatPanel. Run 'Add Scripts' first.", "OK");
            return;
        }

        SerializedObject so = new SerializedObject(panelUI);

        // Combat Panel reference
        so.FindProperty("combatPanel").objectReferenceValue = combatPanel;

        // Player references
        Transform playerPanel = FindChildRecursive(combatPanel.transform, "PlayerPanel");
        if (playerPanel != null)
        {
            // Player Image
            Transform playerImage = FindChildRecursive(playerPanel, "PlayerImage");
            if (playerImage != null)
            {
                so.FindProperty("playerImage").objectReferenceValue = playerImage.GetComponent<Image>();
                so.FindProperty("playerImageTransform").objectReferenceValue = playerImage.GetComponent<RectTransform>();
            }

            // Health Bar
            Transform healthBarPanel = FindChildRecursive(playerPanel, "HealthBarPanel");
            if (healthBarPanel != null)
            {
                // Try to find fill images
                Transform healthFill = FindChildByNameContains(healthBarPanel, "Fill") ?? FindChildByNameContains(healthBarPanel, "Health");
                Transform shieldFill = FindChildByNameContains(healthBarPanel, "Shield");

                // If no specific fill, look for Image children
                if (healthFill == null)
                {
                    foreach (Transform child in healthBarPanel)
                    {
                        Image img = child.GetComponent<Image>();
                        if (img != null && img.type == Image.Type.Filled)
                        {
                            if (healthFill == null)
                                healthFill = child;
                            else if (shieldFill == null)
                                shieldFill = child;
                        }
                    }
                }

                if (healthFill != null)
                    so.FindProperty("playerHealthBarFill").objectReferenceValue = healthFill.GetComponent<Image>();
                if (shieldFill != null)
                    so.FindProperty("playerShieldBarFill").objectReferenceValue = shieldFill.GetComponent<Image>();

                // Health Text
                TextMeshProUGUI healthText = healthBarPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (healthText != null)
                    so.FindProperty("playerHealthText").objectReferenceValue = healthText;
            }

            // Shield Text (might be separate)
            Transform shieldText = FindChildByNameContains(playerPanel, "ShieldText");
            if (shieldText != null)
                so.FindProperty("playerShieldText").objectReferenceValue = shieldText.GetComponent<TextMeshProUGUI>();

            // Player Ability Display
            Transform playerAbilities = FindChildRecursive(playerPanel, "EquippedAbilitiesPanel");
            if (playerAbilities != null)
            {
                CombatAbilityDisplay display = playerAbilities.GetComponent<CombatAbilityDisplay>();
                so.FindProperty("playerAbilityDisplay").objectReferenceValue = display;

                // Assign prefab to display
                if (display != null)
                {
                    AssignAbilityPrefab(display);
                }
            }
        }

        // Enemy references
        Transform aiPanel = FindChildRecursive(combatPanel.transform, "AIPanel");
        if (aiPanel != null)
        {
            // Enemy Image
            Transform enemyImage = FindChildRecursive(aiPanel, "AIImage") ?? FindChildRecursive(aiPanel, "EnemyImage");
            if (enemyImage != null)
            {
                so.FindProperty("enemyImage").objectReferenceValue = enemyImage.GetComponent<Image>();
                so.FindProperty("enemyImageTransform").objectReferenceValue = enemyImage.GetComponent<RectTransform>();
            }

            // Enemy Name
            Transform nameText = FindChildByNameContains(aiPanel, "Name");
            if (nameText != null)
                so.FindProperty("enemyNameText").objectReferenceValue = nameText.GetComponent<TextMeshProUGUI>();

            // Health Bar
            Transform healthBarPanel = FindChildRecursive(aiPanel, "HealthBarPanel");
            if (healthBarPanel != null)
            {
                Transform healthFill = FindChildByNameContains(healthBarPanel, "Fill") ?? FindChildByNameContains(healthBarPanel, "Health");
                Transform shieldFill = FindChildByNameContains(healthBarPanel, "Shield");

                if (healthFill == null)
                {
                    foreach (Transform child in healthBarPanel)
                    {
                        Image img = child.GetComponent<Image>();
                        if (img != null && img.type == Image.Type.Filled)
                        {
                            if (healthFill == null)
                                healthFill = child;
                            else if (shieldFill == null)
                                shieldFill = child;
                        }
                    }
                }

                if (healthFill != null)
                    so.FindProperty("enemyHealthBarFill").objectReferenceValue = healthFill.GetComponent<Image>();
                if (shieldFill != null)
                    so.FindProperty("enemyShieldBarFill").objectReferenceValue = shieldFill.GetComponent<Image>();

                TextMeshProUGUI healthText = healthBarPanel.GetComponentInChildren<TextMeshProUGUI>();
                if (healthText != null)
                    so.FindProperty("enemyHealthText").objectReferenceValue = healthText;
            }

            // Shield Text
            Transform shieldText = FindChildByNameContains(aiPanel, "ShieldText");
            if (shieldText != null)
                so.FindProperty("enemyShieldText").objectReferenceValue = shieldText.GetComponent<TextMeshProUGUI>();

            // Enemy Ability Display
            Transform enemyAbilities = FindChildRecursive(aiPanel, "EquippedAbilitiesPanel");
            if (enemyAbilities != null)
            {
                CombatAbilityDisplay display = enemyAbilities.GetComponent<CombatAbilityDisplay>();
                so.FindProperty("enemyAbilityDisplay").objectReferenceValue = display;

                if (display != null)
                {
                    AssignAbilityPrefab(display);
                }
            }
        }

        // Flee Button - look for Button with "Back" or "Flee" in name or child text
        Transform fleeButton = FindChildByNameContains(combatPanel.transform, "Back") ??
                               FindChildByNameContains(combatPanel.transform, "Flee") ??
                               FindChildByNameContains(combatPanel.transform, "Button (2)");
        if (fleeButton != null)
            so.FindProperty("fleeButton").objectReferenceValue = fleeButton.GetComponent<Button>();

        // Combat Log
        Transform scrollView = FindChildRecursive(combatPanel.transform, "Scroll View");
        if (scrollView != null)
        {
            so.FindProperty("combatLogScrollRect").objectReferenceValue = scrollView.GetComponent<ScrollRect>();

            // Find text in viewport > content
            Transform viewport = FindChildRecursive(scrollView, "Viewport");
            if (viewport != null)
            {
                Transform content = FindChildRecursive(viewport, "Content");
                if (content != null)
                {
                    TextMeshProUGUI logText = content.GetComponentInChildren<TextMeshProUGUI>();
                    if (logText != null)
                        so.FindProperty("combatLogText").objectReferenceValue = logText;
                }
            }
        }

        so.ApplyModifiedProperties();

        Debug.Log("Wired all references for CombatPanelUI!");
        EditorUtility.SetDirty(panelUI);
    }

    private void AssignAbilityPrefab(CombatAbilityDisplay display)
    {
        string prefabPath = "Assets/Prefabs/UI/Combat/CombatAbilityUI.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (prefab != null)
        {
            SerializedObject displaySO = new SerializedObject(display);
            displaySO.FindProperty("abilityPrefab").objectReferenceValue = prefab;
            displaySO.ApplyModifiedProperties();
            Debug.Log("Assigned ability prefab to " + display.gameObject.name);
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;

        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private Transform FindChildByNameContains(Transform parent, string namePart)
    {
        foreach (Transform child in parent)
        {
            if (child.name.ToLower().Contains(namePart.ToLower())) return child;
        }
        // Recursive search
        foreach (Transform child in parent)
        {
            Transform found = FindChildByNameContains(child, namePart);
            if (found != null) return found;
        }
        return null;
    }
}
#endif
