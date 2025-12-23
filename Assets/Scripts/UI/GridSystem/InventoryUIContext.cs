using UnityEngine;

public class InventoryUIContext : MonoBehaviour
{
    public Canvas RootCanvas;
    public RectTransform DragLayer;

    private void Reset()
    {
        RootCanvas = GetComponentInParent<Canvas>();
    }
}
