using TheLostShrine.Player;
using TheLostShrine.Progression;
using UnityEngine;

namespace TheLostShrine.World
{
    [DisallowMultipleComponent, RequireComponent(typeof(Collider2D))]
    public sealed class HeartFragmentPickup : MonoBehaviour, IProgressParticipant
    {
        [SerializeField] private string rewardId;
        [SerializeField] private GameObject visual;
        public string RewardId => rewardId;
        public bool IsCollected { get; private set; }

        private void OnTriggerEnter2D(Collider2D other) => TryCollect(other.GetComponentInParent<PlayerHealth>());
        private void OnTriggerStay2D(Collider2D other) => TryCollect(other.GetComponentInParent<PlayerHealth>());

        public bool TryCollect(PlayerHealth player)
        {
            if (!isActiveAndEnabled || IsCollected || player == null || !player.IsAlive ||
                Vector2.Distance(player.transform.position, transform.position) > 1f ||
                CheckpointSession.Instance == null || !CheckpointSession.Instance.TryCollectHeartFragment(rewardId))
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
