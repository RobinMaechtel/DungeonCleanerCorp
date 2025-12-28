using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Crisp Rigidbody-based player controller:
/// - Rigidbody motor locomotion (tight, FPS-like)
/// - Mouse look (yaw on body, pitch on camera pivot)
/// - Jump with coyote time + jump buffer
/// - Sprint + crouch (capsule resize + camera offset)
///
/// Notes:
/// - Add Rigidbody + CapsuleCollider to the same GameObject.
/// - Rigidbody constraints: Freeze Rotation X/Z (recommended).
/// - Collider PhysicMaterial: friction 0/0, bounciness 0 for smooth sliding.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerControllerRB : MonoBehaviour
{
    [Header("View")]
    [Tooltip("Yaw is applied to this object. Usually the player root.")]
    public Transform yawRoot; // if null, uses this.transform

    [Tooltip("Pitch is applied to this pivot (camera parent).")]
    public Transform pitchPivot;

    [Tooltip("Camera transform (optional, only used for crouch camera offset if you want).")]
    public Transform cameraTransform;

    [Range(0.01f, 5f)]
    public float mouseSensitivity = 0.2f;
    public bool invertY = false;
    public float minPitch = -85f;
    public float maxPitch = 85f;

    [Header("Movement Speeds")]
    public float walkSpeed = 6.5f;
    public float sprintSpeed = 9.0f;
    public float crouchSpeed = 4.0f;

    [Header("Acceleration (Crisp Feel)")]
    [Tooltip("How fast we reach target speed on ground.")]
    public float groundAcceleration = 60f;

    [Tooltip("How fast we stop when no input on ground (higher = snappier).")]
    public float groundBraking = 80f;

    [Tooltip("How much control we have in air.")]
    public float airAcceleration = 18f;

    [Tooltip("Max horizontal speed change per physics step (safety clamp).")]
    public float maxDeltaVPerFixed = 4.5f;

    [Header("Jump")]
    public float jumpSpeed = 6.5f;
    public float coyoteTime = 0.10f;     // still jump shortly after leaving ground
    public float jumpBuffer = 0.10f;     // jump press shortly before landing
    public float extraGravity = 18f;     // makes falling feel snappier
    public float stickToGroundForce = 20f;

    [Header("Ground Check")]
    public LayerMask groundMask = ~0;
    public float groundCheckDistance = 0.08f;
    public float maxSlopeAngle = 55f;

    [Header("Crouch")]
    public bool enableCrouch = true;
    [Tooltip("Capsule height while crouching (meters).")]
    public float crouchHeight = 1.1f;
    [Tooltip("How fast capsule height changes.")]
    public float crouchLerpSpeed = 12f;
    [Tooltip("Optional: camera local Y offset while crouching (applied to cameraTransform).")]
    public float crouchCameraOffsetY = -0.35f;

    [Header("Control Authority")]
    [Tooltip("1 = very crisp/authoritative. Lower this during heavy grabbing if you want the player to be dragged more.")]
    [Range(0f, 1f)]
    public float authority = 1f;

#if ENABLE_INPUT_SYSTEM
    [Header("New Input System (optional)")]
    public InputActionReference moveAction;   // Vector2
    public InputActionReference lookAction;   // Vector2
    public InputActionReference jumpAction;   // Button
    public InputActionReference sprintAction; // Button
    public InputActionReference crouchAction; // Button (hold or toggle by setting crouchToggle)
    public bool crouchToggle = false;
#endif

    [Header("Legacy Input Fallback")]
    public bool useLegacyInput = true; // set false if you use InputActionReferences above

    // Components
    private Rigidbody _rb;
    private CapsuleCollider _capsule;

    // Input state
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _jumpPressed;
    private bool _sprintHeld;
    private bool _crouchHeld;
    private bool _crouchLatched;

    // Look state
    private float _yaw;
    private float _pitch;

    // Grounding
    private bool _isGrounded;
    private Vector3 _groundNormal = Vector3.up;
    private float _lastGroundedTime;
    private float _lastJumpPressedTime;
    private bool _jumpConsumed;

    // Crouch
    private float _standingHeight;
    private float _targetCapsuleHeight;
    private Vector3 _cameraLocalStart;

    public bool IsGrounded => _isGrounded;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();

        if (yawRoot == null) yawRoot = transform;

        _standingHeight = _capsule.height;
        _targetCapsuleHeight = _standingHeight;

        if (cameraTransform != null)
            _cameraLocalStart = cameraTransform.localPosition;

        // Nice defaults for a "motor" feel (you can tweak)
        _rb.useGravity = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

#if ENABLE_INPUT_SYSTEM
    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (lookAction != null) lookAction.action.Enable();
        if (jumpAction != null) jumpAction.action.Enable();
        if (sprintAction != null) sprintAction.action.Enable();
        if (crouchAction != null) crouchAction.action.Enable();
    }

    private void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (lookAction != null) lookAction.action.Disable();
        if (jumpAction != null) jumpAction.action.Disable();
        if (sprintAction != null) sprintAction.action.Disable();
        if (crouchAction != null) crouchAction.action.Disable();
    }
