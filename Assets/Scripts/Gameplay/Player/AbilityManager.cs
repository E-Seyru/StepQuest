// Purpose: Manager for ability ownership and equipment
// Filepath: Assets/Scripts/Gameplay/Player/AbilityManager.cs

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AbilityEvents;

/// <summary>
/// Manages player's owned and equipped abilities.
/// Handles weight-based equipment limits and persistence.
/// </summary>
public class AbilityManager : MonoBehaviour
{
    public static AbilityManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int maxEquippedWeight = 12; // 2 rows x 6 weight
    [SerializeField] private int weightsPerRow = 6;

    [Header("Registry")]
    [SerializeField] private AbilityRegistry abilityRegistry;

    // Events (C# events for direct subscribers)
    public event Action OnOwnedAbilitiesChanged;
    public event Action OnEquippedAbilitiesChanged;

    // Properties
    public int MaxEquippedWeight => maxEquippedWeight;
    public int WeightsPerRow => weightsPerRow;
    public int MaxRows => 2;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Logger.LogWarning("AbilityManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.General);
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (DataManager.Instance == null)
        {
            Logger.LogError("AbilityManager: DataManager not found!", Logger.LogCategory.General);
            return;
        }

        if (abilityRegistry == null)
        {
            // Try to find it
            abilityRegistry = AbilityRegistry.Instance;
            if (abilityRegistry == null)
            {
                Logger.LogWarning("AbilityManager: AbilityRegistry not assigned and not found!", Logger.LogCategory.General);
            }
        }

        Logger.LogInfo("AbilityManager initialized.", Logger.LogCategory.General);
    }

    // === OWNED ABILITIES ===

    /// <summary>
    /// Get all owned abilities as AbilityDefinition objects
    /// </summary>
    public List<AbilityDefinition> GetOwnedAbilities()
    {
        var playerData = DataManager.Instance?.PlayerData;
        if (playerData == null) return new List<AbilityDefinition>();

        var ownedIds = playerData.OwnedAbilities;
        var result = new List<AbilityDefinition>();

        foreach (var id in ownedIds)
        {
            var ability = GetAbilityDefinition(id);
            if (ability != null)
            {
                result.Add(ability);
            }
        }

        return result;
    }

    /// <summary>
    /// Get owned ability IDs
    /// </summary>
    public List<string> GetOwnedAbilityIds()
    {
        var playerData = DataManager.Instance?.PlayerData;
        return playerData?.OwnedAbilities ?? new List<string>();
    }

    /// <summary>
    /// Check if player owns a specific ability
    /// </summary>
    public bool OwnsAbility(string abilityId)
    {
        var ownedIds = GetOwnedAbilityIds();
        return ownedIds.Contains(abilityId);
    }

    /// <summary>
    /// Add an ability to the player's owned abilities
    /// </summary>
    public void AddOwnedAbility(string abilityId)
    {
        var playerData = DataManager.Instance?.PlayerData;
        if (playerData == null)
        {
            Logger.LogError("AbilityManager: Cannot add ability - PlayerData is null", Logger.LogCategory.General);
            return;
        }

        var ability = GetAbilityDefinition(abilityId);
        if (ability == null)
        {
            Logger.LogError($"AbilityManager: Cannot add ability - '{abilityId}' not found in registry", Logger.LogCategory.General);
            return;
        }

        var owned = playerData.OwnedAbilities;
        owned.Add(abilityId);
        playerData.OwnedAbilities = owned;

        SavePlayerData();

        Logger.LogInfo($"AbilityManager: Added ability '{ability.GetDisplayName()}'", Logger.LogCategory.General);

        // Publish events
        OnOwnedAbilitiesChanged?.Invoke();
        EventBus.Publish(new AbilityAcquiredEvent(abilityId, ability));
        EventBus.Publish(new OwnedAbilitiesChangedEvent(owned));
    }

