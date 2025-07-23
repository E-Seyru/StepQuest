// Filepath: Assets/Scripts/Data/Models/PlayerData.cs
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;

[Serializable]
[Table("PlayerData")]
public class PlayerData
{
    [PrimaryKey]
    public int Id { get; set; }

    // === SYSTÈME DE PAS ===

    // Conversion des champs en proprietes pour pouvoir utiliser l'attribut Column
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

    // Ajout: journalisation des changements pour detecter les anomalies
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

    // === SYSTÈME DE LOCALISATION ET VOYAGE ===

    // Où est le joueur actuellement (ID de location comme "Village_01")
    private string _currentLocationId;
    [Column("CurrentLocationId")]
    public string CurrentLocationId
    {
        get { return _currentLocationId; }
        set { _currentLocationId = value; }
    }

    // Est-ce que le joueur voyage actuellement ?
    // (null = non, sinon = destination du segment actuel)
    private string _travelDestinationId;
    [Column("TravelDestinationId")]
    public string TravelDestinationId
    {
        get { return _travelDestinationId; }
        set { _travelDestinationId = value; }
    }

    // À combien de pas le voyage a commence
    private long _travelStartSteps;
    [Column("TravelStartSteps")]
    public long TravelStartSteps
    {
        get { return _travelStartSteps; }
        set { _travelStartSteps = value; }
    }

    // Combien de pas faut-il pour finir le voyage (ou le segment actuel)
    private int _travelRequiredSteps;
    [Column("TravelRequiredSteps")]
    public int TravelRequiredSteps
    {
        get { return _travelRequiredSteps; }
        set { _travelRequiredSteps = value; }
    }

    // NOUVEAU : Destination finale pour les voyages multi-segments (Version 7)
    private string _travelFinalDestinationId;
    [Column("TravelFinalDestinationId")]
    public string TravelFinalDestinationId
    {
        get { return _travelFinalDestinationId; }
        set { _travelFinalDestinationId = value; }
    }

    // NOUVEAU : Location de depart originale du voyage (Version 7)
    private string _travelOriginLocationId;
    [Column("TravelOriginLocationId")]
    public string TravelOriginLocationId
    {
        get { return _travelOriginLocationId; }
        set { _travelOriginLocationId = value; }
    }

    // === SYSTÈME D'ACTIVITÉ ===

    // Activite en cours (JSON serialise)
    private string _currentActivityJson;
    [Column("CurrentActivityJson")]
    public string CurrentActivityJson
    {
        get { return _currentActivityJson; }
        set { _currentActivityJson = value; }
    }

