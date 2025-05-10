// Filepath: Assets/Scripts/Data/Models/MapLocationDefinition.cs
using UnityEngine;
using System.Collections.Generic; // Needed for List

// This attribute allows us to create instances of this class from the Unity Editor menu
[CreateAssetMenu(fileName = "NewMapLocation", menuName = "WalkAndRPG/Map Location Definition")]
public class MapLocationDefinition : ScriptableObject // Inherit from ScriptableObject
{
    [Tooltip("Unique string identifier for this location (e.g., 'HomeBase', 'Forest_Entrance_01')")]
    public string LocationID; // Unique identifier

    [Tooltip("Name displayed to the player")]
    public string DisplayName;

    [Tooltip("Description shown in UI (optional)")]
    [TextArea] public string Description;

    // Define the connections to other locations
    public List<MapConnection> Connections;

    // TODO: Add other location properties later (Type, NPCs, Actions, etc.)
    // public LocationType Type;
    // public List<string> NpcIDsPresent;
}

// We need a small helper class/struct to define a connection
// Needs [System.Serializable] so Unity can display and save it in the Inspector
[System.Serializable]
public class MapConnection
{
    [Tooltip("The LocationID of the destination")]
    public string DestinationLocationID; // Reference MapLocationDefinition by its ID

    [Tooltip("Steps required to travel to this destination")]
    public int StepCost;
}

// --- Keep the enums and other classes from the original MapLocationData.cs if needed ---
// [System.Serializable]
// public class PlayerLocationProgress { ... }
// public enum LocationType { ... }