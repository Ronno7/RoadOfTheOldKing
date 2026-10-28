using RoadOfTheOldKing.Combat;
using System;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.Progression;
using RoadOfTheOldKing.Weapons;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RoadOfTheOldKing.Enemies
{
    public enum PredatorState { Idle, Alert, Chase, Stalk, Windup, Lunge, Snap, Recover, Reposition, Evade, Staggered, Return, Defeated }

    // Predator AI. Circles the player, then commits to a telegraphed lunge whose windup varies in length
    // but always locks its lane a fixed lead before the leap (an honest tell), or snaps when the player
    // is close. It reads the player: hops back from swings, punishes whiffs and an away axe, presses a
    // tired player, bites back when a hit doesn't break its poise, and chains a follow-up when a lunge
    // misses. Its recovery is the player's window. Attack tokens make a pack take turns.
    // Chooses locomotion and actions. EnemyActionRunner owns committed movement/timing/damage; presentation only samples state.
    [DisallowMultipleComponent, RequireComponent(typeof(Damageable), typeof(HitReaction), typeof(Rigidbody2D))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "RoadOfTheOldKing.Combat", null, "WolfAI")]
    public sealed class PredatorBrain : MonoBehaviour, IEnemy, IEnemyFacing, IResetOnRest, ISceneVisitParticipant
    {
        [SerializeField] private PlayerHealth target;

        [Header("Senses")]
        [SerializeField, Min(.1f)] private float noticeRadius = 10f;
        [Tooltip("Gives up and returns home when it strays this far from home.")]
        [SerializeField, Min(.1f)] private float leashRadius = 20f;
        [Tooltip("Pause on first noticing the player, the '!' beat before it closes in.")]
        [SerializeField, Min(0f)] private float alertDuration = .4f;
        [Tooltip("Seconds without line of sight before it loses the player.")]
        [SerializeField, Min(0f)] private float loseSightTime = 2.5f;

        [Header("Movement")]
        [SerializeField, Min(.1f)] private float stalkSpeed = 2.6f;
        [SerializeField, Min(.1f)] private float trotSpeed = 4.2f;
        [SerializeField, Min(.1f)] private float runSpeed = 6.5f;
        [Tooltip("How quickly it changes velocity while moving (units/s²).")]
        [SerializeField, Min(1f)] private float acceleration = 40f;
        [Tooltip("Preferred distance while circling the player.")]
        [SerializeField, Min(.5f)] private float circleRadius = 3.2f;
        [SerializeField, Min(.1f)] private float circleTolerance = .6f;
        [Tooltip("Farther than this it runs in instead of circling.")]
        [SerializeField, Min(1f)] private float chaseDistance = 6f;
        [Tooltip("Pack wolves keep at least this far apart while circling.")]
        [SerializeField, Min(0f)] private float packSpacing = 2.2f;

        [Header("Stalking")]
        [Tooltip("Seconds of circling before trying an attack (random within the range).")]
        [SerializeField] private Vector2 stalkTime = new Vector2(.5f, 1.3f);
        [Tooltip("Circling time multiplier while the player is exposed (axe away) or tired.")]
        [SerializeField, Range(.05f, 1f)] private float pressureScale = .35f;
        [Tooltip("Player stamina below which it presses the attack.")]
        [SerializeField, Min(0f)] private float tiredStamina = 25f;
        [Tooltip("Chance a lunge windup is a feint that breaks off before the leap.")]
        [SerializeField, Range(0f, 1f)] private float feintChance = .25f;

        [Header("Reading the player")]
        [Tooltip("Chance to hop back when the player starts a swing within reach.")]
        [SerializeField, Range(0f, 1f)] private float evadeChance = .45f;
        [SerializeField, Min(.1f)] private float evadeDistance = 2.4f;
        [SerializeField, Min(.05f)] private float evadeDuration = .2f;
        [SerializeField, Min(0f)] private float evadeCooldown = 1.5f;
        [Tooltip("Chance to sidestep an axe thrown straight at it.")]
        [SerializeField, Range(0f, 1f)] private float sidestepChance = .45f;
        [Tooltip("How far off the axe's line (degrees) still counts as thrown at it.")]
        [SerializeField, Range(1f, 60f)] private float throwThreatAngle = 25f;
        [Tooltip("Extra distance beyond the player's weapon reach that still counts as threatened.")]
        [SerializeField, Min(0f)] private float threatMargin = .6f;
        [Tooltip("Chance to attack at once when the player whiffs (attack recovery) or throws the axe away.")]
        [SerializeField, Range(0f, 1f)] private float punishChance = .8f;
        [Tooltip("Chance to snap straight back when a hit doesn't break its poise.")]
        [SerializeField, Range(0f, 1f)] private float retaliateChance = .5f;
        [Tooltip("Chance to snap back right after recovering from a stagger when the player is close.")]
        [SerializeField, Range(0f, 1f)] private float counterChance = .35f;

        [Header("Lunge")]
        [Tooltip("Windup length varies within this range (delayed lunges), but the lane always locks lockLead before the leap.")]
        [SerializeField] private Vector2 lungeWindup = new Vector2(.42f, .95f);
        [Tooltip("Seconds between the lane locking and the leap: the honest dodge cue.")]
        [SerializeField, Min(.05f)] private float lockLead = .32f;
        [SerializeField, Min(.5f)] private float lungeDistance = 3.4f;
        [SerializeField, Min(.05f)] private float lungeDuration = .3f;
        [Tooltip("Standing still after a lunge: the player's punish window.")]
        [SerializeField, Min(0f)] private float lungeRecovery = .55f;
        [Tooltip("Player distance (min, max) from which it lunges.")]
        [SerializeField] private Vector2 lungeRange = new Vector2(2f, 4.8f);
        [Tooltip("Chance to chain a quick follow-up when a lunge misses.")]
        [SerializeField, Range(0f, 1f)] private float followUpChance = .5f;
        [Tooltip("Bite point ahead of the body centre.")]
        [SerializeField, Min(0f)] private float snoutOffset = 1f;
        [SerializeField, Min(.1f)] private float biteRadius = .75f;
        [SerializeField, Min(1)] private int lungeDamage = 20;
        [SerializeField, Min(0f)] private float lungeKnockback = 4f;
        [SerializeField, Min(0f)] private float lungeStagger = .25f;

        [Header("Snap")]
        [Tooltip("Player distance within which it snaps instead of lunging.")]
        [SerializeField, Min(.1f)] private float snapRange = 1.9f;
        [SerializeField, Min(.05f)] private float snapWindup = .36f;
        [Tooltip("Windup for reactive snaps (punish, retaliation, follow-up).")]
        [SerializeField, Min(.05f)] private float quickSnapWindup = .27f;
        [SerializeField, Min(.02f)] private float snapActive = .12f;
        [SerializeField, Min(0f)] private float snapRecovery = .36f;
        [SerializeField, Min(.1f)] private float snapReach = 2.1f;
        [SerializeField, Range(1f, 180f)] private float snapArc = 80f;
        [SerializeField, Min(1)] private int snapDamage = 15;
        [SerializeField, Min(0f)] private float snapKnockback = 2.4f;
        [SerializeField, Min(0f)] private float snapStagger = .18f;

        [Header("Reactions")]
        [Tooltip("Pause after a stagger before acting again. The knockback keeps sliding through it.")]
        [SerializeField, Min(0f)] private float hitRecovery = .15f;

        private EnemyBody actor;
        private Damageable health;
        private HitReaction reaction;
        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private EnemyActionRunner action;
        private PredatorAttackAction attackAction;
        public EnemyActionRunner Action => action;
        private PlayerCombatController combat;
        private PlayerStamina stamina;
        private PlayerFlask flask;
        private Vector2 previousPosition, repositionPoint, evadeDirection;
        private float elapsed, duration, stalkFor, blockedTime, actualSpeed, evadeReadyAt;
        private int circleSign = 1;
        private bool feint, hitPending, playerWasStriking, playerWasExposed;
        private uint lastFlightSequence;

        public PredatorState State { get; private set; }
        public Vector2 FacingDirection { get; private set; } = Vector2.down;
        public float StateProgress => action != null && action.IsRunning ? action.Progress : duration > 0f ? Mathf.Clamp01(elapsed / duration) : 0f;
        public bool IsLungeAttack { get; private set; }
        public bool IsFeint => feint;
        public Vector2 AttackDirection { get; private set; } = Vector2.down;
        // Where the lunge lane starts: the body until the leap begins, then fixed.
        public Vector2 AttackOrigin => State == PredatorState.Lunge ? action.Origin : body.position;
        public bool IsLaneLocked => action != null && action.IsRunning && attackAction != null && attackAction.IsLocked;
        // Progress through the tracking part of a lunge windup (before the lock).
        public float TrackProgress => attackAction != null ? attackAction.TrackProgress : 0f;
        public float LaneLength => lungeDistance + snoutOffset + biteRadius;
        public float LaneWidth => biteRadius * 2f;
        public float SnapReach => snapReach;
        public float SnapArc => snapArc;
        public bool IsAware { get; private set; }
        public bool IsDefeated => State == PredatorState.Defeated;
        public int ResetVersion { get; private set; }
        public event Action PlayerDetected;

        private AxeWeapon PlayerWeapon =>combat != null ? combat.Weapon : null;

        // A swing that can still hit: light windup/active, charging or cleaving.
        private bool PlayerSwinging
        {
            get
            {
                var w = PlayerWeapon;
                return w != null && ((w.State == AxeState.LightChop && w.LightPhase != MeleePhase.Recovery) ||
                    w.State == AxeState.Charging || w.State == AxeState.Cleaving);
            }
        }

        // A blow actually coming: a light chop or a released cleave. Holding a charge is not one, so
        // evasion keys on the release rather than the charge start (playtest, 22 Oct).
        private bool PlayerStriking
        {
            get
            {
                var w = PlayerWeapon;
                return w != null && ((w.State == AxeState.LightChop && w.LightPhase != MeleePhase.Recovery) ||
                    w.State == AxeState.Cleaving);
            }
        }

        // Committed and unable to swing back: attack recovery, a throw being released, or the axe away.
        private bool PlayerExposed
        {
            get
            {
                var w = PlayerWeapon;
                if (combat == null) return false;
                return w == null || w.IsAway || w.IsCatching || (flask != null && flask.IsDrinking) || (w.State == AxeState.LightChop && w.LightPhase == MeleePhase.Recovery) ||
                    (w.IsThrowing && !w.CanCancelThrowAim);
            }
        }

        private bool PlayerTired => stamina != null && stamina.Current < tiredStamina;
        private bool PlayerAxeAway => PlayerWeapon != null && PlayerWeapon.IsAway;

        private float PlayerThreatRange
        {
            get
            {
                var w = PlayerWeapon;
                if (w == null || w.IsAway) return 0f;
                return w.Settings.lightRadius * Mathf.Max(1f, w.Settings.finisherReachMultiplier) + threatMargin;
            }
        }

        private void Awake()
        {
            actor = new EnemyBody(gameObject);
            health = actor.Health; reaction = actor.Reaction; body = actor.Rigidbody; bodyCollider = actor.Collider;
            action = new EnemyActionRunner();
            previousPosition = body.position;
        }

        private void Start()
        {
            if (target == null)
                target = FindFirstObjectByType<PlayerHealth>();
            if (target != null)
            {
                combat = target.GetComponent<PlayerCombatController>();
                stamina = target.GetComponent<PlayerStamina>();
                flask = target.GetComponent<PlayerFlask>();
            }
        }

        private void OnEnable()
        {
            EnemyRegistry.Register(this);
            health.HitReceived += OnHit;
            health.Defeated += OnDefeated;
            reaction.Staggered += OnStaggered;
        }

        private void OnDisable()
        {
            EnemyRegistry.Unregister(this);
            AttackTokens.Release(this);
            IsAware = false;
            health.HitReceived -= OnHit;
            health.Defeated -= OnDefeated;
            reaction.Staggered -= OnStaggered;
            action.Cancel();
            if (body != null) body.linearVelocity = Vector2.zero;
            if (!IsDefeated) Enter(PredatorState.Idle);
        }

        private void FixedUpdate() => Tick(Time.fixedDeltaTime);

        private void Tick(float dt)
        {
            if (State == PredatorState.Defeated) { SlideCorpse(dt); return; }
            if (dt <= 0f) return;
            actualSpeed = (body.position - previousPosition).magnitude / dt;
            previousPosition = body.position;
            elapsed += dt;

            bool targetAlive = target != null && target.IsAlive;
            Vector2 toTarget = targetAlive ? (Vector2)target.transform.position - body.position : Vector2.zero;
            float distance = toTarget.magnitude;
            Vector2 toward = distance > .001f ? toTarget / distance : FacingDirection;
            bool sees = targetAlive && actor.Sees(target.transform, noticeRadius, true);

            // Read the player once per tick; reactions trigger on the moment a swing or an opening begins.
            bool swinging = targetAlive && PlayerSwinging, exposed = targetAlive && PlayerExposed;
            bool striking = targetAlive && PlayerStriking;
            bool swingStarted = striking && !playerWasStriking, openingStarted = exposed && !playerWasExposed;
            playerWasStriking = striking;
            playerWasExposed = exposed;
            bool wasHit = hitPending;
            hitPending = false;
            // A new throw launched this tick (flight sequence advanced).
            var weapon = PlayerWeapon;
            bool throwLaunched = weapon != null && weapon.FlightSequence != lastFlightSequence && weapon.State == AxeState.Flying;
            if (weapon != null) lastFlightSequence = weapon.FlightSequence;

            // Weapon knockback owns the body while staggered; then a short recovery before acting.
            if (State == PredatorState.Staggered)
            {
                if (reaction.IsStaggered) { elapsed = 0f; return; }
                if (elapsed < hitRecovery) return;
                if (targetAlive && distance <= snapRange && Random.value < counterChance && AttackTokens.TryAcquire(this))
                    BeginAttack(false, toward, true);
                else
                    Enter(PredatorState.Reposition);
                return;
            }

            if (IsAware && (!targetAlive || actor.OutsideLeash(leashRadius) || actor.LostSight(loseSightTime)))
            {
                LoseTarget();
                Enter(PredatorState.Return);
            }

            bool free = State == PredatorState.Stalk || State == PredatorState.Reposition || State == PredatorState.Chase;
            if (IsAware && free && Reacted(distance, toward, sees, swingStarted, openingStarted, wasHit, throwLaunched)) return;

            switch (State)
            {
                case PredatorState.Idle:
                    Brake(dt);
                    if (sees && distance <= noticeRadius) Notice();
                    break;
                case PredatorState.Alert:
                    Brake(dt);
                    Face(toward);
                    if (elapsed >= alertDuration) Enter(distance > chaseDistance ? PredatorState.Chase : PredatorState.Stalk);
                    break;
                case PredatorState.Chase:
                    Move(toward, runSpeed, dt);
                    if (distance <= circleRadius + circleTolerance) Enter(PredatorState.Stalk);
                    break;
                case PredatorState.Stalk:
                    Stalk(distance, toward, sees, exposed, dt);
                    break;
                case PredatorState.Windup:
                case PredatorState.Lunge:
                case PredatorState.Snap:
                case PredatorState.Recover:
                case PredatorState.Evade:
                    TickAction(distance, toward, targetAlive, swinging, exposed, dt);
                    break;
                case PredatorState.Reposition:
                    Vector2 toPoint = repositionPoint - body.position;
                    if (toPoint.magnitude <= .3f || elapsed > 1.2f) Enter(PredatorState.Stalk);
                    else Move(toPoint.normalized, trotSpeed, dt);
                    break;
                case PredatorState.Return:
                    Vector2 toHome = actor.Home - body.position;
                    // Followed back into its ground: take the player up again (well inside the leash, so it doesn't flicker at the edge).
                    if (sees && distance <= noticeRadius && actor.CanReengage(leashRadius)) Notice();
                    else if (toHome.magnitude <= .15f) { Brake(dt); Enter(PredatorState.Idle); }
                    else Move(toHome.normalized, trotSpeed, dt);
                    break;
            }
        }

        // Reactions available while circling, repositioning or chasing. Returns true when it acted.
        private bool Reacted(float distance, Vector2 toward, bool sees, bool swingStarted, bool openingStarted, bool wasHit, bool throwLaunched)
        {
            // An axe thrown straight at it: step off the line (the side it is already on).
            var weapon = PlayerWeapon;
            if (throwLaunched && weapon != null && Time.time >= evadeReadyAt && distance <= weapon.Settings.throwRange + 1f)
            {
                Vector2 line = weapon.AttackDirection, fromPlayer = -toward;
                if (Vector2.Angle(line, fromPlayer) <= throwThreatAngle && Random.value < sidestepChance)
                {
                    float side = line.x * fromPlayer.y - line.y * fromPlayer.x;
                    if (Mathf.Abs(side) < .001f) side = Random.value < .5f ? -1f : 1f;
                    evadeReadyAt = Time.time + evadeCooldown;
                    evadeDirection = new Vector2(-line.y, line.x) * Mathf.Sign(side);
                    Enter(PredatorState.Evade, evadeDuration);
                    return true;
                }
            }
            // Hit without breaking its poise: bite back.
            if (wasHit && distance <= snapRange + .3f && Random.value < retaliateChance && AttackTokens.TryAcquire(this))
            {
                BeginAttack(false, toward, true);
                return true;
            }
            // The player swings at it: hop back out of reach.
            if (swingStarted && Time.time >= evadeReadyAt && distance <= PlayerThreatRange && Random.value < evadeChance)
            {
                evadeReadyAt = Time.time + evadeCooldown;
                evadeDirection = -toward;
                Enter(PredatorState.Evade, evadeDuration);
                return true;
            }
            // The player whiffed or threw the axe: punish at once.
            if (openingStarted && sees && Random.value < punishChance)
            {
                bool snapOk = distance <= snapRange, lungeOk = distance >= lungeRange.x && distance <= lungeRange.y;
                if ((snapOk || lungeOk) && AttackTokens.TryAcquire(this))
                {
                    BeginAttack(!snapOk, toward, true);
                    return true;
                }
            }
            return false;
        }

        private void Stalk(float distance, Vector2 toward, bool sees, bool exposed, float dt)
        {
            if (distance > chaseDistance) { Enter(PredatorState.Chase); return; }
            // Unarmed player (axe away): close in at a run instead of circling.
            if (PlayerAxeAway && distance > lungeRange.y * .9f) { Move(toward, runSpeed, dt); return; }
            bool hugged = distance < snapRange * .8f;
            float patience = exposed || PlayerTired ? stalkFor * pressureScale : stalkFor;
            if (sees && (elapsed >= patience || hugged))
            {
                bool snapOk = distance <= snapRange;
                bool lungeOk = distance >= lungeRange.x && distance <= lungeRange.y;
                if ((snapOk || lungeOk) && AttackTokens.TryAcquire(this)) { BeginAttack(!snapOk, toward, false); return; }
                if (elapsed >= stalkFor) stalkFor = elapsed + .25f; // no opening yet: look again shortly
            }
            // Circle the player, easing back to the ring; a hugging player pushes it outward.
            Vector2 tangent = new Vector2(-toward.y, toward.x) * circleSign;
            float radial = Mathf.Clamp((distance - circleRadius) / circleTolerance, -1f, 1f);
            Vector2 desired = tangent + toward * radial * 1.2f + Separation();
            Move(desired.sqrMagnitude > .0001f ? desired.normalized : tangent, stalkSpeed, dt);
            // Circling into a tree or wall: turn the other way.
            blockedTime = actualSpeed < stalkSpeed * .3f ? blockedTime + dt : 0f;
            if (blockedTime > .3f) { circleSign = -circleSign; blockedTime = 0f; }
        }

        // Quick attacks (punish, retaliation, follow-up) use short windups and never feint.
        private void BeginAttack(bool lunge, Vector2 toward, bool quick)
        {
            if (action.IsRunning) return;
            bool selectedFeint = lunge && !quick && Random.value < feintChance;
            float selectedFeintAt = Random.Range(.4f, .6f);
            float windup = lunge
                ? quick ? lockLead + .1f : Random.Range(Mathf.Min(lungeWindup.x, lungeWindup.y), Mathf.Max(lungeWindup.x, lungeWindup.y))
                : quick ? quickSnapWindup : snapWindup;
            var spec = new PredatorAttackSpec
            {
                lunge = lunge, feint = selectedFeint, feintAt = selectedFeintAt, lockLead = lockLead,
                distance = lungeDistance, snoutOffset = snoutOffset, biteRadius = biteRadius, acceleration = acceleration,
                attack = new EnemyAttackSpec
                {
                    windup = Mathf.Max(windup, lunge ? lockLead + .05f : .05f),
                    active = lunge ? lungeDuration : snapActive, recovery = lunge ? lungeRecovery : snapRecovery,
                    reach = snapReach, arc = snapArc, damage = lunge ? lungeDamage : snapDamage,
                    knockback = lunge ? lungeKnockback : snapKnockback, stagger = lunge ? lungeStagger : snapStagger
                }
            };
            var next = new PredatorAttackAction(gameObject, body, reaction, target, HasLineOfSight, spec, toward);
            if (!action.Begin(next)) { AttackTokens.Release(this); return; }
            attackAction = next; IsLungeAttack = lunge; feint = selectedFeint; AttackDirection = toward;
            State = PredatorState.Windup; elapsed = duration = blockedTime = 0f;
        }

        private void TickAction(float distance, Vector2 toward, bool targetAlive, bool swinging, bool exposed, float dt)
        {
            if (!targetAlive) { LoseTarget(); Enter(PredatorState.Return); return; }
            bool evading = State == PredatorState.Evade;
            action.Tick(dt);
            if (evading)
            {
                Face(toward);
                if (!action.IsRunning)
                {
                    if (exposed && distance >= lungeRange.x && distance <= lungeRange.y && AttackTokens.TryAcquire(this))
                        BeginAttack(true, toward, true);
                    else Enter(PredatorState.Stalk);
                }
                return;
            }
            AttackDirection = action.Direction; Face(AttackDirection);
            if (attackAction.Feinted)
            { AttackTokens.Release(this); Enter(PredatorState.Reposition); return; }
            if (attackAction.FinishedActive && IsLungeAttack && !attackAction.HasContact && Random.value < followUpChance)
            {
                if (distance <= snapRange || (distance >= lungeRange.x && distance <= lungeRange.y))
                { action.Cancel(false); BeginAttack(distance > snapRange, toward, true); return; }
            }
            if (!action.IsRunning)
            {
                if (distance <= snapRange && swinging && Random.value < retaliateChance) BeginAttack(false, toward, true);
                else { AttackTokens.Release(this); Enter(PredatorState.Reposition); }
                return;
            }
            State = action.Phase == EnemyAttackPhase.Windup ? PredatorState.Windup :
                action.Phase == EnemyAttackPhase.Active ? (IsLungeAttack ? PredatorState.Lunge : PredatorState.Snap) : PredatorState.Recover;
        }

        private Vector2 Separation()
        {
            Vector2 push = Vector2.zero;
            if (packSpacing <= 0f) return push;
            foreach (var other in EnemyRegistry.Active)
            {
                if (ReferenceEquals(other, this) || other.IsDefeated || !(other is Component component)) continue;
                Vector2 away = body.position - (Vector2)component.transform.position;
                float d = away.magnitude;
                if (d > .001f && d < packSpacing) push += away / d * (1f - d / packSpacing);
            }
            return push * 1.5f;
        }

        private void Notice()
        {
            IsAware = true;
            actor.MarkSeen();
            Enter(PredatorState.Alert);
            PlayerDetected?.Invoke();
        }

        private void LoseTarget()
        {
            IsAware = false;
            AttackTokens.Release(this);
        }

        // Terrain and static props block sight; characters (dynamic bodies) do not.
        private bool HasLineOfSight(Vector2 point) => actor.CanSee(point, target != null ? target.transform : null, true);

        private void Move(Vector2 direction, float speed, float dt)
        {
            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, direction * speed, acceleration * dt);
            if (direction.sqrMagnitude > .0001f) FacingDirection = direction.normalized;
        }

        private void Brake(float dt) => body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, Vector2.zero, acceleration * 2f * dt);

        private void Face(Vector2 direction) { if (direction.sqrMagnitude > .0001f) FacingDirection = direction.normalized; }

        private void Enter(PredatorState state, float stateDuration = 0f)
        {
            State = state;
            elapsed = 0f;
            duration = stateDuration;
            action.Cancel(false);
            blockedTime = 0f;
            if (state == PredatorState.Evade)
                action.Begin(new EvadeAction(body, evadeDirection, evadeDistance, evadeDuration));
            if (state == PredatorState.Stalk)
            {
                stalkFor = Random.Range(Mathf.Min(stalkTime.x, stalkTime.y), Mathf.Max(stalkTime.x, stalkTime.y));
                if (Random.value < .5f) circleSign = -circleSign;
            }
            else if (state == PredatorState.Reposition)
            {
                Vector2 center = target != null ? (Vector2)target.transform.position : actor.Home;
                Vector2 away = body.position - center;
                away = away.sqrMagnitude > .001f ? away.normalized : -FacingDirection;
                float angle = 40f * circleSign * Mathf.Deg2Rad;
                Vector2 rotated = new Vector2(away.x * Mathf.Cos(angle) - away.y * Mathf.Sin(angle), away.x * Mathf.Sin(angle) + away.y * Mathf.Cos(angle));
                repositionPoint = center + rotated * (circleRadius + .6f);
            }
        }

        private void OnHit(CombatHit hit)
        {
            if (!health.IsAlive) return;
            hitPending = true;
            // Struck from hiding: it knows where you are now.
            if (!IsAware && target != null && target.IsAlive)
            {
                IsAware = true;
                actor.MarkSeen();
                if (State == PredatorState.Idle || State == PredatorState.Return) Enter(PredatorState.Stalk);
                PlayerDetected?.Invoke();
            }
        }

        // Only hits that break its poise (or land without armor on a poiseless wolf) stagger it.
        private void OnStaggered(CombatHit hit)
        {
            if (!health.IsAlive) return;
            AttackTokens.Release(this);
            Enter(PredatorState.Staggered);
        }

        private void OnDefeated()
        {
            IsAware = false;
            AttackTokens.Release(this);
            reaction.Armored = false;
            // Carry the killing blow's knockback into a short corpse slide. The collider switches off at
            // once so throws and the player pass through, so the slide stops at walls by casting.
            actor.BeginDefeat();
            Enter(PredatorState.Defeated);
        }

        private void SlideCorpse(float dt) => actor.SlideCorpse(dt);

        public object CaptureVisitState() => actor.CaptureVisitState();

        public void RestoreVisitState(object state)
        {
            if (!(state is EnemyVisitState snapshot)) return;
            ResetOnRest();
            actor.RestoreVisitHealth(snapshot);
            if (health.IsAlive) return;
            previousPosition = snapshot.position;
            OnDefeated();
        }

        public void ResetOnRest()
        {
            IsAware = false;
            AttackTokens.Release(this);
            ResetVersion++;
            actor.Reset();
            previousPosition = actor.Home;
            body.linearVelocity = Vector2.zero;
            hitPending = playerWasStriking = playerWasExposed = false;
            Enter(PredatorState.Idle);
        }
    }
}
