using UnityEngine;
using RoadOfTheOldKing.Progression;

namespace RoadOfTheOldKing.Combat
{
    [DisallowMultipleComponent]
    public sealed class Breakable : MonoBehaviour, IHitReceiver, IHitEventSource, IProgressParticipant
    {
        [SerializeField] private bool requiresFullCleave;
        [Tooltip("Optional stable milestone for an authored obstacle. Empty means it resets on scene load.")]
        [SerializeField] private string progressId;
        public event System.Action<CombatHit> Broken;
        public event System.Action<CombatHit> HitReceived { add => Broken += value; remove => Broken -= value; }
        public bool IsBroken { get; private set; }

        public void RestoreBrokenState()
        {
            IsBroken = true;
            gameObject.SetActive(false);
        }

        public bool ReceiveHit(CombatHit hit)
        {
            if (IsBroken || (requiresFullCleave && (hit.Kind != AttackKind.ChargedCleave || !hit.BreaksGuard)))
                return false;
            IsBroken = true;
            Broken?.Invoke(hit);
            gameObject.SetActive(false);
            if (!string.IsNullOrEmpty(progressId)) GameSession.Instance?.SaveProgress();
            return true;
        }

        public void CaptureProgress(ProgressState state)
        {
            if (IsBroken && !string.IsNullOrEmpty(progressId)) state.Complete(progressId);
        }

        public void RestoreProgress(ProgressState state)
        {
            if (!string.IsNullOrEmpty(progressId) && state.Has(progressId)) RestoreBrokenState();
        }
    }
}
