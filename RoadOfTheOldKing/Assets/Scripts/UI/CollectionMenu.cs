using System;
using System.Collections;
using System.Collections.Generic;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace RoadOfTheOldKing.UI
{
    // Inventory and trading share a view and modal lease, never a second copy of progression.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerHealth))]
    public sealed class CollectionMenu : MonoBehaviour, IModalMenu
    {
        [SerializeField] private VisualTreeAsset layout;
        [SerializeField] private PanelSettings panelSettings;
        private PlayerHealth player;
        private PlayerCombatController combat;
        private PlayerControlLocks locks;
        private UIDocument document;
        private VisualElement overlay;
        private Label heading, balances, context, hint;
        private MenuList list;
        private Trader trader;
        private ShopOffer confirming;
        private IDisposable pause;
        private Coroutine restoring;
        private int stockSelection;
        private bool trading;
        public bool IsOpen => pause != null;
        public bool CanOpen => isActiveAndEnabled && !IsOpen && restoring == null &&
            player != null && player.IsAlive && locks != null && !locks.IsLocked && !MenuStack.IsAnyOpen &&
            Time.timeScale > 0 && GameSession.Instance != null && !GameSession.Instance.IsLoading &&
            combat != null && combat.CanStartAttack && !combat.IsAttacking && combat.Weapon?.IsAway != true;

        private void Awake()
        {
            player = GetComponent<PlayerHealth>(); combat = GetComponent<PlayerCombatController>();
            locks = GetComponent<PlayerControlLocks>();
        }

        private void Update()
        {
            if (IsOpen && (player == null || !player.IsAlive || GameSession.Instance == null ||
                GameSession.Instance.IsLoading || (trading && (trader == null || !trader.CanTradeFrom(player))))) { Close(); return; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
            if (DevToolsPanel.CapturesInput) return;
#endif
            if (!Application.isFocused || Keyboard.current == null || !Keyboard.current.tabKey.wasPressedThisFrame) return;
            if (IsOpen) { if (trader == null) Close(); }
            else OpenInventory();
        }

        private bool Build()
        {
            if (document != null) return true;
            if (layout == null || panelSettings == null) return false;
            var child = new GameObject("Collection UI"); child.SetActive(false); child.transform.SetParent(transform, false);
            document = child.AddComponent<UIDocument>(); document.panelSettings = panelSettings; document.visualTreeAsset = layout;
            child.SetActive(true);
            overlay = document.rootVisualElement.Q("collection-overlay");
            // MenuStack owns keyboard navigation. Intercept before Button's built-in
            // submit handler; a bubbling callback is too late and would activate twice.
            overlay.RegisterCallback<NavigationSubmitEvent>(e => e.StopImmediatePropagation(), TrickleDown.TrickleDown);
            overlay.RegisterCallback<NavigationMoveEvent>(e => e.StopImmediatePropagation(), TrickleDown.TrickleDown);
            heading = overlay.Q<Label>("heading"); balances = overlay.Q<Label>("balances");
            context = overlay.Q<Label>("context"); hint = overlay.Q<Label>("hint");
            list = new MenuList(overlay.Q("entries"), overlay.Q<Label>("description"), action => { if (IsOpen) action.Execute(); });
            return true;
        }

        private bool Begin(Trader source)
        {
            if (!CanOpen || !Build()) return false;
            trader = source; trading = source != null; confirming = null; stockSelection = 0;
            locks.Lock(this); pause = SimulationPause.Acquire(); MenuStack.Push(this);
            overlay.style.display = DisplayStyle.Flex;
            GameSession.Instance.RecordResume();
            return true;
        }

        public bool OpenInventory()
        {
            if (!Begin(null)) return false;
            ShowInventory(); return true;
        }

        public bool OpenTrader(Trader source)
        {
            if (source == null || !source.CanTradeFrom(player) || !Begin(source)) return false;
            ShowStock(); return true;
        }

        private void RefreshBalances()
        {
            var state = GameSession.Instance.Progress;
            int fragments = HeartFragmentProgression.Count(state);
            balances.text = state.bronzeCoins + " Bronze Coins    " + state.sunShards + " Sun Shards    " +
                fragments % HeartFragmentProgression.FragmentsPerHeart + "/3 Fragments";
        }

        private void ShowInventory()
        {
            heading.text = "YOUR PACK"; RefreshBalances();
            var state = GameSession.Instance.Progress;
            context.text = "Health " + player.Health.Health + "/" + player.Health.MaxHealth + "  ·  Tools are used where needed.";
            hint.text = "↑ ↓ Browse    Tab / Esc Close";
            var rows = new List<PauseMenuAction>();
            if (state.hasAxe) rows.Add(new PauseMenuAction("Axe", state.recallUnlocked
                ? "Your axe. Throw it, strike with it, and call it back." : "Your axe. Throw it or strike at close range.", null));
            var flask = GetComponent<PlayerFlask>();
            if (flask != null) rows.Add(new PauseMenuAction("Flasks  " + flask.Charges + "/" + flask.MaxCharges,
                "Drink with " + ControlLabels.Get("heal") + " to recover health. Rest at a bonfire to refill.", null));
            foreach (WorldTool tool in Enum.GetValues(typeof(WorldTool)))
                if (tool != WorldTool.None && state.OwnsTool(tool))
                    rows.Add(new PauseMenuAction(WorldToolNames.Display(tool), ToolDescription(tool), null));
            foreach (var upgrade in GameSession.Instance.Rewards.Upgrades.Selected)
                rows.Add(new PauseMenuAction(upgrade.displayName, upgrade.description, null));
            int lore = ForgeInscriptionProgression.Count(state);
            if (lore > 0) rows.Add(new PauseMenuAction("Smiths' knowledge  " + Math.Min(lore, ForgeInscriptionProgression.RequiredKnowledge) + "/" + ForgeInscriptionProgression.RequiredKnowledge,
                "The measures you have learned from the old smiths' carvings.", null));
            rows.Add(new PauseMenuAction("Close", "", Close)); list.Show(rows);
        }

        private static string ToolDescription(WorldTool tool)
        {
            switch (tool)
            {
                case WorldTool.RopeKit: return "A reusable rope kit. Secure a crossing at a suitable anchor.";
                case WorldTool.MaintenanceCrank: return "A bronze crank for compatible old machinery.";
                case WorldTool.ProtectedLantern: return "A sheltered light for dark passages.";
                default: return "";
            }
        }

        private string OfferDescription(ShopOffer offer) => offer.Reward == ShopReward.Tool ? ToolDescription(offer.Tool) :
            "One heart fragment. Three fragments increase maximum health by 20. Only one is available here.";

        private void ShowStock(string receipt = null)
        {
            confirming = null; heading.text = "TRAVELING TRADER"; RefreshBalances();
            context.text = receipt ?? "A little rope can make a long road shorter.";
            hint.text = "↑ ↓ Browse    Enter Select    Esc Leave";
            var rows = new List<PauseMenuAction>();
            foreach (var offer in trader.Offers)
            {
                if (offer == null || offer.Merchant != trader) continue;
                int row = rows.Count;
                bool sold = offer.IsSold(GameSession.Instance.Progress);
                string label = offer.ItemName + (sold ? "  ·  Sold out" : "  ·  " + offer.Price + " coins");
                string detail = OfferDescription(offer);
                if (sold) detail += "\nSold out.";
                rows.Add(new PauseMenuAction(label, detail, () => { stockSelection = row; ShowConfirmation(offer); }));
            }
            rows.Add(new PauseMenuAction("Leave", "Safe travels.", Close)); list.Show(rows, stockSelection);
        }

        private void ShowConfirmation(ShopOffer offer)
        {
            confirming = offer; heading.text = offer.ItemName.ToUpperInvariant(); RefreshBalances();
            var state = GameSession.Instance.Progress;
            bool sold = offer.IsSold(state);
            bool affordable = state.bronzeCoins >= offer.Price;
            context.text = sold ? "Sold out." : affordable ? "Purchase for " + offer.Price + " Bronze Coins?"
                : "You need " + (offer.Price - state.bronzeCoins) + " more Bronze Coins.";
            hint.text = "↑ ↓ Choose    Enter Select    Esc Back";
            list.Show(new[]{
                new PauseMenuAction("Back", OfferDescription(offer), () => ShowStock()),
                new PauseMenuAction("Buy  ·  " + offer.Price + " coins", OfferDescription(offer), () => Buy(offer),
                    !sold && affordable && offer.CanCollect(player))
            }); // Back is the safe default; opening or browsing never spends money.
        }

        private void Buy(ShopOffer offer)
        {
            if (!IsOpen || trader == null || offer != confirming || offer.Merchant != trader || !trader.CanTradeFrom(player)) return;
            if (!offer.TryCollect(player)) { ShowConfirmation(offer); return; }
            ShowStock("Purchased " + offer.ItemName.ToLowerInvariant() + ".");
        }

        public void Back() { if (confirming != null) ShowStock(); else Close(); }
        public void Navigate(int direction) { if (IsOpen) list.Navigate(direction); }
        public void Submit() { if (IsOpen) list.Submit(); }
        public void Close()
        {
            if (!IsOpen) return;
            Hide(); restoring = StartCoroutine(UnlockNextFrame());
        }
        private IEnumerator UnlockNextFrame() { yield return null; locks?.Unlock(this); restoring = null; }
        private void Hide()
        {
            MenuStack.Remove(this); if (overlay != null) overlay.style.display = DisplayStyle.None;
            pause?.Dispose(); pause = null; trader = null; confirming = null;
        }
        private void OnDisable()
        {
            if (restoring != null) { StopCoroutine(restoring); restoring = null; }
            Hide(); locks?.Unlock(this);
        }
        private void OnDestroy() { if (document != null) Destroy(document.gameObject); }
    }
}
