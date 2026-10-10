using System.Collections.Generic;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.UI;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // The trader owns reach and presentation; offers/RewardService remain the only stock/wallet.
    public sealed class Trader : WorldPickup
    {
        [SerializeField] private ShopOffer[] offers = System.Array.Empty<ShopOffer>();
        public IReadOnlyList<ShopOffer> Offers => offers;
        public override string Prompt => "Trade";
        public bool CanTradeFrom(PlayerHealth player) => base.CanCollect(player) && HasClearAccess(player) &&
            GameSession.Instance != null && !GameSession.Instance.IsLoading && !EncounterState.InCombat;
        public override bool CanCollect(PlayerHealth player) => CanTradeFrom(player) &&
            player.TryGetComponent<CollectionMenu>(out var menu) && menu.CanOpen;
        public override bool TryCollect(PlayerHealth player) => CanCollect(player) &&
            player.GetComponent<CollectionMenu>().OpenTrader(this);
    }
}
