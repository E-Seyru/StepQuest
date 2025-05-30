// Purpose: Data structure representing an active activity session state (mining, gathering, etc.)
// Filepath: Assets/Scripts/Data/Models/ActivityData.cs
using System;

[Serializable]
public class ActivityData
{
    // === REFERENCES AUX DEFINITIONS ===
    public string ActivityId;          // Reference vers ActivityDefinition (ex: "mining")
    public string VariantId;           // Reference vers ActivityVariant (ex: "iron_ore_variant")

    // === ETAT DE LA SESSION ===
    public long StartSteps;            // Nombre de pas totaux quand l'activite a commence
    public int AccumulatedSteps;       // Pas accumules depuis le dernier tic
    public long LastProcessedTotalSteps; // NOUVEAU : Dernier TotalSteps qu'on a traite

    // === METADONNEES DE SESSION ===
    public long StartTimeMs;           // Timestamp Unix de debut (pour calculs offline)
    public string LocationId;          // Dans quelle location l'activite a lieu

    /// <summary>
    /// Constructeur par defaut (requis pour la serialisation)
    /// </summary>
    public ActivityData()
    {
        ActivityId = string.Empty;
        VariantId = string.Empty;
        StartSteps = 0;
        AccumulatedSteps = 0;
        LastProcessedTotalSteps = 0;
        StartTimeMs = 0;
        LocationId = string.Empty;
    }

    /// <summary>
    /// Constructeur pour creer une nouvelle session d'activite
    /// </summary>
    public ActivityData(string activityId, string variantId, long startSteps, string locationId)
    {
        ActivityId = activityId;
        VariantId = variantId;
        StartSteps = startSteps;
        AccumulatedSteps = 0;
        LastProcessedTotalSteps = startSteps; // NOUVEAU : Initialiser avec StartSteps
        StartTimeMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        LocationId = locationId;
    }

    // === METHODES UTILITAIRES ===

    /// <summary>
    /// Verifie si cette activite est valide et active
    /// </summary>
    public bool IsActive()
    {
        return !string.IsNullOrEmpty(ActivityId) &&
               !string.IsNullOrEmpty(VariantId) &&
               StartSteps >= 0;
    }

    /// <summary>
    /// Calcule le progrès vers le prochain tic (0.0 à 1.0)
    /// Necessite le ActivityVariant pour connaître le ActionCost (steps per tick)
    /// </summary>
    public float GetProgressToNextTick(ActivityVariant variant)
    {
        if (variant == null || variant.ActionCost <= 0) return 0f;
        return (float)AccumulatedSteps / variant.ActionCost;
    }

    /// <summary>
    /// Calcule combien de tics complets peuvent être effectues avec les pas donnes
    /// </summary>
    public int CalculateCompleteTicks(ActivityVariant variant, int additionalSteps)
    {
        if (variant == null || variant.ActionCost <= 0) return 0;

        int totalSteps = AccumulatedSteps + additionalSteps;
        return totalSteps / variant.ActionCost;
    }

    /// <summary>
    /// Met à jour les pas accumules après avoir effectue des tics
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
    /// Ajoute des pas à l'accumulation et met à jour LastProcessedTotalSteps
    /// </summary>
    public void AddSteps(int steps)
    {
        if (steps > 0)
        {
            AccumulatedSteps += steps;
            LastProcessedTotalSteps += steps; // NOUVEAU : Garder trace des pas traites
        }
    }

    /// <summary>
    /// Calcule depuis combien de temps l'activite est active (en millisecondes)
    /// </summary>
    public long GetElapsedTimeMs()
    {
        if (StartTimeMs <= 0) return 0;
        return DateTimeOffset.Now.ToUnixTimeMilliseconds() - StartTimeMs;
    }

    /// <summary>
    /// Valide que les donnees de l'activite sont coherentes
    /// </summary>
    public bool IsValid()
    {
        // Verifications de base
        if (string.IsNullOrEmpty(ActivityId)) return false;
        if (string.IsNullOrEmpty(VariantId)) return false;
        if (AccumulatedSteps < 0) return false;
        if (StartSteps < 0) return false;
        if (LastProcessedTotalSteps < StartSteps) return false; // NOUVEAU

        return true;
    }

    /// <summary>
    /// Remet à zero l'activite (pour arrêter proprement)
    /// </summary>
    public void Clear()
    {
        ActivityId = string.Empty;
        VariantId = string.Empty;
        StartSteps = 0;
        AccumulatedSteps = 0;
        LastProcessedTotalSteps = 0; // NOUVEAU
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
    /// Affichage detaille pour le debug avec les informations du variant
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