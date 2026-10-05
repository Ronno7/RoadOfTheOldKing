using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.Prototype
{
    // Legacy placeholder kept for the retired PrototypeLoop: its heading, guide instruction and controls
    // legend (IMGUI). Vitals, prompts and receipts belong to GameHud; the bonfire and defeat screens are
    // the UI Toolkit BonfireMenu and DefeatScreen on the Player prefab. In compact mode (Tutorial) it
    // draws nothing.
    public sealed class TutorialCombatHUD : MonoBehaviour
    {
        [SerializeField] private PlayerCombatController player;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PrototypeLoopGuide guide;
        [SerializeField] private string heading = "PROTOTYPE LOOP";
        [SerializeField] private bool compact;
        private GUIStyle title;
        private GUIStyle text;

        private void OnGUI()
        {
            if (compact || player == null || Time.timeScale <= 0f || RoadOfTheOldKing.UI.MenuStack.IsAnyOpen)
                return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
                text = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            }
            float scale = Mathf.Clamp(Screen.width / 960f, 0.6f, 1.25f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float screenWidth = Screen.width / scale;
            float screenHeight = Screen.height / scale;
            float width = Mathf.Min(440f, screenWidth - 24f);

            var box = new Rect((screenWidth - width) * 0.5f, 12f, width, 94f);
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x + 12f, 18f, width - 24f, 24f), heading, title);
            GUI.Label(new Rect(box.x + 12f, 44f, width - 24f, 60f), guide != null ? guide.Instruction : "", text);

            string controls = "WASD / arrows: move   |   Shift: sprint   |   Mouse: aim   |   Scroll: zoom\n" +
                "LMB: slash   |   Hold / release RMB: cleave   |   Space: dodge   |   Q: flask\n" +
                "Tap E: throw   |   Hold E: aim; release: throw; RMB: cancel" +
                (player.CanRecall ? "   |   E while away: recall" : "\nWalk over your thrown axe to retrieve it") +
                "\nF: pick up / rest   |   Esc: pause / back   |   Left Alt: freelook";
            GUI.Box(new Rect(12f, screenHeight - 128f, Mathf.Min(760f, screenWidth - 24f), 116f), GUIContent.none);
            GUI.Label(new Rect(24f, screenHeight - 122f, Mathf.Min(738f, screenWidth - 48f), 110f), controls, text);
            GUI.matrix = previousMatrix;
        }
    }
}
