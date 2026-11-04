using System;
using System.Collections.Generic;
using RoadOfTheOldKing.Input;
using UnityEngine;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset layout;
        [SerializeField] private PanelSettings panelSettings;
        private UIDocument document;
        private VisualElement overlay, panel, controls;
        private MenuList list;
        private Action<PauseMenuAction> invoke;
        public bool IsVisible => overlay != null && overlay.style.display.value != DisplayStyle.None;

        public bool Show(IReadOnlyList<PauseMenuAction> commands, Action<PauseMenuAction> onAction, int selected = 0, string title = "PAUSED")
        {
            if (commands == null || commands.Count == 0) return false;
            if (layout == null || panelSettings == null) { Debug.LogError("Pause menu needs its layout and panel settings.", this); return false; }
            if (document == null)
            {
                var child = new GameObject("Pause menu UI"); child.SetActive(false); child.transform.SetParent(transform, false);
                document = child.AddComponent<UIDocument>(); document.panelSettings = panelSettings;
                document.visualTreeAsset = layout; child.SetActive(true);
                overlay = document.rootVisualElement.Q("pause-overlay");
                panel = overlay.Q("pause-panel"); controls = overlay.Q("controls");
                list = new MenuList(overlay.Q("actions"), overlay.Q<Label>("description"), action => invoke?.Invoke(action));
            }
            invoke = onAction;
            overlay.style.display = DisplayStyle.Flex;
            panel.RemoveFromClassList("controls-open");
            overlay.Q<Label>("heading").text = title;
            bool paused = title == "PAUSED";
            ControlLabels.Bind(overlay.Q<Label>("hint"), paused ? "Esc Resume" : "Esc Back", paused ? "B Resume" : "B Back");
            list.Show(commands, selected);
            return true;
        }

        // The list stays below the table, so Back works the same as on any other page.
        public bool ShowControls(IReadOnlyList<PauseMenuAction> commands, Action<PauseMenuAction> onAction, bool canRecall)
        {
            if (!Show(commands, onAction, 0, "CONTROLS")) return false;
            ControlsPage.Fill(controls, canRecall);
            panel.AddToClassList("controls-open");
            HighlightDevice();
            return true;
        }

        // The column for the device in use shows gold, and follows a switch while the page is open.
        private void HighlightDevice()
        {
            if (controls == null) return;
            controls.EnableInClassList("using-gamepad", ActiveInput.UsingGamepad);
            controls.EnableInClassList("using-keyboard", !ActiveInput.UsingGamepad);
        }

        private void OnEnable() => ActiveInput.Changed += HighlightDevice;

        public void Navigate(int direction) { if (IsVisible) list.Navigate(direction); }
        public void Submit() { if (IsVisible) list.Submit(); }
        public void Hide() { if (overlay != null) overlay.style.display = DisplayStyle.None; }
        private void OnDisable() { ActiveInput.Changed -= HighlightDevice; Hide(); }
        private void OnDestroy() { if (document != null) Destroy(document.gameObject); }
    }
}
