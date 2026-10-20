using System;
using System.Collections.Generic;
using RoadOfTheOldKing.Combat;
using UnityEngine;

namespace RoadOfTheOldKing.Progression
{
    // The condition half of a reward (17 Oct refactor): when every guard has fallen it records its milestone
    // (saved) and raises Unlocked with where the last one fell. A reward either references it (RewardPickup.unlock,
    // which can appear at that spot) or just requires its milestone (chests). An empty or broken guard list fails
    // closed until actual enemies are wired. Milestones are save data: never rename a shipped one.
    public sealed class EncounterUnlock : MonoBehaviour, IProgressParticipant
    {
        [SerializeField] private string milestone;
        [SerializeField] private Damageable[] guards = Array.Empty<Damageable>();
        private readonly List<(Damageable guard, Action handler)> handlers = new List<(Damageable, Action)>();

        public string Milestone => milestone;
        public IReadOnlyList<Damageable> Guards => guards;
        // Where the last guard fell, this session; null until the encounter ends.
        public Vector3? LastFallen { get; private set; }
        public event Action<EncounterUnlock> Unlocked;

        public bool IsUnlocked(ProgressState state) => state != null && !string.IsNullOrWhiteSpace(milestone) && state.Has(milestone);

        private void OnEnable()
        {
            foreach (var guard in guards)
            {
                if (guard == null) continue;
                var fallen = guard;
                Action handler = () => TryUnlock(fallen);
                guard.Defeated += handler;
                handlers.Add((guard, handler));
            }
        }

        private void OnDisable()
        {
            foreach (var (guard, handler) in handlers) if (guard != null) guard.Defeated -= handler;
            handlers.Clear();
        }

        private void TryUnlock(Damageable fallen)
        {
            var game = GameSession.Instance;
            if (game == null || game.IsLoading || string.IsNullOrWhiteSpace(milestone) ||
                game.Progress.Has(milestone) || guards.Length == 0) return;
            foreach (var guard in guards) if (guard == null || guard.IsAlive) return;
            LastFallen = fallen != null ? fallen.transform.position : (Vector3?)null;
            game.Progress.Complete(milestone);
            game.SaveProgress();
            Unlocked?.Invoke(this);
        }

        public void CaptureProgress(ProgressState state) { }
        public void RestoreProgress(ProgressState state) { }
    }
}
