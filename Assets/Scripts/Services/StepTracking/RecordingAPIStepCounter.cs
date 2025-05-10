// Filepath: Assets/Scripts/Services/StepTracking/RecordingAPIStepCounter.cs
using System.Collections;
using UnityEngine;

public class RecordingAPIStepCounter : MonoBehaviour
{
    private AndroidJavaClass stepPluginClass;
    private const string fullPluginClassName = "com.StepQuest.steps.StepPlugin";

    public long CurrentTodaysStepsFromAPI { get; private set; } = 0;


    private float stepUpdateInterval = 5.0f;
    private bool isPluginInitialized = false;
    private bool isLoadingData = true; // Gardons cela pour la logique d'initialisation du plugin

    public static RecordingAPIStepCounter Instance { get; private set; }

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
        // Le chargement des données se fera après que DataManager soit prêt.
    }

    void Start()
    {
        StartCoroutine(InitializeAfterDataManagerReady());
    }

    IEnumerator InitializeAfterDataManagerReady()
    {
        // Attendre que DataManager et CurrentPlayerData soient initialisés
        yield return null;

        while (DataManager.Instance == null || DataManager.Instance.CurrentPlayerData == null)
        {
            Debug.LogWarning("RecordingAPIStepCounter: Waiting for DataManager and CurrentPlayerData to be ready...");
            yield return new WaitForSeconds(0.5f);
        }

        LoadStepDataFromDataManager(); // Charger nos données de pas spécifiques

        isLoadingData = false;

        InitializePluginAndSubscribe();
        StartCoroutine(PeriodicStepCheckCoroutine());
    }

    void LoadStepDataFromDataManager()
    {

        if (DataManager.Instance != null && DataManager.Instance.CurrentPlayerData != null)
        {
            Logger.LogInfo($"RecordingAPIStepCounter: DataManager is ready. Initial TotalPlayerSteps: {DataManager.Instance.CurrentPlayerData.TotalPlayerSteps}, Initial LastKnownDaily: {DataManager.Instance.CurrentPlayerData.LastKnownDailyStepsForDeltaCalc}");
        }
        else
        {
            // Ce cas ne devrait pas arriver grâce à la boucle d'attente, mais par sécurité :
            Logger.LogError("RecordingAPIStepCounter: DataManager or CurrentPlayerData is unexpectedly null during LoadStepData. Defaults will effectively be used from a new PlayerData instance if DataManager failed to load.");

        }
    }

    void InitializePluginAndSubscribe()
    {
        if (isPluginInitialized) return;
        if (isLoadingData) // MODIFIÉ - Cette vérification est toujours pertinente
        {
            Debug.LogWarning("RecordingAPIStepCounter: Still waiting for initial data load (from DataManager), delaying plugin initialization.");
            return;
        }

        Logger.LogInfo($"RecordingAPIStepCounter: Attempting to initialize StepPlugin: {fullPluginClassName}");
        try
        {
            stepPluginClass = new AndroidJavaClass(fullPluginClassName);
            isPluginInitialized = true;
        }
        catch (AndroidJavaException e)
        {
            Logger.LogError($"RecordingAPIStepCounter: Failed to initialize AndroidJavaClass for {fullPluginClassName}. Exception: {e}");
            stepPluginClass = null;
            isPluginInitialized = false;
            return;
        }

        if (stepPluginClass != null)
        {
            Logger.LogInfo($"RecordingAPIStepCounter: {fullPluginClassName} class found successfully.");
            Logger.LogInfo("RecordingAPIStepCounter: Calling subscribeToSteps (will check/request permission)...");
            stepPluginClass.CallStatic("subscribeToSteps");
        }
        else
        {
            Logger.LogError($"RecordingAPIStepCounter: {fullPluginClassName} class NOT found! Plugin will not function.");
            isPluginInitialized = false;
        }
    }

    IEnumerator PeriodicStepCheckCoroutine()
    {
        while (!isPluginInitialized || isLoadingData)
        {
            if (isLoadingData)
                Debug.LogWarning("RecordingAPIStepCounter: Periodic check waiting for initial data load from DataManager...");
            else if (!isPluginInitialized)
                Debug.LogWarning("RecordingAPIStepCounter: Periodic check waiting for plugin initialization...");

            yield return new WaitForSeconds(1.0f);
            if (!isPluginInitialized && !isLoadingData)
            {
                InitializePluginAndSubscribe();
            }
        }

        Logger.LogInfo("RecordingAPIStepCounter: Starting periodic step checks.");
        yield return new WaitForSeconds(1.0f);

        while (true)
        {
            if (isPluginInitialized && stepPluginClass != null)
            {
                RequestStepReadFromPlugin();
                yield return new WaitForSeconds(0.7f);
                FetchStoredStepsFromPlugin();
                UpdateTotalPlayerStepsAndSave();
                // MODIFIÉ - Log pour utiliser les données de DataManager
                Logger.LogInfo($"[API Steps Today: {CurrentTodaysStepsFromAPI}] -- [Total Game Steps: {DataManager.Instance.CurrentPlayerData.TotalPlayerSteps}]");
            }
            else
            {
                Logger.LogWarning("RecordingAPIStepCounter: StepPlugin not initialized, skipping periodic check.");
                if (!isLoadingData) InitializePluginAndSubscribe();
            }
            yield return new WaitForSeconds(Mathf.Max(0.1f, stepUpdateInterval - 0.7f));
        }
    }

    void RequestStepReadFromPlugin()
    {
        if (stepPluginClass == null)
        {
            Logger.LogError("RecordingAPIStepCounter: StepPlugin class is null in RequestStepReadFromPlugin.");
            return;
        }

        bool hasPermission = stepPluginClass.CallStatic<bool>("hasActivityRecognitionPermission");
        if (hasPermission)
        {
            stepPluginClass.CallStatic("readTodaysStepData");
        }
        else
        {
            Logger.LogWarning("RecordingAPIStepCounter: Permission DENIED. Attempting to request permission via subscribe call.");
            stepPluginClass.CallStatic("subscribeToSteps");
        }
    }

    void FetchStoredStepsFromPlugin()
    {
        if (stepPluginClass == null)
        {
            Logger.LogError("RecordingAPIStepCounter: StepPlugin class is null in FetchStoredStepsFromPlugin.");
            return;
        }

        bool hasPermission = stepPluginClass.CallStatic<bool>("hasActivityRecognitionPermission");
        if (hasPermission)
        {
            long stepsFromPlugin = stepPluginClass.CallStatic<long>("getStoredTodaysSteps");
            CurrentTodaysStepsFromAPI = stepsFromPlugin;
        }
        else
        {
            Logger.LogWarning("RecordingAPIStepCounter: Permission not granted when trying to fetch stored steps.");
        }
    }

    void UpdateTotalPlayerStepsAndSave()
    {
        // MODIFIÉ - S'assurer que DataManager est prêt avant toute modification
        if (DataManager.Instance == null || DataManager.Instance.CurrentPlayerData == null)
        {
            Logger.LogError("RecordingAPIStepCounter: DataManager or CurrentPlayerData is null. Cannot update/save step data!");
            return;
        }
        // La vérification isLoadingData n'est plus nécessaire ici car elle bloque la coroutine PeriodicStepCheckCoroutine

        if (CurrentTodaysStepsFromAPI < 0)
        {
            Logger.LogWarning($"RecordingAPIStepCounter: Invalid CurrentTodaysStepsFromAPI: {CurrentTodaysStepsFromAPI}. Not updating total.");
            return;
        }

        long newStepsThisUpdateCycle;
        // MODIFIÉ - Utiliser DataManager.Instance.CurrentPlayerData.LastKnownDailyStepsForDeltaCalc
        long localLastKnownDaily = DataManager.Instance.CurrentPlayerData.LastKnownDailyStepsForDeltaCalc;

        if (CurrentTodaysStepsFromAPI < localLastKnownDaily)
        {
            newStepsThisUpdateCycle = CurrentTodaysStepsFromAPI;
            Logger.LogInfo($"RecordingAPIStepCounter: New day or step reset. New steps for today: {newStepsThisUpdateCycle}");
        }
        else
        {
            newStepsThisUpdateCycle = CurrentTodaysStepsFromAPI - localLastKnownDaily;
        }

        bool dataChanged = false;
        if (newStepsThisUpdateCycle > 0)
        {
            // MODIFIÉ - Mettre à jour DataManager.Instance.CurrentPlayerData.TotalPlayerSteps
            DataManager.Instance.CurrentPlayerData.TotalPlayerSteps += newStepsThisUpdateCycle;
            Logger.LogInfo($"RecordingAPIStepCounter: Added {newStepsThisUpdateCycle} to TotalPlayerSteps. New Total: {DataManager.Instance.CurrentPlayerData.TotalPlayerSteps}");
            dataChanged = true;
        }

        // MODIFIÉ - Mettre à jour DataManager.Instance.CurrentPlayerData.LastKnownDailyStepsForDeltaCalc
        if (localLastKnownDaily != CurrentTodaysStepsFromAPI)
        {
            DataManager.Instance.CurrentPlayerData.LastKnownDailyStepsForDeltaCalc = CurrentTodaysStepsFromAPI;
            dataChanged = true;
        }

        if (dataChanged)
        {
            // MODIFIÉ - DataManager s'occupe déjà de vérifier s'il est null dans sa propre méthode SaveGame.
            // On appelle directement la sauvegarde.
            Logger.LogInfo($"RecordingAPIStepCounter: Saving to DataManager - Total: {DataManager.Instance.CurrentPlayerData.TotalPlayerSteps}, LastDaily: {DataManager.Instance.CurrentPlayerData.LastKnownDailyStepsForDeltaCalc}");
            DataManager.Instance.SaveGame(); // AJOUTÉ - Appel à la sauvegarde via DataManager
        }
    }
    public long GetCurrentTodaysStepsFromAPI() // Reste pareil
    {
        return CurrentTodaysStepsFromAPI;
    }

    public long GetTotalPlayerSteps() // MODIFIÉ pour lire depuis DataManager
    {
        if (DataManager.Instance != null && DataManager.Instance.CurrentPlayerData != null)
        {
            return DataManager.Instance.CurrentPlayerData.TotalPlayerSteps;
        }
        Logger.LogWarning("RecordingAPIStepCounter: GetTotalPlayerSteps called but DataManager or CurrentPlayerData is not ready. Returning 0.");
        return 0;
    }

    public void InitializeService() { } // Gardé pour la compatibilité avec l'interface potentielle
    public int GetSteps(System.DateTime start, System.DateTime end) { return (int)CurrentTodaysStepsFromAPI; } // Gardé
}