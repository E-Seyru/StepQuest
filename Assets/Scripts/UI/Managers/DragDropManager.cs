// Purpose: Global manager for handling drag and drop operations between inventory slots
// Filepath: Assets/Scripts/UI/Managers/DragDropManager.cs
using UnityEngine;

/// <summary>
/// Singleton manager that handles all drag and drop operations in the game
/// </summary>
public class DragDropManager : MonoBehaviour
{
    [Header("Drag Visual Settings")]
    [SerializeField] private GameObject draggedItemPrefab; // Prefab pour l'affichage pendant le drag
    [SerializeField] private Canvas dragCanvas; // Canvas de haut niveau pour le drag
    [SerializeField] private float dragScale = 0.8f; // Échelle de l'item pendant le drag

    // Singleton
    public static DragDropManager Instance { get; private set; }

    // Drag state
    private bool isDragging = false;
    private IDragDropSlot sourceSlot = null;
    private string draggedItemId = null;
    private int draggedQuantity = 0;
    private GameObject draggedItemVisual = null;

    // Drop detection (event-driven)
    private IDragDropSlot currentHoveredSlot = null;

    // Events
    public System.Action<string, int, IDragDropSlot, IDragDropSlot> OnItemDragCompleted; // itemId, quantity, source, destination
    public System.Action OnDragCancelled;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // Trouve le canvas principal si pas assigné
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
            // UpdateDropDetection(); // SUPPRIMÉ - maintenant event-driven

            // Annuler avec clic droit ou Escape
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelDrag();
            }
        }
    }

    /// <summary>
    /// Commencer une opération de drag depuis un slot
    /// </summary>
    public bool StartDrag(IDragDropSlot slot, string itemId, int quantity)
    {
        if (isDragging || slot == null || string.IsNullOrEmpty(itemId) || quantity <= 0)
            return false;

        // Vérifier que le slot peut bien donner cet item
        if (slot.GetItemId() != itemId || slot.GetQuantity() < quantity)
            return false;

        isDragging = true;
        sourceSlot = slot;
        draggedItemId = itemId;
        draggedQuantity = quantity;

        CreateDragVisual();

        Debug.Log($"DragDropManager: Started dragging {quantity}x {itemId}");
        return true;
    }

    /// <summary>
    /// Terminer une opération de drag avec drop
    /// </summary>
    public bool CompleteDrag(IDragDropSlot targetSlot)
    {
        if (!isDragging || targetSlot == null)
            return false;

        bool success = false;

        // Si on drop sur le même slot, on annule
        if (targetSlot == sourceSlot)
        {
            CancelDrag();
            return false;
        }

        // Gérer l'échange ou le merge
        string targetItemId = targetSlot.GetItemId();
        int targetQuantity = targetSlot.GetQuantity();

        if (targetSlot.IsEmpty())
        {
            // Slot vide - simple transfer (avec vérification)
            if (targetSlot.CanAcceptItem(draggedItemId, draggedQuantity))
            {
                success = PerformSimpleTransfer(targetSlot);
            }
        }
        else if (targetItemId == draggedItemId)
        {
            // Même item - essayer de merge (PerformMerge gère ses propres vérifications)
            success = PerformMerge(targetSlot);
        }
        else
        {
            // Items différents - essayer d'échanger (avec vérification)
            if (targetSlot.CanAcceptItem(draggedItemId, draggedQuantity))
            {
                success = PerformSwap(targetSlot, targetItemId, targetQuantity);
            }
        }

        if (success)
        {
            OnItemDragCompleted?.Invoke(draggedItemId, draggedQuantity, sourceSlot, targetSlot);
            Debug.Log($"DragDropManager: Successfully moved {draggedQuantity}x {draggedItemId}");
        }
        else
        {
            Debug.Log($"DragDropManager: Failed to move {draggedQuantity}x {draggedItemId}");
        }

        EndDrag();
        return success;
    }

    /// <summary>
    /// Annuler l'opération de drag en cours
    /// </summary>
    public void CancelDrag()
    {
        if (!isDragging) return;

        Debug.Log("DragDropManager: Drag cancelled");
        OnDragCancelled?.Invoke();
        EndDrag();
    }

    /// <summary>
    /// Vérifier si une opération de drag est en cours
    /// </summary>
    public bool IsDragging => isDragging;

    /// <summary>
    /// Obtenir l'item actuellement en cours de drag
    /// </summary>
    public string GetDraggedItemId() => isDragging ? draggedItemId : null;

    /// <summary>
    /// Notifié par un slot quand la souris entre (event-driven)
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
    /// Notifié par un slot quand la souris sort (event-driven)
    /// </summary>
    public void ClearHoveredSlot(IDragDropSlot slot)
    {
        if (!isDragging) return;

        // Sécurité : vérifier que le slot existe encore et correspond
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

        // Configurer l'échelle et la position
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

    private bool PerformSimpleTransfer(IDragDropSlot targetSlot)
    {
        if (sourceSlot.TryRemoveItem(draggedQuantity) && targetSlot.TrySetItem(draggedItemId, draggedQuantity))
        {
            return true;
        }

        // Rollback si échec
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

        if (spaceLeft <= 0) return false; // Pas de place

        // Calculer combien on peut effectivement merger
        int toMerge = Mathf.Min(draggedQuantity, spaceLeft);
        int remaining = draggedQuantity - toMerge;

        // Tester l'acceptation avec la quantité qu'on va réellement merger
        if (!targetSlot.CanAcceptItem(draggedItemId, toMerge))
        {
            return false;
        }

        // Sauvegarder l'état initial pour rollback correct
        int originalSourceQuantity = sourceSlot.GetQuantity();

        // Effectuer le merge partiel ou total
        if (sourceSlot.TryRemoveItem(toMerge))
        {
            if (targetSlot.TrySetItem(draggedItemId, targetCurrentQuantity + toMerge))
            {
                // Le slot source contient déjà le reste après TryRemoveItem(toMerge)
                // Pas besoin de ré-injection, il est déjà correct

                // Mettre à jour la quantité annoncée pour l'événement
                draggedQuantity = toMerge;
                return true;
            }
            else
            {
                // Rollback exact : restaurer la quantité originale du source
                sourceSlot.TrySetItem(draggedItemId, originalSourceQuantity);
                return false;
            }
        }

        return false;
    }

    private bool PerformSwap(IDragDropSlot targetSlot, string targetItemId, int targetQuantity)
    {
        // Vérifier que le slot source peut accepter l'item cible
        if (!sourceSlot.CanAcceptItem(targetItemId, targetQuantity))
            return false;

        // Effectuer l'échange
        if (sourceSlot.TryRemoveItem(draggedQuantity) && targetSlot.TryRemoveItem(targetQuantity))
        {
            if (sourceSlot.TrySetItem(targetItemId, targetQuantity) &&
                targetSlot.TrySetItem(draggedItemId, draggedQuantity))
            {
                return true;
            }

            // Rollback en cas d'échec
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

        // Sécurité : vérifier si currentHoveredSlot existe encore
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