// Purpose: Panel section for displaying social activities in a grid layout
// Filepath: Assets/Scripts/UI/Panels/SocialSectionPanel.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel section for displaying social activities at a location.
/// Similar to ActivitiesSectionPanel but for social/exploration activities.
/// </summary>
public class SocialSectionPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject socialSection;
    [SerializeField] private TextMeshProUGUI socialSectionTitle;
    [SerializeField] private Transform socialActivitiesContainer;
    [SerializeField] private GameObject socialAvatarPrefab;
    [SerializeField] private TextMeshProUGUI noSocialActivitiesText;

    // Pool d'objets pour optimiser les performances
    private Queue<GameObject> socialActivityCardPool = new Queue<GameObject>();
    private List<GameObject> instantiatedSocialActivityCards = new List<GameObject>();

    // Current data
    private List<ActivityDefinition> currentSocialActivities = new List<ActivityDefinition>();

    // Events
    public System.Action<ActivityDefinition> OnSocialActivitySelected;

    void Start()
    {
        ValidateReferences();
        SetupGridLayout();

        // Hide "no social activities" text by default
        if (noSocialActivitiesText != null)
        {
            noSocialActivitiesText.gameObject.SetActive(false);
        }
    }

    #region Public Methods

    /// <summary>
    /// Affiche les activites sociales
    /// </summary>
    public void DisplaySocialActivities(List<ActivityDefinition> socialActivities)
    {
        if (socialActivities == null)
        {
            socialActivities = new List<ActivityDefinition>();
        }

        currentSocialActivities = socialActivities;

        // Nettoyer les cartes existantes
        RecycleSocialActivityCards();

        // Update content (tab system controls visibility)
        UpdateSectionTitle(socialActivities.Count);

        if (socialActivities.Count > 0)
        {
            CreateSocialActivityCards(socialActivities);
            if (noSocialActivitiesText != null)
            {
                noSocialActivitiesText.gameObject.SetActive(false);
            }
        }
        else
        {
            if (noSocialActivitiesText != null)
            {
                noSocialActivitiesText.gameObject.SetActive(true);
            }
        }

        Debug.Log($"SocialSectionPanel: Displayed {socialActivities.Count} social activities");
    }

    /// <summary>
    /// Efface toutes les activites sociales affichees
    /// </summary>
    public void ClearSocialActivities()
    {
        RecycleSocialActivityCards();
        currentSocialActivities.Clear();
        HideSection();
    }

    /// <summary>
    /// Masque la section sociale
    /// </summary>
    public void HideSection()
    {
        if (socialSection != null)
        {
            socialSection.SetActive(false);
        }
    }

    /// <summary>
    /// Affiche la section sociale
    /// </summary>
    public void ShowSection()
    {
        if (socialSection != null)
        {
            socialSection.SetActive(true);
        }
    }

    /// <summary>
    /// Verifie si des activites sociales sont disponibles
    /// </summary>
    public bool HasSocialActivities()
    {
        return currentSocialActivities != null && currentSocialActivities.Count > 0;
    }

    #endregion

    #region Private Methods - Setup

    private void ValidateReferences()
    {
        bool hasErrors = false;

        if (socialActivitiesContainer == null)
        {
            Debug.LogError("SocialSectionPanel: SocialActivitiesContainer n'est pas assigne !");
            hasErrors = true;
        }

        if (socialAvatarPrefab == null)
        {
            Debug.LogError("SocialSectionPanel: SocialAvatarPrefab n'est pas assigne !");
            hasErrors = true;
        }

        if (hasErrors)
        {
            Debug.LogError("SocialSectionPanel: Des references critiques manquent !");
        }
    }

    private void SetupGridLayout()
    {
        if (socialActivitiesContainer == null) return;

        // Verifier que GridLayoutGroup est present
        if (socialActivitiesContainer.GetComponent<GridLayoutGroup>() == null)
        {
            Debug.LogWarning("SocialSectionPanel: GridLayoutGroup manquant sur SocialActivitiesContainer, ajoutez-le dans l'Inspector!");
        }
    }

    #endregion

    #region Private Methods - UI Updates

    private void UpdateSectionTitle(int activityCount)
    {
        if (socialSectionTitle != null)
        {
            socialSectionTitle.text = $"Social ({activityCount})";
        }
    }

    #endregion

    #region Private Methods - Social Activity Cards Management

    private void CreateSocialActivityCards(List<ActivityDefinition> socialActivities)
    {
        if (socialActivitiesContainer == null || socialAvatarPrefab == null) return;

        foreach (var activity in socialActivities)
        {
            if (activity == null || !activity.IsValidActivity()) continue;

            GameObject cardObject = GetPooledSocialActivityCard();
            if (cardObject.transform.parent != socialActivitiesContainer)
            {
                cardObject.transform.SetParent(socialActivitiesContainer, false);
            }

            instantiatedSocialActivityCards.Add(cardObject);
            SetupSocialActivityCard(cardObject, activity);
        }

        // Forcer la mise a jour du layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(socialActivitiesContainer.GetComponent<RectTransform>());
    }

    private void SetupSocialActivityCard(GameObject cardObject, ActivityDefinition activity)
    {
        if (cardObject == null || activity == null) return;

        var avatarCard = cardObject.GetComponent<SocialAvatarCard>();
        if (avatarCard == null)
        {
            Debug.LogError("SocialSectionPanel: Le prefab ne contient pas de composant SocialAvatarCard !");
            return;
        }

        // Setup de la carte avec les donnees de l'activite
        avatarCard.Setup(
            activity.ActivityID,
            activity.GetDisplayName(),
            activity.ActivityIcon,
            true
        );

        // S'abonner a l'evenement de clic
        avatarCard.OnCardClicked -= OnSocialAvatarCardClicked;
        avatarCard.OnCardClicked += OnSocialAvatarCardClicked;
    }

    private void OnSocialAvatarCardClicked(string avatarId)
    {
        Debug.Log($"SocialSectionPanel: Social avatar card clicked for {avatarId}");

        // Retrouver l'activite correspondante
        var activity = currentSocialActivities.Find(a => a.ActivityID == avatarId);
        if (activity != null)
        {
            // Propager l'evenement
            OnSocialActivitySelected?.Invoke(activity);
        }
    }

    #endregion

    #region Private Methods - Object Pooling

    private void RecycleSocialActivityCards()
    {
        foreach (var card in instantiatedSocialActivityCards)
        {
            if (card != null)
            {
                var avatarCard = card.GetComponent<SocialAvatarCard>();
                if (avatarCard != null)
                {
                    avatarCard.OnCardClicked -= OnSocialAvatarCardClicked;
                }

                card.SetActive(false);
                socialActivityCardPool.Enqueue(card);
            }
        }
        instantiatedSocialActivityCards.Clear();
    }

    private GameObject GetPooledSocialActivityCard()
    {
        if (socialActivityCardPool.Count > 0)
        {
            var card = socialActivityCardPool.Dequeue();
            card.SetActive(true);
            return card;
        }

        return Instantiate(socialAvatarPrefab, socialActivitiesContainer);
    }

    #endregion

    #region Unity Events

    void OnDestroy()
    {
        foreach (var card in instantiatedSocialActivityCards)
        {
            if (card != null)
            {
                var avatarCard = card.GetComponent<SocialAvatarCard>();
                if (avatarCard != null)
                {
                    avatarCard.OnCardClicked -= OnSocialAvatarCardClicked;
                }
            }
        }
    }

    #endregion
}
