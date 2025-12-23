using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;   // new input system support
#endif

public class RagdollGrabber : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;

    [Header("Grab Settings")]
    public float maxGrabDistance = 4f;
    public float spring = 200f;
    public float damper = 10f;
    public float dragWhileHeld = 5f;
    public float angularDragWhileHeld = 5f;

    SpringJoint currentJoint;
    Rigidbody grabbedBody;
    float grabDistance;

    void Update()
    {
        if (currentJoint == null)
        {
            if (GetGrabPressed())
                TryGrab();
        }
        else
        {
            if (GetGrabReleased())
                Release();
            else
                UpdateGrab();
        }
    }

    bool GetGrabPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    bool GetGrabReleased()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
        return Input.GetMouseButtonUp(0);
#endif
    }

    void TryGrab()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning("RagdollGrabber: No playerCamera assigned.");
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxGrabDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            Rigidbody rb = hit.rigidbody;
            if (rb == null) return;

            grabbedBody = rb;
            grabDistance = hit.distance;

            // Add a spring joint onto the rigidbody we grabbed
            currentJoint = grabbedBody.gameObject.AddComponent<SpringJoint>();
            currentJoint.autoConfigureConnectedAnchor = false;

            // Local anchor on the body where we clicked
            currentJoint.anchor = grabbedBody.transform.InverseTransformPoint(hit.point);
            // World-space point where the joint pulls towards
            currentJoint.connectedAnchor = hit.point;

            currentJoint.spring = spring;
            currentJoint.damper = damper;
            currentJoint.maxDistance = 0.01f;

            // Make the limb feel a bit heavier / less jittery
            grabbedBody.linearDamping = dragWhileHeld;
            grabbedBody.angularDamping = angularDragWhileHeld;
        }
    }

    void UpdateGrab()
    {
        if (grabbedBody == null || currentJoint == null)
        {
            Release();
            return;
        }

        // Target point in front of camera at the original grab distance
        Vector3 targetPos = playerCamera.transform.position + playerCamera.transform.forward * grabDistance;
        currentJoint.connectedAnchor = targetPos;
    }

    void Release()
    {
        if (currentJoint != null)
        {
            Destroy(currentJoint);
            currentJoint = null;
        }

        if (grabbedBody != null)
        {
            // Optional: reset drag
            grabbedBody.linearDamping = 0f;
            grabbedBody.angularDamping = 0.05f;
            grabbedBody = null;
        }
    }
}
