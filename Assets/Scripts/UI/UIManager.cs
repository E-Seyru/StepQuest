// Filepath: Assets/Scripts/UI/UIManager.cs
using System.Collections;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI totalStepsText;
    [SerializeField] private TextMeshProUGUI lastUpdateText; // Nouveau: indicateur de dernière mise à jour

    private StepManager stepManager;
    private long lastDisplayedSteps = -1;
    private float stepUpdateFlashDuration = 0.3f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Logger.LogWarning("UIManager: Multiple instances detected! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        if (totalStepsText == null)
        {
            Logger.LogError("UIManager: totalStepsText n'est pas assigné dans l'inspecteur !");
        }

        // Initialiser l'affichage à une valeur d'attente
        UpdateTotalStepsDisplay(0, true);
    }

    IEnumerator Start()
    {
        while (StepManager.Instance == null)
        {
            yield return null;
        }
        stepManager = StepManager.Instance;
        Logger.LogInfo("UIManager: StepManager.Instance found. Ready to update UI from StepManager.");
    }

    void Update()
    {
        if (stepManager != null && stepManager.enabled)
        {
            // Mettre à jour l'affichage des pas uniquement si la valeur a changé
            if (stepManager.TotalSteps != lastDisplayedSteps)
            {
                UpdateTotalStepsDisplay(stepManager.TotalSteps);
            }
        }
    }

    private void UpdateTotalStepsDisplay(long steps, bool isWaitingMessage = false)
    {
        if (totalStepsText != null)
        {
            if (isWaitingMessage)
            {
                totalStepsText.text = "---";
                if (lastUpdateText != null)
                {
                    lastUpdateText.text = "Chargement...";
                }
            }
            else
            {
                // Effet visuel pour les changements de pas
                bool isIncrease = steps > lastDisplayedSteps && lastDisplayedSteps >= 0;

                // Mettre à jour le texte
                totalStepsText.text = $"{steps}";

                // Mettre à jour l'horodatage si disponible
                if (lastUpdateText != null && DataManager.Instance?.PlayerData != null)
                {
                    long lastChangeMs = DataManager.Instance.PlayerData.LastStepsChangeEpochMs;
                    if (lastChangeMs > 0)
                    {
                        System.DateTime lastChange = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)
                            .AddMilliseconds(lastChangeMs);
                        lastUpdateText.text = $"Dernière mise à jour: {lastChange.ToLocalTime():HH:mm:ss}";
                    }
                }

                // Si c'est une augmentation, ajouter un effet de flash
                if (isIncrease)
                {
                    StartCoroutine(FlashStepUpdate());
                }

                // Enregistrer la valeur
                lastDisplayedSteps = steps;
            }
        }
    }

    private IEnumerator FlashStepUpdate()
    {
        if (totalStepsText != null)
        {
            // Sauvegarder la couleur originale
            Color originalColor = totalStepsText.color;

            // Transition vers le vert pour indiquer une augmentation
            totalStepsText.color = Color.green;

            // Attendre un court instant
            yield return new WaitForSeconds(stepUpdateFlashDuration);

            // Revenir à la couleur d'origine
            totalStepsText.color = originalColor;
        }
    }
}