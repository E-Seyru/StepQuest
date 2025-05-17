// Filepath: Assets/Scripts/Services/StepTracking/RecordingAPIStepCounter.cs
using System;
using System.Collections;
using UnityEngine;

public class RecordingAPIStepCounter : MonoBehaviour
{
    private AndroidJavaClass stepPluginClass;
    private const string fullPluginClassName = "com.StepQuest.steps.StepPlugin";

    private bool isPluginClassInitialized = false;
    private bool isSubscribedToApi = false;
    private bool lastPermissionResult = false;
    private int permissionCheckCounter = 0;
    private const int LOG_FREQUENCY = 60; // Ne journaliser qu'une fois toutes les 60 vérifications

    // Constantes pour le mécanisme d'attente amélioré
    private const int MAX_API_READ_ATTEMPTS = 5;
    private const float BASE_API_WAIT_TIME = 0.5f;

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
            Logger.LogWarning("RecordingAPIStepCounter: Multiple instances detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    public void InitializeService()
    {
        if (isPluginClassInitialized) return;

        Logger.LogInfo("RecordingAPIStepCounter: InitializeService called.");
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

    // Gestion des Permissions
    public void RequestPermission()
    {
        if (!isPluginClassInitialized || stepPluginClass == null)
        {
            Logger.LogWarning("RecordingAPIStepCounter: RequestPermission called but plugin class not initialized.");
            return;
        }
        Logger.LogInfo("RecordingAPIStepCounter: Requesting activity recognition permission via plugin.");
        stepPluginClass.CallStatic("requestActivityRecognitionPermission");
    }

    public bool HasPermission()
    {
        if (!isPluginClassInitialized || stepPluginClass == null)
        {
            Logger.LogWarning("RecordingAPIStepCounter: HasPermission called but plugin class not initialized.");
            return false;
        }

        bool hasPermission = stepPluginClass.CallStatic<bool>("hasActivityRecognitionPermission");

        // Journaliser si le résultat a changé ou périodiquement
        permissionCheckCounter++;
        if (hasPermission != lastPermissionResult || permissionCheckCounter >= LOG_FREQUENCY)
        {
            if (hasPermission != lastPermissionResult)
            {
                Logger.LogInfo($"RecordingAPIStepCounter: Permission status changed to: {hasPermission}");
            }

            lastPermissionResult = hasPermission;
        }

        return hasPermission;
    }

    // Gestion de l'Abonnement API Recording
    public void SubscribeToRecordingApiIfNeeded()
    {
        if (!isPluginClassInitialized || stepPluginClass == null)
        {
            Logger.LogWarning("RecordingAPIStepCounter: SubscribeToRecordingApiIfNeeded called but plugin class not initialized.");
            return;
        }
        if (!HasPermission())
        {
            Logger.LogWarning("RecordingAPIStepCounter: Cannot subscribe, permission not granted.");
            return;
        }
        if (isSubscribedToApi)
        {
            return;
        }

        Logger.LogInfo("RecordingAPIStepCounter: Attempting to subscribe to API Recording.");
        stepPluginClass.CallStatic("subscribeToRecordingAPI");
        isSubscribedToApi = true;
    }

    // Lecture des données entre deux timestamps spécifiques avec mécanisme d'attente amélioré
    public IEnumerator GetDeltaSinceFromAPI(long fromEpochMs, long toEpochMs, System.Action<long> onResultCallback)
    {
        if (!isPluginClassInitialized || stepPluginClass == null || !HasPermission())
        {
            Logger.LogError("RecordingAPIStepCounter: GetDeltaSinceFromAPI cannot execute - plugin not ready or no permission.");
            onResultCallback?.Invoke(-1);
            yield break;
        }

        // Vérifier s'il s'agit d'une plage déjà lue (solution de secours si clearStoredRange échoue)
        if (ShouldSkipRange(fromEpochMs, toEpochMs))
        {
            Logger.LogWarning($"RecordingAPIStepCounter: Skipping already read range from {LocalDatabase.GetReadableDateFromEpoch(fromEpochMs)} to {LocalDatabase.GetReadableDateFromEpoch(toEpochMs)}");
            onResultCallback?.Invoke(0);
            yield break;
        }

        // Vérifier et gérer le cas spécial où fromEpochMs est 0 ou très ancien
        if (fromEpochMs <= 1)
        {
            Logger.LogInfo($"RecordingAPIStepCounter: GetDeltaSinceFromAPI called with very old/initial fromEpochMs={fromEpochMs}. Getting all available history.");
            // Le plugin StepPlugin va gérer ce cas spécial
        }
        else if (fromEpochMs >= toEpochMs)
        {
            Logger.LogWarning($"RecordingAPIStepCounter: GetDeltaSinceFromAPI called with fromEpochMs ({LocalDatabase.GetReadableDateFromEpoch(fromEpochMs)}) >= toEpochMs ({LocalDatabase.GetReadableDateFromEpoch(toEpochMs)}). Returning 0 steps.");
            onResultCallback?.Invoke(0);
            yield break;
        }

        // Mémoriser la plage en cours de lecture
        lastReadStartMs = fromEpochMs;
        lastReadEndMs = toEpochMs;

        Logger.LogInfo($"RecordingAPIStepCounter: Requesting readStepsForTimeRange from plugin for GetDeltaSinceFromAPI (from: {LocalDatabase.GetReadableDateFromEpoch(fromEpochMs)}, to: {LocalDatabase.GetReadableDateFromEpoch(toEpochMs)}).");
        stepPluginClass.CallStatic("readStepsForTimeRange", fromEpochMs, toEpochMs);

        // Mécanisme d'attente amélioré avec vérification
        int attempts = 0;
        long lastResult = -1;
        long currentResult = -1;

        while (attempts < MAX_API_READ_ATTEMPTS)
        {
            // Attente progressive qui augmente avec les tentatives
            yield return new WaitForSeconds(BASE_API_WAIT_TIME * (attempts + 1));

            currentResult = stepPluginClass.CallStatic<long>("getStoredStepsForCustomRange");
            Logger.LogInfo($"RecordingAPIStepCounter: API read attempt {attempts + 1}/{MAX_API_READ_ATTEMPTS}, value: {currentResult}");

            // Si on a une valeur valide et qu'elle est stable (deux lectures identiques), on peut sortir
            if (currentResult >= 0 && (currentResult == lastResult || attempts >= MAX_API_READ_ATTEMPTS - 1))
            {
                Logger.LogInfo($"RecordingAPIStepCounter: GetDeltaSince stable result after {attempts + 1} attempts: {currentResult}");
                break;
            }

            lastResult = currentResult;
            attempts++;
        }

        Logger.LogInfo($"RecordingAPIStepCounter: GetDeltaSinceFromAPI received {currentResult} steps from plugin for time range {LocalDatabase.GetReadableDateFromEpoch(fromEpochMs)} to {LocalDatabase.GetReadableDateFromEpoch(toEpochMs)}.");
        onResultCallback?.Invoke(currentResult >= 0 ? currentResult : 0);

        // Après avoir récupéré les données, effacer automatiquement la valeur stockée (Faille C)
        ClearStoredRange();
    }

    // Vérifier si une plage a déjà été lue (solution de secours pour la Faille #3)
    private bool ShouldSkipRange(long fromEpochMs, long toEpochMs)
    {
        try
        {
            string lastRange = PlayerPrefs.GetString("LastReadRange", "");
            if (!string.IsNullOrEmpty(lastRange))
            {
                string[] parts = lastRange.Split(':');
                if (parts.Length == 2)
                {
                    long lastStart = long.Parse(parts[0]);
                    long lastEnd = long.Parse(parts[1]);

                    // Vérifier si la plage actuelle chevauche la dernière plage lue
                    bool overlaps = (fromEpochMs <= lastEnd && toEpochMs >= lastStart);

                    if (overlaps)
                    {
                        Logger.LogWarning($"RecordingAPIStepCounter: Current range ({LocalDatabase.GetReadableDateFromEpoch(fromEpochMs)} to {LocalDatabase.GetReadableDateFromEpoch(toEpochMs)}) " +
                                         $"overlaps with last recorded range ({LocalDatabase.GetReadableDateFromEpoch(lastStart)} to {LocalDatabase.GetReadableDateFromEpoch(lastEnd)})");
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"RecordingAPIStepCounter: Error checking last range: {ex.Message}");
        }

        return false;
    }

    // Pour effacer la valeur stockée dans le plugin après la lecture (Faille C)
    // IMPORTANT: Toutes les lectures API doivent être suivies d'un appel à cette méthode
    // pour garantir l'effacement du buffer et éviter tout double comptage
    public void ClearStoredRange()
    {
        if (!isPluginClassInitialized || stepPluginClass == null)
        {
            Logger.LogWarning("RecordingAPIStepCounter: ClearStoredRange called but plugin class not initialized.");
            return;
        }

        bool clearSuccess = false;

        try
        {
            // Appeler la méthode dans le plugin Java et récupérer le statut de succès
            // Note: Cette modification nécessite de mettre à jour StepPlugin.java pour retourner un booléen
            clearSuccess = stepPluginClass.CallStatic<bool>("clearStoredStepsForCustomRange");

            if (clearSuccess)
            {
                Logger.LogInfo("RecordingAPIStepCounter: Successfully cleared stored range in plugin.");
            }
            else
            {
                // Échec de nettoyage côté plugin, mémoriser l'état pour éviter un double comptage
                HandleClearFailure();
                Logger.LogWarning("RecordingAPIStepCounter: Plugin reported failure to clear stored range. Using fallback mechanism.");
            }
        }
        catch (AndroidJavaException ex)
        {
            Logger.LogError($"RecordingAPIStepCounter: Failed to clear stored range in plugin. Exception: {ex.Message}");
            // Si la méthode n'existe pas encore dans le plugin, utiliser une solution de secours
            HandleClearFailure();
            Logger.LogWarning("RecordingAPIStepCounter: clearStoredStepsForCustomRange function may not be available. Using fallback mechanism.");
        }
    }

    // Méthode de secours pour éviter le double comptage si le nettoyage échoue
    private void HandleClearFailure()
    {
        // Stocker la dernière plage lue dans PlayerPrefs pour pouvoir l'éviter la prochaine fois
        // Cette solution est une SOLUTION DE SECOURS si le plugin ne supporte pas clearStoredStepsForCustomRange
        try
        {
            // Si nous avons des timestamps de la dernière lecture, les enregistrer
            if (lastReadStartMs > 0 && lastReadEndMs > 0)
            {
                PlayerPrefs.SetString("LastReadRange", $"{lastReadStartMs}:{lastReadEndMs}");
                PlayerPrefs.Save();
                Logger.LogInfo($"RecordingAPIStepCounter: Saved last read range {LocalDatabase.GetReadableDateFromEpoch(lastReadStartMs)} to {LocalDatabase.GetReadableDateFromEpoch(lastReadEndMs)} to PlayerPrefs as fallback.");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"RecordingAPIStepCounter: Failed to save fallback data: {ex.Message}");
        }
    }

    // Variables pour mémoriser la dernière plage lue
    private long lastReadStartMs = 0;
    private long lastReadEndMs = 0;

    // Maintient l'ancienne méthode pour compatibilité, mais utilise la nouvelle en interne
    public IEnumerator GetDeltaSinceFromAPI(long fromEpochMs, System.Action<long> onResultCallback)
    {
        // MODIFIÉ: Utiliser TimeZoneInfo.Local.ToLocalTime pour éviter les problèmes de fuseau horaire (Faille B)
        long nowEpochMs = GetLocalEpochMs();
        return GetDeltaSinceFromAPI(fromEpochMs, nowEpochMs, onResultCallback);
    }

    // NOUVELLE MÉTHODE: Obtenir le timestamp en utilisant le fuseau horaire local (Faille B)
    private long GetLocalEpochMs()
    {
        // MODIFIÉ: Utiliser DateTime.Now pour cohérence avec le reste du code
        return new System.DateTimeOffset(System.DateTime.Now).ToUnixTimeMilliseconds();
    }

    // Méthodes pour le capteur direct
    public void StartDirectSensorListener()
    {
        if (!isPluginClassInitialized || stepPluginClass == null || !HasPermission())
        {
            Logger.LogWarning("RecordingAPIStepCounter: StartDirectSensorListener - plugin not ready or no permission.");
            return;
        }
        Logger.LogInfo("RecordingAPIStepCounter: Requesting plugin to start direct step counter listener.");
        stepPluginClass.CallStatic("startDirectStepCounterListener");
    }

    public void StopDirectSensorListener()
    {
        if (!isPluginClassInitialized || stepPluginClass == null)
        {
            Logger.LogWarning("RecordingAPIStepCounter: StopDirectSensorListener - plugin class not initialized.");
            return;
        }
        Logger.LogInfo("RecordingAPIStepCounter: Requesting plugin to stop direct step counter listener.");
        stepPluginClass.CallStatic("stopDirectStepCounterListener");
    }

    public long GetCurrentRawSensorSteps()
    {
        if (!isPluginClassInitialized || stepPluginClass == null || !HasPermission())
        {
            return -1;
        }
        return stepPluginClass.CallStatic<long>("getCurrentRawSensorSteps");
    }
}