// Purpose: Data structure representing an active combat session state
// Filepath: Assets/Scripts/Data/Models/CombatData.cs
using System;
using System.Collections.Generic;

/// <summary>
/// Represents the current state of an active combat session.
/// Designed to be serializable for persistence across app sessions.
/// </summary>
[Serializable]
public class CombatData
{
    // === REFERENCES ===
    public string EnemyId;              // Reference to EnemyDefinition
    public string LocationId;           // Where combat started

    // === HEALTH STATE ===
    public float PlayerCurrentHealth;
    public float PlayerMaxHealth;
    public float PlayerCurrentShield;

    public float EnemyCurrentHealth;
    public float EnemyMaxHealth;
    public float EnemyCurrentShield;

    // === STATUS EFFECTS ===
    public float PlayerPoisonStacks;
    public float EnemyPoisonStacks;

    // === TIMING ===
    public long StartTimeMs;            // When combat started (Unix timestamp)
    public long LastProcessedTimeMs;    // Last time we processed combat (for background)

    // === ABILITY COOLDOWN STATE ===
    // Stores remaining cooldown time for each ability (for resume after background)
    public List<AbilityCooldownState> PlayerAbilityCooldowns = new List<AbilityCooldownState>();
    public List<AbilityCooldownState> EnemyAbilityCooldowns = new List<AbilityCooldownState>();

    /// <summary>
    /// Default constructor for serialization
    /// </summary>
    public CombatData()
    {
        EnemyId = string.Empty;
        LocationId = string.Empty;
        PlayerCurrentHealth = 0;
        PlayerMaxHealth = 0;
        PlayerCurrentShield = 0;
        EnemyCurrentHealth = 0;
        EnemyMaxHealth = 0;
        EnemyCurrentShield = 0;
        PlayerPoisonStacks = 0;
        EnemyPoisonStacks = 0;
        StartTimeMs = 0;
        LastProcessedTimeMs = 0;
    }

    /// <summary>
    /// Constructor to start a new combat
    /// </summary>
    public CombatData(string enemyId, string locationId, float playerMaxHealth, float enemyMaxHealth)
    {
        EnemyId = enemyId;
        LocationId = locationId;

        PlayerCurrentHealth = playerMaxHealth;
        PlayerMaxHealth = playerMaxHealth;
        PlayerCurrentShield = 0;

        EnemyCurrentHealth = enemyMaxHealth;
        EnemyMaxHealth = enemyMaxHealth;
        EnemyCurrentShield = 0;

        PlayerPoisonStacks = 0;
        EnemyPoisonStacks = 0;

        StartTimeMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        LastProcessedTimeMs = StartTimeMs;
    }

    // === UTILITY METHODS ===

    /// <summary>
    /// Check if combat is active (has valid data)
    /// </summary>
    public bool IsActive()
    {
        return !string.IsNullOrEmpty(EnemyId) &&
               PlayerMaxHealth > 0 &&
               EnemyMaxHealth > 0;
    }

    /// <summary>
    /// Check if player is still alive
    /// </summary>
    public bool IsPlayerAlive()
    {
        return PlayerCurrentHealth > 0;
    }

    /// <summary>
    /// Check if enemy is still alive
    /// </summary>
    public bool IsEnemyAlive()
    {
        return EnemyCurrentHealth > 0;
    }

    /// <summary>
    /// Check if combat has ended
    /// </summary>
    public bool IsCombatOver()
    {
        return !IsPlayerAlive() || !IsEnemyAlive();
    }

    /// <summary>
    /// Check if player won (enemy dead, player alive)
    /// </summary>
    public bool DidPlayerWin()
    {
        return IsPlayerAlive() && !IsEnemyAlive();
    }

    /// <summary>
    /// Get elapsed combat time in milliseconds
    /// </summary>
    public long GetElapsedTimeMs()
    {
        if (StartTimeMs <= 0) return 0;
        return DateTimeOffset.Now.ToUnixTimeMilliseconds() - StartTimeMs;
    }

    /// <summary>
    /// Get unprocessed time (for background combat)
    /// </summary>
    public long GetUnprocessedTimeMs()
    {
        if (LastProcessedTimeMs <= 0) return 0;
        return DateTimeOffset.Now.ToUnixTimeMilliseconds() - LastProcessedTimeMs;
    }

