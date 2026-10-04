using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TheLostShrine.UI
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private VisualTreeAsset layout;
        [SerializeField] private PanelSettings panelSettings;
        private UIDocument document;
        private VisualElement overlay;
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
                list = new MenuList(overlay.Q("actions"), overlay.Q<Label>("description"), action => invoke?.Invoke(action));
            }
            invoke = onAction;
            overlay.style.display = DisplayStyle.Flex;
            overlay.Q<Label>("heading").text = title;
            overlay.Q<Label>("hint").text = title == "PAUSED" ? "Esc Resume" : "Esc Back";
            list.Show(commands, selected);
            return true;
        }

        public void Navigate(int direction) { if (IsVisible) list.Navigate(direction); }
        public void Submit() { if (IsVisible) list.Submit(); }
        public void Hide() { if (overlay != null) overlay.style.display = DisplayStyle.None; }
        private void OnDisable() => Hide();
        private void OnDestroy() { if (document != null) Destroy(document.gameObject); }
    }
}
