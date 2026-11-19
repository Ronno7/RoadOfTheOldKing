using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // A crank socket, through the existing contextual F interaction (also while the axe is lodged away).
    // One crank (user, 15 Nov: "pick up the crank, then have to place it in the spot ... so just one crank"): the
    // player places it in a socket, turns it there, and takes it out to carry to the other once that socket's work is
    // done or it's locked. Which socket holds it is saved (ProgressState.crankSocket).
    public sealed class SluiceCrank : WorldPickup
    {
        [SerializeField] private RecallSluice sluice;
        [SerializeField] private bool carriage;
        [SerializeField] private bool requiresOwnedCrank;
        [Tooltip("This socket's save id while the crank sits in it.")]
        [SerializeField] private string socketId;

        private static ProgressState Progress => GameSession.Instance != null ? GameSession.Instance.Progress : null;
        private bool Owned => Progress != null && Progress.OwnsTool(WorldTool.MaintenanceCrank);
        private bool Carried => Owned && string.IsNullOrEmpty(Progress.crankSocket);
        // A socket without the requirement always has its own crank.
        public bool HoldsCrank => !requiresOwnedCrank || (Owned && !string.IsNullOrEmpty(socketId) && Progress.crankSocket == socketId);
        private bool StageOpen => sluice != null && sluice.Stage < (carriage ? SluiceStage.PondLow : SluiceStage.ServiceDiverted);
        private bool Turnable => StageOpen && sluice.CanTurn(carriage);
        private string TurnPrompt => carriage ? "Turn carriage crank" : "Turn distributor crank";

        public override string Prompt =>
            !requiresOwnedCrank ? (Turnable ? TurnPrompt : sluice != null && sluice.IsMoving ? "Carriage moving" : "Crank locked") :
            HoldsCrank ? (sluice != null && sluice.IsMoving ? "Carriage moving" : Turnable ? TurnPrompt : StageOpen ? "Take crank (locked)" : "Take crank") :
            Carried ? "Place crank" : Owned ? "Crank is in the other socket" : "Requires maintenance crank";

        public override bool CanCollect(PlayerHealth player)
        {
            if (!base.CanCollect(player) || sluice == null || !sluice.isActiveAndEnabled || GameSession.Instance == null ||
                GameSession.Instance.IsLoading || !HasClearAccess(player)) return false;
            if (!requiresOwnedCrank) return StageOpen;
            // Placing and taking work whatever the stage; without the crank the empty socket still answers while it matters.
            return HoldsCrank ? !sluice.IsMoving : Carried || StageOpen;
        }

        public override bool TryCollect(PlayerHealth player)
        {
            if (!CanCollect(player)) return false;
            if (!requiresOwnedCrank)
            {
                bool turnedFixed = sluice.Turn(carriage);
                sluice.Respond(turnedFixed ? SluiceResponse.Turned : SluiceResponse.Rejected, transform);
                return turnedFixed;
            }
            if (HoldsCrank)
            {
                if (Turnable)
                {
                    bool turned = sluice.Turn(carriage);
                    sluice.Respond(turned ? SluiceResponse.Turned : SluiceResponse.Rejected, transform);
                    return turned;
                }
                Progress.crankSocket = "";
                Seat();
                return true;
            }
            if (Carried)
            {
                Progress.crankSocket = socketId;
                Seat();
                return true;
            }
            sluice.Respond(SluiceResponse.Rejected, transform);
            return false;
        }

        // The crank going in or coming out: a latch click, and saved at once like any pickup.
        private void Seat()
        {
            sluice.Respond(SluiceResponse.Accepted, transform);
            GameSession.Instance?.SaveProgress();
        }
    }
}
