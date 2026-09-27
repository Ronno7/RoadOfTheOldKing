using System;
using UnityEngine;

namespace TheLostShrine.Player
{
    public enum AimGait { Forward, Backward, Left, Right }

    // Presentation only: direction selection and one stride phase shared by all aim gaits.
    // Actual displacement advances feet; elapsed time and requested velocity never do.
    public sealed class AimStrideClock
    {
        private const float RetainSectorDot = .6427876f; // 50 degrees: 5 beyond a cardinal boundary.
        private bool hasFacing, hasGait;
        private double phase;
        public Vector2 Facing { get; private set; } = Vector2.right;
        public AimGait Gait { get; private set; }
        public float Phase => (float)phase;

        public void Reset()
        {
            hasFacing = hasGait = false;
            phase = 0;
            Facing = Vector2.right;
            Gait = AimGait.Forward;
        }

        public void Advance(Vector2 aim, Vector2 displacement, float strideLength, bool teleported = false)
        {
            if (!Finite(aim) || !Finite(displacement) || float.IsNaN(strideLength) ||
                float.IsInfinity(strideLength) || strideLength <= 0)
                throw new ArgumentException("Aim stride requires finite vectors and positive stride length.");
            if (aim.sqrMagnitude < .000001f) aim = Facing;
            aim.Normalize();
            if (!hasFacing || Vector2.Dot(Facing, aim) < RetainSectorDot)
                Facing = Cardinal(aim);
            hasFacing = true;
            if (teleported || displacement.sqrMagnitude <= .00000001f) return;

            Vector2 travel = displacement.normalized;
            Vector2 left = new Vector2(-aim.y, aim.x);
            Vector2 previous = Gait == AimGait.Forward ? aim : Gait == AimGait.Backward ? -aim
                : Gait == AimGait.Left ? left : -left;
            if (!hasGait || Vector2.Dot(previous, travel) < RetainSectorDot)
            {
                float forward = Vector2.Dot(aim, travel), sideways = Vector2.Dot(left, travel);
                Gait = Mathf.Abs(forward) >= Mathf.Abs(sideways)
                    ? (forward >= 0 ? AimGait.Forward : AimGait.Backward)
                    : (sideways >= 0 ? AimGait.Left : AimGait.Right);
            }
            hasGait = true;
            phase = (phase + displacement.magnitude / (double)strideLength) % 1.0;
        }

        public int SampleCel(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            return (int)Math.Floor((phase + .0000001) * count) % count;
        }

        private static Vector2 Cardinal(Vector2 direction) => Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
            ? (direction.x >= 0 ? Vector2.right : Vector2.left) : (direction.y >= 0 ? Vector2.up : Vector2.down);
        private static bool Finite(Vector2 v) => !float.IsNaN(v.x) && !float.IsInfinity(v.x) &&
            !float.IsNaN(v.y) && !float.IsInfinity(v.y);
    }
}
