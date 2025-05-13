// Filepath: Assets/Scripts/UI/UIManager.cs
using System.Collections; // Ajouté pour la coroutine d'attente
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI References")] // Renommé pour plus de clarté
    [SerializeField] private TextMeshProUGUI pasDuJourText; // Renommé pour éviter confusion avec variables
    [SerializeField] private TextMeshProUGUI pasTotauxText; // Renommé pour éviter confusion avec variables

    private StepManager stepManager; // Référence au StepManager

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // UIManager est souvent spécifique à une scène, mais si votre UI est persistante, décommentez.
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Vérifier si les références TextMeshPro sont bien assignées
        if (pasDuJourText == null)
        {
            Debug.LogError("UIManager: pasDuJourText n'est pas assigné dans l'inspecteur !");
        }
        if (pasTotauxText == null)
        {
            Debug.LogError("UIManager: pasTotauxText n'est pas assigné dans l'inspecteur !");
        }

        // Initialiser l'affichage à des valeurs d'attente
        UpdateTodaysStepsDisplayInternal(0, true); // true pour indiquer "valeur d'attente"
        UpdateTotalPlayerStepsDisplayInternal(0, true);
    }

    IEnumerator Start() // Utiliser Start comme une coroutine pour attendre StepManager
    {
        // Attendre que StepManager soit initialisé et prêt
        // Logger.LogInfo("UIManager: Waiting for StepManager.Instance..."); // Peut être verbeux
        while (StepManager.Instance == null)
        {
            yield return null; // Attendre la prochaine frame
        }
        stepManager = StepManager.Instance;
        Logger.LogInfo("UIManager: StepManager.Instance found. Ready to update UI from StepManager.");
    }

    void Update()
    {
        // La mise à jour se fait maintenant en lisant les propriétés de StepManager
        if (stepManager != null && stepManager.enabled) // S'assurer que StepManager est prêt et actif
        {
            UpdateTodaysStepsDisplayInternal(stepManager.CurrentDisplayStepsToday);
            UpdateTotalPlayerStepsDisplayInternal(stepManager.CurrentDisplayTotalSteps);
        }
        // Si StepManager n'est pas prêt, Awake a déjà mis des valeurs d'attente.
    }

    // Les méthodes publiques sont appelées par StepManager
    // Correction: Ces méthodes ne sont plus appelées par StepManager.
    // UIManager lit directement les propriétés de StepManager dans son Update.
    // On garde des méthodes internes pour la logique d'affichage.

    private void UpdateTotalPlayerStepsDisplayInternal(long steps, bool isWaitingMessage = false)
    {
        if (pasTotauxText != null)
        {
            if (isWaitingMessage)
            {
                pasTotauxText.text = "---"; // Ou "Chargement..."
            }
            else
            {
                pasTotauxText.text = $"{steps}";
            }
        }
    }

    private void UpdateTodaysStepsDisplayInternal(long steps, bool isWaitingMessage = false)
    {
        if (pasDuJourText != null)
        {
            if (isWaitingMessage)
            {
                pasDuJourText.text = "---"; // Ou "Chargement..."
            }
            else
            {
                pasDuJourText.text = $"{steps}";
            }
        }
    }
}