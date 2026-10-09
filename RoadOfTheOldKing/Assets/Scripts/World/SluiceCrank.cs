using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Uses the existing contextual F interaction, including while the axe is lodged away.
    public sealed class SluiceCrank : WorldPickup
    {
        [SerializeField] private RecallSluice sluice;
        [SerializeField] private bool carriage;
        [SerializeField] private bool requiresOwnedCrank;
        private bool HasCrank => !requiresOwnedCrank || (GameSession.Instance != null &&
            GameSession.Instance.Progress.OwnsTool(WorldTool.MaintenanceCrank));
        public override string Prompt => !HasCrank ? "Requires maintenance crank" :
            carriage ? "Turn carriage crank" : "Turn distributor crank";
        public override bool CanCollect(PlayerHealth player) => base.CanCollect(player) &&
            sluice != null && sluice.CanTurn(carriage) && HasClearAccess(player);
        public override bool TryCollect(PlayerHealth player) => CanCollect(player) && HasCrank && sluice.Turn(carriage);
    }
}
