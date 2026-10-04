using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLostShrine.UI
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-210)]
    public sealed class PauseMenuInput : MonoBehaviour
    {
        public event Action ToggleRequested;
        public event Action<int> NavigationRequested;
        public event Action SubmitRequested;
        private InputActionMap actions;
        private InputAction toggle, up, down, submit;
        private void Awake()
        {
            actions = new InputActionMap("Pause menu");
            toggle = actions.AddAction("Toggle", InputActionType.Button, "<Keyboard>/escape");
            up = actions.AddAction("Previous", InputActionType.Button, "<Keyboard>/upArrow");
            down = actions.AddAction("Next", InputActionType.Button, "<Keyboard>/downArrow");
            submit = actions.AddAction("Submit", InputActionType.Button, "<Keyboard>/enter");
        }
        private void OnEnable() => actions.Enable();
        private void OnDisable() => actions?.Disable();
        private void OnDestroy() => actions?.Dispose();
        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
            if (DevToolsPanel.CapturesInput) return;
#endif
            if (!Application.isFocused) return;
            PollInput();
        }
        private void PollInput()
        {
            if (toggle.WasPressedThisFrame()) { ToggleRequested?.Invoke(); return; }
            if (up.WasPressedThisFrame()) NavigationRequested?.Invoke(-1);
            if (down.WasPressedThisFrame()) NavigationRequested?.Invoke(1);
            if (submit.WasPressedThisFrame()) SubmitRequested?.Invoke();
        }
    }
}
