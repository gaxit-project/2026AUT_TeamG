using UnityEngine;
using UnityEngine.Profiling;

namespace TeamG.DebugTools
{
    /// <summary>どのゲームでも使うシステム系の項目。</summary>
    internal static class SystemDebugItems
    {
        const float Megabyte = 1024f * 1024f;

        [DebugMenuItem("システム/タイムスケール", Min = 0f, Max = 4f)]
        static float TimeScale
        {
            get => Time.timeScale;
            set => Time.timeScale = value;
        }

        [DebugMenuItem("システム/タイムスケールを 1 に戻す")]
        static void ResetTimeScale()
        {
            Time.timeScale = 1f;
        }

        [DebugMenuItem("システム/一時停止 (タイムスケール 0)")]
        static void Pause()
        {
            Time.timeScale = 0f;
        }

        [DebugMenuItem("システム/解像度", Order = 100)]
        static string Resolution => $"{Screen.width} x {Screen.height}";

        [DebugMenuItem("システム/メモリ (確保 / 予約)", Order = 101)]
        static string Memory =>
            $"{Profiler.GetTotalAllocatedMemoryLong() / Megabyte:0} MB / {Profiler.GetTotalReservedMemoryLong() / Megabyte:0} MB";

        [DebugMenuItem("システム/ビルド", Order = 102)]
        static string Build => $"{Application.version} ({Application.unityVersion})";
    }
}
