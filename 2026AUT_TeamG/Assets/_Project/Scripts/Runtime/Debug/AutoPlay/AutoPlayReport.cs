using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TeamG.DebugTools.AutoPlay
{
    public enum AutoPlayIssueKind
    {
        Exception,
        Error,
        Stuck,
        LowFps,
    }

    /// <summary>自動プレイ中に見つかった問題。同じ内容は1件にまとめて回数を数える。</summary>
    [Serializable]
    public sealed class AutoPlayIssue
    {
        public AutoPlayIssueKind Kind;
        public string KindName;
        public string Message;
        public string StackTrace;
        public int Count;
        public float FirstSeconds;
        public string Scene;
        public string PlayerPosition;
        public string Screenshot;
    }

    /// <summary>自動プレイ1回分の結果。report.md（人が読む）と report.json（ツールが読む）に書き出す。</summary>
    [Serializable]
    public sealed class AutoPlayReport
    {
        public string StartedAt;
        public string Behaviour;
        public int Seed;
        public float TimeScale;
        public float ElapsedSeconds;
        public string UnityVersion;
        public string AppVersion;
        public float AverageFps;
        public float MinFps;
        public List<AutoPlayIssue> Issues = new List<AutoPlayIssue>();

        public bool HasProblems => Issues.Count > 0;

        public void Write(string directory)
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "report.json"), JsonUtility.ToJson(this, true), Encoding.UTF8);
            File.WriteAllText(Path.Combine(directory, "report.md"), ToMarkdown(), Encoding.UTF8);
        }

        string ToMarkdown()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# 自動プレイ レポート");
            sb.AppendLine();
            sb.AppendLine("| 項目 | 値 |");
            sb.AppendLine("|---|---|");
            sb.AppendLine($"| 開始 | {StartedAt} |");
            sb.AppendLine($"| ボット | {Behaviour}（シード {Seed}） |");
            sb.AppendLine($"| 実行時間 | {ElapsedSeconds:0} 秒（タイムスケール {TimeScale}） |");
            sb.AppendLine($"| FPS | 平均 {AverageFps:0.0} / 最低 {MinFps:0.0} |");
            sb.AppendLine($"| ビルド | {AppVersion}（Unity {UnityVersion}） |");
            sb.AppendLine($"| 結果 | {(HasProblems ? $"**問題 {Issues.Count} 種類**" : "問題なし")} |");

            foreach (var group in Issues.GroupBy(i => i.Kind))
            {
                sb.AppendLine();
                sb.AppendLine($"## {Title(group.Key)}");
                foreach (var issue in group.OrderBy(i => i.FirstSeconds))
                {
                    sb.AppendLine();
                    sb.AppendLine($"### {FirstLine(issue.Message)}");
                    sb.AppendLine();
                    sb.AppendLine($"- 回数: {issue.Count}（初回 {issue.FirstSeconds:0.0} 秒）");
                    sb.AppendLine($"- シーン: {issue.Scene}");
                    if (!string.IsNullOrEmpty(issue.PlayerPosition))
                    {
                        sb.AppendLine($"- プレイヤー位置: {issue.PlayerPosition}");
                    }
                    if (!string.IsNullOrEmpty(issue.Screenshot))
                    {
                        sb.AppendLine($"- スクリーンショット: ![]({issue.Screenshot})");
                    }
                    if (!string.IsNullOrEmpty(issue.StackTrace))
                    {
                        sb.AppendLine();
                        sb.AppendLine("```");
                        sb.AppendLine(issue.StackTrace.TrimEnd());
                        sb.AppendLine("```");
                    }
                }
            }

            return sb.ToString();
        }

        static string Title(AutoPlayIssueKind kind) => kind switch
        {
            AutoPlayIssueKind.Exception => "例外",
            AutoPlayIssueKind.Error => "エラーログ",
            AutoPlayIssueKind.Stuck => "スタック（プレイヤーが動かない）",
            AutoPlayIssueKind.LowFps => "FPS 低下",
            _ => kind.ToString(),
        };

        internal static string FirstLine(string text)
        {
            var newline = text.IndexOf('\n');
            return (newline < 0 ? text : text.Substring(0, newline)).Trim();
        }
    }
}
