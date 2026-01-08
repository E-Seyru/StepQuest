// Purpose: Utility class for scanning cross-references between game content
// Filepath: Assets/Scripts/Editor/DependencyScanner.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Scans the project for dependencies between game content.
/// Used by manager windows to show "Used By" / "Dropped By" / "Found At" sections,
/// and to show warnings when deleting content that is referenced elsewhere.
/// </summary>
public static class DependencyScanner
{
    #region Reference Classes

    public class ItemReference
    {
        public string SourceType; // "Enemy", "Activity", "Item"
        public string SourceName;
        public string SourceID;
        public string Context; // "Loot Table", "Required Materials", etc.
        public Object SourceAsset;
    }

    public class AbilityReference
    {
        public string SourceType; // "Enemy", "Item"
        public string SourceName;
        public string SourceID;
        public string Context; // "Abilities", "UnlockedAbility"
        public Object SourceAsset;
    }

    public class EnemyReference
    {
        public string LocationName;
        public string LocationID;
        public bool IsHidden;
        public Object LocationAsset;
    }

    public class NPCReference
    {
        public string LocationName;
        public string LocationID;
        public bool IsHidden;
        public Object LocationAsset;
    }

    public class StatusEffectReference
    {
        public string SourceType; // "Ability"
        public string SourceName;
        public string SourceID;
        public Object SourceAsset;
    }

    public class LocationReference
    {
        public string SourceType; // "Location" (for connections)
        public string SourceName;
        public string SourceID;
        public string Context; // "Connection"
        public Object SourceAsset;
    }

    #endregion

    #region Item References

    /// <summary>
    /// Find all enemies that drop this item in their loot table
    /// </summary>
    public static List<ItemReference> FindItemDroppedBy(ItemDefinition item)
    {
        var references = new List<ItemReference>();
        if (item == null) return references;

        string[] enemyGuids = AssetDatabase.FindAssets("t:EnemyDefinition");
        foreach (var guid in enemyGuids)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
                AssetDatabase.GUIDToAssetPath(guid));

            if (enemy?.LootTable == null) continue;

