// Purpose: Popup panel for NPC interaction (illustration, description, hearts, buttons)
// Filepath: Assets/Scripts/UI/Panels/NPCInteractionPanel.cs
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overlay popup panel for interacting with an NPC.
/// Shows illustration, description, relationship hearts, and action buttons.
/// </summary>
public class NPCInteractionPanel : MonoBehaviour
{
    public static NPCInteractionPanel Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Image npcIllustration;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI npcDescriptionText;

    [Header("Heart Display")]
    [SerializeField] private HeartDisplay heartDisplay;

    [Header("Buttons")]
    [SerializeField] private Button talkButton;
    [SerializeField] private Button giftButton;
    [SerializeField] private Button closeButton;

    [Header("Optional")]
    [SerializeField] private Image backgroundOverlay;

    // Current NPC
    private NPCDefinition currentNPC;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SetupButtons();
        Hide();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void SetupButtons()
    {
        if (talkButton != null)
        {
            talkButton.onClick.AddListener(OnTalkClicked);
        }

        if (giftButton != null)
        {
            giftButton.onClick.AddListener(OnGiftClicked);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }

        // Close on background click
        if (backgroundOverlay != null)
        {
            var bgButton = backgroundOverlay.GetComponent<Button>();
            if (bgButton == null)
            {
                bgButton = backgroundOverlay.gameObject.AddComponent<Button>();
                bgButton.transition = Selectable.Transition.None;
            }
            bgButton.onClick.AddListener(Hide);
        }
    }

    /// <summary>
    /// Show the panel with the specified NPC
    /// </summary>
    public void Show(NPCDefinition npc)
    {
        if (npc == null)
        {
            Logger.LogWarning("NPCInteractionPanel: Cannot show panel - NPC is null", Logger.LogCategory.General);
            return;
        }

        currentNPC = npc;
        UpdateDisplay();

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }
        gameObject.SetActive(true);

        Logger.LogInfo($"NPCInteractionPanel: Showing panel for {npc.GetDisplayName()}", Logger.LogCategory.General);
    }

    /// <summary>
    /// Hide the panel
    /// </summary>
    public void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
        gameObject.SetActive(false);

        currentNPC = null;
    }

    /// <summary>
    /// Update the display with current NPC data
    /// </summary>
    private void UpdateDisplay()
    {
        if (currentNPC == null) return;

        // Name
        if (npcNameText != null)
        {
            npcNameText.text = currentNPC.GetDisplayName();
        }

        // Description
        if (npcDescriptionText != null)
        {
            npcDescriptionText.text = currentNPC.Description ?? "";
        }

        // Illustration
        if (npcIllustration != null)
        {
            if (currentNPC.Illustration != null)
            {
                npcIllustration.sprite = currentNPC.Illustration;
                npcIllustration.color = Color.white;
            }
            else if (currentNPC.Avatar != null)
            {
                // Fallback to avatar if no illustration
                npcIllustration.sprite = currentNPC.Avatar;
                npcIllustration.color = Color.white;
            }
            else
            {
                npcIllustration.sprite = null;
                npcIllustration.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }

        // Hearts - get actual relationship points from PlayerData
        if (heartDisplay != null)
        {
            int relationshipPoints = DataManager.Instance?.PlayerData?.GetNPCRelationship(currentNPC.NPCID) ?? 0;
            heartDisplay.SetPoints(relationshipPoints);
        }
    }

    /// <summary>
    /// Get the currently displayed NPC
    /// </summary>
    public NPCDefinition GetCurrentNPC()
    {
        return currentNPC;
    }

    // === Button Handlers ===

    private void OnTalkClicked()
    {
        if (currentNPC == null)
        {
            Logger.LogWarning("NPCInteractionPanel: currentNPC is null!", Logger.LogCategory.DialogueLog);
            return;
        }

        Logger.LogInfo($"NPCInteractionPanel: Talk clicked for {currentNPC.GetDisplayName()}", Logger.LogCategory.DialogueLog);
        Logger.LogInfo($"NPCInteractionPanel: NPC has {currentNPC.Dialogues?.Count ?? 0} dialogues assigned", Logger.LogCategory.DialogueLog);

        // Start dialogue via DialogueManager
        if (DialogueManager.Instance != null)
        {
            Logger.LogInfo("NPCInteractionPanel: DialogueManager found, starting dialogue...", Logger.LogCategory.DialogueLog);
            bool started = DialogueManager.Instance.StartDialogue(currentNPC);
            Logger.LogInfo($"NPCInteractionPanel: StartDialogue returned {started}", Logger.LogCategory.DialogueLog);

            if (started)
            {
                // Hide this panel while in dialogue
                Hide();
            }
            else
            {
                // No dialogue available - could show a default message
                Logger.LogWarning($"NPCInteractionPanel: No dialogue available for {currentNPC.GetDisplayName()}", Logger.LogCategory.DialogueLog);
            }
        }
        else
        {
            Logger.LogError("NPCInteractionPanel: DialogueManager.Instance is null!", Logger.LogCategory.DialogueLog);
        }
    }

    private void OnGiftClicked()
    {
        if (currentNPC == null) return;

        Logger.LogInfo($"NPCInteractionPanel: Gift clicked for {currentNPC.GetDisplayName()}", Logger.LogCategory.General);

        // TODO: Implement gift functionality
    }
}
