using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;

    [Header("Refs")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 6.5f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 12f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = .5f;    // multiplier
    [SerializeField] private float lookVerticalClamp = 85f;     // deg
    [SerializeField] private float lookSmoothing = 0.04f;       // 0 = raw

    [Header("Grounding")]
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private float groundCheckOffset = 0.1f;
    [SerializeField] private LayerMask groundMask = ~0;

    private CharacterController controller;
    private Vector3 velocity;       // y-velocity & smoothing store
    private float targetSpeed;
    private float currentSpeed;
    private float pitch;            // camera X-rotation
    private Vector2 smoothLookVel;  // for simple smoothing
    private Vector2 currentLook;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
            Destroy(gameObject);

        DontDestroyOnLoad(this);

        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleLook();
        HandleMoveAndJump();
    }

    private void HandleLook()
    {
        if (Mouse.current == null) return;

        // Raw delta in pixels this frame
        Vector2 delta = Mouse.current.delta.ReadValue() * mouseSensitivity;

        // Simple lerp smoothing
        currentLook = Vector2.SmoothDamp(currentLook, delta, ref smoothLookVel, lookSmoothing);

        // Horizontal (yaw) on body
        transform.Rotate(Vector3.up * currentLook.x);

        // Vertical (pitch) on camera (clamped)
        pitch -= currentLook.y;
        pitch = Mathf.Clamp(pitch, -lookVerticalClamp, lookVerticalClamp);
        if (cameraTransform != null)
            cameraTransform.localEulerAngles = new Vector3(pitch, 0f, 0f);
    }

    private void HandleMoveAndJump()
    {
        if (Keyboard.current == null) return;

        // WASD
        Vector2 input = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) input.y += 1f;
        if (Keyboard.current.sKey.isPressed) input.y -= 1f;
        if (Keyboard.current.dKey.isPressed) input.x += 1f;
        if (Keyboard.current.aKey.isPressed) input.x -= 1f;
        input = Vector2.ClampMagnitude(input, 1f);

        bool isSprinting = Keyboard.current.leftShiftKey.isPressed;
        float desiredSpeed = (isSprinting ? sprintSpeed : walkSpeed) * input.magnitude;

        // Accel/Decel to keep it snappy but not jerky
        float accel = desiredSpeed > currentSpeed ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, desiredSpeed, accel * Time.deltaTime);

        // Move direction in local space
        Vector3 move = (transform.right * input.x + transform.forward * input.y).normalized * currentSpeed;

        // Ground check (using CharacterController bottom)
        Vector3 groundPos = transform.position + Vector3.down * (controller.height * 0.5f - controller.radius + groundCheckOffset);
        bool isGrounded = Physics.CheckSphere(groundPos, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore) || controller.isGrounded;

        // Gravity & jump
        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f; // small downward force to stick to ground

        if (isGrounded && Keyboard.current.spaceKey.wasPressedThisFrame)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * Time.deltaTime;

        // Apply
        Vector3 total = move + new Vector3(0f, velocity.y, 0f);
        controller.Move(total * Time.deltaTime);
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!controller) controller = GetComponent<CharacterController>();
        Vector3 groundPos = transform.position + Vector3.down * (controller.height * 0.5f - controller.radius + groundCheckOffset);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(groundPos, groundCheckRadius);
    }
#endif
}
