using System.Collections.Generic;
using RoadOfTheOldKing.Player;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RoadOfTheOldKing.World
{
    // Props use their fixed ground contact for depth against the hero: with his feet behind its ground line a prop draws
    // over him, in front of it under him. Animation height must not move the sorting boundary.
    // A tilemap of low bushes is drawn tile by tile in play (the map keeps its tiles for painting, collision and the
    // shadow bake): each tile sorts by its own bush's base row exactly like a prop, so bushes and trees keep one order
    // however the hero moves (user, 23 Nov: "some groups of bushes are still swapping between layers of trees"; one map
    // renderer can't sit in front of one tree and behind another). A groundAnchor instead fixes one line for a whole
    // map, for a prefab's overhead half (the stone bridge's rails).
    [DisallowMultipleComponent]
    public sealed class WorldPropDepth : MonoBehaviour
    {
        [SerializeField] private Transform groundAnchor;
        private Renderer body;
        private SpriteRenderer playerBody;
        private Transform player;
        private int authoredLayer, authoredOrder;
        private SpriteRenderer[] tileArt;
        private float[] tileGround;

        // The hero's art is about a tile wide and two tall from the feet.
        private const float HalfWidth = .5f, Height = 2f;
        // Orders against the hero's body: art he stands in front of goes just under him, art he is behind just over
        // him (over his carried axe and grip fold too).
        private const int UnderHero = -3, OverHero = 5;
        // Art on the same side of the hero shares an order, and the 2D renderer's orthographic sort breaks a tie by
        // distance to the camera: all at z 0, overlapping trees swapped as the hero walked (user, 23 Nov). A z from the
        // ground line keeps the nearer one in front: farther north, farther away (a 200-unit map stays within 0.2).
        private const float DepthPerUnit = .001f;

        private void Start()
        {
            body = GetComponent<Renderer>();
            var movement = FindFirstObjectByType<PlayerMovement>();
            if (body == null || movement == null) { enabled = false; return; }
            authoredLayer = body.sortingLayerID; authoredOrder = body.sortingOrder;
            player = movement.transform;
            playerBody = movement.GetComponentInChildren<SpriteRenderer>();
            var tiles = body is TilemapRenderer ? GetComponent<Tilemap>() : null;
            if (tiles != null && groundAnchor == null) SplitTiles(tiles);
        }

        private void LateUpdate()
        {
            if (body == null || player == null || playerBody == null) return;
            float feet = player.position.y;
            if (tileArt != null)
            {
                for (int i = 0; i < tileArt.Length; i++) Sort(tileArt[i], tileGround[i], feet);
                return;
            }
            float ground = groundAnchor != null ? groundAnchor.position.y : transform.position.y;
            if (body is TilemapRenderer)
            {
                // Only while the hero overlaps the overhead art; elsewhere it stays as authored.
                var art = body.bounds;
                float x = player.position.x;
                bool near = x > art.min.x - HalfWidth && x < art.max.x + HalfWidth && feet < art.max.y && feet + Height > art.min.y;
                bool behind = !near || feet > ground;
                body.sortingLayerID = behind ? authoredLayer : playerBody.sortingLayerID;
                body.sortingOrder = behind ? authoredOrder : playerBody.sortingOrder + UnderHero;
                return;
            }
            Sort(body, ground, feet);
            var position = transform.position;
            float depth = ground * DepthPerUnit;
            if (Mathf.Abs(position.z - depth) > 1e-5f) transform.position = new Vector3(position.x, position.y, depth);
        }

        private void Sort(Renderer art, float ground, float feet)
        {
            int order = playerBody.sortingOrder + (feet > ground ? OverHero : UnderHero);
            if (art.sortingLayerID != playerBody.sortingLayerID) art.sortingLayerID = playerBody.sortingLayerID;
            if (art.sortingOrder != order) art.sortingOrder = order;
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
                while (!IsBase(tiles, baseCell)) baseCell += Vector3Int.down;
                float ground = tiles.CellToWorld(baseCell).y + tiles.cellSize.y * .5f;
                var place = tiles.transform.localToWorldMatrix
                    * Matrix4x4.Translate(tiles.CellToLocal(cell) + tiles.tileAnchor) * tiles.GetTransformMatrix(cell);
                var tile = new GameObject(sprite.name).transform;
                tile.SetParent(root, false);
                tile.SetPositionAndRotation(new Vector3(place.m03, place.m13, ground * DepthPerUnit), place.rotation);
                tile.localScale = place.lossyScale;
                var renderer = tile.gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.color = tiles.color * tiles.GetColor(cell);
                renderer.sharedMaterial = map.sharedMaterial;
                renderer.maskInteraction = map.maskInteraction;
                art.Add(renderer);
                grounds.Add(ground);
            }
            map.enabled = false;
            tileArt = art.ToArray();
            tileGround = grounds.ToArray();
        }

        private static bool IsBase(Tilemap tiles, Vector3Int cell)
        {
            var sprite = tiles.GetSprite(cell);
            return sprite == null || sprite.name.EndsWith("_0") || !tiles.HasTile(cell + Vector3Int.down);
        }
    }
}
