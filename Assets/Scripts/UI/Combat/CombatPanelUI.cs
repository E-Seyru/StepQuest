// Purpose: Main UI panel for displaying combat - handles health bars, abilities, and combat state
// Filepath: Assets/Scripts/UI/Combat/CombatPanelUI.cs

using CombatEvents;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main combat panel UI - displays the auto-battler combat
/// Subscribes to combat events via EventBus and updates UI accordingly
/// </summary>
public class CombatPanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject combatPanel;

    [Header("Player Display")]
    [SerializeField] private Image playerHealthBarFill;
    [SerializeField] private Image playerShieldBarFill;
    [SerializeField] private TextMeshProUGUI playerHealthText;
    [SerializeField] private TextMeshProUGUI playerShieldText;
    [SerializeField] private Image playerImage;
    [SerializeField] private RectTransform playerImageTransform;

    [Header("Enemy Display")]
    [SerializeField] private Image enemyHealthBarFill;
    [SerializeField] private Image enemyShieldBarFill;
    [SerializeField] private TextMeshProUGUI enemyHealthText;
    [SerializeField] private TextMeshProUGUI enemyShieldText;
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private Image enemyImage;
    [SerializeField] private RectTransform enemyImageTransform;

    [Header("Ability Displays")]
    [SerializeField] private CombatAbilityDisplay playerAbilityDisplay;
    [SerializeField] private CombatAbilityDisplay enemyAbilityDisplay;

    [Header("Status Effect Displays")]
    [SerializeField] private StatusEffectUI playerStatusEffects;
    [SerializeField] private StatusEffectUI enemyStatusEffects;
    [SerializeField] private List<StatusEffectPrefab> statusEffectPrefabs;

    [Header("Combat Popups")]
    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private GameObject healPopupPrefab;
    [SerializeField] private GameObject poisonPopupPrefab;
    [SerializeField] private GameObject shieldPopupPrefab;
    [SerializeField] private GameObject burnPopupPrefab;
    [SerializeField] private GameObject regenPopupPrefab;

    [Header("Combat Log")]
    [SerializeField] private TextMeshProUGUI combatLogText;
    [SerializeField] private ScrollRect combatLogScrollRect;
    [SerializeField] private int maxCombatLogLines = 30;

    [Header("Rewards Display")]
    [SerializeField] private GameObject rewardsSection;
    [SerializeField] private TextMeshProUGUI rewardsText;
    [SerializeField] private string xpColor = "#C8A2C8"; // Pale purple for XP
    [SerializeField] private string lootLabelColor = "#8B7355"; // Brownish for loot label

    [Header("Buttons")]
    [SerializeField] private Button fleeButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button startCombatButton;

    [Header("Animation Settings")]
    [SerializeField] private float healthBarAnimDuration = 0.5f;
    [SerializeField] private float attackAnimOffset = 75f;
    [SerializeField] private float attackAnimDuration = 0.3f;
    [SerializeField] private float hitScalePunch = 1.15f;

    // Runtime state
    private CombatData currentCombat;
    private EnemyDefinition currentEnemy;
    private EnemyDefinition selectedEnemy; // Enemy selected but combat not yet started
    private Vector2 playerStartPosition;
    private Vector2 enemyStartPosition;
    private bool isPlayerScalingInProgress = false;
    private bool isEnemyScalingInProgress = false;
    private bool isInitialized = false;
    private bool isCombatOver = false;
    private bool isInPreCombat = false; // Panel open but combat not started
    private List<string> combatLogLines = new List<string>(); // Buffer for combat log lines

    void Awake()
    {
        // Subscribe to combat events in Awake so it works even if GameObject starts disabled
        EventBus.Subscribe<CombatStartedEvent>(OnCombatStarted);
        EventBus.Subscribe<CombatEndedEvent>(OnCombatEnded);
        EventBus.Subscribe<CombatFledEvent>(OnCombatFled);
        EventBus.Subscribe<CombatHealthChangedEvent>(OnHealthChanged);
        EventBus.Subscribe<CombatAbilityUsedEvent>(OnAbilityUsed);
        EventBus.Subscribe<CombatPoisonTickEvent>(OnPoisonTick);
        EventBus.Subscribe<StatusEffectTickEvent>(OnStatusEffectTick);
    }

    void Start()
    {
        InitializeUI();
    }

    void OnEnable()
    {
        // Initialize UI if not already done (in case Start wasn't called yet)
        if (!isInitialized)
        {
            InitializeUI();
        }
    }

    private void InitializeUI()
    {
        if (isInitialized) return;

        // Setup buttons
        if (fleeButton != null)
        {
            fleeButton.onClick.AddListener(OnFleeButtonClicked);
        }
        if (leaveButton != null)
        {
            leaveButton.onClick.AddListener(OnLeaveButtonClicked);
        }
        if (startCombatButton != null)
        {
            startCombatButton.onClick.AddListener(OnStartCombatButtonClicked);
        }

        // Cache start positions for animations
        if (playerImageTransform != null)
            playerStartPosition = playerImageTransform.anchoredPosition;
        if (enemyImageTransform != null)
            enemyStartPosition = enemyImageTransform.anchoredPosition;

        // Initialize status effect displays with prefabs
        var prefabDict = BuildStatusEffectPrefabDictionary();
        if (playerStatusEffects != null) playerStatusEffects.Initialize(prefabDict);
        if (enemyStatusEffects != null) enemyStatusEffects.Initialize(prefabDict);

        // Hide combat panel at start (the visual content, not this script's GameObject)
        if (combatPanel != null)
        {
            combatPanel.SetActive(false);
        }

        isInitialized = true;
    }

    void OnDestroy()
    {
        // Cancel any pending Invoke calls to prevent null reference exceptions
        CancelInvoke(nameof(HidePanel));

        // Unsubscribe from events
        EventBus.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
        EventBus.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
        EventBus.Unsubscribe<CombatFledEvent>(OnCombatFled);
        EventBus.Unsubscribe<CombatHealthChangedEvent>(OnHealthChanged);
        EventBus.Unsubscribe<CombatAbilityUsedEvent>(OnAbilityUsed);
        EventBus.Unsubscribe<CombatPoisonTickEvent>(OnPoisonTick);
        EventBus.Unsubscribe<StatusEffectTickEvent>(OnStatusEffectTick);

        if (fleeButton != null)
        {
            fleeButton.onClick.RemoveListener(OnFleeButtonClicked);
        }
        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveListener(OnLeaveButtonClicked);
        }
        if (startCombatButton != null)
        {
            startCombatButton.onClick.RemoveListener(OnStartCombatButtonClicked);
        }
    }

    // === EVENT HANDLERS ===

    private void OnCombatStarted(CombatStartedEvent eventData)
    {
        currentCombat = eventData.Combat;
        currentEnemy = eventData.Enemy;
        isCombatOver = false;

        // Show panel
        if (combatPanel != null)
        {
            combatPanel.SetActive(true);
        }

        // Setup enemy display
        if (currentEnemy != null)
        {
            if (enemyNameText != null)
                enemyNameText.text = currentEnemy.GetDisplayName();

            if (enemyImage != null && currentEnemy.EnemySprite != null)
                enemyImage.sprite = currentEnemy.EnemySprite;
        }

        // Initialize health displays
        UpdateHealthDisplay(true, currentCombat.PlayerCurrentHealth, currentCombat.PlayerMaxHealth, currentCombat.PlayerCurrentShield, false);
        UpdateHealthDisplay(false, currentCombat.EnemyCurrentHealth, currentCombat.EnemyMaxHealth, currentCombat.EnemyCurrentShield, false);

        // Display abilities
        DisplayAbilities();

        // Clear status effects
        if (playerStatusEffects != null) playerStatusEffects.ClearAll();
        if (enemyStatusEffects != null) enemyStatusEffects.ClearAll();

        // Hide rewards section
        HideRewardsSection();

        // Show Flee button, hide Leave and Start Combat buttons during combat
        if (fleeButton != null) fleeButton.gameObject.SetActive(true);
        if (leaveButton != null) leaveButton.gameObject.SetActive(false);
        if (startCombatButton != null) startCombatButton.gameObject.SetActive(false);
        isInPreCombat = false;

        // Clear combat log
        ClearCombatLog();
        AddToCombatLog($"Combat contre {currentEnemy?.GetDisplayName()} !");

        Logger.LogInfo("CombatPanelUI: Combat panel shown", Logger.LogCategory.General);
    }

    private void DisplayAbilities()
    {
        // Display player abilities
        if (playerAbilityDisplay != null && CombatManager.Instance != null)
        {
            var playerAbilities = CombatManager.Instance.GetPlayerAbilities();
            playerAbilityDisplay.DisplayCombatAbilities(playerAbilities, true);
        }

        // Display enemy abilities
        if (enemyAbilityDisplay != null && currentEnemy != null)
        {
            enemyAbilityDisplay.DisplayCombatAbilities(currentEnemy.Abilities, false);
        }
    }

    private void OnCombatEnded(CombatEndedEvent eventData)
    {
        isCombatOver = true;
        string result = eventData.PlayerWon ? "Victoire !" : "Defaite...";
        AddToCombatLog($"\n=== {result} ===");

        if (eventData.PlayerWon)
        {
            // Build rewards for dedicated section
            DisplayRewardsSection(eventData.ExperienceGained, eventData.LootDropped);

            // Add rewards to combat log (bold + colored)
            if (eventData.ExperienceGained > 0)
            {
                AddToCombatLog($"<b><color={xpColor}>+{eventData.ExperienceGained} XP</color></b>");
            }

            if (eventData.LootDropped != null && eventData.LootDropped.Count > 0)
            {
                AddToCombatLog($"<b><color={lootLabelColor}>Butin:</color></b>");
                foreach (var loot in eventData.LootDropped)
                {
                    if (loot.Key != null && loot.Value > 0)
                    {
                        string itemColorHex = ColorUtility.ToHtmlStringRGB(loot.Key.GetRarityColor());
                        string itemName = loot.Key.GetDisplayName();
                        AddToCombatLog($"  <b><color=#{itemColorHex}>+{loot.Value} {itemName}</color></b>");
                    }
                }
            }
        }
        else
        {
            // Hide rewards section on defeat
            HideRewardsSection();
        }

        // Clear ability displays
        ClearAbilityDisplays();

        // Hide Flee button, show Leave and Start Combat buttons after combat
        if (fleeButton != null) fleeButton.gameObject.SetActive(false);
        if (leaveButton != null) leaveButton.gameObject.SetActive(true);
        if (startCombatButton != null) startCombatButton.gameObject.SetActive(true);
    }

    private void OnCombatFled(CombatFledEvent eventData)
    {
        isCombatOver = true;
        AddToCombatLog("\n=== Fuite ! ===");
        ClearAbilityDisplays();
        HideRewardsSection();

        // Hide Flee button, show Leave and Start Combat buttons after fleeing
        if (fleeButton != null) fleeButton.gameObject.SetActive(false);
        if (leaveButton != null) leaveButton.gameObject.SetActive(true);
        if (startCombatButton != null) startCombatButton.gameObject.SetActive(true);
    }

    private void OnHealthChanged(CombatHealthChangedEvent eventData)
    {
        UpdateHealthDisplay(
            eventData.IsPlayer,
            eventData.CurrentHealth,
            eventData.MaxHealth,
            eventData.CurrentShield,
            true // animate
        );
    }

    private void OnAbilityUsed(CombatAbilityUsedEvent eventData)
    {
        string actorName = eventData.IsPlayerAbility ? "Vous" : currentEnemy?.GetDisplayName() ?? "Ennemi";
        string targetName = eventData.IsPlayerAbility ? currentEnemy?.GetDisplayName() ?? "ennemi" : "vous";
        string abilityName = eventData.Ability?.GetDisplayName() ?? "?";

        // Determine target for popups (damage/poison go to target, heal/shield go to caster)
        RectTransform targetImage = eventData.IsPlayerAbility ? enemyImageTransform : playerImageTransform;
        RectTransform casterImage = eventData.IsPlayerAbility ? playerImageTransform : enemyImageTransform;

        // Build log message and spawn popups
        if (eventData.DamageDealt > 0)
        {
            if (eventData.IsPlayerAbility)
                AddToCombatLog($"{actorName} avez fait <color=#FF6B6B>{eventData.DamageDealt:F0}</color> degats a {targetName} avec {abilityName} !");
            else
                AddToCombatLog($"{actorName} vous a fait <color=#FF6B6B>{eventData.DamageDealt:F0}</color> degats avec {abilityName} !");

            SpawnPopup(damagePopupPrefab, eventData.DamageDealt, targetImage);
        }

        if (eventData.HealingDone > 0)
        {
            if (eventData.IsPlayerAbility)
                AddToCombatLog($"{actorName} vous etes soigne de <color=#6BFF6B>{eventData.HealingDone:F0}</color> PV avec {abilityName} !");
            else
                AddToCombatLog($"{actorName} s'est soigne de <color=#6BFF6B>{eventData.HealingDone:F0}</color> PV avec {abilityName} !");

            SpawnPopup(healPopupPrefab, eventData.HealingDone, casterImage);
        }

        if (eventData.ShieldAdded > 0)
        {
            if (eventData.IsPlayerAbility)
                AddToCombatLog($"{actorName} avez gagne <color=#6B6BFF>{eventData.ShieldAdded:F0}</color> bouclier avec {abilityName} !");
            else
                AddToCombatLog($"{actorName} a gagne <color=#6B6BFF>{eventData.ShieldAdded:F0}</color> bouclier avec {abilityName} !");

            SpawnPopup(shieldPopupPrefab, eventData.ShieldAdded, casterImage);
        }

        if (eventData.PoisonApplied > 0)
        {
            if (eventData.IsPlayerAbility)
                AddToCombatLog($"{actorName} avez empoisonne {targetName} de <color=#9B6BFF>{eventData.PoisonApplied:F0}</color> avec {abilityName} !");
            else
                AddToCombatLog($"{actorName} vous a empoisonne de <color=#9B6BFF>{eventData.PoisonApplied:F0}</color> avec {abilityName} !");

            // NOTE: Status effect display is now handled by StatusEffectUI via StatusEffectAppliedEvent
            // Removed legacy AddEffect call that was causing double-apply bug
        }

        // Animate character images like original system
        AnimateImage(eventData.IsPlayerAbility);
    }

    private void OnPoisonTick(CombatPoisonTickEvent eventData)
    {
        string target = eventData.IsPlayer ? "Vous souffrez" : $"{currentEnemy?.GetDisplayName() ?? "Ennemi"} souffre";
        AddToCombatLog($"{target} du poison ! <color=#9B6BFF>{eventData.PoisonDamage:F0}</color> degats subis !");

        // Spawn poison damage popup
        RectTransform targetImage = eventData.IsPlayer ? playerImageTransform : enemyImageTransform;
        SpawnPopup(poisonPopupPrefab, eventData.PoisonDamage, targetImage);

        // NOTE: Status effect stack display is now handled by StatusEffectUI via StatusEffectAppliedEvent
        // Removed legacy UpdateEffect call - stacks don't change on tick, only on new applications
    }

    private void OnStatusEffectTick(StatusEffectTickEvent eventData)
    {
        if (eventData.Effect == null) return;

        // Skip poison - handled by legacy OnPoisonTick for backwards compatibility
        if (eventData.Effect.EffectType == StatusEffectType.Poison) return;

        RectTransform targetImage = eventData.IsTargetPlayer ? playerImageTransform : enemyImageTransform;
        string targetName = eventData.IsTargetPlayer ? "Vous" : (currentEnemy?.GetDisplayName() ?? "Ennemi");

        // Handle different effect types
        if (eventData.Effect.IsDamageOverTime)
        {
            // Burn, Bleed, etc.
            string effectName = eventData.Effect.GetDisplayName();
            string colorHex = ColorUtility.ToHtmlStringRGB(eventData.Effect.EffectColor);
            AddToCombatLog($"{targetName} {(eventData.IsTargetPlayer ? "subissez" : "subit")} <color=#{colorHex}>{eventData.Value:F0}</color> degats de {effectName} !");

            // Use burn popup if available, otherwise damage popup
            GameObject popup = eventData.Effect.EffectType == StatusEffectType.Burn && burnPopupPrefab != null
                ? burnPopupPrefab
                : damagePopupPrefab;
            SpawnPopup(popup, eventData.Value, targetImage);
        }
        else if (eventData.Effect.IsHealOverTime)
        {
            // Regeneration, etc.
            string effectName = eventData.Effect.GetDisplayName();
            string colorHex = ColorUtility.ToHtmlStringRGB(eventData.Effect.EffectColor);
            AddToCombatLog($"{targetName} {(eventData.IsTargetPlayer ? "recuperez" : "recupere")} <color=#{colorHex}>{eventData.Value:F0}</color> PV ({effectName}) !");

            // Use regen popup if available, otherwise heal popup
            GameObject popup = regenPopupPrefab != null ? regenPopupPrefab : healPopupPrefab;
            SpawnPopup(popup, eventData.Value, targetImage);
        }
    }

    // === UI UPDATE METHODS ===

    private void UpdateHealthDisplay(bool isPlayer, float currentHealth, float maxHealth, float currentShield, bool animate)
    {
        Image healthBar = isPlayer ? playerHealthBarFill : enemyHealthBarFill;
        Image shieldBar = isPlayer ? playerShieldBarFill : enemyShieldBarFill;
        TextMeshProUGUI healthText = isPlayer ? playerHealthText : enemyHealthText;
        TextMeshProUGUI shieldText = isPlayer ? playerShieldText : enemyShieldText;

        float healthPercent = maxHealth > 0 ? currentHealth / maxHealth : 0;
        float shieldPercent = maxHealth > 0 ? Mathf.Min(1f, currentShield / maxHealth) : 0;

        if (animate && healthBar != null)
        {
            LeanTween.cancel(healthBar.gameObject);
            LeanTween.value(healthBar.gameObject, healthBar.fillAmount, healthPercent, healthBarAnimDuration)
                .setOnUpdate((float val) => healthBar.fillAmount = val);
        }
        else if (healthBar != null)
        {
            healthBar.fillAmount = healthPercent;
        }

        if (animate && shieldBar != null)
        {
            LeanTween.cancel(shieldBar.gameObject);
            LeanTween.value(shieldBar.gameObject, shieldBar.fillAmount, shieldPercent, healthBarAnimDuration)
                .setOnUpdate((float val) => shieldBar.fillAmount = val);
        }
        else if (shieldBar != null)
        {
            shieldBar.fillAmount = shieldPercent;
        }

        // Update text
        if (healthText != null)
            healthText.text = $"{Mathf.Ceil(currentHealth)}/{maxHealth}";

        if (shieldText != null)
        {
            shieldText.text = currentShield > 0 ? Mathf.Ceil(currentShield).ToString() : "";
            shieldText.gameObject.SetActive(currentShield > 0);
        }
    }

    private void AddToCombatLog(string message)
    {
        if (combatLogText == null) return;

        // Add new line to buffer
        combatLogLines.Add(message);

        // Trim buffer if it exceeds max lines
        while (combatLogLines.Count > maxCombatLogLines)
        {
            combatLogLines.RemoveAt(0);
        }

        // Rebuild text from buffer
        combatLogText.text = string.Join("\n", combatLogLines);

        // Schedule scroll update for end of frame to avoid expensive Canvas updates during combat
        if (combatLogScrollRect != null && !_scrollUpdatePending)
        {
            _scrollUpdatePending = true;
            StartCoroutine(ScrollToBottomEndOfFrame());
        }
    }

    private bool _scrollUpdatePending = false;

    private System.Collections.IEnumerator ScrollToBottomEndOfFrame()
    {
        yield return new WaitForEndOfFrame();

        if (combatLogText != null)
        {
            // Update rect height for scroll
            RectTransform textRect = combatLogText.GetComponent<RectTransform>();
            if (textRect != null)
            {
                float preferredHeight = combatLogText.preferredHeight;
                textRect.sizeDelta = new Vector2(textRect.sizeDelta.x, preferredHeight);
            }
        }

        // Auto-scroll to bottom
        if (combatLogScrollRect != null)
        {
            combatLogScrollRect.verticalNormalizedPosition = 0f;
        }

        _scrollUpdatePending = false;
    }

    private void ClearCombatLog()
    {
        combatLogLines.Clear();
        if (combatLogText != null)
        {
            combatLogText.text = "";
        }
    }

    // === ANIMATION METHODS (matching original system) ===

    private void AnimateImage(bool isPlayerAttacking)
    {
        Vector3 defaultScale = Vector3.one;

        if (isPlayerAttacking)
        {
            // Reset scales
            if (playerImageTransform != null)
                playerImageTransform.localScale = defaultScale;
            if (enemyImageTransform != null)
                enemyImageTransform.localScale = defaultScale;

            // Player attacks - move forward then back
            if (playerImageTransform != null)
            {
                LeanTween.sequence()
                    .append(LeanTween.moveX(playerImageTransform, playerStartPosition.x + attackAnimOffset, 0.10f).setEaseInQuad())
                    .append(LeanTween.moveX(playerImageTransform, playerStartPosition.x, 0.20f).setEaseOutBack());
            }

            // Enemy takes hit - scale punch
            if (enemyImageTransform != null && !isEnemyScalingInProgress)
            {
                isEnemyScalingInProgress = true;
                LeanTween.scale(enemyImageTransform, new Vector3(hitScalePunch, hitScalePunch, hitScalePunch), 0.40f)
                    .setEasePunch()
                    .setOnComplete(() =>
                    {
                        isEnemyScalingInProgress = false;
                        if (enemyImageTransform != null)
                            enemyImageTransform.localScale = defaultScale;
                    });
            }
        }
        else
        {
            // Reset scales
            if (playerImageTransform != null)
                playerImageTransform.localScale = defaultScale;
            if (enemyImageTransform != null)
                enemyImageTransform.localScale = defaultScale;

            // Enemy attacks - move forward then back
            if (enemyImageTransform != null)
            {
                LeanTween.sequence()
                    .append(LeanTween.moveX(enemyImageTransform, enemyStartPosition.x - attackAnimOffset, 0.10f).setEaseInQuad())
                    .append(LeanTween.moveX(enemyImageTransform, enemyStartPosition.x, 0.20f).setEaseOutBack());
            }

            // Player takes hit - scale punch
            if (playerImageTransform != null && !isPlayerScalingInProgress)
            {
                isPlayerScalingInProgress = true;
                LeanTween.scale(playerImageTransform, new Vector3(hitScalePunch, hitScalePunch, hitScalePunch), 0.40f)
                    .setEasePunch()
                    .setOnComplete(() =>
                    {
                        isPlayerScalingInProgress = false;
                        if (playerImageTransform != null)
                            playerImageTransform.localScale = defaultScale;
                    });
            }
        }
    }

    // === BUTTON HANDLERS ===

    private void OnFleeButtonClicked()
    {
        CombatManager.Instance?.FleeCombat();
    }

    private void OnLeaveButtonClicked()
    {
        HidePanel();
    }

    private void OnStartCombatButtonClicked()
    {
        if (selectedEnemy == null)
        {
            Logger.LogWarning("CombatPanelUI: No enemy selected for combat", Logger.LogCategory.General);
            return;
        }

        if (CombatManager.Instance != null && CombatManager.Instance.CanStartCombat())
        {
            isInPreCombat = false;
            CombatManager.Instance.StartCombat(selectedEnemy);
        }
        else
        {
            Logger.LogWarning("CombatPanelUI: Cannot start combat - conditions not met", Logger.LogCategory.General);
        }
    }

    // === HELPER METHODS ===

    private void DisplayRewardsSection(int experienceGained, Dictionary<ItemDefinition, int> lootDropped)
    {
        if (rewardsSection != null)
        {
            rewardsSection.SetActive(true);
        }

        if (rewardsText != null)
        {
            var sb = new System.Text.StringBuilder();

            // XP line
            if (experienceGained > 0)
            {
                sb.AppendLine($"<b><color={xpColor}>+{experienceGained} XP</color></b>");
            }

            // Loot lines
            if (lootDropped != null && lootDropped.Count > 0)
            {
                foreach (var loot in lootDropped)
                {
                    if (loot.Key != null && loot.Value > 0)
                    {
                        string itemColorHex = ColorUtility.ToHtmlStringRGB(loot.Key.GetRarityColor());
                        string itemName = loot.Key.GetDisplayName();
                        sb.AppendLine($"<b><color=#{itemColorHex}>+{loot.Value} {itemName}</color></b>");
                    }
                }
            }

            rewardsText.text = sb.ToString().TrimEnd();
        }
    }

    private void HideRewardsSection()
    {
        if (rewardsSection != null)
        {
            rewardsSection.SetActive(false);
        }

        if (rewardsText != null)
        {
            rewardsText.text = "";
        }
    }

    private void SpawnPopup(GameObject prefab, float amount, RectTransform characterImage)
    {
        if (prefab == null || characterImage == null) return;

        // Spawn popup as child of character image's parent
        GameObject popup = Instantiate(prefab, characterImage.parent);
        CombatPopup combatPopup = popup.GetComponent<CombatPopup>();
        if (combatPopup != null)
        {
            combatPopup.Setup(amount, characterImage);
        }
    }

    private void ClearAbilityDisplays()
    {
        if (playerAbilityDisplay != null)
            playerAbilityDisplay.ClearDisplays();
        if (enemyAbilityDisplay != null)
            enemyAbilityDisplay.ClearDisplays();
    }

    private void HidePanel()
    {
        if (combatPanel != null)
        {
            combatPanel.SetActive(false);
        }

        currentCombat = null;
        currentEnemy = null;

        ClearCombatLog();
        ClearAbilityDisplays();
    }

    /// <summary>
    /// Called from EnemySelectionUI when an enemy is selected - shows panel in pre-combat state
    /// </summary>
    public void ShowPreCombat(EnemyDefinition enemy)
    {
        if (enemy == null) return;

        selectedEnemy = enemy;
        isInPreCombat = true;
        isCombatOver = false;

        // Show panel
        if (combatPanel != null)
        {
            combatPanel.SetActive(true);
        }

        // Setup enemy display
        if (enemyNameText != null)
            enemyNameText.text = enemy.GetDisplayName();

        if (enemyImage != null && enemy.EnemySprite != null)
            enemyImage.sprite = enemy.EnemySprite;

        // Clear/hide combat-specific displays
        ClearCombatLog();
        ClearAbilityDisplays();
        HideRewardsSection();

        // Clear status effects
        if (playerStatusEffects != null) playerStatusEffects.ClearAll();
        if (enemyStatusEffects != null) enemyStatusEffects.ClearAll();

        // Reset health bars to full (visual only, combat hasn't started)
        if (playerHealthBarFill != null) playerHealthBarFill.fillAmount = 1f;
        if (enemyHealthBarFill != null) enemyHealthBarFill.fillAmount = 1f;
        if (playerShieldBarFill != null) playerShieldBarFill.fillAmount = 0f;
        if (enemyShieldBarFill != null) enemyShieldBarFill.fillAmount = 0f;

        // Show Start Combat and Leave buttons, hide Flee
        if (startCombatButton != null) startCombatButton.gameObject.SetActive(true);
        if (leaveButton != null) leaveButton.gameObject.SetActive(true);
        if (fleeButton != null) fleeButton.gameObject.SetActive(false);

        AddToCombatLog($"Pret a combattre {enemy.GetDisplayName()} ?");

        Logger.LogInfo($"CombatPanelUI: Pre-combat shown for {enemy.GetDisplayName()}", Logger.LogCategory.General);
    }

    private Dictionary<StatusEffectType, GameObject> BuildStatusEffectPrefabDictionary()
    {
        var dict = new Dictionary<StatusEffectType, GameObject>();
        if (statusEffectPrefabs != null)
        {
            foreach (var entry in statusEffectPrefabs)
            {
                if (entry.prefab != null && !dict.ContainsKey(entry.type))
                {
                    dict[entry.type] = entry.prefab;
                }
            }
        }
        return dict;
    }
}

// StatusEffectType enum moved to StatusEffectDefinition.cs

/// <summary>
/// Maps a status effect type to its UI prefab
/// </summary>
[System.Serializable]
public class StatusEffectPrefab
{
    public StatusEffectType type;
    public GameObject prefab;
}
