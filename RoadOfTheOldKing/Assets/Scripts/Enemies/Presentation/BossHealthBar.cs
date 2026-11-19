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
        [Tooltip("Announced when it falls in a fight (user, 17 Nov). Empty: nothing.")]
        [SerializeField] private string defeatAnnouncement = "GUARDIAN FALLEN";
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
            // The arena's own test, so the bar and the walls come and go together.
            bool fighting = brain == null ? health.IsAlive : !brain.IsDormant;
            if (brain != null && brain.IsDefeated && Engaged != this) fighting = false; // already fallen before the fight
            if (fighting) Engaged = this;
            else if (Engaged == this) Engaged = null;
        }

        private void OnEnable() => health.Defeated += Announce;

        private void OnDisable()
        {
            health.Defeated -= Announce;
            if (Engaged == this) Engaged = null;
        }

        // Only a fall in the fight: a boss restored as already fallen was never engaged.
        private void Announce()
        {
            if (Engaged == this && !string.IsNullOrEmpty(defeatAnnouncement))
                RoadOfTheOldKing.UI.HudNotifications.ShowBanner(defeatAnnouncement);
        }
    }
}
