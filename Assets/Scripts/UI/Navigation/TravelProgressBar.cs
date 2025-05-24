// Purpose: Simple travel progress bar that shows current travel progress
// Filepath: Assets/Scripts/UI/Components/TravelProgressBar.cs
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
    [SerializeField] private float updateInterval = 0.5f; // Fréquence de mise à jour
    [SerializeField] private float animationDuration = 0.3f; // Durée d'animation LeanTween

    // État interne
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

        // Cacher la barre au démarrage
        HideProgressBar();
    }

    void Start()
    {
        // Obtenir les références
        mapManager = MapManager.Instance;
        dataManager = DataManager.Instance;

        if (mapManager != null)
        {
            // S'abonner aux événements de voyage
            mapManager.OnTravelStarted += OnTravelStarted;
            mapManager.OnTravelCompleted += OnTravelCompleted;
            mapManager.OnTravelProgress += OnTravelProgress;
        }

        // Validation des références
        ValidateReferences();

        // Configurer le séparateur
        if (separatorText != null)
        {
            separatorText.text = "/";
        }
    }

    void OnDestroy()
    {
        // Se désabonner des événements
        if (mapManager != null)
        {
            mapManager.OnTravelStarted -= OnTravelStarted;
            mapManager.OnTravelCompleted -= OnTravelCompleted;
            mapManager.OnTravelProgress -= OnTravelProgress;
        }
    }

    void Update()
    {
        // Mise à jour périodique si en voyage
        if (isVisible && Time.time - lastUpdateTime > updateInterval)
        {
            UpdateProgressDisplay();
            lastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// Appelé quand un voyage commence
    /// </summary>
    private void OnTravelStarted(string destinationId)
    {
        ShowProgressBar();
        UpdateProgressText(destinationId);
        UpdateProgressDisplay();

        Logger.LogInfo($"TravelProgressBar: Travel started to {destinationId}", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// Appelé quand un voyage se termine
    /// </summary>
    private void OnTravelCompleted(string arrivedLocationId)
    {
        // Animer jusqu'à 100% puis cacher
        AnimateToComplete(() => HideProgressBar());

        Logger.LogInfo($"TravelProgressBar: Travel completed to {arrivedLocationId}", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// Appelé pendant le voyage pour mettre à jour le progrès
    /// </summary>
    private void OnTravelProgress(string destinationId, int currentSteps, int requiredSteps)
    {
        UpdateProgressValues(currentSteps, requiredSteps);
    }

    /// <summary>
    /// Met à jour le texte de destination
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
    /// Met à jour l'affichage du progrès
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
    /// Met à jour les valeurs de progrès et anime si nécessaire
    /// </summary>
    private void UpdateProgressValues(int currentSteps, int requiredSteps)
    {
        // Mise à jour des textes
        if (currentStepText != null)
        {
            currentStepText.text = currentSteps.ToString();
        }

        if (totalRequiredText != null)
        {
            totalRequiredText.text = requiredSteps.ToString();
        }

        // Animation de la barre de progression si elle a changé
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
    /// Anime la barre jusqu'à 100% puis exécute un callback
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
    /// Validation des références UI
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
    /// Force une mise à jour manuelle (utile pour le debug)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceUpdate()
    {
        UpdateProgressDisplay();
    }
}