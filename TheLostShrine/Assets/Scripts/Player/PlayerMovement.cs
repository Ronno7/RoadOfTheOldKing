using TheLostShrine.Input;
using TheLostShrine.Combat;
using UnityEngine;

namespace TheLostShrine.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float moveSpeed = 4.5f;
        [SerializeField] private Vector2 initialFacing = Vector2.down;
        [Tooltip("A component on this player that implements IMovementInput.")]
        [SerializeField] private MonoBehaviour inputSource;

        private Rigidbody2D body;
        private IMovementInput movementInput;
        private HitReaction hitReaction;

        public float MoveSpeed => moveSpeed;
        public Vector2 FacingDirection { get; private set; } = Vector2.down;

        private void Reset()
        {
            inputSource = GetComponent<IMovementInput>() as MonoBehaviour;
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            hitReaction = GetComponent<HitReaction>();
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
            // Let the reaction's impulse move the body during stagger.
            if (hitReaction != null && hitReaction.IsStaggered)
                return;

            Vector2 direction = inputSource != null && inputSource.isActiveAndEnabled
                ? Vector2.ClampMagnitude(movementInput.MoveDirection, 1f)
                : Vector2.zero;

            if (direction.sqrMagnitude > 0f)
                FacingDirection = direction.normalized;

            // Velocity is units per second. Unity applies the physics time step.
            // Moving the Rigidbody, instead of the Transform, preserves collisions.
            body.linearVelocity = direction * moveSpeed;
        }

        private void OnDisable()
        {
            if (body != null)
                body.linearVelocity = Vector2.zero;
        }
    }
}
