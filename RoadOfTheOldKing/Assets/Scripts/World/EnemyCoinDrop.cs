using System.Collections.Generic;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Renewable per life. Pending/empty results belong to the current visit; collected coins belong to the save.
    // An optional first-defeat bonus pays once ever, recorded like a coin cache (22 Oct: the orchard's locked coin
    // pile became bonuses on its three scavengers, so coins are never visible but out of reach).
    [RequireComponent(typeof(Damageable)), DisallowMultipleComponent]
    public sealed class EnemyCoinDrop : MonoBehaviour, ICoinPickupSource, IResetOnRest, ISceneVisitParticipant
    {
        [SerializeField] private GameObject coinVisual;
        [Tooltip("Optional: coins added to the first defeat only, saved under this reward ID.")]
        [SerializeField] private string firstDefeatRewardId;
        [SerializeField, Min(0)] private int firstDefeatCoins;
        [Tooltip("Optional: the coin source this bonus replaced. A save that already collected it gets no bonus.")]
        [SerializeField] private string replacesCoinSource;
        private Damageable health;
        private bool rolled;
        private int amount, bonus;
        private Vector2 dropPosition;
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(8);
        public int PendingCoins => amount;
        private void Awake() { health = GetComponent<Damageable>(); Refresh(); }
        private void OnEnable() => health.Defeated += OnDefeated;
        private void OnDisable() => health.Defeated -= OnDefeated;
        private void OnDefeated()
        {
            var game = GameSession.Instance;
            if (rolled || game == null || game.IsLoading) return;
            rolled = true;
            float sample = Random.value;
            amount = sample < .5f ? 0 : sample < .9f ? 1 : 5;
            bonus = BonusOwed(game.Progress) ? firstDefeatCoins : 0;
            dropPosition = transform.position;
            Refresh();
        }
        private void LateUpdate()
        {
            // Keep the pickup at the fall point while the corpse slides. Idle/alive enemies do no query.
            if (amount + bonus > 0 && coinVisual != null) coinVisual.transform.position = dropPosition;
        }
        private void Refresh()
        {
            if (coinVisual == null) return;
            coinVisual.transform.position = dropPosition;
            coinVisual.SetActive(amount + bonus > 0);
            PickupPresentation.ShowCoinAmount(coinVisual, amount + bonus);
        }
        public bool TryCollect(PlayerHealth player)
        {
            var game = GameSession.Instance;
            if (!isActiveAndEnabled || game == null || game.IsLoading || player == null || !player.IsAlive ||
                amount + bonus <= 0 || coinVisual == null || Vector2.Distance(player.transform.position, dropPosition) > .9f ||
                !WorldAccess.IsClear(player, coinVisual.transform, hits)) return false;
            int reward = amount, extra = bonus; amount = bonus = 0; // Consume before publishing the wallet event.
            bool paid = reward > 0 && game.Rewards.TryCollectEnemyCoins(reward);
            // The bonus records its ID, so it can never be paid twice even if the renewable part was empty.
            paid |= extra > 0 && game.Rewards.TryCollectCoinCache(firstDefeatRewardId, extra);
            if (!paid) { amount = reward; bonus = extra; return false; }
            PickupPresentation.Collected(coinVisual, player);
            Refresh(); return true;
        }
        public void ResetOnRest() { rolled = false; amount = bonus = 0; Refresh(); }

        private bool BonusOwed(ProgressState progress)
        {
            if (firstDefeatCoins <= 0 || string.IsNullOrWhiteSpace(firstDefeatRewardId) || progress.Has(RewardKeys.CoinCache(firstDefeatRewardId)))
                return false;
            var replaced = string.IsNullOrWhiteSpace(replacesCoinSource) ? null : progress.FindCoinSource(replacesCoinSource);
            return replaced == null || !replaced.collected;
        }
        private sealed class Snapshot
        {
            internal bool rolled;
            internal int amount, bonus;
            internal Vector2 position;
        }
        public object CaptureVisitState() => new Snapshot { rolled = rolled, amount = amount, bonus = bonus, position = dropPosition };
        public void RestoreVisitState(object state)
        {
            if (!(state is Snapshot snapshot)) return;
            rolled = snapshot.rolled; amount = snapshot.amount; bonus = snapshot.bonus; dropPosition = snapshot.position; Refresh();
        }
    }
}
