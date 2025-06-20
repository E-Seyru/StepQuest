// Purpose: Classe de base pour tous les �v�nements de l'EventBus
// Filepath: Assets/Scripts/Core/Events/EventBusEvent.cs

using System;

/// <summary>
/// Classe de base abstraite pour tous les �v�nements de l'EventBus.
/// Tous vos �v�nements doivent h�riter de cette classe.
/// </summary>
public abstract class EventBusEvent
{
    /// <summary>
    /// Timestamp de quand l'�v�nement a �t� cr��
    /// Utile pour le debugging et les logs
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// ID unique de l'�v�nement pour le debugging
    /// </summary>
    public string EventId { get; }

    /// <summary>
    /// Nom lisible du type d'�v�nement
    /// </summary>
    public string EventType => GetType().Name;

    protected EventBusEvent()
    {
        Timestamp = DateTime.Now;
        EventId = Guid.NewGuid().ToString("N")[..8]; // 8 premiers caract�res pour plus de lisibilit�
    }

    /// <summary>
    /// Repr�sentation string de l'�v�nement pour le debugging
    /// </summary>
    public override string ToString()
    {
        return $"[{EventType}#{EventId}] at {Timestamp:HH:mm:ss.fff}";
    }
}

/// <summary>
/// Interface optionnelle pour les �v�nements qui peuvent �tre annul�s
/// Utile pour des �v�nements comme "BeforeLocationChange" o� on veut pouvoir emp�cher l'action
/// </summary>
public interface ICancellableEvent
{
    bool IsCancelled { get; set; }
    string CancellationReason { get; set; }

    void Cancel(string reason = "");
}

/// <summary>
/// Classe de base pour les �v�nements annulables
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