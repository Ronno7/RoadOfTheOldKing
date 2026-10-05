using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.Player;
using UnityEngine;

namespace RoadOfTheOldKing.Weapons
{
    public enum AxeState { OnGround, Held, LightChop, Charging, Cleaving, Flying, Stuck, Returning }
    public enum MeleePhase { None, Windup, Active, Recovery }

    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)] // Advance before the player motor consumes action displacement.
    public sealed class AxeWeapon : MonoBehaviour
    {
        [SerializeField] private AxeSettings settings;
        [SerializeField] private Collider2D pickupCollider;
        private PlayerCombatController owner;
        private AxeHitDetector hits;
        private Transform stuckTarget;
        private Vector3 stuckOffset;
        private float elapsed;
        private float travelled;
        private float comboRemaining;
        private float cleaveStrength;
        private Vector2 attackDirection = Vector2.right;
        private float lightSpeedMultiplier = 1f;
        private float addedLightArc;
        private float finisherDamageMultiplier = 1f;
        private Vector2 pendingActionDisplacement;
        private float addedCleaveRadius;
        private float impactPauseRemaining;
        // The current attack's cost pushed stamina below zero: it hits weaker.
        private bool exhaustedAttack;
        private float catchRecoveryRemaining;
        private readonly ThrowActionClock throwAction = new ThrowActionClock();
        public event System.Action<CombatHit> HitConfirmed;
        // Successful Recall arrival only; cancellation, equip and ground pickup do not catch.
        public event System.Action ReturnedToHand;
        // Presentation samples actual transit time, including steps that cross release.
        public float FlightSeconds { get; private set; }
        public uint FlightSequence { get; private set; }
        public Vector2 ReturnApproachDirection { get; private set; }
        public bool IsImpactPaused => impactPauseRemaining > 0f;

        // Pause only this action clock. Enemies, camera, UI and world time keep running.
        public void PauseOnImpact(float seconds)
        {
            if (State == AxeState.LightChop)
                impactPauseRemaining = Mathf.Max(impactPauseRemaining, Mathf.Clamp(seconds, 0f, 0.1f));
        }

        public AxeSettings Settings => settings;
        public AxeState State { get; private set; } = AxeState.OnGround;
        public PlayerCombatController Owner => owner;
        public Vector2 AimDirection { get; private set; } = Vector2.right;
        public Vector2 AttackDirection => attackDirection;
        public int ComboIndex { get; private set; }
        public int ComboStep => State == AxeState.LightChop ||
            (State == AxeState.Held && comboRemaining > 0f) ? ComboIndex + 1 : 0;
        public float ComboTimeRemaining => comboRemaining;
        public bool IsLightThrust => ComboIndex == 2;
        public float LightReach => settings.lightRadius * (IsLightThrust ? settings.finisherReachMultiplier : 1f);
        // Apply widening proportionally so the reverse sweep remains narrower even at the cap.
        public float LightArc => IsLightThrust ? settings.finisherArc
            : Mathf.Clamp(settings.lightArc + addedLightArc, 20f, 180f) *
                (ComboIndex == 1 ? settings.secondLightArc / Mathf.Max(20f, settings.lightArc) : 1f);
        public float LightLaneWidth => 2f * LightReach * Mathf.Tan(settings.finisherArc * 0.5f * Mathf.Deg2Rad);
        public int LightDamage => IsLightThrust ? Mathf.RoundToInt(settings.finisherDamage * finisherDamageMultiplier) : settings.lightDamage;
        public float LightWindupFraction => IsLightThrust ? settings.finisherWindupFraction : settings.lightWindupFraction;
        public float LightSwingEndFraction => IsLightThrust ? settings.finisherSwingEndFraction : settings.lightSwingEndFraction;
        public Vector2 ConsumeActionDisplacement()
        {
            var displacement = pendingActionDisplacement;
            pendingActionDisplacement = Vector2.zero;
            return displacement;
        }
        public float CleaveRadius => settings.cleaveRadius + addedCleaveRadius;
        public bool IsAway => State == AxeState.Flying || State == AxeState.Stuck || State == AxeState.Returning;
        public bool IsAttacking => throwAction.IsActive || State == AxeState.LightChop || State == AxeState.Charging || State == AxeState.Cleaving || IsCatching;
        // Just caught a recalled axe: no attack or dodge until this ends.
        public bool IsCatching => State == AxeState.Held && catchRecoveryRemaining > 0f;
        public ThrowPhase ThrowPhase => throwAction.Phase;
        public int ThrowCelIndex => throwAction.CelIndex;
        public float ThrowPhaseProgress => throwAction.PhaseProgress;
        public float ThrowLaunchFraction => throwAction.LaunchFraction;
        public bool IsThrowing => throwAction.IsActive;
        public bool CanCancelThrowAim => throwAction.CanCancelAim;
        public bool ControlsMovement => IsThrowing || State == AxeState.LightChop;
        public Vector2 ActionFacing => CanCancelThrowAim ? AimDirection : attackDirection;
        public float AimDistance => hits == null || owner == null ? 0f : hits.PreviewFlightDistance(
            owner.transform.position, AimDirection, settings.throwRange, settings.flightRadius);
        public float ActionMovementScale => IsThrowing ? throwAction.MovementScale
            : State == AxeState.LightChop ? Mathf.Max(settings.lightMovementScale, Mathf.SmoothStep(0f, 1f,
                Mathf.InverseLerp(ComboIndex == 2 ? 0.84f : 0.78f, 1f, AttackProgress))) : 1f;
        public float Charge01 => State == AxeState.Charging
            ? Mathf.Clamp01(elapsed / settings.fullCharge) : State == AxeState.Cleaving ? cleaveStrength : 0f;
        public float AttackProgress => Mathf.Clamp01(elapsed / (State == AxeState.Cleaving
            ? CleaveDuration : LightDuration));
        public float LightDuration => settings.lightDuration * (ComboIndex == 2 ? settings.finisherDurationMultiplier : 1f) / lightSpeedMultiplier / ExhaustedSpeed;
        public float CleaveDuration => settings.cleaveDuration / ExhaustedSpeed;
        // An attack made into a stamina deficit plays slower (and hits weaker): an opening for the enemy.
        private float ExhaustedSpeed => exhaustedAttack ? Mathf.Max(0.1f, settings.exhaustedSpeedMultiplier) : 1f;
        private float LightDamageStart => LightDuration * LightWindupFraction;
        private float LightDamageEnd => LightDuration * LightSwingEndFraction;
        // Presentation reads the same action clock/window as the physics interval below.
        public MeleePhase LightPhase => State != AxeState.LightChop ? MeleePhase.None
            : elapsed < LightDamageStart ? MeleePhase.Windup
            : elapsed < LightDamageEnd ? MeleePhase.Active : MeleePhase.Recovery;
        public float LightSwingProgress => Mathf.InverseLerp(LightDamageStart, LightDamageEnd, elapsed);

        // Rebuild from saved selections, never mutate the shared base settings asset.
        public void ApplyUpgrades(System.Collections.Generic.IEnumerable<AxeUpgrade> upgrades)
        {
            lightSpeedMultiplier = 1f;
            finisherDamageMultiplier = 1f;
            addedLightArc = addedCleaveRadius = 0f;
            foreach (var upgrade in upgrades)
            {
                lightSpeedMultiplier *= Mathf.Max(1f, upgrade.lightSpeedMultiplier);
                addedLightArc += Mathf.Max(0f, upgrade.addedLightArc);
                addedCleaveRadius += Mathf.Max(0f, upgrade.addedCleaveRadius);
                finisherDamageMultiplier *= Mathf.Max(1f, upgrade.finisherDamageMultiplier);
            }
        }

        private void Awake()
        {
            if (pickupCollider == null)
                pickupCollider = GetComponent<Collider2D>();
            if (settings == null)
            {
                Debug.LogError("AxeWeapon needs a AxeSettings asset.", this);
                enabled = false;
            }
        }

        public bool TryEquip(PlayerCombatController newOwner)
        {
            if (!isActiveAndEnabled || State != AxeState.OnGround || newOwner == null)
                return false;
            owner = newOwner;
            hits = new AxeHitDetector(owner.transform, transform, OnHitConfirmed);
            if (pickupCollider != null)
                pickupCollider.enabled = false;
            transform.position = owner.transform.position;
            SetState(AxeState.Held);
            return true;
        }

        private void OnHitConfirmed(CombatHit hit)
        {
            if (hit.Kind == AttackKind.LightChop)
                PauseOnImpact(settings.lightHitPause * (ComboIndex == 2 ? settings.finisherHitPauseMultiplier : 1f));
            HitConfirmed?.Invoke(hit);
        }

        public void SetAim(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.001f)
                AimDirection = direction.normalized;
        }

        public bool TryLightChop(Vector2 direction)
        {
            if (!isActiveAndEnabled || State != AxeState.Held || IsThrowing || IsCatching || !owner.CanStartAttack)
                return false;
            int nextComboIndex = comboRemaining > 0f ? (ComboIndex + 1) % 3 : 0;
            if (!owner.Stamina.TrySpend(nextComboIndex == 2 ? settings.finisherStaminaCost : settings.lightStaminaCost))
                return false;
            exhaustedAttack = owner.Stamina.IsExhausted;
            SetAim(direction);
            attackDirection = AimDirection;
            ComboIndex = nextComboIndex;
            hits.BeginAttack();
            SetState(AxeState.LightChop);
            return true;
        }

        public bool TryBeginCharge()
        {
            if (!isActiveAndEnabled || State != AxeState.Held || IsThrowing || IsCatching || !owner.CanStartAttack)
                return false;
            // Pay once on commitment. Holding or cancelling cannot generate a free cleave.
            if (!owner.Stamina.TrySpend(settings.cleaveStaminaCost))
                return false;
            exhaustedAttack = owner.Stamina.IsExhausted;
            comboRemaining = 0f;
            SetState(AxeState.Charging);
            return true;
        }

        public bool TryReleaseCharge(Vector2 direction)
        {
            if (State != AxeState.Charging)
                return false;
            if (elapsed < settings.minimumCharge)
            {
                CancelCharge();
                return false;
            }
            SetAim(direction);
            attackDirection = AimDirection;
            cleaveStrength = Charge01;
            hits.BeginAttack();
            SetState(AxeState.Cleaving);
            return true;
        }

        public void CancelCharge()
        {
            if (State == AxeState.Charging)
                SetState(AxeState.Held);
        }

        // Death cancels damage in progress, including a thrown or returning axe.
        public void CancelAction()
        {
            pendingActionDisplacement = Vector2.zero;
            catchRecoveryRemaining = 0f;
            throwAction.Cancel();
            if (owner == null)
                return;
            stuckTarget = null;
            comboRemaining = 0f;
            transform.position = owner.transform.position;
            SetState(AxeState.Held);
        }

        public void CancelLightCombo()
        {
            comboRemaining = 0f;
            pendingActionDisplacement = Vector2.zero;
            if (State == AxeState.LightChop) CancelAction();
        }

        public bool TryThrow(Vector2 direction)
        {
            return TryBeginThrow(direction) && TryReleaseThrow(direction);
        }

        public bool TryBeginThrow(Vector2 direction)
        {
            if (!isActiveAndEnabled || State != AxeState.Held || IsCatching || owner == null || !owner.CanStartAttack ||
                !throwAction.Begin(settings.throwAction))
                return false;
            SetAim(direction);
            comboRemaining = 0f;
            return true;
        }

        public bool TryReleaseThrow(Vector2 direction)
        {
            if (!isActiveAndEnabled || !CanCancelThrowAim || !owner.CanStartAttack) return false;
            if (!owner.Stamina.TrySpend(settings.throwStaminaCost))
            {
                CancelThrow();
                return false;
            }
            exhaustedAttack = owner.Stamina.IsExhausted;
            SetAim(direction);
            attackDirection = AimDirection;
            return throwAction.Commit();
        }

        // Cancelling after launch ends recovery but does not teleport the flying weapon home.
        public void CancelThrow() => throwAction.Cancel();

        private void LaunchThrow()
        {
            // Start at the player center so a nearby wall cannot be skipped.
            // Future hand sprites supply a visual offset, never an unchecked physics origin.
            transform.position = owner.transform.position;
            travelled = 0f;
            FlightSeconds = 0f;
            FlightSequence++;
            comboRemaining = 0f;
            hits.BeginAttack();
            SetState(AxeState.Flying);
        }

        // A Recall the player asks for costs stamina; refused at zero stamina, so the axe must be fetched on foot.
        public bool TryPlayerRecall()
        {
            if (!isActiveAndEnabled || owner == null || !owner.CanRecall || (State != AxeState.Flying && State != AxeState.Stuck))
                return false;
            return owner.Stamina.TrySpend(settings.recallStaminaCost) && TryRecall();
        }

        // Free Recall: automatic distance returns and scripted sequences.
        public bool TryRecall()
        {
            if (!isActiveAndEnabled || owner == null || !owner.CanRecall ||
                (State != AxeState.Flying && State != AxeState.Stuck))
                return false;
            hits.BeginAttack(); // A target can be hit once outbound and once on return...
            // ...but not the one the axe is embedded in: Recall damage comes from routing the return through enemies.
            if (stuckTarget != null) hits.Exclude(stuckTarget.GetComponentInParent<IHitReceiver>());
            stuckTarget = null;
            ReturnApproachDirection = ((Vector2)(transform.position - owner.transform.position)).normalized;
            SetState(AxeState.Returning);
            return true;
        }

        private void FixedUpdate() => Simulate(Time.fixedDeltaTime);

        // One state machine controls action exclusivity and flight lifecycle.
        private void Simulate(float deltaTime)
        {
            if (deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || State == AxeState.OnGround)
                return;
            if (owner == null)
            {
                throwAction.Cancel();
                SetState(AxeState.OnGround);
                if (pickupCollider != null)
                    pickupCollider.enabled = true;
                return;
            }

            if (IsThrowing && !owner.CanContinueAction) CancelThrow();
            if (State == AxeState.LightChop && !owner.CanContinueAction) CancelLightCombo();
            bool wasThrowing = IsThrowing;
            bool committedThrow = wasThrowing && !CanCancelThrowAim;
            if (throwAction.Advance(deltaTime, out float flightSeconds)) LaunchThrow();

            if (impactPauseRemaining > 0f)
            {
                float paused = Mathf.Min(deltaTime, impactPauseRemaining);
                impactPauseRemaining -= paused;
                deltaTime -= paused;
                owner.Stamina.DelayRecovery();
                if (deltaTime <= 0f) return;
            }
            float previousElapsed = elapsed;
            if (committedThrow || State == AxeState.LightChop || State == AxeState.Charging || State == AxeState.Cleaving)
                owner.Stamina.DelayRecovery();
            elapsed += deltaTime;
            switch (State)
            {
                case AxeState.Held:
                    comboRemaining = Mathf.Max(0f, comboRemaining - deltaTime);
                    catchRecoveryRemaining = Mathf.Max(0f, catchRecoveryRemaining - deltaTime);
                    transform.position = owner.transform.position;
                    break;
                case AxeState.Charging:
                    transform.position = owner.transform.position;
                    break;
                case AxeState.LightChop:
                    transform.position = owner.transform.position;
                    if (IsLightThrust)
                    {
                        float activeSeconds = Mathf.Max(0f, Mathf.Min(elapsed, LightDamageEnd) - Mathf.Max(previousElapsed, LightDamageStart));
                        pendingActionDisplacement += attackDirection * (settings.finisherLungeDistance * activeSeconds / (LightDamageEnd - LightDamageStart));
                    }
                    if (elapsed >= LightDamageStart && previousElapsed < LightDamageEnd)
                        hits.Melee(owner.transform.position, attackDirection,
                            LightReach, LightArc,
                            new CombatHit(owner.gameObject, AttackKind.LightChop,
                                Exhausted(LightDamage),
                                attackDirection, ComboIndex == 2 ? settings.finisherKnockback : settings.lightKnockback,
                                ComboIndex == 2 ? settings.finisherStagger : settings.lightStagger),
                            IsLightThrust ? LightLaneWidth : 0f);
                    if (elapsed >= LightDuration)
                    {
                        comboRemaining = settings.comboWindow;
                        SetState(AxeState.Held);
                    }
                    break;
                case AxeState.Cleaving:
                    transform.position = owner.transform.position;
                    if (elapsed >= CleaveDuration * 0.15f && previousElapsed <= CleaveDuration * 0.8f)
                        hits.Melee(owner.transform.position, attackDirection, CleaveRadius, 360f,
                            new CombatHit(owner.gameObject, AttackKind.ChargedCleave,
                                Exhausted(Mathf.Max(1, Mathf.RoundToInt(settings.cleaveDamage * Mathf.Lerp(0.5f, 1f, cleaveStrength)))),
                                attackDirection, settings.cleaveKnockback * cleaveStrength,
                                settings.cleaveStagger * cleaveStrength, cleaveStrength >= 0.999f));
                    if (elapsed >= CleaveDuration)
                        SetState(AxeState.Held);
                    break;
                case AxeState.Flying:
                    if (!wasThrowing || flightSeconds > 0f)
                        FlyOut(wasThrowing ? flightSeconds : deltaTime);
                    break;
                case AxeState.Stuck:
                    if (stuckTarget != null && stuckTarget.gameObject.activeInHierarchy)
                        transform.position = stuckTarget.TransformPoint(stuckOffset);
                    // An owned throw is a combat tool: retrieve it by walking close.
                    // The initial, unowned OnGround pickup still requires F.
                    if (Vector2.Distance(transform.position, owner.transform.position) <= settings.retrieveDistance)
                    {
                        stuckTarget = null;
                        transform.position = owner.transform.position;
                        SetState(AxeState.Held);
                    }
                    break;
                case AxeState.Returning:
                    FlyBack(deltaTime);
                    break;
            }

            // Share manual Recall's unlock, damage and return behavior; never restart a return.
            if (owner.CanRecall && (State == AxeState.Flying || State == AxeState.Stuck) &&
                ((Vector2)(transform.position - owner.transform.position)).sqrMagnitude >
                settings.autoRecallDistance * settings.autoRecallDistance)
                TryRecall();
        }

        private int Exhausted(int damage) => exhaustedAttack ? Mathf.Max(1, Mathf.RoundToInt(damage * settings.exhaustedDamageMultiplier)) : damage;

        private void FlyOut(float deltaTime)
        {
            Vector2 origin = transform.position;
            float distance = Mathf.Min(settings.throwSpeed * deltaTime, settings.throwRange - travelled);
            Vector2 destination = origin + attackDirection * distance;
            var hit = new CombatHit(owner.gameObject, AttackKind.Throw, Exhausted(settings.throwDamage), attackDirection, settings.throwKnockback, 0.15f);
            if (hits.Flight(origin, destination, settings.flightRadius, hit, true, out RaycastHit2D impact))
            {
                FlightSeconds += impact.distance / settings.throwSpeed;
                transform.position = origin + attackDirection * impact.distance;
                stuckTarget = impact.collider != null && impact.collider.gameObject.activeInHierarchy
                    ? impact.collider.transform : null;
                if (stuckTarget != null)
                    stuckOffset = stuckTarget.InverseTransformPoint(transform.position);
                SetState(AxeState.Stuck);
                return;
            }
            transform.position = destination;
            FlightSeconds += distance / settings.throwSpeed;
            travelled += distance;
            if (travelled >= settings.throwRange - 0.001f)
            {
                stuckTarget = null;
                SetState(AxeState.Stuck);
            }
        }

        private void FlyBack(float deltaTime)
        {
            Vector2 origin = transform.position;
            Vector2 destination = Vector2.MoveTowards(origin, owner.transform.position, settings.recallSpeed * deltaTime);
            if (Vector2.Distance(origin, owner.transform.position) > 0.001f)
                ReturnApproachDirection = (origin - (Vector2)owner.transform.position).normalized;
            FlightSeconds += Vector2.Distance(origin, destination) / settings.recallSpeed;
            Vector2 direction = (destination - origin).normalized;
            hits.Flight(origin, destination, settings.flightRadius,
                new CombatHit(owner.gameObject, AttackKind.Recall, settings.recallDamage, direction, settings.recallKnockback, 0.2f), false, out _);
            transform.position = destination;
            // Return ignores solid terrain so the owned axe cannot get stranded.
            if (Vector2.Distance(destination, owner.transform.position) <= 0.1f)
            {
                SetState(AxeState.Held);
                catchRecoveryRemaining = settings.catchRecovery;
                ReturnedToHand?.Invoke();
            }
        }

        private void SetState(AxeState state)
        {
            State = state;
            elapsed = 0f;
            impactPauseRemaining = 0f;
        }

        private void OnDisable()
        {
            CancelLightCombo();
            CancelCharge();
            CancelThrow();
        }
    }
}
