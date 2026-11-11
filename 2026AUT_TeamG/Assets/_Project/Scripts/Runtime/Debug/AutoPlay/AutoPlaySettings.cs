using System;
using System.Globalization;

namespace TeamG.DebugTools.AutoPlay
{
    /// <summary>自動プレイ1回分の設定。コマンドライン引数からも作れる。</summary>
    [Serializable]
    public sealed class AutoPlaySettings
    {
        /// <summary>実行する秒数（実時間）。0 以下なら止めるまで続ける。</summary>
        public float DurationSeconds = 300f;

        /// <summary>乱数のシード。同じシードなら同じ入力列になる（0 なら起動時刻から決める）。</summary>
        public int Seed;

        /// <summary>自動プレイ中のタイムスケール（2 にすると倍速でテストできる）。</summary>
        public float TimeScale = 1f;

        /// <summary>終わったらアプリ（エディタなら再生）を終了する。夜間テスト用。</summary>
        public bool QuitOnFinish;

        /// <summary>このタグのオブジェクトをプレイヤーとみなして、位置の記録とスタック検出に使う。</summary>
        public string PlayerTag = "Player";

        /// <summary>この秒数のあいだ StuckDistance 以上動かなければ「スタック」として記録する。</summary>
        public float StuckSeconds = 15f;

        public float StuckDistance = 0.5f;

        /// <summary>1秒間の平均 FPS がこれを下回ったら記録する。</summary>
        public float LowFpsThreshold = 20f;

        /// <summary>撮影するスクリーンショットの上限（ディスクを埋めないため）。</summary>
        public int MaxScreenshots = 30;

        /// <summary>
        /// コマンドライン引数から作る。-autoplay がなければ null。
        /// 例: -autoplay -autoplayDuration 600 -autoplaySeed 123 -autoplayTimeScale 2 -autoplayQuit
        /// </summary>
        public static AutoPlaySettings FromCommandLine(string[] args)
        {
            if (Array.IndexOf(args, "-autoplay") < 0)
            {
                return null;
            }

            var settings = new AutoPlaySettings
            {
                QuitOnFinish = Array.IndexOf(args, "-autoplayQuit") >= 0,
            };
            settings.DurationSeconds = ReadFloat(args, "-autoplayDuration", settings.DurationSeconds);
            settings.Seed = (int)ReadFloat(args, "-autoplaySeed", settings.Seed);
            settings.TimeScale = ReadFloat(args, "-autoplayTimeScale", settings.TimeScale);
            return settings;
        }

        static float ReadFloat(string[] args, string name, float fallback)
        {
            var index = Array.IndexOf(args, name);
            if (index < 0 || index + 1 >= args.Length)
            {
                return fallback;
            }
            return float.TryParse(args[index + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : fallback;
        }
    }
}