#endif

    private void Update()
    {
        ReadInput();
        UpdateLook();
        HandleJumpBuffer();
        HandleCrouchInput();
    }

    private void FixedUpdate()
    {
        UpdateGrounding();
        ApplyRotation();
        ApplyMovementMotor();
        ApplyExtraGravityAndStick();

        if (enableCrouch)
            ApplyCrouch();
    }

    private void ReadInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (!useLegacyInput)
        {
            _moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
            _lookInput = lookAction != null ? lookAction.action.ReadValue<Vector2>() : Vector2.zero;

            bool jumpDown = jumpAction != null && jumpAction.action.WasPressedThisFrame();
            if (jumpDown)
            {
                _jumpPressed = true;
                _lastJumpPressedTime = Time.time;
                _jumpConsumed = false;
            }

            _sprintHeld = sprintAction != null && sprintAction.action.IsPressed();

            if (enableCrouch && crouchAction != null)
            {
                if (crouchToggle)
                {
                    if (crouchAction.action.WasPressedThisFrame())
                        _crouchLatched = !_crouchLatched;
                    _crouchHeld = _crouchLatched;
                }
                else
                {
                    _crouchHeld = crouchAction.action.IsPressed();
                }
            }
            else
            {
                _crouchHeld = false;
            }

            return;
        }
