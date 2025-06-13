// Purpose: Auto-validates and cleans registry at runtime startup
// Filepath: Assets/Scripts/Validators/RuntimeRegistryValidator.cs
using UnityEngine;

/// <summary>
/// Valide et nettoie automatiquement les registries au d�marrage du jeu
/// Assure une exp�rience robuste pour les joueurs
/// </summary>
public class RuntimeRegistryValidator : MonoBehaviour
{
    [Header("Registry References")]
    [SerializeField] private ActivityRegistry activityRegistry;
    [SerializeField] private ItemRegistry itemRegistry;

    [Header("Validation Settings")]
    [SerializeField] private bool enableAutoCleanup = true;
    [SerializeField] private bool logValidationResults = true;
    [SerializeField] private bool validateOnAwake = true;

    void Awake()
    {
        if (validateOnAwake)
        {
            ValidateRegistries();
        }
    }

    void Start()
    {
        // Double-check apr�s que tous les syst�mes soient initialis�s
        if (!validateOnAwake)
        {
            ValidateRegistries();
        }
    }

    /// <summary>
    /// Valide et nettoie tous les registries de mani�re silencieuse
    /// </summary>
    public void ValidateRegistries()
    {
        int totalIssues = 0;

        // Validate ActivityRegistry
        if (activityRegistry != null)
        {
            totalIssues += ValidateActivityRegistry() ? 1 : 0;
        }
        else
        {
            Logger.LogError("RuntimeRegistryValidator: ActivityRegistry not assigned in inspector!", Logger.LogCategory.General);
        }

        // Validate ItemRegistry
        if (itemRegistry != null)
        {
            totalIssues += ValidateItemRegistry() ? 1 : 0;
        }
        else
        {
            Logger.LogError("RuntimeRegistryValidator: ItemRegistry not assigned in inspector!", Logger.LogCategory.General);
        }

        if (logValidationResults)
        {
            if (totalIssues > 0)
            {
                Logger.LogInfo("RuntimeRegistryValidator: Registry validation completed with auto-fixes applied", Logger.LogCategory.General);
            }
            else
            {
                Logger.LogInfo("RuntimeRegistryValidator: All registries are valid", Logger.LogCategory.General);
            }
        }
    }

    /// <summary>
    /// Valide l'ActivityRegistry et applique les corrections automatiques
    /// </summary>
    private bool ValidateActivityRegistry()
    {
        bool hadIssues = false;

        if (enableAutoCleanup)
        {
            // Auto-nettoyage silencieux
            int cleanedCount = activityRegistry.CleanNullReferences();
            if (cleanedCount > 0)
            {
                hadIssues = true;
                if (logValidationResults)
                {
                    Logger.LogInfo($"RuntimeRegistryValidator: Auto-cleaned {cleanedCount} broken references from ActivityRegistry", Logger.LogCategory.General);
                }
            }
        }

        // Force refresh du cache pour s'assurer que tout est � jour
        activityRegistry.RefreshCache();

        // Valide que le registry a des activit�s valides
        var validActivities = activityRegistry.GetAllValidActivities();
        if (validActivities.Count == 0)
        {
            Logger.LogWarning("RuntimeRegistryValidator: ActivityRegistry has no valid activities!", Logger.LogCategory.General);
            hadIssues = true;
        }

        return hadIssues;
    }

    /// <summary>
    /// Valide l'ItemRegistry et applique les corrections automatiques
    /// </summary>
    private bool ValidateItemRegistry()
    {
        bool hadIssues = false;

        if (enableAutoCleanup)
        {
            // Auto-nettoyage silencieux
            int cleanedCount = itemRegistry.CleanNullReferences();
            if (cleanedCount > 0)
            {
                hadIssues = true;
                if (logValidationResults)
                {
                    Logger.LogInfo($"RuntimeRegistryValidator: Auto-cleaned {cleanedCount} broken references from ItemRegistry", Logger.LogCategory.General);
                }
            }
        }

        // Force refresh du cache pour s'assurer que tout est � jour
        itemRegistry.RefreshCache();

        // Valide que le registry a des items valides
        var validItems = itemRegistry.GetAllValidItems();
        if (validItems.Count == 0)
        {
            Logger.LogWarning("RuntimeRegistryValidator: ItemRegistry has no valid items!", Logger.LogCategory.General);
            hadIssues = true;
        }

        return hadIssues;
    }

    /// <summary>
    /// M�thode publique pour forcer une revalidation (utile pour debug)
    /// </summary>
    [ContextMenu("Force Validate")]
    public void ForceValidate()
    {
        ValidateRegistries();
    }
}