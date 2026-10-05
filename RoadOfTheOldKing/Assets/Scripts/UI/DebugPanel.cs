using System.Text;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.Weapons;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // F3 developer overlay: performance, player, world and save state. Installs itself in the
    // Editor, development builds and WebGL; reads, never writes, gameplay.
    public sealed class DebugPanel : MonoBehaviour
    {
        private const Key ToggleKey = Key.F3;
        private const string VisiblePref = "RoadOfTheOldKing.Dev.StatsPanel";
        private const float RefreshSeconds = 0.25f;
        private const int FrameWindow = 120;

        private readonly float[] frameTimes = new float[FrameWindow];
        private readonly StringBuilder text = new StringBuilder(1024);
        private UIDocument document;
        private PanelSettings settings;
        private Label readout;
        private int frameIndex, frameCount;
        private float refreshIn;
        private float enemyRefreshIn;
        private int enemiesAlive, enemiesTotal;
        private bool visible;
        private ProfilerRecorder mainThread, batches, setPass, triangles, gcAllocInFrame;
        private PlayerHealth player;
        private PlayerMovement movement;
        private PlayerStamina stamina;
        private PlayerCombatController combat;
        private Rigidbody2D body;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (!Application.isEditor && !Debug.isDebugBuild && Application.platform != RuntimePlatform.WebGLPlayer) return;
            var host = new GameObject("Debug Panel (F3)");
            DontDestroyOnLoad(host);
            host.AddComponent<DebugPanel>();
        }

        private void OnEnable()
        {
            try { visible = PlayerPrefs.GetInt(VisiblePref, 0) == 1; } catch { visible = false; }
            var source = Resources.Load<PanelSettings>("DevTools/DevToolsPanel");
            var layout = Resources.Load<VisualTreeAsset>("DevTools/DebugPanel");
            settings = Instantiate(source);
            settings.sortingOrder = 9999;
            document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = settings;
            document.visualTreeAsset = layout;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;
            readout = document.rootVisualElement.Q<Label>("debug-readout");
            mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
            batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            triangles = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            gcAllocInFrame = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (document != null) Destroy(document);
            if (settings != null) Destroy(settings);
            mainThread.Dispose(); batches.Dispose(); setPass.Dispose(); triangles.Dispose(); gcAllocInFrame.Dispose();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { player = null; enemyRefreshIn = 0f; }

        private void Update()
        {
            // Unscaled: keeps measuring and toggling while paused or in hit stop.
            frameTimes[frameIndex] = Time.unscaledDeltaTime;
            frameIndex = (frameIndex + 1) % FrameWindow;
            frameCount = Mathf.Min(frameCount + 1, FrameWindow);

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[ToggleKey].wasPressedThisFrame)
            {
                visible = !visible;
                try { PlayerPrefs.SetInt(VisiblePref, visible ? 1 : 0); } catch { }
                refreshIn = 0f;
            }
            bool show = visible;
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
            show &= DevToolsPanel.Instance == null || !DevToolsPanel.Instance.IsOpen;
#endif
            readout.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            settings.scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), .65f, 1.5f);
            if (!visible) return;
            refreshIn -= Time.unscaledDeltaTime;
            enemyRefreshIn -= Time.unscaledDeltaTime;
            if (refreshIn <= 0f)
            {
                refreshIn = RefreshSeconds;
                Rebuild();
            }
        }

        private void FindPlayer()
        {
            if (player != null) return;
            player = FindFirstObjectByType<PlayerHealth>();
            if (player == null) return;
            movement = player.GetComponent<PlayerMovement>();
            stamina = player.GetComponent<PlayerStamina>();
            combat = player.GetComponent<PlayerCombatController>();
            body = player.GetComponent<Rigidbody2D>();
        }

        private void Rebuild()
        {
            text.Length = 0;
            text.Append("DEBUG PANEL [F3]\n\n");
            // Performance
            float sum = 0f, worst = 0f;
            for (int i = 0; i < frameCount; i++) { sum += frameTimes[i]; worst = Mathf.Max(worst, frameTimes[i]); }
            float average = frameCount > 0 ? sum / frameCount : 0f;
            text.Append("FPS ").Append(average > 0f ? Mathf.RoundToInt(1f / average) : 0)
                .Append("   frame ").Append((average * 1000f).ToString("F1")).Append(" ms   worst ").Append((worst * 1000f).ToString("F1")).Append(" ms\n");
            text.Append("CPU main ").Append(Ms(mainThread)).Append("   GC alloc/frame ").Append(Bytes(gcAllocInFrame)).Append('\n');
            text.Append("batches ").Append(Count(batches)).Append("   setpass ").Append(Count(setPass)).Append("   tris ").Append(Count(triangles)).Append('\n');
            text.Append("memory ").Append(Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024)).Append(" MB   managed ")
                .Append(Profiler.GetMonoUsedSizeLong() / (1024 * 1024)).Append(" MB   GC runs ").Append(System.GC.CollectionCount(0)).Append('\n');
            text.Append("vsync ").Append(QualitySettings.vSyncCount).Append("   target ").Append(Application.targetFrameRate)
                .Append("   ").Append(Screen.width).Append('x').Append(Screen.height).Append("   timeScale ").Append(Time.timeScale.ToString("0.##")).Append('\n');

            // World
            var scene = SceneManager.GetActiveScene();
            text.Append("\nscene ").Append(scene.name).Append("   t ").Append(Time.timeSinceLevelLoad.ToString("F0")).Append(" s");
            var cam = Camera.main;
            if (cam != null) text.Append("   zoom ").Append(cam.orthographicSize.ToString("F2"));
            text.Append('\n');
            if (enemyRefreshIn <= 0f)
            {
                enemyRefreshIn = 1f;
                enemiesAlive = enemiesTotal = 0;
                foreach (var enemy in EnemyRegistry.Active)
                {
                    enemiesTotal++;
                    if (!enemy.IsDefeated) enemiesAlive++;
                }
            }
            text.Append("enemies ").Append(enemiesAlive).Append('/').Append(enemiesTotal).Append(" alive\n");

            // Player
            FindPlayer();
            if (player != null)
            {
                Vector2 position = player.transform.position;
                text.Append("\nplayer (").Append(position.x.ToString("F2")).Append(", ").Append(position.y.ToString("F2"))
                    .Append(")  cell ").Append(Mathf.FloorToInt(position.x)).Append(',').Append(Mathf.FloorToInt(position.y)).Append('\n');
                if (body != null) text.Append("speed ").Append(body.linearVelocity.magnitude.ToString("F2")).Append(" u/s");
                if (movement != null && movement.IsSprinting) text.Append("  SPRINT");
                text.Append('\n');
                if (player.Health != null) text.Append("HP ").Append(player.Health.Health).Append('/').Append(player.Health.MaxHealth);
                if (stamina != null) text.Append("   STA ").Append(Mathf.FloorToInt(stamina.Current)).Append('/').Append(Mathf.RoundToInt(stamina.Maximum));
                text.Append('\n');
                if (combat != null)
                {
                    var weapon = combat.Weapon;
                    text.Append("axe ").Append(weapon != null ? weapon.State.ToString() : "none");
                    if (weapon != null && weapon.ComboStep > 0) text.Append("  combo ").Append(weapon.ComboStep);
                    text.Append("   recall ").Append(combat.CanRecall ? "unlocked" : "locked").Append('\n');
                }
            }

            // Save
            var session = GameSession.Instance;
            if (session != null)
            {
                var progress = session.Progress;
                text.Append("\nshards ").Append(progress.sunShards).Append("   milestones ").Append(progress.completedIds.Count)
                    .Append("   checkpoint ").Append(string.IsNullOrEmpty(progress.checkpointId) ? "none" : progress.checkpointId).Append('\n');
            }
            text.Append("F3 hide");
            readout.text = text.ToString();
        }

        private static string Ms(ProfilerRecorder recorder)
        {
            if (!recorder.Valid || recorder.Count == 0) return "-";
            double total = 0; int samples = recorder.Count;
            for (int i = 0; i < samples; i++) total += recorder.GetSample(i).Value;
            return (total / samples * 1e-6).ToString("F1") + " ms";
        }

        private static string Count(ProfilerRecorder recorder) => recorder.Valid ? recorder.LastValue.ToString() : "-";
        private static string Bytes(ProfilerRecorder recorder) => !recorder.Valid ? "-" :
            recorder.LastValue >= 1024 ? (recorder.LastValue / 1024f).ToString("F1") + " KB" : recorder.LastValue + " B";

    }
}
