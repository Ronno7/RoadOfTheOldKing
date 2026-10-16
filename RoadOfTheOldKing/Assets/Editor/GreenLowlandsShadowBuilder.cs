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
        public const string AssetFolder = "Assets/Art/Shadows/GreenLowlands";
        public const string RootName = "Ground shadows - baked";
        const int Ppu = 16, Chunk = 512;
        const byte Cast = 42, Contact = 64;

        [MenuItem("Tools/Road of the Old King/Shadows/Rebake Lowlands Ground Shadows")]
        public static void Build()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/GreenLowlands.unity")
                throw new InvalidOperationException("Open GreenLowlands in stopped Edit mode.");
            // Do not save or reload: an artist's unsaved placement is the input to this bake.
            using (var bake = new Bake(scene)) bake.Run();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        [MenuItem("Tools/Road of the Old King/Shadows/Rebake Tutorial Ground Shadows")]
        public static void BuildTutorial()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/Scenes/Tutorial.unity")
                throw new InvalidOperationException("Open Tutorial in stopped Edit mode.");
            using (var bake = new Bake(scene, true)) bake.Run();
            EditorSceneManager.MarkSceneDirty(scene);
        }

        [MenuItem("Tools/Road of the Old King/Shadows/Rebake Barrow Approach Shadows")]
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
                assetFolder = tutorial ? "Assets/Art/Shadows/Tutorial" : barrow ? "Assets/Art/Shadows/BarrowApproach" : AssetFolder;
                maps = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Tilemap>(true)).ToArray();
                ReadSurfaces();
            }

            static string PathOf(Transform t) => AnimationUtility.CalculateTransformPath(t, null);
            static Vector3Int Cell(Vector2Int p) => new Vector3Int(Mathf.FloorToInt(p.x / 16f), Mathf.FloorToInt(p.y / 16f), 0);
            static Vector2Int Pixel(Vector3 p) => new Vector2Int(Mathf.FloorToInt(p.x * Ppu), Mathf.FloorToInt(p.y * Ppu));
            static void Max(Dictionary<Vector2Int, byte> mask, Vector2Int p, byte alpha)
            {
                if (!mask.TryGetValue(p, out byte old) || old < alpha) mask[p] = alpha;
            }

            void ReadSurfaces()
            {
                var shallow = tutorial ? AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Tiles/Tutorial/Current/TutorialCliffs16.png")
                    .OfType<Sprite>().ToDictionary(s => s.name) : null;
                foreach (var map in maps)
                {
                    string n = map.name;
                    bool isGround = n != "Meadow Backdrop" && (n.StartsWith("Ground -") || n == "Border grass" || n == "Border lane" ||
                        PathOf(map.transform).Contains("/Arrival/Ground/") || n.StartsWith("Paths -"));
                    bool isUpper = n == "Terrain - upper meadow terraces";
                    bool isBasin = n == "Ground - exposed basin silt";
                    bool isWater = n == "Terrain - permanent water" || n == "Border river" || n == "Sluice - lower tailrace water";
                    bool isRaised = n == "Terrain - boundary uplands" || n == "Terrain - joined cliff crests";
                    bool isFace = n == "Sluice - basin stair masonry" ||
                        n == "Terrain - structural bank edges" || n == "Terrain - workshop masonry" ||
                        n == "Terrain - shard island masonry" || n == "Terrain - submerged stair supports";
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
                    if (!isGround && !isUpper && !isBasin && !isWater && !isRaised && !isFace) continue;
                    foreach (var c in map.cellBounds.allPositionsWithin)
                    {
                        if (!map.HasTile(c)) continue;
                        var w = Vector3Int.FloorToInt(map.CellToWorld(c) + Vector3.one * .01f); w.z = 0;
                        if (isGround) ground.Add(w);
                        if (isUpper) upper.Add(w);
                        if (isBasin) basin.Add(w);
                        if (isWater) water.Add(w);
                        if (isRaised)
                        {
                            raised[w] = map.GetSprite(c);
                            int depth = tutorial ? (int)raised[w].rect.height / Ppu - 1 : 3;
                            for (int y = 0; y <= depth; y++) volume.Add(w + Vector3Int.down * y);
                            // The bare-stone cap shares face colors. Its matching earth topology
                            // supplies the unambiguous top-surface mask, at the same top-cell pivot.
                            if (tutorial && n == "StoneLedge")
                                capReference[w] = shallow[raised[w].name.Replace("RaisedStone_", "ShallowEarth_")];
                        }
                        if (isFace)
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
                for (int y = 0; y <= 3; y++)
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

            // 0 = no receiver, 1 = low ground, 2 = upper turf, 3 = bed below animated water.
            int Receiver(Vector2Int p)
            {
                if (tutorial && ramps.Contains(p)) return 0;
                if (tutorial && water.Contains(Cell(p))) return 0;
                if (tutorial && upperPaint.Contains(p)) return 2;
                var c = Cell(p);
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
                    "Fallen_Log", "Hedgerow", "Collapsed_Crate_Visual", "WoodenBucket_Visual", "SmallSageBush" };
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
                    else if (n == "TimberChest_Closed" || n == "ForgeCarving" || n == "RopeAnchor" ||
                        PathOf(r.transform).Contains("/Traveling trader/")) Project(PathOf(r.transform), Shape(r.transform), n == "TimberChest_Closed");
                    else if (n == "Machine_Fork" || n == "Machine_Crank" || n == "Machine_Catch")
                        Project(PathOf(r.transform), Shape(r.transform), false, r.transform, true);
                }
                var architecture = world.Find("Gameplay/Sluice/Sluice architecture");
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
                AssetDatabase.Refresh();
                Debug.Log("Lowlands shadows: " + staticSources + " static sources, " + potSources + " pots, " + fittingSources + " fittings, " + files.Count + " masks.");
            }

            void RunBarrow()
            {
                Directory.CreateDirectory(assetFolder);
                Transform world = scene.GetRootGameObjects().Single(r => r.name == "Barrow approach").transform;
                var old = world.Find(RootName);
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
                foreach (var r in world.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.sprite != null &&
                    AssetDatabase.GetAssetPath(r.sprite).StartsWith(assetFolder + "/")).ToArray()) Undo.DestroyObjectImmediate(r.gameObject);
                var root = new GameObject(RootName); Undo.RegisterCreatedObjectUndo(root, "Bake Barrow shadows");
                root.transform.SetParent(world, false); output = root.transform;
                foreach (Transform t in world.Find("Trees and old masonry")) Project(PathOf(t), Shape(t));
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
                var old = world.Find(RootName);
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
                foreach (var r in world.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.sprite != null &&
                    AssetDatabase.GetAssetPath(r.sprite).StartsWith(assetFolder + "/")).ToArray()) Undo.DestroyObjectImmediate(r.gameObject);
                var root = new GameObject(RootName); Undo.RegisterCreatedObjectUndo(root, "Bake Tutorial shadows");
                root.transform.SetParent(world, false); output = root.transform;
                // Flat clutter and living actors retain their existing presentation. These are the
                // standing environment families; their lower/overhead tiles form one silhouette.
                string[] families = { "Oak", "Birch", "Apple_Tree", "Cottage_Thatch", "Cottage_Clay", "Village_Well", "Woodshed",
                    "Firewood_Rack", "Axe_Stump", "Fallen_Log", "Boulder", "Boulder_Cluster", "Stone_Bridge", "Fence_Horizontal",
                    "Fence_Repaired", "Fence_Post", "Hedgerow", "Ruined_Wall", "Broken_Pillar", "ClayJar_Visual",
                    "GrainSack_Visual", "WoodenBucket_Visual", "FireRing_Visual" };
                foreach (var t in world.Find("Environment").GetComponentsInChildren<Transform>(true))
                {
                    if (!t.gameObject.activeInHierarchy || PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject) != t.gameObject) continue;
                    string family = Path.GetFileNameWithoutExtension(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject));
                    if (families.Contains(family)) Project(PathOf(t), Shape(t), family == "FireRing_Visual");
                    else if (family == "Fence_Vertical") Footprint(Shape(t));
                }
                // Trunk slices identify individual trees even where neighboring crowns overwrite
                // slices. A whole row can straddle different elevations, so it cannot share a foot.
                var treeSprites = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(
                    AssetDatabase.LoadAssetAtPath<Tile>("Assets/Art/Tiles/GreenLowlands/Environment/Trees/Tiles/Oak_1_0.asset").sprite))
                    .OfType<Sprite>().Where(s => s.name == "Oak" || s.name == "Birch").ToDictionary(s => s.name);
                treeSprites["Hedgerow"] = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Tiles/Tutorial/Current/TutorialStructures16.png")
                    .OfType<Sprite>().Single(s => s.name == "Hedgerow");
                foreach (var map in maps.Where(m => m.transform.parent == world.Find("Boundary Woodland") ||
                    m.transform.parent == world.Find("Camera Backdrop - scenery only")))
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
                var interactive = world.Find("Interactive Objects");
                foreach (var r in interactive.GetComponentsInChildren<SpriteRenderer>(true))
                    if (r.enabled && r.gameObject.activeInHierarchy && r.sprite != null &&
                        (r.sprite.name == "Target_New" || r.sprite.name == "Target_Worn" || r.sprite.name == "Practice_Dummy"))
                        Project(PathOf(r.transform), Shape(r.transform));
                Project("Recall stone", Shape(interactive.Find("Recall Stone")));
                var gate = interactive.Find("Recall Seal Gate");
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
