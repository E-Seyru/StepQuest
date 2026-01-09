// Purpose: Dedicated editor window for managing POIs (Points of Interest) in the world map
// Filepath: Assets/Scripts/Editor/POIManagerWindow.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Dedicated editor window for managing POIs separately from Activities.
/// Handles POI creation, configuration, and location linking.
/// </summary>
public class POIManagerWindow : EditorWindow
{
    [MenuItem("StepQuest/World/POI Manager")]
    public static void ShowWindow()
    {
        POIManagerWindow window = GetWindow<POIManagerWindow>();
        window.titleContent = new GUIContent("POI Manager");
        window.minSize = new Vector2(500, 400);
        window.Show();
    }

    // Data
    private LocationRegistry locationRegistry;
    private POI[] allPOIs;
    private Dictionary<string, MapLocationDefinition> locationLookup = new Dictionary<string, MapLocationDefinition>();

    // UI State
    private Vector2 scrollPosition;
    private string searchFilter = "";
    private int selectedTab = 0;
    private readonly string[] tabNames = { "POI List", "Create POI", "Validation" };

    // POI List State
    private POI selectedPOI;
    private bool[] poiFoldouts;

    // Create POI State
    private string newPOIName = "";
    private string newLocationID = "";
    private bool useExistingLocation = true;
    private MapLocationDefinition selectedLocation;
    private Vector2 poiSize = new Vector2(100, 100);
    private Sprite poiSprite;
    private Color poiColor = Color.white;
    private bool createTravelStartPoint = true;
    private Vector2 travelStartOffset = new Vector2(0.5f, 0.5f);
    private bool usePrefab = false;
    private GameObject poiPrefab;

    // Create Location State (for new locations)
    private string newLocationName = "";
    private string newLocationDescription = "";

    // Validation State
    private List<ValidationIssue> validationIssues = new List<ValidationIssue>();

    private class ValidationIssue
    {
        public string Description;
        public MessageType Severity;
        public POI RelatedPOI;
        public MapLocationDefinition RelatedLocation;
        public System.Action FixAction;
    }

    void OnEnable()
    {
        LoadRegistry();
        RefreshPOIList();
    }

    void OnFocus()
    {
        RefreshPOIList();
    }

    void OnGUI()
    {
        DrawHeader();

        selectedTab = GUILayout.Toolbar(selectedTab, tabNames);
        EditorGUILayout.Space();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        switch (selectedTab)
        {
            case 0:
                DrawPOIListTab();
                break;
            case 1:
                DrawCreatePOITab();
                break;
            case 2:
                DrawValidationTab();
                break;
        }

        EditorGUILayout.EndScrollView();
    }

