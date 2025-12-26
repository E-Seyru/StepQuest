using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

/// <summary>
/// Runtime debug panel for controlling Logger settings
/// Attach this to a UI Canvas to get runtime control over logging
/// </summary>
public class LoggerDebugPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Dropdown logLevelDropdown;
    [SerializeField] private Toggle loggerEnabledToggle;
    [SerializeField] private Transform categoryTogglesContainer;
    [SerializeField] private GameObject categoryTogglePrefab;

    [Header("Open/Close")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [Header("Shortcuts")]
    [SerializeField] private KeyCode togglePanelKey = KeyCode.F12;

    private void Start()
    {
        InitializePanel();

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        if (openButton != null)
        {
            openButton.onClick.AddListener(() => SetPanelVisible(true));
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => SetPanelVisible(false));
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(togglePanelKey))
        {
            TogglePanel();
        }
    }

    private void InitializePanel()
    {
        // Initialize log level dropdown
        if (logLevelDropdown != null)
        {
            logLevelDropdown.ClearOptions();
            var levelNames = Enum.GetNames(typeof(Logger.LogLevel)).ToList();
            logLevelDropdown.AddOptions(levelNames);
            logLevelDropdown.value = (int)Logger.CurrentLogLevel;
            logLevelDropdown.onValueChanged.AddListener(OnLogLevelChanged);
        }

        // Initialize logger enabled toggle
        if (loggerEnabledToggle != null)
        {
            loggerEnabledToggle.isOn = true; // Logger is enabled by default
            loggerEnabledToggle.onValueChanged.AddListener(OnLoggerEnabledChanged);
        }

        // Create category toggles
        if (categoryTogglesContainer != null && categoryTogglePrefab != null)
        {
            var categories = Enum.GetValues(typeof(Logger.LogCategory));
            foreach (Logger.LogCategory category in categories)
            {
                if (category == Logger.LogCategory.None) continue; // Skip None

                GameObject toggleObj = Instantiate(categoryTogglePrefab, categoryTogglesContainer);
                Toggle toggle = toggleObj.GetComponent<Toggle>();
                TMP_Text label = toggleObj.GetComponentInChildren<TMP_Text>();

                if (label != null)
                {
                    label.text = category.ToString();
                }

                if (toggle != null)
                {
                    // All categories enabled by default (no filtering)
                    toggle.isOn = !Logger.IsUsingCategoryFiltering() || Logger.IsCategoryEnabled(category);
                    toggle.onValueChanged.AddListener((isOn) => OnCategoryToggleChanged(category, isOn));
                }
            }
        }
    }

    private void OnLogLevelChanged(int index)
    {
        Logger.LogLevel newLevel = (Logger.LogLevel)index;
        Logger.SetLogLevel(newLevel);
        Debug.Log($"[LoggerDebugPanel] Log level changed to: {newLevel}");
    }

    private void OnLoggerEnabledChanged(bool isEnabled)
    {
        Logger.SetEnabled(isEnabled);
        Debug.Log($"[LoggerDebugPanel] Logger {(isEnabled ? "enabled" : "disabled")}");
    }

    private void OnCategoryToggleChanged(Logger.LogCategory category, bool isEnabled)
    {
        if (isEnabled)
        {
            Logger.EnableCategory(category);
        }
        else
        {
            Logger.DisableCategory(category);
        }

        Debug.Log($"[LoggerDebugPanel] Category {category} {(isEnabled ? "enabled" : "disabled")}");
    }

    public void TogglePanel()
    {
        if (panelRoot != null)
        {
            SetPanelVisible(!panelRoot.activeSelf);
        }
    }

    public void SetPanelVisible(bool visible)
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(visible);
        }
    }

    public void EnableAllCategories()
    {
        Logger.DisableCategoryFiltering();
        RefreshCategoryToggles(true);
    }

    public void DisableAllCategories()
    {
        Logger.DisableCategoryFiltering();
        RefreshCategoryToggles(false);
    }

    private void RefreshCategoryToggles(bool state)
    {
        if (categoryTogglesContainer != null)
        {
            foreach (Toggle toggle in categoryTogglesContainer.GetComponentsInChildren<Toggle>())
            {
                toggle.SetIsOnWithoutNotify(state);
            }
        }
    }

    public void QuickSetCombatOnly()
    {
        Logger.EnableCategoryFiltering(Logger.LogCategory.CombatLog);
        RefreshFromLogger();
    }

    public void QuickSetStepOnly()
    {
        Logger.EnableCategoryFiltering(Logger.LogCategory.StepLog);
        RefreshFromLogger();
    }

    public void QuickSetMapOnly()
    {
        Logger.EnableCategoryFiltering(Logger.LogCategory.MapLog);
        RefreshFromLogger();
    }

    private void RefreshFromLogger()
    {
        if (categoryTogglesContainer != null)
        {
            var toggles = categoryTogglesContainer.GetComponentsInChildren<Toggle>();
            var labels = categoryTogglesContainer.GetComponentsInChildren<TMP_Text>();

            for (int i = 0; i < toggles.Length; i++)
            {
                if (i < labels.Length && labels[i] != null)
                {
                    string categoryName = labels[i].text;
                    if (Enum.TryParse<Logger.LogCategory>(categoryName, out var category))
                    {
                        bool isEnabled = !Logger.IsUsingCategoryFiltering() || Logger.IsCategoryEnabled(category);
                        toggles[i].SetIsOnWithoutNotify(isEnabled);
                    }
                }
            }
        }
    }
}
