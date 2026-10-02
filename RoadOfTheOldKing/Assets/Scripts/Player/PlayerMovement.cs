using TheLostShrine.Input;
using TheLostShrine.Combat;
using UnityEngine;

namespace TheLostShrine.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    [RequireComponent(typeof(PlayerStamina), typeof(PlayerDash))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 4.5f;
        [SerializeField, Min(1f)] private float sprintMultiplier = 1.6f;
        [SerializeField, Min(0.1f)] private float sprintCostPerSecond = 20f;
        [Tooltip("Minimum stamina needed to start sprinting, preventing stuttering at empty.")]
        [SerializeField, Min(0f)] private float minimumSprintStamina = 20f;
        [SerializeField] private Vector2 initialFacing = Vector2.down;
        [Tooltip("A component on this player that implements IMovementInput.")]
        [SerializeField] private MonoBehaviour inputSource;
        [Header("Momentum")]
        [Tooltip("Ease free movement toward the input velocity instead of snapping. Off restores instant start/stop.")]
        [SerializeField] private bool useMomentum = true;
        [Tooltip("Units/s² when speeding up in roughly the same direction.")]
        [SerializeField, Min(1f)] private float acceleration = 60f;
        [Tooltip("Units/s² with no input: the stopping slide. Slide ≈ speed² / (2 × deceleration); walk speed 4.5, sprint 7.2.")]
        [SerializeField, Min(1f)] private float deceleration = 45f;
        [Tooltip("Units/s² when input opposes current motion, so reversals stay crisp.")]
        [SerializeField, Min(1f)] private float turnAcceleration = 90f;
        [Tooltip("Fraction of into-wall speed returned as a rebound off static colliders. 0 disables.")]
        [SerializeField, Range(0f, 1f)] private float wallBounce = 0f;
        [Tooltip("Minimum into-wall speed (units/s) before a rebound happens. Keep it above walk speed so walking into a wall never jitters.")]
        [SerializeField, Min(0f)] private float bounceMinSpeed = 5f;
        [Tooltip("Seconds after a rebound before input steers again, so the pop is visible even while pushing into the wall.")]
        [SerializeField, Range(0f, 0.5f)] private float bounceRecovery = 0.15f;
        [Tooltip("Minimum seconds between rebounds: one clean bounce per impact.")]
        [SerializeField, Range(0f, 1f)] private float bounceCooldown = 0.35f;
        private bool leavingAction;
        private Vector2 freeVelocity;
        private float bounceRemaining;
        private float lastBounceTime = float.NegativeInfinity;

        private Rigidbody2D body;
        private IMovementInput movementInput;
        private HitReaction hitReaction;
        private PlayerStamina stamina;
        private PlayerCombatController combat;
        private PlayerDash dash;
        private PlayerFlask flask;

        public float MoveSpeed => moveSpeed;
        public float SprintSpeed => moveSpeed * sprintMultiplier;
        public bool IsSprinting { get; private set; }
        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        private void Reset()
        {
            inputSource = GetComponent<IMovementInput>() as MonoBehaviour;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            hitReaction = GetComponent<HitReaction>();
            stamina = GetComponent<PlayerStamina>();
            combat = GetComponent<PlayerCombatController>();
            dash = GetComponent<PlayerDash>();
            flask = GetComponent<PlayerFlask>();
            FacingDirection = initialFacing.sqrMagnitude > 0f ? initialFacing.normalized : Vector2.down;
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.linearDamping = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            if (inputSource == null)
                inputSource = GetComponent<IMovementInput>() as MonoBehaviour;

            movementInput = inputSource as IMovementInput;
            if (movementInput == null)
            {
                Debug.LogError("PlayerMovement needs an IMovementInput component on the player.", this);
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            Vector2 actionDisplacement = combat != null && combat.Weapon != null
                ? combat.Weapon.ConsumeActionDisplacement() : Vector2.zero;
            dash.Tick(Time.fixedDeltaTime);
            bool dashRequested = movementInput.ConsumeDashPress();
            // Only the free-movement branch below sets this; every other owner clears the rebound source.
            freeVelocity = Vector2.zero;
            // Let the reaction's impulse move the body during stagger.
            if (hitReaction != null && hitReaction.IsStaggered)
            {
                IsSprinting = false;
                dash.Cancel();
                return;
            }

            if (inputSource == null || !inputSource.isActiveAndEnabled || !movementInput.IsActive)
            {
                IsSprinting = false;
                bounceRemaining = 0f;
                dash.Cancel();
                body.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 direction = Vector2.ClampMagnitude(movementInput.MoveDirection, 1f);

            if (dashRequested)
                dash.TryStart(direction.sqrMagnitude > 0f ? direction : FacingDirection);
            if (dash.IsDashing)
            {
                IsSprinting = false;
                leavingAction = true;
                bounceRemaining = 0f;
                FacingDirection = dash.Direction;
                stamina.DelayRecovery();
                body.linearVelocity = dash.GetVelocity(Time.fixedDeltaTime);
                return;
            }

            if (combat != null && (combat.ControlsMovement || actionDisplacement.sqrMagnitude > 0f))
            {
                IsSprinting = false;
                FacingDirection = combat.ActionFacing;
                body.linearVelocity = direction * moveSpeed * combat.ActionMovementScale + actionDisplacement / Time.fixedDeltaTime;
                return;
            }

            if (direction.sqrMagnitude > 0f)
                FacingDirection = direction.normalized;

            bool drinking = flask != null && flask.IsDrinking;
            bool wantsSprint = !drinking && direction.sqrMagnitude > 0f && movementInput.SprintHeld &&
                (combat == null || !combat.IsAttacking);
            IsSprinting = wantsSprint && stamina != null &&
                (IsSprinting || stamina.Current >= minimumSprintStamina) &&
                stamina.TryDrain(sprintCostPerSecond * Time.fixedDeltaTime);

            // Velocity is units per second. Unity applies the physics time step.
            // Moving the Rigidbody, instead of the Transform, preserves collisions.
            Vector2 target = direction * (IsSprinting ? SprintSpeed : moveSpeed) * (drinking ? flask.MoveScale : 1f);
            if (bounceRemaining > 0f)
            {
                // Let the rebound play out under friction before input steers again.
                bounceRemaining -= Time.fixedDeltaTime;
                target = Vector2.zero;
            }
            body.linearVelocity = freeVelocity = useMomentum ? Approach(body.linearVelocity, target, Time.fixedDeltaTime) : target;
        }

        // Runs after the physics step that stopped us, so the wall already removed the normal
        // component; add a fraction back outward and let Approach() ease out of the rebound.
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!useMomentum || wallBounce <= 0f || collision.contactCount == 0 || dash.IsDashing) return;
            var other = collision.rigidbody;
            if (other != null && other.bodyType == RigidbodyType2D.Dynamic) return; // enemies own their knockback
            Vector2 normal = collision.GetContact(0).normal;
            float into = Vector2.Dot(freeVelocity, normal);
            if (into > 0f) { normal = -normal; into = -into; }
            if (-into < bounceMinSpeed || Time.time - lastBounceTime < bounceCooldown) return;
            lastBounceTime = Time.time;
            bounceRemaining = bounceRecovery;
            body.linearVelocity += normal * (-into * wallBounce);
            freeVelocity = Vector2.zero;
        }

        // The body's velocity already reflects collisions, so a slide stops at walls and glides along them.
        private Vector2 Approach(Vector2 current, Vector2 target, float deltaTime)
        {
            // A dash keeps its designed distance: only walking speed carries out of it.
            if (leavingAction)
            {
                leavingAction = false;
                current = Vector2.ClampMagnitude(current, moveSpeed);
            }
            float rate = target.sqrMagnitude < 0.0001f ? deceleration
                : Vector2.Dot(current, target) < 0f ? turnAcceleration : acceleration;
            return Vector2.MoveTowards(current, target, rate * deltaTime);
        }

        private void OnDisable()
        {
            IsSprinting = false;
            if (dash != null)
                dash.Cancel();
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }
    }
}
