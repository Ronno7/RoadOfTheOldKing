using System.Collections.Generic;
using System.Linq;
using RoadOfTheOldKing.Cameras;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.Weapons;
using RoadOfTheOldKing.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // The bonfire's menu (UI Toolkit). Presentation only: it follows PlayerBonfireInteraction's
    // open/close events and calls GameSession for resting, travel and upgrades; the
    // session validates everything. Pages: main, upgrade choices and purchase confirmation, and travel by region then fire
    // (as in Dark Souls 3). Escape steps back a page and closes from the main page.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerBonfireInteraction))]
    public sealed class BonfireMenu : MonoBehaviour, IModalMenu
    {
        private enum Page { Main, Upgrades, Confirm, Travel, Region }

        [SerializeField] private VisualTreeAsset layout;
        [SerializeField] private PanelSettings panelSettings;
        [Header("Camera close-up")]
        [Tooltip("Camera size (half the visible height) while the menu is open.")]
        [SerializeField, Min(.5f)] private float closeUpSize = 3.5f;
        [Tooltip("Where the hero sits on screen while the menu is open (viewport 0..1): the left third, menu on the right.")]
        [SerializeField] private Vector2 heroOnScreen = new Vector2(1f / 3f, .45f);

        private PlayerBonfireInteraction interaction;
        private UIDocument document;
        private VisualElement overlay;
        private Label heading, status, shards;
        private MenuList list;
        private Bonfire fire;
        private AxeUpgrade pending;
        private Page page;
        private string region;
        // Row order on the main page; Back from travel returns to the Travel row. Only an upgrade fire lists Upgrades
        // (user, 18 Nov: "upgrade is taken out of the menu screen of the regular bonfires to avoid player confusion").
        private const int UpgradesRow = 0;
        private int TravelRow => fire != null && fire.AllowsUpgrades ? 1 : 0;
        // The upgrade fire's Upgrades row pulses between sun gold and pale flame gold.
        private const long GlowMilliseconds = 700;

        public bool IsOpen => overlay != null && overlay.style.display.value != DisplayStyle.None;

        private void Awake() => interaction = GetComponent<PlayerBonfireInteraction>();

        private void OnEnable()
        {
            interaction.Opened += Open;
            interaction.Closed += Hide;
        }

        private void OnDisable()
        {
            interaction.Opened -= Open;
            interaction.Closed -= Hide;
            Hide();
        }

        private void OnDestroy() { if (document != null) Destroy(document.gameObject); }

        private bool Build()
        {
            if (document != null) return true;
            if (layout == null || panelSettings == null) { Debug.LogError("Bonfire menu needs its layout and panel settings.", this); return false; }
            var child = new GameObject("Bonfire menu UI"); child.SetActive(false); child.transform.SetParent(transform, false);
            document = child.AddComponent<UIDocument>(); document.panelSettings = panelSettings; document.visualTreeAsset = layout;
            child.SetActive(true);
            overlay = document.rootVisualElement.Q("bonfire-overlay");
            heading = overlay.Q<Label>("heading"); status = overlay.Q<Label>("status"); shards = overlay.Q<Label>("shards");
            list = new MenuList(overlay.Q("actions"), overlay.Q<Label>("description"), action => action.Execute());
            overlay.schedule.Execute(() => overlay.Query(className: "upgrade-row").ForEach(row => row.ToggleInClassList("glow")))
                .Every(GlowMilliseconds);
            return true;
        }

        private void Open(Bonfire opened)
        {
            if (!Build()) return;
            fire = opened;
            // Regional skin: the same layout and kit rows inside this fire's frame.
            var panel = overlay.Q("bonfire-panel");
            foreach (var skin in panel.GetClasses().Where(c => c.StartsWith("skin-")).ToList()) panel.RemoveFromClassList(skin);
            panel.AddToClassList("skin-" + fire.MenuSkin);
            // An upgrade fire's own frame and crest, bronze and gold (user, 18 Nov: "a unique border for the upgrade
            // bonfire UI").
            panel.EnableInClassList("upgrade-fire", fire.AllowsUpgrades);
            overlay.style.display = DisplayStyle.Flex;
            ControlGlyphs.Bind(overlay.Q<Label>("hint"), "");
            MenuStack.Push(this);
            FrameCamera(true);
            Show(Page.Main);
        }

        // The camera zooms in and puts the resting hero on the left third, leaving the right for the panel.
        private void FrameCamera(bool frame)
        {
            var view = Camera.main;
            if (view == null) return;
            var follow = view.GetComponent<CameraFollow2D>();
            var zoom = view.GetComponent<CameraZoom2D>();
            if (frame)
            {
                if (follow != null) follow.SetFraming(this, heroOnScreen);
                if (zoom != null) zoom.SetOverride(this, closeUpSize);
            }
            else
            {
                if (follow != null) follow.ClearFraming(this);
                if (zoom != null) zoom.ClearOverride(this);
            }
        }

        private void Hide()
        {
            MenuStack.Remove(this);
            FrameCamera(false);
            if (overlay != null) overlay.style.display = DisplayStyle.None;
            pending = null;
            region = null;
            fire = null;
        }

        public void Back()
        {
            switch (page)
            {
                case Page.Main: interaction.Close(); break;
                case Page.Confirm: Show(Page.Upgrades); break;
                case Page.Region: Show(Page.Travel, RegionRow(region)); break;
                case Page.Travel: Show(Page.Main, TravelRow); break;
                default: Show(Page.Main, UpgradesRow); break;
            }
        }

        public void Navigate(int direction) => list?.Navigate(direction);
        public void Submit() => list?.Submit();

        // Discovered fires other than this one, by region in world order (each region by its first fire's place in
        // the menu order). A record from an older save has no region until its scene loads again: it joins a region
        // known for the same scene, else the scene's own name.
        private List<(string name, List<FireRecord> fires)> Destinations(GameSession session)
        {
            var known = session.Progress.fires.Where(f => !string.IsNullOrEmpty(f.region))
                .GroupBy(f => f.scenePath).ToDictionary(g => g.Key, g => g.First().region);
            return session.Progress.fires.Where(f => f.id != fire.Id)
                .GroupBy(f => !string.IsNullOrEmpty(f.region) ? f.region :
                    known.TryGetValue(f.scenePath, out var name) ? name : System.IO.Path.GetFileNameWithoutExtension(f.scenePath))
                .OrderBy(g => g.Min(f => f.displayOrder))
                .Select(g => (g.Key, g.OrderBy(f => f.displayOrder).ToList())).ToList();
        }

        private int RegionRow(string name)
        {
            var session = GameSession.Instance;
            int row = session == null || fire == null ? -1 : Destinations(session).FindIndex(r => r.name == name);
            return Mathf.Max(0, row);
        }

        private void Show(Page next, int select = 0)
        {
            var session = GameSession.Instance;
            if (session == null || fire == null) { interaction.Close(); return; }
            page = next;
            // Shards against the next tier's cost, red while short; the choices stay browsable but locked
            // until the shards are there and the fire allows purchases (user, 17 Oct: make that apparent).
            var nextTier = session.Rewards.Upgrades.NextTier;
            int have = session.Progress.sunShards;
            bool lacking = nextTier != null && have < nextTier.shardCost;
            bool locked = nextTier != null && (lacking || !fire.AllowsUpgrades);
            string requirement = nextTier == null ? (have > 0 ? "All chosen. Keep spare shards for the road ahead." : "All chosen.") :
                !fire.AllowsUpgrades ? "Choose upgrades at an upgrade bonfire." :
                lacking ? "Need " + nextTier.shardCost + " Sun Shards." :
                "Choose one for " + nextTier.shardCost + " shards.";
            shards.text = "Sun Shards " + have + (nextTier != null ? " / " + nextTier.shardCost : "");
            shards.EnableInClassList("lacking", lacking);
            // Shards only matter where they can be spent.
            shards.style.display = next == Page.Travel || next == Page.Region || !fire.AllowsUpgrades ? DisplayStyle.None : DisplayStyle.Flex;
            status.EnableInClassList("warning", false);
            var items = new List<PauseMenuAction>();
            switch (next)
            {
                case Page.Main:
                    heading.text = fire.DisplayName.ToUpperInvariant();
                    // Sitting down already rested (PlayerBonfireInteraction.Open), as at a Souls bonfire,
                    // so the menu offers only what resting unlocks. Saving shows as the HUD's corner flame, not
                    // words (user, 19 Nov); the line speaks only when a save failed.
                    status.text = session.SaveFailed ? session.Status : "";
                    if (fire.AllowsUpgrades)
                        items.Add(new PauseMenuAction("Upgrades", locked || nextTier == null ? requirement : "",
                            () => Show(Page.Upgrades), locked: locked)
                        // Gold at an upgrade fire; it pulses only when an upgrade can be bought (user, 19 Nov).
                        { StyleClass = locked || nextTier == null ? "upgrade-row" : "upgrade-row available" });
                    bool travel = Destinations(session).Count > 0;
                    items.Add(new PauseMenuAction("Travel", travel ? "" : "Light another fire first.",
                        () => Show(Page.Travel), travel));
                    items.Add(new PauseMenuAction("Leave", "", interaction.Close));
                    break;

                case Page.Travel:
                    heading.text = "TRAVEL";
                    status.text = "";
                    foreach (var destination in Destinations(session))
                    {
                        string name = destination.name;
                        items.Add(new PauseMenuAction(name, "", () => { region = name; Show(Page.Region); }));
                    }
                    items.Add(new PauseMenuAction("Back", "", () => Show(Page.Main, TravelRow)));
                    break;

                case Page.Region:
                    heading.text = (region ?? "").ToUpperInvariant();
                    status.text = "";
                    var fires = Destinations(session).FirstOrDefault(r => r.name == region).fires;
                    if (fires == null) { Show(Page.Travel); return; }
                    foreach (var destination in fires)
                    {
                        var target = destination;
                        string name = !string.IsNullOrEmpty(target.displayName) ? target.displayName :
                            session.Checkpoints.Fires.FirstOrDefault(f => f.Id == target.id)?.DisplayName ?? target.id;
                        items.Add(new PauseMenuAction(name, "",
                            () => { if (session.Checkpoints.Travel(target, fire)) interaction.Close(); }));
                    }
                    items.Add(new PauseMenuAction("Back", "", () => Show(Page.Travel, RegionRow(region))));
                    break;

                case Page.Upgrades:
                    heading.text = "UPGRADES";
                    var tier = session.Rewards.Upgrades.NextTier;
                    if (tier == null)
                    {
                        status.text = requirement;
                        foreach (var chosen in session.Rewards.Upgrades.Selected)
                            items.Add(new PauseMenuAction(chosen.displayName, chosen.description, null, false));
                    }
                    else
                    {
                        status.text = requirement;
                        status.EnableInClassList("warning", locked);
                        foreach (var choice in tier.choices)
                        {
                            if (choice == null) continue;
                            var picked = choice;
                            items.Add(new PauseMenuAction(choice.displayName,
                                choice.description, () => { pending = picked; Show(Page.Confirm); }, locked: locked));
                        }
                    }
                    items.Add(new PauseMenuAction("Back", "", () => Show(Page.Main)));
                    break;

                case Page.Confirm:
                    var current = session.Rewards.Upgrades.NextTier;
                    heading.text = pending != null ? pending.displayName.ToUpperInvariant() : "UPGRADES";
                    status.text = current == null || pending == null ? "" :
                        (!fire.AllowsUpgrades ? "Purchase at an upgrade bonfire." :
                        session.Progress.sunShards < current.shardCost ? "Need " + current.shardCost + " shards." :
                        "Spend " + current.shardCost + " shards? The others are lost.");
                    status.EnableInClassList("warning", locked);
                    // The safe choice comes first so Enter never buys by accident.
                    items.Add(new PauseMenuAction("Back", "", () => Show(Page.Upgrades)));
                    if (current != null && pending != null)
                        items.Add(new PauseMenuAction("Buy (" + current.shardCost + ")", pending.description,
                            () => { session.Rewards.TryPurchaseUpgrade(fire, pending); pending = null; Show(Page.Upgrades); status.text = session.Status; },
                            fire.AllowsUpgrades && session.Progress.sunShards >= current.shardCost));
                    break;

            }
            list.Show(items, Mathf.Clamp(select, 0, items.Count - 1));
        }
    }
}
