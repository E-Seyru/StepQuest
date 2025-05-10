// Purpose: Data structure representing a location on the world map (definition).
// Filepath: Assets/Scripts/Data/Models/MapLocationData.cs
// Use ScriptableObjects for Map Location definitions
// Create -> WalkAndRPG -> Map Location Definition
// using UnityEngine;
// using System.Collections.Generic; // For lists of available actions

// [CreateAssetMenu(fileName = "NewMapLocation", menuName = "WalkAndRPG/Map Location Definition")]
// public class MapLocationDefinition : ScriptableObject
// {
//     public string LocationID; // Unique identifier
//     public string DisplayName;
//     [TextArea] public string Description;
//     public LocationType Type; // Town, Forest, Mine, etc.
//
//     // TODO: Define connections to adjacent locations and step cost
//     // public List<MapConnection> Connections;
//
//     // TODO: Define available activities (Gathering spots, NPCs, Combat Zones)
//     // public List<string> NpcIDsPresent;
//     // public List<GatheringSpot> GatheringSpots;
//     // public List<CombatZone> CombatZones;
//
//     // TODO: Define exploration properties (total discoverables, completion reward)
//     // public int TotalExplorationTargets;
// }

// This class holds the player's progress specific to a location
[System.Serializable]
public class PlayerLocationProgress
{
    public string LocationID; // Reference to MapLocationDefinition
    public bool IsUnlocked;
    public float ExplorationProgress; // e.g., 0.0 to 1.0
    // TODO: Store discovered elements within the location (e.g., specific NPCs met, nodes found)
    // public HashSet<string> DiscoveredElements;

    public PlayerLocationProgress(string locationId, bool unlocked = false)
    {
        LocationID = locationId;
        IsUnlocked = unlocked;
        ExplorationProgress = 0f;
        // DiscoveredElements = new HashSet<string>();
    }
}


public enum LocationType
{
    Town,
    Forest,
    Mountain,
    Cave,
    Mine,
    FishingSpot,
    Road,
    // Add more as needed
}

// Placeholder structures for connections, gathering, combat zones
// [System.Serializable]
// public class MapConnection {
//     public MapLocationDefinition Destination;
//     public int StepCost;
// }
// [System.Serializable]
// public class GatheringSpot {
//     public SkillType RequiredSkill; // e.g., Mining, Woodcutting
//     public int MinSkillLevel;
//     public List<LootDrop> ResourceTable; // Use LootDrop structure for potential resources
// }
// [System.Serializable]
// public class CombatZone {
//     public string ZoneName;
//     public List<MonsterDefinition> PossibleMonsters; // Monsters that can spawn here
//     public bool AllowLooping; // Can battles be looped here?
// }