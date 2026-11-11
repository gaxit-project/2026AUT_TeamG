namespace TeamG.DebugTools.AutoPlay
{
    /// <summary>デバッグメニューの「自動プレイ」カテゴリ。</summary>
    internal static class AutoPlayDebugItems
    {
        const string Category = "自動プレイ";

        [DebugMenuItem(Category + "/状態", Order = -100)]
        static string Status => AutoPlayer.Status;

        [DebugMenuItem(Category + "/ランダム入力で開始（5分）")]
        static void StartFiveMinutes()
        {
            AutoPlayer.Begin(new AutoPlaySettings { DurationSeconds = 300f });
        }

        [DebugMenuItem(Category + "/ランダム入力で開始（止めるまで）")]
        static void StartUnlimited()
        {
            AutoPlayer.Begin(new AutoPlaySettings { DurationSeconds = 0f });
        }

        [DebugMenuItem(Category + "/停止してレポートを書き出す")]
        static void Stop()
        {
            AutoPlayer.End();
        }

        [DebugMenuItem(Category + "/最後のレポート", Order = 100)]
        static string LastReport => AutoPlayer.LastReportDirectory ?? "なし";
    }
}
