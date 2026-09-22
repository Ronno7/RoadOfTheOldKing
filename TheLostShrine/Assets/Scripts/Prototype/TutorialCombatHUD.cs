using TheLostShrine.Player;
using TheLostShrine.Weapons;
using TheLostShrine.Progression;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace TheLostShrine.Prototype
{
    public sealed class TutorialCombatHUD : MonoBehaviour
    {
        [SerializeField] private PlayerCombatController player;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PrototypeLoopGuide guide;
        private GUIStyle title;
        private GUIStyle text;
        private bool restarting;
        private bool confirmNewRun;
        private PlayerBonfireInteraction bonfireInteraction;
        private PlayerStamina stamina;

        private void Start()
        {
            if (player != null)
            {
                bonfireInteraction = player.GetComponent<PlayerBonfireInteraction>();
                stamina = player.GetComponent<PlayerStamina>();
            }
        }

        private void Update()
        {
            bool canRestart = playerHealth != null && !playerHealth.IsAlive;
            if (canRestart && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                Restart();
            if (bonfireInteraction == null || !bonfireInteraction.IsOpen)
                confirmNewRun = false;
        }

        private void Restart()
        {
            if (restarting)
                return;
            restarting = true;
            if (CheckpointSession.Instance != null)
                CheckpointSession.Instance.Respawn();
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnGUI()
        {
            if (player == null)
                return;
            if (title == null)
            {
                title = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold };
                text = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            }
            // Keep this placeholder readable in small Game views as well as full screen.
            float scale = Mathf.Clamp(Screen.width / 960f, 0.6f, 1.25f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            float screenWidth = Screen.width / scale;
            float screenHeight = Screen.height / scale;
            float width = Mathf.Min(440f, screenWidth - 24f);
            Color previousColor = GUI.color;

            GUI.Box(new Rect(12f, 12f, width, 186f), GUIContent.none);
            GUI.Label(new Rect(24f, 18f, width - 24f, 24f), "PROTOTYPE LOOP", title);
            if (playerHealth != null && playerHealth.Health != null)
            {
                var health = playerHealth.Health;
                GUI.Label(new Rect(24f, 46f, 140f, 24f), "HP  " + health.Health + " / " + health.MaxHealth, title);
                GUI.color = new Color(0.2f, 0.2f, 0.2f);
                GUI.DrawTexture(new Rect(174f, 50f, width - 186f, 14f), Texture2D.whiteTexture);
                GUI.color = playerHealth.IsInvulnerable ? Color.white : new Color(0.9f, 0.3f, 0.3f);
                GUI.DrawTexture(new Rect(174f, 50f, (width - 186f) * health.Health / health.MaxHealth, 14f), Texture2D.whiteTexture);
                GUI.color = previousColor;
            }
            if (stamina != null)
            {
                GUI.Label(new Rect(24f, 76f, 140f, 24f), "STA  " + Mathf.FloorToInt(stamina.Current) + " / " + Mathf.RoundToInt(stamina.Maximum), text);
                GUI.color = new Color(0.2f, 0.2f, 0.2f);
                GUI.DrawTexture(new Rect(174f, 80f, width - 186f, 14f), Texture2D.whiteTexture);
                GUI.color = stamina.WasSpendRejected ? new Color(1f, 0.3f, 0.2f) :
                    stamina.Normalized < 0.25f ? new Color(1f, 0.7f, 0.2f) : new Color(0.3f, 0.85f, 0.55f);
                GUI.DrawTexture(new Rect(174f, 80f, (width - 186f) * stamina.Normalized, 14f), Texture2D.whiteTexture);
                GUI.color = previousColor;
                if (stamina.WasSpendRejected)
                    GUI.Label(new Rect(24f, 207f, width - 24f, 24f), "Not enough stamina - walk to recover.", text);
            }
            string instruction = guide != null ? guide.Instruction : "Pick up the hatchet and practice.";
            GUI.Label(new Rect(24f, 107f, width - 24f, 82f), instruction, text);

            string controls = "WASD / arrows: move   |   Shift: sprint   |   Mouse: aim   |   Scroll: zoom\n" +
                "LMB: slash   |   Hold / release RMB: cleave   |   E: throw" +
                (player.CanRecall ? " / recall" : " (retrieve on foot)") + "\nSpace: dash / dodge   |   F: rest / travel   |   Esc: leave fire menu";
            GUI.Box(new Rect(12f, screenHeight - 86f, Mathf.Min(660f, screenWidth - 24f), 74f), GUIContent.none);
            GUI.Label(new Rect(24f, screenHeight - 80f, Mathf.Min(638f, screenWidth - 48f), 70f), controls, text);

            if (bonfireInteraction != null && !bonfireInteraction.IsOpen && bonfireInteraction.Nearby != null)
                GUI.Label(new Rect(24f, 235f, width - 24f, 28f), "F - Rest at " + bonfireInteraction.Nearby.DisplayName, title);

            var weapon = player.Weapon;
            if (weapon != null && weapon.State == HatchetState.Charging)
            {
                GUI.color = weapon.Charge01 >= 1f ? new Color(1f, 0.8f, 0.2f) : new Color(0.5f, 0.8f, 1f);
                GUI.DrawTexture(new Rect(12f, 206f, width * weapon.Charge01, 10f), Texture2D.whiteTexture);
                GUI.color = previousColor;
            }

            if (playerHealth != null && !playerHealth.IsAlive)
            {
                var box = new Rect((screenWidth - 310f) * 0.5f, (screenHeight - 138f) * 0.5f, 310f, 138f);
                GUI.Box(box, GUIContent.none);
                GUI.Label(new Rect(box.x + 18f, box.y + 14f, 270f, 28f), "YOU WERE DEFEATED", title);
                bool checkpoint = CheckpointSession.Instance != null && CheckpointSession.Instance.HasCheckpoint;
                GUI.Label(new Rect(box.x + 18f, box.y + 47f, 274f, 32f), checkpoint ? "Press R to return to your bonfire." : "Press R to retry from the start.", text);
                if (GUI.Button(new Rect(box.x + 18f, box.y + 86f, 274f, 34f), checkpoint ? "Return to bonfire" : "Restart"))
                    Restart();
            }
            else if (bonfireInteraction != null && bonfireInteraction.IsOpen)
                DrawBonfireMenu(screenWidth, screenHeight);
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;
        }

        private void DrawBonfireMenu(float screenWidth, float screenHeight)
        {
            var session = CheckpointSession.Instance;
            if (session == null)
                return;
            var fire = bonfireInteraction.ActiveFire;
            var box = new Rect((screenWidth - 420f) * 0.5f, (screenHeight - 330f) * 0.5f, 420f, 330f);
            Color menuColor = GUI.color;
            GUI.color = new Color(0.06f, 0.08f, 0.08f, 0.97f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = menuColor;
            GUI.Box(box, GUIContent.none);
            float x = box.x + 18f;
            float y = box.y + 16f;
            GUI.Label(new Rect(x, y, 384f, 28f), fire.DisplayName.ToUpperInvariant(), title);
            GUI.Label(new Rect(x, y + 34f, 384f, 48f), session.Status, text);
            y += 90f;
            if (confirmNewRun)
            {
                GUI.Label(new Rect(x, y, 384f, 44f), "Start over? This clears this prototype's saved progress.", text);
                if (GUI.Button(new Rect(x, y + 54f, 188f, 34f), "Start new run"))
                    session.StartNewRun();
                if (GUI.Button(new Rect(x + 196f, y + 54f, 188f, 34f), "Keep playing"))
                    confirmNewRun = false;
                return;
            }
            if (GUI.Button(new Rect(x, y, 384f, 32f), "Rest again (restore HP / stamina / save)"))
                session.Rest(fire);
            y += 42f;
            bool travelAvailable = false;
            foreach (var destination in session.Fires)
            {
                if (destination == fire || !destination.IsDiscovered)
                    continue;
                travelAvailable = true;
                if (GUI.Button(new Rect(x, y, 384f, 32f), "Travel to " + destination.DisplayName) && session.Travel(destination, fire))
                    bonfireInteraction.Close();
                y += 38f;
            }
            if (!travelAvailable)
            {
                GUI.Label(new Rect(x, y, 384f, 36f), "Light another fire to unlock travel.", text);
                y += 38f;
            }
            if (GUI.Button(new Rect(x, box.yMax - 86f, 384f, 30f), "New run..."))
                confirmNewRun = true;
            if (GUI.Button(new Rect(x, box.yMax - 46f, 384f, 30f), "Leave (F / Esc)"))
                bonfireInteraction.Close();
        }
    }
}
