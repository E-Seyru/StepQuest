// Purpose: EventBus principal - Système de communication decouple pour Unity
// Filepath: Assets/Scripts/Core/Events/EventBus.cs

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EventBus global pour la communication decouplee entre les systèmes du jeu.
/// Thread-safe et optimise pour Unity.
/// </summary>
public static class EventBus
{
    #region Configuration et État

    /// <summary>
    /// Active/desactive le logging detaille des evenements
    /// Utile pour debugger, mais peut être verbeux
    /// </summary>
    public static bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Active/desactive le logging des erreurs uniquement
    /// </summary>
    public static bool EnableErrorLogging { get; set; } = true;

    /// <summary>
    /// Statistiques pour monitoring (optionnel)
    /// </summary>
    public static int TotalEventsPublished { get; private set; } = 0;
    public static int ActiveSubscriptions => totalSubscriptions;

    #endregion

    #region Stockage interne

    // Dictionnaire principal : Type d'evenement -> Liste des callbacks
    private static readonly Dictionary<Type, List<Delegate>> eventSubscriptions = new();

    // Lock pour thread-safety
    private static readonly object lockObject = new();

    // SUPPRIMÉ: tempCallbackList - etait inutilise et dangereux pour la concurrence
    // On utilise des listes locales dans chaque methode a la place

    // Compteur pour les stats
    private static int totalSubscriptions = 0;

    #endregion

    #region API Publique - Subscribe

    /// <summary>
    /// S'abonne a un type d'evenement specifique
    /// </summary>
    /// <typeparam name="T">Type de l'evenement</typeparam>
    /// <param name="callback">Methode a appeler quand l'evenement est publie</param>
    public static void Subscribe<T>(Action<T> callback) where T : EventBusEvent
    {
        if (callback == null)
        {
            LogError("Cannot subscribe with null callback");
            return;
        }

        var eventType = typeof(T);

        lock (lockObject)
        {
            // Creer la liste si elle n'existe pas
            if (!eventSubscriptions.ContainsKey(eventType))
            {
                eventSubscriptions[eventType] = new List<Delegate>();
            }

            // Verifier qu'on n'est pas deja abonne (evite les doublons)
            var callbacks = eventSubscriptions[eventType];
            if (callbacks.Contains(callback))
            {
                LogWarning($"Already subscribed to {eventType.Name} with the same callback");
                return;
            }

            // Ajouter le callback
            callbacks.Add(callback);
            totalSubscriptions++;

            LogInfo($"Subscribed to {eventType.Name} (Total: {callbacks.Count} subscribers)");
        }
    }

    #endregion

    #region API Publique - Unsubscribe

    /// <summary>
    /// Se desabonne d'un type d'evenement specifique
    /// </summary>
    /// <typeparam name="T">Type de l'evenement</typeparam>
    /// <param name="callback">Methode a retirer</param>
    public static void Unsubscribe<T>(Action<T> callback) where T : EventBusEvent
    {
        if (callback == null)
        {
            LogError("Cannot unsubscribe with null callback");
            return;
        }

        var eventType = typeof(T);

        lock (lockObject)
        {
            if (!eventSubscriptions.ContainsKey(eventType))
            {
                LogWarning($"No subscriptions found for {eventType.Name}");
                return;
            }

            var callbacks = eventSubscriptions[eventType];
            if (callbacks.Remove(callback))
            {
                totalSubscriptions--;
                LogInfo($"Unsubscribed from {eventType.Name} (Remaining: {callbacks.Count} subscribers)");

                // Nettoyer la liste si elle est vide
                if (callbacks.Count == 0)
                {
                    eventSubscriptions.Remove(eventType);
                    LogInfo($"Removed empty subscription list for {eventType.Name}");
                }
            }
            else
            {
                LogWarning($"Callback not found for {eventType.Name}");
            }
        }
    }

