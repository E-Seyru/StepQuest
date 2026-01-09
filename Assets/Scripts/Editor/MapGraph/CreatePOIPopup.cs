// Purpose: Popup window for creating new POIs from the Map Editor
// Filepath: Assets/Scripts/Editor/MapGraph/CreatePOIPopup.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Popup window for creating new POIs directly from the Map Editor.
/// Creates both the MapLocationDefinition asset and the POI GameObject in the scene.
/// Supports prefab-based creation for consistent POI settings.
/// </summary>
public class CreatePOIPopup : EditorWindow
{
    private const string PREFAB_PREF_KEY = "CreatePOIPopup_PrefabPath";

    // Callback when POI is created
    private Action<string> onCreatedCallback;
    private LocationRegistry locationRegistry;

    // Form fields
    private string locationName = "";
    private string locationId = "";
    private string locationDescription = "";
    private bool autoGenerateId = true;

    // Prefab-based creation
    private GameObject poiPrefab;
    private bool usePrefab = true;

    // Visual settings
    private Sprite locationImage;
    private Sprite locationSprite;

    // Validation
    private string validationError = "";

    /// <summary>
    /// Show the popup window
    /// </summary>
    public static void Show(LocationRegistry registry, Action<string> onCreated)
    {
        var window = GetWindow<CreatePOIPopup>(true, "Create New POI", true);
        window.locationRegistry = registry;
        window.onCreatedCallback = onCreated;
        window.minSize = new Vector2(450, 650);
        window.maxSize = new Vector2(450, 750);

        // Center on screen
        var position = window.position;
        position.x = (Screen.currentResolution.width - position.width) / 2;
        position.y = (Screen.currentResolution.height - position.height) / 2;
        window.position = position;

        window.LoadPrefabPreference();
        window.ShowUtility();
    }

    private void LoadPrefabPreference()
    {
        string prefabPath = EditorPrefs.GetString(PREFAB_PREF_KEY, "");
        if (!string.IsNullOrEmpty(prefabPath))
        {
            poiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }

        // If no saved prefab, try to find one in common locations
        if (poiPrefab == null)
        {
            string[] searchPaths = new[]
            {
                "Assets/Prefabs/POI.prefab",
                "Assets/Prefabs/World/POI.prefab",
                "Assets/Prefabs/Map/POI.prefab"
            };

            foreach (var path in searchPaths)
            {
                poiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (poiPrefab != null) break;
            }
        }

        usePrefab = poiPrefab != null;
    }

