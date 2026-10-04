using System;
using System.Collections.Generic;
using TheLostShrine.Combat;
using TheLostShrine.Player;
using TheLostShrine.Progression;
using UnityEngine;

namespace TheLostShrine.World
{
    // Optional event sources unlock a reward. Without a source it is an exploration pickup. A defeat
    // source can bring a pack (alsoDefeat): the reward unlocks once all of them are down, where the last fell.
    [DisallowMultipleComponent]
    public sealed class SunShardReward : WorldPickup, IProgressParticipant
    {
        [SerializeField] private string rewardId;
        [SerializeField] private Damageable defeatSource;
        [Tooltip("With a defeat source: these must be defeated too (a pack); the shard appears where the last one fell.")]
        [SerializeField] private Damageable[] alsoDefeat = Array.Empty<Damageable>();
        [SerializeField] private ThrowRecallPuzzle puzzleSource;
        [SerializeField] private GameObject pickupVisual;
        [SerializeField] private bool awardImmediately;
        public string RewardId => rewardId;
        public bool IsAvailable { get; private set; }
        private readonly List<(Damageable source, Action handler)> defeatHandlers = new List<(Damageable, Action)>();
        private Damageable lastFallen;
        public bool IsCollected => CheckpointSession.Instance != null &&
            CheckpointSession.Instance.Progress.Has("shard/collected/" + rewardId);

        protected override void OnEnable()
        {
            base.OnEnable();
            foreach (var source in DefeatSources())
            {
                var fallen = source;
                Action handler = () => OnDefeated(fallen);
                source.Defeated += handler;
                defeatHandlers.Add((source, handler));
            }
            if (puzzleSource != null) puzzleSource.Solved += Unlock;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            foreach (var (source, handler) in defeatHandlers) if (source != null) source.Defeated -= handler;
            defeatHandlers.Clear();
            if (puzzleSource != null) puzzleSource.Solved -= Unlock;
        }

        private IEnumerable<Damageable> DefeatSources()
        {
            if (defeatSource != null) yield return defeatSource;
            if (alsoDefeat != null) foreach (var source in alsoDefeat) if (source != null) yield return source;
        }

        private void OnDefeated(Damageable fallen)
        {
            foreach (var source in DefeatSources()) if (source.IsAlive) return;
            lastFallen = fallen;
            Unlock();
        }

        private void Unlock()
        {
            if (IsAvailable || IsCollected) return;
            IsAvailable = true;
            if (lastFallen != null) transform.position = lastFallen.transform.position;
            RefreshVisual();
            if (awardImmediately) Collect();
            else CheckpointSession.Instance?.SaveProgress();
        }

        public override string Prompt => "Take Sun Shard";
        public override bool CanCollect(PlayerHealth player) => base.CanCollect(player) &&
            IsAvailable && !IsCollected && !awardImmediately && !string.IsNullOrEmpty(rewardId) &&
            CheckpointSession.Instance != null;
        public override bool TryCollect(PlayerHealth player) => CanCollect(player) && Collect();

        private bool Collect()
        {
            if (!IsAvailable || IsCollected || string.IsNullOrEmpty(rewardId) ||
                CheckpointSession.Instance == null || !CheckpointSession.Instance.TryCollectShard(rewardId))
                return false;
            RefreshVisual();
            return true;
        }

        private void RefreshVisual()
        {
            if (pickupVisual != null) pickupVisual.SetActive(IsAvailable && !IsCollected && !awardImmediately);
        }

        public void CaptureProgress(ProgressState state)
        {
            if (IsAvailable) state.Complete("shard/available/" + rewardId);
        }

        public void RestoreProgress(ProgressState state)
        {
            IsAvailable = (defeatSource == null && puzzleSource == null) ||
                state.Has("shard/available/" + rewardId) || (puzzleSource != null && puzzleSource.IsSolved);
            // Puzzle restoration order is independent of participant discovery order.
            if (puzzleSource != null && state.Has(puzzleSource.ProgressId)) IsAvailable = true;
            if (IsAvailable && awardImmediately && !IsCollected) Collect();
            RefreshVisual();
        }
    }
}
