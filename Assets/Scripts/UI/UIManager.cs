// Filepath: Assets/Scripts/UI/UIManager.cs
using System.Collections; // Ajout� pour la coroutine d'attente
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI References")] // Renomm� pour plus de clart�
    [SerializeField] private TextMeshProUGUI pasDuJourText; // Renomm� pour �viter confusion avec variables
    [SerializeField] private TextMeshProUGUI pasTotauxText; // Renomm� pour �viter confusion avec variables

    private StepManager stepManager; // R�f�rence au StepManager

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // UIManager est souvent sp�cifique � une sc�ne, mais si votre UI est persistante, d�commentez.
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // V�rifier si les r�f�rences TextMeshPro sont bien assign�es
        if (pasDuJourText == null)
        {
            Debug.LogError("UIManager: pasDuJourText n'est pas assign� dans l'inspecteur !");
        }
        if (pasTotauxText == null)
        {
            Debug.LogError("UIManager: pasTotauxText n'est pas assign� dans l'inspecteur !");
        }

        // Initialiser l'affichage � des valeurs d'attente
        UpdateTodaysStepsDisplayInternal(0, true); // true pour indiquer "valeur d'attente"
        UpdateTotalPlayerStepsDisplayInternal(0, true);
    }

    IEnumerator Start() // Utiliser Start comme une coroutine pour attendre StepManager
    {
        // Attendre que StepManager soit initialis� et pr�t
        // Logger.LogInfo("UIManager: Waiting for StepManager.Instance..."); // Peut �tre verbeux
        while (StepManager.Instance == null)
        {
            yield return null; // Attendre la prochaine frame
        }
        stepManager = StepManager.Instance;
        Logger.LogInfo("UIManager: StepManager.Instance found. Ready to update UI from StepManager.");
    }

    void Update()
    {
        // La mise � jour se fait maintenant en lisant les propri�t�s de StepManager
        if (stepManager != null && stepManager.enabled) // S'assurer que StepManager est pr�t et actif
        {
            UpdateTodaysStepsDisplayInternal(stepManager.CurrentDisplayStepsToday);
            UpdateTotalPlayerStepsDisplayInternal(stepManager.CurrentDisplayTotalSteps);
        }
        // Si StepManager n'est pas pr�t, Awake a d�j� mis des valeurs d'attente.
    }

    // Les m�thodes publiques sont appel�es par StepManager
    // Correction: Ces m�thodes ne sont plus appel�es par StepManager.
    // UIManager lit directement les propri�t�s de StepManager dans son Update.
    // On garde des m�thodes internes pour la logique d'affichage.

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