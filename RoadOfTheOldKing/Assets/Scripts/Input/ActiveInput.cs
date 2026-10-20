using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RoadOfTheOldKing.Input
{
    // Which device family the player last used. Prompts, hints and aim follow it; both families stay
    // bound at all times. Deliberate actuation switches it (a button, a stick past half tilt, a key,
    // a click, a scroll or a clear mouse movement), so stick drift or a bumped desk does not flip
    // labels back and forth. The OS cursor hides while the controller is in use.
    public static class ActiveInput
    {
        private const float StickThreshold = .5f;
        private const float MouseMoveThreshold = 6f;
        private static bool subscribed;

        public static bool UsingGamepad { get; private set; }
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            if (subscribed) InputSystem.onAfterUpdate -= Poll;
            subscribed = false;
            UsingGamepad = false;
            Changed = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (subscribed) return;
            InputSystem.onAfterUpdate += Poll;
            subscribed = true;
        }

        // Explicit switch for verification and future settings; normal play detects use automatically.
        public static void Set(bool gamepad)
        {
            if (UsingGamepad == gamepad) return;
            UsingGamepad = gamepad;
            Cursor.visible = !gamepad;
            Changed?.Invoke();
        }

        private static void Poll()
        {
            if ((InputState.currentUpdateType & InputUpdateType.Editor) != 0) return;
            if (!UsingGamepad) { if (GamepadUsed()) Set(true); }
            else if (KeyboardOrMouseUsed()) Set(false);
        }

        private static bool GamepadUsed()
        {
            var pad = Gamepad.current;
            if (pad == null || !pad.wasUpdatedThisFrame) return false;
            return pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame ||
                pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame ||
                pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame ||
                pad.leftTrigger.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame ||
                pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame ||
                pad.leftStickButton.wasPressedThisFrame || pad.rightStickButton.wasPressedThisFrame ||
                pad.dpad.ReadValue().sqrMagnitude > 0f ||
                pad.leftStick.ReadValue().sqrMagnitude > StickThreshold * StickThreshold ||
                pad.rightStick.ReadValue().sqrMagnitude > StickThreshold * StickThreshold;
        }

        private static bool KeyboardOrMouseUsed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.wasUpdatedThisFrame && keyboard.anyKey.wasPressedThisFrame) return true;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.wasUpdatedThisFrame) return false;
            return mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame ||
                mouse.middleButton.wasPressedThisFrame || mouse.scroll.ReadValue().sqrMagnitude > 0f ||
                mouse.delta.ReadValue().sqrMagnitude > MouseMoveThreshold * MouseMoveThreshold;
        }
    }
}
