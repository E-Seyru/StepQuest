﻿// Filepath: Assets/Scripts/Data/Registry/LocationRegistry.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "LocationRegistry", menuName = "WalkAndRPG/Location Registry")]
public class LocationRegistry : ScriptableObject
{
    [Header("All Map Locations")]
    [Tooltip("Drag all your MapLocationDefinition assets here")]
    public List<MapLocationDefinition> AllLocations = new List<MapLocationDefinition>();

    [Header("Debug Info")]
    [Tooltip("Shows validation errors if any")]
    [TextArea(3, 6)]
    public string ValidationStatus = "Click 'Validate Registry' to check for issues";

    /// <summary>
    /// Find a location by its ID
    /// </summary>
    public MapLocationDefinition GetLocationById(string locationId)
    {
        if (string.IsNullOrEmpty(locationId))
        {
            Logger.LogError("LocationRegistry: GetLocationById called with null/empty ID");
            return null;
        }

        var location = AllLocations.FirstOrDefault(loc => loc != null && loc.LocationID == locationId);

        if (location == null)
        {
            Logger.LogError($"LocationRegistry: Location with ID '{locationId}' not found!");
        }

        return location;
    }

    /// <summary>
    /// Check if a location exists
    /// </summary>
    public bool HasLocation(string locationId)
    {
        return GetLocationById(locationId) != null;
    }

    /// <summary>
    /// Get all locations that can be reached from the given location
    /// </summary>
    public List<MapLocationDefinition> GetConnectedLocations(string fromLocationId)
    {
        var fromLocation = GetLocationById(fromLocationId);
        if (fromLocation == null) return new List<MapLocationDefinition>();

        var connectedLocations = new List<MapLocationDefinition>();

        foreach (var connection in fromLocation.Connections)
        {
            var destination = GetLocationById(connection.DestinationLocationID);
            if (destination != null)
            {
                connectedLocations.Add(destination);
            }
        }

        return connectedLocations;
    }

    /// <summary>
    /// Get the step cost to travel from one location to another (if connected)
    /// </summary>
    public int GetTravelCost(string fromLocationId, string toLocationId)
    {
        var fromLocation = GetLocationById(fromLocationId);
        if (fromLocation == null) return -1;

        var connection = fromLocation.Connections
            .FirstOrDefault(conn => conn.DestinationLocationID == toLocationId);

        return connection?.StepCost ?? -1; // -1 means not connected
    }

    /// <summary>
    /// Check if travel is possible between two locations
    /// </summary>
    public bool CanTravelBetween(string fromLocationId, string toLocationId)
    {
        return GetTravelCost(fromLocationId, toLocationId) > 0;
    }

    /// <summary>
    /// Validate the registry for common issues (call this in editor)
    /// </summary>
    [ContextMenu("Validate Registry")]
    public void ValidateRegistry()
    {
        var issues = new List<string>();

        // Check for null locations
        var nullCount = AllLocations.Count(loc => loc == null);
        if (nullCount > 0)
        {
            issues.Add($"{nullCount} null location(s) in registry");
        }

        // Check for duplicate IDs
        var validLocations = AllLocations.Where(loc => loc != null).ToList();
        var duplicateIds = validLocations
            .GroupBy(loc => loc.LocationID)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        foreach (var duplicateId in duplicateIds)
        {
            issues.Add($"Duplicate Location ID: '{duplicateId}'");
        }

        // Check for empty/null IDs
        var emptyIds = validLocations.Where(loc => string.IsNullOrEmpty(loc.LocationID));
        foreach (var location in emptyIds)
        {
            issues.Add($"Location '{location.name}' has empty LocationID");
        }

        // Check for broken connections
        foreach (var location in validLocations)
        {
            foreach (var connection in location.Connections)
            {
                if (string.IsNullOrEmpty(connection.DestinationLocationID))
                {
                    issues.Add($"'{location.LocationID}' has connection with empty destination");
                }
                else if (!HasLocation(connection.DestinationLocationID))
                {
                    issues.Add($"'{location.LocationID}' connects to missing location '{connection.DestinationLocationID}'");
                }

                if (connection.StepCost <= 0)
                {
                    issues.Add($"'{location.LocationID}' has invalid step cost ({connection.StepCost}) to '{connection.DestinationLocationID}'");
                }
            }
        }

        // Update validation status
        if (issues.Count == 0)
        {
            ValidationStatus = "Registry validation passed!\n" +
                             $"Found {validLocations.Count} valid location(s).";
        }
        else
        {
            ValidationStatus = "Registry validation failed:\n\n" +
                             string.Join("\n", issues);
        }

        Logger.LogInfo($"LocationRegistry: Validation complete. {issues.Count} issue(s) found.");
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-validate when something changes in the editor
        ValidateRegistry();
    }
#endif
}