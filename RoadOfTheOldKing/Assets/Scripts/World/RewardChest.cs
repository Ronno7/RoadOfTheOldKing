using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Contents use the existing reward ID as the single source of truth for opening; the grant and its save
    // record go through RewardService.TryGrant and RewardKeys like every world reward (17 Oct refactor).
    // The reward field was the ChestReward enum; RewardKind keeps the same values.
    // Fixed contents are saved together with exhaustion; no separate open flag.
    [DisallowMultipleComponent]
    public sealed class RewardChest : WorldPickup, IProgressParticipant
    {
        [SerializeField] private string rewardId;
        [SerializeField] private RewardKind reward;
        [SerializeField, Min(1)] private int coinAmount = 25;
        [SerializeField] private WorldTool tool;
        [SerializeField] private string requiredMilestone;
        [SerializeField] private GameObject closedVisual;
        [SerializeField] private GameObject openVisual;
        [SerializeField] private RewardChestSpriteView spriteView;
        public bool IsOpen { get; private set; }
        public string RewardId => rewardId;
        public override string Prompt => "Open chest";

        public override bool CanCollect(PlayerHealth player)
        {
            var session = GameSession.Instance;
            return base.CanCollect(player) && !IsOpen && !string.IsNullOrWhiteSpace(rewardId) &&
                session != null && !session.IsLoading &&
                (string.IsNullOrEmpty(requiredMilestone) || session.Progress.Has(requiredMilestone)) &&
                HasClearAccess(player);
        }

        public override bool TryCollect(PlayerHealth player)
        {
            if (!CanCollect(player)) return false;
            var session = GameSession.Instance;
            bool awarded = session.Rewards.TryGrant(reward, rewardId, coinAmount, tool);
            if (awarded) PickupPresentation.Collected(gameObject, player);
            // Also reconcile a reward granted elsewhere under the same unique ID.
            RestoreProgress(session.Progress);
            if (awarded && spriteView != null) spriteView.PlayOpening();
            return awarded;
        }

        public void CaptureProgress(ProgressState state) { }
        public void RestoreProgress(ProgressState state)
        {
            IsOpen = RewardKeys.IsGranted(state, reward, rewardId, tool);
            if (closedVisual != null) closedVisual.SetActive(!IsOpen);
            if (openVisual != null) openVisual.SetActive(IsOpen);
            if (spriteView != null) spriteView.SetOpen(IsOpen);
        }
    }
}
