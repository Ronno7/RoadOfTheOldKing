using UnityEngine;

namespace TheLostShrine.Combat
{
    [DisallowMultipleComponent, RequireComponent(typeof(Damageable))]
    public sealed class HitReaction : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float knockbackMultiplier = 1f;
        [SerializeField, Min(0f)] private float maxKnockbackImpulse;
        [SerializeField] private bool replaceMovementOnHit;
        [Tooltip("Extra knockback on the killing blow, so defeats land harder.")]
        [SerializeField, Min(1f)] private float lethalKnockbackMultiplier = 1.6f;
        private Damageable health;
        private Rigidbody2D body;
        private float staggerUntil;
        public bool IsStaggered => Time.time < staggerUntil;

        private void Awake()
        {
            health = GetComponent<Damageable>();
            body = GetComponent<Rigidbody2D>();
        }

        private void OnEnable() => health.HitReceived += React;
        private void OnDisable() => health.HitReceived -= React;

        public void Clear()
        {
            staggerUntil = 0f;
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }

        private void React(CombatHit hit)
        {
            staggerUntil = Mathf.Max(staggerUntil, Time.time + hit.StaggerDuration);
            if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
            {
                float impulse = Mathf.Max(0f, hit.Knockback * knockbackMultiplier);
                if (maxKnockbackImpulse > 0f) impulse = Mathf.Min(impulse, maxKnockbackImpulse);
                // Health is already reduced when HitReceived fires, so a dead target took the killing blow.
                if (!health.IsAlive) impulse *= lethalKnockbackMultiplier;
                if (impulse <= 0f) return;
                // Approaching enemies must not absorb the impulse into forward movement.
                if (replaceMovementOnHit) body.linearVelocity = Vector2.zero;
                body.AddForce(hit.Direction * impulse, ForceMode2D.Impulse);
            }
        }
    }
}