            foreach (var loot in enemy.LootTable)
            {
                if (loot?.Item == item)
                {
                    references.Add(new ItemReference
                    {
                        SourceType = "Enemy",
                        SourceName = enemy.GetDisplayName(),
                        SourceID = enemy.EnemyID,
                        Context = $"Loot Table ({loot.DropChance:P0})",
                        SourceAsset = enemy
                    });
                    break; // Only add enemy once even if item appears multiple times
                }
            }
        }

        return references;
    }

    /// <summary>
    /// Find all activity variants that use this item as a required material
    /// </summary>
    public static List<ItemReference> FindItemUsedIn(ItemDefinition item)
    {
        var references = new List<ItemReference>();
        if (item == null) return references;

        string[] variantGuids = AssetDatabase.FindAssets("t:ActivityVariant");
        foreach (var guid in variantGuids)
        {
            var variant = AssetDatabase.LoadAssetAtPath<ActivityVariant>(
                AssetDatabase.GUIDToAssetPath(guid));

            if (variant?.RequiredMaterials == null) continue;

            for (int i = 0; i < variant.RequiredMaterials.Length; i++)
            {
                if (variant.RequiredMaterials[i] == item)
                {
                    int quantity = i < variant.RequiredQuantities?.Length ? variant.RequiredQuantities[i] : 1;
                    references.Add(new ItemReference
                    {
                        SourceType = "Activity",
                        SourceName = variant.GetDisplayName(),
                        SourceID = variant.GetParentActivityID(),
                        Context = $"Requires {quantity}x",
                        SourceAsset = variant
                    });
                    break;
                }
            }
        }

        return references;
    }

    /// <summary>
    /// Find all activity variants that produce this item
    /// </summary>
    public static List<ItemReference> FindItemProducedBy(ItemDefinition item)
    {
        var references = new List<ItemReference>();
        if (item == null) return references;

        string[] variantGuids = AssetDatabase.FindAssets("t:ActivityVariant");
        foreach (var guid in variantGuids)
        {
            var variant = AssetDatabase.LoadAssetAtPath<ActivityVariant>(
                AssetDatabase.GUIDToAssetPath(guid));

            if (variant == null) continue;

            bool isProduced = variant.PrimaryResource == item;
            if (!isProduced && variant.SecondaryResources != null)
            {
                isProduced = variant.SecondaryResources.Contains(item);
            }

            if (isProduced)
            {
                references.Add(new ItemReference
                {
                    SourceType = "Activity",
                    SourceName = variant.GetDisplayName(),
                    SourceID = variant.GetParentActivityID(),
                    Context = variant.PrimaryResource == item ? "Primary Resource" : "Secondary Resource",
                    SourceAsset = variant
                });
            }
        }

        return references;
    }

    /// <summary>
    /// Get all references to an item (for delete warning)
    /// </summary>
    public static List<ItemReference> GetAllItemReferences(ItemDefinition item)
    {
        var allRefs = new List<ItemReference>();
        allRefs.AddRange(FindItemDroppedBy(item));
        allRefs.AddRange(FindItemUsedIn(item));
        allRefs.AddRange(FindItemProducedBy(item));
        return allRefs;
    }

    #endregion

    #region Ability References

    /// <summary>
    /// Find all enemies that use this ability
    /// </summary>
    public static List<AbilityReference> FindAbilityUsedByEnemies(AbilityDefinition ability)
    {
        var references = new List<AbilityReference>();
        if (ability == null) return references;

        string[] enemyGuids = AssetDatabase.FindAssets("t:EnemyDefinition");
        foreach (var guid in enemyGuids)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
                AssetDatabase.GUIDToAssetPath(guid));

            if (enemy?.Abilities == null) continue;

            if (enemy.Abilities.Contains(ability))
            {
                references.Add(new AbilityReference
                {
                    SourceType = "Enemy",
                    SourceName = enemy.GetDisplayName(),
                    SourceID = enemy.EnemyID,
                    Context = "Abilities",
                    SourceAsset = enemy
                });
            }
        }

        return references;
    }

    /// <summary>
    /// Find all items that unlock this ability
    /// </summary>
    public static List<AbilityReference> FindAbilityUnlockedByItems(AbilityDefinition ability)
    {
        var references = new List<AbilityReference>();
        if (ability == null) return references;

        // Load item registry
        string[] registryGuids = AssetDatabase.FindAssets("t:ItemRegistry");
        if (registryGuids.Length == 0) return references;

        var registry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(
            AssetDatabase.GUIDToAssetPath(registryGuids[0]));

        if (registry?.AllItems == null) return references;

        foreach (var item in registry.AllItems.Where(i => i != null))
        {
            // Check base unlocked ability
            if (item.BaseUnlockedAbility == ability)
            {
                references.Add(new AbilityReference
                {
                    SourceType = "Item",
                    SourceName = item.GetDisplayName(),
                    SourceID = item.ItemID,
                    Context = "Base Unlocked Ability",
                    SourceAsset = item
                });
            }

            // Check rarity-specific abilities
            if (item.RarityStats != null)
            {
                foreach (var rarity in item.RarityStats)
                {
                    if (rarity?.UnlockedAbility == ability)
                    {
                        references.Add(new AbilityReference
                        {
                            SourceType = "Item",
                            SourceName = item.GetDisplayName(),
                            SourceID = item.ItemID,
                            Context = $"{rarity.GetRarityDisplayName()} Unlocked Ability",
                            SourceAsset = item
                        });
                    }
                }
            }
        }

        return references;
    }

    /// <summary>
    /// Find all enemies that drop this ability in their loot table
    /// </summary>
    public static List<AbilityReference> FindAbilityDroppedBy(AbilityDefinition ability)
    {
        var references = new List<AbilityReference>();
        if (ability == null) return references;

        // Abilities are dropped indirectly through items that unlock them
        // So we find items that unlock this ability, then find enemies that drop those items
        var itemsWithAbility = FindAbilityUnlockedByItems(ability);

        foreach (var itemRef in itemsWithAbility)
        {
            var item = itemRef.SourceAsset as ItemDefinition;
            if (item == null) continue;

            var droppedBy = FindItemDroppedBy(item);
            foreach (var dropRef in droppedBy)
            {
                references.Add(new AbilityReference
                {
                    SourceType = "Enemy",
                    SourceName = dropRef.SourceName,
                    SourceID = dropRef.SourceID,
                    Context = $"Drops {item.GetDisplayName()} (unlocks this ability)",
                    SourceAsset = dropRef.SourceAsset
                });
            }
        }

        return references;
    }

    /// <summary>
    /// Get all references to an ability (for delete warning)
    /// </summary>
    public static List<AbilityReference> GetAllAbilityReferences(AbilityDefinition ability)
    {
        var allRefs = new List<AbilityReference>();
        allRefs.AddRange(FindAbilityUsedByEnemies(ability));
        allRefs.AddRange(FindAbilityUnlockedByItems(ability));
        return allRefs;
    }

    #endregion

    #region Enemy References

    /// <summary>
    /// Find all locations where this enemy appears
    /// </summary>
    public static List<EnemyReference> FindEnemyLocations(EnemyDefinition enemy)
    {
        var references = new List<EnemyReference>();
        if (enemy == null) return references;

        // Load location registry
        string[] registryGuids = AssetDatabase.FindAssets("t:LocationRegistry");
        if (registryGuids.Length == 0) return references;

        var registry = AssetDatabase.LoadAssetAtPath<LocationRegistry>(
            AssetDatabase.GUIDToAssetPath(registryGuids[0]));

        if (registry?.AllLocations == null) return references;

        foreach (var location in registry.AllLocations.Where(l => l != null))
        {
            if (location.AvailableEnemies == null) continue;

            foreach (var locEnemy in location.AvailableEnemies)
            {
                if (locEnemy?.EnemyReference == enemy)
                {
                    references.Add(new EnemyReference
                    {
                        LocationName = location.DisplayName,
                        LocationID = location.LocationID,
                        IsHidden = locEnemy.IsHidden,
                        LocationAsset = location
                    });
                    break;
                }
            }
        }

        return references;
    }

    #endregion

    #region NPC References

    /// <summary>
    /// Find all locations where this NPC appears
    /// </summary>
    public static List<NPCReference> FindNPCLocations(NPCDefinition npc)
    {
        var references = new List<NPCReference>();
        if (npc == null) return references;

        // Load location registry
        string[] registryGuids = AssetDatabase.FindAssets("t:LocationRegistry");
        if (registryGuids.Length == 0) return references;

        var registry = AssetDatabase.LoadAssetAtPath<LocationRegistry>(
            AssetDatabase.GUIDToAssetPath(registryGuids[0]));

        if (registry?.AllLocations == null) return references;

        foreach (var location in registry.AllLocations.Where(l => l != null))
        {
            if (location.AvailableNPCs == null) continue;

            foreach (var locNPC in location.AvailableNPCs)
            {
                if (locNPC?.NPCReference == npc)
                {
                    references.Add(new NPCReference
                    {
                        LocationName = location.DisplayName,
                        LocationID = location.LocationID,
                        IsHidden = locNPC.IsHidden,
                        LocationAsset = location
                    });
                    break;
                }
            }
        }

        return references;
    }

    /// <summary>
    /// Get dialogue count for an NPC
    /// </summary>
    public static int GetNPCDialogueCount(NPCDefinition npc)
    {
        if (npc?.Dialogues == null) return 0;
        return npc.Dialogues.Count(d => d != null);
    }

    #endregion

    #region Status Effect References

    /// <summary>
    /// Find all abilities that apply this status effect
    /// </summary>
    public static List<StatusEffectReference> FindStatusEffectUsedByAbilities(StatusEffectDefinition effect)
    {
        var references = new List<StatusEffectReference>();
        if (effect == null) return references;

        // Load ability registry
        string[] registryGuids = AssetDatabase.FindAssets("t:AbilityRegistry");
        if (registryGuids.Length == 0) return references;

        var registry = AssetDatabase.LoadAssetAtPath<AbilityRegistry>(
            AssetDatabase.GUIDToAssetPath(registryGuids[0]));

        if (registry?.AllAbilities == null) return references;

        foreach (var ability in registry.AllAbilities.Where(a => a != null))
        {
            if (ability.Effects == null) continue;

            foreach (var abilityEffect in ability.Effects)
            {
                if (abilityEffect.StatusEffect == effect)
                {
                    references.Add(new StatusEffectReference
                    {
                        SourceType = "Ability",
                        SourceName = ability.AbilityName,
                        SourceID = ability.AbilityID,
                        SourceAsset = ability
                    });
                    break;
                }
            }
        }

        return references;
    }

    #endregion

    #region Location References

    /// <summary>
    /// Find all locations that connect to this location
    /// </summary>
    public static List<LocationReference> FindLocationConnections(MapLocationDefinition location)
    {
        var references = new List<LocationReference>();
        if (location == null) return references;

        // Load location registry
        string[] registryGuids = AssetDatabase.FindAssets("t:LocationRegistry");
        if (registryGuids.Length == 0) return references;

        var registry = AssetDatabase.LoadAssetAtPath<LocationRegistry>(
            AssetDatabase.GUIDToAssetPath(registryGuids[0]));

        if (registry?.AllLocations == null) return references;

        foreach (var otherLocation in registry.AllLocations.Where(l => l != null && l != location))
        {
            if (otherLocation.Connections == null) continue;

            foreach (var connection in otherLocation.Connections)
            {
                if (connection.DestinationLocationID == location.LocationID)
                {
                    references.Add(new LocationReference
                    {
                        SourceType = "Location",
                        SourceName = otherLocation.DisplayName,
                        SourceID = otherLocation.LocationID,
                        Context = $"Connection ({connection.StepCost} steps)",
                        SourceAsset = otherLocation
                    });
                }
            }
        }

        return references;
    }

    #endregion

    #region Delete Warning Dialogs

    /// <summary>
    /// Show delete warning dialog for an item. Returns true if user confirms deletion.
    /// </summary>
    public static bool ShowItemDeleteWarning(ItemDefinition item)
    {
        var refs = GetAllItemReferences(item);
        return ShowDeleteWarningDialog(item.GetDisplayName(), "Item", refs.Select(r =>
            $"- {r.SourceType}: {r.SourceName} ({r.Context})").ToList());
    }

    /// <summary>
    /// Show delete warning dialog for an ability. Returns true if user confirms deletion.
    /// </summary>
    public static bool ShowAbilityDeleteWarning(AbilityDefinition ability)
    {
        var refs = GetAllAbilityReferences(ability);
        return ShowDeleteWarningDialog(ability.AbilityName, "Ability", refs.Select(r =>
            $"- {r.SourceType}: {r.SourceName} ({r.Context})").ToList());
    }

    /// <summary>
    /// Show delete warning dialog for an enemy. Returns true if user confirms deletion.
    /// </summary>
    public static bool ShowEnemyDeleteWarning(EnemyDefinition enemy)
    {
        var refs = FindEnemyLocations(enemy);
        return ShowDeleteWarningDialog(enemy.GetDisplayName(), "Enemy", refs.Select(r =>
            $"- Location: {r.LocationName}" + (r.IsHidden ? " (Hidden)" : "")).ToList());
    }

    /// <summary>
    /// Show delete warning dialog for an NPC. Returns true if user confirms deletion.
    /// </summary>
    public static bool ShowNPCDeleteWarning(NPCDefinition npc)
    {
        var refs = FindNPCLocations(npc);
        int dialogueCount = GetNPCDialogueCount(npc);

        var refStrings = refs.Select(r =>
            $"- Location: {r.LocationName}" + (r.IsHidden ? " (Hidden)" : "")).ToList();

        if (dialogueCount > 0)
        {
            refStrings.Insert(0, $"- {dialogueCount} Dialogue(s) assigned");
        }

        return ShowDeleteWarningDialog(npc.NPCName, "NPC", refStrings);
    }

    /// <summary>
    /// Show delete warning dialog for a status effect. Returns true if user confirms deletion.
    /// </summary>
    public static bool ShowStatusEffectDeleteWarning(StatusEffectDefinition effect)
    {
        var refs = FindStatusEffectUsedByAbilities(effect);
        return ShowDeleteWarningDialog(effect.EffectName, "Status Effect", refs.Select(r =>
            $"- Ability: {r.SourceName}").ToList());
    }

    /// <summary>
    /// Show delete warning dialog for a location. Returns true if user confirms deletion.
    /// </summary>
    public static bool ShowLocationDeleteWarning(MapLocationDefinition location)
    {
        var refs = FindLocationConnections(location);
        return ShowDeleteWarningDialog(location.DisplayName, "Location", refs.Select(r =>
            $"- {r.SourceName} connects here ({r.Context})").ToList());
    }

    private static bool ShowDeleteWarningDialog(string itemName, string itemType, List<string> references)
    {
        if (references.Count == 0)
        {
            return EditorUtility.DisplayDialog(
                $"Delete {itemType}",
                $"Delete '{itemName}'?\n\nNo references found.",
                "Delete", "Cancel");
        }

        string refText = string.Join("\n", references.Take(10));
        if (references.Count > 10)
        {
            refText += $"\n... and {references.Count - 10} more";
        }

        return EditorUtility.DisplayDialog(
            $"Delete {itemType}",
            $"'{itemName}' is referenced by:\n\n{refText}\n\nDelete anyway?",
            "Delete", "Cancel");
    }

    #endregion

    #region Cached Scanning (for performance)

    // Cache for repeated lookups within same frame/operation
    private static Dictionary<ItemDefinition, List<ItemReference>> cachedItemDroppedBy;
    private static Dictionary<ItemDefinition, List<ItemReference>> cachedItemUsedIn;
    private static double lastCacheTime;
    private const double CACHE_DURATION = 5.0; // seconds

    /// <summary>
    /// Build cache for all item references (call once before batch operations)
    /// </summary>
    public static void BuildItemReferenceCache()
    {
        cachedItemDroppedBy = new Dictionary<ItemDefinition, List<ItemReference>>();
        cachedItemUsedIn = new Dictionary<ItemDefinition, List<ItemReference>>();
        lastCacheTime = EditorApplication.timeSinceStartup;

        // Build dropped by cache
        string[] enemyGuids = AssetDatabase.FindAssets("t:EnemyDefinition");
        foreach (var guid in enemyGuids)
        {
            var enemy = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
                AssetDatabase.GUIDToAssetPath(guid));

            if (enemy?.LootTable == null) continue;

            foreach (var loot in enemy.LootTable)
            {
                if (loot?.Item == null) continue;

                if (!cachedItemDroppedBy.ContainsKey(loot.Item))
                    cachedItemDroppedBy[loot.Item] = new List<ItemReference>();

                cachedItemDroppedBy[loot.Item].Add(new ItemReference
                {
                    SourceType = "Enemy",
                    SourceName = enemy.GetDisplayName(),
                    SourceID = enemy.EnemyID,
                    Context = $"Loot Table ({loot.DropChance:P0})",
                    SourceAsset = enemy
                });
            }
        }

        // Build used in cache
        string[] variantGuids = AssetDatabase.FindAssets("t:ActivityVariant");
        foreach (var guid in variantGuids)
        {
            var variant = AssetDatabase.LoadAssetAtPath<ActivityVariant>(
                AssetDatabase.GUIDToAssetPath(guid));

            if (variant?.RequiredMaterials == null) continue;

            for (int i = 0; i < variant.RequiredMaterials.Length; i++)
            {
                var mat = variant.RequiredMaterials[i];
                if (mat == null) continue;

                if (!cachedItemUsedIn.ContainsKey(mat))
                    cachedItemUsedIn[mat] = new List<ItemReference>();

                int quantity = i < variant.RequiredQuantities?.Length ? variant.RequiredQuantities[i] : 1;
                cachedItemUsedIn[mat].Add(new ItemReference
                {
                    SourceType = "Activity",
                    SourceName = variant.GetDisplayName(),
                    SourceID = variant.GetParentActivityID(),
                    Context = $"Requires {quantity}x",
                    SourceAsset = variant
                });
            }
        }
    }

    /// <summary>
    /// Get cached item dropped by references (faster for batch operations)
    /// </summary>
    public static List<ItemReference> GetCachedItemDroppedBy(ItemDefinition item)
    {
        if (cachedItemDroppedBy == null || EditorApplication.timeSinceStartup - lastCacheTime > CACHE_DURATION)
        {
            BuildItemReferenceCache();
        }

        return cachedItemDroppedBy.TryGetValue(item, out var refs) ? refs : new List<ItemReference>();
    }

    /// <summary>
    /// Get cached item used in references (faster for batch operations)
    /// </summary>
    public static List<ItemReference> GetCachedItemUsedIn(ItemDefinition item)
    {
        if (cachedItemUsedIn == null || EditorApplication.timeSinceStartup - lastCacheTime > CACHE_DURATION)
        {
            BuildItemReferenceCache();
        }

        return cachedItemUsedIn.TryGetValue(item, out var refs) ? refs : new List<ItemReference>();
    }

    /// <summary>
    /// Clear the reference cache
    /// </summary>
    public static void ClearCache()
    {
        cachedItemDroppedBy = null;
        cachedItemUsedIn = null;
    }

    #endregion
}
#endif