    /// <summary>
    /// Remove an ability from owned abilities (also unequips if equipped)
    /// </summary>
    public bool RemoveOwnedAbility(string abilityId)
    {
        var playerData = DataManager.Instance?.PlayerData;
        if (playerData == null) return false;

        var owned = playerData.OwnedAbilities;
        if (!owned.Contains(abilityId)) return false;

        // First unequip if equipped
        var equipped = playerData.EquippedAbilities;
        if (equipped.Contains(abilityId))
        {
            equipped.Remove(abilityId);
            playerData.EquippedAbilities = equipped;
            OnEquippedAbilitiesChanged?.Invoke();

            var ability = GetAbilityDefinition(abilityId);
            EventBus.Publish(new AbilityUnequippedEvent(abilityId, ability));
            EventBus.Publish(new EquippedAbilitiesChangedEvent(equipped, GetCurrentEquippedWeight(), maxEquippedWeight));
        }

        // Remove from owned
        owned.Remove(abilityId);
        playerData.OwnedAbilities = owned;

        SavePlayerData();

        OnOwnedAbilitiesChanged?.Invoke();
        EventBus.Publish(new OwnedAbilitiesChangedEvent(owned));

        return true;
    }

    // === EQUIPPED ABILITIES ===

    /// <summary>
    /// Get all equipped abilities as AbilityDefinition objects
    /// </summary>
    public List<AbilityDefinition> GetEquippedAbilities()
    {
        var playerData = DataManager.Instance?.PlayerData;
        if (playerData == null) return new List<AbilityDefinition>();

        var equippedIds = playerData.EquippedAbilities;
        var result = new List<AbilityDefinition>();

        foreach (var id in equippedIds)
        {
            var ability = GetAbilityDefinition(id);
            if (ability != null)
            {
                result.Add(ability);
            }
        }

        return result;
    }

    /// <summary>
    /// Get equipped ability IDs
    /// </summary>
    public List<string> GetEquippedAbilityIds()
    {
        var playerData = DataManager.Instance?.PlayerData;
        return playerData?.EquippedAbilities ?? new List<string>();
    }

    /// <summary>
    /// Check if a specific ability is equipped
    /// </summary>
    public bool IsAbilityEquipped(string abilityId)
    {
        var equippedIds = GetEquippedAbilityIds();
        return equippedIds.Contains(abilityId);
    }

    /// <summary>
    /// Get current total weight of equipped abilities
    /// </summary>
    public int GetCurrentEquippedWeight()
    {
        var equippedAbilities = GetEquippedAbilities();
        return equippedAbilities.Sum(a => a.Weight);
    }

    /// <summary>
    /// Get remaining weight capacity
    /// </summary>
    public int GetRemainingWeight()
    {
        return maxEquippedWeight - GetCurrentEquippedWeight();
    }

    /// <summary>
    /// Check if an ability can be equipped (weight check)
    /// </summary>
    public bool CanEquipAbility(string abilityId)
    {
        // Must own the ability
        if (!OwnsAbility(abilityId)) return false;

        var ability = GetAbilityDefinition(abilityId);
        if (ability == null) return false;

        // Check weight
        int currentWeight = GetCurrentEquippedWeight();
        return (currentWeight + ability.Weight) <= maxEquippedWeight;
    }

    /// <summary>
    /// Try to equip an ability
    /// </summary>
    public bool TryEquipAbility(string abilityId)
    {
        if (!CanEquipAbility(abilityId))
        {
            Logger.LogWarning($"AbilityManager: Cannot equip ability '{abilityId}' - weight limit exceeded or not owned", Logger.LogCategory.General);
            return false;
        }

        var playerData = DataManager.Instance?.PlayerData;
        if (playerData == null) return false;

        var ability = GetAbilityDefinition(abilityId);
        if (ability == null) return false;

        var equipped = playerData.EquippedAbilities;
        equipped.Add(abilityId);
        playerData.EquippedAbilities = equipped;

        SavePlayerData();

        Logger.LogInfo($"AbilityManager: Equipped ability '{ability.GetDisplayName()}' (Weight: {GetCurrentEquippedWeight()}/{maxEquippedWeight})", Logger.LogCategory.General);

        OnEquippedAbilitiesChanged?.Invoke();
        EventBus.Publish(new AbilityEquippedEvent(abilityId, ability));
        EventBus.Publish(new EquippedAbilitiesChangedEvent(equipped, GetCurrentEquippedWeight(), maxEquippedWeight));

        return true;
    }

