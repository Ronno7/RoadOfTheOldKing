using System;
using RoadOfTheOldKing.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RoadOfTheOldKing.UI
{
    // Menu input for MenuStack. Escape/Start toggle (step back, or open pause when nothing is open);
    // the controller's B only steps back, because outside menus it dodges.
    [DisallowMultipleComponent, DefaultExecutionOrder(-210)]
    public sealed class PauseMenuInput : MonoBehaviour
    {
        public event Action ToggleRequested;
        public event Action BackRequested;
        public event Action<int> NavigationRequested;
        public event Action SubmitRequested;
        // Left/right: -1 or 1. Only pages with values to adjust listen.
        public event Action<int> AdjustRequested;
        private InputActionMap actions;
        private InputAction toggle, back, up, down, left, right, submit;
        private void Awake()
        {
            actions = new InputActionMap("Pause menu");
            toggle = actions.AddAction("Toggle", InputActionType.Button, "<Keyboard>/escape");
            toggle.AddBinding(GamepadLayout.Pause);
            back = actions.AddAction("Back", InputActionType.Button, GamepadLayout.Back);
            up = actions.AddAction("Previous", InputActionType.Button, "<Keyboard>/upArrow");
            up.AddBinding(GamepadLayout.MenuUp);
            up.AddBinding(GamepadLayout.MenuStickUp);
            down = actions.AddAction("Next", InputActionType.Button, "<Keyboard>/downArrow");
            down.AddBinding(GamepadLayout.MenuDown);
            down.AddBinding(GamepadLayout.MenuStickDown);
            left = actions.AddAction("Left", InputActionType.Button, "<Keyboard>/leftArrow");
            left.AddBinding(GamepadLayout.MenuLeft);
            right = actions.AddAction("Right", InputActionType.Button, "<Keyboard>/rightArrow");
            right.AddBinding(GamepadLayout.MenuRight);
            submit = actions.AddAction("Submit", InputActionType.Button, "<Keyboard>/enter");
            submit.AddBinding(GamepadLayout.Submit);
        }
        private void OnEnable() => actions.Enable();
        private void OnDisable() => actions?.Disable();
        private void OnDestroy() => actions?.Dispose();
        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || ROTOK_DEV_TOOLS
            if (DevToolsPanel.CapturesInput) return;
#endif
            if (!Application.isFocused) return;
            PollInput();
        }
        private void PollInput()
        {
            if (toggle.WasPressedThisFrame()) { ToggleRequested?.Invoke(); return; }
            if (back.WasPressedThisFrame()) { BackRequested?.Invoke(); return; }
            if (up.WasPressedThisFrame()) NavigationRequested?.Invoke(-1);
            if (down.WasPressedThisFrame()) NavigationRequested?.Invoke(1);
            if (left.WasPressedThisFrame()) AdjustRequested?.Invoke(-1);
            if (right.WasPressedThisFrame()) AdjustRequested?.Invoke(1);
            if (submit.WasPressedThisFrame()) SubmitRequested?.Invoke();
        }
    }
}
