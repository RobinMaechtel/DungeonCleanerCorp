using UnityEngine;
using UnityEngine.InputSystem;

public class InteractingDebugger : MonoBehaviour
{
    public GameObject ShopScreen;
    public GameObject InventoryScreen;

    bool isScreenOpen = false;

    // To restore cursor to previous state
    private CursorLockMode previousLockMode;
    private bool previousCursorVisible;

    [Header("Player Control To Disable")]
    [SerializeField] private MonoBehaviour[] playerControlScripts;
    // e.g. your camera look script, movement script, etc.



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.iKey.wasPressedThisFrame)
            ToggleInventory();

        if (Keyboard.current.tabKey.wasPressedThisFrame)
            ToggleShop();
    }

    public void ToggleShop()
    {
            if (isScreenOpen)
            {
                CloseDebugScreen();
            }
            else
            {
                OpenDebugScreen();
            }
    }

    public void ToggleInventory()
    {
        if(isScreenOpen)
        {
            CloseInventoryScreen();
        }
        else
        {
            OpenInventoryScreen();
        }
    }

    public void OpenInventoryScreen()
    {
        InventoryScreen.SetActive(true);
        EnableUIControls();
    }

    public void CloseInventoryScreen()
    {
        InventoryScreen.SetActive(false);
        DisableUIControls();
    }

    public void OpenDebugScreen()
    {

        ShopScreen.SetActive(true);
        EnableUIControls();
    }
    public void CloseDebugScreen()
    {
        ShopScreen.SetActive(false);
        DisableUIControls();
    }

    private void EnableUIControls()
    {

        isScreenOpen = true;
        // Store cursor state
        previousLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;

        // Unlock + show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Disable player controls
        foreach (var script in playerControlScripts)
        {
            if (script != null)
                script.enabled = false;
        }
    }

    private void DisableUIControls()
    {
        isScreenOpen = false;
        // Restore cursor state
        Cursor.lockState = previousLockMode;
        Cursor.visible = previousCursorVisible;

        // Re-enable player controls
        foreach (var script in playerControlScripts)
        {
            if (script != null)
                script.enabled = true;
        }
    }
}
