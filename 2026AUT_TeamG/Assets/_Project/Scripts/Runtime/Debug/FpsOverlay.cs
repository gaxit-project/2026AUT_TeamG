using UnityEngine;

namespace TeamG.DebugTools
{
    /// <summary>画面右上に FPS とフレーム時間を表示する。デバッグメニューの「システム/FPS表示」で切り替える。</summary>
    public sealed class FpsOverlay : MonoBehaviour
    {
        const float ReferenceHeight = 720f;
        const float SampleInterval = 0.5f;

        [DebugMenuItem("システム/FPS表示", Order = -100)]
        static bool Visible { get; set; } = true;

        float elapsed;
        int frames;
        float fps;
        float worstFrameMs;
        float worstInInterval;
        GUIStyle style;

        void Update()
        {
            var dt = Time.unscaledDeltaTime;
            elapsed += dt;
            frames++;
            worstInInterval = Mathf.Max(worstInInterval, dt * 1000f);

            if (elapsed >= SampleInterval)
            {
                fps = frames / elapsed;
                worstFrameMs = worstInInterval;
                elapsed = 0f;
                frames = 0;
                worstInInterval = 0f;
            }
        }

        void OnGUI()
        {
            if (!Visible)
            {
                return;
            }

            style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight, fontSize = 16 };
            style.normal.textColor = fps >= 55f ? Color.green : fps >= 28f ? Color.yellow : Color.red;

            var scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            var width = Screen.width / scale;
            GUI.Label(new Rect(width - 216f, 8f, 208f, 48f), $"{fps:0.0} FPS\nworst {worstFrameMs:0.0} ms", style);
        }

        // Enter Play Mode でドメインリロードを切っている場合に備えて初期値に戻す
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Visible = true;
        }
    }
}
