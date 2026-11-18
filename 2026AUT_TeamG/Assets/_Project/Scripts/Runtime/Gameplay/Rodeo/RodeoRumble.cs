using UnityEngine;
using UnityEngine.InputSystem;

namespace TeamG.Gameplay
{
    /// <summary>ロデオの揺れに合わせてゲームパッドを振動させる。</summary>
    public class RodeoRumble : MonoBehaviour
    {
        [SerializeField] RodeoMachine machine;

        [Header("左右:これより振れていないときは静か(0～1)")]
        [SerializeField, Range(0f, 1f)] float sideThreshold = 0.6f;
        [Header("左右に振り切ったときの重さ")]
        [SerializeField, Range(0f, 1f)] float sideStrength = 0.35f;

        [Header("折り返しのガクンの強さ")]
        [SerializeField, Range(0f, 1f)] float joltStrength = 0.8f;
        [Header("ガクンが消えるまでの秒数")]
        [SerializeField] float joltDuration = 0.15f;

        float jolt;   //今のガクンの残り(1 → 0 に減っていく)

        void OnEnable()
        {
            machine.Jolted += OnJolted;
        }

        private void OnDisable()
        {
            machine.Jolted -= OnJolted;
            Gamepad.current?.SetMotorSpeeds(0f, 0f);   //止めないと再生終了後も震え続ける
        }

        void OnJolted(float strength)
        {
            jolt = Mathf.Max(jolt, strength);
        }

        void Update()
        {
            Gamepad pad = Gamepad.current;
            if (pad == null) return;

            jolt = Mathf.MoveTowards(jolt, 0f, Time.deltaTime / joltDuration);

            float yaw = machine.YawNormalized;
            float left = Mathf.InverseLerp(sideThreshold, 1f, -yaw);   //左の端に近いほど 1
            float right = Mathf.InverseLerp(sideThreshold, 1f, yaw);   //右の端に近いほど 1

            float low = left * sideStrength + jolt * joltStrength;                 //重いモーター(左)
            float high = right * sideStrength * 0.5f + jolt * joltStrength * 0.3f; //軽いモーター(右)は控えめに

            pad.SetMotorSpeeds(Mathf.Clamp01(low), Mathf.Clamp01(high));
        }
    }
}
