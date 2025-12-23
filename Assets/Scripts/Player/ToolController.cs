using UnityEngine;
using UnityEngine.InputSystem;

public class ToolController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform toolSocket;

    [Header("Tools (prefabs with a MonoBehaviour implementing ITool)")]
    [SerializeField] private GameObject[] toolPrefabs;

    [Header("Hit")]
    [SerializeField] private LayerMask hitMask; // e.g. Corpse, World, etc.

    [Header("Input Actions (New Input System)")]
    [SerializeField] private InputActionReference primaryAction;
    [SerializeField] private InputActionReference secondaryAction;
    [SerializeField] private InputActionReference swapToolAction;

    private int activeIndex = -1;
    private GameObject currentToolGO;
    private ITool currentTool;

    private ToolContext Context => new ToolContext(transform, playerCamera, toolSocket, hitMask);

    private void OnEnable()
    {
        primaryAction.action.performed += OnPrimary;
        secondaryAction.action.performed += OnSecondary;
        swapToolAction.action.performed += OnSwapTool;

        primaryAction.action.Enable();
        secondaryAction.action.Enable();
        swapToolAction.action.Enable();

        // Equip first tool by default
        if (activeIndex < 0 && toolPrefabs.Length > 0)
            EquipTool(0);
    }

    private void OnDisable()
    {
        primaryAction.action.performed -= OnPrimary;
        secondaryAction.action.performed -= OnSecondary;
        swapToolAction.action.performed -= OnSwapTool;
    }

    private void OnPrimary(InputAction.CallbackContext _)
    {
        currentTool?.Primary(Context);
    }

    private void OnSecondary(InputAction.CallbackContext _)
    {
        currentTool?.Secondary(Context);
    }

    private void OnSwapTool(InputAction.CallbackContext _)
    {
        if (toolPrefabs.Length == 0) return;
        int next = (activeIndex + 1) % toolPrefabs.Length;
        EquipTool(next);
    }

    private void EquipTool(int index)
    {
        // Unequip old
        if (currentTool != null)
        {
            currentTool.Unequip(Context);
            if (currentToolGO != null) Destroy(currentToolGO);
        }

        activeIndex = index;

        // Spawn new
        currentToolGO = Instantiate(toolPrefabs[activeIndex], toolSocket);
        //currentToolGO.transform.localPosition = Vector3.zero;
        currentToolGO.transform.localPosition = Vector3.up * 0.5f;
        currentToolGO.transform.localRotation = Quaternion.identity;
        //currentToolGO.transform.localScale = Vector3.one;

        currentTool = currentToolGO.GetComponent<ITool>();
        if (currentTool == null)
        {
            Debug.LogError($"Tool prefab '{toolPrefabs[activeIndex].name}' has no component implementing ITool.");
            return;
        }

        currentTool.Equip(Context);
    }
}
