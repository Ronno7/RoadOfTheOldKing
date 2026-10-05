using System;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.Tutorial;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Trigger at the edge of a scene. Entering it records an optional milestone once, then loads the
    // target scene at the named spawn point. With no target it only shows its notice (the Tutorial's
    // end until Green Lowlands exists).
    [DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
    public sealed class SceneExit : MonoBehaviour, IProgressParticipant
    {
        [Tooltip("Optional milestone recorded the first time the player leaves through here.")]
        [SerializeField] private string progressId = "";
        [Tooltip("Scene asset path, e.g. Assets/Scenes/GreenLowlands.unity. Empty: no scene change.")]
        [SerializeField] private string targetScene = "";
        [Tooltip("SceneSpawnPoint id in the target scene.")]
        [SerializeField] private string targetSpawnId = "";
        [SerializeField, TextArea] private string arrivalNotice = "";
        public bool IsComplete { get; private set; }
        public event Action Completed;

        private void Reset() => GetComponent<Collider2D>().isTrigger = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerHealth>();
            var session = GameSession.Instance;
            if (player == null || !player.IsAlive || (session != null && session.IsLoading)) return;
            // Repeat visits still get the message; completion happens once.
            if (!IsComplete && !string.IsNullOrEmpty(progressId))
            {
                IsComplete = true;
                Completed?.Invoke();
                if (string.IsNullOrEmpty(targetScene)) session?.SaveProgress();
            }
            // LeaveScene captures and saves this milestone before loading.
            if (!string.IsNullOrEmpty(targetScene) && session != null && session.Checkpoints.LeaveScene(targetScene, targetSpawnId))
                return;
            if (!string.IsNullOrEmpty(arrivalNotice)) TutorialNotice.Show(arrivalNotice, 7f);
        }

        public void CaptureProgress(ProgressState state) { if (IsComplete) state.Complete(progressId); }
        public void RestoreProgress(ProgressState state) => IsComplete = state.Has(progressId);
    }
}
