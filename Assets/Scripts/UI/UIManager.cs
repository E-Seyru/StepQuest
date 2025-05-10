// Purpose: Main UI coordinator, handles switching between major panels/screens.
// Filepath: Assets/Scripts/UI/UIManager.cs
using UnityEngine;
// using System.Collections.Generic; // Potential dependency for managing panels

public class UIManager : MonoBehaviour
{
    // TODO: References to all major UI Panel GameObjects (assign in Inspector)
    // public GameObject mapPanel;
    // public GameObject characterPanel;
    // public GameObject inventoryPanel;
    // public GameObject combatPanel;
    // public GameObject craftingPanel;
    // public GameObject questLogPanel;
    // public GameObject affinityPanel;
    // public GameObject dialoguePanel;
    // ... add other panels (Settings, Shop, etc.)

    // TODO: Store the currently active panel
    // private GameObject currentActivePanel;

    void Start()
    {
        // TODO: Ensure all panels are initially hidden except the default one (e.g., MapPanel)
        // ShowPanel(mapPanel); // Example starting panel
    }

    public void ShowPanel(GameObject panelToShow)
    {
        // TODO: Check if panelToShow is valid
        // TODO: Hide the currentActivePanel if it exists
        // if (currentActivePanel != null) { currentActivePanel.SetActive(false); }
        // TODO: Show the panelToShow
        // panelToShow.SetActive(true);
        // TODO: Update currentActivePanel reference
        // currentActivePanel = panelToShow;
        Debug.Log($"UIManager: Showing panel {panelToShow?.name} (Placeholder)");
    }

    // TODO: Add specific methods for showing each panel (called by Nav bar or other UI elements)
    // public void ShowMapPanel() { ShowPanel(mapPanel); }
    // public void ShowCharacterPanel() { ShowPanel(characterPanel); }
    // ... etc.

    // TODO: Add methods for showing/hiding popups or modal dialogs
    // public void ShowNotification(string message) { ... }
    // public void ShowConfirmationDialog(string message, System.Action onConfirm) { ... }
}