    private void SavePrefabPreference()
    {
        if (poiPrefab != null)
        {
            string path = AssetDatabase.GetAssetPath(poiPrefab);
            EditorPrefs.SetString(PREFAB_PREF_KEY, path);
        }
        else
        {
            EditorPrefs.DeleteKey(PREFAB_PREF_KEY);
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        // Header
        EditorGUILayout.LabelField("Create New POI", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Creates a new MapLocationDefinition and POI GameObject in the scene.", MessageType.Info);

        EditorGUILayout.Space(10);

        // Location Settings
        EditorGUILayout.LabelField("Location Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        // Location Name
        locationName = EditorGUILayout.TextField("Display Name", locationName);

        // Auto-generate ID option
        EditorGUILayout.BeginHorizontal();
        autoGenerateId = EditorGUILayout.Toggle("Auto-generate ID", autoGenerateId);
        EditorGUILayout.EndHorizontal();

        // Location ID
        EditorGUI.BeginDisabledGroup(autoGenerateId);
        if (autoGenerateId && !string.IsNullOrEmpty(locationName))
        {
            locationId = GenerateIDFromName(locationName);
        }
        locationId = EditorGUILayout.TextField("Location ID", locationId);
        EditorGUI.EndDisabledGroup();

        // Description
        EditorGUILayout.LabelField("Description");
        locationDescription = EditorGUILayout.TextArea(locationDescription, GUILayout.Height(40));

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // POI Prefab Settings
        EditorGUILayout.LabelField("POI Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        // Prefab field
        EditorGUI.BeginChangeCheck();
        poiPrefab = (GameObject)EditorGUILayout.ObjectField("POI Prefab", poiPrefab, typeof(GameObject), false);
        if (EditorGUI.EndChangeCheck())
        {
            SavePrefabPreference();
            usePrefab = poiPrefab != null;
        }

        if (poiPrefab != null)
        {
            // Validate it has a POI component
            POI prefabPOI = poiPrefab.GetComponent<POI>();
            if (prefabPOI != null)
            {
                EditorGUILayout.HelpBox("New POI will be instantiated from this prefab with all settings preserved.", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("Warning: Prefab does not have a POI component. One will be added.", MessageType.Warning);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No prefab assigned. A basic POI will be created.\nAssign a prefab to copy all visual and behavior settings.", MessageType.Warning);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Visual Settings
        EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical("box");

        // Location Image (for details panel)
        locationImage = (Sprite)EditorGUILayout.ObjectField("Location Image", locationImage, typeof(Sprite), false);
        EditorGUILayout.LabelField("    Main image shown in location details panel", EditorStyles.miniLabel);

        EditorGUILayout.Space(5);

        // Location Sprite (used for both LocationIcon and POI SpriteRenderer)
        locationSprite = (Sprite)EditorGUILayout.ObjectField("Location Sprite", locationSprite, typeof(Sprite), false);
        EditorGUILayout.LabelField("    Used for POI on map and icon in UI", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Validation
        ValidateInput();
        if (!string.IsNullOrEmpty(validationError))
        {
            EditorGUILayout.HelpBox(validationError, MessageType.Error);
        }

        EditorGUILayout.Space(10);

        // Buttons
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = string.IsNullOrEmpty(validationError);
        if (GUILayout.Button("Create POI", GUILayout.Height(30)))
        {
            CreatePOI();
        }
        GUI.enabled = true;

        if (GUILayout.Button("Cancel", GUILayout.Height(30)))
        {
            Close();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void ValidateInput()
    {
        validationError = "";

        if (string.IsNullOrEmpty(locationName))
        {
            validationError = "Location name is required.";
            return;
        }

        if (string.IsNullOrEmpty(locationId))
        {
            validationError = "Location ID is required.";
            return;
        }

        // Check for duplicate ID
        if (locationRegistry != null && locationRegistry.AllLocations != null)
        {
            if (locationRegistry.AllLocations.Any(l => l != null && l.LocationID == locationId))
            {
                validationError = $"Location ID '{locationId}' already exists.";
                return;
            }
        }

        // Check for invalid characters
        if (locationId.Contains(" "))
        {
            validationError = "Location ID cannot contain spaces.";
            return;
        }
    }

    private void CreatePOI()
    {
        try
        {
            // 1. Create the MapLocationDefinition asset
            MapLocationDefinition newLocation = CreateInstance<MapLocationDefinition>();
            newLocation.LocationID = locationId;
            newLocation.DisplayName = locationName;
            newLocation.Description = locationDescription;
            newLocation.LocationImage = locationImage;
            newLocation.LocationIcon = locationSprite;
            newLocation.AvailableActivities = new List<LocationActivity>();
            newLocation.AvailableEnemies = new List<LocationEnemy>();
            newLocation.AvailableNPCs = new List<LocationNPC>();
            newLocation.Connections = new List<LocationConnection>();

            // Save to appropriate folder
            string assetPath = $"Assets/ScriptableObjects/MapLocation/{locationName}.asset";
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(newLocation, assetPath);

            // Add to registry
            if (locationRegistry != null)
            {
                locationRegistry.AllLocations.Add(newLocation);
                EditorUtility.SetDirty(locationRegistry);
            }

            // 2. Create the POI GameObject
            GameObject worldMapObject = FindOrCreateWorldMap();
            if (worldMapObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find or create WorldMap object.", "OK");
                return;
            }

            // Generate unique POI name
            string poiName = $"POI_{locationName}_01";

            GameObject poiObject;
            POI poiComponent;

            if (poiPrefab != null)
            {
                // Instantiate from prefab
                poiObject = (GameObject)PrefabUtility.InstantiatePrefab(poiPrefab, worldMapObject.transform);
                poiObject.name = poiName;

                // Get or add POI component
                poiComponent = poiObject.GetComponent<POI>();
                if (poiComponent == null)
                {
                    poiComponent = poiObject.AddComponent<POI>();
                }

                // Set the Location ID
                poiComponent.LocationID = locationId;

                // Update SpriteRenderer reference if it exists
                SpriteRenderer sr = poiObject.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    // Use reflection to set the spriteRenderer field
                    var spriteRendererField = typeof(POI).GetField("spriteRenderer",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (spriteRendererField != null)
                    {
                        spriteRendererField.SetValue(poiComponent, sr);
                    }
                }
            }
            else
            {
                // Create basic POI without prefab
                poiObject = CreateBasicPOI(poiName, worldMapObject);
                poiComponent = poiObject.GetComponent<POI>();
                poiComponent.LocationID = locationId;
            }

            // Position at WorldMap center (user will reposition)
            poiObject.transform.position = worldMapObject.transform.position;

            // Apply location sprite to POI's SpriteRenderer
            if (locationSprite != null)
            {
                SpriteRenderer sr = poiObject.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = locationSprite;
                }
            }

            // Register undo
            Undo.RegisterCreatedObjectUndo(poiObject, "Create POI");

            AssetDatabase.SaveAssets();

            Logger.LogInfo($"Created POI: {poiName} with Location ID: {locationId}", Logger.LogCategory.EditorLog);

            // Invoke callback
            onCreatedCallback?.Invoke(locationId);

            // Close the window
            Close();
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error creating POI: {ex.Message}", Logger.LogCategory.EditorLog);
            EditorUtility.DisplayDialog("Error", $"Failed to create POI:\n{ex.Message}", "OK");
        }
    }

    private GameObject CreateBasicPOI(string poiName, GameObject parent)
    {
        // Create GameObject
        GameObject poiObject = new GameObject(poiName);
        poiObject.transform.SetParent(parent.transform, false);

        // Try to copy settings from an existing POI in the scene
        POI[] existingPOIs = UnityEngine.Object.FindObjectsOfType<POI>();
        if (existingPOIs.Length > 0)
        {
            POI template = existingPOIs[0];

            // Copy transform scale
            poiObject.transform.localScale = template.transform.localScale;

            // Copy SpriteRenderer settings
            SpriteRenderer templateSR = template.GetComponent<SpriteRenderer>();
            if (templateSR != null)
            {
                SpriteRenderer sr = poiObject.AddComponent<SpriteRenderer>();
                sr.sprite = templateSR.sprite;
                sr.color = templateSR.color;
                sr.sortingLayerID = templateSR.sortingLayerID;
                sr.sortingOrder = templateSR.sortingOrder;
                sr.drawMode = templateSR.drawMode;
            }

            // Copy BoxCollider2D settings
            BoxCollider2D templateCollider = template.GetComponent<BoxCollider2D>();
            if (templateCollider != null)
            {
                BoxCollider2D collider = poiObject.AddComponent<BoxCollider2D>();
                collider.size = templateCollider.size;
                collider.offset = templateCollider.offset;
            }

            // Add POI component
            POI poiComponent = poiObject.AddComponent<POI>();

            // Copy TravelStartPoint if template has one
            Transform templateTravelPoint = template.transform.Find("TravelStartPoint");
            if (templateTravelPoint != null)
            {
                GameObject travelStartPoint = new GameObject("TravelStartPoint");
                travelStartPoint.transform.SetParent(poiObject.transform, false);
                travelStartPoint.transform.localPosition = templateTravelPoint.localPosition;

                SpriteRenderer templateTravelSR = templateTravelPoint.GetComponent<SpriteRenderer>();
                if (templateTravelSR != null)
                {
                    SpriteRenderer travelSR = travelStartPoint.AddComponent<SpriteRenderer>();
                    travelSR.sprite = templateTravelSR.sprite;
                    travelSR.color = templateTravelSR.color;
                }

                // Assign to POI
                var field = typeof(POI).GetField("travelPathStartPoint",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(poiComponent, travelStartPoint.transform);
                }
            }

            return poiObject;
        }
        else
        {
            // No existing POIs to copy from, create minimal setup
            SpriteRenderer sr = poiObject.AddComponent<SpriteRenderer>();
            sr.color = Color.white;

            BoxCollider2D collider = poiObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1, 1);

            poiObject.AddComponent<POI>();

            return poiObject;
        }
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
}
#endif
