using System.Collections.Generic;
using TheLostShrine.Player;
using TheLostShrine.Progression;
using UnityEngine;

namespace TheLostShrine.Combat
{
    public enum MeleeEnemyState { Idle, Pursuing, Windup, Striking, Recovering, Returning, Defeated }

    [DisallowMultipleComponent, RequireComponent(typeof(Damageable), typeof(HitReaction), typeof(Rigidbody2D))]
    public sealed class SimpleMeleeEnemy : MonoBehaviour, IResetOnRest
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
        [SerializeField, Min(1)] private int damage = 1;
        private readonly ContactFilter2D solidFilter = new ContactFilter2D { useTriggers = false };
        private readonly List<RaycastHit2D> sight = new List<RaycastHit2D>(12);
        private Damageable health;
        private HitReaction reaction;
        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private Vector2 home;
        private float remaining;
        private bool struck;

        public MeleeEnemyState State { get; private set; }
        public Vector2 FacingDirection { get; private set; } = Vector2.down;
        public float AttackReach => attackReach;
        public float AttackArc => attackArc;

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
            health.HitReceived += OnHit;
            health.Defeated += OnDefeated;
        }

        private void OnDisable()
        {
            health.HitReceived -= OnHit;
            health.Defeated -= OnDefeated;
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }

        private void FixedUpdate() => Tick(Time.fixedDeltaTime);

        private void Tick(float deltaTime)
        {
            if (deltaTime <= 0f || State == MeleeEnemyState.Defeated)
                return;
            if (target == null || !target.IsAlive)
            {
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
                body.linearVelocity = Vector2.zero;
                if (remaining > 0f)
                    return;
                SetState(MeleeEnemyState.Idle);
            }

            Vector2 toTarget = (Vector2)target.transform.position - body.position;
            bool outsideHome = Vector2.Distance(target.transform.position, home) > leashRadius ||
                Vector2.Distance(body.position, home) > leashRadius;
            if (State == MeleeEnemyState.Returning || outsideHome)
            {
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
                SetState(MeleeEnemyState.Idle);
                return;
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
                // Keep the impulse already applied by HitReaction.
                State = MeleeEnemyState.Recovering;
                remaining = Mathf.Max(recoveryDuration, hit.StaggerDuration);
            }
        }

        private void OnDefeated()
        {
            SetState(MeleeEnemyState.Defeated);
            if (bodyCollider != null)
                bodyCollider.enabled = false;
        }

        public void ResetOnRest()
        {
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
            struck = false;
            body.linearVelocity = Vector2.zero;
        }
    }
}
