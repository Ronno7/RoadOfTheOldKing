#if UNITY_EDITOR || DEVELOPMENT_BUILD || ROTOK_DEV_TOOLS
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // F2 / F4's "Hide HUD": clean frames for store screenshots and trailer footage.
    // Hides the HUD and world-space panels (prompts, enemy bars), the old TextMesh labels and the cursor.
    // Menus, dialogue and the inscription reader use the pause-menu panel and stay; scene fades stay.
    // Hides each panel's top element, which no game code styles, and re-scans every frame for new panels.
    public sealed class DevHudHider : MonoBehaviour
    {
        private static readonly string[] HiddenPanels = { "HudPanel", "WorldPanel" };

        private readonly HashSet<VisualElement> hiddenTrees = new HashSet<VisualElement>();
        private readonly HashSet<MeshRenderer> hiddenLabels = new HashSet<MeshRenderer>();

        private void LateUpdate()
        {
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

        private void OnDisable()
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
#endif
