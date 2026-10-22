using System.Collections.Generic;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    // Renewable per life. Pending/empty results belong to the current visit; collected coins belong to the save.
    [RequireComponent(typeof(Damageable)), DisallowMultipleComponent]
    public sealed class EnemyCoinDrop : MonoBehaviour, ICoinPickupSource, IResetOnRest, ISceneVisitParticipant
    {
        [SerializeField] private GameObject coinVisual;
        private Damageable health;
        private bool rolled;
        private int amount;
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
            dropPosition = transform.position;
            Refresh();
        }
        private void LateUpdate()
        {
            // Keep the pickup at the fall point while the corpse slides. Idle/alive enemies do no query.
            if (amount > 0 && coinVisual != null) coinVisual.transform.position = dropPosition;
        }
        private void Refresh()
        {
            if (coinVisual == null) return;
            coinVisual.transform.position = dropPosition;
            coinVisual.SetActive(amount > 0);
            PickupPresentation.ShowCoinAmount(coinVisual, amount);
        }
        public bool TryCollect(PlayerHealth player)
        {
            var game = GameSession.Instance;
            if (!isActiveAndEnabled || game == null || game.IsLoading || player == null || !player.IsAlive ||
                amount <= 0 || coinVisual == null || Vector2.Distance(player.transform.position, dropPosition) > .9f ||
                !WorldAccess.IsClear(player, coinVisual.transform, hits)) return false;
            int reward = amount; amount = 0; // Consume before publishing the wallet event.
            if (!game.Rewards.TryCollectEnemyCoins(reward)) { amount = reward; return false; }
            PickupPresentation.Collected(coinVisual, player);
            Refresh(); return true;
        }
        public void ResetOnRest() { rolled = false; amount = 0; Refresh(); }
        private sealed class Snapshot
        {
            internal bool rolled;
            internal int amount;
            internal Vector2 position;
        }
        public object CaptureVisitState() => new Snapshot { rolled = rolled, amount = amount, position = dropPosition };
        public void RestoreVisitState(object state)
        {
            if (!(state is Snapshot snapshot)) return;
            rolled = snapshot.rolled; amount = snapshot.amount; dropPosition = snapshot.position; Refresh();
        }
    }
}
