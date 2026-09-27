using TheLostShrine.Combat;
using TheLostShrine.Player;
using TheLostShrine.Progression;
using UnityEngine;

namespace TheLostShrine.World
{
    public sealed class ThrowRecallPuzzle : MonoBehaviour, IProgressParticipant, IResetOnRest
    {
        [SerializeField] private string progressId = "prototype/recall-door";
        [SerializeField] private PuzzleDoor door;
        public bool IsArmed { get; private set; }
        public bool IsSolved { get; private set; }
        public PuzzleDoor Door => door;
        public string ProgressId => progressId;
        public event System.Action Solved;

        public bool ReceiveTargetHit(bool anchor, CombatHit hit)
        {
            var player = hit.Source != null ? hit.Source.GetComponent<PlayerCombatController>() : null;
            if (IsSolved || player == null || !player.CanRecall)
                return false;
            if (anchor && hit.Kind == AttackKind.Throw)
            {
                IsArmed = true;
                return true;
            }
            if (!anchor && IsArmed && hit.Kind == AttackKind.Recall)
            {
                IsSolved = true;
                if (door != null)
                    door.SetOpen(true);
                Solved?.Invoke();
                CheckpointSession.Instance?.SaveProgress();
                return true;
            }
            return false;
        }

        public void CaptureProgress(ProgressState state)
        {
            if (IsSolved)
                state.Complete(progressId);
        }
        public void RestoreProgress(ProgressState state)
        {
            IsSolved = state.Has(progressId);
            IsArmed = IsSolved;
            if (door != null)
                door.SetOpen(IsSolved);
        }
        public void ResetOnRest() => IsArmed = IsSolved;
    }
}
