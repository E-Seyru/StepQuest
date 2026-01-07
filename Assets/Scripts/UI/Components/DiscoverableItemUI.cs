// Purpose: UI component for displaying a discoverable item in the exploration panel
// Filepath: Assets/Scripts/UI/Components/DiscoverableItemUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component for displaying a single discoverable item (enemy, NPC, activity) in the exploration panel.
/// Shows icon (blackened if undiscovered), name (??? if undiscovered), rarity background, and discovered checkmark.
/// </summary>
public class DiscoverableItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image rarityBackground;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject discoveredCheckmark;

    [Header("Rarity Colors")]
    [SerializeField] private Color commonColor = new Color(0.8f, 0.8f, 0.8f);
    [SerializeField] private Color uncommonColor = new Color(0.12f, 0.8f, 0f);
    [SerializeField] private Color rareColor = new Color(0f, 0.44f, 0.87f);
    [SerializeField] private Color epicColor = new Color(0.64f, 0.21f, 0.93f);
    [SerializeField] private Color legendaryColor = new Color(1f, 0.5f, 0f);

    [Header("Undiscovered State")]
    [SerializeField] private Color undiscoveredIconColor = Color.black;
    [SerializeField] private float undiscoveredIconAlpha = 0.7f;
    [SerializeField] private Color undiscoveredNameColor = new Color(0.5f, 0.5f, 0.5f);

    // Current data
    private DiscoverableInfo currentInfo;

    /// <summary>
    /// Setup the item with discoverable info
    /// </summary>
    public void Setup(DiscoverableInfo info)
    {
        currentInfo = info;

        // Set icon
        UpdateIcon();

        // Set name (??? if not discovered)
        if (nameText != null)
        {
            nameText.text = info.IsDiscovered ? info.Name : "???";
            nameText.color = info.IsDiscovered ? Color.white : undiscoveredNameColor;
        }

        // Set rarity background color
        if (rarityBackground != null)
        {
            Color bgColor = GetRarityColor(info.Rarity);
            bgColor.a = info.IsDiscovered ? 0.4f : 0.2f;
            rarityBackground.color = bgColor;
        }

        // Show checkmark only if discovered
        if (discoveredCheckmark != null)
        {
            discoveredCheckmark.SetActive(info.IsDiscovered);
        }
    }

    /// <summary>
    /// Update the icon based on discovery status
    /// </summary>
    private void UpdateIcon()
    {
        if (iconImage == null) return;

        // Use the icon from info (DiscoveredItemIcon or fallback to regular icon)
        if (currentInfo.Icon != null)
        {
            iconImage.sprite = currentInfo.Icon;
        }

        if (currentInfo.IsDiscovered)
        {
            // Normal display
            iconImage.color = Color.white;
        }
        else
        {
            // Blackened and faded
            Color blackened = undiscoveredIconColor;
            blackened.a = undiscoveredIconAlpha;
            iconImage.color = blackened;
        }
    }

    /// <summary>
    /// Get color for rarity level
    /// </summary>
    private Color GetRarityColor(DiscoveryRarity rarity)
    {
        switch (rarity)
        {
            case DiscoveryRarity.Common:
                return commonColor;
            case DiscoveryRarity.Uncommon:
                return uncommonColor;
            case DiscoveryRarity.Rare:
                return rareColor;
            case DiscoveryRarity.Epic:
                return epicColor;
            case DiscoveryRarity.Legendary:
                return legendaryColor;
            default:
                return commonColor;
        }
    }
}
