using UnityEngine;
using UnityEngine.InputSystem;

namespace RoadOfTheOldKing.Input
{
    [DisallowMultipleComponent]
    public sealed class PlayerMovementInput : MonoBehaviour, IMovementInput
    {
        private InputActionMap actions;
        private InputAction move;
        private InputAction sprint;
        private InputAction dash;
        private InputAction sprintToggle;
        private bool sprintLatched;
        private float dashQueuedUntil = -1f;
        private bool hasFocus = true;
        private bool paused;

        public Vector2 MoveDirection { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool IsActive => isActiveAndEnabled && hasFocus && !paused && Time.timeScale > 0f && (locks == null || !locks.IsLocked);
        private RoadOfTheOldKing.Player.PlayerControlLocks locks;
        private RoadOfTheOldKing.Player.PlayerStamina stamina;

        private void Awake()
        {
            locks = GetComponent<RoadOfTheOldKing.Player.PlayerControlLocks>();
            stamina = GetComponent<RoadOfTheOldKing.Player.PlayerStamina>();
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
            sprintToggle = actions.AddAction("Sprint Toggle", InputActionType.Button, GamepadLayout.Sprint);
            dash = actions.AddAction("Dash", InputActionType.Button, "<Keyboard>/space");
            dash.AddBinding(GamepadLayout.Dash);
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
            // L3 latches sprint the way holding Shift sustains it; stopping or a second click ends it.
            if (sprintToggle.WasPressedThisFrame()) sprintLatched = !sprintLatched;
            // Running out of stamina also ends it: a latched sprint must not restart on its own once stamina refills.
            if (MoveDirection == Vector2.zero || (stamina != null && stamina.Current <= 0f)) sprintLatched = false;
            SprintHeld = sprint.IsPressed() || sprintLatched;
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
            SprintHeld = false;
            sprintLatched = false;
            dashQueuedUntil = -1f;
        }
    }
}
