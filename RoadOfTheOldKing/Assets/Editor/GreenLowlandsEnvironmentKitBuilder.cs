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
using Prop = RoadOfTheOldKing.EditorTools.TutorialEnvironmentKitBuilder.Prop;
using static RoadOfTheOldKing.EditorTools.TileKitAssets;

namespace RoadOfTheOldKing.EditorTools
{
    /// <summary>Regional sources plus direct references to retained Tutorial objects.</summary>
    public static class GreenLowlandsEnvironmentKitBuilder
    {
        public const string Root = "Assets/Art/Tiles/GreenLowlands/Environment";
        public const string PrefabRoot = "Assets/Prefabs/Environment/GreenLowlands";
        [Serializable] public class RegionalTree { public Prop art; public string[] colors; }
        [Serializable] public class Catalog { public Prop[] entries; public string[] reusedPrefabs; public RegionalTree[] regionalTrees; }
        public static Catalog ReadCatalog() => JsonUtility.FromJson<Catalog>(File.ReadAllText(Root+"/EnvironmentManifest.json"));
        public static string TilePath(string name,int x,int y) => Root+"/Tiles/"+name+"_"+x+"_"+y+".asset";

        public const string TreeRoot = Root+"/Trees";
        public const string NativePrefabRoot = PrefabRoot+"/Native";

        // Refresh authoring references without regenerating artwork or touching a placed scene.
        [MenuItem("Tools/Road of the Old King/Refresh Lowlands Environment Painting Assets")]
        public static void RefreshPaintingAssets()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Use Edit Mode.");
            var replacements=new Dictionary<string,string> {
                {"Watermill","Farm_Watermill"},{"Farm_Cart","Farm_Cart"},{"Plank_Bridge","Farm_Bridge"},
                {"Reused_Stone_Post","Farm_StonePost"},{"Farmhouse_Weathered","Farm_Cottage"},
                {"Workshop_Weathered","Farm_Barn"},{"Woodshed_Weathered","Farm_DryingShed"},
                {"Well_Disused","Farm_Well"},{"Fence_Weathered","Farm_Fence"}
            };
            var catalog=ReadCatalog();
            var treeNames=new HashSet<string>(catalog.regionalTrees.Select(t=>t.art.name));
            var names=catalog.entries.Select(p=>p.name).Concat(catalog.reusedPrefabs).ToArray();
            var sources=names.ToDictionary(n=>n,n=>AssetDatabase.LoadAssetAtPath<GameObject>(
                (replacements.ContainsKey(n)?PrefabRoot:TutorialEnvironmentKitBuilder.PrefabRoot)+"/"+n+".prefab"));
            if(sources.Any(p=>p.Value==null))throw new InvalidDataException("Missing environment source prefab.");
            // Validate every revised slice before writing any authoring assets.
            var tiles=new Dictionary<TileBase,TileBase>();
            foreach(string name in names.Where(n=>replacements.ContainsKey(n)||treeNames.Contains(n)))
                foreach(var map in sources[name].GetComponentsInChildren<Tilemap>(true).Where(m=>m.name=="Environment"||m.name=="Above Player"))
                    foreach(var c in map.cellBounds.allPositionsWithin)
                    {
                        var old=map.GetTile(c);if(old==null)continue;
                        string path=treeNames.Contains(name)?TreeRoot+"/Tiles/"+name+"_"+c.x+"_"+c.y+".asset":
                            "Assets/Art/Tiles/GreenLowlands/Architecture/Tiles/"+replacements[name]+"_"+c.x+"_"+c.y+".asset";
                        var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
                        if(tile==null||tile.colliderType!=Tile.ColliderType.None)throw new InvalidDataException("Missing visual tile: "+path);
                        tiles[old]=tile;
                    }
            Directory.CreateDirectory(NativePrefabRoot);
            string palettePath=Root+"/Palettes/GreenLowlandsEnvironment.prefab";
            var palette=PrefabUtility.LoadPrefabContents(palettePath);
            var scratch=EditorSceneManager.NewPreviewScene();
            try
            {
                var stamps=palette.GetComponentsInChildren<Tilemap>(true).Single();
                stamps.ClearAllTiles();
                for(int i=0;i<names.Length;i++)
                {
                    string name=names[i];var source=sources[name];
                    bool revised=replacements.ContainsKey(name)||treeNames.Contains(name);
                    var instance=(GameObject)PrefabUtility.InstantiatePrefab(source,scratch);
                    try
                    {
                        foreach(var map in instance.GetComponentsInChildren<Tilemap>(true).Where(m=>m.name=="Environment"||m.name=="Above Player"))
                        {
                            foreach(var c in map.cellBounds.allPositionsWithin)
                            {
                                var tile=map.GetTile(c);if(tile==null)continue;
                                if(revised){tile=tiles[tile];map.SetTile(c,tile);}
                                stamps.SetTile(new Vector3Int(i%4*12,-i/4*9,0)+c,tile);
                            }
                            if(revised)PrefabUtility.RecordPrefabInstancePropertyModifications(map);
                        }
                        if(revised)PrefabUtility.SaveAsPrefabAsset(instance,NativePrefabRoot+"/"+name+".prefab");
                    }
                    finally{Object.DestroyImmediate(instance);}
                }
                stamps.CompressBounds();
                PrefabUtility.SaveAsPrefabAsset(palette,palettePath);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(palette);
                EditorSceneManager.ClosePreviewScene(scratch);
            }
            Debug.Log("Lowlands authoring: 28 current object stamps and 12 native prefab variants; source prefabs and scenes preserved.");
        }

