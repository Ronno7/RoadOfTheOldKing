using System.Collections.Generic;
using System.Text;

namespace RoadOfTheOldKing.UI
{
    // One place for the key names shown in prompts and hints. Text uses {tokens} such as
    // "Press {interact}". The labels mirror the current fixed bindings (PlayerMovementInput,
    // PlayerCombatInput, PlayerBonfireInteraction); when rebinding arrives, update Set() from the
    // actions instead of editing hint text.
    public static class ControlLabels
    {
        private static readonly Dictionary<string, string> labels = new Dictionary<string, string>
        {
            { "move", "WASD" }, { "sprint", "Shift" }, { "dash", "Space" }, { "interact", "F" },
            { "attack", "LMB" }, { "cleave", "RMB" }, { "throw", "E" },
            { "recall", "E" }, { "pause", "Esc" }, { "status", "Tab" }, { "look", "Alt" }, { "heal", "Q" },
        };

        public static string Get(string token) => labels.TryGetValue(token, out var label) ? label : token;
        public static void Set(string token, string label) => labels[token] = label;

        // Replaces {token} with a gold [key] in UI Toolkit rich text; unknown tokens stay readable. No bold:
        // the pixel font has one weight.
        public static string Format(string text, string keyColor = "#E7B85C")
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text;
            var result = new StringBuilder(text.Length + 32);
            for (int i = 0; i < text.Length; i++)
            {
                int end = text[i] == '{' ? text.IndexOf('}', i + 1) : -1;
                if (end < 0) { result.Append(text[i]); continue; }
                string token = text.Substring(i + 1, end - i - 1);
                result.Append("<color=").Append(keyColor).Append(">[").Append(Get(token)).Append("]</color>");
                i = end;
            }
            return result.ToString();
        }
    }
}
