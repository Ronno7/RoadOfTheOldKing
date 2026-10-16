using UnityEngine;
using UnityEngine.Tilemaps;

namespace RoadOfTheOldKing.World
{
    /// <summary>A native upper footprint with a complete three-cell front per tile.</summary>
    public sealed class RaisedCliffTile : TileBase
    {
        // Canonical 8-neighbor mask * four horizontal texture phases.
        // The phase is spatial, so broad material planes cross cell boundaries.
        public Sprite[] sprites = new Sprite[256 * 4];
        // Retaining shores paint their crest line; the upper field is a separate
        // walkable surface. Reconstruct only the neighboring upper footprint.
        public bool crestLine;
        static readonly Vector3Int[] Neighbors = {
            new Vector3Int(0,1), new Vector3Int(1,0), new Vector3Int(0,-1), new Vector3Int(-1,0),
            new Vector3Int(1,1), new Vector3Int(1,-1), new Vector3Int(-1,-1), new Vector3Int(-1,1)
        };

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData data)
        {
            int mask = 0;
            for (int i = 0; i < Neighbors.Length; i++)
                if (Connected(position + Neighbors[i], tilemap)) mask |= 1 << i;
            for (int i = 0; i < 4; i++)
                if ((mask & (1 << i)) == 0 || (mask & (1 << ((i + 1) % 4))) == 0)
                    mask &= ~(1 << (i + 4));
            int index = mask * 4 + (position.x & 3);
            data.sprite = sprites != null && index < sprites.Length ? sprites[index] : null;
            data.color = Color.white;
            data.transform = Matrix4x4.identity;
            data.flags = TileFlags.LockColor | TileFlags.LockTransform;
            data.colliderType = Tile.ColliderType.None;
        }

        bool Connected(Vector3Int position, ITilemap tilemap)
        {
            for (int drop = 0; drop <= (crestLine ? 3 : 0); drop++)
                if (tilemap.GetTile(position + Vector3Int.down * drop) == this) return true;
            return false;
        }

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            // The disappearing tile must also refresh its surviving neighbors.
            tilemap.RefreshTile(position);
            foreach (var offset in Neighbors) tilemap.RefreshTile(position + offset);
            if (crestLine)
                for (int y = -4; y <= 4; y++) for (int x = -1; x <= 1; x++)
                    tilemap.RefreshTile(position + new Vector3Int(x, y, 0));
        }
    }
}
