using UnityEngine;

namespace TheLostShrine.Input
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
