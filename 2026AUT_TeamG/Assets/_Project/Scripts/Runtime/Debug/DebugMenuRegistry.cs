using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace TeamG.DebugTools
{
    /// <summary>
    /// デバッグメニューの項目を管理する。
    /// 固定の項目は [DebugMenuItem] 属性で、シーン上のオブジェクトに紐づく項目は Add〇〇 で登録する。
    /// </summary>
    public static class DebugMenuRegistry
    {
        const string DefaultCategory = "その他";

        static readonly List<DebugItem> items = new List<DebugItem>();
        static readonly List<string> categories = new List<string>();

        /// <summary>登録順のカテゴリ一覧。</summary>
        public static IReadOnlyList<string> Categories => categories;

        public static DebugItem AddButton(string path, Action onClick, int order = 0)
        {
            return Add(path, new DebugButton(onClick), order);
        }

        public static DebugItem AddToggle(string path, Func<bool> getter, Action<bool> setter, int order = 0)
        {
            return Add(path, new DebugToggle(getter, setter), order);
        }

        public static DebugItem AddSlider(string path, Func<float> getter, Action<float> setter, float min, float max, int order = 0)
        {
            return Add(path, new DebugSlider(getter, setter, min, max, false), order);
        }

        public static DebugItem AddIntSlider(string path, Func<int> getter, Action<int> setter, int min, int max, int order = 0)
        {
            return Add(path, new DebugSlider(() => getter(), v => setter((int)v), min, max, true), order);
        }

        public static DebugItem AddLabel(string path, Func<string> getter, int order = 0)
        {
            return Add(path, new DebugLabel(getter), order);
        }

        public static void Remove(DebugItem item)
        {
            if (!items.Remove(item))
            {
                return;
            }

            if (items.All(i => i.Category != item.Category))
            {
                categories.Remove(item.Category);
            }
        }

        /// <summary>カテゴリ内の項目を並び順で results に入れる（描画中に登録・削除されても壊れないようにコピーを返す）。</summary>
        public static void GetItems(string category, List<DebugItem> results)
        {
            results.Clear();
            results.AddRange(items.Where(i => i.Category == category).OrderBy(i => i.Order));
        }

        static DebugItem Add(string path, DebugItem item, int order)
        {
            var slash = path.IndexOf('/');
            item.Category = slash < 0 ? DefaultCategory : path.Substring(0, slash);
            item.Label = slash < 0 ? path : path.Substring(slash + 1);
            item.Order = order;

            items.Add(item);
            if (!categories.Contains(item.Category))
            {
                categories.Add(item.Category);
            }
            return item;
        }

        // Enter Play Mode でドメインリロードを切っていても static が残らないように、毎回ここで作り直す
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Initialize()
        {
            items.Clear();
            categories.Clear();
            RegisterAttributes();
        }

        static void RegisterAttributes()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (var type in typeof(DebugMenuRegistry).Assembly.GetTypes())
            {
                foreach (var member in type.GetMembers(flags))
                {
                    var attr = member.GetCustomAttribute<DebugMenuItemAttribute>();
                    if (attr == null)
                    {
                        continue;
                    }

                    switch (member)
                    {
                        case MethodInfo method:
                            RegisterMethod(method, attr);
                            break;
                        case PropertyInfo property:
                            RegisterProperty(property, attr);
                            break;
                    }
                }
            }
        }

        static void RegisterMethod(MethodInfo method, DebugMenuItemAttribute attr)
        {
            if (method.GetParameters().Length > 0)
            {
                UnityEngine.Debug.LogWarning($"[DebugMenu] 引数のあるメソッドは登録できません: {method.DeclaringType}.{method.Name}");
                return;
            }

            var action = (Action)Delegate.CreateDelegate(typeof(Action), method);
            AddButton(attr.Path, action, attr.Order);
        }

        static void RegisterProperty(PropertyInfo property, DebugMenuItemAttribute attr)
        {
            var get = property.GetGetMethod(true);
            var set = property.GetSetMethod(true);
            if (get == null)
            {
                UnityEngine.Debug.LogWarning($"[DebugMenu] getter のないプロパティは登録できません: {property.DeclaringType}.{property.Name}");
                return;
            }

            var type = property.PropertyType;
            if (set != null && type == typeof(bool))
            {
                var getter = (Func<bool>)Delegate.CreateDelegate(typeof(Func<bool>), get);
                var setter = (Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>), set);
                AddToggle(attr.Path, getter, setter, attr.Order);
            }
            else if (set != null && type == typeof(float))
            {
                var getter = (Func<float>)Delegate.CreateDelegate(typeof(Func<float>), get);
                var setter = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), set);
                AddSlider(attr.Path, getter, setter, attr.Min, attr.Max, attr.Order);
            }
            else if (set != null && type == typeof(int))
            {
                var getter = (Func<int>)Delegate.CreateDelegate(typeof(Func<int>), get);
                var setter = (Action<int>)Delegate.CreateDelegate(typeof(Action<int>), set);
                AddIntSlider(attr.Path, getter, setter, (int)attr.Min, (int)attr.Max, attr.Order);
            }
            else
            {
                AddLabel(attr.Path, () => get.Invoke(null, null)?.ToString() ?? "null", attr.Order);
            }
        }
    }
}