    /// <summary>
    /// Update last processed time to now
    /// </summary>
    public void MarkProcessed()
    {
        LastProcessedTimeMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }

    /// <summary>
    /// Apply damage to player (respects shield)
    /// </summary>
    public float DamagePlayer(float damage)
    {
        if (damage <= 0) return 0;

        float actualDamage = damage;

        // Shield absorbs damage first
        if (PlayerCurrentShield > 0)
        {
            if (PlayerCurrentShield >= damage)
            {
                PlayerCurrentShield -= damage;
                return 0; // All absorbed
            }
            else
            {
                actualDamage = damage - PlayerCurrentShield;
                PlayerCurrentShield = 0;
            }
        }

        PlayerCurrentHealth = Math.Max(0, PlayerCurrentHealth - actualDamage);
        return actualDamage;
    }

    /// <summary>
    /// Apply damage to enemy (respects shield)
    /// </summary>
    public float DamageEnemy(float damage)
    {
        if (damage <= 0) return 0;

        float actualDamage = damage;

        // Shield absorbs damage first
        if (EnemyCurrentShield > 0)
        {
            if (EnemyCurrentShield >= damage)
            {
                EnemyCurrentShield -= damage;
                return 0; // All absorbed
            }
            else
            {
                actualDamage = damage - EnemyCurrentShield;
                EnemyCurrentShield = 0;
            }
        }

        EnemyCurrentHealth = Math.Max(0, EnemyCurrentHealth - actualDamage);
        return actualDamage;
    }

    /// <summary>
    /// Heal player
    /// </summary>
    public float HealPlayer(float amount)
    {
        if (amount <= 0) return 0;
        float oldHealth = PlayerCurrentHealth;
        PlayerCurrentHealth = Math.Min(PlayerMaxHealth, PlayerCurrentHealth + amount);
        return PlayerCurrentHealth - oldHealth;
    }

    /// <summary>
    /// Heal enemy
    /// </summary>
    public float HealEnemy(float amount)
    {
        if (amount <= 0) return 0;
        float oldHealth = EnemyCurrentHealth;
        EnemyCurrentHealth = Math.Min(EnemyMaxHealth, EnemyCurrentHealth + amount);
        return EnemyCurrentHealth - oldHealth;
    }

    /// <summary>
    /// Clear combat data
    /// </summary>
    public void Clear()
    {
        EnemyId = string.Empty;
        LocationId = string.Empty;
        PlayerCurrentHealth = 0;
        PlayerMaxHealth = 0;
        PlayerCurrentShield = 0;
        EnemyCurrentHealth = 0;
        EnemyMaxHealth = 0;
        EnemyCurrentShield = 0;
        PlayerPoisonStacks = 0;
        EnemyPoisonStacks = 0;
        StartTimeMs = 0;
        LastProcessedTimeMs = 0;
        PlayerAbilityCooldowns.Clear();
        EnemyAbilityCooldowns.Clear();
    }

    /// <summary>
    /// Validate combat data
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrEmpty(EnemyId)) return false;
        if (PlayerMaxHealth <= 0) return false;
        if (EnemyMaxHealth <= 0) return false;
        if (PlayerCurrentHealth < 0) return false;
        if (EnemyCurrentHealth < 0) return false;
        return true;
    }

    public override string ToString()
    {
        if (!IsActive())
            return "[No Active Combat]";

        return $"[Combat vs {EnemyId}: Player {PlayerCurrentHealth}/{PlayerMaxHealth} vs Enemy {EnemyCurrentHealth}/{EnemyMaxHealth}]";
    }
}

/// <summary>
/// Stores cooldown state for an ability (for persistence)
/// </summary>
[Serializable]
public class AbilityCooldownState
{
    public string AbilityId;
    public int InstanceIndex;           // For duplicate abilities
    public float RemainingCooldown;     // Seconds remaining

    public AbilityCooldownState()
    {
        AbilityId = string.Empty;
        InstanceIndex = 0;
        RemainingCooldown = 0;
    }

    public AbilityCooldownState(string abilityId, int instanceIndex, float remainingCooldown)
    {
        AbilityId = abilityId;
        InstanceIndex = instanceIndex;
        RemainingCooldown = remainingCooldown;
    }
}
