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
        [SerializeField] private Sprite axeIcon, flaskIcon, coinIcon, shardIcon, heartIcon, crankIcon, knowledgeIcon;
        private PlayerHealth player;
        private PlayerCombatController combat;
        private PlayerControlLocks locks;
        private UIDocument document;
        private VisualElement overlay;
        private Label heading, coins, shards, fragments, context, hint;
        private InventoryGridView inventory;
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
            if (!Application.isFocused || Keyboard.current == null) return;
            var keyboard = Keyboard.current;
            if (keyboard.tabKey.wasPressedThisFrame)
            {
                if (IsOpen) { if (trader == null) Close(); }
                else OpenInventory();
                return;
            }
            if (!IsOpen || trading || !ReferenceEquals(MenuStack.Top, this)) return;
            if (keyboard.leftArrowKey.wasPressedThisFrame) inventory.Navigate(-1, 0);
            if (keyboard.rightArrowKey.wasPressedThisFrame) inventory.Navigate(1, 0);
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
            heading = overlay.Q<Label>("heading"); coins = overlay.Q<Label>("coins");
            shards = overlay.Q<Label>("shards"); fragments = overlay.Q<Label>("fragments");
            context = overlay.Q<Label>("context"); hint = overlay.Q<Label>("hint");
            list = new MenuList(overlay.Q("entries"), overlay.Q<Label>("description"), action => { if (IsOpen) action.Execute(); });
            inventory = new InventoryGridView(overlay, CreateIcon);
            overlay.Q<Button>("collection-close").clicked += Back;
            overlay.Q<Image>("coin-icon").sprite = coinIcon;
            overlay.Q<Image>("shard-icon").sprite = shardIcon;
            overlay.Q<Image>("heart-icon").sprite = heartIcon;
            return true;
        }

        private bool Begin(Trader source)
        {
            if (!CanOpen || !Build()) return false;
            trader = source; trading = source != null; confirming = null; stockSelection = 0;
            overlay.EnableInClassList("trading", trading);
            overlay.Q<Button>("collection-close").text = trading ? "Leave" : "Close";
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
            int count = HeartFragmentProgression.Count(state);
            coins.text = state.bronzeCoins.ToString();
            shards.text = state.sunShards.ToString();
            coins.style.fontSize = state.bronzeCoins >= 1000 ? 10 : 20;
            shards.style.fontSize = state.sunShards >= 1000 ? 10 : 20;
            fragments.text = count % HeartFragmentProgression.FragmentsPerHeart + "/3";
        }

        private void ShowInventory()
        {
            heading.text = "INVENTORY"; RefreshBalances();
            context.text = "";
            hint.text = "Tab / Esc";
            inventory.Show(player);
        }

        private VisualElement CreateIcon(string id)
        {
            VisualElement icon;
            if (id == "rope" || id == "lantern") icon = new InventoryToolGlyph(id == "lantern");
            else
            {
                Sprite sprite = id switch {
                    "axe" => axeIcon, "flask" => flaskIcon, "crank" => crankIcon,
                    "knowledge" => knowledgeIcon, _ => heartIcon
                };
                icon = new Image { sprite = sprite, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
            }
            icon.AddToClassList("pack-icon");
            return icon;
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
            overlay.Q<Button>("collection-close").text = "Leave";
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
            overlay.Q<Button>("collection-close").text = "Back";
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
        public void Navigate(int direction) { if (IsOpen) { if (trading) list.Navigate(direction); else inventory.Navigate(0, direction); } }
        public void Submit() { if (IsOpen && trading) list.Submit(); }
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
