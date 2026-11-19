using UnityEngine;

namespace RoadOfTheOldKing.Input
{
    // Input sources supply intent; they never move the player themselves.
    public interface IMovementInput
    {
        // World-space intent: a unit direction when moving, or zero when idle.
        Vector2 MoveDirection { get; }
        // How much of walking speed the input asks for, 0..1: always 1 on keys, the stick's tilt on a controller.
        float MoveAmount { get; }
        bool SprintHeld { get; }
        bool IsActive { get; }
        bool ConsumeDashPress();
    }
}
