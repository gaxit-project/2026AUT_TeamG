using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TeamG.DebugTools
{
    /// <summary>
    /// ゲーム内デバッグメニュー。シーンに置かなくても起動時に自動で生成される。
    /// 開閉: F1 キー / ゲームパッドの L3 を押しながら R3
    /// </summary>
    public sealed class DebugMenu : MonoBehaviour
    {
        // この高さを基準に GUI を拡大縮小する（解像度が変わっても同じ見た目になる）
        const float ReferenceHeight = 720f;
        const int CategoryColumns = 4;
        const int WindowId = 0x0DEB;

        static DebugMenu instance;

        readonly List<DebugItem> visibleItems = new List<DebugItem>();
        Rect windowRect = new Rect(16f, 16f, 440f, 520f);
        Vector2 scroll;
        string selectedCategory;
        bool isOpen;

        /// <summary>メニューが開いているか（開いている間はゲーム側の入力を止めたいときに使う）。</summary>
        public static bool IsOpen => instance != null && instance.isOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (instance != null)
            {
                return;
            }

            var go = new GameObject("[DebugMenu]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<DebugMenu>();
            go.AddComponent<FpsOverlay>();
        }

        void Update()
        {
            if (WasTogglePressed())
            {
                isOpen = !isOpen;
            }
        }

        static bool WasTogglePressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
            {
                return true;
            }

            var gamepad = Gamepad.current;
            return gamepad != null && gamepad.leftStickButton.isPressed && gamepad.rightStickButton.wasPressedThisFrame;
        }

        void OnGUI()
        {
            if (!isOpen)
            {
                return;
            }

            var scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            windowRect = GUILayout.Window(WindowId,windowRect, DrawWindow, "Debug Menu  (F1 で閉じる)");
        }

        void DrawWindow(int id)
        {
            var categories = DebugMenuRegistry.Categories;
            if (categories.Count == 0)
            {
                GUILayout.Label("項目がありません");
                GUI.DragWindow();
                return;
            }

            var index = Mathf.Max(0, IndexOf(categories, selectedCategory));
            var names = new string[categories.Count];
            for (var i = 0; i < names.Length; i++)
            {
                names[i] = categories[i];
            }
            index = GUILayout.SelectionGrid(index, names, CategoryColumns);
            selectedCategory = categories[index];

            GUILayout.Space(8f);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            DebugMenuRegistry.GetItems(selectedCategory, visibleItems);
            foreach (var item in visibleItems)
            {
                item.Draw();
            }
            GUILayout.EndScrollView();

            GUI.DragWindow();
        }

        static int IndexOf(IReadOnlyList<string> list, string value)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == value)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
