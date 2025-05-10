// Purpose: Script for the panel displaying player stats, skills, and potentially stat allocation.
// Filepath: Assets/Scripts/UI/Panels/CharacterPanel.cs
using UnityEngine;
// using UnityEngine.UI; // For Text, Buttons, Sliders
// using System.Collections.Generic; // For lists/dictionaries

public class CharacterPanel : MonoBehaviour
{
    // TODO: References to UI elements (Text for stats, HP bar, Skill list container, Stat allocation buttons, Points available text)
    // public Text playerNameText; // If player can name character
    // public Slider hpSlider;
    // public Text hpText;
    // public Text strengthStatText; // Add texts for all relevant stats
    // public Transform skillListContainer;
    // public GameObject skillEntryPrefab; // Prefab for displaying one skill
    // public Text availableStatPointsText;
    // public Button allocateStrengthButton; // Buttons for each stat

    // TODO: Reference PlayerController for stats
    // private PlayerController playerController;
    // TODO: Reference SkillManager for skill levels/XP
    // private SkillManager skillManager;
    // TODO: Reference StatAllocator for available points and allocation
    // private StatAllocator statAllocator;

    void OnEnable()
    {
        // TODO: Get references to managers
        // TODO: Subscribe to events (PlayerStatsChanged, SkillLeveledUp, AvailableStatPointsChanged)
        // TODO: Populate data
        // RefreshStats();
        // RefreshSkills();
        // RefreshStatAllocation();
    }

    void OnDisable()
    {
        // TODO: Unsubscribe from events
    }

    void RefreshStats()
    {
        // TODO: Get calculated stats from PlayerController
        // TODO: Update Text elements for stats (Strength, Int, Stamina, Attack, Defense...)
        // TODO: Update HP slider and text
        Debug.Log("CharacterPanel: RefreshStats (Placeholder)");
    }

    void RefreshSkills()
    {
        // TODO: Clear existing skill entries in skillListContainer
        // TODO: Get all skill progress from SkillManager/DataManager
        // TODO: For each skill:
        //      - Instantiate skillEntryPrefab
        //      - Populate prefab's UI elements (Icon, Name, Level, XP bar/text)
        Debug.Log("CharacterPanel: RefreshSkills (Placeholder)");
    }

    void RefreshStatAllocation()
    {
        // TODO: Get available points from StatAllocator
        // TODO: Update availableStatPointsText
        // TODO: Set interactable state of allocation buttons based on available points > 0
        // TODO: Add listeners to buttons to call StatAllocator.AllocatePoint("StatName")
        Debug.Log("CharacterPanel: RefreshStatAllocation (Placeholder)");
    }
}