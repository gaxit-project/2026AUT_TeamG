using TeamG.Core;
using TeamG.Gameplay;
using UnityEngine;

namespace TeamG.UI
{
    /// <summary>タイトル画面のボタン</summary>
    public sealed class TitleMenu : MonoBehaviour
    {
        bool isLoading;   // 2回押されても、1回しか移らないように

        public void OnStart()
        {
            StartMatch(MatchMode.VsTarget);
        }

        // 後でボタンを足すときの例：
        // public void OnLocalVersus() => StartMatch(MatchMode.LocalVersus);
        // public void OnOnline()      => StartMatch(MatchMode.Online);

        void StartMatch(MatchMode mode)
        {
            if (isLoading) return;
            isLoading = true;

            MatchSession.Begin(mode);
            SceneLoader.Load(SceneNames.Main);
        }

        public void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
