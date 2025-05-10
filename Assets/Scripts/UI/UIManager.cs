// Purpose: Main UI coordinator, handles switching between major panels/screens.
// Filepath: Assets/Scripts/UI/UIManager.cs
// using System.Collections.Generic; // Potential dependency for managing panels
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    // TODO: References to all major UI Panel GameObjects (assign in Inspector)
    // public GameObject mapPanel;
    // public GameObject characterPanel;
    // public GameObject inventoryPanel;
    // public GameObject combatPanel;
    // public GameObject craftingPanel;
    // public GameObject questLogPanel;
    // public GameObject affinityPanel;
    // public GameObject dialoguePanel;
    // ... add other panels (Settings, Shop, etc.)

    // TODO: Store the currently active panel
    // private GameObject currentActivePanel;

    public static UIManager Instance { get; private set; }


    [SerializeField] private TextMeshProUGUI pasDuJour;
    [SerializeField] private TextMeshProUGUI pasTotaux;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //  DontDestroyOnLoad(gameObject); // Keep this instance across scenes
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate instance
        }

        UpdateTodaysStepsDisplay(0);
        UpdateTotalPlayerStepsDisplay(0);

    }
    void Start()
    {
        // TODO: Ensure all panels are initially hidden except the default one (e.g., MapPanel)
        // ShowPanel(mapPanel); // Example starting panel
    }

    public void ShowPanel(GameObject panelToShow)
    {
        // TODO: Check if panelToShow is valid
        // TODO: Hide the currentActivePanel if it exists
        // if (currentActivePanel != null) { currentActivePanel.SetActive(false); }
        // TODO: Show the panelToShow
        // panelToShow.SetActive(true);
        // TODO: Update currentActivePanel reference
        // currentActivePanel = panelToShow;
        Debug.Log($"UIManager: Showing panel {panelToShow?.name} (Placeholder)");
    }

    public void UpdateTotalPlayerStepsDisplay(long steps)
    {
        if (pasTotaux != null)
        {
            pasTotaux.text = $"Pas depuis la création de l'application : {steps}";
        }
        else
        {
            Debug.LogError("UIManager: pasTotaux TextMeshProUGUI reference is not set.");
        }
    }

    public void UpdateTodaysStepsDisplay(long steps)
    {
        if (pasDuJour != null)
        {
            pasDuJour.text = $"Nombre de pas aujourd'hui : {steps}";
        }
        else
        {
            Debug.LogError("UIManager: pasDuJour TextMeshProUGUI reference is not set.");
        }
    }
}