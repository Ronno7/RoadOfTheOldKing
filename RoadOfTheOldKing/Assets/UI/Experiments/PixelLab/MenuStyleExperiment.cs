#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.Experiments
{
    // Editor-only heading-font preview. Bronze/hearth frames belong exclusively to bonfires.
    public static class MenuStyleExperiment
    {
        private const string FontsKey = "RoadOfTheOldKing.Experiments.MenuFonts";
        private const string Folder = "Assets/UI/Experiments/PixelLab/Styles/";
        private const string FontsMenu = "Road of the Old King/Experiments/Menu Fonts: PixelLab headings";

        public static bool PixelLabFonts
        {
            get => EditorPrefs.GetBool(FontsKey, false);
            set => EditorPrefs.SetBool(FontsKey, value);
        }

        [MenuItem(FontsMenu)] private static void ToggleFonts() => PixelLabFonts = !PixelLabFonts;
        [MenuItem(FontsMenu, true)] private static bool FontsCheck() { Menu.SetChecked(FontsMenu, PixelLabFonts); return true; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var host = new GameObject("Menu Style Experiment") { hideFlags = HideFlags.HideAndDontSave };
            Object.DontDestroyOnLoad(host);
            host.AddComponent<Applier>();
        }

        private sealed class Applier : MonoBehaviour
        {
            private float nextCheck;
            private StyleSheet fonts;

            private void Update()
            {
                if (Time.unscaledTime < nextCheck) return;
                nextCheck = Time.unscaledTime + .2f;
                if (fonts == null) fonts = AssetDatabase.LoadAssetAtPath<StyleSheet>(Folder + "Fonts.uss");
                foreach (var document in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
                {
                    var root = document.rootVisualElement;
                    if (root == null || root.Q(className: "menu-panel") == null) continue;
                    Toggle(root, fonts, PixelLabFonts);

                }
            }

            private static void Toggle(VisualElement root, StyleSheet sheet, bool on)
            {
                if (sheet == null) return;
                bool has = root.styleSheets.Contains(sheet);
                if (on && !has) root.styleSheets.Add(sheet);
                if (!on && has) root.styleSheets.Remove(sheet);
            }
        }
    }
}
#endif
