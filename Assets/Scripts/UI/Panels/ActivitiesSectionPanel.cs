// Purpose: Panel section for displaying primary activities in a grid layout
// Filepath: Assets/Scripts/UI/Panels/ActivitiesSectionPanel.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActivitiesSectionPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject activitiesSection;
    [SerializeField] private TextMeshProUGUI activitiesSectionTitle;
    [SerializeField] private Transform activitiesContainer;
    [SerializeField] private GameObject primaryActivityCardPrefab; // Le prefab PrimaryActivityCard
    [SerializeField] private TextMeshProUGUI noActivitiesText;

    [Header("Slide Animation Settings")]
    [SerializeField] private float slideAnimationDuration = 0.3f;
    [SerializeField] private LeanTweenType slideEaseType = LeanTweenType.easeInOutQuad;

    // Pool d'objets pour optimiser les performances
    private Queue<GameObject> activityCardPool = new Queue<GameObject>();
    private List<GameObject> instantiatedActivityCards = new List<GameObject>();

    // Current data
    private List<ActivityDefinition> currentActivities = new List<ActivityDefinition>();

    // Animation state
    private RectTransform rectTransform;
    private float originalYPosition;
    private float hiddenYPosition;
    private bool isHidden = false;
    private int currentTween = -1;

    // Singleton
    public static ActivitiesSectionPanel Instance { get; private set; }

    // Events
    public System.Action<ActivityDefinition> OnActivitySelected;

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
    }

    void Start()
    {
        // Get RectTransform and store original position
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            originalYPosition = rectTransform.anchoredPosition.y;
            // Calculate hidden position (slide down off screen)
            hiddenYPosition = originalYPosition - rectTransform.rect.height - 100f;
        }

        // Valider les references
        ValidateReferences();

        // Setup grid layout automatiquement
        SetupGridLayout();

        // Hide "no activities" text by default
        if (noActivitiesText != null)
        {
            noActivitiesText.gameObject.SetActive(false);
        }
    }

    #region Public Methods

    /// <summary>
    /// Affiche les activites primaires
    /// </summary>
    public void DisplayActivities(List<ActivityDefinition> activities)
    {
        if (activities == null)
        {
            Logger.LogWarning("ActivitiesSectionPanel: Cannot display null activities list!", Logger.LogCategory.ActivityLog);
            return;
        }

        currentActivities = activities;

        // Nettoyer les cartes existantes
        RecycleActivityCards();

        // Mettre a jour le titre de la section
        UpdateSectionTitle(activities.Count);

        // Afficher ou masquer selon la disponibilite
        if (activities.Count == 0)
        {
            ShowNoActivitiesMessage();
        }
        else
        {
            HideNoActivitiesMessage();
            CreateActivityCards(activities);
        }

        Logger.LogInfo($"ActivitiesSectionPanel: Displayed {activities.Count} activities", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Efface toutes les activites affichees
    /// </summary>
    public void ClearActivities()
    {
        RecycleActivityCards();
        currentActivities.Clear();
        UpdateSectionTitle(0);
        ShowNoActivitiesMessage();
    }

    /// <summary>
    /// Masque ou affiche la section entiere
    /// </summary>
    public void SetSectionVisible(bool visible)
    {
        if (activitiesSection != null)
        {
            activitiesSection.SetActive(visible);
        }
    }

    /// <summary>
    /// Slide the panel down out of view (when opening an activity)
    /// </summary>
    public void SlideOut()
    {
        if (rectTransform == null || isHidden) return;

        // Cancel any existing tween
        if (currentTween >= 0)
        {
            LeanTween.cancel(currentTween);
        }

        isHidden = true;

        currentTween = LeanTween.moveY(rectTransform, hiddenYPosition, slideAnimationDuration)
            .setEase(slideEaseType)
            .setOnComplete(() => currentTween = -1)
            .id;

        Logger.LogInfo("ActivitiesSectionPanel: Sliding out", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Slide the panel back into view (when closing an activity)
    /// </summary>
    public void SlideIn()
    {
        if (rectTransform == null || !isHidden) return;

        // Cancel any existing tween
        if (currentTween >= 0)
        {
            LeanTween.cancel(currentTween);
        }

        isHidden = false;

        currentTween = LeanTween.moveY(rectTransform, originalYPosition, slideAnimationDuration)
            .setEase(slideEaseType)
            .setOnComplete(() => currentTween = -1)
            .id;

        Logger.LogInfo("ActivitiesSectionPanel: Sliding in", Logger.LogCategory.ActivityLog);
    }

    /// <summary>
    /// Immediately reset to original position (no animation)
    /// </summary>
    public void ResetPosition()
    {
        if (rectTransform == null) return;

        // Cancel any existing tween
        if (currentTween >= 0)
        {
            LeanTween.cancel(currentTween);
            currentTween = -1;
        }

        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, originalYPosition);
        isHidden = false;
    }

    #endregion

    #region Private Methods - Setup

    /// <summary>
    /// Valide que toutes les references sont correctement assignees
    /// </summary>
    private void ValidateReferences()
    {
        bool hasErrors = false;

        if (activitiesContainer == null)
        {
            Logger.LogError("ActivitiesSectionPanel: ActivitiesContainer n'est pas assigne !", Logger.LogCategory.ActivityLog);
            hasErrors = true;
        }

        if (primaryActivityCardPrefab == null)
        {
            Logger.LogError("ActivitiesSectionPanel: PrimaryActivityCardPrefab n'est pas assigne !", Logger.LogCategory.ActivityLog);
            hasErrors = true;
        }

        if (hasErrors)
        {
            Logger.LogError("ActivitiesSectionPanel: Des references critiques manquent ! Le panel peut ne pas fonctionner correctement.", Logger.LogCategory.ActivityLog);
        }
    }

    /// <summary>
    /// Verifie que le grid layout est present (ne modifie pas les parametres configures dans l'Inspector)
    /// </summary>
    private void SetupGridLayout()
    {
        if (activitiesContainer == null) return;

        // Verifier que GridLayoutGroup est present
        if (activitiesContainer.GetComponent<GridLayoutGroup>() == null)
        {
            Logger.LogWarning("ActivitiesSectionPanel: GridLayoutGroup manquant sur ActivitiesContainer, ajoutez-le dans l'Inspector!", Logger.LogCategory.ActivityLog);
        }
    }

    #endregion

    #region Private Methods - UI Updates

    /// <summary>
    /// Met a jour le titre de la section
    /// </summary>
    private void UpdateSectionTitle(int activityCount)
    {
        if (activitiesSectionTitle != null)
        {
            activitiesSectionTitle.text = $"Activites disponibles ({activityCount})";
        }
    }

    /// <summary>
    /// Affiche le message "aucune activite"
    /// </summary>
    private void ShowNoActivitiesMessage()
    {
        if (noActivitiesText != null)
        {
            noActivitiesText.gameObject.SetActive(true);
            noActivitiesText.text = "Aucune activite disponible.";
        }
    }

    /// <summary>
    /// Masque le message "aucune activite"
    /// </summary>
    private void HideNoActivitiesMessage()
    {
        if (noActivitiesText != null)
        {
            noActivitiesText.gameObject.SetActive(false);
        }
    }

    #endregion

    #region Private Methods - Activity Cards Management

    /// <summary>
    /// Cree les cartes d'activite pour la liste donnee
    /// </summary>
    private void CreateActivityCards(List<ActivityDefinition> activities)
    {
        if (activitiesContainer == null || primaryActivityCardPrefab == null) return;

        foreach (var activity in activities)
        {
            if (activity == null || !activity.IsValidActivity()) continue;

            GameObject cardObject = GetPooledActivityCard();
            if (cardObject.transform.parent != activitiesContainer)
            {
                cardObject.transform.SetParent(activitiesContainer, false);
            }

            instantiatedActivityCards.Add(cardObject);
            SetupActivityCard(cardObject, activity);
        }

        // Forcer la mise a jour du layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(activitiesContainer.GetComponent<RectTransform>());
    }

    /// <summary>
    /// Configure une carte d'activite individuelle
    /// </summary>
    private void SetupActivityCard(GameObject cardObject, ActivityDefinition activity)
    {
        if (cardObject == null || activity == null) return;

        // Recuperer le composant PrimaryActivityCard
        var activityCard = cardObject.GetComponent<PrimaryActivityCard>();
        if (activityCard == null)
        {
            Logger.LogError($"ActivitiesSectionPanel: Le prefab ne contient pas de composant PrimaryActivityCard !", Logger.LogCategory.ActivityLog);
            return;
        }

        // Setup de la carte avec les donnees d'activite
        activityCard.Setup(activity);

        // S'abonner a l'evenement de clic
        activityCard.OnCardClicked -= OnActivityCardClicked; // eviter les doublons
        activityCard.OnCardClicked += OnActivityCardClicked;
    }

    /// <summary>
    /// Gere le clic sur une carte d'activite
    /// </summary>
    private void OnActivityCardClicked(ActivityDefinition activity)
    {
        Logger.LogInfo($"ActivitiesSectionPanel: Activity card clicked for {activity.GetDisplayName()}", Logger.LogCategory.ActivityLog);

        // Propager l'evenement
        OnActivitySelected?.Invoke(activity);
    }

    #endregion

    #region Private Methods - Object Pooling

    /// <summary>
    /// Recycle toutes les cartes d'activite instantiees
    /// </summary>
    private void RecycleActivityCards()
    {
        foreach (var card in instantiatedActivityCards)
        {
            if (card != null)
            {
                // Nettoyer les evenements
                var activityCard = card.GetComponent<PrimaryActivityCard>();
                if (activityCard != null)
                {
                    activityCard.OnCardClicked -= OnActivityCardClicked;
                }

                card.SetActive(false);
                activityCardPool.Enqueue(card);
            }
        }
        instantiatedActivityCards.Clear();
    }

    /// <summary>
    /// Recupere une carte d'activite du pool ou en cree une nouvelle
    /// </summary>
    private GameObject GetPooledActivityCard()
    {
        if (activityCardPool.Count > 0)
        {
            var card = activityCardPool.Dequeue();
            card.SetActive(true);
            return card;
        }

        return Instantiate(primaryActivityCardPrefab, activitiesContainer);
    }

    #endregion

    #region Unity Events

    void OnDestroy()
    {
        // Nettoyer tous les evenements pour eviter les fuites memoire
        foreach (var card in instantiatedActivityCards)
        {
            if (card != null)
            {
                var activityCard = card.GetComponent<PrimaryActivityCard>();
                if (activityCard != null)
                {
                    activityCard.OnCardClicked -= OnActivityCardClicked;
                }
            }
        }
    }

    #endregion
}
