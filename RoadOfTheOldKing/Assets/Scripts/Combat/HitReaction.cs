using System;
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

        [Header("Poise")]
        [Tooltip("Damage absorbed before a hit staggers. 0 = every staggering hit staggers (player, dummies).")]
        [SerializeField, Min(0f)] private float poise;
        [Tooltip("Seconds without being hit before absorbed damage resets.")]
        [SerializeField, Min(0f)] private float poiseResetDelay = 2.5f;
        [Tooltip("Share of knockback kept by a hit that doesn't stagger.")]
        [SerializeField, Range(0f, 1f)] private float unstaggeredKnockback = .2f;
        [Tooltip("Share of a thrown or Recall hit's damage that counts toward breaking poise; melee breaks guards.")]
        [SerializeField, Range(0f, 1f)] private float rangedPoiseMultiplier = .5f;

        private Damageable health;
        private Rigidbody2D body;
        private float staggerUntil, poiseDamage, lastHitAt = float.NegativeInfinity;
        public bool IsStaggered => Time.time < staggerUntil;
        // Hyper-armor, set by the owner's AI during committed attacks: hits still damage but never stagger.
        public bool Armored { get; set; }
        public float Poise => poise;
        public float PoiseRemaining => poise > 0f ? Mathf.Max(0f, poise - poiseDamage) : 0f;
        // Raised when a hit actually staggers (poise broken or no poise).
        public event Action<CombatHit> Staggered;

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
            poiseDamage = 0f;
            Armored = false;
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }

        private void React(CombatHit hit)
        {
            // Health is already reduced when HitReceived fires, so a dead target took the killing blow.
            bool lethal = !health.IsAlive;
            bool staggers = hit.StaggerDuration > 0f && (lethal || !Armored);
            if (staggers && poise > 0f && !lethal)
            {
                if (Time.time - lastHitAt > poiseResetDelay) poiseDamage = 0f;
                bool ranged = hit.Kind == AttackKind.Throw || hit.Kind == AttackKind.Recall;
                poiseDamage += hit.Damage * (ranged ? rangedPoiseMultiplier : 1f);
                staggers = poiseDamage >= poise;
                if (staggers) poiseDamage = 0f;
            }
            lastHitAt = Time.time;
            if (staggers)
                staggerUntil = Mathf.Max(staggerUntil, Time.time + hit.StaggerDuration);
            // Poise or armor soaked a hit that would have staggered: only a nudge of knockback.
            bool absorbed = hit.StaggerDuration > 0f && !staggers;

            if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
            {
                float impulse = Mathf.Max(0f, hit.Knockback * knockbackMultiplier);
                if (maxKnockbackImpulse > 0f) impulse = Mathf.Min(impulse, maxKnockbackImpulse);
                if (lethal) impulse *= lethalKnockbackMultiplier;
                else if (absorbed) impulse *= unstaggeredKnockback;
                if (impulse > 0f)
                {
                    // Approaching enemies must not absorb the impulse into forward movement.
                    if (replaceMovementOnHit && !absorbed) body.linearVelocity = Vector2.zero;
                    body.AddForce(hit.Direction * impulse, ForceMode2D.Impulse);
                }
            }
            if (staggers) Staggered?.Invoke(hit);
        }
    }
}
