// Purpose: Data structure representing an active activity session state (mining, gathering, crafting, etc.)
// Filepath: Assets/Scripts/Data/Models/ActivityData.cs
using System;

[Serializable]
public class ActivityData
{
    // === REFERENCES AUX DEFINITIONS ===
    public string ActivityId;          // Reference vers ActivityDefinition (ex: "mining")
    public string VariantId;           // Reference vers ActivityVariant (ex: "iron_ore_variant")

    // === ETAT DE LA SESSION - PAS ===
    public long StartSteps;            // Nombre de pas totaux quand l'activite a commence
    public int AccumulatedSteps;       // Pas accumules depuis le dernier tic
    public long LastProcessedTotalSteps; // Dernier TotalSteps qu'on a traite

    // === NOUVEAU: ETAT DE LA SESSION - TEMPS ===
    public bool IsTimeBased;           // Flag pour identifier le type d'activit�
    public long AccumulatedTimeMs;     // Temps accumul� (pour activit�s temporelles)
    public long RequiredTimeMs;        // Temps total requis (ex: 30000ms = 30s de craft)
    public long LastProcessedTimeMs;   // Dernier timestamp trait� (pour calculs offline)

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
        IsTimeBased = false;
        AccumulatedTimeMs = 0;
        RequiredTimeMs = 0;
        LastProcessedTimeMs = 0;
        StartTimeMs = 0;
        LocationId = string.Empty;
    }

    /// <summary>
    /// Constructeur pour creer une nouvelle session d'activite bas�e sur les pas
    /// </summary>
    public ActivityData(string activityId, string variantId, long startSteps, string locationId)
    {
        ActivityId = activityId;
        VariantId = variantId;
        StartSteps = startSteps;
        AccumulatedSteps = 0;
        LastProcessedTotalSteps = startSteps;
        IsTimeBased = false;
        AccumulatedTimeMs = 0;
        RequiredTimeMs = 0;
        LastProcessedTimeMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        StartTimeMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        LocationId = locationId;
    }

    /// <summary>
    /// NOUVEAU: Constructeur pour creer une nouvelle session d'activite bas�e sur le temps
    /// </summary>
    public ActivityData(string activityId, string variantId, long requiredTimeMs, string locationId, bool isTimeBased)
    {
        ActivityId = activityId;
        VariantId = variantId;
        StartSteps = 0;
        AccumulatedSteps = 0;
        LastProcessedTotalSteps = 0;
        IsTimeBased = isTimeBased; // Utiliser le param�tre
        AccumulatedTimeMs = 0;
        RequiredTimeMs = requiredTimeMs;
        LastProcessedTimeMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
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
               (IsTimeBased ? RequiredTimeMs > 0 : StartSteps >= 0);
    }

    /// <summary>
    /// Calcule le progr�s vers le prochain tic (0.0 � 1.0)
    /// Pour les activit�s pas: utilise le ActivityVariant pour conna�tre le ActionCost
    /// Pour les activit�s temps: utilise RequiredTimeMs
    /// </summary>
    public float GetProgressToNextTick(ActivityVariant variant)
    {
        if (IsTimeBased)
        {
            if (RequiredTimeMs <= 0) return 0f;
            return Math.Min(1f, (float)AccumulatedTimeMs / RequiredTimeMs);
        }
        else
        {
            if (variant == null || variant.ActionCost <= 0) return 0f;
            return (float)AccumulatedSteps / variant.ActionCost;
        }
    }

    /// <summary>
    /// MODIFIE: Calcule combien de tics complets peuvent �tre effectues
    /// Pour les pas: utilise les pas donn�s
    /// Pour le temps: v�rifie si le temps requis est atteint
    /// </summary>
    public int CalculateCompleteTicks(ActivityVariant variant, int additionalSteps)
    {
        if (IsTimeBased)
        {
            // Pour les activit�s temporelles, on ne peut compl�ter qu'1 seul "tick" (le craft complet)
            return (AccumulatedTimeMs >= RequiredTimeMs) ? 1 : 0;
        }
        else
        {
            if (variant == null || variant.ActionCost <= 0) return 0;
            int totalSteps = AccumulatedSteps + additionalSteps;
            return totalSteps / variant.ActionCost;
        }
    }

    /// <summary>
    /// MODIFIE: Met � jour apr�s avoir effectue des tics
    /// </summary>
    public void ProcessTicks(ActivityVariant variant, int ticksCompleted)
    {
        if (ticksCompleted <= 0) return;

        if (IsTimeBased)
        {
            // Pour les activit�s temporelles, compl�ter un tick = activit� termin�e
            if (ticksCompleted > 0)
            {
                AccumulatedTimeMs = RequiredTimeMs; // Marquer comme termin�
            }
        }
        else
        {
            if (variant != null && variant.ActionCost > 0)
            {
                int stepsUsed = ticksCompleted * variant.ActionCost;
                AccumulatedSteps = Math.Max(0, AccumulatedSteps - stepsUsed);
            }
        }
    }

    /// <summary>
    /// EXISTANT: Ajoute des pas � l'accumulation
    /// </summary>
    public void AddSteps(int steps)
    {
        if (steps > 0 && !IsTimeBased) // Seulement pour les activit�s bas�es sur les pas
        {
            AccumulatedSteps += steps;
            LastProcessedTotalSteps += steps;
        }
    }

    /// <summary>
    /// NOUVEAU: Ajoute du temps � l'accumulation
    /// </summary>
    public void AddTime(long timeMs)
    {
        if (timeMs > 0 && IsTimeBased)
        {
            AccumulatedTimeMs += timeMs;
            LastProcessedTimeMs = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }
    }

    /// <summary>
    /// NOUVEAU: V�rifie si l'activit� est termin�e
    /// </summary>
    public bool IsComplete()
    {
        if (IsTimeBased)
        {
            return AccumulatedTimeMs >= RequiredTimeMs;
        }
        else
        {
            // Pour les activit�s pas, on ne peut pas savoir sans le variant
            return false; // Sera g�r� par CalculateCompleteTicks
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
    /// NOUVEAU: Calcule combien de temps non-trait� on a (pour les activit�s temporelles)
    /// </summary>
    public long GetUnprocessedTimeMs()
    {
        if (!IsTimeBased) return 0;

        long currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        return Math.Max(0, currentTime - LastProcessedTimeMs);
    }

    /// <summary>
    /// MODIFIE: Valide que les donnees de l'activite sont coherentes
    /// </summary>
    public bool IsValid()
    {
        // Verifications de base
        if (string.IsNullOrEmpty(ActivityId)) return false;
        if (string.IsNullOrEmpty(VariantId)) return false;

        if (IsTimeBased)
        {
            if (RequiredTimeMs <= 0) return false;
            if (AccumulatedTimeMs < 0) return false;
        }
        else
        {
            if (AccumulatedSteps < 0) return false;
            if (StartSteps < 0) return false;
            if (LastProcessedTotalSteps < StartSteps) return false;
        }

        return true;
    }

    /// <summary>
    /// MODIFIE: Remet � zero l'activite (pour arr�ter proprement)
    /// </summary>
    public void Clear()
    {
        ActivityId = string.Empty;
        VariantId = string.Empty;
        StartSteps = 0;
        AccumulatedSteps = 0;
        LastProcessedTotalSteps = 0;
        IsTimeBased = false;
        AccumulatedTimeMs = 0;
        RequiredTimeMs = 0;
        LastProcessedTimeMs = 0;
        StartTimeMs = 0;
        LocationId = string.Empty;
    }

    /// <summary>
    /// MODIFIE: Affichage pour le debug - version simple
    /// </summary>
    public override string ToString()
    {
        if (!IsActive())
            return "[No Active Activity]";

        if (IsTimeBased)
        {
            return $"[Time Activity: {ActivityId}/{VariantId} - Time: {AccumulatedTimeMs}/{RequiredTimeMs}ms - Location: {LocationId}]";
        }
        else
        {
            return $"[Step Activity: {ActivityId}/{VariantId} - Steps: {AccumulatedSteps} accumulated - Location: {LocationId}]";
        }
    }

    /// <summary>
    /// MODIFIE: Affichage detaille pour le debug avec les informations du variant
    /// </summary>
    public string ToString(ActivityVariant variant)
    {
        if (!IsActive())
            return "[No Active Activity]";

        if (variant == null)
            return ToString(); // Fallback

        if (IsTimeBased)
        {
            float progressPercent = GetProgressToNextTick(variant) * 100f;
            return $"[Time Activity: {variant.GetDisplayName()} - Progress: {progressPercent:F1}% ({AccumulatedTimeMs}/{RequiredTimeMs}ms)]";
        }
        else
        {
            return $"[Step Activity: {variant.GetDisplayName()} - Progress: {AccumulatedSteps}/{variant.ActionCost} steps - Resource: {variant.PrimaryResource?.GetDisplayName() ?? "Unknown"}]";
        }
    }
}