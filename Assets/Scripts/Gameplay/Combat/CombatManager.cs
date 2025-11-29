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
/// Designed to continue running in background.
/// </summary>
public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private float poisonTickInterval = 1f;

    [Header("Player Stats (Temporary - will come from equipment later)")]
    [SerializeField] private float playerMaxHealth = 100f;
    [SerializeField] private List<AbilityDefinition> playerAbilities = new List<AbilityDefinition>();

    // === RUNTIME STATE ===
    private CombatData currentCombat;
    private EnemyDefinition currentEnemy;
    private bool isCombatActive = false;

    // Coroutine references for cleanup
    private List<Coroutine> abilityCoroutines = new List<Coroutine>();
    private Coroutine playerPoisonCoroutine;
    private Coroutine enemyPoisonCoroutine;

    // Instance counting for duplicate abilities
    private Dictionary<AbilityDefinition, int> abilityInstanceCounts = new Dictionary<AbilityDefinition, int>();

    // === PUBLIC ACCESSORS ===
    public bool IsCombatActive => isCombatActive;
    public CombatData CurrentCombat => currentCombat;
    public EnemyDefinition CurrentEnemy => currentEnemy;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Logger.LogInfo("CombatManager: Initialized", Logger.LogCategory.General);
        }
        else
        {
            Logger.LogWarning("CombatManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.General);
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        // Cleanup when destroyed
        if (isCombatActive)
        {
            StopAllCoroutines();
            isCombatActive = false;
        }
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

        if (isCombatActive)
        {
            Logger.LogWarning("CombatManager: Combat already active!", Logger.LogCategory.General);
            return false;
        }

        // Check game state
        var gameState = GameManager.Instance?.CurrentState;
        if (gameState == GameState.Traveling)
        {
            Logger.LogWarning("CombatManager: Cannot start combat while traveling", Logger.LogCategory.General);
            return false;
        }

        if (gameState == GameState.DoingActivity)
        {
            // Stop current activity before combat
            ActivityManager.Instance?.StopActivity();
        }

        // Initialize combat
        currentEnemy = enemy;
        string locationId = DataManager.Instance?.PlayerData?.CurrentLocationId ?? "unknown";

        currentCombat = new CombatData(
            enemy.EnemyID,
            locationId,
            playerMaxHealth,
            enemy.MaxHealth
        );

        isCombatActive = true;

        // Clear ability instance counts
        abilityInstanceCounts.Clear();

        // Publish combat started event
        EventBus.Publish(new CombatStartedEvent(currentCombat, currentEnemy));

        if (enableDebugLogs)
        {
            Logger.LogInfo($"CombatManager: Combat started vs {enemy.GetDisplayName()}", Logger.LogCategory.General);
        }

        // Start ability cycles
        StartAbilityCycles();

        return true;
    }

    /// <summary>
    /// Player flees from combat (forfeit, no rewards)
    /// </summary>
    public void FleeCombat()
    {
        if (!isCombatActive)
        {
            Logger.LogWarning("CombatManager: No active combat to flee from", Logger.LogCategory.General);
            return;
        }

        if (enableDebugLogs)
        {
            Logger.LogInfo($"CombatManager: Player fled from combat vs {currentEnemy?.GetDisplayName()}", Logger.LogCategory.General);
        }

        // Publish fled event
        EventBus.Publish(new CombatFledEvent(currentEnemy));

        // End combat with no rewards
        EndCombat(false, "fled");
    }

    /// <summary>
    /// Check if combat can be started
    /// </summary>
    public bool CanStartCombat()
    {
        if (isCombatActive) return false;

        var gameState = GameManager.Instance?.CurrentState;
        if (gameState == GameState.Traveling) return false;
        if (gameState == GameState.InCombat) return false;

        return true;
    }

    /// <summary>
    /// Get debug information about current combat
    /// </summary>
    public string GetDebugInfo()
    {
        if (!isCombatActive || currentCombat == null)
            return "No active combat";

        return $"Combat vs {currentEnemy?.GetDisplayName() ?? "Unknown"}\n" +
               $"Player: {currentCombat.PlayerCurrentHealth:F0}/{currentCombat.PlayerMaxHealth:F0} HP " +
               $"(Shield: {currentCombat.PlayerCurrentShield:F0}, Poison: {currentCombat.PlayerPoisonStacks:F0})\n" +
               $"Enemy: {currentCombat.EnemyCurrentHealth:F0}/{currentCombat.EnemyMaxHealth:F0} HP " +
               $"(Shield: {currentCombat.EnemyCurrentShield:F0}, Poison: {currentCombat.EnemyPoisonStacks:F0})\n" +
               $"Duration: {TimeSpan.FromMilliseconds(currentCombat.GetElapsedTimeMs()).TotalSeconds:F1}s";
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
        abilityCoroutines.Clear();
        abilityInstanceCounts.Clear();

        // Start player ability cycles
        foreach (var ability in playerAbilities)
        {
            if (ability == null) continue;
            int instanceIndex = GetAndIncrementInstanceCount(ability);
            var coroutine = StartCoroutine(ProcessAbilityCycle(ability, true, instanceIndex));
            abilityCoroutines.Add(coroutine);
        }

        // Reset instance counts for enemy abilities
        abilityInstanceCounts.Clear();

        // Start enemy ability cycles
        if (currentEnemy?.Abilities != null)
        {
            foreach (var ability in currentEnemy.Abilities)
            {
                if (ability == null) continue;
                int instanceIndex = GetAndIncrementInstanceCount(ability);
                var coroutine = StartCoroutine(ProcessAbilityCycle(ability, false, instanceIndex));
                abilityCoroutines.Add(coroutine);
            }
        }

        if (enableDebugLogs)
        {
            Logger.LogInfo($"CombatManager: Started {playerAbilities.Count} player abilities and {currentEnemy?.Abilities?.Count ?? 0} enemy abilities", Logger.LogCategory.General);
        }
    }

    private int GetAndIncrementInstanceCount(AbilityDefinition ability)
    {
        if (!abilityInstanceCounts.ContainsKey(ability))
        {
            abilityInstanceCounts[ability] = 0;
        }
        int currentCount = abilityInstanceCounts[ability];
        abilityInstanceCounts[ability]++;
        return currentCount;
    }

    private IEnumerator ProcessAbilityCycle(AbilityDefinition ability, bool isPlayerAbility, int instanceIndex)
    {
        if (ability == null) yield break;

        if (enableDebugLogs)
        {
            string source = isPlayerAbility ? "Player" : "Enemy";
            Logger.LogInfo($"CombatManager: Starting ability cycle for {source}'s {ability.GetDisplayName()} (instance {instanceIndex})", Logger.LogCategory.General);
        }

        while (isCombatActive && !currentCombat.IsCombatOver())
        {
            // Publish cooldown started event
            EventBus.Publish(new CombatAbilityCooldownStartedEvent(isPlayerAbility, ability, instanceIndex, ability.Cooldown));

            // Wait for cooldown
            yield return new WaitForSeconds(ability.Cooldown);

            // Check if combat is still active
            if (!isCombatActive || currentCombat.IsCombatOver()) yield break;

            // Process ability effect
            ProcessAbilityEffect(ability, isPlayerAbility, instanceIndex);

            // Check for combat end
            CheckCombatEnd();
        }
    }

    private void ProcessAbilityEffect(AbilityDefinition ability, bool isPlayerAbility, int instanceIndex)
    {
        if (ability == null || currentCombat == null) return;

        float damageDealt = 0;
        float healingDone = 0;
        float shieldAdded = 0;
        float poisonApplied = 0;

        // Process Damage
        if (ability.HasEffect(AbilityEffectType.Damage) && ability.DamageAmount > 0)
        {
            if (isPlayerAbility)
            {
                damageDealt = currentCombat.DamageEnemy(ability.DamageAmount);
                PublishHealthChanged(false);
            }
            else
            {
                damageDealt = currentCombat.DamagePlayer(ability.DamageAmount);
                PublishHealthChanged(true);
            }
        }

        // Process Heal
        if (ability.HasEffect(AbilityEffectType.Heal) && ability.HealAmount > 0)
        {
            if (isPlayerAbility)
            {
                healingDone = currentCombat.HealPlayer(ability.HealAmount);
                PublishHealthChanged(true);
            }
            else
            {
                healingDone = currentCombat.HealEnemy(ability.HealAmount);
                PublishHealthChanged(false);
            }
        }

        // Process Poison
        if (ability.HasEffect(AbilityEffectType.Poison) && ability.PoisonAmount > 0)
        {
            poisonApplied = ability.PoisonAmount;
            if (isPlayerAbility)
            {
                currentCombat.EnemyPoisonStacks += poisonApplied;
                StartEnemyPoisonIfNeeded();
            }
            else
            {
                currentCombat.PlayerPoisonStacks += poisonApplied;
                StartPlayerPoisonIfNeeded();
            }
        }

        // Process Shield
        if (ability.HasEffect(AbilityEffectType.Shield) && ability.ShieldAmount > 0)
        {
            shieldAdded = ability.ShieldAmount;
            if (isPlayerAbility)
            {
                currentCombat.PlayerCurrentShield += shieldAdded;
                PublishHealthChanged(true);
            }
            else
            {
                currentCombat.EnemyCurrentShield += shieldAdded;
                PublishHealthChanged(false);
            }
        }

        // Publish ability used event
        EventBus.Publish(new CombatAbilityUsedEvent(
            isPlayerAbility, ability, instanceIndex,
            damageDealt, healingDone, shieldAdded, poisonApplied
        ));

        if (enableDebugLogs)
        {
            string source = isPlayerAbility ? "Player" : "Enemy";
            Logger.LogInfo($"CombatManager: {source} used {ability.GetDisplayName()} - Damage: {damageDealt}, Heal: {healingDone}, Shield: {shieldAdded}, Poison: {poisonApplied}", Logger.LogCategory.General);
        }
    }

    // === POISON SYSTEM ===

    private void StartPlayerPoisonIfNeeded()
    {
        if (playerPoisonCoroutine == null && currentCombat.PlayerPoisonStacks > 0)
        {
            playerPoisonCoroutine = StartCoroutine(ProcessPoisonTick(true));
        }
    }

    private void StartEnemyPoisonIfNeeded()
    {
        if (enemyPoisonCoroutine == null && currentCombat.EnemyPoisonStacks > 0)
        {
            enemyPoisonCoroutine = StartCoroutine(ProcessPoisonTick(false));
        }
    }

    private IEnumerator ProcessPoisonTick(bool isPlayer)
    {
        while (isCombatActive && !currentCombat.IsCombatOver())
        {
            float poisonStacks = isPlayer ? currentCombat.PlayerPoisonStacks : currentCombat.EnemyPoisonStacks;

            if (poisonStacks <= 0)
            {
                // Poison ended
                if (isPlayer)
                    playerPoisonCoroutine = null;
                else
                    enemyPoisonCoroutine = null;
                yield break;
            }

            yield return new WaitForSeconds(poisonTickInterval);

            if (!isCombatActive || currentCombat.IsCombatOver()) yield break;

            // Re-read stacks after wait (may have changed if more poison was added)
            poisonStacks = isPlayer ? currentCombat.PlayerPoisonStacks : currentCombat.EnemyPoisonStacks;

            // Apply poison damage
            float poisonDamage = poisonStacks;
            if (isPlayer)
            {
                currentCombat.DamagePlayer(poisonDamage);
                PublishHealthChanged(true);
            }
            else
            {
                currentCombat.DamageEnemy(poisonDamage);
                PublishHealthChanged(false);
            }

            // Publish poison tick event
            EventBus.Publish(new CombatPoisonTickEvent(isPlayer, poisonDamage, poisonStacks));

            if (enableDebugLogs)
            {
                string target = isPlayer ? "Player" : "Enemy";
                Logger.LogInfo($"CombatManager: {target} took {poisonDamage} poison damage", Logger.LogCategory.General);
            }

            // Check for combat end
            CheckCombatEnd();
        }
    }

    // === COMBAT END ===

    private void CheckCombatEnd()
    {
        if (!isCombatActive || currentCombat == null) return;

        if (currentCombat.IsCombatOver())
        {
            bool playerWon = currentCombat.DidPlayerWin();
            EndCombat(playerWon, playerWon ? "victory" : "defeat");
        }
    }

    private void EndCombat(bool playerWon, string reason)
    {
        if (!isCombatActive) return;

        isCombatActive = false;

        // Stop all coroutines
        StopAllCoroutines();
        abilityCoroutines.Clear();
        playerPoisonCoroutine = null;
        enemyPoisonCoroutine = null;

        // Calculate rewards
        int experienceGained = 0;
        Dictionary<ItemDefinition, int> lootDropped = new Dictionary<ItemDefinition, int>();

        if (playerWon && currentEnemy != null)
        {
            experienceGained = currentEnemy.ExperienceReward;
            lootDropped = currentEnemy.GenerateLoot();

            // Add loot to inventory
            var inventoryManager = InventoryManager.Instance;
            if (inventoryManager != null)
            {
                foreach (var loot in lootDropped)
                {
                    if (loot.Key != null && loot.Value > 0)
                    {
                        inventoryManager.AddItem("player", loot.Key.ItemID, loot.Value);
                    }
                }
            }

            // TODO: Add experience to combat skill when skill system is integrated
        }

        // Publish combat ended event
        EventBus.Publish(new CombatEndedEvent(playerWon, currentEnemy, experienceGained, lootDropped, reason));

        if (enableDebugLogs)
        {
            string result = playerWon ? "Victory" : (reason == "fled" ? "Fled" : "Defeat");
            Logger.LogInfo($"CombatManager: Combat ended - {result} vs {currentEnemy?.GetDisplayName()}", Logger.LogCategory.General);
        }

        // Clear combat state
        currentCombat = null;
        currentEnemy = null;
    }

    // === HELPER METHODS ===

    private void PublishHealthChanged(bool isPlayer)
    {
        if (currentCombat == null) return;

        if (isPlayer)
        {
            EventBus.Publish(new CombatHealthChangedEvent(
                true,
                currentCombat.PlayerCurrentHealth,
                currentCombat.PlayerMaxHealth,
                currentCombat.PlayerCurrentShield
            ));
        }
        else
        {
            EventBus.Publish(new CombatHealthChangedEvent(
                false,
                currentCombat.EnemyCurrentHealth,
                currentCombat.EnemyMaxHealth,
                currentCombat.EnemyCurrentShield
            ));
        }
    }

    // === APPLICATION LIFECYCLE ===

    void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus && isCombatActive)
        {
            // App resumed - process any time that passed while backgrounded
            ProcessBackgroundCombat();
        }
    }

    private void ProcessBackgroundCombat()
    {
        if (!isCombatActive || currentCombat == null) return;

        long unprocessedTimeMs = currentCombat.GetUnprocessedTimeMs();
        if (unprocessedTimeMs <= 0) return;

        if (enableDebugLogs)
        {
            Logger.LogInfo($"CombatManager: Processing {unprocessedTimeMs}ms of background combat", Logger.LogCategory.General);
        }

        // For now, we just mark as processed
        // In the future, we could simulate the combat that happened
        // But this is complex because abilities have different cooldowns
        currentCombat.MarkProcessed();

        // Check if combat ended during background
        CheckCombatEnd();
    }
}
