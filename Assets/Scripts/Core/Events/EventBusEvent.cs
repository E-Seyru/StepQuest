// Purpose: Classe de base pour tous les evenements de l'EventBus
// Filepath: Assets/Scripts/Core/Events/EventBusEvent.cs

using System;

/// <summary>
/// Classe de base abstraite pour tous les evenements de l'EventBus.
/// Tous vos evenements doivent heriter de cette classe.
/// </summary>
public abstract class EventBusEvent
{
    /// <summary>
    /// Timestamp de quand l'evenement a ete cree
    /// Utile pour le debugging et les logs
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// ID unique de l'evenement pour le debugging
    /// </summary>
    public string EventId { get; }

    /// <summary>
    /// Nom lisible du type d'evenement
    /// </summary>
    public string EventType => GetType().Name;

    protected EventBusEvent()
    {
        Timestamp = DateTime.Now;
        EventId = Guid.NewGuid().ToString("N")[..8]; // 8 premiers caracteres pour plus de lisibilite
    }

    /// <summary>
    /// Representation string de l'evenement pour le debugging
    /// </summary>
    public override string ToString()
    {
        return $"[{EventType}#{EventId}] at {Timestamp:HH:mm:ss.fff}";
    }
}

/// <summary>
/// Interface optionnelle pour les evenements qui peuvent etre annules
/// Utile pour des evenements comme "BeforeLocationChange" où on veut pouvoir empecher l'action
/// </summary>
public interface ICancellableEvent
{
    bool IsCancelled { get; set; }
    string CancellationReason { get; set; }

    void Cancel(string reason = "");
}

/// <summary>
/// Classe de base pour les evenements annulables
/// </summary>
public abstract class CancellableEventBusEvent : EventBusEvent, ICancellableEvent
{
    public bool IsCancelled { get; set; } = false;
    public string CancellationReason { get; set; } = "";

    public void Cancel(string reason = "")
    {
        IsCancelled = true;
        CancellationReason = reason;
    }

    public override string ToString()
    {
        var baseString = base.ToString();
        if (IsCancelled)
        {
            return $"{baseString} [CANCELLED: {CancellationReason}]";
        }
        return baseString;
    }
}