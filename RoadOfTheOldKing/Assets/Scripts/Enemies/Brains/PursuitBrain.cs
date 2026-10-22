using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    public enum PursuitState { Idle, Pursuing, Windup, Striking, Recovering, Returning, Defeated }

    [DisallowMultipleComponent, RequireComponent(typeof(Damageable), typeof(HitReaction), typeof(Rigidbody2D))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "RoadOfTheOldKing.Combat", null, "SimpleMeleeEnemy")]
    public sealed class PursuitBrain : MonoBehaviour, IEnemy, IEnemyFacing, IResetOnRest, ISceneVisitParticipant
    {
        [SerializeField] private PlayerHealth target;
        [Tooltip("Optional authored action. Unassigned legacy enemies retain their serialized melee tuning.")]
        [SerializeField] private EnemyAttackDefinition attack;
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.1f;
        [SerializeField, Min(0.1f)] private float noticeRadius = 5f;
        [Tooltip("Gives up and walks home when it strays this far from home.")]
        [SerializeField, Min(0.1f)] private float leashRadius = 5.5f;
        [Tooltip("Seconds without line of sight before it gives up the chase.")]
        [SerializeField, Min(0f)] private float loseSightTime = 2f;
        [SerializeField, Min(0.1f)] private float attackDistance = 1.25f;
        [SerializeField, Min(0.1f)] private float attackReach = 1.65f;
        [SerializeField, Range(1f, 180f)] private float attackArc = 85f;
        [SerializeField, Min(0.05f)] private float windupDuration = 0.65f;
        [SerializeField, Min(0.02f)] private float strikeDuration = 0.12f;
        [SerializeField, Min(0.05f)] private float recoveryDuration = 0.9f;
        [SerializeField, Min(1)] private int damage = 20;
        [Tooltip("Pause after a weapon stagger before acting again. The knockback keeps sliding through it.")]
        [SerializeField, Min(0f)] private float hitRecovery = 0.1f;
        private EnemyBody actor;
        private Damageable health;
        private HitReaction reaction;
        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private const float ReengageFraction = 0.6f;
        private Vector2 home;
        private float remaining, lastSeen;
        private bool provoked;
        private float stateDuration;
        private EnemyActionRunner action;
        private bool knockedBack;

        public PursuitState State { get; private set; }
        public Vector2 FacingDirection { get; private set; } = Vector2.down;
        public float AttackReach => Settings.reach;
        public float AttackArc => Settings.arc;
        public float StateProgress => action != null && action.IsRunning ? action.Progress : stateDuration > 0f ? Mathf.Clamp01(1f - remaining / stateDuration) : 0f;
        public bool IsChargeAttack => Settings.chargeSpeed > 0;
        public bool IsLaneLocked => action != null && action.IsLocked;
        public Vector2 AttackOrigin => action != null && State == PursuitState.Striking ? action.Origin : (Vector2)transform.position;
        public float ChargeDistance => Settings.chargeSpeed * Settings.active;
        public EnemyActionRunner Action => action;
        public bool TryBeginAttack(EnemyAttackSpec settings, PlayerHealth victim, Vector2 direction) =>
            !action.IsRunning && action.Begin(new MeleeAttackAction(gameObject, body, bodyCollider, HasLineOfSight, settings, victim, direction));
        private EnemyAttackSpec Settings => attack != null ? attack.settings : new EnemyAttackSpec
        { windup = windupDuration, active = strikeDuration, recovery = recoveryDuration, reach = attackReach,
          arc = attackArc, damage = damage, knockback = 2.4f, stagger = .18f };
        public int ResetVersion { get; private set; }
        public bool IsAware { get; private set; }
        public bool IsDefeated => State == PursuitState.Defeated;
        public event System.Action PlayerDetected;

        private void Awake()
        {
            actor = new EnemyBody(gameObject);
            health = actor.Health; reaction = actor.Reaction; body = actor.Rigidbody; bodyCollider = actor.Collider;
            action = new EnemyActionRunner();
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
            reaction.Staggered += OnHit;
            health.HitReceived += OnProvoked;
            health.Defeated += OnDefeated;
        }

        private void OnDisable()
        {
            EnemyRegistry.Unregister(this);
            IsAware = false;
            reaction.Staggered -= OnHit;
            health.HitReceived -= OnProvoked;
            health.Defeated -= OnDefeated;
            action.Cancel();
            if (!IsDefeated) SetState(PursuitState.Idle);
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }

        private void FixedUpdate() => Tick(Time.fixedDeltaTime);

        private void Tick(float deltaTime)
        {
            if (State == PursuitState.Defeated)
            {
                SlideCorpse(deltaTime);
                return;
            }
            if (deltaTime <= 0f)
                return;
            if (target == null || !target.IsAlive)
            {
                IsAware = false;
                SetState(PursuitState.Idle);
                return;
            }
            // Do not overwrite weapon knockback, and never strike while staggered.
            if (reaction.IsStaggered)
            {
                action.Cancel(false);
                return;
            }

            remaining -= deltaTime;
            if (action.IsRunning)
            {
                action.Tick(deltaTime);
                FacingDirection = action.Direction;
                State = action.Phase == EnemyAttackPhase.Windup ? PursuitState.Windup :
                    action.Phase == EnemyAttackPhase.Active ? PursuitState.Striking :
                    action.Phase == EnemyAttackPhase.Recovery ? PursuitState.Recovering : PursuitState.Idle;
                if (action.IsRunning) return;
            }
            if (State == PursuitState.Recovering)
            {
                // After a weapon hit the knockback slides out under damping instead of stopping dead.
                if (!knockedBack) body.linearVelocity = Vector2.zero;
                if (remaining > 0f)
                    return;
                SetState(PursuitState.Idle);
            }

            // Once aware it keeps chasing until the player is out of sight for a while or it strays past its
            // own leash; then it walks home, and takes the player up again if struck or followed back in.
            Vector2 toTarget = (Vector2)target.transform.position - body.position;
            float fromHome = Vector2.Distance(body.position, home);
            bool sees = toTarget.magnitude <= noticeRadius * 1.5f && HasLineOfSight(target.transform.position);
            if (sees) lastSeen = Time.time;
            if (provoked && fromHome <= leashRadius) Engage();
            provoked = false;
            if (State == PursuitState.Returning)
            {
                if (sees && toTarget.magnitude <= noticeRadius && fromHome <= leashRadius * ReengageFraction)
                    Engage();
                else
                {
                    Vector2 toHome = home - body.position;
                    if (toHome.magnitude <= 0.15f) SetState(PursuitState.Idle);
                    else Move(toHome);
                    return;
                }
            }
            if (IsAware && (fromHome > leashRadius || Time.time - lastSeen > loseSightTime))
            {
                IsAware = false;
                State = PursuitState.Returning;
                return;
            }
            if (!IsAware)
            {
                if (!sees || toTarget.magnitude > noticeRadius)
                {
                    // Idle away from home (e.g. after a stagger): head back.
                    if (fromHome > 0.15f) State = PursuitState.Returning;
                    else SetState(PursuitState.Idle);
                    return;
                }
                Engage();
            }
            if (toTarget.magnitude <= attackDistance)
            {
                FacingDirection = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : FacingDirection;
                if (TryBeginAttack(Settings, target, FacingDirection)) State = PursuitState.Windup;
            }
            else
            {
                State = PursuitState.Pursuing;
                Move(toTarget);
            }
        }

        private void Engage()
        {
            if (State == PursuitState.Returning) State = PursuitState.Idle;
            lastSeen = Time.time;
            if (IsAware) return;
            IsAware = true;
            PlayerDetected?.Invoke();
        }

        private void Move(Vector2 direction)
        {
            FacingDirection = direction.normalized;
            body.linearVelocity = FacingDirection * moveSpeed;
        }

        private bool HasLineOfSight(Vector2 point) => actor.CanSee(point, target != null ? target.transform : null, false);

        private void OnHit(CombatHit hit)
        {
            if (health.IsAlive && hit.StaggerDuration > 0f)
            {
                // Keep the impulse already applied by HitReaction; the stagger itself runs first.
                State = PursuitState.Recovering;
                remaining = hitRecovery;
                stateDuration = remaining;
                action.Cancel(false);
                knockedBack = true;
            }
        }

        // Any hit, staggering or not, makes it take the player up again (within its leash).
        private void OnProvoked(CombatHit hit)
        {
            if (health.IsAlive) provoked = true;
        }

        private void OnDefeated()
        {
            IsAware = false;
            // Carry the killing blow's knockback into a short corpse slide. The collider switches off
            // at once so throws and the player pass through, so the slide stops at walls by casting.
            actor.BeginDefeat();
            SetState(PursuitState.Defeated);
        }

        private void SlideCorpse(float dt) => actor.SlideCorpse(dt);

        public object CaptureVisitState() => actor.CaptureVisitState();

        public void RestoreVisitState(object state)
        {
            if (!(state is EnemyVisitState snapshot)) return;
            ResetOnRest();
            actor.RestoreVisitHealth(snapshot);
            if (health.IsAlive) return;
            OnDefeated();
        }

        public void ResetOnRest()
        {
            IsAware = provoked = false;
            ResetVersion++;
            actor.Reset();
            SetState(PursuitState.Idle);
        }

        private void SetState(PursuitState state, float duration = 0f)
        {
            State = state;
            remaining = duration;
            stateDuration = duration;
            action.Cancel();
            knockedBack = false;
            body.linearVelocity = Vector2.zero;
        }
    }
}
