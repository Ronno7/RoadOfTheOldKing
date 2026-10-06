using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoadOfTheOldKing.Player;
using RoadOfTheOldKing.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace RoadOfTheOldKing.EditorTools
{
    // Density is an editable authoring asset. Baking never repaints existing tiles or source sprites.
    public static class TutorialVegetationBuilder
    {
        public const string Root = "Assets/Art/Vegetation";
        public const string DensityPath = Root + "/TutorialDensity.png";
        public const string LayoutPath = Root + "/TutorialVegetation.asset";
        public const string LeafAtlas = Root + "/GrassLeaves16.png";
        const string TutorialAtlas = "Assets/Art/Tiles/Tutorial/DetailDecoration/TutorialDecoration16.png";
        const string LowlandsAtlas = "Assets/Art/Tiles/GreenLowlands/DetailDecoration/GreenLowlandsDecoration16.png";
        const int Size = 100;
        const float Spacing = .3125f;
        const float SurfaceClearance = .375f;

        [MenuItem("Tools/Road of the Old King/Vegetation/Bake Tutorial Density")]
        public static void Bake()
        {
            var mask = ReadMask();
            if (!File.Exists(DensityPath)) WriteSeed(mask);
            var density = ImportDensity();
            var species = ReadSpecies();
            var layout = AssetDatabase.LoadAssetAtPath<VegetationLayout>(LayoutPath);
            if (layout == null)
            {
                layout = ScriptableObject.CreateInstance<VegetationLayout>();
                AssetDatabase.CreateAsset(layout, LayoutPath);
            }
            Undo.RecordObject(layout, "Bake Tutorial vegetation");
            layout.origin = Vector2Int.zero;
            layout.density = density;
            layout.species = species;
            layout.plants = BuildPlants(mask, density.GetPixels32(), species, layout.seed);
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssetIfDirty(layout);

            var parent = mask.zone.Find("Interactive Objects");
            if (parent == null)
            {
                var container = new GameObject("Interactive Objects");
                Undo.RegisterCreatedObjectUndo(container, "Create vegetation container");
                container.transform.SetParent(mask.zone, false);
                parent = container.transform;
            }
            var fields = parent.GetComponentsInChildren<InteractiveVegetation>(true);
            var field = fields.FirstOrDefault(f => f.layout == layout || f.name == "Interactive Vegetation");
            if (field == null)
            {
                var go = new GameObject("Interactive Vegetation");
                Undo.RegisterCreatedObjectUndo(go, "Create Tutorial vegetation");
                go.transform.SetParent(parent, false);
                field = Undo.AddComponent<InteractiveVegetation>(go);
            }
            var shader = Shader.Find("RoadOfTheOldKing/InteractiveVegetation");
            if (shader == null) throw new InvalidOperationException("InteractiveVegetation shader is missing.");
            Undo.RecordObject(field, "Assign Tutorial vegetation");
            Undo.RecordObject(field.transform, "Align vegetation field");
            field.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            field.transform.localScale = Vector3.one;
            field.layout = layout;
            field.vegetationShader = shader;
            field.interactor = mask.scene.GetRootGameObjects()
                .SelectMany(r => r.GetComponentsInChildren<PlayerMovement>(true))
                .Select(p => p.transform).FirstOrDefault();
            field.Rebuild();
            EditorUtility.SetDirty(field);
            EditorSceneManager.MarkSceneDirty(mask.scene);
            Debug.Log("Tutorial vegetation: " + layout.plants.Length + " plants, " + species.Length +
                " species. Density preserved; scene is unsaved. " + ValidateLayout());
        }

        [MenuItem("Tools/Road of the Old King/Vegetation/Seed Tutorial Density")]
        public static void SeedDensity()
        {
            WriteSeed(ReadMask());
            Debug.Log("Tutorial density seeded from current surfaces and clearings. Bake to update vegetation.");
        }

        [MenuItem("Tools/Road of the Old King/Vegetation/Validate Tutorial Density")]
        public static void Validate() => Debug.Log(ValidateLayout());

        // Read-only: useful after hand-painting density or changing the Tutorial layout.
        public static string ValidateLayout()
        {
            var mask = ReadMask();
            var layout = AssetDatabase.LoadAssetAtPath<VegetationLayout>(LayoutPath);
            if (layout == null) return "No vegetation layout is baked.";
            int invalid = 0, overlaps = 0;
            var occupied = new Dictionary<Vector2Int, List<Vector2>>();
            foreach (var plant in layout.plants)
            {
                if (!mask.Allows(plant.position, SurfaceClearance) || plant.species < 0 ||
                    plant.species >= layout.species.Length) invalid++;
                if (!FarEnough(plant.position, occupied)) overlaps++;
                AddPosition(plant.position, occupied);
            }
            return "Layout validation: " + invalid + " excluded/invalid roots, " + overlaps + " spacing violations.";
        }

        static VegetationLayout.Plant[] BuildPlants(Mask mask, Color32[] pixels,
            VegetationLayout.Species[] species, int seed)
        {
            var plants = new List<VegetationLayout.Plant>();
            var occupied = new Dictionary<Vector2Int, List<Vector2>>();
            // Stable species slots: art refreshes must not change the deterministic placement recipe.
            int fan = species.Length > 3 ? 3 : -1;
            int reeds = species.Length > 4 ? 4 : -1;
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                if (!mask.allowed[x + y * Size]) continue;
                float density = pixels[x + y * Size].r / 255f;
                if (density <= 0f) continue;
                for (int slot = 0; slot < 4; slot++)
                {
                    uint key = unchecked((uint)(seed + (x + y * Size) * 17 + slot * 7919));
                    if (Random01(key) >= density) continue;
                    var p = new Vector2(x + .25f + (slot % 2) * .5f + (Random01(key + 1) - .5f) * .3f,
                        y + .25f + (slot / 2) * .5f + (Random01(key + 2) - .5f) * .3f);
                    p = new Vector2(Mathf.Round(p.x * 16f) / 16f, Mathf.Round(p.y * 16f) / 16f);
                    if (!mask.Allows(p, SurfaceClearance) || !FarEnough(p, occupied)) continue;
                    float variety = Random01(key + 3);
                    int kind = variety < .58f ? 0 : variety < .92f ? 1 : 2;
                    if (fan >= 0 && variety > .965f) kind = fan;
                    if (reeds >= 0 && variety > .72f && mask.NearWater(x, y)) kind = reeds;
                    plants.Add(new VegetationLayout.Plant { position = p, species = kind,
                        phase = Random01(key + 4) * Mathf.PI * 2f });
                    AddPosition(p, occupied);
                }
            }
            return plants.ToArray();
        }

        static float Random01(uint n)
        {
            unchecked { n ^= n >> 16; n *= 0x7feb352du; n ^= n >> 15; n *= 0x846ca68bu; n ^= n >> 16; }
            return (n & 0xffffffu) / 16777216f;
        }

        static bool FarEnough(Vector2 p, Dictionary<Vector2Int, List<Vector2>> occupied)
        {
            var cell = new Vector2Int(Mathf.FloorToInt(p.x / Spacing), Mathf.FloorToInt(p.y / Spacing));
            for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++)
                if (occupied.TryGetValue(cell + new Vector2Int(x, y), out var points))
                    foreach (var other in points) if ((other - p).sqrMagnitude < Spacing * Spacing - .00001f) return false;
            return true;
        }

        static void AddPosition(Vector2 p, Dictionary<Vector2Int, List<Vector2>> occupied)
        {
            var cell = new Vector2Int(Mathf.FloorToInt(p.x / Spacing), Mathf.FloorToInt(p.y / Spacing));
            if (!occupied.TryGetValue(cell, out var points)) occupied.Add(cell, points = new List<Vector2>());
            points.Add(p);
        }

        static void WriteSeed(Mask mask)
        {
            Directory.CreateDirectory(Root);
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
            {
                float density = 0f;
                if (mask.Allows(new Vector2(x + .5f, y + .5f), SurfaceClearance))
                {
                    int neighbors = 0;
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                        if (mask.CellAllowed(x + dx, y + dy)) neighbors++;
                    float broad = Mathf.PerlinNoise(x * .085f + 13.7f, y * .085f + 29.3f);
                    float fine = Mathf.PerlinNoise(x * .23f + 73.1f, y * .23f + 2.6f);
                    density = Mathf.Lerp(.3f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.18f, .8f, broad))) *
                        Mathf.Lerp(.75f, 1f, fine) * Mathf.Lerp(.3f, 1f, neighbors / 9f);
                }
                byte value = (byte)Mathf.RoundToInt(Mathf.Clamp01(density) * 255f);
                pixels[x + y * Size] = new Color32(value, value, value, 255);
            }
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
            try { texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(DensityPath, texture.EncodeToPNG()); }
            finally { Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(DensityPath, ImportAssetOptions.ForceSynchronousImport);
            ImportDensity();
        }

        static Texture2D ImportDensity()
        {
            AssetDatabase.ImportAsset(DensityPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(DensityPath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = false;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 128;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(DensityPath);
            if (texture.width != Size || texture.height != Size)
                throw new InvalidDataException("TutorialDensity.png must be exactly 100 x 100 pixels; each pixel represents one world cell.");
            return texture;
        }

        [MenuItem("Tools/Road of the Old King/Vegetation/Refresh Tutorial Leaf Art")]
        public static void RefreshSpecies()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit Mode.");
            var layout = AssetDatabase.LoadAssetAtPath<VegetationLayout>(LayoutPath);
            if (layout == null) throw new InvalidOperationException("Bake Tutorial density first.");
            var species = ReadSpecies();
            if (layout.plants.Any(p => p.species < 0 || p.species >= species.Length))
                throw new InvalidDataException("Existing layout has an unknown species slot.");
            Undo.RecordObject(layout, "Refresh Tutorial leaf art");
            layout.species = species;
            EditorUtility.SetDirty(layout);
            AssetDatabase.SaveAssetIfDirty(layout);
            foreach (var field in Object.FindObjectsByType<InteractiveVegetation>(FindObjectsSortMode.None))
                if (field.layout == layout) field.Rebuild();
            Debug.Log("Tutorial leaf art refreshed; density, plant records and scenes preserved.");
        }

        static VegetationLayout.Species[] ReadSpecies()
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(LeafAtlas).OfType<Sprite>().ToDictionary(s => s.name);
            Func<string, float, float, float, VegetationLayout.Leaf> leaf = (name, x, angle, phase) =>
                new VegetationLayout.Leaf { sprite = sprites[name], offset = new Vector2(x, 0),
                    restAngle = angle, phaseOffset = phase };
            var green = new Color32(139, 155, 94, 255);
            return new[] {
                new VegetationLayout.Species { leaves = new[] {
                    leaf("GrassLeaf_Left", -.03125f, 12, 0), leaf("GrassLeaf_Right", .03125f, -16, .7f) },
                    debrisColor = green },
                new VegetationLayout.Species { leaves = new[] {
                    leaf("GrassLeaf_LeftShort", -.03125f, 10, .2f), leaf("GrassLeaf_RightShort", .03125f, -13, 1) },
                    debrisColor = green },
                new VegetationLayout.Species { leaves = new[] { leaf("MeadowFlowers", 0, 0, 0) },
                    flexibility = .4f, debrisColor = new Color32(96, 114, 77, 255) },
                new VegetationLayout.Species { leaves = new[] {
                    leaf("GrassLeaf_Left", -.0625f, 20, .4f), leaf("GrassLeaf_RightShort", .0625f, -25, 1.3f) },
                    debrisColor = green },
                new VegetationLayout.Species { leaves = new[] { leaf("Meadow_Reeds", 0, 0, .3f) },
                    flexibility = .65f, debrisColor = new Color32(79, 112, 80, 255) }
            };
        }

        // Import preparation only: sample the generated source, map to the accepted four-color
        // grass ramp, and copy existing flowers/reeds unchanged. Never redraw source artwork.
        [MenuItem("Tools/Road of the Old King/Vegetation/Build Rooted Leaf Atlas")]
        public static void BuildLeafArt()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit Mode.");
            Directory.CreateDirectory(Root);
            var palette = new[] { new Color32(56,75,60,255), new Color32(96,114,77,255),
                new Color32(139,155,94,255), new Color32(190,202,130,255) };
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var atlas = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var output = new Color32[64 * 64];
            var entries = new List<TileKitAssets.Entry>();
            var pivots = new Dictionary<string, Vector2>();
            try
            {
                if (!source.LoadImage(File.ReadAllBytes("../ArtSource/Vegetation/GrassLeaves_Source.png")))
                    throw new InvalidDataException("Cannot read generated grass leaves.");
                var input = source.GetPixels32();
                var names = new[] { "GrassLeaf_Left", "GrassLeaf_Right", "GrassLeaf_LeftShort", "GrassLeaf_RightShort" };
                for (int n = 0; n < names.Length; n++)
                {
                    var frame = new RectInt((n % 2) * (source.width / 2), 0, source.width / 2, source.height);
                    var crop = OpaqueBounds(input, source.width, frame);
                    int h = n < 2 ? 18 : 14;
                    // Keep enough horizontal samples for the solid leaf face and narrow bottom stem.
                    int w = Mathf.Max(8, Mathf.RoundToInt((float)crop.width * h / crop.height));
                    int ox = n * 16 + (16 - w) / 2, oy = 1;
                    float rootSum = 0; int rootCount = 0;
                    for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                    {
                        int sx = crop.x + Mathf.Min(crop.width - 1, (int)((x + .5f) * crop.width / w));
                        int sy = crop.y + Mathf.Min(crop.height - 1, (int)((y + .5f) * crop.height / h));
                        var color = input[sx + sy * source.width];
                        if (color.a < 128) continue;
                        int best = 0, distance = int.MaxValue;
                        for (int c = 0; c < palette.Length; c++)
                        {
                            var p = palette[c];
                            int d = (color.r-p.r)*(color.r-p.r)+(color.g-p.g)*(color.g-p.g)+(color.b-p.b)*(color.b-p.b);
                            if (d < distance) { distance = d; best = c; }
                        }
                        output[ox + x + (oy + y) * 64] = palette[best];
                        if (y == 0) { rootSum += ox + x + .5f - n * 16; rootCount++; }
                    }
                    if (rootCount == 0) throw new InvalidDataException("Leaf has no sampled bottom root: " + names[n]);
                    entries.Add(new TileKitAssets.Entry { name = names[n], x = n * 16, y = 0, width = 16, height = 24 });
                    pivots.Add(names[n], new Vector2(rootSum / rootCount / 16f, oy / 24f));
                }
                CopyRootedSprite(TutorialAtlas, "MeadowFlowers", 0, 32, output, entries, pivots);
                CopyRootedSprite(LowlandsAtlas, "Meadow_Reeds", 16, 32, output, entries, pivots);
                atlas.SetPixels32(output); atlas.Apply();
                File.WriteAllBytes(LeafAtlas, atlas.EncodeToPNG());
            }
            finally { Object.DestroyImmediate(source); Object.DestroyImmediate(atlas); }
            TileKitAssets.ImportSprites(LeafAtlas, entries.ToArray());
            var importer = (TextureImporter)AssetImporter.GetAtPath(LeafAtlas);
            var factories = new SpriteDataProviderFactories(); factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var rects = provider.GetSpriteRects();
            foreach (var rect in rects) { rect.alignment = SpriteAlignment.Custom; rect.pivot = pivots[rect.name]; }
            provider.SetSpriteRects(rects); provider.Apply(); importer.SaveAndReimport();
            Debug.Log("Rooted vegetation atlas: two generated blades in 18px/14px sizes, copied flowers/reeds, 16 PPU, bottom pivots.");
        }

        static RectInt OpaqueBounds(Color32[] pixels, int stride, RectInt rect)
        {
            int minX = rect.xMax, minY = rect.yMax, maxX = -1, maxY = -1;
            for (int y = rect.yMin; y < rect.yMax; y++) for (int x = rect.xMin; x < rect.xMax; x++)
                if (pixels[x + y * stride].a >= 128)
                { minX = Math.Min(minX, x); minY = Math.Min(minY, y); maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y); }
            if (maxX < minX) throw new InvalidDataException("Empty vegetation art.");
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        static void CopyRootedSprite(string path, string name, int ox, int oy, Color32[] output,
            List<TileKitAssets.Entry> entries, Dictionary<string, Vector2> pivots)
        {
            var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First(s => s.name == name);
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                raw.LoadImage(File.ReadAllBytes(path)); var pixels = raw.GetPixels32(); var sr = sprite.rect;
                var crop = OpaqueBounds(pixels, raw.width, new RectInt((int)sr.x, (int)sr.y, (int)sr.width, (int)sr.height));
                for (int y = 0; y < crop.height; y++) for (int x = 0; x < crop.width; x++)
                    output[ox + x + (oy + y) * 64] = pixels[crop.x + x + (crop.y + y) * raw.width];
                entries.Add(new TileKitAssets.Entry { name = name, x = ox, y = oy, width = crop.width, height = crop.height });
                pivots.Add(name, new Vector2(.5f, 0));
            }
            finally { Object.DestroyImmediate(raw); }
        }


        sealed class Mask
        {
            public Scene scene;
            public Transform zone;
            public readonly bool[] allowed = new bool[Size * Size];
            public readonly bool[] water = new bool[Size * Size];
            public readonly List<Collider2D>[] colliders = new List<Collider2D>[Size * Size];
            public readonly List<Vector3> clearings = new List<Vector3>(); // x, y, radius
            public bool CellAllowed(int x, int y) => x >= 1 && y >= 1 && x < Size - 1 && y < Size - 1 && allowed[x + y * Size];
            public bool NearWater(int x, int y)
            {
                for (int dy = -2; dy <= 2; dy++) for (int dx = -2; dx <= 2; dx++)
                    if (x + dx >= 0 && y + dy >= 0 && x + dx < Size && y + dy < Size && water[x + dx + (y + dy) * Size]) return true;
                return false;
            }
            public bool Allows(Vector2 p, float clearance)
            {
                int x = Mathf.FloorToInt(p.x), y = Mathf.FloorToInt(p.y);
                if (!CellAllowed(x, y)) return false;
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    if (CellAllowed(x + dx, y + dy)) continue;
                    var nearest = new Vector2(Mathf.Clamp(p.x, x + dx, x + dx + 1), Mathf.Clamp(p.y, y + dy, y + dy + 1));
                    if ((p - nearest).sqrMagnitude < clearance * clearance) return false;
                }
                foreach (var c in clearings)
                    if (((Vector2)c - p).sqrMagnitude < c.z * c.z) return false;
                var candidates = colliders[x + y * Size];
                if (candidates != null) foreach (var c in candidates)
                    if ((c.ClosestPoint(p) - p).sqrMagnitude < .35f * .35f) return false;
                return true;
            }
        }

        static Mask ReadMask()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Bake vegetation in Edit Mode.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Tutorial.unity") throw new InvalidOperationException("Open Tutorial as the active scene first.");
            var zone = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Tutorial Zone");
            if (zone == null) throw new InvalidOperationException("Tutorial Zone is missing.");
            var grassTransform = zone.transform.Find("Ground/Grass");
            var grass = grassTransform != null ? grassTransform.GetComponent<Tilemap>() : null;
            if (grass == null) throw new InvalidOperationException("Tutorial Zone/Ground/Grass is missing.");
            var mask = new Mask { scene = scene, zone = zone.transform };
            for (int y = 1; y < Size - 1; y++) for (int x = 1; x < Size - 1; x++)
                mask.allowed[x + y * Size] = grass.HasTile(grass.WorldToCell(new Vector3(x + .5f, y + .5f)));
            foreach (var map in zone.GetComponentsInChildren<Tilemap>(false))
            {
                if (map == grass || !Visible(map)) continue;
                string relative = AnimationUtility.CalculateTransformPath(map.transform, zone.transform);
                if (relative.StartsWith("Ground/", StringComparison.Ordinal) || relative.StartsWith("Paths/", StringComparison.Ordinal) ||
                    relative.StartsWith("Terrain/", StringComparison.Ordinal) || relative == "Paths" || relative == "Terrain" ||
                    relative == "Detail Decoration" || relative.StartsWith("Detail Decoration/", StringComparison.Ordinal))
                {
                    bool water = relative.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0;
                    foreach (var cell in map.cellBounds.allPositionsWithin)
                    {
                        if (!map.HasTile(cell)) continue;
                        Vector3 a = map.CellToWorld(cell), b = map.CellToWorld(cell + Vector3Int.one);
                        int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) + .0001f));
                        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) + .0001f));
                        int maxX = Mathf.Min(Size - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) - .0001f) - 1);
                        int maxY = Mathf.Min(Size - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) - .0001f) - 1);
                        for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
                        { mask.allowed[x + y * Size] = false; if (water) mask.water[x + y * Size] = true; }
                    }
                }
            }
            Physics2D.SyncTransforms();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var collider in root.GetComponentsInChildren<Collider2D>(false))
                {
                    if (!collider.enabled || collider.isTrigger ||
                        (collider.attachedRigidbody != null && collider.attachedRigidbody.bodyType != RigidbodyType2D.Static)) continue;
                    var b = collider.bounds; b.Expand(.7f);
                    for (int y = Mathf.Max(0, Mathf.FloorToInt(b.min.y)); y <= Mathf.Min(Size - 1, Mathf.FloorToInt(b.max.y)); y++)
                    for (int x = Mathf.Max(0, Mathf.FloorToInt(b.min.x)); x <= Mathf.Min(Size - 1, Mathf.FloorToInt(b.max.x)); x++)
                    {
                        int index = x + y * Size;
                        if (mask.colliders[index] == null) mask.colliders[index] = new List<Collider2D>();
                        mask.colliders[index].Add(collider);
                    }
                }
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(false))
                {
                    if (component == null) continue;
                    float radius = component is Bonfire ? 2.2f : component is SceneSpawnPoint ? 1.5f :
                        component is PlayerMovement ? 1.8f : component is WorldPickup ? 1.4f :
                        component is AxePuzzleTarget ? 1.5f : component is SceneExit ? 1.1f : 0f;
                    if (radius > 0f) mask.clearings.Add(new Vector3(component.transform.position.x, component.transform.position.y, radius));
                }
            }
            return mask;
        }

        static bool Visible(Tilemap map)
        {
            var renderer = map.GetComponent<TilemapRenderer>();
            return renderer != null && renderer.enabled && map.gameObject.activeInHierarchy;
        }
    }
}
