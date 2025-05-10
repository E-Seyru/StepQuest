// Purpose: Data structure representing a monster type (definition).
// Filepath: Assets/Scripts/Data/Models/MonsterData.cs
// Use ScriptableObjects for Monster definitions
// Create -> WalkAndRPG -> Monster Definition
using UnityEngine;
using System.Collections.Generic; // For abilities/loot lists

// [CreateAssetMenu(fileName = "NewMonster", menuName = "WalkAndRPG/Monster Definition")]
public class MonsterDefinition : ScriptableObject
{
   public string MonsterID; // Unique identifier
   public string DisplayName;
   public Sprite Sprite; // Assign in Inspector

    // TODO: Define base stats (HP, Attack, Defense, Speed, etc.)
 public int MaxHP;
 public int AttackPower;
 public int Defense;
//
//     // TODO: Define abilities the monster can use (Reference AbilityDefinitions)
// public List<AbilityDefinition> Abilities;
//
//     // TODO: Define loot table (Item drops with chances)
 // public List<LootDrop> LootTable;
//
//     // TODO: Define XP reward for defeating
 public int CombatExperienceReward;
 }

// Placeholder structure for loot drops
// [System.Serializable]
// public class LootDrop {
//     public ItemDefinition Item; // Reference ItemDefinition ScriptableObject
//     public int MinQuantity = 1;
//     public int MaxQuantity = 1;
//     [Range(0f, 1f)] public float DropChance = 1.0f; // 0.0 to 1.0 probability
// }