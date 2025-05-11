// Filepath: Assets/Scripts/Services/StepTracking/RecordingAPIStepCounter.cs
using System.Collections;
using UnityEngine;

public class RecordingAPIStepCounter : MonoBehaviour
{
    private AndroidJavaClass stepPluginClass;
    private const string fullPluginClassName = "com.StepQuest.steps.StepPlugin";

    public long CurrentTodaysStepsFromAPI { get; private set; } = 0;

    private float stepUpdateInterval = 5.0f; // Intervalle pour les mises à jour périodiques
    private bool isPluginClassInitialized = false; // Pour la classe Java
    private bool isStepTrackingLogicActive = false; // Si la logique de comptage principale est active
    private bool isLoadingInitialData = true; // Pour attendre DataManager

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
    }

    void Start()
    {
        StartCoroutine(InitializeStepTrackingSequence());
    }

    IEnumerator InitializeStepTrackingSequence()
    {
        // 1. Attendre DataManager
        yield return null; // Assurer qu'on est pas dans le même frame que Awake de DataManager
        while (DataManager.Instance == null || DataManager.Instance.CurrentPlayerData == null)
        {
            Debug.LogWarning("RecordingAPIStepCounter: Waiting for DataManager to be ready...");
            yield return new WaitForSeconds(0.5f);
        }
        isLoadingInitialData = false; // DataManager est prêt, les données initiales (Total, LastKnown) sont chargées
        Logger.LogInfo("RecordingAPIStepCounter: DataManager ready.");
        UpdateInitialUIDisplay(); // Mettre à jour l'UI avec les données chargées par DataManager

        // 2. Initialiser la classe du plugin Java
        InitializePluginClass();
        if (!isPluginClassInitialized)
        {
            Logger.LogError("RecordingAPIStepCounter: Plugin class could not be initialized. Step tracking will not function.");
            UpdatePermissionDeniedUI();
            yield break; // Arrêter la séquence
        }

        // 3. Vérifier et demander la permission
        bool permissionGranted = stepPluginClass.CallStatic<bool>("hasActivityRecognitionPermission");
        if (!permissionGranted)
        {
            Logger.LogInfo("RecordingAPIStepCounter: Permission not yet granted. Requesting...");
            stepPluginClass.CallStatic("requestActivityRecognitionPermission");

            // Attendre activement la réponse à la demande de permission
            float timeWaited = 0f;
            float maxWaitTime = 30f; // Attendre max 30s
            while (!stepPluginClass.CallStatic<bool>("hasActivityRecognitionPermission") && timeWaited < maxWaitTime)
            {
                Logger.LogInfo("RecordingAPIStepCounter: Waiting for permission grant...");
                yield return new WaitForSeconds(1.0f);
                timeWaited += 1.0f;
            }
            permissionGranted = stepPluginClass.CallStatic<bool>("hasActivityRecognitionPermission");
        }

        if (permissionGranted)
        {
            Logger.LogInfo("RecordingAPIStepCounter: Permission is GRANTED.");
            // 4. S'abonner à l'API Recording et faire une première lecture
            stepPluginClass.CallStatic("subscribeToSteps"); // Java va maintenant s'abonner car permission OK
            yield return new WaitForSeconds(0.5f); // Petit délai pour que l'abonnement prenne effet

            Logger.LogInfo("RecordingAPIStepCounter: Requesting initial read from API.");
            stepPluginClass.CallStatic("readTodaysStepData");
            yield return new WaitForSeconds(1.5f); // Délai pour que la lecture API se termine et que la variable Java soit mise à jour

            FetchStoredStepsFromPlugin(); // Met à jour CurrentTodaysStepsFromAPI
            UpdateTotalPlayerStepsAndSave(); // Met à jour DataManager et sauvegarde
            UpdateLiveUIDisplay(); // Met à jour l'UI avec les nouvelles données API

            // 5. Démarrer la coroutine de mise à jour périodique
            isStepTrackingLogicActive = true;
            StartCoroutine(PeriodicStepCheckCoroutine());
        }
        else
        {
            Logger.LogWarning("RecordingAPIStepCounter: Permission DENIED or timed out. API Step tracking will not function.");
            UpdatePermissionDeniedUI();
            // Afficher un message à l'utilisateur pour qu'il active la permission manuellement
        }
    }

    void InitializePluginClass()
    {
        if (isPluginClassInitialized) return;
        Logger.LogInfo($"RecordingAPIStepCounter: Attempting to initialize StepPlugin class: {fullPluginClassName}");
        try
        {
            stepPluginClass = new AndroidJavaClass(fullPluginClassName);
            isPluginClassInitialized = true;
            Logger.LogInfo($"RecordingAPIStepCounter: {fullPluginClassName} class found successfully.");
        }
        catch (AndroidJavaException e)
        {
            Logger.LogError($"RecordingAPIStepCounter: Failed to initialize AndroidJavaClass for {fullPluginClassName}. Exception: {e}");
            stepPluginClass = null;
            isPluginClassInitialized = false;
        }
    }

    void LoadStepDataFromDataManager() // Appelée indirectement via DataManager.Awake()
    {
        // Cette fonction est moins pertinente maintenant car DataManager charge ses propres données.
        // L'important est que DataManager.Instance.CurrentPlayerData soit prêt.
        if (DataManager.Instance != null && DataManager.Instance.CurrentPlayerData != null)
        {
            Logger.LogInfo($"RecordingAPIStepCounter: Data access ready. Initial TotalPlayerSteps: {DataManager.Instance.CurrentPlayerData.TotalPlayerSteps}, Initial LastKnownDaily: {DataManager.Instance.CurrentPlayerData.LastKnownDailyStepsForDeltaCalc}");
        }
    }

    IEnumerator PeriodicStepCheckCoroutine()
    {
        Logger.LogInfo("RecordingAPIStepCounter: Starting periodic step checks.");
        // Pas besoin d'attente initiale ici, la première lecture a déjà été faite.

        while (isStepTrackingLogicActive) // Continue tant que la logique principale est active
        {
            if (isPluginClassInitialized && stepPluginClass != null && stepPluginClass.CallStatic<bool>("hasActivityRecognitionPermission"))
            {
                stepPluginClass.CallStatic("readTodaysStepData");
                yield return new WaitForSeconds(0.7f); // Délai pour la lecture API
                FetchStoredStepsFromPlugin();
                UpdateTotalPlayerStepsAndSave();
                UpdateLiveUIDisplay(); // Mettre à jour l'UI à chaque cycle
            }
            else
            {
                Logger.LogWarning("RecordingAPIStepCounter: Plugin not ready or permission lost, skipping periodic check.");
                isStepTrackingLogicActive = false; // Arrêter la coroutine si la permission est perdue
                UpdatePermissionDeniedUI();
                // Il faudrait un mécanisme pour retenter la demande de permission ou informer l'utilisateur
            }
            yield return new WaitForSeconds(Mathf.Max(0.1f, stepUpdateInterval - 0.7f));
        }
        Logger.LogInfo("RecordingAPIStepCounter: Periodic step checks stopped.");
    }

    void FetchStoredStepsFromPlugin()
    {
        if (!isPluginClassInitialized || stepPluginClass == null) return;

        CurrentTodaysStepsFromAPI = stepPluginClass.CallStatic<long>("getStoredTodaysSteps");
        // Logger.LogInfo($"Fetched API Steps: {CurrentTodaysStepsFromAPI}"); // Peut être verbeux
    }

    void UpdateTotalPlayerStepsAndSave()
    {
        if (isLoadingInitialData || DataManager.Instance == null || DataManager.Instance.CurrentPlayerData == null)
        {
            Logger.LogWarning("RecordingAPIStepCounter: DataManager not ready, cannot update/save steps.");
            return;
        }

        if (CurrentTodaysStepsFromAPI < 0)
        {
            Logger.LogWarning($"RecordingAPIStepCounter: Invalid CurrentTodaysStepsFromAPI: {CurrentTodaysStepsFromAPI}. Not updating total.");
            return;
        }

        long localLastKnownDaily = DataManager.Instance.CurrentPlayerData.LastKnownDailyStepsForDeltaCalc;
        long newStepsThisUpdateCycle;

        if (CurrentTodaysStepsFromAPI < localLastKnownDaily) // Reset de jour ou de l'API
        {
            newStepsThisUpdateCycle = CurrentTodaysStepsFromAPI;
            Logger.LogInfo($"Step Reset/New Day (API): New API steps for today: {newStepsThisUpdateCycle}");
        }
        else
        {
            newStepsThisUpdateCycle = CurrentTodaysStepsFromAPI - localLastKnownDaily;
        }

        bool dataChanged = false;
        if (newStepsThisUpdateCycle > 0)
        {
            DataManager.Instance.CurrentPlayerData.TotalPlayerSteps += newStepsThisUpdateCycle;
            Logger.LogInfo($"Added {newStepsThisUpdateCycle} steps from API. New Total: {DataManager.Instance.CurrentPlayerData.TotalPlayerSteps}");
            dataChanged = true;
        }

        if (localLastKnownDaily != CurrentTodaysStepsFromAPI)
        {
            DataManager.Instance.CurrentPlayerData.LastKnownDailyStepsForDeltaCalc = CurrentTodaysStepsFromAPI;
            dataChanged = true; // Marquer que les données ont changé même si TotalPlayerSteps n'a pas bougé
        }

        if (dataChanged)
        {
            // Logger.LogInfo($"Saving to DataManager - Total: {DataManager.Instance.CurrentPlayerData.TotalPlayerSteps}, LastDaily: {DataManager.Instance.CurrentPlayerData.LastKnownDailyStepsForDeltaCalc}");
            DataManager.Instance.SaveGame();
        }
    }

    // --- Méthodes de mise à jour UI ---
    void UpdateInitialUIDisplay()
    {
        if (UIManager.Instance != null && DataManager.Instance != null && DataManager.Instance.CurrentPlayerData != null)
        {
            UIManager.Instance.UpdateTodaysStepsDisplay(0); // Au tout début, l'API du jour est 0
            UIManager.Instance.UpdateTotalPlayerStepsDisplay(DataManager.Instance.CurrentPlayerData.TotalPlayerSteps);
        }
    }

    void UpdateLiveUIDisplay()
    {
        if (UIManager.Instance != null && DataManager.Instance != null && DataManager.Instance.CurrentPlayerData != null)
        {
            UIManager.Instance.UpdateTodaysStepsDisplay(CurrentTodaysStepsFromAPI);
            UIManager.Instance.UpdateTotalPlayerStepsDisplay(DataManager.Instance.CurrentPlayerData.TotalPlayerSteps);
            // Logger.LogInfo($"UI Updated: API Today: {CurrentTodaysStepsFromAPI}, Total: {DataManager.Instance.CurrentPlayerData.TotalPlayerSteps}");
        }
    }

    void UpdatePermissionDeniedUI()
    {
        if (UIManager.Instance != null)
        {
            // Vous pourriez vouloir un texte spécifique pour cela dans UIManager
            // UIManager.Instance.UpdateTodaysStepsDisplay("N/A (Permission)");
            // UIManager.Instance.UpdateTotalPlayerStepsDisplay(DataManager.Instance.CurrentPlayerData.TotalPlayerSteps); // Le total reste
            Debug.LogWarning("UI should reflect permission denied state.");
        }
    }


    // --- Getters Publics ---
    public long GetCurrentTodaysStepsFromAPI()
    {
        return CurrentTodaysStepsFromAPI;
    }

    public long GetTotalPlayerSteps()
    {
        if (DataManager.Instance != null && DataManager.Instance.CurrentPlayerData != null)
        {
            return DataManager.Instance.CurrentPlayerData.TotalPlayerSteps;
        }
        Logger.LogWarning("GetTotalPlayerSteps: DataManager not ready. Returning 0.");
        return 0;
    }
}