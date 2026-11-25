using UnityEngine;

namespace TeamG.Gameplay
{
    /// <summary>遊び方の種類</summary>
    public enum MatchMode
    {
        VsTarget,      // 今：CPU
        LocalVersus,   // 後で：ローカル対戦
        Online,        // 後で：オンライン対戦
    }

    /// <summary>今行われている試合。タイトルで始めて、結果画面まで残る。</summary>
    public static class MatchSession
    {
        public static MatchMode Mode { get; private set; }
        public static MatchRules Rules { get; private set; }

        /// <summary>試合が始まっているか</summary>
        public static bool HasMatch => Rules != null;

        /// <summary>新しい試合を始める</summary>
        public static void Begin(MatchMode mode, int playerCount = 2, int roundsToWin = 2)
        {
            Mode = mode;
            Rules = new MatchRules(playerCount, roundsToWin);
        }

        /// <summary>同じ設定でもう一度（リザルトのリトライ用）</summary>
        public static void Restart()
        {
            Begin(Mode, Rules.PlayerCount, Rules.RoundsToWin);
        }

        // 再生ボタンを押すたびに、前回の試合を消す
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Clear()
        {
            Rules = null;
        }
    }
}