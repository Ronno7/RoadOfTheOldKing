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
        // This socket's stage lets its crank turn, whether or not the crank is in it yet (its glint, SluiceFeedback).
        public bool ReadyToTurn => Turnable;
        // Pinned until the puzzle frees this socket (or while the guardian holds the controls): its lock pin shows, and it
        // comes out when the stage frees it (SluiceFeedback). A carriage that stops moving doesn't pin it again.
        public bool Latched => sluice != null && (sluice.IsGuarded ||
            sluice.Stage < (carriage ? SluiceStage.ServiceDiverted : SluiceStage.DistributorReleased));

        // Plain words (user, 23 Nov: "just call it a crank, say requires crank, insert crank, remove crank"). A locked
        // socket shows its pin rather than saying so. Only a carried crank can go in: with it seated in the other socket,
        // this one needs it brought over.
        public override string Prompt =>
            !requiresOwnedCrank ? (sluice != null && sluice.IsMoving ? "Carriage moving" : "Turn crank") :
            HoldsCrank ? (sluice != null && sluice.IsMoving ? "Carriage moving" : Turnable ? "Turn crank" : "Remove crank") :
            Carried ? "Insert crank" : "Requires crank";

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
