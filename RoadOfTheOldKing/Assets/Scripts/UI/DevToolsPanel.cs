#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
using System;
using System.Collections.Generic;
using System.IO;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
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
        private VisualElement overlay, playerPage, itemsPage, travelPage, destinations;
        private Label summary, status, marker, inventory;
        private Label effects;
        private Toggle god, infinite, collision, pauseToggle;
        private Slider speed, time;
        private FloatField x, y;
        private TextField travelSearch;
        private DevTravelDestination[] travelEntries = Array.Empty<DevTravelDestination>();
        private readonly List<VisualElement> destinationControls = new List<VisualElement>();
        private readonly List<VisualElement> sessionControls = new List<VisualElement>();
        private readonly Dictionary<WorldTool, Button> toolButtons = new Dictionary<WorldTool, Button>();
        private readonly List<VisualElement> aliveControls = new List<VisualElement>();
        private readonly List<Button> tabs = new List<Button>();
        private PlayerControlLocks lockedPlayer;
        private IDisposable pause;
        private bool releasePending;
        private int closedFrame;
        private float refreshAt;
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
            status = root.Q<Label>("result");
            effects = root.Q<Label>("effects");
            root.Q<Button>("close").clicked += Close;
            root.Q<Button>("reset").clicked += () => Run(() => commands.ResetOverrides());
            pauseToggle = root.Q<Toggle>("pause"); pauseToggle.SetValueWithoutNotify(true);
            pauseToggle.RegisterValueChangedCallback(e => SetPauseWhileOpen(e.newValue));
            playerPage = root.Q("player-page"); itemsPage = root.Q("items-page"); travelPage = root.Q("travel-page");
            foreach (string title in new[] { "Player", "Items", "Travel" })
            {
                var button = new Button(() => SelectTab(title)) { text = title };
                button.AddToClassList("tab"); root.Q("tabs").Add(button); tabs.Add(button);
            }
            BuildPlayer(); BuildItems(); BuildTravel(); SelectTab("Player");
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
                if (commands.HasPlayer && !commands.Alive && currentTab != "Player") SelectTab("Player");
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
            RebuildDestinations(); Refresh();
            tabs.Find(t => t.text == currentTab)?.Focus();
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
            var row = Row(playerPage);
            AddButton(row, "Restore all", commands.RestorePlayer, true).tooltip = "Fill health, stamina and flasks.";
            AddButton(row, "Respawn", commands.Respawn).tooltip = "Reload at the checkpoint; keep progress.";
            row = Row(playerPage);
            SessionButton(row, "Give axe", commands.GiveAxe);
            SessionButton(row, "Unlock Recall", commands.UnlockRecall);
            Section(playerPage, "Overrides");
            god = AddToggle(playerPage, "God mode", commands.SetGodMode);
            infinite = AddToggle(playerPage, "Infinite stamina", commands.SetInfiniteStamina);
            collision = AddToggle(playerPage, "No collision", value => commands.SetCollision(!value));
            speed = AddSlider(playerPage, "Move speed", .25f, 4f, 1f, commands.SetSpeed);
            var testing = new Foldout { text = "Combat & simulation", value = false }; playerPage.Add(testing);
            row = Row(testing);
            var damage = new IntegerField("Damage") { value = 25, isDelayed = true }; damage.AddToClassList("amount"); row.Add(damage);
            AddButton(row, "Hit player", () => commands.DamagePlayer(damage.value), true);
            AddButton(row, "Kill", () => { Close(); return commands.KillPlayer(); }, true, "danger");
            row = Row(testing);
            AddButton(row, "Empty stamina", () => commands.SetStamina(0), true);
            AddButton(row, "Return axe", commands.RetrieveAxe, true);
            time = AddSlider(testing, "Game speed", .1f, 3f, 1f, commands.SetGameSpeed);
            var radius = new FloatField("Enemy radius") { value = 15, isDelayed = true }; testing.Add(radius);
            row = Row(testing);
            AddButton(row, "Kill nearby", () => commands.KillNearby(radius.value), true, "danger").tooltip = "Runs normal reward and save events.";
            AddButton(row, "Reset enemies", commands.ResetEnemies);
        }
        private void BuildItems()
        {
            Hint(itemsPage, "Grants are saved to the current game.");
            inventory = Hint(itemsPage, "");
            ResourceRow("Bronze Coins", 100, commands.GiveCoins);
            ResourceRow("Sun Shards", 5, commands.GiveShards);
            ResourceRow("Heart fragments", 3, commands.GiveHeartFragments);
            Section(itemsPage, "Tools");
            foreach (WorldTool tool in Enum.GetValues(typeof(WorldTool)))
            {
                if (tool == WorldTool.None) continue;
                var item = tool;
                toolButtons.Add(tool, SessionButton(itemsPage, "Give " + WorldToolNames.Display(tool), () => commands.GiveTool(item)));
            }
        }
        private void ResourceRow(string label, int amount, Func<int, bool> grant)
        {
            var row = Row(itemsPage);
            var field = new IntegerField(label) { value = amount, isDelayed = true };
            field.AddToClassList("resource-amount"); row.Add(field);
            SessionButton(row, "Give", () => grant(field.value)).AddToClassList("give");
        }
        private void BuildTravel()
        {
            travelSearch = new TextField("Search"); travelPage.Add(travelSearch);
            travelSearch.RegisterValueChangedCallback(_ => ShowDestinations());
            destinations = new VisualElement(); travelPage.Add(destinations);
            Hint(travelPage, "No rest or checkpoint change. Scene changes save progress.");
            var coordinates = new Foldout { text = "Marker & coordinates", value = false }; travelPage.Add(coordinates);
            var row = Row(coordinates);
            AddButton(row, "Set marker", commands.SetMarker, true);
            AddButton(row, "Return to marker", commands.TeleportToMarker, true);
            marker = Hint(coordinates, "No marker set");
            row = Row(coordinates);
            x = new FloatField("X") { isDelayed = true }; y = new FloatField("Y") { isDelayed = true };
            row.Add(x); row.Add(y);
            row = Row(coordinates);
            AddButton(row, "Use current", () => { if (!commands.HasPlayer) return false; var p = commands.Player.transform.position; x.value = p.x; y.value = p.y; return true; }, true);
            AddButton(row, "Teleport", () => commands.Teleport(new Vector2(x.value, y.value)), true);
        }
        private void RebuildDestinations()
        {
            travelEntries = commands.GetDestinations();
            ShowDestinations();
        }
        private void ShowDestinations()
        {
            destinations.Clear(); destinationControls.Clear();
            string scene = null;
            foreach (var entry in travelEntries)
            {
                string sceneName = Path.GetFileNameWithoutExtension(entry.scenePath);
                string query = travelSearch.value.Trim();
                if (query.Length > 0 && (entry.label + " " + sceneName).IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (scene != entry.scenePath)
                {
                    scene = entry.scenePath;
                    Section(destinations, sceneName + (scene == SceneManager.GetActiveScene().path ? " (here)" : ""));
                }
                var destination = entry;
                destinationControls.Add(AddButton(destinations, entry.label, () => commands.TravelTo(destination)));
            }
            if (destinationControls.Count == 0) Hint(destinations, "No matching destinations.");
            Refresh();
        }
        private void SelectTab(string title)
        {
            currentTab = title;
            playerPage.style.display = title == "Player" ? DisplayStyle.Flex : DisplayStyle.None;
            itemsPage.style.display = title == "Items" ? DisplayStyle.Flex : DisplayStyle.None;
            travelPage.style.display = title == "Travel" ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var tab in tabs) tab.EnableInClassList("selected", tab.text == title);
            if (IsOpen) tabs.Find(t => t.text == title)?.Focus();
            if (title == "Travel") RebuildDestinations();
            document.rootVisualElement.Q<ScrollView>("scroll").scrollOffset = Vector2.zero;
        }
        private void Run(Func<bool> command) { command(); Refresh(); }
        private Button AddButton(VisualElement parent, string title, Func<bool> command, bool needsAlive = false, string css = null)
        {
            var button = new Button(() => Run(command)) { text = title };
            if (css != null) button.AddToClassList(css);
            parent.Add(button); if (needsAlive) aliveControls.Add(button); return button;
        }
        private Button SessionButton(VisualElement parent, string title, Func<bool> command)
        {
            var button = AddButton(parent, title, command);
            sessionControls.Add(button); return button;
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
        private static void Section(VisualElement parent, string title)
        {
            var label = new Label(title); label.AddToClassList("section-title"); parent.Add(label);
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
            if (!IsOpen) return;
            if (commands.HasPlayer)
            {
                var p = commands.Player; var pos = p.transform.position;
                summary.text = $"{SceneManager.GetActiveScene().name}   |   {(p.IsAlive ? "HP " + p.Health.Health + "/" + p.Health.MaxHealth : "Defeated")}   |   {pos.x:0.0}, {pos.y:0.0}";
            }
            else summary.text = "No player in this scene";
            var session = GameSession.Instance;
            bool ready = commands.Alive && (session == null || !session.IsLoading);
            foreach (var control in aliveControls) control.SetEnabled(ready);
            foreach (var control in sessionControls) control.SetEnabled(ready && session != null);
            foreach (var control in destinationControls) control.SetEnabled(ready && session != null);
            foreach (var entry in toolButtons)
            {
                bool owned = session != null && session.Progress.OwnsTool(entry.Key);
                entry.Value.text = WorldToolNames.Display(entry.Key) + (owned ? "  (owned)" : "  + Give");
                entry.Value.SetEnabled(ready && session != null && !owned);
            }
            inventory.text = session == null ? "No active game." :
                $"{session.Progress.bronzeCoins} coins   |   {session.Progress.sunShards} shards   |   {HeartFragmentProgression.Count(session.Progress)} fragments";
            speed.SetEnabled(ready);
            god.SetValueWithoutNotify(commands.GodMode); infinite.SetValueWithoutNotify(commands.InfiniteStamina);
            collision.SetValueWithoutNotify(commands.NoCollision);
            speed.SetValueWithoutNotify(commands.SpeedMultiplier); time.SetValueWithoutNotify(SimulationPause.UnpausedTimeScale);
            marker.text = commands.Marker.HasValue ? $"Marker: {commands.Marker.Value.x:0.0}, {commands.Marker.Value.y:0.0}" : "No marker set";
            status.text = commands.LastResult; status.EnableInClassList("error", !commands.LastSucceeded);
        }
    }
}
#endif
