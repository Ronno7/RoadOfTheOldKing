using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoadOfTheOldKing.Progression
{
    // Loads scenes by asset path. Builds can only load scenes in the build list; the Editor also loads
    // isolated preview/test scenes that are not, so they can be played directly.
    internal static class SceneLoader
    {
        public static string ActivePath => SceneManager.GetActiveScene().path;

        public static bool CanLoad(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (SceneUtility.GetBuildIndexByScenePath(path) >= 0) return true;
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(path) != null;
#else
            return false;
#endif
        }

        public static AsyncOperation LoadAsync(string path)
        {
            int index = SceneUtility.GetBuildIndexByScenePath(path);
            if (index >= 0) return SceneManager.LoadSceneAsync(index);
#if UNITY_EDITOR
            return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            Debug.LogError("Scene is not in the build: " + path);
            return null;
#endif
        }

        // Fallback for screens that work without a session.
        public static void ReloadActive() => LoadAsync(ActivePath);
    }
}
