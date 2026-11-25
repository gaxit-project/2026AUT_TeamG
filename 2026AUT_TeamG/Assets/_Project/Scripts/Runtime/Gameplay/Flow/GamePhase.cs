namespace TeamG.Gameplay
{
    /// <summary>ラウンドの中の場面</summary>
    public enum GamePhase
    {
        Ready,      // 準備
        Countdown,  // 3, 2, 1
        Playing,    // 対戦中
        RoundEnd,   // 判定
    }
}
