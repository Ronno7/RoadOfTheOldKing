using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    [DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
    public sealed class HeartFragmentPickup : WorldPickup, IProgressParticipant
    {
        [SerializeField] private string rewardId;
        [SerializeField] private GameObject visual;
        public string RewardId => rewardId;
        public bool IsCollected { get; private set; }

        public override string Prompt => "Take fragment";
        public override bool CanCollect(PlayerHealth player) => base.CanCollect(player) &&
            !IsCollected && !string.IsNullOrEmpty(rewardId) && GameSession.Instance != null;

        public override bool TryCollect(PlayerHealth player)
        {
            if (!CanCollect(player) || !GameSession.Instance.Rewards.TryCollectHeartFragment(rewardId))
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
