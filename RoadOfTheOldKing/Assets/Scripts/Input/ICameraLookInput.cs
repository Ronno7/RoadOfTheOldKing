using UnityEngine;

namespace RoadOfTheOldKing.Input
{
    public struct CameraLookInputFrame
    {
        public bool Held;
        public Vector2 PointerPosition;
    }

    public interface ICameraLookInput
    {
        CameraLookInputFrame Read();
    }
}
