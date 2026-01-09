// Purpose: Dialog window for configuring POI creation settings
// Filepath: Assets/Scripts/Editor/MapGraph/CreatePOIDialog.cs
#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Dialog window for configuring POI creation with full component setup.
/// Supports prefab-based creation, component configuration, and visual settings.
/// </summary>
public class CreatePOIDialog : EditorWindow
{
    // POI Settings
    private MapLocationDefinition location;
    private Vector2 editorPosition;

    // Size settings
    private Vector2 poiSize = new Vector2(100, 100);

    // Component settings
    private bool addPOIComponent = true;
    private bool addCollider = true;
    private bool addImage = true;

    // Visual settings
    private Sprite poiSprite;
    private Color poiColor = Color.white;

    // Prefab settings
    private GameObject poiPrefab;
    private bool usePrefab = false;

    // Travel start point settings
    private bool createTravelStartPoint = true;
    private Vector2 travelStartOffset = Vector2.zero;

    // Callback
    private Action<POICreationSettings> onConfirm;

    // Scroll position for the dialog
    private Vector2 scrollPosition;

    // EditorPrefs keys for persistence
    private const string PREF_POI_SIZE_X = "CreatePOI_SizeX";
    private const string PREF_POI_SIZE_Y = "CreatePOI_SizeY";
    private const string PREF_ADD_POI_COMPONENT = "CreatePOI_AddComponent";
    private const string PREF_ADD_COLLIDER = "CreatePOI_AddCollider";
    private const string PREF_ADD_IMAGE = "CreatePOI_AddImage";
    private const string PREF_USE_PREFAB = "CreatePOI_UsePrefab";
    private const string PREF_PREFAB_PATH = "CreatePOI_PrefabPath";
    private const string PREF_CREATE_TSP = "CreatePOI_CreateTSP";

    /// <summary>
    /// Settings container for POI creation
    /// </summary>
    public class POICreationSettings
    {
        public MapLocationDefinition Location;
        public Vector2 EditorPosition;
        public Vector2 Size;
        public bool AddPOIComponent;
        public bool AddCollider;
        public bool AddImage;
        public Sprite Sprite;
        public Color Color;
        public GameObject Prefab;
        public bool UsePrefab;
        public bool CreateTravelStartPoint;
        public Vector2 TravelStartOffset;
    }

    /// <summary>
    /// Show the dialog for creating a POI
    /// </summary>
    public static void Show(MapLocationDefinition location, Vector2 editorPosition, Action<POICreationSettings> onConfirm)
    {
        var window = CreateInstance<CreatePOIDialog>();
        window.location = location;
        window.editorPosition = editorPosition;
        window.onConfirm = onConfirm;
        window.titleContent = new GUIContent("Create POI");
        window.LoadPreferences();

        // Size
        window.minSize = new Vector2(320, 400);
        window.maxSize = new Vector2(320, 500);

        // Position near mouse
        Vector2 mousePos = GUIUtility.GUIToScreenPoint(Event.current?.mousePosition ?? Vector2.zero);
        if (mousePos == Vector2.zero)
        {
            mousePos = new Vector2(Screen.width / 2, Screen.height / 2);
        }
        window.position = new Rect(mousePos.x - 160, mousePos.y - 200, 320, 400);

        window.ShowUtility();
        window.Focus();
    }

