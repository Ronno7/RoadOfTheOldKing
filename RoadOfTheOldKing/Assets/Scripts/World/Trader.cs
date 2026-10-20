using RoadOfTheOldKing.Enemies;
using System.Collections.Generic;
using System.Linq;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.UI;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // The trader owns reach and presentation; offers/RewardService remain the only stock/wallet.
    // Talking (user, 17 Oct) opens a short conversation first: Trade hands over to the shop, the road question
    // answers with a little lore, Farewell ends it. He calls out as the player comes near and says goodbye after.
    public sealed class Trader : WorldPickup
    {
        [SerializeField] private ShopOffer[] offers = System.Array.Empty<ShopOffer>();
        [Header("Conversation")]
        [SerializeField] private string displayName = "Traveling Trader";
        [Tooltip("First conversation of a visit.")]
        [SerializeField, TextArea] private string[] greeting =
        {
            "Ho there, traveler. Not many walk the old road these days.",
            "I carry what the road asks for. Rope, mostly, and the odd curiosity."
        };
        [Tooltip("Later conversations of the same visit.")]
        [SerializeField, TextArea] private string[] welcomeBack = { "Back again? Rest your feet a moment." };
        [Tooltip("Instead of the above once everything is sold.")]
        [SerializeField, TextArea] private string[] soldOut = { "My packs are empty, friend. That's all I carry this season." };
        [SerializeField, TextArea] private string[] aboutTheRoad =
        {
            "They say this road once ran all the way to the highlands, back in the old king's day.",
            "Most of it sleeps under the grass now. The smiths' waterworks still stand to the north, though. Folk say they hold back more than water."
        };
        [Tooltip("Called out when the player comes near (one at random, not too often).")]
        [SerializeField] private string[] approachBarks = { "Ho there!", "Wares for the road!", "Fine day for walking." };
        [SerializeField] private string farewellBark = "Safe travels.";
        [SerializeField] private string afterTradeBark = "Mind the road.";
        [SerializeField, Min(0f)] private float barkCooldown = 20f;

        private TraderWalker walker;
        private bool greeted, trading, wasNear;
        private float nextBarkAt;

        public IReadOnlyList<ShopOffer> Offers => offers;
        public override string Prompt => "Talk";
        public bool CanTradeFrom(PlayerHealth player) => base.CanCollect(player) && HasClearAccess(player) &&
            GameSession.Instance != null && !GameSession.Instance.IsLoading && !EncounterState.InCombat;
        public override bool CanCollect(PlayerHealth player) => CanTradeFrom(player) &&
            player.TryGetComponent<DialogueBox>(out var dialogue) && dialogue.CanOpen;
        public override bool TryCollect(PlayerHealth player) => CanCollect(player) && Talk(player);

        private void Awake() => walker = GetComponent<TraderWalker>();

        private bool SoldOut => GameSession.Instance != null && offers.Where(o => o != null).All(o => o.IsSold(GameSession.Instance.Progress));

        private bool Talk(PlayerHealth player)
        {
            var dialogue = player.GetComponent<DialogueBox>();
            var opening = !greeted ? greeting : SoldOut ? soldOut : welcomeBack;
            HudNotifications.ClearSpeech(transform);
            if (!dialogue.Open(displayName, opening, Choices(player, dialogue), () => { if (!trading) SetEngaged(false); })) return false;
            greeted = true;
            SetEngaged(true);
            return true;
        }

        private PauseMenuAction[] Choices(PlayerHealth player, DialogueBox dialogue) => new[]
        {
            new PauseMenuAction("Trade", null, () => OpenShop(player, dialogue)),
            new PauseMenuAction("Ask about the road", null, () => dialogue.Say(aboutTheRoad, Choices(player, dialogue))),
            new PauseMenuAction("Farewell", null, () => { dialogue.Close(); HudNotifications.Say(transform, farewellBark); })
        };

        // The conversation hands its lease straight to the shop, so no gameplay frame slips between them.
        private void OpenShop(PlayerHealth player, DialogueBox dialogue)
        {
            trading = true;
            dialogue.Close(handOff: true);
            if (!player.TryGetComponent<CollectionMenu>(out var menu) || !menu.OpenTrader(this, fromConversation: true)) { trading = false; SetEngaged(false); }
        }

        // CollectionMenu, when the shop closes.
        public void OnShopClosed()
        {
            if (!trading) return;
            trading = false;
            SetEngaged(false);
            HudNotifications.Say(transform, afterTradeBark);
        }

        private void SetEngaged(bool engaged) { if (walker != null) walker.Engaged = engaged; }

        private void Update()
        {
            bool near = walker != null && walker.PlayerNear;
            if (near && !wasNear && !walker.Engaged && Time.unscaledTime >= nextBarkAt && !MenuStack.IsAnyOpen &&
                !EncounterState.InCombat && approachBarks.Length > 0)
            {
                HudNotifications.Say(transform, approachBarks[Random.Range(0, approachBarks.Length)]);
                nextBarkAt = Time.unscaledTime + barkCooldown;
            }
            wasNear = near;
        }
    }
}
