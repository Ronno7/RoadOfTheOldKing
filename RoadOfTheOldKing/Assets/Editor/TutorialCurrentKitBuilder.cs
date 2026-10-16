using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
using static RoadOfTheOldKing.EditorTools.TileKitAssets;

namespace RoadOfTheOldKing.EditorTools
{
    // Imports current Tutorial additions without touching the original kit or scene.
    public static class TutorialCurrentKitBuilder
    {
        public const string Root = "Assets/Art/Tiles/Tutorial/Current";
        public const string PrefabRoot = "Assets/Prefabs/Environment/Tutorial/Current";

        // Reuse the current art; keep original prefabs and all placed scene overrides intact.
        [MenuItem("Tools/Road of the Old King/Refresh Current Tutorial Painting Assets")]
        public static void RefreshPaintingAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Use stopped Edit Mode.");
            string regional = "Assets/Art/Tiles/GreenLowlands/";
            var revised = new HashSet<string>(new[] { "Cottage_Clay", "Cottage_Thatch", "Woodshed",
                "Stone_Bridge", "Old_Arch", "Broken_Pillar", "Ruined_Wall", "Hedgerow", "Firewood_Rack",
                "Fence_Vertical", "Fence_Repaired", "Fence_Post", "Gate_Closed", "Village_Well", "Fence_Horizontal" });
            var trees = new HashSet<string>(new[] { "Oak", "Birch", "Apple_Tree" });
            var props = TutorialEnvironmentKitBuilder.ReadCatalog().Where(p => !p.interactiveArt).ToArray();
            var sources = props.ToDictionary(p => p.name, p => Required<GameObject>(
                (trees.Contains(p.name) ? GreenLowlandsEnvironmentKitBuilder.NativePrefabRoot :
                TutorialEnvironmentKitBuilder.PrefabRoot) + "/" + p.name + ".prefab"));
            var replacements = new Dictionary<TileBase, TileBase>();
            foreach (var prop in props.Where(p => revised.Contains(p.name)))
                foreach (var map in VisualMaps(sources[prop.name]))
                    foreach (var c in map.cellBounds.allPositionsWithin)
                    {
                        var tile = map.GetTile(c); if (tile == null) continue;
                        string name = tile.name, folder = Root + "/Tiles/";
                        if (prop.name == "Village_Well" || prop.name == "Fence_Horizontal")
                        {
                            folder = regional + "Architecture/Tiles/";
                            name = name.Replace(prop.name, prop.name == "Village_Well" ? "Farm_Well" : "Farm_Fence");
                        }
                        var replacement = Required<Tile>(folder + name + ".asset");
                        if (replacement.colliderType != Tile.ColliderType.None)
                            throw new InvalidDataException("Visual tile has collision: " + name);
                        replacements[tile] = replacement;
                    }
            Directory.CreateDirectory(PrefabRoot);
            Directory.CreateDirectory(Root + "/Palettes");
            Directory.CreateDirectory("Assets/Prefabs/Tutorial/Current");
            Directory.CreateDirectory("Assets/Prefabs/WorldArt/Tutorial/Current");
            AssetDatabase.Refresh();
            var scratch = EditorSceneManager.NewPreviewScene();
            try
            {
                WritePalette(scratch, "TutorialCurrent_Environment", stamps => {
                    for (int i = 0; i < props.Length; i++)
                    {
                        string name = props[i].name;
                        var instance = (GameObject)PrefabUtility.InstantiatePrefab(sources[name], scratch);
                        try
                        {
                            foreach (var map in VisualMaps(instance))
                            {
                                foreach (var c in map.cellBounds.allPositionsWithin)
                                {
                                    var tile = map.GetTile(c); if (tile == null) continue;
                                    if (revised.Contains(name)) { tile = replacements[tile]; map.SetTile(c, tile); }
                                    stamps.SetTile(new Vector3Int(i % 4 * 12, -i / 4 * 10, 0) + c, tile);
                                }
                                if (revised.Contains(name)) PrefabUtility.RecordPrefabInstancePropertyModifications(map);
                            }
                            if (revised.Contains(name)) PrefabUtility.SaveAsPrefabAsset(instance, PrefabRoot + "/" + name + ".prefab");
                        }
                        finally { Object.DestroyImmediate(instance); }
                    }
                });
                WritePalette(scratch, "TutorialCurrent_GroundPaths", map => {
                    string[] ground = { "Grass", "DryGrass", "Earth", "DampEarth", "WornStone", "Cobbles", "TilledSoil" };
                    for (int i = 0; i < ground.Length; i++)
                        map.SetTile(new Vector3Int(i * 3, 0, 0), Required<TileBase>(regional + "Ground/Rules/Paint_" + ground[i] + ".asset"));
                    for (int i = 0; i < 2; i++)
                        map.SetTile(new Vector3Int(i * 3, -4, 0), Required<TileBase>(regional + "Paths/Rules/Paint_" + (i == 0 ? "DirtLane" : "Footpath") + ".asset"));
                    map.SetTile(new Vector3Int(9, -4, 0), Required<TileBase>(TutorialGroundKitBuilder.CollisionPath));
                });
                WritePalette(scratch, "TutorialCurrent_Terrain", map => {
                    string[] water = { "MeadowWater", "StoneWater", "DeepWater", "StoneWall" };
                    for (int i = 0; i < water.Length; i++)
                        map.SetTile(new Vector3Int(i * 4, 0, 0), Required<TileBase>(regional + "Terrain/Rules/Paint_" + water[i] + ".asset"));
                    map.SetTile(new Vector3Int(0, -5, 0), Required<TileBase>(Root + "/Rules/Paint_ShallowEarth.asset"));
                    map.SetTile(new Vector3Int(5, -5, 0), Required<TileBase>(Root + "/Rules/Paint_RaisedStone.asset"));
                    string[] rows = { "Foot", "Middle", "Top" }, cols = { "Left", "Center", "Right" };
                    for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
                        map.SetTile(new Vector3Int(x, -12 + y, 0), Required<TileBase>(regional + "Terrain/Tiles/EarthRamp_" + rows[y] + "_" + cols[x] + ".asset"));
                    map.SetTile(new Vector3Int(7, -10, 0), Required<TileBase>(regional + "Terrain/Tiles/FallLip_00.asset"));
                    map.SetTile(new Vector3Int(7, -11, 0), Required<TileBase>(regional + "Terrain/Animations/Animated_FallBody.asset"));
                    map.SetTile(new Vector3Int(7, -12, 0), Required<TileBase>(regional + "Terrain/Animations/Animated_FallFoam.asset"));
                    map.SetTile(new Vector3Int(10, -12, 0), Required<TileBase>(regional + "Terrain/Animations/Animated_Ripple.asset"));
                });
                foreach (string name in new[] { "PracticeStand_New", "PracticeStand_Worn", "TutorialPracticeDummy" })
                    WriteSpriteVariant(scratch, "Assets/Prefabs/" + (name == "TutorialPracticeDummy" ? "Combat/" : "Tutorial/") + name + ".prefab",
                        "Assets/Prefabs/Tutorial/Current/" + name + ".prefab");
                WriteSpriteVariant(scratch, TutorialEnvironmentKitBuilder.VisualRoot + "/FireRing_Visual.prefab",
                    "Assets/Prefabs/WorldArt/Tutorial/Current/FireRing_Visual.prefab");
                AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.ClosePreviewScene(scratch); }
            Debug.Log("Tutorial painting: three current palettes, 15 environment variants, three teaching variants and one fire-ring visual. Scenes and original prefabs preserved.");
        }

