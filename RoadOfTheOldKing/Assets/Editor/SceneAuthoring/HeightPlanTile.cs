using UnityEngine.Tilemaps;

namespace RoadOfTheOldKing.EditorTools
{
    public enum HeightPlanKind { Height, CliffFace, Transition }
    // Editor-only intent, independent of material, collision and current terrain.
    public sealed class HeightPlanTile : Tile
    {
        public int height;
        public HeightPlanKind kind;
    }
}
