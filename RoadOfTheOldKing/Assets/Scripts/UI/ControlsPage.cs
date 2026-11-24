using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // The pause menu's controls reference: each action with its keyboard and controller controls drawn as glyphs, read
    // from ControlLabels so the page can't drift from the prompts. Pure layout; PauseMenuView owns showing it.
    public static class ControlsPage
    {
        private static readonly (string group, (string action, string token)[] rows)[][] Blocks =
        {
            new[]
            {
                ("MOVEMENT", new[] { ("Move", "move"), ("Sprint", "sprint"), ("Dodge", "dash") }),
                ("COMBAT", new[] { ("Light attack", "attack"), ("Charged cleave", "cleave"), ("Throw", "throw"), ("Aim", "aim"), ("Drink flask", "heal") }),
            },
            new[]
            {
                ("WORLD", new[] { ("Interact", "interact"), ("Inventory", "status") }),
                ("CAMERA", new[] { ("Free look", "look"), ("Zoom", "zoom") }),
                ("MENUS", new[] { ("Pause", "pause"), ("Select", "select"), ("Back", "back") }),
            },
        };

        // Recall is named only once it's earned, so the page doesn't give the Tutorial's awakening away.
        public static void Fill(VisualElement table, bool canRecall)
        {
            table.Clear();
            foreach (var block in Blocks)
            {
                var column = new VisualElement();
                column.AddToClassList("controls-block");
                table.Add(column);
                for (int g = 0; g < block.Length; g++)
                {
                    // The first heading also titles the two device columns.
                    var heading = Row(column, block[g].group);
                    heading.Add(Cell(g == 0 ? "Keys" : "", "controls-keys"));
                    heading.Add(Cell(g == 0 ? "Pad" : "", "controls-pad"));
                    heading.AddToClassList("controls-heading");
                    foreach (var (action, token) in block[g].rows)
                    {
                        var row = Row(column, token == "throw" && canRecall ? "Throw, Recall" : action);
                        row.Add(Glyphs(ControlLabels.KeyboardLabel(token), false, "controls-keys"));
                        row.Add(Glyphs(ControlLabels.GamepadLabel(token), true, "controls-pad"));
                    }
                }
            }
        }

        private static VisualElement Row(VisualElement parent, string action)
        {
            var row = new VisualElement();
            row.AddToClassList("controls-row");
            row.Add(Cell(action, "controls-action"));
            parent.Add(row);
            return row;
        }

        private static VisualElement Glyphs(string control, bool pad, string style)
        {
            var cell = new VisualElement { pickingMode = PickingMode.Ignore };
            cell.AddToClassList(style);
            cell.AddToClassList("controls-glyphs");
            cell.Add(ControlGlyphs.Key(control, pad));
            return cell;
        }

        private static Label Cell(string text, string style)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(style);
            return label;
        }
    }
}
