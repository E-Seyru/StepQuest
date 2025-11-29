// Purpose: Service responsible for combat flow control (start, end, flee)
// Filepath: Assets/Scripts/Gameplay/Combat/Services/CombatExecutionService.cs

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Service responsible for managing combat lifecycle: starting, ending, and fleeing.
/// Handles reward calculation and validation.
/// </summary>
public class CombatExecutionService
{
    private readonly CombatEventService _eventService;
    private readonly CombatStatusEffectService _statusEffectService;
    private readonly bool _enableDebugLogs;

    public CombatExecutionService(CombatEventService eventService, CombatStatusEffectService statusEffectService, bool enableDebugLogs = false)
    {
        _eventService = eventService;
        _statusEffectService = statusEffectService;
        _enableDebugLogs = enableDebugLogs;
    }

    // === VALIDATION ===

    /// <summary>
    /// Check if combat can be started
    /// </summary>
    public bool CanStartCombat(bool isCurrentlyInCombat)
    {
        if (isCurrentlyInCombat) return false;

        var gameState = GameManager.Instance?.CurrentState;
        if (gameState == GameState.Traveling) return false;
        if (gameState == GameState.InCombat) return false;

        return true;
    }

    /// <summary>
    /// Validate that an enemy can be fought
    /// </summary>
    public bool ValidateEnemy(EnemyDefinition enemy)
    {
        if (enemy == null) return false;
        if (string.IsNullOrEmpty(enemy.EnemyID)) return false;
        if (enemy.MaxHealth <= 0) return false;
        return true;
    }

    // === COMBAT START ===

    /// <summary>
    /// Initialize combatants for a new combat
    /// </summary>
    public (Combatant player, Combatant enemy) CreateCombatants(
        EnemyDefinition enemyDef,
        float playerMaxHealth,
        List<AbilityDefinition> playerAbilities)
    {
        var player = Combatant.CreatePlayer(playerMaxHealth, playerAbilities);
        var enemy = Combatant.CreateFromEnemy(enemyDef);

        return (player, enemy);
    }

    /// <summary>
    /// Create CombatData for legacy compatibility
    /// </summary>
    public CombatData CreateCombatData(EnemyDefinition enemy, string locationId, float playerMaxHealth)
    {
        return new CombatData(
            enemy.EnemyID,
            locationId,
            playerMaxHealth,
            enemy.MaxHealth
        );
    }

    /// <summary>
    /// Publish combat started event
    /// </summary>
    public void PublishCombatStarted(CombatData combatData, EnemyDefinition enemy)
    {
        _eventService.PublishCombatStarted(combatData, enemy);
        _statusEffectService.Reset();

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatExecutionService: Combat started vs {enemy.GetDisplayName()}", Logger.LogCategory.General);
        }
    }

    // === COMBAT END ===

    /// <summary>
    /// Check if combat should end
    /// </summary>
    public bool IsCombatOver(ICombatant player, ICombatant enemy)
    {
        if (player == null || enemy == null) return true;
        return !player.IsAlive || !enemy.IsAlive;
    }

    /// <summary>
    /// Determine if player won
    /// </summary>
    public bool DidPlayerWin(ICombatant player, ICombatant enemy)
    {
        return player != null && player.IsAlive && (enemy == null || !enemy.IsAlive);
    }

    /// <summary>
    /// End combat and handle rewards
    /// </summary>
    public void EndCombat(
        bool playerWon,
        string reason,
        EnemyDefinition enemy,
        ICombatant playerCombatant,
        ICombatant enemyCombatant)
    {
        // Calculate rewards
        int experienceGained = 0;
        Dictionary<ItemDefinition, int> lootDropped = new Dictionary<ItemDefinition, int>();

        if (playerWon && enemy != null)
        {
            experienceGained = enemy.ExperienceReward;
            lootDropped = enemy.GenerateLoot();

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

        // Clear status effects
        if (playerCombatant != null)
        {
            _statusEffectService.ClearAllEffects(playerCombatant, "combat_ended");
        }
        if (enemyCombatant != null)
        {
            _statusEffectService.ClearAllEffects(enemyCombatant, "combat_ended");
        }

        // Publish combat ended event
        _eventService.PublishCombatEnded(playerWon, enemy, experienceGained, lootDropped, reason);

        if (_enableDebugLogs)
        {
            string result = playerWon ? "Victory" : (reason == "fled" ? "Fled" : "Defeat");
            Logger.LogInfo($"CombatExecutionService: Combat ended - {result} vs {enemy?.GetDisplayName()}", Logger.LogCategory.General);
        }
    }

    /// <summary>
    /// Handle player fleeing combat
    /// </summary>
    public void FleeCombat(EnemyDefinition enemy, ICombatant playerCombatant, ICombatant enemyCombatant)
    {
        // Publish fled event first
        _eventService.PublishCombatFled(enemy);

        // Then end combat with no rewards
        EndCombat(false, "fled", enemy, playerCombatant, enemyCombatant);

        if (_enableDebugLogs)
        {
            Logger.LogInfo($"CombatExecutionService: Player fled from {enemy?.GetDisplayName()}", Logger.LogCategory.General);
        }
    }

    // === HELPER METHODS ===

    /// <summary>
    /// Get current location ID for combat data
    /// </summary>
    public string GetCurrentLocationId()
    {
        return DataManager.Instance?.PlayerData?.CurrentLocationId ?? "unknown";
    }

    /// <summary>
    /// Stop current activity before combat
    /// </summary>
    public void StopCurrentActivity()
    {
        var gameState = GameManager.Instance?.CurrentState;
        if (gameState == GameState.DoingActivity)
        {
            ActivityManager.Instance?.StopActivity();
        }
    }

    /// <summary>
    /// Get debug info about combat state
    /// </summary>
    public string GetDebugInfo(ICombatant player, ICombatant enemy, EnemyDefinition enemyDef, long startTimeMs)
    {
        if (player == null || enemy == null)
            return "No active combat";

        var elapsed = System.TimeSpan.FromMilliseconds(
            System.DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTimeMs
        );

        return $"Combat vs {enemyDef?.GetDisplayName() ?? "Unknown"}\n" +
               $"Player: {player.CurrentHealth:F0}/{player.MaxHealth:F0} HP " +
               $"(Shield: {player.CurrentShield:F0})\n" +
               $"Enemy: {enemy.CurrentHealth:F0}/{enemy.MaxHealth:F0} HP " +
               $"(Shield: {enemy.CurrentShield:F0})\n" +
               $"Duration: {elapsed.TotalSeconds:F1}s";
    }
}