    /// <summary>
    /// Se desabonne de TOUS les evenements (utile dans OnDestroy)
    /// </summary>
    /// <param name="subscriber">L'objet qui veut se desabonner de tout</param>
    public static void UnsubscribeAll(object subscriber)
    {
        if (subscriber == null) return;

        lock (lockObject)
        {
            var removedCount = 0;
            var typesToRemove = new List<Type>();

            foreach (var kvp in eventSubscriptions)
            {
                var callbacks = kvp.Value;
                for (int i = callbacks.Count - 1; i >= 0; i--)
                {
                    if (callbacks[i].Target == subscriber)
                    {
                        callbacks.RemoveAt(i);
                        totalSubscriptions--;
                        removedCount++;
                    }
                }

                if (callbacks.Count == 0)
                {
                    typesToRemove.Add(kvp.Key);
                }
            }

            // Nettoyer les listes vides
            foreach (var type in typesToRemove)
            {
                eventSubscriptions.Remove(type);
            }

            if (removedCount > 0)
            {
                LogInfo($"Unsubscribed {subscriber.GetType().Name} from {removedCount} events");
            }
        }
    }

    #endregion

    #region API Publique - Publish

    /// <summary>
    /// Publie un evenement - tous les abonnes seront notifies
    /// </summary>
    /// <typeparam name="T">Type de l'evenement</typeparam>
    /// <param name="eventData">L'evenement a publier</param>
    public static void Publish<T>(T eventData) where T : EventBusEvent
    {
        if (eventData == null)
        {
            LogError("Cannot publish null event");
            return;
        }

        var eventType = typeof(T);
        TotalEventsPublished++;

        LogInfo($"Publishing {eventData}");

        // THREAD-SAFE: Creer une liste locale pour chaque appel
        // Chaque thread aura sa propre copie, evitant les conflits
        var callbacksToExecute = new List<Delegate>();

        lock (lockObject)
        {
            if (eventSubscriptions.ContainsKey(eventType))
            {
                // SAFE COPY: Creer une nouvelle liste pour eviter les modifications concurrentes
                var callbacks = eventSubscriptions[eventType];
                for (int i = 0; i < callbacks.Count; i++)
                {
                    callbacksToExecute.Add(callbacks[i]);
                }
            }
        }

        // Appeler tous les callbacks HORS du lock pour eviter les deadlocks
        var successCount = 0;
        var errorCount = 0;

        foreach (var callback in callbacksToExecute)
        {
            try
            {
                if (callback is Action<T> typedCallback)
                {
                    typedCallback.Invoke(eventData);
                    successCount++;

                    // Si l'evenement est annulable et a ete annule, on s'arrête
                    if (eventData is ICancellableEvent cancellable && cancellable.IsCancelled)
                    {
                        LogInfo($"Event {eventData} was cancelled by a subscriber");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                errorCount++;
                LogError($"Error in event callback for {eventType.Name}: {ex.Message}");
                // Continue avec les autres callbacks même si un echoue
            }
        }

        LogInfo($"Published {eventData} to {successCount} subscribers ({errorCount} errors)");
    }

    #endregion

    #region Utilitaires et Debug

    /// <summary>
    /// Efface tous les abonnements (utile pour les tests ou reset)
    /// </summary>
    public static void Clear()
    {
        lock (lockObject)
        {
            var totalCleared = totalSubscriptions;
            eventSubscriptions.Clear();
            totalSubscriptions = 0;
            LogInfo($"Cleared all subscriptions ({totalCleared} removed)");
        }
    }

    /// <summary>
    /// Obtient des informations de debug sur l'etat actuel
    /// </summary>
    public static string GetDebugInfo()
    {
        lock (lockObject)
        {
            var info = $"EventBus Status:\n";
            info += $"- Total Events Published: {TotalEventsPublished}\n";
            info += $"- Active Subscriptions: {totalSubscriptions}\n";
            info += $"- Event Types: {eventSubscriptions.Count}\n\n";

            foreach (var kvp in eventSubscriptions)
            {
                info += $"- {kvp.Key.Name}: {kvp.Value.Count} subscribers\n";
            }

            return info;
        }
    }

    #endregion

    #region Logging interne

    private static void LogInfo(string message)
    {
        if (EnableDetailedLogging)
        {
            Logger.LogInfo($"[EventBus] {message}", Logger.LogCategory.General);
        }
    }

    private static void LogWarning(string message)
    {
        if (EnableDetailedLogging || EnableErrorLogging)
        {
            Logger.LogWarning($"[EventBus] {message}", Logger.LogCategory.General);
        }
    }

    private static void LogError(string message)
    {
        if (EnableErrorLogging)
        {
            Logger.LogError($"[EventBus] {message}", Logger.LogCategory.General);
        }
    }

    #endregion
}