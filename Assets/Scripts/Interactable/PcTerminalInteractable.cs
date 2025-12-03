using UnityEngine;

public class PcTerminalInteractable : Interactable
{
    [Header("PC Setup")]
    [SerializeField]
    private InteractingDebugger shopScreen;

    [SerializeField]
    GameObject interactingUI;

    public override void Interact(Interactor interactor)
    {
        if (shopScreen == null)
        {
            Debug.LogWarning($"PcTerminalInteractable on {name} has no ShopScreenController assigned.");
            return;
        }

        shopScreen.ToggleShop();
    }

    public override void OnFocusGained(Interactor interactor)
    {
        // Later: add highlight or UI like "E - Use PC"
        // For now, just for debugging:
        // Debug.Log("Looking at PC");
        interactingUI.SetActive(true);
    }

    public override void OnFocusLost(Interactor interactor)
    {
        // Later: remove highlight / hide UI
        // Debug.Log("Stopped looking at PC");
        interactingUI.SetActive(false);
    }
}
