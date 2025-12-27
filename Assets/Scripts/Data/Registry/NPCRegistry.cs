// Purpose: Central registry for all game NPCs, provides fast lookup and validation
// Filepath: Assets/Scripts/Data/Registry/NPCRegistry.cs
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "NPCRegistry", menuName = "WalkAndRPG/Social/NPC Registry")]
public class NPCRegistry : ScriptableObject
{
    [Header("All Game NPCs")]
    [Tooltip("Drag all your NPCDefinition assets here")]
    public List<NPCDefinition> AllNPCs = new List<NPCDefinition>();

    [Header("Robustness Settings")]
    [SerializeField] private bool enableFallbackSearch = true;
    [SerializeField] private bool logMissingNPCs = false;

    [Header("Debug Info")]
    [Tooltip("Shows validation errors if any")]
    [TextArea(3, 6)]
    public string ValidationStatus = "Click 'Validate Registry' to check for issues";

    // Cache for fast lookup
    private Dictionary<string, NPCDefinition> npcLookup;
    private HashSet<string> loggedMissingNPCs = new HashSet<string>();
    private const int MaxLoggedMissing = 100;

    /// <summary>
    /// Get NPC by ID (fast lookup via cache)
    /// </summary>
    public NPCDefinition GetNPC(string npcId)
    {
        if (npcLookup == null)
        {
            InitializeLookupCache();
        }

        if (string.IsNullOrEmpty(npcId))
        {
            return null;
        }

        if (npcLookup.TryGetValue(npcId, out NPCDefinition npc))
        {
            return npc;
        }

        // Fallback: search by name
        if (enableFallbackSearch)
        {
            npc = FindNPCByNameFallback(npcId);
            if (npc != null)
            {
                return npc;
            }
        }

        // Log once per missing NPC (with cache limit to prevent unbounded growth)
        if (logMissingNPCs && !loggedMissingNPCs.Contains(npcId) && loggedMissingNPCs.Count < MaxLoggedMissing)
        {
            Logger.LogWarning($"NPCRegistry: NPC '{npcId}' not found in registry", Logger.LogCategory.General);
            loggedMissingNPCs.Add(npcId);
        }

        return null;
    }

    /// <summary>
    /// Search for NPC by name if exact ID not found
    /// </summary>
    private NPCDefinition FindNPCByNameFallback(string npcId)
    {
        foreach (var npc in AllNPCs?.Where(n => n != null) ?? Enumerable.Empty<NPCDefinition>())
        {
            if (MatchesNPCName(npc, npcId))
            {
                return npc;
            }
        }
        return null;
    }

    /// <summary>
    /// Flexible name matching
    /// </summary>
    private bool MatchesNPCName(NPCDefinition npc, string searchName)
    {
        if (npc?.NPCID == null) return false;

        string npcIdLower = npc.NPCID.ToLower().Replace(" ", "_");
        string npcNameLower = npc.NPCName?.ToLower().Replace(" ", "_") ?? "";
        string search = searchName.ToLower().Replace(" ", "_");

        return npcIdLower == search ||
               npcNameLower == search ||
               npcIdLower.Contains(search) ||
               search.Contains(npcIdLower);
    }

    /// <summary>
    /// Check if an NPC exists in registry
    /// </summary>
    public bool HasNPC(string npcId)
    {
        return GetNPC(npcId) != null;
    }

    /// <summary>
    /// Get all active NPCs
    /// </summary>
    public List<NPCDefinition> GetActiveNPCs()
    {
        return AllNPCs?.Where(npc => npc != null && npc.IsActive).ToList() ?? new List<NPCDefinition>();
    }

