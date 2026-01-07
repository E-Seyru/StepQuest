// Purpose: ScriptableObject defining an NPC for social interactions
// Filepath: Assets/Scripts/Data/ScriptableObjects/NPCDefinition.cs
using UnityEngine;

/// <summary>
/// Emotion types for NPC dialogue expressions
/// </summary>
public enum NPCEmotion
{
    Neutral,
    Joy,
    Sadness,
    Anger,
    Surprise,
    Fear,
    Curiosity,
    Embarrassment,
    Love
}

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

    [Header("Visual - General")]
    [Tooltip("Small avatar image (for lists, cards)")]
    public Sprite Avatar;

    [Tooltip("Silhouette icon for exploration panel (shown blackened when undiscovered). Falls back to Avatar if not set.")]
    public Sprite SilhouetteIcon;

    [Tooltip("Full illustration image (for interaction panel, details)")]
    public Sprite Illustration;

    [Tooltip("Color theme for this NPC")]
    public Color ThemeColor = Color.white;

    [Header("Visual - Dialogue Emotions")]
    [Tooltip("Neutral expression (default/fallback)")]
    public Sprite EmotionNeutral;

    [Tooltip("Happy/joyful expression")]
    public Sprite EmotionJoy;

    [Tooltip("Sad expression")]
    public Sprite EmotionSadness;

    [Tooltip("Angry expression")]
    public Sprite EmotionAnger;

    [Tooltip("Surprised expression")]
    public Sprite EmotionSurprise;

    [Tooltip("Fearful/scared expression")]
    public Sprite EmotionFear;

    [Tooltip("Curious/interested expression")]
    public Sprite EmotionCuriosity;

    [Tooltip("Embarrassed/shy expression")]
    public Sprite EmotionEmbarrassment;

    [Tooltip("Loving/affectionate expression")]
    public Sprite EmotionLove;

    [Header("Availability")]
    [Tooltip("Is this NPC currently active in the game?")]
    public bool IsActive = true;

    [Header("Dialogues")]
    [Tooltip("All available dialogues for this NPC (selected by priority and conditions)")]
    public System.Collections.Generic.List<DialogueDefinition> Dialogues = new System.Collections.Generic.List<DialogueDefinition>();

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
    /// Get the silhouette icon for exploration panel (falls back to Avatar)
    /// </summary>
    public Sprite GetSilhouetteIcon()
    {
        return SilhouetteIcon != null ? SilhouetteIcon : Avatar;
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

    /// <summary>
    /// Get the sprite for a specific emotion, with fallback to Neutral, then Illustration
    /// </summary>
    public Sprite GetEmotionSprite(NPCEmotion emotion)
    {
        Sprite result = emotion switch
        {
            NPCEmotion.Neutral => EmotionNeutral,
            NPCEmotion.Joy => EmotionJoy,
            NPCEmotion.Sadness => EmotionSadness,
            NPCEmotion.Anger => EmotionAnger,
            NPCEmotion.Surprise => EmotionSurprise,
            NPCEmotion.Fear => EmotionFear,
            NPCEmotion.Curiosity => EmotionCuriosity,
            NPCEmotion.Embarrassment => EmotionEmbarrassment,
            NPCEmotion.Love => EmotionLove,
            _ => EmotionNeutral
        };

        // Fallback chain: requested emotion -> Neutral -> Illustration -> Avatar
        if (result != null) return result;
        if (EmotionNeutral != null) return EmotionNeutral;
        if (Illustration != null) return Illustration;
        return Avatar;
    }

    /// <summary>
    /// Get the default dialogue sprite (Neutral emotion with fallbacks)
    /// </summary>
    public Sprite GetDialogueSprite()
    {
        return GetEmotionSprite(NPCEmotion.Neutral);
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
