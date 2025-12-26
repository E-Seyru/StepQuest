// Purpose: Editor window to create and manage combat abilities
// Filepath: Assets/Scripts/Editor/AbilityManagerWindow.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AbilityManagerWindow : EditorWindow
{
    [MenuItem("WalkAndRPG/Combat/Ability Manager")]
    public static void ShowWindow()
    {
        AbilityManagerWindow window = GetWindow<AbilityManagerWindow>();
        window.titleContent = new GUIContent("Ability Manager");
        window.minSize = new Vector2(500, 400);
        window.Show();
    }

    // Data
    private AbilityRegistry abilityRegistry;
    private StatusEffectRegistry statusEffectRegistry;

    // UI State
    private Vector2 scrollPosition;
    private string searchFilter = "";
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Abilities", "Quick Create", "Validation" };

    // Filter State
    private bool showAllEffectTypes = true;
    private AbilityEffectType filterEffectType = AbilityEffectType.Damage;

    // Creation Dialog State
    private bool showCreateAbilityDialog = false;
    private Vector2 dialogScrollPosition;

    // New ability fields
    private string newAbilityName = "";
    private string newAbilityDescription = "";
    private Sprite newAbilityIcon = null;
    private Color newAbilityColor = Color.white;
    private float newAbilityCooldown = 2f;
    private int newAbilityWeight = 1;
    private List<AbilityEffect> newAbilityEffects = new List<AbilityEffect>();

    // Colors for effect types
    private static readonly Dictionary<AbilityEffectType, Color> EffectTypeColors = new Dictionary<AbilityEffectType, Color>
    {
        { AbilityEffectType.Damage, new Color(1f, 0.3f, 0.3f) },      // Red
        { AbilityEffectType.Heal, new Color(0.3f, 1f, 0.3f) },        // Green
        { AbilityEffectType.Shield, new Color(1f, 0.9f, 0.2f) },      // Yellow
        { AbilityEffectType.StatusEffect, new Color(1f, 0.6f, 0.2f) } // Orange
    };

    void OnEnable()
    {
        LoadRegistries();
    }

    void OnGUI()
    {
        DrawHeader();

        EditorGUILayout.Space();

        // Tab selection
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        switch (selectedTab)
        {
            case 0:
                DrawAbilitiesTab();
                break;
            case 1:
                DrawQuickCreateTab();
                break;
            case 2:
                DrawValidationTab();
                break;
        }

        EditorGUILayout.EndScrollView();

        // Handle creation dialog
        if (showCreateAbilityDialog)
            DrawCreateAbilityDialog();
    }

    #region Header
    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");

        GUILayout.Label("Ability Manager", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        // Registry selection
        abilityRegistry = (AbilityRegistry)EditorGUILayout.ObjectField("Ability Registry", abilityRegistry, typeof(AbilityRegistry), false);

        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            LoadRegistries();
        }

        if (GUILayout.Button("Validate", GUILayout.Width(60)))
        {
            ValidateRegistry();
        }

        EditorGUILayout.EndHorizontal();

        // Search and filters (only for Abilities tab)
        if (selectedTab == 0)
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            showAllEffectTypes = EditorGUILayout.Toggle("Show All Types", showAllEffectTypes, GUILayout.Width(120));

            if (!showAllEffectTypes)
            {
                EditorGUILayout.LabelField("Filter:", GUILayout.Width(40));
                filterEffectType = (AbilityEffectType)EditorGUILayout.EnumPopup(filterEffectType);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }
    #endregion

    #region Abilities Tab
    private void DrawAbilitiesTab()
    {
        if (abilityRegistry == null)
        {
            EditorGUILayout.HelpBox("Select an AbilityRegistry to manage abilities.", MessageType.Info);

            if (GUILayout.Button("Create New Ability Registry"))
            {
                CreateAbilityRegistry();
            }
            return;
        }

        // Create New Ability button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create New Ability", GUILayout.Width(150)))
        {
            showCreateAbilityDialog = true;
            ResetCreateAbilityDialog();
        }

        if (GUILayout.Button("Auto-Populate", GUILayout.Width(100)))
        {
            abilityRegistry.AutoPopulateRegistry();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        var filteredAbilities = GetFilteredAbilities();

        if (filteredAbilities.Count == 0)
        {
            EditorGUILayout.HelpBox("No abilities found matching current filters.", MessageType.Info);
            return;
        }

        // Stats summary
        EditorGUILayout.LabelField($"Abilities Found: {filteredAbilities.Count}", EditorStyles.boldLabel);
        DrawEffectTypesSummary(filteredAbilities);
        EditorGUILayout.Space();

        // Draw abilities
        foreach (var ability in filteredAbilities)
        {
            DrawAbilityEntry(ability);
        }
    }

    private void DrawEffectTypesSummary(List<AbilityDefinition> abilities)
    {
        var typeCounts = new Dictionary<AbilityEffectType, int>();

        foreach (var ability in abilities)
        {
            var types = GetAbilityEffectTypes(ability);
            foreach (var type in types)
            {
                if (!typeCounts.ContainsKey(type))
                    typeCounts[type] = 0;
                typeCounts[type]++;
            }
        }

        EditorGUILayout.BeginHorizontal();
        foreach (var kvp in typeCounts)
        {
            var oldColor = GUI.color;
            GUI.color = GetEffectTypeColor(kvp.Key);
            EditorGUILayout.LabelField($"{kvp.Key}: {kvp.Value}", EditorStyles.miniLabel, GUILayout.Width(90));
            GUI.color = oldColor;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawAbilityEntry(AbilityDefinition ability)
    {
        EditorGUILayout.BeginVertical("box");

        // Header row
        EditorGUILayout.BeginHorizontal();

        // Icon preview
        if (ability.AbilityIcon != null)
        {
            Rect iconRect = EditorGUILayout.GetControlRect(GUILayout.Width(32), GUILayout.Height(32));
            DrawSprite(iconRect, ability.AbilityIcon);
        }
        else
        {
            EditorGUILayout.LabelField("[No Icon]", GUILayout.Width(50), GUILayout.Height(32));
        }

        EditorGUILayout.BeginVertical();

        // Name and effect types
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(ability.GetDisplayName(), EditorStyles.boldLabel, GUILayout.Width(150));

        // Effect type badges
        var effectTypes = GetAbilityEffectTypes(ability);
        foreach (var effectType in effectTypes)
        {
            var oldColor = GUI.color;
            GUI.color = GetEffectTypeColor(effectType);
            EditorGUILayout.LabelField($"[{effectType}]", EditorStyles.miniLabel, GUILayout.Width(70));
            GUI.color = oldColor;
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Edit", GUILayout.Width(40)))
        {
            Selection.activeObject = ability;
            EditorGUIUtility.PingObject(ability);
        }

        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            RemoveAbilityFromRegistry(ability);
        }

        EditorGUILayout.EndHorizontal();

        // Stats row
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"ID: {ability.AbilityID}", EditorStyles.miniLabel, GUILayout.Width(120));
        EditorGUILayout.LabelField($"CD: {ability.Cooldown}s", EditorStyles.miniLabel, GUILayout.Width(60));
        EditorGUILayout.LabelField($"Weight: {ability.Weight}", EditorStyles.miniLabel, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        // Effects summary
        EditorGUILayout.LabelField($"Effects: {ability.GetEffectsSummary()}", EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }
    #endregion

    #region Quick Create Tab
    private void DrawQuickCreateTab()
    {
        if (abilityRegistry == null)
        {
            EditorGUILayout.HelpBox("Select an AbilityRegistry first.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Quick Create Templates", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Click a button to create a pre-configured ability with sensible defaults.", MessageType.Info);
        EditorGUILayout.Space();

        // Damage abilities
        EditorGUILayout.LabelField("Damage Abilities", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Basic Attack\n(10 dmg, 2s CD)", GUILayout.Height(40)))
        {
            QuickCreateAbility("Basic Attack", AbilityEffectType.Damage, 10f, 2f);
        }
        if (GUILayout.Button("Heavy Strike\n(25 dmg, 4s CD)", GUILayout.Height(40)))
        {
            QuickCreateAbility("Heavy Strike", AbilityEffectType.Damage, 25f, 4f);
        }
        if (GUILayout.Button("Quick Slash\n(5 dmg, 1s CD)", GUILayout.Height(40)))
        {
            QuickCreateAbility("Quick Slash", AbilityEffectType.Damage, 5f, 1f);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Healing abilities
        EditorGUILayout.LabelField("Healing Abilities", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Heal\n(15 heal, 5s CD)", GUILayout.Height(40)))
        {
            QuickCreateAbility("Heal", AbilityEffectType.Heal, 15f, 5f, true);
        }
        if (GUILayout.Button("Greater Heal\n(30 heal, 8s CD)", GUILayout.Height(40)))
        {
            QuickCreateAbility("Greater Heal", AbilityEffectType.Heal, 30f, 8f, true);
        }
        if (GUILayout.Button("Minor Heal\n(8 heal, 3s CD)", GUILayout.Height(40)))
        {
            QuickCreateAbility("Minor Heal", AbilityEffectType.Heal, 8f, 3f, true);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Shield abilities
        EditorGUILayout.LabelField("Shield Abilities", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Shield\n(20 shield, 6s CD)", GUILayout.Height(40)))
        {
            QuickCreateAbility("Shield", AbilityEffectType.Shield, 20f, 6f, true);
        }
        if (GUILayout.Button("Barrier\n(40 shield, 10s CD)", GUILayout.Height(40)))
        {
            QuickCreateAbility("Barrier", AbilityEffectType.Shield, 40f, 10f, true);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Combo abilities
        EditorGUILayout.LabelField("Combo Abilities", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Shield Bash\n(5 dmg + 20 shield)", GUILayout.Height(40)))
        {
            QuickCreateComboAbility("Shield Bash", 5f, 0f, 20f, 6f);
        }
        if (GUILayout.Button("Vampiric Strike\n(12 dmg + 6 heal)", GUILayout.Height(40)))
        {
            QuickCreateComboAbility("Vampiric Strike", 12f, 6f, 0f, 5f);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Status effect abilities (if status effect registry is available)
        if (statusEffectRegistry != null && statusEffectRegistry.AllEffects.Count > 0)
        {
            EditorGUILayout.LabelField("Status Effect Abilities", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("These require status effects to be set up first.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            var poisonEffect = statusEffectRegistry.GetEffect("poison");
            if (poisonEffect != null)
            {
                if (GUILayout.Button("Poison Strike\n(5 dmg + Poison)", GUILayout.Height(40)))
                {
                    QuickCreateStatusEffectAbility("Poison Strike", 5f, poisonEffect, 3, 4f);
                }
            }

            var stunEffect = statusEffectRegistry.GetEffect("stun");
            if (stunEffect != null)
            {
                if (GUILayout.Button("Stunning Blow\n(3 dmg + Stun)", GUILayout.Height(40)))
                {
                    QuickCreateStatusEffectAbility("Stunning Blow", 3f, stunEffect, 1, 6f);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void QuickCreateAbility(string name, AbilityEffectType type, float value, float cooldown, bool targetsSelf = false)
    {
        var ability = CreateInstance<AbilityDefinition>();
        ability.AbilityName = name;
        ability.AbilityID = GenerateIDFromName(name);
        ability.Cooldown = cooldown;
        ability.Weight = 1;
        ability.AbilityColor = GetEffectTypeColor(type);

        ability.Effects = new List<AbilityEffect>
        {
            new AbilityEffect
            {
                Type = type,
                Value = value,
                TargetsSelf = targetsSelf
            }
        };

        SaveAbilityAsset(ability);
    }

    private void QuickCreateComboAbility(string name, float damage, float heal, float shield, float cooldown)
    {
        var ability = CreateInstance<AbilityDefinition>();
        ability.AbilityName = name;
        ability.AbilityID = GenerateIDFromName(name);
        ability.Cooldown = cooldown;
        ability.Weight = 1;
        ability.AbilityColor = Color.white;

        ability.Effects = new List<AbilityEffect>();

        if (damage > 0)
        {
            ability.Effects.Add(new AbilityEffect
            {
                Type = AbilityEffectType.Damage,
                Value = damage,
                TargetsSelf = false
            });
        }

        if (heal > 0)
        {
            ability.Effects.Add(new AbilityEffect
            {
                Type = AbilityEffectType.Heal,
                Value = heal,
                TargetsSelf = true
            });
        }

        if (shield > 0)
        {
            ability.Effects.Add(new AbilityEffect
            {
                Type = AbilityEffectType.Shield,
                Value = shield,
                TargetsSelf = true
            });
        }

        SaveAbilityAsset(ability);
    }

    private void QuickCreateStatusEffectAbility(string name, float damage, StatusEffectDefinition statusEffect, int stacks, float cooldown)
    {
        var ability = CreateInstance<AbilityDefinition>();
        ability.AbilityName = name;
        ability.AbilityID = GenerateIDFromName(name);
        ability.Cooldown = cooldown;
        ability.Weight = 1;
        ability.AbilityColor = statusEffect.EffectColor;

        ability.Effects = new List<AbilityEffect>();

        if (damage > 0)
        {
            ability.Effects.Add(new AbilityEffect
            {
                Type = AbilityEffectType.Damage,
                Value = damage,
                TargetsSelf = false
            });
        }

        ability.Effects.Add(new AbilityEffect
        {
            Type = AbilityEffectType.StatusEffect,
            StatusEffect = statusEffect,
            StatusEffectStacks = stacks,
            TargetsSelf = false
        });

        SaveAbilityAsset(ability);
    }
    #endregion

    #region Validation Tab
    private void DrawValidationTab()
    {
        if (abilityRegistry == null)
        {
            EditorGUILayout.HelpBox("Select an AbilityRegistry first.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Registry Validation", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Validation status
        EditorGUILayout.LabelField("Validation Status:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(abilityRegistry.ValidationStatus, MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Run Validation", GUILayout.Height(30)))
        {
            abilityRegistry.ValidateRegistry();
        }

        EditorGUILayout.Space();

        // Statistics
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);

        var validAbilities = abilityRegistry.GetAllValidAbilities();
        EditorGUILayout.LabelField($"Total Abilities: {validAbilities.Count}");

        // Count by effect type
        var effectTypeCounts = new Dictionary<AbilityEffectType, int>();

        foreach (var ability in validAbilities)
        {
            var types = GetAbilityEffectTypes(ability);
            foreach (var type in types)
            {
                if (!effectTypeCounts.ContainsKey(type))
                    effectTypeCounts[type] = 0;
                effectTypeCounts[type]++;
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("By Effect Type:", EditorStyles.boldLabel);

        foreach (var kvp in effectTypeCounts.OrderByDescending(x => x.Value))
        {
            var oldColor = GUI.color;
            GUI.color = GetEffectTypeColor(kvp.Key);
            EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value}");
            GUI.color = oldColor;
        }
    }
    #endregion

    #region Create Ability Dialog
    private void DrawCreateAbilityDialog()
    {
        // Create semi-transparent overlay
        var overlayRect = new Rect(0, 0, position.width, position.height);
        EditorGUI.DrawRect(overlayRect, new Color(0, 0, 0, 0.5f));

        // Center the dialog
        float dialogWidth = 500;
        float dialogHeight = 550;
        var dialogRect = new Rect(
            (position.width - dialogWidth) / 2,
            (position.height - dialogHeight) / 2,
            dialogWidth, dialogHeight);

        GUILayout.BeginArea(dialogRect);
        EditorGUILayout.BeginVertical(GUI.skin.window);

        EditorGUILayout.LabelField("Create New Ability", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        dialogScrollPosition = EditorGUILayout.BeginScrollView(dialogScrollPosition, GUILayout.Height(dialogHeight - 80));

        // Basic info
        EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
        newAbilityName = EditorGUILayout.TextField("Name", newAbilityName);
        newAbilityDescription = EditorGUILayout.TextField("Description", newAbilityDescription);
        newAbilityIcon = (Sprite)EditorGUILayout.ObjectField("Icon", newAbilityIcon, typeof(Sprite), false);
        newAbilityColor = EditorGUILayout.ColorField("Color", newAbilityColor);

        EditorGUILayout.Space();

        // Combat stats
        EditorGUILayout.LabelField("Combat Stats", EditorStyles.boldLabel);
        newAbilityCooldown = EditorGUILayout.Slider("Cooldown (s)", newAbilityCooldown, 0.1f, 30f);
        newAbilityWeight = EditorGUILayout.IntSlider("Weight", newAbilityWeight, 1, 10);

        EditorGUILayout.Space();

        // Effects
        EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel);

        for (int i = 0; i < newAbilityEffects.Count; i++)
        {
            DrawEffectEditor(i);
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+ Add Effect", GUILayout.Width(100)))
        {
            newAbilityEffects.Add(new AbilityEffect { Type = AbilityEffectType.Damage, Value = 10f });
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create"))
        {
            if (!string.IsNullOrEmpty(newAbilityName) && newAbilityEffects.Count > 0)
            {
                CreateNewAbility();
                showCreateAbilityDialog = false;
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Input", "Ability must have a name and at least one effect.", "OK");
            }
        }

        if (GUILayout.Button("Cancel"))
        {
            showCreateAbilityDialog = false;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();

        // Handle escape key
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            showCreateAbilityDialog = false;
            Event.current.Use();
        }
    }

    private void DrawEffectEditor(int index)
    {
        var effect = newAbilityEffects[index];

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();

        var oldColor = GUI.color;
        GUI.color = GetEffectTypeColor(effect.Type);
        EditorGUILayout.LabelField($"Effect {index + 1}", EditorStyles.boldLabel, GUILayout.Width(80));
        GUI.color = oldColor;

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            newAbilityEffects.RemoveAt(index);
            return;
        }

        EditorGUILayout.EndHorizontal();

        // Effect type
        effect.Type = (AbilityEffectType)EditorGUILayout.EnumPopup("Type", effect.Type);

        // Type-specific fields
        switch (effect.Type)
        {
            case AbilityEffectType.Damage:
                effect.Value = EditorGUILayout.FloatField("Damage", effect.Value);
                break;

            case AbilityEffectType.Heal:
                effect.Value = EditorGUILayout.FloatField("Heal Amount", effect.Value);
                effect.TargetsSelf = EditorGUILayout.Toggle("Targets Self", effect.TargetsSelf);
                break;

            case AbilityEffectType.Shield:
                effect.Value = EditorGUILayout.FloatField("Shield Amount", effect.Value);
                effect.TargetsSelf = EditorGUILayout.Toggle("Targets Self", effect.TargetsSelf);
                break;

            case AbilityEffectType.StatusEffect:
                effect.StatusEffect = (StatusEffectDefinition)EditorGUILayout.ObjectField(
                    "Status Effect", effect.StatusEffect, typeof(StatusEffectDefinition), false);
                effect.StatusEffectStacks = EditorGUILayout.IntField("Stacks", Mathf.Max(1, effect.StatusEffectStacks));
                effect.TargetsSelf = EditorGUILayout.Toggle("Targets Self", effect.TargetsSelf);

                // Quick select dropdown
                if (statusEffectRegistry != null && statusEffectRegistry.AllEffects.Count > 0)
                {
                    var effectNames = statusEffectRegistry.AllEffects
                        .Where(e => e != null)
                        .Select(e => e.GetDisplayName())
                        .ToArray();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Quick Select:", GUILayout.Width(80));
                    int selected = EditorGUILayout.Popup(-1, effectNames);
                    if (selected >= 0)
                    {
                        effect.StatusEffect = statusEffectRegistry.AllEffects[selected];
                    }
                    EditorGUILayout.EndHorizontal();
                }
                break;
        }

        newAbilityEffects[index] = effect;

        EditorGUILayout.EndVertical();
    }

    private void CreateNewAbility()
    {
        var ability = CreateInstance<AbilityDefinition>();
        ability.AbilityName = newAbilityName;
        ability.AbilityID = GenerateIDFromName(newAbilityName);
        ability.Description = newAbilityDescription;
        ability.AbilityIcon = newAbilityIcon;
        ability.AbilityColor = newAbilityColor;
        ability.Cooldown = newAbilityCooldown;
        ability.Weight = newAbilityWeight;
        ability.Effects = new List<AbilityEffect>(newAbilityEffects);

        SaveAbilityAsset(ability);
        ResetCreateAbilityDialog();
    }

    private void ResetCreateAbilityDialog()
    {
        newAbilityName = "";
        newAbilityDescription = "";
        newAbilityIcon = null;
        newAbilityColor = Color.white;
        newAbilityCooldown = 2f;
        newAbilityWeight = 1;
        newAbilityEffects = new List<AbilityEffect>();
    }
    #endregion

    #region Utility Methods
    private void LoadRegistries()
    {
        // Load ability registry
        if (abilityRegistry == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:AbilityRegistry");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                abilityRegistry = AssetDatabase.LoadAssetAtPath<AbilityRegistry>(path);
            }
        }

        // Load status effect registry
        if (statusEffectRegistry == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:StatusEffectRegistry");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                statusEffectRegistry = AssetDatabase.LoadAssetAtPath<StatusEffectRegistry>(path);
            }
        }
    }

    private void ValidateRegistry()
    {
        if (abilityRegistry != null)
        {
            abilityRegistry.ValidateRegistry();
            Logger.LogInfo("AbilityRegistry validation triggered", Logger.LogCategory.EditorLog);
        }
    }

    private void CreateAbilityRegistry()
    {
        var registry = CreateInstance<AbilityRegistry>();

        string folder = "Assets/ScriptableObjects";
        EnsureFolderExists(folder);

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/AbilityRegistry.asset");
        AssetDatabase.CreateAsset(registry, assetPath);
        AssetDatabase.SaveAssets();

        abilityRegistry = registry;
        Selection.activeObject = registry;
        EditorGUIUtility.PingObject(registry);

        Logger.LogInfo($"Created AbilityRegistry at {assetPath}", Logger.LogCategory.EditorLog);
    }

    private void SaveAbilityAsset(AbilityDefinition ability)
    {
        string folder = "Assets/ScriptableObjects/Abilities";
        EnsureFolderExists(folder);

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{ability.AbilityName}.asset");
        AssetDatabase.CreateAsset(ability, assetPath);
        AssetDatabase.SaveAssets();

        // Add to registry
        if (abilityRegistry != null)
        {
            abilityRegistry.AllAbilities.Add(ability);
            EditorUtility.SetDirty(abilityRegistry);
            AssetDatabase.SaveAssets();
        }

        Selection.activeObject = ability;
        EditorGUIUtility.PingObject(ability);

        Logger.LogInfo($"Created ability: {ability.AbilityName} at {assetPath}", Logger.LogCategory.EditorLog);
    }

    private List<AbilityDefinition> GetFilteredAbilities()
    {
        if (abilityRegistry == null) return new List<AbilityDefinition>();

        var abilities = abilityRegistry.AllAbilities.Where(a => a != null);

        // Filter by effect type
        if (!showAllEffectTypes)
        {
            abilities = abilities.Where(a => GetAbilityEffectTypes(a).Contains(filterEffectType));
        }

        // Filter by search
        if (!string.IsNullOrEmpty(searchFilter))
        {
            abilities = abilities.Where(a =>
                a.GetDisplayName().ToLower().Contains(searchFilter.ToLower()) ||
                a.AbilityID.ToLower().Contains(searchFilter.ToLower()) ||
                (a.Description != null && a.Description.ToLower().Contains(searchFilter.ToLower())));
        }

        return abilities.OrderBy(a => a.GetDisplayName()).ToList();
    }

    private HashSet<AbilityEffectType> GetAbilityEffectTypes(AbilityDefinition ability)
    {
        var types = new HashSet<AbilityEffectType>();

        if (ability.Effects != null)
        {
            foreach (var effect in ability.Effects)
            {
                if (effect != null)
                {
                    types.Add(effect.Type);
                }
            }
        }

        return types;
    }

    private Color GetEffectTypeColor(AbilityEffectType type)
    {
        return EffectTypeColors.TryGetValue(type, out var color) ? color : Color.white;
    }

    private void RemoveAbilityFromRegistry(AbilityDefinition ability)
    {
        if (ability == null) return;

        bool confirm = EditorUtility.DisplayDialog(
            "Delete Ability",
            $"Delete '{ability.GetDisplayName()}'?\n\nThis will permanently delete the asset file.",
            "Delete", "Cancel");

        if (confirm)
        {
            // Remove from registry first
            if (abilityRegistry != null && abilityRegistry.AllAbilities.Contains(ability))
            {
                abilityRegistry.AllAbilities.Remove(ability);
                EditorUtility.SetDirty(abilityRegistry);
            }

            // Get asset path and delete the file
            string assetPath = AssetDatabase.GetAssetPath(ability);
            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
                Logger.LogInfo($"Deleted ability '{ability.GetDisplayName()}' at {assetPath}", Logger.LogCategory.EditorLog);
            }

            AssetDatabase.SaveAssets();
        }
    }

    private string GenerateIDFromName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";

        return name.ToLower()
                  .Replace(" ", "_")
                  .Replace("'", "")
                  .Replace("-", "_")
                  .Replace("(", "")
                  .Replace(")", "");
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

    /// <summary>
    /// Draw a sprite correctly, handling sprite sheets/atlases
    /// </summary>
    private void DrawSprite(Rect rect, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return;

        Texture2D tex = sprite.texture;
        Rect spriteRect = sprite.textureRect;

        // Calculate UV coordinates for this sprite within the texture
        Rect texCoords = new Rect(
            spriteRect.x / tex.width,
            spriteRect.y / tex.height,
            spriteRect.width / tex.width,
            spriteRect.height / tex.height
        );

        GUI.DrawTextureWithTexCoords(rect, tex, texCoords);
    }
    #endregion
}
#endif
