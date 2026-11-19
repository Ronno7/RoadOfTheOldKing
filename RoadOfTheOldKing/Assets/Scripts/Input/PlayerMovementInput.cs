using UnityEngine;
using UnityEngine.InputSystem;

namespace RoadOfTheOldKing.Input
{
    [DisallowMultipleComponent]
    public sealed class PlayerMovementInput : MonoBehaviour, IMovementInput
    {
        // Super Mario 64 style (user, 18 Nov): walking speed follows the stick's tilt on a curve, fine near the
        // centre, full speed at the rim. The walk cycle already advances with distance, so slower steps don't slide.
        [Tooltip("Controller: share of walking speed at the smallest tilt past the deadzone. Full tilt is full speed.")]
        [SerializeField, Range(0f, 1f)] private float slowestTilt = .2f;
        // Dark Souls on the controller's B (user, 18 Nov): a tap dodges as it's let go, a hold sprints and never dodges.
        [Tooltip("Controller: B let go sooner than this (real seconds) dodges; held longer, it sprints instead.")]
        [SerializeField, Min(0.05f)] private float dodgeTapTime = .25f;
        private InputActionMap actions;
        private InputAction move;
        private InputAction sprint;
        private InputAction dash;
        private InputAction padDodge;
        // When the B press being held began (unscaled), or -1: only a press made in play counts, so B still held
        // from closing a menu neither sprints nor dodges.
        private float padPressedAt = -1f;
        private float dashQueuedUntil = -1f;
        private bool hasFocus = true;
        private bool paused;

        public Vector2 MoveDirection { get; private set; }
        public float MoveAmount { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool IsActive => isActiveAndEnabled && hasFocus && !paused && !RoadOfTheOldKing.UI.SimulationPause.BlocksPlay && (locks == null || !locks.IsLocked);
        private RoadOfTheOldKing.Player.PlayerControlLocks locks;

        private void Awake()
        {
            locks = GetComponent<RoadOfTheOldKing.Player.PlayerControlLocks>();
            actions = new InputActionMap("Player Movement");
            move = actions.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");

            // DigitalNormalized gives eight directions at the same speed.
            // Sharing one composite also lets WASD and arrow keys work together.
            move.AddCompositeBinding("2DVector(mode=0)")
                .With("Up", "<Keyboard>/w")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/s")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/a")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/d")
                .With("Right", "<Keyboard>/rightArrow");
            // The stick shares the action; Update reduces its tilt to the same unit direction.
            move.AddBinding(GamepadLayout.Move).WithProcessor(GamepadLayout.MoveProcessors);

            sprint = actions.AddAction("Sprint", InputActionType.Button);
            sprint.AddBinding("<Keyboard>/leftShift");
            sprint.AddBinding("<Keyboard>/rightShift");
            dash = actions.AddAction("Dash", InputActionType.Button, "<Keyboard>/space");
            padDodge = actions.AddAction("Dodge or Sprint", InputActionType.Button, GamepadLayout.Dash);
        }

        private void OnEnable() => actions.Enable();

        private void Update()
        {
            if (!IsActive)
            {
                ClearInput();
                return;
            }

            Vector2 value = move.ReadValue<Vector2>();
            MoveDirection = value.sqrMagnitude > 0.0001f ? value.normalized : Vector2.zero;
            // Keys give unit vectors; the stick's deadzone processor rescales its tilt to 0..1 past the deadzone.
            float tilt = Mathf.Clamp01(value.magnitude);
            MoveAmount = MoveDirection == Vector2.zero ? 0f : Mathf.Lerp(slowestTilt, 1f, tilt * tilt);
            // Keys: Space dodges on press and Shift sprints. B: held past the tap time it sprints; let go sooner, it
            // dodges then. A press and release inside one frame is a tap.
            float now = Time.unscaledTime;
            if (padDodge.WasPressedThisFrame()) padPressedAt = now;
            bool padSprint = padPressedAt >= 0f && padDodge.IsPressed() && now - padPressedAt >= dodgeTapTime;
            if (padPressedAt >= 0f && !padDodge.IsPressed())
            {
                if (now - padPressedAt < dodgeTapTime) dashQueuedUntil = Time.time + 0.12f;
                padPressedAt = -1f;
            }
            SprintHeld = sprint.IsPressed() || padSprint;
            if (dash.WasPressedThisFrame())
                dashQueuedUntil = Time.time + 0.12f;
        }

        // Update may run between physics steps; consume a short press exactly once.
        public bool ConsumeDashPress()
        {
            bool requested = IsActive && dashQueuedUntil >= Time.time;
            dashQueuedUntil = -1f;
            return requested;
        }

        private void OnDisable()
        {
            actions?.Disable();
            ClearInput();
        }

        private void OnDestroy() => actions?.Dispose();

        private void OnApplicationFocus(bool focused)
        {
            hasFocus = focused;
            ClearInput();
        }

        private void OnApplicationPause(bool isPaused)
        {
            paused = isPaused;
            ClearInput();
        }

        private void ClearInput()
        {
            MoveDirection = Vector2.zero;
            MoveAmount = 0f;
            SprintHeld = false;
            padPressedAt = -1f;
            dashQueuedUntil = -1f;
        }
    }
}
