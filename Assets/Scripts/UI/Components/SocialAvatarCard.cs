// Purpose: UI Card for displaying a social avatar (NPC, social activity, etc.)
// Filepath: Assets/Scripts/UI/Components/SocialAvatarCard.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI Card component for displaying social avatars (NPCs and social activities).
/// Similar to EnemyCard but for social interactions.
/// </summary>
public class SocialAvatarCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI avatarName;
    [SerializeField] private Image avatarImage;
    [SerializeField] private Button cardButton;

    [Header("Optional UI")]
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private TextMeshProUGUI statusText;

    // Data - for now we'll use a simple string ID
    // Later this can be extended to support NPC data
    private string avatarId;
    private string displayName;
    private Sprite avatarSprite;
    private bool isAvailable = true;

    // Events
    public System.Action<string> OnCardClicked;

    void Start()
    {
        if (cardButton != null)
        {
            cardButton.onClick.AddListener(OnCardButtonClicked);
        }
    }

    /// <summary>
    /// Setup the card with basic data (for now just name and sprite)
    /// </summary>
    public void Setup(string id, string name, Sprite sprite, bool available = true)
    {
        avatarId = id;
        displayName = name;
        avatarSprite = sprite;
        isAvailable = available;

        UpdateCardDisplay();

        Logger.LogInfo($"SocialAvatarCard: Setup completed for {displayName}", Logger.LogCategory.DialogueLog);
    }

    /// <summary>
    /// Update the card's display with current data
    /// </summary>
    private void UpdateCardDisplay()
    {
        // Name
        if (avatarName != null)
        {
            avatarName.text = displayName;
        }

        // Avatar image
        if (avatarImage != null && avatarSprite != null)
        {
            avatarImage.sprite = avatarSprite;
            avatarImage.preserveAspect = true;
        }

        // Update availability state
        UpdateCardState(isAvailable);
    }

    /// <summary>
    /// Handle card button click
    /// </summary>
    private void OnCardButtonClicked()
    {
        if (!isAvailable)
        {
            Logger.LogInfo($"SocialAvatarCard: Cannot interact with {displayName} - not available", Logger.LogCategory.DialogueLog);
            return;
        }

        Logger.LogInfo($"SocialAvatarCard: Card clicked for {displayName}", Logger.LogCategory.DialogueLog);
        OnCardClicked?.Invoke(avatarId);
    }

    /// <summary>
    /// Update the card's visual state based on availability
    /// </summary>
    public void UpdateCardState(bool available)
    {
        isAvailable = available;

        if (cardButton != null)
        {
            cardButton.interactable = available;
        }

        // Show/hide locked overlay
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!available);
        }

        // Dim the card if not available
        if (avatarImage != null)
        {
            avatarImage.color = available ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }

    /// <summary>
    /// Set status text (e.g., "Nouveau!", "Relation: Ami", etc.)
    /// </summary>
    public void SetStatusText(string status)
    {
        if (statusText != null)
        {
            if (!string.IsNullOrEmpty(status))
            {
                statusText.gameObject.SetActive(true);
                statusText.text = status;
            }
            else
            {
                statusText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Get the avatar ID this card represents
    /// </summary>
    public string GetAvatarId()
    {
        return avatarId;
    }

    void OnDestroy()
    {
        if (cardButton != null)
        {
            cardButton.onClick.RemoveListener(OnCardButtonClicked);
        }
    }
}
