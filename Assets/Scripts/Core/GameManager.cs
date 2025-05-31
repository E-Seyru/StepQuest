// Filepath: Assets/Scripts/Core/GameManager.cs
using System;
using UnityEngine;

public enum GameState
{
    Loading,        // Chargement initial
    Idle,          // Aucune activité particulière
    Traveling,     // En voyage entre locations
    DoingActivity, // Activité en cours (mining, gathering, etc.)
    InCombat,      // En combat (pour le futur)
    Paused         // Jeu en pause
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private GameState currentState = GameState.Loading;

    // Event que d'autres scripts peuvent écouter
    public event Action<GameState, GameState> OnGameStateChanged; // oldState, newState

    // Propriété publique pour lire l'état
    public GameState CurrentState
    {
        get { return currentState; }
        private set { currentState = value; }
    }

    // Références aux autres managers
    private DataManager dataManager;
    private MapManager mapManager;
    private ActivityManager activityManager;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        // Attendre que les autres managers soient prêts
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

        // Récupérer les références
        dataManager = DataManager.Instance;
        mapManager = MapManager.Instance;
        activityManager = ActivityManager.Instance;

        // S'abonner aux événements des autres managers
        SubscribeToManagerEvents();

        // Déterminer l'état initial du jeu
        DetermineInitialGameState();

        Logger.LogInfo($"GameManager: Initialized. Current state: {currentState}", Logger.LogCategory.General);
    }

    private void SubscribeToManagerEvents()
    {
        // Événements de voyage
        if (mapManager != null)
        {
            mapManager.OnTravelStarted += OnTravelStarted;
            mapManager.OnTravelCompleted += OnTravelCompleted;
        }

        // Événements d'activité
        if (activityManager != null)
        {
            activityManager.OnActivityStarted += OnActivityStarted;
            activityManager.OnActivityStopped += OnActivityStopped;
        }

        Logger.LogInfo("GameManager: Subscribed to manager events", Logger.LogCategory.General);
    }

    private void DetermineInitialGameState()
    {
        if (dataManager?.PlayerData == null)
        {
            ChangeState(GameState.Loading);
            return;
        }

        // Vérifier l'état actuel du joueur
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

    // === MÉTHODES POUR CHANGER D'ÉTAT ===

    private void ChangeState(GameState newState)
    {
        if (newState == currentState) return; // Pas de changement

        GameState oldState = currentState;
        currentState = newState;

        Logger.LogInfo($"GameManager: State changed from {oldState} to {newState}", Logger.LogCategory.General);

        // Déclencher l'événement pour informer les autres scripts
        OnGameStateChanged?.Invoke(oldState, newState);
    }

    // === GESTIONNAIRES D'ÉVÉNEMENTS ===

    private void OnTravelStarted(string destinationId)
    {
        Logger.LogInfo($"GameManager: Travel started to {destinationId}", Logger.LogCategory.General);
        ChangeState(GameState.Traveling);
    }

    private void OnTravelCompleted(string arrivedLocationId)
    {
        Logger.LogInfo($"GameManager: Travel completed at {arrivedLocationId}", Logger.LogCategory.General);

        // Après un voyage, vérifier s'il y a une activité en cours
        if (activityManager.HasActiveActivity())
        {
            ChangeState(GameState.DoingActivity);
        }
        else
        {
            ChangeState(GameState.Idle);
        }
    }

    private void OnActivityStarted(ActivityData activity, ActivityVariant variant)
    {
        Logger.LogInfo($"GameManager: Activity started: {variant.GetDisplayName()}", Logger.LogCategory.General);
        ChangeState(GameState.DoingActivity);
    }

    private void OnActivityStopped(ActivityData activity, ActivityVariant variant)
    {
        Logger.LogInfo($"GameManager: Activity stopped: {variant.GetDisplayName()}", Logger.LogCategory.General);

        // Après arrêt d'activité, vérifier s'il y a un voyage en cours
        if (dataManager.PlayerData.IsCurrentlyTraveling())
        {
            ChangeState(GameState.Traveling);
        }
        else
        {
            ChangeState(GameState.Idle);
        }
    }

    // === MÉTHODES PUBLIQUES POUR FORCER UN CHANGEMENT D'ÉTAT ===

    public void SetGamePaused(bool isPaused)
    {
        if (isPaused)
        {
            ChangeState(GameState.Paused);
        }
        else
        {
            // Revenir à l'état approprié
            DetermineInitialGameState();
        }
    }

    // === GESTION DES ÉVÉNEMENTS SYSTÈME ===

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Logger.LogInfo("GameManager: Application paused", Logger.LogCategory.General);
            // Ne pas changer l'état - juste logger
        }
        else
        {
            Logger.LogInfo("GameManager: Application resumed", Logger.LogCategory.General);
            // Vérifier si l'état a changé pendant la pause
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
        // Se désabonner des événements pour éviter les erreurs
        if (mapManager != null)
        {
            mapManager.OnTravelStarted -= OnTravelStarted;
            mapManager.OnTravelCompleted -= OnTravelCompleted;
        }

        if (activityManager != null)
        {
            activityManager.OnActivityStarted -= OnActivityStarted;
            activityManager.OnActivityStopped -= OnActivityStopped;
        }
    }

    // === MÉTHODES DE DEBUG ===

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