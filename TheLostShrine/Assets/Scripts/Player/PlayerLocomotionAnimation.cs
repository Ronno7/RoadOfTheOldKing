using TheLostShrine.Combat;
using UnityEngine;

namespace TheLostShrine.Player
{
    // Samples native sprite clips by distance travelled. Physics remains the motor's job.
    [DisallowMultipleComponent, RequireComponent(typeof(PlayerMovement))]
    [DefaultExecutionOrder(100)]
    public sealed class PlayerLocomotionAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [Tooltip("World units covered by one complete four-frame walk cycle.")]
        [SerializeField, Min(0.1f)] private float strideLength = 2.25f;
        [Tooltip("Ground distance for a complete authored eight-cel sprint. Missing views retain the walk cadence.")]
        [SerializeField, Min(0.1f)] private float sprintStrideLength = 3.2f;
        [Tooltip("Subtle visual breathing, anchored at the sprite's feet. No physics motion.")]
        [SerializeField, Range(0f, 0.03f)] private float idleBreathAmount = 0.012f;
        [SerializeField, Min(1f)] private float idleBreathPeriod = 3.2f;
        [SerializeField, Min(0f)] private float idleSettleTime = 0.35f;

        private static readonly int[] IdleStates = {
            Animator.StringToHash("Base Layer.IdleSouth"), Animator.StringToHash("Base Layer.IdleEast"),
            Animator.StringToHash("Base Layer.IdleWest"), Animator.StringToHash("Base Layer.IdleNorth")
        };
        private static readonly int[] WalkStates = {
            Animator.StringToHash("Base Layer.WalkSouth"), Animator.StringToHash("Base Layer.WalkEast"),
            Animator.StringToHash("Base Layer.WalkWest"), Animator.StringToHash("Base Layer.WalkNorth")
        };

        private PlayerMovement movement;
        private PlayerDash dash;
        private HitReaction reaction;
        private Rigidbody2D body;
        private PlayerCombatController combat;
        private PlayerChopAnimation chop;
        private RegisteredPlayerAnimation registered;
        private Vector3 restingScale;
        private float idleElapsed;
        private Vector2 previousPosition;
        private float cycle;
        private bool wasWalking;
        private int displayedState;
        private int displayedFrame = -1;

        private void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            dash = GetComponent<PlayerDash>();
            reaction = GetComponent<HitReaction>();
            body = GetComponent<Rigidbody2D>();
            combat = GetComponent<PlayerCombatController>();
            chop = GetComponent<PlayerChopAnimation>();
            registered = GetComponent<RegisteredPlayerAnimation>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                Debug.LogError("Player locomotion needs the player sprite Animator and controller.", this);
                enabled = false;
                return;
            }
            // Clip time is sampled explicitly; no root motion, blending or texture interpolation.
            animator.speed = 0f;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            restingScale = animator.transform.localScale;
        }

        private void OnEnable()
        {
            previousPosition = transform.position;
            cycle = 0f;
            wasWalking = false;
            displayedFrame = -1;
            ResetIdleMotion();
        }

        private void LateUpdate() => Present(Time.deltaTime);

        private void Present(float deltaTime)
        {
            Vector2 position = transform.position;
            Vector2 displacement = position - previousPosition;
            float distance = displacement.magnitude;
            previousPosition = position;
            if (deltaTime <= 0f)
                return;
            bool teleported = distance > Mathf.Max(1f, movement.SprintSpeed * deltaTime * 2f);

            if (registered != null)
            {
                registered.PrepareTravelFrame(deltaTime, displacement, teleported);
                if (registered.IsPresentingBody)
                {
                    if (chop != null) chop.HideHandLayer();
                    ResetIdleMotion();
                    wasWalking = false;
                    displayedFrame = -1;
                    registered.TryApplyBody();
                    return;
                }
            }

            if (chop != null && chop.IsPlaying)
            {
                ResetIdleMotion();
                wasWalking = false;
                displayedFrame = -1;
                chop.TryApplyBody();
                return;
            }

            Vector2 facing = movement.FacingDirection;
            if (combat != null && combat.IsAttacking)
                facing = combat.ActionFacing;
            int direction = Mathf.Abs(facing.x) >= Mathf.Abs(facing.y)
                ? (facing.x > 0f ? 1 : 2) : (facing.y > 0f ? 3 : 0);
            bool canMove = movement.isActiveAndEnabled && body.simulated &&
                (reaction == null || !reaction.IsStaggered);
            // A respawn/teleport must not fast-forward the feet through many strides.

            if (canMove && dash != null && dash.IsDashing && !teleported)
            {
                ResetIdleMotion();
                wasWalking = false;
                SetPose(WalkStates[direction], 2);
                return;
            }

            bool walking = canMove && !teleported && distance > 0.0001f &&
                body.linearVelocity.sqrMagnitude > 0.0025f;
            if (walking)
            {
                // Start on the first step; retain cycle phase when changing direction.
                bool drawnSprint = movement.IsSprinting && registered != null && registered.HasSprintView(facing);
                if (!wasWalking)
                    cycle = drawnSprint ? 0f : 0.25f;
                cycle = Mathf.Repeat(cycle + distance / (drawnSprint ? sprintStrideLength : strideLength), 1f);
                if (drawnSprint && registered.TryPresentSprint(facing, cycle))
                {
                    ResetIdleMotion();
                    wasWalking = true;
                    displayedFrame = -1; // Ensure the original Animator writes again when sprint yields.
                    if (chop != null) chop.HideHandLayer();
                    registered.TryApplyBody();
                    return;
                }
                SetPose(WalkStates[direction], Mathf.FloorToInt(cycle * 4f));
            }
            else
            {
                cycle = 0f;
                SetPose(IdleStates[direction], 0);
            }
            wasWalking = walking;
            UpdateIdleMotion(canMove && !walking && !teleported &&
                (combat == null || !combat.IsAttacking), deltaTime);
        }

        private void UpdateIdleMotion(bool resting, float deltaTime)
        {
            if (!resting)
            {
                ResetIdleMotion();
                return;
            }
            idleElapsed += deltaTime;
            float time = Mathf.Max(0f, idleElapsed - idleSettleTime);
            float breath = (1f - Mathf.Cos(time * Mathf.PI * 2f / idleBreathPeriod)) * 0.5f;
            animator.transform.localScale = new Vector3(restingScale.x,
                restingScale.y * (1f + breath * idleBreathAmount), restingScale.z);
        }

        private void ResetIdleMotion()
        {
            idleElapsed = 0f;
            if (animator != null && restingScale != Vector3.zero)
                animator.transform.localScale = restingScale;
        }

        private void OnDisable() => ResetIdleMotion();

        private void SetPose(int state, int frame)
        {
            if (state == displayedState && frame == displayedFrame)
                return;
            animator.Play(state, 0, frame * 0.25f);
            animator.Update(0f);
            displayedState = state;
            displayedFrame = frame;
        }
    }
}
