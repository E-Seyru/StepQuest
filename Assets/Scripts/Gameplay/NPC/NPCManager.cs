// Purpose: Main NPC manager handling NPC interactions
// Filepath: Assets/Scripts/Gameplay/NPC/NPCManager.cs

using NPCEvents;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages NPC interactions and discovery.
/// Singleton pattern for global access.
/// </summary>
public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private bool enableDebugLogs = false;

    [Header("Registries")]
    [SerializeField] private NPCRegistry npcRegistry;

    // === EVENTS ===
    public event Action<string> OnNPCDiscovered;

    // === PUBLIC ACCESSORS ===
    public NPCRegistry Registry => npcRegistry;

    // === UNITY LIFECYCLE ===

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Logger.LogInfo("NPCManager: Initialized", Logger.LogCategory.General);
        }
        else
        {
            Logger.LogWarning("NPCManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.General);
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // === NPC DISCOVERY ===

    /// <summary>
    /// Discover an NPC (first-time meeting)
    /// </summary>
    public bool DiscoverNPC(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return false;

        // Already discovered?
        if (IsNPCDiscovered(npcId))
        {
            if (enableDebugLogs)
                Logger.LogInfo($"NPCManager: NPC '{npcId}' already discovered", Logger.LogCategory.General);
            return false;
        }

        // Add to PlayerData discovered list
        if (DataManager.Instance?.PlayerData != null)
        {
            DataManager.Instance.PlayerData.AddDiscoveredNPC(npcId);
        }

        if (enableDebugLogs)
            Logger.LogInfo($"NPCManager: Discovered NPC '{npcId}'", Logger.LogCategory.General);

        // Fire events
        OnNPCDiscovered?.Invoke(npcId);
        EventBus.Publish(new NPCDiscoveredEvent(npcId));

        return true;
    }

    /// <summary>
    /// Check if an NPC has been discovered
    /// </summary>
    public bool IsNPCDiscovered(string npcId)
    {
        if (string.IsNullOrEmpty(npcId)) return false;
        return DataManager.Instance?.PlayerData?.HasDiscoveredNPC(npcId) ?? false;
    }

    /// <summary>
    /// Get all discovered NPCs
    /// </summary>
    public List<string> GetDiscoveredNPCs()
    {
        return DataManager.Instance?.PlayerData?.DiscoveredNPCs ?? new List<string>();
    }

    // === UTILITY ===

    /// <summary>
    /// Get NPC definition by ID
    /// </summary>
    public NPCDefinition GetNPCDefinition(string npcId)
    {
        return npcRegistry?.GetNPC(npcId);
    }

    /// <summary>
    /// Interact with an NPC (convenience method for UI)
    /// </summary>
    public void InteractWithNPC(string npcId)
    {
        if (enableDebugLogs)
            Logger.LogInfo($"NPCManager: Interacting with NPC '{npcId}'", Logger.LogCategory.General);

        // Auto-discover on first interaction
        DiscoverNPC(npcId);

        EventBus.Publish(new NPCInteractionStartedEvent(npcId));
    }
}
