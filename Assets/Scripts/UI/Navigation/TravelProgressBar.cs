// Purpose: Simple travel progress bar that shows current travel progress
// Filepath: Assets/Scripts/UI/Components/TravelProgressBar.cs
using MapEvents; // NOUVEAU: Import pour EventBus
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TravelProgressBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject progressContainer;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI currentStepText;
    [SerializeField] private TextMeshProUGUI separatorText;
    [SerializeField] private TextMeshProUGUI totalRequiredText;
    [SerializeField] private Image progressBarFill;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f; // Frequence de mise a jour
    [SerializeField] private float animationDuration = 0.3f; // Duree d'animation LeanTween

    // etat interne
    private MapManager mapManager;
    private DataManager dataManager;
    private float lastUpdateTime;
    private int lastCurrentSteps = -1;
    private bool isVisible = false;

    public static TravelProgressBar Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Logger.LogWarning("TravelProgressBar: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.General);
            Destroy(gameObject);
            return;
        }

        // Cacher la barre au demarrage
        HideProgressBar();
    }

    void Start()
    {
        // Obtenir les references
        mapManager = MapManager.Instance;
        dataManager = DataManager.Instance;

        // =====================================
        // EVENTBUS - S'abonner aux evenements de voyage
        // =====================================
        EventBus.Subscribe<TravelStartedEvent>(OnTravelStarted);
        EventBus.Subscribe<TravelCompletedEvent>(OnTravelCompleted);
        EventBus.Subscribe<TravelProgressEvent>(OnTravelProgress);

        // Validation des references
        ValidateReferences();

        // Configurer le separateur
        if (separatorText != null)
        {
            separatorText.text = "/";
        }

        // NOUVEAU: Verifier si on est deja en voyage au demarrage
        StartCoroutine(CheckTravelStateOnStart());
    }

    // NOUVEAU: Verifier l'etat de voyage au demarrage
    private System.Collections.IEnumerator CheckTravelStateOnStart()
    {
        // Attendre que tous les managers soient prêts
        yield return new WaitForSeconds(0.5f);

        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            string destinationId = dataManager.PlayerData.TravelDestinationId;
            Logger.LogInfo($"TravelProgressBar: Found ongoing travel to {destinationId} at startup - showing progress bar", Logger.LogCategory.General);

            // Afficher la barre et initialiser le texte
            ShowProgressBar();
            UpdateProgressText(destinationId);
            UpdateProgressDisplay();
        }
    }

    void OnDestroy()
    {
        // =====================================
        // EVENTBUS - Se desabonner des evenements
        // =====================================
        EventBus.Unsubscribe<TravelStartedEvent>(OnTravelStarted);
        EventBus.Unsubscribe<TravelCompletedEvent>(OnTravelCompleted);
        EventBus.Unsubscribe<TravelProgressEvent>(OnTravelProgress);
    }

    void Update()
    {
        // Mise a jour periodique si en voyage
        if (isVisible && Time.time - lastUpdateTime > updateInterval)
        {
            UpdateProgressDisplay();
            lastUpdateTime = Time.time;
        }
    }

    // === GESTIONNAIRES D'eVeNEMENTS - ADAPTeS POUR EVENTBUS ===

    /// <summary>
    /// Appele quand un voyage commence
    /// </summary>
    private void OnTravelStarted(TravelStartedEvent eventData)
    {
        ShowProgressBar();
        UpdateProgressText(eventData.DestinationLocationId);
        UpdateProgressDisplay();

        Logger.LogInfo($"TravelProgressBar: Travel started to {eventData.DestinationLocationId}", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// Appele quand un voyage se termine
    /// </summary>
    private void OnTravelCompleted(TravelCompletedEvent eventData)
    {
        // Animer jusqu'a 100% puis cacher
        AnimateToComplete(() => HideProgressBar());

        Logger.LogInfo($"TravelProgressBar: Travel completed to {eventData.NewLocation?.DisplayName ?? eventData.DestinationLocationId}", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// Appele pendant le voyage pour mettre a jour le progrès
    /// </summary>
    private void OnTravelProgress(TravelProgressEvent eventData)
    {
        UpdateProgressValues(eventData.CurrentSteps, eventData.RequiredSteps);
    }

    /// <summary>
    /// Met a jour le texte de destination
    /// </summary>
    private void UpdateProgressText(string destinationId)
    {
        if (progressText == null) return;

        if (mapManager?.LocationRegistry != null)
        {
            var destination = mapManager.LocationRegistry.GetLocationById(destinationId);
            if (destination != null)
            {
                progressText.text = $"En route vers {destination.DisplayName}...";
            }
            else
            {
                progressText.text = $"En route vers {destinationId}...";
            }
        }
        else
        {
            progressText.text = "En voyage...";
        }
    }

    /// <summary>
    /// Met a jour l'affichage du progrès
    /// </summary>
    private void UpdateProgressDisplay()
    {
        if (dataManager?.PlayerData == null || !dataManager.PlayerData.IsCurrentlyTraveling())
        {
            return;
        }

        long currentTotalSteps = dataManager.PlayerData.TotalSteps;
        long progressSteps = dataManager.PlayerData.GetTravelProgress(currentTotalSteps);
        int requiredSteps = dataManager.PlayerData.TravelRequiredSteps;

        UpdateProgressValues((int)progressSteps, requiredSteps);
    }

    /// <summary>
    /// Met a jour les valeurs de progrès et anime si necessaire
    /// </summary>
    private void UpdateProgressValues(int currentSteps, int requiredSteps)
    {
        // Mise a jour des textes
        if (currentStepText != null)
        {
            currentStepText.text = currentSteps.ToString();
        }

        if (totalRequiredText != null)
        {
            totalRequiredText.text = requiredSteps.ToString();
        }

        // Animation de la barre de progression si elle a change
        if (progressBarFill != null && currentSteps != lastCurrentSteps)
        {
            float progress = requiredSteps > 0 ? (float)currentSteps / requiredSteps : 0f;
            progress = Mathf.Clamp01(progress);

            // Utiliser LeanTween pour animer le fillAmount
            LeanTween.value(gameObject, progressBarFill.fillAmount, progress, animationDuration)
                .setOnUpdate((float value) =>
                {
                    progressBarFill.fillAmount = value;
                })
                .setEase(LeanTweenType.easeOutQuad);

            lastCurrentSteps = currentSteps;
        }
    }

    /// <summary>
    /// Anime la barre jusqu'a 100% puis execute un callback
    /// </summary>
    private void AnimateToComplete(System.Action onComplete = null)
    {
        if (progressBarFill != null)
        {
            LeanTween.value(gameObject, progressBarFill.fillAmount, 1f, animationDuration)
                .setOnUpdate((float value) =>
                {
                    progressBarFill.fillAmount = value;
                })
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() =>
                {
                    onComplete?.Invoke();
                });
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Affiche la barre de progression
    /// </summary>
    public void ShowProgressBar()
    {
        if (progressContainer != null)
        {
            progressContainer.SetActive(true);
            isVisible = true;

            // Animation d'apparition simple avec LeanTween
            progressContainer.transform.localScale = Vector3.zero;
            LeanTween.scale(progressContainer, Vector3.one, 0.3f)
                .setEase(LeanTweenType.easeOutBack);
        }
    }

    /// <summary>
    /// Cache la barre de progression
    /// </summary>
    public void HideProgressBar()
    {
        if (progressContainer != null)
        {
            // Animation de disparition
            LeanTween.scale(progressContainer, Vector3.zero, 0.2f)
                .setEase(LeanTweenType.easeInQuad)
                .setOnComplete(() =>
                {
                    progressContainer.SetActive(false);
                    isVisible = false;

                    // Reset des valeurs
                    if (progressBarFill != null)
                    {
                        progressBarFill.fillAmount = 0f;
                    }
                    lastCurrentSteps = -1;
                });
        }
    }

    /// <summary>
    /// Validation des references UI
    /// </summary>
    private void ValidateReferences()
    {
        if (progressContainer == null)
            Logger.LogError("TravelProgressBar: progressContainer not assigned!", Logger.LogCategory.General);

        if (progressText == null)
            Logger.LogWarning("TravelProgressBar: progressText not assigned!", Logger.LogCategory.General);

        if (progressBarFill == null)
            Logger.LogError("TravelProgressBar: progressBarFill not assigned!", Logger.LogCategory.General);

        if (currentStepText == null)
            Logger.LogWarning("TravelProgressBar: currentStepText not assigned!", Logger.LogCategory.General);

        if (totalRequiredText == null)
            Logger.LogWarning("TravelProgressBar: totalRequiredText not assigned!", Logger.LogCategory.General);
    }

    /// <summary>
    /// Force une mise a jour manuelle (utile pour le debug)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceUpdate()
    {
        UpdateProgressDisplay();
    }
}