// Purpose: Global manager for handling drag and drop operations between any containers
// Filepath: Assets/Scripts/UI/Managers/DragDropManager.cs
using UnityEngine;

/// <summary>
/// Singleton manager that handles all drag and drop operations in the game
/// Now supports cross-container transfers
/// </summary>
public class DragDropManager : MonoBehaviour
{
    [Header("Drag Visual Settings")]
    [SerializeField] private GameObject draggedItemPrefab;
    [SerializeField] private Canvas dragCanvas;
    [SerializeField] private float dragScale = 0.8f;

    // Singleton
    public static DragDropManager Instance { get; private set; }

    // Drag state
    private bool isDragging = false;
    private IDragDropSlot sourceSlot = null;
    private string draggedItemId = null;
    private int draggedQuantity = 0;
    private GameObject draggedItemVisual = null;

    // Container info for cross-container transfers
    private string sourceContainerId = null;
    private UniversalSlotUI.SlotContext sourceContext;

    // Drop detection (event-driven)
    private IDragDropSlot currentHoveredSlot = null;

    // Events
    public System.Action<string, int, IDragDropSlot, IDragDropSlot> OnItemDragCompleted;
    public System.Action OnDragCancelled;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Find main canvas if not assigned
            if (dragCanvas == null)
            {
                dragCanvas = FindObjectOfType<Canvas>();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        if (isDragging)
        {
            UpdateDragVisual();

            // Cancel with right click or Escape
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelDrag();
            }
        }
    }

    /// <summary>
    /// Start a drag operation from a slot
    /// </summary>
    public bool StartDrag(IDragDropSlot slot, string itemId, int quantity)
    {
        if (isDragging || slot == null || string.IsNullOrEmpty(itemId) || quantity <= 0)
            return false;

        // Verify slot can give this item
        if (slot.GetItemId() != itemId || slot.GetQuantity() < quantity)
            return false;

        isDragging = true;
        sourceSlot = slot;
        draggedItemId = itemId;
        draggedQuantity = quantity;

        // Get container info if it's a UniversalSlotUI
        if (slot is UniversalSlotUI universalSlot)
        {
            sourceContainerId = universalSlot.ContainerId;
            sourceContext = universalSlot.Context;
        }
        // Or if it's an EquipmentSlotUI
        else if (slot is EquipmentSlotUI)
        {
            sourceContainerId = "equipment";
            sourceContext = UniversalSlotUI.SlotContext.PlayerInventory; // Treat as inventory for transfers
        }

        CreateDragVisual();

        Logger.LogInfo($"DragDropManager: Started dragging {quantity}x {itemId} from {sourceContainerId}", Logger.LogCategory.InventoryLog);
        return true;
    }

    /// <summary>
    /// Complete a drag operation with drop
    /// </summary>
    public bool CompleteDrag(IDragDropSlot targetSlot)
    {
        if (!isDragging || targetSlot == null)
            return false;

        bool success = false;

        // If dropping on same slot, cancel
        if (targetSlot == sourceSlot)
        {
            CancelDrag();
            return false;
        }

        // Get target container info
        string targetContainerId = null;
        UniversalSlotUI.SlotContext targetContext = UniversalSlotUI.SlotContext.PlayerInventory;

        if (targetSlot is UniversalSlotUI targetUniversalSlot)
        {
            targetContainerId = targetUniversalSlot.ContainerId;
            targetContext = targetUniversalSlot.Context;
        }
        else if (targetSlot is EquipmentSlotUI)
        {
            targetContainerId = "equipment";
        }

        // Determine if this is a cross-container transfer
        bool isCrossContainer = sourceContainerId != targetContainerId;

        if (isCrossContainer)
        {
            success = PerformCrossContainerTransfer(targetSlot, targetContainerId);
        }
        else
        {
            // Same container transfer
            string targetItemId = targetSlot.GetItemId();
            int targetQuantity = targetSlot.GetQuantity();

            if (targetSlot.IsEmpty())
            {
                success = PerformSimpleTransfer(targetSlot);
            }
            else if (targetItemId == draggedItemId)
            {
                success = PerformMerge(targetSlot);
            }
            else
            {
                success = PerformSwap(targetSlot, targetItemId, targetQuantity);
            }
        }

        if (success)
        {
            OnItemDragCompleted?.Invoke(draggedItemId, draggedQuantity, sourceSlot, targetSlot);
            Logger.LogInfo($"DragDropManager: Successfully moved {draggedQuantity}x {draggedItemId}", Logger.LogCategory.InventoryLog);
        }
        else
        {
            Logger.LogInfo($"DragDropManager: Failed to move {draggedQuantity}x {draggedItemId}", Logger.LogCategory.InventoryLog);
        }

        EndDrag();
        return success;
    }

    /// <summary>
    /// Cancel the current drag operation
    /// </summary>
    public void CancelDrag()
    {
        if (!isDragging) return;

        Logger.LogInfo("DragDropManager: Drag cancelled", Logger.LogCategory.InventoryLog);
        OnDragCancelled?.Invoke();
        EndDrag();
    }

    /// <summary>
    /// Check if a drag operation is in progress
    /// </summary>
    public bool IsDragging => isDragging;

    /// <summary>
    /// Get the item currently being dragged
    /// </summary>
    public string GetDraggedItemId() => isDragging ? draggedItemId : null;

    /// <summary>
    /// Notified by slot when mouse enters (event-driven)
    /// </summary>
    public void SetHoveredSlot(IDragDropSlot slot)
    {
        if (!isDragging) return;

        if (currentHoveredSlot != slot)
        {
            currentHoveredSlot?.OnDragExit();
            currentHoveredSlot = slot;
            currentHoveredSlot?.OnDragEnter();
        }
    }

    /// <summary>
    /// Notified by slot when mouse exits (event-driven)
    /// </summary>
    public void ClearHoveredSlot(IDragDropSlot slot)
    {
        if (!isDragging) return;

        if (currentHoveredSlot == slot && currentHoveredSlot != null)
        {
            currentHoveredSlot.OnDragExit();
            currentHoveredSlot = null;
        }
    }

    // === PRIVATE METHODS ===

    private void CreateDragVisual()
    {
        if (draggedItemPrefab == null || dragCanvas == null) return;

        draggedItemVisual = Instantiate(draggedItemPrefab, dragCanvas.transform);

        var draggedItemComponent = draggedItemVisual.GetComponent<DraggedItemVisual>();
        if (draggedItemComponent != null)
        {
            draggedItemComponent.Setup(draggedItemId, draggedQuantity);
        }

        // Configure scale and position
        draggedItemVisual.transform.localScale = Vector3.one * dragScale;
        UpdateDragVisualPosition();
    }

    private void UpdateDragVisual()
    {
        if (draggedItemVisual != null)
        {
            UpdateDragVisualPosition();
        }
    }

    private void UpdateDragVisualPosition()
    {
        if (draggedItemVisual != null && dragCanvas != null)
        {
            Vector2 mousePosition = Input.mousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                dragCanvas.transform as RectTransform,
                mousePosition,
                dragCanvas.worldCamera,
                out Vector2 localPoint);

            draggedItemVisual.transform.localPosition = localPoint;
        }
    }

    /// <summary>
    /// Perform a cross-container transfer using InventoryManager
    /// </summary>
    private bool PerformCrossContainerTransfer(IDragDropSlot targetSlot, string targetContainerId)
    {
        // Special case: Equipment slots
        if (targetContainerId == "equipment" && targetSlot is EquipmentSlotUI equipSlot)
        {
            // Use EquipmentPanelUI to equip the item
            if (EquipmentPanelUI.Instance != null)
            {
                // First remove from source
                if (sourceSlot.TryRemoveItem(draggedQuantity))
                {
                    if (EquipmentPanelUI.Instance.TryEquipItem(draggedItemId))
                    {
                        return true;
                    }
                    else
                    {
                        // Rollback
                        sourceSlot.TrySetItem(draggedItemId, draggedQuantity);
                        return false;
                    }
                }
            }
            return false;
        }

        // For universal slots, use InventoryManager transfer
        if (InventoryManager.Instance != null && !string.IsNullOrEmpty(sourceContainerId) && !string.IsNullOrEmpty(targetContainerId))
        {
            // Check if target slot is empty or has same item
            string targetItemId = targetSlot.GetItemId();

            if (targetSlot.IsEmpty() || targetItemId == draggedItemId)
            {
                // Direct transfer
                return InventoryManager.Instance.TransferItem(sourceContainerId, targetContainerId, draggedItemId, draggedQuantity);
            }
            else
            {
                // Need to swap items between containers
                // This is more complex - for now, don't allow cross-container swaps
                Logger.LogInfo("DragDropManager: Cross-container swaps not yet implemented", Logger.LogCategory.InventoryLog);
                return false;
            }
        }

        return false;
    }

    private bool PerformSimpleTransfer(IDragDropSlot targetSlot)
    {
        if (sourceSlot.TryRemoveItem(draggedQuantity) && targetSlot.TrySetItem(draggedItemId, draggedQuantity))
        {
            return true;
        }

        // Rollback if failed
        sourceSlot.TrySetItem(draggedItemId, draggedQuantity);
        return false;
    }

    private bool PerformMerge(IDragDropSlot targetSlot)
    {
        var itemDef = InventoryManager.Instance?.GetItemRegistry()?.GetItem(draggedItemId);
        if (itemDef == null) return false;

        int targetCurrentQuantity = targetSlot.GetQuantity();
        int maxStack = itemDef.MaxStackSize;
        int spaceLeft = maxStack - targetCurrentQuantity;

        if (spaceLeft <= 0) return false;

        // Calculate how much we can merge
        int toMerge = Mathf.Min(draggedQuantity, spaceLeft);
        int remaining = draggedQuantity - toMerge;

        // Test acceptance
        if (!targetSlot.CanAcceptItem(draggedItemId, toMerge))
        {
            return false;
        }

        // Save original state for rollback
        int originalSourceQuantity = sourceSlot.GetQuantity();

        // Perform partial or full merge
        if (sourceSlot.TryRemoveItem(toMerge))
        {
            if (targetSlot.TrySetItem(draggedItemId, targetCurrentQuantity + toMerge))
            {
                // Update dragged quantity for event
                draggedQuantity = toMerge;
                return true;
            }
            else
            {
                // Rollback
                sourceSlot.TrySetItem(draggedItemId, originalSourceQuantity);
                return false;
            }
        }

        return false;
    }

    private bool PerformSwap(IDragDropSlot targetSlot, string targetItemId, int targetQuantity)
    {
        // Verify source slot can accept target item
        if (!sourceSlot.CanAcceptItem(targetItemId, targetQuantity))
            return false;

        // Perform swap
        if (sourceSlot.TryRemoveItem(draggedQuantity) && targetSlot.TryRemoveItem(targetQuantity))
        {
            if (sourceSlot.TrySetItem(targetItemId, targetQuantity) &&
                targetSlot.TrySetItem(draggedItemId, draggedQuantity))
            {
                return true;
            }

            // Rollback on failure
            sourceSlot.TrySetItem(draggedItemId, draggedQuantity);
            targetSlot.TrySetItem(targetItemId, targetQuantity);
        }

        return false;
    }

    private void EndDrag()
    {
        isDragging = false;
        sourceSlot = null;
        draggedItemId = null;
        draggedQuantity = 0;
        sourceContainerId = null;

        // Safety: check if hovered slot still exists
        if (currentHoveredSlot != null)
        {
            currentHoveredSlot.OnDragExit();
            currentHoveredSlot = null;
        }

        if (draggedItemVisual != null)
        {
            Destroy(draggedItemVisual);
            draggedItemVisual = null;
        }
    }
}