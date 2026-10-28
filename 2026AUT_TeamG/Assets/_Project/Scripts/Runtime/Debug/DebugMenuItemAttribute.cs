using System;
using UnityEngine.Scripting;

namespace TeamG.DebugTools
{
    /// <summary>
    /// static メソッド・プロパティをデバッグメニューに表示する。
    /// メソッド → ボタン / bool → トグル / float・int → スライダー / それ以外（getのみ）→ 値の表示
    /// </summary>
    /// <example>
    /// [DebugMenuItem("システム/タイムスケール", Min = 0f, Max = 4f)]
    /// static float TimeScale { get => Time.timeScale; set => Time.timeScale = value; }
    /// </example>
    // リフレクションでしか呼ばれないため、IL2CPP のコード削減で消されないよう Preserve を継承する
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class DebugMenuItemAttribute : PreserveAttribute
    {
        /// <summary>"カテゴリ/項目名" の形式。スラッシュがなければ「その他」に入る。</summary>
        public string Path { get; }

        /// <summary>スライダーの最小値。</summary>
        public float Min { get; set; }

        /// <summary>スライダーの最大値。</summary>
        public float Max { get; set; } = 1f;

        /// <summary>カテゴリ内の並び順（小さいほど上）。</summary>
        public int Order { get; set; }

        public DebugMenuItemAttribute(string path)
        {
            Path = path;
        }
    }
}
