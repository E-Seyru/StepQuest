// Purpose: Manages player's ability selection, equipping rules (weight limit), and cooldowns during combat.
// Filepath: Assets/Scripts/Gameplay/Combat/AbilityController.cs
using UnityEngine;
using System.Collections.Generic; // For List/Dictionary

// Note: Some logic might overlap or belong in EquipmentManager or AutoBattler.
// This class could focus specifically on the *rules* of ability usage.
public class AbilityController : MonoBehaviour
{
    // TODO: Reference EquipmentManager to get equipped abilities and max weight
    // private EquipmentManager equipmentManager;

    // TODO: Potentially store cooldowns here if not managed by AutoBattler
    // private Dictionary<string, float> currentCooldowns; // AbilityID -> Time remaining

    void Start()
    {
        // TODO: Get reference to EquipmentManager
        // TODO: Subscribe to EquipmentManager.OnAbilitiesChanged event?
    }

    public bool CanEquipAbility(string abilityId)
    {
        // TODO: Get ability definition (from Registry?) to find its weight
        // TODO: Get current total weight and max weight from EquipmentManager
        // TODO: Return true if (currentWeight + abilityWeight <= maxWeight)
        return true; // Placeholder
    }

    // --- Cooldown logic might live in AutoBattler instead ---
    public void PutAbilityOnCooldown(string abilityId)
    {
        // TODO: Get ability definition for cooldown duration
        // TODO: Store cooldown end time or remaining time
        Debug.Log($"AbilityController: Putting {abilityId} on cooldown (Placeholder)");
    }

    public bool IsAbilityReady(string abilityId)
    {
        // TODO: Check if the ability exists in the cooldown tracker and if time is <= 0
        return true; // Placeholder
    }

    public void UpdateCooldowns(float deltaTime)
    {
        // TODO: Iterate through currentCooldowns and decrease time remaining
    }
    // --- End Cooldown Logic ---

    public List<string> GetReadyAbilities()
    {
        // TODO: Get all equipped abilities from EquipmentManager
        // TODO: Filter the list based on IsAbilityReady() check
        // TODO: Return the list of ready ability IDs
        return new List<string>(); // Placeholder
    }
}