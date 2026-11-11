using System.IO;
using TeamG.DebugTools.AutoPlay;
using UnityEditor;
using UnityEngine;

namespace TeamG.EditorTools
{
    /// <summary>エディタから自動プレイを始めるメニュー。開いているシーンを再生して、すぐにボットを動かす。</summary>
    internal static class AutoPlayMenu
    {
        const string MenuRoot = "TeamG/自動プレイ/";

        [MenuItem(MenuRoot + "再生して開始（5分）", priority = 0)]
        static void PlayFiveMinutes()
        {
            Play(new AutoPlaySettings { DurationSeconds = 300f, QuitOnFinish = true });
        }

        [MenuItem(MenuRoot + "再生して開始（5分・2倍速）", priority = 1)]
        static void PlayFiveMinutesDoubleSpeed()
        {
            Play(new AutoPlaySettings { DurationSeconds = 300f, TimeScale = 2f, QuitOnFinish = true });
        }

        [MenuItem(MenuRoot + "再生して開始（止めるまで）", priority = 2)]
        static void PlayUnlimited()
        {
            Play(new AutoPlaySettings { DurationSeconds = 0f });
        }

        [MenuItem(MenuRoot + "再生して開始（5分）", true)]
        [MenuItem(MenuRoot + "再生して開始（5分・2倍速）", true)]
        [MenuItem(MenuRoot + "再生して開始（止めるまで）", true)]
        static bool CanPlay()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem(MenuRoot + "停止してレポートを書き出す", priority = 20)]
        static void Stop()
        {
            AutoPlayer.End();
        }

        [MenuItem(MenuRoot + "停止してレポートを書き出す", true)]
        static bool CanStop()
        {
            return AutoPlayer.IsRunning;
        }

        [MenuItem(MenuRoot + "レポートフォルダを開く", priority = 40)]
        static void OpenReports()
        {
            var root = AutoPlayer.ReportRoot;
            Directory.CreateDirectory(root);
            EditorUtility.RevealInFinder(AutoPlayer.LastReportDirectory != null && Directory.Exists(AutoPlayer.LastReportDirectory)
                ? AutoPlayer.LastReportDirectory
                : root + Path.DirectorySeparatorChar);
        }

        static void Play(AutoPlaySettings settings)
        {
            // 再生開始時のドメインリロードで static は消えるので、SessionState 経由で AutoPlayer に渡す
            SessionState.SetString(AutoPlayer.EditorPendingKey, JsonUtility.ToJson(settings));
            EditorApplication.isPlaying = true;
        }
    }
}
