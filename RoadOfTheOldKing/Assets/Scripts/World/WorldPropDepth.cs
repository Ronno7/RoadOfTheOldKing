using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RoadOfTheOldKing.World
{
    // A prop's place in depth (Depth): on the Standing layer at its ground line, so it draws in front of everything
    // standing farther north and behind everything farther south, the hero included, wherever he is (user, 23 Nov: "go
    // with codex's cleaner long term approach"; the hero-relative orders before it left every tie to the renderer). The
    // ground line is its pivot or groundAnchor; the slot comes from its x. Set once, and again only if the prop moves.
    // A tilemap of low bushes is drawn tile by tile in play (the map keeps its tiles for painting, collision and the
    // shadow bake): each tile at its own bush's base row, so bushes and trees keep one order. A groundAnchor instead fixes
    // one line for a whole map: a prefab's overhead half (the stone bridge's near rail).
    [DisallowMultipleComponent, DefaultExecutionOrder(900)]
    public sealed class WorldPropDepth : MonoBehaviour
    {
        [SerializeField] private Transform groundAnchor;
        [Tooltip("Each tile stands on its own row (a low wall, seen from above, is one tile deep everywhere); otherwise a " +
            "tile takes its bush's base row.")]
        [SerializeField] private bool tilesStandAlone;
        private Renderer body;
        private SpriteRenderer[] tileArt;
        private float[] tileGround;

        private void Start()
        {
            body = GetComponent<Renderer>();
            if (body == null) { enabled = false; return; }
            var tiles = body is TilemapRenderer ? GetComponent<Tilemap>() : null;
            if (tiles != null && groundAnchor == null) { SplitTiles(tiles); enabled = false; return; }
            Place();
        }

        // Only a prop that moved (a recoil, a sinking gate leaf) is placed again.
        private void LateUpdate()
        {
            if (!transform.hasChanged && (groundAnchor == null || !groundAnchor.hasChanged)) return;
            Place();
        }

        private void Place()
        {
            transform.hasChanged = false;
            if (groundAnchor != null) groundAnchor.hasChanged = false;
            float ground = groundAnchor != null ? groundAnchor.position.y : transform.position.y;
            float x = body is TilemapRenderer ? body.bounds.center.x : (groundAnchor != null ? groundAnchor : transform).position.x;
            Depth.Apply(body, ground, Depth.SlotAt(x));
            // Props' art objects are mostly named Art: tell them apart by the prop and its sprite.
            string key = (transform.parent != null ? transform.parent.name : name) +
                (body is SpriteRenderer art && art.sprite != null ? art.sprite.name : "");
            Depth.Settle(transform, x, key);
        }

        // One sprite per tile, placed and drawn as the map draws it, at the depth of its ground line: the middle of its
        // bush's base row (the tile whose sprite is an object's bottom row, Name_col_0, or that has nothing under it),
        // where its collision sits.
        private void SplitTiles(Tilemap tiles)
        {
            var map = (TilemapRenderer)body;
            var root = new GameObject(name + " (drawn per tile)").transform;
            root.SetParent(transform, false);
            var art = new List<SpriteRenderer>();
            var grounds = new List<float>();
            foreach (var cell in tiles.cellBounds.allPositionsWithin)
            {
                var sprite = tiles.GetSprite(cell);
                if (sprite == null) continue;
                var baseCell = cell;
                while (!tilesStandAlone && !IsBase(tiles, baseCell)) baseCell += Vector3Int.down;
                float ground = tiles.CellToWorld(baseCell).y + tiles.cellSize.y * .5f;
                var place = tiles.transform.localToWorldMatrix
                    * Matrix4x4.Translate(tiles.CellToLocal(cell) + tiles.tileAnchor) * tiles.GetTransformMatrix(cell);
                var tile = new GameObject(sprite.name).transform;
                tile.SetParent(root, false);
                tile.SetPositionAndRotation(new Vector3(place.m03, place.m13, 0f), place.rotation);
                tile.localScale = place.lossyScale;
                var renderer = tile.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = tiles.color * tiles.GetColor(cell);
                renderer.sharedMaterial = map.sharedMaterial;
                renderer.maskInteraction = map.maskInteraction;
                // The slot follows the bush's centre column (Name_col_row), not the tile, so one bush's tiles never split
                // around something else on their row.
                int centre = tilesStandAlone ? cell.x : cell.x + 1 - Column(sprite, 1);
                float x = tiles.CellToWorld(new Vector3Int(centre, baseCell.y, 0)).x + tiles.cellSize.x * .5f;
                Depth.Apply(renderer, ground, Depth.SlotAt(x));
                Depth.Settle(tile, x, name);
                art.Add(renderer);
                grounds.Add(ground);
            }
            map.enabled = false;
            tileArt = art.ToArray();
            tileGround = grounds.ToArray();
        }

        // "Hedgerow_2_0" -> 2; a name without a column gives the fallback.
        private static int Column(Sprite sprite, int fallback)
        {
            string n = sprite.name;
            int last = n.LastIndexOf('_'), before = last > 0 ? n.LastIndexOf('_', last - 1) : -1;
            return before >= 0 && int.TryParse(n.Substring(before + 1, last - before - 1), out int column) ? column : fallback;
        }

        private static bool IsBase(Tilemap tiles, Vector3Int cell)
        {
            var sprite = tiles.GetSprite(cell);
            return sprite == null || sprite.name.EndsWith("_0") || !tiles.HasTile(cell + Vector3Int.down);
        }
    }
}
