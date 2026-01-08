// Purpose: Unified dashboard to validate all game registries and cross-references
// Filepath: Assets/Scripts/Editor/RegistryValidationDashboard.cs

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class RegistryValidationDashboard : EditorWindow
{
    [MenuItem("StepQuest/Validation/Registry Dashboard")]
    public static void ShowWindow()
    {
        var window = GetWindow<RegistryValidationDashboard>();
        window.titleContent = new GUIContent("Registry Dashboard");
        window.minSize = new Vector2(600, 500);
        window.Show();
    }

    // Registries
    private ItemRegistry itemRegistry;
    private ActivityRegistry activityRegistry;
    private AbilityRegistry abilityRegistry;
    private StatusEffectRegistry statusEffectRegistry;

    // Counts
    private int itemCount;
    private int activityCount;
    private int abilityCount;
    private int statusEffectCount;
    private int enemyCount;
    private int locationCount;

    // Validation results
    private List<ValidationIssue> issues = new List<ValidationIssue>();
    private bool hasRunValidation = false;

    // UI State
    private Vector2 scrollPosition;
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Overview", "Cross-References", "Issues", "Balance Warnings" };

    // Balance warnings
    private List<BalanceWarning> balanceWarnings = new List<BalanceWarning>();
    private bool hasRunBalanceCheck = false;

    private struct BalanceWarning
    {
        public string category;
        public string message;
        public Object asset;
        public string fixAction; // Description of auto-fix if available
    }

    // Issue filters
    private bool showErrors = true;
    private bool showWarnings = true;
    private bool showInfo = true;

    private struct ValidationIssue
    {
        public string category;
        public string message;
        public MessageType severity;
        public Object asset;
    }

    void OnEnable()
    {
        LoadRegistries();
        RefreshCounts();
    }

    void OnGUI()
    {
        DrawHeader();

        EditorGUILayout.Space();

        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        switch (selectedTab)
        {
            case 0:
                DrawOverviewTab();
                break;
            case 1:
                DrawCrossReferencesTab();
                break;
            case 2:
                DrawIssuesTab();
                break;
            case 3:
                DrawBalanceWarningsTab();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    #region Header
    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Registry Validation Dashboard", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Refresh", GUILayout.Width(70)))
        {
            LoadRegistries();
            RefreshCounts();
        }

        if (GUILayout.Button("Run Full Validation", GUILayout.Width(130)))
        {
            RunFullValidation();
        }

        EditorGUILayout.EndHorizontal();

        // Quick stats row
        EditorGUILayout.BeginHorizontal();
        DrawStatBadge("Items", itemCount, new Color(0.3f, 0.7f, 0.9f));
        DrawStatBadge("Activities", activityCount, new Color(0.3f, 0.9f, 0.5f));
        DrawStatBadge("Abilities", abilityCount, new Color(0.9f, 0.5f, 0.3f));
        DrawStatBadge("Effects", statusEffectCount, new Color(0.7f, 0.3f, 0.9f));
        DrawStatBadge("Enemies", enemyCount, new Color(0.9f, 0.3f, 0.3f));
        DrawStatBadge("Locations", locationCount, new Color(0.9f, 0.9f, 0.3f));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawStatBadge(string label, int count, Color color)
    {
        var oldColor = GUI.backgroundColor;
        GUI.backgroundColor = color;
        EditorGUILayout.BeginVertical("box", GUILayout.Width(80));
        EditorGUILayout.LabelField(count.ToString(), EditorStyles.boldLabel);
        EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
        GUI.backgroundColor = oldColor;
    }
    #endregion

    #region Overview Tab
    private void DrawOverviewTab()
    {
        EditorGUILayout.LabelField("Registry Status", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Item Registry
        DrawRegistryStatus("Item Registry", itemRegistry != null, itemCount,
            itemRegistry != null ? itemRegistry.ValidationStatus : "Not loaded");

        // Activity Registry
        DrawRegistryStatus("Activity Registry", activityRegistry != null, activityCount,
            activityRegistry != null ? "Loaded" : "Not loaded");

        // Ability Registry
        DrawRegistryStatus("Ability Registry", abilityRegistry != null, abilityCount,
            abilityRegistry != null ? abilityRegistry.ValidationStatus : "Not loaded");

        // Status Effect Registry
        DrawRegistryStatus("Status Effect Registry", statusEffectRegistry != null, statusEffectCount,
            statusEffectRegistry != null ? statusEffectRegistry.ValidationStatus : "Not loaded");

        EditorGUILayout.Space();

        // Orphaned Assets section
        DrawOrphanedAssetsSection();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Content Summary", EditorStyles.boldLabel);

        // Content breakdown
        EditorGUILayout.BeginVertical("box");

        if (itemRegistry != null)
        {
            var itemsByRarity = CountItemsByRarity();
            EditorGUILayout.LabelField("Items by Rarity:");
            foreach (var kvp in itemsByRarity.OrderByDescending(x => x.Value))
            {
                EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value}");
            }
        }

        EditorGUILayout.Space();

        if (abilityRegistry != null)
        {
            var abilitiesByType = CountAbilitiesByType();
            EditorGUILayout.LabelField("Abilities by Effect Type:");
            foreach (var kvp in abilitiesByType.OrderByDescending(x => x.Value))
            {
                EditorGUILayout.LabelField($"  {kvp.Key}: {kvp.Value}");
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawRegistryStatus(string name, bool isLoaded, int count, string status)
    {
        EditorGUILayout.BeginHorizontal("box");

        var oldColor = GUI.color;
        GUI.color = isLoaded ? Color.green : Color.red;
        EditorGUILayout.LabelField(isLoaded ? "[OK]" : "[!]", GUILayout.Width(30));
        GUI.color = oldColor;

        EditorGUILayout.LabelField(name, EditorStyles.boldLabel, GUILayout.Width(150));
        EditorGUILayout.LabelField($"{count} items", GUILayout.Width(80));

        // Truncate status if too long
        string displayStatus = status.Length > 50 ? status.Substring(0, 47) + "..." : status;
        EditorGUILayout.LabelField(displayStatus, EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.EndHorizontal();
    }

    private void DrawOrphanedAssetsSection()
    {
        EditorGUILayout.LabelField("Orphaned Assets", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Orphaned assets exist on disk but aren't in their registry. Click 'Add' to register them.",
            MessageType.Info);

        // Find orphaned items
        var orphanedItems = FindOrphanedItems();
        var orphanedAbilities = FindOrphanedAbilities();
        var orphanedEffects = FindOrphanedStatusEffects();

        int totalOrphans = orphanedItems.Count + orphanedAbilities.Count + orphanedEffects.Count;

        if (totalOrphans == 0)
        {
            EditorGUILayout.LabelField("No orphaned assets found.", EditorStyles.miniLabel);
            return;
        }

        EditorGUILayout.BeginVertical("box");

        // Orphaned Items
        if (orphanedItems.Count > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Orphaned Items: {orphanedItems.Count}", EditorStyles.boldLabel);
            if (GUILayout.Button("Add All to Registry", GUILayout.Width(130)))
            {
                AddItemsToRegistry(orphanedItems);
            }
            EditorGUILayout.EndHorizontal();

            foreach (var item in orphanedItems.Take(5))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  • {item.GetDisplayName()}", GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Add", GUILayout.Width(40)))
                {
                    AddItemsToRegistry(new List<ItemDefinition> { item });
                }
                EditorGUILayout.EndHorizontal();
            }
            if (orphanedItems.Count > 5)
            {
                EditorGUILayout.LabelField($"  ...and {orphanedItems.Count - 5} more", EditorStyles.miniLabel);
            }
        }

        // Orphaned Abilities
        if (orphanedAbilities.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Orphaned Abilities: {orphanedAbilities.Count}", EditorStyles.boldLabel);
            if (GUILayout.Button("Add All to Registry", GUILayout.Width(130)))
            {
                AddAbilitiesToRegistry(orphanedAbilities);
            }
            EditorGUILayout.EndHorizontal();

            foreach (var ability in orphanedAbilities.Take(5))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  • {ability.GetDisplayName()}", GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Add", GUILayout.Width(40)))
                {
                    AddAbilitiesToRegistry(new List<AbilityDefinition> { ability });
                }
                EditorGUILayout.EndHorizontal();
            }
            if (orphanedAbilities.Count > 5)
            {
                EditorGUILayout.LabelField($"  ...and {orphanedAbilities.Count - 5} more", EditorStyles.miniLabel);
            }
        }

        // Orphaned Status Effects
        if (orphanedEffects.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Orphaned Status Effects: {orphanedEffects.Count}", EditorStyles.boldLabel);
            if (GUILayout.Button("Add All to Registry", GUILayout.Width(130)))
            {
                AddStatusEffectsToRegistry(orphanedEffects);
            }
            EditorGUILayout.EndHorizontal();

            foreach (var effect in orphanedEffects.Take(5))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"  • {effect.GetDisplayName()}", GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Add", GUILayout.Width(40)))
                {
                    AddStatusEffectsToRegistry(new List<StatusEffectDefinition> { effect });
                }
                EditorGUILayout.EndHorizontal();
            }
            if (orphanedEffects.Count > 5)
            {
                EditorGUILayout.LabelField($"  ...and {orphanedEffects.Count - 5} more", EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private List<ItemDefinition> FindOrphanedItems()
    {
        if (itemRegistry == null) return new List<ItemDefinition>();

        var allItems = FindAllAssets<ItemDefinition>();
        var registeredItems = new HashSet<ItemDefinition>(itemRegistry.AllItems.Where(i => i != null));

        return allItems.Where(i => !registeredItems.Contains(i)).ToList();
    }

    private List<AbilityDefinition> FindOrphanedAbilities()
    {
        if (abilityRegistry == null) return new List<AbilityDefinition>();

        var allAbilities = FindAllAssets<AbilityDefinition>();
        var registeredAbilities = new HashSet<AbilityDefinition>(abilityRegistry.AllAbilities.Where(a => a != null));

        return allAbilities.Where(a => !registeredAbilities.Contains(a)).ToList();
    }

    private List<StatusEffectDefinition> FindOrphanedStatusEffects()
    {
        if (statusEffectRegistry == null) return new List<StatusEffectDefinition>();

        var allEffects = FindAllAssets<StatusEffectDefinition>();
        var registeredEffects = new HashSet<StatusEffectDefinition>(statusEffectRegistry.AllEffects.Where(e => e != null));

        return allEffects.Where(e => !registeredEffects.Contains(e)).ToList();
    }

    private void AddItemsToRegistry(List<ItemDefinition> items)
    {
        if (itemRegistry == null || items.Count == 0) return;

        foreach (var item in items)
        {
            if (!itemRegistry.AllItems.Contains(item))
            {
                itemRegistry.AllItems.Add(item);
            }
        }

        EditorUtility.SetDirty(itemRegistry);
        AssetDatabase.SaveAssets();
        RefreshCounts();

        Logger.LogInfo($"Added {items.Count} item(s) to registry", Logger.LogCategory.EditorLog);
    }

    private void AddAbilitiesToRegistry(List<AbilityDefinition> abilities)
    {
        if (abilityRegistry == null || abilities.Count == 0) return;

        foreach (var ability in abilities)
        {
            if (!abilityRegistry.AllAbilities.Contains(ability))
            {
                abilityRegistry.AllAbilities.Add(ability);
            }
        }

        EditorUtility.SetDirty(abilityRegistry);
        AssetDatabase.SaveAssets();
        RefreshCounts();

        Logger.LogInfo($"Added {abilities.Count} ability(ies) to registry", Logger.LogCategory.EditorLog);
    }

    private void AddStatusEffectsToRegistry(List<StatusEffectDefinition> effects)
    {
        if (statusEffectRegistry == null || effects.Count == 0) return;

        foreach (var effect in effects)
        {
            if (!statusEffectRegistry.AllEffects.Contains(effect))
            {
                statusEffectRegistry.AllEffects.Add(effect);
            }
        }

        EditorUtility.SetDirty(statusEffectRegistry);
        AssetDatabase.SaveAssets();
        RefreshCounts();

        Logger.LogInfo($"Added {effects.Count} status effect(s) to registry", Logger.LogCategory.EditorLog);
    }
    #endregion

    #region Cross-References Tab
    private void DrawCrossReferencesTab()
    {
        EditorGUILayout.LabelField("Cross-Registry References", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tab shows how different registries reference each other.\n" +
            "Red items indicate broken or missing references.",
            MessageType.Info);

        EditorGUILayout.Space();

        // Abilities -> Status Effects
        DrawCrossReferenceSection("Abilities referencing Status Effects",
            GetAbilitiesWithStatusEffects());

        EditorGUILayout.Space();

        // Enemies -> Abilities
        DrawCrossReferenceSection("Enemies referencing Abilities",
            GetEnemiesWithAbilities());

        EditorGUILayout.Space();

        // Enemies -> Items (Loot)
        DrawCrossReferenceSection("Enemies referencing Items (Loot)",
            GetEnemiesWithLoot());

        EditorGUILayout.Space();

        // Activities -> Items (Products/Materials)
        DrawCrossReferenceSection("Activities referencing Items",
            GetActivitiesWithItems());
    }

    private void DrawCrossReferenceSection(string title, List<CrossReference> refs)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

        if (refs.Count == 0)
        {
            EditorGUILayout.LabelField("  No references found", EditorStyles.miniLabel);
        }
        else
        {
            int validCount = refs.Count(r => r.isValid);
            int invalidCount = refs.Count - validCount;

            var oldColor = GUI.color;
            if (invalidCount > 0)
            {
                GUI.color = Color.red;
                EditorGUILayout.LabelField($"  {invalidCount} broken reference(s)!");
            }
            GUI.color = oldColor;

            EditorGUILayout.LabelField($"  Total: {refs.Count} ({validCount} valid, {invalidCount} broken)");

            // Show first few broken ones
            foreach (var broken in refs.Where(r => !r.isValid).Take(5))
            {
                EditorGUILayout.BeginHorizontal();
                GUI.color = Color.red;
                EditorGUILayout.LabelField($"    {broken.sourceName} -> {broken.targetName}", EditorStyles.miniLabel);
                GUI.color = oldColor;

                if (broken.sourceAsset != null && GUILayout.Button("Fix", GUILayout.Width(40)))
                {
                    Selection.activeObject = broken.sourceAsset;
                    EditorGUIUtility.PingObject(broken.sourceAsset);
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndVertical();
    }

    private struct CrossReference
    {
        public string sourceName;
        public string targetName;
        public bool isValid;
        public Object sourceAsset;
    }
    #endregion

    #region Issues Tab
    private void DrawIssuesTab()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Validation Issues", EditorStyles.boldLabel);

        if (GUILayout.Button("Re-validate", GUILayout.Width(80)))
        {
            RunFullValidation();
        }
        EditorGUILayout.EndHorizontal();

        // Filters
        EditorGUILayout.BeginHorizontal();
        showErrors = EditorGUILayout.Toggle("Errors", showErrors, GUILayout.Width(70));
        showWarnings = EditorGUILayout.Toggle("Warnings", showWarnings, GUILayout.Width(80));
        showInfo = EditorGUILayout.Toggle("Info", showInfo, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (!hasRunValidation)
        {
            EditorGUILayout.HelpBox("Click 'Run Full Validation' to check for issues.", MessageType.Info);
            return;
        }

        var filteredIssues = issues.Where(i =>
            (showErrors && i.severity == MessageType.Error) ||
            (showWarnings && i.severity == MessageType.Warning) ||
            (showInfo && i.severity == MessageType.Info)
        ).ToList();

        if (filteredIssues.Count == 0)
        {
            EditorGUILayout.HelpBox("No issues found! All registries are valid.", MessageType.Info);
            return;
        }

        // Group by category
        var grouped = filteredIssues.GroupBy(i => i.category);

        foreach (var group in grouped)
        {
            EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);

            foreach (var issue in group)
            {
                EditorGUILayout.BeginHorizontal("box");

                var oldColor = GUI.color;
                switch (issue.severity)
                {
                    case MessageType.Error:
                        GUI.color = new Color(1f, 0.4f, 0.4f);
                        EditorGUILayout.LabelField("[ERR]", GUILayout.Width(40));
                        break;
                    case MessageType.Warning:
                        GUI.color = new Color(1f, 0.8f, 0.3f);
                        EditorGUILayout.LabelField("[WRN]", GUILayout.Width(40));
                        break;
                    default:
                        GUI.color = new Color(0.6f, 0.8f, 1f);
                        EditorGUILayout.LabelField("[INF]", GUILayout.Width(40));
                        break;
                }
                GUI.color = oldColor;

                EditorGUILayout.LabelField(issue.message, EditorStyles.wordWrappedMiniLabel);

                if (issue.asset != null && GUILayout.Button("Go", GUILayout.Width(30)))
                {
                    Selection.activeObject = issue.asset;
                    EditorGUIUtility.PingObject(issue.asset);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
        }
    }
    #endregion

    #region Balance Warnings Tab
    private void DrawBalanceWarningsTab()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Balance Warnings", EditorStyles.boldLabel);

        if (GUILayout.Button("Check Balance", GUILayout.Width(100)))
        {
            CheckBalanceWarnings();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "Balance warnings identify content that may be incomplete or unbalanced:\n" +
            "- Enemies with no abilities\n" +
            "- Items with 0 sell value\n" +
            "- Activities with 0 XP rewards\n" +
            "- Locations with no content\n" +
            "- Abilities with no effects",
            MessageType.Info);

        EditorGUILayout.Space();

        if (!hasRunBalanceCheck)
        {
            EditorGUILayout.HelpBox("Click 'Check Balance' to scan for balance issues.", MessageType.Info);
            return;
        }

        if (balanceWarnings.Count == 0)
        {
            EditorGUILayout.HelpBox("No balance issues found! All content looks good.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"Found {balanceWarnings.Count} balance warning(s)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Group by category
        var grouped = balanceWarnings.GroupBy(w => w.category);

        foreach (var group in grouped)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{group.Key} ({group.Count()})", EditorStyles.boldLabel);

            foreach (var warning in group)
            {
                EditorGUILayout.BeginHorizontal();

                var oldColor = GUI.color;
                GUI.color = new Color(1f, 0.7f, 0.3f);
                EditorGUILayout.LabelField("[BAL]", GUILayout.Width(40));
                GUI.color = oldColor;

                EditorGUILayout.LabelField(warning.message, EditorStyles.wordWrappedMiniLabel, GUILayout.ExpandWidth(true));

                if (warning.asset != null && GUILayout.Button("Edit", GUILayout.Width(40)))
                {
                    Selection.activeObject = warning.asset;
                    EditorGUIUtility.PingObject(warning.asset);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
    }

    private void CheckBalanceWarnings()
    {
        balanceWarnings.Clear();
        hasRunBalanceCheck = true;

        CheckEnemyBalance();
        CheckItemBalance();
        CheckActivityBalance();
        CheckLocationBalance();
        CheckAbilityBalance();

        Logger.LogInfo($"Balance Check Complete: {balanceWarnings.Count} warning(s) found.", Logger.LogCategory.EditorLog);
    }

    private void CheckEnemyBalance()
    {
        var enemies = FindAllAssets<EnemyDefinition>();

        foreach (var enemy in enemies)
        {
            // No abilities
            if (enemy.Abilities == null || !enemy.Abilities.Any(a => a != null))
            {
                balanceWarnings.Add(new BalanceWarning
                {
                    category = "Enemies",
                    message = $"'{enemy.GetDisplayName()}' has no abilities",
                    asset = enemy
                });
            }

            // No loot
            if (enemy.LootTable == null || !enemy.LootTable.Any(l => l != null && l.Item != null))
            {
                balanceWarnings.Add(new BalanceWarning
                {
                    category = "Enemies",
                    message = $"'{enemy.GetDisplayName()}' has no loot drops",
                    asset = enemy
                });
            }

            // Zero or very low HP
            if (enemy.MaxHealth <= 0)
            {
                balanceWarnings.Add(new BalanceWarning
                {
                    category = "Enemies",
                    message = $"'{enemy.GetDisplayName()}' has {enemy.MaxHealth} HP",
                    asset = enemy
                });
            }

            // Zero XP reward
            if (enemy.ExperienceReward <= 0)
            {
                balanceWarnings.Add(new BalanceWarning
                {
                    category = "Enemies",
                    message = $"'{enemy.GetDisplayName()}' gives 0 XP",
                    asset = enemy
                });
            }
        }
    }

    private void CheckItemBalance()
    {
        if (itemRegistry == null) return;

        foreach (var item in itemRegistry.AllItems.Where(i => i != null))
        {
            // Zero sell value for sellable items (exclude quest items, etc.)
            if (item.BasePrice <= 0 && item.Type != ItemType.Quest)
            {
                balanceWarnings.Add(new BalanceWarning
                {
                    category = "Items",
                    message = $"'{item.GetDisplayName()}' has 0 sell value",
                    asset = item
                });
            }
        }
    }

    private void CheckActivityBalance()
    {
        var variants = FindAllAssets<ActivityVariant>();

        foreach (var variant in variants)
        {
            // Zero XP
            if (variant.MainSkillXPPerTick <= 0 && variant.SubSkillXPPerTick <= 0)
            {
                balanceWarnings.Add(new BalanceWarning
                {
                    category = "Activities",
                    message = $"'{variant.GetDisplayName()}' gives 0 XP",
                    asset = variant
                });
            }

            // No primary resource for harvesting activities
            if (!variant.IsTimeBased && variant.PrimaryResource == null)
            {
                balanceWarnings.Add(new BalanceWarning
                {
                    category = "Activities",
                    message = $"'{variant.GetDisplayName()}' has no primary resource",
                    asset = variant
                });
            }

            // Zero step cost for step-based activities
            if (!variant.IsTimeBased && variant.ActionCost <= 0)
            {
                balanceWarnings.Add(new BalanceWarning
                {
                    category = "Activities",
                    message = $"'{variant.GetDisplayName()}' requires 0 steps",
                    asset = variant
                });
            }
        }
    }

    private void CheckLocationBalance()
    {
        var locations = FindAllAssets<MapLocationDefinition>();

        foreach (var location in locations)
        {
            bool hasEnemies = location.AvailableEnemies != null && location.AvailableEnemies.Any(e => e?.EnemyReference != null);
            bool hasNPCs = location.AvailableNPCs != null && location.AvailableNPCs.Any(n => n?.NPCReference != null);
            bool hasActivities = location.AvailableActivities != null && location.AvailableActivities.Any(a => a?.ActivityReference != null);
            bool hasConnections = location.Connections != null && location.Connections.Any(c => c != null);

            // No content at all
            if (!hasEnemies && !hasNPCs && !hasActivities)
            {
                balanceWarnings.Add(new BalanceWarning
                {
                    category = "Locations",
                    message = $"'{location.DisplayName}' has no enemies, NPCs, or activities",
                    asset = location
                });
            }

            // No connections (isolated location)
            if (!hasConnections)
            {
                balanceWarnings.Add(new BalanceWarning
                {
                    category = "Locations",
                    message = $"'{location.DisplayName}' has no connections (isolated)",
                    asset = location
                });
            }
        }
    }

    private void CheckAbilityBalance()
    {
        if (abilityRegistry == null) return;

        foreach (var ability in abilityRegistry.AllAbilities.Where(a => a != null))
        {
            // No effects
            if (ability.Effects == null || !ability.Effects.Any(e => e != null))
            {
                balanceWarnings.Add(new BalanceWarning
                {
                    category = "Abilities",
                    message = $"'{ability.GetDisplayName()}' has no effects",
                    asset = ability
                });
            }

            // Zero cooldown (might be intentional for basic attacks)
            if (ability.Cooldown <= 0 && ability.AbilityID != "basic_attack")
            {
                balanceWarnings.Add(new BalanceWarning
                {
                    category = "Abilities",
                    message = $"'{ability.GetDisplayName()}' has 0 cooldown",
                    asset = ability
                });
            }

            // Zero weight (should have some weight for equipment limits)
            if (ability.Weight <= 0)
            {
                balanceWarnings.Add(new BalanceWarning
                {
                    category = "Abilities",
                    message = $"'{ability.GetDisplayName()}' has 0 weight",
                    asset = ability
                });
            }
        }
    }
    #endregion

    #region Validation Logic
    private void RunFullValidation()
    {
        issues.Clear();
        hasRunValidation = true;

        ValidateItemRegistry();
        ValidateAbilityRegistry();
        ValidateStatusEffectRegistry();
        ValidateEnemies();
        ValidateActivities();
        ValidateCrossReferences();

        Logger.LogInfo($"Registry Validation Complete: {issues.Count} issue(s, Logger.LogCategory.EditorLog) found.");
    }

    private void ValidateItemRegistry()
    {
        if (itemRegistry == null)
        {
            issues.Add(new ValidationIssue
            {
                category = "Item Registry",
                message = "Item Registry not found!",
                severity = MessageType.Error
            });
            return;
        }

        // Trigger registry validation
        itemRegistry.ValidateRegistry();

        // Check for items without icons
        foreach (var item in itemRegistry.AllItems.Where(i => i != null))
        {
            if (item.ItemIcon == null)
            {
                issues.Add(new ValidationIssue
                {
                    category = "Item Registry",
                    message = $"Item '{item.GetDisplayName()}' has no icon",
                    severity = MessageType.Warning,
                    asset = item
                });
            }
        }
    }

    private void ValidateAbilityRegistry()
    {
        if (abilityRegistry == null)
        {
            issues.Add(new ValidationIssue
            {
                category = "Ability Registry",
                message = "Ability Registry not found!",
                severity = MessageType.Error
            });
            return;
        }

        foreach (var ability in abilityRegistry.AllAbilities.Where(a => a != null))
        {
            if (!ability.IsValid())
            {
                issues.Add(new ValidationIssue
                {
                    category = "Ability Registry",
                    message = $"Ability '{ability.GetDisplayName()}' is invalid",
                    severity = MessageType.Error,
                    asset = ability
                });
            }

            if (ability.AbilityIcon == null)
            {
                issues.Add(new ValidationIssue
                {
                    category = "Ability Registry",
                    message = $"Ability '{ability.GetDisplayName()}' has no icon",
                    severity = MessageType.Warning,
                    asset = ability
                });
            }

            // Check status effect references
            if (ability.Effects != null)
            {
                foreach (var effect in ability.Effects)
                {
                    if (effect != null && effect.Type == AbilityEffectType.StatusEffect && effect.StatusEffect == null)
                    {
                        issues.Add(new ValidationIssue
                        {
                            category = "Ability Registry",
                            message = $"Ability '{ability.GetDisplayName()}' has StatusEffect type but no effect assigned",
                            severity = MessageType.Error,
                            asset = ability
                        });
                    }
                }
            }
        }
    }

    private void ValidateStatusEffectRegistry()
    {
        if (statusEffectRegistry == null)
        {
            issues.Add(new ValidationIssue
            {
                category = "Status Effect Registry",
                message = "Status Effect Registry not found!",
                severity = MessageType.Error
            });
            return;
        }

        foreach (var effect in statusEffectRegistry.AllEffects.Where(e => e != null))
        {
            if (!effect.IsValid())
            {
                issues.Add(new ValidationIssue
                {
                    category = "Status Effect Registry",
                    message = $"Status Effect '{effect.GetDisplayName()}' is invalid",
                    severity = MessageType.Error,
                    asset = effect
                });
            }

            if (effect.EffectIcon == null)
            {
                issues.Add(new ValidationIssue
                {
                    category = "Status Effect Registry",
                    message = $"Status Effect '{effect.GetDisplayName()}' has no icon",
                    severity = MessageType.Warning,
                    asset = effect
                });
            }
        }
    }

    private void ValidateEnemies()
    {
        var enemies = FindAllAssets<EnemyDefinition>();

        foreach (var enemy in enemies)
        {
            if (!enemy.IsValid())
            {
                issues.Add(new ValidationIssue
                {
                    category = "Enemies",
                    message = $"Enemy '{enemy.name}' is invalid (missing ID or name)",
                    severity = MessageType.Error,
                    asset = enemy
                });
            }

            if (enemy.Abilities == null || !enemy.Abilities.Any(a => a != null))
            {
                issues.Add(new ValidationIssue
                {
                    category = "Enemies",
                    message = $"Enemy '{enemy.GetDisplayName()}' has no abilities",
                    severity = MessageType.Warning,
                    asset = enemy
                });
            }

            if (enemy.EnemySprite == null && enemy.Avatar == null)
            {
                issues.Add(new ValidationIssue
                {
                    category = "Enemies",
                    message = $"Enemy '{enemy.GetDisplayName()}' has no sprite or avatar",
                    severity = MessageType.Warning,
                    asset = enemy
                });
            }
        }
    }

    private void ValidateActivities()
    {
        var variants = FindAllAssets<ActivityVariant>();

        foreach (var variant in variants)
        {
            if (!variant.IsValidVariant())
            {
                issues.Add(new ValidationIssue
                {
                    category = "Activities",
                    message = $"Activity Variant '{variant.GetDisplayName()}' is invalid",
                    severity = MessageType.Error,
                    asset = variant
                });
            }

            // Check material/quantity sync for time-based
            if (variant.IsTimeBased && variant.RequiredMaterials != null && variant.RequiredQuantities != null)
            {
                if (variant.RequiredMaterials.Length != variant.RequiredQuantities.Length)
                {
                    issues.Add(new ValidationIssue
                    {
                        category = "Activities",
                        message = $"Activity '{variant.GetDisplayName()}' has mismatched materials/quantities arrays",
                        severity = MessageType.Error,
                        asset = variant
                    });
                }
            }
        }
    }

    private void ValidateCrossReferences()
    {
        // Check that all ability status effect references exist in registry
        if (abilityRegistry != null && statusEffectRegistry != null)
        {
            foreach (var ability in abilityRegistry.AllAbilities.Where(a => a != null && a.Effects != null))
            {
                foreach (var effect in ability.Effects.Where(e => e != null && e.Type == AbilityEffectType.StatusEffect))
                {
                    if (effect.StatusEffect != null && !statusEffectRegistry.AllEffects.Contains(effect.StatusEffect))
                    {
                        issues.Add(new ValidationIssue
                        {
                            category = "Cross-References",
                            message = $"Ability '{ability.GetDisplayName()}' references status effect not in registry",
                            severity = MessageType.Warning,
                            asset = ability
                        });
                    }
                }
            }
        }
    }
    #endregion

    #region Helper Methods
    private void LoadRegistries()
    {
        itemRegistry = FindAsset<ItemRegistry>();
        activityRegistry = FindAsset<ActivityRegistry>();
        abilityRegistry = FindAsset<AbilityRegistry>();
        statusEffectRegistry = FindAsset<StatusEffectRegistry>();
    }

    private void RefreshCounts()
    {
        itemCount = itemRegistry?.AllItems?.Count(i => i != null) ?? 0;
        activityCount = activityRegistry?.AllActivities?.Count ?? 0;
        abilityCount = abilityRegistry?.AllAbilities?.Count(a => a != null) ?? 0;
        statusEffectCount = statusEffectRegistry?.AllEffects?.Count(e => e != null) ?? 0;
        enemyCount = FindAllAssets<EnemyDefinition>().Count;
        locationCount = FindAllAssets<MapLocationDefinition>().Count;
    }

    private T FindAsset<T>() where T : Object
    {
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
        return null;
    }

    private List<T> FindAllAssets<T>() where T : Object
    {
        var results = new List<T>();
        string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                results.Add(asset);
            }
        }
        return results;
    }

    private Dictionary<string, int> CountItemsByRarity()
    {
        var counts = new Dictionary<string, int>();
        if (itemRegistry?.AllItems == null) return counts;

        foreach (var item in itemRegistry.AllItems.Where(i => i != null))
        {
            string rarity = item.GetRarityText();
            if (!counts.ContainsKey(rarity))
                counts[rarity] = 0;
            counts[rarity]++;
        }
        return counts;
    }

    private Dictionary<string, int> CountAbilitiesByType()
    {
        var counts = new Dictionary<string, int>();
        if (abilityRegistry?.AllAbilities == null) return counts;

        foreach (var ability in abilityRegistry.AllAbilities.Where(a => a != null && a.Effects != null))
        {
            foreach (var effect in ability.Effects.Where(e => e != null))
            {
                string type = effect.Type.ToString();
                if (!counts.ContainsKey(type))
                    counts[type] = 0;
                counts[type]++;
            }
        }
        return counts;
    }

    private List<CrossReference> GetAbilitiesWithStatusEffects()
    {
        var refs = new List<CrossReference>();
        if (abilityRegistry?.AllAbilities == null) return refs;

        foreach (var ability in abilityRegistry.AllAbilities.Where(a => a != null && a.Effects != null))
        {
            foreach (var effect in ability.Effects.Where(e => e != null && e.Type == AbilityEffectType.StatusEffect))
            {
                refs.Add(new CrossReference
                {
                    sourceName = ability.GetDisplayName(),
                    targetName = effect.StatusEffect?.GetDisplayName() ?? "(null)",
                    isValid = effect.StatusEffect != null,
                    sourceAsset = ability
                });
            }
        }
        return refs;
    }

    private List<CrossReference> GetEnemiesWithAbilities()
    {
        var refs = new List<CrossReference>();
        var enemies = FindAllAssets<EnemyDefinition>();

        foreach (var enemy in enemies.Where(e => e.Abilities != null))
        {
            foreach (var ability in enemy.Abilities)
            {
                refs.Add(new CrossReference
                {
                    sourceName = enemy.GetDisplayName(),
                    targetName = ability?.GetDisplayName() ?? "(null)",
                    isValid = ability != null,
                    sourceAsset = enemy
                });
            }
        }
        return refs;
    }

    private List<CrossReference> GetEnemiesWithLoot()
    {
        var refs = new List<CrossReference>();
        var enemies = FindAllAssets<EnemyDefinition>();

        foreach (var enemy in enemies.Where(e => e.LootTable != null))
        {
            foreach (var loot in enemy.LootTable.Where(l => l != null))
            {
                refs.Add(new CrossReference
                {
                    sourceName = enemy.GetDisplayName(),
                    targetName = loot.Item?.GetDisplayName() ?? "(null)",
                    isValid = loot.Item != null,
                    sourceAsset = enemy
                });
            }
        }
        return refs;
    }

    private List<CrossReference> GetActivitiesWithItems()
    {
        var refs = new List<CrossReference>();
        var variants = FindAllAssets<ActivityVariant>();

        foreach (var variant in variants)
        {
            // Primary resource
            refs.Add(new CrossReference
            {
                sourceName = variant.GetDisplayName(),
                targetName = variant.PrimaryResource?.GetDisplayName() ?? "(null)",
                isValid = variant.PrimaryResource != null,
                sourceAsset = variant
            });

            // Required materials
            if (variant.RequiredMaterials != null)
            {
                foreach (var material in variant.RequiredMaterials)
                {
                    refs.Add(new CrossReference
                    {
                        sourceName = variant.GetDisplayName() + " (material)",
                        targetName = material?.GetDisplayName() ?? "(null)",
                        isValid = material != null,
                        sourceAsset = variant
                    });
                }
            }
        }
        return refs;
    }
    #endregion
}
#endif
