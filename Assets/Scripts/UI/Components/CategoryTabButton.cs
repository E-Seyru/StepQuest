// Purpose: Simple tab button component that displays category data passed to it
// Filepath: Assets/Scripts/UI/Components/CategoryTabButton.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component for category tab buttons.
/// Dumb component that just displays data passed to it - no registry lookup.
/// </summary>
public class CategoryTabButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Image backgroundImage;

    // State
    private string categoryId;
    private bool isAllTab = false;

    /// <summary>
    /// Setup the tab with category data
    /// </summary>
    /// <param name="category">Category ID (null for "All" tab)</param>
    /// <param name="icon">Icon sprite to display</param>
    /// <param name="label">Label text to display</param>
    public void Setup(string category, Sprite icon, string label)
    {
        categoryId = category;
        isAllTab = string.IsNullOrEmpty(category);

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.gameObject.SetActive(icon != null);
        }

        if (labelText != null)
        {
            labelText.text = label;
        }
    }

    /// <summary>
    /// Set the visual state (selected/unselected)
    /// </summary>
    public void SetSelected(bool selected, Color activeColor, Color inactiveColor)
    {
        Color targetColor = selected ? activeColor : inactiveColor;

        if (backgroundImage != null)
        {
            backgroundImage.color = targetColor;
        }

        if (iconImage != null)
        {
            iconImage.color = targetColor;
        }

        if (labelText != null)
        {
            labelText.color = targetColor;
        }
    }

    /// <summary>
    /// Get the category ID this tab represents (null for "All" tab)
    /// </summary>
    public string GetCategoryId() => categoryId;

    /// <summary>
    /// Check if this is the "All" tab
    /// </summary>
    public bool IsAllTab => isAllTab;
}
