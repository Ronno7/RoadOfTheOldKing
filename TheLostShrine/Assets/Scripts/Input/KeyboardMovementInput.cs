using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLostShrine.Input
{
    [DisallowMultipleComponent]
    public sealed class KeyboardMovementInput : MonoBehaviour, IMovementInput
    {
        private readonly CardinalInput cardinal = new CardinalInput();
        private InputActionMap actions;
        private InputAction up;
        private InputAction down;
        private InputAction left;
        private InputAction right;
        private bool hasFocus = true;
        private bool paused;

        public Vector2 MoveDirection { get; private set; }

        private void Awake()
        {
            actions = new InputActionMap("Player Movement");
            up = AddDirection("Up", "<Keyboard>/w", "<Keyboard>/upArrow");
            down = AddDirection("Down", "<Keyboard>/s", "<Keyboard>/downArrow");
            left = AddDirection("Left", "<Keyboard>/a", "<Keyboard>/leftArrow");
            right = AddDirection("Right", "<Keyboard>/d", "<Keyboard>/rightArrow");
        }

        private InputAction AddDirection(string name, string primary, string alternate)
        {
            InputAction action = actions.AddAction(name, InputActionType.Button, primary);
            action.AddBinding(alternate);
            return action;
        }

        private void OnEnable() => actions.Enable();

        private void Update()
        {
            if (!hasFocus || paused)
                return;

            switch (cardinal.Read(up.IsPressed(), down.IsPressed(), left.IsPressed(), right.IsPressed()))
            {
                case CardinalDirection.Up: MoveDirection = Vector2.up; break;
                case CardinalDirection.Down: MoveDirection = Vector2.down; break;
                case CardinalDirection.Left: MoveDirection = Vector2.left; break;
                case CardinalDirection.Right: MoveDirection = Vector2.right; break;
                default: MoveDirection = Vector2.zero; break;
            }
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
            cardinal.Clear();
            MoveDirection = Vector2.zero;
        }
    }
}
