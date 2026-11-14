using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TeamG.Gameplay
{
    public class PlayerThrow : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform throwPoint;
        [SerializeField] private PaintBall ballPrefab;
        [SerializeField] private Transform ownerRoot;

        [Header("Throw")]
        [SerializeField, Min(0.1f)] private float minSpeed = 8f;
        [SerializeField, Min(0.1f)] private float maxSpeed = 22f;
        [SerializeField, Min(0.01f)] private float chargeSeconds = 1.2f;
        [SerializeField, Min(0f)] private float cooldownSeconds = 0.25f;

        [Header("FPS Camera")]
        [SerializeField] private bool controlMouseLook = false;
        [SerializeField, Min(0.01f)] private float mouseSensitivity = 0.12f;
        [SerializeField] private bool showPrototypeHud = true;

        private PaintBall heldBall;
        private float chargeStartedAt;
        private float nextThrowAt;
        private float yaw;
        private float pitch;
        private bool focused = true;

        public float Charge01 => heldBall == null
            ? 0f
            : Mathf.Clamp01(
                (Time.time - chargeStartedAt) /
                Mathf.Max(0.01f, chargeSeconds));

        // 確認用シーンの作成スクリプトから呼ばれます。
        public void Configure(
            Camera camera,
            Transform point,
            PaintBall prefab,
            Transform owner)
        {
            playerCamera = camera;
            throwPoint = point;
            ballPrefab = prefab;
            ownerRoot = owner;
        }

        private void Start()
        {
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();

            if (ownerRoot == null)
                ownerRoot = transform;

            if (playerCamera == null ||
                throwPoint == null ||
                ballPrefab == null)
            {
                Debug.LogError(
                    "PlayerThrow: Player Camera、Throw Point、Ball Prefabを設定してください。",
                    this);

                enabled = false;
                return;
            }

            Vector3 angles = playerCamera.transform.localEulerAngles;
            yaw = angles.y;
            pitch = Mathf.DeltaAngle(0f, angles.x);

            if (controlMouseLook)
                SetCursorLocked(true);
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
            bool escape;
            Vector2 mouseDelta;

#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;

            if (mouse == null)
            {
                CancelCharge();
                return;
            }

            pressed = mouse.leftButton.wasPressedThisFrame;
            released = mouse.leftButton.wasReleasedThisFrame;

            escape = Keyboard.current != null &&
                     Keyboard.current.escapeKey.wasPressedThisFrame;

            mouseDelta = mouse.delta.ReadValue();
#else
            pressed = Input.GetMouseButtonDown(0);
            released = Input.GetMouseButtonUp(0);
            escape = Input.GetKeyDown(KeyCode.Escape);

            mouseDelta = new Vector2(
                Input.GetAxisRaw("Mouse X"),
                Input.GetAxisRaw("Mouse Y")) * 10f;
#endif

            if (escape)
            {
                CancelCharge();

                if (controlMouseLook)
                    SetCursorLocked(false);

                return;
            }

            // マウスでFPS視点を動かします。
            if (controlMouseLook)
            {
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    CancelCharge();

                    if (pressed)
                        SetCursorLocked(true);

                    return;
                }

                yaw += mouseDelta.x * mouseSensitivity;
                pitch = Mathf.Clamp(
                    pitch - mouseDelta.y * mouseSensitivity,
                    -80f,
                    80f);

                playerCamera.transform.localRotation =
                    Quaternion.Euler(pitch, yaw, 0f);
            }

            // 左クリックを押した瞬間に球を構えます。
            if (pressed &&
                heldBall == null &&
                Time.time >= nextThrowAt)
            {
                heldBall = Instantiate(
                    ballPrefab,
                    throwPoint.position,
                    throwPoint.rotation);

                heldBall.Prepare(throwPoint);
                chargeStartedAt = Time.time;
            }

            // 左クリックを離した瞬間に投げます。
            if (released && heldBall != null)
            {
                PaintBall ball = heldBall;

                float speed = Mathf.Lerp(
                    minSpeed,
                    Mathf.Max(minSpeed, maxSpeed),
                    Charge01);

                heldBall = null;

                ball.Launch(
                    playerCamera.transform.forward * speed,
                    ownerRoot);

                nextThrowAt = Time.time + cooldownSeconds;
            }
#else
            CancelCharge();
#endif
        }

        private void CancelCharge()
        {
            if (heldBall != null)
                Destroy(heldBall.gameObject);

            heldBall = null;
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked
                ? CursorLockMode.Locked
                : CursorLockMode.None;

            Cursor.visible = !locked;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            focused = hasFocus;

            if (!hasFocus)
            {
                CancelCharge();

                if (controlMouseLook)
                    SetCursorLocked(false);
            }
        }

        private void OnDisable()
        {
            CancelCharge();

            if (controlMouseLook)
                SetCursorLocked(false);
        }

        private void OnGUI()
        {
            if (!showPrototypeHud)
                return;

            GUI.Label(
                new Rect(
                    Screen.width * 0.5f - 5f,
                    Screen.height * 0.5f - 10f,
                    20f,
                    24f),
                "+");

            GUI.Label(
                new Rect(12f, 12f, 450f, 24f),
                "Hold LMB: ready / Release: throw / Esc: unlock");

            if (heldBall != null)
            {
                GUI.HorizontalSlider(
                    new Rect(12f, 44f, 180f, 20f),
                    Charge01,
                    0f,
                    1f);
            }
        }
    }
}