    private void LoadPreferences()
    {
        poiSize.x = EditorPrefs.GetFloat(PREF_POI_SIZE_X, 100);
        poiSize.y = EditorPrefs.GetFloat(PREF_POI_SIZE_Y, 100);
        addPOIComponent = EditorPrefs.GetBool(PREF_ADD_POI_COMPONENT, true);
        addCollider = EditorPrefs.GetBool(PREF_ADD_COLLIDER, true);
        addImage = EditorPrefs.GetBool(PREF_ADD_IMAGE, true);
        usePrefab = EditorPrefs.GetBool(PREF_USE_PREFAB, false);
        createTravelStartPoint = EditorPrefs.GetBool(PREF_CREATE_TSP, true);

        string prefabPath = EditorPrefs.GetString(PREF_PREFAB_PATH, "");
        if (!string.IsNullOrEmpty(prefabPath))
        {
            poiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
    }

    private void SavePreferences()
    {
        EditorPrefs.SetFloat(PREF_POI_SIZE_X, poiSize.x);
        EditorPrefs.SetFloat(PREF_POI_SIZE_Y, poiSize.y);
        EditorPrefs.SetBool(PREF_ADD_POI_COMPONENT, addPOIComponent);
        EditorPrefs.SetBool(PREF_ADD_COLLIDER, addCollider);
        EditorPrefs.SetBool(PREF_ADD_IMAGE, addImage);
        EditorPrefs.SetBool(PREF_USE_PREFAB, usePrefab);
        EditorPrefs.SetBool(PREF_CREATE_TSP, createTravelStartPoint);

        if (poiPrefab != null)
        {
            EditorPrefs.SetString(PREF_PREFAB_PATH, AssetDatabase.GetAssetPath(poiPrefab));
        }
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(10);

        // Header
        EditorGUILayout.LabelField("Create POI", EditorStyles.boldLabel);
        if (location != null)
        {
            EditorGUILayout.LabelField($"Location: {location.DisplayName}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"ID: {location.LocationID}", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(10);
        DrawSeparator();

        // Prefab Section
        EditorGUILayout.LabelField("Prefab Settings", EditorStyles.boldLabel);
        usePrefab = EditorGUILayout.Toggle("Use Prefab", usePrefab);

        EditorGUI.BeginDisabledGroup(!usePrefab);
        poiPrefab = (GameObject)EditorGUILayout.ObjectField("POI Prefab", poiPrefab, typeof(GameObject), false);
        EditorGUI.EndDisabledGroup();

        if (usePrefab && poiPrefab == null)
        {
            EditorGUILayout.HelpBox("Select a prefab or disable 'Use Prefab' to create manually.", MessageType.Warning);
        }

        EditorGUILayout.Space(5);
        DrawSeparator();

        // Size Section (only when not using prefab)
        EditorGUI.BeginDisabledGroup(usePrefab);
        EditorGUILayout.LabelField("Size Settings", EditorStyles.boldLabel);
        poiSize = EditorGUILayout.Vector2Field("POI Size", poiSize);
        poiSize.x = Mathf.Max(10, poiSize.x);
        poiSize.y = Mathf.Max(10, poiSize.y);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("50x50", GUILayout.Width(60))) poiSize = new Vector2(50, 50);
        if (GUILayout.Button("100x100", GUILayout.Width(60))) poiSize = new Vector2(100, 100);
        if (GUILayout.Button("150x150", GUILayout.Width(60))) poiSize = new Vector2(150, 150);
        if (GUILayout.Button("200x200", GUILayout.Width(60))) poiSize = new Vector2(200, 200);
        EditorGUILayout.EndHorizontal();
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(5);
        DrawSeparator();

        // Components Section (only when not using prefab)
        EditorGUI.BeginDisabledGroup(usePrefab);
        EditorGUILayout.LabelField("Components", EditorStyles.boldLabel);
        addPOIComponent = EditorGUILayout.Toggle("Add POI Component", addPOIComponent);
        addCollider = EditorGUILayout.Toggle("Add BoxCollider2D", addCollider);
        addImage = EditorGUILayout.Toggle("Add Image Component", addImage);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(5);
        DrawSeparator();

        // Visual Section (only when not using prefab and adding image)
        EditorGUI.BeginDisabledGroup(usePrefab || !addImage);
        EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
        poiSprite = (Sprite)EditorGUILayout.ObjectField("Sprite", poiSprite, typeof(Sprite), false);
        poiColor = EditorGUILayout.ColorField("Color", poiColor);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(5);
        DrawSeparator();

        // Travel Start Point Section
        EditorGUILayout.LabelField("Travel Start Point", EditorStyles.boldLabel);
        createTravelStartPoint = EditorGUILayout.Toggle("Create Travel Start Point", createTravelStartPoint);

        EditorGUI.BeginDisabledGroup(!createTravelStartPoint);
        travelStartOffset = EditorGUILayout.Vector2Field("Offset from Center", travelStartOffset);
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(15);

        EditorGUILayout.EndScrollView();

        // Buttons at bottom (outside scroll)
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Cancel", GUILayout.Width(80)))
        {
            Close();
        }

        EditorGUI.BeginDisabledGroup(usePrefab && poiPrefab == null);
        if (GUILayout.Button("Create", GUILayout.Width(80)))
        {
            SavePreferences();

            var settings = new POICreationSettings
            {
                Location = location,
                EditorPosition = editorPosition,
                Size = poiSize,
                AddPOIComponent = addPOIComponent,
                AddCollider = addCollider,
                AddImage = addImage,
                Sprite = poiSprite,
                Color = poiColor,
                Prefab = poiPrefab,
                UsePrefab = usePrefab,
                CreateTravelStartPoint = createTravelStartPoint,
                TravelStartOffset = travelStartOffset
            };

            onConfirm?.Invoke(settings);
            Close();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(5);
    }

    private void DrawSeparator()
    {
        var rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f));
        EditorGUILayout.Space(5);
    }

    private void OnLostFocus()
    {
        // Don't close on lost focus - allows picking objects
    }
}
#endif
