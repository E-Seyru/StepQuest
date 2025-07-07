// Purpose: Enhanced tool to manage activities, variants, and POI assignments with creation capabilities
// Filepath: Assets/Scripts/Editor/ActivityManagerWindow.cs
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class ActivityManagerWindow : EditorWindow
{
    [MenuItem("StepQuest/Activity Manager")]
    public static void ShowWindow()
    {
        ActivityManagerWindow window = GetWindow<ActivityManagerWindow>();
        window.titleContent = new GUIContent("Activity Manager");
        window.Show();
    }

    // Data
    private ActivityRegistry activityRegistry;
    private LocationRegistry locationRegistry;

    // UI State
    private Vector2 scrollPosition;
    private string searchFilter = "";
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Activities", "POI Management" };

    // POI Management
    private POI[] allPOIs;
    private Dictionary<string, MapLocationDefinition> locationLookup = new Dictionary<string, MapLocationDefinition>();

    // Creation Dialog State
    private bool showCreateActivityDialog = false;
    private bool showCreateVariantDialog = false;
    private bool showCreatePOIDialog = false;
    private bool showCreateLocationDialog = false;

    private string newActivityName = "";
    private string newActivityDescription = "";
    private string newVariantName = "";
    private string newVariantDescription = "";
    private string newPOIName = "";
    private string newLocationID = "";
    private string newLocationName = "";
    private string newLocationDescription = "";

    private ActivityDefinition targetActivityForVariant = null;

    void OnEnable()
    {
        LoadRegistries();
        RefreshPOIList();
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
                DrawActivitiesTab();
                break;
            case 1:
                DrawPOIManagementTab();
                break;
        }

        EditorGUILayout.EndScrollView();

        // Handle creation dialogs
        HandleCreationDialogs();
    }

    #region Creation Dialogs
    private void HandleCreationDialogs()
    {
        if (showCreateActivityDialog)
            DrawCreateActivityDialog();

        if (showCreateVariantDialog)
            DrawCreateVariantDialog();

        if (showCreatePOIDialog)
            DrawCreatePOIDialog();

        if (showCreateLocationDialog)
            DrawCreateLocationDialog();
    }

    private void DrawCreateActivityDialog()
    {
        GUILayout.BeginArea(new Rect(50, 100, 400, 250));
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField("Create New Activity", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Activity Name:");
        newActivityName = EditorGUILayout.TextField(newActivityName);

        EditorGUILayout.LabelField("Description:");
        newActivityDescription = EditorGUILayout.TextArea(newActivityDescription, GUILayout.Height(60));

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create"))
        {
            if (!string.IsNullOrEmpty(newActivityName))
            {
                CreateNewActivity(newActivityName, newActivityDescription);
                ResetCreateActivityDialog();
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Input", "Activity name cannot be empty.", "OK");
            }
        }

        if (GUILayout.Button("Cancel"))
        {
            ResetCreateActivityDialog();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawCreateVariantDialog()
    {
        GUILayout.BeginArea(new Rect(50, 100, 400, 250));
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField("Create New Variant", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Target Activity:");
        EditorGUILayout.LabelField(targetActivityForVariant?.GetDisplayName() ?? "None selected", EditorStyles.miniLabel);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Variant Name:");
        newVariantName = EditorGUILayout.TextField(newVariantName);

        EditorGUILayout.LabelField("Description:");
        newVariantDescription = EditorGUILayout.TextArea(newVariantDescription, GUILayout.Height(60));

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create"))
        {
            if (!string.IsNullOrEmpty(newVariantName) && targetActivityForVariant != null)
            {
                CreateNewVariant(targetActivityForVariant, newVariantName, newVariantDescription);
                ResetCreateVariantDialog();
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Input", "Variant name cannot be empty and target activity must be selected.", "OK");
            }
        }

        if (GUILayout.Button("Cancel"))
        {
            ResetCreateVariantDialog();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawCreatePOIDialog()
    {
        // Validation prealable
        if (!ValidateSetupForPOICreation())
        {
            ResetCreatePOIDialog();
            return;
        }

        GUILayout.BeginArea(new Rect(50, 100, 400, 220));
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField("Creer un Nouveau POI", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Nom du POI:");
        newPOIName = EditorGUILayout.TextField(newPOIName);

        EditorGUILayout.LabelField("ID de Location:");
        newLocationID = EditorGUILayout.TextField(newLocationID);

        // Info sur la location existante
        if (!string.IsNullOrEmpty(newLocationID))
        {
            if (locationLookup != null && locationLookup.ContainsKey(newLocationID))
            {
                var existingLocation = locationLookup[newLocationID];
                EditorGUILayout.LabelField($"✅ Location trouvee: {existingLocation.DisplayName}",
                    EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField($"⚠️ Location '{newLocationID}' sera creee",
                    EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        GUI.enabled = !string.IsNullOrEmpty(newPOIName) && !string.IsNullOrEmpty(newLocationID);
        if (GUILayout.Button("Creer"))
        {
            CreateNewPOI(newPOIName, newLocationID);
            ResetCreatePOIDialog();
        }
        GUI.enabled = true;

        if (GUILayout.Button("Annuler"))
        {
            ResetCreatePOIDialog();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void DrawCreateLocationDialog()
    {
        GUILayout.BeginArea(new Rect(50, 100, 400, 250));
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField("Create New Location", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Location ID:");
        newLocationID = EditorGUILayout.TextField(newLocationID);

        EditorGUILayout.LabelField("Display Name:");
        newLocationName = EditorGUILayout.TextField(newLocationName);

        EditorGUILayout.LabelField("Description:");
        newLocationDescription = EditorGUILayout.TextArea(newLocationDescription, GUILayout.Height(60));

        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create"))
        {
            if (!string.IsNullOrEmpty(newLocationID) && !string.IsNullOrEmpty(newLocationName))
            {
                CreateNewLocation(newLocationID, newLocationName, newLocationDescription);
                ResetCreateLocationDialog();
            }
            else
            {
                EditorUtility.DisplayDialog("Invalid Input", "Location ID and Display Name cannot be empty.", "OK");
            }
        }

        if (GUILayout.Button("Cancel"))
        {
            ResetCreateLocationDialog();
        }

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void ResetCreateActivityDialog()
    {
        showCreateActivityDialog = false;
        newActivityName = "";
        newActivityDescription = "";
    }

    private void ResetCreateVariantDialog()
    {
        showCreateVariantDialog = false;
        newVariantName = "";
        newVariantDescription = "";
        targetActivityForVariant = null;
    }

    private void ResetCreatePOIDialog()
    {
        showCreatePOIDialog = false;
        newPOIName = "";
        newLocationID = "";
    }

    private void ResetCreateLocationDialog()
    {
        showCreateLocationDialog = false;
        newLocationID = "";
        newLocationName = "";
        newLocationDescription = "";
    }
    #endregion

    #region Creation Logic
    private void CreateNewActivity(string activityName, string description)
    {
        // Generate ActivityID from name
        string activityID = GenerateIDFromName(activityName);

        // Create the ScriptableObject
        ActivityDefinition newActivity = CreateInstance<ActivityDefinition>();
        newActivity.ActivityName = activityName;
        newActivity.ActivityID = activityID;
        newActivity.BaseDescription = description;
        newActivity.IsAvailable = true;

        // Save to appropriate folder
        string assetPath = $"Assets/ScriptableObjects/Activities/{activityName}.asset";
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        AssetDatabase.CreateAsset(newActivity, assetPath);
        AssetDatabase.SaveAssets();

        // Create folder for variants
        string variantFolderPath = $"Assets/ScriptableObjects/Activities/ActivitiesVariant/{activityName}";
        if (!AssetDatabase.IsValidFolder(variantFolderPath))
        {
            AssetDatabase.CreateFolder("Assets/ScriptableObjects/Activities/ActivitiesVariant", activityName);
        }

        // Create LocationActivity wrapper and add to registry
        if (activityRegistry != null)
        {
            LocationActivity locationActivity = new LocationActivity
            {
                ActivityReference = newActivity,
                IsAvailable = true,
                ActivityVariants = new List<ActivityVariant>()
            };

            // Automatically find and assign existing variants that match this activity
            AssignExistingVariants(locationActivity, activityID);

            activityRegistry.AllActivities.Add(locationActivity);
            EditorUtility.SetDirty(activityRegistry);
            AssetDatabase.SaveAssets();
        }

        // Select the new activity
        Selection.activeObject = newActivity;
        EditorGUIUtility.PingObject(newActivity);

        Debug.Log($"Created new activity: {activityName} (ID: {activityID}) with variant folder");
        LoadRegistries(); // Refresh
    }

    private void CreateNewVariant(ActivityDefinition parentActivity, string variantName, string description)
    {
        // Create the variant ScriptableObject
        ActivityVariant newVariant = CreateInstance<ActivityVariant>();
        newVariant.VariantName = variantName;
        newVariant.VariantDescription = description;
        // Note: L'utilisateur peut configurer le reste dans l'Inspector

        // Save to appropriate folder (inside the parent activity's folder)
        string activityFolderPath = $"Assets/ScriptableObjects/Activities/ActivitiesVariant/{parentActivity.ActivityName}";

        // Create folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder(activityFolderPath))
        {
            AssetDatabase.CreateFolder("Assets/ScriptableObjects/Activities/ActivitiesVariant", parentActivity.ActivityName);
        }

        string assetPath = $"{activityFolderPath}/{variantName}.asset";
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        AssetDatabase.CreateAsset(newVariant, assetPath);
        AssetDatabase.SaveAssets();

        // Select the new variant for editing
        Selection.activeObject = newVariant;
        EditorGUIUtility.PingObject(newVariant);

        Debug.Log($"Created new variant: {variantName} for activity {parentActivity.GetDisplayName()} in folder {activityFolderPath}");
    }

    /// <summary>
    /// CORRIGÉ : Cree un nouveau POI dans la scène avec collider adapte a la taille
    /// </summary>
    private void CreateNewPOI(string poiName, string locationID)
    {
        try
        {
            // Validation des paramètres
            if (string.IsNullOrEmpty(poiName) || string.IsNullOrEmpty(locationID))
            {
                EditorUtility.DisplayDialog("Erreur", "Le nom du POI et l'ID de location ne peuvent pas être vides.", "OK");
                return;
            }

            // Initialiser locationLookup si necessaire
            if (locationLookup == null || locationLookup.Count == 0)
            {
                BuildLocationLookup();
            }

            // Trouver ou creer le GameObject WorldMap
            GameObject worldMapObject = FindOrCreateWorldMap();
            if (worldMapObject == null)
            {
                EditorUtility.DisplayDialog("Erreur", "Impossible de trouver ou creer le GameObject 'WorldMap' dans la scène.", "OK");
                return;
            }

            // Calculer le nom avec le bon format POI_name_number
            string finalPOIName = GeneratePOIName(poiName, worldMapObject);

            // Create GameObject as child of WorldMap
            GameObject poiObject = new GameObject(finalPOIName);
            poiObject.transform.SetParent(worldMapObject.transform, false);

            // NOUVEAU : Add visual representation FIRST
            SpriteRenderer spriteRenderer = poiObject.AddComponent<SpriteRenderer>();
            spriteRenderer.color = Color.white;

            // Optionnel : Vous pouvez assigner un sprite par defaut ici
            // spriteRenderer.sprite = // votre sprite par defaut

            // MODIFIÉ : Add required Collider2D component avec taille adaptee
            BoxCollider2D collider = poiObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = false; // POI needs clickable collider

            // NOUVEAU : Adapter la taille du collider au sprite ou utiliser une taille par defaut
            SetColliderSizeToSprite(collider, spriteRenderer);

            // Add POI component
            POI poiComponent = poiObject.AddComponent<POI>();
            poiComponent.LocationID = locationID;

            // Position intelligente du POI - AU CENTRE DE WORLDMAP
            Vector3 newPosition = CalculatePOIPosition(worldMapObject);
            poiObject.transform.position = newPosition;

            // Creer le TravelStartPoint enfant
            GameObject travelStartPoint = CreateTravelStartPoint(poiObject);

            // Assigner le TravelStartPoint au POI
            AssignTravelStartPointToPOI(poiComponent, travelStartPoint.transform);

            // Handle location creation if needed
            HandleLocationCreation(locationID);

            // Select the new POI
            Selection.activeObject = poiObject;
            EditorGUIUtility.PingObject(poiObject);

            Debug.Log($"✅ Created new POI: {finalPOIName} with LocationID: {locationID} at position {newPosition}");
            Debug.Log($"   └── Parent: {worldMapObject.name}");
            Debug.Log($"   └── TravelStartPoint: {travelStartPoint.name}");
            Debug.Log($"   └── Collider size: {collider.size}");

            // Refresh safely
            RefreshPOIListSafely();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ Error creating POI '{poiName}': {ex.Message}");
            EditorUtility.DisplayDialog("Erreur", $"Impossible de creer le POI:\n{ex.Message}", "OK");
        }
    }

    /// <summary>
    /// NOUVEAU : Adapte la taille du collider a celle du sprite ou utilise une taille par defaut
    /// </summary>
    private void SetColliderSizeToSprite(BoxCollider2D collider, SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer.sprite != null)
        {
            // Utiliser la taille du sprite en unites Unity
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            collider.size = spriteSize;

            Debug.Log($"   └── Collider size set to sprite size: {spriteSize}");
        }
        else
        {
            // Taille par defaut quand aucun sprite n'est assigne
            collider.size = Vector2.one;

            Debug.Log($"   └── No sprite assigned, using default collider size: {Vector2.one}");
            Debug.Log($"   └── Tip: Assign a sprite to the SpriteRenderer and call 'Fit Collider to Sprite' to auto-adjust");
        }
    }

    /// <summary>
    /// BONUS : Methode utilitaire pour ajuster le collider a un sprite après coup
    /// </summary>
    private void FitColliderToSprite(POI poi)
    {
        if (poi == null) return;

        SpriteRenderer spriteRenderer = poi.GetComponent<SpriteRenderer>();
        BoxCollider2D collider = poi.GetComponent<BoxCollider2D>();

        if (spriteRenderer != null && collider != null)
        {
            SetColliderSizeToSprite(collider, spriteRenderer);
            EditorUtility.SetDirty(poi.gameObject);
            Debug.Log($"Fitted collider to sprite for POI: {poi.gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"POI {poi.gameObject.name} missing SpriteRenderer or BoxCollider2D component");
        }
    }

    /// <summary>
    /// NOUVEAU : Calcule une position intelligente pour le nouveau POI
    /// </summary>
    private Vector3 CalculatePOIPosition(GameObject worldMapParent = null)
    {
        // PRIORITÉ 1 : Centre du WorldMap s'il est fourni
        if (worldMapParent != null)
        {
            Vector3 worldMapCenter = worldMapParent.transform.position;
            Debug.Log($"   └── Positioning POI at WorldMap center: {worldMapCenter}");
            return worldMapCenter;
        }

        // PRIORITÉ 2 : Chercher WorldMap dans la scène
        GameObject worldMap = GameObject.Find("WorldMap");
        if (worldMap != null)
        {
            Vector3 worldMapCenter = worldMap.transform.position;
            Debug.Log($"   └── Found WorldMap, using center: {worldMapCenter}");
            return worldMapCenter;
        }

        // PRIORITÉ 3 : Essayer de positionner près de la camera de la SceneView
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null && sceneView.camera != null)
        {
            try
            {
                Vector3 cameraPos = sceneView.camera.transform.position;
                Vector3 cameraForward = sceneView.camera.transform.forward;
                Vector3 sceneViewPosition = cameraPos + cameraForward * 10f;
                Debug.Log($"   └── Using SceneView camera position: {sceneViewPosition}");
                return sceneViewPosition;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not use SceneView camera position: {ex.Message}");
            }
        }

        // PRIORITÉ 4 : Position par defaut si rien d'autre ne fonctionne
        Debug.Log($"   └── Using default position: Vector3.zero");
        return Vector3.zero;
    }

    /// <summary>
    /// NOUVEAU : Gère la creation de location de manière securisee
    /// </summary>
    private void HandleLocationCreation(string locationID)
    {
        if (locationRegistry != null && !locationLookup.ContainsKey(locationID))
        {
            // Note l'ID pour la creation ulterieure mais ne montre pas le dialog maintenant
            // (pour eviter les problèmes de GUI Layout)
            EditorApplication.delayCall += () =>
            {
                bool createLocation = EditorUtility.DisplayDialog(
                    "Location Non Trouvee",
                    $"La location '{locationID}' n'existe pas dans LocationRegistry.\n\nLa creer maintenant ?",
                    "Creer", "Ignorer");

                if (createLocation)
                {
                    // Preparer les donnees pour le dialog de creation
                    newLocationID = locationID;
                    newLocationName = locationID; // Nom par defaut
                    newLocationDescription = "";
                    showCreateLocationDialog = true;

                    // Forcer un repaint de la fenêtre
                    Repaint();
                }
            };
        }
    }

    /// <summary>
    /// NOUVEAU : Version securisee de RefreshPOIList qui ne cause pas d'erreurs
    /// </summary>
    private void RefreshPOIListSafely()
    {
        try
        {
            FindAllPOIsInScene();
            BuildLocationLookup();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Warning during POI refresh: {ex.Message}");
        }
    }

    /// <summary>
    /// NOUVEAU : Trouve le GameObject WorldMap ou le cree s'il n'existe pas
    /// </summary>
    private GameObject FindOrCreateWorldMap()
    {
        // Chercher d'abord dans la scène
        GameObject worldMapObject = GameObject.Find("WorldMap");

        if (worldMapObject != null)
        {
            Debug.Log($"Found existing WorldMap: {worldMapObject.name}");
            return worldMapObject;
        }

        // Si pas trouve, proposer de le creer
        bool createWorldMap = EditorUtility.DisplayDialog(
            "WorldMap Non Trouve",
            "Le GameObject 'WorldMap' n'existe pas dans la scène.\n\nLe creer maintenant ?",
            "Creer", "Annuler");

        if (createWorldMap)
        {
            worldMapObject = new GameObject("WorldMap");
            Debug.Log($"✅ Created new WorldMap GameObject");
            return worldMapObject;
        }

        return null;
    }

    /// <summary>
    /// NOUVEAU : Genère le nom du POI au format POI_name_number
    /// </summary>
    private string GeneratePOIName(string baseName, GameObject worldMapParent)
    {
        // Compter les POIs existants avec le même nom de base
        POI[] existingPOIs = worldMapParent.GetComponentsInChildren<POI>();
        int count = 1;

        // Chercher le prochain numero disponible
        string basePattern = $"POI_{baseName}_";

        foreach (POI poi in existingPOIs)
        {
            if (poi.gameObject.name.StartsWith(basePattern))
            {
                // Extraire le numero a la fin
                string numberPart = poi.gameObject.name.Substring(basePattern.Length);
                if (int.TryParse(numberPart, out int existingNumber) && existingNumber >= count)
                {
                    count = existingNumber + 1;
                }
            }
        }

        return $"POI_{baseName}_{count:D2}"; // Format avec 2 chiffres (01, 02, etc.)
    }

    /// <summary>
    /// NOUVEAU : Cree le GameObject TravelStartPoint enfant
    /// </summary>
    private GameObject CreateTravelStartPoint(GameObject poiParent)
    {
        GameObject travelStartPoint = new GameObject("TravelStartPoint");
        travelStartPoint.transform.SetParent(poiParent.transform, false);

        // Position legèrement decalee du POI pour être visible
        travelStartPoint.transform.localPosition = new Vector3(0.5f, 0.5f, 0f);

        // Ajouter un petit gizmo visuel pour l'identifier dans l'editeur
        // (optionnel - vous pouvez enlever ceci si vous ne voulez pas de sprite)
        SpriteRenderer travelRenderer = travelStartPoint.AddComponent<SpriteRenderer>();
        travelRenderer.color = new Color(0f, 1f, 1f, 0.5f); // Cyan translucide

        Debug.Log($"   └── Created TravelStartPoint as child of {poiParent.name}");
        return travelStartPoint;
    }

    /// <summary>
    /// NOUVEAU : Assigne le TravelStartPoint au component POI via reflection
    /// </summary>
    private void AssignTravelStartPointToPOI(POI poiComponent, Transform travelStartPoint)
    {
        try
        {
            // Utiliser la reflection pour acceder au champ prive travelPathStartPoint
            var field = typeof(POI).GetField("travelPathStartPoint",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(poiComponent, travelStartPoint);
                Debug.Log($"   └── Assigned TravelStartPoint to POI component");
            }
            else
            {
                Debug.LogWarning("Could not find travelPathStartPoint field in POI component. You may need to assign it manually.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to auto-assign TravelStartPoint: {ex.Message}. Please assign manually in the Inspector.");
        }
    }

    /// <summary>
    /// NOUVEAU : Validation du setup avant creation de POI
    /// </summary>
    private bool ValidateSetupForPOICreation()
    {
        if (locationRegistry == null)
        {
            EditorUtility.DisplayDialog("Setup Incomplet",
                "Vous devez d'abord selectionner un LocationRegistry avant de creer des POIs.", "OK");
            return false;
        }

        if (locationLookup == null)
        {
            BuildLocationLookup();
        }

        return true;
    }

    private void CreateNewLocation(string locationID, string displayName, string description)
    {
        // Create the MapLocationDefinition ScriptableObject
        MapLocationDefinition newLocation = CreateInstance<MapLocationDefinition>();
        newLocation.LocationID = locationID;
        newLocation.DisplayName = displayName;
        newLocation.Description = description;
        newLocation.AvailableActivities = new List<LocationActivity>();
        newLocation.Connections = new List<LocationConnection>();

        // Save to appropriate folder
        string assetPath = $"Assets/ScriptableObjects/MapLocation/{displayName}.asset";
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        AssetDatabase.CreateAsset(newLocation, assetPath);
        AssetDatabase.SaveAssets();

        // Add to LocationRegistry
        if (locationRegistry != null)
        {
            locationRegistry.AllLocations.Add(newLocation);
            EditorUtility.SetDirty(locationRegistry);
            AssetDatabase.SaveAssets();
        }

        // Select the new location
        Selection.activeObject = newLocation;
        EditorGUIUtility.PingObject(newLocation);

        Debug.Log($"Created new location: {displayName} (ID: {locationID})");
        LoadRegistries(); // Refresh
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

    private void AssignExistingVariants(LocationActivity locationActivity, string activityID)
    {
        // Find all ActivityVariant assets in the project
        string[] variantGuids = AssetDatabase.FindAssets("t:ActivityVariant");
        int assignedCount = 0;

        foreach (string guid in variantGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ActivityVariant variant = AssetDatabase.LoadAssetAtPath<ActivityVariant>(path);

            if (variant != null && variant.GetParentActivityID() == activityID)
            {
                // Check if not already in the list
                if (!locationActivity.ActivityVariants.Contains(variant))
                {
                    locationActivity.ActivityVariants.Add(variant);
                    assignedCount++;
                    Debug.Log($"Auto-assigned existing variant '{variant.VariantName}' to activity '{activityID}'");
                }
            }
        }

        // NOUVEAU : Aussi synchroniser ces variants avec toutes les autres locations qui utilisent cette activite
        if (assignedCount > 0 && locationRegistry != null)
        {
            foreach (var variant in locationActivity.ActivityVariants)
            {
                SynchronizeVariantAcrossAllLocations(locationActivity.ActivityReference, variant, true);
            }
        }

        if (assignedCount > 0)
        {
            Debug.Log($"Successfully auto-assigned {assignedCount} existing variants to activity '{activityID}' and synchronized across all locations");
        }
        else
        {
            Debug.Log($"No existing variants found for activity '{activityID}'");
        }
    }
    #endregion

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical("box");

        GUILayout.Label("Activity Manager", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        // Registry selection
        activityRegistry = (ActivityRegistry)EditorGUILayout.ObjectField("Activity Registry", activityRegistry, typeof(ActivityRegistry), false);
        locationRegistry = (LocationRegistry)EditorGUILayout.ObjectField("Location Registry", locationRegistry, typeof(LocationRegistry), false);

        if (GUILayout.Button("Refresh", GUILayout.Width(60)))
        {
            LoadRegistries();
            RefreshPOIList();
        }

        if (GUILayout.Button("Validate", GUILayout.Width(60)))
        {
            ValidateRegistries();
        }

        EditorGUILayout.EndHorizontal();

        // Search
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        searchFilter = EditorGUILayout.TextField(searchFilter);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    #region Activities Tab (Enhanced)
    private void DrawActivitiesTab()
    {
        if (activityRegistry == null)
        {
            EditorGUILayout.HelpBox("Select an ActivityRegistry to manage activities.", MessageType.Info);
            return;
        }

        // Create New Activity button
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create New Activity", GUILayout.Width(150)))
        {
            showCreateActivityDialog = true;
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        var filteredActivities = GetFilteredActivities();

        foreach (var locationActivity in filteredActivities)
        {
            DrawActivityEntry(locationActivity);
        }

        if (filteredActivities.Count == 0)
        {
            EditorGUILayout.HelpBox("No activities found.", MessageType.Info);
        }
    }

    private void DrawActivityEntry(LocationActivity locationActivity)
    {
        if (locationActivity?.ActivityReference == null) return;

        var activity = locationActivity.ActivityReference;

        EditorGUILayout.BeginVertical("box");

        // Activity header
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField(activity.GetDisplayName(), EditorStyles.boldLabel, GUILayout.Width(200));
        EditorGUILayout.LabelField($"ID: {activity.ActivityID}", EditorStyles.miniLabel);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Edit Activity", GUILayout.Width(80)))
        {
            Selection.activeObject = activity;
            EditorGUIUtility.PingObject(activity);
        }

        EditorGUILayout.EndHorizontal();

        // Description
        if (!string.IsNullOrEmpty(activity.BaseDescription))
        {
            EditorGUILayout.LabelField(activity.BaseDescription, EditorStyles.wordWrappedMiniLabel);
        }

        EditorGUILayout.Space();

        // Variants section
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Associated Variants:", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Create Variant", GUILayout.Width(100)))
        {
            targetActivityForVariant = activity;
            showCreateVariantDialog = true;
        }
        EditorGUILayout.EndHorizontal();

        // Current variants list
        if (locationActivity.ActivityVariants != null && locationActivity.ActivityVariants.Count > 0)
        {
            for (int i = 0; i < locationActivity.ActivityVariants.Count; i++)
            {
                DrawVariantEntry(locationActivity, i);
            }
        }
        else
        {
            EditorGUILayout.LabelField("  No variants assigned", EditorStyles.miniLabel);
        }

        // Add existing variant
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Add Existing Variant:", GUILayout.Width(130));

        ActivityVariant newVariant = (ActivityVariant)EditorGUILayout.ObjectField(null, typeof(ActivityVariant), false);

        if (newVariant != null)
        {
            AddVariantToActivity(locationActivity, newVariant);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawVariantEntry(LocationActivity locationActivity, int index)
    {
        var variant = locationActivity.ActivityVariants[index];

        if (variant == null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("  [Missing Variant]", EditorStyles.miniLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                RemoveVariantFromActivity(locationActivity, index);
            }

            EditorGUILayout.EndHorizontal();
            return;
        }

        EditorGUILayout.BeginHorizontal();

        // Variant info
        EditorGUILayout.LabelField($"  • {variant.VariantName}", EditorStyles.miniLabel, GUILayout.Width(150));

        if (variant.PrimaryResource != null)
        {
            EditorGUILayout.LabelField($"→ {variant.PrimaryResource.ItemName}", EditorStyles.miniLabel, GUILayout.Width(100));
        }

        EditorGUILayout.LabelField($"{variant.ActionCost} steps", EditorStyles.miniLabel, GUILayout.Width(60));

        GUILayout.FlexibleSpace();

        // Buttons
        if (GUILayout.Button("Edit", GUILayout.Width(40)))
        {
            Selection.activeObject = variant;
            EditorGUIUtility.PingObject(variant);
        }

        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            RemoveVariantFromActivity(locationActivity, index);
        }

        EditorGUILayout.EndHorizontal();
    }

    private List<LocationActivity> GetFilteredActivities()
    {
        if (activityRegistry == null) return new List<LocationActivity>();

        var activities = activityRegistry.AllActivities.Where(a => a?.ActivityReference != null);

        if (!string.IsNullOrEmpty(searchFilter))
        {
            activities = activities.Where(a =>
                a.ActivityReference.GetDisplayName().ToLower().Contains(searchFilter.ToLower()) ||
                a.ActivityReference.ActivityID.ToLower().Contains(searchFilter.ToLower()));
        }

        return activities.ToList();
    }
    #endregion

    #region POI Management Tab (Enhanced)
    private void DrawPOIManagementTab()
    {
        if (locationRegistry == null)
        {
            EditorGUILayout.HelpBox("Select a LocationRegistry to manage POI assignments.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Create New POI", GUILayout.Width(120)))
        {
            showCreatePOIDialog = true;
        }

        if (GUILayout.Button("Refresh POI List", GUILayout.Width(120)))
        {
            RefreshPOIList();
        }

        if (GUILayout.Button("Find All POIs in Scene", GUILayout.Width(150)))
        {
            FindAllPOIsInScene();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (allPOIs == null || allPOIs.Length == 0)
        {
            EditorGUILayout.HelpBox("No POIs found in the current scene. Make sure your scene contains POI objects.", MessageType.Warning);

            if (GUILayout.Button("Scan Scene for POIs"))
            {
                FindAllPOIsInScene();
            }
            return;
        }

        EditorGUILayout.LabelField($"POIs Found: {allPOIs.Length}", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Filter POIs by search
        var filteredPOIs = FilterPOIs();

        foreach (var poi in filteredPOIs)
        {
            DrawPOIEntry(poi);
        }
    }

    private void DrawPOIEntry(POI poi)
    {
        if (poi == null) return;

        EditorGUILayout.BeginVertical("box");

        // POI Header
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField($"🏛️ {poi.gameObject.name}", EditorStyles.boldLabel, GUILayout.Width(200));
        EditorGUILayout.LabelField($"Location ID: {poi.LocationID}", EditorStyles.miniLabel);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Select POI", GUILayout.Width(80)))
        {
            Selection.activeObject = poi.gameObject;
            EditorGUIUtility.PingObject(poi.gameObject);
        }

        EditorGUILayout.EndHorizontal();

        // Location info
        var location = GetLocationForPOI(poi);
        if (location != null)
        {
            EditorGUILayout.LabelField($"📍 Location: {location.DisplayName}", EditorStyles.miniLabel);

            if (!string.IsNullOrEmpty(location.Description))
            {
                EditorGUILayout.LabelField($"Description: {location.Description}", EditorStyles.wordWrappedMiniLabel);
            }
        }
        else
        {
            EditorGUILayout.LabelField($"❌ Location '{poi.LocationID}' not found in LocationRegistry!", EditorStyles.miniLabel);

            if (GUILayout.Button("Create Missing Location"))
            {
                newLocationID = poi.LocationID;
                newLocationName = poi.LocationID; // Default name
                showCreateLocationDialog = true;
            }
        }

        EditorGUILayout.Space();

        // Activities management
        DrawPOIActivitiesSection(poi, location);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawPOIActivitiesSection(POI poi, MapLocationDefinition location)
    {
        if (location == null) return;

        EditorGUILayout.LabelField("🎯 Available Activities:", EditorStyles.boldLabel);

        // Current activities
        if (location.AvailableActivities != null && location.AvailableActivities.Count > 0)
        {
            for (int i = 0; i < location.AvailableActivities.Count; i++)
            {
                DrawLocationActivityEntry(location, i);
            }
        }
        else
        {
            EditorGUILayout.LabelField("  No activities assigned", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space();

        // Add new activity
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Add Activity:", GUILayout.Width(80));

        ActivityDefinition newActivity = (ActivityDefinition)EditorGUILayout.ObjectField(null, typeof(ActivityDefinition), false);

        if (newActivity != null)
        {
            AddActivityToLocation(location, newActivity);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawLocationActivityEntry(MapLocationDefinition location, int activityIndex)
    {
        var locationActivity = location.AvailableActivities[activityIndex];

        if (locationActivity?.ActivityReference == null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("  [Missing Activity Reference]", EditorStyles.miniLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                RemoveActivityFromLocation(location, activityIndex);
            }

            EditorGUILayout.EndHorizontal();
            return;
        }

        var activity = locationActivity.ActivityReference;

        EditorGUILayout.BeginVertical("helpBox");

        // Activity header
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"⚡ {activity.GetDisplayName()}", EditorStyles.boldLabel, GUILayout.Width(150));

        locationActivity.IsAvailable = EditorGUILayout.Toggle("Available", locationActivity.IsAvailable, GUILayout.Width(80));

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Edit", GUILayout.Width(40)))
        {
            Selection.activeObject = activity;
            EditorGUIUtility.PingObject(activity);
        }

        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            RemoveActivityFromLocation(location, activityIndex);
        }

        EditorGUILayout.EndHorizontal();

        // Variants management
        EditorGUILayout.LabelField("Variants:", EditorStyles.miniLabel);

        if (locationActivity.ActivityVariants != null && locationActivity.ActivityVariants.Count > 0)
        {
            for (int i = 0; i < locationActivity.ActivityVariants.Count; i++)
            {
                DrawLocationVariantEntry(locationActivity, i);
            }
        }
        else
        {
            EditorGUILayout.LabelField("    No variants assigned", EditorStyles.miniLabel);
        }

        // Add variant
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("    Add Variant:", GUILayout.Width(100));

        ActivityVariant newVariant = (ActivityVariant)EditorGUILayout.ObjectField(null, typeof(ActivityVariant), false);

        if (newVariant != null)
        {
            AddVariantToLocationActivity(locationActivity, newVariant);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawLocationVariantEntry(LocationActivity locationActivity, int index)
    {
        var variant = locationActivity.ActivityVariants[index];

        if (variant == null)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("    [Missing Variant]", EditorStyles.miniLabel);

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                RemoveVariantFromLocationActivity(locationActivity, index);
            }

            EditorGUILayout.EndHorizontal();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"    • {variant.VariantName}", EditorStyles.miniLabel, GUILayout.Width(150));

        if (variant.PrimaryResource != null)
        {
            EditorGUILayout.LabelField($"→ {variant.PrimaryResource.ItemName}", EditorStyles.miniLabel, GUILayout.Width(100));
        }

        EditorGUILayout.LabelField($"{variant.ActionCost} steps", EditorStyles.miniLabel, GUILayout.Width(60));

        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Edit", GUILayout.Width(40)))
        {
            Selection.activeObject = variant;
            EditorGUIUtility.PingObject(variant);
        }

        if (GUILayout.Button("Remove", GUILayout.Width(60)))
        {
            RemoveVariantFromLocationActivity(locationActivity, index);
        }

        EditorGUILayout.EndHorizontal();
    }
    #endregion

    #region POI Management Logic
    private void RefreshPOIList()
    {
        FindAllPOIsInScene();
        BuildLocationLookup();
    }

    /// <summary>
    /// AMÉLIORÉ : Version plus robuste de FindAllPOIsInScene
    /// </summary>
    private void FindAllPOIsInScene()
    {
        try
        {
            allPOIs = FindObjectsOfType<POI>();
            Debug.Log($"ActivityManager: Found {allPOIs?.Length ?? 0} POIs in the current scene");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error finding POIs in scene: {ex.Message}");
            allPOIs = new POI[0]; // Array vide pour eviter les erreurs
        }
    }

    /// <summary>
    /// AMÉLIORÉ : Version plus robuste de BuildLocationLookup
    /// </summary>
    private void BuildLocationLookup()
    {
        if (locationLookup == null)
        {
            locationLookup = new Dictionary<string, MapLocationDefinition>();
        }
        else
        {
            locationLookup.Clear();
        }

        if (locationRegistry?.AllLocations != null)
        {
            foreach (var location in locationRegistry.AllLocations)
            {
                if (location != null && !string.IsNullOrEmpty(location.LocationID))
                {
                    // Éviter les doublons
                    if (!locationLookup.ContainsKey(location.LocationID))
                    {
                        locationLookup[location.LocationID] = location;
                    }
                    else
                    {
                        Debug.LogWarning($"Duplicate LocationID found: {location.LocationID}");
                    }
                }
            }
        }

        Debug.Log($"LocationLookup initialized with {locationLookup.Count} locations");
    }

    private POI[] FilterPOIs()
    {
        if (string.IsNullOrEmpty(searchFilter))
            return allPOIs;

        return allPOIs.Where(poi =>
            poi.gameObject.name.ToLower().Contains(searchFilter.ToLower()) ||
            poi.LocationID.ToLower().Contains(searchFilter.ToLower())
        ).ToArray();
    }

    private MapLocationDefinition GetLocationForPOI(POI poi)
    {
        if (poi == null || string.IsNullOrEmpty(poi.LocationID))
            return null;

        locationLookup.TryGetValue(poi.LocationID, out MapLocationDefinition location);
        return location;
    }

    private void AddActivityToLocation(MapLocationDefinition location, ActivityDefinition activity)
    {
        if (location.AvailableActivities == null)
        {
            location.AvailableActivities = new List<LocationActivity>();
        }

        // Check if already exists
        bool alreadyExists = location.AvailableActivities.Any(la =>
            la?.ActivityReference == activity);

        if (alreadyExists)
        {
            EditorUtility.DisplayDialog("Already Added",
                $"Activity '{activity.GetDisplayName()}' is already assigned to this location.", "OK");
            return;
        }

        // Create new LocationActivity
        var newLocationActivity = new LocationActivity
        {
            ActivityReference = activity,
            IsAvailable = true,
            ActivityVariants = new List<ActivityVariant>()
        };

        location.AvailableActivities.Add(newLocationActivity);

        MarkLocationDirty(location);
        Debug.Log($"Added activity '{activity.GetDisplayName()}' to location '{location.DisplayName}'");
    }

    private void RemoveActivityFromLocation(MapLocationDefinition location, int index)
    {
        if (location.AvailableActivities != null && index >= 0 && index < location.AvailableActivities.Count)
        {
            var activityName = location.AvailableActivities[index]?.ActivityReference?.GetDisplayName() ?? "Unknown";
            location.AvailableActivities.RemoveAt(index);

            MarkLocationDirty(location);
            Debug.Log($"Removed activity '{activityName}' from location '{location.DisplayName}'");
        }
    }

    private void AddVariantToLocationActivity(LocationActivity locationActivity, ActivityVariant variant)
    {
        if (locationActivity.ActivityVariants == null)
        {
            locationActivity.ActivityVariants = new List<ActivityVariant>();
        }

        if (locationActivity.ActivityVariants.Contains(variant))
        {
            EditorUtility.DisplayDialog("Already Added",
                $"Variant '{variant.VariantName}' is already assigned to this activity.", "OK");
            return;
        }

        locationActivity.ActivityVariants.Add(variant);

        // NOUVEAU : Synchroniser avec toutes les autres LocationActivity qui utilisent la même ActivityDefinition
        SynchronizeVariantAcrossAllLocations(locationActivity.ActivityReference, variant, true);

        MarkLocationDirty(FindLocationContaining(locationActivity));
        Debug.Log($"Added variant '{variant.VariantName}' to activity '{locationActivity.ActivityReference.GetDisplayName()}' and synchronized across all locations");
    }

    private void RemoveVariantFromLocationActivity(LocationActivity locationActivity, int index)
    {
        if (locationActivity.ActivityVariants != null && index >= 0 && index < locationActivity.ActivityVariants.Count)
        {
            var variantName = locationActivity.ActivityVariants[index]?.VariantName ?? "Unknown";
            var variant = locationActivity.ActivityVariants[index];
            locationActivity.ActivityVariants.RemoveAt(index);

            // NOUVEAU : Synchroniser la suppression avec toutes les autres LocationActivity
            if (variant != null)
            {
                SynchronizeVariantAcrossAllLocations(locationActivity.ActivityReference, variant, false);
            }

            MarkLocationDirty(FindLocationContaining(locationActivity));
            Debug.Log($"Removed variant '{variantName}' from activity '{locationActivity.ActivityReference.GetDisplayName()}' and synchronized across all locations");
        }
    }

    private MapLocationDefinition FindLocationContaining(LocationActivity locationActivity)
    {
        if (locationRegistry?.AllLocations == null) return null;

        return locationRegistry.AllLocations.FirstOrDefault(loc =>
            loc?.AvailableActivities?.Contains(locationActivity) == true);
    }

    private void MarkLocationDirty(MapLocationDefinition location)
    {
        if (location != null)
        {
            EditorUtility.SetDirty(location);
        }

        if (locationRegistry != null)
        {
            EditorUtility.SetDirty(locationRegistry);
        }

        AssetDatabase.SaveAssets();
    }
    #endregion

    #region Utility Methods (Legacy)
    private void LoadRegistries()
    {
        if (activityRegistry == null)
        {
            string[] guids = AssetDatabase.FindAssets("t:ActivityRegistry");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                activityRegistry = AssetDatabase.LoadAssetAtPath<ActivityRegistry>(path);
            }
        }

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

    private void ValidateRegistries()
    {
        if (activityRegistry != null)
        {
            activityRegistry.ValidateRegistry();
            Debug.Log("ActivityRegistry validation triggered");
        }

        if (locationRegistry != null)
        {
            locationRegistry.ValidateRegistry();
            Debug.Log("LocationRegistry validation triggered");
        }
    }

    private void AddVariantToActivity(LocationActivity locationActivity, ActivityVariant variant)
    {
        if (locationActivity.ActivityVariants == null)
        {
            locationActivity.ActivityVariants = new List<ActivityVariant>();
        }

        if (locationActivity.ActivityVariants.Contains(variant))
        {
            EditorUtility.DisplayDialog("Already Added", $"Variant '{variant.VariantName}' is already associated with this activity.", "OK");
            return;
        }

        locationActivity.ActivityVariants.Add(variant);

        // NOUVEAU : Synchroniser avec toutes les autres LocationActivity qui utilisent la même ActivityDefinition
        SynchronizeVariantAcrossAllLocations(locationActivity.ActivityReference, variant, true);

        EditorUtility.SetDirty(activityRegistry);
        AssetDatabase.SaveAssets();

        if (activityRegistry != null)
        {
            activityRegistry.ValidateRegistry();
        }

        Debug.Log($"Added variant '{variant.VariantName}' to activity '{locationActivity.ActivityReference.GetDisplayName()}' and synchronized across all locations");
    }

    private void RemoveVariantFromActivity(LocationActivity locationActivity, int index)
    {
        if (locationActivity.ActivityVariants != null && index >= 0 && index < locationActivity.ActivityVariants.Count)
        {
            var variantName = locationActivity.ActivityVariants[index]?.VariantName ?? "Unknown";
            var variant = locationActivity.ActivityVariants[index];
            locationActivity.ActivityVariants.RemoveAt(index);

            // NOUVEAU : Synchroniser la suppression avec toutes les autres LocationActivity
            if (variant != null)
            {
                SynchronizeVariantAcrossAllLocations(locationActivity.ActivityReference, variant, false);
            }

            EditorUtility.SetDirty(activityRegistry);
            AssetDatabase.SaveAssets();

            if (activityRegistry != null)
            {
                activityRegistry.ValidateRegistry();
            }

            Debug.Log($"Removed variant '{variantName}' from activity '{locationActivity.ActivityReference.GetDisplayName()}' and synchronized across all locations");
        }
    }

    /// <summary>
    /// Synchronise l'ajout/suppression d'un variant a travers toutes les LocationActivity qui utilisent la même ActivityDefinition
    /// </summary>
    private void SynchronizeVariantAcrossAllLocations(ActivityDefinition activityDef, ActivityVariant variant, bool add)
    {
        if (activityDef == null || variant == null || locationRegistry == null) return;

        int syncCount = 0;

        // Parcourir tous les MapLocationDefinition du LocationRegistry
        foreach (var location in locationRegistry.AllLocations.Where(l => l != null))
        {
            if (location.AvailableActivities == null) continue;

            // Trouver les LocationActivity qui utilisent cette ActivityDefinition
            foreach (var locActivity in location.AvailableActivities.Where(la => la?.ActivityReference == activityDef))
            {
                if (locActivity.ActivityVariants == null)
                {
                    locActivity.ActivityVariants = new List<ActivityVariant>();
                }

                if (add)
                {
                    // Ajouter le variant s'il n'existe pas deja
                    if (!locActivity.ActivityVariants.Contains(variant))
                    {
                        locActivity.ActivityVariants.Add(variant);
                        syncCount++;
                        EditorUtility.SetDirty(location);
                    }
                }
                else
                {
                    // Supprimer le variant s'il existe
                    if (locActivity.ActivityVariants.Remove(variant))
                    {
                        syncCount++;
                        EditorUtility.SetDirty(location);
                    }
                }
            }
        }

        if (syncCount > 0)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"Synchronized variant '{variant.VariantName}' across {syncCount} location activities");
        }
    }
    #endregion
}
#endif