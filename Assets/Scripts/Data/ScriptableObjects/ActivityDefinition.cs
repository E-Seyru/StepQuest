// Purpose: ScriptableObject defining general activities available in the game
// Filepath: Assets/Scripts/Data/ScriptableObjects/ActivityDefinition.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New Activity", menuName = "WalkAndRPG/Activity Definition")]
public class ActivityDefinition : ScriptableObject
{
    [Header("Activity Info")]
    [Tooltip("Unique identifier for this activity (e.g., 'mining', 'fishing')")]
    public string ActivityID;

    [Tooltip("Display name for this activity (e.g., 'Miner', 'Pêcher')")]
    public string ActivityName;

    [Tooltip("General description of what this activity does")]
    [TextArea(2, 4)]
    public string BaseDescription;

    [Header("Visual")]
    [Tooltip("Icon representing this activity")]
    public Sprite ActivityIcon;

    [Tooltip("Color theme for this activity (used in UI)")]
    public Color ActivityColor = Color.white;

    [Header("Game Design")]
    [Tooltip("Is this activity currently available in the game?")]
    public bool IsAvailable = true;

    [Tooltip("Minimum level or steps required to unlock this activity")]
    public int UnlockRequirement = 0;

    [Header("Debug")]
    [Tooltip("Notes for developers")]
    [TextArea(1, 3)]
    public string DeveloperNotes;

    /// <summary>
    /// Validate the activity definition
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(ActivityID))
        {
            Debug.LogError($"ActivityDefinition '{name}': ActivityID is empty!");
            return false;
        }

        if (string.IsNullOrEmpty(ActivityName))
        {
            Debug.LogError($"ActivityDefinition '{name}': ActivityName is empty!");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Get display info for UI
    /// </summary>
    public string GetDisplayInfo()
    {
        return $"{ActivityName} ({ActivityID})";
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-generate ActivityID from name if empty
        if (string.IsNullOrEmpty(ActivityID) && !string.IsNullOrEmpty(name))
        {
            ActivityID = name.ToLower().Replace(" ", "_");
        }

        // Auto-generate ActivityName from ActivityID if empty
        if (string.IsNullOrEmpty(ActivityName) && !string.IsNullOrEmpty(ActivityID))
        {
            ActivityName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(ActivityID.Replace("_", " "));
        }
    }
#endif
}