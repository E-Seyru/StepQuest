// Purpose: Main combat manager handling auto-battler combat with cooldown-based abilities
// Filepath: Assets/Scripts/Gameplay/Combat/CombatManager.cs

using CombatEvents;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages auto-battler combat with cooldown-based abilities.
/// Combat runs automatically - abilities trigger when their cooldown is ready.
/// Uses service-based architecture for maintainability.
/// </summary>
public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Player Stats (Temporary - will come from equipment later)")]
    [SerializeField] private float playerMaxHealth = 100f;
    [SerializeField] private List<AbilityDefinition> playerAbilities = new List<AbilityDefinition>();

    [Header("Registries")]
    [SerializeField] private StatusEffectRegistry statusEffectRegistry;

    // === SERVICES ===
    private CombatEventService _eventService;
    private CombatStatusEffectService _statusEffectService;
    private CombatAbilityService _abilityService;
    private CombatExecutionService _executionService;

    // === RUNTIME STATE ===
    private Combatant _player;
    private Combatant _enemy;
    private EnemyDefinition _currentEnemyDef;
    private CombatData _legacyCombatData; // For backwards compatibility with existing UI
    private bool _isCombatActive = false;
    private long _combatStartTimeMs;

    // Instance counting for duplicate abilities
    private Dictionary<AbilityDefinition, int> _abilityInstanceCounts = new Dictionary<AbilityDefinition, int>();

    // === OPTIMIZED ABILITY TRACKING ===
    // Instead of one coroutine per ability (9+ coroutines), we track all abilities in one Update loop
    private class AbilityCooldownTracker
    {
        public AbilityDefinition Ability;
        public bool IsPlayerAbility;
        public int InstanceIndex;
        public float CurrentCooldown;
        public float ElapsedTime;
        public ICombatant Source;
        public ICombatant Target;
        public bool CooldownStartEventSent;
    }
    private List<AbilityCooldownTracker> _abilityTrackers = new List<AbilityCooldownTracker>();

    // Cached stats to avoid recalculating every frame
    private CombatantStats _cachedPlayerStats;
    private CombatantStats _cachedEnemyStats;
    private int _lastPlayerEffectCountForStats;
    private int _lastEnemyEffectCountForStats;
    private bool _playerIsStunnedCached;
    private bool _enemyIsStunnedCached;

    // === PUBLIC ACCESSORS ===
    public bool IsCombatActive => _isCombatActive;
    public CombatData CurrentCombat => _legacyCombatData; // Legacy accessor
    public EnemyDefinition CurrentEnemy => _currentEnemyDef;
    public ICombatant Player => _player;
    public ICombatant Enemy => _enemy;

    // === UNITY LIFECYCLE ===

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeServices();
            Logger.LogInfo("CombatManager: Initialized", Logger.LogCategory.General);
        }
        else
        {
            Logger.LogWarning("CombatManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.General);
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Initialize status effect registry
        if (statusEffectRegistry != null)
        {
            statusEffectRegistry.Initialize();
        }
    }

    void OnDestroy()
    {
        // Clear ability trackers
        _abilityTrackers.Clear();

        _isCombatActive = false;

        // Clear singleton reference if this is the active instance
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (!_isCombatActive || _player == null || _enemy == null)
            return;

        // Check if combat is over
        if (_executionService.IsCombatOver(_player, _enemy))
        {
            bool playerWon = _executionService.DidPlayerWin(_player, _enemy);
            EndCombat(playerWon, playerWon ? "victory" : "defeat");
            return;
        }

        float deltaTime = Time.deltaTime;

        // Update cached stats only when effects change
        UpdateCachedStats();

        // Process all abilities in one pass (replaces 9+ separate coroutines)
        ProcessAllAbilities(deltaTime);

        // Process status effects
        ProcessStatusEffectsUpdate(deltaTime);

        // Sync legacy data
        SyncLegacyCombatData();
    }

    private void UpdateCachedStats()
    {
        // Only recalculate stats when effect count changes
        int playerEffectCount = _player.ActiveEffects.Count;
        int enemyEffectCount = _enemy.ActiveEffects.Count;

        if (playerEffectCount != _lastPlayerEffectCountForStats)
        {
            _lastPlayerEffectCountForStats = playerEffectCount;
            _cachedPlayerStats = _player.GetModifiedStats();
            _playerIsStunnedCached = _player.IsStunned;
        }

        if (enemyEffectCount != _lastEnemyEffectCountForStats)
        {
            _lastEnemyEffectCountForStats = enemyEffectCount;
            _cachedEnemyStats = _enemy.GetModifiedStats();
            _enemyIsStunnedCached = _enemy.IsStunned;
        }
    }

    private void ProcessAllAbilities(float deltaTime)
    {
        for (int i = 0; i < _abilityTrackers.Count; i++)
        {
            var tracker = _abilityTrackers[i];
            if (tracker == null || tracker.Ability == null) continue;

            bool isStunned = tracker.IsPlayerAbility ? _playerIsStunnedCached : _enemyIsStunnedCached;

            // Send cooldown started event if not sent yet
            if (!tracker.CooldownStartEventSent)
            {
                var stats = tracker.IsPlayerAbility ? _cachedPlayerStats : _cachedEnemyStats;
                tracker.CurrentCooldown = _abilityService.GetAdjustedCooldown(tracker.Ability.Cooldown, stats);
                _eventService.PublishAbilityCooldownStarted(tracker.IsPlayerAbility, tracker.Ability, tracker.InstanceIndex, tracker.CurrentCooldown);
                tracker.CooldownStartEventSent = true;
            }

            // Skip if stunned (pause cooldown)
            if (isStunned) continue;

            // Update elapsed time
            tracker.ElapsedTime += deltaTime;

            // Check if cooldown is ready
            if (tracker.ElapsedTime >= tracker.CurrentCooldown)
            {
                // Execute ability
                _abilityService.ExecuteAbility(tracker.Ability, tracker.Source, tracker.Target, tracker.InstanceIndex);

                // Reset for next cycle
                tracker.ElapsedTime = 0f;
                tracker.CooldownStartEventSent = false;

                // Check if combat ended due to this ability
                if (_executionService.IsCombatOver(_player, _enemy))
                {
                    return; // Exit early, Update will handle combat end
                }
            }
        }
    }

    private void ProcessStatusEffectsUpdate(float deltaTime)
    {
        // Process player status effects
        if (_player != null)
        {
            _statusEffectService.ProcessTicks(_player, deltaTime);
        }

        // Process enemy status effects
        if (_enemy != null)
        {
            _statusEffectService.ProcessTicks(_enemy, deltaTime);
        }
    }

    private void InitializeServices()
    {
        _eventService = new CombatEventService(enableDebugLogs);
        _statusEffectService = new CombatStatusEffectService(_eventService, enableDebugLogs);
        _abilityService = new CombatAbilityService(_eventService, _statusEffectService, enableDebugLogs);
        _executionService = new CombatExecutionService(_eventService, _statusEffectService, enableDebugLogs);
    }

    // === PUBLIC API ===

    /// <summary>
    /// Start combat against a specific enemy at current location
    /// </summary>
    public bool StartCombat(EnemyDefinition enemy)
    {
        if (enemy == null)
        {
            Logger.LogError("CombatManager: Cannot start combat - enemy is null", Logger.LogCategory.General);
            return false;
        }

        if (!_executionService.CanStartCombat(_isCombatActive))
        {
            Logger.LogWarning("CombatManager: Cannot start combat in current state", Logger.LogCategory.General);
            return false;
        }

        if (!_executionService.ValidateEnemy(enemy))
        {
            Logger.LogError("CombatManager: Invalid enemy definition", Logger.LogCategory.General);
            return false;
        }

        // Stop current activity if any
        _executionService.StopCurrentActivity();

        // Create combatants - use GetPlayerAbilities() to get from AbilityManager
        var combatants = _executionService.CreateCombatants(enemy, playerMaxHealth, GetPlayerAbilities());
        _player = combatants.player;
        _enemy = combatants.enemy;
        _currentEnemyDef = enemy;

        // Create legacy CombatData for backwards compatibility
        string locationId = _executionService.GetCurrentLocationId();
        _legacyCombatData = _executionService.CreateCombatData(enemy, locationId, playerMaxHealth);

        _isCombatActive = true;
        _combatStartTimeMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        // Clear instance counts
        _abilityInstanceCounts.Clear();

        // Publish combat started event
        _executionService.PublishCombatStarted(_legacyCombatData, enemy);

        if (enableDebugLogs)
        {
            Logger.LogInfo($"CombatManager: Combat started vs {enemy.GetDisplayName()}", Logger.LogCategory.General);
        }

        // Initialize ability trackers (replaces coroutines)
        InitializeAbilityTrackers();

        // Initialize cached stats
        _lastPlayerEffectCountForStats = -1;
        _lastEnemyEffectCountForStats = -1;
        UpdateCachedStats();

        return true;
    }

    /// <summary>
    /// Player flees from combat (forfeit, no rewards)
    /// </summary>
    public void FleeCombat()
    {
        if (!_isCombatActive)
        {
            Logger.LogWarning("CombatManager: No active combat to flee from", Logger.LogCategory.General);
            return;
        }

        if (enableDebugLogs)
        {
            Logger.LogInfo($"CombatManager: Player fled from combat vs {_currentEnemyDef?.GetDisplayName()}", Logger.LogCategory.General);
        }

        EndCombat(false, "fled");
    }

    /// <summary>
    /// Check if combat can be started
    /// </summary>
    public bool CanStartCombat()
    {
        return _executionService.CanStartCombat(_isCombatActive);
    }

    /// <summary>
    /// Get debug information about current combat
    /// </summary>
    public string GetDebugInfo()
    {
        if (!_isCombatActive || _player == null || _enemy == null)
            return "No active combat";

        return _executionService.GetDebugInfo(_player, _enemy, _currentEnemyDef, _combatStartTimeMs);
    }

    // === PLAYER ABILITY MANAGEMENT ===

    /// <summary>
    /// Get currently equipped player abilities from AbilityManager (with fallback to inspector list)
    /// </summary>
    public List<AbilityDefinition> GetPlayerAbilities()
    {
        // Use AbilityManager if available and has equipped abilities
        if (AbilityManager.Instance != null)
        {
            var equippedAbilities = AbilityManager.Instance.GetEquippedAbilities();
            if (equippedAbilities != null && equippedAbilities.Count > 0)
            {
                return equippedAbilities;
            }
        }

        // Fallback to inspector list for testing
        return playerAbilities;
    }

    /// <summary>
    /// Set player abilities (temporary - for testing)
    /// </summary>
    public void SetPlayerAbilities(List<AbilityDefinition> abilities)
    {
        playerAbilities = abilities ?? new List<AbilityDefinition>();
    }

    /// <summary>
    /// Set player max health (temporary - will come from equipment)
    /// </summary>
    public void SetPlayerMaxHealth(float maxHealth)
    {
        playerMaxHealth = Mathf.Max(1, maxHealth);
    }

    // === COMBAT LOGIC ===

    private void InitializeAbilityTrackers()
    {
        _abilityTrackers.Clear();
        _abilityInstanceCounts.Clear();

        // Create trackers for player abilities (from AbilityManager)
        foreach (var ability in GetPlayerAbilities())
        {
            if (ability == null) continue;
            int instanceIndex = GetAndIncrementInstanceCount(ability);
            _abilityTrackers.Add(new AbilityCooldownTracker
            {
                Ability = ability,
                IsPlayerAbility = true,
                InstanceIndex = instanceIndex,
                CurrentCooldown = 0f,
                ElapsedTime = 0f,
                Source = _player,
                Target = _enemy,
                CooldownStartEventSent = false
            });
        }

        // Reset instance counts for enemy abilities
        _abilityInstanceCounts.Clear();

        // Create trackers for enemy abilities
        if (_currentEnemyDef?.Abilities != null)
        {
            foreach (var ability in _currentEnemyDef.Abilities)
            {
                if (ability == null) continue;
                int instanceIndex = GetAndIncrementInstanceCount(ability);
                _abilityTrackers.Add(new AbilityCooldownTracker
                {
                    Ability = ability,
                    IsPlayerAbility = false,
                    InstanceIndex = instanceIndex,
                    CurrentCooldown = 0f,
                    ElapsedTime = 0f,
                    Source = _enemy,
                    Target = _player,
                    CooldownStartEventSent = false
                });
            }
        }

        if (enableDebugLogs)
        {
            Logger.LogInfo($"CombatManager: Initialized {_abilityTrackers.Count} ability trackers ({GetPlayerAbilities().Count} player, {_currentEnemyDef?.Abilities?.Count ?? 0} enemy)", Logger.LogCategory.General);
        }
    }

    private int GetAndIncrementInstanceCount(AbilityDefinition ability)
    {
        if (!_abilityInstanceCounts.ContainsKey(ability))
        {
            _abilityInstanceCounts[ability] = 0;
        }
        int currentCount = _abilityInstanceCounts[ability];
        _abilityInstanceCounts[ability]++;
        return currentCount;
    }

    // === COMBAT END ===

    private void EndCombat(bool playerWon, string reason)
    {
        if (!_isCombatActive) return;

        _isCombatActive = false;

        // Clear ability trackers
        _abilityTrackers.Clear();

        // End combat through service (handles rewards and events)
        _executionService.EndCombat(playerWon, reason, _currentEnemyDef, _player, _enemy);

        if (enableDebugLogs)
        {
            string result = playerWon ? "Victory" : (reason == "fled" ? "Fled" : "Defeat");
            Logger.LogInfo($"CombatManager: Combat ended - {result} vs {_currentEnemyDef?.GetDisplayName()}", Logger.LogCategory.General);
        }

        // Clear combat state
        _legacyCombatData = null;
        _currentEnemyDef = null;
        _player = null;
        _enemy = null;
    }

    // === LEGACY COMPATIBILITY ===

    // Cache for dirty checking to avoid unnecessary iterations
    private float _lastPlayerHealth;
    private float _lastPlayerShield;
    private float _lastEnemyHealth;
    private float _lastEnemyShield;
    private int _lastPlayerEffectCount;
    private int _lastEnemyEffectCount;

    /// <summary>
    /// Sync new combatant data to legacy CombatData for backwards compatibility.
    /// Optimized to only iterate effects when something changed.
    /// </summary>
    private void SyncLegacyCombatData()
    {
        if (_legacyCombatData == null || _player == null || _enemy == null) return;

        // Quick sync for health/shield (no iteration needed)
        _legacyCombatData.PlayerCurrentHealth = _player.CurrentHealth;
        _legacyCombatData.PlayerCurrentShield = _player.CurrentShield;
        _legacyCombatData.EnemyCurrentHealth = _enemy.CurrentHealth;
        _legacyCombatData.EnemyCurrentShield = _enemy.CurrentShield;

        // Only recalculate poison stacks if effect count changed
        int playerEffectCount = _player.ActiveEffects.Count;
        int enemyEffectCount = _enemy.ActiveEffects.Count;

        if (playerEffectCount != _lastPlayerEffectCount)
        {
            _lastPlayerEffectCount = playerEffectCount;
            float playerPoisonStacks = 0;
            var playerEffects = _player.ActiveEffects;
            for (int i = 0; i < playerEffects.Count; i++)
            {
                var effect = playerEffects[i];
                if (effect != null && effect.EffectType == StatusEffectType.Poison)
                {
                    playerPoisonStacks += effect.CurrentStacks;
                }
            }
            _legacyCombatData.PlayerPoisonStacks = playerPoisonStacks;
        }

        if (enemyEffectCount != _lastEnemyEffectCount)
        {
            _lastEnemyEffectCount = enemyEffectCount;
            float enemyPoisonStacks = 0;
            var enemyEffects = _enemy.ActiveEffects;
            for (int i = 0; i < enemyEffects.Count; i++)
            {
                var effect = enemyEffects[i];
                if (effect != null && effect.EffectType == StatusEffectType.Poison)
                {
                    enemyPoisonStacks += effect.CurrentStacks;
                }
            }
            _legacyCombatData.EnemyPoisonStacks = enemyPoisonStacks;
        }

        // Update processed time
        _legacyCombatData.MarkProcessed();
    }

    // === APPLICATION LIFECYCLE ===

    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus && _isCombatActive)
        {
            // App resumed - for now just mark as processed
            // Future: implement background combat simulation
            _legacyCombatData?.MarkProcessed();
        }
    }
}
