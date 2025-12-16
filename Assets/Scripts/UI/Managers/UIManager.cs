// Purpose: Main UI manager for step display and updates
// Filepath: Assets/Scripts/UI/Managers/UIManager.cs
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI totalStepsText;
    [SerializeField] private TextMeshProUGUI dailyStepsText;
    [SerializeField] private TextMeshProUGUI lastUpdateText;
    [SerializeField] private Button MapButton;

    private StepManager stepManager;
    private long lastDisplayedTotalSteps = -1;
    private long lastDisplayedDailySteps = -1;
    private float stepUpdateFlashDuration = 0.3f;

    private Coroutine updateCoroutine;
    private Dictionary<TextMeshProUGUI, Coroutine> flashCoroutines = new Dictionary<TextMeshProUGUI, Coroutine>();

    public static UIManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
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

        StartUIUpdateCoroutine();
    }

    void OnDestroy()
    {
        StopUIUpdateCoroutine();

        foreach (var flashCoroutine in flashCoroutines.Values)
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
        }
        flashCoroutines.Clear();
    }

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

    private IEnumerator UIUpdateCoroutine()
    {
        while (true)
        {
            if (stepManager != null && stepManager.enabled)
            {
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

                if (!totalStepsChanged && !dailyStepsChanged)
                {
                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else
            {
                yield return new WaitForSeconds(1f);
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

                string newText = $"{steps}";
                if (totalStepsText.text != newText)
                {
                    totalStepsText.text = newText;
                }

                if (lastUpdateText != null && DataManager.Instance?.PlayerData != null)
                {
                    long lastChangeMs = DataManager.Instance.PlayerData.LastStepsChangeEpochMs;
                    if (lastChangeMs > 0)
                    {
                        string readableDate = LocalDatabase.GetReadableDateFromEpoch(lastChangeMs);
                        string newUpdateText = $"Derniere mise a jour: {readableDate}";
                        if (lastUpdateText.text != newUpdateText)
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

                string newText = $"{steps}";
                if (dailyStepsText.text != newText)
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

    private IEnumerator FlashStepUpdate(TextMeshProUGUI textElement)
    {
        if (textElement != null)
        {
            if (flashCoroutines.ContainsKey(textElement) && flashCoroutines[textElement] != null)
            {
                StopCoroutine(flashCoroutines[textElement]);
            }

            Color originalColor = textElement.color;
            textElement.color = Color.green;

            yield return new WaitForSeconds(stepUpdateFlashDuration);

            textElement.color = originalColor;

            if (flashCoroutines.ContainsKey(textElement))
            {
                flashCoroutines.Remove(textElement);
            }
        }
    }

    public void PauseUIUpdates()
    {
        StopUIUpdateCoroutine();
    }

    public void ResumeUIUpdates()
    {
        StartUIUpdateCoroutine();
    }

    public void ForceUIUpdate()
    {
        if (stepManager != null && stepManager.enabled)
        {
            UpdateTotalStepsDisplay(stepManager.TotalSteps);
            UpdateDailyStepsDisplay(stepManager.DailySteps);
        }
    }
}