        static T Required<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidDataException("Missing current painting asset: " + path);
            return asset;
        }

        static IEnumerable<Tilemap> VisualMaps(GameObject root) => root.GetComponentsInChildren<Tilemap>(true)
            .Where(m => m.name == "Environment" || m.name == "Above Player");

        static void WritePalette(Scene scratch, string name, Action<Tilemap> paint)
        {
            string path = Root + "/Palettes/" + name + ".prefab";
            bool existing = File.Exists(path);
            var root = existing ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name, typeof(Grid));
            if (!existing) SceneManager.MoveGameObjectToScene(root, scratch);
            try
            {
                var map = existing ? root.GetComponentInChildren<Tilemap>() : AddMap(root.transform, "Current brushes and stamps", 0);
                map.ClearAllTiles(); paint(map); map.CompressBounds();
                var renderer = map.GetComponent<TilemapRenderer>();
                renderer.mode = TilemapRenderer.Mode.Individual; renderer.sortOrder = TilemapRenderer.SortOrder.TopRight;
                PrefabUtility.SaveAsPrefabAsset(root, path);
                var settings = AssetDatabase.LoadAllAssetsAtPath(path).OfType<GridPalette>().FirstOrDefault();
                if (settings == null) { settings = ScriptableObject.CreateInstance<GridPalette>(); settings.name = "Palette Settings"; AssetDatabase.AddObjectToAsset(settings, path); }
                settings.cellSizing = GridPalette.CellSizing.Manual; EditorUtility.SetDirty(settings);
            }
            finally { if (existing) PrefabUtility.UnloadPrefabContents(root); else Object.DestroyImmediate(root); }
        }

        static void WriteSpriteVariant(Scene scratch, string source, string destination)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Root + "/TutorialStructures16.png").OfType<Sprite>().ToDictionary(s => s.name);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(Required<GameObject>(source), scratch);
            try
            {
                foreach (var renderer in instance.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer.sprite == null) continue;
                    Sprite replacement;
                    if (renderer.sprite.name == "FireRing") replacement = Required<Sprite>(BonfireFlameBuilder.Root + "/LowlandsBonfireBase16.png");
                    else if (!sprites.TryGetValue(renderer.sprite.name, out replacement)) continue;
                    renderer.sprite = replacement; PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
                }
                PrefabUtility.SaveAsPrefabAsset(instance, destination);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [MenuItem("Tools/Road of the Old King/Build Current Tutorial Art")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Use stopped Edit Mode.");
            Directory.CreateDirectory(Root + "/Rules");
            Directory.CreateDirectory(Root + "/Tiles");
            AssetDatabase.Refresh();
            ImportCliffs("CliffManifest.json", "TutorialCliffs16.png", "ShallowEarth");
            ImportCliffs("StoneCliffManifest.json", "TutorialStoneCliffs16.png", "RaisedStone");
            ImportPieces("WoodshedManifest.json", "TutorialWoodshed16.png");
            ImportPieces("StructuresManifest.json", "TutorialStructures16.png");
            AssetDatabase.SaveAssets();
        }

        static void ImportPieces(string manifest, string atlas)
        {
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/" + manifest)).entries;
            var sprites = ImportSprites(Root + "/" + atlas, entries);
            foreach (var entry in entries)
            {
                if (entry.width != 16 || entry.height != 16) continue;
                var tile = GetOrCreate<Tile>(Root + "/Tiles/" + entry.name + ".asset");
                tile.sprite = sprites[entry.name]; tile.colliderType = Tile.ColliderType.None;
                tile.color = Color.white; tile.transform = Matrix4x4.identity;
                EditorUtility.SetDirty(tile);
            }
        }

        static void ImportCliffs(string manifest, string atlas, string name)
        {
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/" + manifest)).entries;
            var sprites = ImportSprites(Root + "/" + atlas, entries, anchorTopCell: true);
            var brush = GetOrCreate<World.RaisedCliffTile>(Root + "/Rules/Paint_" + name + ".asset");
            brush.crestLine = false;
            brush.sprites = new Sprite[256 * 4];
            foreach (var entry in entries) brush.sprites[entry.mask * 4 + entry.variant] = sprites[entry.name];
            EditorUtility.SetDirty(brush);
        }
    }
}
