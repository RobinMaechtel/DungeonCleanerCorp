using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IDropHandler
{
    public int Index;

    [Header("Optional visuals")]
    public Image SlotBackground;
    public InventoryUIContext Context;

    public bool HasItem => transform.childCount > 0;

    private void Start()
    {
        Index = transform.GetSiblingIndex();   
    }

    public void SetItem(ItemDefinition item)
    {
        // if slot has no view yet, you’ll add an ItemView prefab manually for now
        // (or we can auto-spawn views later)
    }

    public void ClearItem()
    {
        // destroy item view if exists
        if (transform.childCount > 0)
            Destroy(transform.GetChild(0).gameObject);
    }

    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag;
        if (dragged == null) return;

        var itemView = dragged.GetComponent<UIInventoryItemDrag>();
        if (itemView == null) return;

        itemView.HandleDropOnto(this);
    }
}
