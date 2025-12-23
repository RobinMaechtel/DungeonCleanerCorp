using UnityEngine;
using UnityEngine.UI;

public class InventoryController : MonoBehaviour
{
    public static InventoryController Inventory;

    [Header("Grid")]
    public int SlotCount = 40;

    [Header("UI")]
    public RectTransform InventoryWindowRect; // your Window RectTransform
    public RectTransform DragLayer;           // your DragLayer RectTransform
    public InventorySlotUI[] Slots;           // assign after you created slots (or auto later)

    [Header("World Drop")]
    public Transform DropOrigin;              // usually your player camera
    public float DropDistance = 1.5f;

    private ItemDefinition[] _items;

    [SerializeField]
    private GameObject InventoryItemPrefab;

    private void Awake()
    {
        _items = new ItemDefinition[SlotCount];

        if (Inventory == null)
            Inventory = this;
        else
            Destroy(gameObject);
    }

    public bool TryAddItem(ItemDefinition item)
    {
        for (int i = 0; i < _items.Length; i++)
        {
            if (_items[i] == null)
            {
                var newSlotItem = Instantiate(InventoryItemPrefab);
                newSlotItem.transform.SetParent(Slots[i].transform);
                newSlotItem.transform.localPosition = Vector3.zero;
                newSlotItem.GetComponent<UIInventoryItemDrag>().Item = item;
                newSlotItem.GetComponent<Image>().sprite = item.Icon;
                _items[i] = item;
                Slots[i].SetItem(item);
                return true;
            }
        }
        return false;
    }

    public ItemDefinition GetItem(int index) => _items[index];

    public bool MoveOrSwap(int from, int to)
    {
        if (from == to) return false;
        if (_items[from] == null) return false;

        (_items[to], _items[from]) = (_items[from], _items[to]);
        // UI visuals are handled by the drag scripts via reparenting.
        return true;
    }

    public void DropFromSlot(int index)
    {
        var item = _items[index];
        if (item == null) return;

        _items[index] = null;
        Slots[index].ClearItem();

        if (item.WorldPrefab == null)
        {
            Debug.LogWarning($"Item '{item.name}' has no WorldPrefab assigned.");
            return;
        }

        Vector3 pos = DropOrigin.position + DropOrigin.forward * DropDistance;
        Quaternion rot = Quaternion.identity;

        Instantiate(item.WorldPrefab, pos, rot);
    }

    public bool IsPointerInsideInventoryWindow(Vector2 screenPos, Camera eventCam)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            InventoryWindowRect, screenPos, eventCam
        );
    }
}
