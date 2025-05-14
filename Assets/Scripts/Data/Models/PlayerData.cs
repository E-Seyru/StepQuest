// Filepath: Assets/Scripts/Data/Models/PlayerData.cs
using SQLite; // Si vous utilisez SQLite-net pour la persistance
using System;

[Serializable]
public class PlayerData
{
    [PrimaryKey] // Si vous utilisez SQLite-net
    public int Id { get; set; }

    public long TotalPlayerSteps; // Anciennement TotalPlayerStepsInGame, on garde ce nom pour la compatibilit�

    // Anciennement LastKnownDailyStepsForDeltaCalc ou LastApiTodaysStepsValue
    // On le renomme pour plus de clart� par rapport au nouveau plan,
    // m�me si le plan simplifi� ne le persiste plus de cette mani�re.
    // Pour l'instant, on le garde au cas o�, mais il sera moins central.
    // On pourrait le supprimer si StepManager g�re tout.
    // Pour l'instant, le plan dit "On se d�barrasse de baseTodaySteps persistant"
    // donc on peut commenter ou supprimer LastKnownApiTodaysStepsValue.
    // public long LastKnownApiTodaysStepsValue;

    // NOUVEAU CHAMP : Instant UTC de la derni�re synchronisation API r�ussie avec GetDeltaSince
    public long LastSyncEpochMs;


    // Constructeur par d�faut
    public PlayerData()
    {
        Id = 1; // Fixons l'Id � 1 pour notre joueur unique si pas d'AutoIncrement
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