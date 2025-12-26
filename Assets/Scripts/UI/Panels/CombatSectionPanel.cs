// Purpose: Panel section for displaying available enemies in a grid layout
// Filepath: Assets/Scripts/UI/Panels/CombatSectionPanel.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel section for displaying available enemies at a location.
/// Similar to ActivitiesSectionPanel but for combat.
/// </summary>
public class CombatSectionPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject combatSection;
    [SerializeField] private TextMeshProUGUI combatSectionTitle;
    [SerializeField] private Transform enemiesContainer;
    [SerializeField] private GameObject enemyCardPrefab;
    [SerializeField] private TextMeshProUGUI noEnemiesText;


    // Pool d'objets pour optimiser les performances
    private Queue<GameObject> enemyCardPool = new Queue<GameObject>();
    private List<GameObject> instantiatedEnemyCards = new List<GameObject>();

    // Current data
    private List<LocationEnemy> currentEnemies = new List<LocationEnemy>();

    // Events
    public System.Action<EnemyDefinition> OnEnemySelected;

    void Start()
    {
        ValidateReferences();
        SetupGridLayout();

        // Hide "no enemies" text by default
        if (noEnemiesText != null)
        {
            noEnemiesText.gameObject.SetActive(false);
        }
    }

    #region Public Methods

    /// <summary>
    /// Affiche les ennemis disponibles pour une location
    /// </summary>
    public void DisplayEnemies(MapLocationDefinition location)
    {
        if (location == null)
        {
            Logger.LogWarning("CombatSectionPanel: Cannot display enemies for null location!", Logger.LogCategory.CombatLog);
            HideSection();
            return;
        }

        var enemies = location.GetAvailableEnemies();
        DisplayEnemies(enemies);
    }

    /// <summary>
    /// Affiche une liste d'ennemis
    /// </summary>
    public void DisplayEnemies(List<LocationEnemy> enemies)
    {
        if (enemies == null)
        {
            enemies = new List<LocationEnemy>();
        }

        currentEnemies = enemies;

        // Nettoyer les cartes existantes
        RecycleEnemyCards();

        // Update content (tab system controls visibility)
        if (enemies.Count == 0)
        {
            ShowNoEnemiesMessage();
        }
        else
        {
            if (noEnemiesText != null)
            {
                noEnemiesText.gameObject.SetActive(false);
            }
            UpdateSectionTitle(enemies.Count);
            CreateEnemyCards(enemies);
        }

        Logger.LogInfo($"CombatSectionPanel: Displayed {enemies.Count} enemies", Logger.LogCategory.CombatLog);
    }

    /// <summary>
    /// Efface tous les ennemis affiches
    /// </summary>
    public void ClearEnemies()
    {
        RecycleEnemyCards();
        currentEnemies.Clear();
        HideSection();
    }

    /// <summary>
    /// Masque la section combat
    /// </summary>
    public void HideSection()
    {
        if (combatSection != null)
        {
            combatSection.SetActive(false);
        }
    }

    /// <summary>
    /// Affiche la section combat
    /// </summary>
    public void ShowSection()
    {
        if (combatSection != null)
        {
            combatSection.SetActive(true);
        }
    }

    /// <summary>
    /// Verifie si des ennemis sont disponibles
    /// </summary>
    public bool HasEnemies()
    {
        return currentEnemies != null && currentEnemies.Count > 0;
    }

    #endregion

    #region Private Methods - Setup

    private void ValidateReferences()
    {
        bool hasErrors = false;

        if (enemiesContainer == null)
        {
            Logger.LogError("CombatSectionPanel: EnemiesContainer n'est pas assigne !", Logger.LogCategory.CombatLog);
            hasErrors = true;
        }

        if (enemyCardPrefab == null)
        {
            Logger.LogError("CombatSectionPanel: EnemyCardPrefab n'est pas assigne !", Logger.LogCategory.CombatLog);
            hasErrors = true;
        }

        if (hasErrors)
        {
            Logger.LogError("CombatSectionPanel: Des references critiques manquent !", Logger.LogCategory.CombatLog);
        }
    }

    private void SetupGridLayout()
    {
        if (enemiesContainer == null) return;

        // Verifier que GridLayoutGroup est present
        if (enemiesContainer.GetComponent<GridLayoutGroup>() == null)
        {
            Logger.LogWarning("CombatSectionPanel: GridLayoutGroup manquant sur EnemiesContainer, ajoutez-le dans l'Inspector!", Logger.LogCategory.CombatLog);
        }
    }

    #endregion

    #region Private Methods - UI Updates

    private void UpdateSectionTitle(int enemyCount)
    {
        if (combatSectionTitle != null)
        {
            combatSectionTitle.text = $"Combattre ({enemyCount})";
        }
    }

    private void ShowNoEnemiesMessage()
    {
        if (noEnemiesText != null)
        {
            noEnemiesText.gameObject.SetActive(true);
        }
        UpdateSectionTitle(0);
    }

    #endregion

    #region Private Methods - Enemy Cards Management

    private void CreateEnemyCards(List<LocationEnemy> enemies)
    {
        if (enemiesContainer == null || enemyCardPrefab == null) return;

        foreach (var locEnemy in enemies)
        {
            if (locEnemy == null || !locEnemy.IsValid()) continue;

            GameObject cardObject = GetPooledEnemyCard();
            if (cardObject.transform.parent != enemiesContainer)
            {
                cardObject.transform.SetParent(enemiesContainer, false);
            }

            instantiatedEnemyCards.Add(cardObject);
            SetupEnemyCard(cardObject, locEnemy);
        }

        // Forcer la mise a jour du layout
        LayoutRebuilder.ForceRebuildLayoutImmediate(enemiesContainer.GetComponent<RectTransform>());
    }

    private void SetupEnemyCard(GameObject cardObject, LocationEnemy locEnemy)
    {
        if (cardObject == null || locEnemy == null) return;

        var enemyCard = cardObject.GetComponent<EnemyCard>();
        if (enemyCard == null)
        {
            Logger.LogError("CombatSectionPanel: Le prefab ne contient pas de composant EnemyCard !", Logger.LogCategory.CombatLog);
            return;
        }

        // Setup de la carte
        enemyCard.Setup(locEnemy);

        // S'abonner a l'evenement de clic
        enemyCard.OnCardClicked -= OnEnemyCardClicked;
        enemyCard.OnCardClicked += OnEnemyCardClicked;
    }

    private void OnEnemyCardClicked(EnemyDefinition enemy)
    {
        Logger.LogInfo($"CombatSectionPanel: Enemy card clicked for {enemy.GetDisplayName()}", Logger.LogCategory.CombatLog);

        // Propager l'evenement
        OnEnemySelected?.Invoke(enemy);
    }

    #endregion

    #region Private Methods - Object Pooling

    private void RecycleEnemyCards()
    {
        foreach (var card in instantiatedEnemyCards)
        {
            if (card != null)
            {
                var enemyCard = card.GetComponent<EnemyCard>();
                if (enemyCard != null)
                {
                    enemyCard.OnCardClicked -= OnEnemyCardClicked;
                }

                card.SetActive(false);
                enemyCardPool.Enqueue(card);
            }
        }
        instantiatedEnemyCards.Clear();
    }

    private GameObject GetPooledEnemyCard()
    {
        if (enemyCardPool.Count > 0)
        {
            var card = enemyCardPool.Dequeue();
            card.SetActive(true);
            return card;
        }

        return Instantiate(enemyCardPrefab, enemiesContainer);
    }

    #endregion

    #region Unity Events

    void OnDestroy()
    {
        foreach (var card in instantiatedEnemyCards)
        {
            if (card != null)
            {
                var enemyCard = card.GetComponent<EnemyCard>();
                if (enemyCard != null)
                {
                    enemyCard.OnCardClicked -= OnEnemyCardClicked;
                }
            }
        }
    }

    #endregion
}
