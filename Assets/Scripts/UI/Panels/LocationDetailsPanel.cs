﻿// Panel d'affichage des details d'une location
// Chemin: Assets/Scripts/UI/Panels/LocationDetailsPanel.cs
using MapEvents; // NOUVEAU: Import pour EventBus
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LocationDetailsPanel : MonoBehaviour
{
    #region Variables Serialized

    [Header("Interface - En-tête")]
    [SerializeField] private Image locationImage;

    [Header("Interface - Contenu")]
    [SerializeField] private TextMeshProUGUI locationDescriptionText;
    [SerializeField] private ScrollRect descriptionScrollRect;

    [Header("Interface - Section Activites - DELEGUe")]
    [SerializeField] private ActivitiesSectionPanel activitiesSectionPanel; // Reference vers le nouveau panel

    [Header("Interface - Section Infos")]
    [SerializeField] private TextMeshProUGUI locationInfoText;

    [Header("Interface - HeroCard")]
    [SerializeField] private GameObject heroCard; // La carte principale avec le contenu
    [SerializeField] private Image heroCardBackground; // L'image de fond de la HeroCard

    [Header("Paramètres")]
    [SerializeField] private Color defaultImageColor = Color.gray;

    [Header("Paramètres Animation")]
    [SerializeField] private float animationDuration = 0.2f; // Duree de l'animation en secondes
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // Courbe d'animation
    [SerializeField] private float slidePixels = 50f; // Distance en pixels pour l'animation slide

    [Header("Paramètres Ombre")]
    [SerializeField] private bool enableShadow = true; // Activer/desactiver l'ombre
    [SerializeField] private Vector2 shadowOffset = new Vector2(5, -5); // Decalage de l'ombre
    [SerializeField] private Color shadowColor = new Color(0, 0, 0, 0.3f); // Couleur de l'ombre

    #endregion

    #region Variables Privees

    // References vers les managers
    private MapManager mapManager;
    private DataManager dataManager;
    private PanelManager panelManager;

    // etat actuel
    private MapLocationDefinition currentLocation;

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
            Logger.LogWarning("LocationDetailsPanel: Instance multiple detectee ! Destruction du doublon.", Logger.LogCategory.General);
            Destroy(gameObject);
        }
    }

    #endregion

    #region Cycle de Vie Unity

    void Start()
    {
        InitializeReferences();
        SetupEventSubscriptions();
        SetupShadowEffect();
        ValidateRequiredReferences();
        RefreshPanel();
    }

    void OnEnable()
    {
        // Reset animation state au cas où elle aurait ete interrompue
        ResetAnimationState();

        // Refresh immediatement AVANT l'animation pour eviter le texte corrompu
        RefreshPanel();

        // Puis lancer l'animation
        StartCoroutine(PlayOpenAnimation());
    }

    void OnDisable()
    {
        // Reset l'etat d'animation quand le panel se ferme
        ResetAnimationState();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #endregion

    #region Initialisation

    /// <summary>
    /// Recupère les references vers les managers principaux
    /// </summary>
    private void InitializeReferences()
    {
        mapManager = MapManager.Instance;
        dataManager = DataManager.Instance;
        panelManager = PanelManager.Instance;
    }

    /// <summary>
    /// S'abonne aux evenements via EventBus au lieu du MapManager directement
    /// </summary>
    private void SetupEventSubscriptions()
    {
        EventBus.Subscribe<LocationChangedEvent>(OnLocationChanged);
        EventBus.Subscribe<TravelCompletedEvent>(OnTravelCompleted);
        EventBus.Subscribe<TravelStartedEvent>(OnTravelStarted);
        EventBus.Subscribe<TravelProgressEvent>(OnTravelProgress);

        Logger.LogInfo("LocationDetailsPanel: Subscribed to EventBus events", Logger.LogCategory.General);
    }

    /// <summary>
    /// Configure l'effet d'ombre sur la HeroCard
    /// </summary>
    private void SetupShadowEffect()
    {
        if (!enableShadow || heroCardBackground == null) return;

        // Verifier si un composant Shadow existe deja
        heroCardShadow = heroCardBackground.GetComponent<Shadow>();

        // Si pas de Shadow, en ajouter un
        if (heroCardShadow == null)
        {
            heroCardShadow = heroCardBackground.gameObject.AddComponent<Shadow>();
        }

        // Configurer les proprietes de l'ombre
        heroCardShadow.effectColor = shadowColor;
        heroCardShadow.effectDistance = shadowOffset;
        heroCardShadow.useGraphicAlpha = true; // Utilise la transparence de l'image

        Logger.LogInfo("LocationDetailsPanel: Effet d'ombre configure sur la HeroCard", Logger.LogCategory.General);
    }

    /// <summary>
    /// Se desabonne des evenements pour eviter les erreurs
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        EventBus.Unsubscribe<LocationChangedEvent>(OnLocationChanged);
        EventBus.Unsubscribe<TravelCompletedEvent>(OnTravelCompleted);
        EventBus.Unsubscribe<TravelStartedEvent>(OnTravelStarted);
        EventBus.Unsubscribe<TravelProgressEvent>(OnTravelProgress);

        Logger.LogInfo("LocationDetailsPanel: Unsubscribed from EventBus events", Logger.LogCategory.General);
    }

    /// <summary>
    /// Valide que toutes les references necessaires sont assignees
    /// </summary>
    private void ValidateRequiredReferences()
    {
        bool hasErrors = false;

        if (heroCard == null)
        {
            Logger.LogError("LocationDetailsPanel: HeroCard n'est pas assigne !", Logger.LogCategory.General);
            hasErrors = true;
        }

        if (locationDescriptionText == null)
        {
            Logger.LogError("LocationDetailsPanel: LocationDescriptionText n'est pas assigne !", Logger.LogCategory.General);
            hasErrors = true;
        }

        if (activitiesSectionPanel == null)
        {
            Logger.LogError("LocationDetailsPanel: ActivitiesSectionPanel n'est pas assigne !", Logger.LogCategory.General);
            hasErrors = true;
        }

        if (hasErrors)
        {
            Logger.LogError("LocationDetailsPanel: Des references critiques manquent ! Le panel peut ne pas fonctionner correctement.", Logger.LogCategory.General);
        }
    }

    #endregion

    #region Gestion des evenements - ADAPTeE POUR EVENTBUS

    private void OnLocationChanged(LocationChangedEvent eventData)
    {
        Logger.LogInfo($"LocationDetailsPanel: Location changed from {eventData.PreviousLocation?.DisplayName ?? "None"} to {eventData.NewLocation?.DisplayName ?? "None"}", Logger.LogCategory.General);

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

    private void OnTravelCompleted(TravelCompletedEvent eventData)
    {
        Logger.LogInfo($"LocationDetailsPanel: Travel completed to {eventData.NewLocation?.DisplayName ?? "Unknown"} ({eventData.StepsTaken} steps)", Logger.LogCategory.General);

        if (gameObject.activeInHierarchy && !isAnimating)
        {
            StartCoroutine(RefreshPanelSmoothly());
        }
        else if (gameObject.activeInHierarchy && isAnimating)
        {
            StartCoroutine(WaitForAnimationThenRefresh());
        }
    }

    private void OnTravelStarted(TravelStartedEvent eventData)
    {
        Logger.LogInfo($"LocationDetailsPanel: Travel started from {eventData.CurrentLocation?.DisplayName ?? "Unknown"} to {eventData.DestinationLocationId} ({eventData.RequiredSteps} steps)", Logger.LogCategory.General);

        if (gameObject.activeInHierarchy && !isAnimating)
        {
            StartCoroutine(RefreshPanelSmoothly());
        }
        else if (gameObject.activeInHierarchy && isAnimating)
        {
            StartCoroutine(WaitForAnimationThenRefresh());
        }
    }

    private void OnTravelProgress(TravelProgressEvent eventData)
    {
        Logger.LogInfo($"LocationDetailsPanel: Travel progress {eventData.CurrentSteps}/{eventData.RequiredSteps} to {eventData.DestinationLocationId} ({eventData.ProgressPercentage:F1}%)", Logger.LogCategory.General);

        if (gameObject.activeInHierarchy && !isAnimating)
        {
            // Mise a jour seulement de la section info pour eviter de tout recalculer
            UpdateInfoSection();
        }
    }

    #endregion

    #region Animations

    /// <summary>
    /// Remet l'etat d'animation a zero et reposition la HeroCard
    /// </summary>
    private void ResetAnimationState()
    {
        isAnimating = false;

        // Remettre la HeroCard a sa position normale
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

        // Si on etait deja en train d'animer, on force le reset
        if (isAnimating)
        {
            ResetAnimationState();
        }

        isAnimating = true;

        try
        {
            // Position de depart (plus bas selon slidePixels)
            Vector3 startPosition = originalPosition + Vector3.down * slidePixels;
            heroCard.transform.localPosition = startPosition;

            // Animation vers la position finale
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / animationDuration;

                // Appliquer la courbe d'animation
                float easedProgress = animationCurve.Evaluate(progress);

                // Interpoler la position
                Vector3 currentPosition = Vector3.Lerp(startPosition, originalPosition, easedProgress);
                heroCard.transform.localPosition = currentPosition;

                yield return null;
            }

            // S'assurer qu'on termine exactement a la bonne position
            heroCard.transform.localPosition = originalPosition;
        }
        finally
        {
            isAnimating = false;
        }
    }

    /// <summary>
    /// Attendre la fin de l'animation puis rafraîchir
    /// </summary>
    private IEnumerator WaitForAnimationThenRefresh()
    {
        yield return new WaitUntil(() => !isAnimating);
        yield return RefreshPanelSmoothly();
    }

    #endregion

    #region Methodes Publiques

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

        gameObject.SetActive(true);
        Logger.LogInfo($"LocationDetailsPanel: Ouverture pour {mapManager.CurrentLocation.DisplayName}", Logger.LogCategory.General);
    }

    /// <summary>
    /// Ferme le panel
    /// </summary>
    public void ClosePanel()
    {
        gameObject.SetActive(false);
        Logger.LogInfo("LocationDetailsPanel: Panel ferme", Logger.LogCategory.General);
    }

    /// <summary>
    /// Force une mise a jour du contenu du panel
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
        Logger.LogInfo($"LocationDetailsPanel: Panel rafraîchi pour {currentLocation.DisplayName}", Logger.LogCategory.General);
    }

    #endregion

    #region Methodes Privees - Mise a jour du contenu

    /// <summary>
    /// Version smooth du refresh pour eviter les saccades
    /// </summary>
    private IEnumerator RefreshPanelSmoothly()
    {
        yield return null;
        RefreshPanel();
        yield return null;
        Canvas.ForceUpdateCanvases();
        FixScrollRect();
    }

    /// <summary>
    /// Met a jour toutes les sections du panel
    /// </summary>
    private void UpdateAllSections()
    {
        if (currentLocation == null) return;

        UpdateHeaderSection();
        UpdateDescriptionSection();
        UpdateActivitiesSection(); // DeLeGUe au ActivitiesSectionPanel
        UpdateInfoSection();
    }

    /// <summary>
    /// Met a jour l'en-tête (nom et image)
    /// </summary>
    private void UpdateHeaderSection()
    {
        // Image de la location
        UpdateLocationImage();
    }

    /// <summary>
    /// Met a jour l'image de la location
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
    /// Met a jour la description de la location
    /// </summary>
    private void UpdateDescriptionSection()
    {
        if (locationDescriptionText != null)
        {
            locationDescriptionText.text = currentLocation.GetBestDescription();
        }
    }

    /// <summary>
    /// Met a jour la section des activites - DeLeGUe au ActivitiesSectionPanel
    /// </summary>
    private void UpdateActivitiesSection()
    {
        if (activitiesSectionPanel == null || currentLocation == null) return;

        // Recuperer les LocationActivity depuis la location
        var availableLocationActivities = currentLocation.GetAvailableActivities();

        // Convertir en ActivityDefinition pour le ActivitiesSectionPanel
        var activityDefinitions = ConvertToActivityDefinitions(availableLocationActivities);

        activitiesSectionPanel.DisplayActivities(activityDefinitions);

        // S'abonner a l'evenement de selection d'activite
        activitiesSectionPanel.OnActivitySelected -= OnActivitySelected;
        activitiesSectionPanel.OnActivitySelected += OnActivitySelected;
    }

    /// <summary>
    /// Convertit les LocationActivity en ActivityDefinition
    /// </summary>
    private List<ActivityDefinition> ConvertToActivityDefinitions(List<LocationActivity> locationActivities)
    {
        var activityDefinitions = new List<ActivityDefinition>();

        foreach (var locationActivity in locationActivities)
        {
            if (locationActivity != null && locationActivity.ActivityReference != null)
            {
                activityDefinitions.Add(locationActivity.ActivityReference);
            }
        }

        return activityDefinitions;
    }

    /// <summary>
    /// Gère la selection d'une activite depuis le ActivitiesSectionPanel
    /// </summary>
    private void OnActivitySelected(ActivityDefinition activityDefinition)
    {
        Logger.LogInfo($"LocationDetailsPanel: Activity selected - {activityDefinition.GetDisplayName()}", Logger.LogCategory.General);

        // Retrouver la LocationActivity correspondante dans la location courante
        var locationActivity = FindLocationActivityByDefinition(activityDefinition);

        if (locationActivity != null)
        {
            // Ouvrir le panel de variantes d'activite
            if (ActivityVariantsPanel.Instance != null)
            {
                ActivityVariantsPanel.Instance.OpenWithActivity(locationActivity);
            }
            else
            {
                Logger.LogWarning("LocationDetailsPanel: ActivityVariantsPanel introuvable !", Logger.LogCategory.General);
            }
        }
        else
        {
            Logger.LogError($"LocationDetailsPanel: Impossible de retrouver la LocationActivity pour {activityDefinition.GetDisplayName()}", Logger.LogCategory.General);
        }
    }

    /// <summary>
    /// Retrouve la LocationActivity correspondant a une ActivityDefinition dans la location courante
    /// </summary>
    private LocationActivity FindLocationActivityByDefinition(ActivityDefinition activityDefinition)
    {
        if (currentLocation == null || activityDefinition == null) return null;

        var availableActivities = currentLocation.GetAvailableActivities();

        foreach (var locationActivity in availableActivities)
        {
            if (locationActivity != null &&
                locationActivity.ActivityReference != null &&
                locationActivity.ActivityReference.ActivityID == activityDefinition.ActivityID)
            {
                return locationActivity;
            }
        }

        return null;
    }

    /// <summary>
    /// Met a jour la section d'informations supplementaires
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

        // Resume des activites
        infoLines.Add(currentLocation.GetActivitiesSummary());

        // Informations de voyage si en cours
        AddTravelInfo(infoLines);

        locationInfoText.text = string.Join("\n", infoLines);
    }

    #endregion

    #region Methodes Utilitaires

    /// <summary>
    /// Affiche un message quand aucune location n'est disponible
    /// </summary>
    private void ShowNoLocationMessage()
    {
        if (locationDescriptionText != null)
        {
            locationDescriptionText.text = "Aucune information de location disponible.";
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
    /// Ajoute les informations de voyage a la liste d'infos
    /// </summary>
    private void AddTravelInfo(List<string> infoLines)
    {
        if (dataManager?.PlayerData != null && dataManager.PlayerData.IsCurrentlyTraveling())
        {
            string destination = dataManager.PlayerData.TravelDestinationId;
            long progress = dataManager.PlayerData.GetTravelProgress(dataManager.PlayerData.TotalSteps);
            int required = dataManager.PlayerData.TravelRequiredSteps;
            float progressPercent = required > 0 ? (float)progress / required * 100f : 0f;

            infoLines.Add($"En voyage vers {destination}: {progress}/{required} pas ({progressPercent:F1}%)");
        }
    }

    #endregion
}