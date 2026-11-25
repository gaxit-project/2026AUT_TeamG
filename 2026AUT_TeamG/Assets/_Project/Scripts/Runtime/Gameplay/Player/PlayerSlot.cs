using System.Collections.Generic;
using UnityEngine;

namespace TeamG.Gameplay
{
    /// <summary>プレイヤーの番号札</summary>
    public sealed class PlayerSlot : MonoBehaviour
    {
        // シーンにいる番号札の名簿（全員で1つ）
        static readonly List<PlayerSlot> all = new List<PlayerSlot>();
        public static IReadOnlyList<PlayerSlot> All => all;

        [SerializeField, Tooltip("0 = 1P, 1 = 2P")] int index;
        [SerializeField] PlayerHealth health;
        [SerializeField, Tooltip("落ちない人は空でOK")] FallOffRodeo rider;
        [SerializeField] RodeoMachine machine;
        [SerializeField, Tooltip("対戦中だけ動かすもの（投げる、落下判定、視点操作）")]
        Behaviour[] controls;

        public int Index => index;

        /// <summary>このラウンドで負けたか</summary>
        public bool HasLost =>
            (health != null && health.IsDead) ||
            (rider != null && rider.IsFalling);

        /// <summary>対戦中か</summary>
        public void SetPlaying(bool playing)
        {
            if (machine != null) machine.SetRunning(playing);

            foreach (Behaviour control in controls)
            {
                if (control != null) control.enabled = playing;
            }
        }

        void OnEnable()
        {
            all.Add(this);
        }

        void OnDisable()
        {
            all.Remove(this);
        }
    }
}
