using UnityEngine;

namespace RoadOfTheOldKing.Input
{
    public struct CameraLookInputFrame
    {
        public bool Held;
        public Vector2 PointerPosition;
        // Controller look: steer by stick tilt (magnitude 0..1) instead of the pointer.
        public bool UseStick;
        public Vector2 Stick;
    }

    public interface ICameraLookInput
    {
        CameraLookInputFrame Read();
    }
}
