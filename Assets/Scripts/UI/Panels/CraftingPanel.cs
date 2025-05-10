// Purpose: Script for the UI panel handling crafting selection and queue display.
// Filepath: Assets/Scripts/UI/Panels/CraftingPanel.cs
using UnityEngine;
// using UnityEngine.UI; // Potential dependency for lists, buttons, etc.
// using System.Collections.Generic; // Potential dependency

public class CraftingPanel : MonoBehaviour
{
    // TODO: References to UI elements (Recipe list container, Recipe details area, Craft button, Queue container)
    // public Transform recipeListContainer;
    // public GameObject recipeListItemPrefab;
    // public Text selectedRecipeName;
    // public Text selectedRecipeDescription;
    // public Transform ingredientsContainer; // To show required ingredients
    // public GameObject ingredientItemPrefab;
    // public Text craftingTimeText;
    // public Text skillRequirementText;
    // public Button craftButton;
    // public InputField quantityInput;
    // public Transform craftingQueueContainer;
    // public GameObject queueItemPrefab;

    // TODO: Reference CraftingManager
    // private CraftingManager craftingManager;
    // TODO: Reference RecipeRegistry
    // private RecipeRegistry recipeRegistry;
    // TODO: Store the currently selected recipe ID
    // private string selectedRecipeId;

    void OnEnable()
    {
        // TODO: Get references
        // TODO: Subscribe to CraftingManager events (OnQueueUpdated)
        // TODO: Populate recipe list
        // RefreshRecipeList();
        // RefreshQueueDisplay();
        // ClearSelectedRecipeDetails();
    }

    void OnDisable()
    {
        // TODO: Unsubscribe from events
    }

    void RefreshRecipeList()
    {
        // TODO: Clear recipeListContainer
        // TODO: Get all recipes from RecipeRegistry (or filter by skill?)
        // TODO: For each recipe:
        //      - Instantiate recipeListItemPrefab
        //      - Setup prefab UI (name, icon?)
        //      - Add listener to prefab's button to call OnRecipeSelected(recipeId)
        //      - Set interactable based on whether player meets skill requirement? (Optional)
        Debug.Log("CraftingPanel: RefreshRecipeList (Placeholder)");
    }

    void OnRecipeSelected(string recipeId)
    {
        // TODO: Store selectedRecipeId
        // TODO: Get RecipeDefinition from RecipeRegistry
        // TODO: Update selectedRecipeName, selectedRecipeDescription, craftingTimeText, skillRequirementText
        // TODO: Clear and populate ingredientsContainer using ingredientItemPrefab
        // TODO: Check if player CanCraft(recipeId) via CraftingManager
        // TODO: Set interactable state of craftButton
        Debug.Log($"CraftingPanel: Recipe selected {recipeId} (Placeholder)");
    }

    public void OnCraftButtonClicked()
    {
        // TODO: Check if selectedRecipeId is valid
        // TODO: Get quantity from quantityInput (default to 1 if empty/invalid)
        // TODO: Call CraftingManager.AddToQueue(selectedRecipeId, quantity)
        // TODO: Refresh UI (queue will update via event, maybe clear selection?)
    }

    void RefreshQueueDisplay()
    {
        // TODO: Clear craftingQueueContainer
        // TODO: Get current queue from CraftingManager
        // TODO: For each task in queue:
        //      - Instantiate queueItemPrefab
        //      - Setup prefab UI (item name/icon, quantity, progress bar/time remaining)
        //      - Add cancel button listener? (Call CraftingManager.CancelTask)
        Debug.Log("CraftingPanel: RefreshQueueDisplay (Placeholder)");
    }

    void ClearSelectedRecipeDetails()
    {
        // TODO: Clear text fields for selected recipe
        // TODO: Disable craft button
        // selectedRecipeId = null;
    }
}