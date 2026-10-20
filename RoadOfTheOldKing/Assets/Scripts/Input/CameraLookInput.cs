using UnityEngine;
using UnityEngine.InputSystem;

namespace RoadOfTheOldKing.Input
{
    // Input adapter only: camera policy and movement live in the camera components.
    // Alt looks toward the mouse cursor; holding the controller's look button steers with the right stick.
    [DisallowMultipleComponent]
    public sealed class CameraLookInput : MonoBehaviour, ICameraLookInput
    {
        private InputAction look;
        private InputAction padLook;
        private InputAction stick;
        private bool focused = true;
        private bool paused;
        private bool requireRelease;

        private void Awake()
        {
            look = new InputAction("Freelook", InputActionType.Button, "<Keyboard>/leftAlt");
            look.AddBinding("<Keyboard>/rightAlt");
            padLook = new InputAction("Freelook Controller", InputActionType.Button, GamepadLayout.Look);
            stick = new InputAction("Freelook Stick", InputActionType.Value, GamepadLayout.Aim, expectedControlType: "Vector2");
        }
        public bool IsLooking { get; private set; }
        private void OnEnable() { look.Enable(); padLook.Enable(); stick.Enable(); }
        private void OnDisable() { look?.Disable(); padLook?.Disable(); stick?.Disable(); requireRelease = true; }
        private void OnDestroy() { look?.Dispose(); padLook?.Dispose(); stick?.Dispose(); }
        private void OnApplicationFocus(bool value) { focused = value; if (!value) requireRelease = true; }
        private void OnApplicationPause(bool value) { paused = value; if (value) requireRelease = true; }

        public CameraLookInputFrame Read()
        {
            IsLooking = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_WEBGL
            if (RoadOfTheOldKing.UI.DevToolsPanel.CapturesInput) { requireRelease = true; return default; }
#endif
            bool padHeld = padLook.IsPressed();
            bool keyHeld = look.IsPressed() && Mouse.current != null;
            if (!isActiveAndEnabled || !focused || paused || Time.timeScale <= 0f)
            {
                requireRelease = true;
                return default;
            }
            bool held = padHeld || keyHeld;
            if (requireRelease)
            {
                if (!held) requireRelease = false;
                return default;
            }
            IsLooking = held;
            if (padHeld)
            {
                Vector2 tilt = stick.ReadValue<Vector2>();
                if (tilt.sqrMagnitude < GamepadLayout.AimDeadzone * GamepadLayout.AimDeadzone) tilt = Vector2.zero;
                return new CameraLookInputFrame { Held = true, UseStick = true, Stick = Vector2.ClampMagnitude(tilt, 1f) };
            }
            return new CameraLookInputFrame { Held = held, PointerPosition = held ? Mouse.current.position.ReadValue() : default };
        }
    }
}
