using System;
using UnityEngine;

namespace TeamG.DebugTools
{
    /// <summary>デバッグメニューの1項目。Dispose するとメニューから消える。</summary>
    public abstract class DebugItem : IDisposable
    {
        public string Category { get; internal set; }
        public string Label { get; internal set; }
        public int Order { get; internal set; }

        public abstract void Draw();

        public void Dispose()
        {
            DebugMenuRegistry.Remove(this);
        }
    }

    internal sealed class DebugButton : DebugItem
    {
        readonly Action onClick;

        public DebugButton(Action onClick)
        {
            this.onClick = onClick;
        }

        public override void Draw()
        {
            if (GUILayout.Button(Label))
            {
                onClick();
            }
        }
    }

    internal sealed class DebugToggle : DebugItem
    {
        readonly Func<bool> getter;
        readonly Action<bool> setter;

        public DebugToggle(Func<bool> getter, Action<bool> setter)
        {
            this.getter = getter;
            this.setter = setter;
        }

        public override void Draw()
        {
            var current = getter();
            var next = GUILayout.Toggle(current, Label);
            if (next != current)
            {
                setter(next);
            }
        }
    }

    internal sealed class DebugSlider : DebugItem
    {
        readonly Func<float> getter;
        readonly Action<float> setter;
        readonly float min;
        readonly float max;
        readonly bool wholeNumbers;

        public DebugSlider(Func<float> getter, Action<float> setter, float min, float max, bool wholeNumbers)
        {
            this.getter = getter;
            this.setter = setter;
            this.min = min;
            this.max = max;
            this.wholeNumbers = wholeNumbers;
        }

        public override void Draw()
        {
            var current = getter();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Label, GUILayout.Width(160f));
            var next = GUILayout.HorizontalSlider(current, min, max);
            if (wholeNumbers)
            {
                next = Mathf.Round(next);
            }
            GUILayout.Label(wholeNumbers ? next.ToString("0") : next.ToString("0.00"), GUILayout.Width(48f));
            GUILayout.EndHorizontal();

            if (!Mathf.Approximately(next, current))
            {
                setter(next);
            }
        }
    }

    internal sealed class DebugLabel : DebugItem
    {
        readonly Func<string> getter;

        public DebugLabel(Func<string> getter)
        {
            this.getter = getter;
        }

        public override void Draw()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Label, GUILayout.Width(160f));
            GUILayout.Label(getter());
            GUILayout.EndHorizontal();
        }
    }
}
