// Purpose: Script for a reusable UI element representing an ability slot (in combat panel, equipment panel, etc.).
// Filepath: Assets/Scripts/UI/Components/AbilitySlotUI.cs
using UnityEngine;
// using UnityEngine.UI; // For Image, Text

public class AbilitySlotUI : MonoBehaviour
{
    // TODO: References to child UI elements (Icon Image, Cooldown Overlay Image, Cooldown Text)
    // public Image abilityIcon;
    // public Image cooldownOverlay; // e.g., a radial fill image
    // public Text cooldownText;

    // TODO: Store the Ability ID this slot represents (optional)
    // public string AbilityID { get; private set; }

    public void Setup(/* AbilityDefinition */ object abilityDef)
    {
        // TODO: Set AbilityID
        // TODO: Set abilityIcon.sprite from definition
        // TODO: Hide cooldown overlay and text initially
        // cooldownOverlay.fillAmount = 0;
        // cooldownText.gameObject.SetActive(false);
        Debug.Log($"AbilitySlotUI: Setup for {abilityDef} (Placeholder)");
    }

    public void UpdateCooldown(float remainingSeconds, float maxSeconds)
    {
        // TODO: If remainingSeconds > 0:
        //      - Show overlay/text
        //      - Set cooldownOverlay.fillAmount = remainingSeconds / maxSeconds;
        //      - Set cooldownText.text = remainingSeconds.ToString("F1"); // Format as needed
        // else:
        //      - Hide overlay/text
    }

    public void SetEmpty()
    {
        // TODO: Clear icon, hide cooldowns, maybe show default empty graphic
        // abilityIcon.sprite = null; // Or set to default empty sprite
        // AbilityID = null;
    }
}