using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    public sealed class RopeAnchor : WorldPickup
    {
        [SerializeField] private RopeRoute route;
        protected override float PickupDistance => 1.25f;
        public override string Prompt => route != null && route.IsSecured ? "Climb rope" :
            GameSession.Instance != null && GameSession.Instance.Progress.OwnsTool(WorldTool.RopeKit)
                ? "Secure rope and climb" : "Requires rope kit";
        public override bool CanCollect(PlayerHealth player)
        {
            if (!base.CanCollect(player) || route == null || !route.isActiveAndEnabled ||
                !route.IsConfigured || !route.OwnsAnchor(this) || !HasClearAccess(player)) return false;
            var session = GameSession.Instance;
            var motor = player.GetComponent<PlayerMovement>();
            return session != null && !session.IsLoading && motor != null && motor.CanStartRopeTraversal;
        }
        public override bool TryCollect(PlayerHealth player) => route != null && route.TryClimb(player, this);
    }
}
