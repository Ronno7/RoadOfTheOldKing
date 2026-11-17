using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.UI;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Where a build stops: a rereadable card at the closed road, offered once the build's last job is done. Changes nothing.
    [DisallowMultipleComponent]
    public sealed class EndCard : WorldPickup
    {
        [SerializeField] private string heading = "THE ROAD GOES ON";
        [SerializeField, TextArea(3, 10)] private string text;
        [Tooltip("Read instead in Steam demo builds: the Windows Steam build profile defines ROTOK_STEAM_DEMO.")]
        [SerializeField, TextArea(3, 10)] private string steamDemoText;
        [SerializeField] private string requiredMilestone;
        [SerializeField] private string prompt = "Look down the road";
        [Tooltip("How far off it can be read: wide enough for the whole blocked road.")]
        [SerializeField, Min(.5f)] private float reach = 3.2f;

        public override string Prompt => prompt;
        protected override float PickupDistance => reach;

#if ROTOK_STEAM_DEMO
        private string Text => string.IsNullOrWhiteSpace(steamDemoText) ? text : steamDemoText;
#else
        private string Text => text;
#endif

        public override bool CanCollect(PlayerHealth player)
        {
            var session = GameSession.Instance;
            return base.CanCollect(player) && !string.IsNullOrWhiteSpace(Text) && session != null && !session.IsLoading &&
                session.Player == player && (string.IsNullOrEmpty(requiredMilestone) || session.Progress.Has(requiredMilestone)) &&
                player.TryGetComponent<InscriptionReader>(out var reader) && reader.CanOpen;
        }

        public override bool TryCollect(PlayerHealth player) =>
            CanCollect(player) && player.GetComponent<InscriptionReader>().TryOpen(this, heading, Text);
    }
}
