// Panel d'affichage des détails d'une location
// Chemin: Assets/Scripts/UI/Panels/LocationDetailsPanel.cs
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocationDetailsPanel : MonoBehaviour
{
    #region Variables Serialized

    [Header("Interface - En-tête")]
    [SerializeField] private TextMeshProUGUI locationNameText;
    [SerializeField] private Image locationImage;
    [SerializeField] private Button closeButton;

    [Header("Interface - Contenu")]
    [SerializeField] private TextMeshProUGUI locationDescriptionText;
    [SerializeField] private ScrollRect descriptionScrollRect;

    [Header("Interface - Section Activités")]
    [SerializeField] private GameObject activitiesSection;
    [SerializeField] private TextMeshProUGUI activitiesSectionTitle;
    [SerializeField] private Transform activitiesContainer;
    [SerializeField] private GameObject activityItemPrefab;
    [SerializeField] private TextMeshProUGUI noActivitiesText;

    [Header("Interface - Section Infos")]
    [SerializeField] private TextMeshProUGUI locationInfoText;

    [Header("Interface - HeroCard")]
    [SerializeField] private GameObject heroCard; // La carte principale avec le contenu
    [SerializeField] private Image heroCardBackground; // L'image de fond de la HeroCard

    [Header("Paramètres")]
    [SerializeField] private Color defaultImageColor = Color.gray;

    [Header("Paramètres Animation")]
    [SerializeField] private float animationDuration = 0.2f; // Durée de l'animation en secondes
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Courbe d'animation
    [SerializeField] private float slidePixels = 50f; // Distance en pixels pour l'animation slide

    [Header("Paramètres Ombre")]
    [SerializeField] private bool enableShadow = true; // Activer/désactiver l'ombre
    [SerializeField] private Vector2 shadowOffset = new Vector2(5, -5); // Décalage de l'ombre
    [SerializeField] private Color shadowColor = new Color(0, 0, 0, 0.3f); // Couleur de l'ombre

    #endregion

    #region Variables Privées

    // Références vers les managers
    private MapManager mapManager;
    private DataManager dataManager;
    private PanelManager panelManager;

    // État actuel
    private MapLocationDefinition currentLocation;
    private List<GameObject> instantiatedActivityItems = new List<GameObject>();
    private Queue<GameObject> activityItemPool = new Queue<GameObject>();

    // Animation et effets visuels
    private bool isAnimating = false;
    private Vector3 originalPosition;
    private Shadow heroCardShadow; // Composant Shadow de Unity

    #endregion

    #region Singleton

    public static LocationDetailsPanel Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Stocker la position originale pour l'animation
            if (heroCard != null)
            {
                originalPosition = heroCard.transform.localPosition;
            }
        }
        else
        {
            Logger.LogWarning("LocationDetailsPanel: Instance multiple détectée ! Destruction du doublon.", Logger.LogCategory.General);
            Destroy(gameObject);
        }
    }

    #endregion

    #region Cycle de Vie Unity

    void Start()
    {
        InitializeReferences();
        SetupEventSubscriptions();
        SetupCloseButton();
        SetupShadowEffect();
        ValidateRequiredReferences();
        RefreshPanel();
    }

    void OnEnable()
    {
        CheckActivityDisplayPanel();

        // Reset animation state au cas où elle aurait été interrompue
        ResetAnimationState();

        // Refresh immédiatement AVANT l'animation pour éviter le texte corrompu
        RefreshPanel();

        // Puis lancer l'animation
        StartCoroutine(PlayOpenAnimation());
    }

    void OnDisable()
    {
        // Reset l'état d'animation quand le panel se ferme
        ResetAnimationState();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #endregion

    #region Initialisation

    /// <summary>
    /// Récupère les références vers les managers principaux
    /// </summary>
    private void InitializeReferences()
    {
        mapManager = MapManager.Instance;
        dataManager = DataManager.Instance;
        panelManager = PanelManager.Instance;
    }

    /// <summary>
    /// S'abonne aux événements du MapManager pour les mises à jour automatiques
    /// </summary>
    private void SetupEventSubscriptions()
    {
        if (mapManager != null)
        {
            mapManager.OnLocationChanged += OnLocationChanged;
            mapManager.OnTravelCompleted += OnTravelCompleted;
            mapManager.OnTravelStarted += OnTravelStarted;
            mapManager.OnTravelProgress += OnTravelProgress;
        }
    }

    /// <summary>
    /// Configure le bouton de fermeture
    /// </summary>
    private void SetupCloseButton()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => StartCoroutine(PlayCloseAnimation()));
        }
    }

    /// <summary>
    /// Configure l'effet d'ombre sur la HeroCard
    /// </summary>
    private void SetupShadowEffect()
    {
        if (!enableShadow || heroCardBackground == null) return;

        // Vérifier si un composant Shadow existe déjà
        heroCardShadow = heroCardBackground.GetComponent<Shadow>();

        // Si pas de Shadow, en ajouter un
        if (heroCardShadow == null)
        {
            heroCardShadow = heroCardBackground.gameObject.AddComponent<Shadow>();
        }

        // Configurer les propriétés de l'ombre
        heroCardShadow.effectColor = shadowColor;
        heroCardShadow.effectDistance = shadowOffset;
        heroCardShadow.useGraphicAlpha = true; // Utilise la transparence de l'image

        Logger.LogInfo("LocationDetailsPanel: Effet d'ombre configuré sur la HeroCard", Logger.LogCategory.General);
    }

    /// <summary>
    /// Se désabonne des événements pour éviter les erreurs
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (mapManager != null)
        {
            mapManager.OnLocationChanged -= OnLocationChanged;
            mapManager.OnTravelCompleted -= OnTravelCompleted;
            mapManager.OnTravelStarted -= OnTravelStarted;
            mapManager.OnTravelProgress -= OnTravelProgress;
        }
    }

    #endregion

    #region Animations

    /// <summary>
    /// Remet l'état d'animation à zéro et reposition la HeroCard
    /// </summary>
    private void ResetAnimationState()
    {
        isAnimating = false;

        // Remettre la HeroCard à sa position normale
        if (heroCard != null)
        {
            heroCard.transform.localPosition = originalPosition;
        }
    }

    /// <summary>
    /// Joue l'animation d'ouverture du panel (slide up)
    /// </summary>
    private IEnumerator PlayOpenAnimation()
    {
        if (heroCard == null) yield break;

        // Si on était déjà en train d'animer, on force le reset
        if (isAnimating)
        {
            ResetAnimationState();
        }

        isAnimating = true;

        try
        {
            // Position de départ (plus bas selon slidePixels)
            Vector3 startPosition = originalPosition + Vector3.down * slidePixels;
            heroCard.transform.localPosition = startPosition;

            // Animer vers la position normale
            yield return StartCoroutine(AnimatePosition(startPosition, originalPosition, animationDuration));

            // Corriger le layout après l'animation
            StartCoroutine(FixLayoutAfterDelay());
        }
        finally
        {
            // S'assurer que l'état est toujours reset même en cas d'erreur
            isAnimating = false;
        }
    }

    /// <summary>
    /// Joue l'animation de fermeture du panel (slide down)
    /// </summary>
    private IEnumerator PlayCloseAnimation()
    {
        if (heroCard == null)
        {
            ClosePanel();
            yield break;
        }

        // Si on était déjà en train d'animer, on force le reset
        if (isAnimating)
        {
            ResetAnimationState();
        }

        isAnimating = true;

        try
        {
            // Position de fin (plus bas selon slidePixels * 0.6 pour être plus rapide)
            Vector3 endPosition = originalPosition + Vector3.down * (slidePixels * 0.6f);

            // Animer vers la position de sortie
            yield return StartCoroutine(AnimatePosition(originalPosition, endPosition, animationDuration * 0.6f));
        }
        finally
        {
            // S'assurer que l'état est toujours reset même en cas d'erreur
            isAnimating = false;
            ClosePanel();
        }
    }

    /// <summary>
    /// Anime la position d'un objet entre deux valeurs
    /// </summary>
    private IEnumerator AnimatePosition(Vector3 fromPosition, Vector3 toPosition, float duration)
    {
        if (heroCard == null) yield break;

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            float easedProgress = animationCurve.Evaluate(progress);

            heroCard.transform.localPosition = Vector3.Lerp(fromPosition, toPosition, easedProgress);
            yield return null;
        }

        heroCard.transform.localPosition = toPosition;
    }

    #endregion

    #region Gestion des Événements

    private void OnLocationChanged(MapLocationDefinition newLocation)
    {
        if (gameObject.activeInHierarchy && !isAnimating)
        {
            // Si on n'est pas en train d'animer, on peut refresh normalement
            StartCoroutine(RefreshPanelSmoothly());
        }
        else if (gameObject.activeInHierarchy && isAnimating)
        {
            // Si on anime, attendre la fin de l'animation avant de refresh
            StartCoroutine(WaitForAnimationThenRefresh());
        }
    }

    private void OnTravelCompleted(string arrivedLocationId)
    {
        if (gameObject.activeInHierarchy && !isAnimating)
        {
            StartCoroutine(RefreshPanelSmoothly());
        }
        else if (gameObject.activeInHierarchy && isAnimating)
        {
            StartCoroutine(WaitForAnimationThenRefresh());
        }
    }

    private void OnTravelStarted(string destinationId)
    {
        if (gameObject.activeInHierarchy && !isAnimating)
        {
            StartCoroutine(RefreshPanelSmoothly());
        }
        else if (gameObject.activeInHierarchy && isAnimating)
        {
            StartCoroutine(WaitForAnimationThenRefresh());
        }
    }

    private void OnTravelProgress(string destinationId, int currentSteps, int requiredSteps)
    {
        if (gameObject.activeInHierarchy && !isAnimating)
        {
            // Mise à jour seulement de la section info pour éviter de tout recalculer
            UpdateInfoSection();
        }
    }

    #endregion

    #region Méthodes Publiques

    /// <summary>
    /// Ouvre le panel et affiche la location actuelle
    /// </summary>
    public void OpenLocationDetails()
    {
        if (mapManager?.CurrentLocation == null)
        {
            Logger.LogWarning("LocationDetailsPanel: Impossible d'ouvrir - aucune location actuelle !", Logger.LogCategory.General);
            return;
        }

        Logger.LogInfo($"LocationDetailsPanel: Ouverture des détails pour {mapManager.CurrentLocation.DisplayName}", Logger.LogCategory.General);
        RefreshPanel();
    }

    /// <summary>
    /// Actualise le panel avec les données de la location actuelle
    /// </summary>
    public void RefreshPanel()
    {
        if (mapManager?.CurrentLocation == null)
        {
            ShowNoLocationMessage();
            return;
        }

        currentLocation = mapManager.CurrentLocation;
        UpdateAllSections();
    }

    #endregion

    #region Mise à Jour de l'Interface

    /// <summary>
    /// Actualise le panel sans effet de clignotement
    /// </summary>
    private IEnumerator RefreshPanelSmoothly()
    {
        // Ne pas actualiser pendant une animation
        if (isAnimating) yield break;

        // Actualiser le contenu directement sans masquer/afficher
        // pour éviter les conflits avec l'animation
        RefreshPanel();

        // Attendre que Unity recalcule tout
        yield return null;
        Canvas.ForceUpdateCanvases();

        // Corriger le ScrollRect
        FixScrollRect();
    }

    /// <summary>
    /// Attend la fin de l'animation puis actualise le panel
    /// </summary>
    private IEnumerator WaitForAnimationThenRefresh()
    {
        // Attendre que l'animation se termine
        while (isAnimating)
        {
            yield return null;
        }

        // Maintenant on peut actualiser en sécurité
        RefreshPanel();
        yield return null;
        Canvas.ForceUpdateCanvases();
        FixScrollRect();
    }

    /// <summary>
    /// Met à jour toutes les sections du panel
    /// </summary>
    private void UpdateAllSections()
    {
        if (currentLocation == null) return;

        UpdateHeaderSection();
        UpdateDescriptionSection();
        UpdateActivitiesSection();
        UpdateInfoSection();
    }

    /// <summary>
    /// Met à jour l'en-tête (nom et image)
    /// </summary>
    private void UpdateHeaderSection()
    {
        // Nom de la location
        if (locationNameText != null)
        {
            locationNameText.text = currentLocation.DisplayName;
        }

        // Image de la location
        UpdateLocationImage();
    }

    /// <summary>
    /// Met à jour l'image de la location
    /// </summary>
    private void UpdateLocationImage()
    {
        if (locationImage == null) return;

        if (currentLocation.LocationImage != null)
        {
            locationImage.sprite = currentLocation.LocationImage;
            locationImage.color = Color.white;
        }
        else
        {
            locationImage.sprite = null;
            locationImage.color = defaultImageColor;
        }
    }

    /// <summary>
    /// Met à jour la description de la location
    /// </summary>
    private void UpdateDescriptionSection()
    {
        if (locationDescriptionText != null)
        {
            locationDescriptionText.text = currentLocation.GetBestDescription();
        }
    }

    /// <summary>
    /// Met à jour la section des activités
    /// </summary>
    private void UpdateActivitiesSection()
    {
        // Nettoyer les éléments existants
        RecycleActivityItems();

        if (activitiesSection == null || currentLocation == null) return;

        var availableActivities = currentLocation.GetAvailableActivities();

        // Mettre à jour le titre de la section
        if (activitiesSectionTitle != null)
        {
            activitiesSectionTitle.text = $"Activités disponibles ({availableActivities.Count})";
        }

        // Afficher ou masquer selon la disponibilité
        if (availableActivities.Count == 0)
        {
            ShowNoActivitiesMessage();
        }
        else
        {
            HideNoActivitiesMessage();
            CreateActivityItems(availableActivities);
        }
    }

    /// <summary>
    /// Met à jour la section d'informations supplémentaires
    /// </summary>
    private void UpdateInfoSection()
    {
        if (locationInfoText == null || currentLocation == null) return;

        List<string> infoLines = new List<string>();

        // Informations sur les connexions
        if (currentLocation.Connections != null && currentLocation.Connections.Count > 0)
        {
            infoLines.Add($"Connexions: {currentLocation.Connections.Count} destination(s)");
        }

        // Résumé des activités
        infoLines.Add(currentLocation.GetActivitiesSummary());

        // Informations de voyage si en cours
        AddTravelInfo(infoLines);

        locationInfoText.text = string.Join("\n", infoLines);
    }

    #endregion

    #region Gestion des Activités

    /// <summary>
    /// Crée les éléments d'interface pour chaque activité
    /// </summary>
    private void CreateActivityItems(List<LocationActivity> activities)
    {
        if (activitiesContainer == null || activityItemPrefab == null) return;

        foreach (var activity in activities)
        {
            GameObject item = GetPooledActivityItem();
            if (item.transform.parent != activitiesContainer)
            {
                item.transform.SetParent(activitiesContainer, false);
            }

            instantiatedActivityItems.Add(item);
            SetupActivityItem(item, activity);
        }
    }

    /// <summary>
    /// Configure un élément d'activité individuel
    /// </summary>
    private void SetupActivityItem(GameObject item, LocationActivity activity)
    {
        if (item == null || activity == null) return;

        // Récupérer les composants de l'élément d'activité
        var nameText = item.GetComponentInChildren<TextMeshProUGUI>();
        var iconImage = item.GetComponentInChildren<Image>();
        var button = item.GetComponent<Button>();

        // Configurer le nom de l'activité
        if (nameText != null)
        {
            nameText.text = activity.GetDisplayName();
        }

        // Configurer l'icône de l'activité
        if (iconImage != null && activity.GetIcon() != null)
        {
            iconImage.sprite = activity.GetIcon();
        }

        // Configurer le gestionnaire de clic
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnActivityClicked(activity));
        }

        // Configuration additionnelle (tooltip, etc.)
        SetupActivityItemTooltip(item, activity);
    }

    /// <summary>
    /// Configure le tooltip pour un élément d'activité
    /// </summary>
    private void SetupActivityItemTooltip(GameObject item, LocationActivity activity)
    {
        var descTexts = item.GetComponentsInChildren<TextMeshProUGUI>();
        if (descTexts.Length > 1)
        {
            descTexts[1].text = activity.GetResourcesText();
        }
    }

    /// <summary>
    /// Gère le clic sur un élément d'activité
    /// </summary>
    private void OnActivityClicked(LocationActivity activity)
    {
        if (ActivityVariantsPanel.Instance != null)
        {
            ActivityVariantsPanel.Instance.OpenWithActivity(activity);
        }
        else
        {
            Debug.LogWarning("ActivityVariantsPanel introuvable !");
        }
    }

    #endregion

    #region Gestion du Pool d'Objets

    /// <summary>
    /// Recycle tous les éléments d'activité instantiés
    /// </summary>
    private void RecycleActivityItems()
    {
        foreach (var item in instantiatedActivityItems)
        {
            if (item != null)
            {
                item.SetActive(false);
                activityItemPool.Enqueue(item);
            }
        }
        instantiatedActivityItems.Clear();
    }

    /// <summary>
    /// Récupère un élément d'activité du pool ou en crée un nouveau
    /// </summary>
    private GameObject GetPooledActivityItem()
    {
        if (activityItemPool.Count > 0)
        {
            var item = activityItemPool.Dequeue();
            item.SetActive(true);
            return item;
        }

        return Instantiate(activityItemPrefab, activitiesContainer);
    }

    #endregion

    #region Méthodes Utilitaires

    /// <summary>
    /// Affiche un message quand aucune location n'est disponible
    /// </summary>
    private void ShowNoLocationMessage()
    {
        if (locationNameText != null)
        {
            locationNameText.text = "Aucune location";
        }

        if (locationDescriptionText != null)
        {
            locationDescriptionText.text = "Aucune information de location disponible.";
        }
    }

    /// <summary>
    /// Affiche le message "aucune activité"
    /// </summary>
    private void ShowNoActivitiesMessage()
    {
        if (noActivitiesText != null)
        {
            noActivitiesText.gameObject.SetActive(true);
            noActivitiesText.text = "Aucune activité disponible dans cette location.";
        }
    }

    /// <summary>
    /// Masque le message "aucune activité"
    /// </summary>
    private void HideNoActivitiesMessage()
    {
        if (noActivitiesText != null)
        {
            noActivitiesText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Corrige les problèmes de layout du ScrollRect
    /// </summary>
    private void FixScrollRect()
    {
        if (descriptionScrollRect != null && descriptionScrollRect.content != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(descriptionScrollRect.content);
            descriptionScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    /// <summary>
    /// Ajoute les informations de voyage à la liste d'infos
    /// </summary>
    private void AddTravelInfo(List<string> infoLines)
    {
        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            string destination = dataManager.PlayerData.TravelDestinationId;
            long progress = dataManager.PlayerData.GetTravelProgress(dataManager.PlayerData.TotalSteps);
            int required = dataManager.PlayerData.TravelRequiredSteps;
            infoLines.Add($"En voyage vers {destination}: {progress}/{required} pas");
        }
    }

    /// <summary>
    /// Vérifie et affiche le panel d'activité si nécessaire
    /// </summary>
    private void CheckActivityDisplayPanel()
    {
        if (ActivityDisplayPanel.Instance != null)
        {
            ActivityDisplayPanel.Instance.CheckAndShowIfActivityActive();
        }
    }

    /// <summary>
    /// Corrige le layout après un délai
    /// </summary>
    private IEnumerator FixLayoutAfterDelay()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        FixScrollRect();
    }

    /// <summary>
    /// Ferme le panel (méthode privée, appelée après l'animation)
    /// </summary>
    private void ClosePanel()
    {
        if (panelManager != null)
        {
            Logger.LogInfo("LocationDetailsPanel: Demande de retour au panel précédent", Logger.LogCategory.General);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Valide que toutes les références UI requises sont assignées
    /// </summary>
    private void ValidateRequiredReferences()
    {
        List<string> missing = new List<string>();

        if (locationNameText == null) missing.Add("locationNameText");
        if (locationDescriptionText == null) missing.Add("locationDescriptionText");
        if (activitiesContainer == null) missing.Add("activitiesContainer");
        if (heroCard == null) missing.Add("heroCard");

        if (missing.Count > 0)
        {
            Logger.LogWarning($"LocationDetailsPanel: Références UI manquantes: {string.Join(", ", missing)}", Logger.LogCategory.General);
        }
    }

    #endregion

    #region Méthodes de Debug et Contrôles Publics

    /// <summary>
    /// Force l'actualisation du panel (utile pour le debug)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void ForceRefresh()
    {
        RefreshPanel();
    }

    /// <summary>
    /// Permet de modifier les paramètres d'ombre en temps réel
    /// </summary>
    public void UpdateShadowSettings(Vector2 newOffset, Color newColor)
    {
        shadowOffset = newOffset;
        shadowColor = newColor;

        if (heroCardShadow != null)
        {
            heroCardShadow.effectDistance = shadowOffset;
            heroCardShadow.effectColor = shadowColor;
        }
    }

    /// <summary>
    /// Permet de tester l'animation d'ouverture manuellement
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestOpenAnimation()
    {
        if (Application.isPlaying)
        {
            StartCoroutine(PlayOpenAnimation());
        }
    }

    #endregion
}