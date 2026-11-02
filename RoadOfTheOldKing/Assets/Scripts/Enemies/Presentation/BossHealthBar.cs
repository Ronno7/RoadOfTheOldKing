using RoadOfTheOldKing.Combat;
using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    // Marks an enemy as a boss: the HUD shows its name and a long health bar along the bottom of the screen
    // (Dark Souls style) from the moment it wakes until it falls or returns to rest. Its world-space bar is hidden.
    [DisallowMultipleComponent, RequireComponent(typeof(Damageable))]
    public sealed class BossHealthBar : MonoBehaviour
    {
        [SerializeField] private string bossName = "Broken Watcher";
        private Damageable health;
        private PursuitBrain brain;

        // The boss currently fought, if any. One boss at a time; a later engagement replaces an earlier one.
        public static BossHealthBar Engaged { get; private set; }
        public string BossName => bossName;
        public float Fraction => Mathf.Clamp01((float)health.Health / Mathf.Max(1, health.MaxHealth));
        public bool IsAlive => health.IsAlive;

        private void Awake()
        {
            health = GetComponent<Damageable>();
            brain = GetComponent<PursuitBrain>();
            // The boss bar replaces the small bar over its head.
            var overhead = GetComponent<EnemyHealthIndicator>();
            if (overhead != null) overhead.enabled = false;
        }

        private void Update()
        {
            // Engaged from the wake until it rests again; a fall keeps it engaged so the HUD can drain the bar.
            bool fighting = brain == null ? health.IsAlive : !brain.IsDormant && (brain.IsWaking || brain.IsAware || brain.IsDefeated);
            if (brain != null && brain.IsDefeated && Engaged != this) fighting = false; // already fallen before the fight
            if (fighting) Engaged = this;
            else if (Engaged == this) Engaged = null;
        }

        private void OnDisable() { if (Engaged == this) Engaged = null; }
    }
}
