using UnityEngine;
using UnityEngine.InputSystem;   // New Input System

public class Interactor : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField]
    private Camera playerCamera;

    [SerializeField]
    [Tooltip("How far the player can interact with objects.")]
    private float interactDistance = 3f;

    [SerializeField]
    [Tooltip("Only colliders on these layers can be detected as interactables.")]
    private LayerMask interactableLayers = ~0; // default: everything

    private Interactable currentTarget;

    private void Reset()
    {
        // Try to auto-assign the main camera when you add this component
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main;
        }
    }

    private void Update()
    {
        UpdateCurrentTarget();
        HandleInteractionInput();
    }

    private void UpdateCurrentTarget()
    {
        Interactable newTarget = null;

        if (playerCamera != null)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactableLayers))
            {
                newTarget = hit.collider.GetComponent<Interactable>();
            }
        }

        if (newTarget == currentTarget)
            return;

        // Notify old + new targets
        if (currentTarget != null)
        {
            currentTarget.OnFocusLost(this);
        }

        currentTarget = newTarget;

        if (currentTarget != null)
        {
            currentTarget.OnFocusGained(this);
            // later: show "E - Use PC" here via a UI script
        }
        else
        {
            // later: hide UI prompt here
        }
    }

    private void HandleInteractionInput()
    {
        if (Keyboard.current == null)
            return;

        // Press E to interact
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (currentTarget != null)
            {
                currentTarget.Interact(this);
            }
        }
    }
}
