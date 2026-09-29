using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLostShrine.Input
{
    // Input adapter only: camera policy and movement live in the camera components.
    [DisallowMultipleComponent]
    public sealed class CameraLookInput : MonoBehaviour, ICameraLookInput
    {
        private InputAction look;
        private bool focused = true;
        private bool paused;
        private bool requireRelease;

        private void Awake() => look = new InputAction("Freelook", InputActionType.Button, "<Keyboard>/leftAlt");
        private void OnEnable() => look.Enable();
        private void OnDisable() { look?.Disable(); requireRelease = true; }
        private void OnDestroy() => look?.Dispose();
        private void OnApplicationFocus(bool value) { focused = value; if (!value) requireRelease = true; }
        private void OnApplicationPause(bool value) { paused = value; if (value) requireRelease = true; }

        public CameraLookInputFrame Read()
        {
            if (!isActiveAndEnabled || !focused || paused || Time.timeScale <= 0f || Mouse.current == null)
            {
                requireRelease = true;
                return default;
            }
            bool held = look.IsPressed();
            if (requireRelease)
            {
                if (!held) requireRelease = false;
                return default;
            }
            return new CameraLookInputFrame { Held = held, PointerPosition = Mouse.current.position.ReadValue() };
        }
    }
}