        // A separate import preserves Tutorial sources and the historical regional atlas.
        // Scene placement and collision are deliberately outside this asset-only operation.
        [MenuItem("Tools/Road of the Old King/Build Lowlands Regional Trees")]
        public static void BuildRegionalTrees()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Use Edit Mode.");
            var trees=ReadCatalog().regionalTrees;
            if(trees==null||trees.Length!=3)throw new InvalidDataException("Expected oak, birch and apple sources.");
            var zone=File.ReadAllText("../ArtSource/GreenLowlands/ZonePalette.json");
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
                        if(!source.LoadImage(File.ReadAllBytes("../ArtSource/GreenLowlands/Environment/"+prop.source+".png")))
                            throw new InvalidDataException(prop.source);
                        pixels=TutorialEnvironmentKitBuilder.PrepareSprite(source,prop,palette);
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
                File.WriteAllBytes(TreeRoot+"/LowlandsTrees16.png",atlas.EncodeToPNG());
                File.WriteAllText(TreeRoot+"/TreeManifest.json",JsonUtility.ToJson(new Manifest{entries=entries.ToArray()},true));
                AssetDatabase.Refresh();
                var sprites=ImportSprites(TreeRoot+"/LowlandsTrees16.png",entries.ToArray());
                foreach(var entry in entries.Where(e=>e.width==16&&e.height==16))
                {
                    var tile=GetOrCreate<Tile>(TreeRoot+"/Tiles/"+entry.name+".asset");
                    tile.sprite=sprites[entry.name];tile.colliderType=Tile.ColliderType.None;
                    tile.color=Color.white;tile.transform=Matrix4x4.identity;EditorUtility.SetDirty(tile);
                }
                AssetDatabase.SaveAssets();
                Debug.Log("Lowlands trees: three whole 48x64 sprites, 36 visual tiles; authored scenes and Tutorial unchanged.");
            }
            finally{Object.DestroyImmediate(atlas);}
        }

        [MenuItem("Tools/Road of the Old King/Legacy/Build Original Lowlands Environment Kit")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Use Edit Mode.");
            var catalog=ReadCatalog();
            var collisionTile=AssetDatabase.LoadAssetAtPath<Tile>(TutorialGroundKitBuilder.CollisionPath);
            if(!collisionTile)throw new InvalidOperationException("Build Ground first.");
            var reused=catalog.reusedPrefabs.Select(name=>AssetDatabase.LoadAssetAtPath<GameObject>(TutorialEnvironmentKitBuilder.PrefabRoot+"/"+name+".prefab")).ToArray();
            if(reused.Any(p=>!p))throw new InvalidOperationException("A retained Tutorial prefab is missing.");
            var swatches=File.ReadAllLines("../Docs/Art/Palettes/green-lowlands-field-and-old-road-v1.gpl")
                .Select(line=>line.Split((char[])null,StringSplitOptions.RemoveEmptyEntries))
                .Where(parts=>parts.Length>=3&&byte.TryParse(parts[0],out _))
                .Select(parts=>new Color32(byte.Parse(parts[0]),byte.Parse(parts[1]),byte.Parse(parts[2]),255)).ToArray();
            if(swatches.Length!=16)throw new InvalidDataException("Expected Field and Old Road palette.");
            var previous=SceneManager.GetActiveScene();
            var scratch=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            Texture2D atlas=null;
            try
            {
                SceneManager.SetActiveScene(scratch);
                foreach(string folder in new[]{Root+"/Tiles",Root+"/Palettes",PrefabRoot})Directory.CreateDirectory(folder);
                atlas=new Texture2D(512,256,TextureFormat.RGBA32,false);
                atlas.SetPixels32(new Color32[512*256]);
                var entries=new List<Entry>();int ax=2,ay=2,shelfHeight=0;
                foreach(var prop in catalog.entries)
                {
                    if(ax+prop.width+2>atlas.width){ax=2;ay+=shelfHeight+4;shelfHeight=0;}
                    if(ax+prop.width+2>atlas.width||ay+prop.height+2>atlas.height)throw new InvalidDataException("Atlas capacity exceeded.");
                    if(prop.swatches.Length>15)throw new InvalidDataException("Per-object color budget exceeded: "+prop.name);
                    var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
                    Color32[] pixels;
                    try
                    {
                        if(!source.LoadImage(File.ReadAllBytes("../ArtSource/GreenLowlands/Environment/"+prop.source+".png")))throw new InvalidDataException(prop.source);
                        pixels=TutorialEnvironmentKitBuilder.PrepareSprite(source,prop,swatches);
                    }
                    finally{Object.DestroyImmediate(source);}
                    var opaque=pixels.Where(p=>p.a!=0).ToArray();
                    if(opaque.Any(p=>p.a!=255||!swatches.Contains(p))||opaque.Distinct().Count()>prop.swatches.Length)throw new InvalidDataException("Palette/alpha mismatch: "+prop.name);
                    atlas.SetPixels32(ax,ay,prop.width,prop.height,pixels);
                    entries.Add(new Entry{name=prop.name,x=ax,y=ay,width=prop.width,height=prop.height});
                    for(int y=0;y<prop.height/16;y++)for(int x=0;x<prop.width/16;x++)
                        entries.Add(new Entry{name=prop.name+"_"+x+"_"+y,x=ax+x*16,y=ay+y*16,width=16,height=16});
                    ax+=prop.width+4;shelfHeight=Math.Max(shelfHeight,prop.height);
                }
                atlas.Apply();File.WriteAllBytes(Root+"/GreenLowlandsEnvironment16.png",atlas.EncodeToPNG());
                AssetDatabase.Refresh();
                var sprites=ImportSprites(Root+"/GreenLowlandsEnvironment16.png",entries.ToArray());
                var palette=new GameObject("GreenLowlandsEnvironment",typeof(Grid));
                var paletteMap=AddMap(palette.transform,"Complete object stamps",0);
                int index=0;
                foreach(var prop in catalog.entries)
                {
                    var root=new GameObject(prop.name,typeof(Grid));
                    var bases=AddMap(root.transform,"Environment",0,"World");
                    var overhead=AddMap(root.transform,"Above Player",100,"Player");
                    var collision=AddMap(root.transform,"Collision",0);
                    collision.gameObject.layer=LayerMask.NameToLayer("Environment");
                    collision.GetComponent<TilemapRenderer>().enabled=false;
                    collision.gameObject.AddComponent<TilemapCollider2D>();
                    var origin=new Vector3Int(index%4*12,-index/4*9,0);
                    for(int y=0;y<prop.height/16;y++)for(int x=0;x<prop.width/16;x++)
                    {
                        string name=prop.name+"_"+x+"_"+y;
                        var tile=GetOrCreate<Tile>(TilePath(prop.name,x,y));
                        tile.sprite=sprites[name];tile.colliderType=Tile.ColliderType.None;
                        tile.color=Color.white;tile.transform=Matrix4x4.identity;EditorUtility.SetDirty(tile);
                        // Near rail occludes; far rail and deck stay beneath the player.
                        bool above=prop.kind=="bridge"?y==0:y>=prop.overheadRow;
                        (above?overhead:bases).SetTile(new Vector3Int(x,y,0),tile);
                        paletteMap.SetTile(origin+new Vector3Int(x,y,0),tile);
                    }
                    foreach(var area in prop.collision)
                        for(int y=area.y;y<area.y+area.height;y++)for(int x=area.x;x<area.x+area.width;x++)
                            collision.SetTile(new Vector3Int(x,y,0),collisionTile);
                    PrefabUtility.SaveAsPrefabAsset(root,PrefabRoot+"/"+prop.name+".prefab");Object.DestroyImmediate(root);
                    index++;
                }
                // Palette cells point at existing assets. No cloned/recolored Tutorial art.
                foreach(var prefab in reused)
                {
                    var origin=new Vector3Int(index%4*12,-index/4*9,0);
                    foreach(string mapName in new[]{"Environment","Above Player"})
                    {
                        var map=prefab.transform.Find(mapName).GetComponent<Tilemap>();
                        foreach(var cell in map.cellBounds.allPositionsWithin)
                            if(map.HasTile(cell))paletteMap.SetTile(origin+cell,map.GetTile(cell));
                    }
                    index++;
                }
                SavePalette(palette,Root+"/Palettes/GreenLowlandsEnvironment.prefab");
                AssetDatabase.SaveAssets();
                Debug.Log("Green Lowlands Environment: "+catalog.entries.Length+" new prefabs, "+reused.Length+" retained Tutorial objects, one palette; authored scenes unchanged.");
            }
            finally
            {
                if(atlas)Object.DestroyImmediate(atlas);
                if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scratch,true);
            }
        }
    }
}
