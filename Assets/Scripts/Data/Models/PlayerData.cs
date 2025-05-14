// Filepath: Assets/Scripts/Data/Models/PlayerData.cs
using SQLite; // Si vous utilisez SQLite-net pour la persistance
using System;

[Serializable]
public class PlayerData
{
    [PrimaryKey] // Si vous utilisez SQLite-net
    public int Id { get; set; }

    public long TotalPlayerSteps; // Anciennement TotalPlayerStepsInGame, on garde ce nom pour la compatibilité

    // Anciennement LastKnownDailyStepsForDeltaCalc ou LastApiTodaysStepsValue
    // On le renomme pour plus de clarté par rapport au nouveau plan,
    // même si le plan simplifié ne le persiste plus de cette manière.
    // Pour l'instant, on le garde au cas où, mais il sera moins central.
    // On pourrait le supprimer si StepManager gère tout.
    // Pour l'instant, le plan dit "On se débarrasse de baseTodaySteps persistant"
    // donc on peut commenter ou supprimer LastKnownApiTodaysStepsValue.
    // public long LastKnownApiTodaysStepsValue;

    // NOUVEAU CHAMP : Instant UTC de la dernière synchronisation API réussie avec GetDeltaSince
    public long LastSyncEpochMs;


    // Constructeur par défaut
    public PlayerData()
    {
        Id = 1; // Fixons l'Id à 1 pour notre joueur unique si pas d'AutoIncrement
        TotalPlayerSteps = 0;
        //LastKnownApiTodaysStepsValue = 0;
        LastSyncEpochMs = 0; // 0 indique quaucune synchro na encore eu lieu
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