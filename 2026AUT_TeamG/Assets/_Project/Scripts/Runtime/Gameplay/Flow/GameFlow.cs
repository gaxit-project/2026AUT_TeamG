using System.Collections;
using TeamG.Core;
using UnityEngine;

namespace TeamG.Gameplay
{
    /// <summary>1ラウンドの進行</summary>
    public sealed class GameFlow : MonoBehaviour
    {
        [SerializeField] float readyTime = 1.5f;      // ROUND n を見せる時間
        [SerializeField] int countdownSeconds = 3;
        [SerializeField] float roundEndTime = 2.5f;   // 結果を見せる時間
        [SerializeField] bool showLog = true;         // Consoleに場面を出す

        public GamePhase Phase { get; private set; }
        public int CountdownValue { get; private set; }
        /// -1 = 引き分け
        public int RoundWinner { get; private set; } = -1;

        IEnumerator Start()
        {
            if (!MatchSession.HasMatch)
            {
                MatchSession.Begin(MatchMode.VsTarget);
            }

            // ① 準備
            SetPlaying(false);
            SetPhase(GamePhase.Ready);
            yield return new WaitForSeconds(readyTime);

            // ② カウントダウン
            SetPhase(GamePhase.Countdown);
            for (CountdownValue = countdownSeconds; CountdownValue > 0; CountdownValue--)
            {
                Log(CountdownValue.ToString());
                yield return new WaitForSeconds(1f);
            }

            // ③ 対戦中
            SetPlaying(true);
            SetPhase(GamePhase.Playing);
            yield return new WaitUntil(() => CountSurvivors() <= 1);

            // ④ 判定
            SetPlaying(false);
            RoundWinner = FindWinner();
            MatchSession.Rules.FinishRound(RoundWinner);
            SetPhase(GamePhase.RoundEnd);
            yield return new WaitForSeconds(roundEndTime);

            // ⑤ 次へ
            if (MatchSession.Rules.IsOver)
            {
                SceneLoader.Load(SceneNames.Result);
            }
            else
            {
                SceneLoader.Load(SceneNames.Main);
            }
        }

        /// <summary>まだ負けていない人の数</summary>
        int CountSurvivors()
        {
            int count = 0;
            foreach (PlayerSlot slot in PlayerSlot.All)
            {
                if (!slot.HasLost) count++;
            }
            return count;
        }

        /// <summary>残っている人の番号。全員負けていたら -1（引き分け）</summary>
        int FindWinner()
        {
            foreach (PlayerSlot slot in PlayerSlot.All)
            {
                if (!slot.HasLost) return slot.Index;
            }
            return -1;
        }

        void SetPlaying(bool playing)
        {
            foreach (PlayerSlot slot in PlayerSlot.All)
            {
                slot.SetPlaying(playing);
            }
        }

        void SetPhase(GamePhase phase)
        {
            Phase = phase;
            Log(phase.ToString());
        }

        void Log(string message)
        {
            if (!showLog) return;
            MatchRules rules = MatchSession.Rules;
            Debug.Log($"[GameFlow] R{rules.Round} ({rules.GetWins(0)}-{rules.GetWins(1)}) {message}");
        }
    }
}
