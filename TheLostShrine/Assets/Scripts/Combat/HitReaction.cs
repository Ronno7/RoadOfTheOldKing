using UnityEngine;

namespace TheLostShrine.Combat
{
    [DisallowMultipleComponent, RequireComponent(typeof(Damageable))]
    public sealed class HitReaction : MonoBehaviour
    {
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

        private void React(CombatHit hit)
        {
            staggerUntil = Mathf.Max(staggerUntil, Time.time + hit.StaggerDuration);
            if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
                body.AddForce(hit.Direction * hit.Knockback, ForceMode2D.Impulse);
        }
    }
}
