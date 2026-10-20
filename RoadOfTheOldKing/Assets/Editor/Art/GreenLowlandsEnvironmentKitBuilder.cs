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
using Prop = RoadOfTheOldKing.EditorTools.EnvironmentArtCatalog.Prop;
using static RoadOfTheOldKing.EditorTools.TileKitAssets;

namespace RoadOfTheOldKing.EditorTools
{
    /// <summary>Regional atlas entries and shared scenery; placement resolves shared prefabs first.</summary>
    public static class GreenLowlandsEnvironmentKitBuilder
    {
        public const string Root = "Assets/Art/Tiles/GreenLowlands/Environment";
        public const string PrefabRoot = "Assets/Prefabs/Environment/GreenLowlands";
        [Serializable] public class RegionalTree { public Prop art; public string[] colors; }
        [Serializable] public class Catalog { public Prop[] entries; public string[] reusedPrefabs; public RegionalTree[] regionalTrees; }
        [Serializable] class TreeCatalog { public RegionalTree[] trees; }
        public static Catalog ReadCatalog()
        {
            var catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(Root + "/EnvironmentManifest.json"));
            catalog.regionalTrees = JsonUtility.FromJson<TreeCatalog>(File.ReadAllText(TreeRoot + "/SourceRecipe.json")).trees;
            return catalog;
        }

        public const string TreeRoot = "Assets/Art/Tiles/Shared/Environment/Trees";

        // Refresh authoring references without regenerating artwork or touching a placed scene.
        [MenuItem("Road of the Old King/Advanced/Asset Maintenance/Green Lowlands/Refresh Painting Palette")]
        public static void RefreshPaintingAssets()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Use Edit Mode.");
            var catalog=ReadCatalog();
            var regional=new HashSet<string>(catalog.entries.Select(p=>p.name));
            var names=catalog.entries.Select(p=>p.name).Concat(catalog.reusedPrefabs).ToArray();
            var sources=names.ToDictionary(n=>n,n=>AssetDatabase.LoadAssetAtPath<GameObject>(
                EnvironmentArtCatalog.PrefabPath(n, regional.Contains(n) ? PrefabRoot : EnvironmentArtCatalog.PrefabRoot)));
            if(sources.Any(p=>p.Value==null))throw new InvalidDataException("Missing current environment prefab.");
            string palettePath=Root+"/Palettes/GreenLowlandsEnvironment.prefab";
            var palette=PrefabUtility.LoadPrefabContents(palettePath);
            try
            {
                var stamps=palette.GetComponentsInChildren<Tilemap>(true).Single();
                stamps.ClearAllTiles();
                for(int i=0;i<names.Length;i++)
                {
                    foreach(var map in sources[names[i]].GetComponentsInChildren<Tilemap>(true).Where(m=>m.name=="Environment"||m.name=="Above Player"))
                        foreach(var c in map.cellBounds.allPositionsWithin)
                        {
                            var tile=map.GetTile(c);if(tile!=null)stamps.SetTile(new Vector3Int(i%4*12,-i/4*9,0)+c,tile);
                        }
                }
                stamps.CompressBounds();
                PrefabUtility.SaveAsPrefabAsset(palette,palettePath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(palette);
            }
            Debug.Log("Green Lowlands painting palette refreshed from regional and shared prefabs; scenes preserved.");
        }

        // Tree import preserves the accepted regional source recipe.
        // Scene placement and collision are deliberately outside this asset-only operation.
        [MenuItem("Road of the Old King/Advanced/Asset Maintenance/Shared/Refresh Tree Art")]
        public static void BuildRegionalTrees()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Use Edit Mode.");
            var trees=ReadCatalog().regionalTrees;
            if(trees==null||trees.Length!=3)throw new InvalidDataException("Expected oak, birch and apple sources.");
            var zone=File.ReadAllText("../ArtSource/Shared/Palettes/Meadow.json");
            Directory.CreateDirectory(TreeRoot+"/Tiles");
            var atlas=new Texture2D(160,68,TextureFormat.RGBA32,false);
            try
            {
                atlas.SetPixels32(new Color32[160*68]);
                var entries=new List<Entry>();
                for(int i=0;i<trees.Length;i++)
                {
                    var tree=trees[i];var prop=tree.art;
                    if(prop.width!=48||prop.height!=64||tree.colors.Length>8||tree.colors.Distinct().Count()!=tree.colors.Length)
                        throw new InvalidDataException("Tree footprint/color budget: "+prop.name);
                    var palette=tree.colors.Select(hex=>{
                        Color color;
                        if(!zone.Contains("\""+hex+"\"")||!ColorUtility.TryParseHtmlString("#"+hex,out color))
                            throw new InvalidDataException("Color outside shared zone palette: "+hex);
                        return (Color32)color;
                    }).ToArray();
                    if(prop.swatches.Any(index=>index<0||index>=palette.Length))throw new InvalidDataException("Tree palette indices.");
                    var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    Color32[] pixels;
                    try
                    {
                        if(!source.LoadImage(File.ReadAllBytes("../ArtSource/Shared/Environment/"+prop.source+".png")))
                            throw new InvalidDataException(prop.source);
                        pixels=EnvironmentArtCatalog.PrepareSprite(source,prop,palette);
                    }
                    finally{Object.DestroyImmediate(source);}
                    if(pixels.Any(p=>p.a!=0&&(p.a!=255||!palette.Contains(p))))throw new InvalidDataException("Tree palette/alpha.");
                    int ax=2+i*52,ay=2;
                    atlas.SetPixels32(ax,ay,48,64,pixels);
                    entries.Add(new Entry{name=prop.name,x=ax,y=ay,width=48,height=64});
                    for(int y=0;y<4;y++)for(int x=0;x<3;x++)
                        entries.Add(new Entry{name=prop.name+"_"+x+"_"+y,x=ax+x*16,y=ay+y*16,width=16,height=16});
                }
                atlas.Apply();
                File.WriteAllBytes(TreeRoot+"/Trees16.png",atlas.EncodeToPNG());
                File.WriteAllText(TreeRoot+"/TreeManifest.json",JsonUtility.ToJson(new Manifest{entries=entries.ToArray()},true));
                AssetDatabase.Refresh();
                var sprites=ImportSprites(TreeRoot+"/Trees16.png",entries.ToArray());
                foreach(var entry in entries.Where(e=>e.width==16&&e.height==16))
                {
                    var tile=GetOrCreate<Tile>(TreeRoot+"/Tiles/"+entry.name+".asset");
                    tile.sprite=sprites[entry.name];tile.colliderType=Tile.ColliderType.None;
                    tile.color=Color.white;tile.transform=Matrix4x4.identity;EditorUtility.SetDirty(tile);
                }
                AssetDatabase.SaveAssets();
                Debug.Log("Shared trees: three whole 48x64 sprites and 36 visual tiles; authored scenes unchanged.");
            }
            finally{Object.DestroyImmediate(atlas);}
        }

    }
}
