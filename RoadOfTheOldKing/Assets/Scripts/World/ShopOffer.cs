using System.Linq;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.UI;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    public enum ShopReward { Tool, HeartFragment }

    // One authored finite offer. A merchant presents the same saved transaction.
    public sealed class ShopOffer : WorldPickup, IProgressParticipant
    {
        [SerializeField] private string offerId, rewardId;
        [SerializeField] private ShopReward reward;
        [SerializeField] private WorldTool tool;
        [SerializeField, Min(1)] private int price = 30;
        [SerializeField] private GameObject stockVisual, soldVisual;
        [SerializeField] private Trader merchant;
        public Trader Merchant => merchant;
        public string OfferId => offerId;
        public string RewardId => rewardId;
        public ShopReward Reward => reward;
        public WorldTool Tool => tool;
        public int Price => price;
        public string ItemName => reward == ShopReward.Tool ? WorldToolNames.Display(tool) : "Heart fragment";
        public bool IsSold(ProgressState state) => state.Has("shop/purchased/" + offerId) ||
            (reward == ShopReward.Tool ? state.OwnsTool(tool) : HeartFragmentProgression.IsCollected(state, rewardId));
        public override string Prompt => "Buy " + (reward == ShopReward.Tool ? ItemName.ToLowerInvariant() : "fragment") +
            " · " + price + " coins";
        public override bool CanCollect(PlayerHealth player)
        {
            var game = GameSession.Instance;
            bool accessible = merchant != null ? isActiveAndEnabled && merchant.Offers.Contains(this) && merchant.CanTradeFrom(player)
                : base.CanCollect(player) && HasClearAccess(player);
            return accessible && game != null && !game.IsLoading &&
                price > 0 && !string.IsNullOrWhiteSpace(offerId) && !IsSold(game.Progress) &&
                (reward == ShopReward.Tool ? tool != WorldTool.None && System.Enum.IsDefined(typeof(WorldTool), tool) :
                    reward == ShopReward.HeartFragment && !string.IsNullOrWhiteSpace(rewardId));
        }
        public override bool TryCollect(PlayerHealth player)
        {
            if (!CanCollect(player)) return false;
            var game = GameSession.Instance;
            if (game.Progress.bronzeCoins < price) {
                HudNotifications.Post("Need " + (price - game.Progress.bronzeCoins) + " more Bronze Coins.");
                return false;
            }
            bool bought = game.Rewards.TryPurchase(this, player);
            RestoreProgress(game.Progress);
            return bought;
        }
        public void CaptureProgress(ProgressState state) { }
        protected override void OnEnable() { if (merchant == null) base.OnEnable(); }
        public void RestoreProgress(ProgressState state)
        {
            bool sold = IsSold(state);
            if (stockVisual != null) stockVisual.SetActive(!sold);
            if (soldVisual != null) soldVisual.SetActive(sold);
        }
    }
}
