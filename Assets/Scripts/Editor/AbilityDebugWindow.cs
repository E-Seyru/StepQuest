// Purpose: Editor window for debugging and testing the ability system
// Filepath: Assets/Scripts/Editor/AbilityDebugWindow.cs

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AbilityDebugWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private AbilityRegistry abilityRegistry;

    [MenuItem("WalkAndRPG/Ability Debug")]
    public static void ShowWindow()
    {
        GetWindow<AbilityDebugWindow>("Ability Debug");
    }

    private void OnEnable()
    {
        // Try to find AbilityRegistry
        FindAbilityRegistry();
    }

    private void FindAbilityRegistry()
    {
        if (abilityRegistry == null)
        {
            // Try to find in assets
            string[] guids = AssetDatabase.FindAssets("t:AbilityRegistry");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                abilityRegistry = AssetDatabase.LoadAssetAtPath<AbilityRegistry>(path);
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Ability Debug Tools", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Registry reference
        abilityRegistry = (AbilityRegistry)EditorGUILayout.ObjectField("Ability Registry", abilityRegistry, typeof(AbilityRegistry), false);

        if (abilityRegistry == null)
        {
            EditorGUILayout.HelpBox("Please assign an AbilityRegistry or click Find Registry.", MessageType.Warning);
            if (GUILayout.Button("Find Registry"))
            {
                FindAbilityRegistry();
            }
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to add/remove abilities.", MessageType.Info);

            EditorGUILayout.Space();
            GUILayout.Label("Available Abilities in Registry:", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (abilityRegistry.AllAbilities != null)
            {
                foreach (var ability in abilityRegistry.AllAbilities)
                {
                    if (ability != null)
                    {
                        EditorGUILayout.LabelField($"  - {ability.AbilityName} (ID: {ability.AbilityID}, Weight: {ability.Weight})");
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            return;
        }

        // Play mode - show full controls
        EditorGUILayout.Space();
        GUILayout.Label("Quick Actions", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add All Abilities", GUILayout.Height(30)))
        {
            AddAllAbilities();
        }
        if (GUILayout.Button("Clear All Abilities", GUILayout.Height(30)))
        {
            ClearAllAbilities();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // Current state
        GUILayout.Label("Current State", EditorStyles.boldLabel);

        var ownedIds = GetOwnedAbilityIds();
        var equippedIds = GetEquippedAbilityIds();
        int currentWeight = GetCurrentEquippedWeight();

        EditorGUILayout.LabelField($"Owned Abilities: {ownedIds.Count}");
        EditorGUILayout.LabelField($"Equipped Abilities: {equippedIds.Count}");
        EditorGUILayout.LabelField($"Equipped Weight: {currentWeight}/12");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // Individual abilities
        GUILayout.Label("Abilities", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (abilityRegistry.AllAbilities != null)
        {
            foreach (var ability in abilityRegistry.AllAbilities)
            {
                if (ability == null) continue;

                bool isOwned = ownedIds.Contains(ability.AbilityID);
                bool isEquipped = equippedIds.Contains(ability.AbilityID);

                EditorGUILayout.BeginHorizontal("box");

                // Ability info
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(ability.AbilityName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"ID: {ability.AbilityID} | Weight: {ability.Weight}");

                string status = isEquipped ? "EQUIPPED" : (isOwned ? "Owned" : "Not Owned");
                EditorGUILayout.LabelField($"Status: {status}");
                EditorGUILayout.EndVertical();

                // Buttons
                EditorGUILayout.BeginVertical(GUILayout.Width(80));

                if (!isOwned)
                {
                    if (GUILayout.Button("Add"))
                    {
                        AddAbility(ability.AbilityID);
                    }
                }
                else
                {
                    if (GUILayout.Button("Remove"))
                    {
                        RemoveAbility(ability.AbilityID);
                    }

                    if (!isEquipped)
                    {
                        if (GUILayout.Button("Equip"))
                        {
                            EquipAbility(ability.AbilityID);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Unequip"))
                        {
                            UnequipAbility(ability.AbilityID);
                        }
                    }
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndScrollView();

        // Auto-refresh
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    // === Helper Methods ===

    private List<string> GetOwnedAbilityIds()
    {
        if (AbilityManager.Instance != null)
        {
            return AbilityManager.Instance.GetOwnedAbilityIds();
        }

        // Fallback: read from DataManager directly
        if (DataManager.Instance?.PlayerData != null)
        {
            return DataManager.Instance.PlayerData.OwnedAbilities;
        }

        return new List<string>();
    }

    private List<string> GetEquippedAbilityIds()
    {
        if (AbilityManager.Instance != null)
        {
            return AbilityManager.Instance.GetEquippedAbilityIds();
        }

        if (DataManager.Instance?.PlayerData != null)
        {
            return DataManager.Instance.PlayerData.EquippedAbilities;
        }

        return new List<string>();
    }

    private int GetCurrentEquippedWeight()
    {
        if (AbilityManager.Instance != null)
        {
            return AbilityManager.Instance.GetCurrentEquippedWeight();
        }
        return 0;
    }

    private void AddAbility(string abilityId)
    {
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.AddOwnedAbility(abilityId);
            Logger.LogInfo($"[AbilityDebug] Added ability: {abilityId}", Logger.LogCategory.EditorLog);
        }
        else
        {
            Logger.LogWarning("[AbilityDebug] AbilityManager not found!", Logger.LogCategory.EditorLog);
        }
    }

    private void RemoveAbility(string abilityId)
    {
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.RemoveOwnedAbility(abilityId);
            Logger.LogInfo($"[AbilityDebug] Removed ability: {abilityId}", Logger.LogCategory.EditorLog);
        }
    }

    private void EquipAbility(string abilityId)
    {
        if (AbilityManager.Instance != null)
        {
            if (AbilityManager.Instance.TryEquipAbility(abilityId))
            {
                Logger.LogInfo($"[AbilityDebug] Equipped ability: {abilityId}", Logger.LogCategory.EditorLog);
            }
            else
            {
                Logger.LogWarning($"[AbilityDebug] Could not equip ability: {abilityId} (weight limit?, Logger.LogCategory.EditorLog)");
            }
        }
    }

    private void UnequipAbility(string abilityId)
    {
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.TryUnequipAbility(abilityId);
            Logger.LogInfo($"[AbilityDebug] Unequipped ability: {abilityId}", Logger.LogCategory.EditorLog);
        }
    }

    private void AddAllAbilities()
    {
        if (abilityRegistry?.AllAbilities == null) return;

        foreach (var ability in abilityRegistry.AllAbilities)
        {
            if (ability != null)
            {
                AddAbility(ability.AbilityID);
            }
        }
        Logger.LogInfo("[AbilityDebug] Added all abilities from registry", Logger.LogCategory.EditorLog);
    }

    private void ClearAllAbilities()
    {
        if (AbilityManager.Instance != null)
        {
            AbilityManager.Instance.DebugClearAllAbilities();
            Logger.LogInfo("[AbilityDebug] Cleared all abilities", Logger.LogCategory.EditorLog);
        }
        else if (DataManager.Instance?.PlayerData != null)
        {
            DataManager.Instance.PlayerData.OwnedAbilities = new List<string>();
            DataManager.Instance.PlayerData.EquippedAbilities = new List<string>();
            DataManager.Instance.SaveGame();
            Logger.LogInfo("[AbilityDebug] Cleared all abilities (via DataManager, Logger.LogCategory.EditorLog)");
        }
    }
}
