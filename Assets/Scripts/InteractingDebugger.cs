using UnityEngine;
using UnityEngine.InputSystem;

public class InteractingDebugger : MonoBehaviour
{
    public GameObject ShopScreen;

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
        if(Keyboard.current.tabKey.wasPressedThisFrame) { 
        if(isScreenOpen )
            {
                CloseDebugScreen();
            }
        else
            {
                OpenDebugScreen();
            }
        }
        
    }

    public void OpenDebugScreen()
    {
        isScreenOpen = true;

        ShopScreen.SetActive(true);

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
    public void CloseDebugScreen()
    {
        isScreenOpen = false;
        ShopScreen.SetActive(false);

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
