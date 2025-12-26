// Purpose: Panel section for displaying NPCs and social content in a grid layout
// Filepath: Assets/Scripts/UI/Panels/SocialSectionPanel.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel section for displaying NPCs at a location.
/// Shows NPC avatars that players can interact with.
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
    private List<NPCDefinition> currentNPCs = new List<NPCDefinition>();

    // Events
    public System.Action<NPCDefinition> OnNPCSelected;

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
    /// Affiche les NPCs disponibles a cet emplacement
    /// </summary>
    public void DisplayNPCs(List<NPCDefinition> npcs)
    {
        if (npcs == null)
        {
            npcs = new List<NPCDefinition>();
        }

        currentNPCs = npcs;

        // Nettoyer les cartes existantes
        RecycleSocialActivityCards();

        // Update content (tab system controls visibility)
        UpdateSectionTitle(npcs.Count);

        if (npcs.Count > 0)
        {
            CreateNPCCards(npcs);
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

        Logger.LogInfo($"SocialSectionPanel: Displayed {npcs.Count} NPCs", Logger.LogCategory.DialogueLog);
    }

    /// <summary>
    /// Efface tous les NPCs affiches
    /// </summary>
    public void ClearNPCs()
    {
        RecycleSocialActivityCards();
        currentNPCs.Clear();
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
    /// Verifie si des NPCs sont disponibles
    /// </summary>
    public bool HasNPCs()
    {
        return currentNPCs != null && currentNPCs.Count > 0;
    }

    #endregion

    #region Private Methods - Setup

    private void ValidateReferences()
    {
        bool hasErrors = false;

        if (socialActivitiesContainer == null)
        {
            Logger.LogError("SocialSectionPanel: SocialActivitiesContainer n'est pas assigne !", Logger.LogCategory.DialogueLog);
            hasErrors = true;
        }

        if (socialAvatarPrefab == null)
        {
            Logger.LogError("SocialSectionPanel: SocialAvatarPrefab n'est pas assigne !", Logger.LogCategory.DialogueLog);
            hasErrors = true;
        }

        if (hasErrors)
        {
            Logger.LogError("SocialSectionPanel: Des references critiques manquent !", Logger.LogCategory.DialogueLog);
        }
    }

    private void SetupGridLayout()
    {
        if (socialActivitiesContainer == null) return;

        // Verifier que GridLayoutGroup est present
        if (socialActivitiesContainer.GetComponent<GridLayoutGroup>() == null)
        {
            Logger.LogWarning("SocialSectionPanel: GridLayoutGroup manquant sur SocialActivitiesContainer, ajoutez-le dans l'Inspector!", Logger.LogCategory.DialogueLog);
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

    #region Private Methods - NPC Cards Management

    private void CreateNPCCards(List<NPCDefinition> npcs)
    {
        if (socialActivitiesContainer == null || socialAvatarPrefab == null) return;

        foreach (var npc in npcs)
        {
            if (npc == null || !npc.IsValid()) continue;

            GameObject cardObject = GetPooledSocialActivityCard();
            if (cardObject.transform.parent != socialActivitiesContainer)
            {
                cardObject.transform.SetParent(socialActivitiesContainer, false);
            }

            instantiatedSocialActivityCards.Add(cardObject);
            SetupNPCCard(cardObject, npc);
        }

        // Forcer la mise a jour du layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(socialActivitiesContainer.GetComponent<RectTransform>());
    }

    private void SetupNPCCard(GameObject cardObject, NPCDefinition npc)
    {
        if (cardObject == null || npc == null) return;

        var avatarCard = cardObject.GetComponent<SocialAvatarCard>();
        if (avatarCard == null)
        {
            Logger.LogError("SocialSectionPanel: Le prefab ne contient pas de composant SocialAvatarCard !", Logger.LogCategory.DialogueLog);
            return;
        }

        // Setup de la carte avec les donnees du NPC (utilise Avatar pour les cartes)
        avatarCard.Setup(
            npc.NPCID,
            npc.GetDisplayName(),
            npc.Avatar,
            npc.IsActive
        );

        // S'abonner a l'evenement de clic
        avatarCard.OnCardClicked -= OnNPCAvatarCardClicked;
        avatarCard.OnCardClicked += OnNPCAvatarCardClicked;
    }

    private void OnNPCAvatarCardClicked(string npcId)
    {
        Logger.LogInfo($"SocialSectionPanel: NPC avatar card clicked for {npcId}", Logger.LogCategory.DialogueLog);

        // Retrouver le NPC correspondant
        var npc = currentNPCs.Find(n => n.NPCID == npcId);
        if (npc != null)
        {
            // Propager l'evenement
            OnNPCSelected?.Invoke(npc);
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
                    avatarCard.OnCardClicked -= OnNPCAvatarCardClicked;
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
                    avatarCard.OnCardClicked -= OnNPCAvatarCardClicked;
                }
            }
        }
    }

    #endregion
}
