// Purpose: Popup window that shows travel details and confirms before starting travel
// Filepath: Assets/Scripts/UI/Panels/TravelConfirmationPopup.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TravelConfirmationPopup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI titleText; // NOUVEAU : Titre de la popup
    [SerializeField] private TextMeshProUGUI destinationNameText;
    [SerializeField] private TextMeshProUGUI destinationDescriptionText;
    [SerializeField] private TextMeshProUGUI travelCostText;
    [SerializeField] private TextMeshProUGUI currentLocationText;
    [SerializeField] private Button confirmTravelButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button closeButton; // Bouton X dans le header
    [SerializeField] private Button backgroundButton; // Bouton sur le background

    [Header("Optional Visual Elements")]
    [SerializeField] private Image destinationImage; // Optionnel : image de la destination
    [SerializeField] private Image routeVisualization; // Optionnel : mini-carte du trajet

    // References aux services
    private MapManager mapManager;
    private LocationRegistry locationRegistry;

    // État actuel
    private string pendingDestinationId;
    private MapManager.TravelInfo currentTravelInfo;

    public static TravelConfirmationPopup Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Ne pas utiliser DontDestroyOnLoad car ce popup fait partie de l'UI de la scène
        }
        else
        {
            Logger.LogWarning("TravelConfirmationPopup: Multiple instances detected! Destroying duplicate.", Logger.LogCategory.MapLog);
            Destroy(gameObject);
            return;
        }

        // S'assurer que le popup est cache au demarrage
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }

    void Start()
    {
        // Obtenir les references
        mapManager = MapManager.Instance;
        if (mapManager != null)
        {
            locationRegistry = mapManager.LocationRegistry;
        }

        // Configurer les boutons
        if (confirmTravelButton != null)
        {
            confirmTravelButton.onClick.AddListener(OnConfirmTravel);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelTravel);
        }

        // NOUVEAU : Configurer les boutons de fermeture
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePopup);
        }

        if (backgroundButton != null)
        {
            backgroundButton.onClick.AddListener(ClosePopup);
        }

        // Validation des references
        if (popupPanel == null)
        {
            Logger.LogError("TravelConfirmationPopup: popupPanel not assigned!", Logger.LogCategory.MapLog);
        }
        if (destinationNameText == null)
        {
            Logger.LogError("TravelConfirmationPopup: destinationNameText not assigned!", Logger.LogCategory.MapLog);
        }
        if (confirmTravelButton == null)
        {
            Logger.LogError("TravelConfirmationPopup: confirmTravelButton not assigned!", Logger.LogCategory.MapLog);
        }
    }

    /// <summary>
    /// Affiche la popup avec les details du voyage vers la destination specifiee
    /// </summary>
    /// <param name="destinationLocationId">ID de la location de destination</param>
    public void ShowTravelConfirmation(string destinationLocationId)
    {
        if (mapManager == null || locationRegistry == null)
        {
            Logger.LogError("TravelConfirmationPopup: MapManager or LocationRegistry not available!", Logger.LogCategory.MapLog);
            return;
        }

        // Obtenir les informations de voyage
        currentTravelInfo = mapManager.GetTravelInfo(destinationLocationId);

        if (currentTravelInfo == null || currentTravelInfo.To == null)
        {
            Logger.LogError($"TravelConfirmationPopup: Cannot get travel info for destination '{destinationLocationId}'", Logger.LogCategory.MapLog);
            return;
        }

        // Stocker l'ID de destination en attente
        pendingDestinationId = destinationLocationId;

        // Remplir les informations dans l'UI
        PopulateUI();

        // Afficher la popup
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
            Logger.LogInfo($"TravelConfirmationPopup: Showing travel confirmation for {currentTravelInfo.To.DisplayName}", Logger.LogCategory.MapLog);
        }
    }

    /// <summary>
    /// Remplit l'interface utilisateur avec les informations de voyage
    /// </summary>
    private void PopulateUI()
    {
        if (currentTravelInfo == null) return;

        // NOUVEAU : Titre de la popup
        if (titleText != null)
        {
            titleText.text = $"Voyage vers {currentTravelInfo.To.DisplayName}";
        }

        // Nom de la destination
        if (destinationNameText != null)
        {
            destinationNameText.text = currentTravelInfo.To.DisplayName;
        }

        // Description de la destination
        if (destinationDescriptionText != null)
        {
            string description = string.IsNullOrEmpty(currentTravelInfo.To.Description)
                ? "Une location mysterieuse vous attend..."
                : currentTravelInfo.To.Description;
            destinationDescriptionText.text = description;
        }

        // Coût du voyage
        if (travelCostText != null)
        {
            if (currentTravelInfo.StepCost > 0)
            {
                travelCostText.text = $"Coût: {currentTravelInfo.StepCost} pas";
                // Changer la couleur si le joueur n'a pas assez de pas (optionnel)
                // TODO: Implementer la verification des pas actuels vs coût
            }
            else
            {
                travelCostText.text = "Voyage impossible";
            }
        }

        // Location actuelle
        if (currentLocationText != null && currentTravelInfo.From != null)
        {
            currentLocationText.text = $"Depuis: {currentTravelInfo.From.DisplayName}";
        }

        // État du bouton de confirmation
        if (confirmTravelButton != null)
        {
            confirmTravelButton.interactable = currentTravelInfo.CanTravel;
        }

        // Image de destination (optionnel)
        if (destinationImage != null)
        {
            // TODO: Ajouter un système d'images pour les locations
            // destinationImage.sprite = GetLocationSprite(currentTravelInfo.To.LocationID);
        }
    }

    /// <summary>
    /// Appele quand le joueur confirme le voyage
    /// </summary>
    private void OnConfirmTravel()
    {
        if (string.IsNullOrEmpty(pendingDestinationId))
        {
            Logger.LogWarning("TravelConfirmationPopup: No pending destination when confirm clicked!", Logger.LogCategory.MapLog);
            return;
        }

        if (mapManager == null)
        {
            Logger.LogError("TravelConfirmationPopup: MapManager not available when confirming travel!", Logger.LogCategory.MapLog);
            return;
        }

        Logger.LogInfo($"TravelConfirmationPopup: Player confirmed travel to {pendingDestinationId}", Logger.LogCategory.MapLog);

        // Demarrer le voyage via le MapManager
        mapManager.StartTravel(pendingDestinationId);

        // Fermer la popup
        HidePopup();
    }

    /// <summary>
    /// Appele quand le joueur annule le voyage
    /// </summary>
    private void OnCancelTravel()
    {
        Logger.LogInfo("TravelConfirmationPopup: Player cancelled travel", Logger.LogCategory.MapLog);
        HidePopup();
    }

    /// <summary>
    /// Cache la popup et nettoie l'etat
    /// </summary>
    public void HidePopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        // Nettoyer l'etat
        pendingDestinationId = null;
        currentTravelInfo = null;

        Logger.LogInfo("TravelConfirmationPopup: Popup hidden", Logger.LogCategory.MapLog);
    }

    /// <summary>
    /// Methode publique pour fermer la popup depuis l'exterieur (par exemple, via un bouton X)
    /// </summary>
    public void ClosePopup()
    {
        OnCancelTravel();
    }

    // Optionnel : Gerer la fermeture avec la touche Escape ou retour Android
    void Update()
    {
        if (popupPanel != null && popupPanel.activeSelf)
        {
            // Fermer avec Escape sur PC ou bouton retour sur Android
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
            {
                OnCancelTravel();
            }
        }
    }

    // Optionnel : Methodes pour personnaliser l'apparence
    public void SetPopupTheme(Color backgroundColor, Color textColor)
    {
        // TODO: Implementer la personnalisation de thème si necessaire
    }

    // Debug : Methode pour tester la popup depuis l'editeur
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestShowPopup(string locationId)
    {
        ShowTravelConfirmation(locationId);
    }
}