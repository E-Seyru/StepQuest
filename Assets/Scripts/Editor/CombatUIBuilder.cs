// Purpose: Editor tool to automatically generate the Combat UI structure
// Filepath: Assets/Scripts/Editor/CombatUIBuilder.cs

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CombatUIBuilder : EditorWindow
{
    private Canvas targetCanvas;
    private Sprite defaultCardSprite;
    private Sprite defaultCharacterSprite;
    private Color healthBarColor = new Color(0.4f, 0.8f, 0.4f, 1f); // Green
    private Color shieldBarColor = new Color(0.4f, 0.6f, 1f, 1f); // Blue
    private Color cooldownOverlayColor = new Color(0f, 0f, 0f, 0.6f);
    private Color panelBackgroundColor = new Color(0.6f, 0.4f, 0.3f, 1f); // Brown like original
    private int topPadding = 150; // Padding to avoid overlay

    [MenuItem("StepQuest/Combat/UI Builder")]
    public static void ShowWindow()
    {
        GetWindow<CombatUIBuilder>("Combat UI Builder");
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Combat UI Builder", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This tool creates the complete Combat UI structure in your scene.", MessageType.Info);

        EditorGUILayout.Space(10);

        targetCanvas = (Canvas)EditorGUILayout.ObjectField("Target Canvas", targetCanvas, typeof(Canvas), true);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Optional Sprites", EditorStyles.boldLabel);
        defaultCardSprite = (Sprite)EditorGUILayout.ObjectField("Card Background", defaultCardSprite, typeof(Sprite), false);
        defaultCharacterSprite = (Sprite)EditorGUILayout.ObjectField("Default Character", defaultCharacterSprite, typeof(Sprite), false);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
        healthBarColor = EditorGUILayout.ColorField("Health Bar", healthBarColor);
        shieldBarColor = EditorGUILayout.ColorField("Shield Bar", shieldBarColor);
        panelBackgroundColor = EditorGUILayout.ColorField("Panel Background", panelBackgroundColor);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
        topPadding = EditorGUILayout.IntField("Top Padding (for overlay)", topPadding);

        EditorGUILayout.Space(20);

        if (targetCanvas == null)
        {
            EditorGUILayout.HelpBox("Please assign a Canvas to create the UI in.", MessageType.Warning);

            if (GUILayout.Button("Find or Create Canvas", GUILayout.Height(30)))
            {
                targetCanvas = FindObjectOfType<Canvas>();
                if (targetCanvas == null)
                {
                    GameObject canvasObj = new GameObject("Canvas");
                    targetCanvas = canvasObj.AddComponent<Canvas>();
                    targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasObj.AddComponent<CanvasScaler>();
                    canvasObj.AddComponent<GraphicRaycaster>();
                }
            }
        }

        EditorGUILayout.Space(10);

        GUI.enabled = targetCanvas != null;
        if (GUILayout.Button("Create Combat UI", GUILayout.Height(40)))
        {
            CreateCombatUI();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Create Ability Prefab", GUILayout.Height(30)))
        {
            CreateAbilityPrefab();
        }
        GUI.enabled = true;
    }

    private void CreateCombatUI()
    {
        Undo.SetCurrentGroupName("Create Combat UI");
        int undoGroup = Undo.GetCurrentGroup();

        // Main Combat Panel
        GameObject combatPanelObj = CreateUIElement("CombatPanel", targetCanvas.transform);
        RectTransform combatPanelRect = combatPanelObj.GetComponent<RectTransform>();
        combatPanelRect.anchorMin = Vector2.zero;
        combatPanelRect.anchorMax = Vector2.one;
        combatPanelRect.offsetMin = Vector2.zero;
        combatPanelRect.offsetMax = Vector2.zero;

        Image combatPanelBg = combatPanelObj.AddComponent<Image>();
        combatPanelBg.color = new Color(0, 0, 0, 0.8f);

        CombatPanelUI combatPanelUI = combatPanelObj.AddComponent<CombatPanelUI>();

        // Add Vertical Layout with top padding to avoid overlay
        VerticalLayoutGroup mainLayout = combatPanelObj.AddComponent<VerticalLayoutGroup>();
        mainLayout.spacing = 10;
        mainLayout.padding = new RectOffset(10, 10, topPadding, 10); // Top padding to avoid overlay
        mainLayout.childAlignment = TextAnchor.UpperCenter;
        mainLayout.childControlHeight = false;
        mainLayout.childControlWidth = true;
        mainLayout.childForceExpandHeight = false;
        mainLayout.childForceExpandWidth = true;

        // === TOP SECTION (Character Cards) ===
        GameObject topSection = CreateUIElement("TopSection", combatPanelObj.transform);
        SetRectSize(topSection, 0, 200);
        LayoutElement topLayout = topSection.AddComponent<LayoutElement>();
        topLayout.preferredHeight = 200;
        topLayout.flexibleWidth = 1;

        HorizontalLayoutGroup topHLayout = topSection.AddComponent<HorizontalLayoutGroup>();
        topHLayout.spacing = 20;
        topHLayout.childAlignment = TextAnchor.MiddleCenter;
        topHLayout.childControlWidth = false;
        topHLayout.childControlHeight = true;
        topHLayout.childForceExpandWidth = false;

        // Player Card
        GameObject playerCard = CreateCharacterCard("PlayerCard", topSection.transform, true);

        // Enemy Card
        GameObject enemyCard = CreateCharacterCard("EnemyCard", topSection.transform, false);

        // === ABILITY SECTION ===
        GameObject abilitySection = CreateUIElement("AbilitySection", combatPanelObj.transform);
        SetRectSize(abilitySection, 0, 120);
        LayoutElement abilityLayout = abilitySection.AddComponent<LayoutElement>();
        abilityLayout.preferredHeight = 120;
        abilityLayout.flexibleWidth = 1;

        HorizontalLayoutGroup abilityHLayout = abilitySection.AddComponent<HorizontalLayoutGroup>();
        abilityHLayout.spacing = 20;
        abilityHLayout.childAlignment = TextAnchor.MiddleCenter;
        abilityHLayout.childControlWidth = true;
        abilityHLayout.childControlHeight = true;
        abilityHLayout.childForceExpandWidth = true;

        // Player Abilities Display
        GameObject playerAbilities = CreateAbilityDisplay("PlayerAbilityDisplay", abilitySection.transform);

        // Enemy Abilities Display
        GameObject enemyAbilities = CreateAbilityDisplay("EnemyAbilityDisplay", abilitySection.transform);

        // === BUTTON SECTION ===
        GameObject buttonSection = CreateUIElement("ButtonSection", combatPanelObj.transform);
        SetRectSize(buttonSection, 0, 80);
        LayoutElement buttonLayout = buttonSection.AddComponent<LayoutElement>();
        buttonLayout.preferredHeight = 80;

        HorizontalLayoutGroup buttonHLayout = buttonSection.AddComponent<HorizontalLayoutGroup>();
        buttonHLayout.spacing = 20;
        buttonHLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonHLayout.childControlWidth = false;
        buttonHLayout.childControlHeight = false;

        // Flee Button
        GameObject fleeButton = CreateButton("FleeButton", buttonSection.transform, "Fuir");
        SetRectSize(fleeButton, 150, 50);

        // === COMBAT LOG SECTION ===
        GameObject logSection = CreateUIElement("CombatLogSection", combatPanelObj.transform);
        LayoutElement logLayout = logSection.AddComponent<LayoutElement>();
        logLayout.flexibleHeight = 1;
        logLayout.flexibleWidth = 1;

        Image logBg = logSection.AddComponent<Image>();
        logBg.color = panelBackgroundColor;

        // ScrollView for combat log
        GameObject scrollView = CreateScrollView("CombatLogScrollView", logSection.transform);

        // === ASSIGN REFERENCES TO CombatPanelUI ===
        SerializedObject serializedPanel = new SerializedObject(combatPanelUI);

        // Panel reference
        serializedPanel.FindProperty("combatPanel").objectReferenceValue = combatPanelObj;

        // Player references
        serializedPanel.FindProperty("playerHealthBarFill").objectReferenceValue =
            playerCard.transform.Find("HealthBarBG/HealthBarFill")?.GetComponent<Image>();
        serializedPanel.FindProperty("playerShieldBarFill").objectReferenceValue =
            playerCard.transform.Find("HealthBarBG/ShieldBarFill")?.GetComponent<Image>();
        serializedPanel.FindProperty("playerHealthText").objectReferenceValue =
            playerCard.transform.Find("HealthBarBG/HealthText")?.GetComponent<TextMeshProUGUI>();
        serializedPanel.FindProperty("playerShieldText").objectReferenceValue =
            playerCard.transform.Find("ShieldText")?.GetComponent<TextMeshProUGUI>();
        serializedPanel.FindProperty("playerImage").objectReferenceValue =
            playerCard.transform.Find("CharacterImage")?.GetComponent<Image>();
        serializedPanel.FindProperty("playerImageTransform").objectReferenceValue =
            playerCard.transform.Find("CharacterImage")?.GetComponent<RectTransform>();

        // Enemy references
        serializedPanel.FindProperty("enemyHealthBarFill").objectReferenceValue =
            enemyCard.transform.Find("HealthBarBG/HealthBarFill")?.GetComponent<Image>();
        serializedPanel.FindProperty("enemyShieldBarFill").objectReferenceValue =
            enemyCard.transform.Find("HealthBarBG/ShieldBarFill")?.GetComponent<Image>();
        serializedPanel.FindProperty("enemyHealthText").objectReferenceValue =
            enemyCard.transform.Find("HealthBarBG/HealthText")?.GetComponent<TextMeshProUGUI>();
        serializedPanel.FindProperty("enemyShieldText").objectReferenceValue =
            enemyCard.transform.Find("ShieldText")?.GetComponent<TextMeshProUGUI>();
        serializedPanel.FindProperty("enemyNameText").objectReferenceValue =
            enemyCard.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        serializedPanel.FindProperty("enemyImage").objectReferenceValue =
            enemyCard.transform.Find("CharacterImage")?.GetComponent<Image>();
        serializedPanel.FindProperty("enemyImageTransform").objectReferenceValue =
            enemyCard.transform.Find("CharacterImage")?.GetComponent<RectTransform>();

        // Ability displays
        serializedPanel.FindProperty("playerAbilityDisplay").objectReferenceValue =
            playerAbilities.GetComponent<CombatAbilityDisplay>();
        serializedPanel.FindProperty("enemyAbilityDisplay").objectReferenceValue =
            enemyAbilities.GetComponent<CombatAbilityDisplay>();

        // Buttons
        serializedPanel.FindProperty("fleeButton").objectReferenceValue =
            fleeButton.GetComponent<Button>();

        // Combat log
        TextMeshProUGUI logText = scrollView.GetComponentInChildren<TextMeshProUGUI>();
        ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
        serializedPanel.FindProperty("combatLogText").objectReferenceValue = logText;
        serializedPanel.FindProperty("combatLogScrollRect").objectReferenceValue = scrollRect;

        serializedPanel.ApplyModifiedProperties();

        Undo.CollapseUndoOperations(undoGroup);

        Selection.activeGameObject = combatPanelObj;
        EditorGUIUtility.PingObject(combatPanelObj);

        Logger.LogInfo("Combat UI created successfully! Don't forget to create and assign the Ability Prefab.", Logger.LogCategory.EditorLog);
    }

    private GameObject CreateCharacterCard(string name, Transform parent, bool isPlayer)
    {
        GameObject card = CreateUIElement(name, parent);
        SetRectSize(card, 150, 200);
        LayoutElement cardLayout = card.AddComponent<LayoutElement>();
        cardLayout.preferredWidth = 150;
        cardLayout.preferredHeight = 200;

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = panelBackgroundColor;
        if (defaultCardSprite != null)
            cardBg.sprite = defaultCardSprite;

        // Character Image
        GameObject charImage = CreateUIElement("CharacterImage", card.transform);
        RectTransform charRect = charImage.GetComponent<RectTransform>();
        charRect.anchorMin = new Vector2(0.1f, 0.25f);
        charRect.anchorMax = new Vector2(0.9f, 0.95f);
        charRect.offsetMin = Vector2.zero;
        charRect.offsetMax = Vector2.zero;

        Image charImg = charImage.AddComponent<Image>();
        charImg.color = Color.white;
        if (defaultCharacterSprite != null)
            charImg.sprite = defaultCharacterSprite;

        // Health Bar Background
        GameObject healthBarBG = CreateUIElement("HealthBarBG", card.transform);
        RectTransform healthBgRect = healthBarBG.GetComponent<RectTransform>();
        healthBgRect.anchorMin = new Vector2(0.05f, 0.05f);
        healthBgRect.anchorMax = new Vector2(0.95f, 0.18f);
        healthBgRect.offsetMin = Vector2.zero;
        healthBgRect.offsetMax = Vector2.zero;

        Image healthBgImg = healthBarBG.AddComponent<Image>();
        healthBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Health Bar Fill
        GameObject healthFill = CreateUIElement("HealthBarFill", healthBarBG.transform);
        RectTransform healthFillRect = healthFill.GetComponent<RectTransform>();
        healthFillRect.anchorMin = Vector2.zero;
        healthFillRect.anchorMax = Vector2.one;
        healthFillRect.offsetMin = Vector2.zero;
        healthFillRect.offsetMax = Vector2.zero;

        Image healthFillImg = healthFill.AddComponent<Image>();
        healthFillImg.color = healthBarColor;
        healthFillImg.type = Image.Type.Filled;
        healthFillImg.fillMethod = Image.FillMethod.Horizontal;
        healthFillImg.fillOrigin = 0;
        healthFillImg.fillAmount = 1f;

        // Shield Bar Fill (on top of health)
        GameObject shieldFill = CreateUIElement("ShieldBarFill", healthBarBG.transform);
        RectTransform shieldFillRect = shieldFill.GetComponent<RectTransform>();
        shieldFillRect.anchorMin = Vector2.zero;
        shieldFillRect.anchorMax = Vector2.one;
        shieldFillRect.offsetMin = Vector2.zero;
        shieldFillRect.offsetMax = Vector2.zero;

        Image shieldFillImg = shieldFill.AddComponent<Image>();
        shieldFillImg.color = shieldBarColor;
        shieldFillImg.type = Image.Type.Filled;
        shieldFillImg.fillMethod = Image.FillMethod.Horizontal;
        shieldFillImg.fillOrigin = 0;
        shieldFillImg.fillAmount = 0f;

        // Health Text
        GameObject healthText = CreateUIElement("HealthText", healthBarBG.transform);
        RectTransform healthTextRect = healthText.GetComponent<RectTransform>();
        healthTextRect.anchorMin = Vector2.zero;
        healthTextRect.anchorMax = Vector2.one;
        healthTextRect.offsetMin = Vector2.zero;
        healthTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI healthTmp = healthText.AddComponent<TextMeshProUGUI>();
        healthTmp.text = "100/100";
        healthTmp.fontSize = 14;
        healthTmp.alignment = TextAlignmentOptions.Center;
        healthTmp.color = Color.white;

        // Shield Text (above card)
        GameObject shieldText = CreateUIElement("ShieldText", card.transform);
        RectTransform shieldTextRect = shieldText.GetComponent<RectTransform>();
        shieldTextRect.anchorMin = new Vector2(0.7f, 0.85f);
        shieldTextRect.anchorMax = new Vector2(1f, 1f);
        shieldTextRect.offsetMin = Vector2.zero;
        shieldTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI shieldTmp = shieldText.AddComponent<TextMeshProUGUI>();
        shieldTmp.text = "";
        shieldTmp.fontSize = 16;
        shieldTmp.alignment = TextAlignmentOptions.Center;
        shieldTmp.color = shieldBarColor;
        shieldText.SetActive(false);

        // Name Text (for enemy only)
        if (!isPlayer)
        {
            GameObject nameText = CreateUIElement("NameText", card.transform);
            RectTransform nameTextRect = nameText.GetComponent<RectTransform>();
            nameTextRect.anchorMin = new Vector2(0f, 0.18f);
            nameTextRect.anchorMax = new Vector2(1f, 0.25f);
            nameTextRect.offsetMin = Vector2.zero;
            nameTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI nameTmp = nameText.AddComponent<TextMeshProUGUI>();
            nameTmp.text = "Enemy";
            nameTmp.fontSize = 12;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = Color.white;
        }

        return card;
    }

    private GameObject CreateAbilityDisplay(string name, Transform parent)
    {
        GameObject display = CreateUIElement(name, parent);

        Image displayBg = display.AddComponent<Image>();
        displayBg.color = new Color(panelBackgroundColor.r * 0.8f, panelBackgroundColor.g * 0.8f, panelBackgroundColor.b * 0.8f, 1f);

        LayoutElement layoutElement = display.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1;
        layoutElement.preferredHeight = 100;

        VerticalLayoutGroup vlg = display.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 5;
        vlg.padding = new RectOffset(5, 5, 5, 5);

        CombatAbilityDisplay combatDisplay = display.AddComponent<CombatAbilityDisplay>();

        return display;
    }

    private GameObject CreateButton(string name, Transform parent, string text)
    {
        GameObject buttonObj = CreateUIElement(name, parent);
        LayoutElement layout = buttonObj.AddComponent<LayoutElement>();
        layout.preferredWidth = 150;
        layout.preferredHeight = 50;

        Image buttonImg = buttonObj.AddComponent<Image>();
        buttonImg.color = panelBackgroundColor;

        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.8f, 0.6f, 0.5f, 1f);
        colors.pressedColor = new Color(0.5f, 0.3f, 0.2f, 1f);
        button.colors = colors;

        GameObject textObj = CreateUIElement("Text", buttonObj.transform);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return buttonObj;
    }

    private GameObject CreateScrollView(string name, Transform parent)
    {
        GameObject scrollViewObj = CreateUIElement(name, parent);
        RectTransform scrollRect = scrollViewObj.GetComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(5, 5);
        scrollRect.offsetMax = new Vector2(-5, -5);

        ScrollRect scroll = scrollViewObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        Image scrollBg = scrollViewObj.AddComponent<Image>();
        scrollBg.color = new Color(0, 0, 0, 0.3f);

        // Viewport
        GameObject viewport = CreateUIElement("Viewport", scrollViewObj.transform);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Image viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = Color.white;
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        // Content
        GameObject content = CreateUIElement("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;

        // Text
        GameObject textObj = CreateUIElement("CombatLogText", content.transform);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        tmp.richText = true;

        LayoutElement textLayout = textObj.AddComponent<LayoutElement>();
        textLayout.flexibleWidth = 1;

        // Assign scroll references
        scroll.viewport = viewportRect;
        scroll.content = contentRect;

        return scrollViewObj;
    }

    private void CreateAbilityPrefab()
    {
        // Create the prefab structure
        // The root IS the ability image, with overlay as child on top
        GameObject prefabRoot = new GameObject("CombatAbilityUI");

        RectTransform rootRect = prefabRoot.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(80, 80);

        // The root object has the ability image directly
        Image abilityImg = prefabRoot.AddComponent<Image>();
        abilityImg.color = Color.white;

        // Cooldown Overlay as child (renders on top of parent image)
        GameObject cooldownObj = new GameObject("CooldownOverlay", typeof(RectTransform));
        cooldownObj.transform.SetParent(prefabRoot.transform, false);
        RectTransform cooldownRect = cooldownObj.GetComponent<RectTransform>();
        cooldownRect.anchorMin = Vector2.zero;
        cooldownRect.anchorMax = Vector2.one;
        cooldownRect.offsetMin = Vector2.zero;
        cooldownRect.offsetMax = Vector2.zero;

        Image cooldownImg = cooldownObj.AddComponent<Image>();
        cooldownImg.color = cooldownOverlayColor;
        cooldownObj.SetActive(false);

        // Add the CombatAbilityUI component to root
        CombatAbilityUI abilityUI = prefabRoot.AddComponent<CombatAbilityUI>();

        // Assign references via SerializedObject
        // abilityImage is now on the root itself
        SerializedObject serializedAbility = new SerializedObject(abilityUI);
        serializedAbility.FindProperty("abilityImage").objectReferenceValue = abilityImg;
        serializedAbility.FindProperty("cooldownOverlay").objectReferenceValue = cooldownImg;
        serializedAbility.ApplyModifiedProperties();

        // Save as prefab
        string prefabPath = "Assets/Prefabs/UI/Combat";
        if (!AssetDatabase.IsValidFolder(prefabPath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            AssetDatabase.CreateFolder("Assets/Prefabs/UI", "Combat");
        }

        string fullPath = prefabPath + "/CombatAbilityUI.prefab";

        // Check if prefab already exists
        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
        if (existingPrefab != null)
        {
            if (!EditorUtility.DisplayDialog("Prefab Exists",
                "CombatAbilityUI.prefab already exists. Overwrite?", "Yes", "No"))
            {
                DestroyImmediate(prefabRoot);
                return;
            }
        }

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, fullPath);
        DestroyImmediate(prefabRoot);

        // Select the created prefab
        GameObject createdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
        Selection.activeObject = createdPrefab;
        EditorGUIUtility.PingObject(createdPrefab);

        Logger.LogInfo($"Ability prefab created at: {fullPath}", Logger.LogCategory.EditorLog);
        Logger.LogInfo("Now assign this prefab to the CombatAbilityDisplay components in your Combat UI!", Logger.LogCategory.EditorLog);
    }

    private GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        return obj;
    }

    private void SetRectSize(GameObject obj, float width, float height)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
    }
}
#endif
