using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
using static TheLostShrine.EditorTools.TileKitAssets;

namespace TheLostShrine.EditorTools
{
    /// <summary>Assembles authored terrain pixels into stock Unity assets, only in the Editor.</summary>
    public static class TutorialTerrainKitBuilder
    {
        public const string Root = "Assets/Art/Tiles/Tutorial/Terrain";
        public const string TemplatePath = "Assets/Art/Tiles/Tutorial/Templates/TutorialZoneTemplate.prefab";
        private static readonly string[] Materials = { "MeadowWater", "StoneWater", "DeepWater", "GrassLedge", "StoneLedge", "StoneWall" };
        private static readonly string[] MapNames = { "GrassLedge", "StoneLedge", "CliffFaces", "MeadowWater", "StoneWater", "DeepWater", "StoneWall", "Stairs and Ramps", "WaterDetails" };

        [MenuItem("Tools/The Lost Shrine/Build Tutorial Terrain Kit")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
            if (!AssetDatabase.LoadAssetAtPath<Tile>(TutorialGroundKitBuilder.CollisionPath))
                throw new InvalidOperationException("Build the Tutorial Ground kit first.");
            var previous = SceneManager.GetActiveScene();
            var scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scratch);
                var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/TileManifest.json")).entries;
                foreach (string folder in new[] { "Tiles", "Rules", "Animations", "Palettes" }) Directory.CreateDirectory(Root + "/" + folder);
                AssetDatabase.Refresh();
                var sprites = ImportSprites(Root + "/TutorialTerrain16.png", entries);
                var tiles = new Dictionary<string, Tile>();
                foreach (var entry in entries)
                {
                    var tile = GetOrCreate<Tile>(Root + "/Tiles/" + entry.name + ".asset");
                    tile.sprite = sprites[entry.name];
                    tile.colliderType = Tile.ColliderType.None;
                    tile.color = Color.white; tile.transform = Matrix4x4.identity;
                    EditorUtility.SetDirty(tile); tiles.Add(entry.name, tile);
                }
                var rules = CreateRules(entries, sprites);
                var animations = CreateAnimations(entries, sprites);
                var collision = AssetDatabase.LoadAssetAtPath<Tile>(TutorialGroundKitBuilder.CollisionPath);
                CreatePalettes(entries, tiles, rules, animations, collision);
                var template = CreateTemplate();
                PrefabUtility.SaveAsPrefabAsset(template, TemplatePath);
                Object.DestroyImmediate(template);
                AssetDatabase.SaveAssets();
                Debug.Log("Tutorial Terrain kit: 315 visual tiles, 6 RuleTiles, 3 AnimatedTiles, 3 palettes, shared zone template.");
            }
            finally
            {
                SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scratch, true);
            }
        }

        private static Dictionary<string, RuleTile> CreateRules(Entry[] entries, Dictionary<string, Sprite> sprites)
        {
            var result = new Dictionary<string, RuleTile>();
            foreach (string material in Materials)
            {
                var tile = GetOrCreate<RuleTile>(Root + "/Rules/Paint_" + material + ".asset");
                tile.m_TilingRules.Clear();
                var pieces = entries.Where(e => e.material == material && e.variant == 0).ToArray();
                tile.m_DefaultSprite = sprites[pieces[0].name];
                tile.m_DefaultColliderType = Tile.ColliderType.None;
                foreach (var entry in pieces)
                {
                    var fills = entry.mask == 255
                        ? entries.Where(e => e.material == material && e.mask == 255).Select(e => sprites[e.name]).ToArray()
                        : new[] { sprites[entry.name] };
                    // Keep broad surfaces calm: center texture is an occasional accent.
                    if (entry.mask == 255) fills = new[] { fills[0], fills[0], fills[1], fills[0], fills[0], fills[2], fills[0], fills[0], fills[3], fills[0], fills[0], fills[0] };
                    var rule = MakeRule(fills);
                    rule.m_Id = tile.m_TilingRules.Count;
                    for (int bit = 0; bit < (material == "StoneWall" ? 4 : 8); bit++)
                    {
                        if (bit >= 4 && ((entry.mask & (1 << (bit - 4))) == 0 || (entry.mask & (1 << ((bit - 3) % 4))) == 0)) continue;
                        rule.m_NeighborPositions.Add(Neighbors[bit]);
                        rule.m_Neighbors.Add((entry.mask & (1 << bit)) != 0
                            ? RuleTile.TilingRuleOutput.Neighbor.This : RuleTile.TilingRuleOutput.Neighbor.NotThis);
                    }
                    tile.m_TilingRules.Add(rule);
                }
                tile.UpdateNeighborPositions(); EditorUtility.SetDirty(tile); result.Add(material, tile);
            }
            return result;
        }

        private static Dictionary<string, AnimatedTile> CreateAnimations(Entry[] entries, Dictionary<string, Sprite> sprites)
        {
            var result = new Dictionary<string, AnimatedTile>();
            foreach (string material in new[] { "Ripple", "FallBody", "FallFoam" })
            {
                var tile = GetOrCreate<AnimatedTile>(Root + "/Animations/Animated_" + material + ".asset");
                tile.m_AnimatedSprites = entries.Where(e => e.material == material).OrderBy(e => e.variant).Select(e => sprites[e.name]).ToArray();
                tile.m_MinSpeed = tile.m_MaxSpeed = material == "Ripple" ? 1f : 1.25f;
                tile.m_AnimationStartTime = 0; tile.m_AnimationStartFrame = 0;
                tile.m_TileColliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile); result.Add(material, tile);
            }
            return result;
        }

        private static void CreatePalettes(Entry[] entries, Dictionary<string, Tile> tiles, Dictionary<string, RuleTile> rules, Dictionary<string, AnimatedTile> animations, Tile collision)
        {
            var root = new GameObject("Tutorial Terrain - Paint", typeof(Grid));
            var map = AddMap(root.transform, "Automatic brushes", 0);
            for (int i = 0; i < Materials.Length; i++) map.SetTile(new Vector3Int(i * 2, 0, 0), rules[Materials[i]]);
            int x = 0;
            foreach (var animation in animations.Values) { map.SetTile(new Vector3Int(x, -3, 0), animation); x += 2; }
            map.SetTile(new Vector3Int(6, -3, 0), tiles["FallLip_00"]);
            map.SetTile(new Vector3Int(0, -6, 0), collision);
            map.animationFrameRate = 4;
            SavePalette(root, Root + "/Palettes/TutorialTerrain_Paint.prefab");

            root = new GameObject("Tutorial Terrain - Structures", typeof(Grid));
            map = AddMap(root.transform, "Cliffs stairs ramps and walls", 0);
            var structures = new[] { "EarthCliff", "StoneCliff", "StoneStairs", "EarthRamp" };
            var rows = new[] { "Top", "Middle", "Foot" }; var columns = new[] { "Left", "Center", "Right" };
            for (int s = 0; s < structures.Length; s++) for (int r = 0; r < 3; r++) for (int c = 0; c < 3; c++)
                map.SetTile(new Vector3Int(s * 5 + c, -r, 0), tiles[structures[s] + "_" + rows[r] + "_" + columns[c]]);
            var walls = entries.Where(e => e.material == "StoneWall").ToArray();
            for (int i = 0; i < walls.Length; i++) map.SetTile(new Vector3Int(i % 8, -5 - i / 8, 0), tiles[walls[i].name]);
            map.SetTile(new Vector3Int(11, -5, 0), tiles["FallLip_00"]);
            map.SetTile(new Vector3Int(11, -6, 0), animations["FallBody"]);
            map.SetTile(new Vector3Int(11, -7, 0), animations["FallFoam"]);
            map.animationFrameRate = 4;
            SavePalette(root, Root + "/Palettes/TutorialTerrain_Structures.prefab");

            root = new GameObject("Tutorial Terrain - Individual Pieces", typeof(Grid));
            map = AddMap(root.transform, "All individual pieces", 0);
            int row = 0;
            foreach (string material in entries.Select(e => e.material).Distinct())
            {
                var pieces = entries.Where(e => e.material == material).ToArray();
                for (int i = 0; i < pieces.Length; i++) map.SetTile(new Vector3Int(i % 12, -row - i / 12, 0), tiles[pieces[i].name]);
                row += (pieces.Length + 11) / 12 + 1;
            }
            SavePalette(root, Root + "/Palettes/TutorialTerrain_IndividualPieces.prefab");
        }

        internal static GameObject CreateTemplate()
        {
            var root = TutorialGroundKitBuilder.CreateTemplate();
            root.name = "Tutorial Ground and Terrain Grid";
            var terrain = root.transform.Find("Terrain");
            Object.DestroyImmediate(terrain.GetComponent<TilemapRenderer>());
            Object.DestroyImmediate(terrain.GetComponent<Tilemap>());
            for (int i = 0; i < MapNames.Length; i++) AddMap(terrain, MapNames[i], 10 + i);
            terrain.Find("WaterDetails").GetComponent<Tilemap>().animationFrameRate = 4;
            return root;
        }

    }
}