    // Propriete pour acceder facilement a l'activite courante
    [Ignore] // Ne pas sauvegarder en base, c'est juste un wrapper
    public ActivityData CurrentActivity
    {
        get
        {
            if (string.IsNullOrEmpty(_currentActivityJson))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<ActivityData>(_currentActivityJson);
            }
            catch (Exception ex)
            {
                Logger.LogError($"PlayerData: Error deserializing CurrentActivity: {ex.Message}", Logger.LogCategory.General);
                return null;
            }
        }
        set
        {
            if (value == null)
            {
                _currentActivityJson = null;
            }
            else
            {
                try
                {
                    _currentActivityJson = JsonConvert.SerializeObject(value);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"PlayerData: Error serializing CurrentActivity: {ex.Message}", Logger.LogCategory.General);
                    _currentActivityJson = null;
                }
            }
        }
    }

    // === NOUVEAU: SYSTÈME D'EXPÉRIENCE ===

    // XP et niveaux des activités principales (Mining, Woodcutting, etc.)
    private string _skillsJson;
    [Column("SkillsJson")]
    public string SkillsJson
    {
        get { return _skillsJson; }
        set { _skillsJson = value; }
    }

    // XP et niveaux des sous-activités (Iron Mining, Oak Cutting, etc.)  
    private string _subSkillsJson;
    [Column("SubSkillsJson")]
    public string SubSkillsJson
    {
        get { return _subSkillsJson; }
        set { _subSkillsJson = value; }
    }

    // Propriétés pour accéder facilement aux compétences (ne sont pas sauvegardées)
    [Ignore]
    public Dictionary<string, SkillData> Skills
    {
        get
        {
            if (string.IsNullOrEmpty(_skillsJson))
                return new Dictionary<string, SkillData>();

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, SkillData>>(_skillsJson);
            }
            catch (Exception ex)
            {
                Logger.LogError($"PlayerData: Error deserializing Skills: {ex.Message}", Logger.LogCategory.General);
                return new Dictionary<string, SkillData>();
            }
        }
        set
        {
            try
            {
                _skillsJson = JsonConvert.SerializeObject(value);
            }
            catch (Exception ex)
            {
                Logger.LogError($"PlayerData: Error serializing Skills: {ex.Message}", Logger.LogCategory.General);
                _skillsJson = null;
            }
        }
    }

    [Ignore]
    public Dictionary<string, SkillData> SubSkills
    {
        get
        {
            if (string.IsNullOrEmpty(_subSkillsJson))
                return new Dictionary<string, SkillData>();

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, SkillData>>(_subSkillsJson);
            }
            catch (Exception ex)
            {
                Logger.LogError($"PlayerData: Error deserializing SubSkills: {ex.Message}", Logger.LogCategory.General);
                return new Dictionary<string, SkillData>();
            }
        }
        set
        {
            try
            {
                _subSkillsJson = JsonConvert.SerializeObject(value);
            }
            catch (Exception ex)
            {
                Logger.LogError($"PlayerData: Error serializing SubSkills: {ex.Message}", Logger.LogCategory.General);
                _subSkillsJson = null;
            }
        }
    }

    // === CONSTRUCTEUR ===

    // Constructeur par defaut
    public PlayerData()
    {
        Id = 1; // Fixons l'Id a 1 pour notre joueur unique
        _totalPlayerSteps = 0;
        _lastSyncEpochMs = 0; // 0 indique qu'aucune synchro n'a encore eu lieu
        _lastPauseEpochMs = 0;
        _lastStepsDelta = 0;
        _lastStepsChangeEpochMs = 0;
        _dailySteps = 0;
        _lastDailyResetDate = "";
        _lastApiCatchUpEpochMs = 0;

        // Location et voyage
        _currentLocationId = "";
        _travelDestinationId = null;
        _travelStartSteps = 0;
        _travelRequiredSteps = 0;
        _travelFinalDestinationId = null;
        _travelOriginLocationId = null;

        // Activité
        _currentActivityJson = null;

        // XP System
        _skillsJson = null;
        _subSkillsJson = null;
    }

    // === MÉTHODES DE VOYAGE ===

    /// <summary>
    /// Le joueur est-il actuellement en voyage ?
    /// </summary>
    public bool IsCurrentlyTraveling()
    {
        return !string.IsNullOrEmpty(_travelDestinationId);
    }

    /// <summary>
    /// Calculer le progres du voyage actuel
    /// </summary>
    public long GetTravelProgress(long currentPlayerSteps)
    {
        if (!IsCurrentlyTraveling()) return 0;
        return Math.Max(0, currentPlayerSteps - _travelStartSteps);
    }

    /// <summary>
    /// Le voyage est-il complete ?
    /// </summary>
    public bool IsTravelComplete(long currentPlayerSteps)
    {
        if (!IsCurrentlyTraveling()) return false;
        return GetTravelProgress(currentPlayerSteps) >= _travelRequiredSteps;
    }

    /// <summary>
    /// Commencer un voyage
    /// </summary>
    public void StartTravel(string destinationId, int requiredSteps, long currentPlayerSteps)
    {
        _travelDestinationId = destinationId;
        _travelStartSteps = currentPlayerSteps;
        _travelRequiredSteps = requiredSteps;
    }

    /// <summary>
    /// Terminer le voyage actuel
    /// </summary>
    public void CompleteTravel()
    {
        _currentLocationId = _travelDestinationId;
        _travelDestinationId = null;
        _travelStartSteps = 0;
        _travelRequiredSteps = 0;

        // Clear multi-segment data too
        _travelFinalDestinationId = null;
        _travelOriginLocationId = null;
    }

    /// <summary>
    /// Annuler le voyage actuel
    /// </summary>
    public void CancelTravel()
    {
        _travelDestinationId = null;
        _travelStartSteps = 0;
        _travelRequiredSteps = 0;
        _travelFinalDestinationId = null;
        _travelOriginLocationId = null;
    }

    // === MÉTHODES D'ACTIVITÉ ===

    /// <summary>
    /// Le joueur a-t-il une activite en cours ?
    /// </summary>
    public bool HasActiveActivity()
    {
        return !string.IsNullOrEmpty(_currentActivityJson);
    }

    /// <summary>
    /// Arreter l'activite courante
    /// </summary>
    public void StopActivity()
    {
        _currentActivityJson = null;
    }

    // === MÉTHODES D'EXPÉRIENCE ===

    /// <summary>
    /// Obtenir le niveau d'une compétence principale (ex: "Mining")
    /// </summary>
    public int GetSkillLevel(string skillId)
    {
        var skills = Skills;
        if (skills.ContainsKey(skillId))
        {
            return skills[skillId].Level;
        }
        return 1; // Niveau de base
    }

    /// <summary>
    /// Obtenir l'XP d'une compétence principale (ex: "Mining")
    /// </summary>
    public int GetSkillXP(string skillId)
    {
        var skills = Skills;
        if (skills.ContainsKey(skillId))
        {
            return skills[skillId].Experience;
        }
        return 0;
    }

    /// <summary>
    /// Obtenir le niveau d'une sous-compétence (ex: "Iron_Mining")
    /// </summary>
    public int GetSubSkillLevel(string variantId)
    {
        var subSkills = SubSkills;
        if (subSkills.ContainsKey(variantId))
        {
            return subSkills[variantId].Level;
        }
        return 1; // Niveau de base
    }

    /// <summary>
    /// Obtenir l'XP d'une sous-compétence (ex: "Iron_Mining")
    /// </summary>
    public int GetSubSkillXP(string variantId)
    {
        var subSkills = SubSkills;
        if (subSkills.ContainsKey(variantId))
        {
            return subSkills[variantId].Experience;
        }
        return 0;
    }

    // === PROPRIÉTÉS CALCULÉES ET ALIASES ===

    /// <summary>
    /// Alias pour TotalPlayerSteps (compatibilite) - Lecture ET écriture
    /// </summary>
    [Ignore]
    public long TotalSteps
    {
        get => _totalPlayerSteps;
        set => _totalPlayerSteps = value;
    }
}