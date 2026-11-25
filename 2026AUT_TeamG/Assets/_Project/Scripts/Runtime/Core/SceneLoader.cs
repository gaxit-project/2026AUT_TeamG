using UnityEngine;
using UnityEngine.SceneManagement;

namespace TeamG.Core
{
    /// <summary>シーン切り替えの窓口</summary>
    public static class SceneLoader
    {
        public static void Load(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
