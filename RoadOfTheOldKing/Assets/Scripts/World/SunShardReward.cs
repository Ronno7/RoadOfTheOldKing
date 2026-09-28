using TheLostShrine.Combat;
using TheLostShrine.Player;
using TheLostShrine.Progression;
using UnityEngine;

namespace TheLostShrine.World
{
    // Optional event sources unlock a reward. Without a source it is an exploration pickup.
    [DisallowMultipleComponent]
    public sealed class SunShardReward : WorldPickup, IProgressParticipant
    {
        [SerializeField] private string rewardId;
        [SerializeField] private Damageable defeatSource;
        [SerializeField] private ThrowRecallPuzzle puzzleSource;
        [SerializeField] private GameObject pickupVisual;
        [SerializeField] private bool awardImmediately;
        public string RewardId => rewardId;
        public bool IsAvailable { get; private set; }
        public bool IsCollected => CheckpointSession.Instance != null &&
            CheckpointSession.Instance.Progress.Has("shard/collected/" + rewardId);

        protected override void OnEnable()
        {
            base.OnEnable();
            if (defeatSource != null) defeatSource.Defeated += Unlock;
            if (puzzleSource != null) puzzleSource.Solved += Unlock;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (defeatSource != null) defeatSource.Defeated -= Unlock;
            if (puzzleSource != null) puzzleSource.Solved -= Unlock;
        }

        private void Unlock()
        {
            if (IsAvailable || IsCollected) return;
            IsAvailable = true;
            if (defeatSource != null) transform.position = defeatSource.transform.position;
            RefreshVisual();
            if (awardImmediately) Collect();
            else CheckpointSession.Instance?.SaveProgress();
        }

        public override string Prompt => "Pick up Sun Shard";
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
