using RoadOfTheOldKing.Combat;
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
        [Tooltip("With an unlock: lie in the fallen guard's body (where the last one fell, and with the body after a reload or a corpse slide).")]
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
        private bool followFallen;
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
            followFallen = appearWhereLastFell;
            if (awardImmediately) Grant(null);
            Refresh();
            // Only a live unlock announces itself; RestoreProgress shows the reward quietly.
            if (!awardImmediately && !IsCollected) PickupPresentation.Revealed(visual);
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
            if (!awardImmediately && !HoldUp(player)) PickupPresentation.Collected(visual, player);
            Refresh();
            return true;
        }

        // Finds worth a moment raise them over the hero's head instead of the ordinary burst.
        protected virtual bool HoldsUp => true;

        private bool HoldUp(PlayerHealth player)
        {
            if (!HoldsUp || visual == null) return false;
            var view = visual.GetComponentInChildren<PickupPresentation>(true);
            var art = view != null ? view.GetComponent<SpriteRenderer>() : visual.GetComponentInChildren<SpriteRenderer>(true);
            if (art == null) return false;
            bool heart = view != null && view.Style == PickupStyle.Heart;
            return RewardHoldUp.TryPlay(player, art.sprite, art.transform.position, heart ? PickupPresentation.Heart : PickupPresentation.Warm, art);
        }

        protected void Refresh()
        {
            bool shown = IsAvailable && !IsCollected && !awardImmediately;
            if (visual != null) visual.SetActive(shown);
            OnRefresh(shown);
        }

        // An untaken reward stays in the body it fell from: corpses slide after the killing blow, and a scene
        // revisit restores bodies where they fell. It follows the nearest fallen guard; with none fallen
        // (revived by rest) it stays put.
        private void LateUpdate()
        {
            if (!followFallen || !IsAvailable || IsCollected) return;
            Damageable nearest = null;
            float best = float.MaxValue;
            foreach (var guard in unlock.Guards)
            {
                if (guard == null || guard.IsAlive) continue;
                float d = ((Vector2)(guard.transform.position - transform.position)).sqrMagnitude;
                if (d < best) { best = d; nearest = guard; }
            }
            if (nearest != null) transform.position = nearest.transform.position;
        }

        // Subclass presentation beyond the visual (e.g. a collider).
        protected virtual void OnRefresh(bool shown) { }

        public void CaptureProgress(ProgressState state) { }

        public void RestoreProgress(ProgressState state)
        {
            IsAvailable = unlock == null || unlock.IsUnlocked(state);
            followFallen = appearWhereLastFell && unlock != null && IsAvailable;
            if (IsAvailable && awardImmediately && !RewardKeys.IsGranted(state, Kind, rewardId, GrantedTool)) Grant(null);
            Refresh();
        }
    }
}
