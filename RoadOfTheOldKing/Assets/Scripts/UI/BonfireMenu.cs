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
    // session validates everything. Pages: main, upgrade choices and purchase confirmation. Escape steps back a page and closes from the main page.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerBonfireInteraction))]
    public sealed class BonfireMenu : MonoBehaviour, IModalMenu
    {
        private enum Page { Main, Upgrades, Confirm }

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
            return true;
        }

        private void Open(Bonfire opened)
        {
            if (!Build()) return;
            fire = opened;
            overlay.style.display = DisplayStyle.Flex;
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
            fire = null;
        }

        public void Back()
        {
            switch (page)
            {
                case Page.Main: interaction.Close(); break;
                case Page.Confirm: Show(Page.Upgrades); break;
                default: Show(Page.Main); break;
            }
        }

        public void Navigate(int direction) => list?.Navigate(direction);
        public void Submit() => list?.Submit();

        private void Show(Page next)
        {
            var session = GameSession.Instance;
            if (session == null || fire == null) { interaction.Close(); return; }
            page = next;
            shards.text = "Sun Shards " + session.Progress.sunShards;
            var items = new List<PauseMenuAction>();
            switch (next)
            {
                case Page.Main:
                    heading.text = fire.DisplayName.ToUpperInvariant();
                    status.text = session.Status;
                    items.Add(new PauseMenuAction("Rest", "Heal, refill flasks, save.", () => { session.Checkpoints.Rest(fire); Show(Page.Main); }));
                    items.Add(new PauseMenuAction("Upgrades", "View the next choices.", () => Show(Page.Upgrades)));
                    bool travel = false;
                    // Every discovered fire, including those in other scenes; names refresh when rested at.
                    foreach (var destination in session.Progress.fires.OrderBy(f => f.displayOrder))
                    {
                        if (destination.id == fire.Id) continue;
                        travel = true;
                        var target = destination;
                        string name = !string.IsNullOrEmpty(target.displayName) ? target.displayName :
                            session.Checkpoints.Fires.FirstOrDefault(f => f.Id == target.id)?.DisplayName ?? target.id;
                        items.Add(new PauseMenuAction(name, "Travel here.",
                            () => { if (session.Checkpoints.Travel(target, fire)) interaction.Close(); }));
                    }
                    if (!travel) items.Add(new PauseMenuAction("Travel", "Light another fire first.", null, false));
                    items.Add(new PauseMenuAction("Leave", "Back to the road.", interaction.Close));
                    break;

                case Page.Upgrades:
                    heading.text = "UPGRADES";
                    var tier = session.Rewards.Upgrades.NextTier;
                    if (tier == null)
                    {
                        status.text = "All chosen.";
                        foreach (var chosen in session.Rewards.Upgrades.Selected)
                            items.Add(new PauseMenuAction(chosen.displayName, chosen.description, null, false));
                    }
                    else
                    {
                        status.text = "Choose one for " + tier.shardCost + " shards.";
                        foreach (var choice in tier.choices)
                        {
                            if (choice == null) continue;
                            var picked = choice;
                            items.Add(new PauseMenuAction(choice.displayName,
                                choice.description, () => { pending = picked; Show(Page.Confirm); }));
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
                    // The safe choice comes first so Enter never buys by accident.
                    items.Add(new PauseMenuAction("Back", "Keep your shards.", () => Show(Page.Upgrades)));
                    if (current != null && pending != null)
                        items.Add(new PauseMenuAction("Buy (" + current.shardCost + ")", pending.description,
                            () => { session.Rewards.TryPurchaseUpgrade(fire, pending); pending = null; Show(Page.Upgrades); status.text = session.Status; },
                            fire.AllowsUpgrades && session.Progress.sunShards >= current.shardCost));
                    break;

            }
            list.Show(items);
        }
    }
}
