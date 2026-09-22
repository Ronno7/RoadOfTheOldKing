using UnityEngine;

namespace TheLostShrine.Input
{
    // Input sources supply intent; they never move the player themselves.
    public interface IMovementInput
    {
        // World-space intent: a unit direction when moving, or zero when idle.
        Vector2 MoveDirection { get; }
        bool SprintHeld { get; }
        bool IsActive { get; }
        bool ConsumeDashPress();
    }
}
