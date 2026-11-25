using TeamG.Gameplay;
using TMPro;
using UnityEngine;

namespace TeamG.UI
{
    /// <summary>HUD表示</summary>
    public sealed class RoundHud : MonoBehaviour
    {
        [SerializeField] GameFlow flow;
        [SerializeField] TMP_Text centerText;   // 画面中央の大きな文字
        [SerializeField] TMP_Text scoreText;    // 画面上の勝ち数
        [SerializeField] float goTime = 0.8f;   // GO! を出しておく秒数

        float playingTime;

        void Update()
        {
            MatchRules rules = MatchSession.Rules;
            if (rules == null) return;

            scoreText.text = $"1P  {rules.GetWins(0)} - {rules.GetWins(1)}  2P";

            switch (flow.Phase)
            {
                case GamePhase.Ready:
                    centerText.text = $"ROUND {rules.Round}";
                    break;

                case GamePhase.Countdown:
                    centerText.text = flow.CountdownValue.ToString();
                    break;

                case GamePhase.Playing:
                    playingTime += Time.deltaTime;
                    centerText.text = playingTime < goTime ? "GO!" : "";
                    break;

                case GamePhase.RoundEnd:
                    centerText.text = flow.RoundWinner >= 0
                        ? $"{flow.RoundWinner + 1}P WIN!"
                        : "DRAW";
                    break;
            }
        }
    }
}
