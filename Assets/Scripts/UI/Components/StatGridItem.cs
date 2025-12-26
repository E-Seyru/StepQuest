// Purpose: Simple component for stat grid items to expose icon reference
// Filepath: Assets/Scripts/UI/Components/StatGridItem.cs

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Component attached to stat grid item prefabs.
/// Exposes references for icon and text so they can be easily assigned in the Inspector.
/// </summary>
public class StatGridItem : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Image component that displays the stat icon")]
    [SerializeField] private Image iconImage;

    [Tooltip("The TextMeshProUGUI component that displays the stat value")]
    [SerializeField] private TextMeshProUGUI valueText;

    /// <summary>
    /// Get the icon Image component
    /// </summary>
    public Image IconImage => iconImage;

    /// <summary>
    /// Get the value text component
    /// </summary>
    public TextMeshProUGUI ValueText => valueText;

    /// <summary>
    /// Set the stat display (icon sprite + value text)
    /// </summary>
    public void SetStat(Sprite icon, string value)
    {
        if (iconImage != null && icon != null)
        {
            iconImage.sprite = icon;
        }

        if (valueText != null)
        {
            valueText.text = value;
        }
    }
}
