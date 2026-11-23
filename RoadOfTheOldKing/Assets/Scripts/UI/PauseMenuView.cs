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
        private VisualElement overlay, panel, controls, actions, options;
        private MenuList list;
        private OptionList optionList;
        private bool showingOptions;
        private Action<PauseMenuAction> invoke;
        public bool IsVisible => overlay != null && overlay.style.display.value != DisplayStyle.None;

        public bool Show(IReadOnlyList<PauseMenuAction> commands, Action<PauseMenuAction> onAction, int selected = 0, string title = "PAUSED")
        {
            if (commands == null || commands.Count == 0 || !Open(title, false)) return false;
            invoke = onAction;
            bool paused = title == "PAUSED";
            ControlGlyphs.Bind(overlay.Q<Label>("hint"), "");
            list.Show(commands, selected);
            return true;
        }

        // A page of option rows (settings): label and control side by side in a wider panel.
        public bool ShowOptions(IReadOnlyList<OptionList.Row> rows, string title, int selected = 0)
        {
            if (rows == null || rows.Count == 0 || !Open(title, true)) return false;
            optionList.Show(rows, selected);
            ControlGlyphs.Bind(overlay.Q<Label>("hint"), ""); // Rows show their own ‹ › arrows.
            return true;
        }

        private bool Open(string title, bool withOptions)
        {
            if (layout == null || panelSettings == null) { Debug.LogError("Pause menu needs its layout and panel settings.", this); return false; }
            if (document == null)
            {
                var child = new GameObject("Pause menu UI"); child.SetActive(false); child.transform.SetParent(transform, false);
                document = child.AddComponent<UIDocument>(); document.panelSettings = panelSettings;
                document.visualTreeAsset = layout; child.SetActive(true);
                overlay = document.rootVisualElement.Q("pause-overlay");
                panel = overlay.Q("pause-panel"); controls = overlay.Q("controls");
                actions = overlay.Q("actions"); options = overlay.Q("options");
                list = new MenuList(actions, overlay.Q<Label>("description"), action => invoke?.Invoke(action));
                optionList = new OptionList(options, overlay.Q<Label>("description"));
            }
            overlay.style.display = DisplayStyle.Flex;
            panel.RemoveFromClassList("controls-open");
            ShowOptionArea(withOptions);
            overlay.Q<Label>("heading").text = title;
            return true;
        }

        public OptionList Options => optionList;

        private void ShowOptionArea(bool show)
        {
            showingOptions = show;
            panel.EnableInClassList("options-open", show);
            actions.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
            options.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
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

        public void Navigate(int direction) { if (IsVisible) { if (showingOptions) optionList.Navigate(direction); else list.Navigate(direction); } }
        public void Submit() { if (IsVisible) { if (showingOptions) optionList.Submit(); else list.Submit(); } }
        public void Adjust(int direction) { if (IsVisible && showingOptions) optionList.Adjust(direction); }
        public void Hide() { if (overlay != null) overlay.style.display = DisplayStyle.None; }
        private void OnDisable() { ActiveInput.Changed -= HighlightDevice; Hide(); }
        private void OnDestroy() { if (document != null) Destroy(document.gameObject); }
    }
}
