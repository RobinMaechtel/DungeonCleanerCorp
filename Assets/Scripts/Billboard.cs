using System;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    [SerializeField]
    private Transform target;  // usually your camera transform

    private void Start()
    {
        if (target == null)
        {
            //if (Camera.main == null)
            //    return;
            Debug.Log("set camera");
            target = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            if (Camera.main == null)
                return;
            Debug.Log("set camera");
            target = Camera.main.transform;
        }

        // Direction from camera to this object
        Vector3 direction = transform.position - target.position;

        // Ignore vertical difference so it doesn't tilt up/down
        direction.y = 0f;

        // If direction becomes zero (exact same position), skip
        if (direction.sqrMagnitude < 0.0001f)
            return;

        // Rotate this object so its forward faces the camera
        transform.rotation = Quaternion.LookRotation(direction);
    }
}
