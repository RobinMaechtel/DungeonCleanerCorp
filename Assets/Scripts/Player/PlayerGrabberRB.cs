using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerGrabberRB : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public PlayerControllerRB motor; // optional (for authority reduction near max arm stretch)

    [Header("Input (New Input System)")]
#if ENABLE_INPUT_SYSTEM
    public InputActionReference grabLeftAction;   // Button
    public InputActionReference grabRightAction;  // Button
#endif

    [Header("Raycast")]
    public float grabDistance = 3f;
    public LayerMask grabMask = ~0;

    [Header("Hold behaviour")]
    [Tooltip("On grab, store the hit point relative to camera so there's no instant pull.")]
    public bool holdAtGrabPoint = true;

    [Tooltip("Clamp how close the hold point can be in front of the camera.")]
    public float minHoldForward = 0.7f;

    [Header("Arm Range (the 'grey zone')")]
    [Tooltip("Within this radius, the hand can move without pulling the object.")]
    public float armSlackRadius = 0.45f;

    [Tooltip("At this distance the arm is fully stretched (player should stop moving further).")]
    public float armMaxDistance = 1.25f;

    [Tooltip("Safety release if the stretch gets insane (network / explosions / etc.).")]
    public float hardReleaseDistance = 2.5f;

    [Header("Hold Target Smoothing (prevents camera-yank)")]
    [Tooltip("Max speed (m/s) the invisible hold target is allowed to move.")]
    public float holdTargetMaxSpeed = 7.0f;

    [Header("Grab Force / Feel")]
    [Tooltip("Base max pull force per hand. Strength multiplies this.")]
    public float baseMaxForce = 700f;

    [Tooltip("Spring strength once slack is exceeded.")]
    public float positionSpring = 4500f;

    [Tooltip("Damping on the handle to reduce oscillation.")]
    public float damping = 140f;

    [Header("Weight / Mass Scaling")]
    [Tooltip("If enabled, heavy objects reduce effective grab force.")]
    public bool scaleForceByMass = true;

    [Tooltip("Mass at which force starts to feel 'normal'. Higher mass => harder to move.")]
    public float massReference = 25f;

    [Header("Strength")]
    public float strengthMultiplier = 1f;

    [Header("Optional: Movement strain")]
    [Tooltip("Only reduce authority when arm is near fully stretched (not immediately after slack).")]
    [Range(0f, 1f)]
    public float minAuthorityAtFullStretch = 0.2f;

    [Tooltip("Stretch % (0..1) where we START slowing the player. 0.9 means only at the last 10%.")]
    [Range(0f, 1f)]
    public float slowPlayerStartAtStretch01 = 0.9f;

    [Header("Optional: Collision handling")]
    public bool ignorePlayerCollisionWhileGrabbed = true;

    private GrabHand _left;
    private GrabHand _right;
    private Collider[] _playerColliders;

    private void Awake()
    {
        _left = new GrabHand("LeftHandHandle");
        _right = new GrabHand("RightHandHandle");
        _playerColliders = GetComponentsInChildren<Collider>();
    }

#if ENABLE_INPUT_SYSTEM
    private void OnEnable()
    {
        if (grabLeftAction != null) grabLeftAction.action.Enable();
        if (grabRightAction != null) grabRightAction.action.Enable();
    }

    private void OnDisable()
    {
        if (grabLeftAction != null) grabLeftAction.action.Disable();
        if (grabRightAction != null) grabRightAction.action.Disable();
    }
#endif

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (grabLeftAction != null && grabLeftAction.action.WasPressedThisFrame())
            ToggleGrab(_left);

        if (grabRightAction != null && grabRightAction.action.WasPressedThisFrame())
            ToggleGrab(_right);
#else
        if (Input.GetMouseButtonDown(0)) ToggleGrab(_left);
        if (Input.GetMouseButtonDown(1)) ToggleGrab(_right);
