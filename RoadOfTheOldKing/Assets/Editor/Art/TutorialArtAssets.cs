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
    // Maintains Tutorial environment art and its layer-specific painting palettes.
    public static class TutorialArtAssets
    {
        public const string Root = "Assets/Art/Tiles/Tutorial/Environment";
        public const string TerrainRoot = "Assets/Art/Tiles/Tutorial/Terrain";
        public const string PrefabRoot = "Assets/Prefabs/Environment/Tutorial";

        // Read the canonical prefabs; refresh palettes without recreating inheritance or changing placed work.
        [MenuItem("Road of the Old King/Advanced/Asset Maintenance/Tutorial/Refresh Painting Palettes")]
        public static void RefreshPaintingAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Use stopped Edit Mode.");
            string regional = "Assets/Art/Tiles/Shared/";
            var props = EnvironmentArtCatalog.ReadCatalog().Where(p => !p.interactiveArt).ToArray();
            var sources = props.ToDictionary(p => p.name, p => Required<GameObject>(EnvironmentArtCatalog.PrefabPath(p.name)));
            Directory.CreateDirectory(PrefabRoot);
            Directory.CreateDirectory(Root + "/Palettes");
            Directory.CreateDirectory("Assets/Prefabs/Tutorial");
            AssetDatabase.Refresh();
            var scratch = EditorSceneManager.NewPreviewScene();
            try
            {
                WritePalette(scratch, "TutorialEnvironment", stamps => {
                    for (int i = 0; i < props.Length; i++)
                    {
                        foreach (var map in VisualMaps(sources[props[i].name]))
                            foreach (var c in map.cellBounds.allPositionsWithin)
                            {
                                var tile = map.GetTile(c); if (tile != null) stamps.SetTile(new Vector3Int(i % 4 * 12, -i / 4 * 10, 0) + c, tile);
                            }
                    }
                });
                WritePalette(scratch, "TutorialGroundPaths", map => {
                    string[] ground = { "Grass", "DryGrass", "Earth", "DampEarth", "WornStone", "Cobbles", "TilledSoil" };
                    for (int i = 0; i < ground.Length; i++)
                        map.SetTile(new Vector3Int(i * 3, 0, 0), Required<TileBase>(regional + "Ground/Rules/Paint_" + ground[i] + ".asset"));
                    for (int i = 0; i < 2; i++)
                        map.SetTile(new Vector3Int(i * 3, -4, 0), Required<TileBase>(regional + "Paths/Rules/Paint_" + (i == 0 ? "DirtLane" : "Footpath") + ".asset"));
                    map.SetTile(new Vector3Int(9, -4, 0), Required<TileBase>(GroundKitBuilder.CollisionPath));
                });
                WritePalette(scratch, "TutorialTerrain", map => {
                    string[] water = { "MeadowWater", "StoneWater", "DeepWater", "StoneWall" };
                    for (int i = 0; i < water.Length; i++)
                        map.SetTile(new Vector3Int(i * 4, 0, 0), Required<TileBase>(regional + "Terrain/Rules/Paint_" + water[i] + ".asset"));
                    map.SetTile(new Vector3Int(0, -5, 0), Required<TileBase>(TerrainRoot + "/Rules/Paint_ShallowEarth.asset"));
                    map.SetTile(new Vector3Int(5, -5, 0), Required<TileBase>(TerrainRoot + "/Rules/Paint_RaisedStone.asset"));
                    string[] rows = { "Foot", "Middle", "Top" }, cols = { "Left", "Center", "Right" };
                    for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
                        map.SetTile(new Vector3Int(x, -12 + y, 0), Required<TileBase>(regional + "Terrain/Tiles/EarthRamp_" + rows[y] + "_" + cols[x] + ".asset"));
                    map.SetTile(new Vector3Int(7, -10, 0), Required<TileBase>(regional + "Terrain/Tiles/FallLip_00.asset"));
                    map.SetTile(new Vector3Int(7, -11, 0), Required<TileBase>(regional + "Terrain/Animations/Animated_FallBody.asset"));
                    map.SetTile(new Vector3Int(7, -12, 0), Required<TileBase>(regional + "Terrain/Animations/Animated_FallFoam.asset"));
                    map.SetTile(new Vector3Int(10, -12, 0), Required<TileBase>(regional + "Terrain/Animations/Animated_Ripple.asset"));
                });
                AssetDatabase.SaveAssets();
            }
            finally { EditorSceneManager.ClosePreviewScene(scratch); }
            Debug.Log("Tutorial painting: three palettes from current independent prefabs. Prefabs and scenes preserved.");
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
            string layer = name == "TutorialTerrain" ? "Terrain" : name == "TutorialGroundPaths" ? "Paths" : "Environment";
            string path = "Assets/Art/Tiles/Tutorial/" + layer + "/Palettes/" + name + ".prefab";
            bool existing = File.Exists(path);
            var root = existing ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name, typeof(Grid));
            if (!existing) SceneManager.MoveGameObjectToScene(root, scratch);
            try
            {
                var map = existing ? root.GetComponentInChildren<Tilemap>() : AddMap(root.transform, "Brushes and stamps", 0);
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

        [MenuItem("Road of the Old King/Advanced/Asset Maintenance/Tutorial/Refresh Environment and Cliff Art")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Use stopped Edit Mode.");
            Directory.CreateDirectory(TerrainRoot + "/Rules");
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
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(TerrainRoot + "/" + manifest)).entries;
            var sprites = ImportSprites(TerrainRoot + "/" + atlas, entries, anchorTopCell: true);
            var brush = GetOrCreate<World.RaisedCliffTile>(TerrainRoot + "/Rules/Paint_" + name + ".asset");
            brush.crestLine = false;
            brush.sprites = new Sprite[256 * 4];
            foreach (var entry in entries) brush.sprites[entry.mask * 4 + entry.variant] = sprites[entry.name];
            EditorUtility.SetDirty(brush);
        }
    }
}
