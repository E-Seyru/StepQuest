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

    // Pool d'objets pour optimiser les performances
    private Queue<GameObject> activityCardPool = new Queue<GameObject>();
    private List<GameObject> instantiatedActivityCards = new List<GameObject>();

    // Current data
    private List<ActivityDefinition> currentActivities = new List<ActivityDefinition>();

    // Events
    public System.Action<ActivityDefinition> OnActivitySelected;

    void Start()
    {
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
            Debug.LogWarning("ActivitiesSectionPanel: Cannot display null activities list!");
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

        Debug.Log($"ActivitiesSectionPanel: Displayed {activities.Count} activities");
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
            Debug.LogError("ActivitiesSectionPanel: ActivitiesContainer n'est pas assigne !");
            hasErrors = true;
        }

        if (primaryActivityCardPrefab == null)
        {
            Debug.LogError("ActivitiesSectionPanel: PrimaryActivityCardPrefab n'est pas assigne !");
            hasErrors = true;
        }

        if (hasErrors)
        {
            Debug.LogError("ActivitiesSectionPanel: Des references critiques manquent ! Le panel peut ne pas fonctionner correctement.");
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
            Debug.LogWarning("ActivitiesSectionPanel: GridLayoutGroup manquant sur ActivitiesContainer, ajoutez-le dans l'Inspector!");
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
            Debug.LogError($"ActivitiesSectionPanel: Le prefab ne contient pas de composant PrimaryActivityCard !");
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
        Debug.Log($"ActivitiesSectionPanel: Activity card clicked for {activity.GetDisplayName()}");

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
