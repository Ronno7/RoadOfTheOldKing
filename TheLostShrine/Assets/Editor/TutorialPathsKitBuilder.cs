using System;
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
    public static class TutorialPathsKitBuilder
    {
        public const string Root = "Assets/Art/Tiles/Tutorial/Paths";

        [MenuItem("Tools/The Lost Shrine/Build Tutorial Paths Kit")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode.");
            var previous=SceneManager.GetActiveScene();
            var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scratch);
                foreach(var folder in new[]{"Tiles","Rules","Palettes"})Directory.CreateDirectory(Root+"/"+folder);
                AssetDatabase.Refresh();
                var entries=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"/TileManifest.json")).entries;
                var sprites=ImportSprites(Root+"/TutorialPaths16.png",entries);
                foreach(var e in entries)
                {
                    var tile=GetOrCreate<Tile>(Root+"/Tiles/"+e.name+".asset");
                    tile.sprite=sprites[e.name];tile.colliderType=Tile.ColliderType.None;
                    tile.color=Color.white;tile.transform=Matrix4x4.identity;EditorUtility.SetDirty(tile);
                }
                foreach(var material in new[]{"DirtLane","Footpath"})
                {
                    var tile=GetOrCreate<RuleTile>(Root+"/Rules/Paint_"+material+".asset");
                    tile.m_TilingRules.Clear();tile.m_DefaultColliderType=Tile.ColliderType.None;
                    var fills=entries.Where(e=>e.material==material&&e.mask==255).Select(e=>sprites[e.name]).ToArray();
                    tile.m_DefaultSprite=fills[0];
                    foreach(var e in entries.Where(e=>e.material==material&&e.variant==0))
                    {
                        var rule=MakeRule(e.mask==255?fills:new[]{sprites[e.name]});rule.m_Id=tile.m_TilingRules.Count;
                        for(int bit=0;bit<8;bit++)
                        {
                            if(bit>=4&&((e.mask&(1<<(bit-4)))==0||(e.mask&(1<<((bit-3)%4)))==0))continue;
                            rule.m_NeighborPositions.Add(Neighbors[bit]);
                            rule.m_Neighbors.Add((e.mask&(1<<bit))!=0?RuleTile.TilingRuleOutput.Neighbor.This:RuleTile.TilingRuleOutput.Neighbor.NotThis);
                        }
                        tile.m_TilingRules.Add(rule);
                    }
                    tile.UpdateNeighborPositions();EditorUtility.SetDirty(tile);
                }
                // One palette: everyday brushes above, optional manual shapes below.
                var palette=new GameObject("Tutorial Paths",typeof(Grid));var map=AddMap(palette.transform,"Village paths",0);
                map.SetTile(Vector3Int.zero,AssetDatabase.LoadAssetAtPath<RuleTile>(Root+"/Rules/Paint_DirtLane.asset"));
                map.SetTile(new Vector3Int(2,0,0),AssetDatabase.LoadAssetAtPath<RuleTile>(Root+"/Rules/Paint_Footpath.asset"));
                var paving=AssetDatabase.LoadAssetAtPath<RuleTile>(TutorialGroundKitBuilder.Root+"/Rules/Paint_Cobbles.asset");
                if(paving==null)throw new InvalidOperationException("Build Ground before Paths.");
                map.SetTile(new Vector3Int(4,0,0),paving);
                for(int i=0;i<entries.Length;i++)map.SetTile(new Vector3Int(i%10,-3-i/10-(i>=50?1:0),0),AssetDatabase.LoadAssetAtPath<Tile>(Root+"/Tiles/"+entries[i].name+".asset"));
                SavePalette(palette,Root+"/Palettes/TutorialPaths.prefab");
                var template=TutorialTerrainKitBuilder.CreateTemplate();
                PrefabUtility.SaveAsPrefabAsset(template,TutorialTerrainKitBuilder.TemplatePath);Object.DestroyImmediate(template);
                AssetDatabase.SaveAssets();
            }
            finally{SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scratch,true);}
        }
    }
}
