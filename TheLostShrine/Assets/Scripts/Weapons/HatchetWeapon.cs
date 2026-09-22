using TheLostShrine.Combat;
using TheLostShrine.Player;
using UnityEngine;

namespace TheLostShrine.Weapons
{
    public enum HatchetState { OnGround, Held, LightChop, Charging, Cleaving, Flying, Stuck, Returning }

    [DisallowMultipleComponent]
    public sealed class HatchetWeapon : MonoBehaviour
    {
        [SerializeField] private HatchetSettings settings;
        [SerializeField] private Collider2D pickupCollider;
        private PlayerCombatController owner;
        private HatchetHitDetector hits;
        private Transform stuckTarget;
        private Vector3 stuckOffset;
        private float elapsed;
        private float travelled;
        private float comboRemaining;
        private float cleaveStrength;
        private Vector2 attackDirection = Vector2.right;

        public HatchetSettings Settings => settings;
        public HatchetState State { get; private set; } = HatchetState.OnGround;
        public PlayerCombatController Owner => owner;
        public Vector2 AimDirection { get; private set; } = Vector2.right;
        public Vector2 AttackDirection => attackDirection;
        public int ComboIndex { get; private set; }
        public int ComboStep => State == HatchetState.LightChop ||
            (State == HatchetState.Held && comboRemaining > 0f) ? ComboIndex + 1 : 0;
        public float ComboTimeRemaining => comboRemaining;
        public float LightReach => settings.lightRadius + (ComboIndex == 2 ? 0.15f : 0f);
        public bool IsAway => State == HatchetState.Flying || State == HatchetState.Stuck || State == HatchetState.Returning;
        public float Charge01 => State == HatchetState.Charging
            ? Mathf.Clamp01(elapsed / settings.fullCharge) : State == HatchetState.Cleaving ? cleaveStrength : 0f;
        public float AttackProgress => Mathf.Clamp01(elapsed / (State == HatchetState.Cleaving
            ? settings.cleaveDuration : LightDuration));
        public float LightDuration => settings.lightDuration * (ComboIndex == 2 ? settings.finisherDurationMultiplier : 1f);

        private void Awake()
        {
            if (pickupCollider == null)
                pickupCollider = GetComponent<Collider2D>();
            if (settings == null)
            {
                Debug.LogError("HatchetWeapon needs a HatchetSettings asset.", this);
                enabled = false;
            }
        }

        public bool TryEquip(PlayerCombatController newOwner)
        {
            if (!isActiveAndEnabled || State != HatchetState.OnGround || newOwner == null)
                return false;
            owner = newOwner;
            hits = new HatchetHitDetector(owner.transform, transform);
            if (pickupCollider != null)
                pickupCollider.enabled = false;
            transform.position = owner.transform.position;
            SetState(HatchetState.Held);
            return true;
        }

        public void SetAim(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.001f)
                AimDirection = direction.normalized;
        }

        public bool TryLightChop(Vector2 direction)
        {
            if (!isActiveAndEnabled || State != HatchetState.Held)
                return false;
            SetAim(direction);
            attackDirection = AimDirection;
            ComboIndex = comboRemaining > 0f ? (ComboIndex + 1) % 3 : 0;
            hits.BeginAttack();
            SetState(HatchetState.LightChop);
            return true;
        }

        public bool TryBeginCharge()
        {
            if (!isActiveAndEnabled || State != HatchetState.Held)
                return false;
            comboRemaining = 0f;
            SetState(HatchetState.Charging);
            return true;
        }

        public bool TryReleaseCharge(Vector2 direction)
        {
            if (State != HatchetState.Charging)
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
            SetState(HatchetState.Cleaving);
            return true;
        }

        public void CancelCharge()
        {
            if (State == HatchetState.Charging)
                SetState(HatchetState.Held);
        }

        // Death cancels damage in progress, including a thrown or returning hatchet.
        public void CancelAction()
        {
            if (owner == null)
                return;
            stuckTarget = null;
            comboRemaining = 0f;
            transform.position = owner.transform.position;
            SetState(HatchetState.Held);
        }

        public bool TryThrow(Vector2 direction)
        {
            if (!isActiveAndEnabled || State != HatchetState.Held)
                return false;
            SetAim(direction);
            attackDirection = AimDirection;
            // Start at the player center so a nearby wall cannot be skipped.
            transform.position = owner.transform.position;
            travelled = 0f;
            comboRemaining = 0f;
            hits.BeginAttack();
            SetState(HatchetState.Flying);
            return true;
        }

        public bool TryRecall()
        {
            if (!isActiveAndEnabled || owner == null || !owner.CanRecall ||
                (State != HatchetState.Flying && State != HatchetState.Stuck))
                return false;
            stuckTarget = null;
            hits.BeginAttack(); // A target can be hit once outbound and once on return.
            SetState(HatchetState.Returning);
            return true;
        }

