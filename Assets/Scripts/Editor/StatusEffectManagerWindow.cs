// Purpose: Editor window to create and manage status effects
// Filepath: Assets/Scripts/Editor/StatusEffectManagerWindow.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class StatusEffectManagerWindow : EditorWindow
{
    [MenuItem("WalkAndRPG/Combat/Status Effect Manager")]
    public static void ShowWindow()
    {
        StatusEffectManagerWindow window = GetWindow<StatusEffectManagerWindow>();
        window.titleContent = new GUIContent("Status Effect Manager");
        window.minSize = new Vector2(500, 400);
        window.Show();
    }

    // Data
    private StatusEffectRegistry statusEffectRegistry;

    // UI State
    private Vector2 scrollPosition;
    private string searchFilter = "";
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Status Effects", "Quick Create", "Validation" };

    // Filter State
    private bool showAllTypes = true;
    private StatusEffectType filterType = StatusEffectType.Poison;
    private bool showAllBehaviors = true;
    private EffectBehavior filterBehavior = EffectBehavior.DamageOverTime;

    // Creation Dialog State
    private bool showCreateEffectDialog = false;
    private Vector2 dialogScrollPosition;

    // New status effect fields
    private string newEffectName = "";
    private string newEffectDescription = "";
    private Sprite newEffectIcon = null;
    private Color newEffectColor = Color.white;
    private StatusEffectType newEffectType = StatusEffectType.Poison;
    private EffectBehavior newEffectBehavior = EffectBehavior.DamageOverTime;
    private StackingBehavior newEffectStacking = StackingBehavior.Stacking;
    private int newEffectMaxStacks = 10;
    private DecayBehavior newEffectDecay = DecayBehavior.Time;
    private float newEffectDuration = 10f;
    private float newEffectTickInterval = 1f;
    private float newEffectBaseValue = 1f;
    private bool newEffectScalesWithStacks = true;
    private bool newEffectPreventsActions = false;
    private bool newEffectRemovedOnDamage = false;

    // Colors for status effect types
    private static readonly Dictionary<StatusEffectType, Color> StatusEffectTypeColors = new Dictionary<StatusEffectType, Color>
    {
        // DoT - Reds
        { StatusEffectType.Poison, new Color(0.6f, 0.2f, 0.8f) },    // Purple
        { StatusEffectType.Burn, new Color(1f, 0.4f, 0.2f) },        // Orange-red
        { StatusEffectType.Bleed, new Color(0.8f, 0.1f, 0.1f) },     // Dark red

        // HoT - Green
        { StatusEffectType.Regeneration, new Color(0.2f, 0.9f, 0.3f) }, // Green

        // Control - Yellow
        { StatusEffectType.Stun, new Color(1f, 0.9f, 0.2f) },        // Yellow

        // Shield - Yellow
        { StatusEffectType.Shield, new Color(1f, 0.85f, 0.3f) },     // Golden yellow

        // Buffs - Blues
        { StatusEffectType.AttackBuff, new Color(0.3f, 0.6f, 1f) },  // Light blue
        { StatusEffectType.DefenseBuff, new Color(0.2f, 0.4f, 0.9f) }, // Blue
        { StatusEffectType.SpeedBuff, new Color(0.4f, 0.8f, 1f) },   // Cyan

        // Debuffs - Oranges
        { StatusEffectType.AttackDebuff, new Color(1f, 0.5f, 0.2f) },  // Orange
        { StatusEffectType.DefenseDebuff, new Color(0.9f, 0.4f, 0.1f) }, // Dark orange
        { StatusEffectType.SpeedDebuff, new Color(0.8f, 0.5f, 0.3f) }  // Brown-orange
    };

    void OnEnable()
    {
        LoadRegistry();
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
                DrawStatusEffectsTab();
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
        if (showCreateEffectDialog)
            DrawCreateEffectDialog();
    }

    #region Header
    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");

        GUILayout.Label("Status Effect Manager", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        // Registry selection
        statusEffectRegistry = (StatusEffectRegistry)EditorGUILayout.ObjectField("Status Effect Registry", statusEffectRegistry, typeof(StatusEffectRegistry), false);

        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            LoadRegistry();
        }

        if (GUILayout.Button("Validate", GUILayout.Width(60)))
        {
            ValidateRegistry();
        }

        EditorGUILayout.EndHorizontal();

        // Search and filters (only for Status Effects tab)
        if (selectedTab == 0)
        {
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            showAllTypes = EditorGUILayout.Toggle("All Types", showAllTypes, GUILayout.Width(80));

            if (!showAllTypes)
            {
                filterType = (StatusEffectType)EditorGUILayout.EnumPopup(filterType, GUILayout.Width(100));
            }

            showAllBehaviors = EditorGUILayout.Toggle("All Behaviors", showAllBehaviors, GUILayout.Width(100));

            if (!showAllBehaviors)
            {
                filterBehavior = (EffectBehavior)EditorGUILayout.EnumPopup(filterBehavior, GUILayout.Width(100));
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }
    #endregion

    #region Status Effects Tab
    private void DrawStatusEffectsTab()
    {
        if (statusEffectRegistry == null)
        {
            EditorGUILayout.HelpBox("Select a StatusEffectRegistry to manage status effects.", MessageType.Info);

            if (GUILayout.Button("Create New Status Effect Registry"))
            {
                CreateStatusEffectRegistry();
            }
            return;
        }

        // Create New Effect button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create New Status Effect", GUILayout.Width(180)))
        {
            showCreateEffectDialog = true;
            ResetCreateEffectDialog();
        }

        if (GUILayout.Button("Auto-Populate", GUILayout.Width(100)))
        {
            statusEffectRegistry.AutoPopulateRegistry();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        var filteredEffects = GetFilteredEffects();

        if (filteredEffects.Count == 0)
        {
            EditorGUILayout.HelpBox("No status effects found matching current filters.", MessageType.Info);
            return;
        }

        // Stats summary
        EditorGUILayout.LabelField($"Status Effects Found: {filteredEffects.Count}", EditorStyles.boldLabel);
        DrawTypesSummary(filteredEffects);
        EditorGUILayout.Space();

        // Draw effects
        foreach (var effect in filteredEffects)
        {
            DrawStatusEffectEntry(effect);
        }
    }

    private void DrawTypesSummary(List<StatusEffectDefinition> effects)
    {
        var typeCounts = effects.GroupBy(e => e.EffectType).ToDictionary(g => g.Key, g => g.Count());

        EditorGUILayout.BeginHorizontal();
        foreach (var kvp in typeCounts)
        {
            var oldColor = GUI.color;
            GUI.color = GetStatusEffectTypeColor(kvp.Key);
            EditorGUILayout.LabelField($"{kvp.Key}: {kvp.Value}", EditorStyles.miniLabel, GUILayout.Width(100));
            GUI.color = oldColor;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawStatusEffectEntry(StatusEffectDefinition effect)
    {
        EditorGUILayout.BeginVertical("box");

        // Header row
        EditorGUILayout.BeginHorizontal();

        // Icon preview
        if (effect.EffectIcon != null)
        {
            Rect iconRect = EditorGUILayout.GetControlRect(GUILayout.Width(32), GUILayout.Height(32));
            DrawSprite(iconRect, effect.EffectIcon);
        }
        else
        {
            EditorGUILayout.LabelField("[No Icon]", GUILayout.Width(50), GUILayout.Height(32));
        }

        EditorGUILayout.BeginVertical();

        // Name and type badge
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(effect.GetDisplayName(), EditorStyles.boldLabel, GUILayout.Width(120));

        // Type badge
        var oldColor = GUI.color;
        GUI.color = GetStatusEffectTypeColor(effect.EffectType);
        EditorGUILayout.LabelField($"[{effect.EffectType}]", EditorStyles.miniLabel, GUILayout.Width(90));
        GUI.color = oldColor;

        // Behavior badge
        GUI.color = GetBehaviorColor(effect.Behavior);
        EditorGUILayout.LabelField($"[{effect.Behavior}]", EditorStyles.miniLabel, GUILayout.Width(90));
        GUI.color = oldColor;

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Edit", GUILayout.Width(40)))
        {
            Selection.activeObject = effect;
            EditorGUIUtility.PingObject(effect);
        }

        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            RemoveEffectFromRegistry(effect);
        }

        EditorGUILayout.EndHorizontal();

        // Stats row
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"ID: {effect.EffectID}", EditorStyles.miniLabel, GUILayout.Width(100));

        // Stacking info
        string stackInfo = effect.Stacking == StackingBehavior.Stacking
            ? $"Stack: {(effect.MaxStacks == 0 ? "Unlimited" : effect.MaxStacks.ToString())}"
            : "No Stacking";
        EditorGUILayout.LabelField(stackInfo, EditorStyles.miniLabel, GUILayout.Width(80));

        // Duration
        if (effect.Duration > 0)
        {
            EditorGUILayout.LabelField($"Dur: {effect.Duration}s", EditorStyles.miniLabel, GUILayout.Width(60));
        }

        // Tick interval
        if (effect.TickInterval > 0)
        {
            EditorGUILayout.LabelField($"Tick: {effect.TickInterval}s", EditorStyles.miniLabel, GUILayout.Width(60));
        }

        // Base value
        EditorGUILayout.LabelField($"Val: {effect.BaseValue}", EditorStyles.miniLabel, GUILayout.Width(50));

        // Flags
        if (effect.PreventsActions)
        {
            GUI.color = Color.yellow;
            EditorGUILayout.LabelField("[STUN]", EditorStyles.miniLabel, GUILayout.Width(45));
            GUI.color = oldColor;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        // Effect summary
        EditorGUILayout.LabelField($"Effect: {effect.GetEffectSummary()}", EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private Color GetBehaviorColor(EffectBehavior behavior)
    {
        return behavior switch
        {
            EffectBehavior.DamageOverTime => new Color(1f, 0.4f, 0.4f),
            EffectBehavior.HealOverTime => new Color(0.4f, 1f, 0.4f),
            EffectBehavior.StatModifier => new Color(0.4f, 0.7f, 1f),
            EffectBehavior.ControlEffect => new Color(1f, 0.9f, 0.3f),
            _ => Color.white
        };
    }
    #endregion

    #region Quick Create Tab
    private void DrawQuickCreateTab()
    {
        if (statusEffectRegistry == null)
        {
            EditorGUILayout.HelpBox("Select a StatusEffectRegistry first.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Quick Create Templates", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Click a button to create a pre-configured status effect with sensible defaults.", MessageType.Info);
        EditorGUILayout.Space();

        // Damage Over Time
        EditorGUILayout.LabelField("Damage Over Time", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Poison\n(1/stack, 10s, stacking)", GUILayout.Height(40)))
        {
            QuickCreateDoT("Poison", StatusEffectType.Poison, 1f, 10f, 1f, true, 10, new Color(0.6f, 0.2f, 0.8f));
        }
        if (GUILayout.Button("Burn\n(2 flat, 5s, no stack)", GUILayout.Height(40)))
        {
            QuickCreateDoT("Burn", StatusEffectType.Burn, 2f, 5f, 1f, false, 1, new Color(1f, 0.4f, 0.2f));
        }
        if (GUILayout.Button("Bleed\n(1.5/stack, 8s)", GUILayout.Height(40)))
        {
            QuickCreateDoT("Bleed", StatusEffectType.Bleed, 1.5f, 8f, 1f, true, 5, new Color(0.8f, 0.1f, 0.1f));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Heal Over Time
        EditorGUILayout.LabelField("Heal Over Time", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Regeneration\n(2/stack, 8s)", GUILayout.Height(40)))
        {
            QuickCreateHoT("Regeneration", 2f, 8f, 1f, true, 3, new Color(0.2f, 0.9f, 0.3f));
        }
        if (GUILayout.Button("Minor Regen\n(1 flat, 5s)", GUILayout.Height(40)))
        {
            QuickCreateHoT("Minor Regeneration", 1f, 5f, 1f, false, 1, new Color(0.3f, 0.8f, 0.4f));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Control Effects
        EditorGUILayout.LabelField("Control Effects", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Stun\n(2s, no stack)", GUILayout.Height(40)))
        {
            QuickCreateControl("Stun", StatusEffectType.Stun, 2f, true, new Color(1f, 0.9f, 0.2f));
        }
        if (GUILayout.Button("Long Stun\n(4s, no stack)", GUILayout.Height(40)))
        {
            QuickCreateControl("Long Stun", StatusEffectType.Stun, 4f, true, new Color(1f, 0.8f, 0.1f));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Buffs
        EditorGUILayout.LabelField("Buffs", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Attack Up\n(+25%, 10s)", GUILayout.Height(40)))
        {
            QuickCreateStatModifier("Attack Up", StatusEffectType.AttackBuff, 0.25f, 10f, new Color(0.3f, 0.6f, 1f));
        }
        if (GUILayout.Button("Defense Up\n(+25%, 10s)", GUILayout.Height(40)))
        {
            QuickCreateStatModifier("Defense Up", StatusEffectType.DefenseBuff, 0.25f, 10f, new Color(0.2f, 0.4f, 0.9f));
        }
        if (GUILayout.Button("Speed Up\n(+30%, 8s)", GUILayout.Height(40)))
        {
            QuickCreateStatModifier("Speed Up", StatusEffectType.SpeedBuff, 0.30f, 8f, new Color(0.4f, 0.8f, 1f));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Debuffs
        EditorGUILayout.LabelField("Debuffs", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Attack Down\n(-20%, 8s)", GUILayout.Height(40)))
        {
            QuickCreateStatModifier("Attack Down", StatusEffectType.AttackDebuff, -0.20f, 8f, new Color(1f, 0.5f, 0.2f));
        }
        if (GUILayout.Button("Defense Down\n(-20%, 8s)", GUILayout.Height(40)))
        {
            QuickCreateStatModifier("Defense Down", StatusEffectType.DefenseDebuff, -0.20f, 8f, new Color(0.9f, 0.4f, 0.1f));
        }
        if (GUILayout.Button("Speed Down\n(-25%, 6s)", GUILayout.Height(40)))
        {
            QuickCreateStatModifier("Speed Down", StatusEffectType.SpeedDebuff, -0.25f, 6f, new Color(0.8f, 0.5f, 0.3f));
        }
        EditorGUILayout.EndHorizontal();
    }

    private void QuickCreateDoT(string name, StatusEffectType type, float baseValue, float duration, float tickInterval, bool scalesWithStacks, int maxStacks, Color color)
    {
        var effect = CreateInstance<StatusEffectDefinition>();
        effect.EffectName = name;
        effect.EffectID = GenerateIDFromName(name);
        effect.EffectType = type;
        effect.Behavior = EffectBehavior.DamageOverTime;
        effect.Stacking = scalesWithStacks ? StackingBehavior.Stacking : StackingBehavior.NoStacking;
        effect.MaxStacks = maxStacks;
        effect.Decay = DecayBehavior.Time;
        effect.Duration = duration;
        effect.TickInterval = tickInterval;
        effect.BaseValue = baseValue;
        effect.ScalesWithStacks = scalesWithStacks;
        effect.EffectColor = color;

        SaveStatusEffectAsset(effect);
    }

    private void QuickCreateHoT(string name, float baseValue, float duration, float tickInterval, bool scalesWithStacks, int maxStacks, Color color)
    {
        var effect = CreateInstance<StatusEffectDefinition>();
        effect.EffectName = name;
        effect.EffectID = GenerateIDFromName(name);
        effect.EffectType = StatusEffectType.Regeneration;
        effect.Behavior = EffectBehavior.HealOverTime;
        effect.Stacking = scalesWithStacks ? StackingBehavior.Stacking : StackingBehavior.NoStacking;
        effect.MaxStacks = maxStacks;
        effect.Decay = DecayBehavior.Time;
        effect.Duration = duration;
        effect.TickInterval = tickInterval;
        effect.BaseValue = baseValue;
        effect.ScalesWithStacks = scalesWithStacks;
        effect.EffectColor = color;

        SaveStatusEffectAsset(effect);
    }

    private void QuickCreateControl(string name, StatusEffectType type, float duration, bool preventsActions, Color color)
    {
        var effect = CreateInstance<StatusEffectDefinition>();
        effect.EffectName = name;
        effect.EffectID = GenerateIDFromName(name);
        effect.EffectType = type;
        effect.Behavior = EffectBehavior.ControlEffect;
        effect.Stacking = StackingBehavior.NoStacking;
        effect.MaxStacks = 1;
        effect.Decay = DecayBehavior.Time;
        effect.Duration = duration;
        effect.TickInterval = 0f;
        effect.BaseValue = 0f;
        effect.ScalesWithStacks = false;
        effect.PreventsActions = preventsActions;
        effect.EffectColor = color;

        SaveStatusEffectAsset(effect);
    }

    private void QuickCreateStatModifier(string name, StatusEffectType type, float modifier, float duration, Color color)
    {
        var effect = CreateInstance<StatusEffectDefinition>();
        effect.EffectName = name;
        effect.EffectID = GenerateIDFromName(name);
        effect.EffectType = type;
        effect.Behavior = EffectBehavior.StatModifier;
        effect.Stacking = StackingBehavior.NoStacking;
        effect.MaxStacks = 1;
        effect.Decay = DecayBehavior.Time;
        effect.Duration = duration;
        effect.TickInterval = 0f;
        effect.BaseValue = modifier;
        effect.ScalesWithStacks = false;
        effect.EffectColor = color;

        SaveStatusEffectAsset(effect);
    }
    #endregion

    #region Validation Tab
    private void DrawValidationTab()
    {
        if (statusEffectRegistry == null)
        {
            EditorGUILayout.HelpBox("Select a StatusEffectRegistry first.", MessageType.Warning);
            return;
        }

        EditorGUILayout.LabelField("Registry Validation", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Validation status
        EditorGUILayout.LabelField("Validation Status:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(statusEffectRegistry.ValidationStatus, MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("Run Validation", GUILayout.Height(30)))
        {
            statusEffectRegistry.ValidateRegistry();
        }

        EditorGUILayout.Space();

        // Statistics
        EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);

        var validEffects = statusEffectRegistry.GetAllValidEffects();
        EditorGUILayout.LabelField($"Total Status Effects: {validEffects.Count}");

        EditorGUILayout.Space();

        // By Type
        EditorGUILayout.LabelField("By Type:", EditorStyles.boldLabel);
        var typeCounts = validEffects.GroupBy(e => e.EffectType).OrderByDescending(g => g.Count());
        foreach (var group in typeCounts)
        {
            var oldColor = GUI.color;
            GUI.color = GetStatusEffectTypeColor(group.Key);
            EditorGUILayout.LabelField($"  {group.Key}: {group.Count()}");
            GUI.color = oldColor;
        }

        EditorGUILayout.Space();

        // By Behavior
        EditorGUILayout.LabelField("By Behavior:", EditorStyles.boldLabel);
        var behaviorCounts = validEffects.GroupBy(e => e.Behavior).OrderByDescending(g => g.Count());
        foreach (var group in behaviorCounts)
        {
            var oldColor = GUI.color;
            GUI.color = GetBehaviorColor(group.Key);
            EditorGUILayout.LabelField($"  {group.Key}: {group.Count()}");
            GUI.color = oldColor;
        }

        EditorGUILayout.Space();

        // Categorization
        EditorGUILayout.LabelField("Categories:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"  Buffs: {validEffects.Count(e => e.IsBuff)}");
        EditorGUILayout.LabelField($"  Debuffs: {validEffects.Count(e => e.IsDebuff)}");
        EditorGUILayout.LabelField($"  DoT Effects: {validEffects.Count(e => e.IsDamageOverTime)}");
        EditorGUILayout.LabelField($"  HoT Effects: {validEffects.Count(e => e.IsHealOverTime)}");
        EditorGUILayout.LabelField($"  Control Effects: {validEffects.Count(e => e.IsControlEffect)}");
    }
    #endregion

    #region Create Effect Dialog
    private void DrawCreateEffectDialog()
    {
        // Create semi-transparent overlay
        var overlayRect = new Rect(0, 0, position.width, position.height);
        EditorGUI.DrawRect(overlayRect, new Color(0, 0, 0, 0.5f));

        // Center the dialog
        float dialogWidth = 500;
        float dialogHeight = 600;
        var dialogRect = new Rect(
            (position.width - dialogWidth) / 2,
            (position.height - dialogHeight) / 2,
            dialogWidth, dialogHeight);

        GUILayout.BeginArea(dialogRect);
        EditorGUILayout.BeginVertical(GUI.skin.window);

        EditorGUILayout.LabelField("Create New Status Effect", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        dialogScrollPosition = EditorGUILayout.BeginScrollView(dialogScrollPosition, GUILayout.Height(dialogHeight - 80));

        // Basic info
        EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
        newEffectName = EditorGUILayout.TextField("Name", newEffectName);
        newEffectDescription = EditorGUILayout.TextField("Description", newEffectDescription);
        newEffectIcon = (Sprite)EditorGUILayout.ObjectField("Icon", newEffectIcon, typeof(Sprite), false);
        newEffectColor = EditorGUILayout.ColorField("Color", newEffectColor);

        EditorGUILayout.Space();

        // Effect Configuration
        EditorGUILayout.LabelField("Effect Configuration", EditorStyles.boldLabel);

        var oldType = newEffectType;
        newEffectType = (StatusEffectType)EditorGUILayout.EnumPopup("Effect Type", newEffectType);

        // Auto-set behavior based on type
        if (oldType != newEffectType)
        {
            AutoSetBehaviorAndColor();
        }

        newEffectBehavior = (EffectBehavior)EditorGUILayout.EnumPopup("Behavior", newEffectBehavior);

        EditorGUILayout.Space();

        // Stacking Configuration
        EditorGUILayout.LabelField("Stacking Configuration", EditorStyles.boldLabel);
        newEffectStacking = (StackingBehavior)EditorGUILayout.EnumPopup("Stacking", newEffectStacking);

        if (newEffectStacking == StackingBehavior.Stacking)
        {
            newEffectMaxStacks = EditorGUILayout.IntField("Max Stacks (0=unlimited)", Mathf.Max(0, newEffectMaxStacks));
        }
        else
        {
            newEffectMaxStacks = 1;
            EditorGUILayout.LabelField("Max Stacks: 1 (No Stacking)", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();

        // Decay Configuration
        EditorGUILayout.LabelField("Decay Configuration", EditorStyles.boldLabel);
        newEffectDecay = (DecayBehavior)EditorGUILayout.EnumPopup("Decay", newEffectDecay);

        if (newEffectDecay == DecayBehavior.Time)
        {
            newEffectDuration = EditorGUILayout.Slider("Duration (s)", newEffectDuration, 0.5f, 60f);
        }

        EditorGUILayout.Space();

        // Tick Configuration (for DoT/HoT)
        if (newEffectBehavior == EffectBehavior.DamageOverTime || newEffectBehavior == EffectBehavior.HealOverTime)
        {
            EditorGUILayout.LabelField("Tick Configuration", EditorStyles.boldLabel);
            newEffectTickInterval = EditorGUILayout.Slider("Tick Interval (s)", newEffectTickInterval, 0.1f, 5f);
            newEffectBaseValue = EditorGUILayout.FloatField("Base Value (per tick)", newEffectBaseValue);
            newEffectScalesWithStacks = EditorGUILayout.Toggle("Scales With Stacks", newEffectScalesWithStacks);
        }
        else if (newEffectBehavior == EffectBehavior.StatModifier)
        {
            EditorGUILayout.LabelField("Modifier Configuration", EditorStyles.boldLabel);
            newEffectBaseValue = EditorGUILayout.Slider("Modifier %", newEffectBaseValue, -0.5f, 0.5f);
            EditorGUILayout.LabelField($"Result: {(newEffectBaseValue >= 0 ? "+" : "")}{(newEffectBaseValue * 100):F0}%", EditorStyles.miniLabel);
            newEffectTickInterval = 0f;
            newEffectScalesWithStacks = false;
        }
        else if (newEffectBehavior == EffectBehavior.ControlEffect)
        {
            EditorGUILayout.LabelField("Control Configuration", EditorStyles.boldLabel);
            newEffectPreventsActions = EditorGUILayout.Toggle("Prevents Actions (Stun)", newEffectPreventsActions);
            newEffectRemovedOnDamage = EditorGUILayout.Toggle("Removed On Damage", newEffectRemovedOnDamage);
            newEffectTickInterval = 0f;
            newEffectBaseValue = 0f;
            newEffectScalesWithStacks = false;
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Buttons
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create"))
        {
            if (!string.IsNullOrEmpty(newEffectName))
            {
                CreateNewStatusEffect();
                showCreateEffectDialog = false;
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Input", "Status effect must have a name.", "OK");
            }
        }

        if (GUILayout.Button("Cancel"))
        {
            showCreateEffectDialog = false;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();

        // Handle escape key
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
            showCreateEffectDialog = false;
            Event.current.Use();
        }
    }

    private void AutoSetBehaviorAndColor()
    {
        // Auto-set behavior based on type
        switch (newEffectType)
        {
            case StatusEffectType.Poison:
            case StatusEffectType.Burn:
            case StatusEffectType.Bleed:
                newEffectBehavior = EffectBehavior.DamageOverTime;
                break;

            case StatusEffectType.Regeneration:
                newEffectBehavior = EffectBehavior.HealOverTime;
                break;

            case StatusEffectType.Stun:
                newEffectBehavior = EffectBehavior.ControlEffect;
                newEffectPreventsActions = true;
                break;

            case StatusEffectType.AttackBuff:
            case StatusEffectType.DefenseBuff:
            case StatusEffectType.SpeedBuff:
            case StatusEffectType.AttackDebuff:
            case StatusEffectType.DefenseDebuff:
            case StatusEffectType.SpeedDebuff:
                newEffectBehavior = EffectBehavior.StatModifier;
                break;
        }

        // Auto-set color
        newEffectColor = GetStatusEffectTypeColor(newEffectType);
    }

    private void CreateNewStatusEffect()
    {
        var effect = CreateInstance<StatusEffectDefinition>();
        effect.EffectName = newEffectName;
        effect.EffectID = GenerateIDFromName(newEffectName);
        effect.Description = newEffectDescription;
        effect.EffectIcon = newEffectIcon;
        effect.EffectColor = newEffectColor;
        effect.EffectType = newEffectType;
        effect.Behavior = newEffectBehavior;
        effect.Stacking = newEffectStacking;
        effect.MaxStacks = newEffectMaxStacks;
        effect.Decay = newEffectDecay;
        effect.Duration = newEffectDuration;
        effect.TickInterval = newEffectTickInterval;
        effect.BaseValue = newEffectBaseValue;
        effect.ScalesWithStacks = newEffectScalesWithStacks;
        effect.PreventsActions = newEffectPreventsActions;
        effect.RemovedOnDamage = newEffectRemovedOnDamage;

        SaveStatusEffectAsset(effect);
        ResetCreateEffectDialog();
    }

    private void ResetCreateEffectDialog()
    {
        newEffectName = "";
        newEffectDescription = "";
        newEffectIcon = null;
        newEffectColor = Color.white;
        newEffectType = StatusEffectType.Poison;
        newEffectBehavior = EffectBehavior.DamageOverTime;
        newEffectStacking = StackingBehavior.Stacking;
        newEffectMaxStacks = 10;
        newEffectDecay = DecayBehavior.Time;
        newEffectDuration = 10f;
        newEffectTickInterval = 1f;
        newEffectBaseValue = 1f;
        newEffectScalesWithStacks = true;
        newEffectPreventsActions = false;
        newEffectRemovedOnDamage = false;
    }
    #endregion

    #region Utility Methods
    private void LoadRegistry()
    {
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
        if (statusEffectRegistry != null)
        {
            statusEffectRegistry.ValidateRegistry();
            Logger.LogInfo("StatusEffectRegistry validation triggered", Logger.LogCategory.EditorLog);
        }
    }

    private void CreateStatusEffectRegistry()
    {
        var registry = CreateInstance<StatusEffectRegistry>();

        string folder = "Assets/ScriptableObjects";
        EnsureFolderExists(folder);

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/StatusEffectRegistry.asset");
        AssetDatabase.CreateAsset(registry, assetPath);
        AssetDatabase.SaveAssets();

        statusEffectRegistry = registry;
        Selection.activeObject = registry;
        EditorGUIUtility.PingObject(registry);

        Logger.LogInfo($"Created StatusEffectRegistry at {assetPath}", Logger.LogCategory.EditorLog);
    }

    private void SaveStatusEffectAsset(StatusEffectDefinition effect)
    {
        string folder = "Assets/ScriptableObjects/StatusEffects";
        EnsureFolderExists(folder);

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{effect.EffectName}.asset");
        AssetDatabase.CreateAsset(effect, assetPath);
        AssetDatabase.SaveAssets();

        // Add to registry
        if (statusEffectRegistry != null)
        {
            statusEffectRegistry.AllEffects.Add(effect);
            EditorUtility.SetDirty(statusEffectRegistry);
            AssetDatabase.SaveAssets();
        }

        Selection.activeObject = effect;
        EditorGUIUtility.PingObject(effect);

        Logger.LogInfo($"Created status effect: {effect.EffectName} at {assetPath}", Logger.LogCategory.EditorLog);
    }

    private List<StatusEffectDefinition> GetFilteredEffects()
    {
        if (statusEffectRegistry == null) return new List<StatusEffectDefinition>();

        var effects = statusEffectRegistry.AllEffects.Where(e => e != null);

        // Filter by type
        if (!showAllTypes)
        {
            effects = effects.Where(e => e.EffectType == filterType);
        }

        // Filter by behavior
        if (!showAllBehaviors)
        {
            effects = effects.Where(e => e.Behavior == filterBehavior);
        }

        // Filter by search
        if (!string.IsNullOrEmpty(searchFilter))
        {
            effects = effects.Where(e =>
                e.GetDisplayName().ToLower().Contains(searchFilter.ToLower()) ||
                e.EffectID.ToLower().Contains(searchFilter.ToLower()) ||
                (e.Description != null && e.Description.ToLower().Contains(searchFilter.ToLower())));
        }

        return effects.OrderBy(e => e.EffectType).ThenBy(e => e.GetDisplayName()).ToList();
    }

    private Color GetStatusEffectTypeColor(StatusEffectType type)
    {
        return StatusEffectTypeColors.TryGetValue(type, out var color) ? color : Color.white;
    }

    private void RemoveEffectFromRegistry(StatusEffectDefinition effect)
    {
        if (effect == null) return;

        bool confirm = EditorUtility.DisplayDialog(
            "Delete Status Effect",
            $"Delete '{effect.GetDisplayName()}'?\n\nThis will permanently delete the asset file.",
            "Delete", "Cancel");

        if (confirm)
        {
            // Remove from registry first
            if (statusEffectRegistry != null && statusEffectRegistry.AllEffects.Contains(effect))
            {
                statusEffectRegistry.AllEffects.Remove(effect);
                EditorUtility.SetDirty(statusEffectRegistry);
            }

            // Get asset path and delete the file
            string assetPath = AssetDatabase.GetAssetPath(effect);
            if (!string.IsNullOrEmpty(assetPath))
            {
                AssetDatabase.DeleteAsset(assetPath);
                Logger.LogInfo($"Deleted status effect '{effect.GetDisplayName()}' at {assetPath}", Logger.LogCategory.EditorLog);
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
