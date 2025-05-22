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

    // Compteur de pas journalier
    private long _dailySteps;
    [Column("DailySteps")]
    public long DailySteps
    {
        get { return _dailySteps; }
        set { _dailySteps = value; }
    }

    // Date du dernier reset journalier (format yyyy-MM-dd)
    private string _lastDailyResetDate;
    [Column("LastDailyResetDate")]
    public string LastDailyResetDate
    {
        get { return _lastDailyResetDate; }
        set { _lastDailyResetDate = value; }
    }

    // NOUVEAU: Timestamp du dernier catch-up API (Faille A)
    private long _lastApiCatchUpEpochMs;
    [Column("LastApiCatchUpEpochMs")]
    public long LastApiCatchUpEpochMs
    {
        get { return _lastApiCatchUpEpochMs; }
        set { _lastApiCatchUpEpochMs = value; }
    }

    // === NOUVEAU: Système de localisation et voyage ===

    // Où est le joueur actuellement (ID de location comme "Village_01")
    private string _currentLocationId;
    [Column("CurrentLocationId")]
    public string CurrentLocationId
    {
        get { return _currentLocationId; }
        set { _currentLocationId = value; }
    }

    // Est-ce que le joueur voyage actuellement ? (null = non, sinon = destination)
    private string _travelDestinationId;
    [Column("TravelDestinationId")]
    public string TravelDestinationId
    {
        get { return _travelDestinationId; }
        set { _travelDestinationId = value; }
    }

    // À combien de pas le voyage a commencé
    private long _travelStartSteps;
    [Column("TravelStartSteps")]
    public long TravelStartSteps
    {
        get { return _travelStartSteps; }
        set { _travelStartSteps = value; }
    }

    // Combien de pas faut-il pour finir le voyage
    private int _travelRequiredSteps;
    [Column("TravelRequiredSteps")]
    public int TravelRequiredSteps
    {
        get { return _travelRequiredSteps; }
        set { _travelRequiredSteps = value; }
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
        _dailySteps = 0;
        _lastDailyResetDate = DateTime.Now.ToString("yyyy-MM-dd"); // MODIFIÉ: Utiliser DateTime.Now au lieu de DateTime.UtcNow (Faille B)
        _lastApiCatchUpEpochMs = 0; // Nouvelle propriété

        // NOUVEAU: Valeurs par défaut pour le système de voyage
        _currentLocationId = "Foret_01"; // Le joueur commence au village
        _travelDestinationId = null; // Pas de voyage en cours
        _travelStartSteps = 0;
        _travelRequiredSteps = 0;
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
                LastStepsChangeEpochMs = new DateTimeOffset(DateTime.Now).ToUnixTimeMilliseconds(); // MODIFIÉ: Utiliser DateTime.Now (Faille B)
            }
            TotalPlayerSteps = value;
        }
    }

    // NOUVEAU: Méthodes utiles pour le voyage

    // Est-ce que le joueur voyage actuellement ?
    public bool IsCurrentlyTraveling()
    {
        return !string.IsNullOrEmpty(TravelDestinationId);
    }

    // Combien de pas a fait le joueur depuis le début du voyage ?
    public long GetTravelProgress(long currentTotalSteps)
    {
        if (!IsCurrentlyTraveling()) return 0;
        return currentTotalSteps - TravelStartSteps;
    }

    // Le voyage est-il terminé ?
    public bool IsTravelComplete(long currentTotalSteps)
    {
        if (!IsCurrentlyTraveling()) return false;
        return GetTravelProgress(currentTotalSteps) >= TravelRequiredSteps;
    }
}