        private void FixedUpdate() => Simulate(Time.fixedDeltaTime);

        // One state machine controls action exclusivity and flight lifecycle.
        private void Simulate(float deltaTime)
        {
            if (deltaTime <= 0f || State == HatchetState.OnGround)
                return;
            if (owner == null)
            {
                SetState(HatchetState.OnGround);
                if (pickupCollider != null)
                    pickupCollider.enabled = true;
                return;
            }

            float previousElapsed = elapsed;
            elapsed += deltaTime;
            switch (State)
            {
                case HatchetState.Held:
                    comboRemaining = Mathf.Max(0f, comboRemaining - deltaTime);
                    transform.position = owner.transform.position;
                    break;
                case HatchetState.Charging:
                    transform.position = owner.transform.position;
                    break;
                case HatchetState.LightChop:
                    transform.position = owner.transform.position;
                    if (elapsed >= LightDuration * settings.lightWindupFraction &&
                        previousElapsed < LightDuration * settings.lightSwingEndFraction)
                        hits.Melee(owner.transform.position, attackDirection,
                            LightReach, settings.lightArc,
                            new CombatHit(owner.gameObject, AttackKind.LightChop,
                                ComboIndex == 2 ? settings.finisherDamage : settings.lightDamage,
                                attackDirection, ComboIndex == 2 ? 2f : 0.6f, 0.12f));
                    if (elapsed >= LightDuration)
                    {
                        comboRemaining = settings.comboWindow;
                        SetState(HatchetState.Held);
                    }
                    break;
                case HatchetState.Cleaving:
                    transform.position = owner.transform.position;
                    if (elapsed >= settings.cleaveDuration * 0.15f && previousElapsed <= settings.cleaveDuration * 0.8f)
                        hits.Melee(owner.transform.position, attackDirection, settings.cleaveRadius, 360f,
                            new CombatHit(owner.gameObject, AttackKind.ChargedCleave,
                                Mathf.Max(1, Mathf.RoundToInt(settings.cleaveDamage * Mathf.Lerp(0.5f, 1f, cleaveStrength))),
                                attackDirection, settings.cleaveKnockback * cleaveStrength,
                                settings.cleaveStagger * cleaveStrength, cleaveStrength >= 0.999f));
                    if (elapsed >= settings.cleaveDuration)
                        SetState(HatchetState.Held);
                    break;
                case HatchetState.Flying:
                    FlyOut(deltaTime);
                    break;
                case HatchetState.Stuck:
                    if (stuckTarget != null && stuckTarget.gameObject.activeInHierarchy)
                        transform.position = stuckTarget.TransformPoint(stuckOffset);
                    if (Vector2.Distance(transform.position, owner.transform.position) <= settings.retrieveDistance)
                    {
                        stuckTarget = null;
                        SetState(HatchetState.Held);
                    }
                    break;
                case HatchetState.Returning:
                    FlyBack(deltaTime);
                    break;
            }
        }

        private void FlyOut(float deltaTime)
        {
            Vector2 origin = transform.position;
            float distance = Mathf.Min(settings.throwSpeed * deltaTime, settings.throwRange - travelled);
            Vector2 destination = origin + attackDirection * distance;
            var hit = new CombatHit(owner.gameObject, AttackKind.Throw, settings.throwDamage, attackDirection, 1f, 0.15f);
            if (hits.Flight(origin, destination, settings.flightRadius, hit, true, out RaycastHit2D impact))
            {
                transform.position = origin + attackDirection * impact.distance;
                stuckTarget = impact.collider != null && impact.collider.gameObject.activeInHierarchy
                    ? impact.collider.transform : null;
                if (stuckTarget != null)
                    stuckOffset = stuckTarget.InverseTransformPoint(transform.position);
                SetState(HatchetState.Stuck);
                return;
            }
            transform.position = destination;
            travelled += distance;
            if (travelled >= settings.throwRange - 0.001f)
            {
                stuckTarget = null;
                SetState(HatchetState.Stuck);
            }
        }

        private void FlyBack(float deltaTime)
        {
            Vector2 origin = transform.position;
            Vector2 destination = Vector2.MoveTowards(origin, owner.transform.position, settings.recallSpeed * deltaTime);
            Vector2 direction = (destination - origin).normalized;
            hits.Flight(origin, destination, settings.flightRadius,
                new CombatHit(owner.gameObject, AttackKind.Recall, settings.recallDamage, direction, 1.5f, 0.2f), false, out _);
            transform.position = destination;
            // Return ignores solid terrain so the owned hatchet cannot get stranded.
            if (Vector2.Distance(destination, owner.transform.position) <= 0.1f)
                SetState(HatchetState.Held);
        }

        private void SetState(HatchetState state)
        {
            State = state;
            elapsed = 0f;
        }

        private void OnDisable() => CancelCharge();
    }
}
