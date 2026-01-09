// Purpose: Simple popup to confirm travel cancellation
// Filepath: Assets/Scripts/UI/Navigation/TravelCancelPopup.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TravelCancelPopup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    public static TravelCancelPopup Instance { get; private set; }

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

        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }

    void Start()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirm);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancel);
        }
    }

    public void Show()
    {
        var playerData = DataManager.Instance?.PlayerData;
        if (playerData == null || !playerData.IsCurrentlyTraveling())
        {
            Logger.LogWarning("TravelCancelPopup: Not currently traveling", Logger.LogCategory.MapLog);
            return;
        }

        // Calculate steps to return
        int progressMade = (int)(playerData.TotalSteps - playerData.TravelStartSteps);
        if (progressMade < 0) progressMade = 0;

        // Get location names
        var registry = MapManager.Instance?.LocationRegistry;
        string originName = registry?.GetLocationById(playerData.TravelOriginLocationId)?.DisplayName ?? "origin";
        string destName = registry?.GetLocationById(playerData.TravelDestinationId)?.DisplayName ?? "destination";

        // Set message
        if (messageText != null)
        {
            if (progressMade == 0)
            {
                messageText.text = $"Cancel travel to {destName}?\n\nYou will return to {originName} instantly.";
            }
            else
            {
                messageText.text = $"Cancel travel to {destName}?\n\nYou will need {progressMade} steps to return to {originName}.";
            }
        }

        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
        }
    }

    public void Hide()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }

    private void OnConfirm()
    {
        MapManager.Instance?.CancelTravelAndReverse();
        Hide();
    }

    private void OnCancel()
    {
        Hide();
    }

    void Update()
    {
        if (popupPanel != null && popupPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnCancel();
            }
        }
    }
}
