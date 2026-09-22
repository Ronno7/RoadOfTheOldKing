using TheLostShrine.Combat;
using TheLostShrine.Input;
using UnityEngine;

namespace TheLostShrine.Player
{
    // Player-specific damage protection and death response reuse the shared health model.
    [DisallowMultipleComponent, RequireComponent(typeof(Damageable), typeof(HitReaction))]
    public sealed class PlayerHealth : MonoBehaviour, IHitProtection
    {
        [SerializeField, Min(0f)] private float invulnerabilityDuration = 0.8f;
        private Damageable health;
        private float invulnerableUntil;

        public Damageable Health => health;
        public bool IsAlive => health != null && health.IsAlive;
        public bool IsInvulnerable => Time.time < invulnerableUntil;

        private void Awake() => health = GetComponent<Damageable>();
        private void OnEnable()
        {
            health.HitReceived += OnHit;
            health.Defeated += OnDefeated;
        }

        private void OnDisable()
        {
            health.HitReceived -= OnHit;
            health.Defeated -= OnDefeated;
        }

        public bool Blocks(CombatHit hit) => IsInvulnerable;
        private void OnHit(CombatHit hit) => invulnerableUntil = Time.time + invulnerabilityDuration;

        public void HealAtRest()
        {
            if (!IsAlive)
                return;
            health.RestoreHealth();
            invulnerableUntil = 0f;
            GetComponent<HitReaction>().Clear();
        }

        private void OnDefeated()
        {
            var combat = GetComponent<PlayerCombatController>();
            if (combat != null)
            {
                if (combat.Weapon != null)
                    combat.Weapon.CancelAction();
                combat.enabled = false;
            }
            Disable<PlayerMovement>();
            Disable<PlayerMovementInput>();
            Disable<PlayerCombatInput>();
            var body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
            }
        }

        private void Disable<T>() where T : Behaviour
        {
            var component = GetComponent<T>();
            if (component != null)
                component.enabled = false;
        }
    }
}
