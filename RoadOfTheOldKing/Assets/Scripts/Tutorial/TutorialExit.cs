using System;
using TheLostShrine.Player;
using TheLostShrine.Progression;
using UnityEngine;

namespace TheLostShrine.Tutorial
{
    // End of the Tutorial route. Entering the trigger records completion once and says so.
    // Placeholder for the overworld transition: the scene load belongs here when Green Lowlands exists.
    [DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
    public sealed class TutorialExit : MonoBehaviour, IProgressParticipant
    {
        [SerializeField] private string progressId = "tutorial/complete";
        [SerializeField, TextArea] private string arrivalNotice = "The old road leads on to the Green Lowlands. End of the tutorial for now.";
        public bool IsComplete { get; private set; }
        public event Action Completed;

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerHealth>();
            if (player == null || !player.IsAlive) return;
            // Repeat visits still get the message; completion and the save happen once.
            if (!IsComplete)
            {
                IsComplete = true;
                CheckpointSession.Instance?.SaveProgress();
                Completed?.Invoke();
            }
            if (!string.IsNullOrEmpty(arrivalNotice)) TutorialNotice.Show(arrivalNotice, 7f);
        }

        public void CaptureProgress(ProgressState state) { if (IsComplete) state.Complete(progressId); }
        public void RestoreProgress(ProgressState state) => IsComplete = state.Has(progressId);
    }
}
