using System;
using System.Collections.Generic;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // A compact read-only view. World interactions own all item use.
    internal sealed class InventoryGridView
    {
        private readonly VisualElement grid;
        private readonly Label title;
        private readonly Func<string, VisualElement> createIcon;
        private readonly List<Button> buttons = new List<Button>();
        private int selected;
        private const int Columns = 5;

        public InventoryGridView(VisualElement root, Func<string, VisualElement> iconFactory)
        {
            grid = root.Q("item-grid"); title = root.Q<Label>("item-name");
            createIcon = iconFactory;
        }

        public void Show(PlayerHealth player)
        {
            grid.Clear(); buttons.Clear();
            var state = GameSession.Instance.Progress;
            var flask = player.GetComponent<PlayerFlask>();
            if (flask != null) Add("Flasks", "flask", flask.Charges + "/" + flask.MaxCharges);
            if (state.OwnsTool(WorldTool.RopeKit)) Add("Rope kit", "rope");
            if (state.OwnsTool(WorldTool.MaintenanceCrank)) Add("Maintenance crank", "crank");
            if (state.OwnsTool(WorldTool.ProtectedLantern)) Add("Protected lantern", "lantern");
            int lore = ForgeInscriptionProgression.Count(state);
            if (lore > 0) Add("Smiths' knowledge", "knowledge",
                Math.Min(lore, ForgeInscriptionProgression.RequiredKnowledge) + "/" + ForgeInscriptionProgression.RequiredKnowledge);
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, buttons.Count - 1));
            title.text = buttons.Count == 0 ? "Nothing carried" : buttons[selected].tooltip;
            Select(selected, false);
            grid.schedule.Execute(() => { if (buttons.Count > 0) buttons[selected].Focus(); });
        }

        private void Add(string name, string icon, string count = "")
        {
            int index = buttons.Count;
            var button = new Button(() => Select(index, true)) { tooltip = name };
            button.AddToClassList("pack-item"); button.Add(createIcon(icon));
            if (!string.IsNullOrEmpty(count))
            {
                var label = new Label(count) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("item-count"); button.Add(label);
            }
            button.RegisterCallback<PointerEnterEvent>(_ => Select(index, false));
            button.RegisterCallback<FocusInEvent>(_ => Select(index, false));
            grid.Add(button); buttons.Add(button);
        }

        public void Navigate(int x, int y)
        {
            if (buttons.Count == 0) return;
            int next = selected;
            if (x != 0)
            {
                int rowStart = selected / Columns * Columns;
                int rowCount = Mathf.Min(Columns, buttons.Count - rowStart);
                next = rowStart + (selected - rowStart + x + rowCount) % rowCount;
            }
            else if (y != 0)
            {
                int rowCount = (buttons.Count + Columns - 1) / Columns;
                int nextRow = (selected / Columns + y + rowCount) % rowCount;
                next = Mathf.Min(nextRow * Columns + selected % Columns, buttons.Count - 1);
            }
            Select(next, true);
        }

        private void Select(int index, bool focus)
        {
            if (index < 0 || index >= buttons.Count) return;
            selected = index;
            for (int i = 0; i < buttons.Count; i++) buttons[i].EnableInClassList("selected", i == index);
            title.text = buttons[index].tooltip;
            if (focus) buttons[index].Focus();
        }
    }
}
