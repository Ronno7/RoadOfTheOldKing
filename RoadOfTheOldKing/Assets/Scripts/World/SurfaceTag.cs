using UnityEngine;

namespace RoadOfTheOldKing.World
{
    public enum SurfaceMaterial { None, Wood, Stone, Brush, Earth, Pottery }

    // What an object is made of, for the sound and flecks when the axe strikes it. Lives on a prefab or a scene
    // object; a collider anywhere under it counts. Untagged objects stay silent. Presentation data only.
    [DisallowMultipleComponent]
    public sealed class SurfaceTag : MonoBehaviour
    {
        [SerializeField] private SurfaceMaterial material;
        [Tooltip("Cliffs and elevation edges: answer only when the struck rock rises above the striker's ground " +
            "(the scene's ElevationLevels). Swung at from on top, over the drop, they stay silent.")]
        [SerializeField] private bool onlyWhenRisingAbove;
        public SurfaceMaterial Material => material;

        // Whether a blow from someone standing at `from` that met this surface at `point` should sound.
        public bool Answers(Vector2 from, Vector2 point, Vector2 direction)
        {
            if (!onlyWhenRisingAbove) return true;
            Vector2 inside = point + (direction.sqrMagnitude > .0001f ? direction.normalized : Vector2.zero) * .1f;
            if (!ElevationLevels.TryGround(from, out int ground) || !ElevationLevels.TryStruck(inside, out int rock))
                return true;
            return rock > ground;
        }
    }

    public readonly struct SurfaceStrike
    {
        public readonly SurfaceMaterial Material;
        public readonly Vector2 Point, Direction;
        public readonly bool Broke;

        public SurfaceStrike(SurfaceMaterial material, Vector2 point, Vector2 direction, bool broke)
        {
            Material = material; Point = point; Direction = direction; Broke = broke;
        }
    }
}
