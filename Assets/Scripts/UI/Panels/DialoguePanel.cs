// Purpose: Script for the panel displaying NPC conversations and player choices.
// Filepath: Assets/Scripts/UI/Panels/DialoguePanel.cs
using UnityEngine;
// using UnityEngine.UI; // For Text, Buttons, Image
using System.Collections.Generic; // For choices list
using System; // For Action

public class DialoguePanel : MonoBehaviour
{
    // TODO: References to UI elements (NPC Name Text, NPC Portrait Image, Dialogue Text, Choices Button container)
    // public Text npcNameText;
    // public Image npcPortraitImage;
    // public Text dialogueText;
    // public Transform choicesContainer;
    // public GameObject choiceButtonPrefab;

    // TODO: Reference a Dialogue Runner/System (e.g., YarnSpinner, Ink, or custom system)
    // private IDialogueSystem dialogueSystem;

    // TODO: Store current state if needed (e.g., waiting for player choice)

    void Start()
    {
        // TODO: Get reference to dialogue system
        // TODO: Subscribe to dialogue system events (OnLineUpdate, OnChoicesUpdate, OnDialogueComplete)
        // TODO: Ensure panel is hidden initially
        // gameObject.SetActive(false);
    }

    // Called by the Dialogue System when a line should be displayed
    public void ShowLine(string speakerName, /* Sprite speakerPortrait, */ string lineText)
    {
        // TODO: Activate the panel if hidden
        // gameObject.SetActive(true);
        // TODO: Update npcNameText, npcPortraitImage, dialogueText
        // TODO: Clear existing choices in choicesContainer
        // TODO: Maybe use a typewriter effect for dialogueText?
        Debug.Log($"{speakerName}: {lineText}");
    }

    // Called by the Dialogue System when choices should be presented
    public void ShowChoices(List</* DialogueChoice */ object> choices)
    {
        // TODO: Ensure panel is active
        // TODO: Clear existing choices in choicesContainer
        // TODO: For each choice:
        //      - Instantiate choiceButtonPrefab into choicesContainer
        //      - Set button text (choice text)
        //      - Add listener to button onClick to call OnChoiceSelected(choiceIndex or ID)
    }

    // Called when a player clicks a choice button
    public void OnChoiceSelected(/* int choiceIndex or ID */ int index)
    {
        // TODO: Tell the dialogue system which choice was selected
        // dialogueSystem.SelectChoice(index);
        // TODO: Clear choices container (dialogue system will likely provide the next line/choices)
        Debug.Log($"DialoguePanel: Choice {index} selected (Placeholder)");
    }

    // Called by the Dialogue System when the conversation ends
    public void HidePanel()
    {
        // TODO: Deactivate the panel
        // gameObject.SetActive(false);
        // TODO: Clear text fields?
        Debug.Log("DialoguePanel: Hiding panel");
    }
}