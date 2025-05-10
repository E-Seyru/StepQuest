// Purpose: Data structure representing the player's core state.
// Filepath: Assets/Scripts/Data/Models/PlayerData.cs
using SQLite;

[System.Serializable] // Make it serializable for saving/loading
public class PlayerData
{
    // TODO: Define player stats (allocated base stats, current HP)
    // public int CurrentHP;
    // public Dictionary<string, int> BaseStats; // e.g., {"Strength": 10, "Stamina": 5}

    // TODO: Store current location ID or name

    [PrimaryKey, AutoIncrement]
    public int Id { get; set; } // Unique identifier for the player data entry
    public long TotalPlayerSteps; // Total steps accumulated by the game    
    public long LastKnownDailyStepsForDeltaCalc; // Steps from the API for today


    public string CurrentLocationId;


    // TODO: Store currency amount
    // public int Currency;

    // TODO: Store legacy points
    // public int LegacyPointsEarned;
    // public int LegacyPointsSpent;

    // TODO: Store story progression flags/variables
    // public HashSet<string> CompletedQuestIds;
    // public Dictionary<string, int> StoryVariables; // e.g., {"Chapter": 1}

    // TODO: Store info about the currently active task (if any)
    // public ActiveTaskData CurrentTask;

    // TODO: Store saved steps balance
    // public int SavedSteps;

    // Constructor (optional, for default values)
    public PlayerData()
    {
        // BaseStats = new Dictionary<string, int>();
        // CompletedQuestIds = new HashSet<string>();
        // StoryVariables = new Dictionary<string, int>();

        TotalPlayerSteps = 0;
        LastKnownDailyStepsForDeltaCalc = 0;
    }
}

// Potentially define ActiveTaskData struct/class here or in a separate file
[System.Serializable]
public class ActiveTaskData
{
    public TaskType Type;
    public string TargetId; // e.g., LocationId, RecipeId, MonsterZoneId
    public System.DateTime StartTime;
    public int StepsAtStart; // Only relevant for step tasks
                             // Add other relevant task progress data
}
public enum TaskType { None, Traveling, Gathering, Crafting, LoopedCombat }