// Purpose: Data structure representing an active activity session state (mining, gathering, etc.)
// Filepath: Assets/Scripts/Data/Models/ActivityData.cs
using System;

[Serializable]
public class ActivityData
{
    // === R�F�RENCES AUX D�FINITIONS ===
    public string ActivityId;          // R�f�rence vers ActivityDefinition (ex: "mining")
    public string VariantId;           // R�f�rence vers ActivityVariant (ex: "iron_ore_variant")

    // === �TAT DE LA SESSION ===
    public long StartSteps;            // Nombre de pas totaux quand l'activit� a commenc�
    public int AccumulatedSteps;       // Pas accumul�s depuis le dernier tic

    // === M�TADONN�ES DE SESSION ===
    public long StartTimeMs;           // Timestamp Unix de d�but (pour calculs offline)
    public string LocationId;          // Dans quelle location l'activit� a lieu

    /// <summary>
    /// Constructeur par d�faut (requis pour la s�rialisation)
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
    /// Constructeur pour cr�er une nouvelle session d'activit�
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

    // === M�THODES UTILITAIRES ===

    /// <summary>
    /// V�rifie si cette activit� est valide et active
    /// </summary>
    public bool IsActive()
    {
        return !string.IsNullOrEmpty(ActivityId) &&
               !string.IsNullOrEmpty(VariantId) &&
               StartSteps >= 0;
    }

    /// <summary>
    /// Calcule le progr�s vers le prochain tic (0.0 � 1.0)
    /// N�cessite le ActivityVariant pour conna�tre le ActionCost (steps per tick)
    /// </summary>
    public float GetProgressToNextTick(ActivityVariant variant)
    {
        if (variant == null || variant.ActionCost <= 0) return 0f;
        return (float)AccumulatedSteps / variant.ActionCost;
    }

    /// <summary>
    /// Calcule combien de tics complets peuvent �tre effectu�s avec les pas donn�s
    /// </summary>
    public int CalculateCompleteTicks(ActivityVariant variant, int additionalSteps)
    {
        if (variant == null || variant.ActionCost <= 0) return 0;

        int totalSteps = AccumulatedSteps + additionalSteps;
        return totalSteps / variant.ActionCost;
    }

    /// <summary>
    /// Met � jour les pas accumul�s apr�s avoir effectu� des tics
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
    /// Ajoute des pas � l'accumulation
    /// </summary>
    public void AddSteps(int steps)
    {
        if (steps > 0)
        {
            AccumulatedSteps += steps;
        }
    }

    /// <summary>
    /// Calcule depuis combien de temps l'activit� est active (en millisecondes)
    /// </summary>
    public long GetElapsedTimeMs()
    {
        if (StartTimeMs <= 0) return 0;
        return DateTimeOffset.Now.ToUnixTimeMilliseconds() - StartTimeMs;
    }

    /// <summary>
    /// Valide que les donn�es de l'activit� sont coh�rentes
    /// </summary>
    public bool IsValid()
    {
        // V�rifications de base
        if (string.IsNullOrEmpty(ActivityId)) return false;
        if (string.IsNullOrEmpty(VariantId)) return false;
        if (AccumulatedSteps < 0) return false;
        if (StartSteps < 0) return false;

        return true;
    }

    /// <summary>
    /// Remet � z�ro l'activit� (pour arr�ter proprement)
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
    /// Affichage d�taill� pour le debug avec les informations du variant
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