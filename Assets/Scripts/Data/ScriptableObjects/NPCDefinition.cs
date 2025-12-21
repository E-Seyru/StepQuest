// Purpose: ScriptableObject defining an NPC for social interactions
// Filepath: Assets/Scripts/Data/ScriptableObjects/NPCDefinition.cs
using UnityEngine;

/// <summary>
/// ScriptableObject defining an NPC that can be interacted with
/// </summary>
[CreateAssetMenu(fileName = "NewNPC", menuName = "WalkAndRPG/Social/NPC Definition")]
public class NPCDefinition : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Unique identifier for this NPC")]
    public string NPCID;

    [Tooltip("Display name shown in UI")]
    public string NPCName;

    [TextArea(2, 4)]
    [Tooltip("Short description of this NPC")]
    public string Description;

    [Header("Visual")]
    [Tooltip("Small avatar image (for lists, cards)")]
    public Sprite Avatar;

    [Tooltip("Full illustration image (for dialogue, details)")]
    public Sprite Illustration;

    [Tooltip("Color theme for this NPC")]
    public Color ThemeColor = Color.white;

    [Header("Availability")]
    [Tooltip("Is this NPC currently active in the game?")]
    public bool IsActive = true;

    [Header("Debug")]
    [TextArea(1, 2)]
    public string DeveloperNotes;

    /// <summary>
    /// Get display name for UI
    /// </summary>
    public string GetDisplayName()
    {
        return string.IsNullOrEmpty(NPCName) ? NPCID : NPCName;
    }

    /// <summary>
    /// Validate this NPC definition
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(NPCID)) return false;
        if (string.IsNullOrEmpty(NPCName)) return false;
        return true;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Auto-generate NPCID from name if empty
        if (string.IsNullOrEmpty(NPCID) && !string.IsNullOrEmpty(NPCName))
        {
            NPCID = NPCName.ToLower().Replace(" ", "_").Replace("'", "");
        }
    }
#endif
}
