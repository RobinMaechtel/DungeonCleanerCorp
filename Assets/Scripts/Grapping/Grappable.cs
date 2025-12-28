using UnityEngine;

public class Grabbable : MonoBehaviour
{
    [Tooltip("If false, this object cannot be grabbed.")]
    public bool allowGrab = true;

    [Tooltip("Optional: multiply required force (heavier-than-mass feel).")]
    public float weightMultiplier = 1f;
}

