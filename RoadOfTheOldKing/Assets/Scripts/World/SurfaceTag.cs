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
        public SurfaceMaterial Material => material;
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
