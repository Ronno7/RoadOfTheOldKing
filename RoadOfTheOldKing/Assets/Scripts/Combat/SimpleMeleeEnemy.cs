using System.Collections.Generic;
using TheLostShrine.Player;
using TheLostShrine.Progression;
using UnityEngine;

namespace TheLostShrine.Combat
{
    public enum MeleeEnemyState { Idle, Pursuing, Windup, Striking, Recovering, Returning, Defeated }

    [DisallowMultipleComponent, RequireComponent(typeof(Damageable), typeof(HitReaction), typeof(Rigidbody2D))]
    public sealed class SimpleMeleeEnemy : MonoBehaviour, IEnemy, IResetOnRest
    {
        [SerializeField] private PlayerHealth target;
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.1f;
        [SerializeField, Min(0.1f)] private float noticeRadius = 5f;
        [SerializeField, Min(0.1f)] private float leashRadius = 5.5f;
        [SerializeField, Min(0.1f)] private float attackDistance = 1.25f;
        [SerializeField, Min(0.1f)] private float attackReach = 1.65f;
        [SerializeField, Range(1f, 180f)] private float attackArc = 85f;
        [SerializeField, Min(0.05f)] private float windupDuration = 0.65f;
        [SerializeField, Min(0.02f)] private float strikeDuration = 0.12f;
        [SerializeField, Min(0.05f)] private float recoveryDuration = 0.9f;
        [SerializeField, Min(1)] private int damage = 20;
        [Tooltip("Pause after a weapon stagger before acting again. The knockback keeps sliding through it.")]
        [SerializeField, Min(0f)] private float hitRecovery = 0.1f;
        private readonly ContactFilter2D solidFilter = new ContactFilter2D { useTriggers = false };
        private readonly List<RaycastHit2D> sight = new List<RaycastHit2D>(12);
        private Damageable health;
        private HitReaction reaction;
        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private Vector2 home;
        private float remaining;
        private float stateDuration;
        private bool struck;
        private bool knockedBack;
        private Vector2 deathSlide;
        private readonly RaycastHit2D[] slideHits = new RaycastHit2D[8];

        public MeleeEnemyState State { get; private set; }
        public Vector2 FacingDirection { get; private set; } = Vector2.down;
        public float AttackReach => attackReach;
        public float AttackArc => attackArc;
        public float StateProgress => stateDuration > 0f ? Mathf.Clamp01(1f - remaining / stateDuration) : 0f;
        public int ResetVersion { get; private set; }
        public bool IsAware { get; private set; }
        public bool IsDefeated => State == MeleeEnemyState.Defeated;
        public event System.Action PlayerDetected;

        private void Awake()
        {
            health = GetComponent<Damageable>();
            reaction = GetComponent<HitReaction>();
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            home = body.position;
        }

        private void Start()
        {
            if (target == null)
                target = FindFirstObjectByType<PlayerHealth>();
        }

        private void OnEnable()
        {
            EnemyRegistry.Register(this);
            health.HitReceived += OnHit;
            health.Defeated += OnDefeated;
        }

        private void OnDisable()
        {
            EnemyRegistry.Unregister(this);
            IsAware = false;
            health.HitReceived -= OnHit;
            health.Defeated -= OnDefeated;
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }

        private void FixedUpdate() => Tick(Time.fixedDeltaTime);

        private void Tick(float deltaTime)
        {
            if (State == MeleeEnemyState.Defeated)
            {
                SlideCorpse(deltaTime);
                return;
            }
            if (deltaTime <= 0f)
                return;
            if (target == null || !target.IsAlive)
            {
                IsAware = false;
                SetState(MeleeEnemyState.Idle);
                return;
            }
            // Do not overwrite weapon knockback, and never strike while staggered.
            if (reaction.IsStaggered)
                return;

            remaining -= deltaTime;
            if (State == MeleeEnemyState.Windup)
            {
                if (remaining <= 0f)
                    SetState(MeleeEnemyState.Striking, strikeDuration);
                return;
            }
            if (State == MeleeEnemyState.Striking)
            {
                if (!struck)
                    TryStrike();
                if (remaining <= 0f)
                    SetState(MeleeEnemyState.Recovering, recoveryDuration);
                return;
            }
            if (State == MeleeEnemyState.Recovering)
            {
                // After a weapon hit the knockback slides out under damping instead of stopping dead.
                if (!knockedBack) body.linearVelocity = Vector2.zero;
                if (remaining > 0f)
                    return;
                SetState(MeleeEnemyState.Idle);
            }

            Vector2 toTarget = (Vector2)target.transform.position - body.position;
            bool outsideHome = Vector2.Distance(target.transform.position, home) > leashRadius ||
                Vector2.Distance(body.position, home) > leashRadius;
            if (State == MeleeEnemyState.Returning || outsideHome)
            {
                IsAware = false;
                State = MeleeEnemyState.Returning;
                Vector2 toHome = home - body.position;
                if (toHome.magnitude <= 0.15f)
                    SetState(MeleeEnemyState.Idle);
                else
                    Move(toHome);
                return;
            }
            if (toTarget.magnitude > noticeRadius || !HasLineOfSight(target.transform.position))
            {
                IsAware = false;
                SetState(MeleeEnemyState.Idle);
                return;
            }
            if (!IsAware)
            {
                IsAware = true;
                PlayerDetected?.Invoke();
            }
            if (toTarget.magnitude <= attackDistance)
            {
                FacingDirection = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : FacingDirection;
                SetState(MeleeEnemyState.Windup, windupDuration);
            }
            else
            {
                State = MeleeEnemyState.Pursuing;
                Move(toTarget);
            }
        }