    /// <summary>
    /// Initialize the lookup cache
    /// </summary>
    private void InitializeLookupCache()
    {
        npcLookup = new Dictionary<string, NPCDefinition>();

        if (AllNPCs == null) return;

        foreach (var npc in AllNPCs.Where(n => n != null))
        {
            if (!string.IsNullOrEmpty(npc.NPCID))
            {
                if (!npcLookup.ContainsKey(npc.NPCID))
                {
                    npcLookup[npc.NPCID] = npc;
                }
                else
                {
                    Logger.LogError($"NPCRegistry: Duplicate NPCID '{npc.NPCID}' found!", Logger.LogCategory.General);
                }
            }
        }

        Logger.LogInfo($"NPCRegistry: Cache initialized with {npcLookup.Count} NPCs", Logger.LogCategory.General);
    }

    /// <summary>
    /// Clean null references
    /// </summary>
    public int CleanNullReferences()
    {
        if (AllNPCs == null) return 0;

        int removedCount = 0;

        for (int i = AllNPCs.Count - 1; i >= 0; i--)
        {
            if (AllNPCs[i] == null)
            {
                AllNPCs.RemoveAt(i);
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            RefreshCache();
        }

        return removedCount;
    }

    /// <summary>
    /// Force refresh the lookup cache
    /// </summary>
    public void RefreshCache()
    {
        npcLookup = null;
        loggedMissingNPCs.Clear();
        InitializeLookupCache();
    }

    /// <summary>
    /// Validate all NPCs in the registry
    /// </summary>
    [ContextMenu("Validate Registry")]
    public void ValidateRegistry()
    {
        var issues = new List<string>();
        var npcIds = new HashSet<string>();

        int cleanedCount = CleanNullReferences();
        if (cleanedCount > 0)
        {
            issues.Add($"Auto-cleaned {cleanedCount} null references");
        }

        var nullCount = AllNPCs?.Count(npc => npc == null) ?? 0;
        if (nullCount > 0)
        {
            issues.Add($"{nullCount} null NPC(s) in registry");
        }

        foreach (var npc in AllNPCs?.Where(n => n != null) ?? Enumerable.Empty<NPCDefinition>())
        {
            if (!npc.IsValid())
            {
                issues.Add($"NPC '{npc.name}' failed validation");
            }

            if (!string.IsNullOrEmpty(npc.NPCID))
            {
                if (npcIds.Contains(npc.NPCID))
                {
                    issues.Add($"Duplicate NPCID: '{npc.NPCID}'");
                }
                else
                {
                    npcIds.Add(npc.NPCID);
                }
            }
            else
            {
                issues.Add($"NPC '{npc.name}' has empty NPCID");
            }

            if (npc.Avatar == null && npc.Illustration == null)
            {
                issues.Add($"NPC '{npc.NPCID}' missing avatar/illustration");
            }
        }

        if (issues.Count == 0)
        {
            ValidationStatus = $"✅ Registry is valid! ({AllNPCs?.Count ?? 0} NPCs)";
        }
        else
        {
            ValidationStatus = $"⚠️ Issues found:\n• " + string.Join("\n• ", issues);
        }

        Logger.LogInfo($"NPCRegistry: Validation complete. {issues.Count} issue(s) found.", Logger.LogCategory.General);

        RefreshCache();
    }

    /// <summary>
    /// Get debug info about the registry
    /// </summary>
    public string GetDebugInfo()
    {
        var validNPCs = AllNPCs?.Count(n => n != null) ?? 0;
        var totalNPCs = AllNPCs?.Count ?? 0;
        var activeNPCs = AllNPCs?.Count(n => n != null && n.IsActive) ?? 0;

        return $"NPCRegistry: {validNPCs}/{totalNPCs} valid NPCs loaded\n" +
               $"Cache: {(npcLookup?.Count ?? 0)} NPCs\n" +
               $"Active: {activeNPCs}";
    }

    /// <summary>
    /// Get all valid NPCs
    /// </summary>
    public List<NPCDefinition> GetAllValidNPCs()
    {
        return AllNPCs?.Where(n => n != null).ToList() ?? new List<NPCDefinition>();
    }

    void OnEnable()
    {
        CleanNullReferences();
        RefreshCache();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (AllNPCs != null && AllNPCs.Count > 0)
        {
            UnityEditor.EditorApplication.delayCall += () => ValidateRegistry();
        }
    }
#endif
}
