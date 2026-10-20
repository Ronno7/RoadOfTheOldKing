using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // A vertical list of menu buttons built from PauseMenuAction commands: mouse hover/click and
    // keyboard/controller selection share one highlighted entry, and the selected entry's description
    // shows in a label. Navigation and submit come from MenuStack's input routing, so UI Toolkit's own
    // navigation events are intercepted while trickling down, before a button's built-in handler can
    // move focus or activate it a second time.
    public sealed class MenuList
    {
        private readonly VisualElement container;
        private readonly Label description;
        private readonly Action<PauseMenuAction> invoke;
        private readonly List<Button> buttons = new List<Button>();
        private IReadOnlyList<PauseMenuAction> actions = Array.Empty<PauseMenuAction>();
        private int selected;

        public MenuList(VisualElement container, Label description, Action<PauseMenuAction> invoke)
        {
            this.container = container;
            this.description = description;
            this.invoke = invoke;
            container.RegisterCallback<NavigationSubmitEvent>(e => e.StopImmediatePropagation(), TrickleDown.TrickleDown);
            container.RegisterCallback<NavigationMoveEvent>(e => e.StopImmediatePropagation(), TrickleDown.TrickleDown);
        }

        public void Show(IReadOnlyList<PauseMenuAction> items, int select = 0)
        {
            actions = items ?? Array.Empty<PauseMenuAction>();
            buttons.Clear();
            container.Clear();
            for (int i = 0; i < actions.Count; i++)
            {
                int index = i;
                var button = new Button(() => invoke(actions[index])) { text = actions[i].Label };
                // Shared kit row (Kit.uss); "menu-button" stays for per-screen adjustments.
                button.AddToClassList("menu-button"); button.AddToClassList("kit-row");
                // The kit shows this cursor beside the selected row only.
                var cursor = new VisualElement { pickingMode = PickingMode.Ignore };
                cursor.AddToClassList("menu-cursor"); cursor.AddToClassList("kit-cursor");
                button.Add(cursor);
                button.SetEnabled(actions[i].Enabled);
                button.EnableInClassList("locked", actions[i].Locked);
                // Not focusable: UI Toolkit's built-in controller/keyboard navigation moves focus even when its
                // events are stopped, which would step a second row. The "selected" class is the highlight.
                button.focusable = false;
                button.RegisterCallback<PointerEnterEvent>(_ => Select(index));
                buttons.Add(button);
                container.Add(button);
            }
            if (buttons.Count == 0) { if (description != null) description.text = ""; return; }
            Select(Math.Max(0, Math.Min(select, buttons.Count - 1)));
        }

        public void Navigate(int direction)
        {
            if (buttons.Count == 0) return;
            Select((selected + direction + buttons.Count) % buttons.Count);
        }

        public void Submit() { if (buttons.Count > 0 && actions[selected].Enabled) invoke(actions[selected]); }

        private void Select(int index)
        {
            selected = index;
            for (int i = 0; i < buttons.Count; i++) buttons[i].EnableInClassList("selected", i == selected);
            if (description != null) description.text = actions[selected].Description ?? "";
        }
    }
}
