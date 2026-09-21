using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLostShrine.Input
{
    [DisallowMultipleComponent]
    public sealed class PlayerMovementInput : MonoBehaviour, IMovementInput
    {
        private InputActionMap actions;
        private InputAction move;
        private bool hasFocus = true;
        private bool paused;

        public Vector2 MoveDirection { get; private set; }

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
        }

        private void OnEnable() => actions.Enable();

        private void Update()
        {
            if (!hasFocus || paused)
                return;

            MoveDirection = move.ReadValue<Vector2>();
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
        }
    }
}
