// Purpose: Tool to manage connections between locations easily with visual interface
// Filepath: Assets/Scripts/Editor/ConnectionManagerWindow.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ConnectionManagerWindow : EditorWindow
{
    [MenuItem("StepQuest/World/Connection Manager")]
    public static void ShowWindow()
    {
        ConnectionManagerWindow window = GetWindow<ConnectionManagerWindow>();
        window.titleContent = new GUIContent("Connection Manager");
        window.Show();
    }

    // Data
    private LocationRegistry locationRegistry;

    // UI State
    private Vector2 scrollPosition;
    private string searchFilter = "";
    private int selectedTab = 0;
    private readonly string[] tabNames = { "All Connections", "Create Connection", "Validation" };

    // Create Connection Dialog State
    private bool showCreateConnectionDialog = false;
    private MapLocationDefinition fromLocation = null;
    private MapLocationDefinition toLocation = null;
    private int newConnectionStepCost = 50;
    private bool newConnectionIsAvailable = true;
    private string newConnectionRequirements = "";
    private bool createBidirectional = true;

    // Settings
    private bool autoCreateBidirectional = true;

    // Validation cache
    private List<string> validationIssues = new List<string>();
    private DateTime lastValidationTime = DateTime.MinValue;

    void OnEnable()
    {
        LoadLocationRegistry();
        ValidateConnections();

        // Synchroniser les settings
        createBidirectional = autoCreateBidirectional;
    }

    void OnGUI()
    {
        DrawHeader();

        // Tab selection
        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        switch (selectedTab)
        {
            case 0:
                DrawAllConnectionsTab();
                break;
            case 1:
                DrawCreateConnectionTab();
                break;
            case 2:
                DrawValidationTab();
                break;
        }

        EditorGUILayout.EndScrollView();

        // Handle creation dialog
        if (showCreateConnectionDialog)
        {
            DrawCreateConnectionDialog();
        }
    }

    #region Header
    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");

        GUILayout.Label("Connection Manager", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        // Registry selection
        locationRegistry = (LocationRegistry)EditorGUILayout.ObjectField("Location Registry", locationRegistry, typeof(LocationRegistry), false);

        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            LoadLocationRegistry();
            ValidateConnections();
        }

        if (GUILayout.Button("Validate", GUILayout.Width(60)))
        {
            ValidateConnections();
        }

        EditorGUILayout.EndHorizontal();

        // Search
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        EditorGUILayout.EndHorizontal();

        // Quick stats
        if (locationRegistry != null)
        {
            int totalConnections = GetTotalConnectionCount();
            int totalLocations = locationRegistry.AllLocations.Count(l => l != null);
            EditorGUILayout.LabelField($"📊 {totalLocations} Locations • {totalConnections} Connections • {validationIssues.Count} Issues",
                EditorStyles.miniLabel);
        }

        // Settings
        EditorGUILayout.BeginHorizontal();
        autoCreateBidirectional = EditorGUILayout.Toggle("Auto Bidirectional:", autoCreateBidirectional);
        if (autoCreateBidirectional)
        {
            EditorGUILayout.LabelField("↔ (A→B creera automatiquement B→A)", EditorStyles.miniLabel);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }
    #endregion

    #region All Connections Tab
    private void DrawAllConnectionsTab()
    {
        if (locationRegistry == null)
        {
            EditorGUILayout.HelpBox("Select a LocationRegistry to manage connections.", MessageType.Info);
            return;
        }

        // Quick Create button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create New Connection", GUILayout.Width(150)))
        {
            showCreateConnectionDialog = true;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        var filteredLocations = GetFilteredLocations();

        foreach (var location in filteredLocations)
        {
            DrawLocationConnectionsEntry(location);
        }

        if (filteredLocations.Count == 0)
        {
            EditorGUILayout.HelpBox("No locations found.", MessageType.Info);
        }
    }

    private void DrawLocationConnectionsEntry(MapLocationDefinition location)
    {
        if (location?.Connections == null) return;

        EditorGUILayout.BeginVertical("box");

        // Location header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"🏛️ {location.DisplayName}", EditorStyles.boldLabel, GUILayout.Width(200));
        EditorGUILayout.LabelField($"ID: {location.LocationID}", EditorStyles.miniLabel);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Edit Location", GUILayout.Width(80)))
        {
            Selection.activeObject = location;
            EditorGUIUtility.PingObject(location);
        }

        EditorGUILayout.EndHorizontal();

        // Connections
        EditorGUILayout.LabelField($"🔗 Connections ({location.Connections.Count}):", EditorStyles.boldLabel);

        if (location.Connections.Count > 0)
        {
            for (int i = 0; i < location.Connections.Count; i++)
            {
                DrawConnectionEntry(location, i);
            }
        }
        else
        {
            EditorGUILayout.LabelField("  No connections", EditorStyles.miniLabel);
        }

        // Quick add connection
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Quick Add:", GUILayout.Width(70));

        MapLocationDefinition newDestination = (MapLocationDefinition)EditorGUILayout.ObjectField(null, typeof(MapLocationDefinition), false, GUILayout.Width(150));

        if (newDestination != null)
        {
            QuickAddConnection(location, newDestination);
        }

        // Bouton pour creer une connection unidirectionnelle si auto-bidirectional est active
        if (autoCreateBidirectional && newDestination != null)
        {
            if (GUILayout.Button("→", GUILayout.Width(20)))
            {
                QuickAddConnectionUnidirectional(location, newDestination);
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawConnectionEntry(MapLocationDefinition fromLocation, int index)
    {
        var connection = fromLocation.Connections[index];

        EditorGUILayout.BeginVertical("helpBox");
        EditorGUILayout.BeginHorizontal();

        // Connection info
        var destination = locationRegistry.GetLocationById(connection.DestinationLocationID);
        string destinationName = destination?.DisplayName ?? $"[Missing: {connection.DestinationLocationID}]";

        EditorGUILayout.LabelField($"  → {destinationName}", EditorStyles.miniLabel, GUILayout.Width(150));
        EditorGUILayout.LabelField($"{connection.StepCost} steps", EditorStyles.miniLabel, GUILayout.Width(80));

        // Available toggle
        connection.IsAvailable = EditorGUILayout.Toggle("", connection.IsAvailable, GUILayout.Width(20));

        // Status indicator
        if (!connection.IsAvailable)
        {
            EditorGUILayout.LabelField("❌", GUILayout.Width(20));
        }
        else if (destination == null)
        {
            EditorGUILayout.LabelField("⚠️", GUILayout.Width(20));
        }
        else
        {
            EditorGUILayout.LabelField("✅", GUILayout.Width(20));
        }

        GUILayout.FlexibleSpace();

        // Edit/Remove buttons
        if (GUILayout.Button("Edit", GUILayout.Width(40)))
        {
            EditConnection(fromLocation, index);
        }

        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            RemoveConnection(fromLocation, index);
        }

        EditorGUILayout.EndHorizontal();

        // Requirements if any
        if (!string.IsNullOrEmpty(connection.Requirements))
        {
            EditorGUILayout.LabelField($"    Requirements: {connection.Requirements}", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }
    #endregion

    #region Create Connection Tab
    private void DrawCreateConnectionTab()
    {
        if (locationRegistry == null)
        {
            EditorGUILayout.HelpBox("Select a LocationRegistry to create connections.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Create New Connection", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // From location
        EditorGUILayout.LabelField("From Location:");
        fromLocation = (MapLocationDefinition)EditorGUILayout.ObjectField(fromLocation, typeof(MapLocationDefinition), false);

        // To location  
        EditorGUILayout.LabelField("To Location:");
        toLocation = (MapLocationDefinition)EditorGUILayout.ObjectField(toLocation, typeof(MapLocationDefinition), false);

        EditorGUILayout.Space();

        // Connection settings
        EditorGUILayout.LabelField("Connection Settings:", EditorStyles.boldLabel);
        newConnectionStepCost = EditorGUILayout.IntField("Step Cost:", newConnectionStepCost);
        newConnectionIsAvailable = EditorGUILayout.Toggle("Is Available:", newConnectionIsAvailable);
        newConnectionRequirements = EditorGUILayout.TextField("Requirements:", newConnectionRequirements);

        EditorGUILayout.Space();
        createBidirectional = EditorGUILayout.Toggle("Create Bidirectional:", createBidirectional);

        if (createBidirectional)
        {
            EditorGUILayout.HelpBox("This will create connections in both directions (A→B and B→A)", MessageType.Info);
        }

        // Synchroniser avec le setting global
        if (createBidirectional != autoCreateBidirectional)
        {
            autoCreateBidirectional = createBidirectional;
        }

        EditorGUILayout.Space();

        // Create button
        GUI.enabled = fromLocation != null && toLocation != null && fromLocation != toLocation;

        if (GUILayout.Button("Create Connection", GUILayout.Height(30)))
        {
            CreateConnection();
        }

        GUI.enabled = true;

        // Connection preview
        if (fromLocation != null && toLocation != null)
        {
            if (fromLocation == toLocation)
            {
                EditorGUILayout.HelpBox("❌ Cannot create connection to the same location", MessageType.Error);
            }
            else
            {
                DrawConnectionPreview();
            }
        }
    }

    private void DrawConnectionPreview()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview:", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        string arrow = createBidirectional ? "↔" : "→";
        EditorGUILayout.LabelField($"🔗 {fromLocation.DisplayName} {arrow} {toLocation.DisplayName}");
        EditorGUILayout.LabelField($"   Cost: {newConnectionStepCost} steps");
        EditorGUILayout.LabelField($"   Available: {(newConnectionIsAvailable ? "Yes" : "No")}");

        if (!string.IsNullOrEmpty(newConnectionRequirements))
        {
            EditorGUILayout.LabelField($"   Requirements: {newConnectionRequirements}");
        }

        EditorGUILayout.EndVertical();
    }
    #endregion

    #region Validation Tab
    private void DrawValidationTab()
    {
        if (locationRegistry == null)
        {
            EditorGUILayout.HelpBox("Select a LocationRegistry to validate connections.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Connection Validation", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Re-validate", GUILayout.Width(80)))
        {
            ValidateConnections();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"Last validation: {lastValidationTime:HH:mm:ss}", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        if (validationIssues.Count == 0)
        {
            EditorGUILayout.HelpBox("✅ All connections are valid!", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox($"❌ Found {validationIssues.Count} issue(s):", MessageType.Warning);

            foreach (string issue in validationIssues)
            {
                EditorGUILayout.LabelField($"• {issue}", EditorStyles.wordWrappedLabel);
            }
        }

        EditorGUILayout.Space();
        DrawConnectionStatistics();
    }

    private void DrawConnectionStatistics()
    {
        EditorGUILayout.LabelField("Connection Statistics:", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        if (locationRegistry?.AllLocations != null)
        {
            int totalLocations = locationRegistry.AllLocations.Count(l => l != null);
            int totalConnections = GetTotalConnectionCount();
            int bidirectionalConnections = CountBidirectionalConnections();
            int unidirectionalConnections = totalConnections - (bidirectionalConnections * 2);
            int isolatedLocations = CountIsolatedLocations();

            EditorGUILayout.LabelField($"📍 Total Locations: {totalLocations}");
            EditorGUILayout.LabelField($"🔗 Total Connections: {totalConnections}");
            EditorGUILayout.LabelField($"↔ Bidirectional: {bidirectionalConnections}");
            EditorGUILayout.LabelField($"→ Unidirectional: {unidirectionalConnections}");
            EditorGUILayout.LabelField($"🏝️ Isolated Locations: {isolatedLocations}");
        }

        EditorGUILayout.EndVertical();
    }
    #endregion

    #region Create Connection Dialog
    private void DrawCreateConnectionDialog()
    {
        GUILayout.BeginArea(new Rect(50, 100, 500, 300));
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField("Quick Create Connection", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Same content as Create tab but in dialog form
        fromLocation = (MapLocationDefinition)EditorGUILayout.ObjectField("From:", fromLocation, typeof(MapLocationDefinition), false);
        toLocation = (MapLocationDefinition)EditorGUILayout.ObjectField("To:", toLocation, typeof(MapLocationDefinition), false);

        newConnectionStepCost = EditorGUILayout.IntField("Step Cost:", newConnectionStepCost);
        newConnectionIsAvailable = EditorGUILayout.Toggle("Available:", newConnectionIsAvailable);
        createBidirectional = EditorGUILayout.Toggle("Bidirectional:", createBidirectional);

        // Info sur l'etat du setting global
        if (autoCreateBidirectional)
        {
            EditorGUILayout.LabelField("ℹ️ Auto bidirectional is ON", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        GUI.enabled = fromLocation != null && toLocation != null && fromLocation != toLocation;
        if (GUILayout.Button("Create"))
        {
            CreateConnection();
            showCreateConnectionDialog = false;
        }
        GUI.enabled = true;

        if (GUILayout.Button("Cancel"))
        {
            showCreateConnectionDialog = false;
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }
    #endregion

    #region Logic Methods
    private void LoadLocationRegistry()
    {
        if (locationRegistry == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:LocationRegistry");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                locationRegistry = AssetDatabase.LoadAssetAtPath<LocationRegistry>(path);
            }
        }
    }

    private List<MapLocationDefinition> GetFilteredLocations()
    {
        if (locationRegistry == null) return new List<MapLocationDefinition>();

        var locations = locationRegistry.AllLocations.Where(l => l != null);

        if (!string.IsNullOrEmpty(searchFilter))
        {
            locations = locations.Where(l =>
                l.DisplayName.ToLower().Contains(searchFilter.ToLower()) ||
                l.LocationID.ToLower().Contains(searchFilter.ToLower()));
        }

        return locations.ToList();
    }

    private void QuickAddConnection(MapLocationDefinition from, MapLocationDefinition to)
    {
        if (from == to)
        {
            EditorUtility.DisplayDialog("Invalid Connection", "Cannot create connection to the same location.", "OK");
            return;
        }

        // Check if connection already exists
        bool connectionExists = from.Connections.Any(c => c.DestinationLocationID == to.LocationID);
        if (connectionExists)
        {
            EditorUtility.DisplayDialog("Connection Exists", $"Connection from '{from.DisplayName}' to '{to.DisplayName}' already exists.", "OK");
            return;
        }

        // Create primary connection with default values
        var newConnection = new LocationConnection
        {
            DestinationLocationID = to.LocationID,
            StepCost = 50,
            IsAvailable = true,
            Requirements = ""
        };

        from.Connections.Add(newConnection);
        EditorUtility.SetDirty(from);

        string logMessage = $"✅ Created connection: {from.DisplayName} → {to.DisplayName} (50 steps)";

        // ⭐ NOUVEAU : Creer automatiquement la connection inverse si active
        if (autoCreateBidirectional)
        {
            // Verifier que la connection inverse n'existe pas deja
            bool reverseExists = to.Connections.Any(c => c.DestinationLocationID == from.LocationID);

            if (!reverseExists)
            {
                var reverseConnection = new LocationConnection
                {
                    DestinationLocationID = from.LocationID,
                    StepCost = 50,
                    IsAvailable = true,
                    Requirements = ""
                };

                to.Connections.Add(reverseConnection);
                EditorUtility.SetDirty(to);

                logMessage += $"\n✅ Auto-created reverse connection: {to.DisplayName} → {from.DisplayName}";
            }
            else
            {
                logMessage += $"\n⚠️ Reverse connection already exists: {to.DisplayName} → {from.DisplayName}";
            }
        }

        AssetDatabase.SaveAssets();
        Logger.LogInfo(logMessage, Logger.LogCategory.EditorLog);
        ValidateConnections(); // Refresh validation
    }

    private void QuickAddConnectionUnidirectional(MapLocationDefinition from, MapLocationDefinition to)
    {
        if (from == to)
        {
            EditorUtility.DisplayDialog("Invalid Connection", "Cannot create connection to the same location.", "OK");
            return;
        }

        // Check if connection already exists
        bool connectionExists = from.Connections.Any(c => c.DestinationLocationID == to.LocationID);
        if (connectionExists)
        {
            EditorUtility.DisplayDialog("Connection Exists", $"Connection from '{from.DisplayName}' to '{to.DisplayName}' already exists.", "OK");
            return;
        }

        // Create only unidirectional connection
        var newConnection = new LocationConnection
        {
            DestinationLocationID = to.LocationID,
            StepCost = 50,
            IsAvailable = true,
            Requirements = ""
        };

        from.Connections.Add(newConnection);
        EditorUtility.SetDirty(from);
        AssetDatabase.SaveAssets();

        Logger.LogInfo($"✅ Created UNIDIRECTIONAL connection: {from.DisplayName} → {to.DisplayName} (50 steps, Logger.LogCategory.EditorLog)");
        ValidateConnections(); // Refresh validation
    }

    private void CreateConnection()
    {
        try
        {
            // Create primary connection
            var connection1 = new LocationConnection
            {
                DestinationLocationID = toLocation.LocationID,
                StepCost = newConnectionStepCost,
                IsAvailable = newConnectionIsAvailable,
                Requirements = newConnectionRequirements
            };

            fromLocation.Connections.Add(connection1);
            EditorUtility.SetDirty(fromLocation);

            string logMessage = $"✅ Created connection: {fromLocation.DisplayName} → {toLocation.DisplayName} ({newConnectionStepCost} steps)";

            // Create reverse connection if bidirectional
            if (createBidirectional)
            {
                var connection2 = new LocationConnection
                {
                    DestinationLocationID = fromLocation.LocationID,
                    StepCost = newConnectionStepCost,
                    IsAvailable = newConnectionIsAvailable,
                    Requirements = newConnectionRequirements
                };

                toLocation.Connections.Add(connection2);
                EditorUtility.SetDirty(toLocation);

                logMessage += $"\n✅ Created reverse connection: {toLocation.DisplayName} → {fromLocation.DisplayName}";
            }

            AssetDatabase.SaveAssets();
            Logger.LogInfo(logMessage, Logger.LogCategory.EditorLog);

            // Reset form
            fromLocation = null;
            toLocation = null;
            newConnectionStepCost = 50;
            newConnectionRequirements = "";

            ValidateConnections(); // Refresh validation
        }
        catch (Exception ex)
        {
            Logger.LogError($"❌ Error creating connection: {ex.Message}", Logger.LogCategory.EditorLog);
            EditorUtility.DisplayDialog("Error", $"Failed to create connection:\n{ex.Message}", "OK");
        }
    }

    private void EditConnection(MapLocationDefinition location, int index)
    {
        // For now, just select the location for manual editing
        // Could be enhanced with a dedicated edit dialog
        Selection.activeObject = location;
        EditorGUIUtility.PingObject(location);
        Logger.LogInfo($"📝 Edit connection {index} from {location.DisplayName} in Inspector", Logger.LogCategory.EditorLog);
    }

    private void RemoveConnection(MapLocationDefinition location, int index)
    {
        if (index >= 0 && index < location.Connections.Count)
        {
            var connection = location.Connections[index];
            var destination = locationRegistry.GetLocationById(connection.DestinationLocationID);
            string destinationName = destination?.DisplayName ?? connection.DestinationLocationID;

            bool confirm = EditorUtility.DisplayDialog(
                "Remove Connection",
                $"Remove connection from '{location.DisplayName}' to '{destinationName}'?",
                "Remove", "Cancel");

            if (confirm)
            {
                location.Connections.RemoveAt(index);
                EditorUtility.SetDirty(location);
                AssetDatabase.SaveAssets();

                Logger.LogInfo($"🗑️ Removed connection: {location.DisplayName} → {destinationName}", Logger.LogCategory.EditorLog);
                ValidateConnections(); // Refresh validation
            }
        }
    }

    private void ValidateConnections()
    {
        validationIssues.Clear();
        lastValidationTime = DateTime.Now;

        if (locationRegistry?.AllLocations == null) return;

        foreach (var location in locationRegistry.AllLocations.Where(l => l != null))
        {
            if (location.Connections == null) continue;

            foreach (var connection in location.Connections)
            {
                // Check for missing destination
                if (string.IsNullOrEmpty(connection.DestinationLocationID))
                {
                    validationIssues.Add($"'{location.DisplayName}' has connection with empty destination");
                    continue;
                }

                var destination = locationRegistry.GetLocationById(connection.DestinationLocationID);
                if (destination == null)
                {
                    validationIssues.Add($"'{location.DisplayName}' connects to missing location '{connection.DestinationLocationID}'");
                }

                // Check for invalid step cost
                if (connection.StepCost <= 0)
                {
                    string destName = destination?.DisplayName ?? connection.DestinationLocationID;
                    validationIssues.Add($"'{location.DisplayName}' → '{destName}' has invalid step cost ({connection.StepCost})");
                }
            }
        }
    }

    private int GetTotalConnectionCount()
    {
        if (locationRegistry?.AllLocations == null) return 0;

        return locationRegistry.AllLocations
            .Where(l => l?.Connections != null)
            .Sum(l => l.Connections.Count);
    }

    private int CountBidirectionalConnections()
    {
        if (locationRegistry?.AllLocations == null) return 0;

        int bidirectionalCount = 0;
        var processedPairs = new HashSet<string>();

        foreach (var location in locationRegistry.AllLocations.Where(l => l?.Connections != null))
        {
            foreach (var connection in location.Connections)
            {
                string pairKey = $"{location.LocationID}-{connection.DestinationLocationID}";
                string reversePairKey = $"{connection.DestinationLocationID}-{location.LocationID}";

                if (processedPairs.Contains(pairKey) || processedPairs.Contains(reversePairKey))
                    continue;

                // Check if reverse connection exists
                var destination = locationRegistry.GetLocationById(connection.DestinationLocationID);
                bool hasReverse = destination?.Connections?.Any(c => c.DestinationLocationID == location.LocationID) == true;

                if (hasReverse)
                {
                    bidirectionalCount++;
                    processedPairs.Add(pairKey);
                    processedPairs.Add(reversePairKey);
                }
            }
        }

        return bidirectionalCount;
    }

    private int CountIsolatedLocations()
    {
        if (locationRegistry?.AllLocations == null) return 0;

        return locationRegistry.AllLocations
            .Where(l => l != null)
            .Count(location =>
                (location.Connections?.Count ?? 0) == 0 &&
                !HasIncomingConnections(location.LocationID));
    }

    private bool HasIncomingConnections(string locationId)
    {
        return locationRegistry.AllLocations
            .Where(l => l?.Connections != null)
            .Any(l => l.Connections.Any(c => c.DestinationLocationID == locationId));
    }
    #endregion
}
#endif