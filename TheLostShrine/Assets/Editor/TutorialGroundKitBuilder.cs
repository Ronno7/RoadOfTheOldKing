using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;
using static TheLostShrine.EditorTools.TileKitAssets;

namespace TheLostShrine.EditorTools
{
    // Editor-only assembly of authored pixels and stock Unity Tile / RuleTile assets.
    // No custom tile behavior or generation work is needed in a player build.
    public static class TutorialGroundKitBuilder
    {
        public const string Root = "Assets/Art/Tiles/Tutorial/Ground";
        public const string CollisionPath = "Assets/Art/Tiles/Tutorial/Collision/Collision_Block.asset";
        private static readonly string[] Materials = { "Grass", "DryGrass", "Earth", "DampEarth", "WornStone", "Cobbles", "TilledSoil" };
        [MenuItem("Tools/The Lost Shrine/Build Tutorial Ground Kit")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Use Edit Mode.");
            var entries = JsonUtility.FromJson<Manifest>(File.ReadAllText(Root + "/TileManifest.json")).entries;
            foreach (string folder in new[] { "Tiles", "Rules", "Palettes" }) Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            var sprites = ImportSprites(Root + "/TutorialGround16.png", entries);
            var tiles = new Dictionary<string, Tile>();
            foreach (var entry in entries)
            {
                var tile = GetOrCreate<Tile>(Root + "/Tiles/" + entry.name + ".asset");
                tile.sprite = sprites[entry.name];
                tile.colliderType = Tile.ColliderType.None;
                tile.color = Color.white;
                tile.transform = Matrix4x4.identity;
                EditorUtility.SetDirty(tile);
                tiles.Add(entry.name, tile);
            }
            var rules = CreateRules(entries, sprites);
            var collision = GetOrCreate<Tile>(CollisionPath);
            collision.sprite = sprites["Grass_Fill_00"];
            collision.color = new Color(1f, .15f, .2f, .7f);
            collision.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(collision);
            CreatePalettes(entries, tiles, rules, collision);
            AssetDatabase.SaveAssets();
            Debug.Log("Tutorial Ground kit: 308 visual tiles, 7 RuleTiles, 2 palettes and shared collision brush.");
        }

        private static Dictionary<string, RuleTile> CreateRules(Entry[] entries, Dictionary<string, Sprite> sprites)
        {
            var result = new Dictionary<string, RuleTile>();
            foreach (string material in Materials)
            {
                var tile = GetOrCreate<RuleTile>(Root + "/Rules/Paint_" + material + ".asset");
                tile.m_TilingRules.Clear();
                var fills = entries.Where(e => e.material == material && e.mask == 255).Select(e => sprites[e.name]).ToArray();
                tile.m_DefaultSprite = fills[0];
                tile.m_DefaultColliderType = Tile.ColliderType.None;
                if (material == "Grass")
                {
                    // Bias the base toward quiet cells; occasional marks prevent wallpaper noise.
                    fills = Enumerable.Repeat(fills[0], 12).Concat(fills.Skip(1)).ToArray();
                    tile.m_TilingRules.Add(MakeRule(fills));
                }
                else foreach (var entry in entries.Where(e => e.material == material && e.variant == 0))
                {
                    var rule = MakeRule(entry.mask == 255 ? fills : new[] { sprites[entry.name] });
                    rule.m_Id = tile.m_TilingRules.Count;
                    for (int bit = 0; bit < 8; bit++)
                    {
                        if (bit >= 4)
                        {
                            int first = bit - 4, second = (first + 1) % 4;
                            if ((entry.mask & (1 << first)) == 0 || (entry.mask & (1 << second)) == 0) continue;
                        }
                        rule.m_NeighborPositions.Add(Neighbors[bit]);
                        rule.m_Neighbors.Add((entry.mask & (1 << bit)) != 0
                            ? RuleTile.TilingRuleOutput.Neighbor.This : RuleTile.TilingRuleOutput.Neighbor.NotThis);
                    }
                    tile.m_TilingRules.Add(rule);
                }
                tile.UpdateNeighborPositions();
                EditorUtility.SetDirty(tile);
                result.Add(material,tile);
            }
            return result;
        }

        private static void CreatePalettes(Entry[] entries, Dictionary<string,Tile> tiles, Dictionary<string,RuleTile> rules, Tile collision)
        {
            var automatic = new GameObject("Tutorial Ground - Paint");
            automatic.AddComponent<Grid>();
            var map = AddMap(automatic.transform,"Paint brushes",0);
            for (int i = 0; i < Materials.Length; i++) map.SetTile(new Vector3Int(i*2,0,0),rules[Materials[i]]);
            map.SetTile(new Vector3Int(0,-3,0),collision);
            SavePalette(automatic,Root + "/Palettes/TutorialGround_Paint.prefab");
            var manual = new GameObject("Tutorial Ground - Individual Pieces");
            manual.AddComponent<Grid>();
            map = AddMap(manual.transform,"Individual pieces",0);
            for (int m=0;m<Materials.Length;m++)
            {
                var pieces = entries.Where(e=>e.material==Materials[m]).ToArray();
                for (int i=0;i<pieces.Length;i++) map.SetTile(new Vector3Int(i%12,-m*6-i/12,0),tiles[pieces[i].name]);
            }
            SavePalette(manual,Root + "/Palettes/TutorialGround_IndividualPieces.prefab");
        }

        internal static GameObject CreateTemplate()
        {
            var root = new GameObject("Tutorial Zone Grid");
            root.AddComponent<Grid>();
            var ground = new GameObject("Ground");
            ground.transform.SetParent(root.transform,false);
            for (int i=0;i<Materials.Length;i++) AddMap(ground.transform,Materials[i],i);
            AddMap(root.transform,"Terrain",10);
            var paths = new GameObject("Paths");
            paths.transform.SetParent(root.transform,false);
            AddMap(paths.transform,"Footpath",20);
            AddMap(paths.transform,"DirtLane",21);
            AddMap(paths.transform,"Paving",22);
            var environment = AddMap(root.transform,"Environment",0,"World");
            environment.GetComponent<TilemapRenderer>().mode = TilemapRenderer.Mode.Individual;
            AddMap(root.transform,"Detail Decoration",30);
            var collision = AddMap(root.transform,"Collision",0);
            collision.gameObject.AddComponent<TilemapCollider2D>();
            collision.GetComponent<TilemapRenderer>().enabled = false;
            AddMap(root.transform,"Above Player",100,"Player");
            new GameObject("Interactive Objects").transform.SetParent(root.transform,false);
            return root;
        }

    }
}
