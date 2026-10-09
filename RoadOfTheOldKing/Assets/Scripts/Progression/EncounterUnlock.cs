using System;
using RoadOfTheOldKing.Combat;
using UnityEngine;

namespace RoadOfTheOldKing.Progression
{
    // Permanent stash unlock. An empty or broken guard list fails closed until actual enemies are wired.
    public sealed class EncounterUnlock : MonoBehaviour, IProgressParticipant
    {
        [SerializeField] private string milestone;
        [SerializeField] private Damageable[] guards = Array.Empty<Damageable>();
        private void OnEnable() { foreach (var guard in guards) if (guard != null) guard.Defeated += TryUnlock; }
        private void OnDisable() { foreach (var guard in guards) if (guard != null) guard.Defeated -= TryUnlock; }
        private void TryUnlock()
        {
            var game = GameSession.Instance;
            if (game == null || game.IsLoading || string.IsNullOrWhiteSpace(milestone) ||
                game.Progress.Has(milestone) || guards.Length == 0) return;
            foreach (var guard in guards) if (guard == null || guard.IsAlive) return;
            game.Progress.Complete(milestone);
            game.SaveProgress();
        }
        public void CaptureProgress(ProgressState state) { }
        public void RestoreProgress(ProgressState state) { }
    }
}
