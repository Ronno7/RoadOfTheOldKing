namespace RoadOfTheOldKing.Input
{
    // The fixed controller layout (user, 16 Oct): Souls-style triggers. Bindings use the generic
    // Gamepad layout, so Xbox, PlayStation and browser "standard" pads all work; ControlLabels shows
    // Xbox names. Adapters bind these paths beside their keyboard/mouse bindings, so both stay live.
    public static class GamepadLayout
    {
        public const string Move = "<Gamepad>/leftStick";
        public const string Aim = "<Gamepad>/rightStick";
        // Click to sprint; it stops when the left stick returns to rest or on a second click.
        public const string Sprint = "<Gamepad>/leftStickPress";
        public const string Dash = "<Gamepad>/buttonEast";
        public const string LightAttack = "<Gamepad>/rightShoulder";
        public const string Cleave = "<Gamepad>/rightTrigger";
        public const string ThrowRecall = "<Gamepad>/leftTrigger";
        public const string Heal = "<Gamepad>/buttonWest";
        public const string Interact = "<Gamepad>/buttonSouth";
        // Hold, then steer the view with the right stick; aim falls back to movement meanwhile.
        public const string Look = "<Gamepad>/leftShoulder";
        public const string ZoomIn = "<Gamepad>/dpad/up";
        public const string ZoomOut = "<Gamepad>/dpad/down";
        public const string Inventory = "<Gamepad>/select";

        public const string Pause = "<Gamepad>/start";
        public const string Submit = "<Gamepad>/buttonSouth";
        public const string Back = "<Gamepad>/buttonEast";
        public const string MenuUp = "<Gamepad>/dpad/up";
        public const string MenuDown = "<Gamepad>/dpad/down";
        public const string MenuLeft = "<Gamepad>/dpad/left";
        public const string MenuRight = "<Gamepad>/dpad/right";
        public const string MenuStickUp = "<Gamepad>/leftStick/up";
        public const string MenuStickDown = "<Gamepad>/leftStick/down";
        public const string MenuStickLeft = "<Gamepad>/leftStick/left";
        public const string MenuStickRight = "<Gamepad>/leftStick/right";

        // Stick tilt below this reads as rest (movement) or as no aim (right stick).
        public const float MoveDeadzone = .2f;
        public const float AimDeadzone = .35f;
        public const string MoveProcessors = "stickDeadzone(min=0.2,max=0.925)";
    }
}
