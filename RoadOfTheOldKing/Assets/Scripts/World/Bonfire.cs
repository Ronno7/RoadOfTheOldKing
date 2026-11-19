using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.World
{
    [DisallowMultipleComponent]
    public sealed class Bonfire : MonoBehaviour
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName = "Bonfire";
        [SerializeField] private int displayOrder;
        [SerializeField] private bool allowsUpgrades;
        public bool AllowsUpgrades => allowsUpgrades;
        [Tooltip("Regional bonfire menu skin: BonfireMenu puts skin-<name> on its panel (UI/BonfireMenu.uss). Same layout everywhere; only the frame changes.")]
        [SerializeField] private string menuSkin = "hearth";
        public string MenuSkin => string.IsNullOrEmpty(menuSkin) ? "hearth" : menuSkin;
        [SerializeField] private Transform spawnPoint;
        [SerializeField, Min(0.1f)] private float interactionRadius = 1.7f;
        [Header("Heat")]
        [Tooltip("A lit fire burns the hero in slow, small ticks while his feet are in its drawn flame (BonfireSpriteView.HeatArea), " +
            "like a World of Warcraft campfire: each tick is a real hit (flinch, flash, shake, rumble) without a stagger or push " +
            "(user, 19 Nov: \"it also burns too quickly and for too much\").")]
        [SerializeField, Min(0)] private int heatDamage = 2;
        [SerializeField, Min(.1f)] private float heatInterval = 1.5f;
        private float heatClock;
        private BonfireSpriteView view;
        public string Id => id;
        public string DisplayName => displayName;
        public int DisplayOrder => displayOrder;
        // Travel groups fires by the region they stand in.
        public string Region => AreaName.RegionAt(transform.position);
        public Vector2 SpawnPosition => spawnPoint != null ? (Vector2)spawnPoint.position : (Vector2)transform.position;
        public bool IsDiscovered => GameSession.Instance != null && GameSession.Instance.Progress.HasFire(id);
        // The moment the player lights it (never on load or travel): BonfireSpriteView plays the ignition.
        public event System.Action Lit;
        internal void NotifyLit() => Lit?.Invoke();
        public bool CanUse(Transform player) => !string.IsNullOrEmpty(id) && player != null &&
            Vector2.Distance(player.position, transform.position) <= interactionRadius;

        private void Update() => Burn(Time.deltaTime);

        // The first touch burns at once; staying in burns again every heatInterval.
        private void Burn(float elapsed)
        {
            var session = GameSession.Instance;
            var player = session != null ? session.Player : null;
            if (view == null) view = GetComponent<BonfireSpriteView>();
            if (player == null || !player.IsAlive || heatDamage <= 0 || !IsDiscovered || view == null ||
                !view.HeatArea.Contains((Vector2)transform.InverseTransformPoint(player.transform.position)))
            {
                heatClock = 0f;
                return;
            }
            heatClock -= elapsed;
            if (heatClock > 0f) return;
            heatClock = heatInterval;
            // Away from the fire: the direction the camera's small kick takes; the burn itself never pushes.
            Vector2 away = player.transform.position - transform.position;
            player.Health.ReceiveHit(new CombatHit(gameObject, AttackKind.Burn, heatDamage,
                away.sqrMagnitude > .0001f ? away.normalized : Vector2.down, 0f, 0f));
        }

#if UNITY_EDITOR
        // Ticking or unticking upgrades swaps the saved base between the stone ring and the bronze brazier.
        private void OnValidate() { if (TryGetComponent(out BonfireSpriteView view)) view.SyncSavedBase(); }
#endif
    }
}