#endif

        // Legacy fallback (quick and reliable for testing)
        _moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        _lookInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _jumpPressed = true;
            _lastJumpPressedTime = Time.time;
            _jumpConsumed = false;
        }

        _sprintHeld = Input.GetKey(KeyCode.LeftShift);

        if (enableCrouch)
            _crouchHeld = Input.GetKey(KeyCode.LeftControl);
        else
            _crouchHeld = false;
    }

    private void UpdateLook()
    {
        if (pitchPivot == null) return;

        float mx = _lookInput.x * mouseSensitivity;
        float my = _lookInput.y * mouseSensitivity * (invertY ? 1f : -1f);

        _yaw += mx;
        _pitch = Mathf.Clamp(_pitch + my, minPitch, maxPitch);

        pitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void ApplyRotation()
    {
        // Apply yaw via Rigidbody rotation for smooth physics interpolation.
        Quaternion targetRot = Quaternion.Euler(0f, _yaw, 0f);
        _rb.MoveRotation(targetRot);
    }

    private void HandleJumpBuffer()
    {
        // Nothing to do here beyond timestamping; actual jump is in motor.
        // Keeping this separate makes it easier to tune.
    }

    private void HandleCrouchInput()
    {
        if (!enableCrouch) return;

        _targetCapsuleHeight = _crouchHeld ? crouchHeight : _standingHeight;
    }

    private void UpdateGrounding()
    {
        // Spherecast from capsule center down to check ground.
        Vector3 up = Vector3.up;
        float radius = Mathf.Max(0.01f, _capsule.radius * 0.95f);

        // Calculate bottom of capsule in world
        Vector3 center = transform.TransformPoint(_capsule.center);
        float halfHeight = Mathf.Max(_capsule.height * 0.5f, radius);
        Vector3 bottom = center - up * (halfHeight - radius);

        // Cast slightly below bottom
        bool hit = Physics.SphereCast(
            bottom + up * 0.02f,
            radius,
            Vector3.down,
            out RaycastHit rh,
            groundCheckDistance + 0.02f,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (hit)
        {
            float slope = Vector3.Angle(rh.normal, up);
            if (slope <= maxSlopeAngle)
            {
                _isGrounded = true;
                _groundNormal = rh.normal;
                _lastGroundedTime = Time.time;
                return;
            }
        }

        _isGrounded = false;
        _groundNormal = up;
    }

    private void ApplyMovementMotor()
    {
        // 1) Determine desired speed
        float speed = walkSpeed;
        if (_crouchHeld) speed = crouchSpeed;
        else if (_sprintHeld) speed = sprintSpeed;

        // 2) Build wish direction in world space based on yawRoot (body forward)
        Vector3 forward = yawRoot.forward;
        Vector3 right = yawRoot.right;

        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();

        Vector3 wishDir = (forward * _moveInput.y + right * _moveInput.x);
        if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

        // Project movement along ground plane for slopes
        wishDir = Vector3.ProjectOnPlane(wishDir, _groundNormal).normalized;

        Vector3 desiredPlanarVel = wishDir * speed;

        // 3) Current planar velocity
        Vector3 v = _rb.linearVelocity;
        Vector3 currentPlanarVel = Vector3.ProjectOnPlane(v, Vector3.up);

        bool hasInput = _moveInput.sqrMagnitude > 0.0001f;

        // 4) Compute deltaV toward desired
        float accel = _isGrounded ? groundAcceleration : airAcceleration;
        float brake = groundBraking;

        accel *= authority;
        brake *= authority;

        Vector3 targetPlanarVel = desiredPlanarVel;

        Vector3 delta;
        if (hasInput)
            delta = targetPlanarVel - currentPlanarVel;
        else
            delta = -currentPlanarVel; // brake to zero when no input

        float maxDv = (hasInput ? accel : brake) * Time.fixedDeltaTime;
        maxDv = Mathf.Min(maxDv, maxDeltaVPerFixed);

        Vector3 dv = Vector3.ClampMagnitude(delta, maxDv);

        // Apply snappy velocity change (mass-independent motor feel)
        _rb.AddForce(dv, ForceMode.VelocityChange);

        // 5) Jump (coyote + buffer)
        bool canCoyote = (Time.time - _lastGroundedTime) <= coyoteTime;
        bool buffered = (Time.time - _lastJumpPressedTime) <= jumpBuffer;

        if (!_jumpConsumed && buffered && (_isGrounded || canCoyote))
        {
            // Consume buffered jump
            _jumpConsumed = true;
            _jumpPressed = false;
            _lastJumpPressedTime = -999f;

            // Zero out downward velocity then apply jump speed
            Vector3 vel = _rb.linearVelocity;
            if (vel.y < 0f) vel.y = 0f;
            vel.y = jumpSpeed;
            _rb.linearVelocity = vel;

            _isGrounded = false;
        }

        // If jump was pressed long ago, clear it
        if (_jumpPressed && (Time.time - _lastJumpPressedTime) > jumpBuffer)
        {
            _jumpPressed = false;
        }
    }

    private void ApplyExtraGravityAndStick()
    {
        // Make falling feel snappier without messing with motor on ground.
        if (!_isGrounded)
        {
            _rb.AddForce(Vector3.down * extraGravity, ForceMode.Acceleration);
            return;
        }

        // Keep the capsule glued to ground to avoid micro-bounces on slopes/steps
        _rb.AddForce(Vector3.down * stickToGroundForce, ForceMode.Acceleration);
    }

    private void ApplyCrouch()
    {
        // Smoothly change capsule height (and keep feet planted by moving center)
        float newHeight = Mathf.Lerp(_capsule.height, _targetCapsuleHeight, Time.fixedDeltaTime * crouchLerpSpeed);
        newHeight = Mathf.Max(newHeight, _capsule.radius * 2f);

        float heightDelta = newHeight - _capsule.height;

        _capsule.height = newHeight;
        Vector3 c = _capsule.center;
        c.y += heightDelta * 0.5f; // keep bottom roughly stable
        _capsule.center = c;

        // Optional camera offset
        if (cameraTransform != null && Mathf.Abs(crouchCameraOffsetY) > 0.001f)
        {
            float t = Mathf.InverseLerp(_standingHeight, crouchHeight, _capsule.height);
            Vector3 cam = _cameraLocalStart;
            cam.y += Mathf.Lerp(0f, crouchCameraOffsetY, t);
            cameraTransform.localPosition = cam;
        }
    }

    // Optional helper if later your grab system wants to reduce control while straining.
    public void SetAuthority(float newAuthority)
    {
        authority = Mathf.Clamp01(newAuthority);
    }
}

