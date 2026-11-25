namespace TeamG.Gameplay
{
    /// <summary>試合のルールと記録(データだけ)</summary>
    public sealed class MatchRules
    {
        readonly int[] wins;   // プレイヤーごとの勝ち数

        public int PlayerCount { get; }
        public int RoundsToWin { get; }
        public int Round { get; private set; } = 1;

        public MatchRules(int playerCount, int roundsToWin)
        {
            PlayerCount = playerCount;
            RoundsToWin = roundsToWin;
            wins = new int[playerCount];
        }

        public int GetWins(int player) => wins[player];

        /// <summary>先取した人の番号。まだ決着していなければ -1</summary>
        public int Winner
        {
            get
            {
                for (int i = 0; i < wins.Length; i++)
                {
                    if (wins[i] >= RoundsToWin) return i;
                }
                return -1;
            }
        }

        public bool IsOver => Winner >= 0;

        /// <summary>ラウンドの勝者を記録する。-1 = 引き分け</summary>
        public void FinishRound(int winner)
        {
            if (winner >= 0) wins[winner]++;
            Round++;
        }
    }
}