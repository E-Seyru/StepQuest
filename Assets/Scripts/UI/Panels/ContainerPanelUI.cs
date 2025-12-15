// Purpose: Base class for container UI panels (Inventory, Bank) to eliminate code duplication
// Filepath: Assets/Scripts/UI/Panels/ContainerPanelUI.cs
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Base class for UI panels that display item containers (Inventory, Bank, etc.)
/// Provides common functionality for slot management, selection, and auto-deselection.
/// </summary>
public abstract class ContainerPanelUI : MonoBehaviour
{
    [Header("Container UI References")]
    [SerializeField] protected Transform slotsContainer;
    [SerializeField] protected GameObject slotPrefab;
    [SerializeField] protected TextMeshProUGUI titleText;
    [SerializeField] protected TextMeshProUGUI capacityText;

    // Common state
    protected InventoryManager inventoryManager;
    protected List<UniversalSlotUI> slotUIs = new List<UniversalSlotUI>();
    protected UniversalSlotUI selectedSlot;

    // Cached references for performance
    protected Camera cachedMainCamera;
    protected RectTransform cachedRectTransform;

    /// <summary>
    /// The container ID this panel displays (e.g., "player", "bank")
    /// </summary>
    protected abstract string ContainerId { get; }

    /// <summary>
    /// The slot context for this panel type
    /// </summary>
    protected abstract UniversalSlotUI.SlotContext SlotContext { get; }

    /// <summary>
    /// Called during Start() after base initialization. Override to add panel-specific setup.
    /// </summary>
    protected virtual void OnInitialize() { }

    /// <summary>
    /// Called when a slot is created. Override to add additional event subscriptions.
    /// </summary>
    protected virtual void OnSlotCreated(UniversalSlotUI slotUI, int index) { }

    /// <summary>
    /// Called when a slot is being cleared. Override to remove additional event subscriptions.
    /// </summary>
    protected virtual void OnSlotClearing(UniversalSlotUI slotUI) { }

    /// <summary>
    /// Called during RefreshInfo(). Override to add panel-specific info updates.
    /// </summary>
    protected virtual void OnRefreshInfo(InventoryContainer container) { }

    /// <summary>
    /// Called when the panel opens. Override for panel-specific open logic.
    /// </summary>
    protected virtual void OnPanelOpened() { }

    /// <summary>
    /// Called when the panel closes. Override for panel-specific close logic.
    /// </summary>
    protected virtual void OnPanelClosed() { }

    protected virtual void Start()
    {
        // Cache references
        cachedMainCamera = Camera.main;
        cachedRectTransform = GetComponent<RectTransform>();

        // Get InventoryManager
        inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            Logger.LogError($"{GetType().Name}: InventoryManager not found!", Logger.LogCategory.InventoryLog);
            return;
        }

        // Subscribe to container changes
        inventoryManager.OnContainerChanged += OnContainerChanged;

        // Panel-specific initialization
        OnInitialize();

