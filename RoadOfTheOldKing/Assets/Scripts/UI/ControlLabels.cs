using System.Collections.Generic;
using System.Text;
using RoadOfTheOldKing.Input;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // One place for the key names shown in prompts and hints. Text uses {tokens} such as
    // "Press {interact}". Labels follow the last-used device (ActiveInput): keyboard/mouse names mirror
    // the adapters' keyboard bindings, controller names mirror GamepadLayout (Xbox names). On a PlayStation pad
    // every controller label and hint swaps the Xbox button words for its own (Cross, Circle, L1...). When
    // rebinding arrives, update Set() from the actions instead of editing hint text.
    public static class ControlLabels
    {
        private static readonly Dictionary<string, string> keyboard = new Dictionary<string, string>
        {
            { "move", "WASD" }, { "sprint", "Hold Shift" }, { "dash", "Space" }, { "interact", "F" },
            { "attack", "LMB" }, { "cleave", "RMB" }, { "throw", "E" },
            { "recall", "E" }, { "pause", "Esc" }, { "status", "Tab" }, { "look", "Alt" }, { "heal", "R" },
            { "aim", "Mouse" }, { "zoom", "Wheel" }, { "select", "Enter" }, { "back", "Esc" },
            { "skip", "Esc" },
        };
        private static readonly Dictionary<string, string> gamepad = new Dictionary<string, string>
        {
            { "move", "LS" }, { "sprint", "Hold B" }, { "dash", "B" }, { "interact", "A" },
            { "attack", "RB" }, { "cleave", "RT" }, { "throw", "LT" },
            { "recall", "LT" }, { "pause", "Start" }, { "status", "View" }, { "look", "LB + RS" }, { "heal", "X" },
            { "aim", "RS" }, { "zoom", "D-pad" }, { "select", "A" }, { "back", "B" },
            { "skip", "Start" },
        };
        private static readonly Dictionary<string, string> playStation = new Dictionary<string, string>
        {
            { "A", "Cross" }, { "B", "Circle" }, { "X", "Square" }, { "Y", "Triangle" }, { "LB", "L1" }, { "RB", "R1" },
            { "LT", "L2" }, { "RT", "R2" }, { "View", "Create" }, { "Start", "Options" },
        };
        private static readonly System.Text.RegularExpressions.Regex XboxWords =
            new System.Text.RegularExpressions.Regex(@"\b(A|B|X|Y|LB|RB|LT|RT|View|Start)\b");
        private static readonly List<(Label label, string keys, string pad)> bound = new List<(Label, string, string)>();
        private static bool subscribed;

        public static string Get(string token) => ActiveInput.UsingGamepad ? GamepadLabel(token) : KeyboardLabel(token);
        // Both devices at once, for the controls page.
        public static string KeyboardLabel(string token) => keyboard.TryGetValue(token, out var label) ? label : token;
        public static string GamepadLabel(string token) => gamepad.TryGetValue(token, out var label) ? ForPad(label) : token;

        // Plain menu hint lines (user rule, 4 Oct: key names stay out of buttons) for each device.
        public static string Pick(string keyboardText, string gamepadText) => ActiveInput.UsingGamepad ? ForPad(gamepadText) : keyboardText;

        // Xbox button words as the pad in use names them.
        private static string ForPad(string text) => !ActiveInput.PlayStationPad || string.IsNullOrEmpty(text) ? text :
            XboxWords.Replace(text, match => playStation[match.Value]);

        // Shows the matching hint now and whenever the active device changes; rebinding a label replaces it.
        public static void Bind(Label label, string keyboardText, string gamepadText)
        {
            if (label == null) return;
            if (!subscribed) { ActiveInput.Changed += Refresh; subscribed = true; }
            bound.RemoveAll(entry => entry.label == label || entry.label.panel == null);
            bound.Add((label, keyboardText, gamepadText));
            label.text = Pick(keyboardText, gamepadText);
        }

        private static void Refresh()
        {
            bound.RemoveAll(entry => entry.label.panel == null);
            foreach (var entry in bound) entry.label.text = Pick(entry.keys, entry.pad);
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { bound.Clear(); subscribed = false; }

        // Replaces {token} with a gold [key] in UI Toolkit rich text; unknown tokens stay readable. No bold:
        // the pixel font has one weight.
        public static string Format(string text, string keyColor = "#E7B85C") => Format(text, keyColor, true);

        // Without the brackets: for text in a face that lacks them (RotOK Heading16), the gold alone marks the key.
        public static string FormatBare(string text, string keyColor = "#E7B85C") => Format(text, keyColor, false);

        private static string Format(string text, string keyColor, bool brackets)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
            var result = new StringBuilder(text.Length + 32);
            for (int i = 0; i < text.Length; i++)
            {
                int end = text[i] == '{' ? text.IndexOf('}', i + 1) : -1;
                if (end < 0) { result.Append(text[i]); continue; }
                string token = text.Substring(i + 1, end - i - 1);
                result.Append("<color=").Append(keyColor).Append(brackets ? ">[" : ">").Append(Get(token))
                    .Append(brackets ? "]</color>" : "</color>");
                i = end;
            }
            return result.ToString();
        }
    }
}
