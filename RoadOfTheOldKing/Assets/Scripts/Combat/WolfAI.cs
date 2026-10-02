using System;
using System.Collections.Generic;
using TheLostShrine.Player;
using TheLostShrine.Progression;
using TheLostShrine.Weapons;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TheLostShrine.Combat
{
    public enum WolfState { Idle, Alert, Chase, Stalk, Windup, Lunge, Snap, Recover, Reposition, Evade, Staggered, Return, Defeated }

    // Predator AI. Circles the player, then commits to a telegraphed lunge whose windup varies in length
    // but always locks its lane a fixed lead before the leap (an honest tell), or snaps when the player
    // is close. It reads the player: hops back from swings, punishes whiffs and an away axe, presses a
    // tired player, bites back when a hit doesn't break its poise, and chains a follow-up when a lunge
    // misses. Its recovery is the player's window. Attack tokens make a pack take turns.
    // Owns movement, attack commitment and damage; WolfView only samples it.
    [DisallowMultipleComponent, RequireComponent(typeof(Damageable), typeof(HitReaction), typeof(Rigidbody2D))]
    public sealed class WolfAI : MonoBehaviour, IEnemy, IResetOnRest
    {
        [SerializeField] private PlayerHealth target;

        [Header("Senses")]
        [SerializeField, Min(.1f)] private float noticeRadius = 10f;
        [Tooltip("Gives up and returns home when it or the player strays this far from home.")]
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
        [SerializeField] private Vector2 stalkTime = new Vector2(.4f, 1.1f);
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
        [SerializeField] private Vector2 lungeWindup = new Vector2(.35f, .85f);
        [Tooltip("Seconds between the lane locking and the leap: the honest dodge cue.")]
        [SerializeField, Min(.05f)] private float lockLead = .25f;
        [SerializeField, Min(.5f)] private float lungeDistance = 3.4f;
        [SerializeField, Min(.05f)] private float lungeDuration = .26f;
        [Tooltip("Standing still after a lunge: the player's punish window.")]
        [SerializeField, Min(0f)] private float lungeRecovery = .45f;
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
        [SerializeField, Min(.05f)] private float snapWindup = .3f;
        [Tooltip("Windup for reactive snaps (punish, retaliation, follow-up).")]
        [SerializeField, Min(.05f)] private float quickSnapWindup = .22f;
        [SerializeField, Min(.02f)] private float snapActive = .12f;
        [SerializeField, Min(0f)] private float snapRecovery = .3f;
        [SerializeField, Min(.1f)] private float snapReach = 2.1f;
        [SerializeField, Range(1f, 180f)] private float snapArc = 80f;
        [SerializeField, Min(1)] private int snapDamage = 15;
        [SerializeField, Min(0f)] private float snapKnockback = 2.4f;
        [SerializeField, Min(0f)] private float snapStagger = .18f;

        [Header("Reactions")]
        [Tooltip("Pause after a stagger before acting again. The knockback keeps sliding through it.")]
        [SerializeField, Min(0f)] private float hitRecovery = .15f;

        private readonly ContactFilter2D solidFilter = new ContactFilter2D { useTriggers = false };
        private readonly List<RaycastHit2D> sight = new List<RaycastHit2D>(12);
        private readonly RaycastHit2D[] slideHits = new RaycastHit2D[8];
        private Damageable health;
        private HitReaction reaction;
        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private PlayerCombatController combat;
        private PlayerStamina stamina;
        private Vector2 home, previousPosition, repositionPoint, deathSlide, lungeOrigin, evadeDirection;
        private float elapsed, duration, stalkFor, feintAt, lastSeen, blockedTime, actualSpeed, evadeReadyAt;
        private int circleSign = 1;
        private bool struck, feint, hitPending, playerWasSwinging, playerWasExposed;

        public WolfState State { get; private set; }
        public Vector2 FacingDirection { get; private set; } = Vector2.down;
        public float StateProgress => duration > 0f ? Mathf.Clamp01(elapsed / duration) : 0f;
        public bool IsLungeAttack { get; private set; }
        public bool IsFeint => feint;
        public Vector2 AttackDirection { get; private set; } = Vector2.down;
        // Where the lunge lane starts: the body until the leap begins, then fixed.
        public Vector2 AttackOrigin => State == WolfState.Lunge ? lungeOrigin : body.position;
        public bool IsLaneLocked => State == WolfState.Lunge ||
            (State == WolfState.Windup && IsLungeAttack && elapsed >= duration - lockLead);
        // Progress through the tracking part of a lunge windup (before the lock).
        public float TrackProgress => Mathf.Clamp01(elapsed / Mathf.Max(.01f, duration - lockLead));
        public float LaneLength => lungeDistance + snoutOffset + biteRadius;
        public float LaneWidth => biteRadius * 2f;
        public float SnapReach => snapReach;
        public float SnapArc => snapArc;
        public bool IsAware { get; private set; }
        public bool IsDefeated => State == WolfState.Defeated;
        public int ResetVersion { get; private set; }
        public event Action PlayerDetected;

        private AxeWeapon PlayerWeapon => combat != null ? combat.Weapon : null;

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

        // Committed and unable to swing back: attack recovery, a throw being released, or the axe away.
        private bool PlayerExposed
        {
            get
            {
                var w = PlayerWeapon;
                if (combat == null) return false;
                return w == null || w.IsAway || (w.State == AxeState.LightChop && w.LightPhase == MeleePhase.Recovery) ||
                    (w.IsThrowing && !w.CanCancelThrowAim);
            }
        }

        private bool PlayerTired => stamina != null && stamina.Current < tiredStamina;

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
            health = GetComponent<Damageable>();
            reaction = GetComponent<HitReaction>();
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            home = previousPosition = body.position;
        }

        private void Start()
        {
            if (target == null)
                target = FindFirstObjectByType<PlayerHealth>();
            if (target != null)
            {
                combat = target.GetComponent<PlayerCombatController>();
                stamina = target.GetComponent<PlayerStamina>();
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
            reaction.Armored = false;
            if (body != null) body.linearVelocity = Vector2.zero;
        }

        private void FixedUpdate() => Tick(Time.fixedDeltaTime);

        private void Tick(float dt)
        {
            if (State == WolfState.Defeated) { SlideCorpse(dt); return; }
            if (dt <= 0f) return;
            actualSpeed = (body.position - previousPosition).magnitude / dt;
            previousPosition = body.position;
            elapsed += dt;

            bool targetAlive = target != null && target.IsAlive;
            Vector2 toTarget = targetAlive ? (Vector2)target.transform.position - body.position : Vector2.zero;
            float distance = toTarget.magnitude;
            Vector2 toward = distance > .001f ? toTarget / distance : FacingDirection;
            bool sees = targetAlive && distance <= noticeRadius * 1.5f && HasLineOfSight(target.transform.position);
            if (sees) lastSeen = Time.time;

            // Read the player once per tick; reactions trigger on the moment a swing or an opening begins.
            bool swinging = targetAlive && PlayerSwinging, exposed = targetAlive && PlayerExposed;
            bool swingStarted = swinging && !playerWasSwinging, openingStarted = exposed && !playerWasExposed;
            playerWasSwinging = swinging;
            playerWasExposed = exposed;
            bool wasHit = hitPending;
            hitPending = false;

            // Weapon knockback owns the body while staggered; then a short recovery before acting.
            if (State == WolfState.Staggered)
            {
                if (reaction.IsStaggered) { elapsed = 0f; return; }
                if (elapsed < hitRecovery) return;
                if (targetAlive && distance <= snapRange && Random.value < counterChance && AttackTokens.TryAcquire(this))
                    BeginAttack(false, toward, true);
                else
                    Enter(WolfState.Reposition);
                return;
            }

            if (IsAware && (!targetAlive || OutsideLeash() || Time.time - lastSeen > loseSightTime))
            {
                LoseTarget();
                Enter(WolfState.Return);
            }

            bool free = State == WolfState.Stalk || State == WolfState.Reposition || State == WolfState.Chase;
            if (IsAware && free && Reacted(distance, toward, sees, swingStarted, openingStarted, wasHit)) return;

            switch (State)
            {
                case WolfState.Idle:
                    Brake(dt);
                    if (sees && distance <= noticeRadius) Notice();
                    break;
                case WolfState.Alert:
                    Brake(dt);
                    Face(toward);
                    if (elapsed >= alertDuration) Enter(distance > chaseDistance ? WolfState.Chase : WolfState.Stalk);
                    break;
                case WolfState.Chase:
                    Move(toward, runSpeed, dt);
                    if (distance <= circleRadius + circleTolerance) Enter(WolfState.Stalk);
                    break;
                case WolfState.Stalk:
                    Stalk(distance, toward, sees, exposed, dt);
                    break;
                case WolfState.Windup:
                    Brake(dt);
                    if (!IsLaneLocked) AttackDirection = toward;
                    Face(AttackDirection);
                    if (feint && StateProgress >= feintAt)
                    {
                        AttackTokens.Release(this);
                        Enter(WolfState.Reposition);
                    }
                    else if (elapsed >= duration)
                    {
                        if (IsLungeAttack) { lungeOrigin = body.position; Enter(WolfState.Lunge, lungeDuration); }
                        else Enter(WolfState.Snap, snapActive);
                    }
                    break;
                case WolfState.Lunge:
                    Lunge(distance, toward, targetAlive);
                    break;
                case WolfState.Snap:
                    Brake(dt);
                    if (!struck) TrySnap();
                    if (elapsed >= duration) Enter(WolfState.Recover, snapRecovery);
                    break;
                case WolfState.Recover:
                    // The punish window: hits land freely (poise still applies) but it does not act.
                    Brake(dt);
                    if (elapsed >= duration)
                    {
                        // Overstaying a punish gets bitten.
                        if (targetAlive && distance <= snapRange && swinging && Random.value < retaliateChance)
                            BeginAttack(false, toward, true);
                        else
                        {
                            AttackTokens.Release(this);
                            Enter(WolfState.Reposition);
                        }
                    }
                    break;
                case WolfState.Evade:
                    body.linearVelocity = evadeDirection * (evadeDistance / evadeDuration);
                    Face(toward);
                    if (elapsed >= duration)
                    {
                        body.linearVelocity *= .3f;
                        // Hopped clear of a swing: lunge into the recovery if the opening is there.
                        if (targetAlive && exposed && distance >= lungeRange.x && distance <= lungeRange.y && AttackTokens.TryAcquire(this))
                            BeginAttack(true, toward, true);
                        else
                            Enter(WolfState.Stalk);
                    }
                    break;
                case WolfState.Reposition:
                    Vector2 toPoint = repositionPoint - body.position;
                    if (toPoint.magnitude <= .3f || elapsed > 1.2f) Enter(WolfState.Stalk);
                    else Move(toPoint.normalized, trotSpeed, dt);
                    break;
                case WolfState.Return:
                    Vector2 toHome = home - body.position;
                    if (toHome.magnitude <= .15f) { Brake(dt); Enter(WolfState.Idle); }
                    else Move(toHome.normalized, trotSpeed, dt);
                    break;
            }
        }

        // Reactions available while circling, repositioning or chasing. Returns true when it acted.
        private bool Reacted(float distance, Vector2 toward, bool sees, bool swingStarted, bool openingStarted, bool wasHit)
        {
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
                Enter(WolfState.Evade, evadeDuration);
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
            if (distance > chaseDistance) { Enter(WolfState.Chase); return; }
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
            IsLungeAttack = lunge;
            feint = lunge && !quick && Random.value < feintChance;
            feintAt = Random.Range(.4f, .6f);
            AttackDirection = toward;
            float windup = lunge
                ? quick ? lockLead + .1f : Random.Range(Mathf.Min(lungeWindup.x, lungeWindup.y), Mathf.Max(lungeWindup.x, lungeWindup.y))
                : quick ? quickSnapWindup : snapWindup;
            Enter(WolfState.Windup, Mathf.Max(windup, lunge ? lockLead + .05f : .05f));
        }

        private void Lunge(float distance, Vector2 toward, bool targetAlive)
        {
            float speed = lungeDistance / lungeDuration;
            body.linearVelocity = AttackDirection * speed;
            FacingDirection = AttackDirection;
            if (!struck) TryBite(body.position + AttackDirection * snoutOffset);
            // A wall (or the player's body after the bite) ends the leap early.
            bool blocked = elapsed > .05f && actualSpeed < speed * .25f;
            if (!blocked && elapsed < duration) return;
            body.linearVelocity *= .4f; // short skid
            // Missed: chain a quick follow-up instead of recovering.
            if (!struck && targetAlive && Random.value < followUpChance)
            {
                if (distance <= snapRange) { BeginAttack(false, toward, true); return; }
                if (distance >= lungeRange.x && distance <= lungeRange.y) { BeginAttack(true, toward, true); return; }
            }
            Enter(WolfState.Recover, lungeRecovery);
        }

        private void TryBite(Vector2 snout)
        {
            var collider = target != null ? target.GetComponent<Collider2D>() : null;
            if (collider == null || !collider.enabled) return;
            Vector2 point = collider.ClosestPoint(snout);
            if (Vector2.Distance(point, snout) > biteRadius || !HasLineOfSight(point)) return;
            struck = true;
            target.Health.ReceiveHit(new CombatHit(gameObject, AttackKind.EnemyMelee, lungeDamage,
                AttackDirection, lungeKnockback, lungeStagger, false, point));
        }

        private void TrySnap()
        {
            var collider = target != null ? target.GetComponent<Collider2D>() : null;
            if (collider == null || !collider.enabled) return;
            Vector2 point = collider.ClosestPoint(body.position);
            Vector2 direction = point - body.position;
            // One damage attempt per snap, even when the player is invulnerable.
            struck = true;
            if (direction.magnitude > snapReach ||
                (direction.sqrMagnitude > .001f && Vector2.Angle(AttackDirection, direction) > snapArc * .5f) ||
                !HasLineOfSight(point))
                return;
            target.Health.ReceiveHit(new CombatHit(gameObject, AttackKind.EnemyMelee, snapDamage,
                AttackDirection, snapKnockback, snapStagger, false, point));
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
            lastSeen = Time.time;
            Enter(WolfState.Alert);
            PlayerDetected?.Invoke();
        }

        private void LoseTarget()
        {
            IsAware = false;
            AttackTokens.Release(this);
        }

        private bool OutsideLeash() => target != null &&
            (Vector2.Distance(target.transform.position, home) > leashRadius || Vector2.Distance(body.position, home) > leashRadius);

        // Terrain and static props block sight; characters (dynamic bodies) do not.
        private bool HasLineOfSight(Vector2 point)
        {
            Physics2D.Linecast(body.position, point, solidFilter, sight);
            foreach (var hit in sight)
            {
                if (hit.collider == null || hit.transform.IsChildOf(transform) ||
                    (target != null && hit.transform.IsChildOf(target.transform)))
                    continue;
                if (hit.collider.attachedRigidbody != null && hit.collider.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic)
                    continue;
                return false;
            }
            return true;
        }

        private void Move(Vector2 direction, float speed, float dt)
        {
            body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, direction * speed, acceleration * dt);
            if (direction.sqrMagnitude > .0001f) FacingDirection = direction.normalized;
        }

        private void Brake(float dt) => body.linearVelocity = Vector2.MoveTowards(body.linearVelocity, Vector2.zero, acceleration * 2f * dt);

        private void Face(Vector2 direction) { if (direction.sqrMagnitude > .0001f) FacingDirection = direction.normalized; }

        private void Enter(WolfState state, float stateDuration = 0f)
        {
            State = state;
            elapsed = 0f;
            duration = stateDuration;
            struck = false;
            blockedTime = 0f;
            // Hyper-armor for the committed leap: hits still land but cannot stop it.
            reaction.Armored = state == WolfState.Lunge;
            if (state == WolfState.Stalk)
            {
                stalkFor = Random.Range(Mathf.Min(stalkTime.x, stalkTime.y), Mathf.Max(stalkTime.x, stalkTime.y));
                if (Random.value < .5f) circleSign = -circleSign;
            }
            else if (state == WolfState.Reposition)
            {
                Vector2 center = target != null ? (Vector2)target.transform.position : home;
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
                lastSeen = Time.time;
                if (State == WolfState.Idle || State == WolfState.Return) Enter(WolfState.Stalk);
                PlayerDetected?.Invoke();
            }
        }

        // Only hits that break its poise (or land without armor on a poiseless wolf) stagger it.
        private void OnStaggered(CombatHit hit)
        {
            if (!health.IsAlive) return;
            AttackTokens.Release(this);
            Enter(WolfState.Staggered);
        }

        private void OnDefeated()
        {
            IsAware = false;
            AttackTokens.Release(this);
            reaction.Armored = false;
            // Carry the killing blow's knockback into a short corpse slide. The collider switches off at
            // once so throws and the player pass through, so the slide stops at walls by casting.
            deathSlide = body.linearVelocity;
            Enter(WolfState.Defeated);
            if (bodyCollider != null) bodyCollider.enabled = false;
        }

        private void SlideCorpse(float dt)
        {
            if (dt <= 0f || deathSlide.sqrMagnitude < .0004f) { body.linearVelocity = Vector2.zero; return; }
            Vector2 step = deathSlide * dt;
            float distance = step.magnitude;
            float radius = bodyCollider is CircleCollider2D circle ? circle.radius * .8f : .3f;
            int count = Physics2D.CircleCast(body.position, radius, step / distance, solidFilter, slideHits, distance);
            for (int i = 0; i < count; i++)
            {
                var other = slideHits[i].collider;
                // Characters and dynamic props do not stop a corpse; terrain and static props do.
                if (other == bodyCollider || (other.attachedRigidbody != null && other.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic))
                    continue;
                distance = Mathf.Min(distance, Mathf.Max(0f, slideHits[i].distance - .02f));
                deathSlide = Vector2.zero;
            }
            body.position += step.normalized * distance;
            body.linearVelocity = Vector2.zero;
            deathSlide *= Mathf.Exp(-body.linearDamping * dt);
        }

        public void ResetOnRest()
        {
            IsAware = false;
            AttackTokens.Release(this);
            ResetVersion++;
            deathSlide = Vector2.zero;
            health.RestoreHealth();
            reaction.Clear();
            body.position = home;
            transform.position = home;
            previousPosition = home;
            body.linearVelocity = Vector2.zero;
            hitPending = playerWasSwinging = playerWasExposed = false;
            if (bodyCollider != null) bodyCollider.enabled = true;
            Enter(WolfState.Idle);
        }
    }
}