        private void Move(Vector2 direction)
        {
            FacingDirection = direction.normalized;
            body.linearVelocity = FacingDirection * moveSpeed;
        }

        private bool HasLineOfSight(Vector2 point)
        {
            Physics2D.Linecast(body.position, point, solidFilter, sight);
            foreach (var hit in sight)
                if (hit.collider != null && !hit.transform.IsChildOf(transform) &&
                    !hit.transform.IsChildOf(target.transform))
                    return false;
            return true;
        }

        private void TryStrike()
        {
            var collider = target.GetComponent<Collider2D>();
            if (collider == null || !collider.enabled)
                return;
            Vector2 point = collider.ClosestPoint(body.position);
            Vector2 direction = point - body.position;
            if (direction.magnitude > attackReach ||
                (direction.sqrMagnitude > 0.001f && Vector2.Angle(FacingDirection, direction) > attackArc * 0.5f) ||
                !HasLineOfSight(point))
                return;
            // One damage attempt per swing, even when the player is invulnerable.
            struck = true;
            target.Health.ReceiveHit(new CombatHit(gameObject, AttackKind.EnemyMelee, damage,
                FacingDirection, 2.4f, 0.18f, false, point));
        }

        private void OnHit(CombatHit hit)
        {
            if (health.IsAlive && hit.StaggerDuration > 0f)
            {
                // Keep the impulse already applied by HitReaction; the stagger itself runs first.
                State = MeleeEnemyState.Recovering;
                remaining = hitRecovery;
                stateDuration = remaining;
                struck = false;
                knockedBack = true;
            }
        }

        private void OnDefeated()
        {
            IsAware = false;
            // Carry the killing blow's knockback into a short corpse slide. The collider switches off
            // at once so throws and the player pass through, so the slide stops at walls by casting.
            deathSlide = body.linearVelocity;
            SetState(MeleeEnemyState.Defeated);
            if (bodyCollider != null)
                bodyCollider.enabled = false;
        }

        private void SlideCorpse(float deltaTime)
        {
            if (deltaTime <= 0f || deathSlide.sqrMagnitude < 0.0004f)
                return;
            Vector2 step = deathSlide * deltaTime;
            float distance = step.magnitude;
            float radius = bodyCollider is CircleCollider2D circle ? circle.radius * 0.8f : 0.3f;
            int count = Physics2D.CircleCast(body.position, radius, step / distance, solidFilter, slideHits, distance);
            for (int i = 0; i < count; i++)
            {
                var other = slideHits[i].collider;
                // Characters and dynamic props do not stop a corpse; terrain and static props do.
                if (other == bodyCollider || (other.attachedRigidbody != null && other.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic))
                    continue;
                distance = Mathf.Min(distance, Mathf.Max(0f, slideHits[i].distance - 0.02f));
                deathSlide = Vector2.zero;
            }
            body.position += step.normalized * distance;
            body.linearVelocity = Vector2.zero;
            deathSlide *= Mathf.Exp(-body.linearDamping * deltaTime);
        }

        public void ResetOnRest()
        {
            IsAware = false;
            ResetVersion++;
            deathSlide = Vector2.zero;
            health.RestoreHealth();
            reaction.Clear();
            body.position = home;
            transform.position = home;
            if (bodyCollider != null)
                bodyCollider.enabled = true;
            SetState(MeleeEnemyState.Idle);
        }

        private void SetState(MeleeEnemyState state, float duration = 0f)
        {
            State = state;
            remaining = duration;
            stateDuration = duration;
            struck = false;
            knockedBack = false;
            body.linearVelocity = Vector2.zero;
        }
    }
}
