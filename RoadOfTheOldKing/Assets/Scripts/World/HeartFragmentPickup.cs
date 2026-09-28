using TheLostShrine.Player;
using TheLostShrine.Progression;
using UnityEngine;

namespace TheLostShrine.World
{
    [DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
    public sealed class HeartFragmentPickup : WorldPickup, IProgressParticipant
    {
        [SerializeField] private string rewardId;
        [SerializeField] private GameObject visual;
        public string RewardId => rewardId;
        public bool IsCollected { get; private set; }

        public override string Prompt => "Pick up heart fragment";
        public override bool CanCollect(PlayerHealth player) => base.CanCollect(player) &&
            !IsCollected && !string.IsNullOrEmpty(rewardId) && CheckpointSession.Instance != null;

        public override bool TryCollect(PlayerHealth player)
        {
            if (!CanCollect(player) || !CheckpointSession.Instance.TryCollectHeartFragment(rewardId))
                return false;
            IsCollected = true;
            Refresh();
            return true;
        }

        private void Refresh()
        {
            if (visual != null) visual.SetActive(!IsCollected);
            GetComponent<Collider2D>().enabled = !IsCollected;
        }

        public void CaptureProgress(ProgressState state) { }
        public void RestoreProgress(ProgressState state)
        {
            IsCollected = HeartFragmentProgression.IsCollected(state, rewardId);
            Refresh();
        }
    }
}
