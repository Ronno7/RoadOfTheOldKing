using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // F2 "Hide HUD": clean frames for screenshots and footage, in every build (user, 23 Nov: "provide the F2 and F3
    // functions for all builds going forward, then only internal playtesters get access to F4"). F4's Hide HUD toggle uses
    // the same switch. Hides the HUD and world-space panels (prompts, enemy bars), the old TextMesh labels and the cursor.
    // Menus, dialogue and the inscription reader use the pause-menu panel and stay; scene fades stay.
    // Hides each panel's top element, which no game code styles, and re-scans every frame for new panels.
    public sealed class HudHider : MonoBehaviour
    {
        private static readonly string[] HiddenPanels = { "HudPanel", "WorldPanel" };
        private static HudHider instance;

        private readonly HashSet<VisualElement> hiddenTrees = new HashSet<VisualElement>();
        private readonly HashSet<MeshRenderer> hiddenLabels = new HashSet<MeshRenderer>();
        private bool hidden;

        public static bool Hidden => instance != null && instance.hidden;

        public static void SetHidden(bool value)
        {
            Install();
            instance.Apply(value);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (instance != null) return;
            var host = new GameObject("HUD Hider (F2)");
            DontDestroyOnLoad(host);
            instance = host.AddComponent<HudHider>();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (Application.isFocused && keyboard != null && keyboard.f2Key.wasPressedThisFrame) Apply(!hidden);
        }

        private void LateUpdate()
        {
            if (!hidden) return;
            foreach (var document in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                var tree = document.rootVisualElement?.panel?.visualTree;
                if (tree == null || hiddenTrees.Contains(tree) || !IsHidden(document.panelSettings)) continue;
                tree.style.display = DisplayStyle.None;
                hiddenTrees.Add(tree);
            }
            foreach (var label in FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            {
                if (!label.TryGetComponent<MeshRenderer>(out var renderer) || !renderer.enabled) continue;
                renderer.enabled = false;
                hiddenLabels.Add(renderer);
            }
            // Menus still need a pointer; gameplay frames don't.
            UnityEngine.Cursor.visible = MenuStack.IsAnyOpen && !RoadOfTheOldKing.Input.ActiveInput.UsingGamepad;
        }

        private void Apply(bool value)
        {
            if (value == hidden) return;
            hidden = value;
            if (!value) Restore();
        }

        private void OnDisable() { if (hidden) Restore(); }

        private void Restore()
        {
            foreach (var tree in hiddenTrees) tree.style.display = StyleKeyword.Null;
            foreach (var renderer in hiddenLabels) if (renderer != null) renderer.enabled = true;
            hiddenTrees.Clear(); hiddenLabels.Clear();
            UnityEngine.Cursor.visible = !RoadOfTheOldKing.Input.ActiveInput.UsingGamepad;
        }

        private static bool IsHidden(PanelSettings settings)
        {
            if (settings == null) return false;
            foreach (string name in HiddenPanels) if (settings.name == name) return true;
            return false;
        }
    }
}
