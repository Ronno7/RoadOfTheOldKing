using UnityEngine;
using UnityEngine.InputSystem;

namespace TheLostShrine.Input
{
    [DisallowMultipleComponent]
    public sealed class PlayerCombatInput : MonoBehaviour, ICombatInput
    {
        private InputActionMap actions;
        private InputAction lightChop;
        private InputAction charge;
        private InputAction throwWeapon;
        private bool focused = true;
        private bool paused;

        private void Awake()
        {
            actions = new InputActionMap("Player Combat");
            lightChop = actions.AddAction("Light Chop", InputActionType.Button, "<Mouse>/leftButton");
            charge = actions.AddAction("Charged Cleave", InputActionType.Button, "<Mouse>/rightButton");
            throwWeapon = actions.AddAction("Throw Recall", InputActionType.Button, "<Keyboard>/e");
        }

        private void OnEnable() => actions.Enable();
        private void OnDisable() => actions?.Disable();
        private void OnDestroy() => actions?.Dispose();
        private void OnApplicationFocus(bool value) => focused = value;
        private void OnApplicationPause(bool value) => paused = value;

        public CombatInputFrame Read()
        {
            var mouse = Mouse.current;
            if (!isActiveAndEnabled || !focused || paused || Time.timeScale <= 0f || mouse == null)
                return default;

            return new CombatInputFrame
            {
                Active = true,
                LightPressed = lightChop.WasPressedThisFrame(),
                ChargePressed = charge.WasPressedThisFrame(),
                ChargeHeld = charge.IsPressed(),
                ChargeReleased = charge.WasReleasedThisFrame(),
                ThrowPressed = throwWeapon.WasPressedThisFrame(),
                PointerPosition = mouse.position.ReadValue()
            };
        }
    }
}
