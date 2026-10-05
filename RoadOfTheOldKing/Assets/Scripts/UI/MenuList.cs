using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // A vertical list of menu buttons built from PauseMenuAction commands: mouse hover/click and
    // keyboard selection share one highlighted entry, and the selected entry's description shows in a
    // label. Keyboard navigation comes from MenuStack's input routing, so UI Toolkit's own navigation
    // events are stopped here to avoid double moves.
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
                button.AddToClassList("menu-button");
                // Hidden by default (MenuTheme.uss); styles such as Hearth show it beside the selected entry.
                var cursor = new VisualElement { pickingMode = PickingMode.Ignore };
                cursor.AddToClassList("menu-cursor");
                button.Add(cursor);
                button.SetEnabled(actions[i].Enabled);
                button.RegisterCallback<FocusInEvent>(_ => Select(index));
                button.RegisterCallback<PointerEnterEvent>(_ => Select(index));
                button.RegisterCallback<NavigationSubmitEvent>(e => e.StopPropagation());
                button.RegisterCallback<NavigationMoveEvent>(e => e.StopPropagation());
                buttons.Add(button);
                container.Add(button);
            }
            if (buttons.Count == 0) { if (description != null) description.text = ""; return; }
            Select(Math.Max(0, Math.Min(select, buttons.Count - 1)));
            container.schedule.Execute(Focus);
        }

        public void Focus() { if (buttons.Count > 0 && selected < buttons.Count) buttons[selected].Focus(); }

        public void Navigate(int direction)
        {
            if (buttons.Count == 0) return;
            Select((selected + direction + buttons.Count) % buttons.Count);
            Focus();
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
