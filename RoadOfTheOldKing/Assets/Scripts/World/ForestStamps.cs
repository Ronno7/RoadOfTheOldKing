using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RoadOfTheOldKing.World
{
    // A forest painted as tilemaps holds many trees per map, and one map renderer can't sit in front of one tree and
    // behind another (user, 23 Nov: "tons of incorrectly layered trees ... theres something wrong with the way trees are
    // being layered or placed in the world"). The Tutorial's woodland rows mix two base lines per map; the Lowlands'
    // forest mass split every tree between a trunk map under all props and a canopy map over them. In play each tree
    // draws as one sprite at its own ground line (the bottom of its base row, as a prop tree's), found as the shadow
    // baker finds it: its centre base slice (Oak_1_0) and that cell's placement jitter, all twelve slices present, at
    // its absolute depth (Depth), set once. A tile outside every whole tree draws on its own, at its tree's ground line
    // and slot if its centre slice is there, else at its own base row. The maps keep their tiles for painting,
    // collision and the shadow bake; only their renderers switch off.
    // Rows mode is for the camera backdrop: scenery outside play whose crowns overwrite each other, so each map sorts as
    // one unit at its lowest trunk, which keeps it behind or in front of the woodland it meets at the edges.
    [DisallowMultipleComponent]
    public sealed class ForestStamps : MonoBehaviour
    {
        [Tooltip("Whole-tree sprites from Trees16 (Oak, Birch, Apple_Tree): a tree draws with the one named like its slices.")]
        [SerializeField] private Sprite[] wholeTrees = System.Array.Empty<Sprite>();
        [Tooltip("Sort each map as one unit at its lowest trunk instead of drawing trees one by one (scenery outside play).")]
        [SerializeField] private bool rows;

        private Renderer[] art = System.Array.Empty<Renderer>();
        private float[] grounds = System.Array.Empty<float>();

        public int TreeCount { get; private set; }
        public int LooseTileCount { get; private set; }
        public int MapCount { get; private set; }

        private void Start()
        {
            var families = new Dictionary<string, Sprite>();
            foreach (var sprite in wholeTrees) if (sprite != null) families[sprite.name] = sprite;
            var maps = new List<Tilemap>();
            foreach (var map in GetComponentsInChildren<Tilemap>())
            {
                var renderer = map.GetComponent<TilemapRenderer>();
                if (renderer != null && renderer.enabled && map.GetComponent<WorldPropDepth>() == null && HasTrees(map, families))
                    maps.Add(map);
            }
            MapCount = maps.Count;
            var outArt = new List<Renderer>();
            var outGround = new List<float>();
            if (rows) BuildRows(maps, families, outArt, outGround);
            else BuildTrees(maps, families, outArt, outGround);
            art = outArt.ToArray();
            grounds = outGround.ToArray();
        }

        private static bool HasTrees(Tilemap map, Dictionary<string, Sprite> families)
        {
            foreach (var cell in map.cellBounds.allPositionsWithin)
                if (Family(map.GetSprite(cell), out string family, out _, out _) && families.ContainsKey(family)) return true;
            return false;
        }

        // "Oak_1_0" -> Oak, column 1, row 0 (row 0 is the base).
        private static bool Family(Sprite slice, out string family, out int column, out int row)
        {
            family = null; column = row = 0;
            if (slice == null) return false;
            string name = slice.name;
            int last = name.LastIndexOf('_'), before = last > 0 ? name.LastIndexOf('_', last - 1) : -1;
            if (before <= 0 || !int.TryParse(name.Substring(last + 1), out row) ||
                !int.TryParse(name.Substring(before + 1, last - before - 1), out column)) return false;
            family = name.Substring(0, before);
            return true;
        }

        // The cell's place in the world with its placement jitter: the matrix the map draws its tile with.
        private static Matrix4x4 Place(Tilemap map, Vector3Int cell) => map.transform.localToWorldMatrix *
            Matrix4x4.Translate(map.CellToLocal(cell) + map.tileAnchor) * map.GetTransformMatrix(cell);

        // The bottom of a base cell, jitter included: the ground line prop trees use.
        private static float BaseOf(Tilemap map, Vector3Int cell) => Place(map, cell).MultiplyPoint3x4(new Vector3(0f, -.5f * map.cellSize.y, 0f)).y;

        private void BuildTrees(List<Tilemap> maps, Dictionary<string, Sprite> families, List<Renderer> outArt, List<float> outGround)
        {
            // Every slice across the maps by cell: a tree's trunk and crown can sit in different maps (Lowlands).
            var slices = new Dictionary<Vector3Int, List<(Tilemap map, string name)>>();
            foreach (var map in maps)
                foreach (var cell in map.cellBounds.allPositionsWithin)
                {
                    var sprite = map.GetSprite(cell);
                    if (sprite == null) continue;
                    if (!slices.TryGetValue(cell, out var here)) slices[cell] = here = new List<(Tilemap, string)>(2);
                    here.Add((map, sprite.name));
                }
            var drawn = new HashSet<(Tilemap, Vector3Int)>();
            var bases = new Dictionary<(string, Vector3Int), float>();
            var trees = new GameObject(name + " (drawn per tree)").transform;
            trees.SetParent(transform, false);
            foreach (var pair in slices)
                foreach (var (map, sliceName) in pair.Value)
                {
                    if (!sliceName.EndsWith("_1_0", System.StringComparison.Ordinal)) continue;
                    string family = sliceName.Substring(0, sliceName.Length - 4);
                    if (!families.TryGetValue(family, out var whole)) continue;
                    float ground = BaseOf(map, pair.Key);
                    bases[(family, pair.Key)] = ground;
                    var parts = new List<(Tilemap, Vector3Int)>(12);
                    for (int column = 0; column < 3; column++)
                        for (int row = 0; row < 4; row++)
                        {
                            var cell = pair.Key + new Vector3Int(column - 1, row, 0);
                            string wanted = family + "_" + column + "_" + row;
                            if (!slices.TryGetValue(cell, out var here)) continue;
                            foreach (var (other, otherName) in here)
                                if (otherName == wanted) { parts.Add((other, cell)); break; }
                        }
                    if (parts.Count < 12) continue;
                    foreach (var part in parts) drawn.Add(part);
                    // The whole sprite placed as the baker places it: its centre base tile on this cell, jitter included.
                    var place = Place(map, pair.Key) * Matrix4x4.Translate(new Vector3(
                        (whole.pivot.x - 1.5f * whole.pixelsPerUnit * map.cellSize.x) / whole.pixelsPerUnit,
                        (whole.pivot.y - .5f * whole.pixelsPerUnit * map.cellSize.y) / whole.pixelsPerUnit));
                    var tree = Draw(trees, whole, map, pair.Key, place, ground, Place(map, pair.Key).m03);
                    outArt.Add(tree);
                    outGround.Add(ground);
                    TreeCount++;
                }
            // Anything not in a whole tree: at its tree's ground if its centre slice is there, else at its own base row.
            var loose = new GameObject(name + " (loose tiles)").transform;
            loose.SetParent(transform, false);
            foreach (var pair in slices)
                foreach (var (map, sliceName) in pair.Value)
                {
                    if (drawn.Contains((map, pair.Key))) continue;
                    var sprite = map.GetSprite(pair.Key);
                    float ground, slotX = Place(map, pair.Key).m03;
                    if (Family(sprite, out string family, out int column, out int row) &&
                        bases.TryGetValue((family, pair.Key + new Vector3Int(1 - column, -row, 0)), out float owner))
                    {
                        ground = owner;
                        slotX = Place(map, pair.Key + new Vector3Int(1 - column, -row, 0)).m03;
                    }
                    else
                    {
                        var baseCell = pair.Key;
                        while (map.HasTile(baseCell + Vector3Int.down) && !(Family(map.GetSprite(baseCell), out _, out _, out int r) && r == 0))
                            baseCell += Vector3Int.down;
                        ground = BaseOf(map, baseCell);
                    }
                    outArt.Add(Draw(loose, sprite, map, pair.Key, Place(map, pair.Key), ground, slotX));
                    outGround.Add(ground);
                    LooseTileCount++;
                }
            foreach (var map in maps) map.GetComponent<TilemapRenderer>().enabled = false;
        }

        private static SpriteRenderer Draw(Transform parent, Sprite sprite, Tilemap map, Vector3Int cell, Matrix4x4 place, float ground, float slotX)
        {
            var source = map.GetComponent<TilemapRenderer>();
            var item = new GameObject(sprite.name).transform;
            item.SetParent(parent, false);
            item.SetPositionAndRotation(new Vector3(place.m03, place.m13, 0f), place.rotation);
            item.localScale = place.lossyScale;
            var renderer = item.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = map.color * map.GetColor(cell);
            renderer.sharedMaterial = source.sharedMaterial;
            renderer.maskInteraction = source.maskInteraction;
            Depth.Apply(renderer, ground, Depth.SlotAt(slotX));
            Depth.Settle(item, slotX, map.name);
            return renderer;
        }

        private void BuildRows(List<Tilemap> maps, Dictionary<string, Sprite> families, List<Renderer> outArt, List<float> outGround)
        {
            foreach (var map in maps)
            {
                float ground = float.MaxValue;
                foreach (var cell in map.cellBounds.allPositionsWithin)
                    if (Family(map.GetSprite(cell), out string family, out int column, out int row) && column == 1 && row == 0 &&
                        families.ContainsKey(family)) ground = Mathf.Min(ground, BaseOf(map, cell));
                if (ground == float.MaxValue) ground = map.GetComponent<TilemapRenderer>().bounds.min.y;
                var renderer = map.GetComponent<TilemapRenderer>();
                Depth.Apply(renderer, ground, Depth.SlotAt(renderer.bounds.center.x));
                Depth.Settle(map.transform, renderer.bounds.center.x, map.name);
                outArt.Add(renderer);
                outGround.Add(ground);
            }
        }
    }
}
