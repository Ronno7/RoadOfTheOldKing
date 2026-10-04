using System;
using TheLostShrine.Combat;
using TheLostShrine.Player;
using TheLostShrine.Progression;
using UnityEngine;

namespace TheLostShrine.Tutorial
{
    // Completes after a thrown hit on any stand followed by getting the axe back in hand.
    [DisallowMultipleComponent]
    public sealed class ThrowRetrieveLesson : MonoBehaviour, IProgressParticipant, IResetOnRest
    {
        [SerializeField] private string progressId = "tutorial/lesson/throw-retrieve";
        [SerializeField] private ThrowPracticeStand[] stands = Array.Empty<ThrowPracticeStand>();
        [SerializeField, TextArea] private string completionNotice = "Walk over the axe to take it back.";
        private PlayerCombatController player;
        private PlayerHealth playerHealth;
        private bool armed;

        public bool IsComplete { get; private set; }
        public bool IsArmed => armed;
        public event Action Completed;

        private void Awake()
        {
            if (stands.Length == 0)
                stands = GetComponentsInChildren<ThrowPracticeStand>(true);
        }

        private void OnEnable() { foreach (var stand in stands) if (stand != null) stand.HitReceived += OnStandHit; }
        private void OnDisable() { foreach (var stand in stands) if (stand != null) stand.HitReceived -= OnStandHit; }

        private void OnStandHit(CombatHit hit)
        {
            if (IsComplete || hit.Kind != AttackKind.Throw)
                return;
            player = hit.Source.GetComponent<PlayerCombatController>();
            playerHealth = hit.Source.GetComponent<PlayerHealth>();
            armed = player != null;
        }

        private void Update()
        {
            if (!armed || IsComplete)
                return;
            if (player == null || player.Weapon == null || (playerHealth != null && !playerHealth.IsAlive))
            {
                armed = false;
                return;
            }
            if (player.Weapon.IsAway)
                return;
            armed = false;
            IsComplete = true;
            CheckpointSession.Instance?.SaveProgress();
            if (!string.IsNullOrEmpty(completionNotice))
                TutorialNotice.Show(completionNotice);
            Completed?.Invoke();
        }

        // Rest cancels an away axe back to hand; that is not a retrieval.
        public void ResetOnRest() => armed = false;

        public void CaptureProgress(ProgressState state)
        {
            if (IsComplete)
                state.Complete(progressId);
        }

        public void RestoreProgress(ProgressState state)
        {
            IsComplete = state.Has(progressId);
            armed = false;
        }
    }
}
