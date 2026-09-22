using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLostShrine.Input
{
    [DisallowMultipleComponent]
    public sealed class PlayerMovementInput : MonoBehaviour, IMovementInput
    {
        private InputActionMap actions;
        private InputAction move;
        private InputAction sprint;
        private InputAction dash;
        private float dashQueuedUntil = -1f;
        private bool hasFocus = true;
        private bool paused;

        public Vector2 MoveDirection { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool IsActive => isActiveAndEnabled && hasFocus && !paused && Time.timeScale > 0f;

        private void Awake()
        {
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

            sprint = actions.AddAction("Sprint", InputActionType.Button);
            sprint.AddBinding("<Keyboard>/leftShift");
            sprint.AddBinding("<Keyboard>/rightShift");
            dash = actions.AddAction("Dash", InputActionType.Button, "<Keyboard>/space");
        }

        private void OnEnable() => actions.Enable();

        private void Update()
        {
            if (!IsActive)
            {
                ClearInput();
                return;
            }

            MoveDirection = move.ReadValue<Vector2>();
            SprintHeld = sprint.IsPressed();
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
            dashQueuedUntil = -1f;
        }
    }
}
