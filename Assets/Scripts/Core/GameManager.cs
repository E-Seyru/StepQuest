// Filepath: Assets/Scripts/Core/GameManager.cs
using ActivityEvents;
using GameEvents;
using MapEvents;
using UnityEngine;

public enum GameState
{
    Loading,        // Chargement initial
    Idle,          // Aucune activit� particuli�re
    Traveling,     // En voyage entre locations
    DoingActivity, // Activit� en cours (mining, gathering, etc.)
    InCombat,      // En combat (pour le futur)
    Paused         // Jeu en pause
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Loading;

    // Propri�t� publique pour lire l'�tat
    public GameState CurrentState
    {
        get { return currentState; }
        private set { currentState = value; }
    }

    // R�f�rences aux autres managers
    private DataManager dataManager;
    private MapManager mapManager;
    private ActivityManager activityManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Logger.LogInfo("GameManager: Initializing...", Logger.LogCategory.General);
    }

    void Start()
    {
        // Attendre que les autres managers soient pr�ts
        StartCoroutine(InitializeGameState());
    }

    private System.Collections.IEnumerator InitializeGameState()
    {
        Logger.LogInfo("GameManager: Waiting for other managers...", Logger.LogCategory.General);

        // Attendre que les managers soient disponibles
        while (DataManager.Instance == null ||
               MapManager.Instance == null ||
               ActivityManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // R�cup�rer les r�f�rences
        dataManager = DataManager.Instance;
        mapManager = MapManager.Instance;
        activityManager = ActivityManager.Instance;

        // S'abonner aux �v�nements des autres managers
        SubscribeToManagerEvents();

        // D�terminer l'�tat initial du jeu
        DetermineInitialGameState();

        Logger.LogInfo($"GameManager: Initialized. Current state: {currentState}", Logger.LogCategory.General);
    }

    private void SubscribeToManagerEvents()
    {
        // =====================================
        // EVENTBUS - �v�nements de voyage
        // =====================================
        EventBus.Subscribe<TravelStartedEvent>(OnTravelStarted);
        EventBus.Subscribe<TravelCompletedEvent>(OnTravelCompleted);

        // =====================================
        // EVENTBUS - �v�nements d'activit�  
        // =====================================
        EventBus.Subscribe<ActivityStartedEvent>(OnActivityStarted);
        EventBus.Subscribe<ActivityStoppedEvent>(OnActivityStopped);

        Logger.LogInfo("GameManager: Subscribed to EventBus events", Logger.LogCategory.General);
    }

    private void DetermineInitialGameState()
    {
        if (dataManager?.PlayerData == null)
        {
            ChangeState(GameState.Loading);
            return;
        }

        // V�rifier l'�tat actuel du joueur
        if (dataManager.PlayerData.IsCurrentlyTraveling())
        {
            ChangeState(GameState.Traveling);
        }
        else if (activityManager.HasActiveActivity())
        {
            ChangeState(GameState.DoingActivity);
        }
        else
        {
            ChangeState(GameState.Idle);
        }
    }

    // === M�THODES POUR CHANGER D'�TAT ===

    private void ChangeState(GameState newState)
    {
        if (newState == currentState) return; // Pas de changement

        GameState oldState = currentState;
        currentState = newState;

        Logger.LogInfo($"GameManager: State changed from {oldState} to {newState}", Logger.LogCategory.General);

        // =====================================
        // EVENTBUS - Publier le changement d'�tat
        // =====================================
        EventBus.Publish(new GameStateChangedEvent(oldState, newState));
    }

    // === GESTIONNAIRES D'�V�NEMENTS - ADAPT�S POUR EVENTBUS ===

    private void OnTravelStarted(TravelStartedEvent eventData)
    {
        Logger.LogInfo($"GameManager: Travel started to {eventData.DestinationLocationId}", Logger.LogCategory.General);
        ChangeState(GameState.Traveling);
    }

    private void OnTravelCompleted(TravelCompletedEvent eventData)
    {
        Logger.LogInfo($"GameManager: Travel completed at {eventData.NewLocation?.DisplayName ?? eventData.DestinationLocationId}", Logger.LogCategory.General);

        // Apr�s un voyage, v�rifier s'il y a une activit� en cours
        if (activityManager.HasActiveActivity())
        {
            ChangeState(GameState.DoingActivity);
        }
        else
        {
            ChangeState(GameState.Idle);
        }
    }

    private void OnActivityStarted(ActivityStartedEvent eventData)
    {
        Logger.LogInfo($"GameManager: Activity started: {eventData.Activity?.ActivityId}/{eventData.Variant?.VariantName}", Logger.LogCategory.General);
        ChangeState(GameState.DoingActivity);
    }

    private void OnActivityStopped(ActivityStoppedEvent eventData)
    {
        Logger.LogInfo($"GameManager: Activity stopped: {eventData.Activity?.ActivityId}/{eventData.Variant?.VariantName} (Completed: {eventData.WasCompleted})", Logger.LogCategory.General);

        // Apr�s arr�t d'activit�, v�rifier s'il y a un voyage en cours
        if (dataManager.PlayerData.IsCurrentlyTraveling())
        {
            ChangeState(GameState.Traveling);
        }
        else
        {
            ChangeState(GameState.Idle);
        }
    }

    // === M�THODES PUBLIQUES POUR FORCER UN CHANGEMENT D'�TAT ===

    public void SetGamePaused(bool isPaused)
    {
        if (isPaused)
        {
            ChangeState(GameState.Paused);
        }
        else
        {
            // Revenir � l'�tat appropri�
            DetermineInitialGameState();
        }
    }

    // === GESTION DES �V�NEMENTS SYST�ME ===

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Logger.LogInfo("GameManager: Application paused", Logger.LogCategory.General);
            // Ne pas changer l'�tat - juste logger
        }
        else
        {
            Logger.LogInfo("GameManager: Application resumed", Logger.LogCategory.General);
            // V�rifier si l'�tat a chang� pendant la pause
            DetermineInitialGameState();
        }
    }

    void OnApplicationQuit()
    {
        Logger.LogInfo("GameManager: Application quitting", Logger.LogCategory.General);
    }

    // === NETTOYAGE ===

    void OnDestroy()
    {
        // =====================================
        // EVENTBUS - Se d�sabonner des �v�nements
        // =====================================
        EventBus.Unsubscribe<TravelStartedEvent>(OnTravelStarted);
        EventBus.Unsubscribe<TravelCompletedEvent>(OnTravelCompleted);
        EventBus.Unsubscribe<ActivityStartedEvent>(OnActivityStarted);
        EventBus.Unsubscribe<ActivityStoppedEvent>(OnActivityStopped);
    }

    // === M�THODES DE DEBUG ===

    public string GetGameStateInfo()
    {
        string info = $"Game State: {currentState}\n";

        if (dataManager?.PlayerData != null)
        {
            info += $"Traveling: {dataManager.PlayerData.IsCurrentlyTraveling()}\n";
            info += $"Has Activity: {activityManager?.HasActiveActivity()}\n";
            info += $"Current Location: {dataManager.PlayerData.CurrentLocationId}";
        }

        return info;
    }
}