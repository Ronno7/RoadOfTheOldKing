using UnityEngine;

namespace TheLostShrine.Input
{
    // Input sources supply intent; they never move the player themselves.
    public interface IMovementInput
    {
        Vector2 MoveDirection { get; }
    }
}
