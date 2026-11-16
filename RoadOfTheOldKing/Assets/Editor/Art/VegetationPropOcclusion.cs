using System.Collections.Generic;
using System.IO;
using RoadOfTheOldKing.World;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace RoadOfTheOldKing.EditorTools
{
    // Prop art the vegetation field sorts under. The field draws on the Ground layer, so a plant rooted just in front of a
    // rock, fence or bush, whose leaves reach up into that art, is drawn behind it (user, 15 Nov: "grass that is positioned
    // below the rock is getting rendered behind it"). Both bakes leave that strip bare.
    // Counted: World-layer art, and Player-layer art that stands on the ground (an object's base row, Name_col_0, or a
    // sprite with WorldPropDepth). Overhead canopies and roofs are left out: grass under them is meant to be covered.
    internal sealed class VegetationPropOcclusion
    {
        // Opaque art rectangles, filed by the cell holding each one's lowest edge.
        private readonly Dictionary<Vector2Int, List<Rect>> bottoms = new Dictionary<Vector2Int, List<Rect>>();
        private readonly Dictionary<Sprite, Rect> opaque = new Dictionary<Sprite, Rect>();
        private readonly Dictionary<Texture2D, Texture2D> sources = new Dictionary<Texture2D, Texture2D>();

        public int Count { get; private set; }

        public static VegetationPropOcclusion Read(Scene scene)
        {
            var art = new VegetationPropOcclusion();
            int ground = SortingLayer.GetLayerValueFromName("Ground"), player = SortingLayer.GetLayerValueFromName("Player");
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var renderer in root.GetComponentsInChildren<TilemapRenderer>(false))
                    {
                        int layer = SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);
                        var map = renderer.GetComponent<Tilemap>();
                        if (!renderer.enabled || layer <= ground || map == null || map.color.a < .05f) continue;
                        bool overhead = layer >= player;
                        foreach (var cell in map.cellBounds.allPositionsWithin)
                        {
                            var sprite = map.GetSprite(cell);
                            if (sprite == null) continue;
                            bool baseRow = sprite.name.EndsWith("_0") || (!overhead && !map.HasTile(cell + Vector3Int.down));
                            if (!baseRow) continue;
                            var anchor = map.CellToLocalInterpolated(cell + (Vector3)map.tileAnchor);
                            var matrix = map.GetTransformMatrix(cell);
                            art.Add(art.Bounds(sprite, local => map.transform.TransformPoint(anchor + matrix.MultiplyPoint(local))));
                        }
                    }
                    foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(false))
                    {
                        int layer = SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);
                        if (!renderer.enabled || renderer.sprite == null || renderer.color.a < .05f || layer <= ground) continue;
                        if (layer >= player && renderer.GetComponent<WorldPropDepth>() == null) continue;
                        // Actors move; only static scenery owns ground.
                        var body = renderer.GetComponentInParent<Rigidbody2D>();
                        if (body != null && body.bodyType != RigidbodyType2D.Static) continue;
                        var t = renderer.transform;
                        float fx = renderer.flipX ? -1f : 1f, fy = renderer.flipY ? -1f : 1f;
                        art.Add(art.Bounds(renderer.sprite, local => t.TransformPoint(new Vector3(local.x * fx, local.y * fy))));
                    }
                }
            }
            finally { art.Release(); }
            return art;
        }

        // Each species' leaves at rest, relative to the root: their opaque pixels turned to their rest angles. Wind and the
        // hero only bend them for a moment, so the swept extent the planting clearance uses would bare far too much.
        public static Rect[] Rest(IList<VegetationLayout.Species> species)
        {
            var scratch = new VegetationPropOcclusion();
            try
            {
                var rests = new Rect[species?.Count ?? 0];
                for (int i = 0; i < rests.Length; i++)
                {
                    Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
                    if (species[i]?.leaves != null)
                        foreach (var leaf in species[i].leaves)
                        {
                            if (leaf?.sprite == null) continue;
                            var local = scratch.Opaque(leaf.sprite);
                            if (local.width <= 0f) continue;
                            var turn = Quaternion.Euler(0f, 0f, leaf.restAngle);
                            for (int corner = 0; corner < 4; corner++)
                            {
                                var v = new Vector3((corner & 1) == 0 ? local.xMin : local.xMax, (corner & 2) == 0 ? local.yMin : local.yMax);
                                if (leaf.flipX) v.x = -v.x;
                                Vector2 q = leaf.offset + (Vector2)(turn * v);
                                min = Vector2.Min(min, q); max = Vector2.Max(max, q);
                            }
                        }
                    rests[i] = float.IsInfinity(min.x) ? Rect.zero : Rect.MinMaxRect(min.x, min.y, max.x, max.y);
                }
                return rests;
            }
            finally { scratch.Release(); }
        }

        // True when leaves at rest over `rest` (relative to the root) would reach into art whose lowest edge is above
        // the root: the plant stands in front of it.
        public bool Covers(Vector2 root, Rect rest)
        {
            float left = root.x + rest.xMin, right = root.x + rest.xMax, top = root.y + Mathf.Max(.0625f, rest.yMax);
            float floor = root.y - .03125f;
            for (int y = Mathf.FloorToInt(floor); y <= Mathf.FloorToInt(top); y++)
                for (int x = Mathf.FloorToInt(left); x <= Mathf.FloorToInt(right); x++)
                    if (bottoms.TryGetValue(new Vector2Int(x, y), out var rects))
                        foreach (var r in rects)
                            if (r.yMin >= floor && r.yMin < top && r.xMax > left && r.xMin < right) return true;
            return false;
        }

        private void Release()
        {
            foreach (var texture in sources.Values) if (texture != null) Object.DestroyImmediate(texture);
            sources.Clear();
        }

        private void Add(Rect rect)
        {
            if (rect.width <= 0f) return;
            Count++;
            int y = Mathf.FloorToInt(rect.yMin);
            for (int x = Mathf.FloorToInt(rect.xMin); x <= Mathf.FloorToInt(rect.xMax); x++)
            {
                var key = new Vector2Int(x, y);
                if (!bottoms.TryGetValue(key, out var list)) bottoms.Add(key, list = new List<Rect>());
                list.Add(rect);
            }
        }

        // The sprite's opaque pixels, placed in the world through `place` (sprite-local units from the pivot).
        private Rect Bounds(Sprite sprite, System.Func<Vector3, Vector3> place)
        {
            var local = Opaque(sprite);
            if (local.width <= 0f) return Rect.zero;
            Vector2 min = Vector2.positiveInfinity, max = Vector2.negativeInfinity;
            for (int corner = 0; corner < 4; corner++)
            {
                Vector3 p = place(new Vector3((corner & 1) == 0 ? local.xMin : local.xMax, (corner & 2) == 0 ? local.yMin : local.yMax));
                min = Vector2.Min(min, p); max = Vector2.Max(max, p);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private Rect Opaque(Sprite sprite)
        {
            if (opaque.TryGetValue(sprite, out var cached)) return cached;
            var rect = sprite.rect;
            float ppu = sprite.pixelsPerUnit;
            var whole = new Rect(-sprite.pivot.x / ppu, -sprite.pivot.y / ppu, rect.width / ppu, rect.height / ppu);
            var source = Source(sprite.texture);
            Rect result = whole;
            if (source != null && source.width == sprite.texture.width && source.height == sprite.texture.height)
            {
                var pixels = source.GetPixels32();
                int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
                for (int y = 0; y < (int)rect.height; y++)
                    for (int x = 0; x < (int)rect.width; x++)
                        if (pixels[(int)rect.x + x + ((int)rect.y + y) * source.width].a > 0)
                        { minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y); }
                result = maxX < 0 ? Rect.zero : Rect.MinMaxRect((minX - sprite.pivot.x) / ppu, (minY - sprite.pivot.y) / ppu,
                    (maxX + 1 - sprite.pivot.x) / ppu, (maxY + 1 - sprite.pivot.y) / ppu);
            }
            opaque.Add(sprite, result);
            return result;
        }

        // Import settings can make the asset unreadable; read the file the importer reads.
        private Texture2D Source(Texture2D texture)
        {
            if (texture == null) return null;
            if (sources.TryGetValue(texture, out var loaded)) return loaded;
            string path = UnityEditor.AssetDatabase.GetAssetPath(texture);
            loaded = null;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                loaded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!loaded.LoadImage(File.ReadAllBytes(path))) { Object.DestroyImmediate(loaded); loaded = null; }
            }
            sources.Add(texture, loaded);
            return loaded;
        }
    }
}
