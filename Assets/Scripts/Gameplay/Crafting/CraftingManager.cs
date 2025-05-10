// Purpose: Manages crafting queues, timers, and item creation.
// Filepath: Assets/Scripts/Gameplay/Crafting/CraftingManager.cs
using UnityEngine;
using System.Collections.Generic; // For List/Queue
using System; // For Action/DateTime

public class CraftingManager : MonoBehaviour
{
    // TODO: Reference RecipeRegistry to get recipe details (ingredients, time, skill reqs)
    // private RecipeRegistry recipeRegistry;
    // TODO: Reference InventoryManager to consume ingredients and add crafted items
    // private InventoryManager inventoryManager;
    // TODO: Reference SkillManager to check skill levels and grant crafting XP
    // private SkillManager skillManager;
    // TODO: Reference TimeService to handle offline progress
    // private TimeService timeService;
    // TODO: Reference DataManager to save/load active crafting queue
    // private DataManager dataManager;

    // TODO: Store the active crafting queue(s) - maybe one per station or just one global?
    // private Queue<CraftingTask> craftingQueue;
    // private CraftingTask currentCraftingTask;

    // TODO: Define events for queue updates, task start/completion
    // public event Action OnQueueUpdated;
    // public event Action<CraftingTask> OnCraftingComplete;

    void Start()
    {
        // TODO: Get references to dependencies
        // TODO: Load any saved crafting queue state from DataManager
        // TODO: Process offline crafting progress using TimeService.GetOfflineTimeSpan()
    }

    void Update()
    {
        // TODO: If a task is currently crafting, check if its timer has expired
        // if (currentCraftingTask != null && Time.time >= currentCraftingTask.CompletionTime)
        // {
        //     CompleteCraftingTask(currentCraftingTask);
        //     StartNextTask();
        // }
    }

    public bool CanCraft(string recipeId)
    {
        // TODO: Get recipe definition from RecipeRegistry
        // TODO: Check player skill level against recipe requirements (via SkillManager)
        // TODO: Check if player has required ingredients in inventory (via InventoryManager)
        return true; // Placeholder
    }

    public bool AddToQueue(string recipeId, int quantity = 1)
    {
        // TODO: Check if CanCraft(recipeId) is true
        // TODO: Get recipe definition for duration etc.
        // TODO: Consume ingredients from inventory (InventoryManager.RemoveItem) - Do this when STARTING or adding to queue? (Usually when starting)
        // TODO: Create CraftingTask object(s) and add to craftingQueue
        // TODO: If no task is currently running, call StartNextTask()
        // TODO: Trigger OnQueueUpdated event
        // TODO: Save queue state?
        Debug.Log($"CraftingManager: Adding {recipeId} x{quantity} to queue (Placeholder)");
        return true; // Placeholder
    }

    private void StartNextTask()
    {
        // TODO: If queue is not empty and no task is current:
        // TODO: Dequeue the next CraftingTask
        // TODO: Consume ingredients NOW if not done when adding to queue
        // TODO: If ingredients consumed successfully:
        //      currentCraftingTask = task;
        //      Set task.StartTime = Time.time (or use TimeService.GetCurrentTime())
        //      Set task.CompletionTime = Time.time + recipe.DurationSeconds
        //      Trigger event OnTaskStarted?
        // else:
        //      Report error (ingredients missing?) and potentially try next task?
    }

    private void CompleteCraftingTask(CraftingTask task)
    {
        // TODO: Add crafted item(s) to inventory (InventoryManager.AddItem)
        // TODO: Grant skill XP (SkillManager.AddXP)
        // TODO: Trigger OnCraftingComplete event
        // TODO: Clear currentCraftingTask variable
        Debug.Log($"CraftingManager: Completed crafting {task.RecipeId} (Placeholder)");
        // currentCraftingTask = null;
    }

    public void ProcessOfflineCrafting(TimeSpan offlineTime)
    {
        // TODO: Iterate through the saved queue state
        // TODO: For each task, calculate how much time it would have progressed during offlineTime
        // TODO: Complete any tasks that would have finished
        // TODO: Update the progress/remaining time of the task that was running when the app closed
        // TODO: Update the overall queue state
        Debug.Log($"CraftingManager: Processing offline crafting for {offlineTime.TotalMinutes} mins (Placeholder)");
    }

    // TODO: Add methods to view queue, cancel tasks?
}

// Data structure for an active crafting task
[System.Serializable] // If saved directly by DataManager
public class CraftingTask
{
    public string RecipeId;
    public int Quantity; // How many to craft in this specific task entry
    public float TimePerItem; // Duration from recipe
    public float CompletionTime; // Calculated time when this specific item finishes
    public DateTime StartTimeUtc; // Use UTC for consistency across sessions
    // Add other state if needed (e.g., progress)
}