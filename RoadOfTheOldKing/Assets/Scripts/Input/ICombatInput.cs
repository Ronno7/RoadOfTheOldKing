using UnityEngine;

namespace RoadOfTheOldKing.Input
{
    // Pointer: aim from the player toward PointerPosition (screen pixels; the default, so mouse and
    // preview drivers need set nothing else). Direction: aim along AimDirection (world space, from a
    // stick or movement). Hold: keep the current aim.
    public enum AimSource { Pointer, Direction, Hold }

    public struct CombatInputFrame
    {
        public bool Active;
        public bool LightPressed;
        public bool ChargePressed;
        public bool ChargeHeld;
        public bool ChargeReleased;
        public bool ThrowPressed;
        public bool ThrowHeld;
        public bool ThrowReleased;
        public bool HealPressed;
        public AimSource Aim;
        public Vector2 PointerPosition;
        public Vector2 AimDirection;
    }

    public interface ICombatInput { CombatInputFrame Read(); }
}
