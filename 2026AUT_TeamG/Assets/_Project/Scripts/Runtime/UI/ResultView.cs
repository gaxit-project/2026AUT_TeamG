using TeamG.Core;
using TeamG.Gameplay;
using TMPro;
using UnityEngine;

namespace TeamG.UI
{
    /// <summary>結果画面</summary>
    public sealed class ResultView : MonoBehaviour
    {
        [SerializeField] TMP_Text winnerText;
        [SerializeField] TMP_Text scoreText;

        bool isLoading;

        void Start()
        {
            MatchRules rules = MatchSession.Rules;

            // リザルトから直接再生したときは、試合がない
            if (rules == null)
            {
                winnerText.text = "NO MATCH";
                scoreText.text = "";
                return;
            }

            winnerText.text = $"{rules.Winner + 1}P WIN!";

            // 全員の勝ち数を「2 - 0」の形で並べる（人数が増えても使える）
            string score = "";
            for (int i = 0; i < rules.PlayerCount; i++)
            {
                if (i > 0) score += " - ";
                score += rules.GetWins(i);
            }
            scoreText.text = score;
        }

        /// <summary>リトライボタン</summary>
        public void OnRetry()
        {
            if (isLoading) return;
            isLoading = true;

            if (MatchSession.HasMatch)
            {
                MatchSession.Restart();
            }
            else
            {
                MatchSession.Begin(MatchMode.VsTarget);
            }
            SceneLoader.Load(SceneNames.Main);
        }

        /// <summary>タイトルボタン</summary>
        public void OnTitle()
        {
            if (isLoading) return;
            isLoading = true;

            SceneLoader.Load(SceneNames.Title);
        }
    }
}
