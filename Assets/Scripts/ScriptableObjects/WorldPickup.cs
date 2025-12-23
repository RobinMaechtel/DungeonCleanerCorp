using UnityEngine;

public class WorldPickup : Interactable
{
    public ItemDefinition Item;
    public int Amount = 1; // unused for now (future stacking)

    // Optional: nice interaction point
    public Transform PickupPoint;

    public override void Interact(Interactor interactor)
    {
        bool added = InventoryController.Inventory.TryAddItem(Item);
        if (added)
        {
            Destroy(gameObject);
        }
        else
        {
            // Inventory full (later: show UI feedback)
            Debug.Log("Inventory full!");
        }
    }
}
