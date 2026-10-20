using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoadOfTheOldKing.World
{
    // The pickup half of a world reward (17 Oct refactor). A subclass names what it grants (kind, prompt);
    // when it may appear belongs to an optional EncounterUnlock and/or a required milestone; what is saved
    // belongs to RewardService and RewardKeys. The pickup itself only presents, checks reach and asks for the grant.
    //  unlock: the encounter whose defeat makes it appear (optionally where the last guard fell).
    //  awardImmediately: granted the moment it unlocks, with nothing to pick up.
    public abstract class RewardPickup : WorldPickup, IProgressParticipant
    {
        [Tooltip("Unique reward id: save data, never rename a shipped one.")]
        [SerializeField] protected string rewardId;
        [SerializeField] protected bool requireClearAccess;
        [Tooltip("Progress milestone that must be complete before it can be taken (e.g. a drained pond).")]
        [SerializeField] protected string requiredMilestone;
        [Tooltip("Optional encounter: the reward appears once all its guards are defeated.")]
        [SerializeField] protected EncounterUnlock unlock;
        [Tooltip("With an unlock: move to where the last guard fell.")]
        [SerializeField] protected bool appearWhereLastFell;
        [Tooltip("With an unlock: grant immediately, with nothing to pick up.")]
        [SerializeField] protected bool awardImmediately;
        [FormerlySerializedAs("pickupVisual")]
        [SerializeField] protected GameObject visual;

        protected abstract RewardKind Kind { get; }
        protected virtual WorldTool GrantedTool => WorldTool.None;
        protected virtual int CoinAmount => 0;

        public string RewardId => rewardId;
        public EncounterUnlock Unlock => unlock;
        public bool IsAvailable { get; private set; } = true;
        public bool IsCollected => GameSession.Instance != null &&
            RewardKeys.IsGranted(GameSession.Instance.Progress, Kind, rewardId, GrantedTool);

        // Until the session restores progress, only an ungated reward counts as available.
        protected virtual void Awake() => IsAvailable = unlock == null;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (unlock != null) unlock.Unlocked += OnUnlocked;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (unlock != null) unlock.Unlocked -= OnUnlocked;
        }

        private void OnUnlocked(EncounterUnlock source)
        {
            if (IsAvailable || IsCollected) return;
            IsAvailable = true;
            if (appearWhereLastFell && source.LastFallen.HasValue) transform.position = source.LastFallen.Value;
            if (awardImmediately) Grant(null);
            Refresh();
        }

        public override bool CanCollect(PlayerHealth player)
        {
            var game = GameSession.Instance;
            return base.CanCollect(player) && IsAvailable && !IsCollected && !awardImmediately &&
                !string.IsNullOrWhiteSpace(rewardId) && game != null && !game.IsLoading &&
                (!requireClearAccess || HasClearAccess(player)) &&
                (string.IsNullOrEmpty(requiredMilestone) || game.Progress.Has(requiredMilestone));
        }

        public override bool TryCollect(PlayerHealth player) => CanCollect(player) && Grant(player);

        private bool Grant(PlayerHealth player)
        {
            var game = GameSession.Instance;
            if (game == null || !game.Rewards.TryGrant(Kind, rewardId, CoinAmount, GrantedTool)) return false;
            if (!awardImmediately) PickupPresentation.Collected(visual, player);
            Refresh();
            return true;
        }

        protected void Refresh()
        {
            bool shown = IsAvailable && !IsCollected && !awardImmediately;
            if (visual != null) visual.SetActive(shown);
            OnRefresh(shown);
        }

        // Subclass presentation beyond the visual (e.g. a collider).
        protected virtual void OnRefresh(bool shown) { }

        public void CaptureProgress(ProgressState state) { }

        public void RestoreProgress(ProgressState state)
        {
            IsAvailable = unlock == null || unlock.IsUnlocked(state);
            if (IsAvailable && awardImmediately && !RewardKeys.IsGranted(state, Kind, rewardId, GrantedTool)) Grant(null);
            Refresh();
        }
    }
}
