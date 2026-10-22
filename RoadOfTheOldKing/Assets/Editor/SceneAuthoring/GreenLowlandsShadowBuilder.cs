using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoadOfTheOldKing.Combat;
using RoadOfTheOldKing.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace RoadOfTheOldKing.EditorTools
{
    // Shadows are native-pixel scene data, not replacement art or a realtime lighting system.
    // Rebuild after moving scenery/terrain. Static overlaps use maximum alpha, never multiplication.
    public static class GreenLowlandsShadowBuilder
    {
        public const string AssetFolder = "Assets/Art/Generated/GreenLowlands/Shadows";
        public const string RootName = "Ground shadows - baked";
        const int Ppu = 16, Chunk = 512;
        const byte Cast = 42, Contact = 64;

        // These are authored layers, not a generated height system. The same
        // contract also lets vegetation distinguish a walkable top from a wall.
        internal static bool TryTerrainLayer(Tilemap map, out int height, out bool top)
        {
            top = map.name.EndsWith(" tops", StringComparison.Ordinal);
            height = 0;
            switch (map.name)
            {
                case "Terrain - Farm tops": case "Terrain - Farm faces": return true;
                case "Terrain - Ridge tops": case "Terrain - Ridge faces": height = 1; return true;
                case "Terrain - Enclosure tops": case "Terrain - Enclosure faces": height = 2; return true;
                default: return false;
            }
        }


        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/GreenLowlands.unity")
                throw new InvalidOperationException("Open GreenLowlands in stopped Edit mode.");
            // Do not save or reload: an artist's unsaved placement is the input to this bake.
            using (var bake = new Bake(scene)) bake.Run();
            EditorSceneManager.MarkSceneDirty(scene);
        }


        public static void BuildTutorial()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/Tutorial.unity")
                throw new InvalidOperationException("Open Tutorial in stopped Edit mode.");
            using (var bake = new Bake(scene, true)) bake.Run();
            EditorSceneManager.MarkSceneDirty(scene);
        }


        public static void BuildBarrow()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/BarrowApproach.unity")
                throw new InvalidOperationException("Open BarrowApproach in stopped Edit mode.");
            using (var bake = new Bake(scene)) bake.Run();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        sealed class Bake : IDisposable
        {
            readonly Scene scene;
            readonly bool tutorial;
            readonly bool barrow;
            readonly string assetFolder;
            readonly Tilemap[] maps;
            readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
            readonly Dictionary<Sprite, Color32[]> pixels = new Dictionary<Sprite, Color32[]>();
            readonly HashSet<Vector3Int> ground = new HashSet<Vector3Int>(), upper = new HashSet<Vector3Int>(),
                basin = new HashSet<Vector3Int>(), water = new HashSet<Vector3Int>(), faces = new HashSet<Vector3Int>(),
                volume = new HashSet<Vector3Int>();
            readonly Dictionary<Vector3Int, Sprite> raised = new Dictionary<Vector3Int, Sprite>();
            readonly Dictionary<Vector3Int, Tilemap> cliffTops = new Dictionary<Vector3Int, Tilemap>();
            readonly Dictionary<Vector3Int, int> topHeights = new Dictionary<Vector3Int, int>(),
                floorHeights = new Dictionary<Vector3Int, int>();
            readonly Dictionary<Tilemap, int> faceHeights = new Dictionary<Tilemap, int>();
            readonly Dictionary<Vector3Int, Sprite> capReference = new Dictionary<Vector3Int, Sprite>();
            readonly HashSet<Vector2Int> ramps = new HashSet<Vector2Int>(), upperPaint = new HashSet<Vector2Int>();
            readonly Dictionary<Vector3Int, List<Tilemap>> manual = new Dictionary<Vector3Int, List<Tilemap>>();
            readonly Dictionary<Vector2Int, byte> dryMask = new Dictionary<Vector2Int, byte>(), wetMask = new Dictionary<Vector2Int, byte>();
            readonly List<object> sources = new List<object>();
            readonly List<string> files = new List<string>();
            Transform output;
            int rejected, staticSources, potSources, fittingSources;

            public Bake(Scene scene, bool tutorial = false)
            {
                this.scene = scene;
                this.tutorial = tutorial;
                barrow = scene.path == "Assets/Scenes/BarrowApproach.unity";
                assetFolder = tutorial ? "Assets/Art/Generated/Tutorial/Shadows" : barrow ? "Assets/Art/Generated/BarrowApproach/Shadows" : AssetFolder;
                maps = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Tilemap>(true)).ToArray();
                ReadSurfaces();
            }

            static string PathOf(Transform t) => AnimationUtility.CalculateTransformPath(t, null);
            // Region groups are found by name anywhere under the region root, so the hierarchy can be reorganized.
            static Transform Deep(Transform root, string name) =>
                root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);
            static Transform Scenery(Transform world)
            {
                var scenery = world.Find("Scenery");
                if (scenery != null) return scenery;
                var go = new GameObject("Scenery"); Undo.RegisterCreatedObjectUndo(go, "Create Scenery group");
                go.transform.SetParent(world, false);
                return go.transform;
            }
            static Vector3Int Cell(Vector2Int p) => new Vector3Int(Mathf.FloorToInt(p.x / 16f), Mathf.FloorToInt(p.y / 16f), 0);
            static Vector2Int Pixel(Vector3 p) => new Vector2Int(Mathf.FloorToInt(p.x * Ppu), Mathf.FloorToInt(p.y * Ppu));
            static void Max(Dictionary<Vector2Int, byte> mask, Vector2Int p, byte alpha)
            {
                if (!mask.TryGetValue(p, out byte old) || old < alpha) mask[p] = alpha;
            }

            void ReadSurfaces()
            {
                var shallow = tutorial ? AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Tiles/Tutorial/Terrain/TutorialCliffs16.png")
                    .OfType<Sprite>().ToDictionary(s => s.name) : null;
                if (!tutorial && !barrow)
                    foreach (var guide in maps.Where(m => m.name == LowlandsElevationAuthoring.ElevationName))
                        foreach (var c in guide.cellBounds.allPositionsWithin)
                        {
                            int height;
                            if (!LowlandsElevationAuthoring.TryHeight(LowlandsElevationAuthoring.ReadClassification(guide, c), out height)) continue;
                            var w = Vector3Int.FloorToInt(guide.CellToWorld(c) + Vector3.one * .01f); w.z = 0;
                            floorHeights[w] = height;
                        }
                foreach (var map in maps)
                {
                    string n = map.name;
                    int layerHeight = 0; bool layerTop = false;
                    bool nativeLayer = !tutorial && !barrow && TryTerrainLayer(map, out layerHeight, out layerTop);
                    // Barrow's uplands are one terrain map holding both grassy tops and cliff faces; its tops receive
                    // shadows (sampled per pixel by colour in Receiver), its faces never do.
                    if (barrow && n == "Terrain - boundary uplands") { nativeLayer = true; layerTop = true; layerHeight = 1; }
                    bool isGround = n != "Meadow Backdrop" && (n.StartsWith("Ground -") || n == "Border grass" || n == "Border lane" ||
                        PathOf(map.transform).Contains("/Arrival/Ground/") || n.StartsWith("Paths -"));
                    bool isUpper = n == "Terrain - upper meadow terraces";
                    bool isBasin = n == "Ground - exposed basin silt";
                    bool isWater = n == "Terrain - permanent water" || n == "Border river" || n == "Sluice - lower tailrace water";
                    bool isRaised = false;
                    bool isCliffTop = nativeLayer && layerTop;
                    bool isFace = n == "Sluice - basin stair masonry" ||
                        n == "Terrain - structural bank edges" || n == "Terrain - workshop masonry" ||
                        n == "Terrain - shard island masonry" || n == "Storehouse - foundation" || nativeLayer && !layerTop;
                    if (nativeLayer && !layerTop) faceHeights[map] = layerHeight;
                    bool submergedAdapters = n == "Terrain - submerged height connectors";
                    if (tutorial)
                    {
                        string path = PathOf(map.transform);
                        isGround = path.StartsWith("Tutorial Zone/Ground/") || path.StartsWith("Tutorial Zone/Paths/");
                        isUpper = isBasin = false;
                        isWater = path.StartsWith("Tutorial Zone/Terrain/") && (n == "MeadowWater" || n == "StoneWater" || n == "DeepWater");
                        isRaised = path == "Tutorial Zone/Terrain/GrassLedge" || path == "Tutorial Zone/Terrain/StoneLedge";
                        isFace = path == "Tutorial Zone/Terrain/StoneWall";
                        if (path.Contains("/Raised Surfaces/") || n == "Stairs and Ramps")
                        {
                            var mask = path.Contains("/Raised Surfaces/") ? upperPaint : ramps;
                            foreach (var c in map.cellBounds.allPositionsWithin)
                                if (map.HasTile(c)) AddSprite(mask, map.GetSprite(c), map.transform.localToWorldMatrix *
                                    Matrix4x4.Translate(map.CellToLocal(c) + map.tileAnchor) * map.GetTransformMatrix(c));
                        }
                    }
                    if (!isGround && !isUpper && !isBasin && !isWater && !isRaised && !isFace && !isCliffTop && !submergedAdapters) continue;
                    foreach (var c in map.cellBounds.allPositionsWithin)
                    {
                        if (!map.HasTile(c)) continue;
                        var w = Vector3Int.FloorToInt(map.CellToWorld(c) + Vector3.one * .01f); w.z = 0;
                        if (isGround) ground.Add(w);
                        if (isUpper) upper.Add(w);
                        if (isBasin) basin.Add(w);
                        if (isWater) water.Add(w);
                        if (isCliffTop)
                        {
                            int previous;
                            if (!topHeights.TryGetValue(w, out previous) || layerHeight > previous)
                            { cliffTops[w] = map; topHeights[w] = layerHeight; }
                            volume.Add(w);
                        }
                        if (isRaised)
                        {
                            var sourceSprite = map.GetSprite(c);
                            raised[w] = sourceSprite;
                            int depth = (int)sourceSprite.rect.height / Ppu - 1;
                            for (int y = 0; y <= depth; y++) volume.Add(w + Vector3Int.down * y);
                            // The bare-stone cap shares face colors. Its matching earth topology
                            // supplies the unambiguous top-surface mask, at the same top-cell pivot.
                            if (tutorial && n == "StoneLedge")
                                capReference[w] = shallow[raised[w].name.Replace("RaisedStone_", "ShallowEarth_")];
                        }
                        if (isFace || submergedAdapters &&
                            (map.GetTile(c).name.StartsWith("ShoreInside_", StringComparison.Ordinal) ||
                             map.GetTile(c).name.StartsWith("ShoreReturn_", StringComparison.Ordinal)))
                        {
                            faces.Add(w); volume.Add(w);
                            if (!manual.TryGetValue(w, out var list)) manual[w] = list = new List<Tilemap>();
                            list.Add(map);
                        }
                    }
                }
            }

            Color32[] Read(Sprite sprite)
            {
                if (pixels.TryGetValue(sprite, out var data)) return data;
                string path = AssetDatabase.GetAssetPath(sprite.texture);
                if (!textures.TryGetValue(path, out var texture))
                {
                    texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!texture.LoadImage(File.ReadAllBytes(path))) throw new InvalidDataException(path);
                    textures[path] = texture;
                }
                Rect r = sprite.rect;
                data = texture.GetPixels(Mathf.RoundToInt(r.x), Mathf.RoundToInt(r.y), (int)r.width, (int)r.height)
                    .Select(c => (Color32)c).ToArray();
                pixels[sprite] = data;
                return data;
            }

            Color32 Sample(Sprite sprite, float x, float y)
            {
                int ix = Mathf.FloorToInt(x * sprite.pixelsPerUnit + sprite.pivot.x);
                int iy = Mathf.FloorToInt(y * sprite.pixelsPerUnit + sprite.pivot.y);
                int w = (int)sprite.rect.width, h = (int)sprite.rect.height;
                return ix < 0 || iy < 0 || ix >= w || iy >= h ? new Color32() : Read(sprite)[iy * w + ix];
            }

            bool CliffSolid(Vector2Int p)
            {
                Vector3Int c = Cell(p);
                // Lowlands cliffs occupy explicit cells. Interior top sprites are
                // transparent so Ground/Paths remain visible, but the top is solid.
                if (cliffTops.ContainsKey(c)) return true;
                for (int y = 0; y <= 6; y++)
                {
                    var top = c + Vector3Int.up * y;
                    if (raised.TryGetValue(top, out var sprite) && Sample(sprite,
                        (p.x + .5f) / 16 - top.x - .5f, (p.y + .5f) / 16 - top.y - .5f).a > 127) return true;
                }
                if (manual.TryGetValue(c, out var layers))
                    foreach (var map in layers)
                    {
                        var cell = map.WorldToCell(new Vector3((p.x + .5f) / 16, (p.y + .5f) / 16));
                        var sprite = map.GetSprite(cell);
                        if (sprite == null) continue;
                        Vector3 anchor = map.CellToWorld(cell) + map.tileAnchor;
                        if (Sample(sprite, (p.x + .5f) / 16 - anchor.x, (p.y + .5f) / 16 - anchor.y).a > 127) return true;
                    }
                return false;
            }

            bool MapSolid(Tilemap map, Vector2Int p)
            {
                var point = new Vector3((p.x + .5f) / 16, (p.y + .5f) / 16);
                var cell = map.WorldToCell(point);
                var sprite = map.GetSprite(cell);
                if (sprite == null) return false;
                Vector3 anchor = map.CellToWorld(cell) + map.tileAnchor;
                return Sample(sprite, point.x - anchor.x, point.y - anchor.y).a > 127;
            }

            int FaceHeight(Vector2Int p)
            {
                int height = int.MinValue;
                List<Tilemap> layers;
                if (manual.TryGetValue(Cell(p), out layers))
                    foreach (var map in layers)
                    {
                        int tier;
                        if (faceHeights.TryGetValue(map, out tier) && tier > height && MapSolid(map, p)) height = tier;
                    }
                return height;
            }

            static int ReceiverForHeight(int height) { return height == -1 ? 3 : height == 0 ? 1 : height == 1 ? 2 : 4; }
            static int HeightForReceiver(int receiver) { return receiver == 3 ? -1 : receiver == 1 ? 0 : receiver == 2 ? 1 : 2; }

            int LowlandsReceiver(Vector2Int p)
            {
                var c = Cell(p);
                int top;
                bool hasTop = topHeights.TryGetValue(c, out top);
                // A higher wall covers the lower surface's deliberately filled footprint.
                int face = FaceHeight(p);
                if (face != int.MinValue && (!hasTop || face > top)) return 0;
                if (hasTop) return ReceiverForHeight(top);
                if (volume.Contains(c) && CliffSolid(p) || water.Contains(c)) return 0;
                int floor;
                if (floorHeights.TryGetValue(c, out floor)) return ReceiverForHeight(floor);
                if (basin.Contains(c)) return 3;
                return ground.Contains(c) ? 1 : 0;
            }

            // 0 = no receiver, 1 = Farm, 2 = Ridge, 3 = Basin, 4 = Enclosure.
            int Receiver(Vector2Int p)
            {
                if (!tutorial && !barrow) return LowlandsReceiver(p);
                if (tutorial && ramps.Contains(p)) return 0;
                if (tutorial && water.Contains(Cell(p))) return 0;
                if (tutorial && upperPaint.Contains(p)) return 2;
                var c = Cell(p);
                if (cliffTops.TryGetValue(c, out var topMap))
                {
                    var topSprite = topMap.GetSprite(topMap.WorldToCell(new Vector3((p.x+.5f)/16, (p.y+.5f)/16)));
                    var color = Sample(topSprite, (p.x+.5f)/16-c.x-.5f, (p.y+.5f)/16-c.y-.5f);
                    int rgb = color.r << 16 | color.g << 8 | color.b;
                    return color.a == 0 || rgb == 0x709276 || rgb == 0x7f9e7e || rgb == 0x52745a || rgb == 0x344e40 ? 2 : 0;
                }
                if (raised.TryGetValue(c, out var sprite))
                {
                    if (capReference.TryGetValue(c, out var cap)) sprite = cap;
                    var color = Sample(sprite, (p.x + .5f) / 16 - c.x - .5f, (p.y + .5f) / 16 - c.y - .5f);
                    int rgb = color.r << 16 | color.g << 8 | color.b;
                    return color.a > 127 && (rgb == 0x709276 || rgb == 0x7f9e7e || rgb == 0x52745a || rgb == 0x344e40) ? 2 : 0;
                }
                if (volume.Contains(c) && CliffSolid(p)) return 0;
                if (upper.Contains(c)) return 2;
                if (water.Contains(c)) return 0;
                if (basin.Contains(c)) return 3;
                return ground.Contains(c) ? 1 : 0;
            }

            void AddSprite(HashSet<Vector2Int> shape, Sprite sprite, Matrix4x4 transform)
            {
                if (sprite == null) return;
                var data = Read(sprite); int width = (int)sprite.rect.width;
                for (int i = 0; i < data.Length; i++)
                    if (data[i].a > 127)
                    {
                        var local = new Vector3((i % width + .5f - sprite.pivot.x) / sprite.pixelsPerUnit,
                            (i / width + .5f - sprite.pivot.y) / sprite.pixelsPerUnit);
                        shape.Add(Pixel(transform.MultiplyPoint3x4(local)));
                    }
            }

            HashSet<Vector2Int> Shape(Transform root)
            {
                var shape = new HashSet<Vector2Int>();
                foreach (var map in root.GetComponentsInChildren<Tilemap>(true))
                {
                    var renderer = map.GetComponent<TilemapRenderer>();
                    if (renderer == null || !renderer.enabled || renderer.sortingLayerName == "Ground") continue;
                    foreach (var c in map.cellBounds.allPositionsWithin)
                        if (map.HasTile(c)) AddSprite(shape, map.GetSprite(c), map.transform.localToWorldMatrix *
                            Matrix4x4.Translate(map.CellToLocal(c) + map.tileAnchor) * map.GetTransformMatrix(c));
                }
                foreach (var r in root.GetComponentsInChildren<SpriteRenderer>(true))
                    if (r.enabled && r.gameObject.activeInHierarchy && r.sprite != null &&
                        !r.name.ToLowerInvariant().Contains("shadow") && r.sprite.name != "WhiteSquare")
                        AddSprite(shape, r.sprite, r.transform.localToWorldMatrix);
                return shape;
            }

            void Project(string name, HashSet<Vector2Int> shape, bool contactOnly = false, Transform follower = null, bool fitting = false, string attachedName = null)
            {
                if (shape.Count == 0) return;
                int foot = shape.Min(p => p.y) + 1;
                var feet = shape.Where(p => p.y <= foot + 2).ToArray();
                int x = (feet.Min(p => p.x) + feet.Max(p => p.x)) / 2;
                int receiver = Receiver(new Vector2Int(x, foot));
                // Foundations can occupy a blocked seam; use the nearest supported exterior pixel.
                if (receiver == 0)
                    for (int d = 1; d <= 16 && receiver == 0; d++) receiver = Receiver(new Vector2Int(x, foot - d));
                if (receiver == 0) receiver = feet.Select(Receiver).FirstOrDefault(r => r != 0);
                var local = new Dictionary<Vector2Int, byte>();
                foreach (var p in shape)
                {
                    int height = Mathf.Max(0, p.y - foot);
                    if (!contactOnly)
                    {
                        var q = new Vector2Int(p.x + Mathf.RoundToInt(height * .28f), foot - Mathf.RoundToInt(height * .26f));
                        if (Receiver(q) == receiver && receiver != 0) Max(local, q, Cast); else rejected++;
                    }
                    if (height <= 3)
                        for (int dy = -2; dy <= 0; dy++)
                        {
                            var q = new Vector2Int(p.x + 1, foot + dy);
                            if (Receiver(q) == receiver && receiver != 0) Max(local, q, Contact);
                        }
                }
                sources.Add(new { name, footX = x / 16f, footY = foot / 16f, receiver, pixels = local.Count, dynamic = follower != null });
                if (local.Count == 0) return;
                if (follower != null)
                {
                    WriteMask(local, attachedName ?? (fitting ? "Fitting-shadow-" + fittingSources.ToString("D2") : "Pot-" + potSources.ToString("D2")), follower, receiver == 3 ? 44 : 55);
                    if (fitting) fittingSources++; else potSources++;
                }
                else
                {
                    foreach (var entry in local) Max(receiver == 3 ? wetMask : dryMask, entry.Key, entry.Value);
                    staticSources++;
                }
            }

            void CliffContacts()
            {
                if (!tutorial && !barrow)
                {
                    // Front rows and narrow side rims are both real height edges.
                    // A change of material alone never casts a terrain shadow.
                    foreach (var c in faces)
                        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
                        {
                            var p = new Vector2Int(c.x * 16 + x, c.y * 16 + y);
                            int source = FaceHeight(p), top;
                            if (source == int.MinValue || topHeights.TryGetValue(c, out top) && top >= source) continue;
                            for (int d = 1; d <= 3; d++)
                            {
                                var q = p + new Vector2Int((d + 1) / 2, -d);
                                int receiver = Receiver(q);
                                if (receiver != 0 && HeightForReceiver(receiver) < source)
                                    Max(receiver == 3 ? wetMask : dryMask, q, Cast);
                            }
                        }
                    foreach (var entry in topHeights)
                    {
                        var c = entry.Key;
                        int source = entry.Value, neighbor;
                        if (topHeights.TryGetValue(c + Vector3Int.right, out neighbor) && neighbor >= source) continue;
                        // Light falls southeast. The exposed east rim is the
                        // caster; receive only on genuinely lower, dry ground.
                        for (int y = 0; y < Ppu; y++)
                            for (int d = 1; d <= 3; d++)
                            {
                                var q = new Vector2Int((c.x + 1) * Ppu - 1 + d, c.y * Ppu + y - d);
                                int receiver = Receiver(q);
                                if (receiver != 0 && HeightForReceiver(receiver) < source)
                                    Max(receiver == 3 ? wetMask : dryMask, q, Cast);
                            }
                    }
                    return;
                }
                foreach (var c in volume)
                {
                    if (volume.Contains(c + Vector3Int.down) && volume.Contains(c + Vector3Int.right)) continue;
                    for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++)
                    {
                        var p = new Vector2Int(c.x * 16 + x, c.y * 16 + y);
                        if (!CliffSolid(p)) continue;
                        for (int d = 1; d <= 3; d++)
                        {
                            var q = p + new Vector2Int((d + 1) / 2, -d);
                            if (CliffSolid(q)) continue;
                            int receiver = Receiver(q);
                            if (receiver == 1 || receiver == 3) Max(receiver == 3 ? wetMask : dryMask, q, Cast);
                        }
                    }
                }
            }

            void WriteMask(Dictionary<Vector2Int, byte> mask, string name, Transform parent, int order)
            {
                int x0 = mask.Keys.Min(p => p.x), y0 = mask.Keys.Min(p => p.y);
                int w = mask.Keys.Max(p => p.x) - x0 + 1, h = mask.Keys.Max(p => p.y) - y0 + 1;
                var rgba = new Color32[w * h];
                foreach (var p in mask) rgba[(p.Key.y - y0) * w + p.Key.x - x0] = new Color32(23, 28, 27, p.Value);
                var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
                texture.SetPixels32(rgba); texture.Apply();
                string path = assetFolder + "/" + name + ".png";
                File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = Ppu; importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.BottomLeft; settings.spritePivot = Vector2.zero;
                settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
                var go = new GameObject(name); Undo.RegisterCreatedObjectUndo(go, "Bake ground shadows");
                go.transform.SetParent(parent, false); go.transform.position = new Vector3(x0 / 16f, y0 / 16f, 0);
                var r = go.AddComponent<SpriteRenderer>(); r.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                r.sortingLayerName = "Ground"; r.sortingOrder = order;
                files.Add(path);
            }

            void WriteChunks(Dictionary<Vector2Int, byte> mask, string prefix, int order)
            {
                foreach (var group in mask.GroupBy(p => new Vector2Int(Mathf.FloorToInt(p.Key.x / (float)Chunk), Mathf.FloorToInt(p.Key.y / (float)Chunk)))
                    .OrderBy(g => g.Key.y).ThenBy(g => g.Key.x))
                    WriteMask(group.ToDictionary(p => p.Key, p => p.Value), prefix + "-" + group.Key.x + "-" + group.Key.y, output, order);
            }

            public void Run()
            {
                if (tutorial) { RunTutorial(); return; }
                if (barrow) { RunBarrow(); return; }
                Directory.CreateDirectory(AssetFolder);
                Transform world = scene.GetRootGameObjects().Single(r => r.name == "Green Lowlands").transform;
                var architecture = world.Find("Gameplay/Sluice/Sluice architecture");
                var cutaway = architecture.GetComponent<BuildingCutaway>();
                // A stopped-mode cutaway preview may hide source silhouettes. Restore its
                // authored renderer states and clear that temporary cache before replacing shadows.
                // Restore only this component's artwork; do not toggle gameplay owners.
                if (cutaway != null) cutaway.RestoreAuthoredVisuals();
                var old = world.Find("Scenery/" + RootName);
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
                // Pot shadows belong to the Breakable, so breaking/loading removes them with the pot.
                foreach (var r in world.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.sprite != null &&
                    AssetDatabase.GetAssetPath(r.sprite).StartsWith(AssetFolder + "/")).ToArray()) Undo.DestroyObjectImmediate(r.gameObject);
                foreach (var r in world.GetComponentsInChildren<SpriteRenderer>(true))
                    if (r.name.ToLowerInvariant().Contains("shadow") && r.enabled)
                    {
                        Undo.RecordObject(r, "Replace legacy shadows"); r.enabled = false;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(r);
                    }
                var root = new GameObject(RootName); Undo.RegisterCreatedObjectUndo(root, "Bake Lowlands shadows");
                root.transform.SetParent(world.Find("Scenery"), false); output = root.transform;
                string[] families = { "Oak", "Birch", "Apple_Tree", "Farmhouse_Weathered", "Workshop_Weathered", "Watermill", "Woodshed_Weathered",
                    "Well_Disused", "Farm_Cart", "Plank_Bridge", "Fence_Weathered", "Reused_Stone_Post", "Boulder", "Boulder_Cluster",
                    "Fallen_Log", "Hedgerow", "Collapsed_Crate", "WoodenBucket", "SmallSageBush",
                    "Firewood_Rack", "Axe_Stump", "Fence_Horizontal", "GrainSack", "ClayJar" };
                foreach (var t in world.Find("Scenery").GetComponentsInChildren<Transform>(true).ToArray())
                {
                    if (PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject) != t.gameObject) continue;
                    string family = System.IO.Path.GetFileNameWithoutExtension(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject));
                    if (families.Contains(family)) Project(PathOf(t), Shape(t));
                }
                foreach (var r in world.Find("Gameplay").GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (!r.enabled || !r.gameObject.activeInHierarchy || r.sprite == null) continue;
                    string n = r.sprite.name;
                    if (n == "ClayPot")
                    {
                        var b = r.GetComponentInParent<Breakable>();
                        if (b == null) throw new InvalidOperationException("Pot missing Breakable");
                        Project(PathOf(r.transform), Shape(r.transform), false, b.transform);
                    }
                    else if (PathOf(r.transform).Contains("/Traveling trader/"))
                        // He walks a little (TraderWalker), so his shadow is a child that goes with him.
                        Project(PathOf(r.transform), Shape(r.transform), false, r.GetComponentInParent<Trader>().transform, false, "Trader-shadow");
                    else if (n == "TimberChest_Closed" || n == "ForgeCarving" || n == "RopeAnchor")
                        Project(PathOf(r.transform), Shape(r.transform), n == "TimberChest_Closed");
                    else if (n == "Machine_Fork" || n == "Machine_Crank" || n == "Machine_Catch")
                        Project(PathOf(r.transform), Shape(r.transform), false, r.transform, true);
                }
                foreach (string hall in new[] { "Main", "Distributor" })
                {
                    var shape = Shape(architecture.Find(hall + " hall roof"));
                    shape.UnionWith(Shape(architecture.Find(hall + " south facade")));
                    Project(hall + " sluice exterior", shape);
                }
                foreach (var r in architecture.GetComponentsInChildren<SpriteRenderer>())
                    if (r.enabled && r.name.StartsWith("Low wall -"))
                    {
                        // These simple sliced piers represent wall footprints; their long north/south
                        // dimension lies on the ground and must never be treated as object height.
                        Bounds b = r.bounds;
                        for (int y = Mathf.FloorToInt(b.min.y * 16); y < Mathf.CeilToInt(b.max.y * 16); y++)
                        for (int x = Mathf.FloorToInt(b.min.x * 16); x < Mathf.CeilToInt(b.max.x * 16); x++)
                        {
                            var p = new Vector2Int(x + 2, y - 3); int receiver = Receiver(p);
                            if (receiver == 1 || receiver == 3) Max(receiver == 3 ? wetMask : dryMask, p, Cast);
                        }
                    }
                var connections = world.Find("Gameplay/Regional connections");
                if (connections != null)
                    foreach (var t in connections.GetComponentsInChildren<Transform>(true))
                        if (t.name == "Broken smiths shelter" || t.name == "Door frame and sill" || t.name == "Door lintel overhead")
                            Project(PathOf(t), Shape(t));
                CliffContacts();
                WriteChunks(dryMask, "Ground", 55); WriteChunks(wetMask, "Basin", 44);
                File.WriteAllText(AssetFolder + "/Bake.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                    ppu = Ppu, light = "upper left", cast = Cast, contact = Contact, direction = new[] { .28f, -.26f },
                    staticSources, potSources, fittingSources, dryPixels = dryMask.Count, basinPixels = wetMask.Count, clipped = rejected, files, sources
                }, Newtonsoft.Json.Formatting.Indented));
                RepairInteriorShadowBindings(cutaway);
                AssetDatabase.Refresh();
                Debug.Log("Lowlands shadows: " + staticSources + " static sources, " + potSources + " pots, " + fittingSources + " fittings, " + files.Count + " masks.");
            }

            void RepairInteriorShadowBindings(BuildingCutaway cutaway)
            {
                if (cutaway == null) return;
                var serialized = new SerializedObject(cutaway);
                var property = serialized.FindProperty("interiorRenderers");
                var renderers = Enumerable.Range(0, property.arraySize)
                    .Select(i => property.GetArrayElementAtIndex(i).objectReferenceValue as Renderer)
                    .Where(r => r != null).Distinct().ToList();
                // The bake attaches fitting masks directly beneath their source renderer.
                // Follow that ownership and the generated asset folder, never arbitrary child art.
                foreach (var source in renderers.ToArray())
                    foreach (Transform child in source.transform)
                    {
                        var shadow = child.GetComponent<SpriteRenderer>();
                        if (shadow != null && shadow.sprite != null &&
                            AssetDatabase.GetAssetPath(shadow.sprite).StartsWith(AssetFolder + "/", StringComparison.Ordinal) &&
                            !renderers.Contains(shadow)) renderers.Add(shadow);
                    }
                Undo.RecordObject(cutaway, "Rebind interior fitting shadows");
                property.arraySize = renderers.Count;
                for (int i = 0; i < renderers.Count; i++)
                    property.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(cutaway);
            }

            void RunBarrow()
            {
                Directory.CreateDirectory(assetFolder);
                Transform world = scene.GetRootGameObjects().Single(r => r.name == "Barrow approach").transform;
                var old = Deep(world, RootName);
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
                foreach (var r in world.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.sprite != null &&
                    AssetDatabase.GetAssetPath(r.sprite).StartsWith(assetFolder + "/")).ToArray()) Undo.DestroyObjectImmediate(r.gameObject);
                var root = new GameObject(RootName); Undo.RegisterCreatedObjectUndo(root, "Bake Barrow shadows");
                root.transform.SetParent(Scenery(world), false); output = root.transform;
                foreach (Transform t in Deep(world, "Trees and old masonry")) Project(PathOf(t), Shape(t));
                foreach (var r in world.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (!r.enabled || !r.gameObject.activeInHierarchy || r.sprite == null) continue;
                    if (r.sprite.name == "TimberChest_Closed") Project(PathOf(r.transform), Shape(r.transform), true);
                    else if (r.sprite.name == "Machine_Crank") Project(PathOf(r.transform), Shape(r.transform), false, r.transform, true);
                }
                CliffContacts();
                WriteChunks(dryMask, "Ground", 55);
                File.WriteAllText(assetFolder + "/Bake.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                    ppu = Ppu, light = "upper left", cast = Cast, contact = Contact, direction = new[] { .28f, -.26f },
                    staticSources, fittingSources, dryPixels = dryMask.Count, clipped = rejected, files, sources
                }, Newtonsoft.Json.Formatting.Indented));
                AssetDatabase.Refresh();
                Debug.Log("Barrow approach shadows: " + staticSources + " static sources, " + files.Count + " masks.");
            }

            void RunTutorial()
            {
                Directory.CreateDirectory(assetFolder);
                Transform world = scene.GetRootGameObjects().Single(r => r.name == "Tutorial Zone").transform;
                var old = Deep(world, RootName);
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
                foreach (var r in world.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.sprite != null &&
                    AssetDatabase.GetAssetPath(r.sprite).StartsWith(assetFolder + "/")).ToArray()) Undo.DestroyObjectImmediate(r.gameObject);
                var root = new GameObject(RootName); Undo.RegisterCreatedObjectUndo(root, "Bake Tutorial shadows");
                root.transform.SetParent(Scenery(world), false); output = root.transform;
                // Flat clutter and living actors retain their existing presentation. These are the
                // standing environment families; their lower/overhead tiles form one silhouette.
                string[] families = { "Oak", "Birch", "Apple_Tree", "Cottage_Thatch", "Cottage_Clay", "Village_Well", "Woodshed",
                    "Firewood_Rack", "Axe_Stump", "Fallen_Log", "Boulder", "Boulder_Cluster", "Stone_Bridge", "Fence_Horizontal",
                    "Fence_Repaired", "Fence_Post", "Hedgerow", "Ruined_Wall", "Broken_Pillar", "ClayJar",
                    "GrainSack", "WoodenBucket" };
                // Route-edge trees are authored under Boundary Woodland too.
                // Find eligible prefab roots throughout the zone; the family list
                // and receiver clipping still decide what can cast a static shadow.
                foreach (var t in world.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.gameObject.activeInHierarchy || PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject) != t.gameObject) continue;
                    string family = Path.GetFileNameWithoutExtension(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject));
                    if (families.Contains(family)) Project(PathOf(t), Shape(t));
                    else if (family == "Fence_Vertical") Footprint(Shape(t));
                }
                // The bonfire now owns the same base sprite formerly placed as a separate ring.
                // Keep its contact-only shadow and exclude the animated flame from the silhouette.
                foreach (var bonfire in world.GetComponentsInChildren<Bonfire>(true))
                {
                    var baseTransform = bonfire.transform.Find("Base");
                    var renderer = baseTransform != null ? baseTransform.GetComponent<SpriteRenderer>() : null;
                    if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.sprite != null)
                        Project(PathOf(baseTransform), Shape(baseTransform), true);
                }
                // Trunk slices identify individual trees even where neighboring crowns overwrite
                // slices. A whole row can straddle different elevations, so it cannot share a foot.
                var treeSprites = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(
                    AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Tiles/Shared/Environment/Trees/Tiles/Oak_1_0.asset").sprite))
                    .OfType<Sprite>().Where(s => s.name == "Oak" || s.name == "Birch").ToDictionary(s => s.name);
                treeSprites["Hedgerow"] = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Tiles/Tutorial/Environment/TutorialStructures16.png")
                    .OfType<Sprite>().Single(s => s.name == "Hedgerow");
                Transform woodland = Deep(world, "Boundary Woodland"), backdrop = Deep(world, "Camera Backdrop - scenery only");
                foreach (var map in maps.Where(m => m.transform.parent == woodland || m.transform.parent == backdrop))
                    foreach (var c in map.cellBounds.allPositionsWithin)
                    {
                        var slice = map.GetSprite(c);
                        if (slice == null || !slice.name.EndsWith("_1_0")) continue;
                        var sprite = treeSprites[slice.name.Substring(0, slice.name.Length - 4)];
                        var shape = new HashSet<Vector2Int>();
                        AddSprite(shape, sprite, map.transform.localToWorldMatrix * Matrix4x4.Translate(
                            map.CellToLocal(c) + map.tileAnchor + new Vector3((sprite.pivot.x - 24) / Ppu, (sprite.pivot.y - 8) / Ppu)));
                        Project(PathOf(map.transform) + " " + c, shape);
                    }
                foreach (var r in world.GetComponentsInChildren<SpriteRenderer>(true))
                    if (r.enabled && r.gameObject.activeInHierarchy && r.sprite != null &&
                        (r.sprite.name == "Target_New" || r.sprite.name == "Target_Worn" || r.sprite.name == "Practice_Dummy"))
                        Project(PathOf(r.transform), Shape(r.transform));
                Project("Recall stone", Shape(Deep(world, "Recall Stone")));
                var gate = Deep(world, "Recall Seal Gate");
                var arch = Shape(gate.Find("Seal arch - Environment"));
                arch.UnionWith(Shape(gate.Find("Seal arch - Above Player")));
                Project("Seal arch", arch);
                Footprint(Shape(gate.Find("Masonry surrounds")));
                Project("Opening gate", Shape(gate.Find("Sealed timber gate")), false, gate, true, "Gate-shadow");
                var controller = new SerializedObject(gate.GetComponent<RoadOfTheOldKing.Tutorial.RecallSealGate>());
                controller.FindProperty("groundShadow").objectReferenceValue = gate.Find("Gate-shadow").GetComponent<SpriteRenderer>();
                controller.ApplyModifiedProperties();
                CliffContacts();
                WriteChunks(dryMask, "Ground", 55);
                File.WriteAllText(assetFolder + "/Bake.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                    ppu = Ppu, light = "upper left", cast = Cast, contact = Contact, direction = new[] { .28f, -.26f },
                    staticSources, gateSources = fittingSources, dryPixels = dryMask.Count, clipped = rejected, files, sources
                }, Newtonsoft.Json.Formatting.Indented));
                AssetDatabase.Refresh();
                Debug.Log("Tutorial shadows: " + staticSources + " static sources, " + fittingSources + " gate, " + files.Count + " masks.");
            }

            // North/south fences and masonry lie along the ground. Offset their real footprint
            // by only a few pixels instead of mistaking their length for object height.
            void Footprint(HashSet<Vector2Int> shape)
            {
                foreach (var p in shape)
                {
                    var q = p + new Vector2Int(2, -3);
                    if (!shape.Contains(q) && Receiver(q) != 0) Max(dryMask, q, Cast);
                }
            }

            public void Dispose() { foreach (var texture in textures.Values) UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