#endif
    }

    private void FixedUpdate()
    {
        float maxForce = baseMaxForce * Mathf.Max(0.05f, strengthMultiplier);

        float leftLoad = _left.FixedUpdateDrive(
            GetDesiredHoldWorld(_left),
            positionSpring, damping,
            maxForce,
            armSlackRadius, armMaxDistance, hardReleaseDistance,
            holdTargetMaxSpeed,
            scaleForceByMass, massReference,
            out float leftStretch01
        );

        float rightLoad = _right.FixedUpdateDrive(
            GetDesiredHoldWorld(_right),
            positionSpring, damping,
            maxForce,
            armSlackRadius, armMaxDistance, hardReleaseDistance,
            holdTargetMaxSpeed,
            scaleForceByMass, massReference,
            out float rightStretch01
        );

        // Only slow the player when arm is near max stretch (your requirement #2)
        if (motor != null)
        {
            float stretch01 = Mathf.Max(leftStretch01, rightStretch01);
            float slow01 = Mathf.InverseLerp(slowPlayerStartAtStretch01, 1f, stretch01);

            // Optional: also factor in "load" so light objects don't slow you even if stretched
            float load01 = Mathf.Max(leftLoad, rightLoad);
            float strain = Mathf.Clamp01(Mathf.Max(slow01, slow01 * load01));

            float authority = Mathf.Lerp(1f, minAuthorityAtFullStretch, strain);
            motor.SetAuthority(authority);
        }
    }

    private void ToggleGrab(GrabHand hand)
    {
        if (hand.IsGrabbing)
        {
            hand.Release();
            return;
        }

        if (cam == null) return;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, grabDistance, grabMask, QueryTriggerInteraction.Ignore))
            return;

        if (hit.collider != null && hit.collider.transform.IsChildOf(transform))
            return;

        Rigidbody hitRb = hit.rigidbody;
        if (hitRb == null || hitRb.isKinematic)
            return;

        Grabbable grabbable = hitRb.GetComponentInParent<Grabbable>();
        if (grabbable == null || !grabbable.allowGrab)
            return;

        Vector3 holdLocalCam = default;
        bool useLocalHold = false;

        if (holdAtGrabPoint)
        {
            holdLocalCam = cam.transform.InverseTransformPoint(hit.point);
            holdLocalCam.z = Mathf.Max(holdLocalCam.z, minHoldForward);
            useLocalHold = true;
        }

        hand.StartGrab(
            target: hitRb,
            grabPointWorld: hit.point,
            weightMultiplier: grabbable.weightMultiplier,
            useLocalHold: useLocalHold,
            holdLocalToCam: holdLocalCam,
            cam: cam.transform,
            ignoreCollision: ignorePlayerCollisionWhileGrabbed,
            playerColliders: _playerColliders,
            grabbedCollider: hit.collider
        );
    }

    private Vector3 GetDesiredHoldWorld(GrabHand hand)
    {
        // If we grabbed at a point, desired hold follows camera transform (but will be smoothed/limited)
        if (hand.IsGrabbing && hand.HasLocalHold && cam != null)
            return cam.transform.TransformPoint(hand.HoldLocalToCam);

        // Fallback: constant hold point in front of camera
        return cam.transform.position + cam.transform.forward * 1.6f;
    }

    private class GrabHand
    {
        public bool IsGrabbing => _targetRb != null && _handleRb != null && _joint != null;

        public bool HasLocalHold { get; private set; }
        public Vector3 HoldLocalToCam { get; private set; }

        private readonly string _name;

        private Rigidbody _targetRb;
        private Rigidbody _handleRb;
        private ConfigurableJoint _joint;

        private float _weightMultiplier = 1f;

        // smoothed/limited hold target in world-space
        private Vector3 _holdTargetWorld;
        private bool _holdTargetInitialized;

        // collision ignore bookkeeping
        private bool _ignoredCollision;
        private Collider[] _playerCols;
        private Collider _grabbedCol;

        public GrabHand(string name) => _name = name;

        public void StartGrab(
            Rigidbody target,
            Vector3 grabPointWorld,
            float weightMultiplier,
            bool useLocalHold,
            Vector3 holdLocalToCam,
            Transform cam,
            bool ignoreCollision,
            Collider[] playerColliders,
            Collider grabbedCollider
        )
        {
            Release();

            _targetRb = target;
            _weightMultiplier = Mathf.Max(0.01f, weightMultiplier);

            HasLocalHold = useLocalHold;
            HoldLocalToCam = holdLocalToCam;

            GameObject go = new GameObject(_name);
            _handleRb = go.AddComponent<Rigidbody>();
            _handleRb.mass = 0.2f;
            _handleRb.useGravity = false;
            _handleRb.interpolation = RigidbodyInterpolation.Interpolate;
            _handleRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            go.transform.position = grabPointWorld;
            go.transform.rotation = Quaternion.identity;

            _joint = go.AddComponent<ConfigurableJoint>();
            _joint.connectedBody = _targetRb;
            _joint.autoConfigureConnectedAnchor = false;

            _joint.anchor = Vector3.zero;
            _joint.connectedAnchor = _targetRb.transform.InverseTransformPoint(grabPointWorld);

            _joint.xMotion = ConfigurableJointMotion.Locked;
            _joint.yMotion = ConfigurableJointMotion.Locked;
            _joint.zMotion = ConfigurableJointMotion.Locked;

            _joint.angularXMotion = ConfigurableJointMotion.Free;
            _joint.angularYMotion = ConfigurableJointMotion.Free;
            _joint.angularZMotion = ConfigurableJointMotion.Free;

            _joint.projectionMode = JointProjectionMode.PositionAndRotation;
            _joint.projectionDistance = 0.1f;
            _joint.projectionAngle = 10f;

            _targetRb.solverIterations = Mathf.Max(_targetRb.solverIterations, 12);
            _targetRb.solverVelocityIterations = Mathf.Max(_targetRb.solverVelocityIterations, 12);

            // Initialize hold target = current handle position (zero yank)
            _holdTargetWorld = _handleRb.position;
            _holdTargetInitialized = true;

            if (ignoreCollision && playerColliders != null && grabbedCollider != null)
            {
                _ignoredCollision = true;
                _playerCols = playerColliders;
                _grabbedCol = grabbedCollider;

                for (int i = 0; i < _playerCols.Length; i++)
                {
                    var pc = _playerCols[i];
                    if (pc == null || pc.isTrigger) continue;
                    if (_grabbedCol.isTrigger) continue;
                    Physics.IgnoreCollision(pc, _grabbedCol, true);
                }
            }
        }

        /// <summary>
        /// Drive the handle toward desiredHoldPos, with:
        /// - limited hold target speed (prevents camera yank)
        /// - slack radius (grey zone)
        /// - force clamp (strength)
        /// - optional mass scaling
        ///
        /// Returns load01 and outputs stretch01 (0..1) between slack and armMax.
        /// </summary>
        public float FixedUpdateDrive(
            Vector3 desiredHoldPos,
            float spring, float damp,
            float maxForce,
            float slackRadius,
            float armMax,
            float hardRelease,
            float holdTargetMaxSpeed,
            bool scaleByMass,
            float massReference,
            out float stretch01
        )
        {
            stretch01 = 0f;
            if (!IsGrabbing) return 0f;

            if (!_holdTargetInitialized)
            {
                _holdTargetWorld = _handleRb.position;
                _holdTargetInitialized = true;
            }

            // 1) Limit how fast the desired target can move (kills camera yank)
            float step = holdTargetMaxSpeed * Time.fixedDeltaTime;
            _holdTargetWorld = Vector3.MoveTowards(_holdTargetWorld, desiredHoldPos, step);

            // 2) Compute arm stretch vs slack
            Vector3 toTarget = _holdTargetWorld - _handleRb.position;
            float dist = toTarget.magnitude;

            if (dist > hardRelease)
            {
                Release();
                stretch01 = 1f;
                return 1f;
            }

            stretch01 = Mathf.Clamp01(Mathf.InverseLerp(slackRadius, armMax, dist));

            // 3) Grey zone: within slack, don't pull the object at all
            if (dist <= slackRadius)
            {
                // tiny damping to keep it stable (optional)
                _handleRb.AddForce(-_handleRb.linearVelocity * (damp * 0.25f), ForceMode.Force);
                return 0f;
            }

            Vector3 dir = toTarget / Mathf.Max(0.0001f, dist);
            float error = dist - slackRadius;

            Vector3 force = dir * (error * spring) - _handleRb.linearVelocity * damp;

            // 4) Effective max force (strength, weight multiplier, optional mass scaling)
            float effectiveMaxForce = maxForce / _weightMultiplier;

            if (scaleByMass)
            {
                float massScale = Mathf.Max(1f, _targetRb.mass / Mathf.Max(0.01f, massReference));
                effectiveMaxForce /= massScale;
            }

            float rawMag = force.magnitude;
            float usedMag;

            if (rawMag > effectiveMaxForce)
            {
                force = force.normalized * effectiveMaxForce;
                usedMag = effectiveMaxForce;
            }
            else
            {
                usedMag = rawMag;
            }

            _handleRb.AddForce(force, ForceMode.Force);

            return Mathf.Clamp01(usedMag / Mathf.Max(0.001f, effectiveMaxForce));
        }

        public void Release()
        {
            if (_ignoredCollision && _playerCols != null && _grabbedCol != null)
            {
                for (int i = 0; i < _playerCols.Length; i++)
                {
                    var pc = _playerCols[i];
                    if (pc == null || pc.isTrigger) continue;
                    if (_grabbedCol.isTrigger) continue;
                    Physics.IgnoreCollision(pc, _grabbedCol, false);
                }
            }

            _ignoredCollision = false;
            _playerCols = null;
            _grabbedCol = null;

            if (_joint != null) Object.Destroy(_joint);
            _joint = null;

            if (_handleRb != null) Object.Destroy(_handleRb.gameObject);
            _handleRb = null;

            _targetRb = null;
            _weightMultiplier = 1f;

            HasLocalHold = false;
            HoldLocalToCam = default;

            _holdTargetInitialized = false;
            _holdTargetWorld = default;
        }
    }
}
