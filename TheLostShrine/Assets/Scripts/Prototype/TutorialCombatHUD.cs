using TheLostShrine.Player;
using TheLostShrine.Weapons;
using UnityEngine;

namespace TheLostShrine.Prototype
{
    public sealed class TutorialCombatHUD : MonoBehaviour
    {
        [SerializeField] private PlayerCombatController player;
        private GUIStyle title;
        private GUIStyle text;

        private void OnGUI()
        {
            if (player == null)
                return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
                text = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            }
            float width = Mathf.Min(410f, Screen.width - 24f);
            GUI.Box(new Rect(12f, 12f, width, 162f), GUIContent.none);
            GUI.Label(new Rect(24f, 19f, width - 24f, 26f), "HATCHET PRACTICE", title);
            var weapon = player.Weapon;
            string status = weapon == null ? "Walk over the golden hatchet to pick it up."
                : !player.CanRecall ? "Recall locked: retrieve throws on foot. Find the northern altar."
                : "Recall awakened: throw, reposition, then press E to call it back.";
            GUI.Label(new Rect(24f, 48f, width - 24f, 40f), status, text);
            GUI.Label(new Rect(24f, 91f, width - 24f, 73f),
                "Mouse: aim  |  LMB: light combo\nHold RMB, release: spin  |  E: throw / recall\nFull gold charge breaks shields and cracked blocks.\nRecall hits from behind bypass shields.", text);

            if (weapon != null && weapon.State == HatchetState.Charging)
            {
                GUI.Box(new Rect(12f, 182f, width, 30f), GUIContent.none);
                Color previous = GUI.color;
                GUI.color = weapon.Charge01 >= 1f ? new Color(1f, 0.8f, 0.2f) : new Color(0.5f, 0.8f, 1f);
                GUI.DrawTexture(new Rect(18f, 188f, (width - 12f) * weapon.Charge01, 18f), Texture2D.whiteTexture);
                GUI.color = previous;
            }
        }
    }
}
