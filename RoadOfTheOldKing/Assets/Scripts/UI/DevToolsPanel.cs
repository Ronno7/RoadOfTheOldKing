#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
using System;
using System.Collections.Generic;
using TheLostShrine.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TheLostShrine.UI
{
    // F4 runtime workbench. Commands are independently callable through DevRuntimeCommands.Instance.
    [DefaultExecutionOrder(-300)]
    public sealed class DevToolsPanel : MonoBehaviour, IModalMenu
    {
        public static DevToolsPanel Instance { get; private set; }
        public static bool CapturesInput => Instance != null && (Instance.IsOpen || Instance.releasePending);
        public bool IsOpen { get; private set; }
        public bool PauseWhileOpen { get; private set; } = true;
        private DevRuntimeCommands commands;
        private UIDocument document;
        private PanelSettings settings;
        private VisualElement overlay, playerPage, worldPage, travelPage, fires;
        private Label summary, status, mode, active, speedReadout, nearby, marker;
        private Label effects;
        private Toggle god, infinite, collision, pauseToggle;
        private Slider speed, time;
        private FloatField x, y;
        private readonly List<VisualElement> aliveControls = new List<VisualElement>();
        private readonly List<Button> tabs = new List<Button>();
        private PlayerControlLocks lockedPlayer;
        private IDisposable pause;
        private bool releasePending;
        private int closedFrame;
        private float refreshAt;
        private float radius = 15f;
        private string currentTab = "Player";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => Instance = null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Instance != null) return;
            var host = new GameObject("Developer Tools (F4)");
            DontDestroyOnLoad(host);
            host.AddComponent<DevRuntimeCommands>();
            host.AddComponent<DevToolsPanel>();
        }
        private void Awake()
        {
            Instance = this;
            commands = GetComponent<DevRuntimeCommands>();
            var layout = Resources.Load<VisualTreeAsset>("DevTools/DevTools");
            var source = Resources.Load<PanelSettings>("DevTools/DevToolsPanel");
            if (layout == null || source == null)
            { Debug.LogError("Developer Tools needs its Resources/DevTools layout and panel settings."); enabled = false; return; }
            settings = Instantiate(source);
            document = gameObject.AddComponent<UIDocument>();
            document.panelSettings = settings; document.visualTreeAsset = layout;
            var root = document.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            overlay = root.Q("overlay"); summary = root.Q<Label>("summary");
            status = root.Q<Label>("result"); mode = root.Q<Label>("mode"); active = root.Q<Label>("active");
            effects = root.Q<Label>("effects");
            root.Q<Button>("close").clicked += Close;
            root.Q<Button>("reset").clicked += () => Run(() => commands.ResetOverrides());
            pauseToggle = root.Q<Toggle>("pause"); pauseToggle.SetValueWithoutNotify(true);
            pauseToggle.RegisterValueChangedCallback(e => SetPauseWhileOpen(e.newValue));
            playerPage = root.Q("player-page"); worldPage = root.Q("world-page"); travelPage = root.Q("travel-page");
            foreach (string title in new[] { "Player", "World", "Travel" })
            {
                var button = new Button(() => SelectTab(title)) { text = title };
                button.AddToClassList("tab"); root.Q("tabs").Add(button); tabs.Add(button);
            }
            BuildPlayer(); BuildWorld(); BuildTravel(); SelectTab("Player");
            commands.SceneChanged += OnSceneChanged;
            overlay.style.display = DisplayStyle.None;
        }
        private void Update()
        {
            if (Application.isFocused) PollInput();
            if (IsOpen)
            {
                LockPlayer();
                // Defeat must be allowed to present normally, including when invoked from automation.
                if (commands.HasPlayer && !commands.Alive && currentTab != "Travel") SelectTab("Travel");
            }
            if (releasePending && Time.frameCount > closedFrame && InputsReleased()) ReleaseInput();
            if (Time.unscaledTime >= refreshAt)
            {
                refreshAt = Time.unscaledTime + .15f;
                Refresh();
            }
            if (settings != null) settings.scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), .65f, 1.5f);
        }
        private void PollInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.f4Key.wasPressedThisFrame) { if (IsOpen) Close(); else Open(); }
            else if (IsOpen && keyboard.escapeKey.wasPressedThisFrame) Close();
        }
        public void Open()
        {
            if (overlay == null || IsOpen) return;
            IsOpen = true; releasePending = false;
            LockPlayer();
            commands.Combat?.Weapon?.CancelAction();
            commands.Player?.GetComponent<PlayerDash>()?.Cancel();
            MenuStack.Push(this);
            if (PauseWhileOpen) pause = SimulationPause.Acquire(true);
            overlay.style.display = DisplayStyle.Flex;
            effects.style.display = DisplayStyle.None;
            RebuildFires(); Refresh();
            tabs[0].Focus();
        }
        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            overlay.style.display = DisplayStyle.None;
            MenuStack.Remove(this);
            pause?.Dispose(); pause = null;
            // Don't let the click/key which closes the panel turn into an attack, drink or sprint.
            releasePending = true; closedFrame = Time.frameCount;
            document.rootVisualElement.focusController?.focusedElement?.Blur();
            Refresh();
        }
        public void SetPauseWhileOpen(bool value)
        {
            PauseWhileOpen = value;
            if (IsOpen && value && pause == null) pause = SimulationPause.Acquire(true);
            if (!value) { pause?.Dispose(); pause = null; }
            pauseToggle?.SetValueWithoutNotify(value);
            Refresh();
        }
        private void LockPlayer()
        {
            var target = commands.Player != null ? commands.Player.GetComponent<PlayerControlLocks>() : null;
            if (lockedPlayer == target) return;
            lockedPlayer?.Unlock(this); lockedPlayer = target; lockedPlayer?.Lock(this);
        }
        private void ReleaseInput() { lockedPlayer?.Unlock(this); lockedPlayer = null; releasePending = false; }
        private static bool InputsReleased()
        {
            var mouse = Mouse.current; var k = Keyboard.current;
            if (mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed)) return false;
            if (k == null) return true;
            return !(k.wKey.isPressed || k.aKey.isPressed || k.sKey.isPressed || k.dKey.isPressed ||
                k.upArrowKey.isPressed || k.downArrowKey.isPressed || k.leftArrowKey.isPressed || k.rightArrowKey.isPressed ||
                k.eKey.isPressed || k.qKey.isPressed || k.fKey.isPressed || k.rKey.isPressed || k.spaceKey.isPressed ||
                k.escapeKey.isPressed || k.enterKey.isPressed || k.f4Key.isPressed);
        }
        private void OnSceneChanged() { Close(); ReleaseInput(); }
        private void OnDisable()
        {
            Close(); ReleaseInput(); MenuStack.Remove(this);
            if (commands != null) commands.SceneChanged -= OnSceneChanged;
            if (Instance == this) Instance = null;
        }
        private void OnDestroy() { if (settings != null) Destroy(settings); }
        public void Back() => Close();
        // UI Toolkit owns arrows/Enter/Tab, including text fields, rather than MenuList's buttons.
        public void Navigate(int direction) { }
        public void Submit() { }

        private void BuildPlayer()
        {
            Section(playerPage, "HEALTH & RECOVERY", "Test hits use real feedback. Debug damage bypasses protection.");
            var row = Row(playerPage);
            var damage = new IntegerField("Damage") { value = 25, isDelayed = true }; damage.AddToClassList("amount"); row.Add(damage);
            AddButton(row, "Apply hit", () => commands.DamagePlayer(damage.value), true);
            AddButton(row, "Kill player", () => { Close(); return commands.KillPlayer(); }, true, "danger");
            row = Row(playerPage);
            AddButton(row, "Full heal", commands.HealPlayer, true);
            AddButton(row, "Refill flasks", commands.RefillFlasks, true);
            AddButton(playerPage, "Restore player", commands.RestorePlayer, true);
            Hint(playerPage, "Restore fills health, stamina and flasks, and clears stagger/dodge recovery.");
            Section(playerPage, "TEST OVERRIDES", "These stay active when you close the panel.");
            god = AddToggle(playerPage, "God mode", commands.SetGodMode);
            infinite = AddToggle(playerPage, "Infinite stamina", commands.SetInfiniteStamina);
            collision = AddToggle(playerPage, "Player collision", commands.SetCollision); collision.SetValueWithoutNotify(true);
            Section(playerPage, "MOVEMENT", "Scales walk, sprint and attack walking. Dodge distance stays authored.");
            speed = AddSlider(playerPage, "Speed multiplier", .25f, 4f, 1f, commands.SetSpeed);
            speedReadout = Hint(playerPage, "");
            row = Row(playerPage);
            foreach (float value in new[] { .5f, 1f, 2f, 4f }) AddButton(row, value + "x", () => commands.SetSpeed(value), true);
            Section(playerPage, "STAMINA & AXE", "Set stamina for exhaustion and recovery tests.");
            row = Row(playerPage);
            AddButton(row, "Empty", () => commands.SetStamina(0), true);
            AddButton(row, "Deficit", () => commands.SetStamina(-commands.Stamina.DeficitLimit), true);
            AddButton(row, "Full", () => commands.SetStamina(commands.Stamina.Maximum), true);
            AddButton(playerPage, "Return owned axe", commands.RetrieveAxe, true);
        }
        private void BuildWorld()
        {
            Section(worldPage, "SIMULATION", "Speed applies when the world resumes. Other pause owners are respected.");
            time = AddSlider(worldPage, "Game speed", .1f, 3f, 1f, commands.SetGameSpeed);
            var row = Row(worldPage);
            foreach (float value in new[] { .25f, .5f, 1f, 2f }) AddButton(row, value + "x", () => commands.SetGameSpeed(value));
            Section(worldPage, "ENCOUNTERS", "Radius is measured from the player in world units.");
            var radiusSlider = new Slider("Radius", 1, 50) { value = radius, showInputField = true };
            radiusSlider.RegisterValueChangedCallback(e => { radius = Mathf.Clamp(e.newValue, 1f, 50f); Refresh(); });
            worldPage.Add(radiusSlider);
            nearby = Hint(worldPage, "");
            AddButton(worldPage, "Kill nearby enemies", () => commands.KillNearby(radius), true, "danger");
            Hint(worldPage, "Runs real defeat events: rewards unlock and milestones may save.", "caution");
            AddButton(worldPage, "Reset all enemies", commands.ResetEnemies);
            Hint(worldPage, "Respawns enemies at their starting points. Saved progression stays completed.");
            Section(worldPage, "SESSION", "Overrides reset on respawn or scene change; nothing edits source assets.");
            AddButton(worldPage, "Reset all runtime overrides", commands.ResetOverrides);
        }
        private void BuildTravel()
        {
            Section(travelPage, "BONFIRES", "Teleport only: no rest, unlock, heal or checkpoint change.");
            fires = new VisualElement(); travelPage.Add(fires);
            AddButton(travelPage, "Refresh destinations", () => { RebuildFires(); return true; });
            Section(travelPage, "RETURN MARKER", "One temporary marker for this scene.");
            marker = Hint(travelPage, "No marker set");
            var row = Row(travelPage);
            AddButton(row, "Set here", commands.SetMarker, true);
            AddButton(row, "Return", commands.TeleportToMarker, true);
            Section(travelPage, "COORDINATES", "World position. Collision-off mode lets you explore blocked areas.");
            row = Row(travelPage);
            x = new FloatField("X") { isDelayed = true }; y = new FloatField("Y") { isDelayed = true };
            row.Add(x); row.Add(y);
            row = Row(travelPage);
            AddButton(row, "Use current", () => { if (!commands.HasPlayer) return false; var p = commands.Player.transform.position; x.value = p.x; y.value = p.y; return true; }, true);
            AddButton(row, "Teleport", () => commands.Teleport(new Vector2(x.value, y.value)), true);
            Section(travelPage, "RESPAWN", "Reload at the last checkpoint, or the start. Permanent progress is saved and kept.");
            AddButton(travelPage, "Respawn at checkpoint", commands.Respawn);
        }
        private void RebuildFires()
        {
            fires.Clear();
            var destinations = commands.GetBonfires();
            if (destinations.Length == 0) Hint(fires, "No bonfires in this scene.");
            foreach (var fire in destinations)
            {
                var destination = fire;
                AddButton(fires, fire.DisplayName, () => commands.TeleportToBonfire(destination));
            }
        }
        private void SelectTab(string title)
        {
            currentTab = title;
            playerPage.style.display = title == "Player" ? DisplayStyle.Flex : DisplayStyle.None;
            worldPage.style.display = title == "World" ? DisplayStyle.Flex : DisplayStyle.None;
            travelPage.style.display = title == "Travel" ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var tab in tabs) tab.EnableInClassList("selected", tab.text == title);
        }
        private void Run(Func<bool> command) { command(); Refresh(); }
        private Button AddButton(VisualElement parent, string title, Func<bool> command, bool needsAlive = false, string css = null)
        {
            var button = new Button(() => Run(command)) { text = title };
            if (css != null) button.AddToClassList(css);
            parent.Add(button); if (needsAlive) aliveControls.Add(button); return button;
        }
        private Toggle AddToggle(VisualElement parent, string label, Func<bool, bool> apply)
        {
            var control = new Toggle(label);
            control.RegisterValueChangedCallback(e => Run(() => apply(e.newValue)));
            parent.Add(control); aliveControls.Add(control); return control;
        }
        private Slider AddSlider(VisualElement parent, string label, float low, float high, float value, Func<float, bool> apply)
        {
            var control = new Slider(label, low, high) { value = value, showInputField = true };
            control.RegisterValueChangedCallback(e => Run(() => apply(e.newValue)));
            parent.Add(control); return control;
        }
        private static VisualElement Row(VisualElement parent) { var row = new VisualElement(); row.AddToClassList("row"); parent.Add(row); return row; }
        private static Label Hint(VisualElement parent, string text, string css = null)
        {
            var label = new Label(text); label.AddToClassList("hint"); if (css != null) label.AddToClassList(css); parent.Add(label); return label;
        }
        private static void Section(VisualElement parent, string title, string detail)
        {
            var label = new Label(title); label.AddToClassList("section-title"); parent.Add(label);
            if (!string.IsNullOrEmpty(detail)) Hint(parent, detail);
        }
        private void Refresh()
        {
            if (summary == null) return;
            var modifiers = new List<string>();
            if (commands.GodMode) modifiers.Add("GOD"); if (commands.InfiniteStamina) modifiers.Add("STAMINA");
            if (commands.NoCollision) modifiers.Add("NO COLLISION");
            if (!Mathf.Approximately(commands.SpeedMultiplier, 1)) modifiers.Add($"MOVE {commands.SpeedMultiplier:0.##}x");
            if (!Mathf.Approximately(SimulationPause.UnpausedTimeScale, 1)) modifiers.Add($"TIME {SimulationPause.UnpausedTimeScale:0.##}x");
            string flags = modifiers.Count > 0 ? string.Join(" · ", modifiers) : "No runtime overrides";
            effects.text = flags;
            effects.style.display = !IsOpen && modifiers.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            active.text = flags;
            mode.text = SimulationPause.IsPaused ? "PAUSED" : "LIVE"; mode.EnableInClassList("live", !SimulationPause.IsPaused);
            if (!IsOpen) return;
            if (commands.HasPlayer)
            {
                var p = commands.Player; var pos = p.transform.position;
                summary.text = $"{(p.IsAlive ? "PLAYER" : "DEFEATED")}   HP {p.Health.Health}/{p.Health.MaxHealth}   STA {commands.Stamina.Current:0}/{commands.Stamina.Maximum:0}\n" +
                    $"Flasks {commands.Flask.Charges}/{commands.Flask.MaxCharges}   Position {pos.x:0.0}, {pos.y:0.0}";
            }
            else summary.text = "No player in this scene";
            foreach (var control in aliveControls) control.SetEnabled(commands.Alive);
            speed.SetEnabled(commands.HasPlayer);
            god.SetValueWithoutNotify(commands.GodMode); infinite.SetValueWithoutNotify(commands.InfiniteStamina);
            collision.SetValueWithoutNotify(!commands.NoCollision);
            speed.SetValueWithoutNotify(commands.SpeedMultiplier); time.SetValueWithoutNotify(SimulationPause.UnpausedTimeScale);
            speedReadout.text = commands.Movement != null ? $"Walk {commands.Movement.MoveSpeed:0.##} u/s · Sprint {commands.Movement.SprintSpeed:0.##} u/s" : "";
            nearby.text = $"{commands.NearbyCount(radius)} living enemies within {radius:0} units";
            marker.text = commands.Marker.HasValue ? $"Marker: {commands.Marker.Value.x:0.0}, {commands.Marker.Value.y:0.0}" : "No marker set";
            status.text = commands.LastResult; status.EnableInClassList("error", !commands.LastSucceeded);
        }
    }
}
#endif
