using UnityEngine;

namespace RoadOfTheOldKing.World
{
    /// <summary>Materials share an outline on one elevation's Tilemap.</summary>
    public sealed class TerrainRuleTile : RuleTile
    {
        public enum Part { Top, Face }
        public Part part;

        public override bool RuleMatch(int neighbor, UnityEngine.Tilemaps.TileBase other)
        {
            var terrain = other as TerrainRuleTile;
            bool joins = terrain != null && terrain.part == part;
            // Plain continuation/foot tiles are rows of the same cliff face.
            // Treating them as empty neighbors drew a seam beside taller walls.
            if (!joins && other != null && part == Part.Face)
                joins = other.name.StartsWith("CliffEarthFace_", System.StringComparison.Ordinal) ||
                        other.name.StartsWith("CliffStoneFace_", System.StringComparison.Ordinal);
            if (neighbor == TilingRuleOutput.Neighbor.This) return joins;
            if (neighbor == TilingRuleOutput.Neighbor.NotThis) return !joins;
            return base.RuleMatch(neighbor, other);
        }
    }
}
