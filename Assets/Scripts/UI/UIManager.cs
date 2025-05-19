// Filepath: Assets/Scripts/UI/UIManager.cs
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI totalStepsText;
    [SerializeField] private TextMeshProUGUI dailyStepsText; // Nouveau: texte pour afficher les pas quotidiens
    [SerializeField] private TextMeshProUGUI lastUpdateText; // Indicateur de dernière mise à jour
    [SerializeField] private Button MapButton;

    private StepManager stepManager;
    private long lastDisplayedTotalSteps = -1;
    private long lastDisplayedDailySteps = -1;
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

        if (dailyStepsText == null)
        {
            Logger.LogWarning("UIManager: dailyStepsText n'est pas assigné dans l'inspecteur ! L'affichage des pas quotidiens ne fonctionnera pas.");
        }

        // Initialiser l'affichage à une valeur d'attente
        UpdateTotalStepsDisplay(0, true);
        UpdateDailyStepsDisplay(0, true);
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
            // Mettre à jour l'affichage des pas totaux uniquement si la valeur a changé
            if (stepManager.TotalSteps != lastDisplayedTotalSteps)
            {
                UpdateTotalStepsDisplay(stepManager.TotalSteps);
            }

            // Mettre à jour l'affichage des pas quotidiens uniquement si la valeur a changé
            if (stepManager.DailySteps != lastDisplayedDailySteps)
            {
                UpdateDailyStepsDisplay(stepManager.DailySteps);
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
                bool isIncrease = steps > lastDisplayedTotalSteps && lastDisplayedTotalSteps >= 0;

                // Mettre à jour le texte
                totalStepsText.text = $"{steps}";

                // Mettre à jour l'horodatage si disponible
                if (lastUpdateText != null && DataManager.Instance?.PlayerData != null)
                {
                    long lastChangeMs = DataManager.Instance.PlayerData.LastStepsChangeEpochMs;
                    if (lastChangeMs > 0)
                    {
                        string readableDate = LocalDatabase.GetReadableDateFromEpoch(lastChangeMs);
                        lastUpdateText.text = $"Dernière mise à jour: {readableDate}";
                    }
                }

                // Si c'est une augmentation, ajouter un effet de flash
                if (isIncrease)
                {
                    StartCoroutine(FlashStepUpdate(totalStepsText));
                }

                // Enregistrer la valeur
                lastDisplayedTotalSteps = steps;
            }
        }
    }

    // Nouvelle méthode pour mettre à jour l'affichage des pas quotidiens
    private void UpdateDailyStepsDisplay(long steps, bool isWaitingMessage = false)
    {
        if (dailyStepsText != null)
        {
            if (isWaitingMessage)
            {
                dailyStepsText.text = "---";
            }
            else
            {
                // Effet visuel pour les changements de pas
                bool isIncrease = steps > lastDisplayedDailySteps && lastDisplayedDailySteps >= 0;

                // Mettre à jour le texte
                dailyStepsText.text = $"{steps}";

                // Si c'est une augmentation, ajouter un effet de flash
                if (isIncrease)
                {
                    StartCoroutine(FlashStepUpdate(dailyStepsText));
                }

                // Enregistrer la valeur
                lastDisplayedDailySteps = steps;
            }
        }
    }

    // Méthode modifiée pour accepter n'importe quel TextMeshProUGUI comme paramètre
    private IEnumerator FlashStepUpdate(TextMeshProUGUI textElement)
    {
        if (textElement != null)
        {
            // Sauvegarder la couleur originale
            Color originalColor = textElement.color;

            // Transition vers le vert pour indiquer une augmentation
            textElement.color = Color.green;

            // Attendre un court instant
            yield return new WaitForSeconds(stepUpdateFlashDuration);

            // Revenir à la couleur d'origine
            textElement.color = originalColor;
        }
    }



}