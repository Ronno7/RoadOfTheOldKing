using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.UI;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // A carving stays present and readable after learning it. It is never a consumable pickup.
    [DisallowMultipleComponent]
    public sealed class ForgeInscription : WorldPickup
    {
        [Tooltip("Permanent catalog number F1..F8. Zero is unconfigured; never reuse a number for another discovery.")]
        [SerializeField, Range(0, ForgeInscriptionProgression.SiteCount)] private int siteNumber;
        [SerializeField] private string heading = "THE SMITHS' MARK";
        [SerializeField, TextArea(3, 10)] private string inscriptionText;
        [SerializeField] private string requiredMilestone;

        public int SiteNumber => siteNumber;
        public string Heading => heading;
        public string Text => inscriptionText;
        public override string Prompt => "Read carving";

        public override bool CanCollect(PlayerHealth player)
        {
            var session = GameSession.Instance;
            return base.CanCollect(player) && ForgeInscriptionProgression.IsValidSite(siteNumber) &&
                !string.IsNullOrWhiteSpace(inscriptionText) && session != null && !session.IsLoading && session.Player == player &&
                (string.IsNullOrEmpty(requiredMilestone) || session.Progress.Has(requiredMilestone)) &&
                player.TryGetComponent<InscriptionReader>(out var reader) && reader.CanOpen &&
                HasClearAccess(player);
        }

        public override bool TryCollect(PlayerHealth player) =>
            CanCollect(player) && player.GetComponent<InscriptionReader>().TryOpen(this);
    }
}
