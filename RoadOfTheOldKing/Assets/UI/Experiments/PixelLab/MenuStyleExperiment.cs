#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.Experiments
{
    // EXPERIMENT (Editor only, never in builds): tries PixelLab menu experiments on the game's menus in Play Mode.
    // Road of the Old King > Experiments: Menu Style (Off / Bronze, which gives the pause panel the
    // adopted bronze-frame classes and ornaments from OrnateMenu.uss) and Menu Fonts (PixelLab heading fonts).
    // Choices are EditorPrefs; both off leaves the game untouched.
    public static class MenuStyleExperiment
    {
        public enum Style { Off, Bronze }
        private const string StyleKey = "RoadOfTheOldKing.Experiments.MenuStyle";
        private const string FontsKey = "RoadOfTheOldKing.Experiments.MenuFonts";
        private const string Folder = "Assets/UI/Experiments/PixelLab/Styles/";
        private const string StyleMenu = "Road of the Old King/Experiments/Menu Style/";
        private const string FontsMenu = "Road of the Old King/Experiments/Menu Fonts: PixelLab headings";

        public static Style Current
        {
            get => (Style)Mathf.Clamp(EditorPrefs.GetInt(StyleKey, 0), 0, 1);
            set => EditorPrefs.SetInt(StyleKey, (int)value);
        }

        public static bool PixelLabFonts
        {
            get => EditorPrefs.GetBool(FontsKey, false);
            set => EditorPrefs.SetBool(FontsKey, value);
        }

        [MenuItem(StyleMenu + "Off")] private static void Off() => Current = Style.Off;
        [MenuItem(StyleMenu + "Bronze")] private static void Bronze() => Current = Style.Bronze;
        [MenuItem(StyleMenu + "Off", true)] private static bool OffCheck() { Menu.SetChecked(StyleMenu + "Off", Current == Style.Off); return true; }
        [MenuItem(StyleMenu + "Bronze", true)] private static bool BronzeCheck() { Menu.SetChecked(StyleMenu + "Bronze", Current == Style.Bronze); return true; }
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
            private StyleSheet ornate, fonts;

            private void Update()
            {
                if (Time.unscaledTime < nextCheck) return;
                nextCheck = Time.unscaledTime + .2f;
                if (ornate == null) ornate = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/UI/OrnateMenu.uss");
                if (fonts == null) fonts = AssetDatabase.LoadAssetAtPath<StyleSheet>(Folder + "Fonts.uss");
                foreach (var document in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
                {
                    var root = document.rootVisualElement;
                    if (root == null || root.Q(className: "menu-panel") == null) continue;
                    Toggle(root, fonts, PixelLabFonts);
                    bool bronze = Current == Style.Bronze;
                    foreach (var name in new[] { "pause-panel" })
                    {
                        var panel = root.Q(name);
                        if (panel == null) continue;
                        if (bronze) Toggle(root, ornate, true);
                        panel.EnableInClassList("ornate-panel", bronze);
                        panel.EnableInClassList("bronze-frame", bronze);
                        Ensure(panel, "menu-crest", 0, bronze);
                        panel.Query(className: "menu-divider").ForEach(divider => Ensure(divider, "menu-medallion", -1, bronze));
                    }
                }
            }

            // Adds (or removes) an injected ornament marked with "exp-injected".
            private static void Ensure(VisualElement parent, string className, int index, bool on)
            {
                VisualElement existing = null;
                foreach (var child in parent.Children())
                    if (child.ClassListContains(className)) { existing = child; break; }
                if (!on)
                {
                    if (existing != null && existing.ClassListContains("exp-injected")) existing.RemoveFromHierarchy();
                    return;
                }
                if (existing != null) return;
                var element = new VisualElement { pickingMode = PickingMode.Ignore };
                element.AddToClassList(className);
                element.AddToClassList("exp-injected");
                if (index >= 0) parent.Insert(index, element);
                else parent.Add(element);
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
