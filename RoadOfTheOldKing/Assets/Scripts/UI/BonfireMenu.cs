using System.Collections.Generic;
using TheLostShrine.Player;
using TheLostShrine.Progression;
using TheLostShrine.Weapons;
using TheLostShrine.World;
using UnityEngine;
using UnityEngine.UIElements;

namespace TheLostShrine.UI
{
    // The bonfire's menu (UI Toolkit). Presentation only: it follows PlayerBonfireInteraction's
    // open/close events and calls CheckpointSession for resting, travel, upgrades and a new run; the
    // session validates everything. Pages: main, upgrade choices, purchase confirmation, new-run
    // confirmation. Escape steps back a page and closes from the main page.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerBonfireInteraction))]
    public sealed class BonfireMenu : MonoBehaviour, IModalMenu
    {
        private enum Page { Main, Upgrades, Confirm, NewRun }

        [SerializeField] private VisualTreeAsset layout;
        [SerializeField] private PanelSettings panelSettings;

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
            Show(Page.Main);
        }

        private void Hide()
        {
            MenuStack.Remove(this);
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
            var session = CheckpointSession.Instance;
            if (session == null || fire == null) { interaction.Close(); return; }
            page = next;
            shards.text = "Sun Shards  " + session.Progress.sunShards;
            var items = new List<PauseMenuAction>();
            switch (next)
            {
                case Page.Main:
                    heading.text = fire.DisplayName.ToUpperInvariant();
                    status.text = session.Status;
                    items.Add(new PauseMenuAction("Rest", "Restore health, stamina and flasks, reset enemies and save.", () => { session.Rest(fire); Show(Page.Main); }));
                    if (fire.AllowsUpgrades)
                        items.Add(new PauseMenuAction("Axe upgrades", "Spend Sun Shards on one of three upgrades.", () => Show(Page.Upgrades)));
                    bool travel = false;
                    foreach (var destination in session.Fires)
                    {
                        if (destination == null || destination == fire || !destination.IsDiscovered) continue;
                        travel = true;
                        var target = destination;
                        items.Add(new PauseMenuAction("Travel to " + target.DisplayName, "Rest and travel to this fire.",
                            () => { if (session.Travel(target, fire)) interaction.Close(); }));
                    }
                    if (!travel) items.Add(new PauseMenuAction("Travel", "Light another fire to unlock travel.", null, false));
                    items.Add(new PauseMenuAction("New run...", "Start over from the beginning. You will be asked first.", () => Show(Page.NewRun)));
                    items.Add(new PauseMenuAction("Leave", "Back to the road.", interaction.Close));
                    break;

                case Page.Upgrades:
                    heading.text = "AXE UPGRADE";
                    var tier = session.Upgrades.NextTier;
                    if (tier == null)
                    {
                        status.text = "All available upgrades chosen.";
                        foreach (var chosen in session.Upgrades.Selected)
                            items.Add(new PauseMenuAction(chosen.displayName, chosen.description, null, false));
                    }
                    else
                    {
                        bool affordable = session.Progress.sunShards >= tier.shardCost;
                        status.text = "Choose one for " + tier.shardCost + " Sun Shards. The other two are gone for this run.";
                        foreach (var choice in tier.choices)
                        {
                            if (choice == null) continue;
                            var picked = choice;
                            items.Add(new PauseMenuAction(choice.displayName + (affordable ? "" : "  (need " + tier.shardCost + ")"),
                                choice.description, () => { pending = picked; Show(Page.Confirm); }, affordable));
                        }
                    }
                    items.Add(new PauseMenuAction("Back", "Return to the fire.", () => Show(Page.Main)));
                    break;

                case Page.Confirm:
                    var current = session.Upgrades.NextTier;
                    heading.text = pending != null ? pending.displayName.ToUpperInvariant() : "AXE UPGRADE";
                    status.text = current == null || pending == null ? "" :
                        "Spend " + current.shardCost + " Sun Shards on " + pending.displayName + "? The other two choices are lost for this run.";
                    // The safe choice comes first so Enter never buys by accident.
                    items.Add(new PauseMenuAction("Back to choices", "Keep your shards for now.", () => Show(Page.Upgrades)));
                    if (current != null && pending != null)
                        items.Add(new PauseMenuAction("Spend " + current.shardCost + " shards", pending.description,
                            () => { session.TryPurchaseUpgrade(fire, pending); pending = null; Show(Page.Upgrades); status.text = session.Status; },
                            session.Progress.sunShards >= current.shardCost));
                    break;

                case Page.NewRun:
                    heading.text = "NEW RUN";
                    status.text = "Start over? This clears this journey's saved progress.";
                    items.Add(new PauseMenuAction("Keep playing", "Return to the fire.", () => Show(Page.Main)));
                    items.Add(new PauseMenuAction("Start new run", "Erase this save and begin again.", session.StartNewRun));
                    break;
            }
            list.Show(items);
        }
    }
}
