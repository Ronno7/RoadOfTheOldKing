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

        private Rigidbody2D body;
        private IMovementInput movementInput;
        private HitReaction hitReaction;
        private PlayerStamina stamina;
        private PlayerCombatController combat;
        private PlayerDash dash;
        private PlayerChopAnimation chop;

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
            chop = GetComponent<PlayerChopAnimation>();
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
            dash.Tick(Time.fixedDeltaTime);
            bool dashRequested = movementInput.ConsumeDashPress();
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
                FacingDirection = dash.Direction;
                stamina.DelayRecovery();
                body.linearVelocity = dash.GetVelocity(Time.fixedDeltaTime);
                return;
            }

            if (chop != null && chop.IsPlaying)
            {
                IsSprinting = false;
                FacingDirection = combat.Weapon.AttackDirection;
                body.linearVelocity = direction * moveSpeed * chop.MovementScale;
                return;
            }

            if (direction.sqrMagnitude > 0f)
                FacingDirection = direction.normalized;

            bool wantsSprint = direction.sqrMagnitude > 0f && movementInput.SprintHeld &&
                (combat == null || !combat.IsAttacking);
            IsSprinting = wantsSprint && stamina != null &&
                (IsSprinting || stamina.Current >= minimumSprintStamina) &&
                stamina.TrySpend(sprintCostPerSecond * Time.fixedDeltaTime);

            // Velocity is units per second. Unity applies the physics time step.
            // Moving the Rigidbody, instead of the Transform, preserves collisions.
            body.linearVelocity = direction * (IsSprinting ? SprintSpeed : moveSpeed);
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
