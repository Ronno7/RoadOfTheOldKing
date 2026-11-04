using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // The pause menu's controls reference: each action with its keyboard and controller names, read from
    // ControlLabels so the page can't drift from the prompts. Pure layout; PauseMenuView owns showing it.
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
                ("CAMERA", new[] { ("Look around", "look"), ("Zoom", "zoom") }),
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
                    var heading = Row(column, block[g].group, g == 0 ? "Keys" : "", g == 0 ? "Pad" : "");
                    heading.AddToClassList("controls-heading");
                    foreach (var (action, token) in block[g].rows)
                        Row(column, token == "throw" && canRecall ? "Throw, Recall" : action,
                            ControlLabels.KeyboardLabel(token), ControlLabels.GamepadLabel(token));
                }
            }
        }

        private static VisualElement Row(VisualElement parent, string action, string keys, string pad)
        {
            var row = new VisualElement();
            row.AddToClassList("controls-row");
            row.Add(Cell(action, "controls-action"));
            row.Add(Cell(keys, "controls-keys"));
            row.Add(Cell(pad, "controls-pad"));
            parent.Add(row);
            return row;
        }

        private static Label Cell(string text, string style)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList(style);
            return label;
        }
    }
}