    #region Header
    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("POI Manager", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Refresh", GUILayout.Width(70)))
        {
            LoadRegistry();
            RefreshPOIList();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        locationRegistry = (LocationRegistry)EditorGUILayout.ObjectField("Location Registry", locationRegistry, typeof(LocationRegistry), false);
        EditorGUILayout.EndHorizontal();

        // Search (for POI List tab)
        if (selectedTab == 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                searchFilter = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }
    #endregion

    #region POI List Tab
    private void DrawPOIListTab()
    {
        if (allPOIs == null || allPOIs.Length == 0)
        {
            EditorGUILayout.HelpBox("No POIs found in the current scene. Make sure your scene contains POI objects or create one using the 'Create POI' tab.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Scan Scene for POIs", GUILayout.Height(30)))
            {
                RefreshPOIList();
            }
            if (GUILayout.Button("Create New POI", GUILayout.Height(30)))
            {
                selectedTab = 1; // Switch to Create POI tab
            }
            EditorGUILayout.EndHorizontal();
            return;
        }

        // Stats bar
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField($"POIs Found: {allPOIs.Length}", EditorStyles.boldLabel);

        int linkedCount = allPOIs.Count(p => !string.IsNullOrEmpty(p.LocationID) && locationLookup.ContainsKey(p.LocationID));
        int unlinkedCount = allPOIs.Length - linkedCount;

        GUIStyle linkedStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.green } };
        GUIStyle unlinkedStyle = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = Color.yellow } };

        EditorGUILayout.LabelField($"Linked: {linkedCount}", linkedStyle, GUILayout.Width(80));
        if (unlinkedCount > 0)
        {
            EditorGUILayout.LabelField($"Unlinked: {unlinkedCount}", unlinkedStyle, GUILayout.Width(80));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Filter POIs
        var filteredPOIs = FilterPOIs();

        // Initialize foldouts if needed
        if (poiFoldouts == null || poiFoldouts.Length != filteredPOIs.Length)
        {
            poiFoldouts = new bool[filteredPOIs.Length];
        }

        for (int i = 0; i < filteredPOIs.Length; i++)
        {
            DrawPOIEntry(filteredPOIs[i], i);
        }
    }

    private void DrawPOIEntry(POI poi, int index)
    {
        if (poi == null) return;

        var location = GetLocationForPOI(poi);
        bool hasLocation = location != null;
        bool isSelected = selectedPOI == poi;

        // Color coding based on status
        Color bgColor = hasLocation ? new Color(0.2f, 0.4f, 0.2f, 0.3f) : new Color(0.5f, 0.3f, 0.1f, 0.3f);
        if (isSelected) bgColor = new Color(0.3f, 0.5f, 0.7f, 0.5f);

        GUI.backgroundColor = bgColor;
        EditorGUILayout.BeginVertical("box");
        GUI.backgroundColor = Color.white;

        // Header row
        EditorGUILayout.BeginHorizontal();

        // Foldout
        poiFoldouts[index] = EditorGUILayout.Foldout(poiFoldouts[index], "", true);

        // Status icon
        string statusIcon = hasLocation ? "d_winbtn_mac_max" : "d_winbtn_mac_min";
        GUIContent iconContent = EditorGUIUtility.IconContent(statusIcon);
        GUILayout.Label(iconContent, GUILayout.Width(20), GUILayout.Height(20));

        // POI Name
        EditorGUILayout.LabelField(poi.gameObject.name, EditorStyles.boldLabel, GUILayout.Width(180));

        // Location ID
        string locationLabel = hasLocation ? $"{location.DisplayName}" : $"[{poi.LocationID}] - Not Found";
        GUIStyle locationStyle = new GUIStyle(EditorStyles.miniLabel);
        locationStyle.normal.textColor = hasLocation ? Color.white : Color.yellow;
        EditorGUILayout.LabelField(locationLabel, locationStyle);

        GUILayout.FlexibleSpace();

        // Quick actions
        if (GUILayout.Button("Select", GUILayout.Width(50)))
        {
            Selection.activeObject = poi.gameObject;
            EditorGUIUtility.PingObject(poi.gameObject);
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        // Delete button
        GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            DeletePOIWithConfirmation(poi);
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        // Expanded content
        if (poiFoldouts[index])
        {
            EditorGUI.indentLevel++;
            DrawPOIDetails(poi, location);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    private void DrawPOIDetails(POI poi, MapLocationDefinition location)
    {
        EditorGUILayout.Space(5);

        // Position info
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Position:", GUILayout.Width(80));
        EditorGUILayout.LabelField($"({poi.transform.position.x:F1}, {poi.transform.position.y:F1})", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();

        // Location ID field (editable)
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Location ID:", GUILayout.Width(80));

        string newLocationID = EditorGUILayout.TextField(poi.LocationID);
        if (newLocationID != poi.LocationID)
        {
            Undo.RecordObject(poi, "Change POI Location ID");
            poi.LocationID = newLocationID;
            EditorUtility.SetDirty(poi);
        }

        // Quick assign from dropdown
        if (locationRegistry != null && locationRegistry.AllLocations != null)
        {
            if (GUILayout.Button("...", GUILayout.Width(25)))
            {
                GenericMenu menu = new GenericMenu();
                foreach (var loc in locationRegistry.AllLocations.Where(l => l != null))
                {
                    string locId = loc.LocationID;
                    menu.AddItem(new GUIContent($"{loc.DisplayName} ({loc.LocationID})"),
                        poi.LocationID == locId,
                        () => {
                            Undo.RecordObject(poi, "Change POI Location ID");
                            poi.LocationID = locId;
                            EditorUtility.SetDirty(poi);
                            RefreshPOIList();
                        });
                }
                menu.ShowAsContext();
            }
        }
        EditorGUILayout.EndHorizontal();

        // Location details (if found)
        if (location != null)
        {
            EditorGUILayout.BeginVertical("helpBox");
            EditorGUILayout.LabelField($"Location: {location.DisplayName}", EditorStyles.boldLabel);

            if (!string.IsNullOrEmpty(location.Description))
            {
                EditorGUILayout.LabelField(location.Description, EditorStyles.wordWrappedMiniLabel);
            }

            // Activities count
            int activityCount = location.AvailableActivities?.Count ?? 0;
            int enemyCount = location.AvailableEnemies?.Count ?? 0;
            int npcCount = location.AvailableNPCs?.Count ?? 0;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Activities: {activityCount}", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField($"Enemies: {enemyCount}", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField($"NPCs: {npcCount}", EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Edit Location", GUILayout.Width(100)))
            {
                Selection.activeObject = location;
                EditorGUIUtility.PingObject(location);
            }
            EditorGUILayout.EndVertical();
        }
        else if (!string.IsNullOrEmpty(poi.LocationID))
        {
            EditorGUILayout.HelpBox($"Location '{poi.LocationID}' not found in registry. Create it or fix the ID.", MessageType.Warning);

            if (GUILayout.Button("Create Missing Location"))
            {
                CreateLocationForPOI(poi);
            }
        }

        // Components info
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Components:", EditorStyles.boldLabel);

        var spriteRenderer = poi.GetComponent<SpriteRenderer>();
        var collider = poi.GetComponent<Collider2D>();

        EditorGUILayout.BeginHorizontal();
        DrawComponentStatus("SpriteRenderer", spriteRenderer != null);
        DrawComponentStatus("Collider2D", collider != null);
        EditorGUILayout.EndHorizontal();

        // Travel Start Point
        var travelStartField = typeof(POI).GetField("travelPathStartPoint",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Transform travelStart = travelStartField?.GetValue(poi) as Transform;

        EditorGUILayout.BeginHorizontal();
        DrawComponentStatus("TravelStartPoint", travelStart != null);
        if (travelStart == null)
        {
            if (GUILayout.Button("Create", GUILayout.Width(60)))
            {
                CreateTravelStartPointForPOI(poi);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
    }

    private void DrawComponentStatus(string name, bool exists)
    {
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
        style.normal.textColor = exists ? Color.green : Color.gray;
        string icon = exists ? "\u2713" : "\u2717";
        EditorGUILayout.LabelField($"{icon} {name}", style, GUILayout.Width(120));
    }
    #endregion

    #region Create POI Tab
    private void DrawCreatePOITab()
    {
        EditorGUILayout.LabelField("Create New POI", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // POI Name
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);

        newPOIName = EditorGUILayout.TextField("POI Name", newPOIName);
        if (string.IsNullOrEmpty(newPOIName))
        {
            EditorGUILayout.HelpBox("Enter a name for the POI (e.g., 'Village', 'Mine', 'Forest')", MessageType.None);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Location Selection
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Location", EditorStyles.boldLabel);

        useExistingLocation = EditorGUILayout.Toggle("Use Existing Location", useExistingLocation);

        if (useExistingLocation)
        {
            // Dropdown for existing locations
            if (locationRegistry != null && locationRegistry.AllLocations != null && locationRegistry.AllLocations.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                selectedLocation = (MapLocationDefinition)EditorGUILayout.ObjectField("Location", selectedLocation, typeof(MapLocationDefinition), false);

                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    GenericMenu menu = new GenericMenu();
                    foreach (var loc in locationRegistry.AllLocations.Where(l => l != null).OrderBy(l => l.DisplayName))
                    {
                        var localLoc = loc;
                        menu.AddItem(new GUIContent($"{loc.DisplayName} ({loc.LocationID})"),
                            selectedLocation == loc,
                            () => { selectedLocation = localLoc; });
                    }
                    menu.ShowAsContext();
                }
                EditorGUILayout.EndHorizontal();

                if (selectedLocation != null)
                {
                    EditorGUILayout.LabelField($"ID: {selectedLocation.LocationID}", EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No locations found in registry. Create a new location below.", MessageType.Info);
                useExistingLocation = false;
            }
        }
        else
        {
            // New location fields
            newLocationID = EditorGUILayout.TextField("Location ID", newLocationID);
            newLocationName = EditorGUILayout.TextField("Display Name", newLocationName);
            newLocationDescription = EditorGUILayout.TextArea(newLocationDescription, GUILayout.Height(40));

            // Auto-generate ID from name
            if (!string.IsNullOrEmpty(newLocationName) && string.IsNullOrEmpty(newLocationID))
            {
                if (GUILayout.Button("Generate ID from Name", GUILayout.Width(150)))
                {
                    newLocationID = GenerateIDFromName(newLocationName);
                }
            }

            // Check if ID already exists
            if (!string.IsNullOrEmpty(newLocationID) && locationLookup.ContainsKey(newLocationID))
            {
                EditorGUILayout.HelpBox($"Location ID '{newLocationID}' already exists!", MessageType.Error);
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Prefab vs Manual Creation
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Creation Method", EditorStyles.boldLabel);

        usePrefab = EditorGUILayout.Toggle("Use Prefab", usePrefab);

        if (usePrefab)
        {
            poiPrefab = (GameObject)EditorGUILayout.ObjectField("POI Prefab", poiPrefab, typeof(GameObject), false);
            if (poiPrefab == null)
            {
                EditorGUILayout.HelpBox("Select a prefab to instantiate, or disable 'Use Prefab' for manual creation.", MessageType.Info);
            }
        }
        else
        {
            // Size
            EditorGUILayout.LabelField("Size", EditorStyles.miniLabel);
            poiSize = EditorGUILayout.Vector2Field("", poiSize);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("50x50")) poiSize = new Vector2(50, 50);
            if (GUILayout.Button("100x100")) poiSize = new Vector2(100, 100);
            if (GUILayout.Button("150x150")) poiSize = new Vector2(150, 150);
            if (GUILayout.Button("200x200")) poiSize = new Vector2(200, 200);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Visual
            poiSprite = (Sprite)EditorGUILayout.ObjectField("Sprite", poiSprite, typeof(Sprite), false);
            poiColor = EditorGUILayout.ColorField("Color", poiColor);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Travel Start Point
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Travel Start Point", EditorStyles.boldLabel);
        createTravelStartPoint = EditorGUILayout.Toggle("Create Travel Start Point", createTravelStartPoint);

        if (createTravelStartPoint)
        {
            travelStartOffset = EditorGUILayout.Vector2Field("Offset from Center", travelStartOffset);
            EditorGUILayout.HelpBox("Travel Start Point is where the player character appears when arriving at this location.", MessageType.None);
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(20);

        // Create Button
        bool canCreate = CanCreatePOI();

        EditorGUI.BeginDisabledGroup(!canCreate);
        if (GUILayout.Button("Create POI", GUILayout.Height(35)))
        {
            CreatePOI();
        }
        EditorGUI.EndDisabledGroup();

        if (!canCreate)
        {
            EditorGUILayout.HelpBox(GetCreateValidationMessage(), MessageType.Warning);
        }
    }

    private bool CanCreatePOI()
    {
        if (string.IsNullOrEmpty(newPOIName)) return false;

        if (useExistingLocation)
        {
            return selectedLocation != null;
        }
        else
        {
            if (string.IsNullOrEmpty(newLocationID) || string.IsNullOrEmpty(newLocationName)) return false;
            if (locationLookup.ContainsKey(newLocationID)) return false;
        }

        if (usePrefab && poiPrefab == null) return false;

        return true;
    }

    private string GetCreateValidationMessage()
    {
        if (string.IsNullOrEmpty(newPOIName)) return "Enter a POI name";

        if (useExistingLocation)
        {
            if (selectedLocation == null) return "Select a location";
        }
        else
        {
            if (string.IsNullOrEmpty(newLocationID)) return "Enter a Location ID";
            if (string.IsNullOrEmpty(newLocationName)) return "Enter a Location Name";
            if (locationLookup.ContainsKey(newLocationID)) return "Location ID already exists";
        }

        if (usePrefab && poiPrefab == null) return "Select a prefab or disable 'Use Prefab'";

        return "";
    }

    private void CreatePOI()
    {
        try
        {
            // Determine location
            MapLocationDefinition targetLocation;
            string locationID;

            if (useExistingLocation)
            {
                targetLocation = selectedLocation;
                locationID = selectedLocation.LocationID;
            }
            else
            {
                // Create new location
                targetLocation = CreateNewLocation(newLocationID, newLocationName, newLocationDescription);
                locationID = newLocationID;
            }

            // Find or create WorldMap
            GameObject worldMapObject = FindOrCreateWorldMap();
            if (worldMapObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find or create WorldMap object.", "OK");
                return;
            }

            // Generate unique POI name
            string finalPOIName = GeneratePOIName(newPOIName, worldMapObject);

            // Create POI GameObject
            GameObject poiObject;
            if (usePrefab && poiPrefab != null)
            {
                poiObject = (GameObject)PrefabUtility.InstantiatePrefab(poiPrefab);
                poiObject.name = finalPOIName;
            }
            else
            {
                poiObject = new GameObject(finalPOIName);

                // Add SpriteRenderer
                SpriteRenderer spriteRenderer = poiObject.AddComponent<SpriteRenderer>();
                spriteRenderer.sprite = poiSprite;
                spriteRenderer.color = poiColor;

                // Add Collider
                BoxCollider2D collider = poiObject.AddComponent<BoxCollider2D>();
                if (poiSprite != null)
                {
                    collider.size = poiSprite.bounds.size;
                }
                else
                {
                    collider.size = poiSize / 100f; // Convert to Unity units
                }
            }

            // Parent to WorldMap
            poiObject.transform.SetParent(worldMapObject.transform, false);

            // Position at WorldMap center
            poiObject.transform.position = worldMapObject.transform.position;

            // Add POI component if not from prefab or prefab doesn't have it
            POI poiComponent = poiObject.GetComponent<POI>();
            if (poiComponent == null)
            {
                poiComponent = poiObject.AddComponent<POI>();
            }
            poiComponent.LocationID = locationID;

            // Create Travel Start Point
            if (createTravelStartPoint)
            {
                CreateTravelStartPointForPOI(poiComponent, travelStartOffset);
            }

            // Register undo
            Undo.RegisterCreatedObjectUndo(poiObject, "Create POI");

            // Select the new POI
            Selection.activeObject = poiObject;
            EditorGUIUtility.PingObject(poiObject);
            SceneView.lastActiveSceneView?.FrameSelected();

            // Log success
            Logger.LogInfo($"Created POI: {finalPOIName} with Location ID: {locationID}", Logger.LogCategory.EditorLog);

            // Refresh
            RefreshPOIList();

            // Reset form
            ResetCreateForm();

            EditorUtility.DisplayDialog("Success", $"POI '{finalPOIName}' created successfully!", "OK");
        }
        catch (System.Exception ex)
        {
            Logger.LogError($"Error creating POI: {ex.Message}", Logger.LogCategory.EditorLog);
            EditorUtility.DisplayDialog("Error", $"Failed to create POI:\n{ex.Message}", "OK");
        }
    }

    private void ResetCreateForm()
    {
        newPOIName = "";
        if (!useExistingLocation)
        {
            newLocationID = "";
            newLocationName = "";
            newLocationDescription = "";
        }
    }
    #endregion

    #region Validation Tab
    private void DrawValidationTab()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

        if (GUILayout.Button("Run Validation", GUILayout.Width(120)))
        {
            RunValidation();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (validationIssues.Count == 0)
        {
            EditorGUILayout.HelpBox("No issues found! All POIs are properly configured.", MessageType.Info);
            return;
        }

        // Group by severity
        var errors = validationIssues.Where(i => i.Severity == MessageType.Error).ToList();
        var warnings = validationIssues.Where(i => i.Severity == MessageType.Warning).ToList();
        var info = validationIssues.Where(i => i.Severity == MessageType.Info).ToList();

        if (errors.Count > 0)
        {
            DrawValidationSection("Errors", errors, new Color(1f, 0.3f, 0.3f));
        }

        if (warnings.Count > 0)
        {
            DrawValidationSection("Warnings", warnings, new Color(1f, 0.8f, 0.2f));
        }

        if (info.Count > 0)
        {
            DrawValidationSection("Info", info, new Color(0.5f, 0.8f, 1f));
        }
    }

    private void DrawValidationSection(string title, List<ValidationIssue> issues, Color color)
    {
        GUI.backgroundColor = color;
        EditorGUILayout.BeginVertical("box");
        GUI.backgroundColor = Color.white;

        EditorGUILayout.LabelField($"{title} ({issues.Count})", EditorStyles.boldLabel);

        foreach (var issue in issues)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(issue.Description, issue.Severity);

            EditorGUILayout.BeginVertical(GUILayout.Width(80));
            if (issue.RelatedPOI != null)
            {
                if (GUILayout.Button("Select POI", GUILayout.Width(75)))
                {
                    Selection.activeObject = issue.RelatedPOI.gameObject;
                    EditorGUIUtility.PingObject(issue.RelatedPOI.gameObject);
                }
            }
            if (issue.FixAction != null)
            {
                if (GUILayout.Button("Fix", GUILayout.Width(75)))
                {
                    issue.FixAction();
                    RunValidation();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void RunValidation()
    {
        validationIssues.Clear();
        RefreshPOIList();

        if (allPOIs == null || allPOIs.Length == 0)
        {
            validationIssues.Add(new ValidationIssue
            {
                Description = "No POIs found in the scene",
                Severity = MessageType.Info
            });
            return;
        }

        foreach (var poi in allPOIs)
        {
            ValidatePOI(poi);
        }

        // Check for locations without POIs
        if (locationRegistry != null && locationRegistry.AllLocations != null)
        {
            var poiLocationIDs = allPOIs.Select(p => p.LocationID).ToHashSet();

            foreach (var location in locationRegistry.AllLocations.Where(l => l != null))
            {
                if (!poiLocationIDs.Contains(location.LocationID))
                {
                    validationIssues.Add(new ValidationIssue
                    {
                        Description = $"Location '{location.DisplayName}' ({location.LocationID}) has no POI in scene",
                        Severity = MessageType.Warning,
                        RelatedLocation = location
                    });
                }
            }
        }

        Logger.LogInfo($"Validation complete: {validationIssues.Count} issues found", Logger.LogCategory.EditorLog);
    }

    private void ValidatePOI(POI poi)
    {
        // Check for empty Location ID
        if (string.IsNullOrEmpty(poi.LocationID))
        {
            validationIssues.Add(new ValidationIssue
            {
                Description = $"POI '{poi.gameObject.name}' has no Location ID",
                Severity = MessageType.Error,
                RelatedPOI = poi
            });
        }
        // Check for missing location in registry
        else if (!locationLookup.ContainsKey(poi.LocationID))
        {
            validationIssues.Add(new ValidationIssue
            {
                Description = $"POI '{poi.gameObject.name}' references unknown location '{poi.LocationID}'",
                Severity = MessageType.Error,
                RelatedPOI = poi,
                FixAction = () => CreateLocationForPOI(poi)
            });
        }

        // Check for missing collider
        if (poi.GetComponent<Collider2D>() == null)
        {
            validationIssues.Add(new ValidationIssue
            {
                Description = $"POI '{poi.gameObject.name}' has no Collider2D (required for click detection)",
                Severity = MessageType.Error,
                RelatedPOI = poi,
                FixAction = () => {
                    poi.gameObject.AddComponent<BoxCollider2D>();
                    EditorUtility.SetDirty(poi.gameObject);
                }
            });
        }

        // Check for missing travel start point
        var travelStartField = typeof(POI).GetField("travelPathStartPoint",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Transform travelStart = travelStartField?.GetValue(poi) as Transform;

        if (travelStart == null)
        {
            validationIssues.Add(new ValidationIssue
            {
                Description = $"POI '{poi.gameObject.name}' has no Travel Start Point",
                Severity = MessageType.Warning,
                RelatedPOI = poi,
                FixAction = () => CreateTravelStartPointForPOI(poi)
            });
        }

        // Check for duplicate location IDs
        var duplicates = allPOIs.Where(p => p != poi && p.LocationID == poi.LocationID).ToArray();
        if (duplicates.Length > 0 && !string.IsNullOrEmpty(poi.LocationID))
        {
            validationIssues.Add(new ValidationIssue
            {
                Description = $"POI '{poi.gameObject.name}' shares Location ID '{poi.LocationID}' with {duplicates.Length} other POI(s)",
                Severity = MessageType.Warning,
                RelatedPOI = poi
            });
        }
    }
    #endregion

    #region Helper Methods
    private void LoadRegistry()
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

        BuildLocationLookup();
    }

    private void RefreshPOIList()
    {
        allPOIs = FindObjectsOfType<POI>();
        poiFoldouts = null; // Reset foldouts
        BuildLocationLookup();
    }

    private void BuildLocationLookup()
    {
        locationLookup.Clear();

        if (locationRegistry?.AllLocations != null)
        {
            foreach (var location in locationRegistry.AllLocations)
            {
                if (location != null && !string.IsNullOrEmpty(location.LocationID))
                {
                    if (!locationLookup.ContainsKey(location.LocationID))
                    {
                        locationLookup[location.LocationID] = location;
                    }
                }
            }
        }
    }

    private POI[] FilterPOIs()
    {
        if (string.IsNullOrEmpty(searchFilter))
            return allPOIs;

        string filter = searchFilter.ToLower();
        return allPOIs.Where(poi =>
            poi.gameObject.name.ToLower().Contains(filter) ||
            poi.LocationID.ToLower().Contains(filter) ||
            (locationLookup.TryGetValue(poi.LocationID, out var loc) && loc.DisplayName.ToLower().Contains(filter))
        ).ToArray();
    }

    private MapLocationDefinition GetLocationForPOI(POI poi)
    {
        if (poi == null || string.IsNullOrEmpty(poi.LocationID))
            return null;

        locationLookup.TryGetValue(poi.LocationID, out MapLocationDefinition location);
        return location;
    }

    private GameObject FindOrCreateWorldMap()
    {
        GameObject worldMapObject = GameObject.Find("WorldMap");

        if (worldMapObject != null)
            return worldMapObject;

        bool create = EditorUtility.DisplayDialog(
            "WorldMap Not Found",
            "The 'WorldMap' GameObject doesn't exist in the scene. Create it now?",
            "Create", "Cancel");

        if (create)
        {
            worldMapObject = new GameObject("WorldMap");
            Undo.RegisterCreatedObjectUndo(worldMapObject, "Create WorldMap");
            return worldMapObject;
        }

        return null;
    }

    private string GeneratePOIName(string baseName, GameObject worldMapParent)
    {
        POI[] existingPOIs = worldMapParent.GetComponentsInChildren<POI>();
        int count = 1;

        string basePattern = $"POI_{baseName}_";

        foreach (POI poi in existingPOIs)
        {
            if (poi.gameObject.name.StartsWith(basePattern))
            {
                string numberPart = poi.gameObject.name.Substring(basePattern.Length);
                if (int.TryParse(numberPart, out int existingNumber) && existingNumber >= count)
                {
                    count = existingNumber + 1;
                }
            }
        }

        return $"POI_{baseName}_{count:D2}";
    }

    private string GenerateIDFromName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "";

        return name.ToLower()
                  .Replace(" ", "_")
                  .Replace("'", "")
                  .Replace("-", "_")
                  .Replace("(", "")
                  .Replace(")", "");
    }

    private MapLocationDefinition CreateNewLocation(string locationID, string displayName, string description)
    {
        MapLocationDefinition newLocation = CreateInstance<MapLocationDefinition>();
        newLocation.LocationID = locationID;
        newLocation.DisplayName = displayName;
        newLocation.Description = description;
        newLocation.AvailableActivities = new List<LocationActivity>();
        newLocation.Connections = new List<LocationConnection>();

        string assetPath = $"Assets/ScriptableObjects/MapLocation/{displayName}.asset";
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        AssetDatabase.CreateAsset(newLocation, assetPath);

        if (locationRegistry != null)
        {
            locationRegistry.AllLocations.Add(newLocation);
            EditorUtility.SetDirty(locationRegistry);
        }

        AssetDatabase.SaveAssets();

        Logger.LogInfo($"Created new location: {displayName} (ID: {locationID})", Logger.LogCategory.EditorLog);

        BuildLocationLookup();

        return newLocation;
    }

    private void CreateLocationForPOI(POI poi)
    {
        string locationID = poi.LocationID;
        string displayName = string.IsNullOrEmpty(locationID) ? poi.gameObject.name : locationID;

        CreateNewLocation(locationID, displayName, "");
    }

    private void CreateTravelStartPointForPOI(POI poi, Vector2 offset = default)
    {
        if (offset == default) offset = new Vector2(0.5f, 0.5f);

        GameObject travelStartPoint = new GameObject("TravelStartPoint");
        travelStartPoint.transform.SetParent(poi.transform, false);
        travelStartPoint.transform.localPosition = new Vector3(offset.x, offset.y, 0f);

        // Add visual indicator
        SpriteRenderer travelRenderer = travelStartPoint.AddComponent<SpriteRenderer>();
        travelRenderer.color = new Color(0f, 1f, 1f, 0.5f);

        // Assign to POI
        var field = typeof(POI).GetField("travelPathStartPoint",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            field.SetValue(poi, travelStartPoint.transform);
            EditorUtility.SetDirty(poi);
        }

        Undo.RegisterCreatedObjectUndo(travelStartPoint, "Create Travel Start Point");

        Logger.LogInfo($"Created TravelStartPoint for POI: {poi.gameObject.name}", Logger.LogCategory.EditorLog);
    }

    /// <summary>
    /// Delete a POI with confirmation dialog
    /// </summary>
    private void DeletePOIWithConfirmation(POI poi)
    {
        if (poi == null) return;

        string locationId = poi.LocationID;
        MapLocationDefinition location = GetLocationForPOI(poi);

        // Count connections that will be removed
        int outgoingConnections = 0;
        int incomingConnections = 0;

        if (location != null)
        {
            outgoingConnections = location.Connections?.Count ?? 0;

            if (locationRegistry?.AllLocations != null)
            {
                foreach (var otherLoc in locationRegistry.AllLocations.Where(l => l != null && l != location))
                {
                    if (otherLoc.Connections?.Any(c => c?.DestinationLocationID == locationId) == true)
                    {
                        incomingConnections++;
                    }
                }
            }
        }

        string locationInfo = location != null
            ? $"'{location.DisplayName}' ({locationId})"
            : $"'{poi.gameObject.name}' (Location ID: {locationId})";

        string message = $"Delete POI {locationInfo}?\n\n" +
                        $"This will:\n" +
                        $"- Delete the POI GameObject from the scene\n";

        if (location != null)
        {
            message += $"- Remove {outgoingConnections} outgoing connection(s)\n" +
                      $"- Remove {incomingConnections} incoming connection(s)\n\n" +
                      $"Do you also want to delete the MapLocationDefinition asset?";
        }
        else
        {
            message += "\nThe POI has no linked location in the registry.";
        }

        int choice;
        if (location != null)
        {
            choice = EditorUtility.DisplayDialogComplex(
                "Delete POI",
                message,
                "Delete POI Only",      // Option 0 - keep the location asset
                "Cancel",               // Option 1 - cancel
                "Delete POI & Asset"    // Option 2 - delete everything including asset
            );
        }
        else
        {
            // No location, just ask to delete the GameObject
            bool confirm = EditorUtility.DisplayDialog(
                "Delete POI",
                message,
                "Delete", "Cancel");
            choice = confirm ? 0 : 1;
        }

        if (choice == 1) return; // Cancel

        bool deleteAsset = (choice == 2);

        // If we have a location, use the utility method
        if (location != null && !string.IsNullOrEmpty(locationId))
        {
            if (MapGraphUtility.DeletePOI(locationId, locationRegistry, deleteAsset))
            {
                RefreshPOIList();
                Logger.LogInfo($"Deleted POI '{poi.gameObject.name}' with location '{locationId}'", Logger.LogCategory.EditorLog);
            }
            else
            {
                EditorUtility.DisplayDialog("Delete Failed", $"Failed to delete POI. Check the console for details.", "OK");
            }
        }
        else
        {
            // No location, just delete the GameObject
            Undo.DestroyObjectImmediate(poi.gameObject);
            RefreshPOIList();
            Logger.LogInfo($"Deleted POI GameObject '{poi.gameObject.name}' (no linked location)", Logger.LogCategory.EditorLog);
        }
    }
    #endregion
}
#endif
