// Purpose: Data structure representing an active activity session state (mining, gathering, etc.)
// Filepath: Assets/Scripts/Data/Models/ActivityData.cs
using System;

[Serializable]
public class ActivityData
{
    // === RÉFÉRENCES AUX DÉFINITIONS ===
    public string ActivityId;          // Référence vers ActivityDefinition (ex: "mining")
    public string VariantId;           // Référence vers ActivityVariant (ex: "iron_ore_variant")

    // === ÉTAT DE LA SESSION ===
    public long StartSteps;            // Nombre de pas totaux quand l'activité a commencé
    public int AccumulatedSteps;       // Pas accumulés depuis le dernier tic

    // === MÉTADONNÉES DE SESSION ===
    public long StartTimeMs;           // Timestamp Unix de début (pour calculs offline)
    public string LocationId;          // Dans quelle location l'activité a lieu

    /// <summary>
    /// Constructeur par défaut (requis pour la sérialisation)
    /// </summary>
    public ActivityData()
    {
        ActivityId = string.Empty;
        VariantId = string.Empty;
        StartSteps = 0;
        AccumulatedSteps = 0;
        StartTimeMs = 0;
        LocationId = string.Empty;
    }

    /// <summary>
    /// Constructeur pour créer une nouvelle session d'activité
    /// </summary>
    public ActivityData(string activityId, string variantId, long startSteps, string locationId)
    {
        ActivityId = activityId;
        VariantId = variantId;
        StartSteps = startSteps;
        AccumulatedSteps = 0;
        StartTimeMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        LocationId = locationId;
    }

    // === MÉTHODES UTILITAIRES ===

    /// <summary>
    /// Vérifie si cette activité est valide et active
    /// </summary>
    public bool IsActive()
    {
        return !string.IsNullOrEmpty(ActivityId) &&
               !string.IsNullOrEmpty(VariantId) &&
               StartSteps >= 0;
    }

    /// <summary>
    /// Calcule le progrès vers le prochain tic (0.0 à 1.0)
    /// Nécessite le ActivityVariant pour connaître le ActionCost (steps per tick)
    /// </summary>
    public float GetProgressToNextTick(ActivityVariant variant)
    {
        if (variant == null || variant.ActionCost <= 0) return 0f;
        return (float)AccumulatedSteps / variant.ActionCost;
    }

    /// <summary>
    /// Calcule combien de tics complets peuvent être effectués avec les pas donnés
    /// </summary>
    public int CalculateCompleteTicks(ActivityVariant variant, int additionalSteps)
    {
        if (variant == null || variant.ActionCost <= 0) return 0;

        int totalSteps = AccumulatedSteps + additionalSteps;
        return totalSteps / variant.ActionCost;
    }

    /// <summary>
    /// Met à jour les pas accumulés après avoir effectué des tics
    /// </summary>
    public void ProcessTicks(ActivityVariant variant, int ticksCompleted)
    {
        if (ticksCompleted > 0 && variant != null && variant.ActionCost > 0)
        {
            int stepsUsed = ticksCompleted * variant.ActionCost;
            AccumulatedSteps = Math.Max(0, AccumulatedSteps - stepsUsed);
        }
    }

    /// <summary>
    /// Ajoute des pas à l'accumulation
    /// </summary>
    public void AddSteps(int steps)
    {
        if (steps > 0)
        {
            AccumulatedSteps += steps;
        }
    }

    /// <summary>
    /// Calcule depuis combien de temps l'activité est active (en millisecondes)
    /// </summary>
    public long GetElapsedTimeMs()
    {
        if (StartTimeMs <= 0) return 0;
        return DateTimeOffset.Now.ToUnixTimeMilliseconds() - StartTimeMs;
    }

    /// <summary>
    /// Valide que les données de l'activité sont cohérentes
    /// </summary>
    public bool IsValid()
    {
        // Vérifications de base
        if (string.IsNullOrEmpty(ActivityId)) return false;
        if (string.IsNullOrEmpty(VariantId)) return false;
        if (AccumulatedSteps < 0) return false;
        if (StartSteps < 0) return false;

        return true;
    }

    /// <summary>
    /// Remet à zéro l'activité (pour arrêter proprement)
    /// </summary>
    public void Clear()
    {
        ActivityId = string.Empty;
        VariantId = string.Empty;
        StartSteps = 0;
        AccumulatedSteps = 0;
        StartTimeMs = 0;
        LocationId = string.Empty;
    }

    /// <summary>
    /// Affichage pour le debug - version simple sans ActivityVariant
    /// </summary>
    public override string ToString()
    {
        if (!IsActive())
            return "[No Active Activity]";

        return $"[Activity: {ActivityId}/{VariantId} - Steps: {AccumulatedSteps} accumulated - Location: {LocationId}]";
    }

    /// <summary>
    /// Affichage détaillé pour le debug avec les informations du variant
    /// </summary>
    public string ToString(ActivityVariant variant)
    {
        if (!IsActive())
            return "[No Active Activity]";

        if (variant == null)
            return ToString(); // Fallback

        return $"[Activity: {variant.GetDisplayName()} - Progress: {AccumulatedSteps}/{variant.ActionCost} steps - Resource: {variant.PrimaryResource?.GetDisplayName() ?? "Unknown"}]";
    }
}