    /// <summary>
    /// Unequip an ability by its ID (removes first occurrence)
    /// </summary>
    public bool TryUnequipAbility(string abilityId)
    {
        var playerData = DataManager.Instance?.PlayerData;
        if (playerData == null) return false;

        var equipped = playerData.EquippedAbilities;
        if (!equipped.Contains(abilityId)) return false;

        var ability = GetAbilityDefinition(abilityId);

        equipped.Remove(abilityId);
        playerData.EquippedAbilities = equipped;

        SavePlayerData();

        Logger.LogInfo($"AbilityManager: Unequipped ability '{ability?.GetDisplayName() ?? abilityId}' (Weight: {GetCurrentEquippedWeight()}/{maxEquippedWeight})", Logger.LogCategory.General);

        OnEquippedAbilitiesChanged?.Invoke();
        EventBus.Publish(new AbilityUnequippedEvent(abilityId, ability));
        EventBus.Publish(new EquippedAbilitiesChangedEvent(equipped, GetCurrentEquippedWeight(), maxEquippedWeight));

        return true;
    }

    /// <summary>
    /// Unequip an ability by index
    /// </summary>
    public bool TryUnequipAbilityAtIndex(int index)
    {
        var equipped = GetEquippedAbilityIds();
        if (index < 0 || index >= equipped.Count) return false;

        return TryUnequipAbility(equipped[index]);
    }

    /// <summary>
    /// Unequip all abilities
    /// </summary>
    public void UnequipAll()
    {
        var playerData = DataManager.Instance?.PlayerData;
        if (playerData == null) return;

        var equipped = playerData.EquippedAbilities;
        if (equipped.Count == 0) return;

        playerData.EquippedAbilities = new List<string>();

        SavePlayerData();

        Logger.LogInfo("AbilityManager: Unequipped all abilities", Logger.LogCategory.General);

        OnEquippedAbilitiesChanged?.Invoke();
        EventBus.Publish(new EquippedAbilitiesChangedEvent(new List<string>(), 0, maxEquippedWeight));
    }

    // === HELPERS ===

    /// <summary>
    /// Get ability definition from registry
    /// </summary>
    public AbilityDefinition GetAbilityDefinition(string abilityId)
    {
        if (abilityRegistry != null)
        {
            return abilityRegistry.GetAbility(abilityId);
        }

        // Fallback to static instance
        return AbilityRegistry.Instance?.GetAbility(abilityId);
    }

    /// <summary>
    /// Get the AbilityRegistry
    /// </summary>
    public AbilityRegistry GetAbilityRegistry()
    {
        return abilityRegistry ?? AbilityRegistry.Instance;
    }

    private void SavePlayerData()
    {
        DataManager.Instance?.SaveGame();
    }

    // === DEBUG ===

    /// <summary>
    /// Get debug info string
    /// </summary>
    public string GetDebugInfo()
    {
        var owned = GetOwnedAbilityIds();
        var equipped = GetEquippedAbilityIds();
        return $"Owned: {owned.Count}, Equipped: {equipped.Count} ({GetCurrentEquippedWeight()}/{maxEquippedWeight} weight)";
    }

    /// <summary>
    /// Debug: Add all abilities from registry to owned
    /// </summary>
    [ContextMenu("Debug: Add All Abilities")]
    public void DebugAddAllAbilities()
    {
        var registry = GetAbilityRegistry();
        if (registry == null || registry.AllAbilities == null) return;

        foreach (var ability in registry.AllAbilities)
        {
            if (ability != null && !OwnsAbility(ability.AbilityID))
            {
                AddOwnedAbility(ability.AbilityID);
            }
        }
    }

    /// <summary>
    /// Debug: Add test abilities for testing UI
    /// </summary>
    [ContextMenu("Debug: Add Test Abilities")]
    public void DebugAddTestAbilities()
    {
        string[] testAbilityIds = { "basic_attack", "heal", "poison_strike", "venom_strike" };

        foreach (var abilityId in testAbilityIds)
        {
            if (!OwnsAbility(abilityId))
            {
                AddOwnedAbility(abilityId);
            }
        }

        Logger.LogInfo($"AbilityManager: Added test abilities. Owned: {GetOwnedAbilities().Count}", Logger.LogCategory.General);
    }

    /// <summary>
    /// Debug: Clear all owned and equipped abilities
    /// </summary>
    [ContextMenu("Debug: Clear All Abilities")]
    public void DebugClearAllAbilities()
    {
        var playerData = DataManager.Instance?.PlayerData;
        if (playerData == null) return;

        playerData.OwnedAbilities = new List<string>();
        playerData.EquippedAbilities = new List<string>();
        SavePlayerData();

        OnOwnedAbilitiesChanged?.Invoke();
        OnEquippedAbilitiesChanged?.Invoke();

        Logger.LogInfo("AbilityManager: Cleared all abilities", Logger.LogCategory.General);
    }
}
