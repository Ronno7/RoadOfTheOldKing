using UnityEngine;
using UnityEngine.InputSystem;

namespace RoadOfTheOldKing.Input
{
    [DisallowMultipleComponent]
    public sealed class PlayerCombatInput : MonoBehaviour, ICombatInput
    {
        [Tooltip("Controller: after the right stick returns to rest, keep its last aim this long before aim follows movement, so a flick followed by an attack still goes where it pointed.")]
        [SerializeField, Min(0f)] private float stickAimHold = .35f;
        private InputActionMap actions;
        private InputAction lightChop;
        private InputAction charge;
        private InputAction throwWeapon;
        private InputAction heal;
        private InputAction aimStick;
        private InputAction lookHold;
        private bool focused = true;
        private RoadOfTheOldKing.Player.PlayerControlLocks locks;
        private IMovementInput movement;
        private bool paused;
        private float stickReleasedAt = float.NegativeInfinity;

        private void Awake()
        {
            locks = GetComponent<RoadOfTheOldKing.Player.PlayerControlLocks>();
            movement = GetComponent<IMovementInput>();
            actions = new InputActionMap("Player Combat");
            lightChop = actions.AddAction("Light Chop", InputActionType.Button, "<Mouse>/leftButton");
            lightChop.AddBinding(GamepadLayout.LightAttack);
            charge = actions.AddAction("Charged Cleave", InputActionType.Button, "<Mouse>/rightButton");
            charge.AddBinding(GamepadLayout.Cleave);
            throwWeapon = actions.AddAction("Throw Recall", InputActionType.Button, "<Keyboard>/e");
            throwWeapon.AddBinding(GamepadLayout.ThrowRecall);
            heal = actions.AddAction("Heal", InputActionType.Button, "<Keyboard>/q");
            heal.AddBinding(GamepadLayout.Heal);
            aimStick = actions.AddAction("Aim", InputActionType.Value, GamepadLayout.Aim, expectedControlLayout: "Vector2");
            // While the look button steers the camera, the right stick is not aiming.
            lookHold = actions.AddAction("Look Hold", InputActionType.Button, GamepadLayout.Look);
        }

        private void OnEnable() => actions.Enable();
        private void OnDisable() => actions?.Disable();
        private void OnDestroy() => actions?.Dispose();
        private void OnApplicationFocus(bool value) => focused = value;
        private void OnApplicationPause(bool value) => paused = value;

        public CombatInputFrame Read()
        {
            if (!isActiveAndEnabled || !focused || paused || Time.timeScale <= 0f || (locks != null && locks.IsLocked))
                return default;

            var frame = new CombatInputFrame
            {
                Active = true,
                LightPressed = lightChop.WasPressedThisFrame(),
                ChargePressed = charge.WasPressedThisFrame(),
                ChargeHeld = charge.IsPressed(),
                ChargeReleased = charge.WasReleasedThisFrame(),
                ThrowPressed = throwWeapon.WasPressedThisFrame(),
                ThrowHeld = throwWeapon.IsPressed(),
                ThrowReleased = throwWeapon.WasReleasedThisFrame(),
                HealPressed = heal.WasPressedThisFrame()
            };
            if (ActiveInput.UsingGamepad) ReadStickAim(ref frame);
            else if (Mouse.current != null) frame.PointerPosition = Mouse.current.position.ReadValue();
            else frame.Aim = AimSource.Hold;
            return frame;
        }

        // Right stick aims; at rest, aim follows the left stick (after a short hold); standing still keeps it.
        private void ReadStickAim(ref CombatInputFrame frame)
        {
            Vector2 stick = lookHold.IsPressed() ? Vector2.zero : aimStick.ReadValue<Vector2>();
            if (stick.sqrMagnitude >= GamepadLayout.AimDeadzone * GamepadLayout.AimDeadzone)
            {
                frame.Aim = AimSource.Direction;
                frame.AimDirection = stick.normalized;
                stickReleasedAt = Time.time;
                return;
            }
            Vector2 moving = movement != null ? movement.MoveDirection : Vector2.zero;
            bool holding = Time.time - stickReleasedAt < stickAimHold;
            frame.Aim = !holding && moving.sqrMagnitude > 0.01f ? AimSource.Direction : AimSource.Hold;
            frame.AimDirection = moving;
        }
    }
}
