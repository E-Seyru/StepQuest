// Purpose: UI for selecting enemies at a location to start combat
// Filepath: Assets/Scripts/UI/Combat/EnemySelectionUI.cs

using System.Collections.Generic;
using MapEvents;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays available enemies at the current location and allows player to start combat
/// </summary>
public class EnemySelectionUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject enemySelectionPanel;

    [Header("Enemy List")]
    [SerializeField] private Transform enemyListContainer;
    [SerializeField] private GameObject enemyButtonPrefab;

    [Header("No Enemies")]
    [SerializeField] private GameObject noEnemiesMessage;

    [Header("Location Display")]
    [SerializeField] private TextMeshProUGUI locationNameText;

    // Cache
    private MapLocationDefinition currentLocation;
    private List<GameObject> spawnedButtons = new List<GameObject>();

    void Start()
    {
        // Subscribe to location changes
        EventBus.Subscribe<LocationChangedEvent>(OnLocationChanged);

        // Initialize with current location if available
        UpdateCurrentLocation();
    }

    void OnDestroy()
    {
        EventBus.Unsubscribe<LocationChangedEvent>(OnLocationChanged);
    }

    private void OnLocationChanged(LocationChangedEvent eventData)
    {
        UpdateForLocation(eventData.NewLocation);
    }

    private void UpdateCurrentLocation()
    {
        if (DataManager.Instance?.PlayerData == null) return;

        string currentLocationId = DataManager.Instance.PlayerData.CurrentLocationId;
        if (string.IsNullOrEmpty(currentLocationId)) return;

        // Get location from MapManager's LocationRegistry
        if (MapManager.Instance?.LocationRegistry != null)
        {
            var location = MapManager.Instance.LocationRegistry.GetLocationById(currentLocationId);
            UpdateForLocation(location);
        }
    }

    /// <summary>
    /// Update the UI for a specific location
    /// </summary>
    public void UpdateForLocation(MapLocationDefinition location)
    {
        currentLocation = location;

        // Clear existing buttons
        ClearEnemyButtons();

        if (location == null)
        {
            ShowNoEnemies(true);
            return;
        }

        // Update location name
        if (locationNameText != null)
        {
            locationNameText.text = location.DisplayName;
        }

        // Get available enemies
        var enemies = location.GetAvailableEnemies();

        if (enemies == null || enemies.Count == 0)
        {
            ShowNoEnemies(true);
            return;
        }

        ShowNoEnemies(false);

        // Create buttons for each enemy
        foreach (var locationEnemy in enemies)
        {
            if (locationEnemy?.EnemyReference == null) continue;

            CreateEnemyButton(locationEnemy);
        }
    }

    private void CreateEnemyButton(LocationEnemy locationEnemy)
    {
        if (enemyButtonPrefab == null || enemyListContainer == null) return;

        GameObject buttonObj = Instantiate(enemyButtonPrefab, enemyListContainer);
        spawnedButtons.Add(buttonObj);

        var enemy = locationEnemy.EnemyReference;

        // Setup button visuals
        var buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = enemy.GetDisplayName();
        }

        var buttonImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
        if (buttonImage != null && enemy.EnemySprite != null)
        {
            buttonImage.sprite = enemy.EnemySprite;
        }

        // Setup button click
        var button = buttonObj.GetComponent<Button>();
        if (button != null)
        {
            // Capture enemy reference for closure
            EnemyDefinition capturedEnemy = enemy;
            button.onClick.AddListener(() => OnEnemySelected(capturedEnemy));
        }

        // Disable if can't fight
        if (!locationEnemy.CanFight() && button != null)
        {
            button.interactable = false;
        }
    }

    private void ClearEnemyButtons()
    {
        foreach (var buttonObj in spawnedButtons)
        {
            if (buttonObj != null)
            {
                // Remove listeners before destroying to prevent memory leaks
                var button = buttonObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
                Destroy(buttonObj);
            }
        }
        spawnedButtons.Clear();
    }

    private void ShowNoEnemies(bool show)
    {
        if (noEnemiesMessage != null)
        {
            noEnemiesMessage.SetActive(show);
        }

        if (enemyListContainer != null)
        {
            enemyListContainer.gameObject.SetActive(!show);
        }
    }

    private void OnEnemySelected(EnemyDefinition enemy)
    {
        if (enemy == null) return;

        // Find CombatPanelUI and show pre-combat screen
        var combatPanelUI = FindObjectOfType<CombatPanelUI>();
        if (combatPanelUI != null)
        {
            combatPanelUI.ShowPreCombat(enemy);
            Logger.LogInfo($"EnemySelectionUI: Showing pre-combat for {enemy.GetDisplayName()}", Logger.LogCategory.General);
        }
        else
        {
            Logger.LogWarning("EnemySelectionUI: CombatPanelUI not found", Logger.LogCategory.General);
        }
    }

    /// <summary>
    /// Show the enemy selection panel
    /// </summary>
    public void Show()
    {
        if (enemySelectionPanel != null)
        {
            enemySelectionPanel.SetActive(true);
        }
        UpdateCurrentLocation();
    }

    /// <summary>
    /// Hide the enemy selection panel
    /// </summary>
    public void Hide()
    {
        if (enemySelectionPanel != null)
        {
            enemySelectionPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Check if current location has combat available
    /// </summary>
    public bool HasCombatAvailable()
    {
        return currentLocation != null && currentLocation.HasCombat();
    }
}
