using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI totalStepsText;
    [SerializeField] private TextMeshProUGUI dailyStepsText;
    [SerializeField] private TextMeshProUGUI lastUpdateText;
    [SerializeField] private Button MapButton;

    private StepManager stepManager;
    private long lastDisplayedTotalSteps = -1;
    private long lastDisplayedDailySteps = -1;
    private float stepUpdateFlashDuration = 0.3f;

    // OPTIMISATION : Variables pour eviter les Update() constants
    private Coroutine updateCoroutine;
    private bool isUpdateActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

        }
        else
        {
            Logger.LogWarning("UIManager: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.General);
            Destroy(gameObject);
            return;
        }

        if (totalStepsText == null)
        {
            Logger.LogError("UIManager: totalStepsText n'est pas assigne dans l'inspecteur !", Logger.LogCategory.General);
        }

        if (dailyStepsText == null)
        {
            Logger.LogWarning("UIManager: dailyStepsText n'est pas assigne dans l'inspecteur ! L'affichage des pas quotidiens ne fonctionnera pas.", Logger.LogCategory.General);
        }

        // Initialiser l'affichage a une valeur d'attente
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
        Logger.LogInfo("UIManager: StepManager.Instance found. Ready to update UI from StepManager.", Logger.LogCategory.General);

        // OPTIMISATION : Demarrer la coroutine d'update au lieu d'Update()
        StartUIUpdateCoroutine();
    }

    // OPTIMISATION : Remplacer Update() par une coroutine plus efficace
    private void StartUIUpdateCoroutine()
    {
        if (updateCoroutine != null)
            StopCoroutine(updateCoroutine);

        updateCoroutine = StartCoroutine(UIUpdateCoroutine());
    }

    private void StopUIUpdateCoroutine()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }
    }

    // OPTIMISATION : Coroutine qui verifie les changements moins frequemment
    private IEnumerator UIUpdateCoroutine()
    {
        while (true)
        {
            if (stepManager != null && stepManager.enabled)
            {
                // OPTIMISATION : Verifier seulement si les valeurs ont vraiment change
                bool totalStepsChanged = stepManager.TotalSteps != lastDisplayedTotalSteps;
                bool dailyStepsChanged = stepManager.DailySteps != lastDisplayedDailySteps;

                if (totalStepsChanged)
                {
                    UpdateTotalStepsDisplay(stepManager.TotalSteps);
                }

                if (dailyStepsChanged)
                {
                    UpdateDailyStepsDisplay(stepManager.DailySteps);
                }

                // OPTIMISATION : Si rien n'a change, attendre plus longtemps
                if (!totalStepsChanged && !dailyStepsChanged)
                {
                    yield return new WaitForSeconds(0.5f); // Attendre 0.5s si pas de changement
                }
                else
                {
                    yield return new WaitForSeconds(0.1f); // Verifier plus souvent s'il y a des changements
                }
            }
            else
            {
                yield return new WaitForSeconds(1f); // Attendre plus longtemps si StepManager pas prêt
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
                bool isIncrease = steps > lastDisplayedTotalSteps && lastDisplayedTotalSteps >= 0;

                // OPTIMISATION : Mettre a jour le texte seulement si necessaire
                string newText = $"{steps}";
                if (totalStepsText.text != newText) // MODIFICATION APPLIQUeE
                {
                    totalStepsText.text = newText;
                }

                if (lastUpdateText != null && DataManager.Instance?.PlayerData != null)
                {
                    long lastChangeMs = DataManager.Instance.PlayerData.LastStepsChangeEpochMs;
                    if (lastChangeMs > 0)
                    {
                        string readableDate = LocalDatabase.GetReadableDateFromEpoch(lastChangeMs);
                        string newUpdateText = $"Dernière mise a jour: {readableDate}";
                        if (lastUpdateText.text != newUpdateText) // MODIFICATION APPLIQUeE
                        {
                            lastUpdateText.text = newUpdateText;
                        }
                    }
                }

                if (isIncrease)
                {
                    StartCoroutine(FlashStepUpdate(totalStepsText));
                }
                lastDisplayedTotalSteps = steps;
            }
        }
    }

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
                bool isIncrease = steps > lastDisplayedDailySteps && lastDisplayedDailySteps >= 0;

                // OPTIMISATION : Mettre a jour seulement si necessaire
                string newText = $"{steps}";
                if (dailyStepsText.text != newText) // MODIFICATION APPLIQUeE
                {
                    dailyStepsText.text = newText;
                }

                if (isIncrease)
                {
                    StartCoroutine(FlashStepUpdate(dailyStepsText));
                }
                lastDisplayedDailySteps = steps;
            }
        }
    }

    // OPTIMISATION : eviter les coroutines multiples pour le même element
    private System.Collections.Generic.Dictionary<TextMeshProUGUI, Coroutine> flashCoroutines =
        new System.Collections.Generic.Dictionary<TextMeshProUGUI, Coroutine>();

    private IEnumerator FlashStepUpdate(TextMeshProUGUI textElement)
    {
        if (textElement != null)
        {
            // OPTIMISATION : Arrêter la coroutine precedente si elle existe
            if (flashCoroutines.ContainsKey(textElement) && flashCoroutines[textElement] != null)
            {
                StopCoroutine(flashCoroutines[textElement]);
            }

            // Sauvegarder la couleur originale
            Color originalColor = textElement.color;

            // Transition vers le vert pour indiquer une augmentation
            textElement.color = Color.green;

            // Attendre un court instant
            yield return new WaitForSeconds(stepUpdateFlashDuration);

            // Revenir a la couleur d'origine
            textElement.color = originalColor;

            // Nettoyer la reference
            if (flashCoroutines.ContainsKey(textElement))
            {
                flashCoroutines.Remove(textElement);
            }
        }
    }

    // OPTIMISATION : Methodes publiques pour contrôler l'update
    public void PauseUIUpdates()
    {
        StopUIUpdateCoroutine();
    }

    public void ResumeUIUpdates()
    {
        StartUIUpdateCoroutine();
    }

    // OPTIMISATION : Forcer une mise a jour immediate si necessaire
    public void ForceUIUpdate()
    {
        if (stepManager != null && stepManager.enabled)
        {
            UpdateTotalStepsDisplay(stepManager.TotalSteps);
            UpdateDailyStepsDisplay(stepManager.DailySteps);
        }
    }

    // OPTIMISATION : Nettoyer a la destruction
    private void OnDestroy()
    {
        StopUIUpdateCoroutine();

        // Arrêter toutes les coroutines de flash en cours
        foreach (var flashCoroutine in flashCoroutines.Values)
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
        }
        flashCoroutines.Clear();
    }
}