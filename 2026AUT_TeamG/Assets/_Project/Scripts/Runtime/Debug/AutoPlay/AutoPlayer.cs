using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TeamG.DebugTools.AutoPlay
{
    /// <summary>
    /// 自動プレイの本体。仮想ゲームパッドを追加してボットに操作させ、終わったらレポートを書き出す。
    /// ゲーム側からは人がゲームパッドで遊んでいるのと区別がつかないので、ゲームのコードに手を入れなくてよい。
    /// </summary>
    public sealed class AutoPlayer : MonoBehaviour
    {
        /// <summary>エディタのメニューから「再生して開始」するときの受け渡し用（SessionState のキー）。</summary>
        public const string EditorPendingKey = "TeamG.AutoPlay.Pending";

        static AutoPlayer instance;

        AutoPlaySettings settings;
        IAutoPlayBehaviour behaviour;
        AutoPlayMonitor monitor;
        Gamepad gamepad;
        float elapsed;
        float previousTimeScale;
        bool previousRunInBackground;
#if !UNITY_EDITOR
        InputSettings.BackgroundBehavior previousBackgroundBehavior;
#endif
        bool finished;
        bool destroying;

        public static bool IsRunning => instance != null;

        /// <summary>実行中なら経過秒数と見つかった問題の数。</summary>
        public static string Status => instance == null
            ? "停止中"
            : $"{instance.behaviour.Name} 実行中 {instance.elapsed:0} 秒 / 問題 {instance.monitor.IssueCount} 種類";

        /// <summary>最後に書き出したレポートのフォルダ。</summary>
        public static string LastReportDirectory { get; private set; }

        /// <summary>レポートの出力先。PC ならプロジェクト（実行ファイル）の隣の AutoPlayReports。</summary>
        public static string ReportRoot => Application.isMobilePlatform
            ? Path.Combine(Application.persistentDataPath, "AutoPlayReports")
            : Path.GetFullPath(Path.Combine(Application.dataPath, "..", "AutoPlayReports"));

        public static void Begin(AutoPlaySettings settings, IAutoPlayBehaviour behaviour = null)
        {
            if (instance != null)
            {
                UnityEngine.Debug.LogWarning($"{AutoPlayMonitor.LogPrefix} すでに実行中です");
                return;
            }

            if (settings.Seed == 0)
            {
                settings.Seed = Math.Max(1, Environment.TickCount & int.MaxValue);
            }

            var go = new GameObject("[AutoPlayer]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AutoPlayer>();
            instance.Setup(settings, behaviour ?? new RandomInputBehaviour(settings.Seed));
        }

        /// <summary>止めてレポートを書き出す。</summary>
        public static void End()
        {
            if (instance != null)
            {
                instance.Finish();
            }
        }

        void Setup(AutoPlaySettings settings, IAutoPlayBehaviour behaviour)
        {
            this.settings = settings;
            this.behaviour = behaviour;

            var directory = Path.Combine(ReportRoot, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            monitor = new AutoPlayMonitor(settings, behaviour.Name, directory);
            LastReportDirectory = directory;

            gamepad = InputSystem.AddDevice<Gamepad>("AutoPlayGamepad");
            gamepad.MakeCurrent();

            // ウィンドウが非アクティブでも止まらないようにする（夜間テストで別のウィンドウが前に出ても続ける）
            previousRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
#if !UNITY_EDITOR
            // エディタではゲームパッド入力はフォーカスに関係なく届く。設定アセットを書き換えないよう実機だけ変える
            previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#endif

            previousTimeScale = Time.timeScale;
            Time.timeScale = settings.TimeScale;

            monitor.Begin();
            var duration = settings.DurationSeconds > 0f ? $"{settings.DurationSeconds:0} 秒" : "無制限";
            UnityEngine.Debug.Log($"{AutoPlayMonitor.LogPrefix} 開始: {behaviour.Name} / シード {settings.Seed} / {duration}");
        }

        void Update()
        {
            if (finished)
            {
                return;
            }

            var dt = Time.unscaledDeltaTime;
            elapsed += dt;
            InputSystem.QueueStateEvent(gamepad, behaviour.Tick(dt));
            monitor.Tick(dt, Time.deltaTime);

            if (settings.DurationSeconds > 0f && elapsed >= settings.DurationSeconds)
            {
                Finish();
            }
        }

        // 再生停止やアプリ終了で途中終了したときも、そこまでのレポートを残す
        void OnDestroy()
        {
            destroying = true;
            Finish();
        }

        void Finish()
        {
            if (finished)
            {
                return;
            }
            finished = true;

            var report = monitor.End();
            UnityEngine.Debug.Log($"{AutoPlayMonitor.LogPrefix} 終了: 問題 {report.Issues.Count} 種類 → {LastReportDirectory}");

            if (gamepad != null && gamepad.added)
            {
                InputSystem.RemoveDevice(gamepad);
            }
            Application.runInBackground = previousRunInBackground;
#if !UNITY_EDITOR
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
#endif
            Time.timeScale = previousTimeScale;

            if (instance == this)
            {
                instance = null;
            }
            if (!destroying)
            {
                Destroy(gameObject);
            }

            if (settings.QuitOnFinish)
            {
                Quit(report.HasProblems ? 1 : 0);
            }
        }

        // CI で結果を判定できるよう、問題があれば終了コード 1 にする
        static void Quit(int exitCode)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(exitCode);
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartFromLaunchOptions()
        {
            var settings = AutoPlaySettings.FromCommandLine(Environment.GetCommandLineArgs());

#if UNITY_EDITOR
            var pending = UnityEditor.SessionState.GetString(EditorPendingKey, "");
            if (pending.Length > 0)
            {
                UnityEditor.SessionState.EraseString(EditorPendingKey);
                settings = JsonUtility.FromJson<AutoPlaySettings>(pending);
            }
#endif

            if (settings != null)
            {
                Begin(settings);
            }
        }
    }
}
