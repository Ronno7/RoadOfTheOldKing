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
        private VisualElement overlay, buttons;
        private Label description;
        private readonly List<Button> controls = new List<Button>();
        private IReadOnlyList<PauseMenuAction> actions;
        private Action<PauseMenuAction> invoke;
        private int selected;
        public bool IsVisible => overlay != null && overlay.style.display.value != DisplayStyle.None;

        public bool Show(IReadOnlyList<PauseMenuAction> commands, Action<PauseMenuAction> onAction)
        {
            if (commands == null || commands.Count == 0) return false;
            if (layout == null || panelSettings == null) { Debug.LogError("Pause menu needs its layout and panel settings.", this); return false; }
            if (document == null)
            {
                var child = new GameObject("Pause menu UI"); child.SetActive(false); child.transform.SetParent(transform, false);
                document = child.AddComponent<UIDocument>(); document.panelSettings = panelSettings;
                document.visualTreeAsset = layout; child.SetActive(true);
                overlay = document.rootVisualElement.Q("pause-overlay");
                buttons = overlay.Q("actions"); description = overlay.Q<Label>("description");
            }
            actions = commands; invoke = onAction; controls.Clear(); buttons.Clear(); selected = 0;
            for (int i = 0; i < commands.Count; i++)
            {
                int index = i;
                var button = new Button(() => invoke(actions[index])) { text = commands[i].Label, tooltip = commands[i].Description };
                button.AddToClassList("menu-button"); button.SetEnabled(commands[i].Enabled);
                button.RegisterCallback<FocusInEvent>(_ => Select(index));
                button.RegisterCallback<PointerEnterEvent>(_ => Select(index));
                // Navigation is owned by PauseMenuInput, avoiding duplicate submit/move events.
                button.RegisterCallback<NavigationSubmitEvent>(e => e.StopPropagation());
                button.RegisterCallback<NavigationMoveEvent>(e => e.StopPropagation());
                controls.Add(button); buttons.Add(button);
            }
            overlay.style.display = DisplayStyle.Flex;
            Select(0); overlay.schedule.Execute(() => { if (IsVisible && controls.Count > 0) controls[selected].Focus(); });
            return true;
        }

        private void Select(int index)
        {
            selected = index;
            for (int i = 0; i < controls.Count; i++) controls[i].EnableInClassList("selected", i == selected);
            description.text = actions[selected].Description;
        }
        public void Navigate(int direction)
        {
            if (!IsVisible || controls.Count == 0) return;
            Select((selected + direction + controls.Count) % controls.Count);
            controls[selected].Focus();
        }
        public void Submit() { if (IsVisible && actions.Count > 0) invoke(actions[selected]); }
        public void Hide() { if (overlay != null) overlay.style.display = DisplayStyle.None; }
        private void OnDisable() => Hide();
        private void OnDestroy() { if (document != null) Destroy(document.gameObject); }
    }
}
