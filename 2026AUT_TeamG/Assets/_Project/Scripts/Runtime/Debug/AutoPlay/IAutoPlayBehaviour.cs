using UnityEngine.InputSystem.LowLevel;

namespace TeamG.DebugTools.AutoPlay
{
    /// <summary>
    /// ボットの「頭脳」。毎フレーム、仮想ゲームパッドの状態を決める。
    /// ゲームが形になってきたら、ここを実装してシナリオ型のボット（目的地へ歩く など）を足す。
    /// </summary>
    public interface IAutoPlayBehaviour
    {
        string Name { get; }

        /// <summary>今フレームのゲームパッド入力を返す。</summary>
        GamepadState Tick(float unscaledDeltaTime);
    }
}
