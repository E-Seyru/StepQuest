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
        // Valider les r�f�rences
        ValidateReferences();

        // Setup grid layout automatiquement
        SetupGridLayout();
    }

    #region Public Methods

    /// <summary>
    /// Affiche les activit�s primaires
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

        // Mettre � jour le titre de la section
        UpdateSectionTitle(activities.Count);

        // Afficher ou masquer selon la disponibilit�
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
    /// Efface toutes les activit�s affich�es
    /// </summary>
    public void ClearActivities()
    {
        RecycleActivityCards();
        currentActivities.Clear();
        UpdateSectionTitle(0);
        ShowNoActivitiesMessage();
    }

    /// <summary>
    /// Masque ou affiche la section enti�re
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
    /// Valide que toutes les r�f�rences sont correctement assign�es
    /// </summary>
    private void ValidateReferences()
    {
        bool hasErrors = false;

        if (activitiesContainer == null)
        {
            Debug.LogError("ActivitiesSectionPanel: ActivitiesContainer n'est pas assign� !");
            hasErrors = true;
        }

        if (primaryActivityCardPrefab == null)
        {
            Debug.LogError("ActivitiesSectionPanel: PrimaryActivityCardPrefab n'est pas assign� !");
            hasErrors = true;
        }

        if (hasErrors)
        {
            Debug.LogError("ActivitiesSectionPanel: Des r�f�rences critiques manquent ! Le panel peut ne pas fonctionner correctement.");
        }
    }

    /// <summary>
    /// Configure le grid layout automatiquement - AM�LIOR� comme ActivityVariantsPanel
    /// </summary>
    private void SetupGridLayout()
    {
        if (activitiesContainer == null) return;

        // Ajouter GridLayoutGroup si pas pr�sent
        GridLayoutGroup gridLayout = activitiesContainer.GetComponent<GridLayoutGroup>();
        if (gridLayout == null)
        {
            gridLayout = activitiesContainer.gameObject.AddComponent<GridLayoutGroup>();
        }

        // Configurer le grid layout
        gridLayout.childAlignment = TextAnchor.MiddleLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
        gridLayout.spacing = new Vector2(15f, 15f); // Espacement entre cartes
        gridLayout.padding = new RectOffset(15, 15, 15, 15); // Padding autour du container

        // Taille des cellules (ajustable selon votre design de cartes)
        gridLayout.cellSize = new Vector2(210f, 290f);

        // Ajouter ContentSizeFitter si pas pr�sent
        ContentSizeFitter contentSizeFitter = activitiesContainer.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter == null)
        {
            contentSizeFitter = activitiesContainer.gameObject.AddComponent<ContentSizeFitter>();
        }

        // Configurer le ContentSizeFitter
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        Debug.Log("ActivitiesSectionPanel: Grid layout configur� automatiquement");
    }

    #endregion

    #region Private Methods - UI Updates

    /// <summary>
    /// Met � jour le titre de la section
    /// </summary>
    private void UpdateSectionTitle(int activityCount)
    {
        if (activitiesSectionTitle != null)
        {
            activitiesSectionTitle.text = $"Activit�s disponibles ({activityCount})";
        }
    }

    /// <summary>
    /// Affiche le message "aucune activit�"
    /// </summary>
    private void ShowNoActivitiesMessage()
    {
        if (noActivitiesText != null)
        {
            noActivitiesText.gameObject.SetActive(true);
            noActivitiesText.text = "Aucune activit� disponible.";
        }
    }

    /// <summary>
    /// Masque le message "aucune activit�"
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
    /// Cr�e les cartes d'activit� pour la liste donn�e
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

        // Forcer la mise � jour du layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(activitiesContainer.GetComponent<RectTransform>());
    }

    /// <summary>
    /// Configure une carte d'activit� individuelle
    /// </summary>
    private void SetupActivityCard(GameObject cardObject, ActivityDefinition activity)
    {
        if (cardObject == null || activity == null) return;

        // R�cup�rer le composant PrimaryActivityCard
        var activityCard = cardObject.GetComponent<PrimaryActivityCard>();
        if (activityCard == null)
        {
            Debug.LogError($"ActivitiesSectionPanel: Le prefab ne contient pas de composant PrimaryActivityCard !");
            return;
        }

        // Setup de la carte avec les donn�es d'activit�
        activityCard.Setup(activity);

        // S'abonner � l'�v�nement de clic
        activityCard.OnCardClicked -= OnActivityCardClicked; // �viter les doublons
        activityCard.OnCardClicked += OnActivityCardClicked;
    }

    /// <summary>
    /// G�re le clic sur une carte d'activit�
    /// </summary>
    private void OnActivityCardClicked(ActivityDefinition activity)
    {
        Debug.Log($"ActivitiesSectionPanel: Activity card clicked for {activity.GetDisplayName()}");

        // Propager l'�v�nement
        OnActivitySelected?.Invoke(activity);
    }

    #endregion

    #region Private Methods - Object Pooling

    /// <summary>
    /// Recycle toutes les cartes d'activit� instanti�es
    /// </summary>
    private void RecycleActivityCards()
    {
        foreach (var card in instantiatedActivityCards)
        {
            if (card != null)
            {
                // Nettoyer les �v�nements
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
    /// R�cup�re une carte d'activit� du pool ou en cr�e une nouvelle
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
        // Nettoyer tous les �v�nements pour �viter les fuites m�moire
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