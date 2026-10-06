using System;
using UnityEngine;

namespace RoadOfTheOldKing.Weapons
{
    // The weapon's real damage interval, also usable by lightweight world effects
    // that have no hit collider. This is not a confirmed hit or an input event.
    public readonly struct WeaponSweep
    {
        public readonly GameObject Source;
        public readonly Vector2 Origin;
        public readonly Vector2 End;
        public readonly Vector2 Aim;
        public readonly float Radius;
        public readonly float Arc;
        public readonly float LaneWidth;
        public readonly bool IsMelee;
        // Melee terrain protection. Query only during Raised: the detector reuses
        // its origin and query buffers for the next physics interval. Null in flight.
        public readonly Func<Vector2, bool> CanReach;

        public static event Action<WeaponSweep> Raised;

        internal WeaponSweep(GameObject source, Vector2 origin, Vector2 end, Vector2 aim,
            float radius, float arc, float laneWidth, bool isMelee, Func<Vector2, bool> canReach)
        {
            Source = source;
            Origin = origin;
            End = end;
            Aim = aim;
            Radius = radius;
            Arc = arc;
            LaneWidth = laneWidth;
            IsMelee = isMelee;
            CanReach = canReach;
        }

        internal static void Raise(WeaponSweep sweep) => Raised?.Invoke(sweep);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic() => Raised = null;
    }
}
