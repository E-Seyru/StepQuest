// Purpose: Data structure representing a combat ability (definition).
// Filepath: Assets/Scripts/Data/Models/AbilityData.cs
// Use ScriptableObjects for Ability definitions
// Create -> WalkAndRPG -> Ability Definition
// using UnityEngine;
// using System.Collections.Generic; // For effects list

// [CreateAssetMenu(fileName = "NewAbility", menuName = "WalkAndRPG/Ability Definition")]
// public class AbilityDefinition : ScriptableObject
// {
//     public string AbilityID; // Unique identifier
//     public string DisplayName;
//     [TextArea] public string Description;
//     // public Sprite Icon;
//
//     public AbilityType Type; // Damage, Heal, Buff, Debuff, etc.
//     public float CooldownSeconds;
//     public int Weight; // Cost to equip
//
//     // TODO: Define parameters for the ability's effect
//     // Examples:
//     // public float BaseDamage;
//     // public float HealAmount;
//     // public float DurationSeconds; // For buffs/debuffs
//     // public string TargetStat; // For buffs/debuffs
//     // public float StatModifier; // For buffs/debuffs
//
//     // Could use a list of Effect structures/classes for complex abilities
//     // public List<AbilityEffect> Effects;
// }

// May not need a separate *Data* class if definitions are ScriptableObjects
// and players just equip the definition ID. If abilities can be leveled up
// or have instance-specific data, then a separate data class would be needed.

// Example:
// [System.Serializable]
// public class PlayerAbilityData
// {
//    public string AbilityID; // Reference to AbilityDefinition
//    public int CurrentLevel; // If abilities can level up
// }

public enum AbilityType
{
    Damage,
    Heal,
    Shield,
    Buff_Attack,
    Buff_Defense,
    Debuff_Poison,
    Debuff_Defense,
    // Add more specific types as needed
}