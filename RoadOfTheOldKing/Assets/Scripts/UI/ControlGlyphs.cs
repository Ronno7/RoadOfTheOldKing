using System.Collections.Generic;
using System.Text.RegularExpressions;
using RoadOfTheOldKing.Input;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // Controls drawn as icons instead of key names in brackets (user, 23 Nov: "find a better way to represent that";
    // research: platform guidelines and shipped games show button glyphs that follow the device in use). Every key, mouse
    // and pad control is a drawn sprite (Kit.uss, Tools/Art/UI/ui_kit.py); a key without one falls back to a keycap
    // holding its name. Text with {tokens} lays out as words and glyphs, line by line, centred.
    public static class ControlGlyphs
    {
        private static readonly Regex Pieces = new Regex(@"\{[a-z]+\}|[^\s{]+");
        // ControlLabels' names -> the sprite's class. Pad names cover Xbox and PlayStation; Options shares Start's lines.
        private static readonly Dictionary<string, string> PadSprites = new Dictionary<string, string>
        {
            { "A", "glyph-a" }, { "B", "glyph-b" }, { "X", "glyph-x" }, { "Y", "glyph-y" },
            { "Cross", "glyph-cross" }, { "Circle", "glyph-circle" }, { "Square", "glyph-square" }, { "Triangle", "glyph-triangle" },
            { "LS", "glyph-ls" }, { "RS", "glyph-rs" }, { "D-pad", "glyph-dpad" },
            { "LB", "glyph-lb" }, { "RB", "glyph-rb" }, { "LT", "glyph-lt" }, { "RT", "glyph-rt" },
            { "L1", "glyph-l1" }, { "R1", "glyph-r1" }, { "L2", "glyph-l2" }, { "R2", "glyph-r2" },
            { "Start", "glyph-start" }, { "Options", "glyph-start" }, { "View", "glyph-view" }, { "Create", "glyph-create" },
        };
        private static readonly Dictionary<string, string> KeyboardSprites = new Dictionary<string, string>
        {
            { "LMB", "glyph-mouse-left" }, { "RMB", "glyph-mouse-right" }, { "Wheel", "glyph-mouse-wheel" }, { "Mouse", "glyph-mouse" },
            { "WASD", "glyph-key-wasd" }, { "Shift", "glyph-key-shift" }, { "Space", "glyph-key-space" }, { "Enter", "glyph-key-enter" },
            { "Esc", "glyph-key-esc" }, { "Tab", "glyph-key-tab" }, { "Alt", "glyph-key-alt" },
            { "F", "glyph-key-f" }, { "E", "glyph-key-e" }, { "R", "glyph-key-r" },
        };
        private static readonly List<(VisualElement host, string text)> bound = new List<(VisualElement, string)>();
        private static bool subscribed;

        // Shows the text now and again whenever the device changes.
        public static void Bind(VisualElement host, string text)
        {
            if (host == null) return;
            if (!subscribed) { ActiveInput.Changed += Refresh; subscribed = true; }
            bound.RemoveAll(entry => entry.host == host || entry.host.panel == null);
            bound.Add((host, text));
            Flow(host, text);
        }

        public static void Unbind(VisualElement host) => bound.RemoveAll(entry => entry.host == host);

        private static void Refresh()
        {
            bound.RemoveAll(entry => entry.host.panel == null);
            foreach (var entry in bound) Flow(entry.host, entry.text);
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { bound.Clear(); subscribed = false; }

        public static void Flow(VisualElement host, string text)
        {
            host.Clear();
            if (host is TextElement label) label.text = "";
            // Empty text hides the host; otherwise its visibility stays with the owner (the title hides its "Press" line
            // on the menu page), except a host this method hid itself.
            if (string.IsNullOrEmpty(text))
            {
                host.style.display = DisplayStyle.None;
                host.AddToClassList("glyph-emptied");
                return;
            }
            if (host.ClassListContains("glyph-emptied"))
            {
                host.RemoveFromClassList("glyph-emptied");
                host.style.display = DisplayStyle.Flex;
            }
            foreach (string line in text.Split('\n'))
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList("glyph-line");
                foreach (Match piece in Pieces.Matches(line))
                    row.Add(piece.Value.StartsWith("{") ? Token(piece.Value.Substring(1, piece.Value.Length - 2)) : Word(piece.Value));
                host.Add(row);
            }
        }

        // The control a token names on the active device, e.g. {interact} -> F or A.
        public static VisualElement Token(string token) => Key(ControlLabels.Get(token));

        // A key or button name as ControlLabels gives it: "F", "Hold B", "LB + RS", "Cross", on the device in use.
        public static VisualElement Key(string name) => Key(name, ActiveInput.UsingGamepad);

        // The same for a named device, for the controls page's two columns.
        public static VisualElement Key(string name, bool pad)
        {
            var group = new VisualElement { pickingMode = PickingMode.Ignore };
            group.AddToClassList("glyph-group");
            if (name.StartsWith("Hold ")) { group.Add(Word("Hold")); name = name.Substring(5); }
            string[] parts = name.Split(new[] { " + " }, System.StringSplitOptions.None);
            for (int i = 0; i < parts.Length; i++)
            {
                if (i > 0) group.Add(Word("+"));
                group.Add(Single(parts[i], pad));
            }
            return group;
        }

        private static VisualElement Single(string name, bool pad)
        {
            var glyph = new VisualElement { pickingMode = PickingMode.Ignore };
            glyph.AddToClassList("glyph");
            if ((pad ? PadSprites : KeyboardSprites).TryGetValue(name, out string sprite))
            {
                glyph.AddToClassList("glyph-sprite");
                glyph.AddToClassList(sprite);
                return glyph;
            }
            glyph.AddToClassList("glyph-key");
            glyph.Add(new Label(name) { pickingMode = PickingMode.Ignore });
            return glyph;
        }

        private static Label Word(string text)
        {
            var word = new Label(text) { pickingMode = PickingMode.Ignore };
            word.AddToClassList("glyph-word");
            return word;
        }
    }
}
