// Purpose: UI component for displaying a discoverable item in the exploration panel
// Filepath: Assets/Scripts/UI/Components/DiscoverableItemUI.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI component for displaying a single discoverable item (enemy, NPC, dungeon) in the exploration panel.
/// Shows name, rarity, type, and discovery status.
/// </summary>
public class DiscoverableItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image iconBackground;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private TextMeshProUGUI bonusXPText;
    [SerializeField] private GameObject discoveredOverlay;
    [SerializeField] private GameObject unknownOverlay;
    [SerializeField] private Image rarityBorder;

    [Header("Rarity Colors")]
    [SerializeField] private Color commonColor = Color.white;
    [SerializeField] private Color uncommonColor = new Color(0.12f, 1f, 0f); // Green
    [SerializeField] private Color rareColor = new Color(0f, 0.44f, 0.87f); // Blue
    [SerializeField] private Color epicColor = new Color(0.64f, 0.21f, 0.93f); // Purple
    [SerializeField] private Color legendaryColor = new Color(1f, 0.5f, 0f); // Orange

    [Header("Unknown State")]
    [SerializeField] private Sprite unknownIcon;
    [SerializeField] private Color unknownColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);

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

        // Set name
        if (nameText != null)
        {
            nameText.text = info.IsDiscovered ? info.Name : "???";
            nameText.color = info.IsDiscovered ? GetRarityColor(info.Rarity) : unknownColor;
        }

        // Set type
        if (typeText != null)
        {
            typeText.text = GetTypeString(info.Type);
        }

        // Set rarity
        if (rarityText != null)
        {
            rarityText.text = GetRarityString(info.Rarity);
            rarityText.color = GetRarityColor(info.Rarity);
        }

        // Set bonus XP
        if (bonusXPText != null)
        {
            if (info.IsDiscovered)
            {
                bonusXPText.text = $"+{info.BonusXP} XP";
                bonusXPText.gameObject.SetActive(false); // Already discovered, no bonus
            }
            else
            {
                bonusXPText.text = $"+{info.BonusXP} XP";
                bonusXPText.gameObject.SetActive(true);
            }
        }

        // Set rarity border
        if (rarityBorder != null)
        {
            rarityBorder.color = GetRarityColor(info.Rarity);
        }

        // Set overlays
        if (discoveredOverlay != null)
        {
            discoveredOverlay.SetActive(info.IsDiscovered);
        }

        if (unknownOverlay != null)
        {
            unknownOverlay.SetActive(!info.IsDiscovered);
        }
    }

    /// <summary>
    /// Update the icon based on discovery status
    /// </summary>
    private void UpdateIcon()
    {
        if (iconImage == null) return;

        if (currentInfo.IsDiscovered && currentInfo.Icon != null)
        {
            iconImage.sprite = currentInfo.Icon;
            iconImage.color = Color.white;
        }
        else if (unknownIcon != null)
        {
            iconImage.sprite = unknownIcon;
            iconImage.color = unknownColor;
        }
        else
        {
            iconImage.color = unknownColor;
        }

        // Set background color based on rarity
        if (iconBackground != null)
        {
            Color bgColor = GetRarityColor(currentInfo.Rarity);
            bgColor.a = currentInfo.IsDiscovered ? 0.3f : 0.1f;
            iconBackground.color = bgColor;
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

    /// <summary>
    /// Get display string for rarity
    /// </summary>
    private string GetRarityString(DiscoveryRarity rarity)
    {
        switch (rarity)
        {
            case DiscoveryRarity.Common:
                return "Commun";
            case DiscoveryRarity.Uncommon:
                return "Peu commun";
            case DiscoveryRarity.Rare:
                return "Rare";
            case DiscoveryRarity.Epic:
                return "epique";
            case DiscoveryRarity.Legendary:
                return "Legendaire";
            default:
                return "Inconnu";
        }
    }

    /// <summary>
    /// Get display string for discoverable type
    /// </summary>
    private string GetTypeString(DiscoverableType type)
    {
        switch (type)
        {
            case DiscoverableType.Enemy:
                return "Monstre";
            case DiscoverableType.NPC:
                return "PNJ";
            case DiscoverableType.Dungeon:
                return "Donjon";
            case DiscoverableType.Activity:
                return "Activite";
            default:
                return "???";
        }
    }
}
