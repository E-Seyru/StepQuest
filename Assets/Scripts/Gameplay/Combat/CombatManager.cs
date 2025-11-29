// Purpose: Main combat manager handling auto-battler combat with cooldown-based abilities
// Filepath: Assets/Scripts/Gameplay/Combat/CombatManager.cs

using CombatEvents;
using System;
using System.Collections;
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

    // Coroutine references for cleanup
    private List<Coroutine> _abilityCoroutines = new List<Coroutine>();
    private Coroutine _statusEffectCoroutine;

    // Instance counting for duplicate abilities
    private Dictionary<AbilityDefinition, int> _abilityInstanceCounts = new Dictionary<AbilityDefinition, int>();

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
        if (_isCombatActive)
        {
            StopAllCoroutines();
            _isCombatActive = false;
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

        // Create combatants
        var combatants = _executionService.CreateCombatants(enemy, playerMaxHealth, playerAbilities);
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

        // Start ability cycles and status effect processing
        StartAbilityCycles();
        StartStatusEffectProcessing();

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

    // === TEMPORARY: Player ability management (will be replaced by AbilityManager later) ===

    /// <summary>
    /// Get currently equipped player abilities (temporary - will come from AbilityManager)
    /// </summary>
    public List<AbilityDefinition> GetPlayerAbilities()
    {
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

    private void StartAbilityCycles()
    {
        _abilityCoroutines.Clear();
        _abilityInstanceCounts.Clear();

        // Start player ability cycles
        foreach (var ability in playerAbilities)
        {
            if (ability == null) continue;
            int instanceIndex = GetAndIncrementInstanceCount(ability);
            var coroutine = StartCoroutine(ProcessAbilityCycle(ability, true, instanceIndex));
            _abilityCoroutines.Add(coroutine);
        }

        // Reset instance counts for enemy abilities
        _abilityInstanceCounts.Clear();

        // Start enemy ability cycles
        if (_currentEnemyDef?.Abilities != null)
        {
            foreach (var ability in _currentEnemyDef.Abilities)
            {
                if (ability == null) continue;
                int instanceIndex = GetAndIncrementInstanceCount(ability);
                var coroutine = StartCoroutine(ProcessAbilityCycle(ability, false, instanceIndex));
                _abilityCoroutines.Add(coroutine);
            }
        }

        if (enableDebugLogs)
        {
            Logger.LogInfo($"CombatManager: Started {playerAbilities.Count} player abilities and {_currentEnemyDef?.Abilities?.Count ?? 0} enemy abilities", Logger.LogCategory.General);
        }
    }

    private void StartStatusEffectProcessing()
    {
        _statusEffectCoroutine = StartCoroutine(ProcessStatusEffects());
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

    private IEnumerator ProcessAbilityCycle(AbilityDefinition ability, bool isPlayerAbility, int instanceIndex)
    {
        if (ability == null) yield break;

        ICombatant source = isPlayerAbility ? _player : _enemy;
        ICombatant target = isPlayerAbility ? _enemy : _player;

        if (enableDebugLogs)
        {
            Logger.LogInfo($"CombatManager: Starting ability cycle for {source.DisplayName}'s {ability.GetDisplayName()} (instance {instanceIndex})", Logger.LogCategory.General);
        }

        while (_isCombatActive && !_executionService.IsCombatOver(_player, _enemy))
        {
            // Calculate cooldown with speed modifier
            var stats = source.GetModifiedStats();
            float adjustedCooldown = _abilityService.GetAdjustedCooldown(ability.Cooldown, stats);

            // Publish cooldown started event
            _eventService.PublishAbilityCooldownStarted(isPlayerAbility, ability, instanceIndex, adjustedCooldown);

            // Wait for cooldown, checking for stun
            float elapsedTime = 0f;
            while (elapsedTime < adjustedCooldown)
            {
                if (!_isCombatActive || _executionService.IsCombatOver(_player, _enemy))
                    yield break;

                // If stunned, pause cooldown
                if (source.IsStunned)
                {
                    yield return null;
                    continue;
                }

                yield return null;
                elapsedTime += Time.deltaTime;
            }

            // Check if combat is still active
            if (!_isCombatActive || _executionService.IsCombatOver(_player, _enemy))
                yield break;

            // Check if stunned (can't use ability while stunned)
            if (source.IsStunned)
                continue;

            // Execute ability
            _abilityService.ExecuteAbility(ability, source, target, instanceIndex);

            // Update legacy combat data for backwards compatibility
            SyncLegacyCombatData();

            // Check for combat end
            CheckCombatEnd();
        }
    }

    private IEnumerator ProcessStatusEffects()
    {
        while (_isCombatActive && !_executionService.IsCombatOver(_player, _enemy))
        {
            float deltaTime = Time.deltaTime;

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

            // Sync legacy data
            SyncLegacyCombatData();

            // Check for combat end
            CheckCombatEnd();

            yield return null;
        }
    }

    // === COMBAT END ===

    private void CheckCombatEnd()
    {
        if (!_isCombatActive) return;

        if (_executionService.IsCombatOver(_player, _enemy))
        {
            bool playerWon = _executionService.DidPlayerWin(_player, _enemy);
            EndCombat(playerWon, playerWon ? "victory" : "defeat");
        }
    }

    private void EndCombat(bool playerWon, string reason)
    {
        if (!_isCombatActive) return;

        _isCombatActive = false;

        // Stop all coroutines
        StopAllCoroutines();
        _abilityCoroutines.Clear();
        _statusEffectCoroutine = null;

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

    /// <summary>
    /// Sync new combatant data to legacy CombatData for backwards compatibility
    /// </summary>
    private void SyncLegacyCombatData()
    {
        if (_legacyCombatData == null || _player == null || _enemy == null) return;

        // Sync player state
        _legacyCombatData.PlayerCurrentHealth = _player.CurrentHealth;
        _legacyCombatData.PlayerCurrentShield = _player.CurrentShield;

        // Sync poison stacks (for legacy UI)
        float playerPoisonStacks = 0;
        foreach (var effect in _player.ActiveEffects)
        {
            if (effect != null && effect.EffectType == StatusEffectType.Poison)
            {
                playerPoisonStacks += effect.CurrentStacks;
            }
        }
        _legacyCombatData.PlayerPoisonStacks = playerPoisonStacks;

        // Sync enemy state
        _legacyCombatData.EnemyCurrentHealth = _enemy.CurrentHealth;
        _legacyCombatData.EnemyCurrentShield = _enemy.CurrentShield;

        // Sync enemy poison stacks
        float enemyPoisonStacks = 0;
        foreach (var effect in _enemy.ActiveEffects)
        {
            if (effect != null && effect.EffectType == StatusEffectType.Poison)
            {
                enemyPoisonStacks += effect.CurrentStacks;
            }
        }
        _legacyCombatData.EnemyPoisonStacks = enemyPoisonStacks;

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
