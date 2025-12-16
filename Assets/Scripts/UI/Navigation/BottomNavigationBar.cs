// Purpose: Bottom navigation bar with icons for quick panel switching
// Filepath: Assets/Scripts/UI/Navigation/BottomNavigationBar.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BottomNavigationBar : MonoBehaviour
{
    [System.Serializable]
    public class NavItem
    {
        public Button button;
        public Image icon;
        public int panelIndex;
    }

    [Header("Navigation Items")]
    [SerializeField] private List<NavItem> navItems = new List<NavItem>();

    [Header("Selection Style")]
    [SerializeField] private float selectedScale = 1.2f;
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color normalColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private float animationDuration = 0.15f;

    private int currentSelectedIndex = -1;
    private PanelManager panelManager;

    void Start()
    {
        panelManager = PanelManager.Instance;

        if (panelManager == null)
        {
            Logger.LogError("BottomNavigationBar: PanelManager not found!", Logger.LogCategory.General);
            return;
        }

        // Setup button click handlers
        for (int i = 0; i < navItems.Count; i++)
        {
            int index = i; // Capture for closure
            if (navItems[i].button != null)
            {
                navItems[i].button.onClick.AddListener(() => OnNavItemClicked(index));
            }
        }

        // Subscribe to panel change events
        panelManager.OnPanelChanged.AddListener(OnPanelChanged);

        // Initialize to current panel
        OnPanelChanged(panelManager.CurrentPanelIndex);
    }

    void OnDestroy()
    {
        if (panelManager != null)
        {
            panelManager.OnPanelChanged.RemoveListener(OnPanelChanged);
        }

        // Clean up button listeners
        foreach (var item in navItems)
        {
            if (item.button != null)
            {
                item.button.onClick.RemoveAllListeners();
            }
        }
    }

    private void OnNavItemClicked(int navIndex)
    {
        if (navIndex < 0 || navIndex >= navItems.Count) return;

        int panelIndex = navItems[navIndex].panelIndex;

        // Use HideMapAndGoToPanel to handle both map and panel navigation
        panelManager.HideMapAndGoToPanel(panelIndex);
    }

    private void OnPanelChanged(int newPanelIndex)
    {
        // Find which nav item corresponds to this panel
        int navIndex = FindNavIndexForPanel(newPanelIndex);

        if (navIndex == currentSelectedIndex) return;

        // Deselect previous
        if (currentSelectedIndex >= 0 && currentSelectedIndex < navItems.Count)
        {
            AnimateNavItem(currentSelectedIndex, false);
        }

        // Select new
        if (navIndex >= 0 && navIndex < navItems.Count)
        {
            AnimateNavItem(navIndex, true);
        }

        currentSelectedIndex = navIndex;
    }

    private int FindNavIndexForPanel(int panelIndex)
    {
        for (int i = 0; i < navItems.Count; i++)
        {
            if (navItems[i].panelIndex == panelIndex)
            {
                return i;
            }
        }
        return -1;
    }

    private void AnimateNavItem(int navIndex, bool selected)
    {
        NavItem item = navItems[navIndex];
        if (item.icon == null) return;

        float targetScale = selected ? selectedScale : normalScale;
        Color targetColor = selected ? selectedColor : normalColor;

        // Cancel any existing animations on this icon
        LeanTween.cancel(item.icon.gameObject);

        // Animate scale
        LeanTween.scale(item.icon.gameObject, Vector3.one * targetScale, animationDuration)
            .setEase(LeanTweenType.easeOutBack);

        // Animate color
        LeanTween.color(item.icon.rectTransform, targetColor, animationDuration)
            .setEase(LeanTweenType.easeOutQuad);
    }

    /// <summary>
    /// Manually select a nav item (useful for external control)
    /// </summary>
    public void SelectNavItem(int navIndex)
    {
        if (navIndex >= 0 && navIndex < navItems.Count)
        {
            OnNavItemClicked(navIndex);
        }
    }

    /// <summary>
    /// Get the currently selected nav index
    /// </summary>
    public int CurrentSelectedIndex => currentSelectedIndex;
}
