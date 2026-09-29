using UnityEngine.SceneManagement;

namespace TheLostShrine.Progression
{
    internal static class SceneReload
    {
        public static void Active()
        {
            var scene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            // Isolated preview/test scenes can be played without being in the build list.
            if (scene.buildIndex < 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(scene.path,
                    new LoadSceneParameters(LoadSceneMode.Single));
                return;
            }
#endif
            SceneManager.LoadScene(scene.buildIndex);
        }
    }
}
