using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TeamG.DebugTools
{
    /// <summary>Build Profiles に登録されたシーンへジャンプする項目。</summary>
    internal static class SceneDebugItems
    {
        const string Category = "シーン";

        [DebugMenuItem(Category + "/現在のシーン", Order = -100)]
        static string Current => SceneManager.GetActiveScene().name;

        [DebugMenuItem(Category + "/現在のシーンを読み直す", Order = -99)]
        static void Reload()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void RegisterBuildScenes()
        {
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var buildIndex = i;
                var name = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                DebugMenuRegistry.AddButton($"{Category}/→ {name}", () => SceneManager.LoadScene(buildIndex), i);
            }
        }
    }
}
