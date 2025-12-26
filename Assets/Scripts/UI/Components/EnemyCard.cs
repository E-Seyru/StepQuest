// Purpose: UI Card for displaying an enemy that can be fought
// Filepath: Assets/Scripts/UI/Components/EnemyCard.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Card component for displaying enemy information and initiating combat.
/// Similar to PrimaryActivityCard but for enemies.
/// </summary>
public class EnemyCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private Image enemyImage;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI rewardsText;
    [SerializeField] private Button cardButton;

    [Header("Optional UI")]
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private TextMeshProUGUI requirementText;

    // Data
    private EnemyDefinition enemyDefinition;
    private LocationEnemy locationEnemy;
    private bool canFight = true;

    // Events
    public System.Action<EnemyDefinition> OnCardClicked;

    void Start()
    {
        if (cardButton != null)
        {
            cardButton.onClick.AddListener(OnCardButtonClicked);
        }
    }

    /// <summary>
    /// Setup the card with enemy data from a location
    /// </summary>
    public void Setup(LocationEnemy locEnemy)
    {
        if (locEnemy == null || !locEnemy.IsValid())
        {
            Logger.LogWarning("EnemyCard: Cannot setup with null or invalid LocationEnemy!", Logger.LogCategory.CombatLog);
            return;
        }

        locationEnemy = locEnemy;
        enemyDefinition = locEnemy.EnemyReference;
        canFight = locEnemy.CanFight();

        SetupBasicInfo();
        SetupRewardsInfo();
        UpdateCardState(canFight);

        Logger.LogInfo($"EnemyCard: Setup completed for {enemyDefinition.GetDisplayName()}", Logger.LogCategory.CombatLog);
    }

    /// <summary>
    /// Setup the card directly with an EnemyDefinition
    /// </summary>
    public void Setup(EnemyDefinition enemy)
    {
        if (enemy == null)
        {
            Logger.LogWarning("EnemyCard: Cannot setup with null enemy!", Logger.LogCategory.CombatLog);
            return;
        }

        enemyDefinition = enemy;
        locationEnemy = null;
        canFight = true;

        SetupBasicInfo();
        SetupRewardsInfo();
        UpdateCardState(canFight);

        Logger.LogInfo($"EnemyCard: Setup completed for {enemy.GetDisplayName()}", Logger.LogCategory.CombatLog);
    }

    /// <summary>
    /// Setup basic card information (name, image, level)
    /// </summary>
    private void SetupBasicInfo()
    {
        // Name
        if (nameText != null)
        {
            nameText.text = enemyDefinition.GetDisplayName();
        }

        // Enemy avatar (fallback to EnemySprite if no avatar set)
        if (enemyImage != null)
        {
            Sprite displaySprite = enemyDefinition.Avatar != null ? enemyDefinition.Avatar : enemyDefinition.EnemySprite;
            if (displaySprite != null)
            {
                enemyImage.sprite = displaySprite;
                enemyImage.preserveAspect = true;
            }
        }

        // Level indicator
        if (levelText != null)
        {
            levelText.text = $"Niv. {enemyDefinition.Level}";
        }
    }

    /// <summary>
    /// Setup rewards information (XP, potential loot)
    /// </summary>
    private void SetupRewardsInfo()
    {
        if (rewardsText != null)
        {
            string rewards = $"+{enemyDefinition.ExperienceReward} XP";

            // Add loot count if available
            if (enemyDefinition.LootTable != null && enemyDefinition.LootTable.Count > 0)
            {
                rewards += $"\n{enemyDefinition.LootTable.Count} loot possible";
            }

            rewardsText.text = rewards;
        }
    }

    /// <summary>
    /// Handle card button click
    /// </summary>
    private void OnCardButtonClicked()
    {
        if (!canFight)
        {
            Logger.LogInfo($"EnemyCard: Cannot fight {enemyDefinition.GetDisplayName()} - requirements not met", Logger.LogCategory.CombatLog);
            return;
        }

        if (enemyDefinition != null)
        {
            Logger.LogInfo($"EnemyCard: Card clicked for {enemyDefinition.GetDisplayName()}", Logger.LogCategory.CombatLog);
            OnCardClicked?.Invoke(enemyDefinition);
        }
    }

    /// <summary>
    /// Update the card's visual state based on availability
    /// </summary>
    public void UpdateCardState(bool isAvailable)
    {
        canFight = isAvailable;

        if (cardButton != null)
        {
            cardButton.interactable = isAvailable;
        }

        // Show/hide locked overlay
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!isAvailable);
        }

        // Show requirement text if locked
        if (requirementText != null && locationEnemy != null)
        {
            if (!isAvailable && !string.IsNullOrEmpty(locationEnemy.Requirements))
            {
                requirementText.gameObject.SetActive(true);
                requirementText.text = locationEnemy.Requirements;
            }
            else
            {
                requirementText.gameObject.SetActive(false);
            }
        }

        // Dim the card if not available
        if (enemyImage != null)
        {
            enemyImage.color = isAvailable ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }

    /// <summary>
    /// Get the enemy definition this card represents
    /// </summary>
    public EnemyDefinition GetEnemyDefinition()
    {
        return enemyDefinition;
    }

    void OnDestroy()
    {
        if (cardButton != null)
        {
            cardButton.onClick.RemoveListener(OnCardButtonClicked);
        }
    }
}
