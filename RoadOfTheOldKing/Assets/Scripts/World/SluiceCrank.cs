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
            sluice != null && !sluice.CanTurn(carriage) ? (sluice.IsMoving ? "Carriage moving" : "Crank locked") :
            carriage ? "Turn carriage crank" : "Turn distributor crank";
        public override bool CanCollect(PlayerHealth player) => base.CanCollect(player) &&
            sluice != null && sluice.isActiveAndEnabled && GameSession.Instance != null &&
            !GameSession.Instance.IsLoading &&
            sluice.Stage < (carriage ? SluiceStage.PondLow : SluiceStage.ServiceDiverted) && HasClearAccess(player);
        public override bool TryCollect(PlayerHealth player)
        {
            if (!CanCollect(player)) return false;
            bool turned = HasCrank && sluice.Turn(carriage);
            sluice.Respond(turned ? SluiceResponse.Turned : SluiceResponse.Rejected, transform);
            return turned;
        }
    }
}