        // Initial setup
        CreateSlotUIs();
        RefreshDisplay();
    }

    protected virtual void Update()
    {
        CheckForDeselection();
    }

    protected virtual void OnDestroy()
    {
        if (inventoryManager != null)
        {
            inventoryManager.OnContainerChanged -= OnContainerChanged;
        }
    }

    #region Slot Management

    /// <summary>
    /// Create UI slots based on container capacity
    /// </summary>
    protected virtual void CreateSlotUIs()
    {
        if (slotsContainer == null || slotPrefab == null)
        {
            Logger.LogError($"{GetType().Name}: Missing slotsContainer or slotPrefab!", Logger.LogCategory.InventoryLog);
            return;
        }

        ClearSlotUIs();

        var container = inventoryManager?.GetContainer(ContainerId);
        if (container == null)
        {
            Logger.LogError($"{GetType().Name}: Container '{ContainerId}' not found!", Logger.LogCategory.InventoryLog);
            return;
        }

        for (int i = 0; i < container.MaxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotsContainer);
            UniversalSlotUI slotUI = slotObj.GetComponent<UniversalSlotUI>();

            if (slotUI != null)
            {
                slotUI.Setup(container.Slots[i], i, ContainerId, SlotContext);
                slotUI.OnSlotClicked += OnSlotClicked;
                OnSlotCreated(slotUI, i);
                slotUIs.Add(slotUI);
            }
            else
            {
                Logger.LogError($"{GetType().Name}: SlotPrefab doesn't have UniversalSlotUI component!", Logger.LogCategory.InventoryLog);
            }
        }

        Logger.LogInfo($"{GetType().Name}: Created {slotUIs.Count} slot UIs for container '{ContainerId}'", Logger.LogCategory.InventoryLog);
    }

    /// <summary>
    /// Clear all slot UIs
    /// </summary>
    protected virtual void ClearSlotUIs()
    {
        foreach (var slotUI in slotUIs)
        {
            if (slotUI != null)
            {
                slotUI.OnSlotClicked -= OnSlotClicked;
                OnSlotClearing(slotUI);
                Destroy(slotUI.gameObject);
            }
        }
        slotUIs.Clear();
    }

    #endregion

    #region Display Refresh

    /// <summary>
    /// Refresh entire display
    /// </summary>
    public virtual void RefreshDisplay()
    {
        RefreshSlots();
        RefreshInfo();
    }

    /// <summary>
    /// Refresh all slot displays
    /// </summary>
    protected virtual void RefreshSlots()
    {
        var container = inventoryManager?.GetContainer(ContainerId);
        if (container == null) return;

        for (int i = 0; i < slotUIs.Count && i < container.Slots.Count; i++)
        {
            slotUIs[i].Setup(container.Slots[i], i, ContainerId, SlotContext);
        }
    }

    /// <summary>
    /// Refresh info display (capacity, selected item)
    /// </summary>
    protected virtual void RefreshInfo()
    {
        var container = inventoryManager?.GetContainer(ContainerId);
        if (container == null) return;

        // Update capacity
        if (capacityText != null)
        {
            capacityText.text = $"{container.GetUsedSlotsCount()}/{container.MaxSlots}";
        }

        // Panel-specific info updates
        OnRefreshInfo(container);
    }

    #endregion

    #region Selection

    /// <summary>
    /// Handle slot clicked
    /// </summary>
    protected virtual void OnSlotClicked(UniversalSlotUI slotUI, int index)
    {
        // Deselect previous slot
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        // Select new slot
        selectedSlot = slotUI;
        selectedSlot.SetSelected(true);

        RefreshInfo();
    }

    /// <summary>
    /// Deselect the current slot
    /// </summary>
    protected void DeselectCurrentSlot()
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
            RefreshInfo();
        }
    }

    /// <summary>
    /// Get the currently selected slot
    /// </summary>
    public UniversalSlotUI GetSelectedSlot() => selectedSlot;

    #endregion

    #region Auto-Deselection

    /// <summary>
    /// Check for auto-deselection on click/touch outside panel (mobile-friendly)
    /// </summary>
    protected void CheckForDeselection()
    {
        if (selectedSlot == null) return;

        bool inputDetected = false;

        // Mobile touch
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                inputDetected = true;
            }
        }
        // Desktop mouse
        else if (Input.GetMouseButtonDown(0))
        {
            inputDetected = true;
        }

        if (inputDetected && !IsPointerOverPanelArea())
        {
            DeselectCurrentSlot();
        }
    }

    /// <summary>
    /// Check if pointer/touch is over the panel area
    /// </summary>
    protected virtual bool IsPointerOverPanelArea()
    {
        // Ensure camera is cached
        if (cachedMainCamera == null)
        {
            cachedMainCamera = Camera.main;
            if (cachedMainCamera == null) return false;
        }

        Vector2 screenPosition = GetInputPosition();

        // Check ItemActionPanel (don't deselect if clicking on it)
        if (ItemActionPanel.Instance != null && ItemActionPanel.Instance.gameObject.activeInHierarchy)
        {
            RectTransform actionPanelRect = ItemActionPanel.Instance.GetComponent<RectTransform>();
            if (actionPanelRect != null && RectTransformUtility.RectangleContainsScreenPoint(
                actionPanelRect, screenPosition, cachedMainCamera))
            {
                return true;
            }
        }

        // Check slots
        foreach (var slot in slotUIs)
        {
            if (slot != null && RectTransformUtility.RectangleContainsScreenPoint(
                slot.GetRectTransform(), screenPosition, cachedMainCamera))
            {
                return true;
            }
        }

        // Check panel itself
        if (cachedRectTransform != null)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(
                cachedRectTransform, screenPosition, cachedMainCamera);
        }

        return false;
    }

    /// <summary>
    /// Get input position (touch or mouse)
    /// </summary>
    protected Vector2 GetInputPosition()
    {
        if (Input.touchCount > 0)
        {
            return Input.GetTouch(0).position;
        }
        return Input.mousePosition;
    }

    #endregion

    #region Panel Open/Close

    /// <summary>
    /// Open the panel
    /// </summary>
    public virtual void OpenPanel()
    {
        gameObject.SetActive(true);
        RefreshDisplay();
        OnPanelOpened();
    }

    /// <summary>
    /// Close the panel
    /// </summary>
    public virtual void ClosePanel()
    {
        gameObject.SetActive(false);
        DeselectCurrentSlot();

        // Close ItemActionPanel if open
        if (ItemActionPanel.Instance != null)
        {
            ItemActionPanel.Instance.HidePanel();
        }

        OnPanelClosed();
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handle container changed event
    /// </summary>
    protected virtual void OnContainerChanged(string containerId)
    {
        if (containerId == ContainerId)
        {
            RefreshDisplay();
        }
    }

    #endregion

    #region Debug

    /// <summary>
    /// Get debug info
    /// </summary>
    public virtual string GetDebugInfo()
    {
        var container = inventoryManager?.GetContainer(ContainerId);
        return $"{GetType().Name}: {slotUIs.Count} slots, Container: {container?.GetDebugInfo() ?? "null"}";
    }

    #endregion
}
