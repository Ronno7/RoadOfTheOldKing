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
        [Header("What the hero holds up")]
        [SerializeField] private Sprite coinsSprite;
        [SerializeField] private Sprite shardSprite;
        [SerializeField] private Sprite heartSprite;
        [SerializeField] private Sprite toolSprite;
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
            if (awarded && !HoldUp(player)) PickupPresentation.Collected(gameObject, player);
            // Also reconcile a reward granted elsewhere under the same unique ID.
            RestoreProgress(session.Progress);
            if (awarded && spriteView != null) spriteView.PlayOpening();
            return awarded;
        }

        private bool HoldUp(PlayerHealth player)
        {
            var contents = reward == RewardKind.BronzeCoins ? coinsSprite : reward == RewardKind.SunShard ? shardSprite :
                reward == RewardKind.HeartFragment ? heartSprite : toolSprite;
            var template = GetComponentInChildren<SpriteRenderer>(true);
            return RewardHoldUp.TryPlay(player, contents, transform.position + Vector3.up * .5f,
                reward == RewardKind.HeartFragment ? PickupPresentation.Heart : PickupPresentation.Warm, template);
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
