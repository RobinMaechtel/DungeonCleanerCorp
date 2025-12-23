using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class UIInventoryItemDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform _rt;
    private CanvasGroup _cg;

    private InventoryUIContext _ctx;

    private Transform _originalParent;
    private Vector2 _originalAnchoredPos;

    private RectTransform _dragLayerRT;

    public ItemDefinition Item; // assigned when created
    public Image IconImage;     // assign on prefab


    private InventoryController _inv;
    private InventorySlotUI _originSlot;

    private bool _droppedOnSlot;

    private bool isInit = false;

    private void Start()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();

        _ctx = GetComponentInParent<InventorySlotUI>().Context;
        if (_ctx == null)
            Debug.LogError("No InventoryUIContext found in parents. Put it on InventoryRoot.");

        isInit = true;
    }

    private void Init()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();

        _ctx = GetComponentInParent<InventorySlotUI>().Context;
        if (_ctx == null)
            Debug.LogError("No InventoryUIContext found in parents. Put it on InventoryRoot.");

        isInit = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isInit == false)
            Init();

        _droppedOnSlot = false;
        _dragLayerRT = _ctx.DragLayer;
        _originSlot = GetComponentInParent<InventorySlotUI>();
        _inv = InventoryController.Inventory;

        transform.SetParent(_inv.DragLayer, worldPositionStays: false);
        transform.SetAsLastSibling();

        _cg.blocksRaycasts = false;
    }

    public void HandleDropOnto(InventorySlotUI targetSlot)
    {
        _droppedOnSlot = true;

        int from = _originSlot.Index;
        int to = targetSlot.Index;

        // Swap/move model
        if (!_inv.MoveOrSwap(from, to))
        {
            // invalid -> snap back
            PlaceIntoSlot(_originSlot);
            return;
        }

        // If target has an existing item view (besides us), move it back to origin (UI swap)
        if (targetSlot.transform.childCount > 0)
        {
            var existing = targetSlot.transform.GetChild(0);
            if (existing != transform)
            {
                existing.SetParent(_originSlot.transform, false);
                var exRT = existing.GetComponent<RectTransform>();
                if (exRT != null) exRT.anchoredPosition = Vector2.zero;
            }
        }

        PlaceIntoSlot(targetSlot);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Move the dragged item to follow cursor in the drag layer space
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _dragLayerRT,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint
        );

        _rt.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _cg.blocksRaycasts = true;

        if (_droppedOnSlot) return;

        // Not dropped on a slot -> check if outside inventory window
        bool inside = _inv.IsPointerInsideInventoryWindow(eventData.position, eventData.pressEventCamera);

        if (!inside)
        {
            // Drop to world
            int from = _originSlot.Index;
            _inv.DropFromSlot(from);
            Destroy(gameObject);
            return;
        }

        // Released inside window but not on a slot -> snap back
        PlaceIntoSlot(_originSlot);
    }

    public void PlaceIntoSlot(InventorySlotUI slot)
    {
        transform.SetParent(slot.transform, false);
        _rt.anchoredPosition = Vector2.zero;
    }

    // Called by slot when it has an item and we want to swap
    public void SwapWith(Transform otherItem)
    {
        // Move existing item back to our original slot
        otherItem.SetParent(_originalParent, worldPositionStays: false);

        var otherRT = otherItem.GetComponent<RectTransform>();
        if (otherRT != null)
            otherRT.anchoredPosition = Vector2.zero;
    }
}
