using UnityEngine;
using UnityEngine.InputSystem;

public class DebugManager : MonoBehaviour
{
    public bool isDebugMode = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Keyboard.current.rightShiftKey.wasPressedThisFrame) {
            Global.Instance.Gold += 20;
        }
    }
}
