using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class AutoSaveScene
{
    // Static constructor - this runs when Unity starts or recompiles
    static AutoSaveScene()
    {
        // Subscribe to the update event
        EditorApplication.update += Update;

        // Log that autosave is active
        Debug.Log("AutoSave: Script loaded and active");
    }

    // Configuration variables
    private static float saveInterval = 300f; // Save every 5 minutes (300 seconds)
    private static float nextSaveTime = 0f;

    // Keep track of whether we've shown the first save message
    private static bool hasShownStartMessage = false;

    static void Update()
    {
        // Only run in Edit mode (not when playing the game)
        if (EditorApplication.isPlaying || EditorApplication.isPaused)
            return;

        // Show startup message once
        if (!hasShownStartMessage)
        {
            Debug.Log($"AutoSave: Will save scene every {saveInterval / 60f:F1} minutes");
            nextSaveTime = (float)EditorApplication.timeSinceStartup + saveInterval;
            hasShownStartMessage = true;
        }

        // Check if it's time to save
        if (EditorApplication.timeSinceStartup >= nextSaveTime)
        {
            SaveScene();
            nextSaveTime = (float)EditorApplication.timeSinceStartup + saveInterval;
        }
    }

    static void SaveScene()
    {
        // Check if there's an active scene to save
        var activeScene = EditorSceneManager.GetActiveScene();

        if (activeScene != null && !string.IsNullOrEmpty(activeScene.path))
        {
            // Save the current scene
            bool saveSuccessful = EditorSceneManager.SaveScene(activeScene);

            if (saveSuccessful)
            {
                Debug.Log($"AutoSave: Scene '{activeScene.name}' saved successfully at {System.DateTime.Now:HH:mm:ss}");
            }
            else
            {
                Debug.LogWarning("AutoSave: Failed to save scene!");
            }
        }
        else
        {
            Debug.LogWarning("AutoSave: No scene to save or scene hasn't been saved before. Please save your scene manually first.");
        }
    }

    // Menu item to manually trigger save (useful for testing)
    [MenuItem("Tools/AutoSave/Save Scene Now")]
    static void SaveSceneManually()
    {
        SaveScene();
    }

    // Menu item to change save interval
    [MenuItem("Tools/AutoSave/Change Save Interval")]
    static void ChangeSaveInterval()
    {
        string currentInterval = (saveInterval / 60f).ToString("F1");
        string newInterval = EditorInputDialog.Show("Change AutoSave Interval",
            $"Enter save interval in minutes (current: {currentInterval})", currentInterval);

        if (!string.IsNullOrEmpty(newInterval) && float.TryParse(newInterval, out float minutes))
        {
            if (minutes > 0)
            {
                saveInterval = minutes * 60f;
                nextSaveTime = (float)EditorApplication.timeSinceStartup + saveInterval;
                Debug.Log($"AutoSave: Interval changed to {minutes:F1} minutes");
            }
            else
            {
                Debug.LogWarning("AutoSave: Interval must be greater than 0");
            }
        }
    }
}

// Simple input dialog helper class
public class EditorInputDialog : EditorWindow
{
    private string inputText = "";
    private string description = "";
    private bool shouldClose = false;
    private System.Action<string> onComplete;

    public static string Show(string title, string description, string defaultValue = "")
    {
        var window = CreateInstance<EditorInputDialog>();
        window.titleContent = new GUIContent(title);
        window.description = description;
        window.inputText = defaultValue;
        window.ShowModal();

        return window.inputText;
    }

    void OnGUI()
    {
        GUILayout.Label(description, EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);

        inputText = EditorGUILayout.TextField("Value:", inputText);

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("OK"))
        {
            shouldClose = true;
        }
        if (GUILayout.Button("Cancel"))
        {
            inputText = "";
            shouldClose = true;
        }
        GUILayout.EndHorizontal();

        if (shouldClose)
        {
            Close();
        }
    }
}