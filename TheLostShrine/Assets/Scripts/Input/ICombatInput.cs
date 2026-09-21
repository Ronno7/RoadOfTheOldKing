using UnityEngine;

namespace TheLostShrine.Input
{
    public struct CombatInputFrame
    {
        public bool Active;
        public bool LightPressed;
        public bool ChargePressed;
        public bool ChargeHeld;
        public bool ChargeReleased;
        public bool ThrowPressed;
        public Vector2 PointerPosition;
    }

    public interface ICombatInput { CombatInputFrame Read(); }
}
