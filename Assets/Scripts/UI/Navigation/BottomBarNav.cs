// Purpose: Script for handling button clicks on the main bottom navigation bar.
// Filepath: Assets/Scripts/UI/Navigation/BottomNavBar.cs
using UnityEngine;
// using UnityEngine.UI; // Potential dependency if using Button components

public class BottomNavBar : MonoBehaviour
{
    // TODO: Reference UIManager to call panel switching methods
    // private UIManager uiManager;

    // TODO: References to the Button components on the nav bar (optional, can use public methods)
    // public Button mapButton;
    // public Button characterButton;
    // ... etc.

    void Start()
    {
        // TODO: Get reference to UIManager instance
        // uiManager = FindObjectOfType<UIManager>(); // Use Singleton or Service Locator ideally

        // TODO: Add listeners to buttons if using Button references
        // mapButton.onClick.AddListener(OnMapButtonClicked);
        // characterButton.onClick.AddListener(OnCharacterButtonClicked);
    }

    // Public methods called by Button OnClick() events assigned in the Inspector
    public void OnMapButtonClicked()
    {
        // TODO: Call uiManager.ShowMapPanel();
        Debug.Log("BottomNavBar: Map Button Clicked");
    }

    public void OnCharacterButtonClicked()
    {
        // TODO: Call uiManager.ShowCharacterPanel(); // Or combined Inventory/Character panel?
        Debug.Log("BottomNavBar: Character Button Clicked");
    }

    public void OnInventoryButtonClicked()
    {
        // TODO: Call uiManager.ShowInventoryPanel();
        Debug.Log("BottomNavBar: Inventory Button Clicked");
    }

    public void OnCombatButtonClicked()
    {
        // TODO: Call uiManager.ShowCombatPanel(); // Might show active combat or a combat preparation screen
        Debug.Log("BottomNavBar: Combat Button Clicked");
    }

    public void OnCraftingButtonClicked()
    {
        // TODO: Call uiManager.ShowCraftingPanel();
        Debug.Log("BottomNavBar: Crafting Button Clicked");
    }

    public void OnQuestsButtonClicked()
    {
        // TODO: Call uiManager.ShowQuestLogPanel();
        Debug.Log("BottomNavBar: Quests Button Clicked");
    }

    // TODO: Add methods for other nav bar buttons (Affinity, Settings?)

    // TODO: Implement visual feedback for the active button/panel
}