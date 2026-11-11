using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TeamG.DebugTools.AutoPlay
{
    /// <summary>自動プレイ中の例外・エラーログ・スタック・FPS 低下を見張り、レポートにまとめる。</summary>
    internal sealed class AutoPlayMonitor
    {
        internal const string LogPrefix = "[AutoPlay]";

        // シーン読み込み直後のカクつきは数えない
        const float FpsWarmupSeconds = 3f;
        const float PlayerSearchInterval = 1f;

        readonly AutoPlaySettings settings;
        readonly AutoPlayReport report;
        readonly string outputDirectory;
        readonly Dictionary<string, AutoPlayIssue> issuesByKey = new Dictionary<string, AutoPlayIssue>();
        readonly Queue<(string message, string stackTrace, LogType type)> pendingLogs = new Queue<(string, string, LogType)>();

        float elapsed;
        int screenshotCount;

        float fpsWindowTime;
        int fpsWindowFrames;
        float fpsSum;
        int fpsSamples;
        float sinceSceneLoaded;

        Transform player;
        float playerSearchTimer;
        Vector3 stuckAnchor;
        float stuckTimer;

        public AutoPlayMonitor(AutoPlaySettings settings, string behaviourName, string outputDirectory)
        {
            this.settings = settings;
            this.outputDirectory = outputDirectory;
            report = new AutoPlayReport
            {
                StartedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Behaviour = behaviourName,
                Seed = settings.Seed,
                TimeScale = settings.TimeScale,
                UnityVersion = Application.unityVersion,
                AppVersion = Application.version,
                MinFps = float.MaxValue,
            };
        }

        public int IssueCount => report.Issues.Count;

        public void Begin()
        {
            Application.logMessageReceivedThreaded += OnLog;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        /// <summary>メインスレッドから毎フレーム呼ぶ。</summary>
        public void Tick(float unscaledDeltaTime, float scaledDeltaTime)
        {
            elapsed += unscaledDeltaTime;
            sinceSceneLoaded += unscaledDeltaTime;
            DrainLogs();
            UpdateFps(unscaledDeltaTime);
            UpdateStuck(scaledDeltaTime);
        }

        /// <summary>監視をやめてレポートを書き出す。</summary>
        public AutoPlayReport End()
        {
            Application.logMessageReceivedThreaded -= OnLog;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            DrainLogs();

            report.ElapsedSeconds = elapsed;
            report.AverageFps = fpsSamples > 0 ? fpsSum / fpsSamples : 0f;
            if (fpsSamples == 0)
            {
                report.MinFps = 0f;
            }
            report.Write(outputDirectory);
            return report;
        }

        // 別スレッドから呼ばれることがあるので、キューに積むだけにする
        void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Log || type == LogType.Warning || message.StartsWith(LogPrefix))
            {
                return;
            }

            lock (pendingLogs)
            {
                pendingLogs.Enqueue((message, stackTrace, type));
            }
        }

        void DrainLogs()
        {
            while (true)
            {
                (string message, string stackTrace, LogType type) log;
                lock (pendingLogs)
                {
                    if (pendingLogs.Count == 0)
                    {
                        return;
                    }
                    log = pendingLogs.Dequeue();
                }

                var kind = log.type == LogType.Exception ? AutoPlayIssueKind.Exception : AutoPlayIssueKind.Error;
                AddIssue(kind, $"{kind}:{AutoPlayReport.FirstLine(log.message)}", log.message, log.stackTrace);
            }
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            sinceSceneLoaded = 0f;
            player = null;
            stuckTimer = 0f;
        }

        void UpdateFps(float dt)
        {
            fpsWindowTime += dt;
            fpsWindowFrames++;
            if (fpsWindowTime < 1f)
            {
                return;
            }

            var fps = fpsWindowFrames / fpsWindowTime;
            fpsWindowTime = 0f;
            fpsWindowFrames = 0;
            if (sinceSceneLoaded < FpsWarmupSeconds)
            {
                return;
            }

            fpsSum += fps;
            fpsSamples++;
            report.MinFps = Mathf.Min(report.MinFps, fps);
            if (fps < settings.LowFpsThreshold)
            {
                var scene = SceneManager.GetActiveScene().name;
                AddIssue(AutoPlayIssueKind.LowFps, $"LowFps:{scene}", $"FPS が {settings.LowFpsThreshold} を下回りました（{fps:0.0} FPS）", null);
            }
        }

        // タイムスケール 0（ポーズ中）は動かなくて当然なので、スケール後の時間で数える
        void UpdateStuck(float scaledDeltaTime)
        {
            if (player == null)
            {
                playerSearchTimer -= Time.unscaledDeltaTime;
                if (playerSearchTimer > 0f)
                {
                    return;
                }

                playerSearchTimer = PlayerSearchInterval;
                player = FindPlayer();
                if (player == null)
                {
                    return;
                }
                stuckAnchor = player.position;
                stuckTimer = 0f;
            }

            if (Vector3.Distance(player.position, stuckAnchor) > settings.StuckDistance)
            {
                stuckAnchor = player.position;
                stuckTimer = 0f;
                return;
            }

            stuckTimer += scaledDeltaTime;
            if (stuckTimer < settings.StuckSeconds)
            {
                return;
            }

            stuckTimer = 0f;
            var p = player.position;
            var key = $"Stuck:{SceneManager.GetActiveScene().name}:{Mathf.Round(p.x)},{Mathf.Round(p.y)},{Mathf.Round(p.z)}";
            AddIssue(AutoPlayIssueKind.Stuck, key, $"{settings.StuckSeconds:0} 秒以上プレイヤーが動いていません", null);
        }

        Transform FindPlayer()
        {
            try
            {
                var go = GameObject.FindWithTag(settings.PlayerTag);
                return go != null ? go.transform : null;
            }
            catch (UnityException)
            {
                // タグが Tags & Layers に登録されていない
                return null;
            }
        }

        void AddIssue(AutoPlayIssueKind kind, string key, string message, string stackTrace)
        {
            if (issuesByKey.TryGetValue(key, out var existing))
            {
                existing.Count++;
                return;
            }

            var issue = new AutoPlayIssue
            {
                Kind = kind,
                KindName = kind.ToString(),
                Message = message,
                StackTrace = stackTrace,
                Count = 1,
                FirstSeconds = elapsed,
                Scene = SceneManager.GetActiveScene().name,
                PlayerPosition = player != null ? player.position.ToString("F1") : null,
                Screenshot = TakeScreenshot(),
            };
            issuesByKey.Add(key, issue);
            report.Issues.Add(issue);
            UnityEngine.Debug.Log($"{LogPrefix} {kind}: {AutoPlayReport.FirstLine(message)}");
        }

        string TakeScreenshot()
        {
            if (screenshotCount >= settings.MaxScreenshots)
            {
                return null;
            }

            screenshotCount++;
            var fileName = $"shot_{screenshotCount:000}.png";
            Directory.CreateDirectory(outputDirectory);
            ScreenCapture.CaptureScreenshot(Path.Combine(outputDirectory, fileName));
            return fileName;
        }
    }
}
