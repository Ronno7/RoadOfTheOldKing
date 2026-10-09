using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    [DisallowMultipleComponent]
    public sealed class RopeRoute : MonoBehaviour, IProgressParticipant
    {
        [SerializeField] private string routeId;
        [SerializeField] private RopeAnchor lowerAnchor;
        [SerializeField] private RopeAnchor upperAnchor;
        [Tooltip("Only this cliff may be crossed, along the straight authored segment.")]
        [SerializeField] private Collider2D traversedCliff;
        [SerializeField, Min(.1f)] private float climbSpeed = 2.6f;
        [SerializeField] private GameObject securedRopeVisual;
        private PlayerMovement climber;

        public string ProgressId => "rope/secured/" + routeId;
        public bool IsSecured => GameSession.Instance != null && GameSession.Instance.Progress.Has(ProgressId);
        public bool IsConfigured => !string.IsNullOrWhiteSpace(routeId) && lowerAnchor != null &&
            upperAnchor != null && lowerAnchor != upperAnchor && traversedCliff != null;
        public bool OwnsAnchor(RopeAnchor anchor) => anchor != null && (anchor == lowerAnchor || anchor == upperAnchor);

        public bool TryClimb(PlayerHealth player, RopeAnchor anchor)
        {
            var session = GameSession.Instance;
            if (!IsConfigured || !isActiveAndEnabled || !OwnsAnchor(anchor) || !anchor.CanCollect(player) ||
                session == null || session.IsLoading ||
                (!IsSecured && !session.Progress.OwnsTool(WorldTool.RopeKit))) return false;
            var motor = player.GetComponent<PlayerMovement>();
            Vector2 end = anchor == lowerAnchor ? upperAnchor.transform.position : lowerAnchor.transform.position;
            if (motor == null || !motor.TryBeginRopeTraversal(this, anchor.transform.position, end, traversedCliff, climbSpeed))
                return false;
            climber = motor;
            if (!IsSecured)
            {
                session.Progress.Complete(ProgressId);
                session.SaveProgress();
            }
            RefreshVisual();
            return true;
        }

        public void CaptureProgress(ProgressState state) { }
        public void RestoreProgress(ProgressState state)
        {
            if (securedRopeVisual != null) securedRopeVisual.SetActive(state.Has(ProgressId));
        }
        private void RefreshVisual()
        {
            if (securedRopeVisual != null) securedRopeVisual.SetActive(IsSecured);
        }
        private void OnDisable()
        {
            if (climber != null && climber.IsTraversing(this)) climber.CancelRopeTraversal();
            climber = null;
        }
    }
}
