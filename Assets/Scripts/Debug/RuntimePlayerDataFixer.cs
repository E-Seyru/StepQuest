// Purpose: Runtime player data fixer for Android debugging - Works on device!
// Filepath: Assets/Scripts/Debug/RuntimePlayerDataFixer.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interface de debug runtime pour reparer les donnees joueur corrompues
/// Fonctionne sur Android ! Ajoute ce script a un GameObject avec UI
/// </summary>
public class RuntimePlayerDataFixer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Button diagnoseButton;
    [SerializeField] private Button repairTravelButton;
    [SerializeField] private Button repairActivityButton;
    [SerializeField] private Button fullRepairButton;
    [SerializeField] private Button togglePanelButton;

    [Header("Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F1; // Pour l'editeur
    [SerializeField] private bool enableTouchToggle = true; // 5 taps rapides pour ouvrir
    [SerializeField] private bool startVisible = false;

    // Touch detection for Android
    private float lastTapTime = 0f;
    private int tapCount = 0;
    private const float MULTI_TAP_TIME = 0.5f;
    private const int REQUIRED_TAPS = 5;

    // References
    private DataManager dataManager;
    private MapManager mapManager;
    private ActivityManager activityManager;

    void Start()
    {
        // Get references
        dataManager = DataManager.Instance;
        mapManager = MapManager.Instance;
        activityManager = ActivityManager.Instance;

        // Setup UI
        if (debugPanel != null)
        {
            debugPanel.SetActive(startVisible);
        }

        // Setup buttons
        if (diagnoseButton != null)
            diagnoseButton.onClick.AddListener(DiagnosePlayerState);

        if (repairTravelButton != null)
            repairTravelButton.onClick.AddListener(RepairTravelState);

        if (repairActivityButton != null)
            repairActivityButton.onClick.AddListener(RepairActivityState);

        if (fullRepairButton != null)
            fullRepairButton.onClick.AddListener(FullRepair);

        if (togglePanelButton != null)
            togglePanelButton.onClick.AddListener(TogglePanel);

        UpdateStatusText("Player Data Fixer Ready");
    }

    void Update()
    {
        // Keyboard toggle (Editor)
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }

        // Touch toggle (Android)
        if (enableTouchToggle && Input.GetMouseButtonDown(0))
        {
            DetectMultiTap();
        }
    }

    private void DetectMultiTap()
    {
        float currentTime = Time.time;

        if (currentTime - lastTapTime < MULTI_TAP_TIME)
        {
            tapCount++;
        }
        else
        {
            tapCount = 1;
        }

        lastTapTime = currentTime;

        if (tapCount >= REQUIRED_TAPS)
        {
            tapCount = 0;
            TogglePanel();
            UpdateStatusText($"Debug Panel Opened! (Tapped {REQUIRED_TAPS}x)");
        }
    }

    public void TogglePanel()
    {
        if (debugPanel != null)
        {
            bool newState = !debugPanel.activeSelf;
            debugPanel.SetActive(newState);

            if (newState)
            {
                UpdateStatusText("Debug Panel Opened");
                DiagnosePlayerState(); // Auto-diagnose on open
            }
        }
    }

    public void DiagnosePlayerState()
    {
        string diagnosis = "DIAGNOSTIC:\n\n";

        if (dataManager?.PlayerData == null)
        {
            diagnosis += "ERROR: DataManager non disponible!\n";
            UpdateStatusText(diagnosis);
            return;
        }

        var playerData = dataManager.PlayerData;

        // Position & Travel
        diagnosis += "POSITION:\n";
        diagnosis += $"Location: {playerData.CurrentLocationId}\n";
        diagnosis += $"En voyage: {(playerData.IsCurrentlyTraveling() ? "PROBLEME - OUI" : "OK - Non")}\n";

        if (playerData.IsCurrentlyTraveling())
        {
            diagnosis += $"Vers: {playerData.TravelDestinationId}\n";
            long progress = playerData.GetTravelProgress(playerData.TotalSteps);
            diagnosis += $"Progres: {progress}/{playerData.TravelRequiredSteps}\n";
        }

        // Activity
        diagnosis += "\nACTIVITE:\n";
        diagnosis += $"   Active: {(playerData.HasActiveActivity() ? "Oui" : "Non")}\n";

        if (playerData.HasActiveActivity())
        {
            var activity = playerData.CurrentActivity;
            diagnosis += $"   Type: {activity.ActivityId}/{activity.VariantId}\n";
        }

        // Conditions
        diagnosis += "\nCONDITIONS:\n";
        diagnosis += $"   Peut faire activite: {(activityManager?.CanStartActivity() == true ? "OK - Oui" : "PROBLEME - Non")}\n";

        if (mapManager?.CurrentLocation != null)
        {
            bool canTravel = mapManager.CanTravelTo("Village");
            diagnosis += $"Peut voyager: {(canTravel ? "OK - Oui" : "PROBLEME - Non")}\n";
        }

        // Problem detection
        bool hasProblems = playerData.IsCurrentlyTraveling() ||
                          (activityManager?.CanStartActivity() == false);

        if (hasProblems)
        {
            diagnosis += "\nPROBLEMES DETECTES!\n";
            diagnosis += "Utilise 'REPARATION COMPLETE'\n";
        }
        else
        {
            diagnosis += "\nTout semble OK!\n";
        }

        UpdateStatusText(diagnosis);

        // Also log to console for more details
        Debug.Log("=== RUNTIME DIAGNOSTIC ===");
        Debug.Log(diagnosis.Replace("\n", " | "));
        Debug.Log("=== END DIAGNOSTIC ===");
    }

    public void RepairTravelState()
    {
        if (dataManager?.PlayerData == null)
        {
            UpdateStatusText("ERROR: DataManager non disponible!");
            return;
        }

        var playerData = dataManager.PlayerData;

        if (playerData.IsCurrentlyTraveling())
        {
            string oldDest = playerData.TravelDestinationId;

            // Clear travel state
            playerData.TravelDestinationId = null;
            playerData.TravelStartSteps = 0;
            playerData.TravelRequiredSteps = 0;

            // Clear MapManager state
            mapManager?.ClearTravelState();

            // Force save
            dataManager?.ForceSave();

            UpdateStatusText($"SUCCESS: Etat voyage repare!\nSupprime: {oldDest}");
            Debug.Log($"RuntimeFixer: Repaired travel state - removed destination '{oldDest}'");
        }
        else
        {
            UpdateStatusText("INFO: Pas d'etat voyage a reparer");
        }
    }

    public void RepairActivityState()
    {
        if (dataManager?.PlayerData == null)
        {
            UpdateStatusText("ERROR: DataManager non disponible!");
            return;
        }

        var playerData = dataManager.PlayerData;

        if (playerData.HasActiveActivity())
        {
            string oldActivity = $"{playerData.CurrentActivity.ActivityId}/{playerData.CurrentActivity.VariantId}";

            // Stop activity
            playerData.StopActivity();

            // Force save
            dataManager?.ForceSave();

            UpdateStatusText($"SUCCESS: Activite arretee!\nSupprime: {oldActivity}");
            Debug.Log($"RuntimeFixer: Repaired activity state - stopped '{oldActivity}'");
        }
        else
        {
            UpdateStatusText("INFO: Pas d'activite a arreter");
        }
    }

    public void FullRepair()
    {
        UpdateStatusText("Reparation en cours...");

        string report = "REPARATION COMPLETE:\n\n";
        bool anyRepairs = false;

        if (dataManager?.PlayerData == null)
        {
            UpdateStatusText("ERROR: DataManager non disponible!");
            return;
        }

        var playerData = dataManager.PlayerData;

        // Repair travel
        if (playerData.IsCurrentlyTraveling())
        {
            string oldDest = playerData.TravelDestinationId;
            playerData.TravelDestinationId = null;
            playerData.TravelStartSteps = 0;
            playerData.TravelRequiredSteps = 0;
            mapManager?.ClearTravelState();

            report += $"SUCCESS: Voyage repare (etait: {oldDest})\n";
            anyRepairs = true;
        }

        // Repair activity
        if (playerData.HasActiveActivity())
        {
            string oldActivity = $"{playerData.CurrentActivity.ActivityId}/{playerData.CurrentActivity.VariantId}";
            playerData.StopActivity();

            report += $"SUCCESS: Activite reparee (etait: {oldActivity})\n";
            anyRepairs = true;
        }

        if (anyRepairs)
        {
            // Force save
            dataManager.ForceSave();
            report += "\nDonnees sauvegardees!";
            report += "\n\nTu peux maintenant:";
            report += "\n   • Voyager vers d'autres POI";
            report += "\n   • Commencer des activites";

            Debug.Log("RuntimeFixer: Full repair completed with changes");
        }
        else
        {
            report += "INFO: Aucune reparation necessaire\n";
            report += "Tout etait deja OK!";

            Debug.Log("RuntimeFixer: Full repair completed - no issues found");
        }

        UpdateStatusText(report);
    }

    private void UpdateStatusText(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }

        // Also log important messages
        if (text.Contains("SUCCESS") || text.Contains("PROBLEME"))
        {
            Debug.Log($"RuntimePlayerDataFixer: {text.Replace("\n", " | ")}");
        }
    }

    // Public method to be called from UI or other scripts
    public void ShowQuickFix()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(true);
            FullRepair(); // Immediate fix
        }
    }

    // Context menu for testing in editor
    [ContextMenu("Force Show Panel")]
    private void ForceShowPanel()
    {
        if (debugPanel != null)
        {
            debugPanel.SetActive(true);
            DiagnosePlayerState();
        }
    }
}