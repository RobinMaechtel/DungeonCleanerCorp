using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string Id;
    public Sprite Icon;

    [Header("World")]
    public GameObject WorldPrefab;
}
