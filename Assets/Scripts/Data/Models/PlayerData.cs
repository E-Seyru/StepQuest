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

    // === SYSTeME DE PAS ===

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

    // Timestamp de la derniere mise en pause/fermeture de l'application
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

    // === SYSTeME DE LOCALISATION ET VOYAGE ===

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

    // a combien de pas le voyage a commence
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

    // NOUVEAU : Flag pour voyage multi-segment (persisté pour restauration après crash)
    private bool _isMultiSegmentTravel;
    [Column("IsMultiSegmentTravel")]
    public bool IsMultiSegmentTravel
    {
        get { return _isMultiSegmentTravel; }
        set { _isMultiSegmentTravel = value; }
    }

    // === SYSTeME D'ACTIVITe ===

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

    // === NOUVEAU: SYSTeME D'EXPeRIENCE ===

    // XP et niveaux des activites principales (Mining, Woodcutting, etc.)
    private string _skillsJson;
    [Column("SkillsJson")]
    public string SkillsJson
    {
        get { return _skillsJson; }
        set { _skillsJson = value; }
    }

    // XP et niveaux des sous-activites (Iron Mining, Oak Cutting, etc.)  
    private string _subSkillsJson;
    [Column("SubSkillsJson")]
    public string SubSkillsJson
    {
        get { return _subSkillsJson; }
        set { _subSkillsJson = value; }
    }

    // Proprietes pour acceder facilement aux competences (ne sont pas sauvegardees)
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

    // === SYSTEME D'ABILITIES ===

    // Abilities possedees (JSON serialise - liste d'IDs)
    private string _ownedAbilitiesJson;
    [Column("OwnedAbilitiesJson")]
    public string OwnedAbilitiesJson
    {
        get { return _ownedAbilitiesJson; }
        set { _ownedAbilitiesJson = value; }
    }

    // Abilities equipees (JSON serialise - liste d'IDs)
    private string _equippedAbilitiesJson;
    [Column("EquippedAbilitiesJson")]
    public string EquippedAbilitiesJson
    {
        get { return _equippedAbilitiesJson; }
        set { _equippedAbilitiesJson = value; }
    }

    // Propriete pour acceder facilement aux abilities possedees
    [Ignore]
    public List<string> OwnedAbilities
    {
        get
        {
            if (string.IsNullOrEmpty(_ownedAbilitiesJson))
                return new List<string>();

            try
            {
                return JsonConvert.DeserializeObject<List<string>>(_ownedAbilitiesJson);
            }
            catch (Exception ex)
            {
                Logger.LogError($"PlayerData: Error deserializing OwnedAbilities: {ex.Message}", Logger.LogCategory.General);
                return new List<string>();
            }
        }
        set
        {
            try
            {
                _ownedAbilitiesJson = value != null ? JsonConvert.SerializeObject(value) : null;
            }
            catch (Exception ex)
            {
                Logger.LogError($"PlayerData: Error serializing OwnedAbilities: {ex.Message}", Logger.LogCategory.General);
                _ownedAbilitiesJson = null;
            }
        }
    }

    // Propriete pour acceder facilement aux abilities equipees
    [Ignore]
    public List<string> EquippedAbilities
    {
        get
        {
            if (string.IsNullOrEmpty(_equippedAbilitiesJson))
                return new List<string>();

            try
            {
                return JsonConvert.DeserializeObject<List<string>>(_equippedAbilitiesJson);
            }
            catch (Exception ex)
            {
                Logger.LogError($"PlayerData: Error deserializing EquippedAbilities: {ex.Message}", Logger.LogCategory.General);
                return new List<string>();
            }
        }
        set
        {
            try
            {
                _equippedAbilitiesJson = value != null ? JsonConvert.SerializeObject(value) : null;
            }
            catch (Exception ex)
            {
                Logger.LogError($"PlayerData: Error serializing EquippedAbilities: {ex.Message}", Logger.LogCategory.General);
                _equippedAbilitiesJson = null;
            }
        }
    }

    // === SYSTEME NPC ===

    // Discovered NPCs (JSON serialise - List<npcId>)
    private string _discoveredNPCsJson;

    // === SYSTEME DE DIALOGUE ===

    // Dialogue flags (JSON serialise - Dictionary<flagName, bool>)
    private string _dialogueFlagsJson;
    [Column("DialogueFlagsJson")]
    public string DialogueFlagsJson
    {
        get { return _dialogueFlagsJson; }
        set { _dialogueFlagsJson = value; }
    }

    // NPC Relationships (JSON serialise - Dictionary<npcId, int> - echelle 0 a 10)
    private string _npcRelationshipsJson;
    [Column("NPCRelationshipsJson")]
    public string NPCRelationshipsJson
    {
        get { return _npcRelationshipsJson; }
        set { _npcRelationshipsJson = value; }
    }

    // Propriete pour acceder aux flags de dialogue
    [Ignore]
    public Dictionary<string, bool> DialogueFlags
    {
        get
        {
            if (string.IsNullOrEmpty(_dialogueFlagsJson))
                return new Dictionary<string, bool>();

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, bool>>(_dialogueFlagsJson);
            }
            catch (Exception ex)
            {
                Logger.LogError($"PlayerData: Error deserializing DialogueFlags: {ex.Message}", Logger.LogCategory.General);
                return new Dictionary<string, bool>();
            }
        }
        set
        {
            try
            {
                _dialogueFlagsJson = value != null ? JsonConvert.SerializeObject(value) : null;
            }
            catch (Exception ex)
            {
                Logger.LogError($"PlayerData: Error serializing DialogueFlags: {ex.Message}", Logger.LogCategory.General);
                _dialogueFlagsJson = null;
            }
        }
    }

    // Propriete pour acceder aux relations NPC
    [Ignore]
    public Dictionary<string, int> NPCRelationships
    {
        get
        {
            if (string.IsNullOrEmpty(_npcRelationshipsJson))
                return new Dictionary<string, int>();

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, int>>(_npcRelationshipsJson);
            }
            catch (Exception ex)
            {
                Logger.LogError($"PlayerData: Error deserializing NPCRelationships: {ex.Message}", Logger.LogCategory.General);
                return new Dictionary<string, int>();
            }
        }
        set
        {
            try
            {
                _npcRelationshipsJson = value != null ? JsonConvert.SerializeObject(value) : null;
            }
            catch (Exception ex)
            {
                Logger.LogError($"PlayerData: Error serializing NPCRelationships: {ex.Message}", Logger.LogCategory.General);
                _npcRelationshipsJson = null;
            }
        }
    }
    [Column("DiscoveredNPCsJson")]
    public string DiscoveredNPCsJson
    {
        get { return _discoveredNPCsJson; }
        set { _discoveredNPCsJson = value; }
    }

    // Propriete pour acceder aux NPCs decouverts
    [Ignore]
    public List<string> DiscoveredNPCs
    {
        get
        {
            if (string.IsNullOrEmpty(_discoveredNPCsJson))
                return new List<string>();

            try
            {
                return JsonConvert.DeserializeObject<List<string>>(_discoveredNPCsJson);
            }
            catch (Exception ex)
            {
                Logger.LogError($"PlayerData: Error deserializing DiscoveredNPCs: {ex.Message}", Logger.LogCategory.General);
                return new List<string>();
            }
        }
        set
        {
            try
            {
                _discoveredNPCsJson = value != null ? JsonConvert.SerializeObject(value) : null;
            }
            catch (Exception ex)
            {
                Logger.LogError($"PlayerData: Error serializing DiscoveredNPCs: {ex.Message}", Logger.LogCategory.General);
                _discoveredNPCsJson = null;
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
        _isMultiSegmentTravel = false;

        // Activite
        _currentActivityJson = null;

        // XP System
        _skillsJson = null;
        _subSkillsJson = null;

        // Ability System
        _ownedAbilitiesJson = null;
        _equippedAbilitiesJson = null;

        // NPC System
        _discoveredNPCsJson = null;

        // Dialogue System
        _dialogueFlagsJson = null;
        _npcRelationshipsJson = null;
    }

    // === MeTHODES DE VOYAGE ===

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
        _isMultiSegmentTravel = false;
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

    // === MeTHODES D'ACTIVITe ===

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

    // === MeTHODES D'EXPeRIENCE ===

    /// <summary>
    /// Obtenir le niveau d'une competence principale (ex: "Mining")
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
    /// Obtenir l'XP d'une competence principale (ex: "Mining")
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
    /// Obtenir le niveau d'une sous-competence (ex: "Iron_Mining")
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
    /// Obtenir l'XP d'une sous-competence (ex: "Iron_Mining")
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

    // === MeTHODES NPC ===

    /// <summary>
    /// Check if an NPC has been discovered
    /// </summary>
    public bool HasDiscoveredNPC(string npcId)
    {
        var discovered = DiscoveredNPCs;
        return discovered.Contains(npcId);
    }

    // === METHODES DE DIALOGUE ===

    /// <summary>
    /// Get the value of a dialogue flag
    /// </summary>
    public bool GetDialogueFlag(string flagName)
    {
        if (string.IsNullOrEmpty(flagName)) return false;
        var flags = DialogueFlags;
        return flags.ContainsKey(flagName) && flags[flagName];
    }

    /// <summary>
    /// Set a dialogue flag
    /// </summary>
    public void SetDialogueFlag(string flagName, bool value)
    {
        if (string.IsNullOrEmpty(flagName)) return;
        var flags = DialogueFlags;
        flags[flagName] = value;
        DialogueFlags = flags;
    }

    /// <summary>
    /// Get the relationship level with an NPC (0-10)
    /// </summary>
    public int GetNPCRelationship(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return 0;
        var relationships = NPCRelationships;
        return relationships.ContainsKey(npcId) ? relationships[npcId] : 0;
    }

    /// <summary>
    /// Set the relationship level with an NPC (clamped 0-10)
    /// </summary>
    public void SetNPCRelationship(string npcId, int value)
    {
        if (string.IsNullOrEmpty(npcId)) return;
        var relationships = NPCRelationships;
        relationships[npcId] = UnityEngine.Mathf.Clamp(value, 0, 10);
        NPCRelationships = relationships;
    }

    /// <summary>
    /// Modify the relationship level with an NPC by a delta (can be negative)
    /// </summary>
    public void ModifyNPCRelationship(string npcId, int delta)
    {
        if (string.IsNullOrEmpty(npcId)) return;
        int current = GetNPCRelationship(npcId);
        SetNPCRelationship(npcId, current + delta);
    }

    // === PROPRIeTeS CALCULeES ET ALIASES ===

    /// <summary>
    /// Alias pour TotalPlayerSteps (compatibilite) - Lecture ET ecriture
    /// </summary>
    [Ignore]
    public long TotalSteps
    {
        get => _totalPlayerSteps;
        set => _totalPlayerSteps = value;
    }
}