using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using UnityEngine;

namespace RoadOfTheOldKing.Enemies
{
    public enum PursuitState { Idle, Pursuing, Windup, Striking, Recovering, Returning, Defeated, Watching, Holding }

    [DisallowMultipleComponent, RequireComponent(typeof(Damageable), typeof(HitReaction), typeof(Rigidbody2D))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "RoadOfTheOldKing.Combat", null, "SimpleMeleeEnemy")]
    public sealed class PursuitBrain : MonoBehaviour, IEnemy, IEnemyFacing, IResetOnRest, ISceneVisitParticipant
    {
        [SerializeField] private PlayerHealth target;
        [Tooltip("Optional authored action. Unassigned legacy enemies retain their serialized melee tuning.")]
        [SerializeField] private EnemyAttackDefinition attack;
        [SerializeField, Min(0.1f)] private float moveSpeed = 2.1f;
        [SerializeField, Min(0.1f)] private float noticeRadius = 5f;
        [Tooltip("Continuous visible exposure before pursuit. Zero preserves instant detection.")]
        [SerializeField, Min(0f)] private float watchDuration;
        [Tooltip("Seconds to lose full suspicion after the player leaves sight or notice range.")]
        [SerializeField, Min(.05f)] private float forgetDuration = .6f;
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

        [Header("Duel sequence (optional)")]
        [Tooltip("Committed close attack: chosen when the player is close, and the single follow-up when the main attack misses a player still in its reach. Unassigned keeps one attack.")]
        [SerializeField] private EnemyAttackDefinition closeAttack;
        [Tooltip("Share of the close attack's reach inside which it opens with the close attack instead of the main one.")]
        [SerializeField, Range(.1f, 1f)] private float closeRangeFraction = .75f;
        [Tooltip("A missed main attack continues into the close attack when the player is still in its reach. Off: the main attack's recovery is always the opening.")]
        [SerializeField] private bool followUpOnMiss = true;
        [Tooltip("Distance kept, facing the player, during its cooldown between attack sequences. Zero closes straight in.")]
        [SerializeField, Min(0f)] private float holdDistance;
        [Tooltip("Pause after a finished attack sequence before the next; it keeps its distance meanwhile.")]
        [SerializeField, Min(0f)] private float attackCooldown;
        [Tooltip("Random extra cooldown, up to this many seconds, so a swarm doesn't strike in lockstep.")]
        [SerializeField, Min(0f)] private float cooldownJitter;
        private EnemyBody actor;
        private Damageable health;
        private HitReaction reaction;
        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private float remaining;
        private bool provoked;
        private float stateDuration;
        private EnemyActionRunner action;
        private bool knockedBack;
        private float suspicion;
        private EnemyAttackSpec current;
        private bool inSequence, followUpChecked;
        private float cooldown;
        private MeleeAttackAction melee;

        public PursuitState State { get; private set; }
        public Vector2 FacingDirection { get; private set; } = Vector2.down;
        public float AttackReach => Shown.reach;
        public float AttackArc => Shown.arc;
        public float StateProgress => action != null && action.IsRunning ? action.Progress : stateDuration > 0f ? Mathf.Clamp01(1f - remaining / stateDuration) : 0f;
        public bool IsChargeAttack => Shown.chargeSpeed > 0;
        public bool IsLaneLocked => action != null && action.IsLocked;
        public Vector2 AttackOrigin => action != null && State == PursuitState.Striking ? action.Origin : (Vector2)transform.position;
        public float ChargeDistance => Shown.chargeSpeed * Shown.active;
        public EnemyActionRunner Action => action;
        public bool IsCloseAttack { get; private set; }
        public bool IsFollowUp { get; private set; }
        public bool IsWallStunned => melee != null && action.IsRunning && melee.WallStunned;

        // Starts one attack outside the duel sequence (no follow-up); the sequence uses BeginSequenceAttack.
        public bool TryBeginAttack(EnemyAttackSpec settings, PlayerHealth victim, Vector2 direction)
        {
            if (action.IsRunning) return false;
            var next = new MeleeAttackAction(gameObject, body, bodyCollider, reaction, HasLineOfSight, settings, victim, direction);
            if (!action.Begin(next)) return false;
            melee = next;
            current = settings; IsCloseAttack = IsFollowUp = false; followUpChecked = true;
            return true;
        }
        private EnemyAttackSpec Settings => attack != null ? attack.settings : new EnemyAttackSpec
        { windup = windupDuration, active = strikeDuration, recovery = recoveryDuration, reach = attackReach,
          arc = attackArc, damage = damage, knockback = 2.4f, stagger = .18f };
        private EnemyAttackSpec CloseSettings => closeAttack != null ? closeAttack.settings : Settings;
        // The attack being shown: the one in progress, otherwise the main attack.
        private EnemyAttackSpec Shown => action != null && action.IsRunning ? current : Settings;
        public int ResetVersion { get; private set; }
        public bool IsAware { get; private set; }
        public float Suspicion => suspicion;
        public bool IsWatching => State == PursuitState.Watching && !IsAware;
        public bool HasWatchingStage => watchDuration > 0f;
        public bool IsDefeated => State == PursuitState.Defeated;
        public event System.Action PlayerDetected;

        private void Awake()
        {
            actor = new EnemyBody(gameObject);
            health = actor.Health; reaction = actor.Reaction; body = actor.Rigidbody; bodyCollider = actor.Collider;
            action = new EnemyActionRunner();
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
            EndSequence();
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
                EndSequence();
                SetState(PursuitState.Idle);
                return;
            }
            // Do not overwrite weapon knockback, and never strike while staggered.
            if (reaction.IsStaggered)
            {
                action.Cancel(false);
                EndSequence();
                return;
            }

            remaining -= deltaTime;
            cooldown = Mathf.Max(0f, cooldown - deltaTime);
            if (action.IsRunning)
            {
                action.Tick(deltaTime);
                FacingDirection = action.Direction;
                State = action.Phase == EnemyAttackPhase.Windup ? PursuitState.Windup :
                    action.Phase == EnemyAttackPhase.Active ? PursuitState.Striking :
                    action.Phase == EnemyAttackPhase.Recovery ? PursuitState.Recovering : PursuitState.Idle;
                // At most one follow-up, decided once as the recovery begins: a missed main attack with the
                // player still in reach continues into the close attack. Otherwise the recovery is the
                // promised opening and is never cancelled; its facing stays locked, exposing the flank.
                if (action.IsRunning && action.Phase == EnemyAttackPhase.Recovery && !followUpChecked)
                {
                    followUpChecked = true;
                    if (closeAttack != null && followUpOnMiss && !action.HasContact && CanFollowUp(out Vector2 toward))
                    {
                        action.Cancel(false);
                        if (BeginSequenceAttack(CloseSettings, toward, true)) { IsFollowUp = true; State = PursuitState.Windup; return; }
                    }
                }
                if (action.IsRunning) return;
            }
            if (inSequence) EndSequence();
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
            bool sees = actor.Sees(target.transform, noticeRadius, false);
            if (provoked && !actor.OutsideLeash(leashRadius)) Engage();
            provoked = false;
            if (State == PursuitState.Returning)
            {
                if (sees && toTarget.magnitude <= noticeRadius && actor.CanReengage(leashRadius))
                    Engage();
                else
                {
                    Vector2 toHome = actor.Home - body.position;
                    if (toHome.magnitude <= 0.15f) SetState(PursuitState.Idle);
                    else Move(toHome);
                    return;
                }
            }
            if (IsAware && (actor.OutsideLeash(leashRadius) || actor.LostSight(loseSightTime)))
            {
                IsAware = false;
                State = PursuitState.Returning;
                return;
            }
            if (!IsAware)
            {
                if (watchDuration > 0f)
                {
                    bool visible = sees && toTarget.magnitude <= noticeRadius;
                    suspicion = Mathf.Clamp01(suspicion + deltaTime * (visible ? 1f / watchDuration : -1f / Mathf.Max(.05f, forgetDuration)));
                    if (suspicion > 0f)
                    {
                        State = PursuitState.Watching;
                        body.linearVelocity = Vector2.zero;
                        if (visible && toTarget.sqrMagnitude > .001f) FacingDirection = toTarget.normalized;
                        if (suspicion < 1f) return;
                        Engage();
                    }
                    else if (State == PursuitState.Watching) SetState(PursuitState.Idle);
                }
                if (!IsAware)
                {
                    if (!sees || toTarget.magnitude > noticeRadius)
                    {
                        // Idle away from home (e.g. after a stagger): head back.
                        if (actor.DistanceFromHome > 0.15f) State = PursuitState.Returning;
                        else SetState(PursuitState.Idle);
                        return;
                    }
                    Engage();
                }
            }
            float distance = toTarget.magnitude;
            bool close = closeAttack != null && distance <= CloseSettings.reach * closeRangeFraction;
            bool ready = cooldown <= 0f;
            if (ready && (close || distance <= attackDistance))
            {
                FacingDirection = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized : FacingDirection;
                if (BeginSequenceAttack(close ? CloseSettings : Settings, FacingDirection, close)) State = PursuitState.Windup;
            }
            else if (!ready && holdDistance > 0f) Hold(distance, toTarget);
            else
            {
                State = PursuitState.Pursuing;
                Move(toTarget);
            }
        }

        private bool BeginSequenceAttack(EnemyAttackSpec settings, Vector2 direction, bool isClose)
        {
            if (!TryBeginAttack(settings, target, direction)) return false;
            inSequence = true; IsCloseAttack = isClose; followUpChecked = isClose;
            return true;
        }

        private bool CanFollowUp(out Vector2 toward)
        {
            toward = FacingDirection;
            if (target == null || !target.IsAlive) return false;
            Vector2 toTarget = (Vector2)target.transform.position - body.position;
            if (toTarget.magnitude > CloseSettings.reach || !HasLineOfSight(target.transform.position)) return false;
            if (toTarget.sqrMagnitude > .001f) toward = toTarget.normalized;
            return true;
        }

        // A finished, interrupted or abandoned sequence waits out its cooldown.
        private void EndSequence()
        {
            if (!inSequence) return;
            inSequence = false; IsFollowUp = false;
            cooldown = attackCooldown + (cooldownJitter > 0f ? Random.Range(0f, cooldownJitter) : 0f);
        }

        // Between sequences: keep a useful distance and face the player.
        private void Hold(float distance, Vector2 toTarget)
        {
            State = PursuitState.Holding;
            if (toTarget.sqrMagnitude > .001f) FacingDirection = toTarget.normalized;
            float gap = distance - holdDistance;
            body.linearVelocity = gap > .3f ? FacingDirection * moveSpeed : gap < -.3f ? -FacingDirection * moveSpeed * .6f : Vector2.zero;
        }

        private void Engage()
        {
            suspicion = 0f;
            if (State == PursuitState.Returning || State == PursuitState.Watching) State = PursuitState.Idle;
            actor.MarkSeen();
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
            if (!health.IsAlive) return;
            if (HasWatchingStage && !actor.OutsideLeash(leashRadius)) Engage();
            else provoked = true;
        }

        private void OnDefeated()
        {
            IsAware = false;
            EndSequence();
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
            EndSequence();
            cooldown = 0f;
            ResetVersion++;
            actor.Reset();
            SetState(PursuitState.Idle);
        }

        private void SetState(PursuitState state, float duration = 0f)
        {
            State = state;
            suspicion = 0f;
            remaining = duration;
            stateDuration = duration;
            action.Cancel();
            knockedBack = false;
            body.linearVelocity = Vector2.zero;
        }
    }
}
