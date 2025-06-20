// Purpose: Classe de base pour tous les événements de l'EventBus
// Filepath: Assets/Scripts/Core/Events/EventBusEvent.cs

using System;

/// <summary>
/// Classe de base abstraite pour tous les événements de l'EventBus.
/// Tous vos événements doivent hériter de cette classe.
/// </summary>
public abstract class EventBusEvent
{
    /// <summary>
    /// Timestamp de quand l'événement a été créé
    /// Utile pour le debugging et les logs
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// ID unique de l'événement pour le debugging
    /// </summary>
    public string EventId { get; }

    /// <summary>
    /// Nom lisible du type d'événement
    /// </summary>
    public string EventType => GetType().Name;

    protected EventBusEvent()
    {
        Timestamp = DateTime.Now;
        EventId = Guid.NewGuid().ToString("N")[..8]; // 8 premiers caractères pour plus de lisibilité
    }

    /// <summary>
    /// Représentation string de l'événement pour le debugging
    /// </summary>
    public override string ToString()
    {
        return $"[{EventType}#{EventId}] at {Timestamp:HH:mm:ss.fff}";
    }
}

/// <summary>
/// Interface optionnelle pour les événements qui peuvent être annulés
/// Utile pour des événements comme "BeforeLocationChange" où on veut pouvoir empêcher l'action
/// </summary>
public interface ICancellableEvent
{
    bool IsCancelled { get; set; }
    string CancellationReason { get; set; }

    void Cancel(string reason = "");
}

/// <summary>
/// Classe de base pour les événements annulables
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