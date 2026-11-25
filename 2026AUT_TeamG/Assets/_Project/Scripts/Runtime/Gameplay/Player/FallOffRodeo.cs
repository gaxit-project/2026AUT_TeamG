
using UnityEngine;
using UnityEngine.InputSystem;

namespace TeamG.Gameplay
{
    public class FallOffRodeo : MonoBehaviour
    {
        [Header("Base of Raycast")]
        [SerializeField] private Transform rayPosition;
        [SerializeField] private float rayDistance = 10f; // レイキャストの距離
        [SerializeField] private float cameraElapsedTime = 0f; // 視点制御が失敗している時間の計測
        [SerializeField] private float cameraTimeToFall = 3f; // 視点制御が失敗している時間のリミット（この時間を超えると落下する）
        [SerializeField] private string targetTag = "Test";
        [SerializeField] private LayerMask targetLayer;

        [Header("Move on Rodeo")]
        [SerializeField] private InputActionReference leftStickAction;
        // （ロデオの閾値を超えるとLスティックの入力の判定が有効になる）
        [SerializeField] private float requiredYawRange = 30f; // ロデオの回転角度の閾値（左右方向）
        [SerializeField] private float requiredPitchRange = 15f; // ロデオの回転角度の閾値（上下方向）
        [SerializeField] private float requiredStickInput = 0.5f; // Lスティックの入力の閾値

        [SerializeField] private float stickElapsedTime = 0f; // Lスティックの入力が閾値を下回る時間の計測
        [SerializeField] private float stickTimeToFall = 3f; // Lスティックの入力が閾値を下回る時間のリミット（この時間を超えると落下する）

        [Header("Fall of Flags")]
        [SerializeField] private bool isHit = false;
        [SerializeField] private bool isTracking = false;
        [SerializeField] private bool isFalling = false;

        private RaycastHit hit;
        private Rigidbody rb;
        private RodeoMachine rodeoMachine;

        public bool IsFalling => isFalling;

        void Start()
        {
            if (rayPosition == null)
            {
                Debug.LogError("Base of Raycast is not assigned.");
                enabled = false;
                return;
            }

            rb = GetComponent<Rigidbody>();

            if (rb == null)
            {
                Debug.LogError("Rigidbody is not assigned.");
                enabled = false;
                return;
            }

            rb.useGravity = false;
            rb.isKinematic = true;

            rodeoMachine = GetComponentInParent<RodeoMachine>();

            if (rodeoMachine == null)
            {
                Debug.LogError("RodeoMachine is not found.");
                enabled = false;
                return;
            }

            if (leftStickAction == null || leftStickAction.action == null)
            {
                Debug.LogError("Left Stick Action is not assigned.");
                enabled = false;
                return;
            }
        }

        private void Update()
        {
            if (isFalling) return;

            // ① 視点制御の判定
            isHit = Physics.Raycast(
                rayPosition.position,
                rayPosition.forward,
                out hit,
                rayDistance,
                targetLayer
            );

            Debug.DrawRay(
                rayPosition.position,
                rayPosition.forward * rayDistance,
                Color.red
            );

            bool isCameraControlled =
                isHit && hit.collider.CompareTag(targetTag);

            if (isCameraControlled)
            {
                cameraElapsedTime = 0f;
            }
            else
            {
                cameraElapsedTime += Time.deltaTime;
            }

            // ② ロデオの回転角度とスティック入力を取得
            float currentYaw = rodeoMachine.currentYaw;
            float currentPitch = rodeoMachine.currentPitch;

            Vector2 leftStickInput =
                leftStickAction.action.ReadValue<Vector2>();

            // ③ 左右方向（Yaw）の追従判定
            bool isYawTracking = true;

            if (Mathf.Abs(currentYaw) >= requiredYawRange)
            {
                isYawTracking =
                    Mathf.Abs(leftStickInput.x) >= requiredStickInput &&
                    currentYaw * leftStickInput.x > 0f;
            }

            // ④ 上下方向（Pitch）の追従判定
            bool isPitchTracking = true;

            if (Mathf.Abs(currentPitch) >= requiredPitchRange)
            {
                isPitchTracking =
                    Mathf.Abs(leftStickInput.y) >= requiredStickInput &&
                    currentPitch * leftStickInput.y > 0f;
            }

            // ⑤ 左右・上下の両方を判定
            isTracking = isYawTracking && isPitchTracking;

            // ⑥ Lスティックの失敗時間を計測
            if (isTracking)
            {
                stickElapsedTime = 0f;
            }
            else
            {
                stickElapsedTime += Time.deltaTime;
            }

            // ⑦ それぞれ独立して落下判定
            if (cameraElapsedTime >= cameraTimeToFall ||
                stickElapsedTime >= stickTimeToFall)
            {
                Fall();
            }
        }

        // 落ちる処理
        private void Fall()
        {
            if (isFalling) return;

            isFalling = true;

            rb.useGravity = true;
            rb.isKinematic = false;

            Debug.Log("ロデオから落下！");
        }
    }
}
