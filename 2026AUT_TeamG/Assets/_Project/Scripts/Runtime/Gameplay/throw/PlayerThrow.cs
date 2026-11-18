using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TeamG.Gameplay
{
    // Reads the existing camera's aim; never controls the camera, pointer or HUD.
    public class PlayerThrow : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform throwPoint;
        [SerializeField] private PaintBall ballPrefab;
        [Tooltip("Assign only this player's root, not the scene/world root.")]
        [SerializeField] private Transform ownerRoot;

        [Header("Throw")]
        [SerializeField, Min(0.1f)] private float minSpeed = 8f;
        [SerializeField, Min(0.1f)] private float maxSpeed = 22f;
        [SerializeField, Min(0.01f)] private float chargeSeconds = 1.2f;
        [SerializeField, Min(0f)] private float cooldownSeconds = 0.25f;

        [Header("Aim (read only - no camera control)")]
        [SerializeField, Min(1f)] private float aimDistance = 100f;
        [SerializeField] private LayerMask aimMask = ~0;

        private PaintBall heldBall;
        private float chargeStartedAt;
        private float nextThrowAt;
        private bool focused = true;

#if ENABLE_INPUT_SYSTEM
        private InputAction throwInput;
        private InputAction cancelInput;
#endif

        public float Charge01
        {
            get
            {
                if (heldBall == null)
                {
                    return 0f;
                }

                return Mathf.Clamp01(
                    (Time.time - chargeStartedAt) / Mathf.Max(0.01f, chargeSeconds));
            }
        }

        // Keep this signature compatible with RodeoSplashDemoBuilder.
        public void Configure(Camera camera, Transform point, PaintBall prefab, Transform owner)
        {
            playerCamera = camera;
            throwPoint = point;
            ballPrefab = prefab;
            ownerRoot = owner;
        }

        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            // InputSystem_Actions is the project's configured, automatically enabled asset.
            InputActionAsset actions = InputSystem.actions;
            if (actions == null)
            {
                Debug.LogError("PlayerThrow: assign InputSystem_Actions as the Project-wide Actions asset in Project Settings > Input System Package.", this);
                enabled = false;
                return;
            }

            throwInput = actions.FindAction("Player/Throw");
            cancelInput = actions.FindAction("Player/CancelThrow");
            if (throwInput == null || cancelInput == null)
            {
                Debug.LogError("PlayerThrow: add Player/Throw and Player/CancelThrow to InputSystem_Actions.", this);
                enabled = false;
            }
#endif
        }

        private void Start()
        {
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera == null) playerCamera = Camera.main;
            if (ownerRoot == null) ownerRoot = transform;

            if (playerCamera == null || throwPoint == null || ballPrefab == null)
            {
                Debug.LogError("PlayerThrow: assign Player Camera, Throw Point and Ball Prefab.", this);
                enabled = false;
            }
        }

        private void Update()
        {
            if (!focused || Time.timeScale <= 0f)
            {
                CancelCharge();
                return;
            }

#if ENABLE_INPUT_SYSTEM || ENABLE_LEGACY_INPUT_MANAGER
            bool pressed;
            bool released;
            bool cancel;

#if ENABLE_INPUT_SYSTEM
            if (!throwInput.enabled)
            {
                CancelCharge();
                return;
            }
            pressed = throwInput.WasPressedThisFrame();
            released = throwInput.WasReleasedThisFrame();
            cancel = cancelInput.WasPressedThisFrame();
#else
            pressed = Input.GetMouseButtonDown(0);
            released = Input.GetMouseButtonUp(0);
            cancel = Input.GetKeyDown(KeyCode.Escape);
#endif

            if (cancel)
            {
                CancelCharge();
                return;
            }

            if (pressed && heldBall == null && Time.time >= nextThrowAt)
            {
                heldBall = Instantiate(ballPrefab, throwPoint.position, throwPoint.rotation);
                heldBall.Prepare(throwPoint);
                chargeStartedAt = Time.time;
            }

            if (released && heldBall != null)
            {
                PaintBall ball = heldBall;
                float speed = Mathf.Lerp(minSpeed, Mathf.Max(minSpeed, maxSpeed), Charge01);
                Vector3 target = GetAimPoint();
                Vector3 direction = target - ball.transform.position;
                if (direction.sqrMagnitude < 0.0001f) direction = playerCamera.transform.forward;
                heldBall = null;
                ball.Launch(direction.normalized * speed, ownerRoot);
                nextThrowAt = Time.time + cooldownSeconds;
            }
#else
            CancelCharge();
#endif
        }

        private Vector3 GetAimPoint()
        {
            // Use the screen centre. The throw point controls the origin, not the aim.
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float maxDistance = Mathf.Max(1f, aimDistance);
            Vector3 target = ray.GetPoint(maxDistance);
            float nearestDistance = float.PositiveInfinity;

            // RaycastAll is used only on release, to skip the player's own colliders.
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, aimMask, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (ownerRoot != null && hit.collider.transform.IsChildOf(ownerRoot)) continue;
                if (hit.distance >= nearestDistance) continue;
                nearestDistance = hit.distance;
                target = hit.point;
            }
            return target;
        }

        private void CancelCharge()
        {
            if (heldBall != null) Destroy(heldBall.gameObject);
            heldBall = null;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            focused = hasFocus;
            if (!hasFocus) CancelCharge();
        }

        private void OnDisable()
        {
            CancelCharge();
            // The project owns these shared actions; leave their lifetime to the Input System.
        }
    }
}
