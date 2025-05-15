// Filepath: Assets/Scripts/Data/Models/PlayerData.cs
using SQLite;
using System;

[Serializable]
[Table("PlayerData")]
public class PlayerData
{
    [PrimaryKey]
    public int Id { get; set; }

    // Conversion des champs en propriétés pour pouvoir utiliser l'attribut Column
    private long _totalPlayerSteps;
    [Column("TotalPlayerSteps")]
    public long TotalPlayerSteps
    {
        get { return _totalPlayerSteps; }
        set { _totalPlayerSteps = value; }
    }

    private long _lastSyncEpochMs;
    [Column("LastSyncEpochMs")]
    public long LastSyncEpochMs
    {
        get { return _lastSyncEpochMs; }
        set { _lastSyncEpochMs = value; }
    }

    // Timestamp de la dernière mise en pause/fermeture de l'application
    private long _lastPauseEpochMs;
    [Column("LastPauseEpochMs")]
    public long LastPauseEpochMs
    {
        get { return _lastPauseEpochMs; }
        set { _lastPauseEpochMs = value; }
    }

    // Ajout: journalisation des changements pour détecter les anomalies
    private long _lastStepsDelta;
    [Column("LastStepsDelta")]
    public long LastStepsDelta
    {
        get { return _lastStepsDelta; }
        set { _lastStepsDelta = value; }
    }

    // Ajout: horodatage du dernier changement de pas pour suivi des anomalies
    private long _lastStepsChangeEpochMs;
    [Column("LastStepsChangeEpochMs")]
    public long LastStepsChangeEpochMs
    {
        get { return _lastStepsChangeEpochMs; }
        set { _lastStepsChangeEpochMs = value; }
    }

    // Constructeur par défaut
    public PlayerData()
    {
        Id = 1; // Fixons l'Id à 1 pour notre joueur unique
        _totalPlayerSteps = 0;
        _lastSyncEpochMs = 0; // 0 indique qu'aucune synchro n'a encore eu lieu
        _lastPauseEpochMs = 0; // 0 indique que l'app n'a jamais été mise en pause auparavant
        _lastStepsDelta = 0;
        _lastStepsChangeEpochMs = 0;
    }

    // Propriété pour accéder à TotalPlayerSteps avec le nom simplifié TotalSteps
    public long TotalSteps
    {
        get { return TotalPlayerSteps; }
        set
        {
            // Calculer et stocker le delta pour détecter les anomalies
            long delta = value - TotalPlayerSteps;
            if (delta != 0)
            {
                LastStepsDelta = delta;
                LastStepsChangeEpochMs = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeMilliseconds();
            }
            TotalPlayerSteps = value;
        }
    }
}