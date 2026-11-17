using UnityEngine;

namespace TeamG.Gameplay
{
    public class RodeoMachine : MonoBehaviour
    {
        [SerializeField] Rigidbody body;
        [Header("左右に回転する最大角度")]
        [SerializeField] float yawRange = 45f;
        [Header("左右の振動の頻度")]
        [SerializeField] float yawFrequency = 0.3f;
        [Header("前後への振動角度")]
        [SerializeField] float pitchAngle = 15f;
        [Header("揺れの速さ")]
        [SerializeField] Vector2 calmFrequencyRange = new Vector2(0.3f, 0.5f);    //通常時
        [SerializeField] Vector2 burstFrequencyRange = new Vector2(0.8f, 1.0f);   //暴走時
        [SerializeField] float calmDuration = 5f;          //通常時の時間
        [SerializeField] float burstDuration = 3f;         //暴走時の時間
        [SerializeField] float wanderSpeed = 0.5f;         //周回する速さ(大きいほどせわしない)
        [SerializeField] float frequencyResponse = 3f;     //目標の速さに近づく速さ(大きいほどすぐに切り替わる)
        [SerializeField] float powerResponse = 3f;         //揺れの強さの変化の速さ(大きいほどすぐに切り替わる)

        Vector3 restLocalPosition;
        Quaternion restLocalRotation;

        float power = 1f;           //揺れの強さ。1 = 全開、0 = 停止
        float yawPhase;
        float phase;
        float frequency;
        float modeTimer;

        bool isBursting;
        bool running = true;

        public bool Running => running;
        public void SetRunning(bool value) => running = value;

        void Awake()
        {
            restLocalPosition = transform.InverseTransformPoint(body.position);
            restLocalRotation = Quaternion.Inverse(transform.rotation) * body.rotation;
            modeTimer = calmDuration;
            frequency = calmFrequencyRange.x;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            UpdateFrequency(dt);
            float targetPower = running ? 1f : 0f;
            power = Mathf.Lerp(power, targetPower, 1f - Mathf.Exp(-powerResponse * dt));
            yawPhase += yawFrequency * 2f * Mathf.PI * dt;
            float yaw = yawRange * power * Mathf.Sin(yawPhase);
            phase += frequency * 2f * Mathf.PI * dt;
            float pitch = pitchAngle * power * Mathf.Sin(phase);
            Quaternion rot = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.Euler(pitch, 0f, 0f) * restLocalRotation;
            body.MovePosition(transform.TransformPoint(restLocalPosition));
            body.MoveRotation(transform.rotation * rot);
        }

        void UpdateFrequency(float dt)
        {
            modeTimer -= dt;
            if (modeTimer <= 0f)
            {
                isBursting = !isBursting;
                modeTimer = isBursting ? burstDuration : calmDuration;
            }
            Vector2 range = isBursting ? burstFrequencyRange : calmFrequencyRange;
            float noise = Mathf.Clamp01(Mathf.PerlinNoise(Time.fixedTime * wanderSpeed, 0f));
            float target = Mathf.Lerp(range.x, range.y, noise);
            frequency = Mathf.Lerp(frequency, target, 1f - Mathf.Exp(-frequencyResponse * dt));
        }
    }
}
