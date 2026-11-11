using System;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace TeamG.DebugTools.AutoPlay
{
    /// <summary>
    /// モンキーテスト用のランダム入力。スティックを一定時間倒し、ボタンをランダムに押したり離したりする。
    /// 同じシードなら同じ入力列になる（フレーム時間が違うと完全には一致しない）。
    /// </summary>
    public sealed class RandomInputBehaviour : IAutoPlayBehaviour
    {
        // L3 / R3（デバッグメニューの開閉）と Select は押さない
        static readonly GamepadButton[] Buttons =
        {
            GamepadButton.South, GamepadButton.East, GamepadButton.West, GamepadButton.North,
            GamepadButton.LeftShoulder, GamepadButton.RightShoulder,
            GamepadButton.DpadUp, GamepadButton.DpadDown, GamepadButton.DpadLeft, GamepadButton.DpadRight,
            GamepadButton.Start,
        };

        // Start（ポーズ）は押されすぎると進まないので確率を下げる
        static readonly float[] PressChancePerSecond =
        {
            1.5f, 1.0f, 1.0f, 0.6f,
            0.4f, 0.4f,
            0.3f, 0.3f, 0.3f, 0.3f,
            0.03f,
        };

        const float MinHoldSeconds = 0.05f;
        const float MaxHoldSeconds = 0.6f;

        readonly System.Random random;
        readonly float[] buttonReleaseTimers = new float[Buttons.Length];

        Vector2 leftStick;
        Vector2 rightStick;
        float leftStickTimer;
        float rightStickTimer;
        float leftTrigger;
        float rightTrigger;
        float triggerTimer;

        public RandomInputBehaviour(int seed)
        {
            random = new System.Random(seed);
        }

        public string Name => "ランダム入力";

        public GamepadState Tick(float dt)
        {
            leftStick = UpdateStick(leftStick, ref leftStickTimer, dt, 0.3f, 2.5f);
            rightStick = UpdateStick(rightStick, ref rightStickTimer, dt, 0.2f, 1.5f);

            triggerTimer -= dt;
            if (triggerTimer <= 0f)
            {
                triggerTimer = Range(0.2f, 1.5f);
                leftTrigger = random.NextDouble() < 0.2 ? 1f : 0f;
                rightTrigger = random.NextDouble() < 0.3 ? 1f : 0f;
            }

            var state = new GamepadState
            {
                leftStick = leftStick,
                rightStick = rightStick,
                leftTrigger = leftTrigger,
                rightTrigger = rightTrigger,
            };

            for (var i = 0; i < Buttons.Length; i++)
            {
                if (buttonReleaseTimers[i] > 0f)
                {
                    buttonReleaseTimers[i] -= dt;
                }
                else if (random.NextDouble() < PressChancePerSecond[i] * dt)
                {
                    buttonReleaseTimers[i] = Range(MinHoldSeconds, MaxHoldSeconds);
                }

                if (buttonReleaseTimers[i] > 0f)
                {
                    state = state.WithButton(Buttons[i]);
                }
            }

            return state;
        }

        Vector2 UpdateStick(Vector2 current, ref float timer, float dt, float minSeconds, float maxSeconds)
        {
            timer -= dt;
            if (timer > 0f)
            {
                return current;
            }

            timer = Range(minSeconds, maxSeconds);
            // 2割はニュートラル、残りはランダムな方向に倒す
            if (random.NextDouble() < 0.2)
            {
                return Vector2.zero;
            }

            var angle = Range(0f, Mathf.PI * 2f);
            var magnitude = Range(0.5f, 1f);
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * magnitude;
        }

        float Range(float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
